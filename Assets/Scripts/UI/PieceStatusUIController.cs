using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using static UnityEngine.Audio.ProcessorInstance;

// <변경부분> 선택한 필드 기물의 현재 정보를 그대로 UI에 표시하는 컨트롤러
public class PieceStatusUIController : MonoBehaviour
{
    [Header("Piece Image")]
    [SerializeField] private Image pieceImage;

    [Header("Piece Type Icon")]
    [SerializeField] private Image pieceTypeIconImage;

    [Header("General Skill Slots")]
    [SerializeField] private TMP_Text[] generalSkillTexts;

    // <변경부분> 일반스킬 슬롯에 표시할 아이콘 이미지 배열
    [SerializeField] private Image[] generalSkillIconImages;

    // <변경부분> 일반스킬 슬롯에 연결된 TooltipTrigger 배열
    [SerializeField] private TooltipTrigger[] generalSkillTooltipTriggers;

    // <변경부분> 일반스킬 아이콘/설명을 찾기 위한 데이터베이스
    [SerializeField] private GeneralSkillDatabase generalSkillDatabase;


    [Header("Unique Skill Slot")]
    // <변경부분> 이 스테이터스 UI에서 고유스킬 아이콘을 표시할지 여부.
    //
    // Enemy Status에서는 true,
    // Player Status에서는 false로 사용한다.
    //
    // 같은 PieceStatusUIController를 양쪽이 공용으로 사용하더라도
    // 플레이어 UI에는 불필요한 고유스킬 아이콘이 나타나지 않게 한다.
    [SerializeField]
    private bool showUniqueSkillSlot =
        false;

    // <변경부분> Enemy 고유스킬 아이콘 전체를 감싸는 슬롯 Root.
    //
    // 고유스킬이 없을 때는 Root 전체를 비활성화하여
    // 보이지 않는 Tooltip Raycast 영역이 남지 않게 한다.
    [SerializeField]
    private GameObject uniqueSkillSlotRoot;

    // <변경부분> 현재 선택한 Enemy의 고유스킬 아이콘을 표시할 Image.
    [SerializeField]
    private Image uniqueSkillIconImage;

    // <변경부분> 고유스킬 아이콘에 연결된 TooltipTrigger.
    //
    // PC:
    // Hover -> Tooltip
    //
    // Mobile:
    // Long Press -> Tooltip
    [SerializeField]
    private TooltipTrigger uniqueSkillTooltipTrigger;

    // <변경부분> UniqueSkillType에 맞는
    // 이름 / 아이콘 / 설명 / Tooltip Section을 찾기 위한 데이터베이스.
    [SerializeField]
    private UniqueSkillDatabase uniqueSkillDatabase;


    [Header("Status Effect Slots")]
    // <변경부분> 상태이상 아이콘/이름/설명을 찾기 위한 데이터베이스
    [SerializeField] private StatusEffectDatabase statusEffectDatabase;

    // <변경부분> 스테이터스 창에 표시할 상태이상 슬롯 배열
    [SerializeField] private StatusEffectSlotUI[] statusEffectSlots;

    [Header("Root")]
    [SerializeField] private GameObject statusRoot;

    [Header("Open Animation")]
    // <변경부분> 스테이터스 창이 갱신될 때 지지직 오픈 애니메이션을 재생하는 컴포넌트
    [SerializeField] private PopupOpenAnimator popupOpenAnimator;

    // <변경부분> 기물을 클릭해 스테이터스 정보가 갱신될 때마다 오픈 애니메이션을 다시 재생할지 여부
    [SerializeField] private bool playOpenAnimationOnRefresh = true;


        // <변경부분> 현재 Status UI가 표시하고 있는 기물을 보관한다.
    //
    // Locale이 변경되었을 때 현재 기물의 General Skill Tooltip을
    // 새로운 언어 기준으로 다시 생성하기 위해 사용한다.
    private Piece currentSelectedPiece;

    // <변경부분> 이 Status UI가 활성화되어 있는 동안
    // Unity Localization의 Locale 변경 이벤트를 구독한다.
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged +=
            OnSelectedLocaleChanged;
    }

    // <변경부분> Status UI가 비활성화되거나 제거될 때
    // Locale 변경 이벤트 구독을 반드시 해제한다.
    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -=
            OnSelectedLocaleChanged;
    }

    // <변경부분> 게임 실행 중 Locale이 변경되면
    // 현재 선택 기물의 Localization 영향을 받는 Tooltip 데이터를 다시 생성한다.
    //
    // 전체 Refresh()를 호출하지 않으므로
    // Status 창 Open Animation은 다시 재생되지 않는다.
    private void OnSelectedLocaleChanged(
        Locale locale)
    {
        if (currentSelectedPiece == null)
        {
            return;
        }

        // 일반스킬 Tooltip 갱신
        SetGeneralSkillSlots(
            currentSelectedPiece.GetGeneralSkills()
        );

        // <변경부분> Enemy Status처럼
        // 고유스킬 슬롯을 사용하는 UI에서는
        // Unique Skill Tooltip도 현재 Locale 기준으로 다시 생성한다.
        if (showUniqueSkillSlot)
        {
            SetUniqueSkillSlot(
                currentSelectedPiece
            );
        }
    }

    private void Start()
    {
        // <변경부분> PopupOpenAnimator가 연결되지 않았다면 자동으로 탐색
        AutoBindPopupOpenAnimator();
    }

    // <변경부분> 선택한 필드 기물의 현재 정보를
    // 스테이터스 UI에 갱신한다.
    public void Refresh(
    Piece selectedPiece)
    {
        // <변경부분> 현재 Status UI가 표시하는 기물을 저장한다.
        //
        // 이후 Locale 변경 시 이 기물의 General Skill Tooltip을
        // 새로운 언어 기준으로 다시 생성한다.
        currentSelectedPiece =
            selectedPiece;

        // 선택한 기물이 없으면
        // 기존 스테이터스 내용을 모두 비우고 숨긴다.
        if (selectedPiece == null)
        {
            Clear();
            return;
        }

        // 선택한 기물이 있으면 스테이터스 창 표시
        if (statusRoot != null)
        {
            statusRoot.SetActive(
                true
            );
        }

        // 현재 기물 외형 표시
        SetPieceImageFromSelectedPiece(
            selectedPiece
        );

        // 현재 기물 타입 아이콘 표시
        SetPieceTypeIconFromSelectedPiece(
            selectedPiece
        );

        // 기물이 보유한 일반스킬 표시
        SetGeneralSkillSlots(
            selectedPiece.GetGeneralSkills()
        );

        // <변경부분> Enemy Status에서 사용할
        // 고유스킬 아이콘과 Tooltip을 갱신한다.
        //
        // showUniqueSkillSlot이 false인 Player Status에서는
        // 자동으로 비활성화 상태가 유지된다.
        SetUniqueSkillSlot(
            selectedPiece
        );

        // 현재 기물이 보유한 상태이상 표시
        SetStatusEffectSlots(
            selectedPiece
        );

        // 모든 스테이터스 정보 갱신이 끝난 뒤
        // 기존 오픈 애니메이션을 재생한다.
        PlayStatusOpenAnimation();
    }

    // <변경부분> 선택한 필드 기물의 현재 SpriteRenderer 이미지를 UI에 복사하는 함수
    private void SetPieceImageFromSelectedPiece(Piece selectedPiece)
    {
        if (pieceImage == null)
        {
            return;
        }

        // <변경부분> 필드 이미지가 아니라 PieceManager가 넣어준 스테이터스 UI용 이미지를 가져옴
        Sprite statusSprite = selectedPiece.GetStatusUISprite();

        if (statusSprite == null)
        {
            pieceImage.sprite = null;
            pieceImage.enabled = false;
            return;
        }

        // <변경부분> 필드 스프라이트가 아니라 UI용 앞면 스프라이트를 표시
        pieceImage.sprite = statusSprite;
        pieceImage.enabled = true;
        pieceImage.preserveAspect = true;
    }

    // <변경부분> 선택한 필드 기물의 현재 타입 아이콘을 UI에 복사하는 함수
    private void SetPieceTypeIconFromSelectedPiece(Piece selectedPiece)
    {
        if (pieceTypeIconImage == null)
        {
            return;
        }

        // Piece가 현재 들고 있는 타입 아이콘 스프라이트를 가져옴
        Sprite currentTypeIconSprite = selectedPiece.GetCurrentTypeIconSprite();

        // 타입 아이콘이 없으면 숨김
        if (currentTypeIconSprite == null)
        {
            pieceTypeIconImage.sprite = null;
            pieceTypeIconImage.enabled = false;
            return;
        }

        // 현재 필드 기물에 연결된 타입 아이콘을 UI에 그대로 표시
        pieceTypeIconImage.sprite = currentTypeIconSprite;
        pieceTypeIconImage.enabled = true;
        pieceTypeIconImage.preserveAspect = true;
    }
    // <변경부분> 일반스킬 슬롯에 아이콘과 Tooltip을 표시하는 함수
    // 일반스킬 레벨 시스템이 제거되었으므로 레벨 텍스트는 표시하지 않는다.
    private void SetGeneralSkillSlots(
        List<OwnedGeneralSkillData> generalSkills)
    {
        // 모든 일반스킬 슬롯을 먼저 빈 상태로 초기화
        ClearGeneralSkillSlots();

        // 일반스킬이 없으면 종료
        if (generalSkills == null)
        {
            return;
        }

        int textSlotCount =
            generalSkillTexts != null
                ? generalSkillTexts.Length
                : 0;

        int iconSlotCount =
            generalSkillIconImages != null
                ? generalSkillIconImages.Length
                : 0;

        int tooltipSlotCount =
            generalSkillTooltipTriggers != null
                ? generalSkillTooltipTriggers.Length
                : 0;

        int maxSlotCount =
            Mathf.Max(
                textSlotCount,
                iconSlotCount,
                tooltipSlotCount
            );

        int displayCount =
            Mathf.Min(
                generalSkills.Count,
                maxSlotCount,
                6
            );

        for (int i = 0;
             i < displayCount;
             i++)
        {
            OwnedGeneralSkillData ownedSkillData =
                generalSkills[i];

            if (ownedSkillData == null ||
                ownedSkillData.skillType ==
                GeneralSkillType.None)
            {
                continue;
            }

            // 일반스킬 타입에 맞는 표시 데이터 가져오기
            GeneralSkillData skillData =
                GetGeneralSkillData(
                    ownedSkillData.skillType
                );

            // <변경부분> 일반스킬 레벨 없이
            // 고정 확률 설명으로 Tooltip을 구성한다.
            if (generalSkillTooltipTriggers != null &&
                i < generalSkillTooltipTriggers.Length &&
                generalSkillTooltipTriggers[i] != null)
            {
                generalSkillTooltipTriggers[i]
                    .SetTooltipViewData(
                        TooltipViewData
                            .FromGeneralSkillData(
                                skillData
                            )
                    );
            }

            // 일반스킬 아이콘 표시
            if (generalSkillIconImages != null &&
                i < generalSkillIconImages.Length &&
                generalSkillIconImages[i] != null)
            {
                Sprite skillIconSprite =
                    skillData != null
                        ? skillData.iconSprite
                        : null;

                generalSkillIconImages[i].sprite =
                    skillIconSprite;

                generalSkillIconImages[i].enabled =
                    skillIconSprite != null;

                generalSkillIconImages[i].preserveAspect =
                    true;
            }

            // <변경부분> 일반스킬 레벨 텍스트는 사용하지 않는다.
            // 기존 Text 오브젝트가 연결돼 있어도 빈 문자열로 유지한다.
            if (generalSkillTexts != null &&
                i < generalSkillTexts.Length &&
                generalSkillTexts[i] != null)
            {
                generalSkillTexts[i].text =
                    string.Empty;
            }
        }
    }

    // <변경부분> 일반스킬 슬롯 아이콘과 텍스트를 모두 비우는 함수
    private void ClearGeneralSkillSlots()
    {
        // 일반스킬 레벨 텍스트 초기화
        if (generalSkillTexts != null)
        {
            for (int i = 0; i < generalSkillTexts.Length; i++)
            {
                if (generalSkillTexts[i] != null)
                {
                    generalSkillTexts[i].text = "";
                }
            }
        }

        // 일반스킬 아이콘 초기화
        if (generalSkillIconImages != null)
        {
            for (int i = 0; i < generalSkillIconImages.Length; i++)
            {
                if (generalSkillIconImages[i] != null)
                {
                    generalSkillIconImages[i].sprite = null;
                    generalSkillIconImages[i].enabled = false;
                }
            }
        }

        // <변경부분> 일반스킬 Tooltip 데이터 초기화
        if (generalSkillTooltipTriggers != null)
        {
            for (int i = 0; i < generalSkillTooltipTriggers.Length; i++)
            {
                if (generalSkillTooltipTriggers[i] != null)
                {
                    generalSkillTooltipTriggers[i].SetTooltipViewData(null);
                }
            }
        }
    }

    // <변경부분> 현재 선택한 기물이 보유한 고유스킬을
    // Enemy Status 전용 아이콘과 Tooltip에 표시한다.
    private void SetUniqueSkillSlot(
        Piece selectedPiece)
    {
        // 이전 Enemy의 아이콘/Tooltip 데이터가
        // 다음 Enemy에게 남지 않도록 먼저 초기화한다.
        ClearUniqueSkillSlot();

        // Player Status처럼
        // 이 기능을 사용하지 않는 스테이터스 UI에서는 종료한다.
        if (showUniqueSkillSlot == false)
        {
            return;
        }

        if (selectedPiece == null ||
            selectedPiece.UniqueSkill ==
            UniqueSkillType.None)
        {
            return;
        }

        // UniqueSkillData 없이는
        // 아이콘과 Tooltip 설명을 구성할 수 없다.
        if (uniqueSkillDatabase == null)
        {
            Debug.LogWarning(
                "PieceStatusUIController에 " +
                "UniqueSkillDatabase가 연결되지 않았습니다."
            );

            return;
        }

        UniqueSkillData skillData =
            uniqueSkillDatabase.GetData(
                selectedPiece.UniqueSkill
            );

        if (skillData == null)
        {
            Debug.LogWarning(
                $"고유스킬 데이터를 찾지 못했습니다: " +
                $"{selectedPiece.UniqueSkill}"
            );

            return;
        }

        // 표시할 실제 아이콘이 없다면
        // 빈 슬롯을 노출하지 않는다.
        if (skillData.iconSprite == null)
        {
            Debug.LogWarning(
                $"고유스킬 아이콘이 없습니다: " +
                $"{selectedPiece.UniqueSkill}"
            );

            return;
        }

        // <변경부분> 모든 데이터 검사가 끝난 뒤
        // 실제 고유스킬 슬롯을 활성화한다.
        if (uniqueSkillSlotRoot != null)
        {
            uniqueSkillSlotRoot.SetActive(
                true
            );
        }

        if (uniqueSkillIconImage != null)
        {
            uniqueSkillIconImage.sprite =
                skillData.iconSprite;

            uniqueSkillIconImage.enabled =
                true;

            uniqueSkillIconImage.preserveAspect =
                true;
        }

        // <변경부분> 기존 UniqueSkillData를 그대로 사용해
        // 고유스킬 이름 / 설명 / Section Tooltip을 연결한다.
        //
        // 최신 TooltipTrigger가 플랫폼을 구분하므로:
        // PC = Hover
        // Mobile = Long Press
        // 로 자동 동작한다.
        if (uniqueSkillTooltipTrigger != null)
        {
            uniqueSkillTooltipTrigger
                .SetTooltipViewData(
                    TooltipViewData
                        .FromUniqueSkillData(
                            skillData
                        )
                );
        }
    }


    // <변경부분> Enemy 고유스킬 슬롯의
    // 이전 표시 정보와 Tooltip 데이터를 완전히 초기화한다.
    private void ClearUniqueSkillSlot()
    {
        // 이전 Enemy Tooltip 데이터 제거
        if (uniqueSkillTooltipTrigger != null)
        {
            uniqueSkillTooltipTrigger
                .SetTooltipViewData(
                    null
                );
        }

        // 이전 Enemy 아이콘 제거
        if (uniqueSkillIconImage != null)
        {
            uniqueSkillIconImage.sprite =
                null;

            uniqueSkillIconImage.enabled =
                false;
        }

        // 고유스킬이 없거나
        // Player Status처럼 사용하지 않는 경우
        // 슬롯 Root 자체를 숨긴다.
        if (uniqueSkillSlotRoot != null)
        {
            uniqueSkillSlotRoot.SetActive(
                false
            );
        }
    }

    // <변경부분> 상태이상 슬롯에 아이콘/남은 턴/중첩을 표시하는 함수
    private void SetStatusEffectSlots(Piece selectedPiece)
    {
        // 먼저 모든 상태이상 슬롯을 비움
        ClearStatusEffectSlots();

        // 선택 기물이 없으면 종료
        if (selectedPiece == null)
        {
            return;
        }

        // 상태이상 슬롯 배열이 없으면 종료
        if (statusEffectSlots == null || statusEffectSlots.Length == 0)
        {
            return;
        }

        // 상태이상 데이터베이스가 없으면 표시 데이터 검색 불가
        if (statusEffectDatabase == null)
        {
            Debug.LogWarning("PieceStatusUIController에 StatusEffectDatabase가 연결되지 않았습니다.");
            return;
        }

        // 선택 기물이 현재 보유한 상태이상 목록 가져오기
        List<OwnedStatusEffectData> ownedStatusEffects = selectedPiece.GetStatusEffectsCopy();

        if (ownedStatusEffects == null)
        {
            return;
        }

        int displayCount = Mathf.Min(ownedStatusEffects.Count, statusEffectSlots.Length);

        for (int i = 0; i < displayCount; i++)
        {
            OwnedStatusEffectData ownedStatusEffect = ownedStatusEffects[i];

            if (ownedStatusEffect == null || ownedStatusEffect.effectType == StatusEffectType.None)
            {
                continue;
            }

            // 상태이상 타입에 맞는 데이터 검색
            StatusEffectData statusEffectData = statusEffectDatabase.GetData(ownedStatusEffect.effectType);

            if (statusEffectSlots[i] != null)
            {
                statusEffectSlots[i].Refresh(statusEffectData, ownedStatusEffect);
            }
        }
    }

    // <변경부분> 상태이상 슬롯을 모두 빈 상태로 초기화하는 함수
    private void ClearStatusEffectSlots()
    {
        if (statusEffectSlots == null)
        {
            return;
        }

        for (int i = 0; i < statusEffectSlots.Length; i++)
        {
            if (statusEffectSlots[i] != null)
            {
                statusEffectSlots[i].Clear();
            }
        }
    }

    // <변경부분> 일반스킬 타입에 맞는 GeneralSkillData를 Database에서 찾는 함수
    private GeneralSkillData GetGeneralSkillData(GeneralSkillType skillType)
    {
        if (generalSkillDatabase == null)
        {
            return null;
        }

        return generalSkillDatabase.GetData(skillType);
    }

    // <변경부분> 스테이터스 창에 연결된 PopupOpenAnimator를 자동으로 찾는 함수
    private void AutoBindPopupOpenAnimator()
    {
        if (popupOpenAnimator != null)
        {
            return;
        }

        // statusRoot에 PopupOpenAnimator가 붙어 있으면 우선 사용
        if (statusRoot != null)
        {
            popupOpenAnimator = statusRoot.GetComponent<PopupOpenAnimator>();
        }

        // statusRoot에 없으면 PieceStatusUIController가 붙은 현재 오브젝트에서 찾음
        if (popupOpenAnimator == null)
        {
            popupOpenAnimator = GetComponent<PopupOpenAnimator>();
        }

        // 자식 오브젝트까지 fallback 탐색
        if (popupOpenAnimator == null)
        {
            popupOpenAnimator = GetComponentInChildren<PopupOpenAnimator>(true);
        }
    }


    // <변경부분> 기물 정보 타입 아이콘 위치에서 검은 픽셀 파티클 재생

    // <변경부분> 스테이터스 창 갱신 시 오픈 애니메이션을 다시 재생하는 함수
    private void PlayStatusOpenAnimation()
    {
        if (playOpenAnimationOnRefresh == false)
        {
            return;
        }

        AutoBindPopupOpenAnimator();

        if (popupOpenAnimator == null)
        {
            Debug.LogWarning("PieceStatusUIController에 PopupOpenAnimator가 연결되지 않았습니다.");
            return;
        }

        popupOpenAnimator.PlayOpen();
    }

    public void Clear()
    {
        // <변경부분> 더 이상 표시 중인 기물이 없으므로
        // Locale 변경용 현재 선택 기물 참조도 함께 제거한다.
        currentSelectedPiece =
            null;

        // 선택한 기물이 없으면 스테이터스 창 숨김
        if (statusRoot != null)
        {
            statusRoot.SetActive(
                false
            );
        }

        if (pieceImage != null)
        {
            pieceImage.sprite =
                null;

            pieceImage.enabled =
                false;
        }

        if (pieceTypeIconImage != null)
        {
            pieceTypeIconImage.sprite =
                null;

            pieceTypeIconImage.enabled =
                false;
        }

        // 일반스킬 슬롯 초기화
        ClearGeneralSkillSlots();

        // <변경부분> Enemy 고유스킬 아이콘과
        // Tooltip 데이터도 함께 초기화한다.
        ClearUniqueSkillSlot();

        // 상태이상 슬롯 초기화
        ClearStatusEffectSlots();
    }
}