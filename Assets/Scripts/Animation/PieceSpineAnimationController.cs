using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine;

// <변경부분> 기물 Spine Visual의 애니메이션 재생을 담당하는 컨트롤러
public class PieceSpineAnimationController : MonoBehaviour
{
    [Header("Spine")]
    // Spine SkeletonAnimation 컴포넌트
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("Animation Names")]
    // <변경부분> 기물 생성 또는 외형 변경 시 재생
    [SpineAnimation][SerializeField] private string bornAnimation = "Born";

    // <변경부분> 일반 이동·공격 착지 시 재생
    [SpineAnimation][SerializeField] private string downAnimation = "Down";

    // <변경부분> 흡수 공격 내려찍기 시 일반 Down 대신 재생
    [SpineAnimation][SerializeField] private string downAbsorbAnimation = "Down_Absorb";

    // <변경부분> 흡수 공격에서 Stop 이후 내려찍기 전에 재생
    [SpineAnimation][SerializeField] private string absorbAnimation = "Absorb";

    [SpineAnimation][SerializeField] private string idleAnimation = "Idle";

    [SpineAnimation][SerializeField] private string leftMoveAnimation = "Left";
    [SpineAnimation][SerializeField] private string rightMoveAnimation = "Right";

    [SpineAnimation][SerializeField] private string selectAnimation = "Select";
    [SpineAnimation][SerializeField] private string selectIdleAnimation = "Select_Idle";

    [SpineAnimation][SerializeField] private string leftStopAnimation = "Stop_Left";
    [SpineAnimation][SerializeField] private string rightStopAnimation = "Stop_Right";

    // 현재 선택 상태인지 저장
    private bool isSelected;

    // <변경부분> 이전 코루틴이 나중에 끝나면서 현재 애니메이션을 덮어쓰지 못하게 막는 버전값
    private int animationVersion;

    private void Awake()
    {
        // 인스펙터 연결이 비어 있으면 현재 오브젝트 또는 자식에서 자동 탐색
        if (skeletonAnimation == null)
        {
            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
        }
    }

    private void OnEnable()
    {
        // Spine Visual이 켜질 때 기본 Idle 재생
        PlayIdle();
    }

    // <변경부분> 기본 대기 애니메이션 재생
    public void PlayIdle()
    {
        animationVersion++;
        isSelected = false;
        SetLoopAnimation(idleAnimation);
    }

    // <변경부분> 선택 애니메이션 후 선택 대기 상태 유지
    public IEnumerator PlaySelectRoutine()
    {
        int requestVersion = ++animationVersion;

        isSelected = true;

        yield return PlayOnceRoutine(selectAnimation);

        // 중간에 다른 애니메이션 요청이 들어왔으면 Select_Idle로 덮어쓰지 않음
        if (requestVersion != animationVersion)
        {
            yield break;
        }

        SetLoopAnimation(selectIdleAnimation);
    }

    // <변경부분> 선택 대기 애니메이션 즉시 재생
    public void PlaySelectIdle()
    {
        animationVersion++;
        isSelected = true;
        SetLoopAnimation(selectIdleAnimation);
    }

    // <변경부분> 생성 또는 변형 애니메이션 재생
    public IEnumerator PlayBornRoutine()
    {
        int requestVersion = ++animationVersion;

        yield return PlayOnceRoutine(bornAnimation);

        if (requestVersion != animationVersion)
        {
            yield break;
        }

        PlayIdle();
    }

    // <변경부분> 흡수 공격에서 Stop_Left / Stop_Right 다음에
    // Absorb 애니메이션을 1회 재생하는 코루틴
    // 완료 후 Idle로 돌아가지 않고 Down_Absorb로 이어진다.
    public IEnumerator PlayAbsorbRoutine()
    {
        int requestVersion = ++animationVersion;

        yield return PlayOnceRoutine(absorbAnimation);

        if (requestVersion != animationVersion)
        {
            yield break;
        }
    }

    // <변경부분> 방향에 맞는 이동 애니메이션 재생
    public void PlayMoveByDirection(bool isRightDirection)
    {
        animationVersion++;

        string animationName = isRightDirection ? rightMoveAnimation : leftMoveAnimation;
        SetLoopAnimation(animationName);
    }

    // <변경부분> 방향에 맞는 멈춤 애니메이션만 재생
    // 공격 중 Stop 다음에 바로 Down으로 넘어갈 때 사용
    public IEnumerator PlayStopOnlyRoutine(bool isRightDirection)
    {
        int requestVersion = ++animationVersion;

        string animationName = isRightDirection ? rightStopAnimation : leftStopAnimation;

        yield return PlayOnceRoutine(animationName);

        if (requestVersion != animationVersion)
        {
            yield break;
        }
    }

    // <변경부분> 방향에 맞는 멈춤 애니메이션 재생 후 현재 선택 상태에 맞게 복귀
    public IEnumerator PlayStopRoutine(bool isRightDirection)
    {
        int requestVersion = ++animationVersion;

        string animationName = isRightDirection ? rightStopAnimation : leftStopAnimation;

        yield return PlayOnceRoutine(animationName);

        if (requestVersion != animationVersion)
        {
            yield break;
        }

        PlayReturnIdle();
    }

    // <변경부분> 내려찍기 애니메이션 재생 후 선택 상태에 맞게 복귀
    public IEnumerator PlayDownRoutine()
    {
        int requestVersion = ++animationVersion;

        yield return PlayOnceRoutine(downAnimation);

        if (requestVersion != animationVersion)
        {
            yield break;
        }

        PlayReturnIdle();
    }

    // <변경부분> 내려찍기 애니메이션 재생 후 무조건 Idle로 복귀
    // 기물 선택 해제, 일반 이동 착지, 공격 종료에 사용
    public IEnumerator PlayDownToIdleRoutine()
    {
        int requestVersion = ++animationVersion;

        isSelected = false;

        yield return PlayOnceRoutine(downAnimation);

        if (requestVersion != animationVersion)
        {
            yield break;
        }

        SetLoopAnimation(idleAnimation);
    }

    // <변경부분> 흡수 공격 내려찍기에서는 일반 Down 대신
    // Down_Absorb를 재생한 뒤 Idle로 복귀한다.
    public IEnumerator PlayDownAbsorbToIdleRoutine()
    {
        int requestVersion = ++animationVersion;

        isSelected = false;

        yield return PlayOnceRoutine(downAbsorbAnimation);

        if (requestVersion != animationVersion)
        {
            yield break;
        }

        SetLoopAnimation(idleAnimation);
    }

    // <변경부분> 선택 상태에 맞춰 Idle 또는 Select_Idle로 복귀
    public void PlayReturnIdle()
    {
        if (isSelected)
        {
            SetLoopAnimation(selectIdleAnimation);
        }
        else
        {
            SetLoopAnimation(idleAnimation);
        }
    }

    // <변경부분> Spine Visual의 렌더러 정렬 순서를 갱신
    public void SetSortingOrder(int sortingOrder)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = sortingOrder;
        }
    }

    // <변경부분> 루프 애니메이션 재생
    private void SetLoopAnimation(string animationName)
    {
        if (skeletonAnimation == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(animationName))
        {
            return;
        }

        skeletonAnimation.AnimationState.SetAnimation(0, animationName, true);
    }

    // <변경부분> 1회성 애니메이션 재생 후 해당 길이만큼 대기
    private IEnumerator PlayOnceRoutine(string animationName)
    {
        if (skeletonAnimation == null)
        {
            yield break;
        }

        if (string.IsNullOrEmpty(animationName))
        {
            yield break;
        }

        TrackEntry trackEntry = skeletonAnimation.AnimationState.SetAnimation(0, animationName, false);

        if (trackEntry == null || trackEntry.Animation == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(trackEntry.Animation.Duration);
    }
}