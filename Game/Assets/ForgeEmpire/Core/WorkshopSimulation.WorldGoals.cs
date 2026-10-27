using System;

namespace ForgeEmpire
{
    public enum WorldGoalKind { Milestone, Contract, Material, Recipe, Guardian, Restoration }
    public sealed class WorldGoalLink
    {
        public WorldGoalKind Kind;
        public string Title="",Detail="",Button="",Blocker="";
        public int Region,Route,RecipeId=-1,QuestId=-1;
        public bool Eligible;
    }
    public sealed class RestorationPreview
    {
        public int Region,Stage,NextStage,GoldCost,MaterialCost;
        public string StageName="",Blocker="";
        public bool Eligible;
    }
    public sealed partial class WorkshopSimulation
    {
        public ExpeditionRewardPreview PreviewLivingJourneyRewards()
        {
            if(!SagaInProgress)return null;var run=State.Saga.Run;
            if(run.Phase==SagaPhase.Result)return new ExpeditionRewardPreview{Gold=run.Won?run.RewardGold:0,Material=run.Won?run.RewardMaterial:0};
            return new ExpeditionRewardPreview{Gold=(int)(((run.Region+1)*(55+40*run.Route)+run.BonusGold)*(1+run.Modifier*.35)),Material=5+run.Route*3+run.BonusMaterial+State.World.Projects[1]};
        }
        public static string RestorationStageName(int stage)
        {
            return new[]{"Ruined landmark","Worksite reopened","Community at work","A flourishing landmark"}[Clamp(stage,0,3)];
        }
        public RestorationPreview PreviewRestoration(int region)
        {
            if(region<0||region>=GameContent.RegionCount)return null;
            int stage=State.World.Projects[region];
            var preview=new RestorationPreview{Region=region,Stage=stage,NextStage=Math.Min(3,stage+1),StageName=RestorationStageName(stage),GoldCost=RestorationGoldCost(region),MaterialCost=RestorationMaterialCost(region)};
            if(region>UnlockedRegion)preview.Blocker=RegionUnlockText(region);
            else if(!State.Campaign.QuestClaims[GameContent.SideQuestId(region,1)])preview.Blocker="First claim: "+GameContent.Quests[GameContent.SideQuestId(region,1)].Title+".";
            else if(stage>=3)preview.Blocker="All three restoration stages are complete.";
            else if(State.Gold<preview.GoldCost)preview.Blocker="Need "+Number(preview.GoldCost-State.Gold)+" more gold. Guild orders fund town work.";
            else if(State.Campaign.Materials[region]<preview.MaterialCost)preview.Blocker="Need "+(preview.MaterialCost-State.Campaign.Materials[region])+" more "+MaterialName(region).ToLowerInvariant()+". Return to this region's supply trail.";
            preview.Eligible=preview.Blocker.Length==0;return preview;
        }
        public string LegendaryContractBlocker(int contract)
        {
            if(contract<0||contract>=12)return "Choose a guardian commission.";
            if(State.World.LegendaryClaims[contract])return "Legendary commission fulfilled.";
            if(SagaInProgress)return "Collect your expedition before delivering this creation.";
            if(State.Saga.BossMastery[contract]<1)return "Defeat "+ChronicleContent.Bosses[contract].Name+" to unlock this commission.";
            int recipe=LegendaryRecipeId(contract);
            if(State.Inventory.FindIndex(i=>i.Masterwork&&i.RecipeId==recipe&&i.Quality==3)<0)return "Keep an unequipped quality 3 artisan "+GameContent.Recipes[recipe].Name+" in storage.";
            return "";
        }
        public WorldGoalLink RecipeWorldGoal(int recipe)
        {
            if(!ValidRecipe(recipe))return null;
            var definition=GameContent.Recipes[recipe];var quote=QuoteMasterwork(recipe,0);
            return new WorldGoalLink{Kind=WorldGoalKind.Recipe,RecipeId=recipe,Region=definition.Region,Title="Create "+definition.Name,
                Detail=RecipeCost(recipe)+" · "+definition.Element+" "+definition.Slot.ToString().ToLowerInvariant()+" · "+definition.Power+" power / "+definition.Guard+" guard"+(quote.Reserved?"\nRecipe materials already reserved; honest iron needs no additional payment.":""),
                Button=MasterworkActive?"Continue the artisan bench":"View recipe & technique",Blocker=quote.Blocker,Eligible=quote.CanBegin};
        }
        public WorldGoalLink[] WorldGoalLinks
        {
            get
            {
                var next=NextStep;var order=ActiveCommission;
                int recipe=order!=null?order.RecipeId:next.RecipeId>=0?next.RecipeId:UnlockedRegion*4;
                var pattern=GameContent.Recipes[recipe];int source=pattern.RareCost>0?pattern.Region-1:UnlockedRegion;
                var reward=PreviewRouteRewards(source,0);string roadBlocker=ExpeditionBlocker(source,0);
                int town=UnlockedRegion;for(int region=0;region<=UnlockedRegion;region++)if(State.World.Projects[region]<3){town=region;break;}
                var restoration=PreviewRestoration(town);int guardian=UnlockedRegion,quest=GameContent.MainQuestId(guardian,2);
                bool claim=IsQuestReady(quest);string guardianBlocker=claim?"":ExpeditionBlocker(guardian,2);
                var offer=order??CommissionOffer(0);string contractBlocker=order!=null&&CommissionProgress<order.Quantity?"Keep "+(order.Quantity-CommissionProgress)+" more qualifying creation(s) in storage.":"";
                return new[]{
                    new WorldGoalLink{Kind=WorldGoalKind.Milestone,Title=next.Title,Detail=(!CampaignComplete?"Chapter milestone: "+MilestoneTitle(JourneyMilestone)+"\n":"")+next.Detail,Button=next.Button,RecipeId=next.RecipeId,QuestId=next.QuestId,Region=next.Region,Route=next.Route,Eligible=true},
                    new WorldGoalLink{Kind=WorldGoalKind.Contract,Title=order==null?"Fund the workshop":"Your guild contract",RecipeId=offer.RecipeId,Region=offer.Region,
                        Detail=offer.Quantity+" × "+GameContent.Recipes[offer.RecipeId].Name+" · "+QualityName(offer.MinimumQuality)+" or better\n"+RecipeCost(offer.RecipeId)+" per item → "+offer.Gold+" gold + "+offer.Reputation+" reputation",
                        Button=order==null?"Choose a guild order":CommissionProgress>=order.Quantity?"Deliver the earned order":"Continue the contract",Blocker=contractBlocker,Eligible=contractBlocker.Length==0},
                    new WorldGoalLink{Kind=WorldGoalKind.Material,Title="Gather "+MaterialName(source).ToLowerInvariant(),Region=source,Route=0,RecipeId=recipe,
                        Detail=State.Campaign.Materials[source]+" owned · "+GameContent.Regions[source].Name+" supply trail\nBase return: "+reward.Material+" materials + "+reward.Gold+" gold"+(pattern.RareCost>0?" · "+pattern.RareCost+" needed for "+pattern.Name:" · Stock for town restoration"),
                        Button="Prepare the supply trail",Blocker=roadBlocker,Eligible=roadBlocker.Length==0},
                    RecipeWorldGoal(recipe),
                    new WorldGoalLink{Kind=WorldGoalKind.Guardian,Title=GameContent.Regions[guardian].BossName,Region=guardian,Route=2,QuestId=claim?quest:-1,
                        Detail=GuardianObjective(guardian)+"\nGuardian victory earns mastery."+(!IsFrontierCleared(guardian)?guardian<5?" Claim its story to open the next frontier.":" Claim its story to complete the valley's journey.":""),Button=claim?"Claim the guardian story":"Prepare the guardian route",Blocker=guardianBlocker,Eligible=guardianBlocker.Length==0},
                    new WorldGoalLink{Kind=WorldGoalKind.Restoration,Title=Restorations[town].Name,Region=town,
                        Detail=restoration.StageName+" · "+restoration.Stage+" / 3\n"+(restoration.Stage==3?Restorations[town].Benefit:restoration.GoldCost+" gold + "+restoration.MaterialCost+" "+MaterialName(town).ToLowerInvariant()+" → stage "+restoration.NextStage),
                        Button="Visit the town worksite",Blocker=restoration.Blocker,Eligible=restoration.Eligible}
                };
            }
        }
    }
}
