
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
    // BattleScene에서 Player 등장 후 Enemy 등장까지의 대기 시간.
    // 기존 EventScene의 전체 기물 동시 등장에는 영향을 주지 않는다.
    [Header("Battle Team Intro")]
    [SerializeField, Min(0f)]
    private float teamFadeInterval = 0.06f;

    private Coroutine introCoroutine;
    private readonly List<FadeVisual> tileVisuals = new List<FadeVisual>();
    private readonly List<FadeVisual> pieceVisuals = new List<FadeVisual>();

    // <변경부분>
    // BattleScene에서 팀별 순차 페이드를 실행하기 위한 목록.
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

        float totalTileDuration =
            (tileVisuals.Count - 1) * stagger + tileDuration;

        float elapsed = 0f;

        // <변경부분>
        // 타일 5x2 또는 5x6을 기존 순서대로 등장시킨다.
        while (elapsed < totalTileDuration)
        {
            for (int i = 0; i < tileVisuals.Count; i++)
            {
                float t = Mathf.Clamp01(
                    (elapsed - i * stagger) / tileDuration
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
        // BattleScene은 Player → Enemy 순서로 등장한다.
        // EventScene은 기존처럼 모든 기물이 동시에 등장한다.
        if (separatePieceTeams)
        {
            yield return FadePieceGroupRoutine(
                playerPieceVisuals
            );

            // 플레이어와 상대 기물이 모두 있을 때만 간격을 준다.
            if (playerPieceVisuals.Count > 0 &&
                opponentPieceVisuals.Count > 0 &&
                teamFadeInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    teamFadeInterval
                );
            }

            yield return FadePieceGroupRoutine(
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

    public void StopAndRestore()
    {
        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

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

