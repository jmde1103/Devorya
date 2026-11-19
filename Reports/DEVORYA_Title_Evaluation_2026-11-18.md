# DEVORYA 개발환경 동기화 및 타이틀 평가판 안내 — 2026-11-18

## 작업 환경과 결과
- 작업 프로젝트: C:/Users/zntif/.codex/worktrees/shop-final-1105/Devorya
- 작업 브랜치: codex/absorption-catalog
- Codex HEAD: ee09483c6000d8c82f8d1fb560dfc9e4552e9292 (변경 없음)
- 최신 기준: C:/Users/zntif/OneDrive/Documents/GitHub/Devorya
- 기준 브랜치: 11.17-코덱스-병합-작업
- 본 프로젝트 HEAD: d2bc7946f67cdf6363177c1fa7300b85f063a35c (변경 없음)
- 본 프로젝트는 지시서 작성 당시와 달리 이전 병합 결과가 커밋되어 있었고, BattleScene·WorldMapScene·DotGothic 폰트에는 별도 미커밋 변경이 있었다. HEAD만 쓰지 않고 최신 실제 파일까지 비교했다.
- 본 프로젝트의 HEAD, git status, 미커밋 파일 해시 불변을 확인했다. 본 프로젝트 수정·재병합 없음.
- 커밋·Push·Reset·Checkout·새 Worktree 생성 없음. 양쪽 HEAD가 다른 것은 의도된 상태다.

## 동기화
아래 8파일을 최신 본 프로젝트에서 Codex로 반영했다. 기존 6파일은 이전 병합 당시 Codex 백업과 동일함을 먼저 확인했고, 누락 2파일은 meta와 함께 추가했다.
- Assets/Devorya/Data/Battle/StageBattleData_Test.asset
- Assets/Devorya/Data/Battle/StageBattleData_Test.asset.meta
- Assets/Devorya/Data/Environment/EnvironmentVisualProfile_EndNight.asset
- Assets/Devorya/Data/Environment/TutorialStage.asset
- Assets/Devorya/Data/Event/Event Sequence Data/EventSequenceData_Test.asset
- Assets/Devorya/Data/Event/EventScene/Event Test 01.asset
- Assets/Devorya/Data/Skills/Unique/능력_방어하기.asset
- Assets/Scenes/BattleScene.unity

재구성 Phase1~5 코드, EventScene과 두 재구성 프리팹, WorldMapScene 및 기타 이미 동일한 파일은 다시 쓰지 않았다.
기존 Codex 전용 검사·보고서·복구 파일·AGENTS 운영 규칙은 보존했다. 이 개발 보조 파일들과 새 타이틀 기능은 본 프로젝트와 의도적으로 다르다.
사용자 재구성 UI 세 파일은 동기화 전후 SHA256 동일: 폰트/Auto Size/Margin/위치/배경/아이콘/Scene Override/EditorPreviewCard 보존.

## 타이틀 구현 파일
- 수정: Assets/Scripts/System/Localization/DevoryaLocalizationManager.cs
- 신규: Assets/Scripts/Title/TitleEvaluationNotice.cs 및 meta
- 수정: Assets/Scenes/Title.unity (새 프리팹 인스턴스와 연결만 추가)
- 신규: Assets/Prefabs/Title/EvaluationNotice.prefab 및 meta, Title 폴더 meta
- 수정: Assets/Localization/StringTables/Event_UI Shared Data.asset, Event_UI_ko.asset, Event_UI_en.asset, Event_UI_ja.asset
- 개발 도구: Assets/Editor/TitleEvaluationSetup.cs, TitleEvaluationChecks.cs 및 meta

## 재사용한 기존 리소스
- 직접 복제 기준: Assets/Scenes/Title.unity / TutorialChoiceCanvas / TutorialChoiceModal / TutorialChoicePanel
- 위 팝업은 기존 Assets/Scenes/EventScene.unity의 ShopPurchaseButton/ShopExitButton 디자인을 사용한다. 그 중 YesButton을 Continue로 재사용했다.
- 배경 및 버튼 Sprite: Assets/Sprites/UI/TooltipPopup UI_2.png
- 기존 열기: Assets/Scripts/Animation/PopupOpenAnimator.cs + Assets/Devorya/Data/UI/PopupOpenAnimationData.asset
- 기존 버튼 효과: Assets/Devorya/Data/UI/ButtonIcon_NoiseData.asset
- 닫기는 TutorialChoicePopup과 동일한 비활성화 경로다. PopupOpenAnimator.OnDisable이 최종 상태를 복구한다. 별도 닫기 애니메이션 시스템을 만들지 않았다.
- 기존 팝업 원본, 기존 카메라/Canvas 설정, 기존 버튼 Listener는 수정하지 않았다.

## 동작
TitleEvaluationNotice가 초기 Awake에서 기존 타이틀 Canvas와 입력의 원래 상태를 기록하고 잠시 숨김/잠금한다.
기존 전역 언어 관리자의 초기화 완료를 기다린 뒤 InitializeFirstTitleLanguage()가 실행당 최초 타이틀에만 SelectedLocale을 en으로 설정한다. 자동 영어 설정은 Devorya.Language 저장값을 덮어쓰지 않는다.
Event_UI를 사전 로딩하고 LocalizedString.StringChanged로 제목/본문/버튼 문자열 수신을 모두 확인한 뒤 타이틀을 공개하고 팝업을 연다. 비동기 완료 핸들의 수명에 의존하지 않는다.
Continue를 클릭하면 팝업을 닫고 기존 입력 상태를 복원한다. 전체 화면 차단 Image와 뒤쪽 Selectable 잠금으로 마우스/키보드 입력을 보호한다.
같은 실행의 타이틀 복귀에서는 영어 재설정과 팝업 재표시를 하지 않는다. RuntimeInitializeOnLoadMethod(SubsystemRegistration)로 새 실행 및 새 Play 세션에서만 상태를 초기화한다.

## Localization
새 키:
- title.evaluation.title = Work-in-Progress Evaluation Build
- title.evaluation.body = This is a work-in-progress evaluation build featuring DEVORYA's core combat systems, playable encounters, and event nodes. The full roguelite progression system, sound, and visual effects are still in development.
- title.evaluation.continue = Continue
EN은 제공 원문과 정확히 동일하다. KR/JA는 번역 확정 전이므로 영어 원문을 임시 값으로 등록했다. 기존 키/ID/의미 메타데이터와 기존 12개 항목은 유지하고 각 언어에 3개만 추가했다.

## Unity에서 편집하기
Assets/Prefabs/Title/EvaluationNotice.prefab을 열거나 Title 씬의 EvaluationNoticeCanvas 인스턴스를 선택한다.
- 전체 배치: EvaluationNoticeModal/EvaluationNoticePanel RectTransform
- 배경: EvaluationNoticePanel/NoticeBackground RectTransform·Image
- 제목: NoticeBackground/TitleText TMP·RectTransform
- 본문: NoticeBackground/BodyText TMP·RectTransform
- 버튼: EvaluationNoticePanel/ContinueButton RectTransform 및 하위 TMP
편집 시 숨겨진 EvaluationNoticeModal을 켜서 미리 볼 수 있다. 실행 시 제어기가 초기 표시 조건을 처리한다.
기본 패널 940×650, 배경 940×520, 본문 740×300/30pt, 제목 Auto Size 28~36pt, 버튼 320×90/Auto Size 24~32pt.
런타임에서 수동 RectTransform이나 TMP 크기를 다시 배치하지 않는다. Title 씬 인스턴스의 titleCanvases/titleInputs 연결은 유지한다.
생성 도구는 이미 프리팹이 있으면 중단하므로 사용자 편집을 재생성으로 덮어쓰지 않는다.

## 실행한 검증
- Unity 6000.4.0f1 컴파일: 최종 오류 0.
- 저장 언어 ko로 새 Play 시작: 영어 적용, 준비 전 기존 타이틀 Canvas 비표시, 전역 Locale=en, 저장값 ko 유지 PASS.
- 다른 Unity 프로세스로 새 실행, 저장 언어 ja: 영어 초기화·팝업 재표시 PASS.
- 원문 일치, 제목·본문 TMP overflow 없음, 기존 글리치 완료 PASS.
- 실제 Continue 화면 좌표 Raycast 우선 적중 및 pointerClick 처리 PASS.
- 뒤쪽 Play 좌표에서 팝업이 Raycast를 가로채고, Play가 비활성 상태임을 확인 PASS.
- 닫기 후 입력 복원 및 반복 닫기 안전성 PASS.
- 기존 Play → 튜토리얼 선택창 → No → TextCutsceneScene 실제 전환 PASS.
- 한국어 변경 후 다음 씬 유지, Title 재진입 시 언어 유지/팝업 없음/입력 정상 PASS.
- 일본어 선택 기능 PASS.
- 최종 TitleEvaluationChecks는 1940 단언 PASS. 이 숫자에는 매 프레임 초기화/전환 대기 검사가 포함되며 1940개의 독립 시나리오라는 의미는 아니다.
- 최종 타이틀의 기존 Scene 오브젝트/컴포넌트 Serialized 블록은 작업 전과 동일. SceneRoots에는 새 팝업 Root만 추가.
- 재구성 UI 보존은 해시로 확인. 이번에 재구성 수천 개 검사를 반복 실행하지 않았다.
- 본 프로젝트 및 다른 씬의 팝업 원본 변경 없음.
- 실제 Play Mode UI를 1920×1080 RenderTexture로 촬영해 본문과 테두리 내부 여백을 시각적으로 확인했다. 이는 팝업 검사 카메라 캡처이며 사용자의 실제 모니터 전체 화면 캡처와 구분한다.

## 한계 및 미실행
Android 빌드·실기 터치와 사용자의 직접 마우스 조작은 미검증이다. 플레이어 빌드 재실행 대신 별도 Unity 프로세스/Play 세션 재시작을 검증했다.
새 기능을 본 프로젝트로 다시 병합하지 않았다. 사용자 검토 후 별도 병합 요청이 필요하다.
고정 Worktree 경로·Git 등록을 유지했으며 Worktree 삭제/이동은 없었다. 앱 자동 정리 정책까지 변경하거나 보장한 것은 아니다.

## 백업·검증 증거
- 백업: C:/Users/zntif/.codex/visualizations/2026/10/18/01a14fa4-372b-7c92-8641-6bca3977cf64/DevoryaTitleEvaluation1118
- sync-result.json, final-verification.json, 양쪽 HEAD/status 및 변경 파일 백업
- Logs/TitleEvaluation-Setup-Retry.log, TitleEvaluation-Checks-Retry.log, TitleEvaluation-Restart.log, TitleEvaluation-Final.log
- Reports/TitleEvaluationNotice_2026-11-18.png
