// <변경부분> TextCutsceneScene을 여러 컷씬에서 재사용하기 위한
// 씬 전환용 임시 런타임 데이터 저장소.
//
// TextCutsceneController Inspector에는
// 기본 컷씬 데이터(Prologue_Boot)를 연결해두고,
// 다른 컷씬을 실행할 때만 PendingCutsceneData를 등록한다.
public static class TextCutsceneRuntimeState
{
    // <변경부분> 다음 TextCutsceneScene에서
    // 한 번만 사용할 컷씬 데이터.
    private static TextCutsceneData pendingCutsceneData;

    // <변경부분> 현재 전달 대기 중인
    // 컷씬 데이터를 읽기 전용으로 반환한다.
    public static TextCutsceneData PendingCutsceneData
    {
        get
        {
            return pendingCutsceneData;
        }
    }

    // <변경부분> 다음 TextCutsceneScene에서 실행할
    // 컷씬 데이터를 등록한다.
    public static void SetPendingCutsceneData(
        TextCutsceneData cutsceneData)
    {
        pendingCutsceneData =
            cutsceneData;
    }

    // <변경부분> 전달된 컷씬 데이터를 한 번 가져온 뒤
    // 즉시 Pending 상태에서 제거한다.
    //
    // 이후 TextCutsceneScene에 다시 들어왔을 때
    // 이전 컷씬이 실수로 반복 실행되는 것을 방지한다.
    public static TextCutsceneData ConsumePendingCutsceneData()
    {
        TextCutsceneData result =
            pendingCutsceneData;

        pendingCutsceneData =
            null;

        return result;
    }

    // <변경부분> 새 게임 시작이나 런 초기화 등에서
    // 남아 있는 컷씬 전달 데이터를 강제로 제거할 때 사용한다.
    public static void Clear()
    {
        pendingCutsceneData =
            null;
    }
}
