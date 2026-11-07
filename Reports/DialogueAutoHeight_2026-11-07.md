# 다이어로그 자동 높이 — 2026-11-07
- 브랜치: codex/dialogue-auto-height. 기준 개발 커밋 5be2ce64.
- EventGuideUI 공통 구현으로 BattleScene / EventScene / WorldMapScene 모두 적용.
- 기존 음수 TMP 여백의 실제 영역을 정상 내부 여백으로 환산. 전체 페이지의 줄 높이로 배경 높이를 계산하며 타이핑 글자 수에 따라 창을 반복 변경하지 않음.
- 기존 위치·최소 높이 유지, 짧은 페이지로 전환하면 복원. 폭·문구·폰트 변경 시 재계산.
- ContinueArrow는 아래쪽 가장자리 간격 유지. 기존 배경 sprite border를 사용해 Sliced 표시. 씬/프리팹/GUID/Localization 문구 변경 없음.
- Unity 6000.4.0f1: 66 PASS / 0 FAIL, 컴파일 오류 없음. 3개 장면 × KO/EN/JA 긴 문장·줄바꿈·굵은 글씨·타이핑·폭 축소·하단 화살표·짧은 문장 복원 검사.
- 기존 UnityEditor.Search 인덱스 예외는 기록됨. 검사 중 신규 게임 코드 예외 없음. 모바일 실기기·수동 클릭 검증은 미실행.
- 로그: Logs/dialogue-height-1107.log. 개발 브랜치 병합/원격 Push 없음.
