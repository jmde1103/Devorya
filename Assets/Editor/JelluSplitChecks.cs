using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class JelluSplitChecks
{
    const string Pending = "Devorya.JelluSplitChecks";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int assertions;
    static bool failed;
    static JelluSplitChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                Application.runInBackground = true;
                Time.timeScale = 1;
                EditorApplication.isPaused = false;
                Application.logMessageReceived += OnLog;
                var host = new GameObject("Split check host").AddComponent<PieceAnimationManager>();
                host.StartCoroutine(Guard(RunCases()));
            }
        };
    }
    public static void RunBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run this isolated check with -batchmode.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    static void OnLog(string text, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) failed = true;
    }
    static IEnumerator Guard(IEnumerator routine)
    {
        while (true)
        {
            object current;
            try { if (!routine.MoveNext()) break; current = routine.Current; }
            catch (Exception e) { Debug.LogException(e); failed = true; break; }
            yield return current;
        }
        string report = (failed ? "FAIL" : "PASS") + ": " + assertions + " split assertions; Play Mode, real pawn prefab and spawn animation.\n";
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/jellu-split-results.txt", report);
        Debug.Log(report);
        EditorApplication.Exit(failed ? 1 : 0);
    }
    static IEnumerator RunCases()
    {
        StatusRestrictionChecks.Run();
        var db = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/Devorya/Data/Piece/PieceDatabase.asset");
        var skillsDb = AssetDatabase.LoadAssetAtPath<UniqueSkillDatabase>("Assets/Devorya/Data/Skills/Unique/UniqueSkillDatabase.asset");
        var data = skillsDb.GetData(UniqueSkillType.JelluSplit);
        var statusDb = AssetDatabase.LoadAssetAtPath<StatusEffectDatabase>("Assets/Devorya/Data/StatusEffect/StatusEffectDatabase.asset");
        var attackDisabled = statusDb.GetData(StatusEffectType.AttackDisabled);
        Check(data.tooltipSections.Count == 1 && data.tooltipSections[0].statusEffectData == attackDisabled, "Attack-disabled tooltip section linked");
        Check(data.description.Contains("[공격불가]를 1턴") && !data.description.Contains("최대"), "Description updated");
        var bishop = db.GetData("Jellu Bishop");
        Check(data != null && data.skillName == "분열", "Split registered");
        Check(data.oncePerTurn && data.cooldownTurn == 0, "One ability per turn; consumed caster has no cooldown");
        Check(bishop.uniqueSkill == UniqueSkillType.JelluSplit, "Bishop default");
        Check(skillsDb.GetData(UniqueSkillType.JelluWall) != null && (int)UniqueSkillType.JelluWall == 5, "Wall preserved");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PiecePrefab.prefab");
        foreach (PieceTeam team in new[] { PieceTeam.Player, PieceTeam.Enemy })
        foreach (int count in new[] { 1, 9, 10 })
        {
            var root = new GameObject("Split fixture");
            var board = root.AddComponent<BoardManager>(); board.enabled = false;
            var manager = root.AddComponent<PieceManager>(); manager.enabled = false;
            var animation = root.AddComponent<PieceAnimationManager>();
            var validator = root.AddComponent<BattleMoveValidator>();
            var skill = root.AddComponent<BattleSkillManager>();
            Set(skill, "statusEffectDatabase", statusDb);
            var tiles = new Tile[board.Width, board.Height];
            for (int y = 0; y < board.Height; y++) for (int x = 0; x < board.Width; x++)
            {
                var go = new GameObject("Tile"); go.transform.SetParent(root.transform);
                go.transform.position = new Vector3(x, y, 0);
                go.AddComponent<SpriteRenderer>();
                tiles[x, y] = go.AddComponent<Tile>();
            }
            Set(board, "tiles", tiles);
            Set(manager, "boardManager", board); Set(manager, "pieceDatabase", db);
            Set(manager, "piecePrefab", prefab); Set(manager, "pieceParent", root.transform);
            Set(manager, "pieceAnimationManager", animation);
            manager.InitializePieceGrid(); validator.Initialize(board, manager);
            Set(skill, "pieceManager", manager); Set(skill, "boardManager", board); Set(skill, "battleMoveValidator", validator);
            Piece source = manager.SpawnPieceFromData(bishop, team, 2, 2, true, team == PieceTeam.Player);
            for (int n = 0; n < count - 1; n++) manager.SpawnJelluPawn(team, n % board.Width, n / board.Width);
            var originalPieces = new HashSet<Piece>();
            for (int y = 0; y < board.Height; y++) for (int x = 0; x < board.Width; x++)
                if (manager.GetPieceAt(x, y) != null) originalPieces.Add(manager.GetPieceAt(x, y));
            var ai = new BattleAIActionGenerator(board, manager, validator);
            var candidates = new List<BattleAIAction>(); ai.GenerateActions(team, candidates);
            Check(candidates.Exists(a => a.ActingPiece == source && a.UniqueSkillType == UniqueSkillType.JelluSplit) == (count < 10), "AI capacity predicate");
            Check(skill.CanUseUniqueSkillEffect(source) == (count < 10), "Preview capacity predicate");
            if (count == 10)
            {
                Check(!skill.TryUseUniqueSkill(source), "At limit rejected");
                Check(manager.GetPieceAt(2, 2) == source && manager.GetPieceCount(team) == 10, "At limit source retained");
                Check(skill.GetUniqueSkillFailureMessage(source, data) == "최대 기물 수를 초과하게 되어 분열을 사용할 수 없습니다.", "Capacity message");
            }
            else
            {
                var effects = (List<OwnedStatusEffectData>)typeof(Piece).GetField("statusEffects", Private).GetValue(source);
                effects.Add(new OwnedStatusEffectData(StatusEffectType.ActionDisabled, 1, 1));
                Check(!skill.TryUseUniqueSkill(source), "Action restriction blocks split"); effects.Clear();
                Set(manager, "piecePrefab", null);
                Check(!skill.TryUseUniqueSkill(source) && manager.GetPieceAt(2, 2) == source, "Missing prefab preserves source");
                Set(manager, "piecePrefab", prefab);
                Check(!manager.TrySplitJelluPiece(source, new Vector2Int(4, 5), attackDisabled), "Non-adjacent rejected");
                Check(!manager.TrySplitJelluPiece(source, new Vector2Int(2, 2), attackDisabled), "Same cell rejected");
                if (count == 1)
                {
                    var blockers = new List<Piece>();
                    PieceTeam other = team == PieceTeam.Player ? PieceTeam.Enemy : PieceTeam.Player;
                    for (int y = 1; y <= 3; y++) for (int x = 1; x <= 3; x++)
                        if (x != 2 || y != 2) blockers.Add(manager.SpawnJelluPawn(other, x, y));
                    Check(!skill.CanUseUniqueSkillEffect(source) && !skill.TryUseUniqueSkill(source), "Surrounded source cannot split");
                    Check(manager.GetPieceAt(2, 2) == source && manager.GetPieceCount(team) == 1, "No-space failure preserves caster");
                    Check(skill.GetUniqueSkillFailureMessage(source, data) == "인접한 빈칸이 없어 분열을 사용할 수 없습니다.", "No-space message");
                    foreach (Piece blocker in blockers) manager.RemovePiece(blocker);
                }
                if (team == PieceTeam.Player && count == 1)
                {
                    // Exercise the real player completion coroutine after its caster disappears.
                    var battle = root.AddComponent<BattleManager>(); battle.enabled = false;
                    Set(battle, "boardManager", board); Set(battle, "pieceManager", manager);
                    Set(battle, "battleSkillManager", skill); Set(battle, "battleMoveValidator", validator);
                    Set(battle, "selectedPiece", source); Set(battle, "isBattleEnded", true);
                    yield return (IEnumerator)typeof(BattleManager).GetMethod("UseSelectedPieceSkillRoutine", Private)
                        .Invoke(battle, new object[] { source, data });
                    Check((bool)typeof(BattleManager).GetField("hasUsedUniqueSkillThisTurn", Private).GetValue(battle), "Turn ability consumed");
                    Check(typeof(BattleManager).GetField("selectedPiece", Private).GetValue(battle) == null, "Removed caster deselected");
                    Check(!(bool)typeof(BattleManager).GetField("isActionAnimating", Private).GetValue(battle), "Input unlocked");
                }
                else Check(skill.TryUseUniqueSkill(source), "Split succeeds");
                Piece first = manager.GetPieceAt(2, 2);
                Check(first != source && first.CurrentPieceData == db.GetData("Jellu Pawn"), "Origin replaced by fresh pawn");
                Check(first.UniqueSkill == UniqueSkillType.JelluSynthesis, "Pawn receives own ability");
                Check(manager.GetPieceCount(team) == count + 1, "Net one piece added");
                var newborns = new List<Piece>();
                for (int y = 1; y <= 3; y++) for (int x = 1; x <= 3; x++)
                {
                    Piece p = manager.GetPieceAt(x, y);
                    if (p != null && !originalPieces.Contains(p)) newborns.Add(p);
                }
                Check(newborns.Count == 2, "Two adjacent pawns");
                yield return new WaitForSeconds(1.5f);
                Check(source == null, "Caster removed after frame");
                foreach (Piece p in newborns)
                {
                    Check(p.Team == team && p.PieceType == PieceType.Pawn, "Team/type preserved");
                    Check(p.HasStatusEffect(StatusEffectType.AttackDisabled) && p.CanMoveThisTurn && !p.CanAttackThisTurn, "Both pawns can move but cannot attack");
                    var pawnEffects = (List<OwnedStatusEffectData>)typeof(Piece).GetField("statusEffects", Private).GetValue(p);
                    Check(pawnEffects.Count == 1 && pawnEffects[0].remainingTurn == 1, "Exactly one turn of attack restriction");
                    p.ReduceActionRestrictionTurnAndRemoveExpired();
                    Check(!p.CanAttackThisTurn, "Attack restriction survives creation turn end");
                    p.ReduceStatusEffectTurnAndRemoveExpired();
                    Check(p.CanAttackThisTurn, "Attack restriction expires at next own turn start");
                    Vector3 end = tiles[p.X, p.Y].transform.position + new Vector3(0, 0.25f, 0);
                    Check(Vector3.Distance(p.transform.position, end) < 0.001f, "Spawn animation reaches destination");
                    Check(p.GetComponentInChildren<Spine.Unity.SkeletonAnimation>() != null, "Spine visual present");
                }
            }
            UnityEngine.Object.Destroy(root);
            yield return null;
        }
    }
    static void Set(object o, string field, object value) => o.GetType().GetField(field, Private).SetValue(o, value);
    static void Check(bool condition, string name) { assertions++; if (!condition) throw new Exception(name); }
}
