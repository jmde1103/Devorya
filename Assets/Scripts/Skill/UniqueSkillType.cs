public enum UniqueSkillType
{
    None,

    // <변경부분> 기존 증식 효과의 이름 변경: 원본 기물과 같은 정보를 가진 기물을 복제
    JelluClone,

    // <변경부분> 새 증식 효과: 인접한 빈칸에 젤루 Pawn 생성
    JelluMultiply,

    KingQueenMove,
    JelluSynthesis,

    // <변경부분> 젤루 룩 고유스킬: 진행방향 1칸 앞에 젤루 태그 중립 벽 생성
    JelluWall,

    // <변경부분> 젤루 나이트 고유스킬: 자기 자신에게 퇴화 상태이상 부여
    JelluDegeneration,

    // <변경부분> 액티브 고유스킬: 물/늪 타일 위에서 사용 시 자신에게 돌파 상태 1턴 부여
    HornHeadbutt
}
