public enum GeneralSkillType
{
    None,

    ChanceAttack,

    // <변경부분> 확률적으로 상대 공격/흡수공격을 무효화하는 일반스킬
    Defense,

    // <변경부분> 상대의 Defense / Evasion 같은 방어형 일반스킬 발동을 확률적으로 무효화하는 일반스킬
    Insight
}