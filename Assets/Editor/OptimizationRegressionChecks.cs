using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Unity.Profiling;

// Uses disposable fixtures and does not load or save project scenes or assets.
public static class OptimizationRegressionChecks
{
    [MenuItem("Devorya/Validation/Run Optimization Regression Checks")]
    public static void Run()
    {
        var root = new GameObject("Optimization regression fixtures");
        root.SetActive(false);
        try
        {
            var board = root.AddComponent<BoardManager>();
            var manager = root.AddComponent<PieceManager>();
            var validator = root.AddComponent<BattleMoveValidator>();
            SetField(manager, "boardManager", board);
            validator.Initialize(board, manager);
            var grid = new Piece[board.Width, board.Height];
            SetField(manager, "pieces", grid);
            var pieces = new Piece[board.Width * board.Height];
            for (int i = 0; i < pieces.Length; i++)
            {
                var child = new GameObject("Fixture " + i);
                child.transform.SetParent(root.transform);
                pieces[i] = child.AddComponent<Piece>();
                SetProperty(pieces[i], "X", i % board.Width);
                SetProperty(pieces[i], "Y", i / board.Width);
            }
            var evaluator = new BattleAIActionEvaluator(null, validator);
            var evaluate = (Func<Piece, PieceType, Piece, Piece, float>)Delegate.CreateDelegate(
                typeof(Func<Piece, PieceType, Piece, Piece, float>), evaluator,
                typeof(BattleAIActionEvaluator).GetMethod("EvaluateBestAttackValueForSimulatedType", BindingFlags.Instance | BindingFlags.NonPublic));
            var random = new System.Random(20261019);
            var snapshot = new StringBuilder();
            var types = (PieceType[])Enum.GetValues(typeof(PieceType));
            var teams = (PieceTeam[])Enum.GetValues(typeof(PieceTeam));
            int checkedPieces = 0;
            for (int layout = 0; layout < 128; layout++)
            {
                for (int i = 0; i < pieces.Length; i++)
                {
                    Piece piece = pieces[i];
                    SetProperty(piece, "PieceType", types[random.Next(types.Length)]);
                    SetProperty(piece, "Team", teams[random.Next(teams.Length)]);
                    SetProperty(piece, "CanMove", random.Next(5) != 0);
                    SetField(piece, "temporaryMoveType", layout % 3 == 0 ? (PieceType?)types[random.Next(types.Length)] : null);
                    grid[piece.X, piece.Y] = random.Next(4) == 0 ? null : piece;
                }
                for (int i = 0; i < pieces.Length; i++)
                {
                    Piece piece = pieces[i];
                    if (grid[piece.X, piece.Y] == null) continue;
                    checkedPieces++;
                    var selectable = validator.GetSelectablePositions(piece);
                    var moves = validator.GetMovePositions(piece);
                    var attacks = validator.GetAttackPositions(piece);
                    Check(validator.HasAnySelectableTile(piece) == (selectable.Count != 0), "Availability");
                    Check(Equal(moves, selectable.FindAll(p => manager.GetPieceAt(p.x, p.y) == null)), "Move contents/order");
                    Check(Equal(attacks, selectable.FindAll(p => {
                        Piece target = manager.GetPieceAt(p.x, p.y);
                        return target != null && piece.IsEnemyOf(target);
                    })), "Attack contents/order");
                    Append(snapshot, selectable); Append(snapshot, moves); Append(snapshot, attacks);
                    Piece a = pieces[random.Next(pieces.Length)];
                    Piece b = layout % 2 == 0 ? null : pieces[random.Next(pieces.Length)];
                    foreach (PieceType type in types)
                    {
                        Append(snapshot, validator.GetAttackPositionsForSimulatedType(piece, type, a, b));
                        snapshot.Append(evaluate(piece, type, a, b).ToString("R", CultureInfo.InvariantCulture)).Append('|');
                    }
                    var retained = new List<Vector2Int>(moves);
                    validator.HasAnySelectableTile(pieces[(i + 1) % pieces.Length]);
                    validator.GetMovePositions(pieces[(i + 2) % pieces.Length]);
                    Check(Equal(moves, retained), "Returned list ownership");
                }
            }
            Check(validator.GetMovePositions(null).Count == 0, "Null move");
            Check(validator.GetAttackPositions(null).Count == 0, "Null attack");
            Check(!validator.HasAnySelectableTile(null), "Null availability");
            Check(validator.GetAttackPositionsForSimulatedType(null, PieceType.Rook, null, null).Count == 0, "Null simulation");
            Array.Clear(grid, 0, grid.Length);
            Piece actor = pieces[0];
            SetProperty(actor, "CanMove", true);
            SetProperty(actor, "PieceType", PieceType.Queen);
            SetField(actor, "temporaryMoveType", null);
            SetProperty(actor, "Team", PieceTeam.Player);
            grid[0, 0] = actor;
            SetProperty(pieces[4], "Team", PieceTeam.Enemy);
            grid[4, 0] = pieces[4];
            string hash;
            using (var sha = SHA256.Create())
                hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(snapshot.ToString()))).Replace("-", "");
            CheckTooltip(root.transform);
            CheckLightRefresh();
            string report = "PASS\nTooltip dynamic children/alignment: PASS\nLight hierarchy refresh: PASS\nChecked piece states: " + checkedPieces + "\nSnapshot SHA256: " + hash + "\n"
                + Measure("Availability", () => validator.HasAnySelectableTile(actor))
                + Measure("AI simulated attack", () => evaluate(actor, PieceType.Rook, null, null))
                + Measure("Move query (caller-owned list)", () => validator.GetMovePositions(actor));
            Directory.CreateDirectory("Logs/OptimizationValidation");
            File.WriteAllText("Logs/OptimizationValidation/latest.txt", report);
            UnityEngine.Debug.Log(report);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void CheckTooltip(Transform root)
    {
        var popup = new GameObject("Tooltip", typeof(RectTransform));
        popup.transform.SetParent(root, false);
        var ui = popup.AddComponent<TooltipPopupUI>();
        SetField(ui, "popupRoot", popup.GetComponent<RectTransform>());
        Action refresh = Bind(ui, "DisableTooltipRaycastTargets");
        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(popup.transform, false);
        refresh();
        Check(!background.GetComponent<Image>().raycastTarget, "Tooltip existing child");
        var section = new GameObject("Dynamic section", typeof(RectTransform), typeof(Image));
        section.transform.SetParent(popup.transform, false);
        refresh();
        Check(!section.GetComponent<Image>().raycastTarget, "Tooltip new inactive child");
        var baseRect = background.GetComponent<RectTransform>();
        var sectionRect = section.GetComponent<RectTransform>();
        SetField(ui, "baseTooltipBackground", baseRect);
        SetField(ui, "sectionParent", sectionRect);
        SetField(ui, "sectionGapBelowBase", 7f);
        Action align = Bind(ui, "AlignSectionParentBelowBaseTooltip");
        var a = new Vector3[4]; var b = new Vector3[4];
        for (int i = 0; i < 8; i++)
        {
            baseRect.sizeDelta = new Vector2(100 + i * 10, 50 + i * 5);
            baseRect.anchoredPosition = new Vector2(i * 13, i * 9);
            sectionRect.sizeDelta = new Vector2(70 + i * 7, 30 + i * 4);
            align(); baseRect.GetWorldCorners(a); sectionRect.GetWorldCorners(b);
            Check(Mathf.Abs(a[0].y - b[1].y - 7f) < 0.001f, "Tooltip vertical gap");
            Check(Mathf.Abs((a[0].x + a[3].x) - (b[1].x + b[2].x)) < 0.001f, "Tooltip horizontal alignment");
        }
        UnityEngine.Object.DestroyImmediate(section);
        refresh();
    }

    private static void CheckLightRefresh()
    {
        var source = new GameObject("Regression light source");
        var controllerRoot = new GameObject("Regression fireflies");
        controllerRoot.SetActive(false);
        try
        {
            source.AddComponent<FlameLightFlickerController>();
            var controller = controllerRoot.AddComponent<FireflyEnvironmentController>();
            SetField(controller, "currentSettings", new EnvironmentFireflySettings { useExternalLightAvoidance = true });
            Action refresh = Bind(controller, "RefreshExternalAvoidanceLights");
            var smallRoot = new GameObject("Small light"); smallRoot.transform.SetParent(source.transform);
            var small = smallRoot.AddComponent<Light2D>(); small.lightType = Light2D.LightType.Point; small.pointLightOuterRadius = 2f;
            refresh();
            var lights = (List<Light2D>)typeof(FireflyEnvironmentController).GetField("externalAvoidanceLights", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            Check(lights.Contains(small), "Initial light");
            var largeRoot = new GameObject("New large light"); largeRoot.transform.SetParent(source.transform);
            var large = largeRoot.AddComponent<Light2D>(); large.lightType = Light2D.LightType.Point; large.pointLightOuterRadius = 5f;
            refresh(); Check(lights.Contains(large) && !lights.Contains(small), "Largest newly added light");
            large.enabled = false;
            refresh(); Check(lights.Contains(small) && !lights.Contains(large), "Disabled light fallback");
            UnityEngine.Object.DestroyImmediate(smallRoot);
            refresh(); Check(!lights.Contains(small) && !lights.Contains(large), "Removed light");
        }
        finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(controllerRoot); }
    }
    private static Action Bind(object target, string method)
        => (Action)Delegate.CreateDelegate(typeof(Action), target, target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic));
    private static string Measure(string name, Action query)
    {
        for (int i = 0; i < 256; i++) query();
        var timer = new Stopwatch();
        using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc", 100000,
            ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
        {
            Check(recorder.Valid, "GC.Alloc recorder availability");
            timer.Start();
            for (int i = 0; i < 10000; i++) query();
            timer.Stop();
            recorder.Stop();
            Check(recorder.Count < recorder.Capacity, "GC.Alloc recorder capacity");
            long bytes = 0;
            for (int i = 0; i < recorder.Count; i++) bytes += recorder.GetSample(i).Value;
            return name + ": " + bytes + " bytes / 10000 calls (" + recorder.Count + " allocations), "
                + timer.Elapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture) + " ms\n";
        }
    }
    private static void SetField(object target, string name, object value)
        => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void SetProperty(object target, string name, object value) => target.GetType().GetProperty(name).SetValue(target, value);
    private static void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("Regression: " + name); }
    private static bool Equal(List<Vector2Int> a, List<Vector2Int> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
        return true;
    }
    private static void Append(StringBuilder output, List<Vector2Int> positions)
    {
        foreach (Vector2Int p in positions) output.Append(p.x).Append(',').Append(p.y).Append(';');
        output.Append('|');
    }
}
