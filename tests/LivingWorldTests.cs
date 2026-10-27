using System;
using ForgeEmpire;

internal static class LivingWorldTests
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static WorkshopSimulation OpenRegion(int region)
    {
        var sim=new WorkshopSimulation(new WorkshopState{Gold=100000,Ingots=1000});
        for(int r=0;r<=region;r++)
        {
            if(r<region)for(int q=0;q<3;q++)sim.State.Campaign.QuestClaims[GameContent.MainQuestId(r,q)]=true;
            sim.State.Campaign.QuestClaims[GameContent.SideQuestId(r,1)]=true;
            sim.State.Campaign.Materials[r]=1000;
        }
        return new WorkshopSimulation(sim.State);
    }
    public static void Run(Action<string,Action> test)
    {
        test("Living world fully restored projects have no next cost",()=>{
            var sim=OpenRegion(0);
            for(int tier=1;tier<=3;tier++)Check(sim.TryRestoreProject(0),"tier "+tier);
            Check(sim.RestorationGoldCost(0)==0&&sim.RestorationMaterialCost(0)==0,"completed project must not quote a fourth payment");
        });
        test("Living world all six landmarks settle three earned tiers once",()=>{
            for(int region=0;region<6;region++)
            {
                var sim=OpenRegion(region);
                for(int tier=1;tier<=3;tier++)
                {
                    int gold=sim.RestorationGoldCost(region),materials=sim.RestorationMaterialCost(region),stock=sim.State.Campaign.Materials[region];double balance=sim.State.Gold;
                    Check(sim.TryRestoreProject(region),"earned tier accepted");
                    Check(sim.State.World.Projects[region]==tier&&sim.State.Gold==balance-gold&&sim.State.Campaign.Materials[region]==stock-materials,"tier and costs settle together");
                    sim=new WorkshopSimulation(sim.State);Check(sim.State.World.Projects[region]==tier,"earned tier survives normalization");
                }
                double paid=sim.State.Gold;int supplies=sim.State.Campaign.Materials[region];
                Check(!sim.TryRestoreProject(region)&&sim.State.Gold==paid&&sim.State.Campaign.Materials[region]==supplies,"completed project is a no-op");
            }
        });
        test("Living world old saves cannot invent unlocked restoration",()=>{
            var sim=new WorkshopSimulation(new WorkshopState{World=new WorldState{Projects=new[]{99,-2,1,2,3,1}}});
            for(int region=0;region<6;region++)Check(sim.State.World.Projects[region]==0,"unearned restoration removed");
            double gold=sim.State.Gold;Check(!sim.TryRestoreProject(0)&&sim.State.Gold==gold,"rescue gate cannot charge");
        });
        test("Living world previews expose real recipe sources and contract terms without payment",()=>{
            var sim=OpenRegion(1);Check(sim.TryAcceptCommission(0),"accept order");var order=sim.ActiveCommission;
            double gold=sim.State.Gold;int ingots=sim.State.Ingots;var links=sim.WorldGoalLinks;
            Check(links.Length==6,"milestone, contract, material, recipe, guardian and restoration links");
            Check(links[1].RecipeId==order.RecipeId&&links[1].Detail.Contains(order.Gold+" gold"),"actual contract reward");
            Check(!links[1].Eligible&&links[1].Blocker.Length>0,"undeliverable order is clearly blocked");
            Check(links[3].RecipeId==order.RecipeId&&links[3].Detail.Contains(sim.RecipeCost(order.RecipeId)),"recipe costs follow active order");
            Check(links[2].Region==GameContent.Recipes[order.RecipeId].Region-1,"rare ingredient points at its supplying region");
            Check(sim.State.Gold==gold&&sim.State.Ingots==ingots,"preview never settles an action");
        });
        test("Living world restoration quote agrees with insufficient and exact payments",()=>{
            var sim=OpenRegion(0);var preview=sim.PreviewRestoration(0);Check(preview.Eligible&&preview.NextStage==1,"earned first stage");
            sim.State.Gold=preview.GoldCost-1;int stock=sim.State.Campaign.Materials[0];
            Check(!sim.PreviewRestoration(0).Eligible&&!sim.TryRestoreProject(0)&&sim.State.Campaign.Materials[0]==stock,"insufficient gold no-op");
            sim.State.Gold=preview.GoldCost;sim.State.Campaign.Materials[0]=preview.MaterialCost;
            Check(sim.PreviewRestoration(0).Eligible&&sim.TryRestoreProject(0),"exact cost accepted");
            Check(sim.State.Gold==0&&sim.State.Campaign.Materials[0]==0,"exact stock deducted once");
            Check(!sim.TryRestoreProject(0)&&sim.State.World.Projects[0]==1,"next tier cannot reuse previous payment");
        });
        test("Living world legendary links never claim unearned guardian or missing creation",()=>{
            var sim=OpenRegion(0);Check(sim.LegendaryContractBlocker(0).Length>0&&!sim.ClaimLegendaryContract(0),"guardian gate");
            sim.State.Saga.BossMastery[0]=1;Check(sim.LegendaryContractBlocker(0).Contains("quality 3"),"actual creation gate");
            sim.State.Inventory.Add(new CraftedItem{RecipeId=sim.LegendaryRecipeId(0),Quality=3,Masterwork=true});
            Check(sim.LegendaryContractBlocker(0)==""&&sim.ClaimLegendaryContract(0),"earned delivery");
            Check(sim.LegendaryContractBlocker(0).Length>0&&!sim.ClaimLegendaryContract(0),"once-only claim");
        });
        test("Living world long horizon counts every earned town stage",()=>{
            var sim=OpenRegion(5);
            for(int step=0;step<3;step++)sim.State.Campaign.QuestClaims[GameContent.MainQuestId(5,step)]=true;
            for(int region=0;region<6;region++)sim.State.World.Projects[region]=1;
            sim=new WorkshopSimulation(sim.State);
            Check(sim.LongTermGoal.Contains("6 / 18 town stages"),"first tiers must not imply six completed restorations");
        });
        test("Living world saved journey preview includes earned branch bonuses without paying",()=>{
            var sim=OpenRegion(0);sim.State.ChroniclesEnabled=true;sim.State.Saga.Mastery=9;
            sim.State.Campaign.Heroes[0].Weapon=WorkshopSimulation.PreviewMasterwork(0,3,0,0,0);
            Check(sim.StartSaga(0,0,2),"start challenge");var run=sim.State.Saga.Run;
            run.BonusGold=20;run.BonusMaterial=2;double gold=sim.State.Gold;int materials=sim.State.Campaign.Materials[0];
            var preview=sim.PreviewLivingJourneyRewards();
            Check(preview.Gold==(int)((55+20)*1.7)&&preview.Material==7,"saved bonus and challenge quote");
            Check(sim.State.Gold==gold&&sim.State.Campaign.Materials[0]==materials,"conditional preview cannot settle rewards");
            run.Phase=SagaPhase.Result;run.Won=false;run.RewardGold=0;run.RewardMaterial=0;
            preview=sim.PreviewLivingJourneyRewards();Check(preview.Gold==0&&preview.Material==0,"defeated run has no victory forecast");
        });
    }
}
