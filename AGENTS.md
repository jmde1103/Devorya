# DEVORYA 데이터 이름 규칙

- 능력, 특성, 상태효과, 아이템의 사용자 표시명과 Unity Inspector 드롭다운은 한글로 작성한다.
- 데이터 에셋 이름은 `능력_이름`, `특성_이름`, `상태효과_이름`, `아이템_이름` 형식으로 작성한다. 파일명에 쓸 수 없는 콜론은 밑줄로 바꾼다.
- 새 enum 항목에는 한글 `InspectorName`을 지정한다. 기존 enum 식별자와 숫자 값은 호환성을 위해 유지한다.
- 에셋 이름 변경 시 기존 `.meta` GUID를 보존하고 하드코딩된 경로 참조도 함께 수정한다.
- 기존 영어·일본어 Localization 번역과 키는 유지하며, 한국어 표시명은 한글로 작성한다.
- Hierarchy의 GameObject 이름과 실행 중 생성되는 오브젝트 이름은 영문으로 작성한다. Inspector 설정명과 데이터 에셋의 한글 표시는 유지한다.


# DEVORYA 고정 작업 환경 운영 규칙 (2026-11-12 사용자 확정)

- Codex 개발 및 Unity 테스트 프로젝트 경로는 `C:/Users/zntif/.codex/worktrees/shop-final-1105/Devorya`로 고정한다. 셸 기본 작업 폴더가 다르더라도 모든 개발 명령의 workdir는 이 경로로 명시한다.
- 본 개발 프로젝트는 `C:/Users/zntif/OneDrive/Documents/GitHub/Devorya`를 유지한다. 조사와 명시적으로 승인된 병합 외에 본 프로젝트를 개발 작업 폴더로 사용하지 않는다.
- 새로운 Worktree 생성, 작업 폴더 이동 또는 변경은 실행 전에 반드시 사용자 승인을 받는다. 임시 통합용 Worktree도 예외가 아니다.
- 작업 시작과 종료 시 실제 프로젝트 절대 경로와 현재 Git 브랜치를 사용자에게 명시한다. 이전 폴더를 현재 작업 폴더로 안내하지 않는다.
- 작업 시작 시 `git worktree list --porcelain`, 현재 브랜치 및 미커밋·신규 파일 상태를 확인한다. 브랜치 변경 전 기존 수정 사항과 충돌 여부를 먼저 점검한다.
- 재구성 1·2단계의 미커밋 코드·신규 파일·테스트·보고서 및 사용자 Scene/Prefab/.meta 변경을 보존한다. 기존 Worktree, 미커밋 파일을 임의로 삭제·초기화하거나 덮어쓰지 않는다.
- 기존 인코딩, 한글 주석, GUID와 Inspector 연결을 보존한다.
- Unity 테스트는 고정 경로에서 실시하고 실제 Unity 프로세스의 프로젝트 경로가 일치하는지 확인한다. 본 프로젝트와 Codex 테스트 프로젝트의 두 프로젝트 운영 체계를 유지한다.
- 테스트 완료 후 사용자에게 승인된 범위에서 본 개발 브랜치로 안전하게 병합한다. 운영 규칙 설정 요청만으로 코드 변경이나 병합을 시작하지 않는다.
- 고정 경로의 존재와 Git 등록 상태, Codex 관리 Worktree의 자동 정리 가능성을 확인한다. 지시 문서 자체를 자동 삭제 방지 장치로 간주하지 않는다. 앱의 보호/자동 정리 설정이 확인되지 않으면 그 한계를 보고하며 임의 삭제·이동으로 해결하지 않는다.
- 현재 기준 브랜치는 `codex/absorption-catalog`이다. 브랜치는 작업에 따라 승인 범위에서 변경할 수 있으나 고정 폴더는 바꾸지 않는다.

# 재구성 UI 병합 필수 규칙 (2026-11-16 사용자 지시)

- 향후 본 프로젝트 병합 전에 `Reports/DEVORYA_MERGE_REQUIRED_USER_UI_DESIGN_2026-11-16.md`를 반드시 읽는다.
- 고정 Codex 프로젝트에 사용자가 직접 저장한 최신 UI 디자인과 Scene의 Prefab Override를 보존한다. 이전 프리팹 기본값이나 백업으로 덮어쓰지 않는다.
- 병합 직전 최신 사용자 변경을 다시 확인하고, 배치·폰트·아이콘·배경·GUID 및 Override를 병합 전후 비교한다. 다른 개발 기능은 차이를 검토하여 선별 통합한다.


# Title 사용자 설정 병합 필수 규칙 (2026-11-19 사용자 지시)

- Title 작업 및 본 프로젝트 병합 전에 `Reports/DEVORYA_TITLE_USER_SETTINGS_PRESERVATION_2026-11-19.md`를 읽고 최신 실제 저장 파일과 비교한다.
- Continue / Yes / No의 기존 TitleMenuButtonAnimation 수치·참조·EventTrigger 및 Continue의 Scene 추가 컴포넌트 Override를 보존한다.
- TutorialChoiceCanvas의 WorldMap Sorting Layer와 사용자 저장 Sorting Order를 유지한다. 과거 제안이나 기본값으로 임의 변경하지 않는다.
- 최초 English Locale, 평가판 자동 표시·Continue 입력, 사용자 UI 디자인과 Scene/Prefab Override를 보존한다. 전체 덮어쓰기·재생성·일괄 Revert/Apply 금지.
- 본 보고서의 백업은 비교 자료이며 이후 사용자 편집을 되돌리는 기준이 아니다. 병합 직전 최신 저장값을 다시 확인한다.
