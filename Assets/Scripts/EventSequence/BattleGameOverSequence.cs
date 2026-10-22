using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleGameOverSequence : MonoBehaviour
{
    [Header("Screen Glitch")]
    [SerializeField]
    private EventScreenGlitchController screenGlitchController;

    // <변경부분>
    // 게임오버 전용 화면 전체 노이즈.
    // 배경 및 TMP 글자 위에도 신호 잡음을 표시한다.
    [SerializeField]
    private GameOverSignalNoiseUI signalNoiseUI;

    // <변경부분>
    // 검은 화면 수평 전환과 TMP 글자 찢김.
    [SerializeField]
    private GameOverCorruptionFX corruptionFX;

    [SerializeField, Range(0f, 1f)]
    private float openingGlitchStrength = 1f;

    [SerializeField, Min(0.05f)]
    private float openingGlitchDuration = 1.55f;

    [Header("Game Over UI")]
    [SerializeField]
    private CanvasGroup inputBlockerGroup;

    [SerializeField]
    private CanvasGroup blackoutGroup;

    [SerializeField]
    private CanvasGroup messageGroup;

    [SerializeField]
    private TMP_Text mainMessageText;

    [SerializeField]
    private TMP_Text redGhostText;

    [SerializeField]
    private TMP_Text cyanGhostText;

    [Header("Blackout Timing")]

    // <변경부분>
    // 화면 글리치만 먼저 보여주는 시간.
    [SerializeField, Min(0f)]
    private float blackoutStartDelay = 0.7f;

    // <변경부분>
    // 검은 배경이 서서히 나타나는 시간.
    [SerializeField, Min(0.05f)]
    private float blackoutFadeDuration = 0.85f;

    [Header("Message Timing")]
    [SerializeField, Min(0.1f)]
    private float messageHoldDuration = 2.8f;

    // <변경부분>
    // 검은 배경 등장 후 문구의 페이드 인.
    [SerializeField, Min(0.05f)]
    private float messageFadeInDuration = 0.5f;

    [SerializeField, Min(0.05f)]
    private float messageFadeOutDuration = 0.4f;

    [Header("Text Glitch")]
    [SerializeField, Min(0.05f)]
    private float textGlitchInterval = 0.45f;

    [SerializeField, Min(0.01f)]
    private float textGlitchBurstDuration = 0.1f;

    [SerializeField, Min(0f)]
    private float textGlitchOffset = 12f;

    [Header("Title Transition")]
    [SerializeField]
    private string titleSceneName = "Title";

    [SerializeField, Min(0.05f)]
    private float titleRevealDuration = 0.5f;

    // <변경부분>
    // NO SIGNAL 이후 마지막에 잠깐 보여줄 문구.
    // 일단 영어 UI 느낌으로 고정 문자열을 사용한다.
    [Header("Final Reboot Message")]
    [SerializeField]
    private string rebootMessageText = "REBOOTING...";

    [SerializeField, Min(0.05f)]
    private float rebootMessageDuration = 0.75f;

    [SerializeField, Min(0.05f)]
    private float rebootMessageGlitchDuration = 0.28f;

    // <변경부분>
    // 킹이 잡힌 뒤 실제 오류가 발생하는 것처럼
    // 정상 화면과 글리치를 단계별로 교차한다.
    [Header("Game Over Glitch Sequence")]

    [SerializeField, Min(0f)]
    private float kingDeathWaitDuration = 0.5f;

    [SerializeField, Min(0.05f)]
    private float firstGlitchDuration = 0.3f;

    [SerializeField, Range(0f, 1f)]
    private float firstGlitchStrength = 0.55f;

    [SerializeField, Min(0f)]
    private float firstRecoveryDuration = 0.5f;

    [SerializeField, Min(0.05f)]
    private float secondGlitchDuration = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float secondGlitchStrength = 0.75f;

    [SerializeField, Min(0f)]
    private float secondRecoveryDuration = 0.25f;

    // 마지막 화면 붕괴와 검은 화면 교차.
    [SerializeField, Min(0.05f)]
    private float finalCrashDuration = 0.5f;

    [Header("NO SIGNAL Interference")]

    // <변경부분>
    // 문구가 노이즈 속에서 드러나는 시간.
    [SerializeField, Min(0.05f)]
    private float signalRevealDuration = 1f;

    // 노이즈가 서서히 약해지는 데 걸리는 시간.
    [SerializeField, Min(0.1f)]
    private float signalNoiseDecayDuration = 2.5f;

    // 지속 노이즈의 최종 강도.
    [SerializeField, Range(0f, 1f)]
    private float signalNoiseEndStrength = 0.12f;

    // 검은 화면과 깨진 전투 화면의 반복 간격.
    [SerializeField, Min(0.1f)]
    private float signalBurstInterval = 0.48f;

    [SerializeField, Min(0.02f)]
    private float signalBurstDuration = 0.13f;

    private bool isSequenceStarted;

    private RectTransform redGhostRect;
    private RectTransform cyanGhostRect;

    private Vector2 redGhostOriginalPosition;
    private Vector2 cyanGhostOriginalPosition;

    private Color mainMessageOriginalColor;

    // <변경부분>
    // Localize String Event가 넣어준 기본 문구(NO SIGNAL)를 보존한다.
    private string defaultMessageText;


    private void Awake()
    {
        if (inputBlockerGroup != null)
        {
            inputBlockerGroup.alpha = 1f;
            inputBlockerGroup.interactable = false;
            inputBlockerGroup.blocksRaycasts = false;
        }

        if (blackoutGroup != null)
        {
            blackoutGroup.alpha = 0f;
        }

        if (messageGroup != null)
        {
            messageGroup.alpha = 0f;
        }

        if (mainMessageText != null)
        {
            mainMessageOriginalColor = mainMessageText.color;
            defaultMessageText =
                mainMessageText.text;
        }

        if (redGhostText != null)
        {
            redGhostRect = redGhostText.rectTransform;
            redGhostOriginalPosition =
                redGhostRect.anchoredPosition;
        }

        if (cyanGhostText != null)
        {
            cyanGhostRect = cyanGhostText.rectTransform;
            cyanGhostOriginalPosition =
                cyanGhostRect.anchoredPosition;
        }

        HideTextGlitch();
    }


    // <변경부분>
    // BattleEndFlowController에서 패배 확정 후 호출한다.
    // 중복 실행을 방지한다.
    public void Play()
    {
        if (isSequenceStarted)
        {
            return;
        }

        if (blackoutGroup == null ||
            messageGroup == null ||
            inputBlockerGroup == null)
        {
            Debug.LogError(
                "게임오버 UI 연결이 누락되었습니다.",
                this
            );

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                titleSceneName))
        {
            Debug.LogError(
                $"타이틀 씬을 찾을 수 없습니다: {titleSceneName}",
                this
            );

            return;
        }

        isSequenceStarted = true;

        // <변경부분>
        // BattleScene이 파괴되어도 검은 전환 화면은
        // Title 로딩이 완료될 때까지 유지한다.
        // 이 컴포넌트는 루트 GameObject에 있어야 한다.
        DontDestroyOnLoad(gameObject);

        inputBlockerGroup.blocksRaycasts = true;

        StartCoroutine(
            PlayGameOverRoutine()
        );
    }


    [ContextMenu("Preview Game Over")]
    private void PreviewGameOver()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Play();
    }


    // <변경부분>
    // 킹 사망 → 정상 화면 대기 → 오류 발생/복구 반복
    // → 화면 붕괴 → NO SIGNAL → Title.
    private IEnumerator PlayGameOverRoutine()
    {
        blackoutGroup.alpha = 0f;
        messageGroup.alpha = 0f;

        if (corruptionFX != null)
        {
            corruptionFX.SetTextTearing(false);
            corruptionFX.SetBackgroundBurst(false);
            corruptionFX.CompleteBlackout();
        }

        if (signalNoiseUI != null)
        {
            signalNoiseUI.SetVisible(false);
        }

        // 1. 킹 사망 화면을 잠시 그대로 보여준다.
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, kingDeathWaitDuration)
        );

        // 2. 첫 번째 비교적 약한 오류.
        yield return StartCoroutine(
            PlayBattleGlitchBurst(
                firstGlitchDuration,
                firstGlitchStrength
            )
        );

        // 3. 다시 정상 화면.
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, firstRecoveryDuration)
        );

        // 4. 두 번째 오류.
        yield return StartCoroutine(
            PlayBattleGlitchBurst(
                secondGlitchDuration,
                secondGlitchStrength
            )
        );

        // 5. 다시 정상 화면.
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, secondRecoveryDuration)
        );

        // 6. 최종 강한 글리치와 검은 화면 교차.
        yield return StartCoroutine(
            PlayFinalCrashRoutine()
        );

        // 7. 노이즈 속에서 NO SIGNAL이 나타난다.
        yield return StartCoroutine(
            PlayNoSignalRoutine()
        );

        // <변경부분>
        // 마지막 강한 지직 후 REBOOTING... 문구를 잠깐 보여준다.
        yield return StartCoroutine(
            PlayRebootMessageRoutine()
        );

        // <변경부분>
        // 마지막 전환에서는 완전한 검은 화면을 사용한다.
        blackoutGroup.alpha = 1f;
        messageGroup.alpha = 0f;

        if (corruptionFX != null)
        {
            corruptionFX.SetTextTearing(false);
            corruptionFX.SetBackgroundBurst(false);
            corruptionFX.CompleteBlackout();
        }

        if (signalNoiseUI != null)
        {
            signalNoiseUI.SetVisible(false);
        }

        if (screenGlitchController != null)
        {
            screenGlitchController.StopGlitch();
        }

        // <변경부분>
        // 검은 배경 Image가 반투명하게 설정되어 있어도
        // Title 로딩 중에는 완전히 불투명하게 만든다.
        UnityEngine.UI.Image blackoutImage =
            blackoutGroup.GetComponent<UnityEngine.UI.Image>();

        if (blackoutImage != null)
        {
            Color color = blackoutImage.color;
            color.a = 1f;
            blackoutImage.color = color;
        }

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(titleSceneName);

        if (operation == null)
        {
            Debug.LogError(
                "타이틀 씬 로드를 시작하지 못했습니다.",
                this
            );

            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        yield return null;

        // 8. 검은 화면을 걷어 타이틀을 보여준다.
        float elapsed = 0f;

        while (elapsed < titleRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed /
                Mathf.Max(0.05f, titleRevealDuration)
            );

            blackoutGroup.alpha = 1f - t;

            yield return null;
        }

        blackoutGroup.alpha = 0f;
        inputBlockerGroup.blocksRaycasts = false;

        Destroy(gameObject);
    }


    // <변경부분>
    // 글리치가 끝나면 완전한 정상 화면으로 복귀한다.
    private IEnumerator PlayBattleGlitchBurst(
        float duration,
        float strength)
    {
        blackoutGroup.alpha = 0f;
        messageGroup.alpha = 0f;

        if (signalNoiseUI != null)
        {
            signalNoiseUI.SetIntensity(0.4f);
            signalNoiseUI.SetVisible(true);
        }

        if (screenGlitchController != null)
        {
            screenGlitchController.PlayGlitch(
                strength,
                Mathf.Max(0.05f, duration)
            );
        }

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.05f, duration)
        );

        if (screenGlitchController != null)
        {
            screenGlitchController.StopGlitch();
        }

        if (signalNoiseUI != null)
        {
            signalNoiseUI.SetVisible(false);
        }
    }


    // <변경부분>
    // 마지막 오류에서는 정상 전투 화면과 검은 화면을
    // 매우 짧고 불규칙한 느낌으로 번갈아 표시한다.
    private IEnumerator PlayFinalCrashRoutine()
    {
        float duration =
            Mathf.Max(0.05f, finalCrashDuration);

        if (screenGlitchController != null)
        {
            screenGlitchController.PlayGlitch(
                openingGlitchStrength,
                duration
            );
        }

        // <변경부분>
        // 최종 오류부터는 노이즈를 끊지 않고
        // NO SIGNAL 구간까지 계속 유지한다.
        if (signalNoiseUI != null)
        {
            signalNoiseUI.SetIntensity(1f);
            signalNoiseUI.SetVisible(true);
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            // <변경부분>
            // 검정 / 깨진 전투 / 검정 / 깨진 전투 /
            // 검정 순서로 전환한다.
            bool showBlack =
                (t >= 0.22f && t < 0.38f) ||
                (t >= 0.60f && t < 0.76f) ||
                (t >= 0.88f);

            blackoutGroup.alpha =
                showBlack ? 1f : 0f;

            if (corruptionFX != null)
            {
                corruptionFX.SetBackgroundBurst(
                    showBlack
                );
            }

            yield return null;
        }

        blackoutGroup.alpha = 1f;

        if (corruptionFX != null)
        {
            corruptionFX.SetBackgroundBurst(false);
        }

        if (screenGlitchController != null)
        {
            screenGlitchController.StopGlitch();
        }
    }


    // <변경부분>
    // 신호 없음 화면에서 노이즈가 점점 약해지는 동안
    // 검은 화면과 깨진 전투 화면이 간헐적으로 교차한다.
    private IEnumerator PlayNoSignalRoutine()
    {
        if (mainMessageText != null &&
            string.IsNullOrEmpty(defaultMessageText) == false)
        {
            mainMessageText.text =
                defaultMessageText;
        }

        float duration =
            Mathf.Max(
                messageHoldDuration,
                signalRevealDuration +
                messageFadeOutDuration + 0.1f
            );

        float elapsed = 0f;
        float nextBurstTime = 0.1f;
        float burstEndTime = -1f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float noiseProgress =
                Mathf.Clamp01(
                    elapsed /
                    Mathf.Max(
                        0.1f,
                        signalNoiseDecayDuration
                    )
                );

            // <변경부분>
            // 기본 노이즈는 절대로 중간에 끄지 않는다.
            // 1 → 0.12 순서로 서서히 약해진다.
            float baseNoiseStrength =
                Mathf.Lerp(
                    1f,
                    signalNoiseEndStrength,
                    Mathf.SmoothStep(
                        0f, 1f, noiseProgress
                    )
                );

            if (elapsed >= nextBurstTime)
            {
                float burstDuration =
                    signalBurstDuration *
                    Random.Range(0.7f, 1.4f);

                burstEndTime =
                    elapsed + burstDuration;

                nextBurstTime =
                    elapsed +
                    signalBurstInterval *
                    Random.Range(0.8f, 1.5f);

                // <변경부분>
                // 검정 화면을 걷었을 때 보이는 전투 화면에
                // 실제 RGB 글리치가 들어가도록 실행한다.
                if (screenGlitchController != null)
                {
                    screenGlitchController.PlayGlitch(
                        openingGlitchStrength,
                        Mathf.Max(0.05f, burstDuration)
                    );
                }
            }

            bool glitchBurst =
                elapsed < burstEndTime;

            // <변경부분>
            // 강한 글리치 순간에 검은 배경을 잠시 끈다.
            // 검정 화면의 단순한 깜빡임이 아니라
            // 뒤쪽의 깨진 전투 화면이 드러난다.
            blackoutGroup.alpha =
                glitchBurst ? 0f : 1f;

            if (signalNoiseUI != null)
            {
                signalNoiseUI.SetIntensity(
                    glitchBurst
                        ? Mathf.Max(0.9f, baseNoiseStrength)
                        : baseNoiseStrength
                );
            }

            if (corruptionFX != null)
            {
                corruptionFX.SetBackgroundBurst(
                    !glitchBurst &&
                    Random.value < 0.35f
                );
            }

            // <변경부분>
            // 처음 1초에는 글자도 대부분 찢어진 상태로
            // 나타나고, 이후에는 간헐적으로만 찢어진다.
            bool tearText =
                glitchBurst ||
                (elapsed < signalRevealDuration &&
                 Random.value < 0.65f);

            if (tearText)
            {
                ApplyTextGlitch();
            }
            else
            {
                HideTextGlitch();
            }

            // <변경부분>
            // 글자가 노이즈 속에서 점차 선명해진다.
            float reveal =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed /
                        Mathf.Max(
                            0.05f,
                            signalRevealDuration
                        )
                    )
                );

            float fadeStart =
                Mathf.Max(
                    0f,
                    duration - messageFadeOutDuration
                );

            float fadeOut =
                1f - Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        (elapsed - fadeStart) /
                        Mathf.Max(
                            0.05f,
                            messageFadeOutDuration
                        )
                    )
                );

            messageGroup.alpha =
                Mathf.Min(reveal, fadeOut);

            yield return null;
        }

        HideTextGlitch();

        if (corruptionFX != null)
        {
            corruptionFX.SetBackgroundBurst(false);
        }

        messageGroup.alpha = 0f;
        blackoutGroup.alpha = 1f;
    }


    // <변경부분>
    // NO SIGNAL 이후 마지막 한 번 더 지직거리고
    // REBOOTING... 문구를 잠깐 표시한다.
    private IEnumerator PlayRebootMessageRoutine()
    {
        // 마지막 강한 glitch burst
        if (screenGlitchController != null)
        {
            screenGlitchController.PlayGlitch(
                openingGlitchStrength,
                Mathf.Max(
                    0.05f,
                    rebootMessageGlitchDuration
                )
            );
        }

        if (signalNoiseUI != null)
        {
            signalNoiseUI.SetVisible(true);
            signalNoiseUI.SetIntensity(1f);
        }

        // 잠깐 깨진 화면 노출
        blackoutGroup.alpha = 0f;

        yield return new WaitForSecondsRealtime(
            Mathf.Max(
                0.05f,
                rebootMessageGlitchDuration
            )
        );

        // 다시 검은 화면 위에 문구 표시
        blackoutGroup.alpha = 1f;
        messageGroup.alpha = 1f;

        if (mainMessageText != null)
        {
            mainMessageText.text =
                rebootMessageText;
        }

        // <변경부분>
        // REBOOTING...은 NO SIGNAL보다 덜 길게,
        // 하지만 약간의 glitch는 유지한다.
        float elapsed = 0f;
        float duration =
            Mathf.Max(
                0.05f,
                rebootMessageDuration
            );

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            bool glitchActive =
                Random.value < 0.4f;

            if (glitchActive)
            {
                ApplyTextGlitch();
            }
            else
            {
                HideTextGlitch();
            }

            // 노이즈는 강하게 시작했다가 조금 줄어들게
            if (signalNoiseUI != null)
            {
                float t = Mathf.Clamp01(
                    elapsed / duration
                );

                signalNoiseUI.SetIntensity(
                    Mathf.Lerp(
                        0.9f,
                        0.3f,
                        t
                    )
                );
            }

            yield return null;
        }

        HideTextGlitch();
        messageGroup.alpha = 0f;
    }

    // <변경부분>
    // TMP 폰트 재질을 변경하지 않고
    // 복제된 텍스트 2개를 어긋나게 표시한다.
    private void ApplyTextGlitch()
    {
        if (mainMessageText == null)
        {
            return;
        }

        Color flickerColor =
            mainMessageOriginalColor;

        flickerColor.a *= Random.Range(
            0.55f,
            1f
        );

        mainMessageText.color = flickerColor;

        // <변경부분>
        // 글자의 여러 가로 조각을 독립적으로 이동시킨다.
        if (corruptionFX != null)
        {
            corruptionFX.SetTextTearing(true);
        }

        if (redGhostText != null)
        {
            redGhostText.text =
                mainMessageText.text;

            redGhostText.enabled = true;

            if (redGhostRect != null)
            {
                redGhostRect.anchoredPosition =
                    redGhostOriginalPosition +
                    new Vector2(
                        Random.Range(
                            -textGlitchOffset,
                            textGlitchOffset
                        ),
                        Random.Range(-3f, 3f)
                    );
            }
        }

        if (cyanGhostText != null)
        {
            cyanGhostText.text =
                mainMessageText.text;

            cyanGhostText.enabled = true;

            if (cyanGhostRect != null)
            {
                cyanGhostRect.anchoredPosition =
                    cyanGhostOriginalPosition +
                    new Vector2(
                        Random.Range(
                            -textGlitchOffset,
                            textGlitchOffset
                        ),
                        Random.Range(-3f, 3f)
                    );
            }
        }
    }


    private void HideTextGlitch()
    {
        // <변경부분>
        // 글리치 종료 후 원래 TMP를 복구한다.
        if (corruptionFX != null)
        {
            corruptionFX.SetTextTearing(false);
        }

        if (mainMessageText != null)
        {
            mainMessageText.color =
                mainMessageOriginalColor;
        }

        if (redGhostText != null)
        {
            redGhostText.enabled = false;
        }

        if (cyanGhostText != null)
        {
            cyanGhostText.enabled = false;
        }

        if (redGhostRect != null)
        {
            redGhostRect.anchoredPosition =
                redGhostOriginalPosition;
        }

        if (cyanGhostRect != null)
        {
            cyanGhostRect.anchoredPosition =
                cyanGhostOriginalPosition;
        }
    }
}
