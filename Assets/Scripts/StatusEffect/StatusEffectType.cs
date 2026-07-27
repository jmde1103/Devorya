public enum StatusEffectType
{
    None,

    // <변경부분> 사망 시 주변 빈칸에 젤루 Pawn을 생성하는 상태이상
    Degeneration,

    // <변경부분> 공격 시 상대의 Defense 일반스킬과
    // Defence 상태효과를 무시하는 상태이상
    Breakthrough,

    // <변경부분> 공격받을 때 기존 Defense 방어 효과를
    // 확정 발동시켜 해당 공격을 무효화하는 상태이상
    Defence
}