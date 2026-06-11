using UnityEngine;

// <변경부분> 전투 유물의 실제 효과 발동 조건을 BattleRelicData 기준으로 판정하는 클래스
public class BattleRelicEffectHandler : MonoBehaviour
{
    // <변경부분> 흡수 성공 시 유물 효과로 찬스어택을 발동할 수 있는지 BattleRelicData 기준으로 검사하는 함수
    public bool CanActivateAbsorbChanceAttackRelic(
        BattleRelicData relicData,
        Piece piece,
        BattleTurn currentTurn,
        bool hasUsedThisTurn,
        bool hasAnySelectableTile
    )
    {
        // <변경부분> 보유한 유물 데이터가 없으면 발동 불가
        if (relicData == null)
        {
            return false;
        }

        // <변경부분> 흡수 찬스어택 유물이 아니면 발동 불가
        if (relicData.relicType != BattleRelicType.AbsorbChanceAttackOncePerTurn)
        {
            return false;
        }

        // 검사할 기물이 없으면 발동 불가
        if (piece == null)
        {
            return false;
        }

        // <변경부분> 데이터에서 플레이어 턴 전용 여부를 확인
        if (relicData.onlyPlayerTurn && currentTurn != BattleTurn.Player)
        {
            return false;
        }

        // <변경부분> 데이터에서 턴당 1회 제한 여부를 확인
        if (relicData.oncePerTurn && hasUsedThisTurn)
        {
            Debug.Log("유물 효과 발동 실패: 이번 턴에 이미 흡수 찬스어택 유물이 발동했습니다.");
            return false;
        }

        // <변경부분> 데이터에서 추가 행동 가능한 타일 필요 여부를 확인
        if (relicData.requireSelectableTile && hasAnySelectableTile == false)
        {
            Debug.Log("유물 효과 발동 실패: 추가 행동 가능한 이동/공격 타일이 없습니다.");
            return false;
        }

        // <변경부분> 데이터에 설정된 확률 기준으로 유물 발동 판정
        float clampedChancePercent = Mathf.Clamp(relicData.triggerChancePercent, 0f, 100f);
        float randomValue = Random.Range(0f, 100f);

        bool isActivated = randomValue < clampedChancePercent;

        Debug.Log($"유물 효과 판정: {relicData.relicName} / 확률 {clampedChancePercent:F1}% / 랜덤 {randomValue:F1} / 결과 {isActivated}");

        return isActivated;
    }
}
