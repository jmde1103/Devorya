
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class WorldMapMenuEventSceneLink : MonoBehaviour
{
    [Header("Event Scene")]
    [SerializeField]
    private EventSceneData eventSceneData;

    [SerializeField]
    private string eventSceneName = "EventScene";

    [Header("Click Animation Wait")]
    [SerializeField, Min(0f)]
    private float sceneTransitionDelay = 0.16f;

    private Button targetButton;
    private bool isLoading;

    private void Awake()
    {
        targetButton = GetComponent<Button>();

        // <변경부분>
        // Inspector의 On Click() 연결 없이
        // 버튼 클릭 시 이벤트 씬 진입 함수를 자동 실행한다.
        if (targetButton != null)
        {
            targetButton.onClick.AddListener(OpenEventScene);
        }
    }

    // <변경부분>
    // 오브젝트 파괴 시 등록한 클릭 이벤트를 해제한다.
    private void OnDestroy()
    {
        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(OpenEventScene);
        }
    }

    // 버튼 클릭 이벤트를 통해 자동 호출된다.
    public void OpenEventScene()
    {
        if (isLoading || !isActiveAndEnabled)
            return;

        if (targetButton == null ||
            !targetButton.IsActive() ||
            !targetButton.IsInteractable())
            return;

        if (eventSceneData == null)
        {
            Debug.LogWarning(
                "[WorldMapMenu] EventSceneData가 연결되지 않았습니다.",
                this);
            return;
        }

        if (string.IsNullOrWhiteSpace(eventSceneName) ||
            !Application.CanStreamedLevelBeLoaded(eventSceneName))
        {
            Debug.LogError(
                "[WorldMapMenu] EventScene을 찾을 수 없습니다: " +
                eventSceneName,
                this);
            return;
        }

        StartCoroutine(OpenEventSceneRoutine());
    }

    private IEnumerator OpenEventSceneRoutine()
    {
        isLoading = true;

        // 기존 WorldMapMenuButton 클릭 확대 효과를 보여준다.
        if (sceneTransitionDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                sceneTransitionDelay);
        }

        // 기존 EventSceneData 전달 시스템을 재사용한다.
        bool registered =
            WorldMapRuntimeState
                .SetPendingEventSceneDataFromEventScene(
                    eventSceneData);

        if (!registered)
        {
            Debug.LogError(
                "[WorldMapMenu] EventSceneData 전달 실패",
                this);

            isLoading = false;
            yield break;
        }

        Debug.Log(
            "[WorldMapMenu] EventScene 실행: " +
            eventSceneData.name,
            this);

        SceneManager.LoadScene(eventSceneName);
    }

    private void OnDisable()
    {
        isLoading = false;
    }
}

