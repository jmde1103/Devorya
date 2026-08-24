public enum StatusEffectType
{
    None,

    // <변경부분> 사망 시 주변 빈칸에 젤루 Pawn을 생성하는 상태이상
    Degeneration,

    // 공격 시 상대에게 적용된 Defence 상태효과의
    // 실제 방어 판정을 무시하는 상태이상.
    //
    // Defense 일반스킬 자체는 공격을 직접 방어하지 않고
    // 이동 완료 후 확률적으로 Defence 상태효과를 부여한다.
    Breakthrough,

    // <변경부분> 공격받을 때 기존 Defense 방어 효과를
    // 확정 발동시켜 해당 공격을 무효화하는 상태이상
    Defence
}