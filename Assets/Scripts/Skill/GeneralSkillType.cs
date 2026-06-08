using UnityEngine;

// <변경부분> 모든 기물이 가질 수 있는 일반 스킬 종류
public enum GeneralSkillType
{
    None,          // 일반 스킬 없음
    ChanceAttack   // 찬스어택: 적 처치 시 확률로 한 번 더 행동
}