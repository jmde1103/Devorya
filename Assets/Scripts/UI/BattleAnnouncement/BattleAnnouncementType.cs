// <변경부분> 전투 화면에서 재생할
// 공용 Announcement 연출 종류.
//
// 현재는 BattleStart만 실제 데이터를 사용하고,
// Warning은 이후 Spine 파일 완성 시
// BattleAnnouncementData만 추가해서 사용할 수 있도록 미리 정의한다.
public enum BattleAnnouncementType
{
    BattleStart,
    Warning
}