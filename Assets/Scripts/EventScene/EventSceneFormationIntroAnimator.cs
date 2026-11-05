
using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

// <변경부분>
// Event Scene 편성 보드의 시각적 등장 연출 전용.
// 실제 타일/기물 생성과 RunState 복원 순서는 변경하지 않는다.
public class EventSceneFormationIntroAnimator : MonoBehaviour
{
    [Header("Formation Intro")]
    [SerializeField] private bool playIntro = true;

    // <변경부분>
    // 한 타일의 페이드 시간보다 등장 간격을 조금 길게 설정해
    // 각 타일이 하나씩 완성되는 느낌을 강조한다.
    [SerializeField, Min(0.01f)] private float tileFadeDuration = 0.20f;
    [SerializeField, Min(0f)] private float tileInterval = 0.22f;
    [SerializeField, Min(0f)] private float tileDropHeight = 0.25f;
    [SerializeField, Min(0f)] private float pieceStartDelay = 0.08f;
    [SerializeField, Min(0.01f)] private float pieceFadeDuration = 0.35f;

    // <변경부분>
    // BattleScene에서만 타일 등장 순서와
    // 한 번에 등장하는 타일 개수를 설정한다.
    // EventScene 5x2 편성 보드는 기존 순서를 유지한다.
    [Header("Battle Tile Intro")]
    [SerializeField]
    private bool randomizeBattleTileOrder = true;

    [SerializeField, Min(1)]
    private int battleTilesPerWave = 2;

    // <변경부분>
    // BattleScene에서 기물이 하나씩 등장하기 시작하는 간격.
    // EventScene의 기물 동시 등장에는 영향을 주지 않는다.
    [Header("Battle Piece Intro")]
    [SerializeField, Min(0f)]
    private float battlePieceInterval = 0.10f;

    // <변경부분>
    // BattleScene에서 Player 등장 후 Enemy 등장까지의 대기 시간.
    // 기존 EventScene의 전체 기물 동시 등장에는 영향을 주지 않는다.
    [Header("Battle Team Intro")]
    [SerializeField, Min(0f)]
    private float teamFadeInterval = 0.06f;

    private Coroutine introCoroutine;

    // <변경부분>
    // 여러 기물의 Born 애니메이션을 시간차로 재생하되,
    // 전투 진입 연출이 중단되면 모두 정리할 수 있도록 추적한다.
    private readonly List<Coroutine> activeBornCoroutines =
        new List<Coroutine>();

    private readonly List<FadeVisual> tileVisuals = new List<FadeVisual>();
    private readonly List<FadeVisual> pieceVisuals = new List<FadeVisual>();

    // <변경부분>
    // BattleScene에서 Player / Enemy 기물 등장 순서를 구분한다.
    // Enemy 목록에는 Neutral 기물도 포함해 누락되지 않도록 한다.
    private readonly List<FadeVisual> playerPieceVisuals =
        new List<FadeVisual>();

    private readonly List<FadeVisual> opponentPieceVisuals =
        new List<FadeVisual>();

    private bool separatePieceTeams = false;

    public bool IsPlaying { get; private set; }

    // <변경부분>
    // 모든 연출 대상을 숨기고 재생 준비가 완료된 상태.
    public bool IsPrepared { get; private set; }

    public void Play(BoardManager boardManager, PieceManager pieceManager)
    {
        // <변경부분>
        // 기존 EventScene은 준비와 재생을 한 번에 수행한다.
        // 따라서 기존 Play() 호출부는 수정할 필요가 없다.
        if (Prepare(boardManager, pieceManager, false))
        {
            PlayPrepared();
        }
    }

    // <변경부분>
    // BattleScene에서 카메라 확대 이전에 타일과 기물을 숨겨 둔다.
    // separateTeams = true일 때만 Player / Enemy 순차 등장.
    public bool Prepare(
        BoardManager boardManager,
        PieceManager pieceManager,
        bool separateTeams = false)
    {
        StopAndRestore();

        if (!playIntro)
        {
            Debug.LogWarning(
                "[FormationIntro] Play Intro가 OFF입니다.",
                this
            );
            return false;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogError(
                "[FormationIntro] Animator 컴포넌트가 비활성화되어 있습니다.",
                this
            );
            return false;
        }

        if (boardManager == null)
        {
            Debug.LogError(
                "[FormationIntro] BoardManager가 연결되지 않았습니다.",
                this
            );
            return false;
        }

        separatePieceTeams = separateTeams;

        // <변경부분>
        // BoardManager의 실제 크기에 따라 타일과 기물을 수집한다.
        // 기존 EventScene 5x2와 BattleScene 5x6 모두 지원한다.
        for (int y = 0; y < boardManager.Height; y++)
        {
            for (int x = 0; x < boardManager.Width; x++)
            {
                Tile tile = boardManager.GetTile(x, y);

                if (tile != null)
                {
                    tileVisuals.Add(
                        new FadeVisual(tile.transform)
                    );
                }

                if (pieceManager == null)
                {
                    continue;
                }

                Piece piece = pieceManager.GetPieceAt(x, y);

                if (piece == null)
                {
                    continue;
                }

                FadeVisual pieceVisual =
                    new FadeVisual(piece.transform);

                pieceVisuals.Add(pieceVisual);

                if (!separatePieceTeams)
                {
                    continue;
                }

                if (piece.Team == PieceTeam.Player)
                {
                    playerPieceVisuals.Add(pieceVisual);
                }
                else
                {
                    // Enemy와 Neutral은 두 번째 단계에서 등장한다.
                    opponentPieceVisuals.Add(pieceVisual);
                }
            }
        }

        if (tileVisuals.Count == 0)
        {
            Debug.LogError(
                "[FormationIntro] 타일을 하나도 찾지 못해 연출을 시작할 수 없습니다.",
                this
            );

            StopAndRestore();
            return false;
        }

        Debug.Log(
     $"[FormationIntro] 연출 대상 확인 | " +
     $"Tiles={tileVisuals.Count} | " +
     $"Pieces={pieceVisuals.Count} | " +
     $"SeparateTeams={separatePieceTeams}",
     this
 );

        // <변경부분>
        // BattleScene에서만 타일의 등장 순서를 무작위로 섞는다.
        // 실제 BoardManager 타일 배열이나 배치 좌표는 변경하지 않는다.
        //
        // Fisher-Yates Shuffle을 사용해 중복이나 누락 없이
        // 모든 타일이 정확히 한 번씩 등장하도록 한다.
        //
        // System.Random을 사용해 전투 로직의
        // UnityEngine.Random 전역 상태에 영향을 주지 않는다.
        if (separatePieceTeams && randomizeBattleTileOrder)
        {
            System.Random tileRandom = new System.Random();

            for (int i = tileVisuals.Count - 1; i > 0; i--)
            {
                int randomIndex = tileRandom.Next(i + 1);

                FadeVisual temp = tileVisuals[i];
                tileVisuals[i] = tileVisuals[randomIndex];
                tileVisuals[randomIndex] = temp;
            }
        }

        // <변경부분>
        // 카메라 확대가 시작되기 전 같은 프레임에서 즉시 숨긴다.
        // GameObject 활성 상태와 실제 기물 데이터는 변경하지 않는다.
        for (int i = 0; i < tileVisuals.Count; i++)
        {
            tileVisuals[i].SetAlpha(0f);
            tileVisuals[i].SetDrop(tileDropHeight);
        }

        for (int i = 0; i < pieceVisuals.Count; i++)
        {
            pieceVisuals[i].SetAlpha(0f);
        }

        IsPrepared = true;
        return true;
    }

    // <변경부분>
    // Prepare()로 숨겨 둔 타일과 기물을 실제로 등장시킨다.
    // 카메라 시작 확대가 끝난 후 BattleSetupManager가 호출한다.
    public void PlayPrepared()
    {
        if (!IsPrepared)
        {
            Debug.LogWarning(
                "[FormationIntro] Prepare가 완료되지 않아 재생할 수 없습니다.",
                this
            );
            return;
        }

        if (!playIntro || !isActiveAndEnabled)
        {
            StopAndRestore();
            return;
        }

        IsPrepared = false;
        IsPlaying = true;

        introCoroutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        float tileDuration = Mathf.Max(0.01f, tileFadeDuration);
        float stagger = Mathf.Max(0f, tileInterval);

        // <변경부분>
        // BattleScene은 설정한 개수만큼 타일을 동시에 등장시킨다.
        // EventScene은 기존처럼 한 번에 하나씩 등장한다.
        int tilesPerWave = separatePieceTeams
            ? Mathf.Max(1, battleTilesPerWave)
            : 1;

        int tileWaveCount = Mathf.CeilToInt(
            tileVisuals.Count / (float)tilesPerWave
        );

        float totalTileDuration =
            (tileWaveCount - 1) * stagger + tileDuration;

        float elapsed = 0f;

        // <변경부분>
        // 무작위로 섞인 타일을 설정한 개수씩 묶어 등장시킨다.
        //
        // BattleScene: 기본 2개씩 15회
        // EventScene: 기존 순서대로 1개씩 등장
        while (elapsed < totalTileDuration)
        {
            for (int i = 0; i < tileVisuals.Count; i++)
            {
                // <변경부분>
                // 같은 Wave에 속하는 타일은 등장 시작 시간이 같다.
                // 예: 0,1번 동시 → 2,3번 동시 → 4,5번 동시
                int waveIndex = i / tilesPerWave;

                float startTime = waveIndex * stagger;

                float t = Mathf.Clamp01(
                    (elapsed - startTime) / tileDuration
                );

                float eased = t * t * (3f - 2f * t);

                tileVisuals[i].SetAlpha(eased);

                tileVisuals[i].SetDrop(
                    tileDropHeight * (1f - eased)
                );
            }

            yield return null;

            // <변경부분>
            // 씬 진입 직후 한 프레임이 길어져도
            // 타일 여러 개의 등장 시간을 한꺼번에 건너뛰지 않는다.
            elapsed += Mathf.Min(
                Time.unscaledDeltaTime,
                1f / 30f
            );
        }

        for (int i = 0; i < tileVisuals.Count; i++)
        {
            tileVisuals[i].Restore();
        }

        if (pieceStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(pieceStartDelay);
        }

        // <변경부분>
        // BattleScene은 Player 기물의 Born을 하나씩 시작한 뒤
        // Enemy / Neutral 기물의 Born을 하나씩 시작한다.
        //
        // EventScene은 기존의 전체 기물 동시 페이드인을 유지한다.
        if (separatePieceTeams)
        {
            yield return PlayPieceBornGroupRoutine(
                playerPieceVisuals
            );

            // <변경부분>
            // 플레이어 진영의 Born이 모두 끝난 후
            // 상대 진영의 Born 시작 전 간격을 둔다.
            if (playerPieceVisuals.Count > 0 &&
                opponentPieceVisuals.Count > 0 &&
                teamFadeInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    teamFadeInterval
                );
            }

            yield return PlayPieceBornGroupRoutine(
                opponentPieceVisuals
            );
        }
        else
        {
            yield return FadePieceGroupRoutine(
                pieceVisuals
            );
        }

        // <변경부분>
        // 다음 진입에서 원래 색상과 위치를 사용할 수 있도록 정리한다.
        tileVisuals.Clear();
        pieceVisuals.Clear();
        playerPieceVisuals.Clear();
        opponentPieceVisuals.Clear();

        separatePieceTeams = false;
        IsPrepared = false;
        introCoroutine = null;
        IsPlaying = false;
    }

    // <변경부분>
    // 지정된 기물 그룹을 동시에 페이드인한다.
    // 원래 Sprite / Spine 알파값을 최종적으로 복원한다.
    private IEnumerator FadePieceGroupRoutine(
        List<FadeVisual> visuals)
    {
        if (visuals == null || visuals.Count == 0)
        {
            yield break;
        }

        float fadeDuration = Mathf.Max(
            0.01f,
            pieceFadeDuration
        );

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            float t = Mathf.Clamp01(
                elapsed / fadeDuration
            );

            float eased = t * t * (3f - 2f * t);

            for (int i = 0; i < visuals.Count; i++)
            {
                visuals[i].SetAlpha(eased);
            }

            yield return null;

            // <변경부분>
            // 프레임 지연이 발생해도 페이드 진행률이
            // 한 번에 크게 뛰지 않도록 제한한다.
            elapsed += Mathf.Min(
                Time.unscaledDeltaTime,
                1f / 30f
            );
        }

        for (int i = 0; i < visuals.Count; i++)
        {
            visuals[i].Restore();
        }
    }

    // <변경부분>
    // BattleScene 전용 기물 Born 등장 연출.
    //
    // 기물마다 일정한 간격으로 Born을 시작한다.
    // 각 기물의 Born은 기존 Spine Controller가 관리하며
    // Born 완료 후 Idle로 복귀한다.
    //
    // 모든 기물의 Born이 끝나야 다음 전투 단계로 진행한다.
    // EventScene에서는 이 함수를 사용하지 않는다.
    private IEnumerator PlayPieceBornGroupRoutine(
        List<FadeVisual> visuals)
    {
        if (visuals == null || visuals.Count == 0)
        {
            yield break;
        }

        float stagger = Mathf.Max(
            0f,
            battlePieceInterval
        );

        List<Coroutine> groupCoroutines =
            new List<Coroutine>(visuals.Count);

        for (int i = 0; i < visuals.Count; i++)
        {
            // <변경부분>
            // 기물별 Born을 독립적으로 시작하여
            // 앞 기물의 Born 도중 다음 기물도 등장할 수 있다.
            Coroutine bornCoroutine = StartCoroutine(
                PlaySinglePieceBornRoutine(visuals[i])
            );

            if (bornCoroutine != null)
            {
                groupCoroutines.Add(bornCoroutine);
                activeBornCoroutines.Add(bornCoroutine);
            }

            // <변경부분>
            // 마지막 기물 이후에는 불필요한 간격을 추가하지 않는다.
            if (i < visuals.Count - 1 && stagger > 0f)
            {
                yield return new WaitForSecondsRealtime(stagger);
            }
        }

        // <변경부분>
        // Born 시작 시간은 서로 달라도,
        // 실제 종료는 모든 Born이 완료된 뒤에 처리한다.
        // BattleSetupManager의 전투 입력 잠금이 유지된다.
        for (int i = 0; i < groupCoroutines.Count; i++)
        {
            yield return groupCoroutines[i];
        }

        activeBornCoroutines.Clear();
    }

    // <변경부분>
    // 기물 1개의 기존 Born 애니메이션을 재생한다.
    //
    // PieceVisualController에 연결된 현재 Spine Controller를
    // 우선 사용하므로 기물마다 다른 Spine 외형을 지원한다.
    private IEnumerator PlaySinglePieceBornRoutine(
        FadeVisual visual)
    {
        if (visual == null)
        {
            yield break;
        }

        Piece piece = visual.OwnerPiece;

        PieceSpineAnimationController spineAnimator = null;

        if (piece != null)
        {
            PieceVisualController visualController =
                piece.GetComponent<PieceVisualController>();

            if (visualController != null)
            {
                spineAnimator =
                    visualController.CurrentSpineAnimationController;
            }

            if (spineAnimator == null)
            {
                spineAnimator =
                    piece.GetComponentInChildren<
                        PieceSpineAnimationController>(true);
            }
        }

        // <변경부분>
        // Spine Controller가 없는 기물은
        // 기존 단일 페이드인으로 안전하게 표시한다.
        if (spineAnimator == null ||
            !spineAnimator.isActiveAndEnabled)
        {
            yield return FadePieceGroupRoutine(
                new List<FadeVisual> { visual }
            );

            yield break;
        }

        // <변경부분>
        // 카메라 확대 대기 동안 숨겨 둔 알파를 복원한다.
        // 0 알파인 상태에서 Born을 재생하면 보이지 않으므로
        // 기물이 등장할 차례에만 시각 상태를 복구한다.
        visual.Restore();

        // <변경부분>
        // 기존 PieceSpineAnimationController의
        // Born → Idle 재생 경로를 그대로 사용한다.
        // 흡수/변형용 Color Adjustment는 새로 실행하지 않는다.
        yield return spineAnimator.PlayBornRoutine();
    }

    public void StopAndRestore()
    {
        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        // <변경부분>
        // 중간에 종료된 Born 애니메이션이 남지 않도록
        // 실행 중인 개별 기물 코루틴도 정지한다.
        bool interruptedBorn =
            activeBornCoroutines.Count > 0;

        for (int i = 0; i < activeBornCoroutines.Count; i++)
        {
            if (activeBornCoroutines[i] != null)
            {
                StopCoroutine(activeBornCoroutines[i]);
            }
        }

        activeBornCoroutines.Clear();

        // <변경부분>
        // 애니메이션 도중 Scene 전환이나 컴포넌트 비활성화가
        // 발생해도 타일/기물의 원래 상태를 복원한다.
        for (int i = 0; i < tileVisuals.Count; i++)
        {
            tileVisuals[i].Restore();
        }

        for (int i = 0; i < pieceVisuals.Count; i++)
        {
            pieceVisuals[i].Restore();

            // <변경부분>
            // Born이 중단되었다면 Spine이 중간 프레임에
            // 멈춰 있지 않도록 기존 Idle 상태로 되돌린다.
            if (!interruptedBorn)
            {
                continue;
            }

            Piece piece = pieceVisuals[i].OwnerPiece;

            if (piece == null)
            {
                continue;
            }

            PieceVisualController visualController =
                piece.GetComponent<PieceVisualController>();

            PieceSpineAnimationController spineAnimator =
                visualController != null
                    ? visualController.CurrentSpineAnimationController
                    : null;

            if (spineAnimator == null)
            {
                spineAnimator =
                    piece.GetComponentInChildren<
                        PieceSpineAnimationController>(true);
            }

            if (spineAnimator != null &&
                spineAnimator.isActiveAndEnabled)
            {
                spineAnimator.PlayIdle();
            }
        }

        tileVisuals.Clear();
        pieceVisuals.Clear();
        playerPieceVisuals.Clear();
        opponentPieceVisuals.Clear();

        separatePieceTeams = false;
        IsPrepared = false;
        IsPlaying = false;
    }

    private void OnDisable()
    {
        StopAndRestore();
    }

    // <변경부분>
    // 원래 Sprite / Spine 알파와 Transform 로컬 좌표를 기록하고,
    // 애니메이션 중에만 임시로 변경한다.
    private sealed class FadeVisual
    {
        private readonly Transform root;
        private readonly Vector3 originalLocalPosition;
        private readonly SpriteRenderer[] spriteRenderers;
        private readonly Color[] originalSpriteColors;
        private readonly SkeletonRenderer[] spineRenderers;
        private readonly float[] originalSpineAlphas;

        // <변경부분>
        // 기존에 수집한 시각 연출 대상에서 Piece를 가져온다.
        // 타일 대상에서는 null이 반환된다.
        public Piece OwnerPiece
        {
            get
            {
                return root != null
                    ? root.GetComponent<Piece>()
                    : null;
            }
        }

        public FadeVisual(Transform target)
        {
            root = target;
            originalLocalPosition = target.localPosition;

            spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            originalSpriteColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                originalSpriteColors[i] = spriteRenderers[i].color;
            }

            spineRenderers = target.GetComponentsInChildren<SkeletonRenderer>(true);
            originalSpineAlphas = new float[spineRenderers.Length];
            for (int i = 0; i < spineRenderers.Length; i++)
            {
                // <변경부분>
                // Spine 4.3부터 Skeleton.A 대신 GetColor()를 사용한다.
                var skeleton = spineRenderers[i] != null
                    ? spineRenderers[i].Skeleton
                    : null;

                originalSpineAlphas[i] = skeleton != null
                    ? skeleton.GetColor().a
                    : 1f;
            }
        }

        public void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] == null) continue;
                Color color = originalSpriteColors[i];
                color.a *= alpha;
                spriteRenderers[i].color = color;
            }

            for (int i = 0; i < spineRenderers.Length; i++)
            {
                if (spineRenderers[i] == null ||
                    spineRenderers[i].Skeleton == null)
                {
                    continue;
                }

                // <변경부분>
                // Spine 4.3은 색상을 가져온 뒤 수정하여 다시 적용한다.
                // RGB 색상은 유지하고 Alpha만 변경한다.
                var skeleton = spineRenderers[i].Skeleton;

                Color color = skeleton.GetColor();
                color.a = originalSpineAlphas[i] * alpha;

                skeleton.SetColor(color);
            }
        }

        public void SetDrop(float localY)
        {
            if (root != null)
            {
                root.localPosition = originalLocalPosition + Vector3.up * localY;
            }
        }

        public void Restore()
        {
            SetAlpha(1f);
            if (root != null)
            {
                root.localPosition = originalLocalPosition;
            }
        }
    }
}

