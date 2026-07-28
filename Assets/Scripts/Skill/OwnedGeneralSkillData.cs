// <변경부분> 기물이 실제로 보유한 일반스킬 종류만 저장하는 런타임 데이터
// 일반스킬 레벨 시스템은 제거되었으며 동일 스킬은 중복 보유하지 않는다.
[System.Serializable]
public class OwnedGeneralSkillData
{
    // 보유한 일반스킬 종류
    public GeneralSkillType skillType = GeneralSkillType.None;

    // 일반스킬 보유 데이터 생성
    public OwnedGeneralSkillData(
        GeneralSkillType skillType)
    {
        this.skillType =
            skillType;
    }

    // 기존 보유 일반스킬 데이터를 복사한다.
    // 행동 시작 전에 스킬 보유 여부를 저장할 때 사용한다.
    public OwnedGeneralSkillData(
        OwnedGeneralSkillData original)
    {
        skillType =
            original != null
                ? original.skillType
                : GeneralSkillType.None;
    }
}
