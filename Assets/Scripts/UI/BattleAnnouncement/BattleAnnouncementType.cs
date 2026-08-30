// <변경부분> 전투 화면에서 재생할
// 공용 Announcement 연출 종류.
//
// 기존 ScriptableObject에 저장된 enum 값을 보호하기 위해
// BattleStart = 0, Warning = 1 값은 변경하지 않는다.
//
// None은 튜토리얼 / 특수 이벤트 전투처럼
// 시작 Announcement가 필요 없는 스테이지에서 사용한다.
public enum BattleAnnouncementType
{
    BattleStart = 0,
    Warning = 1,
    None = 2
}