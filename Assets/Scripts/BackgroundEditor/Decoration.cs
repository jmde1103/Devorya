using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Decoration : MonoBehaviour
{
    [Header("장식물 데이터")]
    // <변경부분> Play 후에도 유지되도록 장식물 타입을 직렬화해서 저장
    [SerializeField] private DecorationType decorationType;

    // <변경부분> Play 후에도 유지되도록 장식물 X 좌표를 직렬화해서 저장
    [SerializeField] private int x;

    // <변경부분> Play 후에도 유지되도록 장식물 Y 좌표를 직렬화해서 저장
    [SerializeField] private int y;

    // 장식물이 어떤 종류인지 확인
    public DecorationType DecorationType => decorationType;

    // 장식물의 배경 타일 X 좌표 확인
    public int X => x;

    // 장식물의 배경 타일 Y 좌표 확인
    public int Y => y;

    // 장식물의 타입과 배치 좌표를 초기화
    public void Initialize(DecorationType newDecorationType, int newX, int newY)
    {
        decorationType = newDecorationType;
        x = newX;
        y = newY;

        MarkDirtyInEditor();
    }

    // <변경부분> 에디터에서 변경된 장식물 데이터가 씬에 저장되도록 표시
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