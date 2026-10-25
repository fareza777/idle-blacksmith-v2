using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        bool journeyFollowing=true;
        bool awaitingCraftResult;
        int expectedRecipe,expectedCraftCount;
        readonly List<CraftedItem> previousCraftStock=new List<CraftedItem>();
        public bool CraftResultsEnabled { get; set; } = true;

        public void ResumeJourney()
        {
            CloseModal();journeyFollowing=true;Refresh();
        }
        void SyncWorkflow()
        {
            if(!journeyFollowing)return;
            var advice=app.Sim.NextStep;
            if(advice.Action==GuideAction.Battle||advice.Action==GuideAction.CollectLoot)return;
            int previousRecipe=app.Sim.State.SelectedRecipe;
            bool wasReserved=app.Sim.State.ReserveJourneyGear;int previousReservation=app.Sim.State.ReservedJourneyRecipe;
            if(advice.RecipeId>=0)app.Sim.PlanRecipe(advice.RecipeId);
            bool gearProject=advice.RecipeId>=0&&(advice.Action==GuideAction.Equip||!app.Sim.State.TrackCommission||app.Sim.ActiveCommission==null||advice.RecipeId!=app.Sim.ActiveCommission.RecipeId);
            app.Sim.State.ReserveJourneyGear=gearProject;
            if(gearProject)app.Sim.State.ReservedJourneyRecipe=advice.RecipeId;
            if(previousRecipe!=app.Sim.State.SelectedRecipe||wasReserved!=gearProject||previousReservation!=app.Sim.State.ReservedJourneyRecipe)app.Save();
            if(HasModal)return;
            Station target=advice.Action==GuideAction.Mine?Station.Mine:advice.Action==GuideAction.Smelt?Station.Smelter:Station.Forge;
            if(advice.Action==GuideAction.Upgrade||advice.Action==GuideAction.Hire)target=advice.Station;
            if(selected==target)return;
            selected=target;
            if(worldViewport&&scene)
            {
                float y=Stations[(int)target].y*Pixel*SceneZoom-worldViewport.rect.height*.52f;
                float x=Mathf.Clamp(Stations[(int)target].x*Pixel*SceneZoom-540,0,1080*(SceneZoom-1));
                MoveCamera(new Vector2(-x,Mathf.Clamp(y,0,Mathf.Max(0,scene.rect.height*SceneZoom-worldViewport.rect.height))));
            }
        }
        void RefreshWorkflowDock()
        {
            if(!journeyFollowing)return;
            var advice=app.Sim.NextStep;
            stationTitle.text=advice.Title;stationInfo.text=advice.Detail;
            bool striking=advice.Action==GuideAction.Forge&&app.Sim.IsRunning(Station.Forge)&&!app.Sim.MasterworkActive;
            actionButton.gameObject.SetActive(!striking);strikeButton.gameObject.SetActive(striking);
            marker.parent.gameObject.SetActive(striking);
            actionText.text=advice.Button;
            bool waiting=(advice.Action==GuideAction.Mine||advice.Action==GuideAction.Smelt)&&app.Sim.IsRunning(advice.Station);
            actionButton.interactable=!waiting;
            if(waiting)actionText.text="Working · "+Math.Ceiling((1-app.Sim.GetProgress(advice.Station))*app.Sim.GetDuration(advice.Station))+"s";
        }
        void ExecuteJourneyAction()
        {
            var sim=app.Sim;var advice=sim.NextStep;
            switch(advice.Action)
            {
                case GuideAction.Mine:case GuideAction.Smelt:
                    app.Work(advice.Station);journeyFollowing=true;break;
                case GuideAction.Forge:
                    if(sim.IsRunning(Station.Forge)){app.Strike();break;}
                    if(advice.RecipeId>=0)sim.SelectRecipe(advice.RecipeId);
                    bool previousPurpose=sim.State.KeepCraftedEquipment;
                    sim.State.KeepCraftedEquipment=true;
                    app.Work(Station.Forge);sim.State.KeepCraftedEquipment=previousPurpose;journeyFollowing=true;app.Save();break;
                case GuideAction.Equip:
                    if(advice.ItemIndex<0){ShowInventory();break;}
                    if(app.Equip(advice.ItemIndex))
                    {
                        if(sim.State.TotalCrafted<=1)sim.State.KeepCraftedEquipment=false;
                        app.Save();ResumeJourney();
                    }
                    break;
                case GuideAction.ClaimQuest:ClaimJourneyQuest(advice.QuestId);break;
                case GuideAction.Adventure:ShowDeparture(advice.Region,advice.Route);break;
                case GuideAction.Battle:case GuideAction.CollectLoot:ShowBattle();break;
                case GuideAction.Hire:
                    if(sim.TryAutomate(advice.Station))
                    {
                        app.Save();app.Play("coin");
                        ShowProgressReward("Your workshop is growing",advice.Station+" worker hired","This station starts new jobs automatically. Visit the crew to pause or resume workers. Your workshop keeps producing while you explore.");
                    }
                    break;
                case GuideAction.Upgrade:app.Upgrade(advice.Station);ResumeJourney();break;
                case GuideAction.DeliverCommission:DeliverGuildOrder();break;
                case GuideAction.Commission:ShowGuildOrders();break;
                default:ShowRecipes();break;
            }
            Refresh();
        }
        void ClaimJourneyQuest(int id)
        {
            var sim=app.Sim;if(!sim.IsQuestReady(id))return;
            var quest=GameContent.Quests[id];int chapter=sim.UnlockedRegion;
            app.ClaimCampaignQuest(id,false);
            string rewards="+"+quest.RewardGold+" gold · +"+quest.RewardReputation+" reputation";
            if(quest.RewardMaterial>0)rewards+="\n+"+quest.RewardMaterial+" "+WorkshopSimulation.MaterialName(quest.Region).ToLowerInvariant();
            if(quest.RewardTown>0)rewards+="\nTown restored · "+sim.State.Campaign.TownLevel+" / "+GameContent.RegionCount;
            string title=sim.UnlockedRegion>chapter?"Welcome to "+GameContent.Regions[sim.UnlockedRegion].Name:sim.CampaignComplete&&id==14?"The valley is free":quest.Title;
            ShowProgressReward(title,rewards,quest.Consequence);
        }
        public void ExpectCraftResult()
        {
            if(!CraftResultsEnabled)return;
            expectedRecipe=app.Sim.State.PendingRecipe;expectedCraftCount=app.Sim.State.TotalCrafted;awaitingCraftResult=true;
            previousCraftStock.Clear();previousCraftStock.AddRange(app.Sim.State.Inventory);
        }
        void TickWorkflow()
        {
            if(!awaitingCraftResult||HasModal||app.Sim.State.TotalCrafted<=expectedCraftCount)return;
            awaitingCraftResult=false;
            foreach(var item in app.Sim.State.Inventory)
                if(item.RecipeId==expectedRecipe&&!previousCraftStock.Contains(item)){ShowCraftResult(item);break;}
            previousCraftStock.Clear();
        }
        void ShowCraftResult(CraftedItem item)
        {
            var sim=app.Sim;var recipe=GameContent.Recipes[item.RecipeId];
            var p=Page("craft-result","A creation from your forge",recipe.Name,()=>ShowCraftResult(item));
            EquipmentIcon("CraftResultItem",p,item.RecipeId,350,0,320);
            float y=Paragraph(p,"CraftResultQuality",WorkshopSimulation.QualityName(item.Quality)+" · "+item.Quality+" / 3",334,ForgeUI.Subheading,ForgeUI.Gold,true)+Gap;
            y=Paragraph(p,"CraftResultStats",sim.ItemPower(item)+" Power · "+sim.ItemGuard(item)+" Guard · "+recipe.Element,y,ForgeUI.Subheading,ForgeUI.Teal)+Gap;
            if(sim.State.ChroniclesEnabled)y=Paragraph(p,"CraftResultBuild",WorkshopSimulation.ItemBuildDescription(item),y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            bool reserved=sim.IsCommissionReserved(item);
            var active=sim.ActiveCommission;
            bool belowQuality=active!=null&&active.RecipeId==item.RecipeId&&item.Quality<active.MinimumQuality;
            int index=sim.State.Inventory.IndexOf(item);
            if(index>=0&&!reserved)
                y=Paragraph(p,"CraftResultComparison",GameContent.Heroes[sim.State.Campaign.SelectedHero].Name+": "+Signed(sim.EquipmentDelta(index,true))+" Power · "+Signed(sim.EquipmentDelta(index,false))+" Guard",y,ForgeUI.Small,ForgeUI.Cream)+Gap;
            if(belowQuality)
            {
                y=Paragraph(p,"CraftResultPurpose","This order needs "+WorkshopSimulation.QualityName(active.MinimumQuality)+" or better. This piece can stay as spare equipment. Aim for the bright zone to improve the next one.",y,ForgeUI.Small,ForgeUI.Orange)+Gap;
                y=AddAction(p,"CraftResultContinue","Try the quality challenge again",y,ResumeJourney);
            }
            else if(reserved)
            {
                y=Paragraph(p,"CraftResultPurpose","Reserved for your guild order · "+sim.CommissionProgress+" / "+sim.ActiveCommission.Quantity+" ready. Your merchant cannot sell this equipment.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
                y=AddAction(p,"CraftResultContinue",sim.CommissionProgress>=sim.ActiveCommission.Quantity?"Deliver the order":"Forge the next order item",y,()=>{if(sim.ActiveCommission!=null&&sim.CommissionProgress>=sim.ActiveCommission.Quantity)DeliverGuildOrder();else ResumeJourney();});
            }
            else
            {
                var equip=WideButton(p,"CraftResultEquip","Equip "+GameContent.Heroes[sim.State.Campaign.SelectedHero].Name,y,()=>{
                    int current=sim.State.Inventory.IndexOf(item);
                    if(current<0){ResumeJourney();return;}
                    if(!app.Equip(current))return;
                    if(journeyFollowing&&sim.State.TotalCrafted<=1)sim.State.KeepCraftedEquipment=false;
                    app.Save();ResumeJourney();
                });equip.interactable=!sim.State.Campaign.Battle.Active;y+=equip.GetComponent<RectTransform>().rect.height+Gap;
                modalRefresh.Add(()=>equip.interactable=sim.State.Inventory.Contains(item)&&!sim.State.Campaign.Battle.Active);
                y=AddAction(p,"CraftResultContinue","Keep in storage & continue",y,ResumeJourney,false);
            }
            y=Paragraph(p,"CraftQualityHint",item.Quality>=3?"Masterwork. Each perfect hit strengthened the finished piece.":"Aim for the bright zone while forging. Three perfect hits make a Masterwork with stronger stats and a higher sale price.",y,ForgeUI.Small,ForgeUI.Muted);
            EndPage(y);
        }
        void ShowProgressReward(string title,string rewards,string consequence)
        {
            var p=Page("journey-reward","A step forward",title,()=>ShowProgressReward(title,rewards,consequence));
            HandDrawnArt.Prop("JourneyRewardCrest",p,4,350,0,320,250);
            float y=Paragraph(p,"JourneyReward",rewards,270,ForgeUI.Subheading,ForgeUI.Gold,true)+Gap;
            y=Paragraph(p,"JourneyConsequence",consequence,y)+Gap;
            Rule(p,y);y+=Gap;
            y=Paragraph(p,"RewardNextGoal","Next: "+app.Sim.NextStep.Title,y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            y=AddAction(p,"ContinueJourney","Continue journey",y,ResumeJourney);
            EndPage(y);
        }
        public void ShowJourney()
        {
            var sim=app.Sim;int chapter=sim.UnlockedRegion;
            var p=Page("journey","Chapter "+(chapter+1)+" of "+GameContent.RegionCount+" · "+sim.GuildRank,sim.CampaignComplete?"Your master forge":sim.ChapterTitle,ShowJourney);
            bool compact=root.rect.height<1600;
            if(!compact)Landscape("JourneyLandscape",p,chapter,0,0,1020,260);
            float y=Paragraph(p,"JourneyProgress",sim.CampaignComplete?"Story complete · "+GameContent.RegionCount*7+" milestones":sim.ChapterProgress+" / 7 milestones · "+GameContent.Regions[chapter].Name,compact?0:284,compact?ForgeUI.Small:ForgeUI.Subheading,ForgeUI.Gold,true)+Gap;
            ForgeUI.Fill("JourneyTrack",p,0,y,1020,8,ForgeUI.Line);
            ForgeUI.Fill("JourneyProgressFill",p,0,y,1020*sim.ChapterProgress/7f,8,ForgeUI.Teal);y+=Gap+12;
            y=Paragraph(p,"JourneyCurrentGoal",sim.NextStep.Title,y,ForgeUI.Subheading,null,true)+12;
            y=Paragraph(p,"JourneyCurrentDetail",sim.NextStep.Detail,y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            y=AddAction(p,"JourneyContinue","Continue · "+sim.NextStep.Button,y,ResumeJourney);
            y=Paragraph(p,"JourneyNextUnlock",sim.ChapterUnlock,y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            for(int i=chapter*7;i<chapter*7+7;i++)
            {
                bool done=sim.MilestoneComplete(i),current=i==sim.JourneyMilestone;
                Rule(p,y);y+=22;
                ForgeUI.Glyph("MilestoneMark"+i,p,done?"check":current?"forge":"lock",0,y+8,44,done?ForgeUI.Teal:current?ForgeUI.Gold:ForgeUI.Line);
                y=Paragraph(p,"Milestone"+i,sim.MilestoneTitle(i),y,ForgeUI.Small,done?ForgeUI.Teal:current?ForgeUI.Cream:ForgeUI.Muted,current,68,952)+24;
            }
            y+=Gap;y=Paragraph(p,"LongTermTitle","The road ahead",y,ForgeUI.Subheading,null,true)+Gap;
            for(int i=0;i<GameContent.RegionCount;i++)
            {
                string status=sim.IsFrontierCleared(i)?"Restored":i==chapter?"Your current chapter":sim.RegionUnlockText(i);
                y=Paragraph(p,"ChapterRoadmap"+i,(i+1)+". "+GameContent.Regions[i].Name+"\n"+status,y,ForgeUI.Small,i<=chapter?ForgeUI.Cream:ForgeUI.Muted)+Gap;
            }
            y=AddAction(p,"JourneyOrders","Guild orders · Earn gold for your workshop",y,ShowGuildOrders,false);
            y=AddAction(p,"JourneyMastery","Mastery & collections",y,ShowMastery,false);
            y=AddAction(p,"JourneyStories","Story & town requests",y,()=>ShowQuests(false),false);
            int builtMilestone=sim.JourneyMilestone;string builtAdvice=sim.NextStep.Title+sim.NextStep.Detail;
            modalRefresh.Add(()=>{if(builtMilestone!=sim.JourneyMilestone||builtAdvice!=sim.NextStep.Title+sim.NextStep.Detail)RequestPageRebuild();});
            EndPage(y);
        }
        public void ShowGuildOrders()
        {
            var sim=app.Sim;
            var p=Page("guild-orders",sim.GuildRank+" · "+sim.State.Campaign.Reputation+" reputation","Work worth doing",ShowGuildOrders);
            float y=Paragraph(p,"GuildOrderIntro","Earn gold by making equipment the valley needs. Orders never expire. Keep exploring while your crew prepares the materials.",0,ForgeUI.Small,ForgeUI.Muted)+Gap;
            var active=sim.ActiveCommission;
            if(active!=null)
            {
                y=OrderTerms(p,active,y,"ActiveOrder");
                int progress=sim.CommissionProgress;
                y=Paragraph(p,"ActiveOrderProgress",progress+" / "+active.Quantity+" ready in storage",y,ForgeUI.Subheading,ForgeUI.Teal,true)+Gap;
                y=AddAction(p,"GuildOrderContinue",progress>=active.Quantity?"Deliver · "+active.Gold+" gold":"Work on this order",y,()=>{
                    if(sim.CommissionProgress>=active.Quantity){DeliverGuildOrder();return;}
                    sim.State.TrackCommission=true;app.Save();ResumeJourney();
                });
                y=Paragraph(p,"OrderReservation","Matching equipment is reserved automatically. Delivery uses the lowest qualifying quality and never takes equipped gear.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
                y=AddAction(p,"GuildOrderStory","Follow the chapter goal",y,()=>{sim.State.TrackCommission=false;app.Save();ResumeJourney();},false);
                y=AddAction(p,"CancelGuildOrder","Release this order",y,()=>{sim.CancelCommission();app.Save();RebuildCurrentPage();},false);
                modalRefresh.Add(()=>{if(progress!=sim.CommissionProgress)RequestPageRebuild();});
            }
            else
            {
                for(int i=0;i<3;i++)
                {
                    int slot=i;var offer=sim.CommissionOffer(i);
                    Rule(p,y);y+=Gap;y=OrderTerms(p,offer,y,"GuildOffer"+i);
                    y=AddAction(p,"AcceptGuildOrder"+i,"Accept order",y,()=>{
                        if(!sim.TryAcceptCommission(slot))return;
                        app.Save();app.Play("click");ResumeJourney();
                    },i==0);
                }
            }
            EndPage(y);
        }
        float OrderTerms(Transform p,CommissionDefinition offer,float y,string name)
        {
            y=Paragraph(p,name+"Title",offer.Title,y,ForgeUI.Subheading,null,true)+10;
            y=Paragraph(p,name+"Patron",offer.Patron,y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            EquipmentIcon(name+"Icon",p,offer.RecipeId,0,y,144);
            string terms=offer.Quantity+" × "+GameContent.Recipes[offer.RecipeId].Name+"\n"+(offer.MinimumQuality>0?WorkshopSimulation.QualityName(offer.MinimumQuality)+" quality or better":"Any quality");
            float h=ForgeUI.Height(terms,842,ForgeUI.Body);ForgeUI.Label(name+"Terms",p,terms,178,y,842,h,ForgeUI.Body,ForgeUI.Cream);y+=Mathf.Max(144,h)+20;
            y=Paragraph(p,name+"Materials",app.Sim.RecipeCost(offer.RecipeId)+" per item",y,ForgeUI.Small,ForgeUI.Muted)+12;
            return Paragraph(p,name+"Reward","+"+offer.Gold+" gold · +"+offer.Reputation+" reputation",y,ForgeUI.Small,ForgeUI.Gold)+Gap;
        }
        void DeliverGuildOrder()
        {
            var offer=app.Sim.ActiveCommission;if(offer==null||!app.Sim.TryDeliverCommission())return;
            app.Save();app.Play("coin");
            ShowProgressReward("Order delivered","+"+offer.Gold+" gold · +"+offer.Reputation+" reputation",offer.Patron+" received "+offer.Quantity+" × "+GameContent.Recipes[offer.RecipeId].Name+". New work is available at the guild. Your next chapter goal is ready.");
        }
        void ShowMastery()
        {
            if(app.Sim.State.ChroniclesEnabled){ShowLegends();return;}
            var sim=app.Sim;var c=sim.State.Campaign;
            var p=Page("mastery","Your workshop's legacy",sim.GuildRank,ShowMastery);
            int crafted=0,mastered=0,recruited=0,stories=0;
            foreach(int count in c.CraftedCounts)if(count>0)crafted++;
            foreach(var hero in c.Heroes)if(hero.Recruited)recruited++;
            foreach(bool done in c.QuestClaims)if(done)stories++;
            foreach(int wins in c.BossKills)if(wins>0)mastered++;
            float y=Paragraph(p,"MasteryReputation",c.Reputation+" reputation"+(sim.NextRankReputation>0?" · Next rank at "+sim.NextRankReputation:" · Highest guild rank"),0,ForgeUI.Small,ForgeUI.Gold)+Gap;
            string[] goals={"Recipes forged", "Masterwork recipes", "Town buildings restored", "Stories completed", "Heroes recruited", "Guardians defeated", "Guild orders delivered"};
            string[] values={crafted+" / 20",sim.MasterworkRecipes+" / 20",c.TownLevel+" / 5",stories+" / 30",recruited+" / 3",mastered+" / 5",sim.State.ContractsCompleted.ToString()};
            for(int i=0;i<goals.Length;i++){Rule(p,y);y+=Gap;y=Paragraph(p,"MasteryGoal"+i,goals[i]+"\n"+values[i],y,ForgeUI.Subheading,ForgeUI.Teal)+Gap;}
            y=AddAction(p,"MasteryOrders","Take a guild order",y,ShowGuildOrders);
            y=AddAction(p,"MasteryHeroes","Meet your heroes",y,ShowHeroes,false);
            y=Paragraph(p,"RecipeCollectionTitle","The masterwork collection",y,ForgeUI.Subheading,null,true)+Gap;
            for(int i=0;i<20;i++)
            {
                int recipe=i;string status=c.CraftedCounts[i]>0?WorkshopSimulation.QualityName(c.BestCraftQuality[i])+" · "+c.CraftedCounts[i]+" forged":"Not forged yet";
                if(!sim.IsRecipeUnlocked(i))status="Unlock "+GameContent.Regions[i/4].Name;
                y=Paragraph(p,"RecipeCollection"+i,GameContent.Recipes[i].Name+"\n"+status,y,ForgeUI.Small,c.BestCraftQuality[i]>=3?ForgeUI.Gold:ForgeUI.Muted)+12;
                if(sim.IsRecipeUnlocked(i))y=AddAction(p,"MasterRecipe"+i,"Practice this recipe",y,()=>ShowRecipeDetail(recipe),false);
                y+=Gap;
            }
            EndPage(y);
        }
        void ShowDeparture(int region,int route)
        {
            if(app.Sim.State.ChroniclesEnabled){ShowSagaDeparture(region,route);return;}
            var sim=app.Sim;var place=GameContent.Regions[region];var rewards=sim.PreviewRouteRewards(region,route);
            string name=route==2?place.BossName:route==1?"Rescue expedition":"Supply trail";
            var p=Page("departure",place.Name+" · "+(route==0?2:3)+" encounters",name,()=>ShowDeparture(region,route));
            Landscape("DepartureLandscape",p,region,0,0,1020,340);
            float y=Paragraph(p,"DepartureGoal",route==2?sim.GuardianObjective(region):route==1?"Find the missing townsfolk and help restore their home.":"Secure materials for your next creations.",366,ForgeUI.Subheading,null,true)+Gap;
            y=Paragraph(p,"DepartureParty",GameContent.Heroes[sim.State.Campaign.SelectedHero].Name+" · "+sim.HeroPower+" Power · "+sim.HeroGuard+" Guard",y,ForgeUI.Small,ForgeUI.Teal)+12;
            y=Paragraph(p,"DepartureWeakness",ElementAdvice(region),y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            y=Paragraph(p,"DepartureRewards","+"+rewards.Gold+" gold · +"+rewards.Experience+" hero XP\n+"+rewards.Material+" "+WorkshopSimulation.MaterialName(region).ToLowerInvariant(),y,ForgeUI.Body,ForgeUI.Gold)+Gap;
            string blocker=sim.ExpeditionBlocker(region,route);
            if(blocker!="")y=Paragraph(p,"DepartureBlocker",blocker,y,ForgeUI.Small,ForgeUI.Orange)+Gap;
            y=AddAction(p,"JourneyDepart",blocker==""?"Depart · "+name:"Prepare equipment",y,()=>{
                if(sim.ExpeditionBlocker(region,route)!=""){ShowHeroes();return;}
                if(app.BeginExpedition(region,route))ShowBattle();
            });
            y=Paragraph(p,"DepartureTip","Brace when a heavy attack approaches. Recover restores health once. Your hero and equipment are safe if you lose.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            y=AddAction(p,"DepartureEquipment","Review equipment",y,ShowHeroes,false);
            EndPage(y);
        }
    }
}
