using System;
using System.Reflection;
using System.Text.Json;
using ForgeEmpire;

internal static class ArtisanTechniqueTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Technique(object item,int value)
    {
        var field=item.GetType().GetField("Technique");Check(field!=null,"Creation must persist an optional chosen Technique");field.SetValue(item,value);
    }
    static int Technique(object item){var field=item.GetType().GetField("Technique");Check(field!=null,"Creation must persist Technique");return (int)field.GetValue(item);}
    static GearEffect Secondary(CraftedItem item)
    {
        var method=typeof(WorkshopSimulation).GetMethod("ItemTechniqueEffect",new[]{typeof(CraftedItem)});
        Check(method!=null,"specialized technique needs a distinct secondary benefit that survives paid runes");return (GearEffect)method.Invoke(null,new object[]{item});
    }
    static double Timing(string name,params object[] arguments)
    {
        var method=typeof(WorkshopSimulation).GetMethod(name);
        Check(method!=null,"category-specific timing must expose "+name+" to both controls and native probes");return (double)method.Invoke(null,arguments);
    }
    static WorkshopSimulation Reload(WorkshopSimulation sim){var j=new JsonSerializerOptions{IncludeFields=true};return new WorkshopSimulation(JsonSerializer.Deserialize<WorkshopState>(JsonSerializer.Serialize(sim.State,j),j));}
    static CraftedItem Preview(int id,int quality,int alloy,int temper,int rune,int technique)
    {
        var method=typeof(WorkshopSimulation).GetMethod("PreviewMasterwork",new[]{typeof(int),typeof(int),typeof(int),typeof(int),typeof(int),typeof(int)});
        Check(method!=null,"Technique-aware preview is required before payment");return (CraftedItem)method.Invoke(null,new object[]{id,quality,alloy,temper,rune,technique});
    }
    static bool Begin(WorkshopSimulation sim,int id,int alloy,int technique)
    {
        var method=typeof(WorkshopSimulation).GetMethod("BeginMasterwork",new[]{typeof(int),typeof(int),typeof(int)});
        Check(method!=null,"Technique selection must be part of the once-only begin transaction");return (bool)method.Invoke(sim,new object[]{id,alloy,technique});
    }
    static void Finish(WorkshopSimulation s,int temper=0,int rune=0)
    {
        Check(s.StrikeMasterwork(1),"heat");for(int i=0;i<3;i++){s.Advance(.21);Check(s.StrikeMasterwork(1),"shape");}Check(s.QuenchMasterwork(temper)&&s.EngraveMasterwork(rune)&&s.CompleteMasterwork(),"complete");
    }
    static double ArmoredSkillDamage(int technique)
    {
        var s=new WorkshopSimulation(new WorkshopState{Ingots=30,Gold=100});Check(Begin(s,0,0,technique),"begin armored comparison");Finish(s,0,1);
        var hero=s.State.Campaign.Heroes[1];hero.Recruited=true;hero.Weapon=s.State.Inventory[0];s.State.Saga.Party=new[]{1,-1,-1};
        Check(s.StartSaga(0,0)&&s.ChooseSagaPath(0),"start Lyra armored comparison");var run=s.State.Saga.Run;
        run.RoomTick=8;run.EnemyHP=run.EnemyMaxHP=1000;run.EnemyGuard=40;run.EnemyBarrier=0;run.Mechanic=-1;run.BreakUntilTick=0;
        Check(s.UsePartySkill(0),"focused skill reaches armored target");return 1000-run.EnemyHP;
    }
    public static void Run(Action<string,Action> test)
    {
        test("artisan manual shaping has real category speed and saved-mark target sequences",()=>{
            foreach(int id in new[]{0,1,24,25,27,28,29})
            {
                Check(Timing("ArtisanTimingSpeed",id)==.72,"weapon shaping retains the released strike speed");
                for(int strike=0;strike<3;strike++)Check(Timing("ArtisanTimingTarget",id,strike)==.5,"weapon timing retains the released center target");
            }
            foreach(int id in new[]{2,3,30,31,32})
            {
                Check(Timing("ArtisanTimingSpeed",id)==.48,"fitting moves more slowly than hammer strikes");
                Check(Timing("ArtisanTimingTarget",id,0)==.30&&Timing("ArtisanTimingTarget",id,1)==.70&&Timing("ArtisanTimingTarget",id,2)==.50,"fitting balances left right then center using saved progress");
            }
            foreach(int id in new[]{26,33,34,35,36,37})
            {
                Check(Timing("ArtisanTimingSpeed",id)==.55,"channel tracing has its own pace");
                Check(Timing("ArtisanTimingTarget",id,0)==.38&&Timing("ArtisanTimingTarget",id,1)==.62&&Timing("ArtisanTimingTarget",id,2)==.50,"engraving traces outer inner then center channels");
            }
        });
        test("artisan category marker and true-mark gold bounds use one truthful timing formula",()=>{
            Check(Math.Abs(Timing("ArtisanTimingMarker",0,1d)-.72)<.000000001&&Math.Abs(Timing("ArtisanTimingMarker",2,1d)-.48)<.000000001&&Math.Abs(Timing("ArtisanTimingMarker",26,1d)-.55)<.000000001,"same scene clock produces three genuinely different marker positions");
            foreach(int id in new[]{0,2,26})
            {
                double speed=Timing("ArtisanTimingSpeed",id);
                Check(Math.Abs(Timing("ArtisanTimingMarker",id,1.5/speed)-.5)<.000000001&&Math.Abs(Timing("ArtisanTimingMarker",id,2/speed))<.000000001,"triangle marker returns from the far edge and repeats");
                for(int strike=0;strike<3;strike++)
                {
                    double target=Timing("ArtisanTimingTarget",id,strike);
                    Check(Math.Abs(Timing("ArtisanTimingMarker",id,target/speed)-target)<.000000001,"native probes can aim the real category marker");
                    Check(Timing("ArtisanTimingAccuracy",id,strike,target)==1,"target center earns full accuracy");
                    Check(Math.Abs(Timing("ArtisanTimingAccuracy",id,strike,target-.09)-.82)<.000000001&&Math.Abs(Timing("ArtisanTimingAccuracy",id,strike,target+.09)-.82)<.000000001,"181.44 wide gold zone represents the real .82 threshold on a 1008 wide track");
                    Check(Timing("ArtisanTimingAccuracy",id,strike,target+.091)<.82&&Timing("ArtisanTimingAccuracy",id,strike,3d)==0,"outside gold never claims true placement");
                }
            }
            Check(Timing("ArtisanTimingMarker",2,double.NaN)==0&&Timing("ArtisanTimingMarker",26,double.PositiveInfinity)==0&&Timing("ArtisanTimingAccuracy",2,0,double.NaN)==0,"malformed clocks or marker samples cannot create quality");
            Check(Timing("ArtisanTimingTarget",2,-4)==.3&&Timing("ArtisanTimingTarget",26,99)==.5,"saved mark bounds stay safe");
        });
        test("artisan actual fitting feedback scores the moving saved-mark target after reload",()=>{
            var s=new WorkshopSimulation(new WorkshopState{Ingots=30,Gold=100});Check(Begin(s,2,0,0)&&s.StrikeMasterwork(1),"warm mail fittings");
            s.Advance(.21);Check(s.StrikeMasterwork(Timing("ArtisanTimingAccuracy",2,0,.5)),"first fitting mark at old center");
            Check(s.State.Artisan.Project.Quality==0&&Math.Abs(s.State.Artisan.Project.Feedback[0].Accuracy-.6)<.000000001,"old center is a real missed side seam");
            s.Advance(.21);Check(s.StrikeMasterwork(Timing("ArtisanTimingAccuracy",2,1,.7)),"balance opposite seam");s=Reload(s);
            Check(s.State.Artisan.Project.Strikes==2&&Timing("ArtisanTimingTarget",2,s.State.Artisan.Project.Strikes)==.5,"reload preserves next center fitting target");
            s.Advance(.21);Check(s.StrikeMasterwork(Timing("ArtisanTimingAccuracy",2,2,.5)),"seat center fitting");
            Check(s.State.Artisan.Project.Quality==2&&s.State.Artisan.Project.Stage==ArtisanStage.Quench,"category accuracy changes real saved quality and stage");
        });
        foreach(var overlap in new[]{new[]{25,39,53,67,81,95},new[]{32,46,60,74,88,102},new[]{37,51,65,79,93,107}})
        foreach(int recipeId in overlap)
        {
            int id=recipeId;
            test("artisan technique affinity offers a different earned build for natural-effect recipe "+id,()=>{
                var recipe=GameContent.Recipes[id];
                GearEffect expected=recipe.Effect==GearEffect.ArmorBreak?GearEffect.ChainLightning:recipe.Effect==GearEffect.Mend?GearEffect.Shelter:GearEffect.BraceBarrier;
                Check(Secondary(Preview(id,3,0,0,0,2))==expected,"penalized technique must offer a different useful effect than "+recipe.Effect);
                var option=WorkshopSimulation.TechniqueOptionsFor(id)[2];
                Check(option.Description.Contains(WorkshopSimulation.EffectDescription(expected)),"selected recipe description must explain the actual technique benefit");
                Check(option.Name!=WorkshopSimulation.TechniqueOptionsFor(recipe.Slot==GearSlot.Armor?2:recipe.Slot==GearSlot.Accessory?33:0)[2].Name,"recipe-aware name must distinguish the offered affinity");
                foreach(int rune in new[]{0,1,2,3})
                {
                    var state=new WorkshopState{Ingots=30,Gold=100,Campaign=new CampaignState()};
                    for(int region=0;region<6;region++){state.Campaign.Materials[region]=50;for(int step=0;step<3;step++)state.Campaign.QuestClaims[GameContent.MainQuestId(region,step)]=true;}
                    var s=new WorkshopSimulation(state);
                    Check(Begin(s,id,0,2),"begin alternative affinity");s=Reload(s);Finish(s,0,rune);var actual=s.State.Inventory[0];var preview=Preview(id,3,0,0,rune,2);
                    GearEffect finalEffect=rune==1?GearEffect.ChainLightning:rune==2?GearEffect.BraceBarrier:rune==3?GearEffect.LifeOnKill:recipe.Effect;
                    GearEffect secondary=expected==finalEffect?recipe.Effect:expected;
                    Check(WorkshopSimulation.ItemEffect(actual)==finalEffect&&WorkshopSimulation.ItemEffect(preview)==finalEffect,"primary recipe or rune remains compatible in preview and delivery");
                    Check(Secondary(actual)==secondary&&Secondary(preview)==secondary&&secondary!=finalEffect,"secondary specialization survives every explicit rune without duplicating it");
                    Check(WorkshopSimulation.ItemBuildDescription(actual).Contains(WorkshopSimulation.EffectDescription(secondary)),"delivered build explains the surviving specialization benefit");
                    Check(s.ItemPower(actual)==s.ItemPower(preview)&&s.ItemGuard(actual)==s.ItemGuard(preview)&&Technique(actual)==2,"saved alternative retains exact delivered stats and technique ID");
                    if(id==25)Check(s.ItemPower(actual)==19&&s.ItemGuard(actual)==1,"warhammer pays three power for the new lightning benefit");
                    if(id==32)Check(s.ItemPower(actual)==0&&s.ItemGuard(actual)==7,"runic robe pays one guard for the new shelter benefit");
                    if(id==37)Check(s.ItemPower(actual)==2&&s.ItemGuard(actual)==0,"hunter sigil pays one power and one guard for the new barrier benefit");
                    Check(s.State.Ingots==30-recipe.Ingots&&s.State.Gold==(rune==0?100:88)&&s.State.TotalCrafted==1&&!s.CompleteMasterwork(),"alternative does not add or repeat any payment or output");
                }
            });
        }
        test("artisan all current natural-affinity overlaps receive a nonduplicate technique",()=>{
            foreach(var recipe in GameContent.Recipes)
            {
                var method=WorkshopSimulation.ArtisanMethodFor(recipe.Id);var affinity=method==ArtisanMethod.Strike?GearEffect.ArmorBreak:method==ArtisanMethod.Fitting?GearEffect.Mend:GearEffect.Focus;
                if(recipe.Effect==affinity)Check(Secondary(Preview(recipe.Id,3,0,0,0,2))!=recipe.Effect,"natural affinity overlap remains a strict downgrade for "+recipe.Id);
            }
        });
        test("artisan every recipe retains a distinct specialized benefit with each primary rune",()=>{
            foreach(var recipe in GameContent.Recipes)for(int rune=0;rune<4;rune++)
            {
                var ordinary=Preview(recipe.Id,3,0,0,rune,0);var specialized=Preview(recipe.Id,3,0,0,rune,2);
                var primary=WorkshopSimulation.ItemEffect(ordinary);var secondary=Secondary(specialized);
                Check(WorkshopSimulation.ItemEffect(specialized)==primary,"technique does not replace the chosen primary affinity");
                Check(secondary!=GearEffect.None&&secondary!=primary&&Secondary(ordinary)==GearEffect.None,"paid stat penalty buys a real additional effect for recipe "+recipe.Id+" rune "+rune);
                Check(WorkshopSimulation.ItemBuildDescription(specialized).Contains(WorkshopSimulation.EffectDescription(primary))&&WorkshopSimulation.ItemBuildDescription(specialized).Contains(WorkshopSimulation.EffectDescription(secondary)),"build text discloses both primary and secondary effects");
            }
            Check(Secondary(null)==GearEffect.None&&Secondary(new CraftedItem{RecipeId=0,Technique=2})==GearEffect.None,"ordinary items never gain specialized secondary affinity");
        });
        test("artisan paid-rune specialization enters the actual expedition snapshot",()=>{
            foreach(int id in new[]{0,2,25,26,32,37})foreach(int rune in new[]{1,2,3})
            {
                var s=new WorkshopSimulation(new WorkshopState{Ingots=30,Gold=100});Check(Begin(s,id,0,2),"begin surviving benefit");Finish(s,0,rune);var item=s.State.Inventory[0];
                var hero=s.State.Campaign.Heroes[0];hero.Weapon=new CraftedItem{RecipeId=0,Name="Iron Sword",Quality=3};
                var slot=GameContent.Recipes[id].Slot;if(slot==GearSlot.Weapon)hero.Weapon=item;else if(slot==GearSlot.Armor)hero.Armor=item;else hero.Accessory=item;
                Check(s.StartSaga(0,0),"start expedition with specialized gear");var unit=s.State.Saga.Run.Units[0];
                Check((unit.Effects&(1<<(int)Secondary(item)))!=0&&(unit.Effects&(1<<(int)WorkshopSimulation.ItemEffect(item)))!=0,"combat snapshot must preserve both real affinity effects");
            }
        });
        test("artisan paid Storm rune retains a real armor-breaking damage tradeoff",()=>{
            Check(Math.Abs(ArmoredSkillDamage(0)-29.6)<.000001,"balanced Storm sword has 29.6 focused damage against 40 guard");
            Check(Math.Abs(ArmoredSkillDamage(2)-42.4)<.000001,"lower-power specialized Storm sword deals 42.4 with its surviving armor break");
        });
        test("artisan guarded weapon technique exchanges delivered power for actual guard",()=>{
            var s=new WorkshopSimulation(new WorkshopState{Ingots=20,Gold=100});
            var preview=Preview(0,0,0,0,0,1);
            Check(WorkshopSimulation.ArtisanStat(preview,true)==10&&WorkshopSimulation.ArtisanStat(preview,false)==2,"Iron sword gives up 2 power to gain 2 guard");
            Check(Begin(s,0,0,1),"choose guarded balance");s=Reload(s);Check(Technique(s.State.Artisan.Project)==1,"paid technique survives reload");
            Finish(s);var item=s.State.Inventory[0];
            Check(Technique(item)==1&&Technique(s.State.Artisan.LastCreation)==1,"delivery and history retain technique");
            Check(s.ItemPower(item)==17&&s.ItemGuard(item)==4,"quality three guarded sword has hand-checked 17 power and 4 guard");
        });
        test("artisan armor fitting and engraving techniques have category-specific real tradeoffs",()=>{
            var armor=Preview(2,3,0,0,0,1);var staff=Preview(26,3,0,0,0,1);var charm=Preview(33,3,0,0,0,1);
            Check(WorkshopSimulation.ArtisanStat(armor,true)==2&&WorkshopSimulation.ArtisanStat(armor,false)==8,"mobile mail gives 2 power for 2 guard");
            Check(WorkshopSimulation.ArtisanStat(staff,true)==16&&WorkshopSimulation.ArtisanStat(staff,false)==5,"sheltered staff converts power to guard");
            Check(WorkshopSimulation.ArtisanStat(charm,true)==2&&WorkshopSimulation.ArtisanStat(charm,false)==2,"engraved charm converts power to guard");
            Check(Secondary(Preview(0,3,0,0,0,2))==GearEffect.ArmorBreak,"weapon technique grants armor break");
            Check(Secondary(Preview(2,3,0,0,0,2))==GearEffect.Mend,"armor technique grants mend");
            Check(Secondary(Preview(26,3,0,0,0,2))==GearEffect.Focus,"engraving technique grants focus");
            var serrated=Preview(0,3,0,0,0,2);var mending=Preview(2,3,0,0,0,2);var focus=Preview(33,3,0,0,0,2);
            Check(WorkshopSimulation.ArtisanStat(serrated,true)==18&&WorkshopSimulation.ArtisanStat(mending,false)==8,"technique effects give up real power or guard");
            Check(WorkshopSimulation.ArtisanStat(focus,true)==2&&WorkshopSimulation.ArtisanStat(focus,false)==0,"focused charm pays both stat penalties");
            Check(WorkshopSimulation.ItemEffect(Preview(26,3,0,0,1,2))==GearEffect.ChainLightning,"paid rune replaces technique effect");
        });
        test("artisan every category technique preview equals delivery without a second charge",()=>{
            foreach(int id in new[]{0,2,24,25,26,29,31,32,33})for(int technique=0;technique<3;technique++)
            {
                var s=new WorkshopSimulation(new WorkshopState{Ingots=30,Gold=100});
                Check(Begin(s,id,1,technique),"begin category technique");int ingots=s.State.Ingots;
                Check(!Begin(s,id,2,technique)&&s.State.Ingots==ingots,"pending choice cannot charge twice");
                s=Reload(s);Finish(s,2,2);var actual=s.State.Inventory[0];var preview=Preview(id,3,1,2,2,technique);
                Check(s.ItemPower(actual)==s.ItemPower(preview)&&s.ItemGuard(actual)==s.ItemGuard(preview)&&WorkshopSimulation.ItemEffect(actual)==WorkshopSimulation.ItemEffect(preview),"category preview equals delivery");
                Check(s.State.Ingots==ingots&&s.State.Gold==88&&s.State.TotalCrafted==1&&!s.CompleteMasterwork(),"materials rune and output settle once");
            }
        });
        test("artisan legacy and malformed technique fields normalize without inventing choices",()=>{
            var s=new WorkshopSimulation(new WorkshopState{Ingots=20,Gold=100});Check(s.BeginMasterwork(0,0),"legacy begin");
            Check(Technique(s.State.Artisan.Project)==0,"old API defaults balanced");Technique(s.State.Artisan.Project,99);s=Reload(s);
            Check(Technique(s.State.Artisan.Project)==0,"invalid pending technique becomes balanced");
            Finish(s);var item=s.State.Inventory[0];Technique(item,-1);Technique(s.State.Artisan.LastCreation,99);s=Reload(s);
            Check(Technique(s.State.Inventory[0])==0&&Technique(s.State.Artisan.LastCreation)==0,"invalid delivered/history choice becomes balanced");
            var ordinary=new CraftedItem{RecipeId=0,Quality=3};Technique(ordinary,1);s.State.Inventory.Add(ordinary);s=Reload(s);
            Check(Technique(s.State.Inventory[1])==0&&s.ItemPower(s.State.Inventory[1])==21,"ordinary old gear never receives masterwork technique");
            int before=s.State.Ingots;Check(!Begin(s,0,0,3)&&s.State.Ingots==before&&Preview(0,3,0,0,0,3)==null,"bad technique is a no-op");
        });
        test("artisan category methods and saved shaping stages select the relevant unfinished family",()=>{
            var family=typeof(WorkshopSimulation).GetMethod("ArtisanWorkpieceFor");var method=typeof(WorkshopSimulation).GetMethod("ArtisanMethodFor");var stage=typeof(WorkshopSimulation).GetMethod("ArtisanWorkpieceStage");
            Check(family!=null&&method!=null&&stage!=null,"workpiece category and stage must come from the saved project");
            foreach(var fixture in new[]{new[]{0,0,0},new[]{25,1,0},new[]{29,2,0},new[]{2,3,1},new[]{24,4,0},new[]{31,5,1},new[]{32,6,1},new[]{33,7,2},new[]{26,2,2}})
            {
                Check(Convert.ToInt32(family.Invoke(null,new object[]{fixture[0]}))==fixture[1],"recipe uses its unfinished family");
                Check(Convert.ToInt32(method.Invoke(null,new object[]{fixture[0]}))==fixture[2],"recipe gives the right crafting action");
            }
            Check((int)stage.Invoke(null,new object[]{ArtisanStage.Heat,0})==0&&(int)stage.Invoke(null,new object[]{ArtisanStage.Shape,1})==1&&(int)stage.Invoke(null,new object[]{ArtisanStage.Shape,2})==2,"real strikes advance material rough and shaped silhouettes");
            Check((int)stage.Invoke(null,new object[]{ArtisanStage.Engrave,3})==3&&(int)stage.Invoke(null,new object[]{ArtisanStage.Ready,3})==4,"cooling and assembly occur after shaping");
        });
        test("artisan guided placement saves honest steady feedback and never works without a tap",()=>{
            var s=new WorkshopSimulation(new WorkshopState{Ingots=20,Gold=100,ReducedMotion=true});Check(Begin(s,26,0,1),"start guided staff");
            s.State.Artisan.Guided=true;Check(s.StrikeMasterwork(1),"warm fittings");s.Advance(600);s=Reload(s);
            Check(s.State.Artisan.Guided&&s.State.Artisan.Project.Strikes==0&&s.State.Artisan.Project.Quality==0,"saved assistance does not manufacture marks or quality");
            Check(s.StrikeMasterwork(.86),"manual steady trace");s=Reload(s);var mark=s.State.Artisan.Project.Feedback[0];
            Check(mark.Accuracy==.86&&mark.QualityBefore==0&&mark.QualityAfter==1,"steady placement never claims a perfect mark");
            Check(!s.StrikeMasterwork(.86)&&s.State.Artisan.Project.Strikes==1,"guided input still respects the real strike cooldown");
        });
    }
}
