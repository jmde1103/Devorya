using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// <변경부분>
// 현재 런을 종료하고 타이틀로 돌아가는 공통 버튼.
//
// BattleScene, WorldMapScene, EventScene 등에서
// 동일한 초기화 경로를 사용한다.
//
// Button 컴포넌트가 있는 GameObject에 추가하면
// OnClick을 자동으로 연결한다.
[RequireComponent(typeof(Button))]
public sealed class ReturnToTitleButton : MonoBehaviour
{
    [Header("Scene Transition")]
    [SerializeField]
    private string titleSceneName = "Title";

    private Button targetButton;
    private bool isReturning;


    private void Awake()
    {
        targetButton = GetComponent<Button>();

        targetButton.onClick.RemoveListener(ReturnToTitle);
        targetButton.onClick.AddListener(ReturnToTitle);
    }


    private void OnDestroy()
    {
        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(ReturnToTitle);
        }
    }


    // <변경부분>
    // 현재 런을 초기화한 다음 타이틀 씬으로 이동한다.
    //
    // 씬만 변경하면 DontDestroyOnLoad 및 static 데이터가
    // 남기 때문에 반드시 런 초기화를 먼저 실행한다.
    public void ReturnToTitle()
    {
        if (isReturning)
        {
            return;
        }

        // 타이틀 씬이 실제로 로드 가능한지 먼저 확인한다.
        // 로드할 수 없다면 런 데이터를 삭제하지 않는다.
        if (string.IsNullOrWhiteSpace(titleSceneName) ||
            !Application.CanStreamedLevelBeLoaded(titleSceneName))
        {
            Debug.LogError(
                "타이틀 복귀 실패: " +
                $"'{titleSceneName}' 씬을 로드할 수 없습니다. " +
                "Build Profiles의 Scene List를 확인해주세요.",
                this
            );

            return;
        }

        isReturning = true;

        // <변경부분>
        // 런 전체 데이터 초기화.
        //
        // 플레이어 기물, 골드, 아이템, 유물,
        // 엔드 타이머 등을 초기화한다.
        if (RunStateManager.Instance != null)
        {
            RunStateManager.Instance.ClearRunState();
        }
        else
        {
            Debug.LogWarning(
                "타이틀 복귀: RunStateManager가 없어 " +
                "런 데이터 초기화를 실행하지 못했습니다.",
                this
            );
        }

        // <변경부분>
        // 월드맵 노드, 포그, 방문 기록,
        // 이벤트/전투 대기 데이터와 상점 상태를 초기화한다.
        WorldMapRuntimeState.Clear();

        // <변경부분>
        // 다음 TextCutscene에 전달할 데이터가
        // 이전 런에서 남아 있지 않도록 초기화한다.
        TextCutsceneRuntimeState.Clear();

        // <변경부분>
        // 전투 슬로모션이나 일시정지 상태에서
        // 타이틀로 돌아와도 정상 속도로 실행한다.
        Time.timeScale = 1f;

        Debug.Log(
            "런 초기화 완료. 타이틀 씬으로 이동합니다.",
            this
        );

        SceneManager.LoadScene(titleSceneName);
    }
}
