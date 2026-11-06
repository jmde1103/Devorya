using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Spine.Unity;

[InitializeOnLoad]
public static class ShopMergeLatestBattleChecks
{
    const string Pending="ShopMergeLatestBattleChecks";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static ShopMergeLatestBattleChecks(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("ShopMergeBattleChecks").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());}};}
    public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/BattleScene.unity");SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    static T Get<T>(object o,string f)=>(T)o.GetType().GetField(f,F).GetValue(o);
    static void Set(object o,string f,object value)=>o.GetType().GetField(f,F).SetValue(o,value);
    static object Call(object o,string m,params object[] values)=>o.GetType().GetMethod(m,F).Invoke(o,values);
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);Debug.Log("SHOP_MERGE_BATTLE_CHECK: "+message);}
    static IEnumerator Guard(){var e=Checks();while(true){object n;try{if(!e.MoveNext())break;n=e.Current;}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);yield break;}yield return n;}Debug.Log("SHOP_MERGE_BATTLE_PASS");EditorApplication.Exit(0);}
    static IEnumerator Checks()
    {
        yield return new WaitForSecondsRealtime(12);
        var bm=BattleManager.Instance;Check(bm!=null,"Latest BattleScene starts");
        var intro=UnityEngine.Object.FindFirstObjectByType<EventSceneFormationIntroAnimator>();Check(intro!=null,"Battle intro animator remains connected");
        Check(Get<int>(intro,"battleTilesPerWave")==3,"User's latest serialized three-tile wave retained");
        Check(Get<bool>(intro,"randomizeBattleTileOrder"),"Random tile order enabled");
        bm.StopAllCoroutines();intro.StopAndRestore();
        var setup=UnityEngine.Object.FindFirstObjectByType<BattleSetupManager>();if(setup!=null)setup.StopAllCoroutines();
        var ec=Get<EventSequenceController>(bm,"eventSequenceController");if(ec!=null)ec.StopAllCoroutines();Set(bm,"eventSequenceController",null);Set(bm,"battleAIManager",null);
        Set(bm,"isPlayerDeploymentPhase",false);Set(bm,"isBattleAnnouncementPlaying",false);Set(bm,"isActionAnimating",false);Set(bm,"isBattleEnded",false);Set(bm,"currentTurn",BattleTurn.Player);bm.SetBattleIntroActive(false);
        var pm=Get<PieceManager>(bm,"pieceManager");var board=Get<BoardManager>(bm,"boardManager");
        pm.ClearAllPieces();
        var player=pm.SpawnPiece(PieceType.Rook,PieceTeam.Player,1,1,true);
        var enemy=pm.SpawnPiece(PieceType.Rook,PieceTeam.Enemy,3,3,true);
        var playerKing=pm.SpawnPiece(PieceType.King,PieceTeam.Player,0,0,true);var enemyKing=pm.SpawnPiece(PieceType.King,PieceTeam.Enemy,4,4,true);
        bm.SelectPiece(enemy);
        var previews=Get<List<Tile>>(bm,"enemyMovePreviewTiles");Check(previews.Count>0,"Enemy selection computes legal preview tiles");
        var tile=previews[0];var baseRenderer=Get<SpriteRenderer>(tile,"spriteRenderer");var originalColor=baseRenderer.color;
        var diamond=Get<SpriteRenderer>(tile,"enemyMoveOverlapBorderRenderer");
        Check(diamond!=null&&diamond.color.a>0&&diamond.color.a<1,"Enemy preview is translucent separate diamond");
        Check(Mathf.Abs(diamond.color.r-diamond.color.g)<.001f&&Mathf.Abs(diamond.color.g-diamond.color.b)<.001f,"Enemy preview remains gray");
        Check(diamond.sprite.texture.GetPixels().Where(c=>c.a>.1f).All(c=>c.r>.9f&&c.g>.9f&&c.b>.9f),"Diamond source has fill only with no black border");
        tile.ShowHighlight();var blue=Get<SpriteRenderer>(tile,"highlightOverlayRenderer");var blueColor=blue.color;
        tile.ShowEnemyMoveOverlapPreview(board.TileWidthHalf,board.TileHeightHalf);
        Check(blue.color==blueColor&&baseRenderer.color==originalColor,"Gray overlay preserves ally highlight and base tile colors");
        Call(bm,"ClearEnemyMovePreview");Check(diamond.color.a==0,"Enemy deselection cleanup hides gray overlay");
        bm.SelectPiece(player);Check(Get<Piece>(bm,"selectedPiece")==player,"Player selection path remains available");
        var roots=new List<Transform>();for(int y=0;y<board.Height;y++)for(int x=0;x<board.Width;x++)if(board.GetTile(x,y)!=null)roots.Add(board.GetTile(x,y).transform);
        foreach(int wave in new[]{2,3})
        {
            Set(intro,"battleTilesPerWave",wave);Set(intro,"tileInterval",1f);Set(intro,"tileFadeDuration",.4f);
            Check(intro.Prepare(board,pm,true),"Battle intro prepares wave "+wave);
            var visuals=Get<IList>(intro,"tileVisuals");var order=visuals.Cast<object>().Select(v=>Get<Transform>(v,"root")).ToArray();
            Check(!order.SequenceEqual(roots),"Battle tiles shuffled for wave "+wave);
            intro.PlayPrepared();yield return new WaitForSeconds(.12f);
            int visible=roots.Count(t=>Get<SpriteRenderer>(t.GetComponent<Tile>(),"spriteRenderer").color.a>.001f);
            Check(visible==wave,"Exactly "+wave+" tiles begin first wave");intro.StopAndRestore();
        }
        Set(intro,"battleTilesPerWave",3);Set(intro,"tileInterval",.01f);Set(intro,"tileFadeDuration",.03f);
        var born=new List<PieceTeam>();var skeletons=new List<SkeletonAnimation>();
        foreach(var piece in new[]{player,enemy,playerKing,enemyKing})
        {
            var skeleton=piece.GetComponentInChildren<SkeletonAnimation>(true);if(skeleton==null||skeleton.AnimationState==null)continue;
            skeletons.Add(skeleton);var team=piece.Team;
            skeleton.AnimationState.Start+=entry=>{if(entry.Animation.Name.IndexOf("born",StringComparison.OrdinalIgnoreCase)>=0)born.Add(team);};
        }
        Check(skeletons.Count>0,"Runtime pieces retain Spine animation controllers");
        intro.Prepare(board,pm,true);intro.PlayPrepared();float timeout=0;
        while(intro.IsPlaying&&timeout<25){yield return new WaitForSecondsRealtime(.1f);timeout+=.1f;}
        Check(!intro.IsPlaying,"Tile and Born intro completes");
        Check(born.Contains(PieceTeam.Player)&&born.Contains(PieceTeam.Enemy),"Both teams play Born");
        Check(born.LastIndexOf(PieceTeam.Player)<born.IndexOf(PieceTeam.Enemy),"All player Born starts precede enemy Born");
        Check(skeletons.All(s=>s.AnimationState.GetTrack(0)!=null&&s.AnimationState.GetTrack(0).Animation.Name.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0),"Pieces return to Idle after Born");
    }
}
