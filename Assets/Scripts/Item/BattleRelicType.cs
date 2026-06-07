// 전투 중 보유하면 지속 효과를 제공하는 유물 종류
public enum BattleRelicType
{
    // 유물이 없는 상태
    None,

    // <변경부분> 플레이어가 흡수에 성공하면 턴당 1번 찬스어택을 확정 발동시키는 유물
    AbsorbChanceAttackOncePerTurn
}
