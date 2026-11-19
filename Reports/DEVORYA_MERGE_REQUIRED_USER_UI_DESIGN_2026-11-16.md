# 필수 병합 인수인계 — 2026-11-16 사용자 재구성 UI 디자인

이 문서는 향후 DEVORYA 본 프로젝트 병합 시 반드시 확인합니다. 이번 작업에서는 병합하지 않았습니다.

## 디자인 기준
고정 Codex 프로젝트 C:/Users/zntif/.codex/worktrees/shop-final-1105/Devorya (codex/absorption-catalog)에 사용자가 직접 저장한 2026-11-16 UI가 기준입니다. 이후 사용자가 더 수정했다면 그 최신 상태가 우선이며, 이 백업으로 되돌리지 않습니다.

기준 스냅샷과 SHA256: Reports/User_UI_Design_Baseline_2026-11-16_AbilityHidden/manifest.json
같은 폴더의 Assets 하위에 작업 시작 시점 Scene/Prefab/스크립트/.meta를 보존했습니다. 런타임 스크립트는 이번 Ability 없음 처리 이후의 최신 작업본과 함께 통합해야 하며, 백업 스크립트로 덮어쓰지 않습니다.

## 보존 대상
- Assets/Scenes/EventScene.unity (씬 인스턴스 Override 포함)
- Assets/Prefabs/Run/Recomposition/RecompositionTooltipUI.prefab
- Assets/Prefabs/Run/Recomposition/RecompositionStatusEffectCard.prefab
- RecompositionTooltipUI.cs / RecompositionStatusEffectCard.cs / RecompositionVerticalPanel.cs 및 .meta
- 후보 정보 생성, 선택·이동·글리치·재구성과 연결된 최신 코드

Root/Header/Ability/Status 패널 및 카드 내부의 Position, Width/Height, Anchor/Pivot/Scale, 아이콘, 배경 Sprite/색상, TMP Font Size/Auto Size Min·Max/Margin/Alignment/Spacing/색상/Font Style을 사용자 디자인 기준으로 유지합니다.

## 병합 절차
1. 병합 직전 본 개발 브랜치와 Codex의 최신 HEAD, 미커밋 파일, Scene/Prefab 저장 여부를 다시 확인합니다.
2. 위 파일의 현재 저장본을 새로 백업하고, 이 기준 스냅샷 이후 사용자 변경도 확인합니다.
3. Scene에만 저장된 Prefab Instance Override를 포함하여 비교합니다. 원본 프리팹만 비교해서 동일하다고 판단하지 않습니다.
4. 본 프로젝트의 다른 최신 기능과 비교하여 필요한 변경만 선별 통합합니다. 전체 파일 덮어쓰기, 일괄 Override Revert/Apply, 프리팹 재생성으로 사용자 디자인을 지우지 않습니다.
5. 충돌에서는 사용자 UI 디자인을 보존하고, 다른 기능의 필수 변경을 그 위에 반영합니다. 새로운 사용자 편집이 있으면 과거 기본값이나 이 백업으로 되돌리지 않습니다.
6. 병합 전후 배치·폰트·이미지·GUID·Override를 비교하고 Unity에서 확인합니다.
7. 실제 글자 기반 높이 확장/원래 크기 복원, Ability→상태효과 최소 간격, Ability 없음 숨김/상태효과 Header 아래 표시, 후보 전환 및 KR/EN/JA를 확인합니다.
8. 본 프로젝트의 최신 기능과 미커밋 작업을 보호합니다. 사용자가 명시적으로 요청한 병합 범위 밖의 커밋·Push·초기화는 하지 않습니다.

## 이번 작업 결과 (2026-11-16)
- 실행 코드 변경: EventSceneRecompositionCandidateController.Tooltip.cs, RecompositionTooltipUI.cs 두 파일.
- PieceData.uniqueSkill == UniqueSkillType.None이면 Ability 섹션을 만들지 않으며 AbilityPanel 전체를 SetActive(false) 처리합니다. 표시 문구는 비교하지 않습니다.
- Ability 없는 상태에서 별도 상태효과 섹션이 전달되면 실제 카드 배경을 Header 배경 아래 최소 간격에 배치합니다. Ability 복귀 및 Hide 시 기본 좌표를 기준으로 다시 계산/복원합니다.
- Unity 컴파일 및 Play Mode 집중 검사: NO_ABILITY_PASS checks=31, 종료 코드 0 (Logs/no-ability-ui-checks.log).
- 실제 Devorya Pawn의 None enum, 실제 Ability 기물, 없음→있음→없음, 재오픈, KR/EN/JA 및 좌표 복원을 검사했습니다.
- 현재 PieceData 정보 경로에는 Ability와 독립된 상태효과 목록이 없으므로, 상태효과-only 경우는 기존 TooltipViewData에 상태효과 섹션을 넣은 임시 테스트로 검증했습니다. 새 데이터 필드는 만들지 않았습니다.
- 원본 Scene/Prefab/기존 .meta는 스냅샷과 동일합니다. 원본 에셋 저장, Override 변경, 본 프로젝트 병합은 수행하지 않았습니다.
