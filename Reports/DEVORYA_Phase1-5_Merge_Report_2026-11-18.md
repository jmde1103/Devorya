# DEVORYA Phase 1~5 본 프로젝트 통합 결과 — 2026-11-18

## A. Git 상태
- 대상: C:/Users/zntif/OneDrive/Documents/GitHub/Devorya
- 대상 브랜치: 11.17-코덱스-병합-작업
- 출처: C:/Users/zntif/.codex/worktrees/shop-final-1105/Devorya
- 출처 브랜치: codex/absorption-catalog
- 본 프로젝트 병합 전·후 HEAD: 77756da89caa910d7d5fd102e5659e7d09b0c8bb (변경 없음)
- Codex HEAD: ee09483c6000d8c82f8d1fb560dfc9e4552e9292 (변경 없음)
- 선별 통합 결과는 미커밋 상태. 커밋, Push, 원격 병합, 브랜치 변경 미실행.
- 본 프로젝트는 시작 시 clean. Codex 미커밋·신규 파일을 별도 백업한 뒤 실제 최신 파일 기준으로 통합.
- 원본 Codex 프로젝트의 백업 대상 모든 파일 SHA256 일치. 원본 프로젝트 수정 없음.

## B. 통합 범위와 보존 방식
런타임 코드·에셋·meta 총 72개를 통합. 파일별 목록은 아래에 기록.
신규 재구성 프리팹 2개와 해당 meta, 신규 partial 코드, 기존 코드의 Phase1~5 변경, EventScene, Localization, Jellu 데이터 및 UI Sprite·폰트를 포함.
공통 기준 ee09483c 및 이후 개발 기준 4f34c7b0와 실제 파일을 비교했다. 본 개발 최신 변경 9개를 점검하고, FormationController에는 기존 최신 타입 아이콘 수정과 재구성 추가를 함께 유지했다.
BattleScene과 WorldMapScene은 기존 본 개발 파일을 유지했다. 최신 전투 테스트·환경·이벤트·방어하기 데이터도 유지했다.
EventScene은 본 프로젝트의 추가 변경이 없는 것을 확인한 뒤 Codex 최신 UI 변경(+318/-13)을 반영했다. 무조건적인 Scene 덮어쓰기가 아니라 공통 기준과 차이를 검토한 결과다.
실제 런타임과 무관한 과거 보고서·복구 Scene·캐시·Editor 생성 도구는 통합하지 않았다. 임시 검사 22파일은 별도 보관 후 프로젝트에서 제거했다.

## C. 사용자 UI 보존
- EventScene 및 재구성 프리팹은 최신 Codex 원본과 바이트 단위 동일.
- Root/Header/Ability/StatusEffect 패널 위치·크기, Font Size, Auto Size/Min/Max, Margin, 정렬, 색상, 스타일, 아이콘·배경, Anchor/Pivot/Scale 보존.
- Scene Prefab Override와 EditorPreviewCard 설정 보존. 일괄 Revert/Apply 및 프리팹 재생성 없음.
- 원본 최신 Scene의 UI와 통합 Scene의 UI를 동일 실행 환경·후보·언어에서 비교: KR/EN/JA 모두 런타임 RectTransform/TMP/Image 설정 일치.
- Devorya Pawn(None), Jellu Pawn, Virus를 지정한 임시 Jellu 데이터(상태효과 2개) 비교 통과. 테스트 데이터는 저장하지 않음.
- 1920×1920 검사 카메라로 생성한 KR/EN/JA 비교 PNG는 원본/통합본 각각 SHA256까지 동일. 이는 UI 단독 렌더 비교이며 실제 게임 화면 전체 캡처와는 구분한다.
- 긴 설명과 짧은 설명의 높이 복원은 별도 Play Mode 검사에서 확인. 사용자 배치는 변경하지 않음.

## D. 실제 실행한 검증
| 검사 | 결과 |
|---|---|
| Unity 6000.4.0f1 C# 컴파일 | 오류 0 |
| AbsorptionCatalogChecks | 46 PASS |
| RecompositionExecutionChecks | 86 PASS |
| 기존 ShopSelling Play Mode | 75 PASS |
| RecompositionSelectionTooltipChecks | 2209 PASS (프레임별 반복 단언 포함, 독립 시나리오 2209개 의미 아님) |
| RecompositionManualEditingChecks | 69 PASS |
| RecompositionNoAbilityChecks | 31 PASS |
| TooltipBodySizeChecks | 본문·섹션 KR/EN/JA 검사 PASS |
| 원본/통합 UI 런타임 비교 | 10 PASS |
| Scene 참조 병합 전후 비교 | Event/Battle/WorldMap 신규 누락 없음 |
| 신규 meta GUID 충돌 | 없음 |
| git diff --check | 통과 |

실제 재구성, Trait 보존, Ability 교체, 기본색/흡수 외형, 편성 이동 저장, EventScene→WorldMap→BattleScene 재진입 및 결과 복원을 확인했다.
후보 1/2/5/9개 선택·취소, 중앙 이동, 비선택 후보 글리치 퇴장·복귀, 언어 변경과 Scene 템플릿 값의 런타임 카드 복사를 확인했다.
Ability 없음→있음→없음, 상태효과만 표시, 높이 확장·축소, 배경 기준 최소 14 간격, 공용 툴팁 회귀를 확인했다.

초기 검사 실패의 구분:
- 최초 Unity 실행은 라이선스 초기화에서 정지하여 자동 검사 프로세스만 종료 후 권한을 갖춰 재실행했다.
- 누락 참조 0 조건은 기존 EventScene Sprite 누락 때문에 실패했다. 병합 전 HEAD의 Scene을 함께 로드하여 비교했고 새 누락이 아님을 확인했다.
- 첫 UI 동등성 검사는 임시 Root 이름 Fixture0/Fixture1을 포함해 실패했다. 비교용 이름만 정규화한 뒤 실제 배치/글자/이미지 속성과 PNG 동등성이 통과했다. 게임 코드는 변경하지 않았다.

## E. 남은 사항과 사용자 확인
기존 Sprite GUID 9f35698a4335d494195ecd9bcf486cf6가 프로젝트에서 해석되지 않는다.
- EventScene: EventUIMarker.m_Sprite, EventWorldMarker.m_Sprite
- WorldMapScene: EventUIMarker.m_Sprite
병합 전후 동일하며, 이번 통합의 신규 누락은 아니다. 원래 어떤 Sprite를 연결해야 하는지 확정되지 않아 임의 복원하지 않았다. 따라서 전체 프로젝트 Missing Reference 0으로 보고하지 않는다.
사용자의 실제 마우스/터치 수동 검증 및 Android 빌드는 이번에 실행하지 않았다. 본 프로젝트를 열어 최종 디자인과 입력감을 확인할 필요가 있다.
기존 사용자가 지정한 UI 편집값은 향후 병합에서도 최신 저장본을 우선하며, 과거 프리팹 기본값으로 되돌리지 않는다. 별도 보존 지침 Reports/DEVORYA_MERGE_REQUIRED_USER_UI_DESIGN_2026-11-16.md도 함께 전달했다.

## 백업 및 증거
- 양쪽 HEAD/branch/status/diff 및 변경·신규 파일 백업: C:/Users/zntif/.codex/visualizations/2026/10/18/01a14fa4-372b-7c92-8641-6bca3977cf64/DevoryaMerge1117
- manifest.json, plan.json, applied.json, final-verification.json, guid-verification.json
- 검사 코드 보관: verification-tools/
- UI 비교 캡처: screenshots/merge-ko-0.png ~ merge-ja-1.png
- Unity 실행 로그: 본 프로젝트 Logs/Merge1117-*.log

## 통합 파일 목록
- [source-delta] Assets/Devorya/Data/Piece/PieceData_Jellu/PieceData_Jellu Bishop.asset
- [source-delta] Assets/Devorya/Data/Piece/PieceData_Jellu/PieceData_Jellu King.asset
- [source-delta] Assets/Devorya/Data/Piece/PieceData_Jellu/PieceData_Jellu Knight.asset
- [source-delta] Assets/Devorya/Data/Piece/PieceData_Jellu/PieceData_Jellu Pawn.asset
- [source-delta] Assets/Devorya/Data/Piece/PieceData_Jellu/PieceData_Jellu Rook.asset
- [source-delta] Assets/Devorya/Data/Skills/Unique/능력_합성.asset
- [source-delta] Assets/Localization/StringTables/Event_UI Shared Data.asset
- [source-delta] Assets/Localization/StringTables/Event_UI_en.asset
- [source-delta] Assets/Localization/StringTables/Event_UI_ja.asset
- [source-delta] Assets/Localization/StringTables/Event_UI_ko.asset
- [source-delta] Assets/Localization/StringTables/Piece_Data Shared Data.asset
- [source-delta] Assets/Localization/StringTables/Piece_Data_en.asset
- [source-delta] Assets/Localization/StringTables/Piece_Data_ja.asset
- [source-delta] Assets/Localization/StringTables/Piece_Data_ko.asset
- [source-delta] Assets/Localization/StringTables/Tooltip_Common Shared Data.asset
- [source-delta] Assets/Localization/StringTables/Tooltip_Common_en.asset
- [source-delta] Assets/Localization/StringTables/Tooltip_Common_ja.asset
- [source-delta] Assets/Localization/StringTables/Tooltip_Common_ko.asset
- [source-delta] Assets/Localization/StringTables/Unique_Skill_en.asset
- [source-delta] Assets/Localization/StringTables/Unique_Skill_ja.asset
- [source-delta] Assets/Localization/StringTables/Unique_Skill_ko.asset
- [source-delta] Assets/Scenes/EventScene.unity
- [source-delta] Assets/Scripts/Animation/PieceAnimationManager.cs
- [source-delta] Assets/Scripts/Animation/PieceSpineAnimationController.cs
- [source-delta] Assets/Scripts/Animation/PopupOpenAnimator.cs
- [source-delta] Assets/Scripts/Board/BoardManager.cs
- [source-delta] Assets/Scripts/EventScene/EventSceneFormationBoardController.cs
- [source-delta] Assets/Scripts/EventScene/EventSceneFormationIntroAnimator.cs
- [source-delta] Assets/Scripts/EventScene/ShopEvent/ShopController.cs
- [source-delta] Assets/Scripts/Pieces/Piece.cs
- [source-delta] Assets/Scripts/Pieces/PieceManager.cs
- [source-delta] Assets/Scripts/Pieces/RunStateManager.cs
- [source-delta] Assets/Scripts/UI/PieceStatusUIController.cs
- [source-delta] Assets/Scripts/UI/ToolTip/TooltipData.cs
- [source-delta] Assets/Scripts/UI/ToolTip/TooltipPopupUI.cs
- [source-delta] Assets/Scripts/UI/ToolTip/TooltipSectionItemUI.cs
- [source-delta] Assets/Sprites/UI/TooltipPopup UI_1.png
- [source-delta] Assets/Sprites/UI/UI_BattleRewardPopup.png
- [source-delta] Assets/TextMesh Pro/Resources/Fonts & Materials/NeoDunggeunmoPro-Regular SDF.asset
- [new-runtime] Assets/Prefabs/Run/Recomposition.meta
- [new-runtime] Assets/Prefabs/Run/Recomposition/RecompositionStatusEffectCard.prefab
- [new-runtime] Assets/Prefabs/Run/Recomposition/RecompositionStatusEffectCard.prefab.meta
- [new-runtime] Assets/Prefabs/Run/Recomposition/RecompositionTooltipUI.prefab
- [new-runtime] Assets/Prefabs/Run/Recomposition/RecompositionTooltipUI.prefab.meta
- [new-runtime] Assets/Scripts/Animation/PieceAnimationManager.Recomposition.cs
- [new-runtime] Assets/Scripts/Animation/PieceAnimationManager.Recomposition.cs.meta
- [new-runtime] Assets/Scripts/EventScene/EventSceneRecompositionCandidateController.Execution.cs
- [new-runtime] Assets/Scripts/EventScene/EventSceneRecompositionCandidateController.Execution.cs.meta
- [new-runtime] Assets/Scripts/EventScene/EventSceneRecompositionCandidateController.Focus.cs
- [new-runtime] Assets/Scripts/EventScene/EventSceneRecompositionCandidateController.Focus.cs.meta
- [new-runtime] Assets/Scripts/EventScene/EventSceneRecompositionCandidateController.Selection.cs
- [new-runtime] Assets/Scripts/EventScene/EventSceneRecompositionCandidateController.Selection.cs.meta
- [new-runtime] Assets/Scripts/EventScene/EventSceneRecompositionCandidateController.Tooltip.cs
- [new-runtime] Assets/Scripts/EventScene/EventSceneRecompositionCandidateController.Tooltip.cs.meta
- [new-runtime] Assets/Scripts/EventScene/EventSceneRecompositionCandidateController.cs
- [new-runtime] Assets/Scripts/EventScene/EventSceneRecompositionCandidateController.cs.meta
- [new-runtime] Assets/Scripts/EventScene/ShopEvent/ShopController.Recomposition.cs
- [new-runtime] Assets/Scripts/EventScene/ShopEvent/ShopController.Recomposition.cs.meta
- [new-runtime] Assets/Scripts/Pieces/Piece.Recomposition.cs
- [new-runtime] Assets/Scripts/Pieces/Piece.Recomposition.cs.meta
- [new-runtime] Assets/Scripts/Pieces/PieceManager.Recomposition.cs
- [new-runtime] Assets/Scripts/Pieces/PieceManager.Recomposition.cs.meta
- [new-runtime] Assets/Scripts/Pieces/PieceManager.RecompositionPreview.cs
- [new-runtime] Assets/Scripts/Pieces/PieceManager.RecompositionPreview.cs.meta
- [new-runtime] Assets/Scripts/UI/ToolTip/RecompositionStatusEffectCard.cs
- [new-runtime] Assets/Scripts/UI/ToolTip/RecompositionStatusEffectCard.cs.meta
- [new-runtime] Assets/Scripts/UI/ToolTip/RecompositionTextSizing.cs
- [new-runtime] Assets/Scripts/UI/ToolTip/RecompositionTextSizing.cs.meta
- [new-runtime] Assets/Scripts/UI/ToolTip/RecompositionTooltipUI.cs
- [new-runtime] Assets/Scripts/UI/ToolTip/RecompositionTooltipUI.cs.meta
- [new-runtime] Assets/Scripts/UI/ToolTip/RecompositionVerticalPanel.cs
- [new-runtime] Assets/Scripts/UI/ToolTip/RecompositionVerticalPanel.cs.meta

검증 후 정리: 자동 갱신된 DotGothic16 폰트 캐시는 별도 보관 후 원래 내용과 CRLF로 복원했다. 최종 Git 변경은 통합 72파일과 보고서 2개뿐이다.
