using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine;

// <변경부분> BATTLE START / WARNING 등
// 전투 화면 중앙 Announcement Spine 연출을
// 하나의 공용 SkeletonAnimation으로 재생하는 Controller.
//
// 종류별 GameObject를 따로 만들지 않고,
// BattleAnnouncementData에 등록된 SkeletonDataAsset과
// Animation을 런타임에 교체해서 사용한다.
public class BattleAnnouncementController : MonoBehaviour
{
    [Header("Shared Visual")]

    // <변경부분> 실제 Spine Visual을 감싸는 자식 Root.
    //
    // Controller가 붙은 현재 GameObject는 항상 활성 상태로 두고,
    // 이 Visual Root만 재생할 때 켰다가 종료 후 끈다.
    [SerializeField]
    private GameObject visualRoot;

    // <변경부분> 위치 / Scale / Glitch Jitter를 적용할 Transform.
    //
    // 비어 있으면 visualRoot의 Transform을 자동 사용한다.
    [SerializeField]
    private Transform visualTransform;

    // <변경부분> 모든 Announcement가 공용으로 사용하는
    // 단 하나의 SkeletonAnimation.
    [SerializeField]
    private SkeletonAnimation skeletonAnimation;


    [Header("Announcement Data")]

    // <변경부분> 사용할 Announcement 데이터를 등록한다.
    //
    // 현재는 BattleStart 데이터 하나만 넣으면 되고,
    // 이후 Warning 등이 완성되면 배열에 Data Asset만 추가한다.
    [SerializeField]
    private BattleAnnouncementData[] announcementDataList;


    [Header("World Glitch")]

    // <변경부분> PopupOpenAnimationData의 jitterRange는
    // 원래 UI Pixel 단위에 가까운 값이므로,
    // SkeletonAnimation World Position에 그대로 적용하면
    // 지나치게 크게 움직인다.
    //
    // 이 값을 곱해 World 좌표용 흔들림 크기로 변환한다.
    [SerializeField, Min(0f)]
    private float jitterToWorldScale =
        0.01f;


    // <변경부분> 현재 Announcement가 재생 중인지 외부에서 확인한다.
    public bool IsPlaying
    {
        get;
        private set;
    }


    private void Awake()
    {
        EnsureReferences();

        // <변경부분> Controller 자체를 Visual Root로 사용하면
        // 종료 시 SetActive(false)와 함께 Coroutine도 중단되므로
        // 반드시 자식 Visual Root를 사용해야 한다.
        if (visualRoot == gameObject)
        {
            Debug.LogError(
                "BattleAnnouncementController 설정 오류: " +
                "Visual Root는 Controller GameObject 자체가 아니라 " +
                "자식 GameObject를 연결해야 합니다."
            );

            return;
        }

        // <변경부분> Battle Scene 시작 시에는
        // Announcement Visual을 보이지 않게 시작한다.
        if (visualRoot != null)
        {
            visualRoot.SetActive(
                false
            );
        }
    }


    private void OnDisable()
    {
        // <변경부분> Scene 종료 또는 Controller 비활성화 시
        // 재생 상태가 다음 활성화까지 남지 않도록 초기화한다.
        IsPlaying =
            false;

        SetSkeletonAlpha(
            1f
        );

        if (visualRoot != null &&
            visualRoot != gameObject)
        {
            visualRoot.SetActive(
                false
            );
        }
    }


    // <변경부분> 지정한 종류의 Announcement를
    // 공용 SkeletonAnimation으로 1회 재생한다.
    //
    // 전체 흐름:
    //
    // Data 검색
    // → SkeletonDataAsset 교체
    // → Spine Animation 시작
    // → 등장 Glitch
    // → Spine 종료 대기
    // → 퇴장 Glitch
    // → Visual Root 숨김
    public IEnumerator PlayRoutine(
        BattleAnnouncementType announcementType)
    {
        // 같은 공용 SkeletonAnimation에서
        // 두 Announcement가 겹쳐 재생되지 않게 한다.
        if (IsPlaying)
        {
            Debug.LogWarning(
                $"Battle Announcement 재생 요청 무시: " +
                $"이미 다른 Announcement가 재생 중입니다. " +
                $"요청={announcementType}"
            );

            yield break;
        }

        EnsureReferences();

        BattleAnnouncementData announcementData =
            GetAnnouncementData(
                announcementType
            );

        if (ValidatePlayRequest(
                announcementType,
                announcementData) == false)
        {
            yield break;
        }

        IsPlaying =
            true;

        // <변경부분> Data에 저장된 위치와 Scale을
        // 이번 연출의 정상 기준값으로 사용한다.
        Vector3 baseLocalPosition =
            announcementData.localPosition;

        Vector3 baseLocalScale =
            announcementData.localScale;

        visualRoot.SetActive(
            true
        );

        visualTransform.localPosition =
            baseLocalPosition;

        visualTransform.localScale =
            baseLocalScale;


        // <변경부분> 공용 SkeletonAnimation에
        // 이번 Announcement의 SkeletonDataAsset을 덮어씌운다.
        skeletonAnimation.skeletonDataAsset =
            announcementData.skeletonDataAsset;

        // SkeletonData 교체 후
        // 반드시 강제 초기화해서 새 Skeleton을 생성한다.
        skeletonAnimation.Initialize(
            true
        );


        if (skeletonAnimation.Skeleton == null ||
            skeletonAnimation.AnimationState == null)
        {
            Debug.LogWarning(
                $"Battle Announcement 재생 실패: " +
                $"Skeleton 초기화에 실패했습니다. " +
                $"Type={announcementType}"
            );

            CleanupVisual(
                baseLocalPosition,
                baseLocalScale
            );

            yield break;
        }


        // <변경부분> 문자열 오타나
        // 잘못된 SkeletonData 연결로 Animation이 없는 경우
        // 전투 전체가 멈추지 않도록 미리 검사한다.
        Spine.Animation spineAnimation =
            skeletonAnimation
                .Skeleton
                .Data
                .FindAnimation(
                    announcementData.AnimationName
                );

        if (spineAnimation == null)
        {
            Debug.LogWarning(
                $"Battle Announcement 재생 실패: " +
                $"Animation을 찾을 수 없습니다. " +
                $"Type={announcementType} / " +
                $"Animation={announcementData.AnimationName}"
            );

            CleanupVisual(
                baseLocalPosition,
                baseLocalScale
            );

            yield break;
        }


        // 이전 퇴장 연출의 Alpha가 남아있지 않도록
        // 정상 표시 상태로 먼저 복구한다.
        SetSkeletonAlpha(
            1f
        );


        bool isSpineAnimationCompleted =
            false;

        // <변경부분> Announcement Spine은 Loop 없이
        // 정확히 1회만 재생한다.
        TrackEntry trackEntry =
            skeletonAnimation
                .AnimationState
                .SetAnimation(
                    0,
                    announcementData.AnimationName,
                    false
                );

        if (trackEntry == null)
        {
            Debug.LogWarning(
                $"Battle Announcement 재생 실패: " +
                $"TrackEntry를 생성하지 못했습니다. " +
                $"Type={announcementType}"
            );

            CleanupVisual(
                baseLocalPosition,
                baseLocalScale
            );

            yield break;
        }


        // <변경부분> Animation 길이를 코드에 하드코딩하지 않고
        // Spine의 실제 Complete Event를 기준으로 종료를 판정한다.
        trackEntry.Complete +=
            entry =>
            {
                isSpineAnimationCompleted =
                    true;
            };


        // <변경부분> Spine Animation을 이미 시작한 상태에서
        // 등장 Glitch를 동시에 재생한다.
        //
        // 따라서 Glitch가 끝난 뒤에야 Spine이 시작되는
        // 느린 연출이 되지 않는다.
        if (announcementData.enterAnimationData != null)
        {
            yield return
                PlayVisualTransitionRoutine(
                    announcementData.enterAnimationData,
                    baseLocalPosition,
                    baseLocalScale
                );
        }
        else
        {
            ApplyBaseVisualState(
                baseLocalPosition,
                baseLocalScale
            );
        }


        // Spine 본 애니메이션이 끝날 때까지 대기한다.
        while (isSpineAnimationCompleted == false)
        {
            // 외부에서 Visual Root가 비정상적으로 꺼졌다면
            // 영원히 대기하지 않고 종료한다.
            if (visualRoot == null ||
                visualRoot.activeInHierarchy == false)
            {
                break;
            }

            yield return null;
        }


        // <변경부분> 정상적으로 Spine이 끝났다면
        // 퇴장 Glitch를 실행한 뒤 Visual을 숨긴다.
        if (isSpineAnimationCompleted &&
            announcementData.exitAnimationData != null)
        {
            yield return
                PlayVisualTransitionRoutine(
                    announcementData.exitAnimationData,
                    baseLocalPosition,
                    baseLocalScale
                );
        }


        CleanupVisual(
            baseLocalPosition,
            baseLocalScale
        );
    }


    // <변경부분> PopupOpenAnimationData의 설정값을
    // World 기반 SkeletonAnimation에 맞게 재생한다.
    //
    // Scale      → Transform.localScale
    // Jitter     → Transform.localPosition
    // Alpha      → Spine Skeleton Color Alpha
    private IEnumerator PlayVisualTransitionRoutine(
        PopupOpenAnimationData animationData,
        Vector3 baseLocalPosition,
        Vector3 baseLocalScale)
    {
        if (animationData == null)
        {
            yield break;
        }

        float duration =
            Mathf.Max(
                0.001f,
                animationData.duration
            );

        float elapsedTime =
            0f;


        // 시작 상태 적용
        visualTransform.localPosition =
            baseLocalPosition;

        visualTransform.localScale =
            MultiplyScale(
                baseLocalScale,
                animationData.startScale
            );

        SetSkeletonAlpha(
            animationData.startAlpha
        );


        while (elapsedTime < duration)
        {
            float deltaTime =
                animationData.useUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

            elapsedTime +=
                deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsedTime /
                    duration
                );

            float curveTime =
                animationData.scaleCurve != null
                    ? animationData
                        .scaleCurve
                        .Evaluate(
                            normalizedTime
                        )
                    : normalizedTime;


            // <변경부분> 기존 PopupOpenAnimationData와 동일하게
            // Start Scale → End Scale을 Curve 기준으로 보간한다.
            Vector2 currentScale =
                Vector2.Lerp(
                    animationData.startScale,
                    animationData.endScale,
                    curveTime
                );

            visualTransform.localScale =
                MultiplyScale(
                    baseLocalScale,
                    currentScale
                );


            float currentAlpha;

            // <변경부분> 기존 PopupOpenAnimator의
            // Alpha Flicker 규칙을 Skeleton Alpha에 동일하게 적용한다.
            if (animationData.useAlphaFlicker)
            {
                float baseAlpha =
                    Mathf.Lerp(
                        animationData.startAlpha,
                        animationData.endAlpha,
                        normalizedTime
                    );

                int flickerIndex =
                    Mathf.FloorToInt(
                        normalizedTime *
                        animationData.flickerCount
                    );

                bool isFlickerLow =
                    flickerIndex % 2 == 0 &&
                    normalizedTime < 0.9f;

                currentAlpha =
                    isFlickerLow
                        ? Mathf.Min(
                            baseAlpha,
                            animationData
                                .flickerMinAlpha
                        )
                        : baseAlpha;
            }
            else
            {
                currentAlpha =
                    Mathf.Lerp(
                        animationData.startAlpha,
                        animationData.endAlpha,
                        normalizedTime
                    );
            }

            SetSkeletonAlpha(
                currentAlpha
            );


            // <변경부분> UI용 Pixel Jitter 값을
            // World 좌표 배율로 변환해 적용한다.
            if (animationData.usePositionJitter)
            {
                float jitterPower =
                    1f -
                    normalizedTime;

                Vector2 jitterOffset =
                    new Vector2(
                        Random.Range(
                            -animationData.jitterRange.x,
                            animationData.jitterRange.x
                        ),
                        Random.Range(
                            -animationData.jitterRange.y,
                            animationData.jitterRange.y
                        )
                    ) *
                    jitterPower *
                    jitterToWorldScale;

                visualTransform.localPosition =
                    baseLocalPosition +
                    new Vector3(
                        jitterOffset.x,
                        jitterOffset.y,
                        0f
                    );
            }
            else
            {
                visualTransform.localPosition =
                    baseLocalPosition;
            }

            yield return null;
        }


        // <변경부분> 마지막 프레임에는
        // 난수나 보간 오차 없이 Data의 최종 상태를 정확히 적용한다.
        visualTransform.localPosition =
            baseLocalPosition;

        visualTransform.localScale =
            MultiplyScale(
                baseLocalScale,
                animationData.endScale
            );

        SetSkeletonAlpha(
            animationData.endAlpha
        );
    }


    // <변경부분> Announcement 종류에 맞는
    // ScriptableObject 데이터를 찾는다.
    private BattleAnnouncementData GetAnnouncementData(
        BattleAnnouncementType announcementType)
    {
        if (announcementDataList == null)
        {
            return null;
        }

        for (int i = 0;
             i < announcementDataList.Length;
             i++)
        {
            BattleAnnouncementData data =
                announcementDataList[i];

            if (data == null)
            {
                continue;
            }

            if (data.announcementType ==
                announcementType)
            {
                return data;
            }
        }

        return null;
    }


    // <변경부분> Announcement 재생에 필요한
    // 공용 오브젝트와 Data 연결 상태를 검사한다.
    private bool ValidatePlayRequest(
        BattleAnnouncementType announcementType,
        BattleAnnouncementData announcementData)
    {
        if (visualRoot == null)
        {
            Debug.LogWarning(
                "Battle Announcement 재생 실패: " +
                "Visual Root가 연결되지 않았습니다."
            );

            return false;
        }

        if (visualRoot == gameObject)
        {
            Debug.LogWarning(
                "Battle Announcement 재생 실패: " +
                "Visual Root는 Controller의 자식 GameObject여야 합니다."
            );

            return false;
        }

        if (visualTransform == null)
        {
            Debug.LogWarning(
                "Battle Announcement 재생 실패: " +
                "Visual Transform이 없습니다."
            );

            return false;
        }

        if (skeletonAnimation == null)
        {
            Debug.LogWarning(
                "Battle Announcement 재생 실패: " +
                "SkeletonAnimation이 연결되지 않았습니다."
            );

            return false;
        }

        if (announcementData == null)
        {
            Debug.LogWarning(
                $"Battle Announcement 재생 실패: " +
                $"{announcementType} 데이터가 등록되지 않았습니다."
            );

            return false;
        }

        if (announcementData.skeletonDataAsset == null)
        {
            Debug.LogWarning(
                $"Battle Announcement 재생 실패: " +
                $"{announcementType} SkeletonDataAsset이 없습니다."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(
                announcementData.AnimationName))
        {
            Debug.LogWarning(
                $"Battle Announcement 재생 실패: " +
                $"{announcementType} Animation Name이 비어 있습니다."
            );

            return false;
        }

        return true;
    }


    // <변경부분> Inspector 연결이 비어 있을 경우
    // 안전하게 공용 Visual 참조를 자동 탐색한다.
    private void EnsureReferences()
    {
        if (visualTransform == null &&
            visualRoot != null)
        {
            visualTransform =
                visualRoot.transform;
        }

        if (skeletonAnimation == null &&
            visualRoot != null)
        {
            skeletonAnimation =
                visualRoot
                    .GetComponentInChildren<SkeletonAnimation>(
                        true
                    );
        }
    }


    // <변경부분> Vector3 기본 Scale에
    // PopupOpenAnimationData의 X/Y 배율을 곱한다.
    private Vector3 MultiplyScale(
        Vector3 baseLocalScale,
        Vector2 animationScale)
    {
        return new Vector3(
            baseLocalScale.x *
                animationScale.x,

            baseLocalScale.y *
                animationScale.y,

            baseLocalScale.z
        );
    }


    // <변경부분> 현재 Skeleton의 RGB는 그대로 유지하고
    // 전체 Alpha만 변경한다.
    //
    // 프로젝트의 기존 SpineFadeController와
    // 동일한 Skeleton Color 제어 방식을 사용한다.
    private void SetSkeletonAlpha(
        float alpha)
    {
        if (skeletonAnimation == null ||
            skeletonAnimation.Skeleton == null)
        {
            return;
        }

        Color currentColor =
            skeletonAnimation
                .Skeleton
                .GetColor();

        currentColor.a =
            Mathf.Clamp01(
                alpha
            );

        skeletonAnimation
            .Skeleton
            .SetColor(
                currentColor
            );
    }


    // <변경부분> 별도 Transition Data가 없을 때
    // Announcement를 정상적인 기본 표시 상태로 맞춘다.
    private void ApplyBaseVisualState(
        Vector3 baseLocalPosition,
        Vector3 baseLocalScale)
    {
        if (visualTransform != null)
        {
            visualTransform.localPosition =
                baseLocalPosition;

            visualTransform.localScale =
                baseLocalScale;
        }

        SetSkeletonAlpha(
            1f
        );
    }


    // <변경부분> 한 Announcement 재생이 끝난 뒤
    // 공용 Visual을 다음 재사용을 위한 정상 상태로 되돌리고 숨긴다.
    private void CleanupVisual(
        Vector3 baseLocalPosition,
        Vector3 baseLocalScale)
    {
        if (skeletonAnimation != null &&
            skeletonAnimation.AnimationState != null)
        {
            skeletonAnimation
                .AnimationState
                .ClearTrack(
                    0
                );
        }

        ApplyBaseVisualState(
            baseLocalPosition,
            baseLocalScale
        );

        if (visualRoot != null &&
            visualRoot != gameObject)
        {
            visualRoot.SetActive(
                false
            );
        }

        IsPlaying =
            false;
    }
}