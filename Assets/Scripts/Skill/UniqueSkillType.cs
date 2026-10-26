public enum UniqueSkillType
{
    [UnityEngine.InspectorName("\uc5c6\uc74c")]
    None,

    // <변경부분> 기존 증식 효과의 이름 변경: 원본 기물과 같은 정보를 가진 기물을 복제
    [UnityEngine.InspectorName("\ubcf5\uc81c")]
    JelluClone,

    // <변경부분> 새 증식 효과: 인접한 빈칸에 젤루 Pawn 생성
    [UnityEngine.InspectorName("\uc99d\uc2dd")]
    JelluMultiply,

    [UnityEngine.InspectorName("\uc655\uc758 \uac01\uc131")]
    KingQueenMove,
    [UnityEngine.InspectorName("\ud569\uc131")]
    JelluSynthesis,

    // <변경부분> 젤루 룩 고유스킬: 진행방향 1칸 앞에 젤루 태그 중립 벽 생성
    [UnityEngine.InspectorName("\uc824\ub8e8 \ubcbd")]
    JelluWall,

    // <변경부분> 젤루 나이트 고유스킬: 자기 자신에게 퇴화 상태이상 부여
    [UnityEngine.InspectorName("\ud1f4\ud654")]
    JelluDegeneration,

    // <변경부분> 액티브 고유스킬: 물/늪 타일 위에서 사용 시 자신에게 돌파 상태 1턴 부여
    [UnityEngine.InspectorName("\ubfd4 \ubc15\uce58\uae30")]
    HornHeadbutt,
    [UnityEngine.InspectorName("\ubd84\uc5f4")]
    JelluSplit,
    [UnityEngine.InspectorName("\ud569\uc131 : \uac15\ud654")]
    JelluReinforce = 9,
    [UnityEngine.InspectorName("방어하기")] GuardSelf = 10,
    [UnityEngine.InspectorName("공간이동")] TeleportSelf = 11,
    [UnityEngine.InspectorName("바이러스")] Virus = 12,
    [UnityEngine.InspectorName("회피하기")] EvadeSelf = 13
}
