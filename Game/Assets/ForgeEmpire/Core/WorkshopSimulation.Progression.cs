using System;

namespace ForgeEmpire
{
    public sealed partial class WorkshopSimulation
    {
        static readonly string[] ChapterTitles={"Reopen the old road","Light the ferry home","Relight the mountain watch","Cool the ancient furnace","Bring the stars home","Choose a living dawn"};
        public string ChapterTitle { get { return ChapterTitles[UnlockedRegion]; } }
        public string ChapterUnlock
        {
            get { return CampaignComplete?"The valley is free. Restore the town, master 108 recipes and keep your guild growing.":GuardianObjective(UnlockedRegion); }
        }
        public bool IsFrontierCleared(int region)
        {
            return region>=0&&region<GameContent.RegionCount&&State.Campaign.QuestClaims[GameContent.MainQuestId(region,2)];
        }
        public string GuardianObjective(int region)
        {
            if(region<0||region>=GameContent.RegionCount)return "Unknown frontier.";
            if(IsFrontierCleared(region))return "Return for materials, gold and hero XP. This frontier is already restored.";
            string story="\""+GameContent.Quests[GameContent.MainQuestId(region,2)].Title+"\"";
            string action=State.Campaign.BossKills[region]>0?"Claim "+story:"Defeat "+GameContent.Regions[region].BossName+", then claim "+story;
            return action+(region<GameContent.RegionCount-1?" to open "+GameContent.Regions[region+1].Name+" and 18 regional recipes.":" to free the valley and complete the final chapter.");
        }
        public string RegionUnlockText(int region)
        {
            if(region<0||region>=GameContent.RegionCount)return "Unknown frontier.";
            if(region<=UnlockedRegion)return IsFrontierCleared(region)?"Restored · Routes remain open for materials and trade.":"Open · Your current chapter frontier.";
            return "Defeat "+GameContent.Regions[region-1].BossName+", then claim \""+GameContent.Quests[GameContent.MainQuestId(region-1,2)].Title+"\" to open "+GameContent.Regions[region].Name+".";
        }
        public bool CampaignComplete { get { return State.Campaign.QuestClaims[GameContent.MainQuestId(GameContent.RegionCount-1,2)]; } }
        public int MasterworkRecipes
        {
            get { int count=0;for(int i=0;i<GameContent.RecipeCount;i++)if(State.Campaign.CraftedCounts[i]>0&&State.Campaign.BestCraftQuality[i]>=3)count++;return count; }
        }
        public int ChapterProgress
        {
            get { int count=0;for(int i=UnlockedRegion*7;i<UnlockedRegion*7+7;i++)if(MilestoneComplete(i))count++;return count; }
        }
        public int JourneyMilestone
        {
            get
            {
                if(CampaignComplete)return GameContent.RegionCount*7;
                for(int i=UnlockedRegion*7;i<UnlockedRegion*7+7;i++)if(!MilestoneComplete(i))return i;
                return UnlockedRegion*7+6;
            }
        }
        public string MilestoneTitle(int index)
        {
            if(index<0||index>=GameContent.RegionCount*7)return "A master forge";
            int chapter=index/7;string place=GameContent.Regions[chapter].Name;
            switch(index%7)
            {
                case 0:return chapter==0?"Arm Rowan for the road":"Forge a "+place+" weapon";
                case 1:return "Earn "+GameContent.Quests[GameContent.MainQuestId(chapter,0)].Npc+"'s backing";
                case 2:return "Secure the supply trail";
                case 3:return "Forge protection for your hero";
                case 4:return "Bring the missing townsfolk home";
                case 5:return chapter<4?"Hire your "+new[]{"miner","smelter","blacksmith","merchant"}[chapter]:"Build a level "+(chapter==5?7:5)+" guild workshop";
                default:return "Defeat "+GameContent.Regions[chapter].BossName;
            }
        }
        bool MilestoneCondition(int index)
        {
            int chapter=index/7,step=index%7;var c=State.Campaign;
            if(chapter>UnlockedRegion)return false;
            if(chapter<UnlockedRegion||CampaignComplete)return true;
            if(step==0||step==3)
            {
                foreach(var hero in c.Heroes)
                {
                    var gear=step==0?hero.Weapon:hero.Armor;
                    if(gear!=null&&GameContent.Recipes[gear.RecipeId].Region>=chapter)return true;
                }
                return false;
            }
            if(step==1)return c.QuestClaims[GameContent.MainQuestId(chapter,0)];
            if(step==2)return c.QuestClaims[GameContent.MainQuestId(chapter,1)];
            if(step==4)return c.QuestClaims[GameContent.SideQuestId(chapter,1)];
            if(step==5)
            {
                if(chapter<4)return State.Automated[chapter];
                foreach(int level in State.Levels)if(level<(chapter==5?7:5))return false;
                return true;
            }
            return c.QuestClaims[GameContent.MainQuestId(chapter,2)];
        }
        public bool MilestoneComplete(int index)
        {
            return index>=0&&index<GameContent.RegionCount*7&&(State.Campaign.JourneyMilestones[index]||MilestoneCondition(index));
        }
        void CaptureJourney()
        {
            if(State.Campaign==null||State.Campaign.JourneyMilestones==null)return;
            for(int i=0;i<GameContent.RegionCount*7;i++)if(MilestoneCondition(i))State.Campaign.JourneyMilestones[i]=true;
        }
        void NormalizeProgression()
        {
            var c=State.Campaign;c.JourneyMilestones=ResizeCampaign(c.JourneyMilestones,GameContent.RegionCount*7);
            c.BestCraftQuality=NormalizeCounters(c.BestCraftQuality,GameContent.RecipeCount);
            for(int i=0;i<GameContent.RecipeCount;i++)c.BestCraftQuality[i]=Math.Min(3,c.BestCraftQuality[i]);
            c.RetryLevel=Clamp(c.RetryLevel,0,30);c.LastLostRegion=Clamp(c.LastLostRegion,0,GameContent.RegionCount-1);c.LastLostRoute=Clamp(c.LastLostRoute,0,2);
            State.ContractsCompleted=Clamp(State.ContractsCompleted,0,MaxCounter);
            if(!IsRecipeUnlocked(State.ReservedJourneyRecipe))State.ReserveJourneyGear=false;
            var contract=State.Contract;
            if(contract==null||!contract.Active||!ValidRecipe(contract.RecipeId)||contract.Region<0||contract.Region>UnlockedRegion||
                GameContent.Recipes[contract.RecipeId].Region!=contract.Region||contract.Quantity<1||contract.Quantity>4||contract.MinimumQuality<0||contract.MinimumQuality>3)
            {State.Contract=new GuildContract();State.TrackCommission=false;}
            CaptureJourney();
        }
        NextStepAdvice TaskAdvice(GuideAction action,string title,string detail,string button,int recipe=-1,int region=0,int route=0,int item=-1,int quest=-1,Station station=Station.Mine)
        {
            return new NextStepAdvice{Action=action,Title=title,Detail=detail,Button=button,RecipeId=recipe,Region=region,Route=route,ItemIndex=item,QuestId=quest,Station=station};
        }
        int FindProjectGear(int recipe,int quality)
        {
            int best=-1;
            for(int i=0;i<State.Inventory.Count;i++)
                if(State.Inventory[i].RecipeId==recipe&&State.Inventory[i].Quality>=quality&&(best<0||State.Inventory[i].Quality>State.Inventory[best].Quality))best=i;
            return best;
        }
        NextStepAdvice ProjectAdvice(int recipeId,bool equip,int minimumQuality=0)
        {
            var recipe=GameContent.Recipes[recipeId];
            int item=FindProjectGear(recipeId,minimumQuality);
            if(equip&&item>=0)return TaskAdvice(GuideAction.Equip,"Your "+recipe.Slot.ToString().ToLowerInvariant()+" is ready","Equip "+recipe.Name+" on "+GameContent.Heroes[State.Campaign.SelectedHero].Name+".","Equip & continue",recipeId,item:item);
            if(IsRunning(Station.Forge))return TaskAdvice(GuideAction.Forge,"Shape the hot iron",State.PendingRecipe==recipeId?"Hit the bright zone. 3 perfect strikes make a Masterwork.":"Finish "+GameContent.Recipes[State.PendingRecipe].Name+". Next: "+recipe.Name+".","Strike",recipeId,station:Station.Forge);
            string purpose=equip?"For "+GameContent.Heroes[State.Campaign.SelectedHero].Name:CommissionProgress+" / "+ActiveCommission.Quantity+" ready in storage";
            if(State.Inventory.Count>=InventoryCapacity)return TaskAdvice(GuideAction.Equip,"Make room for the next creation","Equip or sell spare stock. Your order items stay reserved.","Open storage");
            if(State.Ingots<recipe.Ingots)
            {
                string detail=State.Ingots+" / "+recipe.Ingots+" ingots for "+recipe.Name;
                if(State.Ore<2)return TaskAdvice(GuideAction.Mine,"Gather iron for "+recipe.Name,detail+" · 2 ore each trip","Mine ore",recipeId,station:Station.Mine);
                return TaskAdvice(GuideAction.Smelt,"Smelt iron for "+recipe.Name,detail+" · 2 ore → 1 ingot","Smelt iron",recipeId,station:Station.Smelter);
            }
            if(recipe.RareCost>0&&State.Campaign.Materials[recipe.Region-1]<recipe.RareCost)
            {
                var hero=State.Campaign.Heroes[State.Campaign.SelectedHero];int source=recipe.Region-1;
                // Catch up a newly recruited hero through affordable earlier recipes.
                // Each missing rare ingredient moves to an earlier region, ending at
                // ordinary iron, so a weapon-gated route cannot depend on itself.
                if(hero.Weapon==null)
                {
                    int available=-1;
                    for(int i=0;i<State.Inventory.Count;i++)
                    {
                        var candidate=State.Inventory[i];
                        if(GameContent.Recipes[candidate.RecipeId].Slot==GearSlot.Weapon&&!IsCommissionReserved(candidate)&&
                            (available<0||ItemPower(candidate)>ItemPower(State.Inventory[available])))available=i;
                    }
                    if(available>=0)return TaskAdvice(GuideAction.Equip,"Arm your new adventurer","Equip a spare weapon before gathering rare materials.","Equip & continue",State.Inventory[available].RecipeId,item:available);
                    return ProjectAdvice(0,true);
                }
                if(GameContent.Recipes[hero.Weapon.RecipeId].Region<source)return ProjectAdvice(source*4,true);
                if(hero.Armor==null||GameContent.Recipes[hero.Armor.RecipeId].Region<source)return ProjectAdvice(source*4+2,true);
                return TaskAdvice(GuideAction.Adventure,"Find "+MaterialName(source).ToLowerInvariant(),State.Campaign.Materials[source]+" / "+recipe.RareCost+" for "+recipe.Name+". Scout "+GameContent.Regions[source].Name+".","Scout for materials",recipeId,source,0);
            }
            return TaskAdvice(GuideAction.Forge,"Forge "+recipe.Name,purpose+(minimumQuality>0?" · "+QualityName(minimumQuality)+" or better":"")+" · "+RecipeCost(recipeId)+" · Temper by hand", "Forge "+recipe.Slot.ToString().ToLowerInvariant(),recipeId,station:Station.Forge);
        }
        NextStepAdvice ExpeditionAdvice(int region,int route,string title)
        {
            var hero=State.Campaign.Heroes[State.Campaign.SelectedHero];
            if(hero.Weapon==null)return ProjectAdvice(region*4,true);
            var c=State.Campaign;
            if(c.RetryLevel>0&&c.LastLostRegion==region&&c.LastLostRoute==route)
            {
                int best=region==0?1:region*4;
                if(hero.Weapon.Quality<2)return ProjectAdvice(best,true,2);
                if(hero.Armor==null||hero.Armor.Quality<1)return ProjectAdvice(region*4+2,true,1);
                if(hero.Level<c.RetryLevel)return TaskAdvice(GuideAction.Adventure,"Train before another attempt","Win a supply trail for hero XP. Level "+hero.Level+" / "+c.RetryLevel+".","Train on the supply trail",region:region,route:0);
            }
            string reward=route==2?ChapterUnlock:route==1?"Rescue the townsfolk. Restore a building and earn rare materials.":"Collect "+MaterialName(region).ToLowerInvariant()+" to forge the next chapter's equipment.";
            return TaskAdvice(GuideAction.Adventure,title,reward,route==2?"Challenge the guardian":route==1?"Depart on the rescue":"Scout the supply trail",region:region,route:route);
        }
        NextStepAdvice ProgressionAdvice()
        {
            if(SagaInProgress)return TaskAdvice(State.Saga.Run.Phase==SagaPhase.Result?GuideAction.CollectLoot:GuideAction.Battle,"Your party's journey",SagaNextAction,"Continue the expedition");
            if(MasterworkActive)return TaskAdvice(GuideAction.Forge,"Finish your masterwork",State.Artisan.Project.Stage+" · "+GameContent.Recipes[State.Artisan.Project.RecipeId].Name,"Continue crafting",State.Artisan.Project.RecipeId,station:Station.Forge);
            var c=State.Campaign;var battle=c.Battle;
            if(battle.Finished)return TaskAdvice(GuideAction.CollectLoot,battle.Won?"Your hero has returned":"A setback, a stronger plan",battle.Won?"Collect the expedition spoils to advance your journey.":"Your gear is safe. Improve quality and train before the next attempt.",battle.Won?"Collect expedition loot":"Prepare another attempt");
            if(battle.Active)return TaskAdvice(GuideAction.Battle,"Your expedition is underway","Strike for damage. Brace before the heavy hit. Recover once.","Return to the expedition");
            var order=ActiveCommission;
            if(State.TrackCommission&&order!=null)
            {
                if(CommissionProgress>=order.Quantity)return TaskAdvice(GuideAction.DeliverCommission,"Your guild order is ready",order.Quantity+" × "+GameContent.Recipes[order.RecipeId].Name+" → "+order.Gold+" gold + "+order.Reputation+" reputation","Deliver the order",order.RecipeId);
                return ProjectAdvice(order.RecipeId,false,order.MinimumQuality);
            }
            int milestone=JourneyMilestone;
            if(milestone==GameContent.RegionCount*7)return TaskAdvice(GuideAction.Commission,"Your master forge awaits","Master 108 recipes, restore all 6 buildings and fulfill guild orders.","Choose a guild order");
            int chapter=milestone/7,step=milestone%7,quest=GameContent.MainQuestId(chapter,step==2?1:step==6?2:0);
            if(step==0)return ProjectAdvice(chapter*4,true);
            if(step==3)return ProjectAdvice(chapter*4+2,true);
            if(step==1||step==2||step==6)
            {
                if(IsQuestReady(quest))return TaskAdvice(GuideAction.ClaimQuest,GameContent.Quests[quest].Title,"Story complete · +"+GameContent.Quests[quest].RewardGold+" gold · +"+GameContent.Quests[quest].RewardReputation+" reputation","Claim story reward",quest:quest);
                if(step==1)return ProjectAdvice(chapter*4,true);
                return ExpeditionAdvice(chapter,step==6?2:0,MilestoneTitle(milestone));
            }
            if(step==4)
            {
                int rescue=GameContent.SideQuestId(chapter,1);
                if(IsQuestReady(rescue))return TaskAdvice(GuideAction.ClaimQuest,"The townsfolk are safe","Claim the rescue reward to restore part of your town.","Welcome them home",quest:rescue);
                return ExpeditionAdvice(chapter,1,MilestoneTitle(milestone));
            }
            Station station=(Station)Math.Min(3,chapter);
            if(chapter>=4){for(int i=0;i<4;i++)if(State.Levels[i]<(chapter==5?7:5)){station=(Station)i;break;}}
            double cost=chapter<4?GetAutomationCost(station):GetUpgradeCost(station);
            if(State.Gold<cost)return TaskAdvice(GuideAction.Commission,"Fund your next improvement",MilestoneTitle(milestone)+" · Need "+Math.Ceiling(cost-State.Gold)+" more gold. Fulfill a guild order.",order==null?"Find a guild order":"Continue your order");
            return TaskAdvice(chapter<4?GuideAction.Hire:GuideAction.Upgrade,MilestoneTitle(milestone),chapter<4?"Pay "+cost+" gold once. Materials keep moving while you explore.":"Improve "+station+" · "+Math.Ceiling(cost)+" gold",""+(chapter<4?"Hire ":"Improve ")+station+" · "+Math.Ceiling(cost)+" G",station:station);
        }
    }
}
