using System;
using System.Collections.Generic;
using UnityEngine;

public partial class PieceManager
{
    public PieceAnimationManager RecompositionAnimationManager => GetPieceAnimationManager();
    // Exact database default only; species tags and translated display names are not identity.
    public bool UsesAbsorbedRecompositionVisual(PieceData data) =>
        data != null && data != GetRecompositionDefaultData(data.pieceType);

    public sealed class RecompositionTransaction
    {
        private readonly PieceManager manager;
        private readonly RunStateManager run;
        private readonly Piece piece;
        private readonly PieceData target;
        private readonly Piece.RecompositionState beforePiece;
        private readonly PlayerPieceRuntimeData beforeLink;
        private readonly List<PlayerPieceRuntimeData> beforeRun;
        private readonly Vector2Int position;
        private readonly Vector3 worldPosition;
        private bool applied, wroteRun, finished;
        public bool IsCommitted { get; private set; }
        internal RecompositionTransaction(PieceManager manager, Piece piece, PieceData target, RunStateManager run)
        {
            this.manager=manager; this.piece=piece; this.target=target; this.run=run;
            beforePiece=piece.CaptureRecompositionState(); beforeLink=manager.GetSpawnedRuntimeSourceCopy(piece);
            beforeRun=run.GetPlayerPiecesCopy(); position=piece.GetGridPosition(); worldPosition=piece.transform.position;
        }
        public bool Apply()
        {
            if (finished || applied || piece == null || manager.GetPieceAt(position.x,position.y)!=piece ||
                !SameRoster(beforeRun,run.GetPlayerPiecesCopy())) return false;
            applied=true; // rollback also covers exceptions during visual rebuilding
            piece.ChangePieceData(target,manager.UsesAbsorbedRecompositionVisual(target)); // never AbsorbPiece / AbsorbGeneralSkillsFrom
            manager.RefreshPieceVisual(piece);
            var updated=PlayerPieceRuntimeData.FromPiece(piece);
            updated.formationX=position.x;updated.formationY=position.y;
            manager.spawnedRuntimeSourceByPiece[piece]=updated;
            return true;
        }
        public bool Commit()
        {
            if (finished || !applied || piece==null || run==null ||
                !SameRoster(beforeRun,run.GetPlayerPiecesCopy()) ||
                !manager.TryBuildRecompositionRoster(run,out var roster)) return false;
            var expected=PlayerPieceRuntimeData.FromPiece(piece);
            expected.formationX=position.x; expected.formationY=position.y;
            if (JsonUtility.ToJson(expected)!=JsonUtility.ToJson(manager.GetSpawnedRuntimeSourceCopy(piece))) return false;
            wroteRun=true;
            run.SavePlayerPieces(roster);
            if (!SameRoster(roster,run.GetPlayerPiecesCopy())) return false;
            IsCommitted=true;finished=true;return true;
        }
        public void Rollback()
        {
            if (finished) return;
            finished=true;
            if (wroteRun && run!=null) run.SavePlayerPieces(beforeRun);
            if (manager!=null && piece!=null)
            {
                manager.spawnedRuntimeSourceByPiece[piece]=beforeLink.Clone();
                piece.RestoreRecompositionState(beforePiece);
                piece.transform.position=worldPosition;
                manager.RefreshPieceVisual(piece);
                manager.PlayPieceIdleAnimation(piece);
            }
        }
    }
    private static bool SameRoster(List<PlayerPieceRuntimeData> a,List<PlayerPieceRuntimeData> b)
    {
        if(a==null||b==null||a.Count!=b.Count)return false;
        for(int i=0;i<a.Count;i++)if(JsonUtility.ToJson(a[i])!=JsonUtility.ToJson(b[i]))return false;
        return true;
    }
    private bool TryBuildRecompositionRoster(RunStateManager run,out List<PlayerPieceRuntimeData> roster)
    {
        roster=new List<PlayerPieceRuntimeData>();
        if(run==null||!run.HasPlayerPieceRuntimeData||boardManager==null)return false;
        for(int y=0;y<boardManager.Height;y++)for(int x=0;x<boardManager.Width;x++)
        {
            var p=GetPieceAt(x,y);if(p==null)continue;
            var data=GetSpawnedRuntimeSourceCopy(p);
            if(p.Team!=PieceTeam.Player||data==null||data.pieceData==null)return false;
            data.formationX=x;data.formationY=y;roster.Add(data);
        }
        return roster.Count>0&&roster.Count==run.GetPlayerPieceCount();
    }
    public bool TryBeginRecomposition(Piece piece,PieceData target,out RecompositionTransaction transaction)
    {
        transaction=null;var run=RunStateManager.Instance;
        if(piece==null||target==null||piece.Team!=PieceTeam.Player||piece.CurrentPieceData==null||
            target==piece.CurrentPieceData||target.pieceType!=piece.PieceType||boardManager==null||
            GetPieceAt(piece.X,piece.Y)!=piece||boardManager.GetTile(piece.X,piece.Y)!=piece.CurrentTile||
            GetSpawnedRuntimeSourceCopy(piece)==null||!TryBuildRecompositionRoster(run,out var roster))return false;
        // Reject stale/mismatched run state rather than replacing unrelated player entries.
        var stored=run.GetPlayerPiecesCopy();
        foreach(var item in roster)
        {
            var match=stored.Find(p=>
            {
                // Fresh run entries may not have assigned formation coordinates yet.
                if(!((p.formationX==item.formationX&&p.formationY==item.formationY)||
                     (p.formationX<0&&p.formationY<0)))return false;
                var normalized=p.Clone();normalized.formationX=item.formationX;normalized.formationY=item.formationY;
                return JsonUtility.ToJson(normalized)==JsonUtility.ToJson(item);
            });
            if(match==null)return false;
            stored.Remove(match);
        }
        transaction=new RecompositionTransaction(this,piece,target,run);return true;
    }
}
