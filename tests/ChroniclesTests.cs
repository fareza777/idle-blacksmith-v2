using System;
using System.Text.Json;
using ForgeEmpire;

internal static class ChroniclesTests
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static WorkshopSimulation Reload(WorkshopSimulation sim)
    {
        var options=new JsonSerializerOptions{IncludeFields=true};
        return new WorkshopSimulation(JsonSerializer.Deserialize<WorkshopState>(JsonSerializer.Serialize(sim.State,options),options));
    }
    public static void Run(Action<string,Action> test)
    {
        test("Chronicles expands content with stable original identities",()=>{
            Check(GameContent.Recipes.Length==108&&GameContent.Regions.Length==6&&GameContent.Heroes.Length==6,"expanded catalog");
            Check(ChronicleContent.Enemies.Length==48&&ChronicleContent.Bosses.Length==12,"enemy catalog");
            Check(GameContent.Recipes[0].Name=="Iron Sword"&&GameContent.Recipes[19].Name=="Nightstar Ward","old recipe identities");
            Check(GameContent.Quests[14].Title=="A Town Beneath the Stars"&&GameContent.MainQuestId(5,2)==32,"old and new quest IDs");
        });
        test("Chronicles migrates completed original campaign without losing collection",()=>{
            var state=new WorkshopState{Campaign=new CampaignState{QuestClaims=new bool[30],Heroes=new HeroState[3],Materials=new int[5]}};
            for(int i=0;i<15;i++)state.Campaign.QuestClaims[i]=true;
            state.Inventory.Add(new CraftedItem{Name="Nightstar Axe",RecipeId=17,Value=404,Quality=3});
            var sim=new WorkshopSimulation(state);
            Check(sim.UnlockedRegion==5&&!sim.CampaignComplete,"sixth frontier opens");
            Check(sim.State.Inventory[0].RecipeId==17&&sim.State.Campaign.Heroes.Length==6,"collection and hero expansion");
            Check(Reload(sim).State.Inventory[0].Quality==3,"round trip");
        });
        test("Masterwork reserves once and waits through offline time",()=>{
            var sim=new WorkshopSimulation(new WorkshopState{Ingots=10,Gold=100});
            Check(sim.BeginMasterwork(0,1),"begin");int ingots=sim.State.Ingots;
            sim.Advance(28800);sim=Reload(sim);
            Check(sim.MasterworkActive&&sim.State.TotalCrafted==0&&sim.State.Ingots==ingots,"wait at heat");
            Check(!sim.BeginMasterwork(0,1),"duplicate reservation rejected");
        });
    }
}
