using System.Collections.Generic;
using UnityEngine;

// <변경부분> 필드 위 기물에 현재 적용된 상태효과를
// 작은 StatusEffectSlotUI 아이콘으로 표시하는 컴포넌트
public class PieceFieldStatusEffectUI : MonoBehaviour
{
    // 필드에 동시에 표시할 최대 상태효과 개수
    // 4번째 상태효과부터는 필드에서는 표시하지 않는다.
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
    // 상태효과가 하나 이상 있을 때만 활성화할 표시 루트
    // PieceFieldStatusEffectUI가 붙은 오브젝트 자체가 아니라
    // 슬롯들을 감싸는 별도 자식 오브젝트를 연결한다.
    [SerializeField]
    private GameObject contentRoot;

    // 필드 위에 표시할 상태효과 슬롯 배열
    // 최대 3개의 StatusEffectSlotUI를 순서대로 연결한다.
    [SerializeField]
    private StatusEffectSlotUI[] statusEffectSlots;

    [Header("Field Transform")]
    // <변경부분> 필드 상태효과 아이콘 전체의
    // 위치와 크기를 조절할 Transform
    [SerializeField]
    private RectTransform displayRoot;

    // <변경부분> 현재 PieceData에서 전달받은
    // 필드 상태효과 아이콘의 기물별 로컬 위치
    private Vector3 fieldLocalPosition =
        Vector3.zero;

    // 스테이터스 창보다 작게 표시하기 위한 필드 전용 스케일
    // 기물 프리팹마다 Inspector에서 별도로 조절할 수 있다.
    [SerializeField]
    private Vector3 fieldLocalScale =
        new Vector3(
            0.35f,
            0.35f,
            1f
        );

    [Header("Slot Layout")]
    // <변경부분> 상태효과 아이콘 사이의 간격
    // 하나일 때는 중앙, 둘 이상일 때는 이 값을 기준으로 좌우 배치한다.
    [SerializeField]
    private float slotSpacing = 42f;

    private void Awake()
    {
        // Owner가 연결되지 않았다면
        // 현재 오브젝트 또는 부모에서 Piece를 자동으로 찾는다.
        AutoBindOwnerPiece();

        // 표시 루트가 연결되지 않았다면 자동으로 찾는다.
        AutoBindDisplayRoot();

        // 기물별 위치와 스케일 적용
        ApplyFieldTransform();

        // 시작 시 모든 슬롯 숨김
        Clear();
    }

    private void OnEnable()
    {
        // 오브젝트가 다시 활성화된 경우
        // 현재 기물의 상태효과를 다시 표시한다.
        Refresh();
    }

    // <변경부분> 현재 Piece가 보유한 상태효과를
    // 필드용 상태효과 슬롯에 다시 표시한다.
    public void Refresh()
    {
        AutoBindOwnerPiece();
        AutoBindDisplayRoot();
        ApplyFieldTransform();

        // 이전에 표시됐던 모든 슬롯을 먼저 숨긴다.
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

        // Piece 원본 목록이 아니라 UI 표시용 복사본을 가져온다.
        List<OwnedStatusEffectData> ownedStatusEffects =
            ownerPiece.GetStatusEffectsCopy();

        if (ownedStatusEffects == null ||
            ownedStatusEffects.Count == 0)
        {
            SetContentVisible(false);
            return;
        }

        // 실제 표시할 수 있는 유효 상태효과만 수집한다.
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

            // 필드에는 최대 3개까지만 표시한다.
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

        // 전체 표시 루트를 먼저 활성화한다.
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

            // <변경부분> 데이터가 들어갈 슬롯만 활성화한다.
            // 슬롯 배열에 세 개가 연결돼 있어도
            // 실제 상태효과 수만큼만 켜진다.
            slot.gameObject.SetActive(true);

            // 기존 스테이터스 창 슬롯과 동일한 방식으로
            // 아이콘, 툴팁, 경고 애니메이션을 갱신한다.
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

        // <변경부분> 실제 활성화된 슬롯 수를 기준으로
        // 기물 중심에 맞춰 좌우 대칭으로 정렬한다.
        ArrangeVisibleSlots(
            actualVisibleCount
        );
    }

    // <변경부분> 모든 필드 상태효과 슬롯을 비우고 숨긴다.
    public void Clear()
    {
        ClearSlots();
        SetContentVisible(false);
    }

    // <변경부분> 상태효과 슬롯 배열을 모두 비운 뒤
    // 슬롯 GameObject 자체도 비활성화한다.
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

            // 슬롯 내부의 아이콘, Tooltip,
            // 경고 애니메이션 데이터를 초기화한다.
            slot.Clear();

            // <변경부분> 데이터가 없는 슬롯은
            // 슬롯 GameObject 전체를 숨긴다.
            slot.gameObject.SetActive(false);
        }
    }

    // <변경부분> 활성화된 슬롯을 기물 중심에 맞춰 배치한다.
    private void ArrangeVisibleSlots(
        int visibleCount)
    {
        if (statusEffectSlots == null ||
            visibleCount <= 0)
        {
            return;
        }

        int arrangedCount = 0;

        for (int i = 0;
             i < statusEffectSlots.Length;
             i++)
        {
            StatusEffectSlotUI slot =
                statusEffectSlots[i];

            if (slot == null ||
                slot.gameObject.activeSelf == false)
            {
                continue;
            }

            RectTransform slotRect =
                slot.transform as RectTransform;

            if (slotRect == null)
            {
                arrangedCount++;
                continue;
            }

            float targetX =
                GetCenteredSlotPositionX(
                    arrangedCount,
                    visibleCount
                );

            // Y 위치는 프리팹에 설정된 값을 유지하고
            // X 위치만 중앙 정렬 결과로 변경한다.
            Vector2 anchoredPosition =
                slotRect.anchoredPosition;

            anchoredPosition.x =
                targetX;

            slotRect.anchoredPosition =
                anchoredPosition;

            arrangedCount++;

            if (arrangedCount >= visibleCount)
            {
                break;
            }
        }
    }

    // <변경부분> 슬롯 수와 순서에 따라
    // 기물 중심을 기준으로 한 X 좌표를 반환한다.
    private float GetCenteredSlotPositionX(
        int slotIndex,
        int visibleCount)
    {
        switch (visibleCount)
        {
            // 상태효과 하나는 기물 중앙에 배치
            case 1:
                return 0f;

            // 상태효과 두 개는 기물 중심을 기준으로
            // 좌우에 절반 간격씩 배치
            case 2:
                return slotIndex == 0
                    ? -slotSpacing * 0.5f
                    : slotSpacing * 0.5f;

            // 상태효과 세 개는 가운데 슬롯을 기물 중심에 두고
            // 나머지 슬롯을 좌우에 배치
            case 3:
                if (slotIndex == 0)
                {
                    return -slotSpacing;
                }

                if (slotIndex == 1)
                {
                    return 0f;
                }

                return slotSpacing;
        }

        return 0f;
    }

    // 상태효과 표시 루트 활성화 여부를 변경한다.
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

    // <변경부분> PieceData에서 전달받은 기물별 위치와
    // 필드 상태효과 UI 공용 스케일을 표시 루트에 적용한다.
    private void ApplyFieldTransform()
    {
        if (displayRoot == null)
        {
            return;
        }

        // 타입 아이콘과 동일하게
        // Piece 루트 기준 로컬 위치를 직접 적용한다.
        displayRoot.localPosition =
            fieldLocalPosition;

        displayRoot.localScale =
            fieldLocalScale;
    }

    // <변경부분> PieceData에 저장된 기물별 상태효과 아이콘 위치를
    // 현재 필드 상태효과 표시 루트에 적용한다.
    // 타입 아이콘의 SetTypeIconLocalPosition()과 같은 역할이다.
    public void SetLocalPosition(
        Vector3 localPosition)
    {
        fieldLocalPosition =
            localPosition;

        ApplyFieldTransform();
    }

    // Piece 참조가 비어 있으면 자동으로 찾는다.
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

    // <변경부분> DisplayRoot가 연결되지 않았다면
    // ContentRoot의 RectTransform을 자동으로 사용한다.
    private void AutoBindDisplayRoot()
    {
        if (displayRoot != null)
        {
            return;
        }

        if (contentRoot != null)
        {
            displayRoot =
                contentRoot.GetComponent<RectTransform>();
        }

        if (displayRoot == null)
        {
            displayRoot =
                GetComponent<RectTransform>();
        }
    }
}