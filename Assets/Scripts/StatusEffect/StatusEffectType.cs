public enum StatusEffectType
{
    [UnityEngine.InspectorName("\uc5c6\uc74c")]
    None,

    // <변경부분> 사망 시 주변 빈칸에 젤루 Pawn을 생성하는 상태이상
    [UnityEngine.InspectorName("\ud1f4\ud654")]
    Degeneration,

    // 공격 시 상대에게 적용된 Defence 상태효과의
    // 실제 방어 판정을 무시하는 상태이상.
    //
    // Defense 일반스킬 자체는 공격을 직접 방어하지 않고
    // 이동 완료 후 확률적으로 Defence 상태효과를 부여한다.
    [UnityEngine.InspectorName("\ub3cc\ud30c")]
    Breakthrough,

    // <변경부분> 공격받을 때 기존 Defense 방어 효과를
    // 확정 발동시켜 해당 공격을 무효화하는 상태이상
    [UnityEngine.InspectorName("\ubc29\uc5b4")]
    Defence,

    [UnityEngine.InspectorName("\uacf5\uaca9\ubd88\uac00")]
    AttackDisabled = 4,
    [UnityEngine.InspectorName("\ud589\ub3d9\ubd88\uac00")]
    ActionDisabled = 5,
    [UnityEngine.InspectorName("\ud638\uc704")]
    Taunt = 6,
    [UnityEngine.InspectorName("\ud68c\ud53c")]
    Evasion = 7
}