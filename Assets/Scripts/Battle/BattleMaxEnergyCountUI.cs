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

    private int lastMaxEnergy = int.MinValue;

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
        lastMaxEnergy = int.MinValue;
        RefreshMaxEnergy();
    }


    private void Update()
    {
        // <변경부분>
        // 실제 최대 에너지 값이 변경되었을 때만 UI 갱신.
        RefreshMaxEnergy();
    }


    private void OnDisable()
    {
        StopPulse();
        lastMaxEnergy = int.MinValue;
    }


    private void RefreshMaxEnergy()
    {
        if (battleManager == null)
        {
            return;
        }

        int maxEnergy =
            battleManager.CurrentPlayerAbilityEnergy;

        if (lastMaxEnergy == maxEnergy)
        {
            return;
        }

        bool playAnimation =
            lastMaxEnergy != int.MinValue;

        lastMaxEnergy = maxEnergy;

        if (countNumText != null)
        {
            countNumText.text =
                maxEnergy.ToString();
        }

        // 최초 표시에서는 애니메이션을 재생하지 않는다.
        if (!playAnimation ||
            iconImage == null ||
            !iconImage.gameObject.activeInHierarchy)
        {
            StopPulse();
            return;
        }

        StopPulse();

        pulseCoroutine =
            StartCoroutine(PlayPulseRoutine());
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
