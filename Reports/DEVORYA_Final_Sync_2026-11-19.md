# DEVORYA 최종 로컬 통합 보고 — 2026-11-19

## 환경 및 이전 HEAD
- 본 프로젝트: C:/Users/zntif/OneDrive/Documents/GitHub/Devorya
- 브랜치: 11.17-코덱스-병합-작업 / 이전 HEAD d2bc7946f67cdf6363177c1fa7300b85f063a35c
- Codex: C:/Users/zntif/.codex/worktrees/shop-final-1105/Devorya
- 브랜치: codex/absorption-catalog / 이전 HEAD ee09483c6000d8c82f8d1fb560dfc9e4552e9292
- 같은 Git 공통 저장소. 새 Worktree/Reset/Clean/Push 없음.

## 커밋 및 변경 수
- 본 프로젝트 보존: abf45e2e, BattleScene 1파일. WorldMapScene/폰트는 줄바꿈 차이뿐이었다.
- Codex 보존: 76750852, 98파일. 이전 Phase1~5 작업 스냅샷 포함.
- 정식 통합 merge: 4892fe3c2b4598f3b06a56b409b986019dac8440, 본 프로젝트 첫 부모 대비 22파일 변경.
- Codex는 merge --ff-only로 같은 통합 커밋에 동기화했다.
- 이 보고서만 별도 커밋한 후 본 프로젝트에도 fast-forward한다. 최종 HEAD는 완료 응답에 기록한다.

## 충돌 해결
- RunStateManager.cs: 신규 기록 정책 초기화 보존. 자동 병합으로 중복 삽입된 기존 도감 멤버만 제거. 최종 코드는 Codex 최신 코드와 공백 제외 동일. CP949 유지.
- Event_UI Shared Data 및 EN/JA/KO 4개 테이블: 기존 항목을 유지하고 title.evaluation.* 3개 키/ID 추가. ID 중복 없음.
- 파일 전체 ours/theirs 적용, Scene/Prefab 전체 덮어쓰기 없음.

## 기능 보존
- 지시서와 달리 신규 도감 ON/OFF 코드는 실제로 Codex 작업 폴더에 저장되어 있었다. 이 최신 구현을 통합했다.
- StageBattleData.saveAbsorbedPiecesToCatalog, BattleSetupManager 전달/종료 복원, RunStateManager 등록 차단/새 런 초기화 유지.
- 튜토리얼 데이터의 도감 기록 OFF 설정 보존. BattleManager의 흡수 코드는 변경하지 않음.
- 최초 English Locale, Evaluation Notice, Continue 입력/배경 차단, Continue/Yes/No 기존 Hover 연결 보존.
- TutorialChoiceCanvas Sorting Layer=WorldMap. 이미 저장된 Sorting Order=110 유지(이번에 임의 적용하지 않음).
- Title.unity, EvaluationNotice.prefab, EventScene.unity, 재구성 툴팁/카드 프리팹은 병합 전 Codex 내용과 동일. Scene Override 포함.
- 본 프로젝트 BattleScene/WorldMapScene/DotGothic 폰트는 병합 전 본 내용과 동일. 비교 시 줄바꿈만 정규화.
- 기존 Recomposition/Shop 실행 코드와 관련 GUID/Inspector 연결 보존.

## 검증 결과
- Unity 6000.4.0f1 C# 컴파일 오류 없음.
- 실제 Editor Play Mode 통합 검사: FINAL_INTEGRATION_PASS checks=31.
- ON 등록/OFF 신규 차단/이전 기록 유지/실제 BattleManager 흡수 및 외형 변경/기록 정책 복원/새 런 초기화 통과.
- 실제 Title Scene 평가판/English/Play 차단, 보이는 버튼 좌표의 최상위 Raycast, InputSystem 마우스 이벤트로 Continue 닫기 및 Play 복원 통과.
- Yes/No 기존 Hover 및 각각 기존 씬 진행 통과. 이 검사는 직접 onClick 호출로 대체하지 않았다.
- 재구성 실행 회귀검사: RECOMPOSITION_EXECUTION_PASS checks=86, exit 0.
- 상점 회귀검사: SHOP_SELLING_PLAYMODE_PASS, exit 0.
- 초기 임시 검사 실패는 PieceManager가 기물 삭제까지 수행한다는 잘못된 가정 때문이었다. 검사만 실제 BattleManager 경로로 수정했다.
- batchmode 입력 주입은 장치/포커스 조건에서 실패. 최종 통과는 batchmode가 아닌 실제 Editor Play Mode에서 확인했다.
- 사용자 물리 마우스 수동 테스트와 Android 실기는 이번에 미실행. 자동 Play Mode 검사와 구분한다.
- 빈 기물 테스트 fixture의 파티클 미연결 경고 및 기존 패키지 경고는 별도 존재.

## 최종 상태 / 증거
- 두 브랜치 추적 파일 차이 없음. 보고서 커밋 후 HEAD/diff/status 재확인.
- 기존 Codex 미추적 검사/보고서/스크린샷/복구 파일은 삭제하지 않고 보존. 무관한 복구 파일과 이번 임시 검사는 커밋하지 않음.
- 이번 임시 검사 파일은 프로젝트에서 제거하고 외부에 보관했다.
- Scene/Prefab 재생성, UI 자동 재배치, Override 일괄 변경 없음.
- 백업 및 로그: C:\Users\zntif\.codex\visualizations\2026\10\18\01a14fa4-372b-7c92-8641-6bca3977cf64\FinalSync_20261119_232017
