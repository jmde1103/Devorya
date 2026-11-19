using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class EventSceneRecompositionCandidateController
{
    private PieceManager.RecompositionTransaction transaction;
    private Piece.RecompositionState previewBefore;
    private Candidate executionCandidate;
    private Piece executionSource;
    private PieceAnimationManager executionAnimations;
    private Coroutine execution;
    private readonly List<Coroutine> executionLanes = new List<Coroutine>();
    private Exception executionFailure;
    public bool IsProcessing => processing;
    public string LastExecutionResult { get; private set; }

    private static PieceSpineAnimationController SpineOf(Piece piece)
    {
        var visual=piece!=null?piece.GetComponent<PieceVisualController>():null;
        return visual!=null?visual.CurrentSpineAnimationController:null;
    }
    private static bool CanBorn(PieceData data,PieceTeam team,bool absorbed)
    {
        var prefab=data!=null?data.GetSpineVisualPrefab(team,absorbed):null;
        var spine=prefab!=null?prefab.GetComponentInChildren<PieceSpineAnimationController>(true):null;
        return spine!=null && spine.HasRecompositionAnimations(false);
    }
    private bool BeginRecomposition()
    {
        var manager=formation!=null?formation.RecompositionPieceManager:null;
        var first=SpineOf(source);var second=SpineOf(selectedCandidate.Piece);
        executionAnimations=manager!=null?manager.RecompositionAnimationManager:null;
        if(manager==null || executionAnimations==null || first==null || second==null ||
            !first.HasRecompositionAnimations(true)||!second.HasRecompositionAnimations(true)||
            !CanBorn(selectedCandidate.Data,PieceTeam.Player,manager.UsesAbsorbedRecompositionVisual(selectedCandidate.Data))||!CanBorn(source.CurrentPieceData,PieceTeam.Enemy,false)||
            !manager.TryBeginRecomposition(source,selectedCandidate.Data,out transaction))
        {
            LastExecutionResult="Rejected: runtime source, position or animation reference is invalid.";
            Debug.LogWarning("재구성 시작 실패: 런 저장 데이터와 애니메이션 연결을 확인하세요.",this);return false;
        }
        executionSource=source;executionCandidate=selectedCandidate;
        previewBefore=executionCandidate.Piece.CaptureRecompositionState();
        executionFailure=null;LastExecutionResult="Running";
        StopSelectionAnimations();SetProcessing(true);formation.SetRecompositionLock(true);
        execution=StartCoroutine(ExecuteGuarded());return true;
    }
    private IEnumerator ExecuteGuarded()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Execute());
        while(stack.Count>0)
        {
            object next=null;bool moved=false;
            try { moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current; }
            catch(Exception exception) { executionFailure=exception; }
            if(executionFailure!=null)break;
            if(!moved){stack.Pop();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        if(executionFailure!=null)
        {
            Debug.LogWarning("재구성 취소 및 복구: "+executionFailure.Message,this);
            LastExecutionResult="Rolled back: "+executionFailure.Message;
            execution=null;CancelRecomposition();
        }
    }
    private sealed class Lane { public bool done; }
    private IEnumerator RunLane(IEnumerator routine,Lane lane)
    {
        var stack=new Stack<IEnumerator>();stack.Push(routine);
        while(stack.Count>0)
        {
            object next=null;bool moved=false;
            try { moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current; }
            catch(Exception exception){executionFailure=exception;break;}
            if(!moved){stack.Pop();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        lane.done=true;
    }
    private IEnumerator Together(params IEnumerator[] routines)
    {
        var lanes=new List<Lane>();float elapsed=0;
        foreach(var routine in routines)
        {
            var lane=new Lane();lanes.Add(lane);
            executionLanes.Add(StartCoroutine(RunLane(routine,lane)));
        }
        while(lanes.Exists(l=>!l.done))
        {
            if(executionFailure!=null)throw executionFailure;
            if(executionSource==null || executionCandidate.Piece==null)throw new InvalidOperationException("Piece removed during animation");
            // A generous scaled-time watchdog does not abort normal clips or a paused game.
            elapsed+=Time.deltaTime;
            if(elapsed>120)throw new TimeoutException("Animation did not finish");
            yield return null;
        }
        if(executionFailure!=null)throw executionFailure;
        executionLanes.Clear();
    }
    private IEnumerator Impact()
    {
        yield return new WaitForSeconds(executionAnimations.RecompositionImpactDelay);
        executionAnimations.PlayRecompositionImpact(executionSource.transform.position,executionCandidate.Piece.transform.position);
    }
    private IEnumerator Execute()
    {
        var manager=formation.RecompositionPieceManager;
        var oldData=executionSource.CurrentPieceData;
        var first=SpineOf(executionSource);var second=SpineOf(executionCandidate.Piece);
        // Start both in the same frame. Spine motion only; each root stays on its own tile.
        yield return Together(first.PlayAbsorbRoutine(),second.PlayAbsorbRoutine());
        yield return Together(first.PlayDownAbsorbToIdleRoutine(),second.PlayDownAbsorbToIdleRoutine(),Impact());
        if(!transaction.Apply())throw new InvalidOperationException("Data apply rejected");
        executionCandidate.Piece.ChangePieceData(oldData,false);
        manager.RefreshPieceVisual(executionCandidate.Piece);
        executionSource.SetTypeIconSelected(false);executionCandidate.Piece.SetTypeIconSelected(false);
        executionCandidate.Tile.HideHighlight();
        if(SpineOf(executionSource)==null||SpineOf(executionCandidate.Piece)==null)throw new InvalidOperationException("Born visual missing");
        yield return Together(executionAnimations.PlayPieceBornAnimation(executionSource),
            executionAnimations.PlayPieceBornAnimation(executionCandidate.Piece));
        if(!transaction.Commit())throw new InvalidOperationException("RunState read-back verification failed");
        transaction=null;previewBefore=null;executionCandidate=null;executionSource=null;execution=null;
        LastExecutionResult="Committed";
        SetProcessing(false);formation.SetRecompositionLock(false);
        formation.RefreshAfterRecomposition();
    }
    public void CancelRecomposition()
    {
        if(transaction==null && execution==null)return;
        if(execution!=null)StopCoroutine(execution);
        execution=null;
        foreach(var lane in executionLanes)if(lane!=null)StopCoroutine(lane);
        executionLanes.Clear();
        try
        {
            transaction?.Rollback();
            if(executionCandidate!=null && executionCandidate.Piece!=null && previewBefore!=null)
            {
                executionCandidate.Piece.RestoreRecompositionState(previewBefore);
                if(formation!=null && formation.RecompositionPieceManager!=null)
                {
                    formation.RecompositionPieceManager.RefreshPieceVisual(executionCandidate.Piece);
                    formation.RecompositionPieceManager.PlayPieceIdleAnimation(executionCandidate.Piece);
                }
            }
        }
        finally
        {
            transaction=null;previewBefore=null;executionCandidate=null;executionSource=null;
            if(LastExecutionResult=="Running")LastExecutionResult="Canceled and rolled back";
            SetProcessing(false);if(formation!=null)formation.SetRecompositionLock(false);
            RefreshCandidateIcons();RefreshButton();
        }
    }
}
