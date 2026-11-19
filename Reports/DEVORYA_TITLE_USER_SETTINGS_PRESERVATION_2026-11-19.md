# DEVORYA Title 최신 사용자 설정 보존 — 2026-11-19

## 작업 환경
- 프로젝트: C:/Users/zntif/.codex/worktrees/shop-final-1105/Devorya
- 브랜치: codex/absorption-catalog
- HEAD: ee09483c6000d8c82f8d1fb560dfc9e4552e9292
- 기존 미커밋/신규 파일 다수 존재. 전체 상태는 백업 git-status.txt에 기록.
- 본 프로젝트 수정, 병합, 커밋, Push, 브랜치 전환 없음.

## 실제 저장 상태
- Continue / Yes / No 모두 기존 TitleMenuButtonAnimation을 사용.
- EventTrigger PointerEnter → PlaySelectAnimation, PointerExit → ResetSelection. 참조 대상은 각 버튼의 애니메이션 컴포넌트이며 RuntimeOnly 연결.
- 세 버튼 모두 targetRect/buttonText 참조 있음, 컴포넌트 enabled=1.
- 공통 저장 수치: floatingDistance=0, floatingSpeed=1.2, floatingPhaseOffset=0, selectedScale=1.1, selectedOvershootScale=1.15, selectDuration=0.18, normalTextColor=white, selectedTextColor=white, outlineColor=black, outlineWidth=0.2.
- No: animation fileID 242881566, EventTrigger 242881567.
- Yes: animation fileID 1666586355, EventTrigger 1666586356.
- Continue: animation fileID 1681196710, EventTrigger 1681196711. EvaluationNotice.prefab 원본에는 없고 Title.unity의 prefab instance m_AddedComponents Override로 저장됨. Scene을 누락하거나 Override를 Revert하면 Hover가 사라짐.

## TutorialChoiceCanvas 값 구분
- Canvas Sorting Layer = WorldMap (serialized ID -254369403, unsigned 4040597893).
- GameObject Layer = Default (m_Layer: 0). 사용자 지시의 WorldMap은 실제 저장 파일상 Canvas Sorting Layer에 해당.
- Sorting Order는 현재 이미 110으로 저장됨. 이번 작업에서 110으로 변경한 것이 아님. 사용자 지시에 따라 기존 실제 저장값을 유지했고 다른 숫자를 추정해 적용하지 않음.
- BackgroundClickArea, GraphicRaycaster, Yes/No Listener, TutorialChoicePopup 로직 변경 없음.

## 기존 기능 확인 및 검증 범위
- DevoryaLocalizationManager.InitializeFirstTitleLanguage에서 최초 English 설정 유지.
- TitleEvaluationNotice의 자동 표시, Continue Close listener, Canvas 정렬 보정, 팝업 동안 입력 선택 유지/종료 시 복원 코드 유지.
- TutorialChoicePopup의 ChooseYes/ChooseNo 등록 및 기존 Show/Choose 처리 유지.
- 이번에는 디스크에 저장된 YAML 컴포넌트/참조/코드와 해시만 검사함. Unity Play Mode, Hover 재생, 물리 입력을 재실행한 결과로 보고하지 않음.
- 저장되지 않은 Unity 메모리 편집은 이번 디스크 확인으로 보장하지 않음. 향후 병합 직전 저장된 최신 파일을 다시 확인.

## 향후 병합 필수 규칙
- 이 보고서와 당시 최신 사용자 저장 상태를 먼저 비교. 이 백업으로 이후 사용자 편집을 되돌리지 않음.
- Title.unity와 EvaluationNotice.prefab의 배경/RectTransform/Anchor/Pivot/Scale/TMP 및 Scene Override 우선 보존.
- Continue의 Scene 추가 컴포넌트 Override와 Yes/No EventTrigger를 함께 반영.
- WorldMap Sorting Layer를 Default로 복원하지 않음. Sorting Order는 과거 제안이나 기본값으로 임의 변경하지 않음.
- TitleMenuButtonAnimation 재사용, 새로운 Hover 시스템 금지.
- 본 프로젝트 최신 기능과 파일별 비교하여 선별 통합. Scene/Prefab 전체 덮어쓰기, 일괄 Apply/Revert, 생성 도구 재실행 금지.
- Event_UI Shared Data 및 언어 테이블은 기존 본 프로젝트 키/ID를 보호하며 title.evaluation.* 변경만 선별 통합.
- 아래 의존 파일은 수정 필요 여부를 비교한다. 이미 동일한 스크립트/TagManager는 덮어쓰지 않는다. .meta GUID 및 Title 폴더 meta 보존.

## 병합 반영/비교 대상
- Assets/Scenes/Title.unity
- Assets/Scenes/Title.unity.meta
- Assets/Prefabs/Title.meta
- Assets/Prefabs/Title/EvaluationNotice.prefab
- Assets/Prefabs/Title/EvaluationNotice.prefab.meta
- Assets/Scripts/Title/TitleEvaluationNotice.cs
- Assets/Scripts/Title/TitleEvaluationNotice.cs.meta
- Assets/Scripts/Title/TitleMenuButtonAnimation.cs
- Assets/Scripts/Title/TitleMenuButtonAnimation.cs.meta
- Assets/Scripts/Title/TutorialChoicePopup.cs
- Assets/Scripts/Title/TutorialChoicePopup.cs.meta
- Assets/Scripts/Title/TitleMenuController.cs
- Assets/Scripts/Title/TitlePlayHoverHandler.cs
- Assets/Scripts/System/Localization/DevoryaLocalizationManager.cs
- Assets/Scripts/System/Localization/DevoryaLocalizationManager.cs.meta
- Assets/Localization/StringTables/Event_UI Shared Data.asset
- Assets/Localization/StringTables/Event_UI_en.asset
- Assets/Localization/StringTables/Event_UI_ko.asset
- Assets/Localization/StringTables/Event_UI_ja.asset
- ProjectSettings/TagManager.asset

## 이번 실제 변경
- 신규: 본 보존 보고서.
- 수정: AGENTS.md에 이 보고서 확인 및 최신 Title 설정 보존 규칙만 추가.
- 미변경: 모든 Scene, Prefab, Script, Localization, ProjectSettings 및 기존 사용자 작업.

## 백업 및 해시
C:\Users\zntif\.codex\visualizations\2026\10\18\01a14fa4-372b-7c92-8641-6bca3977cf64\TitlePreservation_20261119_220123

- `Assets/Scenes/Title.unity`: `5f3c4064904af4001da11fa9c5b8b9d48baa9d78db4270b7bc87a93d42f1e2df`
- `Assets/Scenes/Title.unity.meta`: `6b5b65f55fd6b1a8d84e5088bf67dd50e0da2dc15f00b2c3107378158d7a4188`
- `Assets/Prefabs/Title.meta`: `959cff6b37fd755ef960414561e95e4eec0228ddc67440a7cfce312aad6e331c`
- `Assets/Prefabs/Title/EvaluationNotice.prefab`: `41a959d7f91ef1524c5181a166de9707749c67b80659cd00971615fd77dad16f`
- `Assets/Prefabs/Title/EvaluationNotice.prefab.meta`: `70bfd47bdeb4a3e6096508ceb873864615ada85576ab66b8e938b610c2785793`
- `Assets/Scripts/Title/TitleEvaluationNotice.cs`: `a6b31310760241082db04483e4590e0c1e172014bdbe50a3154f89c46ee8cb16`
- `Assets/Scripts/Title/TitleEvaluationNotice.cs.meta`: `88dce2410fdae623b4921ab0895edea0485d7a708f8628c4ee5d378cfb5f2d8f`
- `Assets/Scripts/Title/TitleMenuButtonAnimation.cs`: `a71791335238847fd0ede1ab5fe26bd8a948c88c837ee6c5f221ae3f88ebcbd6`
- `Assets/Scripts/Title/TitleMenuButtonAnimation.cs.meta`: `74c70b17f9dd36fe3e7edba3dd79d00c403c6eb1a6ff0dc95153bd5f26b6445e`
- `Assets/Scripts/Title/TutorialChoicePopup.cs`: `a3f872a7920992a270ac245141b94250937ddd61e46d6215a39769ccf6572064`
- `Assets/Scripts/Title/TutorialChoicePopup.cs.meta`: `25fc7a8f42be5cd4db5f8fd7735b26a2f81fd611d97ce762f230d34bf27c22f4`
- `Assets/Scripts/Title/TitleMenuController.cs`: `d69a59a97dd8784a5495bb8d1bf70207d0255a40f1e26d0906ecc68f41520da5`
- `Assets/Scripts/Title/TitlePlayHoverHandler.cs`: `b89bca06242817cd2adc27e7afdc2d7601388a4dbeb08cda33540a11f39c147c`
- `Assets/Scripts/System/Localization/DevoryaLocalizationManager.cs`: `9572ac047e79d9c05660668759d7a35185eaa937fd2f18e109666f6cef400cc8`
- `Assets/Scripts/System/Localization/DevoryaLocalizationManager.cs.meta`: `ce31c6792e691e36eba6e12df78a2eed5797d28aa6a85b900de5d660c1e56d4c`
- `Assets/Localization/StringTables/Event_UI Shared Data.asset`: `35ea24d3bd35e601034199d9e0471befaf32a0e50eb4ac70a811f8d16eb3a464`
- `Assets/Localization/StringTables/Event_UI_en.asset`: `bb973ff781237cd588a7d9a24e77ead1b745460347c5ac0ddb72062ca7d458a8`
- `Assets/Localization/StringTables/Event_UI_ko.asset`: `eeb1b2d92ae6acb6294e2e98cfd23aebd5e5e5e4f5c1e90837338a67ca96c092`
- `Assets/Localization/StringTables/Event_UI_ja.asset`: `2ed99d063cfdd3b41d6d77945579eb44662ee457fb6def92de4f4f4c8f5fc9b9`
- `ProjectSettings/TagManager.asset`: `4a3844058b226cc733b720e4ef2409ec0ad5a398e5dbbb77cbd254282b3da9f0`
