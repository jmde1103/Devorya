using Spine.Unity;
using UnityEngine;

// <변경부분> 공용 Battle Announcement 오브젝트에
// 어떤 Spine 파일과 Animation을 덮어씌울지 정의하는 데이터.
//
// Piece 공용 프리팹이 PieceData를 받아 외형을 바꾸는 것처럼,
// BattleAnnouncementController는 이 데이터를 받아
// 하나의 SkeletonAnimation을 계속 재사용한다.
[CreateAssetMenu(
    fileName = "BattleAnnouncementData",
    menuName = "Devorya/Battle/Battle Announcement Data"
)]
public class BattleAnnouncementData : ScriptableObject
{
    [Header("Identity")]

    // <변경부분> 이 데이터가 담당하는 Announcement 종류.
    public BattleAnnouncementType announcementType =
        BattleAnnouncementType.BattleStart;


    [Header("Spine")]

    // <변경부분> 공용 SkeletonAnimation에
    // 런타임으로 덮어씌울 Spine SkeletonDataAsset.
    public SkeletonDataAsset skeletonDataAsset;

    // <변경부분> SkeletonDataAsset 안에서 실제 재생할 Animation 이름.
    //
    // 서로 다른 SkeletonDataAsset을 런타임에 교체하는 구조이므로
    // SpineAnimation Attribute 대신 문자열로 직접 관리한다.
    [SerializeField]
    private string animationName;

    public string AnimationName
    {
        get
        {
            return animationName;
        }
    }


    [Header("Transform")]

    // <변경부분> Main Camera 자식 기준으로 사용할
    // Announcement의 기본 Local Position.
    //
    // BattleStart / Warning마다 위치가 달라도
    // 별도 GameObject를 만들 필요 없이 데이터에서 설정한다.
    public Vector3 localPosition =
        Vector3.zero;

    // <변경부분> 해당 Announcement의 기본 Scale.
    public Vector3 localScale =
        Vector3.one;


    [Header("Visual Transition")]

    // <변경부분> Announcement가 처음 나타날 때 사용할
    // 기존 Popup 계열 Glitch Animation 데이터.
    //
    // 실제 실행은 PopupOpenAnimator가 아니라
    // BattleAnnouncementController가 SkeletonAnimation용으로 처리한다.
    public PopupOpenAnimationData enterAnimationData;

    // <변경부분> Spine Animation이 끝난 뒤
    // Announcement가 사라질 때 사용할 Glitch Animation 데이터.
    //
    // Start Alpha 1 / End Alpha 0 형태의
    // 별도 PopupOpenAnimationData 사용을 권장한다.
    public PopupOpenAnimationData exitAnimationData;
}
