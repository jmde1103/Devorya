public enum StatusEffectType
{
    None,

    // <변경부분> 사망 시 주변 빈칸에 젤루 Pawn을 생성하는 상태이상
    Degeneration,

    // <변경부분> 공격 시 상대의 Defense 일반스킬을 무시하는 상태이상
    Breakthrough
}