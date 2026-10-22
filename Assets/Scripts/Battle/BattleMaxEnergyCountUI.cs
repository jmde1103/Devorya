using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// <변경부분>
// 기존 Absorb Count UI를 복제하여 사용하는 임시 최대 에너지 UI.
// BattleManager의 실제 최대 에너지 값을 표시한다.
//
// 최대치가 변경되면 기존 흡수 카운터와 동일한
// 아이콘 확대 / 축소 / 복귀 및 반투명 잔상 연출을 재생한다.
public class BattleMaxEnergyCountUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private TMP_Text countNumText;
    [SerializeField] private Image iconImage;

    [Header("Count Animation")]
    [SerializeField, Min(1f)]
    private float pulsePeakScale = 1.23f;

    [SerializeField, Range(0.5f, 1f)]
    private float pulseDipScale = 0.94f;

    [SerializeField, Min(0.05f)]
    private float pulseDuration = 0.32f;

    [SerializeField, Min(1f)]
    private float ghostMaxScale = 1.45f;

    [SerializeField, Range(0f, 1f)]
    private float ghostStartAlpha = 0.3f;

    // <변경부분>
    // 숫자가 1씩 증가하거나 감소하는 간격(초).
    // 값이 작을수록 숫자가 빠르게 변경된다.
    [Header("Number Step Animation")]
    [SerializeField, Min(0.01f)]
    private float countUpStepInterval = 0.06f;

    [SerializeField, Min(0.01f)]
    private float countDownStepInterval = 0.06f;

    // <변경부분>
    // 마지막으로 확인한 실제 에너지 목표값.
    private int lastMaxEnergy = int.MinValue;

    // <변경부분>
    // 화면에 현재 표시 중인 숫자.
    // 실제 에너지 데이터와 분리하여 애니메이션한다.
    private int displayedEnergy = 0;

    // <변경부분>
    // 전투 시작 시 최초 카운트업은 한 번만 실행한다.
    private bool hasInitializedEnergyDisplay = false;

    private Coroutine countCoroutine;

    private Vector3 defaultIconScale = Vector3.one;
    private Image ghostImage;
    private Coroutine pulseCoroutine;


    private void Awake()
    {
        if (iconImage != null)
        {
            defaultIconScale =
                iconImage.rectTransform.localScale;
        }
    }


    private void OnEnable()
    {
        // <변경부분>
        // 최초 활성화에서는 반드시 0부터 표시한다.
        // 이미 초기화된 UI를 다시 활성화할 때는
        // 처음부터 카운트업하지 않고 실제 잔량으로 동기화한다.
        if (hasInitializedEnergyDisplay &&
            battleManager != null)
        {
            displayedEnergy =
                Mathf.Max(
                    0,
                    battleManager.CurrentPlayerAbilityEnergy
                );

            lastMaxEnergy = displayedEnergy;
        }
        else
        {
            displayedEnergy = 0;
            lastMaxEnergy = int.MinValue;
        }

        if (countNumText != null)
        {
            countNumText.text =
                displayedEnergy.ToString();
        }
    }


    private void Update()
    {
        // <변경부분>
        // 실제 에너지 목표값이 달라졌을 때만
        // 숫자 증가/감소 애니메이션을 갱신한다.
        RefreshMaxEnergy();
    }


    private void OnDisable()
    {
        // <변경부분>
        // 비활성화 시 진행 중인 숫자 애니메이션을 정리한다.
        if (countCoroutine != null)
        {
            StopCoroutine(countCoroutine);
            countCoroutine = null;
        }

        StopPulse();

        lastMaxEnergy = int.MinValue;
    }


    private void RefreshMaxEnergy()
    {
        if (battleManager == null)
        {
            return;
        }

        int currentEnergy =
            Mathf.Max(
                0,
                battleManager.CurrentPlayerAbilityEnergy
            );

        // <변경부분>
        // BattleManager의 현재 에너지는 코드상 초기값 5를 가진다.
        // Inspector 최대치가 5가 아니라면 최초 턴 충전 전에
        // 임시로 5가 읽힐 수 있다.
        //
        // 따라서 최초에는 현재 에너지가 최대치까지
        // 초기화된 상태를 확인한 후 카운트업을 시작한다.
        if (!hasInitializedEnergyDisplay)
        {
            if (currentEnergy !=
                battleManager.MaxPlayerAbilityEnergy)
            {
                return;
            }

            hasInitializedEnergyDisplay = true;
        }

        if (lastMaxEnergy == currentEnergy)
        {
            return;
        }

        lastMaxEnergy = currentEnergy;

        // <변경부분>
        // 숫자 애니메이션 도중 에너지가 다시 변경되면
        // 현재 표시 숫자에서 새로운 목표값으로 진행한다.
        if (countCoroutine != null)
        {
            StopCoroutine(countCoroutine);
            countCoroutine = null;
        }

        if (displayedEnergy == currentEnergy)
        {
            StopPulse();
            return;
        }

        // <변경부분>
        // 실제 에너지 값은 변경하지 않고
        // TMP 숫자만 순차적으로 변경한다.
        countCoroutine =
            StartCoroutine(
                AnimateEnergyCountRoutine(currentEnergy)
            );

        // 기존 아이콘 확대/축소 및 잔상 연출은 유지한다.
        if (iconImage == null ||
            !iconImage.gameObject.activeInHierarchy)
        {
            StopPulse();
            return;
        }

        StopPulse();

        pulseCoroutine =
            StartCoroutine(PlayPulseRoutine());
    }


    // <변경부분>
    // 표시 숫자를 목표 에너지까지 1씩 증가하거나 감소시킨다.
    // TimeScale이 0이어도 재생되도록 실시간 대기를 사용한다.
    private IEnumerator AnimateEnergyCountRoutine(
        int targetEnergy)
    {
        // 최초 0 또는 변경 직전 숫자가
        // 최소 한 프레임 표시되도록 한다.
        yield return null;

        while (displayedEnergy != targetEnergy)
        {
            int stepDirection =
                targetEnergy > displayedEnergy
                    ? 1
                    : -1;

            displayedEnergy += stepDirection;

            if (countNumText != null)
            {
                countNumText.text =
                    displayedEnergy.ToString();
            }

            if (displayedEnergy == targetEnergy)
            {
                break;
            }

            float stepInterval =
                stepDirection > 0
                    ? countUpStepInterval
                    : countDownStepInterval;

            yield return new WaitForSecondsRealtime(
                Mathf.Max(0.01f, stepInterval)
            );
        }

        countCoroutine = null;
    }


    private void EnsureGhost()
    {
        if (ghostImage != null ||
            iconImage == null ||
            iconImage.sprite == null)
        {
            return;
        }

        RectTransform iconRect =
            iconImage.rectTransform;

        GameObject ghostObject =
            new GameObject(
                "EnergyCountGhost",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement)
            );

        RectTransform ghostRect =
            ghostObject.GetComponent<RectTransform>();

        ghostRect.SetParent(
            iconRect.parent,
            false
        );

        ghostObject.GetComponent<LayoutElement>()
            .ignoreLayout = true;

        ghostRect.anchorMin = iconRect.anchorMin;
        ghostRect.anchorMax = iconRect.anchorMax;
        ghostRect.pivot = iconRect.pivot;
        ghostRect.sizeDelta = iconRect.sizeDelta;
        ghostRect.anchoredPosition =
            iconRect.anchoredPosition;

        ghostRect.localRotation =
            iconRect.localRotation;

        ghostRect.localScale =
            defaultIconScale;

        // 기존 아이콘 뒤에 표시한다.
        ghostRect.SetSiblingIndex(
            iconRect.GetSiblingIndex()
        );

        ghostImage =
            ghostObject.GetComponent<Image>();

        ghostImage.sprite = iconImage.sprite;
        ghostImage.type = iconImage.type;
        ghostImage.preserveAspect =
            iconImage.preserveAspect;

        ghostImage.raycastTarget = false;

        ghostObject.SetActive(false);
    }


    private IEnumerator PlayPulseRoutine()
    {
        EnsureGhost();

        RectTransform iconRect =
            iconImage.rectTransform;

        RectTransform ghostRect =
            ghostImage != null
                ? ghostImage.rectTransform
                : null;

        if (ghostImage != null)
        {
            ghostImage.sprite =
                iconImage.sprite;

            ghostImage.gameObject.SetActive(true);
        }

        float duration =
            Mathf.Max(0.05f, pulseDuration);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(elapsed / duration);

            float scaleMultiplier;

            // 기존 흡수 카운터와 동일한 타이밍.
            // 0~25%: 확대
            // 25~60%: 축소
            // 60~100%: 원래 크기로 복귀
            if (progress < 0.25f)
            {
                float t = Mathf.SmoothStep(
                    0f, 1f, progress / 0.25f
                );

                scaleMultiplier = Mathf.Lerp(
                    1f, pulsePeakScale, t
                );
            }
            else if (progress < 0.6f)
            {
                float t = Mathf.SmoothStep(
                    0f,
                    1f,
                    (progress - 0.25f) / 0.35f
                );

                scaleMultiplier = Mathf.Lerp(
                    pulsePeakScale,
                    pulseDipScale,
                    t
                );
            }
            else
            {
                float t = Mathf.SmoothStep(
                    0f,
                    1f,
                    (progress - 0.6f) / 0.4f
                );

                scaleMultiplier = Mathf.Lerp(
                    pulseDipScale,
                    1f,
                    t
                );
            }

            iconRect.localScale =
                defaultIconScale * scaleMultiplier;

            if (ghostImage != null &&
                ghostRect != null)
            {
                float ghostProgress =
                    Mathf.SmoothStep(
                        0f, 1f, progress
                    );

                float ghostScale =
                    Mathf.Lerp(
                        1f,
                        ghostMaxScale,
                        ghostProgress
                    );

                ghostRect.localScale =
                    defaultIconScale * ghostScale;

                Color ghostColor =
                    iconImage.color;

                ghostColor.a *=
                    ghostStartAlpha *
                    (1f - ghostProgress);

                ghostImage.color =
                    ghostColor;
            }

            yield return null;
        }

        iconRect.localScale =
            defaultIconScale;

        if (ghostImage != null)
        {
            ghostImage.gameObject.SetActive(false);
        }

        pulseCoroutine = null;
    }


    private void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (iconImage != null)
        {
            iconImage.rectTransform.localScale =
                defaultIconScale;
        }

        if (ghostImage != null)
        {
            ghostImage.gameObject.SetActive(false);
        }
    }
}
