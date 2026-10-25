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
