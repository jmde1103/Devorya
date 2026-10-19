# 공격불가 / 행동불가

기준 커밋: 6d0a25a. 구현 브랜치: codex/status-effect-restrictions.

| 효과 | 기본 지속 | 동작 |
|---|---|---|
| 공격불가 (AttackDisabled) | 1턴 | 빈 칸 이동은 허용하고 상대/중립 기물을 잡는 이동은 차단 |
| 행동불가 (ActionDisabled) | 1턴 | 이동·공격 및 능동 고유스킬 사용 차단 |

두 효과는 카테고리 '상태효과'의 기존 UI/툴팁을 사용한다.
지속 시간은 영향을 받은 진영의 턴 종료 시 감소한다. 상대 턴에 부여한 1턴 효과가 대상 진영의 턴 시작 직전에 사라지지 않는다.
기존 Defence, Degeneration, Breakthrough의 턴 시작 만료 규칙은 그대로 유지한다.
재부여하면 기존 방식대로 지속 시간을 갱신한다. 기본 최대 중첩은 1이다.

## 제작 시 사용

`Assets/Devorya/Data/StatusEffect/` 아래의 `StatusEffect_AttackDisabled.asset`, `StatusEffect_ActionDisabled.asset`을 사용한다.
두 에셋은 기존 `StatusEffectDatabase.asset`에 등록되어 있다.
아이템의 기존 ApplyStatusEffect 설정에 해당 에셋을 지정하거나, 코드에서 `piece.AddStatusEffect(data)`로 부여할 수 있다.
특정 적/아이템에 자동 부여하는 규칙은 추가하지 않았다.
이름과 설명은 한국어 원문을 제공하며, 번역은 기존 StatusEffectData 인스펙터의 Localization 작업 흐름으로 추가한다.
아이콘은 기존 UI_Status 공용 스프라이트를 사용한다. 전용 아이콘으로 교체할 수 있다.

## 코드 변경

- enum 끝에 새 값을 추가하여 기존 저장된 효과 번호 유지.
- Piece의 기존 CanMove 값은 유지하고 CanMoveThisTurn/CanAttackThisTurn으로 임시 제한 판정.
- 플레이어 실행, AI 후보, 이동 경로, 합성 공격 예상, 위협 평가에 같은 제한 적용.
- 제한된 상대 기물도 경로를 막는다. 상대 기물을 통과하는 이동은 허용하지 않는다.
- 행동불가는 고유스킬 직접 호출 및 UI/AI 경로에서도 확인.
- 일시적인 제한만으로 NoActionablePieces 패배를 발생시키지 않도록 처리.
- Scene, Prefab, 기존 SerializeField, UnityEvent 연결, Spine, URP 설정은 변경하지 않음.

## 검증

Unity 6000.4.0f1 배치 실행 종료 코드 0.
504개 기물 종류/진영/배치 조합, 총 5,564개 assertion 통과.
빈 칸 이동 보존, 공격 제한, 행동 제한, AI, 실제 행동 호출 거부, 합성 공격 예상, 위협 판정, 턴 만료, 재부여, 기존 이동 불가 플래그 유지 및 데이터 등록을 검사했다.
별도 Roslyn 게임 코드 컴파일도 통과했다.

재실행: Unity 메뉴 `Devorya > Validation > Check Action Restriction Effects`.
결과: `Logs/status-restrictions-results.txt`.
화면 없는 테스트 오브젝트 검증이며, 실제 전투 씬의 전체 플레이와 최종 아이콘 배치는 수동 확인이 남아 있다.

## 병합

다른 기능 브랜치에서 이 브랜치를 병합할 수 있다. 특히 Piece.cs, BattleManager.cs, BattleMoveValidator.cs 및 StatusEffectDatabase.asset을 양쪽에서 수정했다면 충돌 내용을 검토해야 한다.
원본 작업 폴더와 분리된 worktree에서 구현했으며 원본 브랜치를 전환하지 않았다.
