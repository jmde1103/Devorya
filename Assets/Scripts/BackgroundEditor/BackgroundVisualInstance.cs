using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BackgroundVisualInstance : MonoBehaviour
{
    // 현재 이 Background/Decoration 오브젝트에 적용된
    // 원본 Sprite를 저장한다.
    [SerializeField, HideInInspector]
    private Sprite sourceSprite;

    // 현재 이 Background/Decoration 오브젝트에 적용된
    // 원본 Prefab Asset을 저장한다.
    [SerializeField, HideInInspector]
    private GameObject sourcePrefab;

    // Prefab 방식일 때 실제로 생성된 Visual 자식 오브젝트.
    [SerializeField, HideInInspector]
    private GameObject spawnedVisualRoot;


    public Sprite SourceSprite =>
        sourceSprite;

    public GameObject SourcePrefab =>
        sourcePrefab;

    public GameObject SpawnedVisualRoot =>
        spawnedVisualRoot;

    public bool UsesPrefab =>
        sourcePrefab != null;


    // Sprite 방식으로 변경했을 때 호출한다.
    public void SetSpriteSource(
        Sprite sprite)
    {
        sourceSprite =
            sprite;

        sourcePrefab =
            null;

        spawnedVisualRoot =
            null;

        MarkDirtyInEditor();
    }


    // Prefab 방식으로 변경했을 때
    // 원본 Prefab과 실제 생성된 Visual 자식을 함께 기록한다.
    public void SetPrefabSource(
        GameObject prefab,
        GameObject visualRoot)
    {
        sourceSprite =
            null;

        sourcePrefab =
            prefab;

        spawnedVisualRoot =
            visualRoot;

        MarkDirtyInEditor();
    }


    public void ClearSpawnedVisualReference()
    {
        spawnedVisualRoot =
            null;

        MarkDirtyInEditor();
    }


    private void MarkDirtyInEditor()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(
                this
            );

            EditorUtility.SetDirty(
                gameObject
            );
        }
#endif
    }
}
