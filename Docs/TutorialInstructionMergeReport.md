# 튜토리얼 안내 팝업 안전 병합 보고서

## 기준과 복구 지점
- 개발 브랜치: `10.26-코덱스-병합-작업`
- 작업 시작 당시 양쪽 HEAD 및 공통 조상: `5406cc07b580775766326cdbf745bf172d450a8c`
- 사용자 최신 전체 접기 개선 보존 커밋: `d34006b5`
- 코덱스 안내 기능 소스 커밋: `0fde3bd6`
- 백업: `C:/Users/zntif/.codex/backups/tutorial-instruction-merge-20261029-214417`
- 백업 폴더에는 양쪽 HEAD/브랜치/경로 JSON, tracked binary patch, 미커밋 및 신규 파일 원본 ZIP, status, stash 목록이 있습니다.
- 복구는 백업 브랜치에서 별도 작업 폴더를 만든 뒤 해당 patch를 적용하고 ZIP의 신규 파일을 복원하는 방식으로 가능합니다. 현재 개발 폴더에 무조건 덮어쓰지 마세요.
- 이전 사용자 테스트 데이터/씬 변경을 보존한 stash는 적용하거나 제거하지 않았습니다.

## 병합 방식
양쪽 전체 상태를 조사한 결과 코덱스의 미커밋 변경에는 이번 안내 기능과 개발 폴더에서 복사한 동일한 최신 에디터 개선만 있었습니다. 관련 없는 추가 변경은 없었습니다.
개발 폴더의 미커밋 에디터 개선 3개 파일을 먼저 별도 커밋으로 보존하고, 확인한 소스 변경만 커밋하여 병합했습니다. EventSequenceDataEditor는 공통 조상을 기준으로 Git이 자동 통합했고 충돌은 없었습니다. 파일 전체 덮어쓰기나 ours/theirs 일괄 선택은 사용하지 않았습니다.

## 개발 기준 대비 기능 변경 파일 전체
아래 목록은 전체 접기 개선 보존 커밋 이후의 안내 기능 변경입니다.
- `Assets/Editor/Localization/EventSequenceDataEditor.TutorialInstruction.cs`
- `Assets/Editor/Localization/EventSequenceDataEditor.TutorialInstruction.cs.meta`
- `Assets/Editor/Localization/EventSequenceDataEditor.cs`
- `Assets/Editor/TutorialInstructionChecks.cs`
- `Assets/Editor/TutorialInstructionChecks.cs.meta`
- `Assets/Scripts/EventSequence/EventSequenceController.TutorialInstruction.cs`
- `Assets/Scripts/EventSequence/EventSequenceController.TutorialInstruction.cs.meta`
- `Assets/Scripts/EventSequence/EventSequenceController.cs`
- `Assets/Scripts/EventSequence/EventSequenceStepData.cs`
- `Assets/Scripts/UI/BattleUIController.TutorialInstruction.cs`
- `Assets/Scripts/UI/BattleUIController.TutorialInstruction.cs.meta`
- `Assets/Scripts/UI/BattleUIController.cs`
- `Docs/TutorialInstructionPopup.md`

## 보호 대상 확인
- WorldMapEventDataEditor와 EventSceneDataEditor는 사용자 최신 개선본과 동일합니다.
- EventSequenceDataEditor의 기존 전체 접기, 재정렬, 기존 번역 편집 코드는 보존하고 안내 설정·번역·복제 연결만 추가했습니다.
- 기존 enum 값과 Serialized 필드명 및 기존 .meta GUID는 변경하지 않았습니다. 신규 C# 4개와 대응 .meta 4개가 포함됩니다.
- 기존 씬, 프리팹, ScriptableObject 에셋, Localization 테이블, ProjectSettings는 변경하지 않았습니다.
- 합성: 강화, 상태효과, 킹 아이템 규칙, EndTimer/RunState, 텍스트 스타일 등 관련 없는 게임 코드는 변경하지 않았습니다.
- 신규 안내 키는 사용자가 인스펙터에서 동기화할 때 Event_Dialogue에 생성합니다. 기존 테이블 전체 재생성/동기화는 수행하지 않았습니다.
- 추가 Inspector 오브젝트 연결은 필요 없습니다. 원하는 강제 행동 스텝에서 안내를 켜고 번역을 입력한 뒤 기존 동기화 버튼을 사용하면 됩니다.

## 검사 범위
자동 Play Mode 검사와 화면을 직접 조작한 검증은 구분합니다. 실제 Inspector의 전체 접기·드래그 재정렬 UI와 모바일 화면 가독성은 이번 병합에서 수동 조작하지 않았습니다. 기존 접기 구현 보존은 소스 비교로 확인합니다.

## 병합된 개발 프로젝트 검증 결과
Unity 6000.4.0f1 컴파일 성공. 개발 폴더에서 TutorialInstructionChecks 41개 및 BattleItemUXChecks 123개, 총 164개 자동 검사를 다시 통과했습니다.
검증 로그: `Logs/tutorial-instruction-merge.log`, `Logs/tutorial-instruction-merge-regression.log`.
일반 실패 메시지 종료/안내 복원, 강제 행동 성공/잘못된 입력, 마커 ON/OFF 독립 조합, 언어 변경, 번역 fallback, 키 유지/복제, 기존 아이템/추가 이동 취소를 실제 Play Mode 자동 실행으로 검증했습니다.
인스펙터 직접 조작(접기·드래그)과 모바일 시각 검증은 수행하지 않았습니다. 신규 컴파일 오류 및 검사 실패 없음. 기존 프로젝트의 symbolic-link 경고와 미사용 Unity 테스트 어셈블리 로딩 경고는 발생했으나 이번 병합에서 해당 파일을 변경하지 않았습니다.
`git diff --check` 통과. 기존 GUID/씬/프리팹/번역 테이블 변경 없음.
병합 보고서 자체(`Docs/TutorialInstructionMergeReport.md`)도 최종 커밋에 포함합니다. 원격 push와 main 브랜치 변경은 수행하지 않습니다.
