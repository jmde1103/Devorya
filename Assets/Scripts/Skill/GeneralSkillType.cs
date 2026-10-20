public enum GeneralSkillType
{
    [UnityEngine.InspectorName("\uc5c6\uc74c")]
    None,

    [UnityEngine.InspectorName("\ucc2c\uc2a4\uc5b4\ud0dd")]
    ChanceAttack,

    // <변경부분> 확률적으로 상대 공격/흡수공격을 무효화하는 일반스킬
    [UnityEngine.InspectorName("\ubc29\uc5b4 \uc790\uc138")]
    Defense,

    // <변경부분> 상대의 Defense / Evasion 같은 방어형 일반스킬 발동을 확률적으로 무효화하는 일반스킬
    [UnityEngine.InspectorName("\uac04\ud30c")]
    Insight,
    [UnityEngine.InspectorName("\ube60\ub978 \ubc1c\uac78\uc74c")]
    QuickStep = 4
}