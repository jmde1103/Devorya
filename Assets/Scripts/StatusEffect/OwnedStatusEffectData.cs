using System;

// <변경부분> 전투 중 기물이 실제로 보유 중인 상태이상 정보
[Serializable]
public class OwnedStatusEffectData
{
    // 상태이상 종류
    public StatusEffectType effectType;

    // 남은 유지 턴
    public int remainingTurn;

    // 현재 중첩 수
    public int stackCount;

    public OwnedStatusEffectData(StatusEffectType effectType, int remainingTurn, int stackCount)
    {
        this.effectType = effectType;
        this.remainingTurn = remainingTurn;
        this.stackCount = stackCount;
    }

    // <변경부분> 외부에서 안전하게 복사본을 사용할 수 있도록 복제 함수 제공
    public OwnedStatusEffectData Clone()
    {
        return new OwnedStatusEffectData(effectType, remainingTurn, stackCount);
    }
}
