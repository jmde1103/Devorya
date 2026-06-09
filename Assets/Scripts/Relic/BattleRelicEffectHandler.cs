using UnityEngine;

// <변경부분> 전투 유물의 실제 효과 발동 조건을 판정하는 클래스
public class BattleRelicEffectHandler : MonoBehaviour
{
    // <변경부분> 흡수 성공 시 유물 효과로 찬스어택을 확정 발동할 수 있는지 검사하는 함수
    public bool CanActivateAbsorbChanceAttackRelic(
        Piece piece,
        BattleTurn currentTurn,
        bool hasRelic,
        bool hasUsedThisTurn,
        bool hasAnySelectableTile
    )
    {
        // 검사할 기물이 없으면 발동 불가
        if (piece == null)
        {
            return false;
        }

        // 현재 플레이어 턴이 아니면 발동 불가
        if (currentTurn != BattleTurn.Player)
        {
            return false;
        }

        // 해당 유물을 보유하고 있지 않으면 발동 불가
        if (hasRelic == false)
        {
            return false;
        }

        // 이번 플레이어 턴에 이미 발동했다면 발동 불가
        if (hasUsedThisTurn)
        {
            Debug.Log("유물 효과 발동 실패: 이번 턴에 이미 흡수 찬스어택 유물이 발동했습니다.");
            return false;
        }

        // 추가 행동 가능한 이동/공격 타일이 없으면 발동하지 않음
        if (hasAnySelectableTile == false)
        {
            Debug.Log("유물 효과 발동 실패: 추가 행동 가능한 이동/공격 타일이 없습니다.");
            return false;
        }

        return true;
    }
}
