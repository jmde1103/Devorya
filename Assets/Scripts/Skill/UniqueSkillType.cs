using UnityEngine;

public enum UniqueSkillType
{
    None,

    // <변경부분> Jellu 폰 전용 고유 스킬: 인접 빈칸에 자신을 복제
    JelluMultiply,

    // <변경부분> King 전용 고유스킬: 이번 턴 동안 이동/공격 판정만 Queen처럼 변경
    KingQueenMove
}
