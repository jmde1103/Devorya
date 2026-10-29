# EventMarker 연동 튜토리얼 안내 팝업

## 설정 방법
1. EventSequenceData에서 ForcePieceSelect, ForceTileSelect, ForceButton 스텝을 펼칩니다.
2. `튜토리얼 안내 표시`를 켭니다. 기존 데이터의 기본값은 OFF입니다.
3. 한국어 원문과 영어·일본어 번역을 입력합니다.
4. 아래 Event Dialogue Localization의 기존 `Localization 생성 / 한국어 동기화` 버튼을 누릅니다. 시퀀스 Localization ID가 없다면 기존 생성 기능으로 먼저 지정합니다.
5. Show Marker와 안내 표시는 별개입니다. 마커를 꺼도 안내만 사용할 수 있습니다.

번역은 Event_Dialogue 테이블에 저장됩니다. 영어·일본어를 비우면 실행 중 한국어 원문으로 표시됩니다. 동기화는 영어·일본어를 덮어쓰거나 자동 번역하지 않습니다.

## 표시 및 종료
기존 BattleUIController의 SkillFailurePopup 연결을 그대로 사용합니다. 새로운 Canvas/프리팹 연결은 필요 없습니다.
처음에 기존 오픈 연출을 실행한 뒤 1.0~0.5 알파로 천천히 점멸합니다. 기본 편도 1.2초, 왕복 2.4초이며 시간 배율과 무관하게 작동합니다.
BattleUIController의 `튜토리얼 안내 점멸`에서 최소/최대 알파와 편도 시간을 조절할 수 있습니다.
안내는 지정 행동의 기존 성공 판정에서 종료됩니다. 틀린 입력으로 종료되지 않습니다.

일반 실패 메시지가 발생하면 안내를 잠시 중지합니다. 기존 실패 표시 시간과 페이드가 끝난 뒤 진행 중인 안내만 복원합니다. 완료된 안내는 다시 나오지 않으며 새 스텝으로 바뀌었다면 새 문구만 표시합니다.
시퀀스 중단·완료·교체, 컨트롤러 비활성화에서 번역 이벤트와 안내를 정리합니다.

## 안정적인 번역 키
`event.<sequence ID>.instruction.<step ID>` 형식입니다.
스텝 이름과 순서를 바꿔도 유지됩니다. 기존 Duplicate Step Below를 사용하면 새 ID/키를 만들고 원본의 한국어·영어·일본어를 복사합니다.
기존 Dialogue/SpeechBubble 키와 테이블 ID는 변경하지 않습니다.

## 변경 범위
- EventSequenceStepData: 기본 OFF 안내 옵션, 한국어 원문, 고유 ID, LocalizedString.
- EventSequenceDataEditor 및 TutorialInstruction partial: 기존 접기 구조에 설정 추가, 번역 동기화와 복제 연결.
- EventSequenceController 및 TutorialInstruction partial: 강제 행동 성공 판정 연결, 현재 언어 조회와 구독 정리.
- BattleUIController 및 TutorialInstruction partial: 일반 실패 메시지 우선권, 지속 안내 점멸, 입력 통과.
- TutorialInstructionChecks: 에디터 데이터/복제 및 실제 BattleScene 플레이 검사.

기존 EventScene/WorldMap/배틀 에디터의 전체 접기 개선은 최신 로컬 파일에서 보존했습니다. 씬·프리팹·기존 이벤트 데이터는 자동 수정하지 않았습니다. 실제 튜토리얼에서 안내를 켜고 문구를 입력하는 작업은 인스펙터에서 할 수 있습니다.

## 검증 결과 (2026-10-29)
Unity 6000.4.0f1 컴파일 성공. 튜토리얼 안내 에디터/플레이 검사 41개, 기존 아이템 UX·추가 이동 취소 회귀 검사 123개 통과.
키 생성·유지·복제, 세 가지 강제 행동, 마커/안내 독립 조합, 실패 메시지 우선/복원, 일시정지 중 점멸, KR/EN/JA 변경, 한국어 fallback, 중단/비활성화 정리를 확인했습니다.
씬·프리팹·기존 Localization 데이터 변경 없음. 화면 없는 자동 검증이므로 실제 Inspector 배치와 모바일 화면의 가독성은 수동 확인이 필요합니다.
작업 브랜치: codex/status-effect-restrictions. 커밋 및 메인 병합은 수행하지 않았습니다.
