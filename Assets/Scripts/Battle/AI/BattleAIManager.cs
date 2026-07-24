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

    // 실제 전투 규칙과 행동 실행을 담당하는 매니저
    private BattleManager battleManager;

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

    // <변경부분> BattleManager가 전투 시작 시 한 번 호출하는 초기화 함수
    public void Initialize(BattleManager manager)
    {
        // 실제 전투 실행을 담당하는 BattleManager 저장
        battleManager = manager;

        // <변경부분> AI 행동 점수 평가기가
        // 가상 King 위험도 판정을 요청할 수 있도록
        // BattleManager 참조를 전달한다.
        actionEvaluator =
            new BattleAIActionEvaluator(
                battleManager
            );
    }

    // <변경부분> 턴이 시작될 때 BattleManager가 호출한다.
    // Enemy AI가 비활성화되어 있으면 기존 수동 조작을 그대로 유지한다.
    public void HandleTurnStarted(BattleTurn startedTurn)
    {
        if (controlEnemyWithAI == false)
        {
            return;
        }

        if (startedTurn != BattleTurn.Enemy)
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
            StopCoroutine(enemyTurnRoutine);
        }

        enemyTurnRoutine =
            StartCoroutine(ExecuteEnemyTurnRoutine());
    }

    // <변경부분> 현재 Enemy 진영을 AI가 조작하는지 반환한다.
    // BattleManager가 사람의 클릭 입력을 막을 때 사용한다.
    public bool IsEnemyControlledByAI()
    {
        return controlEnemyWithAI;
    }

    // <변경부분> Enemy 턴의 모든 행동 후보를 평가하고
    // 최고 점수 행동을 선택해 공용 전투 실행 함수에 전달한다.
    private IEnumerator ExecuteEnemyTurnRoutine()
    {
        if (decisionDelay > 0f)
        {
            yield return new WaitForSeconds(decisionDelay);
        }

        // 다른 연출이 끝날 때까지 기다린다.
        while (battleManager != null &&
               battleManager.IsActionAnimating)
        {
            yield return null;
        }

        if (battleManager == null ||
            battleManager.IsBattleEnded ||
            battleManager.CurrentTurn != BattleTurn.Enemy)
        {
            enemyTurnRoutine = null;
            yield break;
        }

        // Enemy 진영의 현재 합법 행동 후보 생성
        battleManager.GenerateAIActions(
            PieceTeam.Enemy,
            actionCandidates
        );

        // 행동 가능한 후보가 없다면 승패 조건을 다시 확인한다.
        if (actionCandidates.Count == 0)
        {
            Debug.LogWarning(
                "Enemy AI 행동 실행 실패: 생성된 행동 후보가 없습니다."
            );

            battleManager.ResolveNoActionableTurn(
                PieceTeam.Enemy
            );

            enemyTurnRoutine = null;
            yield break;
        }

        // <변경부분> AI 행동 평가기가 초기화되지 않았다면
        // 행동을 선택할 수 없으므로 턴 진행을 중단한다.
        if (actionEvaluator == null)
        {
            Debug.LogWarning(
                "Enemy AI 행동 선택 실패: " +
                "BattleAIActionEvaluator가 초기화되지 않았습니다."
            );

            enemyTurnRoutine = null;
            yield break;
        }

        // <변경부분> 생성된 모든 이동 및 공격 후보의 점수를 계산한다.
        actionEvaluator.EvaluateActions(
            actionCandidates
        );

        // 개발 중에는 각 행동 후보의 점수를 Console에서 확인한다.
        if (logEvaluatedActionScores)
        {
            actionEvaluator.DebugLogEvaluatedActions(
                actionCandidates
            );
        }

        // <변경부분> 최고 점수 행동들을 추려내고,
        // 같은 점수의 행동 중 하나를 랜덤으로 선택한다.
        BattleAIAction selectedAction =
            actionEvaluator.SelectBestAction(
                actionCandidates,
                bestActionCandidates
            );

        // 유효한 행동을 선택하지 못했다면 실행하지 않는다.
        if (selectedAction == null)
        {
            Debug.LogWarning(
                "Enemy AI 행동 선택 실패: " +
                "최고 점수 행동을 선택하지 못했습니다."
            );

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
            $"동점 후보 {bestActionCandidates.Count}개"
        );

        // 실제 이동 및 공격은 BattleManager의 공용 실행 함수 사용
        bool actionStarted =
     battleManager.TryExecuteBattleAction(
         selectedAction.ActingPiece,
         selectedAction.TargetPosition
     );

        if (actionStarted == false)
        {
            Debug.LogWarning(
                "Enemy AI 행동 실행 실패: 선택한 행동을 시작하지 못했습니다."
            );
        }
        else
        {
            // <변경부분> 실제 행동 실행이 시작된 경우에만
            // 다음 Enemy 턴의 왕복 이동 판정용 기록으로 저장한다.
            actionEvaluator.SetPreviousExecutedAction(
                selectedAction
            );
        }

        enemyTurnRoutine = null;
    }
}
