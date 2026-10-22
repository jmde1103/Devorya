using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [InspectorName("월드맵 이벤트 데이터 (선택)")] public WorldMapEventData worldMapEventData;
    // Inspector에서 입력한 씬 이름으로 이동
    public void ChangeScene(string sceneName)
    {
        WorldMapEventRuntimeState.Queue(worldMapEventData);
        SceneManager.LoadScene(sceneName);
    }
}
