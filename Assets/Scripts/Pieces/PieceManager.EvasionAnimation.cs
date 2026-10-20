using System;
using System.Collections;
using UnityEngine;
public partial class PieceManager
{
    public IEnumerator PlayEvadedAttack(Piece attacker, Piece defender, Vector2Int destination, Action onEvade)
    {
        Vector3 impact = defender.transform.position;
        Coroutine escape = null;
        Action startEscape = () => { onEvade?.Invoke(); escape=StartCoroutine(EscapeAtImpact(defender,destination)); };
        var animation = GetPieceAnimationManager();
        if(animation!=null)yield return animation.PlayPieceAttackMoveAnimation(attacker,impact,false,null,startEscape);
        else { startEscape(); attacker.transform.position=impact; }
        if(escape!=null)yield return escape;
    }
    IEnumerator EscapeAtImpact(Piece defender,Vector2Int destination)
    {
        Vector3 origin=defender.transform.position;
        var move = MovePieceRoutine(defender,destination.x,destination.y,false);
        while (move.MoveNext()) yield return move.Current;
        Vector3 end=defender.transform.position;
        for(float t=0;t<.1f;t+=Time.deltaTime)
        {
            if(defender==null)yield break;
            float p=Mathf.Clamp01(t/.1f);
            defender.transform.position=Vector3.Lerp(origin,end,p)+Vector3.up*(Mathf.Sin(p*Mathf.PI)*.12f);
            yield return null;
        }
        if(defender!=null)defender.transform.position=end;
    }
}