using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

// 타이틀 화면의 Play / Setting 버튼 선택과
// 2단계 클릭 실행 흐름을 관리한다.
public class TitleMenuController : MonoBehaviour
{
    [Header("Scene Transition")]

    // Play 실행 후 씬 이동 전에 사용할
    // 전체 화면 전환 애니메이션
    [SerializeField]
    private TitleSceneTransitionController
    sceneTransitionController;

    [Header("Play Button")]

    [SerializeField]
    private Button playButton;

    // Play 버튼 위치
    [SerializeField]
    private RectTransform playButtonRect;

    // Play 기본 부유 / 선택 확대 애니메이션
    [SerializeField]
    private TitleMenuButtonAnimation playButtonAnimation;

    // Play 선택 시 노이즈 애니메이션
    [SerializeField]
    private PopupOpenAnimator playPopupOpenAnimator;


    [Header("Play Pixel Patterns")]

    // Play 주변 픽셀 배치 형태 3가지
    [SerializeField]
    private List<TitleFloatingPixelEffect.PixelPattern>
        playPixelPatterns =
            new List<TitleFloatingPixelEffect.PixelPattern>();


    [Header("Setting Button")]

    [SerializeField]
    private Button settingButton;

    // Setting 버튼 위치
    [SerializeField]
    private RectTransform settingButtonRect;

    // Setting 기본 부유 / 선택 확대 애니메이션
    [SerializeField]
    private TitleMenuButtonAnimation settingButtonAnimation;

    // Setting 선택 시 노이즈 애니메이션
    [SerializeField]
    private PopupOpenAnimator settingPopupOpenAnimator;


    [Header("Setting Pixel Patterns")]

    // Setting 주변 픽셀 배치 형태 3가지
    [SerializeField]
    private List<TitleFloatingPixelEffect.PixelPattern>
        settingPixelPatterns =
            new List<TitleFloatingPixelEffect.PixelPattern>();


    [Header("Floating Pixels")]

    // 화면 전체 검은 픽셀 관리
    [SerializeField]
    private TitleFloatingPixelEffect floatingPixelEffect;


    [Header("Play Destination")]

    // Play 두 번째 클릭 시 사용할 StageBattleData
    [SerializeField]
    private StageBattleData playStageBattleData;

    // 이동할 씬 이름
    [SerializeField]
    private string playSceneName;


    // 현재 선택된 메뉴
    private TitleMenuType selectedMenu =
        TitleMenuType.None;

    // 씬 이동 중 중복 클릭 방지
    private bool isLoadingScene;


    private enum TitleMenuType
    {
        None,
        Play,
        Setting
    }


    private void Awake()
    {
        // Play 버튼 이벤트 연결
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(
                OnClickPlay
            );

            playButton.onClick.AddListener(
                OnClickPlay
            );
        }


        // Setting 버튼 이벤트 연결
        if (settingButton != null)
        {
            settingButton.onClick.RemoveListener(
                OnClickSetting
            );

            settingButton.onClick.AddListener(
                OnClickSetting
            );
        }
    }


    // =========================================================
    // PLAY
    // =========================================================

    private void OnClickPlay()
    {
        if (isLoadingScene)
        {
            return;
        }


        // 이미 Play가 선택되어 있다면
        // 두 번째 클릭 = 실행
        if (selectedMenu ==
            TitleMenuType.Play)
        {
            ConfirmPlay();
            return;
        }


        SelectPlay();
    }


    // Play 첫 클릭
    private void SelectPlay()
    {
        // 다른 메뉴가 선택되어 있었다면
        // 버튼 확대 상태만 먼저 초기화
        ResetCurrentButtonSelection();


        selectedMenu =
            TitleMenuType.Play;


        // 검은 픽셀을 Play 쪽으로 집결
        // 패턴 3개 중 하나는 내부에서 랜덤 선택
        if (floatingPixelEffect != null &&
            playButtonRect != null)
        {
            floatingPixelEffect.GatherTo(
                playButtonRect,
                playPixelPatterns
            );
        }


        // 기존 PopupOpenAnimationData 기반
        // 노이즈 깜빡임 실행
        if (playPopupOpenAnimator != null)
        {
            playPopupOpenAnimator.PlayOpen();
        }


        // Play 버튼 확대
        if (playButtonAnimation != null)
        {
            playButtonAnimation.PlaySelectAnimation();
        }


        Debug.Log(
            "타이틀 PLAY 선택: " +
            "한 번 더 누르면 실행됩니다."
        );
    }


    // Play 두 번째 클릭
    private void ConfirmPlay()
    {
        if (playStageBattleData == null)
        {
            Debug.LogError(
                "타이틀 PLAY 실행 실패: " +
                "Play Stage Battle Data가 연결되지 않았습니다."
            );

            return;
        }


        if (string.IsNullOrWhiteSpace(
                playSceneName))
        {
            Debug.LogError(
                "타이틀 PLAY 실행 실패: " +
                "Play Scene Name이 비어 있습니다."
            );

            return;
        }


        if (isLoadingScene)
        {
            return;
        }


        isLoadingScene = true;


        // 바로 Scene을 불러오지 않고
        // 전환 애니메이션부터 실행
        StartCoroutine(
            PlaySceneTransitionRoutine()
        );
    }


    // =========================================================
    // PLAY SCENE TRANSITION
    // =========================================================

    // 전체 화면 확대 + 뿌연 Fade 효과가 끝난 후
    // 실제 Battle Scene으로 이동한다.
    private IEnumerator PlaySceneTransitionRoutine()
    {
        // =========================================================
        // StageBattleData 전달 로직
        //
        // 현재 프로젝트의 WorldMapRuntimeState 최신 구조에 맞춰
        // Scene Load 전에 이 위치에서 StageBattleData를 전달한다.
        // =========================================================


        // 전환 애니메이션 실행
        if (sceneTransitionController != null)
        {
            yield return
                sceneTransitionController
                    .PlayTransition();
        }


        Debug.Log(
            $"타이틀 PLAY 씬 이동: {playSceneName}"
        );


        // 화면이 완전히 덮인 다음 Scene 변경
        SceneManager.LoadScene(
            playSceneName
        );
    }


    // =========================================================
    // SETTING
    // =========================================================

    private void OnClickSetting()
    {
        if (isLoadingScene)
        {
            return;
        }


        // 이미 Setting이 선택되어 있다면
        // 두 번째 클릭
        if (selectedMenu ==
            TitleMenuType.Setting)
        {
            ConfirmSetting();
            return;
        }


        SelectSetting();
    }


    // Setting 첫 클릭
    private void SelectSetting()
    {
        ResetCurrentButtonSelection();


        selectedMenu =
            TitleMenuType.Setting;


        // Setting용 3가지 패턴 중
        // 하나를 랜덤 선택해 픽셀 이동
        if (floatingPixelEffect != null &&
            settingButtonRect != null)
        {
            floatingPixelEffect.GatherTo(
                settingButtonRect,
                settingPixelPatterns
            );
        }


        // Setting 버튼 노이즈
        if (settingPopupOpenAnimator != null)
        {
            settingPopupOpenAnimator.PlayOpen();
        }


        // Setting 버튼 확대
        if (settingButtonAnimation != null)
        {
            settingButtonAnimation.PlaySelectAnimation();
        }


        Debug.Log(
            "타이틀 SETTING 선택"
        );
    }


    // Setting 두 번째 클릭
    private void ConfirmSetting()
    {
        // Setting 실제 기능은 아직 구현하지 않는다.
        Debug.Log(
            "타이틀 SETTING: " +
            "현재 Setting 기능은 미구현 상태입니다."
        );
    }


    // =========================================================
    // BACKGROUND
    // =========================================================

    // Play / Setting이 아닌 화면 영역을 클릭했을 때 호출
    public void OnClickBackground()
    {
        if (isLoadingScene)
        {
            return;
        }


        // 아무 것도 선택되지 않았다면 처리하지 않음
        if (selectedMenu ==
            TitleMenuType.None)
        {
            return;
        }


        // 선택된 버튼 크기를 원래대로 복구
        ResetCurrentButtonSelection();


        // 모든 픽셀을
        // 원래 위치 + Alpha 1로 복귀
        if (floatingPixelEffect != null)
        {
            floatingPixelEffect.ResetToDefault();
        }


        selectedMenu =
            TitleMenuType.None;


        Debug.Log(
            "타이틀 메뉴 선택 해제"
        );
    }


    // =========================================================
    // SELECTION RESET
    // =========================================================

    // 현재 선택된 버튼의 확대 상태만 해제한다.
    // 픽셀은 새 버튼으로 바로 이동해야 하므로
    // 여기서는 ResetToDefault를 실행하지 않는다.
    private void ResetCurrentButtonSelection()
    {
        switch (selectedMenu)
        {
            case TitleMenuType.Play:

                if (playButtonAnimation != null)
                {
                    playButtonAnimation.ResetSelection();
                }

                break;


            case TitleMenuType.Setting:

                if (settingButtonAnimation != null)
                {
                    settingButtonAnimation.ResetSelection();
                }

                break;
        }
    }
}