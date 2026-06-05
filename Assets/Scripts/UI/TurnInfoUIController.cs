using TMPro;
using UnityEngine;

// <변경부분> 좌측 상단 턴 정보 UI를 관리하는 컨트롤러
public class TurnInfoUIController : MonoBehaviour
{
    [Header("Turn Info Texts")]
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text turnNumberText;
    [SerializeField] private TMP_Text turnOwnerText;

    // <변경부분> 스테이지 이름을 표시하는 함수
    public void SetStageName(string stageName)
    {
        if (stageNameText == null)
        {
            return;
        }

        stageNameText.text = stageName;
    }

    // <변경부분> 현재 턴 정보를 UI에 표시하는 함수
    public void RefreshTurnInfo(int turnCount, BattleTurn currentTurn)
    {
        if (turnNumberText != null)
        {
            turnNumberText.text = "Turn " + turnCount;
        }

        if (turnOwnerText != null)
        {
            turnOwnerText.text = GetTurnOwnerText(currentTurn);
        }
    }

    // <변경부분> 현재 턴 enum을 UI용 텍스트로 변환하는 함수
    private string GetTurnOwnerText(BattleTurn currentTurn)
    {
        switch (currentTurn)
        {
            case BattleTurn.Player:
                return "플레이어 턴";

            case BattleTurn.Enemy:
                return "AI 턴";

            default:
                return "";
        }
    }
}