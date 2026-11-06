> **2026-11-06 후속 작업:** 이전 보류 사유는 자동검사 전제 오류로 확인됐다. 아래 11월 5일 보고는 이력이며, 최종 판단은 문서 끝의 11월 6일 결과를 따른다. **실제 개발 브랜치 로컬 병합 완료.**

# DEVORYA 상점 최종 병합 보고서 — 2026-11-05

## 기준과 보호
- 대상: `11.01-코덱스-병합-작업`, 시작 HEAD `2d6eace022a1c0e5c78b0094d80d5da085c5fc4c`.
- 소스: `codex/shop-selling-gold-ui`, HEAD `8910f1361e4e2615f46289a7b98ba88e42845e11`와 현재 미커밋 최종 상점 변경.
- 공통 조상: `7c97a8eb5e1dc8f61547be44bf5a89747239f447`.
- 과거 `e3a61b57666a286d68c42b8e5b8d46703973b626` 통합은 이미 포함된 이력이다. 이를 최신 HEAD로 가정하지 않았다.
- `c75aac7385cc717d1a211073d0d1de79d3249281`은 과거 사용자 작업 보존 커밋으로 통째로 재적용하지 않았다.
- 백업: `C:/Users/zntif/.codex/merge-backups/shop-final-20261105` (manifest SHA256, 원본 index, patch, 수정/미추적 파일 복사).
- 개발 스냅샷: `codex/backup-shop-final-1105-development` = `131676c80db18f301f583c35e0a07e7dc1e0b4ca`.
- 소스 스냅샷: `codex/backup-shop-final-1105-codex` = `f05f29f0320a8e26f872c0f6cbc4504eeac1e065`.
- 원본 소스 작업 트리의 HEAD/미커밋 변경을 유지했다. 별도 `codex/shop-final-integration-1105`에서 검증한다.
- 사용자 BattleScene의 최신 미커밋 enableEnemyMovePreview=1, randomizeBattleTileOrder=1, battleTilesPerWave=3, battlePieceInterval=0.1을 보존했다. 문서의 2개로 되돌리지 않았으며 2개와 3개 설정을 검사한다.

## 선별 적용
소스 브랜치 전체 merge/cherry-pick 없이 최신 개발 스냅샷에 상점 차이만 patch로 적용했다. 적용 충돌 없음. 공용 Controller는 상인 등록 hunk 하나만 변경했다.

- `Assets/Editor/Localization/EventSceneDataEditor.cs`: 상점주인 클릭 대사 편집·고유 ID·기존 EventScene_Dialogue 연결. 기존 단계 편집 유지.
- `Assets/Editor/ShopMerchantEntryChecks.cs`: 실제 WorldMap 상점 진입, 화면 Raycast, 대사/언어/버튼/판매/복귀 자동 검사.
- `Assets/Editor/ShopMerchantEntryChecks.cs.meta`: 소스의 기존 GUID 유지.
- `Assets/Editor/ShopSellingChecks.cs`: 최종 판매 UI에 맞춘 검사 갱신. 구매·판매·취소 검사 유지.
- `Assets/Scripts/EventScene/EventSceneSequenceController.cs`: SpawnActor 상인 등록 함수 부분만 변경. 비활성 ShopController 검색과 Configure 전달. 편성/CameraShot/단계 실행 코드는 유지.
- `Assets/Scripts/EventScene/EventSceneStepData.cs`: 상인 클릭 대사 목록/설정 추가. 폐기된 머리 위 아이콘 설정은 직렬화 호환을 위해 숨김 보존.
- `Assets/Scripts/EventScene/ShopEvent/ShopController.Selling.cs`: 머리 위 SaleItemPreview 제거, 버튼 안 선택 아이템 및 취소 상태 동기화.
- `Assets/Scripts/EventScene/ShopEvent/ShopMerchant.cs`: 상인별 순환 클릭 대사, 기존 말풍선 재사용, 실제 기물 아이콘 규격으로 버튼 배치, 호버 X 연결.
- `Assets/Scripts/Pieces/Piece.cs`: 기존 아이콘 규격을 읽는 읽기 전용 접근자만 추가. 일반 기물 동작 불변.
- `Assets/Scripts/UI/BattleItemSlotUI.Shop.cs`: 선택 원본 Image 흰색 점멸 Outline, 두께 (1.5,-1.5).
- `Assets/Scripts/UI/Button/FieldAbsorbButton.ContextAction.cs`: 상인 복제 버튼에만 18×19 배경/16×16 아이콘 규격 적용, 호버 X 0.82배. 일반 흡수 경로 유지.
- `Assets/Scripts/UI/RunUI/RunGoldUI.cs`: +금액의 폰트/머티리얼/크기/기본 스케일 일치, 배경 상단 위 배치와 LateUpdate 추적.
- `Assets/Scripts/UI/RunUI/RunItemBarUI.cs`: 판매 선택 강조 연결.
- `Assets/Devorya/Data/Event/EventScene/Shop/Event_Shop_FieldStage#1.asset`: 실제 상인 SpawnActor의 spawnActorIsShopkeeper만 0→1. 나머지 데이터 보존.
- `Assets/Editor/Merge1101FormationChecks.cs`: 실제 편성 선택→이동→RunState 저장→재개 복원 검사 추가.
- `Assets/Editor/ShopMergeLatestBattleChecks.cs`: 최신 회색 적 이동 미리보기, 타일 무작위/2·3개 wave, 팀별 Born→Idle 회귀 검사 추가.
- `Assets/Editor/ShopMergeLatestBattleChecks.cs.meta`: 새 검사 파일 전용 GUID.
- `Assets/Scenes/BattleScene.unity`: 상점 변경이 아니라 사용자 미커밋 설정 4개를 백업 커밋으로 보존. 타일 wave 최신값 3 유지.

## 제외 및 무결성
- 구 소스의 BattleScene/EventScene 배치, FormationBoard/FormationIntro/Camera/BattleManager/Tile/BoardManager/SetupManager, 폰트 캐시, outputs/node_modules는 가져오지 않았다.
- 전체 비교 목록은 백업 `all-source-differences.tsv`, 선택 목록은 `selected-paths.json`, 인코딩/범위 검사는 `integration-audit.json`에 있다.
- 기존 C# CP949/UTF-8 BOM 인코딩 유지. 기존 meta/GUID와 Localization 테이블 변경 없음. 새 검사 파일 meta만 추가.
- Scene은 사용자 원래 BattleScene 변경만 포함. Prefab 변경 없음. 상점 에셋은 상점주인 플래그 1개만 수정.

## 검증 결과 (2026-11-06 KST 재개)
모두 별도 통합 후보에서 Unity 6000.4.0f1로 새로 실행했다. 소스의 과거 PASS 수를 재사용하지 않았다.

| 검사 | 결과 | 통과/실패 | 근거 |
|---|---|---|---|
| ShopMerchantEntryChecks | PASS | 102 / 0 | shop-final-entry-retest.log |
| ShopSellingChecks | PASS | 75 / 0 | shop-final-ShopSellingChecks.log |
| BattleItemUXChecks | PASS | 123 / 0 | shop-final-BattleItemUXChecks.log |
| 편성 이동·저장·재개 | PASS | 25 / 0 | shop-final-Merge1101FormationChecks.log |
| TutorialInstructionChecks 통합 후보 | FAIL | 12 / 1, 이후 미실행 | shop-final-TutorialInstructionChecks.log |
| TutorialInstructionChecks 개발 기준본 | FAIL (동일 재현) | 12 / 1, 이후 미실행 | shop-final-tutorial-baseline.log |
| 최신 전투 회귀 | PASS | 22 / 0 | shop-final-battle-retest.log |

### 튜토리얼 실패와 안전 중단
`Instruction does not block input` 단언에서 실패했다. 안내 팝업 CanvasGroup의 blocksRaycasts/interactable 검사가 기대값과 다르다. 실제 사용자 입력 차단까지 확인한 결과는 아니며, 테스트 타이밍/활성 상태 또는 기존 구현 문제인지 추가 진단이 필요하다.
상점 차이를 모두 제외한 최신 개발 스냅샷에서도 동일한 검사, 동일한 12개 PASS 이후 같은 실패가 재현됐다. 비교 시 원본 개발 폴더는 건드리지 않았고, 검증 폴더의 후보 파일을 별도 바이트 백업한 뒤 기준본으로 임시 전환하고 검사 종료 후 정확히 복원했다.
따라서 상점 변경으로 새로 생긴 회귀라는 증거는 없지만, 전체 검증 PASS로 처리하지 않는다. 지시서 6·8절에 따라 개발 브랜치에 반영을 강행하지 않고 통합 후보만 보존한다. 튜토리얼 게임 코드는 이번 상점 작업에서 수정하지 않았다.

### 컴파일·예외·경고
- 최초 추가 검사 코드가 Spine의 GetCurrent API를 잘못 사용해 컴파일 실패했다. 검사 코드만 프로젝트 API인 GetTrack으로 수정했고 이후 컴파일 통과했다.
- 최신 전투 검사의 최초 실행은 PieceManager 자식에서 기물을 찾는 잘못된 검사 가정 때문에 실패했다. 실제 SpawnPiece 반환 기물을 추적하도록 검사 코드만 수정했다.
- UnityEditor.Search.SearchDatabase의 ArgumentOutOfRangeException이 여러 실행에서 발생했다. 개발 기준본에서도 동일하게 발생하는 편집기 검색 인덱스 예외이며 게임 코드 스택이 아니다. 예외 0이라고 보고하지 않는다.
- 기존 obsolete API/직렬화 관련 경고가 있으며 신규 검사 및 상인 검색에서도 FindFirstObjectByType 등 CS0618 경고가 나온다. 경고를 없애려 무관한 코드를 수정하지 않았다.
- 위 검사 단언 실패를 제외하고 검사한 게임 실행 경로에서 신규 게임 코드 예외는 발견하지 못했다.

### 보존 및 실제 검증 범위
- 최신 BattleManager/Tile/BoardManager/BattleMoveValidator/SetupManager/PieceManager/FormationIntro/FormationBoard/카메라 구현은 상점 후보에서 변경하지 않았다.
- 상점 검사: WorldMap→Shop Event, 실제 화면 Raycast, 상인 대사/KO·EN·JA/안정 ID, 버튼 내부 아이템/호버 X/취소/선택 원본 흰색 Outline/+금액/판매/연속 판매/구매/Exit→WorldMap 통과.
- 편성 검사: 5×2/수동 위치/기물 선택과 이동/RunState 저장/다시 열기 복원 통과.
- 실제 사람의 마우스·터치 조작은 미검증. 자동 검사는 EventSystem Raycast 및 프로덕션 입력 핸들러 호출이다.
- 모든 CameraShot/WaitForComplete/ContinueStepsWhileOpen 조합, 전체 전투 이동·공격·흡수·턴종료·승패 흐름, 모든 유물, Android 실기기는 완전 재검증하지 않았다. 해당 코드 무변경과 실행한 검사의 통과를 구분한다.
- 스크린샷은 Play Mode 카메라 렌더 결과이며 사용자 수동 테스트 증거가 아니다.

## 최종 판정
- 최신 개발본 코드·사용자 파일 보존: PASS (범위/해시 비교). 모든 기능의 런타임 검증 완료: 미검증/튜토리얼 FAIL.
- 최종 상점 기능 선별 반영 및 자동 검사: PASS (통합 후보).
- 실제 `11.01-코덱스-병합-작업` 반영: 미실행. 튜토리얼 비교 실패를 확인해 안전 기준에 따라 보류.
- 원격 Push: 하지 않음.
- 원본 개발 HEAD는 `2d6eace022a1c0e5c78b0094d80d5da085c5fc4c`, 원본 소스 HEAD는 `8910f1361e4e2615f46289a7b98ba88e42845e11` 유지.
- 실제 최종 대상 통합 커밋: 없음. 후보 커밋 SHA는 아래에 기록한다.


최종 전투 검사에서 회색 마름모·아군 색상 보존·해제, 타일 무작위 2/3개 wave, 플레이어→적 Born 및 Idle 복귀를 확인했다. Unity 임포트가 바꾼 Spine/폰트 파일은 모두 줄바꿈만 다른 것으로 확인하여 후보의 원본 바이트로 복원했다.

## 후보 저장 및 다음 확인 사항
- 상점 통합 후보 커밋: `4084cdf340f5c5676a215692ac204c2e617dc9a5`.
- 후보 브랜치: `codex/shop-final-integration-1105`.
- 후보 작업 폴더: `C:/Users/zntif/.codex/worktrees/shop-final-1105/Devorya`.
- 결과 요약: `shop-final-test-results.json`.
- 실행 로그/Play Mode 렌더: `ShopFinalMergeEvidence/` (로그는 로컬 검증 자료로 보관).
- 보고서 및 검사 결과 JSON, `ShopFinalMergeEvidence/merchant-selected-item.png`도 이번 후보의 문서 산출물이다.
- 다음 단계는 개발 기준본에서도 실패하는 튜토리얼 CanvasGroup 입력 검사 원인을 확인하는 것이다. 입력 차단 실제 동작과 검사 타이밍을 구분한 뒤, 전체 안전 검증을 다시 판정하고 대상 브랜치 반영을 진행해야 한다.
- 원본 개발·코덱스 파일 해시는 최종 확인에서도 백업과 동일했다. 미커밋 사용자 파일을 삭제하거나 기존 브랜치 HEAD를 이동하지 않았다.

## 2026-11-06 튜토리얼 원인 해결 및 최종 반영
- 원인: 최신 BattleScene의 배치 안내가 `uniqueSkillFailureMessageCoroutine`으로 공용 팝업을 사용 중인데, 기존 검사가 고정 5초 후 튜토리얼을 시작해 그 팝업을 검사했다. 진단 시 `instruction=True`, `failure=True`, `blocks=True`, `interactable=True`, 문구는 “기물 자리 배치를 진행하고 체크 버튼을 누르세요.”였다. 튜토리얼 자체가 팝업을 소유한 상태가 아니었다.
- 판정: 자동검사의 실행 순서/고정 시간 가정 오류. 튜토리얼 게임 코드 변경 없음. 일반 안내의 입력 처리 정책은 이 작업에서 변경하지 않았다.
- 수정 파일: `Assets/Editor/TutorialInstructionChecks.cs` 한 개. 전투 시작 연출 완료, 기존 안내 코루틴 종료, 튜토리얼 오픈 완료를 기다리고 팝업 소유 문구를 확인한다. 한 시점의 알파 차이 대신 1.5초 범위 관찰을 사용하며 기존 비차단 단언은 유지했다.
- 추가 검사: 실제 화면 EventSystem Raycast, 강제 버튼/기물/타일 선택 gate. 튜토리얼 53 PASS / 0 FAIL. Localization KO/EN/JA, 일반 실패 메시지 우선권과 복귀, disable 정리도 통과.
- 별도 검사 수정 커밋: `e412fda9`.
- 개발 원본에 추가된 최신 씬/상점/기타 64개 미커밋·미추적·삭제 상태는 `C:/Users/zntif/.codex/merge-backups/shop-final-20261106`에 별도 보호했다. 개발 데이터 보호 스냅샷은 `3c8d8c53ee98224be314807604d3089a3dd1ff4a`.
- 후보에 최신 사용자 BattleScene/EventScene/상점 데이터를 통합한 커밋: `16c29468`. 상인 Spine override 1곳의 충돌은 최신 사용자 연결을 유지해 해결했다. 상점 데이터 전체 내용이 사용자 최신 스냅샷과 일치함을 검사했다.
- 중간 중단한 검사에서 생성된 임시 Event_Dialogue 키는 검증 폴더에서만 발생했고 원본 테이블로 복원했다. 제품 Localization 변경 없음.
- 최종 핵심 회귀 및 실제 개발 반영 결과는 아래 기록한다.

최종 후보 재검사: 튜토리얼 53, 상점 진입 102, 판매·구매 75, 전투 아이템 123, 편성 25, 최신 전투 22 = **400 PASS / 0 FAIL**. 컴파일 오류 없음. 기존 UnityEditor.Search 검색 인덱스 예외는 남아 있으며, 신규 게임 코드 예외는 발견되지 않았다. 수동 마우스/터치 및 Android 실기기 검증은 하지 않았다.

### 실제 개발 브랜치 반영 완료
- 대상: `11.01-코덱스-병합-작업`.
- 사용자 데이터 보존 커밋: `5a54171d` (최신 BattleScene/EventScene/상점 에셋 3개). 그 외 사용자 파일 61개의 변경·미추적·삭제 상태는 그대로 유지했다.
- 실제 병합 커밋: `323c1a16c1689e4ff84839a17d5308141c665477`.
- 병합 직후 Assets/Packages/ProjectSettings가 400개 검사에 통과한 후보와 동일함을 비교했다. 사용자 3개 데이터 내용 및 그 외 파일 해시도 백업과 일치했다.
- 개발본 최신 기능 보존: PASS. 상점 기능과 튜토리얼 관련 검사: PASS. 실제 로컬 개발 브랜치 반영: PASS. 원격 Push: 하지 않음.
- 남은 사항: 기존 UnityEditor.Search 인덱스 예외, 사람의 마우스/터치 및 Android 실기기 확인. 신규 게임 코드 오류는 검사한 범위에서 발견되지 않았다.
