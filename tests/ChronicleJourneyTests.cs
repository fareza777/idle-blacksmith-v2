using System;
using System.Text.Json;
using ForgeEmpire;

internal static class ChronicleJourneyTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static WorkshopSimulation Reload(WorkshopSimulation sim){var j=new JsonSerializerOptions{IncludeFields=true};return new WorkshopSimulation(JsonSerializer.Deserialize<WorkshopState>(JsonSerializer.Serialize(sim.State,j),j));}
    public static void Run(Action<string,Action> test)
    {
        test("Chronicles guided campaign reaches six frontiers from an ordinary fresh save",()=>{
            var sim=new WorkshopSimulation(new WorkshopState{ChroniclesEnabled=true});int actions=0,defeats=0;
            while(!sim.CampaignComplete&&actions++<8000)
            {
                if(sim.SagaInProgress)
                {
                    var r=sim.State.Saga.Run;
                    if(r.Phase==SagaPhase.Choice)Check(sim.ChooseSagaPath(r.Room==1&&!r.CampUsed?1:0),"choose road");
                    else if(r.Phase==SagaPhase.Camp)Check(sim.ChooseSagaPath(0),"rest");
                    else if(r.Phase==SagaPhase.Result){if(!r.Won)defeats++;Check(sim.ClaimSaga(),"collect");}
                    else
                    {
                        for(int tick=0;tick<12000&&r.Phase==SagaPhase.Combat;tick++)
                        {
                            if(!string.IsNullOrEmpty(r.Telegraph))sim.UsePartySkill(0);
                            sim.Advance(.1);
                        }
                    }
                }
                else if(sim.MasterworkActive)
                {
                    var p=sim.State.Artisan.Project;
                    if(p.Stage==ArtisanStage.Heat||p.Stage==ArtisanStage.Shape){sim.Advance(.21);Check(sim.StrikeMasterwork(1),"shape creation");}
                    else if(p.Stage==ArtisanStage.Quench)Check(sim.QuenchMasterwork(0),"quench");
                    else if(p.Stage==ArtisanStage.Engrave)Check(sim.EngraveMasterwork(0),"engrave");
                    else Check(sim.CompleteMasterwork(),"deliver creation");
                }
                else
                {
                    var next=sim.NextStep;
                    if(next.RecipeId>=0)sim.PlanRecipe(next.RecipeId);
                    bool gear=next.RecipeId>=0&&(next.Action==GuideAction.Equip||!sim.State.TrackCommission||sim.ActiveCommission==null||next.RecipeId!=sim.ActiveCommission.RecipeId);
                    sim.State.ReserveJourneyGear=gear;if(gear)sim.State.ReservedJourneyRecipe=next.RecipeId;
                    switch(next.Action)
                    {
                        case GuideAction.Mine:case GuideAction.Smelt:
                            if(!sim.IsRunning(next.Station))Check(sim.TryStart(next.Station),"start material production");sim.Advance(sim.GetDuration(next.Station)+.01);break;
                        case GuideAction.Forge:
                            if(!sim.IsRunning(Station.Forge))Check(sim.BeginMasterwork(next.RecipeId,0),"begin paid artisan project");
                            else {sim.TryBeginTempering();for(int hit=0;hit<3;hit++){sim.Advance(.21);sim.TryStrike(1);}sim.Advance(sim.GetDuration(Station.Forge)+.01);}break;
                        case GuideAction.Equip:Check(sim.TryEquip(next.ItemIndex),"equip current creation");break;
                        case GuideAction.ClaimQuest:Check(sim.ClaimQuest(next.QuestId),"claim earned story");break;
                        case GuideAction.Adventure:Check(sim.StartSaga(next.Region,next.Route),"start advised expedition");break;
                        case GuideAction.Hire:Check(sim.TryAutomate(next.Station),"fund worker");break;
                        case GuideAction.Upgrade:Check(sim.TryUpgrade(next.Station),"fund upgrade");break;
                        case GuideAction.Commission:if(sim.ActiveCommission==null)Check(sim.TryAcceptCommission(0),"accept guild funding");else sim.State.TrackCommission=true;break;
                        case GuideAction.DeliverCommission:Check(sim.TryDeliverCommission(),"deliver guild funding");break;
                        default:throw new Exception("Unreachable next step "+next.Action);
                    }
                }
                if(actions%19==0)sim=Reload(sim);
            }
            Check(sim.CampaignComplete,"stuck after "+actions+" actions; "+defeats+" defeats; "+sim.NextStep.Title);
            Check(sim.State.Campaign.TownLevel==6&&sim.State.Artisan.Completed>0&&sim.State.Campaign.ExpeditionsWon>=18,"actual crafting, travel and rescue");
            Check(sim.State.Gold>=0&&sim.State.Campaign.Heroes[0].Level<=30,"bounded economy");
            Console.WriteLine("CHRONICLES_JOURNEY "+actions+" decisions · "+defeats+" defeats · "+WorkshopSimulation.Number(sim.State.SimulationTime)+" simulated seconds · Rowan level "+sim.State.Campaign.Heroes[0].Level);
        });
    }
}
