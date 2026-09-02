using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// 데보리아 전체에서 사용하는 언어 설정 관리자.
//
// 처음 실행:
// Unity Localization의 Locale Selector 결과를 그대로 사용한다.
// 예: 시스템 언어가 영어면 English.
//
// 사용자가 게임 내에서 언어를 직접 변경:
// 선택한 Locale을 적용하고 PlayerPrefs에 저장한다.
//
// 이후 게임 실행:
// 저장된 언어가 존재하면 시스템 언어보다
// 사용자가 직접 선택했던 언어를 우선 적용한다.
public class DevoryaLocalizationManager : MonoBehaviour
{
    public static DevoryaLocalizationManager Instance
    {
        get;
        private set;
    }

    // PlayerPrefs에 저장할 언어 설정 Key.
    private const string LanguagePreferenceKey =
        "Devorya.Language";

    private bool isInitialized = false;

    public bool IsInitialized =>
        isInitialized;

    private void Awake()
    {
        // Scene 이동 중 동일 Manager가 중복 생성되는 것을 방지한다.
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 언어 설정은 Scene이 바뀌어도 유지한다.
        DontDestroyOnLoad(gameObject);

        StartCoroutine(
            InitializeLocalizationRoutine()
        );
    }

    // Unity Localization 초기화가 끝난 뒤
    // 이전에 사용자가 직접 저장한 언어가 있다면 복원한다.
    private IEnumerator InitializeLocalizationRoutine()
    {
        yield return
            LocalizationSettings
                .InitializationOperation;

        // 저장된 언어가 없다면
        // System Locale Selector / Specific Locale Selector 등
        // Project Settings에서 결정한 초기 Locale을 그대로 사용한다.
        if (PlayerPrefs.HasKey(
                LanguagePreferenceKey))
        {
            string savedLocaleCode =
                PlayerPrefs.GetString(
                    LanguagePreferenceKey
                );

            ApplyLocaleByCode(
                savedLocaleCode,
                false
            );
        }

        isInitialized =
            true;

        Locale selectedLocale =
            LocalizationSettings
                .SelectedLocale;

        Debug.Log(
            $"Localization 초기화 완료: " +
            $"{selectedLocale?.Identifier.Code}"
        );
    }

    // 한국어 선택.
    // 추후 Button OnClick에서도 바로 연결할 수 있다.
    public void SetKorean()
    {
        SetLanguage(
            "ko"
        );
    }

    // 영어 선택.
    public void SetEnglish()
    {
        SetLanguage(
            "en"
        );
    }

    // 일본어 선택.
    public void SetJapanese()
    {
        SetLanguage(
            "ja"
        );
    }

    // Locale Code를 사용하여 언어를 변경한다.
    //
    // 현재 지원:
    // ko = Korean
    // en = English
    // ja = Japanese
    public void SetLanguage(
        string localeCode)
    {
        if (isInitialized == false)
        {
            Debug.LogWarning(
                "Localization 초기화 전이라 " +
                "언어를 변경할 수 없습니다."
            );

            return;
        }

        ApplyLocaleByCode(
            localeCode,
            true
        );
    }

    // Available Locales에서 요청한 Locale Code를 찾아
    // 실제 SelectedLocale에 적용한다.
    private void ApplyLocaleByCode(
        string localeCode,
        bool savePreference)
    {
        if (string.IsNullOrWhiteSpace(
                localeCode))
        {
            return;
        }

        Locale targetLocale =
            LocalizationSettings
                .AvailableLocales
                .GetLocale(
                    new LocaleIdentifier(
                        localeCode
                    )
                );

        if (targetLocale == null)
        {
            Debug.LogWarning(
                $"지원하지 않는 Locale입니다: " +
                $"{localeCode}"
            );

            return;
        }

        LocalizationSettings
            .SelectedLocale =
            targetLocale;

        if (savePreference)
        {
            PlayerPrefs.SetString(
                LanguagePreferenceKey,
                localeCode
            );

            PlayerPrefs.Save();
        }

        Debug.Log(
            $"언어 변경 완료: " +
            $"{targetLocale.LocaleName} / " +
            $"{targetLocale.Identifier.Code}"
        );
    }

    // 현재 선택되어 있는 Locale Code를 반환한다.
    public string GetCurrentLanguageCode()
    {
        Locale selectedLocale =
            LocalizationSettings
                .SelectedLocale;

        if (selectedLocale == null)
        {
            return string.Empty;
        }

        return
            selectedLocale
                .Identifier
                .Code;
    }

    // 개발 테스트용.
    // 저장했던 사용자 언어 설정을 제거하여
    // 다음 실행부터 Project Settings의 Locale Selector를 다시 사용한다.
    public void ResetSavedLanguage()
    {
        PlayerPrefs.DeleteKey(
            LanguagePreferenceKey
        );

        PlayerPrefs.Save();

        Debug.Log(
            "저장된 언어 설정을 초기화했습니다."
        );
    }
}
