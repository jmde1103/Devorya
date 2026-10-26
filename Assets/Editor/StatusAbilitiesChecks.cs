using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;

[InitializeOnLoad]
public static class StatusAbilitiesChecks
{
    const string Pending="StatusAbilitiesChecks";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static int count;
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;}
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,Flags).SetValue(o,value);
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static StatusAbilitiesChecks()
    {
        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
            {SessionState.SetBool(Pending,false);new GameObject("StatusAbilitiesCheckHost").AddComponent<PieceAnimationManager>().StartCoroutine(Guard());}
        };
    }
    public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    static IEnumerator Guard()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Cases());
        while(stack.Count>0)
        {
            object next;
            try{if(!stack.Peek().MoveNext()){stack.Pop();continue;}next=stack.Peek().Current;}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);yield break;}
            if(next is IEnumerator routine)stack.Push(routine);else yield return next;
        }
        Debug.Log("STATUS_ABILITIES_PLAY_PASS checks="+count);EditorApplication.Exit(0);
    }
    static BattleManager Battle(SynthesisAbilityChecks.Fixture f)
    {
        var b=f.root.AddComponent<BattleManager>();b.enabled=false;
        Set(b,"boardManager",f.board);Set(b,"pieceManager",f.manager);Set(b,"battleMoveValidator",f.validator);
        Set(b,"battleSkillManager",f.skill);Set(b,"uniqueSkillDatabase",f.skills);Set(b,"isBattleEnded",true);
        Set(b,"isPlayerDeploymentPhase",false);Set(b,"currentTurn",BattleTurn.Player);
        f.skill.Initialize(f.board,f.manager,f.validator,f.skills);
        for(int y=0;y<f.board.Height;y++)for(int x=0;x<f.board.Width;x++)
        {typeof(Tile).GetProperty("X").SetValue(f.board.GetTile(x,y),x);typeof(Tile).GetProperty("Y").SetValue(f.board.GetTile(x,y),y);}
        return b;
    }
    static void Skill(Piece p,UniqueSkillType t)=>typeof(Piece).GetProperty("UniqueSkill").SetValue(p,t);
    static IEnumerator Cases()
    {
        yield return LocalizationSettings.InitializationOperation;
        yield return SynthesisAbilityChecks.RunReinforcement();
        var types=new[]{UniqueSkillType.GuardSelf,UniqueSkillType.TeleportSelf,UniqueSkillType.Virus,UniqueSkillType.EvadeSelf};
        int[] costs={3,2,3,2};
        for(int i=0;i<types.Length;i++)
        {
            var f=new SynthesisAbilityChecks.Fixture();var b=Battle(f);
            var caster=f.Spawn("Devorya Pawn",PieceTeam.Player,1,1);Skill(caster,types[i]);
            f.Spawn("Devorya Pawn",PieceTeam.Enemy,3,3);
            var data=f.skills.GetData(types[i]);Check(data!=null && data.energyCost==costs[i],"Registered cost");
            Set(b,"selectedPiece",caster);Set(b,"isBattleEnded",false);Set(b,"currentPlayerAbilityEnergy",costs[i]-1);
            b.UseSelectedPieceSkill();yield return null;
            Check(b.CurrentPlayerAbilityEnergy==costs[i]-1 && !(bool)Get(b,"isActionAnimating"),"Insufficient energy rejected");
            Set(b,"isBattleEnded",true);
            caster.SetQuickStepMove(true);Check(!f.skill.CanUseUniqueSkillEffect(caster),"Quick step restriction");caster.SetQuickStepMove(false);
            caster.AddStatusEffect(f.statuses.GetData(StatusEffectType.AbilitySealed));Check(!f.skill.CanUseUniqueSkillEffect(caster),"Seal restriction");
            caster.ReduceStatusEffectTurnAndRemoveExpired();
            Set(b,"currentPlayerAbilityEnergy",10);
            yield return (IEnumerator)typeof(BattleManager).GetMethod("UseSelectedPieceSkillWithTargetRoutine",Flags).Invoke(b,new object[]{caster,data,null});
            Check(b.CurrentPlayerAbilityEnergy==10-costs[i],"Success spends exact cost");
            if(i!=2)
            {
                var status=i==0?StatusEffectType.Defence:i==1?StatusEffectType.Teleport:StatusEffectType.Evasion;
                Check(caster.HasStatusEffect(status),"Self state applied");
            }
            foreach(var locale in new[]{"ko","en","ja"})
            {
                LocalizationSettings.SelectedLocale=LocalizationSettings.AvailableLocales.GetLocale(locale);yield return null;
                Check(!string.IsNullOrWhiteSpace(data.GetLocalizedDescription()),"Translation");
                Check(TooltipTextStyle.Format(data.localizedDescription,data.GetLocalizedDescription()).Contains("<color=#"),"Semantic styling");
            }
            f.Dispose();yield return null;
        }
        foreach(var team in new[]{PieceTeam.Player,PieceTeam.Enemy})
        foreach(int n in new[]{0,1,2,3,5})
        {
            var f=new SynthesisAbilityChecks.Fixture();Battle(f);
            var caster=f.Spawn("Devorya Pawn",team,0,0);Skill(caster,UniqueSkillType.Virus);
            var targets=new List<Piece>();
            for(int i=0;i<n;i++)targets.Add(f.Spawn("Devorya Pawn",team==PieceTeam.Player?PieceTeam.Enemy:PieceTeam.Player,i,2));
            bool success=false;
            yield return f.skill.TryUseUniqueSkillRoutine(caster,null,result=>success=result);
            Check(success==(n>0),"Virus empty target failure");
            int affected=0;
            foreach(var t in targets)
            {
                if(t.HasStatusEffect(StatusEffectType.AttackDisabled)||t.HasStatusEffect(StatusEffectType.MoveDisabled))
                {
                    affected++;Check(t.HasStatusEffect(StatusEffectType.AttackDisabled)!=t.HasStatusEffect(StatusEffectType.MoveDisabled),"Exactly one debuff");
                    t.ReduceStatusEffectTurnAndRemoveExpired();Check(t.HasStatusEffect(StatusEffectType.AttackDisabled)||t.HasStatusEffect(StatusEffectType.MoveDisabled),"Survives incoming turn");
                    t.ReduceStatusEffectTurnAndRemoveExpired();Check(!t.HasStatusEffect(StatusEffectType.AttackDisabled)&&!t.HasStatusEffect(StatusEffectType.MoveDisabled),"Expires second own turn");
                }
            }
            Check(affected==Math.Min(3,n),"Unique maximum three victims");
            f.Dispose();yield return null;
        }
        foreach(var team in new[]{PieceTeam.Player,PieceTeam.Enemy})
        {
            var f=new SynthesisAbilityChecks.Fixture();var b=Battle(f);
            var p=f.Spawn("Devorya Pawn",team,0,0);var q=f.Spawn("Devorya Pawn",team,1,0);
            p.AddGeneralSkill(GeneralSkillType.Defense);Skill(p,UniqueSkillType.TeleportSelf);
            p.AddStatusEffect(f.statuses.GetData(StatusEffectType.Teleport));q.AddStatusEffect(f.statuses.GetData(StatusEffectType.Teleport));
            yield return f.manager.PlayTurnEndTeleports(team);
            Check(f.manager.GetPieceAt(p.X,p.Y)==p && f.manager.GetPieceAt(q.X,q.Y)==q && p.CurrentTile!=q.CurrentTile,"Distinct grid occupancy");
            Check(p.Team==team && p.UniqueSkill==UniqueSkillType.TeleportSelf && p.HasGeneralSkill(GeneralSkillType.Defense) && !p.HasStatusEffect(StatusEffectType.Teleport) && !q.HasStatusEffect(StatusEffectType.Teleport),"Preserved instance; teleport consumed");
            p.AddStatusEffect(f.statuses.GetData(StatusEffectType.Teleport));
            for(int y=0;y<f.board.Height;y++)for(int x=0;x<f.board.Width;x++)typeof(Tile).GetProperty("IsWalkable").SetValue(f.board.GetTile(x,y),false);
            var pos=p.transform.position;yield return f.manager.PlayTurnEndTeleports(team);Check(p.transform.position==pos && p.HasStatusEffect(StatusEffectType.Teleport),"No available tile preserves status and position");
            for(int y=0;y<f.board.Height;y++)for(int x=0;x<f.board.Width;x++)typeof(Tile).GetProperty("IsWalkable").SetValue(f.board.GetTile(x,y),true);
            Set(b,"isBattleEnded",false);Set(b,"currentTurn",team==PieceTeam.Player?BattleTurn.Player:BattleTurn.Enemy);
            typeof(BattleManager).GetMethod("EndTurn",Flags).Invoke(b,null);
            Check((bool)Get(b,"isActionAnimating"),"Turn-end input lock");
            float until=Time.realtimeSinceStartup+20;
            while((bool)Get(b,"isResolvingTurnEnd")){if(Time.realtimeSinceStartup>=until)throw new Exception("Turn end timeout");yield return null;}
            Check((BattleTurn)Get(b,"currentTurn")==(team==PieceTeam.Player?BattleTurn.Enemy:BattleTurn.Player),"Switch only after teleport");
            Check(!p.HasStatusEffect(StatusEffectType.Teleport),"Turn-end teleport consumed after success");
            f.Dispose();yield return null;
        }
        // A fully resisted cast is still a successful use and must spend its cost.
        bool sawResistedCast=false;
        for(int attempt=0;attempt<12 && !sawResistedCast;attempt++)
        {
            var f=new SynthesisAbilityChecks.Fixture();var b=Battle(f);
            var caster=f.Spawn("Devorya Pawn",PieceTeam.Player,0,0);Skill(caster,UniqueSkillType.Virus);
            var target=f.Spawn("Devorya Pawn",PieceTeam.Enemy,2,2);
            target.AddGeneralSkill(GeneralSkillType.AttackDisabledResistanceAdvanced);
            target.AddGeneralSkill(GeneralSkillType.MoveDisabledResistanceAdvanced);
            Set(b,"currentPlayerAbilityEnergy",10);UnityEngine.Random.InitState(1000+attempt);
            yield return (IEnumerator)typeof(BattleManager).GetMethod("UseSelectedPieceSkillWithTargetRoutine",Flags).Invoke(b,new object[]{caster,f.skills.GetData(UniqueSkillType.Virus),null});
            Check(b.CurrentPlayerAbilityEnergy==7,"Cast spends three even when target resists");
            sawResistedCast=!target.HasStatusEffect(StatusEffectType.AttackDisabled)&&!target.HasStatusEffect(StatusEffectType.MoveDisabled);
            f.Dispose();yield return null;
        }
        Check(sawResistedCast,"Fully resisted cast exercised");
        yield return VirusTurnChecks();
        yield return Additional();
        yield return ConsumptionChecks.Run();
        yield return RunItemsChecks.StateAndFrozenChecks();
    }
    static IEnumerator VirusTurnChecks()
    {
        var apply=typeof(BattleSkillManager).GetMethod("ApplyVirusStatus",Flags);
        foreach(var team in new[]{PieceTeam.Player,PieceTeam.Enemy})
        foreach(bool attack in new[]{true,false})
        {
            var f=new SynthesisAbilityChecks.Fixture();var b=Battle(f);
            var other=team==PieceTeam.Player?PieceTeam.Enemy:PieceTeam.Player;
            var caster=f.Spawn("Devorya Pawn",other,2,4);Skill(caster,UniqueSkillType.Virus);
            var victim=f.Spawn("Devorya Rook",team,2,2);
            // Both destinations are legal before the virus restriction.
            var empty=new Vector2Int(3,2);var capture=new Vector2Int(2,4);
            Check(f.validator.GetSelectablePositions(victim).Contains(empty)&&f.validator.GetSelectablePositions(victim).Contains(capture),"Baseline movement and capture");
            for(int seed=0;;seed++){UnityEngine.Random.InitState(seed);if((UnityEngine.Random.Range(0,2)==0)==attack){UnityEngine.Random.InitState(seed);break;}}
            apply.Invoke(f.skill,new object[]{victim});
            Set(b,"isBattleEnded",false);Set(b,"currentTurn",other==PieceTeam.Player?BattleTurn.Player:BattleTurn.Enemy);
            typeof(BattleManager).GetMethod("EndTurn",Flags).Invoke(b,null);
            Check((BattleTurn)Get(b,"currentTurn")== (team==PieceTeam.Player?BattleTurn.Player:BattleTurn.Enemy),"Victim team now active");
            var legal=f.validator.GetSelectablePositions(victim);
            Check(legal.Contains(empty)==attack && legal.Contains(capture)!=attack,"Actual next-turn move/capture restriction");
            typeof(BattleManager).GetMethod("EndTurn",Flags).Invoke(b,null);
            typeof(BattleManager).GetMethod("EndTurn",Flags).Invoke(b,null);
            Check(f.validator.GetSelectablePositions(victim).Contains(empty)&&f.validator.GetSelectablePositions(victim).Contains(capture),"Restriction expires next own turn");
            f.Dispose();yield return null;
        }
        {
            var f=new SynthesisAbilityChecks.Fixture();Battle(f);
            var p=f.Spawn("Devorya Pawn",PieceTeam.Enemy,2,2);
            p.AddGeneralSkill(GeneralSkillType.AttackDisabledResistanceAdvanced);
            p.AddGeneralSkill(GeneralSkillType.MoveDisabledResistanceAdvanced);
            int attacks=0,moves=0,resisted=0;
            for(int seed=0;seed<200;seed++)
            {
                UnityEngine.Random.InitState(seed);bool attack=UnityEngine.Random.Range(0,2)==0;
                bool resists=UnityEngine.Random.Range(0,100)<50;
                if(attack)attacks++;else moves++;if(resists)resisted++;
                UnityEngine.Random.InitState(seed);apply.Invoke(f.skill,new object[]{p});
                Check(p.HasStatusEffect(StatusEffectType.AttackDisabled)==(attack&&!resists)&&p.HasStatusEffect(StatusEffectType.MoveDisabled)==(!attack&&!resists),"Only chosen effect rolls resistance; no fallback");
                p.ReduceStatusEffectTurnAndRemoveExpired();p.ReduceStatusEffectTurnAndRemoveExpired();
            }
            Check(attacks>70&&moves>70&&resisted>50,"Both independent random outcomes and resistance exercised");
            foreach(var effect in new[]{StatusEffectType.AttackDisabled,StatusEffectType.MoveDisabled,StatusEffectType.Defence,StatusEffectType.Evasion})
                Check(f.statuses.GetData(effect).durationTurn==1,"Shared duration remains one");
            f.Dispose();yield return null;
        }
    }
    static IEnumerator Additional()
    {
        foreach(var status in new[]{StatusEffectType.Defence,StatusEffectType.Evasion})
        foreach(int chance in new[]{0,100})
        {
            var f=new SynthesisAbilityChecks.Fixture();var battle=Battle(f);
            var db=AssetDatabase.LoadAssetAtPath<GeneralSkillDatabase>("Assets/Devorya/Data/Skills/General/특성_데이터베이스.asset");
            Set(f.skill,"generalSkillDatabase",db);
            var insight=db.GetData(GeneralSkillType.Insight);int saved=insight.insightPercent;
            try
            {
                insight.insightPercent=chance;
                var attacker=f.Spawn("Devorya Rook",PieceTeam.Player,2,0);
                attacker.AddGeneralSkill(GeneralSkillType.Insight);
                var target=f.Spawn("Devorya Bishop",PieceTeam.Enemy,2,2);
                f.Spawn("Devorya King",PieceTeam.Enemy,4,5);
                target.AddStatusEffect(f.statuses.GetData(status));
                // Force only the evasion roll to succeed; Insight uses its configured probability.
                for(int seed=0;seed<100;seed++)
                {
                    UnityEngine.Random.InitState(seed);
                    if(UnityEngine.Random.Range(0f,100f)<50){UnityEngine.Random.InitState(seed);break;}
                }
                yield return (IEnumerator)typeof(BattleManager).GetMethod("ExecutePieceActionRoutine",Flags).Invoke(battle,new object[]{attacker,f.board.GetTile(2,2)});
                yield return null;
                Check((target==null)==(chance==100),"Insight cancels "+status+" only when activated");
            }
            finally{insight.insightPercent=saved;f.Dispose();}
            yield return null;
        }
        {
            var f=new SynthesisAbilityChecks.Fixture();Battle(f);
            var p=f.Spawn("Devorya Pawn",PieceTeam.Player,0,0);
            p.AddGeneralSkill(GeneralSkillType.AttackDisabledResistanceAdvanced);
            p.AddGeneralSkill(GeneralSkillType.MoveDisabledResistanceAdvanced);
            for(int seed=0;seed<100;seed++)
            {
                UnityEngine.Random.InitState(seed);
                bool attack=UnityEngine.Random.Range(0,100)>=50;
                bool move=UnityEngine.Random.Range(0,100)>=50;
                UnityEngine.Random.InitState(seed);
                p.AddStatusEffect(f.statuses.GetData(StatusEffectType.AttackDisabled),1);
                p.AddStatusEffect(f.statuses.GetData(StatusEffectType.MoveDisabled),1);
                Check(p.HasStatusEffect(StatusEffectType.AttackDisabled)==attack && p.HasStatusEffect(StatusEffectType.MoveDisabled)==move,"Independent resistance rolls");
                p.ReduceStatusEffectTurnAndRemoveExpired();
            }
            f.Dispose();yield return null;
        }
        {
            var f=new SynthesisAbilityChecks.Fixture();Battle(f);
            var caster=f.Spawn("Devorya Pawn",PieceTeam.Player,0,0);Skill(caster,UniqueSkillType.Virus);
            var victims=new List<Piece>();
            for(int i=0;i<3;i++)victims.Add(f.Spawn("Devorya Pawn",PieceTeam.Enemy,i,2));
            bool done=false,success=false;int peak=0;
            f.skill.StartCoroutine(f.skill.TryUseUniqueSkillRoutine(caster,null,r=>{success=r;done=true;}));
            float until=Time.realtimeSinceStartup+30;
            while(!done)
            {
                if(Time.realtimeSinceStartup>until)throw new Exception("Virus animation timeout");
                int active=0;
                foreach(var fx in f.root.GetComponentsInChildren<PixelBurstEffect>())if(fx.name=="VirusPixelBurst")active++;
                peak=Mathf.Max(active,peak);
                foreach(var p in victims)if(p.HasStatusEffect(StatusEffectType.AttackDisabled)||p.HasStatusEffect(StatusEffectType.MoveDisabled))throw new Exception("Effect before animation completion");
                yield return null;
            }
            Check(success && peak==3,"Exactly one simultaneous burst per victim");
            foreach(var p in victims)Check(p.HasStatusEffect(StatusEffectType.AttackDisabled)!=p.HasStatusEffect(StatusEffectType.MoveDisabled),"One status after burst");
            f.Dispose();yield return null;
        }
        {
            var f=new SynthesisAbilityChecks.Fixture();var battle=Battle(f);
            var p=f.Spawn("Devorya Pawn",PieceTeam.Player,0,0);Skill(p,UniqueSkillType.Virus);
            Set(battle,"currentPlayerAbilityEnergy",10);
            yield return (IEnumerator)typeof(BattleManager).GetMethod("UseSelectedPieceSkillWithTargetRoutine",Flags).Invoke(battle,new object[]{p,f.skills.GetData(UniqueSkillType.Virus),null});
            Check(battle.CurrentPlayerAbilityEnergy==10,"Failure preserves energy");
            p.AddStatusEffect(f.statuses.GetData(StatusEffectType.Teleport));
            var origin=p.transform.position;
            bool cancel=false,done=false;
            f.manager.StartCoroutine(CancelTeleport(f,p,()=>cancel,()=>done=true));
            yield return null;cancel=true;
            float until=Time.realtimeSinceStartup+10;
            while(!done){if(Time.realtimeSinceStartup>until)throw new Exception("Cancellation timeout");yield return null;}
            Check(p.transform.position==origin && f.manager.GetPieceAt(0,0)==p && p.HasStatusEffect(StatusEffectType.Teleport),"Cancelled teleport preserves position and status");
            foreach(var skeleton in p.GetComponentsInChildren<Spine.Unity.SkeletonAnimation>())
                if(skeleton.Skeleton!=null)Check(skeleton.Skeleton.GetColor().a>.99f,"Cancelled fade restores alpha");
            f.Dispose();yield return null;
        }
        // Existing evasion scenarios also exercise taunt, no escape tile and absorption misses.
        yield return EvasionChecks.Run();
    }
    static IEnumerator CancelTeleport(SynthesisAbilityChecks.Fixture f,Piece p,Func<bool> cancel,Action done)
    {
        yield return f.manager.PlayTurnEndTeleports(p.Team,cancel);
        done();
    }

}
