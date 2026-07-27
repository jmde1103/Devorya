using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// <변경부분> 필드 위 기물에 현재 적용된 상태효과를
// 말풍선 배경 안의 작은 StatusEffectSlotUI 아이콘으로 표시하는 컴포넌트
public class PieceFieldStatusEffectUI : MonoBehaviour
{
    // 필드에 동시에 표시할 최대 상태효과 개수
    // 네 번째 상태효과부터는 필드에서는 표시하지 않는다.
    private const int MaxVisibleStatusEffectCount = 3;

    [Header("Owner")]
    // 상태효과 정보를 가져올 소유 기물
    [SerializeField]
    private Piece ownerPiece;

    [Header("Data")]
    // 상태효과 타입에 맞는 아이콘과 설명 데이터를 찾는 데이터베이스
    [SerializeField]
    private StatusEffectDatabase statusEffectDatabase;

    [Header("Display")]
    // 상태효과가 하나 이상 있을 때만 활성화할 말풍선 루트
    [SerializeField]
    private GameObject contentRoot;

    // <변경부분> 말풍선 배경과 HorizontalLayoutGroup,
    // ContentSizeFitter가 붙어 있는 RectTransform
    [SerializeField]
    private RectTransform bubbleRoot;

    // 필드 위에 표시할 상태효과 슬롯 배열
    // 최대 세 개의 StatusEffectSlotUI를 순서대로 연결한다.
    [SerializeField]
    private StatusEffectSlotUI[] statusEffectSlots;

    [Header("Field Transform")]
    // 필드 상태효과 말풍선 전체의 위치와 크기를 조절할 Transform
    // 일반적으로 BubbleRoot 자체를 연결한다.
    [SerializeField]
    private RectTransform displayRoot;

    // 현재 PieceData에서 전달받은
    // 필드 상태효과 말풍선의 기물별 로컬 위치
    private Vector3 fieldLocalPosition =
        Vector3.zero;

    // 필드 전용 말풍선과 아이콘 전체 스케일
    [SerializeField]
    private Vector3 fieldLocalScale =
        new Vector3(
            0.35f,
            0.35f,
            1f
        );

    private void Awake()
    {
        // 소유 기물 자동 연결
        AutoBindOwnerPiece();

        // 표시용 RectTransform 자동 연결
        AutoBindDisplayRoots();

        // PieceData 기준 위치와 공용 스케일 적용
        ApplyFieldTransform();

        // 시작 시 모든 슬롯과 말풍선을 숨긴다.
        Clear();
    }

    private void OnEnable()
    {
        // 기물이 다시 활성화되면
        // 현재 상태효과를 기준으로 표시를 다시 갱신한다.
        Refresh();
    }

    // <변경부분> 현재 기물이 보유한 상태효과를
    // 말풍선 내부 슬롯에 다시 표시한다.
    public void Refresh()
    {
        AutoBindOwnerPiece();
        AutoBindDisplayRoots();
        ApplyFieldTransform();

        // 이전 슬롯 정보를 모두 제거하고 숨긴다.
        ClearSlots();

        if (ownerPiece == null)
        {
            SetContentVisible(false);
            return;
        }

        if (statusEffectDatabase == null)
        {
            Debug.LogWarning(
                $"{ownerPiece.name} 필드 상태효과 UI 갱신 실패: " +
                "StatusEffectDatabase가 연결되지 않았습니다."
            );

            SetContentVisible(false);
            return;
        }

        if (statusEffectSlots == null ||
            statusEffectSlots.Length == 0)
        {
            SetContentVisible(false);
            return;
        }

        // 원본 상태효과 리스트가 아니라
        // UI 표시용 복사본을 가져온다.
        List<OwnedStatusEffectData> ownedStatusEffects =
            ownerPiece.GetStatusEffectsCopy();

        if (ownedStatusEffects == null ||
            ownedStatusEffects.Count == 0)
        {
            SetContentVisible(false);
            return;
        }

        // 실제 표시할 수 있는 유효 상태효과를 수집한다.
        List<OwnedStatusEffectData> visibleStatusEffects =
            new List<OwnedStatusEffectData>();

        for (int i = 0;
             i < ownedStatusEffects.Count;
             i++)
        {
            OwnedStatusEffectData ownedStatusEffect =
                ownedStatusEffects[i];

            if (ownedStatusEffect == null)
            {
                continue;
            }

            if (ownedStatusEffect.effectType ==
                StatusEffectType.None)
            {
                continue;
            }

            if (ownedStatusEffect.remainingTurn <= 0 ||
                ownedStatusEffect.stackCount <= 0)
            {
                continue;
            }

            StatusEffectData statusEffectData =
                statusEffectDatabase.GetData(
                    ownedStatusEffect.effectType
                );

            if (statusEffectData == null)
            {
                Debug.LogWarning(
                    $"{ownerPiece.name} 필드 상태효과 데이터 누락: " +
                    $"{ownedStatusEffect.effectType}"
                );

                continue;
            }

            visibleStatusEffects.Add(
                ownedStatusEffect
            );

            // 필드에는 최대 세 개까지만 표시한다.
            if (visibleStatusEffects.Count >=
                MaxVisibleStatusEffectCount)
            {
                break;
            }
        }

        int availableSlotCount =
            Mathf.Min(
                statusEffectSlots.Length,
                MaxVisibleStatusEffectCount
            );

        int displayCount =
            Mathf.Min(
                visibleStatusEffects.Count,
                availableSlotCount
            );

        if (displayCount <= 0)
        {
            SetContentVisible(false);
            return;
        }

        // 말풍선을 먼저 활성화해야
        // LayoutGroup과 ContentSizeFitter가 크기를 계산할 수 있다.
        SetContentVisible(true);

        int actualVisibleCount = 0;

        for (int i = 0;
             i < displayCount;
             i++)
        {
            StatusEffectSlotUI slot =
                statusEffectSlots[i];

            if (slot == null)
            {
                continue;
            }

            OwnedStatusEffectData ownedStatusEffect =
                visibleStatusEffects[i];

            StatusEffectData statusEffectData =
                statusEffectDatabase.GetData(
                    ownedStatusEffect.effectType
                );

            if (statusEffectData == null)
            {
                continue;
            }

            // 데이터가 들어가는 슬롯만 활성화한다.
            slot.gameObject.SetActive(true);

            // 기존 슬롯 표시 기능을 그대로 사용한다.
            slot.Refresh(
                statusEffectData,
                ownedStatusEffect
            );

            actualVisibleCount++;
        }

        if (actualVisibleCount <= 0)
        {
            SetContentVisible(false);
            return;
        }

        // <변경부분> 활성화된 슬롯 수를 기준으로
        // HorizontalLayoutGroup과 ContentSizeFitter를 즉시 다시 계산한다.
        RebuildBubbleLayout();
    }

    // 모든 필드 상태효과 슬롯과 말풍선을 숨긴다.
    public void Clear()
    {
        ClearSlots();
        SetContentVisible(false);
    }

    // 상태효과 슬롯 배열을 모두 초기화하고 비활성화한다.
    private void ClearSlots()
    {
        if (statusEffectSlots == null)
        {
            return;
        }

        for (int i = 0;
             i < statusEffectSlots.Length;
             i++)
        {
            StatusEffectSlotUI slot =
                statusEffectSlots[i];

            if (slot == null)
            {
                continue;
            }

            // 아이콘, Tooltip, 경고 애니메이션 초기화
            slot.Clear();

            // 데이터가 없는 슬롯은 GameObject 전체를 숨긴다.
            slot.gameObject.SetActive(false);
        }
    }

    // <변경부분> 현재 활성화된 슬롯 크기를 기준으로
    // 말풍선 배경 크기와 내부 정렬을 즉시 다시 계산한다.
    private void RebuildBubbleLayout()
    {
        if (bubbleRoot == null)
        {
            return;
        }

        // 자식 슬롯 활성화 상태 변경을 Canvas에 반영
        Canvas.ForceUpdateCanvases();

        // HorizontalLayoutGroup과 ContentSizeFitter의
        // Preferred Size 계산을 즉시 실행한다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            bubbleRoot
        );
    }

    // 말풍선 전체 표시 여부 변경
    private void SetContentVisible(
        bool isVisible)
    {
        if (contentRoot == null)
        {
            return;
        }

        contentRoot.SetActive(
            isVisible
        );
    }

    // PieceData에서 전달받은 기물별 위치와
    // 필드 상태효과 UI 공용 스케일을 적용한다.
    private void ApplyFieldTransform()
    {
        if (displayRoot == null)
        {
            return;
        }

        displayRoot.localPosition =
            fieldLocalPosition;

        displayRoot.localScale =
            fieldLocalScale;
    }

    // PieceData에 저장된 기물별 말풍선 위치 적용
    public void SetLocalPosition(
        Vector3 localPosition)
    {
        fieldLocalPosition =
            localPosition;

        ApplyFieldTransform();
    }

    // 소유 Piece 자동 연결
    private void AutoBindOwnerPiece()
    {
        if (ownerPiece != null)
        {
            return;
        }

        ownerPiece =
            GetComponent<Piece>();

        if (ownerPiece == null)
        {
            ownerPiece =
                GetComponentInParent<Piece>();
        }
    }

    // 말풍선과 표시 루트 RectTransform 자동 연결
    private void AutoBindDisplayRoots()
    {
        if (bubbleRoot == null &&
            contentRoot != null)
        {
            bubbleRoot =
                contentRoot.GetComponent<RectTransform>();
        }

        if (displayRoot == null)
        {
            displayRoot =
                bubbleRoot;
        }

        if (displayRoot == null)
        {
            displayRoot =
                GetComponent<RectTransform>();
        }
    }
}