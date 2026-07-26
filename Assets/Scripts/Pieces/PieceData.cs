using UnityEngine;

// <변경부분> 기물 1종의 기본 원형 데이터를 관리하는 ScriptableObject
// 적/플레이어/흡수 외형, 기본 고유스킬, 종족 태그, 기본 일반스킬을 한곳에서 관리한다.
[CreateAssetMenu(fileName = "PieceData", menuName = "Devorya/Piece/Piece Data")]
public class PieceData : ScriptableObject
{
    [Header("Basic")]
    // <변경부분> PieceDatabase에서 정확히 이 기물 데이터를 찾기 위한 고유 ID
    // 예: DevoryaPawn, JelluPawn, JelluRook, NeutralJelluWall
    public string pieceId;

    // <변경부분> 이 데이터가 의미하는 기물 타입
    public PieceType pieceType;

    // <변경부분> 기본 고유스킬
    public UniqueSkillType uniqueSkill = UniqueSkillType.None;

    // <변경부분> 기본 이동 가능 여부
    public bool canMove = true;

    [Header("Species")]
    // <변경부분> 이 기물이 기본으로 가지는 종족 태그
    public PieceSpeciesTag[] speciesTags;

    [Header("General Skills")]
    // <변경부분> 이 기물이 기본으로 가지고 시작할 일반스킬 목록
    public OwnedGeneralSkillData[] defaultGeneralSkills;

    [Header("Sprites")]
    // <변경부분> 플레이어 진영에서 사용할 스프라이트
    public Sprite playerSprite;

    // <변경부분> 적 진영에서 사용할 스프라이트
    public Sprite enemySprite;

    // <변경부분> 중립 진영에서 사용할 스프라이트
    public Sprite neutralSprite;

    // <변경부분> 플레이어가 Jellu 계열을 흡수했을 때 사용할 뒤통수 스프라이트
    public Sprite absorbedPlayerBackSprite;

    [Header("Spine Visual Prefabs")]
    // <변경부분> 플레이어 진영에서 사용할 Spine Visual 프리팹
    public GameObject playerSpineVisualPrefab;

    // <변경부분> 적 진영에서 사용할 Spine Visual 프리팹
    public GameObject enemySpineVisualPrefab;

    // <변경부분> 중립 진영에서 사용할 Spine Visual 프리팹
    public GameObject neutralSpineVisualPrefab;

    // <변경부분> 플레이어가 Jellu 계열을 흡수했을 때 사용할 뒤통수 Spine Visual 프리팹
    public GameObject absorbedPlayerBackSpineVisualPrefab;

    [Header("Status UI Sprites")]
    // <변경부분> 플레이어 상태 UI에서 사용할 스프라이트
    public Sprite playerStatusSprite;

    // <변경부분> 적 상태 UI에서 사용할 스프라이트
    public Sprite enemyStatusSprite;

    // <변경부분> 중립 상태 UI에서 사용할 스프라이트
    public Sprite neutralStatusSprite;

    // <변경부분> 흡수된 플레이어 상태 UI에서 사용할 스프라이트
    public Sprite absorbedPlayerStatusSprite;

    [Header("Type Icon Positions")]
    // <변경부분> 플레이어 진영 타입 아이콘 위치
    public Vector3 playerTypeIconPosition;

    // <변경부분> 적 진영 타입 아이콘 위치
    public Vector3 enemyTypeIconPosition;

    // <변경부분> 중립 진영 타입 아이콘 위치
    public Vector3 neutralTypeIconPosition;

    // <변경부분> 흡수된 플레이어 외형 타입 아이콘 위치
    public Vector3 absorbedPlayerTypeIconPosition;

    [Header("Field Status Effect Icon Positions")]
    // <변경부분> 플레이어 진영 필드 상태효과 아이콘 위치
    public Vector3 playerFieldStatusEffectPosition;

    // <변경부분> 적 진영 필드 상태효과 아이콘 위치
    public Vector3 enemyFieldStatusEffectPosition;

    // <변경부분> 중립 진영 필드 상태효과 아이콘 위치
    public Vector3 neutralFieldStatusEffectPosition;

    // <변경부분> 흡수된 플레이어 외형 필드 상태효과 아이콘 위치
    public Vector3 absorbedPlayerFieldStatusEffectPosition;

    // <변경부분> 팀과 외형 상태에 맞는 스프라이트 반환
    public Sprite GetSprite(PieceTeam team, bool isAbsorbedPlayerVisual)
    {
        if (team == PieceTeam.Player && isAbsorbedPlayerVisual && absorbedPlayerBackSprite != null)
        {
            return absorbedPlayerBackSprite;
        }

        switch (team)
        {
            case PieceTeam.Player:
                return playerSprite;

            case PieceTeam.Enemy:
                return enemySprite;

            case PieceTeam.Neutral:
                return neutralSprite;
        }

        return null;
    }

    // <변경부분> 팀과 외형 상태에 맞는 Spine Visual 프리팹 반환
    public GameObject GetSpineVisualPrefab(PieceTeam team, bool isAbsorbedPlayerVisual)
    {
        if (team == PieceTeam.Player &&
            isAbsorbedPlayerVisual &&
            absorbedPlayerBackSpineVisualPrefab != null)
        {
            return absorbedPlayerBackSpineVisualPrefab;
        }

        switch (team)
        {
            case PieceTeam.Player:
                return playerSpineVisualPrefab;

            case PieceTeam.Enemy:
                return enemySpineVisualPrefab;

            case PieceTeam.Neutral:
                return neutralSpineVisualPrefab;
        }

        return null;
    }

    // <변경부분> 팀과 외형 상태에 맞는 상태 UI 스프라이트 반환
    public Sprite GetStatusSprite(PieceTeam team, bool isAbsorbedPlayerVisual)
    {
        if (team == PieceTeam.Player && isAbsorbedPlayerVisual && absorbedPlayerStatusSprite != null)
        {
            return absorbedPlayerStatusSprite;
        }

        switch (team)
        {
            case PieceTeam.Player:
                return playerStatusSprite;

            case PieceTeam.Enemy:
                return enemyStatusSprite;

            case PieceTeam.Neutral:
                return neutralStatusSprite;
        }

        return null;
    }

    // <변경부분> 팀과 외형 상태에 맞는 타입 아이콘 위치 반환
    public Vector3 GetTypeIconPosition(PieceTeam team, bool isAbsorbedPlayerVisual)
    {
        if (team == PieceTeam.Player && isAbsorbedPlayerVisual)
        {
            return absorbedPlayerTypeIconPosition;
        }

        switch (team)
        {
            case PieceTeam.Player:
                return playerTypeIconPosition;

            case PieceTeam.Enemy:
                return enemyTypeIconPosition;

            case PieceTeam.Neutral:
                return neutralTypeIconPosition;
        }

        return Vector3.zero;
    }

    // <변경부분> 팀과 외형 상태에 맞는
    // 필드 상태효과 아이콘 위치를 반환한다.
    // 타입 아이콘 위치와 동일한 기준으로 관리한다.
    public Vector3 GetFieldStatusEffectPosition(
        PieceTeam team,
        bool isAbsorbedPlayerVisual)
    {
        // 플레이어가 흡수 외형을 사용 중이면
        // 흡수 외형 전용 상태효과 아이콘 위치를 반환한다.
        if (team == PieceTeam.Player &&
            isAbsorbedPlayerVisual)
        {
            return absorbedPlayerFieldStatusEffectPosition;
        }

        switch (team)
        {
            case PieceTeam.Player:
                return playerFieldStatusEffectPosition;

            case PieceTeam.Enemy:
                return enemyFieldStatusEffectPosition;

            case PieceTeam.Neutral:
                return neutralFieldStatusEffectPosition;
        }

        return Vector3.zero;
    }
}
