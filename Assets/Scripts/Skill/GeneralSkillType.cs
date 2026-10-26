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
    QuickStep = 4,
    [UnityEngine.InspectorName("공격불가 저항 I")]
    AttackDisabledResistanceBasic = 5,
    [UnityEngine.InspectorName("공격불가 저항 II")]
    AttackDisabledResistanceIntermediate = 6,
    [UnityEngine.InspectorName("공격불가 저항 III")]
    AttackDisabledResistanceAdvanced = 7,
    [UnityEngine.InspectorName("행동불가 저항 I")]
    ActionDisabledResistanceBasic = 8,
    [UnityEngine.InspectorName("행동불가 저항 II")]
    ActionDisabledResistanceIntermediate = 9,
    [UnityEngine.InspectorName("행동불가 저항 III")]
    ActionDisabledResistanceAdvanced = 10,
    [UnityEngine.InspectorName("이동불가 저항 I")]
    MoveDisabledResistanceBasic = 11,
    [UnityEngine.InspectorName("이동불가 저항 II")]
    MoveDisabledResistanceIntermediate = 12,
    [UnityEngine.InspectorName("이동불가 저항 III")]
    MoveDisabledResistanceAdvanced = 13,
    [UnityEngine.InspectorName("능력봉인 저항 I")]
    AbilitySealedResistanceBasic = 14,
    [UnityEngine.InspectorName("능력봉인 저항 II")]
    AbilitySealedResistanceIntermediate = 15,
    [UnityEngine.InspectorName("능력봉인 저항 III")]
    AbilitySealedResistanceAdvanced = 16,
    [UnityEngine.InspectorName("회피 자세")]
    EvasionStance = 17,
    [UnityEngine.InspectorName("흡수 방어")]
    AbsorbGuard = 18
}