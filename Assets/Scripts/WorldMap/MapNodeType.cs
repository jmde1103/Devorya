// 월드맵 노드의 역할을 구분한다.
public enum MapNodeType
{
    // 일반 전투가 진행되는 노드
    Battle,

    // 보스 전투가 진행되는 노드
    BossBattle,

    // 일반 이벤트가 발생하는 노드
    Event,

    // 유적지 전용 이벤트가 발생하는 노드
    RuinsEvent,

    // 이미 클리어한 노드를 표시한다.
    //
    // 맵 시작 지점도 별도의 Start 타입을 만들지 않고
    // 처음부터 이 타입으로 표시한다.
    Cleared,

    // 아이템이나 유물 등을 구매하는 상점 노드
    Shop
}
