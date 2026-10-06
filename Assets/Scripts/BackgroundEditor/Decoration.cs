using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Decoration : MonoBehaviour
{
    [Header("장식물 데이터")]

    // 장식물 종류.
    [SerializeField]
    private DecorationType decorationType;

    // 이 장식물이 기준으로 삼는 Background Tile의 X 좌표.
    //
    // Grid Placement에서는 실제 배치된 Grid 좌표이고,
    // Free Placement에서는 선택한 Anchor Tile의 X 좌표다.
    [SerializeField]
    private int x;

    // 이 장식물이 기준으로 삼는 Background Tile의 Y 좌표.
    //
    // Grid Placement에서는 실제 배치된 Grid 좌표이고,
    // Free Placement에서는 선택한 Anchor Tile의 Y 좌표다.
    [SerializeField]
    private int y;

    // <변경부분>
    // 동일한 Anchor Tile 안에서 장식물끼리
    // 앞뒤 순서를 조절하기 위한 Layer 값.
    //
    // 0  = 기본
    // 음수 = 뒤쪽
    // 양수 = 앞쪽
    //
    // 실제 Sorting Order 계산은 BackgroundManager가 담당한다.
    [SerializeField, HideInInspector]
    private int layerOffset = 0;


    // 장식물 종류 확인
    public DecorationType DecorationType =>
        decorationType;

    // 기준 Background Tile X 좌표 확인
    public int X =>
        x;

    // 기준 Background Tile Y 좌표 확인
    public int Y =>
        y;

    // <변경부분>
    // 현재 장식물이 사용하는 Layer 값.
    // BackgroundMapData 저장 시 사용한다.
    public int LayerOffset =>
        layerOffset;


    // 기존 생성 코드 호환용.
    // 별도의 Layer 값을 전달하지 않으면 기본값 0을 사용한다.
    public void Initialize(
        DecorationType newDecorationType,
        int newX,
        int newY)
    {
        Initialize(
            newDecorationType,
            newX,
            newY,
            0
        );
    }


    // <변경부분>
    // 장식물 종류, Anchor 좌표,
    // 동일 Anchor 내부 Layer 값을 함께 초기화한다.
    public void Initialize(
        DecorationType newDecorationType,
        int newX,
        int newY,
        int newLayerOffset)
    {
        decorationType =
            newDecorationType;

        x =
            newX;

        y =
            newY;

        layerOffset =
            newLayerOffset;

        MarkDirtyInEditor();
    }


    // Editor에서 변경된 장식물 데이터가
    // Scene에 저장되도록 Dirty 상태로 표시한다.
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