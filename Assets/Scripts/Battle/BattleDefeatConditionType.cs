// <변경부분> 여러 패배 조건을 동시에 조합할 수 있도록 Flags enum으로 변경
[System.Flags]
public enum BattleDefeatConditionType
{
    // <변경부분> 패배 조건 없음
    None = 0,

    // <변경부분> 해당 진영의 King이 없으면 패배
    KingDeath = 1 << 0,

    // <변경부분> 해당 진영의 모든 기물이 사라지면 패배
    AllPiecesDead = 1 << 1,

    // <변경부분> 해당 진영의 King을 제외한 모든 기물이 사라지면 패배
    AllNonKingPiecesDead = 1 << 2,

    // <변경부분> 해당 진영에 실제 이동/공격 가능한 기물이 하나도 없으면 패배
    NoActionablePieces = 1 << 3
}
