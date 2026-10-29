using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AbsorbRelicChecks
{
    const string Pending = "AbsorbRelicChecks";
    const string Folder = "Assets/Devorya/Data/Relic/";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static readonly string[] Names = { "초기 가속 모듈", "잔류 추진 제어기", "흡수 과충전 장치" };
    static readonly BattleRelicType[] Types = { BattleRelicType.InitialAcceleration, BattleRelicType.ResidualPropulsion, BattleRelicType.AbsorbOvercharge };
    static AbsorbRelicChecks()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            new GameObject("RelicCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());
        };
    }
    static void Set(object o, string key, object value) => o.GetType().GetField(key, Flags).SetValue(o, value);
    static object Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, Flags).Invoke(o, args);
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    public static void Run()
    {
        Setup();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    public static void ValidateExisting()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    static void Setup()
    {
        string[] descriptions = {
            "변형하지 않은 데보리아 [폰]은 전투 중 첫 이동에 한해 앞으로 2칸 이동할 수 있습니다. 이동 경로는 비어 있어야 합니다.",
            "상대 기물 흡수에 성공하면 즉시 추가 이동 기회를 1회 얻습니다. 추가 이동으로는 공격할 수 없습니다.",
            "상대 기물 흡수에 성공하면 다음 내 턴 동안 최대 에너지가 1 증가합니다. 해당 턴이 끝나면 원래 값으로 돌아오며, 여러 번 흡수해도 중첩되지 않습니다."
        };
        string[] enNames = { "Initial Acceleration Module", "Residual Thrust Controller", "Absorption Overcharger" };
        string[] jaNames = { "初期加速モジュール", "残留推進制御装置", "吸収過充電装置" };
        string[] en = {
            "An untransformed Devorya [Pawn] can move forward two squares on its first move in battle. Both squares must be empty.",
            "Successfully absorbing an enemy piece immediately grants one extra move. This extra move cannot capture an enemy.",
            "Successfully absorbing an enemy piece increases maximum energy by 1 during your next turn. The bonus expires when that turn ends and does not stack."
        };
        string[] ja = {
            "変形していないデボリア[ポーン]は、戦闘中の初回移動に限り前方へ2マス移動できます。経路上の2マスは空いている必要があります。",
            "敵の駒の吸収に成功すると、即座に追加移動を1回得ます。この追加移動では攻撃できません。",
            "敵の駒の吸収に成功すると、次の自分のターン中、最大エネルギーが1増加します。そのターンの終了時に元に戻り、効果は重複しません。"
        };
        var collection = DevoryaLocalizationEditorUtility.GetOrCreateStringTableCollection("Battle_Relic");
        var db = AssetDatabase.LoadAssetAtPath<BattleRelicDatabase>(Folder + "BattleRelicDatabase.asset");
        var serialized = new SerializedObject(db);
        var list = serialized.FindProperty("relicDatas");
        var icon = AssetDatabase.LoadAssetAtPath<BattleRelicData>(Folder + "BattleRelic_AbsorbChanceAttackOncePerTurn.asset").iconSprite;
        for (int i = 0; i < Types.Length; i++)
        {
            string path = Folder + "유물_" + Names[i] + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<BattleRelicData>(path);
            if (data == null) { data = ScriptableObject.CreateInstance<BattleRelicData>(); AssetDatabase.CreateAsset(data, path); }
            data.relicType = Types[i]; data.relicName = Names[i]; data.description = descriptions[i];
            data.iconSprite = icon; data.enableUniqueSkillAfterAbsorb = false;
            string key = "relic." + DevoryaLocalizationEditorUtility.ToSnakeCase(Types[i].ToString());
            string[] locales = { "ko", "en", "ja" };
            string[] names = { Names[i], enNames[i], jaNames[i] };
            string[] texts = { descriptions[i], en[i], ja[i] };
            for (int j = 0; j < 3; j++)
            {
                var table = DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection, locales[j]);
                DevoryaLocalizationEditorUtility.SetTableValue(table, key + ".name", names[j]);
                DevoryaLocalizationEditorUtility.SetTableValue(table, key + ".description", texts[j]);
            }
            data.localizedRelicName = DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection, key + ".name");
            data.localizedDescription = DevoryaLocalizationEditorUtility.CreateLocalizedStringReference(collection, key + ".description");
            EditorUtility.SetDirty(data);
            bool exists = false;
            for (int j = 0; j < list.arraySize; j++) if (list.GetArrayElementAtIndex(j).objectReferenceValue == data) exists = true;
            if (!exists) list.GetArrayElementAtIndex(list.arraySize++).objectReferenceValue = data;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }
    static IEnumerator Guard()
    {
        var stack = new Stack<IEnumerator>(); stack.Push(Cases());
        while (stack.Count > 0)
        {
            object next;
            try { if (!stack.Peek().MoveNext()) { stack.Pop(); continue; } next = stack.Peek().Current; }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
            if (next is IEnumerator nested) stack.Push(nested); else yield return next;
        }
        Debug.Log("ABSORB_RELIC_PASS: " + checks + " assertions");
        EditorApplication.Exit(0);
    }
    static IEnumerator Cases()
    {
        var db = AssetDatabase.LoadAssetAtPath<BattleRelicDatabase>(Folder + "BattleRelicDatabase.asset");
        foreach (var type in Types)
        {
            var d = db.GetData(type); Check(d != null, "Registered relic " + type);
            var collection = DevoryaLocalizationEditorUtility.GetStringTableCollection("Battle_Relic");
            foreach (string locale in new[] { "ko", "en", "ja" })
            {
                var table = DevoryaLocalizationEditorUtility.GetOrCreateStringTable(collection, locale);
                string key = "relic." + DevoryaLocalizationEditorUtility.ToSnakeCase(type.ToString());
                Check(!string.IsNullOrEmpty(table.GetEntry(key + ".name").LocalizedValue) &&
                    !string.IsNullOrEmpty(table.GetEntry(key + ".description").LocalizedValue), "Translations " + locale);
            }
        }
        var f = new SynthesisAbilityChecks.Fixture();
        var b = f.root.AddComponent<BattleManager>(); b.enabled = false;
        Set(b, "boardManager", f.board); Set(b, "pieceManager", f.manager); Set(b, "battleMoveValidator", f.validator); Set(b, "battleSkillManager", f.skill);
        Set(b, "currentTurn", BattleTurn.Player); Set(b, "isPlayerDeploymentPhase", false);
        Set(b, "playerDefeatCondition", BattleDefeatConditionType.None); Set(b, "enemyDefeatCondition", BattleDefeatConditionType.None);
        f.validator.Initialize(f.board, f.manager, b);
        var relics = f.root.AddComponent<BattleRelicManager>(); Set(b, "battleRelicManager", relics);
        Set(relics, "relicSlots", new[] { db.GetData(Types[0]), db.GetData(Types[1]), db.GetData(Types[2]) });
        for (int y = 0; y < f.board.Height; y++) for (int x = 0; x < f.board.Width; x++)
        {
            typeof(Tile).GetProperty("X").SetValue(f.board.GetTile(x, y), x);
            typeof(Tile).GetProperty("Y").SetValue(f.board.GetTile(x, y), y);
        }
        var pawn = f.Spawn("Devorya Pawn", PieceTeam.Player, 0, 0);
        Check(f.validator.GetSelectablePositions(pawn).Contains(new Vector2Int(0, 2)), "First move double advance");
        pawn.AddStatusEffect(f.statuses.GetData(StatusEffectType.MoveDisabled));
        Check(!f.validator.GetSelectablePositions(pawn).Contains(new Vector2Int(0, 2)), "Move restriction respected");
        pawn.RemoveStatusEffect(StatusEffectType.MoveDisabled);
        var blocker = f.Spawn("Jellu Pawn", PieceTeam.Player, 0, 1);
        Check(!f.validator.GetSelectablePositions(pawn).Contains(new Vector2Int(0, 2)), "Cannot jump over piece");
        var transformed = f.Spawn("Jellu Pawn", PieceTeam.Player, 3, 0);
        Check(b.CanUseInitialAcceleration(transformed), "Other species pawn included");
        Check(f.validator.GetSelectablePositions(transformed).Contains(new Vector2Int(3, 2)), "Jellu pawn double move");
        var destinationBlocker = f.Spawn("Jellu Pawn", PieceTeam.Enemy, 3, 2);
        Check(!f.validator.GetSelectablePositions(transformed).Contains(new Vector2Int(3, 2)), "Double move cannot capture");
        var enemy = f.Spawn("Devorya Pawn", PieceTeam.Enemy, 3, 4);
        Check(!b.CanUseInitialAcceleration(enemy), "Enemy excluded");
        pawn.SetAbsorbedJelluVisual(true); Check(b.CanUseInitialAcceleration(pawn), "Absorbed pawn included"); pawn.SetAbsorbedJelluVisual(false);
        var firstMover = f.Spawn("Devorya Pawn", PieceTeam.Player, 4, 0);
        yield return f.manager.MovePieceRoutine(firstMover, 4, 2, false);
        Check(firstMover.HasMovedThisBattle && !b.CanUseInitialAcceleration(firstMover), "First move consumed");
        var actor = f.Spawn("Devorya Rook", PieceTeam.Player, 1, 0);
        Check(!b.CanUseInitialAcceleration(actor), "Non-pawn excluded");
        var slots = new[] { db.GetData(Types[0]), db.GetData(Types[1]), db.GetData(Types[2]) };
        Set(relics, "relicSlots", new BattleRelicData[3]);
        Check(!b.CanUseInitialAcceleration(pawn), "Relic required");
        Set(relics, "relicSlots", slots);
        f.Spawn("Jellu Pawn", PieceTeam.Enemy, 1, 2);
        int originalMax = b.MaxPlayerAbilityEnergy;
        Set(b, "isAbsorbMode", true);
        yield return (IEnumerator)Call(b, "ExecutePieceActionRoutine", actor, f.board.GetTile(1, 2));
        Check(actor.IsQuickStepMove && !actor.CanAttackThisTurn, "Successful absorption grants move only");
        Check(b.GetChanceAttackBonusPiece() == actor, "Same piece owns bonus");
        Check(b.MaxPlayerAbilityEnergy == originalMax, "No immediate energy increase");
        Check(f.validator.GetSelectablePositions(actor).TrueForAll(p => f.manager.GetPieceAt(p.x, p.y) == null), "No capture candidates");
        Check(!b.TryExecuteBattleAction(firstMover, new Vector2Int(4, 3)), "Other piece cannot use bonus");
        yield return (IEnumerator)Call(b, "ExecutePieceActionRoutine", actor, f.board.GetTile(1, 3));
        Check(!actor.IsQuickStepMove && b.GetChanceAttackBonusPiece() == null, "Bonus consumed exactly once");
        Check(b.MaxPlayerAbilityEnergy == originalMax, "Enemy turn has normal maximum");
        Call(b, "EndTurn");
        Check(b.MaxPlayerAbilityEnergy == originalMax + 1 && b.CurrentPlayerAbilityEnergy == originalMax + 1, "Next player turn maximum and refill");
        Call(b, "QueueAbsorbOvercharge", actor); Call(b, "QueueAbsorbOvercharge", actor);
        Call(b, "EndTurn");
        Check(b.MaxPlayerAbilityEnergy == originalMax && b.CurrentPlayerAbilityEnergy <= originalMax, "Expiry clamps remaining energy");
        Call(b, "EndTurn");
        Check(b.MaxPlayerAbilityEnergy == originalMax + 1, "Repeated absorptions reserve only plus one");
        Call(b, "EndTurn"); Call(b, "EndTurn");
        Check(b.MaxPlayerAbilityEnergy == originalMax, "No bonus leaks to subsequent turns");
        f.Dispose(); yield return null;
    }
}