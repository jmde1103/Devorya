using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class StatusRestrictionChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int assertions;

    [MenuItem("Devorya/Validation/Check Action Restriction Effects")]
    public static void Run()
    {
        assertions = 0;
        var root = new GameObject("Status restriction tests");
        root.SetActive(false);
        try
        {
            var board = root.AddComponent<BoardManager>();
            var manager = root.AddComponent<PieceManager>();
            var validator = root.AddComponent<BattleMoveValidator>();
            var battle = root.AddComponent<BattleManager>();
            Set(manager, "boardManager", board);
            var grid = new Piece[board.Width, board.Height];
            Set(manager, "pieces", grid);
            validator.Initialize(board, manager);
            Set(battle, "boardManager", board);
            Set(battle, "pieceManager", manager);
            Set(battle, "battleMoveValidator", validator);
            var actor = Create(root, 2, 2);
            var others = new List<Piece>();
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    if (x != 2 || y != 2) others.Add(Create(root, x, y));
            var db = AssetDatabase.LoadAssetAtPath<StatusEffectDatabase>("Assets/Devorya/Data/StatusEffect/StatusEffectDatabase.asset");
            var attack = db.GetData(StatusEffectType.AttackDisabled);
            var action = db.GetData(StatusEffectType.ActionDisabled);
            Check(attack != null && action != null, "Registered assets");
            Check(attack.durationTurn == 1 && action.durationTurn == 1, "One-turn defaults");
            Check(attack.effectName == "공격불가" && action.effectName == "행동불가", "Korean names");
            Check(attack.iconSprite != null && action.iconSprite != null, "Visible status icons");
            Check((int)StatusEffectType.Defence == 3 && (int)StatusEffectType.Degeneration == 1, "Existing enum IDs");

            var random = new System.Random(19102026);
            var generator = new BattleAIActionGenerator(board, manager, validator);
            var actions = new List<BattleAIAction>();
            int cases = 0;
            foreach (PieceTeam team in Enum.GetValues(typeof(PieceTeam)))
            foreach (PieceType type in Enum.GetValues(typeof(PieceType)))
            for (int layout = 0; layout < 24; layout++)
            {
                Array.Clear(grid, 0, grid.Length);
                Effects(actor).Clear();
                Property(actor, "Team", team); Property(actor, "PieceType", type);
                Property(actor, "CanMove", true);
                grid[2, 2] = actor;
                foreach (Piece p in others)
                {
                    Effects(p).Clear();
                    Property(p, "Team", (PieceTeam)random.Next(3));
                    Property(p, "PieceType", PieceType.Pawn);
                    if (random.Next(3) == 0) grid[p.X, p.Y] = p;
                }
                var normal = validator.GetSelectablePositions(actor);
                var emptyOnly = normal.FindAll(p => grid[p.x, p.y] == null);
                Effects(actor).Add(new OwnedStatusEffectData(StatusEffectType.AttackDisabled, 1, 1));
                Check(Equal(validator.GetSelectablePositions(actor), emptyOnly), "Attack lock preserves every empty move in order");
                Check(validator.GetAttackPositions(actor).Count == 0, "No captures");
                Check(actor.CanMove && actor.CanMoveThisTurn && !actor.CanAttackThisTurn, "Attack-only permissions");
                foreach (PieceType simulated in new[] { PieceType.Knight, PieceType.Bishop, PieceType.Rook })
                    Check(validator.GetAttackPositionsForSimulatedType(actor, simulated, null, null).Count == 0, "No simulated captures");
                generator.GenerateActions(team, actions);
                Check(!actions.Exists(a => a.ActingPiece == actor && a.ActionType == BattleAIActionType.Attack), "AI capture restriction");
                Effects(actor).Add(new OwnedStatusEffectData(StatusEffectType.ActionDisabled, 1, 1));
                Check(validator.GetSelectablePositions(actor).Count == 0, "No moves or captures");
                generator.GenerateActions(team, actions);
                Check(!actions.Exists(a => a.ActingPiece == actor), "AI skips disabled actor");
                Check(actor.CanMove && !actor.CanMoveThisTurn, "Base movement flag preserved");
                actor.ReduceStatusEffectTurnAndRemoveExpired();
                Check(Equal(validator.GetSelectablePositions(actor), normal), "Movement restored after expiry");
                cases++;
            }

            // One-turn effects expire at the next affected-team turn start.
            Array.Clear(grid, 0, grid.Length);
            Piece target = others.Find(p => p.X == 2 && p.Y == 4);
            Effects(actor).Clear(); Effects(target).Clear();
            Property(actor, "Team", PieceTeam.Player); Property(actor, "PieceType", PieceType.Rook);
            Property(target, "Team", PieceTeam.Enemy); Property(target, "PieceType", PieceType.King);
            grid[2, 2] = actor; grid[2, 4] = target;
            Check(validator.IsPieceCurrentlyThreatened(target), "Baseline rook threat");
            actor.AddStatusEffect(attack);
            Check(!validator.IsPieceCurrentlyThreatened(target), "Attack-disabled piece is not a threat");
            Set(battle, "currentTurn", BattleTurn.Player);
            Set(battle, "isPlayerDeploymentPhase", false);
            Check(!battle.TryExecuteBattleAction(actor, new Vector2Int(2, 4)), "Execution rejects a forbidden capture");
            target.AddStatusEffect(action);
            target.AddStatusEffect(db.GetData(StatusEffectType.Defence));
            Set(battle, "currentTurn", BattleTurn.Enemy);
            Call(battle, "UpdateStatusEffectTurnState", false);
            Check(!target.HasStatusEffect(StatusEffectType.ActionDisabled), "Expires at next own turn start");
            Check(!target.HasStatusEffect(StatusEffectType.Defence), "Defence expires at next own turn start");
            Call(battle, "UpdateStatusEffectTurnState", true);
            Check(!target.HasStatusEffect(StatusEffectType.ActionDisabled), "Turn end does not restore expired effect");
            Check(actor.HasStatusEffect(StatusEffectType.AttackDisabled), "Other team's status remains");
            Set(battle, "currentTurn", BattleTurn.Player);
            Check(!(bool)Call(battle, "HasNoActionablePieces", PieceTeam.Player), "Temporary restriction does not cause defeat");
            Call(battle, "UpdateStatusEffectTurnState", false);
            Check(actor.CanAttackThisTurn && validator.IsPieceCurrentlyThreatened(target), "Threat restored after expiry");
            actor.AddStatusEffect(action); actor.AddStatusEffect(action);
            Check(Effects(actor).Count == 1 && Effects(actor)[0].remainingTurn == 1, "Reapply refreshes without duplicating");
            Check(!battle.TryExecuteBattleAction(actor, new Vector2Int(2, 3)), "Execution rejects a forbidden empty move");
            Property(actor, "UniqueSkill", UniqueSkillType.JelluClone);
            Check(!actor.CanUseUniqueSkill(), "Action lock prevents active skill bypass");
            var skills = root.AddComponent<BattleSkillManager>();
            Set(skills, "pieceManager", manager);
            Set(skills, "battleMoveValidator", validator);
            Check(!skills.CanUseUniqueSkillEffect(actor), "Skill preview blocks action lock");
            Check(!skills.TryUseUniqueSkill(actor), "Direct skill execution blocks action lock");
            Property(actor, "CanMove", false);
            actor.ReduceStatusEffectTurnAndRemoveExpired();
            Check(!actor.CanMoveThisTurn, "Expiry preserves inherent immobility");
            // Duration two expires at the second affected-team turn start.
            foreach (PieceTeam affectedTeam in new[] { PieceTeam.Player, PieceTeam.Enemy })
            foreach (StatusEffectType effect in new[] { StatusEffectType.Defence, StatusEffectType.Degeneration, StatusEffectType.AttackDisabled, StatusEffectType.ActionDisabled })
            {
                Effects(actor).Clear(); Effects(target).Clear();
                Property(actor, "Team", affectedTeam);
                Effects(actor).Add(new OwnedStatusEffectData(effect, 2, 1));
                BattleTurn ownTurn = affectedTeam == PieceTeam.Player ? BattleTurn.Player : BattleTurn.Enemy;
                BattleTurn otherTurn = ownTurn == BattleTurn.Player ? BattleTurn.Enemy : BattleTurn.Player;
                Set(battle, "currentTurn", otherTurn);
                Call(battle, "UpdateStatusEffectTurnState", false);
                Call(battle, "UpdateStatusEffectTurnState", true);
                Check(Effects(actor)[0].remainingTurn == 2, "Opponent phases preserve duration");
                Set(battle, "currentTurn", ownTurn);
                Call(battle, "UpdateStatusEffectTurnState", false);
                Check(Effects(actor)[0].remainingTurn == 1, "Own start decrements duration once");
                Call(battle, "UpdateStatusEffectTurnState", true);
                Check(Effects(actor)[0].remainingTurn == 1, "Own end preserves remaining duration");
                Call(battle, "UpdateStatusEffectTurnState", false);
                Check(!actor.HasStatusEffect(effect), "Second own start expires effect");
                Call(battle, "UpdateStatusEffectTurnState", true);
                Check(!actor.HasStatusEffect(effect), "Second own end leaves expired effect absent");
                Effects(actor).Add(new OwnedStatusEffectData(effect, 1, 1));
                Call(battle, "UpdateStatusEffectTurnState", true);
                Check(actor.HasStatusEffect(effect), "One-turn effect survives application turn end");
                Set(battle, "currentTurn", otherTurn);
                Call(battle, "UpdateStatusEffectTurnState", false);
                Call(battle, "UpdateStatusEffectTurnState", true);
                Check(actor.HasStatusEffect(effect), "One-turn effect protects throughout opponent turn");
                Set(battle, "currentTurn", ownTurn);
                Call(battle, "UpdateStatusEffectTurnState", false);
                Check(!actor.HasStatusEffect(effect), "One-turn effect expires at next own turn start");
            }
            Effects(actor).Add(new OwnedStatusEffectData(StatusEffectType.Breakthrough, 1, 1));
            Call(battle, "UpdateStatusEffectTurnState", true);
            Check(actor.HasStatusEffect(StatusEffectType.Breakthrough), "Breakthrough does not tick at turn end");
            Call(battle, "UpdateStatusEffectTurnState", false);
            Check(!actor.HasStatusEffect(StatusEffectType.Breakthrough), "Breakthrough retains turn-start expiry");
            string report = "PASS: " + assertions + " assertions; " + cases + " board/type/team cases.\n"
                + "Assets, player/AI movement, captures, simulation, threats, duration, reapply and base flags verified.\n";
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/status-restrictions-results.txt", report);
            Debug.Log(report);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    private static Piece Create(GameObject root, int x, int y)
    {
        var child = new GameObject("Piece"); child.transform.SetParent(root.transform);
        var p = child.AddComponent<Piece>();
        Property(p, "X", x); Property(p, "Y", y); Property(p, "CanMove", true);
        return p;
    }
    private static List<OwnedStatusEffectData> Effects(Piece p) => (List<OwnedStatusEffectData>)typeof(Piece).GetField("statusEffects", Private).GetValue(p);
    private static void Set(object o, string name, object value) => o.GetType().GetField(name, Private).SetValue(o, value);
    private static void Property(object o, string name, object value) => o.GetType().GetProperty(name).SetValue(o, value);
    private static object Call(object o, string name, object value) => o.GetType().GetMethod(name, Private).Invoke(o, new[] { value });
    private static bool Equal(List<Vector2Int> a, List<Vector2Int> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
        return true;
    }
    private static void Check(bool ok, string name) { assertions++; if (!ok) throw new Exception(name); }
}
