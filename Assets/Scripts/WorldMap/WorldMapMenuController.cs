
using System;
using System.Collections;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// SpineMenu 오브젝트에 부착한다.
// 투명 ClickArea 버튼은 SpineMenu의 자식으로 배치한다.
[DisallowMultipleComponent]
public sealed class WorldMapMenuController : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Spine 4.3")]
    [SerializeField] private SkeletonAnimation spineAnimation;
    [SerializeField] private SkeletonGraphic spineGraphic;
    [SerializeField] private Button toggleButton;

    [Header("Menu Buttons (A, B, C, D)")]
    [SerializeField]
    private WorldMapMenuButton[] menuButtons =
        new WorldMapMenuButton[4];

    [SerializeField] private RectTransform line;
    [SerializeField] private RectTransform menuText;

    [Header("Spine Animation Names")]
    [SerializeField] private string off = "Off";
    [SerializeField] private string on = "ON";
    [SerializeField] private string onActive = "On_Active";
    [SerializeField] private string offActive = "Off_Active";
    [SerializeField] private string onHovering = "On_Hovering";
    [SerializeField] private string offHovering = "Off_Hovering";

    [Header("Glitch Sequence")]
    [SerializeField, Min(0f)]
    private float firstItemDelay = 0.08f;

    [SerializeField, Min(0f)]
    private float itemGlitchDuration = 0.13f;

    [SerializeField, Min(0f)]
    private float betweenItemsDelay = 0.02f;

    [SerializeField, Min(0f)]
    private float horizontalGlitchPixels = 2f;

    [SerializeField, Min(0.1f)]
    private float maxSpineWait = 10f;

    // A, B, C, D, Line, MenuText 순서.
    private readonly RectTransform[] parts =
        new RectTransform[6];

    private readonly CanvasGroup[] groups =
        new CanvasGroup[6];

    private readonly Vector2[] originalPositions =
        new Vector2[6];

    private static readonly float[] GlitchAlphas =
    {
        0.12f, 0.92f, 0.23f, 1f,
        0.38f, 0.86f, 0.58f, 1f
    };

    private bool ready;
    private bool isOpen;
    private bool isTransitioning;
    private Coroutine transitionRoutine;

    private void Awake()
    {
        if (spineAnimation == null)
            spineAnimation = GetComponent<SkeletonAnimation>();

        if (spineGraphic == null)
            spineGraphic = GetComponent<SkeletonGraphic>();

        if (toggleButton == null)
            toggleButton = GetComponentInChildren<Button>(true);

        // Spine 그래픽 자체는 버튼 입력을 가로채지 않는다.
        if (spineGraphic != null)
            spineGraphic.raycastTarget = false;

        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleMenu);
    }

    private void Start()
    {
        if (spineAnimation == null ||
            toggleButton == null ||
            line == null ||
            menuText == null ||
            menuButtons == null ||
            menuButtons.Length != 4)
        {
            Debug.LogError(
                "[WorldMapMenu] Spine, ClickArea, 버튼 4개, " +
                "Line, MenuText 연결을 확인하세요.",
                this);

            enabled = false;
            return;
        }

        spineAnimation.Initialize(false);

        if (spineAnimation.AnimationState == null ||
            spineAnimation.Skeleton == null)
        {
            Debug.LogError(
                "[WorldMapMenu] Spine 애니메이션 데이터를 " +
                "초기화할 수 없습니다.",
                this);

            enabled = false;
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (menuButtons[i] == null ||
                !menuButtons[i].Prepare())
            {
                Debug.LogError(
                    "[WorldMapMenu] MenuButton_A~D 연결 및 " +
                    "Normal/Hover를 확인하세요.",
                    this);

                enabled = false;
                return;
            }

            parts[i] = menuButtons[i].MenuRect;
            groups[i] = menuButtons[i].RootGroup;
        }

        parts[4] = line;
        parts[5] = menuText;

        groups[4] = GetOrAddGroup(line.gameObject);
        groups[5] = GetOrAddGroup(menuText.gameObject);

        for (int i = 0; i < parts.Length; i++)
        {
            originalPositions[i] = parts[i].anchoredPosition;

            groups[i].alpha = 0f;
            groups[i].interactable = false;
            groups[i].blocksRaycasts = false;
        }

        ready = true;
        ResetClosed();
    }

    private void OnEnable()
    {
        if (ready)
            ResetClosed();
    }

    private void OnDisable()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(ToggleMenu);
    }

    public void ToggleMenu()
    {
        if (!ready || isTransitioning)
            return;

        transitionRoutine =
            StartCoroutine(Transition(!isOpen));
    }

    private IEnumerator Transition(bool opening)
    {
        isTransitioning = true;
        toggleButton.interactable = false;

        // 전환 도중 큰 버튼 클릭을 차단한다.
        for (int i = 0; i < menuButtons.Length; i++)
            menuButtons[i].SetAvailable(false);

        bool spineComplete = false;

        // <변경부분>
        // 열기:
        // Spine On_Active를 먼저 실행하고
        // 기존처럼 UI 글리치 등장과 병행한다.
        //
        // 닫기:
        // Spine Off_Active는 아직 실행하지 않는다.
        // 모든 UI가 역순으로 퇴장한 후 실행한다.
        if (opening)
        {
            PlayTransitionAnimation(
                true,
                () => spineComplete = true);

            if (firstItemDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    firstItemDelay);
            }
        }

        for (int i = 0; i < parts.Length; i++)
        {
            // 열기: A B C D Line MenuText
            // 닫기: MenuText Line D C B A
            int index = opening
                ? i
                : parts.Length - 1 - i;

            yield return GlitchPart(index, opening);

            if (i < parts.Length - 1 &&
                betweenItemsDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    betweenItemsDelay);
            }
        }

        // <변경부분>
        // 닫기에서는 모든 UI 글리치 퇴장이 끝난 다음
        // Spine Off_Active 애니메이션을 시작한다.
        //
        // 마지막 MenuButton_A가 완전히 사라진 뒤
        // 접기 애니메이션이 실행되는 순서를 보장한다.
        if (!opening)
        {
            PlayTransitionAnimation(
                false,
                () => spineComplete = true);
        }

        // <변경부분>
        // 열기: UI 전개와 Spine On_Active가 모두 끝날 때까지 대기.
        // 닫기: UI 퇴장 후 시작한 Spine Off_Active 완료까지 대기.
        float deadline =
            Time.unscaledTime + maxSpineWait;

        while (!spineComplete &&
               Time.unscaledTime < deadline)
        {
            yield return null;
        }

        if (!spineComplete)
        {
            Debug.LogWarning(
                "[WorldMapMenu] Spine 완료 신호를 받지 못해 " +
                "기본 상태로 복귀합니다.",
                this);

            PlayIdle(opening);
        }

        isOpen = opening;
        isTransitioning = false;

        if (isOpen)
        {
            for (int i = 0; i < menuButtons.Length; i++)
                menuButtons[i].SetAvailable(true);
        }

        toggleButton.interactable = true;
        transitionRoutine = null;
    }

    private IEnumerator GlitchPart(int index, bool showing)
    {
        CanvasGroup group = groups[index];
        RectTransform rect = parts[index];
        Vector2 origin = originalPositions[index];

        if (itemGlitchDuration <= 0f)
        {
            group.alpha = showing ? 1f : 0f;
            rect.anchoredPosition = origin;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < itemGlitchDuration)
        {
            float progress = Mathf.Clamp01(
                elapsed / itemGlitchDuration);

            int phase = Mathf.Min(
                GlitchAlphas.Length - 1,
                Mathf.FloorToInt(
                    progress * GlitchAlphas.Length));

            float flicker = GlitchAlphas[phase];

            group.alpha = showing
                ? flicker
                : 1f - flicker;

            // 짧은 수평 픽셀 흔들림.
            rect.anchoredPosition = origin +
                new Vector2(
                    ((phase % 3) - 1) * horizontalGlitchPixels,
                    0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 원래 위치와 최종 표시 상태를 정확히 복원.
        rect.anchoredPosition = origin;
        group.alpha = showing ? 1f : 0f;
    }

    private void PlayTransitionAnimation(
        bool opening,
        Action completed)
    {
        string activeName = opening ? onActive : offActive;
        string idleName = opening ? on : off;

        if (!HasAnimation(activeName))
        {
            Debug.LogWarning(
                "[WorldMapMenu] Spine 애니메이션 없음: " +
                activeName,
                this);

            PlayIdle(opening);
            completed?.Invoke();
            return;
        }

        Spine.TrackEntry entry =
            spineAnimation.AnimationState.SetAnimation(
                0, activeName, false);

        entry.Complete += _ => completed?.Invoke();

        if (HasAnimation(idleName))
        {
            spineAnimation.AnimationState.AddAnimation(
                0, idleName, true, 0f);
        }
    }

    private void PlayIdle(bool opened)
    {
        string idleName = opened ? on : off;

        if (HasAnimation(idleName))
        {
            spineAnimation.AnimationState.SetAnimation(
                0, idleName, true);
        }
    }

    // 작은 마름모 버튼 마우스 진입.
    // <변경부분>
    // 메뉴가 닫힌 Off 상태에서만 호버 애니메이션을 실행한다.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ready &&
            !isTransitioning &&
            !isOpen &&
            HasAnimation(onHovering))
        {
            // 1회 재생한 뒤 마지막 포즈를 유지.
            spineAnimation.AnimationState.SetAnimation(
                0, onHovering, false);
        }
    }

    // 작은 마름모 버튼 마우스 이탈.
    // <변경부분>
    // ON 상태에서는 호버 종료 애니메이션도 실행하지 않는다.
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!ready || isTransitioning || isOpen)
            return;

        if (HasAnimation(offHovering))
        {
            spineAnimation.AnimationState.SetAnimation(
                0, offHovering, false);

            string idleName = isOpen ? on : off;

            if (HasAnimation(idleName))
            {
                spineAnimation.AnimationState.AddAnimation(
                    0, idleName, true, 0f);
            }
        }
        else
        {
            // Off_Hovering이 아직 없는 경우.
            PlayIdle(isOpen);
        }
    }

    private bool HasAnimation(string name)
    {
        return !string.IsNullOrEmpty(name) &&
               spineAnimation != null &&
               spineAnimation.Skeleton != null &&
               spineAnimation.Skeleton.Data.FindAnimation(name) != null;
    }

    private void ResetClosed()
    {
        isOpen = false;
        isTransitioning = false;

        for (int i = 0; i < parts.Length; i++)
        {
            parts[i].anchoredPosition = originalPositions[i];
            groups[i].alpha = 0f;
            groups[i].interactable = false;
            groups[i].blocksRaycasts = false;
        }

        for (int i = 0; i < menuButtons.Length; i++)
            menuButtons[i].SetAvailable(false);

        toggleButton.interactable = true;
        PlayIdle(false);
    }

    private static CanvasGroup GetOrAddGroup(GameObject target)
    {
        CanvasGroup group =
            target.GetComponent<CanvasGroup>();

        return group != null
            ? group
            : target.AddComponent<CanvasGroup>();
    }
}

