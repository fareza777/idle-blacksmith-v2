using System;
using System.Text.Json;
using ForgeEmpire;

internal static class ArtisanTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static WorkshopSimulation Reload(WorkshopSimulation sim){var j=new JsonSerializerOptions{IncludeFields=true};return new WorkshopSimulation(JsonSerializer.Deserialize<WorkshopState>(JsonSerializer.Serialize(sim.State,j),j));}
    static WorkshopSimulation Project(int alloy=0)
    {
        var s=new WorkshopSimulation(new WorkshopState{Ingots=20,Gold=100});Check(s.BeginMasterwork(0,alloy),"start");return s;
    }
    static void Shape(WorkshopSimulation s)
    {
        Check(s.StrikeMasterwork(1),"heat");for(int i=0;i<3;i++){s.Advance(.21);Check(s.StrikeMasterwork(1),"shape");}
    }
    public static void Run(Action<string,Action> test)
    {
        test("artisan equip eligibility follows the actual party lock and storage ownership",()=>{
            var s=Project();Shape(s);s.QuenchMasterwork(0);s.EngraveMasterwork(0);s.CompleteMasterwork();var item=s.State.Inventory[0];
            Check(s.CreationEquipBlocker(item)=="","kept result can equip");
            s.State.Campaign.Heroes[0].Weapon=new CraftedItem{RecipeId=0,Name="Iron Sword",Quality=0};
            Check(s.StartSaga(0,0)&&s.CreationEquipBlocker(item)!=""&&!s.TryEquip(0),"saga prevents equip and explains why");
            s.State.Saga.Run=null;s.State.Campaign.Battle.Active=true;
            Check(s.CreationEquipBlocker(item)!=""&&!s.TryEquip(0),"legacy expedition also locks equipment");
            s.State.Campaign.Battle.Active=false;Check(s.TryEquip(0)&&s.CreationEquipBlocker(item)!="","removed result cannot equip twice");
        });
        test("artisan normalization safely advances a saved completed shaping stage without fake strikes",()=>{
            var s=Project();var p=s.State.Artisan.Project;p.Stage=ArtisanStage.Shape;p.Strikes=3;p.Quality=2;p.Feedback=null;
            s=Reload(s);p=s.State.Artisan.Project;
            Check(p.Stage==ArtisanStage.Quench&&!s.StrikeMasterwork(1)&&p.Quality==2,"completed shaping cannot write a fourth strike");
            Check(p.Feedback!=null&&p.Feedback.Length==3&&p.Feedback[0]==null&&p.Feedback[2]==null,"legacy save does not invent technique");
        });
        test("artisan persists actual missed and accurate strike feedback without inventing quality",()=>{
            var s=Project();Check(s.StrikeMasterwork(1),"heat");
            s.Advance(.21);Check(s.StrikeMasterwork(.4),"wide strike");
            s.Advance(.21);Check(s.StrikeMasterwork(.9),"accurate strike");
            s=Reload(s);var p=s.State.Artisan.Project;
            Check(p.Feedback!=null&&p.Feedback.Length==3&&p.Feedback[0]!=null&&p.Feedback[1]!=null&&p.Feedback[2]==null,"only real strikes saved");
            Check(p.Feedback[0].Strike==1&&p.Feedback[0].Accuracy==.4&&p.Feedback[0].QualityBefore==0&&p.Feedback[0].QualityAfter==0,"missed strike keeps quality");
            Check(p.Feedback[1].Strike==2&&p.Feedback[1].Accuracy==.9&&p.Feedback[1].QualityBefore==0&&p.Feedback[1].QualityAfter==1,"accurate strike adds real quality");
            Check(!s.StrikeMasterwork(1)&&p.Feedback[2]==null,"rejected strike has no feedback");
            s.Advance(.21);s.StrikeMasterwork(.82);s.QuenchMasterwork(0);s.EngraveMasterwork(0);s.CompleteMasterwork();s=Reload(s);
            var item=s.State.Inventory[0];var finished=s.State.Artisan.LastCreation;
            Check(finished!=null&&finished.CreationNumber==item.Provenance&&finished.Feedback[2].Accuracy==.82&&finished.Quality==2,"delivered creation retains true technique after reload");
        });
        test("artisan quote exposes exact unpaid alloy and rare cost without charging",()=>{
            var s=new WorkshopSimulation(new WorkshopState{Ingots=10,Gold=100});
            var quote=s.QuoteMasterwork(0,1);
            Check(quote.CanBegin&&!quote.Reserved&&quote.Ingots==4&&quote.RareCost==0&&s.State.Ingots==10,"two recipe and two alloy ingots quoted once");
            Check(s.BeginMasterwork(0,1)&&s.State.Ingots==6,"paid amount matches quote");
            s=new WorkshopSimulation(new WorkshopState{Ingots=20,Gold=100});s.State.Campaign.Chapter=1;
            int id=4;var recipe=GameContent.Recipes[id];s.State.Campaign.Materials[0]=0;
            quote=s.QuoteMasterwork(id,2);
            Check(!quote.CanBegin&&quote.Ingots==recipe.Ingots+2&&quote.RareCost==recipe.RareCost&&quote.RareRegion==0,"rare cost disclosed even when unavailable");
            s.State.Campaign.Materials[0]=20;quote=s.QuoteMasterwork(id,2);int ingots=s.State.Ingots,rare=s.State.Campaign.Materials[0];
            Check(quote.CanBegin&&s.BeginMasterwork(id,2)&&s.State.Ingots==ingots-quote.Ingots&&s.State.Campaign.Materials[0]==rare-quote.RareCost,"rare payment matches quote");
        });
        test("artisan takeover quotes only unpaid alloy and preserves reserved quality",()=>{
            var s=new WorkshopSimulation(new WorkshopState{Ingots=10,Gold=100});s.TryStart(Station.Forge);s.TryStrike(1);
            var quote=s.QuoteMasterwork(0,2);
            Check(quote.Reserved&&quote.CanBegin&&quote.Ingots==2&&quote.RareCost==0&&quote.Quality==1,"reserved piece quoted accurately");
            int before=s.State.Ingots;Check(s.BeginMasterwork(0,2)&&s.State.Ingots==before-2&&s.State.Artisan.Project.Quality==1,"quote is paid once");
            Check(!s.QuoteMasterwork(0,0).CanBegin,"active project cannot charge again");
        });
        test("artisan choice preview matches the delivered item for every alloy temper and rune",()=>{
            for(int a=0;a<3;a++)for(int t=0;t<3;t++)for(int r=0;r<4;r++)
            {
                var s=Project(a);Shape(s);s.QuenchMasterwork(t);s.EngraveMasterwork(r);
                var preview=WorkshopSimulation.PreviewMasterwork(0,3,a,t,r);
                Check(preview!=null&&s.CompleteMasterwork(),"valid preview and finish");var item=s.State.Inventory[0];
                Check(s.ItemPower(preview)==s.ItemPower(item)&&s.ItemGuard(preview)==s.ItemGuard(item)&&WorkshopSimulation.ItemEffect(preview)==WorkshopSimulation.ItemEffect(item),"preview equals delivered build");
            }
            Check(WorkshopSimulation.PreviewMasterwork(-1,3,0,0,0)==null&&WorkshopSimulation.PreviewMasterwork(0,3,3,0,0)==null,"invalid build has no misleading preview");
        });
        test("artisan can take over a worker's reserved piece without paying the recipe twice",()=>{
            var s=new WorkshopSimulation(new WorkshopState{Ingots=4,Gold=100});Check(s.TryStart(Station.Forge),"worker reservation");int ingots=s.State.Ingots;
            s.PlanRecipe(1);Check(s.BeginMasterwork(0,0),"take over paid recipe");Check(s.State.Ingots==ingots&&s.State.SelectedRecipe==1&&s.State.PendingRecipe==0,"reservation and next plan preserved");
            Shape(s);s.QuenchMasterwork(0);s.EngraveMasterwork(0);Check(s.CompleteMasterwork(),"finish takeover");Check(s.State.TotalCrafted==1&&s.State.Inventory[0].RecipeId==0&&s.State.Inventory[0].Masterwork,"one true artisan output");
        });
        test("artisan ordered stages survive reload and output exactly one protected piece",()=>{
            var s=Project(1);int ingots=s.State.Ingots;Check(!s.QuenchMasterwork(1)&&!s.EngraveMasterwork(1)&&!s.CompleteMasterwork(),"stage bypass");
            Shape(s);s=Reload(s);Check(s.State.Artisan.Project.Stage==ArtisanStage.Quench,"saved quench");
            Check(s.QuenchMasterwork(2)&&!s.QuenchMasterwork(0),"one quench");s=Reload(s);double gold=s.State.Gold;
            Check(s.EngraveMasterwork(1)&&!s.EngraveMasterwork(1)&&s.State.Gold==gold-12,"one rune cost");s=Reload(s);
            Check(s.CompleteMasterwork()&&!s.CompleteMasterwork(),"one completion");s=Reload(s);s.Advance(200);
            Check(s.State.TotalCrafted==1&&s.State.Inventory.Count==1&&s.State.Ingots==ingots,"one paid output");var item=s.State.Inventory[0];
            Check(item.Masterwork&&item.Protected&&item.Alloy==1&&item.Temper==2&&item.Rune==1&&item.Quality==3,"saved recipe metadata");
            Check(s.ItemPower(item)>s.ItemPower(new CraftedItem{RecipeId=0,Quality=3}),"real offensive benefit");
        });
        test("artisan rejects spam nonfinite accuracy and recipe mutation",()=>{
            var s=Project();Check(s.PlanRecipe(1),"plan next");Check(!s.StrikeMasterwork(double.NaN)&&!s.StrikeMasterwork(-1),"bad accuracy");
            Check(s.StrikeMasterwork(1)&&!s.StrikeMasterwork(1),"cooldown");Check(!s.TryStrike(1)&&!s.TryBeginTempering(),"legacy bypass");
            for(int i=0;i<3;i++){s.Advance(.21);s.StrikeMasterwork(1);}s.QuenchMasterwork(0);s.EngraveMasterwork(0);s.CompleteMasterwork();
            Check(s.State.Inventory[0].RecipeId==0&&s.State.SelectedRecipe==1,"paid recipe immutable");
        });
        test("artisan keeps workers productive while player choices wait offline",()=>{
            var s=Project();s.State.Automated[0]=true;s.State.Automated[1]=true;s.Advance(28800);
            Check(s.State.TotalMined>100&&s.State.Ingots>100&&s.State.TotalCrafted==0,"independent production");
            s=Reload(s);Check(s.MasterworkActive&&s.State.Artisan.Project.Stage==ArtisanStage.Heat,"paid wait survives");
        });
        for(int alloy=0;alloy<3;alloy++)for(int temper=0;temper<3;temper++)
        {
            int a=alloy,t=temper;test("artisan alloy "+a+" temper "+t+" has truthful stats",()=>{
                var item=new CraftedItem{RecipeId=2,Quality=3,Masterwork=true,Alloy=a,Temper=t};
                int baseline=10;double expected=baseline*(a==1?.9:a==2?1.25:1)*(t==1?1.15:t==2?.9:1);
                Check(WorkshopSimulation.ArtisanStat(item,false)==(int)Math.Floor(expected),"guard tradeoff");
                Check(WorkshopSimulation.ItemBuildDescription(item).Contains(WorkshopSimulation.TemperOptions[t].Name),"truthful description");
            });
        }
        test("artisan full storage and insufficient alloy never consume anything",()=>{
            var s=new WorkshopSimulation(new WorkshopState{Ingots=3,Gold=10});Check(!s.BeginMasterwork(0,1)&&s.State.Ingots==3,"extra alloy checked first");
            s.State.Ingots=10;for(int i=0;i<120;i++)s.State.Inventory.Add(new CraftedItem{RecipeId=0,Name="Iron Sword",Value=24});
            Check(!s.BeginMasterwork(0,0)&&s.State.Ingots==10,"reserved capacity");
        });
    }
}
