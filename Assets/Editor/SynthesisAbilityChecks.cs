using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SynthesisAbilityChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("Synthesis check: " + message); checks++; }
    static void Set(object obj, string name, object value) { obj.GetType().GetField(name, Private).SetValue(obj, value); }
    static object Get(object obj, string name) { return obj.GetType().GetField(name, Private).GetValue(obj); }

    internal sealed class Fixture
    {
        public GameObject root;
        public BoardManager board;
        public PieceManager manager;
        public PieceAnimationManager animation;
        public BattleMoveValidator validator;
        public BattleSkillManager skill;
        public PieceDatabase db;
        public StatusEffectDatabase statuses;
        public UniqueSkillDatabase skills;
        public Fixture()
        {
            root = new GameObject("Synthesis fixture");
            board = root.AddComponent<BoardManager>(); board.enabled = false;
            manager = root.AddComponent<PieceManager>(); manager.enabled = false;
            animation = root.AddComponent<PieceAnimationManager>();
            validator = root.AddComponent<BattleMoveValidator>();
            skill = root.AddComponent<BattleSkillManager>();
            db = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/Devorya/Data/Piece/PieceDatabase.asset");
            statuses = AssetDatabase.LoadAssetAtPath<StatusEffectDatabase>("Assets/Devorya/Data/StatusEffect/\uc0c1\ud0dc\ud6a8\uacfc_\ub370\uc774\ud130\ubca0\uc774\uc2a4.asset");
            skills = AssetDatabase.LoadAssetAtPath<UniqueSkillDatabase>("Assets/Devorya/Data/Skills/Unique/\ub2a5\ub825_\ub370\uc774\ud130\ubca0\uc774\uc2a4.asset");
            var tiles = new Tile[board.Width, board.Height];
            for (int y=0; y<board.Height; y++) for (int x=0; x<board.Width; x++)
            {
                var tile = new GameObject("Tile"); tile.transform.SetParent(root.transform); tile.transform.position = new Vector3(x,y,0);
                tile.AddComponent<SpriteRenderer>(); tiles[x,y] = tile.AddComponent<Tile>();
            }
            Set(board,"tiles",tiles); Set(manager,"boardManager",board); Set(manager,"pieceDatabase",db);
            Set(manager,"piecePrefab",AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PiecePrefab.prefab"));
            Set(manager,"pieceParent",root.transform); Set(manager,"pieceAnimationManager",animation);
            manager.InitializePieceGrid(); validator.Initialize(board,manager);
            Set(skill,"boardManager",board); Set(skill,"pieceManager",manager); Set(skill,"battleMoveValidator",validator); Set(skill,"statusEffectDatabase",statuses);
        }
        public Piece Spawn(string id, PieceTeam team, int x,int y) { return manager.SpawnPieceFromData(db.GetData(id),team,x,y,true, false); }
        public void Dispose() { UnityEngine.Object.Destroy(root); }
    }

    public static IEnumerator Run()
    {
        foreach (string species in new[] { "Jellu", "Devorya", "Dummy" })
        foreach (PieceTeam team in new[] { PieceTeam.Player, PieceTeam.Enemy })
        {
            var f = new Fixture();
            Piece caster=f.Spawn(species+" Pawn",team,2,2); caster.SetUniqueSkillForEvent(UniqueSkillType.JelluSynthesis);
            caster.AddGeneralSkill(GeneralSkillType.Insight);
            Piece first=f.Spawn("Devorya Pawn",team,1,2), second=f.Spawn("Dummy Pawn",team,3,2);
            Piece wrong=f.Spawn("Jellu Bishop",team,2,1), neutral=f.Spawn("Jellu Pawn",PieceTeam.Neutral,1,1);
            Piece enemy=f.Spawn("Jellu Pawn",team==PieceTeam.Player?PieceTeam.Enemy:PieceTeam.Player,3,3);
            Check(f.validator.GetJelluSynthesisMaterialCandidates(caster).Count==2,"Only allied same types; species irrelevant");
            Check(f.skill.CanUseUniqueSkillEffect(caster),"All three species can synthesize");
            var actions=new List<BattleAIAction>(); new BattleAIActionGenerator(f.board,f.manager,f.validator).GenerateActions(team,actions);
            Check(actions.Exists(a=>a.ActingPiece==caster&&a.UniqueSkillType==UniqueSkillType.JelluSynthesis),"AI same rule");
            var speciesBefore=caster.GetSpeciesTagsCopy();
            Set(f.manager,"pieceDatabase",null);
            Check(!f.skill.CanUseUniqueSkillEffect(caster),"Missing promotion data rejected before consumption");
            bool failed=true; yield return f.skill.TryUseUniqueSkillRoutine(caster,null,result=>failed=result);
            Check(!failed&&f.manager.GetPieceAt(1,2)==first&&f.manager.GetPieceAt(3,2)==second,"Failure preserves both materials");
            Set(f.manager,"pieceDatabase",f.db);
            bool succeeded=false; yield return f.skill.TryUseUniqueSkillRoutine(caster,null,result=>succeeded=result);
            Check(succeeded,"Promotion succeeds");
            Check(caster.CurrentPieceData.pieceId.StartsWith(species+" "),"Promotion preserves species data: "+species);
            Check(caster.PieceType==PieceType.Knight||caster.PieceType==PieceType.Bishop||caster.PieceType==PieceType.Rook,"Promotion result");
            foreach(var tag in speciesBefore) Check(caster.HasSpeciesTag(tag),"Runtime species retained");
            Check(f.manager.GetPieceAt(1,2)==null&&f.manager.GetPieceAt(3,2)==null,"Two materials consumed");
            Check(wrong!=null&&neutral!=null&&enemy!=null,"Unrelated pieces retained");
            Check(caster.HasGeneralSkill(GeneralSkillType.Insight),"Caster trait retained");
            Check(!caster.CanAttackThisTurn&&caster.CanMoveThisTurn,"Promoted piece can move but cannot attack");
            caster.ReduceActionRestrictionTurnAndRemoveExpired(); Check(!caster.CanAttackThisTurn,"Survives current turn end");
            caster.ReduceStatusEffectTurnAndRemoveExpired(); Check(caster.CanAttackThisTurn,"Expires next own turn");
            Check(f.skills.GetData(UniqueSkillType.JelluSynthesis).tooltipSections[0].statusEffectData==f.statuses.GetData(StatusEffectType.AttackDisabled),"Tooltip status linked");
            f.Dispose(); yield return null;
        }
        foreach(string type in new[]{"Knight","Bishop","Rook"})
        {
            var f=new Fixture(); Piece c=f.Spawn("Devorya "+type,PieceTeam.Player,2,2); c.SetUniqueSkillForEvent(UniqueSkillType.JelluSynthesis);
            f.Spawn((type=="Bishop"?"Devorya ":"Dummy ")+type,PieceTeam.Player,1,2); f.Spawn("Jellu "+type,PieceTeam.Player,3,2);
            Check(f.validator.GetJelluSynthesisMaterialCandidates(c).Count==2&&f.skill.CanUseUniqueSkillEffect(c),"Non-pawn same-type use: "+type+"; count="+f.validator.GetJelluSynthesisMaterialCandidates(c).Count+"; skill="+c.UniqueSkill+"; promotion="+f.manager.HasSynthesisPromotion(c)+"; blocked="+c.HasStatusEffect(StatusEffectType.ActionDisabled));
            bool success=false; yield return f.skill.TryUseUniqueSkillRoutine(c,null,result=>success=result);
            Check(success&&c.CurrentPieceData.pieceId.StartsWith("Devorya "),"Non-pawn promotion remains same species");
            f.Dispose(); yield return null;
        }
        foreach(PieceTeam team in new[]{PieceTeam.Player,PieceTeam.Enemy})
        {
            var f=new Fixture(); Piece rook=f.Spawn("Jellu Rook",team,2,2);
            Piece king=f.Spawn("Jellu King",team,2,1), material=f.Spawn("Devorya Pawn",team,3,2);
            Piece enemy=f.Spawn("Jellu Pawn",team==PieceTeam.Player?PieceTeam.Enemy:PieceTeam.Player,1,1);
            Piece neutral=f.Spawn("Jellu Pawn",PieceTeam.Neutral,3,3), far=f.Spawn("Jellu Pawn",team,0,0);
            Check(rook.UniqueSkill==UniqueSkillType.JelluReinforce,"Rook default ability");
            Check(f.skills.GetData(UniqueSkillType.JelluReinforce).tooltipSections.Count==2,"Both status tooltips");
            foreach(Piece invalid in new[]{king,enemy,neutral,far,rook})
            {
                Check(!f.skill.CanUseReinforcementTarget(rook,invalid),"Invalid reinforcement target rejected");


            }
            Check(f.skill.CanUseReinforcementTarget(rook,material),"Different species allied material accepted");
            rook.AddStatusEffect(f.statuses.GetData(StatusEffectType.ActionDisabled),1);
            Check(!f.skill.CanUseReinforcementTarget(rook,material),"ActionDisabled prevents reinforce");
            rook.ReduceStatusEffectTurnAndRemoveExpired();
            var actions=new List<BattleAIAction>(); new BattleAIActionGenerator(f.board,f.manager,f.validator).GenerateActions(team,actions);
            Check(actions.Exists(a=>a.ActingPiece==rook&&a.UniqueSkillType==UniqueSkillType.JelluReinforce&&a.TargetPiece==null),"AI automatic random material");
            Check(!actions.Exists(a=>a.ActingPiece==rook&&a.UniqueSkillType==UniqueSkillType.JelluReinforce&&a.TargetPiece==king),"AI excludes King");
            Vector3 start=material.transform.position; bool done=false,success=false,moved=false;
            BattleManager battle=null;
            if(team==PieceTeam.Player)
            {
                battle=f.root.AddComponent<BattleManager>(); battle.enabled=false;
                Set(battle,"boardManager",f.board); Set(battle,"pieceManager",f.manager); Set(battle,"battleMoveValidator",f.validator);
                Set(battle,"battleSkillManager",f.skill); Set(battle,"uniqueSkillDatabase",f.skills); Set(battle,"selectedPiece",rook);
                battle.UseSelectedPieceSkill();
                Check((bool)Get(battle,"isActionAnimating"),"Ability starts immediately without target selection");
            }
            else f.animation.StartCoroutine(f.skill.TryUseUniqueSkillRoutine(rook,null,result=>{success=result;done=true;}));
            float deadline=Time.realtimeSinceStartup+15;
            while(!done&&Time.realtimeSinceStartup<deadline)
            {
                if(material!=null&&Vector3.Distance(start,material.transform.position)>.01f)moved=true;
                if(battle!=null&&!(bool)Get(battle,"isActionAnimating")) { done=true; success=rook.HasStatusEffect(StatusEffectType.Defence); }
                yield return null;
            }
            Check(done&&success&&moved,"Absorption moves material into rook");
            if(battle!=null) Check(rook.GetUniqueSkillCooldown()==3&&!rook.CanUseUniqueSkill(),"Success starts caster cooldown");
            Check(f.manager.GetPieceAt(3,2)==null&&king!=null,"Only selected material removed");
            Check(rook.HasStatusEffect(StatusEffectType.Defence)&&rook.HasStatusEffect(StatusEffectType.Breakthrough),"Both buffs applied");
            rook.ReduceActionRestrictionTurnAndRemoveExpired(); Check(rook.HasStatusEffect(StatusEffectType.Defence),"Reinforce survives current turn end");
            rook.ReduceStatusEffectTurnAndRemoveExpired(); Check(!rook.HasStatusEffect(StatusEffectType.Defence)&&!rook.HasStatusEffect(StatusEffectType.Breakthrough),"Both buffs expire next own turn");
            Check(!f.skill.CanUseUniqueSkillEffect(rook),"No remaining valid adjacent material");
            f.Dispose(); yield return null;
        }
        File.WriteAllText("Logs/synthesis-results.txt","PASS: "+checks+" synthesis/reinforcement assertions in Play Mode.\n");
        Debug.Log("Synthesis checks PASS: "+checks);
    }
}
