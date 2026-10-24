using System;
using System.Reflection;
using System.Text.Json;
using ForgeEmpire;

internal static class ProgressionTests
{
    static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true };
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static object Read(object value,string name)
    {
        var type=value.GetType();var field=type.GetField(name);if(field!=null)return field.GetValue(value);
        var property=type.GetProperty(name);Check(property!=null,"Missing progression property: "+name);return property.GetValue(value);
    }
    static object Call(object value,string name,params object[] args)
    {
        var method=value.GetType().GetMethod(name);Check(method!=null,"Missing progression action: "+name);return method.Invoke(value,args);
    }
    static int Int(object value,string name){return (int)Read(value,name);}
    static string ActionOf(WorkshopSimulation sim){return sim.NextStep.Action.ToString();}
    static WorkshopSimulation Reload(WorkshopSimulation sim){return new WorkshopSimulation(JsonSerializer.Deserialize<WorkshopState>(JsonSerializer.Serialize(sim.State,Json),Json));}
    static CraftedItem Gear(int recipe,int quality=0){var r=GameContent.Recipes[recipe];return new CraftedItem{RecipeId=recipe,Name=r.Name,Quality=quality,Value=r.Value+r.Value*quality/4,Protected=true};}
    static WorkshopSimulation Chapter(int region)
    {
        var s=new WorkshopState{Gold=10000,Campaign=new CampaignState()};
        for(int i=0;i<region*3;i++)s.Campaign.QuestClaims[i]=true;
        return new WorkshopSimulation(s);
    }
    public static void Run(Action<string,Action> test)
    {
        test("journey exposes seven purposeful milestones in every chapter",delegate{
            for(int chapter=0;chapter<5;chapter++)
            {
                var sim=Chapter(chapter);
                Check(Int(sim,"JourneyMilestone")==chapter*7,"wrong first milestone in chapter "+chapter);
                Check(((string)Read(sim,"ChapterUnlock")).Length>15,"missing concrete unlock");
                Check((string)Call(sim,"MilestoneTitle",chapter*7+6)!="","missing boss destination");
            }
        });
        test("guided production names target recipe and exact material work",delegate{
            var sim=Chapter(0);
            Check(Int(sim.NextStep,"RecipeId")==0&&ActionOf(sim)=="Mine","no first sword project");
            sim.State.Ore=2;Check(ActionOf(sim)=="Smelt","ore not converted");
            sim.State.Ingots=2;Check(ActionOf(sim)=="Forge","ready materials not forged");
            sim.TryStart(Station.Forge);sim.Advance(5);
            Check(ActionOf(sim)=="Equip"&&Int(sim.NextStep,"ItemIndex")==0,"new item has no direct equip action");
        });
        test("equipment milestones survive changing heroes and save reload",delegate{
            var sim=Chapter(0);sim.State.Inventory.Add(Gear(0));sim.TryEquip(0);
            Check(Int(sim,"JourneyMilestone")==1,"equipped sword did not advance");
            Check(sim.TryRecruit(1)&&sim.SelectHero(1),"hero switch fixture");
            sim=Reload(sim);Check((bool)Call(sim,"MilestoneComplete",0),"achievement regressed after hero change");
        });
        test("rescue objective selects elite and boss objective selects guardian",delegate{
            var sim=Chapter(0);sim.State.Inventory.Add(Gear(0));sim.TryEquip(0);
            sim.State.Inventory.Add(Gear(2));sim.TryEquip(0);
            sim.State.Campaign.QuestClaims[0]=sim.State.Campaign.QuestClaims[1]=true;
            Check(Int(sim,"JourneyMilestone")==4&&Int(sim.NextStep,"Route")==1,"rescue not routed to elite");
            sim.State.Campaign.QuestClaims[16]=true;sim.TryAutomate(Station.Mine);
            Check(Int(sim,"JourneyMilestone")==6&&Int(sim.NextStep,"Route")==2,"guardian goal sent player back to supply trail");
        });
        test("first scout reward leads to armor rather than aimless repeat scouting",delegate{
            var sim=Chapter(0);sim.State.Inventory.Add(Gear(0));sim.TryEquip(0);
            sim.State.Campaign.QuestClaims[0]=sim.State.Campaign.QuestClaims[1]=true;
            Check(Int(sim,"JourneyMilestone")==3&&Int(sim.NextStep,"RecipeId")==2,"armor goal missing after first scout");
        });
        test("guild offers accept one contract and preserve its terms on chapter change",delegate{
            var sim=Chapter(0);var offer=Call(sim,"CommissionOffer",0);
            Check(Int(offer,"RecipeId")==0&&Int(offer,"Quantity")==1,"intro order too complex");
            Check((bool)Call(sim,"TryAcceptCommission",0),"accept failed");
            Check(!(bool)Call(sim,"TryAcceptCommission",1),"accepted overlapping order");
            sim.State.Campaign.QuestClaims[0]=sim.State.Campaign.QuestClaims[1]=sim.State.Campaign.QuestClaims[2]=true;
            sim=Reload(sim);var active=Read(sim,"ActiveCommission");
            Check(Int(active,"RecipeId")==0&&Int(active,"Quantity")==1,"chapter unlock rewrote accepted terms");
        });
        test("merchant and selected sale cannot take accepted order stock",delegate{
            var sim=Chapter(0);Call(sim,"TryAcceptCommission",0);
            var sword=Gear(0);sword.Protected=false;sim.State.Inventory.Add(sword);
            var armor=Gear(2);armor.Protected=false;sim.State.Inventory.Add(armor);
            Check((bool)Call(sim,"IsCommissionReserved",sword),"order not reserved");
            Check(sim.NextSaleIndex==1&&!sim.TrySellItem(0),"merchant can steal order item");
            Check(sim.TrySellItem(1),"unrelated sale blocked");
        });
        test("order delivery consumes required items and pays exactly once across reload",delegate{
            var sim=Chapter(0);Call(sim,"TryAcceptCommission",0);
            var active=Read(sim,"ActiveCommission");int gold=Int(active,"Gold"),rep=Int(active,"Reputation");
            sim.State.Inventory.Add(Gear(0));sim.State.Inventory.Add(Gear(2));
            Check(Int(sim,"CommissionProgress")==1,"ready count missing");
            double before=sim.State.Gold;int beforeRep=sim.State.Campaign.Reputation;
            Check((bool)Call(sim,"TryDeliverCommission"),"delivery failed");sim=Reload(sim);
            Check(!(bool)Call(sim,"TryDeliverCommission"),"paid completed order twice");
            Check(sim.State.Gold==before+gold&&sim.State.Campaign.Reputation==beforeRep+rep,"wrong payout");
            Check(sim.State.Inventory.Count==1&&sim.State.Inventory[0].RecipeId==2,"wrong stock consumed");
        });
        test("incomplete order has no partial consumption and cancel frees stock",delegate{
            var sim=Chapter(0);Call(sim,"TryAcceptCommission",1);
            sim.State.Inventory.Add(Gear(2));double before=sim.State.Gold;
            Check(!(bool)Call(sim,"TryDeliverCommission"),"incomplete batch delivered");
            Check(sim.State.Inventory.Count==1&&sim.State.Gold==before,"failed order consumed inputs");
            Check((bool)Call(sim,"CancelCommission"),"cancel failed");
            sim.State.Inventory[0].Protected=false;
            Check(sim.NextSaleIndex==0,"cancel did not release stock");
        });
        test("quality order consumes lowest qualifying quality and keeps masterworks",delegate{
            var sim=Chapter(0);Call(sim,"TryAcceptCommission",2);var order=Read(sim,"ActiveCommission");
            int recipe=Int(order,"RecipeId"),minimum=Int(order,"MinimumQuality");
            sim.State.Inventory.Add(Gear(recipe,3));sim.State.Inventory.Add(Gear(recipe,minimum));sim.State.Inventory.Add(Gear(recipe,0));
            Check((bool)Call(sim,"TryDeliverCommission"),"quality order failed");
            Check(sim.State.Inventory.Count==2&&sim.State.Inventory[0].Quality==3&&sim.State.Inventory[1].Quality==0,"used masterwork before lower qualifying item");
        });
        test("commission advice fixes wrong recipe and routes rare material source",delegate{
            var sim=Chapter(2);Call(sim,"TryAcceptCommission",0);
            sim.State.Campaign.Heroes[0].Weapon=Gear(4);sim.State.Campaign.Heroes[0].Armor=Gear(6);
            Check(Int(sim.NextStep,"RecipeId")==8,"not tracking accepted project");
            sim.State.Ingots=20;
            Check(ActionOf(sim)=="Adventure"&&Int(sim.NextStep,"Region")==1&&Int(sim.NextStep,"Route")==0,"rare material advice sent to wrong frontier");
        });
        test("legacy and malformed progression saves normalize without losing inventory",delegate{
            var sim=new WorkshopSimulation(JsonSerializer.Deserialize<WorkshopState>("{\"Gold\":73,\"Campaign\":{\"JourneyMilestones\":null},\"Inventory\":[{\"Name\":\"Iron Sword\",\"RecipeId\":0,\"Value\":24}]}",Json));
            Check(Int(sim,"JourneyMilestone")==0&&Read(sim,"ActiveCommission")==null,"legacy profile gained phantom order");
            Check(sim.State.Gold==73&&sim.State.Inventory.Count==1,"migration destroyed progress");
            var corrupted=JsonSerializer.Deserialize<WorkshopState>("{\"Contract\":{\"Active\":true,\"RecipeId\":999,\"Quantity\":-5,\"Region\":9,\"MinimumQuality\":99},\"ContractsCompleted\":-4}",Json);
            sim=new WorkshopSimulation(corrupted);Check(Read(sim,"ActiveCommission")==null,"malformed contract remained active");
        });
        test("completed campaign retains clear collection and contract goals",delegate{
            var sim=Chapter(4);for(int i=0;i<30;i++)sim.State.Campaign.QuestClaims[i]=true;
            sim=Reload(sim);Check(Int(sim,"JourneyMilestone")==35,"final campaign loops back to old chapter goals");
            Check(ActionOf(sim)=="Commission"&&((string)Read(sim,"GuildRank")).Length>0,"no playable endgame goal");
            Check((bool)Call(sim,"TryAcceptCommission",0),"endgame orders unavailable");
            Check(Int(sim.NextStep,"RecipeId")==16,"endgame order missing latest recipes");
        });
        test("guild reputation raises order complexity with explicit quality and quantity",delegate{
            var sim=Chapter(4);sim.State.Campaign.Reputation=300;
            var supply=Call(sim,"CommissionOffer",0);var challenge=Call(sim,"CommissionOffer",2);
            Check(Int(supply,"Quantity")==3&&Int(supply,"MinimumQuality")==1,"guild rank gives no new production target");
            Check(Int(challenge,"Quantity")==2&&Int(challenge,"MinimumQuality")==3,"masterwork challenge is missing");
            Check((bool)Call(sim,"TryAcceptCommission",2),"accept masterwork order");sim=Reload(sim);
            Check(Int(Read(sim,"ActiveCommission"),"MinimumQuality")==3,"reload discarded high rank order");
        });
        test("masterwork recipe collection remembers actual output quality through saves",delegate{
            var sim=Chapter(0);sim.State.Ingots=2;sim.TryStart(Station.Forge);
            for(int i=0;i<3;i++){sim.Advance(.21);sim.TryStrike(1);}sim.Advance(5);sim=Reload(sim);
            Check(Int(sim,"MasterworkRecipes")==1,"masterwork was not recorded");
            sim.State.Ingots=2;sim.TryStart(Station.Forge);sim.Advance(5);sim=Reload(sim);
            Check(Int(sim,"MasterworkRecipes")==1,"later common output erased mastery");
        });
        test("planning a new recipe preserves active work and changes the next automatic job",delegate{
            var sim=Chapter(0);sim.State.Ingots=10;sim.State.Automated[2]=true;sim.TryStart(Station.Forge);
            double duration=sim.GetDuration(Station.Forge);
            Check((bool)Call(sim,"PlanRecipe",2),"cannot plan next recipe");
            Check(sim.State.SelectedRecipe==2&&sim.State.PendingRecipe==0&&sim.GetDuration(Station.Forge)==duration,"planning changed reserved output");
            sim.Advance(duration);
            Check(sim.State.Inventory[0].RecipeId==0&&sim.State.PendingRecipe==2&&sim.State.Ingots==5,"worker ignored queued recipe");
        });
        test("guided gear survives automatic market and retains protection after leaving guide",delegate{
            var sim=Chapter(0);sim.State.Ingots=2;
            var reserve=sim.State.GetType().GetField("ReserveJourneyGear");Check(reserve!=null,"guided reservation absent");
            reserve.SetValue(sim.State,true);sim.State.GetType().GetField("ReservedJourneyRecipe").SetValue(sim.State,0);
            var sword=Gear(0);sword.Protected=false;sim.State.Inventory.Add(sword);
            Check(sim.NextSaleIndex==-1&&!sim.TrySellItem(0),"automatic market stole current hero project");
            Check(sim.TryStart(Station.Forge,true),"start protected project");reserve.SetValue(sim.State,false);sim.Advance(7);
            Check(sim.State.Inventory[1].Protected,"leaving guided mode lost reservation on active work");
        });
        test("fast workshops still allow three manual perfect hits without slowing automatic production",delegate{
            var sim=Chapter(0);sim.State.Levels[2]=25;sim.State.Ingots=10;
            var start=sim.GetType().GetMethod("TryStart");
            Check(start.GetParameters().Length==2,"manual tempering has no timing allowance");
            Check((bool)start.Invoke(sim,new object[]{Station.Forge,true}),"manual job did not start");
            Check(sim.GetDuration(Station.Forge)>=6,"manual window too short");
            sim.Advance(1.249);Check(sim.TryStrike(1),"first perfect hit");
            sim.Advance(1.25);Check(sim.TryStrike(1),"second perfect hit");
            sim.Advance(1.25);Check(sim.TryStrike(1),"third perfect hit");
            sim.Advance(6);Check(sim.State.Inventory[0].Quality==3,"fast workshop cannot create Masterwork");
            Check(sim.TryStart(Station.Forge)&&sim.GetDuration(Station.Forge)<2,"automatic production inherited manual timing floor");
        });
        test("manual tempering can take over a fast automatic job without paying materials twice",delegate{
            var sim=Chapter(0);sim.State.Levels[2]=25;sim.State.Ingots=2;sim.TryStart(Station.Forge);
            sim.Advance(sim.GetDuration(Station.Forge)*.9);
            Check((bool)Call(sim,"TryBeginTempering"),"cannot take over automatic forge");
            Check(sim.State.Ingots==0&&sim.GetProgress(Station.Forge)==0&&sim.GetDuration(Station.Forge)>=6,"takeover changed reserved inputs or missed its fresh timing window");
            Check(!(bool)Call(sim,"TryBeginTempering"),"manual job can be reset repeatedly");
            sim.Advance(1.249);sim.TryStrike(1);sim.Advance(1.25);sim.TryStrike(1);sim.Advance(1.25);sim.TryStrike(1);sim.Advance(6);
            Check(sim.State.Inventory.Count==1&&sim.State.Inventory[0].Quality==3,"takeover failed to produce one Masterwork");
        });
        test("new unarmed heroes can bootstrap late chapter materials and tracked orders without a blocked route",delegate{
            foreach(bool order in new[]{false,true})
            {
                var sim=Chapter(2);sim.State.Ingots=100;sim.TryRecruit(1);sim.SelectHero(1);
                if(order)Call(sim,"TryAcceptCommission",0);
                int actions=0;
                while(actions++<150)
                {
                    var next=sim.NextStep;string action=next.Action.ToString();
                    if(order&&Int(sim,"CommissionProgress")>=Int(Read(sim,"ActiveCommission"),"Quantity"))break;
                    if(!order&&sim.State.Campaign.Heroes[1].Weapon!=null&&GameContent.Recipes[sim.State.Campaign.Heroes[1].Weapon.RecipeId].Region>=2)break;
                    if(action=="Adventure")
                    {
                        Check(sim.ExpeditionBlocker(next.Region,next.Route)=="","advisor requested a blocked material route for unarmed hero");
                        Check(sim.StartExpedition(next.Region,next.Route),"bootstrap departure");
                    }
                    else if(action=="Battle")
                    {
                        int ticks=0;while(sim.State.Campaign.Battle.Active&&ticks++<12000)
                        {
                            var b=sim.State.Campaign.Battle;sim.UseBattleSkill(0);if(b.Elapsed%6>=4.5)sim.UseBattleSkill(1);if(b.HeroHP<b.HeroMaxHP*.5)sim.UseBattleSkill(2);sim.Advance(.1);
                        }
                    }
                    else if(action=="CollectLoot")Check(sim.ClaimExpedition(),"bootstrap result");
                    else if(action=="Forge")
                    {
                        if(!sim.IsRunning(Station.Forge)){sim.PlanRecipe(next.RecipeId);sim.State.KeepCraftedEquipment=true;Check(sim.TryStart(Station.Forge),"bootstrap forge");}
                        for(int hit=0;hit<3;hit++){sim.Advance(.21);sim.TryStrike(1);}sim.Advance(sim.GetDuration(Station.Forge)+.01);
                    }
                    else if(action=="Equip")Check(next.ItemIndex>=0&&sim.TryEquip(next.ItemIndex),"bootstrap equip");
                    else if(action=="ClaimQuest")Check(sim.ClaimQuest(next.QuestId),"bootstrap story");
                    else throw new Exception("bootstrap stalled at "+next.Title);
                }
                Check(actions<150,"new hero never reached the late chapter project");
            }
        });
        test("automatic forge respects completed project batches and leaves quality orders for hand tempering",delegate{
            var sim=Chapter(0);sim.State.Ingots=20;sim.State.Automated[2]=true;
            sim.State.ReserveJourneyGear=true;sim.State.ReservedJourneyRecipe=0;sim.State.Inventory.Add(Gear(0));
            sim.Advance(10);Check(sim.State.TotalCrafted==0&&sim.State.Ingots==20,"worker made unwanted duplicate hero gear");
            sim.State.ReserveJourneyGear=false;Call(sim,"TryAcceptCommission",0);sim.Advance(10);
            Check(sim.State.TotalCrafted==0&&sim.State.Ingots==20,"worker overfilled an already-ready order");
            Call(sim,"CancelCommission");sim.State.Inventory.Clear();Call(sim,"TryAcceptCommission",2);sim.PlanRecipe(3);sim.Advance(10);
            Check(sim.State.TotalCrafted==0&&sim.State.Ingots==20,"automatic common output wasted quality-order materials");
            Check(sim.TryStart(Station.Forge,true),"quality order has no manual start");
            sim.Advance(1.249);sim.TryStrike(1);sim.Advance(1.25);sim.TryStrike(1);sim.Advance(1.25);sim.TryStrike(1);sim.Advance(6);
            Check(sim.CommissionProgress==1&&!sim.IsRunning(Station.Forge),"quality order not ready or batch kept running");
            Check((bool)Call(sim,"TryDeliverCommission"),"batch cannot be delivered");sim.Advance(.01);
            Check(sim.IsRunning(Station.Forge),"worker did not resume after finished order");
        });
        test("guided journey can finish all five chapters from zero supplies without gifted resources",delegate{
            var sim=new WorkshopSimulation(new WorkshopState());int actions=0,defeats=0;
            while(!(bool)Read(sim,"CampaignComplete")&&actions++<4000)
            {
                var next=sim.NextStep;string action=next.Action.ToString();
                if(action=="Mine"||action=="Smelt")
                {
                    var station=(Station)Read(next,"Station");if(!sim.IsRunning(station))Check(sim.TryStart(station),"blocked "+action);
                    sim.Advance(sim.GetDuration(station)+.01);
                }
                else if(action=="Forge")
                {
                    if(!sim.IsRunning(Station.Forge))
                    {
                        Check(sim.SelectRecipe(Int(next,"RecipeId")),"project selection");
                        sim.State.KeepCraftedEquipment=true;Check(sim.TryStart(Station.Forge),"project could not start");sim.State.KeepCraftedEquipment=false;
                    }
                    for(int hit=0;hit<3&&sim.IsRunning(Station.Forge);hit++){sim.Advance(.21);sim.TryStrike(1);}
                    sim.Advance(sim.GetDuration(Station.Forge)+.01);
                }
                else if(action=="Equip")Check(sim.TryEquip(Int(next,"ItemIndex")),"cannot equip project");
                else if(action=="ClaimQuest")Check(sim.ClaimQuest(next.QuestId),"cannot claim objective");
                else if(action=="Adventure")Check(sim.StartExpedition(Int(next,"Region"),Int(next,"Route")),"cannot depart");
                else if(action=="Battle")
                {
                    int ticks=0;
                    while(sim.State.Campaign.Battle.Active&&ticks++<12000)
                    {
                        var b=sim.State.Campaign.Battle;sim.UseBattleSkill(0);
                        if(b.Elapsed%6>=4.5)sim.UseBattleSkill(1);
                        if(b.HeroHP<b.HeroMaxHP*.5)sim.UseBattleSkill(2);
                        sim.Advance(.1);
                    }
                }
                else if(action=="CollectLoot"){if(!sim.State.Campaign.Battle.Won)defeats++;Check(sim.ClaimExpedition(),"claim result");}
                else if(action=="Hire")Check(sim.TryAutomate((Station)Read(next,"Station")),"cannot fund required worker");
                else if(action=="Upgrade")Check(sim.TryUpgrade((Station)Read(next,"Station")),"cannot fund required upgrade");
                else if(action=="Commission")
                {
                    if(Read(sim,"ActiveCommission")==null)Check((bool)Call(sim,"TryAcceptCommission",0),"cannot fund chapter with order");
                    else sim.State.TrackCommission=true;
                }
                else if(action=="DeliverCommission")Check((bool)Call(sim,"TryDeliverCommission"),"cannot deliver funding order");
                else throw new Exception("unplayable advice: "+action);
                if(actions%17==0)sim=Reload(sim);
            }
            Check((bool)Read(sim,"CampaignComplete"),"journey stuck after "+actions+" actions at "+sim.NextStep.Title+"; defeats "+defeats);
            Check(sim.State.Campaign.TownLevel==5&&Array.TrueForAll(sim.State.Automated,x=>x),"journey skipped rebuilding or idle economy");
            Check(sim.State.Gold>=0&&actions>35,"journey completed without actual gameplay");
            Console.WriteLine("JOURNEY_PLAYTHROUGH "+actions+" actions · "+defeats+" defeats · "+WorkshopSimulation.Number(sim.State.SimulationTime)+" simulated seconds");
        });
    }
}
