using UnityEngine;

public class Decoration : MonoBehaviour
{
    // 장식물이 어떤 종류인지 저장
    public DecorationType DecorationType { get; private set; }

    // 장식물이 배치된 배경 타일 좌표를 저장
    public int X { get; private set; }
    public int Y { get; private set; }

    // 장식물의 타입과 배치 좌표를 초기화
    public void Initialize(DecorationType decorationType, int x, int y)
    {
        DecorationType = decorationType;
        X = x;
        Y = y;
    }
}