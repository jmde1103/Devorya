using System.Collections.Generic;
using UnityEngine;

// <변경부분> 런 진행 중 플레이어 기물 1개의 현재 상태를 저장하는 데이터
// 씬 오브젝트를 저장하지 않고, 다음 전투에서 다시 생성할 수 있는 순수 데이터만 저장한다.
[System.Serializable]
public class PlayerPieceRuntimeData
{
    [Header("Piece")]
    // <변경부분> 현재 기물이 참조하는 PieceData
    // 흡수/승급 후 외형, 타입, 고유스킬 복원 기준으로 사용
    public PieceData pieceData;

    // <변경부분> 저장 당시 기물 타입
    public PieceType pieceType;

    // <변경부분> 저장 당시 고유스킬
    public UniqueSkillType uniqueSkill;

    // <변경부분> 저장 당시 이동 가능 여부
    public bool canMove = true;

    // <변경부분> 플레이어가 젤루 계열을 흡수한 뒷면 외형 상태인지 여부
    public bool isAbsorbedPlayerVisual = false;

    [Header("General Skills")]
    // <변경부분> 현재 보유 중인 일반스킬 목록
    public List<GeneralSkillRuntimeData> generalSkills = new List<GeneralSkillRuntimeData>();

    // <변경부분> Piece에서 런타임 저장 데이터를 생성하는 함수
    public static PlayerPieceRuntimeData FromPiece(Piece piece)
    {
        if (piece == null)
        {
            return null;
        }

        PlayerPieceRuntimeData data = new PlayerPieceRuntimeData();

        data.pieceData = piece.CurrentPieceData;
        data.pieceType = piece.PieceType;
        data.uniqueSkill = piece.UniqueSkill;
        data.canMove = piece.CanMove;
        data.isAbsorbedPlayerVisual = piece.IsAbsorbedJelluVisual;

        List<OwnedGeneralSkillData> ownedGeneralSkills = piece.GetGeneralSkills();

        if (ownedGeneralSkills != null)
        {
            for (int i = 0; i < ownedGeneralSkills.Count; i++)
            {
                OwnedGeneralSkillData ownedSkill = ownedGeneralSkills[i];

                if (ownedSkill == null)
                {
                    continue;
                }

                if (ownedSkill.skillType == GeneralSkillType.None)
                {
                    continue;
                }

                data.generalSkills.Add(new GeneralSkillRuntimeData(
                    ownedSkill.skillType,
                    ownedSkill.level
                ));
            }
        }

        return data;
    }

    // <변경부분> 보상으로 새 데보리아 기물을 획득할 때 PieceData 기준으로 런타임 데이터를 생성하는 함수
    public static PlayerPieceRuntimeData CreateFromPieceData(PieceData pieceData, bool isAbsorbedPlayerVisual = false)
    {
        if (pieceData == null)
        {
            return null;
        }

        PlayerPieceRuntimeData data = new PlayerPieceRuntimeData();

        data.pieceData = pieceData;
        data.pieceType = pieceData.pieceType;
        data.uniqueSkill = pieceData.uniqueSkill;
        data.canMove = pieceData.canMove;
        data.isAbsorbedPlayerVisual = isAbsorbedPlayerVisual;

        if (pieceData.defaultGeneralSkills != null)
        {
            for (int i = 0; i < pieceData.defaultGeneralSkills.Length; i++)
            {
                OwnedGeneralSkillData defaultSkill = pieceData.defaultGeneralSkills[i];

                if (defaultSkill == null)
                {
                    continue;
                }

                if (defaultSkill.skillType == GeneralSkillType.None)
                {
                    continue;
                }

                data.generalSkills.Add(new GeneralSkillRuntimeData(
                    defaultSkill.skillType,
                    defaultSkill.level
                ));
            }
        }

        return data;
    }

    // <변경부분> 외부에서 원본 리스트를 직접 수정하지 못하도록 복사본 생성
    public PlayerPieceRuntimeData Clone()
    {
        PlayerPieceRuntimeData copiedData = new PlayerPieceRuntimeData();

        copiedData.pieceData = pieceData;
        copiedData.pieceType = pieceType;
        copiedData.uniqueSkill = uniqueSkill;
        copiedData.canMove = canMove;
        copiedData.isAbsorbedPlayerVisual = isAbsorbedPlayerVisual;

        if (generalSkills != null)
        {
            for (int i = 0; i < generalSkills.Count; i++)
            {
                GeneralSkillRuntimeData skillData = generalSkills[i];

                if (skillData == null)
                {
                    continue;
                }

                copiedData.generalSkills.Add(skillData.Clone());
            }
        }

        return copiedData;
    }
}

// <변경부분> 런 진행 중 저장할 일반스킬 1개의 상태
[System.Serializable]
public class GeneralSkillRuntimeData
{
    public GeneralSkillType skillType = GeneralSkillType.None;
    public int level = 1;

    public GeneralSkillRuntimeData()
    {
    }

    public GeneralSkillRuntimeData(GeneralSkillType skillType, int level)
    {
        this.skillType = skillType;
        this.level = Mathf.Max(1, level);
    }

    public GeneralSkillRuntimeData Clone()
    {
        return new GeneralSkillRuntimeData(skillType, level);
    }
}
