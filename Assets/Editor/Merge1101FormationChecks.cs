using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class Merge1101FormationChecks
{
    const string Pending="Merge1101FormationChecks";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static Merge1101FormationChecks()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))
            {SessionState.SetBool(Pending,false);new GameObject("FormationMergeCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());}
        };
    }
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/EventScene.unity");SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,Flags).GetValue(o);
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);Debug.Log("FORMATION_MERGE_CHECK: "+text);}
    static IEnumerator Guard()
    {
        var checks=Checks();while(true){object next;try{if(!checks.MoveNext())break;next=checks.Current;}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}yield return next;}
        Debug.Log("FORMATION_MERGE_PLAYMODE_PASS");EditorApplication.Exit(0);
    }
    static IEnumerator Checks()
    {
        yield return new WaitForSecondsRealtime(3);
        var sequence=UnityEngine.Object.FindFirstObjectByType<EventSceneSequenceController>();sequence.StopAllCoroutines();
        var formation=UnityEngine.Object.FindFirstObjectByType<EventSceneFormationBoardController>(FindObjectsInactive.Include);
        Check(formation!=null,"Latest formation controller linked in EventScene");
        formation.CloseFormationBoard();
        var root=Get<GameObject>(formation,"formationBoardRoot");var position=root.transform.position;
        Check(!Get<bool>(formation,"autoCenterBoard"),"Manual board positioning retained");
        var run=RunStateManager.Instance;run.SavePlayerPieces(new List<PlayerPieceRuntimeData>());
        Check(formation.OpenFormationBoard(),"Existing formation opens");yield return new WaitForSecondsRealtime(2);
        Check(root.transform.position==position,"Open preserves authored board position");
        var board=formation.BoardManager;Check(board.Width==5&&board.Height==2,"Board dimensions remain 5x2");
        for(int y=0;y<2;y++)for(int x=0;x<5;x++)Check(board.GetTile(x,y)!=null,"Tile exists "+x+","+y);
        var pieces=Get<PieceManager>(formation,"pieceManager");
        Check(pieces.GetPieceCount(PieceTeam.Player)>0,"Editor fallback creates player pieces");
        var king=pieces.GetPieceAt(2,0);Check(king!=null&&king.PieceType==PieceType.King,"King begins at 2,0");
        Check(run.GetPlayerPiecesCopy().Count==0,"Editor fallback never writes player RunState");
        formation.CloseFormationBoard();Check(!formation.IsFormationOpen&&!root.activeSelf,"Close cleans active formation");
        Debug.Log("FORMATION_SELECTION_MANUAL_CHECK_PENDING: Mouse/touch input not simulated; existing unresolved issue remains open.");
    }
}
