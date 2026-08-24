using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// <변경부분> AI가 담당하는 진영의 턴 시작, 후보 생성, 행동 선택을 관리하는 클래스
// 실제 이동과 공격은 BattleManager.TryExecuteBattleAction()에 위임한다.
public class BattleAIManager : MonoBehaviour
{
    [Header("AI Control")]
    [SerializeField] private bool controlEnemyWithAI = false;

    [SerializeField, Min(0f)]
    private float decisionDelay = 0.5f;

    // <변경부분> ChanceAttack이 비정상적으로 계속 발동해
    // Enemy 턴이 무한히 이어지는 상황을 막기 위한 최대 AI 행동 횟수
    [SerializeField, Min(1)]
    private int maxActionsPerEnemyTurn = 10;

    // <변경부분> 현재 StageBattleData에서 전달받은
    // Enemy AI 고유스킬 사용 허용 확률.
    //
    // 기본값 100%는 기존 AI 동작을 그대로 유지한다.
    private float uniqueSkillUseChance =
        100f;

    // <변경부분> 현재 Enemy 턴에서
    // 고유스킬 후보를 사용할 수 있는지 저장한다.
    //
    // Enemy 턴 시작 시 확률을 딱 한 번만 판정하며,
    // 고유스킬 사용 후 재평가나 ChanceAttack 추가 행동에서도
    // 같은 결과를 계속 사용한다.
    private bool allowUniqueSkillThisEnemyTurn =
        true;

    // 실제 전투 규칙과 행동 실행을 담당하는 매니저
    private BattleManager battleManager;

    // <변경부분> 합성 승급 후 Knight/Bishop/Rook의
    // 가상 공격 가능 범위를 평가할 때 사용하는 이동 판정기
    //
    // BattleManager가 사용하는 것과 동일한
    // BattleMoveValidator 컴포넌트를 Inspector에서 연결한다.
    [SerializeField]
    private BattleMoveValidator battleMoveValidator;

    // 매 턴 재사용하는 AI 행동 후보 목록
    private readonly List<BattleAIAction> actionCandidates =
        new List<BattleAIAction>();

    // <변경부분> 최고 점수가 같은 행동 후보들을 저장하는 재사용 목록
    // 매 턴 새로운 List를 만들지 않아 불필요한 GC 할당을 줄인다.
    private readonly List<BattleAIAction> bestActionCandidates =
        new List<BattleAIAction>();

    // <변경부분> AI 행동 후보의 점수를 계산하고
    // 최고 점수 행동을 선택하는 일반 C# 평가기
    private BattleAIActionEvaluator actionEvaluator;

    [Header("AI Debug")]
    [SerializeField]
    private bool logEvaluatedActionScores = true;

    // 같은 Enemy 턴에 AI 코루틴이 중복 실행되는 것을 방지한다.
    private Coroutine enemyTurnRoutine;

    // <변경부분> 현재 스테이지에서 사용할
    // Enemy AI 고유스킬 사용 확률을 적용한다.
    //
    // StageBattleData → BattleSetupManager
    // → BattleManager → BattleAIManager 순서로 전달된다.
    public void SetUniqueSkillUseChance(
        float chancePercent)
    {
        uniqueSkillUseChance =
            Mathf.Clamp(
                chancePercent,
                0f,
                100f
            );

        Debug.Log(
            $"Enemy AI 고유스킬 사용 확률 적용: " +
            $"{uniqueSkillUseChance}%"
        );
    }

    // <변경부분> BattleManager가 전투 시작 시 한 번 호출하는 초기화 함수
    public void Initialize(BattleManager manager)
    {
        // 실제 전투 실행을 담당하는 BattleManager 저장
        battleManager = manager;

        // 합성 승급 공격 기대값을 계산하려면
        // BattleMoveValidator 참조가 필요하다.
        //
        // Inspector 연결이 빠졌더라도 같은 오브젝트 또는
        // 자식 오브젝트에서 한 번 더 검색해 초기화를 시도한다.
        if (battleMoveValidator == null &&
            battleManager != null)
        {
            battleMoveValidator =
                battleManager.GetComponentInChildren<
                    BattleMoveValidator
                >(true);
        }

        if (battleMoveValidator == null)
        {
            Debug.LogWarning(
                "Enemy AI 초기화 경고: " +
                "BattleMoveValidator가 연결되지 않아 " +
                "합성 승급 공격 기대값을 계산할 수 없습니다."
            );
        }

        // <변경부분> 평가기에 BattleManager와
        // BattleMoveValidator를 함께 전달한다.
        //
        // BattleManager:
        // 일반 행동 후 King 및 행동 기물 위험도 평가
        //
        // BattleMoveValidator:
        // 합성 후 Knight/Bishop/Rook 가상 공격 범위 평가
        actionEvaluator =
            new BattleAIActionEvaluator(
                battleManager,
                battleMoveValidator
            );
    }



    // <변경부분> 턴이 시작될 때 BattleManager가 호출한다.
    // Enemy AI가 비활성화되어 있으면 기존 수동 조작을 그대로 유지한다.
    public void HandleTurnStarted(
    BattleTurn startedTurn)
    {
        if (controlEnemyWithAI == false)
        {
            return;
        }

        if (startedTurn !=
            BattleTurn.Enemy)
        {
            return;
        }

        if (battleManager == null ||
            battleManager.IsBattleEnded)
        {
            return;
        }

        if (enemyTurnRoutine != null)
        {
            StopCoroutine(
                enemyTurnRoutine
            );
        }

        // <변경부분> Enemy 턴이 시작되는 순간
        // StageBattleData에서 받은 확률을 한 번만 판정한다.
        //
        // 같은 Enemy 턴 안에서 고유스킬 사용 후 재평가하거나
        // ChanceAttack 추가 행동이 발생해도 다시 랜덤을 굴리지 않는다.
        allowUniqueSkillThisEnemyTurn =
            RollUniqueSkillUseForCurrentEnemyTurn();

        Debug.Log(
            $"Enemy AI 이번 턴 고유스킬 사용 여부: " +
            $"{allowUniqueSkillThisEnemyTurn} / " +
            $"스테이지 확률 {uniqueSkillUseChance}%"
        );

        enemyTurnRoutine =
            StartCoroutine(
                ExecuteEnemyTurnRoutine()
            );
    }

    // <변경부분> 현재 Enemy 턴에
    // 고유스킬을 사용할 수 있을지 한 번만 추첨한다.
    private bool RollUniqueSkillUseForCurrentEnemyTurn()
    {
        // 0%는 랜덤 호출 없이 확실하게 차단한다.
        if (uniqueSkillUseChance <= 0f)
        {
            return false;
        }

        // 100%는 랜덤 호출 없이 기존 AI를 그대로 허용한다.
        if (uniqueSkillUseChance >= 100f)
        {
            return true;
        }

        float rolledValue =
            Random.Range(
                0f,
                100f
            );

        return
            rolledValue <
            uniqueSkillUseChance;
    }

    // <변경부분> 이번 Enemy 턴에서 고유스킬 사용이 허용되지 않았다면
    // 생성된 행동 후보 중 UniqueSkill만 제거한다.
    //
    // 일반 이동과 공격 후보는 그대로 유지한다.
    private void RemoveUniqueSkillActionsForCurrentTurn(
        List<BattleAIAction> actions)
    {
        if (allowUniqueSkillThisEnemyTurn)
        {
            return;
        }

        if (actions == null ||
            actions.Count == 0)
        {
            return;
        }

        // 뒤에서부터 제거하여
        // List 인덱스 변경 문제를 방지한다.
        for (int i = actions.Count - 1;
             i >= 0;
             i--)
        {
            BattleAIAction action =
                actions[i];

            if (action == null)
            {
                continue;
            }

            if (action.ActionType !=
                BattleAIActionType.UniqueSkill)
            {
                continue;
            }

            actions.RemoveAt(
                i
            );
        }
    }

    // <변경부분> 현재 Enemy 진영을 AI가 조작하는지 반환한다.
    // BattleManager가 사람의 클릭 입력을 막을 때 사용한다.
    public bool IsEnemyControlledByAI()
    {
        return controlEnemyWithAI;
    }

    // <변경부분> Enemy 턴의 행동을 실행하고,
    // ChanceAttack 추가 행동이 발생하면 같은 코루틴 안에서
    // 행동 종료를 기다린 뒤 추가 행동 기물로 다시 판단한다.
    private IEnumerator ExecuteEnemyTurnRoutine()
    {
        int executedActionCount = 0;

        while (battleManager != null &&
               battleManager.IsBattleEnded == false &&
               battleManager.CurrentTurn ==
               BattleTurn.Enemy)
        {
            // <변경부분> Event Sequence가 Enemy AI 일시정지를
            // 요청하고 있는 동안에는 AI 행동 후보를 생성하지 않고 기다린다.
            //
            // 코루틴 자체는 종료하지 않으므로
            // Event Sequence가 끝난 뒤 현재 Enemy 턴에서
            // 기존 AI 행동을 그대로 이어서 실행할 수 있다.
            while (battleManager != null &&
                   battleManager.IsBattleEnded == false &&
                   battleManager.CurrentTurn ==
                       BattleTurn.Enemy &&
                   battleManager.ShouldPauseEnemyAIForEvent)
            {
                yield return null;
            }

            // 대기 중 전투가 종료됐거나
            // 다른 진영 턴으로 변경됐다면 AI 코루틴 종료
            if (battleManager == null ||
                battleManager.IsBattleEnded ||
                battleManager.CurrentTurn !=
                    BattleTurn.Enemy)
            {
                enemyTurnRoutine =
                    null;

                yield break;
            }

            // 첫 행동과 추가 행동 사이에 판단 지연을 둔다.
            if (decisionDelay > 0f)
            {
                yield return
                    new WaitForSeconds(
                        decisionDelay
                    );
            }

            // <변경부분> 판단 대기 시간 중 Event Sequence가 시작됐을 수도 있으므로
            // 실제 행동 후보 생성 직전에 다시 AI 일시정지 상태를 확인한다.
            while (battleManager != null &&
                   battleManager.IsBattleEnded == false &&
                   battleManager.CurrentTurn ==
                       BattleTurn.Enemy &&
                   battleManager.ShouldPauseEnemyAIForEvent)
            {
                yield return null;
            }

            // 이전 이동, 공격 또는 스킬 연출이
            // 완전히 끝날 때까지 기다린다.
            while (battleManager != null &&
                   battleManager.IsActionAnimating)
            {
                yield return null;
            }

            // 대기 중 전투가 끝나거나 턴이 변경됐다면 종료한다.
            if (battleManager == null ||
                battleManager.IsBattleEnded ||
                battleManager.CurrentTurn !=
                BattleTurn.Enemy)
            {
                enemyTurnRoutine = null;
                yield break;
            }

            // <변경부분> ChanceAttack 추가 행동 중인지 확인한다.
            // 추가 행동 중이라면 해당 기물만 다시 행동해야 한다.
            Piece bonusActionPiece =
                battleManager
                    .GetChanceAttackBonusPiece();

            if (bonusActionPiece != null)
            {
                battleManager
                    .GenerateAIActionsForPiece(
                        bonusActionPiece,
                        actionCandidates
                    );
            }
            else
            {
                // 일반 Enemy 턴 첫 행동에서는
                // Enemy 전체 기물의 후보를 생성한다.
                battleManager.GenerateAIActions(
                    PieceTeam.Enemy,
                    actionCandidates
                );
            }

            // <변경부분> 이번 Enemy 턴의
            // 스테이지 고유스킬 사용 확률 판정에 실패했다면
            // 고유스킬 후보만 제거한다.
            //
            // 이동과 공격 후보는 기존 AI 그대로 유지한다.
            RemoveUniqueSkillActionsForCurrentTurn(
                actionCandidates
            );

            // 행동 가능한 후보가 없다면 현재 상황에 따라 처리한다.
            if (actionCandidates.Count == 0)
            {
                if (bonusActionPiece != null)
                {
                    Debug.LogWarning(
                        "Enemy AI ChanceAttack 추가 행동 실패: " +
                        "추가 행동 기물의 합법 행동 후보가 없습니다."
                    );

                    // 다른 Enemy 기물을 대신 움직이지 않고
                    // 추가 행동 상태를 정리한 뒤 Enemy 턴을 종료한다.
                    battleManager
                        .FinishEnemyAIChanceAttackTurn();
                }
                else
                {
                    Debug.LogWarning(
                        "Enemy AI 행동 실행 실패: " +
                        "생성된 행동 후보가 없습니다."
                    );

                    battleManager.ResolveNoActionableTurn(
                        PieceTeam.Enemy
                    );
                }

                enemyTurnRoutine = null;
                yield break;
            }

            if (actionEvaluator == null)
            {
                Debug.LogWarning(
                    "Enemy AI 행동 선택 실패: " +
                    "BattleAIActionEvaluator가 초기화되지 않았습니다."
                );

                // 추가 행동 상태에서 평가기가 없다면
                // Enemy 턴이 멈추지 않도록 정리한다.
                if (bonusActionPiece != null)
                {
                    battleManager
                        .FinishEnemyAIChanceAttackTurn();
                }

                enemyTurnRoutine = null;
                yield break;
            }

            // 모든 후보의 점수를 계산한다.
            actionEvaluator.EvaluateActions(
                actionCandidates
            );

            if (logEvaluatedActionScores)
            {
                actionEvaluator
                    .DebugLogEvaluatedActions(
                        actionCandidates
                    );
            }

            // 최고 점수 후보 중 하나를 선택한다.
            BattleAIAction selectedAction =
                actionEvaluator.SelectBestAction(
                    actionCandidates,
                    bestActionCandidates
                );

            if (selectedAction == null)
            {
                // <변경부분>
                // 후보 목록 자체는 존재하지만 모든 후보가 float.MinValue 등으로
                // 실행 불가 판정을 받은 경우에도 이 경로로 들어올 수 있다.
                //
                // 단순히 AI 코루틴만 종료하면 CurrentTurn이 Enemy인 상태로
                // 남을 수 있으므로 현재 행동 형태에 맞게 턴 상태까지 정리한다.
                Debug.LogWarning(
                    "Enemy AI 행동 선택 실패: " +
                    "실행 가능한 최고 점수 행동이 없습니다."
                );

                if (bonusActionPiece != null)
                {
                    // ChanceAttack 추가 행동 중 실행 가능한 행동이 사라졌다면
                    // 다른 Enemy 기물로 행동을 넘기지 않고 추가 행동 상태를 종료한다.
                    battleManager
                        .FinishEnemyAIChanceAttackTurn();
                }
                else
                {
                    // 일반 Enemy 행동에서 모든 후보가 실행 불가라면
                    // 기존 '후보가 0개인 경우'와 동일한 경로로 처리하여
                    // Enemy 턴이 멈춘 채 남는 것을 방지한다.
                    battleManager
                        .ResolveNoActionableTurn(
                            PieceTeam.Enemy
                        );
                }

                enemyTurnRoutine = null;
                yield break;
            }

            Debug.Log(
                $"Enemy AI 행동 선택: " +
                $"{selectedAction.ActionType} / " +
                $"{selectedAction.ActingPiece.PieceType} / " +
                $"{selectedAction.SourcePosition} → " +
                $"{selectedAction.TargetPosition} / " +
                $"점수 {selectedAction.Score} / " +
                $"동점 후보 {bestActionCandidates.Count}개 / " +
                $"추가 행동 여부 {bonusActionPiece != null}"
            );

            // <변경부분> 선택된 행동 종류에 따라
            // 일반 이동·공격과 고유스킬의 실행 경로를 분리한다.
            bool actionStarted = false;

            switch (selectedAction.ActionType)
            {
                case BattleAIActionType.Move:
                case BattleAIActionType.Attack:
                    actionStarted =
                        battleManager
                            .TryExecuteBattleAction(
                                selectedAction.ActingPiece,
                                selectedAction.TargetPosition
                            );
                    break;

                case BattleAIActionType.UniqueSkill:
                    actionStarted =
                        battleManager
                            .TryExecuteAIUniqueSkill(
                                selectedAction
                            );
                    break;
            }

            if (actionStarted == false)
            {
                Debug.LogWarning(
                    "Enemy AI 행동 실행 실패: " +
                    "선택한 행동을 시작하지 못했습니다."
                );

                // 추가 행동 실행에 실패했다면
                // Enemy 턴이 그대로 멈추지 않도록 정리한다.
                if (bonusActionPiece != null)
                {
                    battleManager
                        .FinishEnemyAIChanceAttackTurn();
                }

                enemyTurnRoutine = null;
                yield break;
            }

            // <변경부분> 실제 위치를 변경하는 이동과 공격만
            // 왕복 이동 및 최근 방문 위치 기록에 저장한다.
            //
            // 젤루 합성은 현재 좌표에서 사용하는 고유스킬이므로
            // 이동 이력에 기록하지 않는다.
            if (selectedAction.ActionType ==
                    BattleAIActionType.Move ||
                selectedAction.ActionType ==
                    BattleAIActionType.Attack)
            {
                actionEvaluator
                    .SetPreviousExecutedAction(
                        selectedAction
                    );
            }

            executedActionCount++;

            // StartCoroutine 직후에는 BattleManager의 행동 코루틴이
            // 아직 첫 프레임을 실행하지 않아 IsActionAnimating이
            // false일 수 있으므로 반드시 한 프레임 기다린다.
            yield return null;

            // 현재 행동 애니메이션과 전투 후처리가
            // 모두 끝날 때까지 기다린다.
            while (battleManager != null &&
                   battleManager.IsActionAnimating)
            {
                yield return null;
            }

            // 일반 행동이라면 BattleManager.EndTurn()이 실행돼
            // CurrentTurn이 Player로 변경되므로 while 반복이 끝난다.
            //
            // ChanceAttack이 발동했다면 Enemy 턴과
            // chanceAttackBonusPiece가 유지되므로 다음 반복에서
            // 같은 기물의 추가 행동 후보를 다시 평가한다.
            if (battleManager == null ||
                battleManager.IsBattleEnded ||
                battleManager.CurrentTurn !=
                BattleTurn.Enemy)
            {
                enemyTurnRoutine = null;
                yield break;
            }

            // <변경부분> 고유스킬, 일반 이동, 공격을 포함하여
            // Enemy 한 턴에 실제로 시작한 모든 행동 횟수를 제한한다.
            //
            // 고유스킬 재평가 continue보다 먼저 검사해야
            // 스킬 내부 실패나 비정상 후보 반복이 발생해도
            // 안전 제한을 우회하지 않는다.
            if (executedActionCount >=
                Mathf.Max(
                    1,
                    maxActionsPerEnemyTurn
                ))
            {
                Debug.LogWarning(
                    $"Enemy AI 한 턴 최대 행동 횟수 도달: " +
                    $"{executedActionCount}회 / " +
                    $"Enemy 턴을 안전하게 종료합니다."
                );

                battleManager
                    .FinishEnemyAIChanceAttackTurn();

                enemyTurnRoutine = null;
                yield break;
            }

            // <변경부분> 고유스킬은 Enemy 턴의 일반 이동·공격 사용권을
            // 즉시 종료하지 않는다.
            //
            // 합성으로 기물 종류와 보드 배치가 변경됐으므로
            // 다음 반복에서 전체 Enemy 행동 후보를 다시 생성하고 평가한다.
            //
            // 정상적인 젤루 합성은:
            // 1. hasUsedUniqueSkillThisTurn 적용
            // 2. 합성 Pawn이 상위 기물로 승급
            // 3. 기존 JelluSynthesis 후보 소멸
            //
            // 순서로 처리되므로 다음 반복에서는
            // 같은 합성을 다시 선택하지 않고 이동 또는 공격을 판단한다.
            if (selectedAction.ActionType ==
                BattleAIActionType.UniqueSkill)
            {
                continue;
            }

            // Enemy 턴이 유지됐지만 추가 행동 기물이 없다면
            // 예상하지 못한 상태이므로 반복하지 않고 안전 종료한다.
            if (battleManager
                    .GetChanceAttackBonusPiece() ==
                null)
            {
                Debug.LogWarning(
                    "Enemy AI 행동 후 Enemy 턴이 유지됐지만 " +
                    "ChanceAttack 추가 행동 기물이 없습니다."
                );

                enemyTurnRoutine = null;
                yield break;
            }
        }

        enemyTurnRoutine = null;
    }
}
