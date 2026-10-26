using System;
using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

public partial class PieceManager
{
    static void SetTeleportAlpha(SkeletonAnimation animation,float alpha)
    {
        var color=animation.Skeleton.GetColor();color.a=alpha;animation.Skeleton.SetColor(color);
    }
    List<Piece> TeleportPieces(PieceTeam team)
    {
        var result=new List<Piece>();
        if(boardManager==null || pieces==null)return result;
        for(int y=0;y<boardManager.Height;y++)for(int x=0;x<boardManager.Width;x++)
        {
            var p=GetPieceAt(x,y);
            if(p!=null && p.Team==team && p.HasStatusEffect(StatusEffectType.Teleport))result.Add(p);
        }
        return result;
    }
    public bool HasTurnEndTeleport(PieceTeam team) => TeleportPieces(team).Count>0;
    List<Vector2Int> TeleportTiles()
    {
        var result=new List<Vector2Int>();
        for(int y=0;y<boardManager.Height;y++)for(int x=0;x<boardManager.Width;x++)
        {
            var tile=boardManager.GetTile(x,y);
            if(tile!=null && tile.IsWalkable && !tile.HasObstacle && GetPieceAt(x,y)==null)
                result.Add(new Vector2Int(x,y));
        }
        return result;
    }
    public IEnumerator PlayTurnEndTeleports(PieceTeam team, Func<bool> cancelled = null)
    {
        foreach(var piece in TeleportPieces(team))
        {
            if(cancelled!=null && cancelled())yield break;
            if(piece==null || GetPieceAt(piece.X,piece.Y)!=piece || !piece.HasStatusEffect(StatusEffectType.Teleport))continue;
            var tiles=TeleportTiles();
            if(tiles.Count==0)continue;
            var animator=piece.GetComponentInChildren<PieceSpineAnimationController>();
            if(animator!=null)yield return animator.PlaySelectRoutine();
            if(piece==null)continue;
            var skeletons=piece.GetComponentsInChildren<SkeletonAnimation>();
            var sprites=piece.GetComponentsInChildren<SpriteRenderer>();
            var alphas=new float[skeletons.Length];
            var colors=new Color[sprites.Length];
            for(int i=0;i<skeletons.Length;i++)alphas[i]=skeletons[i].Skeleton!=null?skeletons[i].Skeleton.GetColor().a:1;
            for(int i=0;i<sprites.Length;i++)colors[i]=sprites[i].color;
            bool moved = false;
            try
            {
                for(float time=0;time<.2f;time+=Time.deltaTime)
                {
                    if(piece==null || (cancelled!=null && cancelled()))yield break;
                    float factor=1-Mathf.Clamp01(time/.2f);
                    for(int i=0;i<skeletons.Length;i++)if(skeletons[i]!=null && skeletons[i].Skeleton!=null)SetTeleportAlpha(skeletons[i],alphas[i]*factor);
                    for(int i=0;i<sprites.Length;i++)if(sprites[i]!=null){var c=colors[i];c.a*=factor;sprites[i].color=c;}
                    yield return null;
                }
                if(piece==null || (cancelled!=null && cancelled()))yield break;
                for(int i=0;i<skeletons.Length;i++)if(skeletons[i]!=null && skeletons[i].Skeleton!=null)SetTeleportAlpha(skeletons[i],0);
                foreach(var sprite in sprites)if(sprite!=null){var c=sprite.color;c.a=0;sprite.color=c;}
                tiles=TeleportTiles();
                if(tiles.Count==0)continue;
                var destination=tiles[UnityEngine.Random.Range(0,tiles.Count)];
                yield return MovePieceRoutine(piece,destination.x,destination.y,false);
                moved = piece != null && piece.X == destination.x && piece.Y == destination.y &&
                    GetPieceAt(destination.x,destination.y) == piece;
            }
            finally
            {
                for(int i=0;i<skeletons.Length;i++)if(skeletons[i]!=null && skeletons[i].Skeleton!=null)SetTeleportAlpha(skeletons[i],alphas[i]);
                for(int i=0;i<sprites.Length;i++)if(sprites[i]!=null)sprites[i].color=colors[i];
                if(animator!=null)animator.PlayIdle();
            }
            if(piece!=null && moved)
            {
                yield return PlayPieceBornAnimation(piece);
                if(piece!=null)piece.RemoveStatusEffect(StatusEffectType.Teleport);
            }
        }
    }
}
