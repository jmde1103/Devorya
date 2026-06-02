using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BackgroundTile : MonoBehaviour
{
    [Header("배경 타일 데이터")]
    // <변경부분> Play 후에도 유지되도록 배경 타일 타입을 직렬화해서 저장
    [SerializeField] private BackgroundTileType tileType;

    // <변경부분> Play 후에도 유지되도록 배경 타일 X 좌표를 직렬화해서 저장
    [SerializeField] private int x;

    // <변경부분> Play 후에도 유지되도록 배경 타일 Y 좌표를 직렬화해서 저장
    [SerializeField] private int y;

    // 배경 타일이 어떤 지형 타입인지 확인
    public BackgroundTileType TileType => tileType;

    // 배경 타일의 배열 X 좌표 확인
    public int X => x;

    // 배경 타일의 배열 Y 좌표 확인
    public int Y => y;

    // 배경 타일의 타입과 좌표 정보를 초기화
    public void Initialize(BackgroundTileType newTileType, int newX, int newY)
    {
        tileType = newTileType;
        x = newX;
        y = newY;

        MarkDirtyInEditor();
    }

    // 기존 배경 타일 오브젝트를 유지한 채 타입 정보만 변경
    public void ChangeTileType(BackgroundTileType newTileType)
    {
        tileType = newTileType;

        MarkDirtyInEditor();
    }

    // <변경부분> 에디터에서 변경된 배경 타일 데이터가 씬에 저장되도록 표시
    private void MarkDirtyInEditor()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(gameObject);
        }
#endif
    }
}
