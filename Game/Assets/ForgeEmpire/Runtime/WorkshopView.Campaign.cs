using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        bool showLockedRecipes,showFutureQuests,showCompletedQuests;
        static RawImage CampaignSprite(string name,Transform parent,string asset,int frame,int count,float x,float y,float width,float height)
        {
            if(asset=="heroes")return HandDrawnArt.Person(name,parent,"heroes",frame,new Vector2(x+width*.5f,y+height*.96f),width,height);
            if(asset=="enemies"||asset=="guardians")return HandDrawnArt.Person(name,parent,asset,frame,new Vector2(x+width*.5f,y+height*.96f),width,height);
            if(asset=="equipment")return HandDrawnArt.Icon(name,parent,frame,x,y,width,height);
            if(asset=="worldmap")return ForgeUI.Picture(name,parent,HandDrawnArt.Texture("worldmap"),x,y,width,height);
            if(asset=="battlefields")return HandDrawnArt.Landscape(name,parent,frame,x,y,width,height,true);
            var image=ForgeUI.Picture(name,parent,Resources.Load<Texture2D>("Art/"+asset),x,y,width,height);
            image.uvRect=new Rect(frame/(float)count,0,1f/count,1);return image;
        }
        static RawImage Landscape(string name,Transform parent,int region,float x,float y,float width,float height)
        {
            return HandDrawnArt.Landscape(name,parent,region,x,y,width,height);
        }
        static RawImage EquipmentIcon(string name,Transform parent,int recipe,float x,float y,float size)
        {
            return HandDrawnArt.Icon(name,parent,Mathf.Clamp(recipe,0,GameContent.RecipeCount-1),x,y,size,size);
        }
        string QuestGoal(QuestDefinition quest)
        {
            return quest.Kind==QuestKind.QualityDelivery?"Deliver one unequipped "+GameContent.Recipes[quest.RecipeId].Name+" of Superior (2/3) quality or better.":quest.Objective;
        }
        public void ShowQuests(bool side)
        {
            var p=Page("quests","Chapter "+(app.Sim.UnlockedRegion+1)+" · "+app.Sim.State.Campaign.Reputation+" reputation","Quest journal",()=>ShowQuests(side));
            ForgeUI.Button("MainQuestTab",p,"Story",0,0,498,ActionHeight,side?ForgeUI.Surface:ForgeUI.Gold,side?ForgeUI.Cream:ForgeUI.Background,()=>{rebuildingPage=true;try{ShowQuests(false);}finally{rebuildingPage=false;}});
            ForgeUI.Button("SideQuestTab",p,"Town",522,0,498,ActionHeight,side?ForgeUI.Gold:ForgeUI.Surface,side?ForgeUI.Background:ForgeUI.Cream,()=>{rebuildingPage=true;try{ShowQuests(true);}finally{rebuildingPage=false;}});
            float y=ActionHeight+Gap;
            if(side)y=Paragraph(p,"TownProgress","Town restoration: "+app.Sim.State.Campaign.TownLevel+" / "+GameContent.RegionCount+". Rescue your townsfolk to bring the valley back to life.",y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            int hidden=0,completed=0,shown=0;
            foreach(var definition in GameContent.Quests)
            {
                var quest=definition;if(quest.Main==side)continue;
                bool done=app.Sim.State.Campaign.QuestClaims[quest.Id];
                if(done&&!showCompletedQuests){completed++;continue;}
                if(quest.Region>app.Sim.UnlockedRegion&&!showFutureQuests){hidden++;continue;}
                shown++;bool available=app.Sim.IsQuestAvailable(quest.Id),ready=app.Sim.IsQuestReady(quest.Id);
                y=Paragraph(p,"QuestTitle"+quest.Id,quest.Title,y,ForgeUI.Subheading,null,true)+12;
                y=Paragraph(p,"QuestObjective"+quest.Id,done?"Completed":available?QuestGoal(quest):app.Sim.QuestBlocker(quest.Id),y,ForgeUI.Small,done?ForgeUI.Teal:ForgeUI.Muted)+12;
                y=Paragraph(p,"QuestReward"+quest.Id,(available?app.Sim.QuestProgress(quest.Id)+" / "+quest.Target+" · ":"")+"+"+quest.RewardGold+" gold · +"+quest.RewardReputation+" reputation",y,ForgeUI.Small,ForgeUI.Gold)+20;
                ForgeUI.Button("QuestOpen"+quest.Id,p,"Story details",0,y,498,ActionHeight,ForgeUI.Surface,ForgeUI.Cream,()=>ShowQuestDetail(quest.Id),ForgeUI.Small);
                var claim=ForgeUI.Button("QuestClaim"+quest.Id,p,done?"Completed":ready?"Claim reward":available?"Go to task":"Locked",522,y,498,ActionHeight,ready?ForgeUI.Gold:ForgeUI.Surface,ready?ForgeUI.Background:ForgeUI.Cream,()=>{
                    if(app.Sim.IsQuestReady(quest.Id)){app.ClaimCampaignQuest(quest.Id);RebuildCurrentPage();}else FollowQuest(quest.Id);
                },ForgeUI.Small);
                claim.interactable=available&&!done;
                int progress=app.Sim.QuestProgress(quest.Id);bool readyAtBuild=ready;
                modalRefresh.Add(()=>{if(progress!=app.Sim.QuestProgress(quest.Id)||readyAtBuild!=app.Sim.IsQuestReady(quest.Id))RequestPageRebuild();});
                y+=ActionHeight+Gap;Rule(p,y);y+=Gap;
            }
            if(shown==0)y=Paragraph(p,"QuestEmpty","Every available request is complete. Explore the next region or review your completed stories.",y)+Gap;
            if(hidden>0)y=AddAction(p,"FutureQuests",showFutureQuests?"Hide later chapters":"Show "+hidden+" future quests",y,()=>{showFutureQuests=!showFutureQuests;RebuildCurrentPage();},false);
            else if(showFutureQuests)y=AddAction(p,"FutureQuests","Hide later chapters",y,()=>{showFutureQuests=false;RebuildCurrentPage();},false);
            if(completed>0||showCompletedQuests)y=AddAction(p,"CompletedQuests",showCompletedQuests?"Hide completed stories":"Review "+completed+" completed stories",y,()=>{showCompletedQuests=!showCompletedQuests;RebuildCurrentPage();},false);
            y=AddAction(p,"TabORDERS","Guild orders · Earn gold",y,ShowGuildOrders,false);
            EndPage(y);
        }
        void FollowQuest(int id)
        {
            var quest=GameContent.Quests[id];
            if(!app.Sim.IsQuestAvailable(id)){Notify(app.Sim.QuestBlocker(id));return;}
            if(quest.Kind==QuestKind.OreSupply){CloseModal();FocusStation(Station.Mine);return;}
            if(quest.Kind==QuestKind.Craft){ShowRecipes();return;}
            if(quest.Kind==QuestKind.Delivery||quest.Kind==QuestKind.QualityDelivery){ShowRecipeDetail(quest.RecipeId);return;}
            if(app.Sim.State.Campaign.Heroes[app.Sim.State.Campaign.SelectedHero].Weapon==null){ShowHeroes();return;}
            ShowRegion(quest.Region);
        }
        void ShowQuestDetail(int id)
        {
            var quest=GameContent.Quests[id];
            var p=Page("quest-detail",quest.Npc,quest.Title,()=>ShowQuestDetail(id));
            Landscape("QuestLandscape",p,quest.Region,0,0,1020,300);
            float y=Paragraph(p,"QuestStory",quest.Description,332)+Gap;
            y=Paragraph(p,"QuestGoal",QuestGoal(quest),y,ForgeUI.Subheading,ForgeUI.Gold,true)+Gap;
            y=Paragraph(p,"QuestConsequence",quest.Consequence,y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            y=Paragraph(p,"QuestRewards","Reward: "+quest.RewardGold+" gold · "+quest.RewardReputation+" reputation"+(quest.RewardMaterial>0?"\n"+quest.RewardMaterial+" "+WorkshopSimulation.MaterialName(quest.Region).ToLowerInvariant():""),y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            bool done=app.Sim.State.Campaign.QuestClaims[id],available=app.Sim.IsQuestAvailable(id);
            if(!available)y=Paragraph(p,"QuestLocked",app.Sim.QuestBlocker(id),y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            var action=WideButton(p,"QuestDetailClaim",done?"Completed":app.Sim.IsQuestReady(id)?"Claim reward":"Go to task",y,()=>{
                if(app.Sim.IsQuestReady(id)){ClaimJourneyQuest(id);}else FollowQuest(id);
            });action.interactable=available&&!done;y+=action.GetComponent<RectTransform>().rect.height+Gap;
            if(done&&quest.Main&&app.Sim.MainQuest!=null)
            {
                var next=app.Sim.MainQuest;
                y=AddAction(p,"NextStory",app.Sim.IsQuestReady(next.Id)?"Next reward: "+next.Title:"Continue: "+next.Title,y,()=>{
                    if(app.Sim.IsQuestReady(next.Id))ShowQuestDetail(next.Id);else FollowQuest(next.Id);
                });
            }
            EndPage(y);
        }

        public void ShowRecipes()
        {
            if(app.Sim.State.ChroniclesEnabled){catalogRegion=app.Sim.UnlockedRegion;ShowChronicleCatalog();return;}
            var p=Page("recipes","Workshop / Forge","Choose a recipe",ShowRecipes);
            var sim=app.Sim;
            float materialsHeight=ForgeUI.Height("1000000000 iron ingots available",1020,ForgeUI.Subheading,FontStyle.Bold);
            var materials=ForgeUI.Label("CraftMaterials",p,"",0,0,1020,materialsHeight,ForgeUI.Subheading,ForgeUI.Gold,FontStyle.Bold);
            modalRefresh.Add(()=>materials.text=sim.State.Ingots+" iron ingots available");
            float y=materialsHeight+12;
            y=Paragraph(p,"RecipeHelp","Choose a recipe here, then forge it in the workshop. Kept creations stay safe for heroes and requests.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            if(sim.IsRunning(Station.Forge))y=Paragraph(p,"ForgeBusy","Finish the current craft before changing its recipe.",y,ForgeUI.Small,ForgeUI.Orange)+Gap;
            int hidden=0;
            foreach(var definition in GameContent.Recipes)
            {
                var recipe=definition;bool unlocked=sim.IsRecipeUnlocked(recipe.Id);
                if(!unlocked&&!showLockedRecipes){hidden++;continue;}
                Rule(p,y);y+=Gap;
                float nameH=ForgeUI.Height(recipe.Name,846,ForgeUI.Subheading,FontStyle.Bold);
                EquipmentIcon("RecipeIcon"+recipe.Id,p,recipe.Id,0,y,144);
                ForgeUI.Label("RecipeName"+recipe.Id,p,recipe.Name,174,y,846,nameH,ForgeUI.Subheading,ForgeUI.Cream,FontStyle.Bold);
                float metaH=ForgeUI.Height(recipe.Element+" "+recipe.Slot.ToString().ToLowerInvariant(),846,ForgeUI.Small);
                ForgeUI.Label("RecipeMeta"+recipe.Id,p,recipe.Element+" "+recipe.Slot.ToString().ToLowerInvariant(),174,y+nameH+10,846,metaH,ForgeUI.Small,ForgeUI.Teal);
                y+=Mathf.Max(144,nameH+metaH+10)+16;
                y=Paragraph(p,"RecipeStats"+recipe.Id,recipe.Power+" Power · "+recipe.Guard+" Guard",y,ForgeUI.Body)+12;
                y=Paragraph(p,"RecipeCost"+recipe.Id,"Cost: "+sim.RecipeCost(recipe.Id),y,ForgeUI.Small,ForgeUI.Muted)+20;
                if(!unlocked)y=Paragraph(p,"RecipeLock"+recipe.Id,"Unlock: claim "+GameContent.Quests[(recipe.Region-1)*3+2].Title,y,ForgeUI.Small,ForgeUI.Orange)+16;
                ForgeUI.Button("Recipe"+recipe.Id,p,"Details",0,y,498,ActionHeight,ForgeUI.Surface,ForgeUI.Cream,()=>ShowRecipeDetail(recipe.Id));
                var choose=ForgeUI.Button("RecipeSelect"+recipe.Id,p,unlocked?(sim.State.SelectedRecipe==recipe.Id?"Selected":"Choose"):"Locked",522,y,498,ActionHeight,ForgeUI.Gold,ForgeUI.Background,()=>{
                    if(app.ChooseRecipe(recipe.Id)){CloseModal();FocusStation(Station.Forge);}
                });choose.interactable=unlocked&&!sim.IsRunning(Station.Forge);
                modalRefresh.Add(()=>choose.interactable=sim.IsRecipeUnlocked(recipe.Id)&&!sim.IsRunning(Station.Forge));
                y+=ActionHeight+Gap;
            }
            if(hidden>0||showLockedRecipes)y=AddAction(p,"FutureRecipes",showLockedRecipes?"Hide locked recipes":"Show "+hidden+" future recipes",y,()=>{showLockedRecipes=!showLockedRecipes;RebuildCurrentPage();},false);
            EndPage(y);
        }
        void ShowRecipeDetail(int id)
        {
            if(app.Sim.State.ChroniclesEnabled){ShowArtisan(id);return;}
            var recipe=GameContent.Recipes[id];
            var p=Page("recipe-detail",GameContent.Regions[recipe.Region].Name,recipe.Name,()=>ShowRecipeDetail(id));
            EquipmentIcon("RecipeDetailIcon",p,id,350,0,320);
            float y=Paragraph(p,"RecipeDescription",recipe.Description,348)+Gap;
            y=Paragraph(p,"RecipeAttributes",recipe.Power+" Power · "+recipe.Guard+" Guard\n"+recipe.Element+" · Base value "+recipe.Value+" gold",y,ForgeUI.Subheading,ForgeUI.Teal,true)+Gap;
            y=Paragraph(p,"RecipeMaterials","Requires: "+app.Sim.RecipeCost(id)+"\nYou have "+app.Sim.State.Ingots+" iron ingots"+(recipe.RareCost>0?" and "+app.Sim.State.Campaign.Materials[recipe.Region-1]+" "+WorkshopSimulation.MaterialName(recipe.Region-1).ToLowerInvariant():""),y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            bool unlocked=app.Sim.IsRecipeUnlocked(id);
            if(!unlocked)y=Paragraph(p,"RecipePrerequisite","First claim: "+GameContent.Quests[(recipe.Region-1)*3+2].Title,y,ForgeUI.Small,ForgeUI.Orange)+Gap;
            var choose=WideButton(p,"RecipeDetailSelect",unlocked?"Choose this recipe":"Recipe locked",y,()=>{if(app.ChooseRecipe(id)){CloseModal();FocusStation(Station.Forge);}});
            modalRefresh.Add(()=>{choose.interactable=app.Sim.IsRecipeUnlocked(id)&&!app.Sim.IsRunning(Station.Forge);choose.GetComponentInChildren<Text>().text=app.Sim.IsRunning(Station.Forge)?"Finish current craft":unlocked?"Choose this recipe":"Recipe locked";});
            y+=ActionHeight+Gap;
            EndPage(y);
        }

        public void ShowHeroes()
        {
            if(app.Sim.State.ChroniclesEnabled){ShowParty();return;}
            var p=Page("heroes","Your companions","Heroes of the valley",ShowHeroes);
            var sim=app.Sim;var campaign=sim.State.Campaign;
            float tileHeight=180+112*ForgeUI.TextScale;
            for(int i=0;i<3;i++)
            {
                int id=i;var definition=GameContent.Heroes[i];bool owned=campaign.Heroes[i].Recruited;
                var tile=ForgeUI.Medallion("HeroSelect"+i,p,84+i*348,0,156,()=>{
                    if(campaign.Battle.Active){Notify("Finish the expedition before changing hero.");return;}
                    if(!campaign.Heroes[id].Recruited){ShowHeroRecruit(id);return;}
                    if(sim.SelectHero(id)){app.Save();RebuildCurrentPage();}
                });
                var portrait=CampaignSprite("HeroChoicePortrait",tile.transform,"heroes",i*4,12,12,4,132,132);portrait.color=owned?Color.white:new Color(.55f,.6f,.55f,.8f);
                string label=definition.Name+(owned?"":"\n"+definition.Cost+" G");
                ForgeUI.Label("HeroChoiceName",p,label,i*348,164,324,112*ForgeUI.TextScale,ForgeUI.Small,campaign.SelectedHero==i?ForgeUI.Gold:ForgeUI.Muted,FontStyle.Normal,TextAnchor.UpperCenter);
                modalRefresh.Add(()=>tile.interactable=!campaign.Battle.Active);
            }
            float y=tileHeight+12;
            if(campaign.Battle.Active)y=Paragraph(p,"HeroLocked","Your hero is on an expedition. Hero and equipment changes unlock when it ends.",y,ForgeUI.Small,ForgeUI.Orange)+Gap;
            Landscape("HeroLandscape",p,Mathf.Min(campaign.Chapter,4),0,y,1020,540);
            ForgeUI.Fade("HeroLandscapeFoot",p,0,y+360,1020,180,ForgeUI.Background,true);
            CampaignSprite("HeroPortrait",p,"heroes",campaign.SelectedHero*4,12,270,y+16,480,480);y+=540;
            var definitionNow=GameContent.Heroes[campaign.SelectedHero];var hero=campaign.Heroes[campaign.SelectedHero];
            y=Paragraph(p,"HeroName",definitionNow.Name+" · "+definitionNow.Role,y,ForgeUI.Subheading)+8;
            y=Paragraph(p,"HeroStats",sim.HeroPower+" Power · "+sim.HeroGuard+" Guard · "+sim.HeroMaxHealth+" Health",y,ForgeUI.Body,ForgeUI.Teal)+12;
            y=Paragraph(p,"HeroExperience","Level "+hero.Level+" · "+hero.Experience+" / "+(hero.Level*30)+" XP",y,ForgeUI.Small,ForgeUI.Muted)+20;
            float gearHeight=Mathf.Max(HeroGearCard(p,"Weapon",hero.Weapon,0,y),HeroGearCard(p,"Armor",hero.Armor,522,y));y+=gearHeight+Gap;
            y=AddAction(p,"HeroEquipment","Equipment · "+sim.State.Inventory.Count+" in storage",y,ShowInventory,hero.Weapon==null);
            y=AddAction(p,"HeroWorld",campaign.Battle.Active?"View expedition ›":"Begin an adventure ›",y,ShowWorld,hero.Weapon!=null);
            EndPage(y);
        }
        float HeroGearCard(Transform parent,string slot,CraftedItem item,float x,float y)
        {
            string name=item==null?"None equipped":item.Name;
            float nameH=ForgeUI.Height(name,450,ForgeUI.Small,FontStyle.Bold);
            float height=72*ForgeUI.TextScale+160+nameH+20;
            var card=ForgeUI.Rect(slot+"Equipment",parent,x,y,498,height);
            ForgeUI.Label(slot+"Label",card,slot,24,0,450,72*ForgeUI.TextScale,ForgeUI.Small,ForgeUI.Gold,FontStyle.Normal,TextAnchor.MiddleCenter);
            if(item!=null)EquipmentIcon(slot+"Icon",card,item.RecipeId,169,72*ForgeUI.TextScale,160);
            else ForgeUI.Glyph(slot+"Empty",card,"bag",201,84*ForgeUI.TextScale,96,ForgeUI.Line);
            ForgeUI.Label(slot+"Name",card,name,24,72*ForgeUI.TextScale+160,450,nameH,ForgeUI.Small,item==null?ForgeUI.Muted:ForgeUI.Cream,FontStyle.Normal,TextAnchor.MiddleCenter);
            return height;
        }
        void ShowHeroRecruit(int id)
        {
            var hero=GameContent.Heroes[id];
            var p=Page("recruit",hero.Role,hero.Name,()=>ShowHeroRecruit(id));
            CampaignSprite("RecruitPortrait",p,"heroes",id*4,12,330,0,360,360);
            float y=Paragraph(p,"RecruitStory",hero.Description,388)+Gap;
            y=Paragraph(p,"RecruitStats",hero.Power+" Power · "+hero.Guard+" Guard · "+hero.Health+" Health",y,ForgeUI.Subheading,ForgeUI.Teal,true)+Gap;
            var recruit=WideButton(p,"RecruitHero"+id,"Recruit · "+hero.Cost+" gold",y,()=>{if(app.RecruitHero(id)){app.Sim.SelectHero(id);app.Save();GoBack();}});
            modalRefresh.Add(()=>recruit.interactable=!app.Sim.State.Campaign.Battle.Active&&!app.Sim.State.Campaign.Heroes[id].Recruited&&app.Sim.State.Gold>=hero.Cost);
            EndPage(y+ActionHeight+Gap);
        }

        public void ShowWorld()
        {
            var sim=app.Sim;var battle=sim.State.Campaign.Battle;
            if(sim.State.ChroniclesEnabled){if(sim.SagaInProgress)ShowSaga();else if(battle.Active||battle.Finished)ShowBattle();else ShowAtlas();return;}
            if(battle.Active||battle.Finished){ShowBattle();return;}
            var p=Page("world",sim.CampaignComplete?"The valley restored":"Chapter "+(sim.UnlockedRegion+1)+" of 5 · "+GameContent.Regions[sim.UnlockedRegion].Name,"Atlas of the valley",ShowWorld);
            string hero=GameContent.Heroes[sim.State.Campaign.SelectedHero].Name;
            float y=Paragraph(p,"MapParty",hero+" · "+sim.HeroPower+" Power · "+sim.HeroGuard+" Guard",0,ForgeUI.Small,ForgeUI.Teal)+Gap;
            y=Paragraph(p,"MapObjective","Next: "+sim.NextStep.Title,y,ForgeUI.Small,ForgeUI.Cream)+Gap;
            float mapY=y;const float mapHeight=1275;
            CampaignSprite("WorldMap",p,"worldmap",0,1,0,mapY,1020,mapHeight);
            // Top-left normalized landmarks calibrated to the selected painting.
            Vector2[] locations={new Vector2(.21f,.80f),new Vector2(.77f,.67f),new Vector2(.30f,.265f),new Vector2(.74f,.28f),new Vector2(.55f,.095f)};
            for(int i=0;i<5;i++)
            {
                int id=i;bool unlocked=i<=sim.UnlockedRegion,cleared=sim.IsFrontierCleared(i),current=i==sim.UnlockedRegion&&!sim.CampaignComplete;
                float x=locations[i].x*1020,posY=mapY+locations[i].y*mapHeight;
                if(current)ForgeUI.Light("CurrentFrontier",p,x-86,posY-86,172,172).color=new Color(1,.77f,.35f,.28f);
                var pin=ForgeUI.Medallion("Region"+i,p,x-56,posY-56,112,()=>ShowRegion(id),cleared?"check":!unlocked?"lock":null);
                var pinText=pin.GetComponentInChildren<Text>();pinText.text=unlocked&&!cleared?(i+1).ToString():"";pinText.rectTransform.anchoredPosition=Vector2.zero;pinText.rectTransform.sizeDelta=new Vector2(112,112);pinText.color=current?ForgeUI.Gold:ForgeUI.Teal;
                string label=GameContent.Regions[i].Name;
                float textX=Mathf.Clamp(x-210,0,600),height=ForgeUI.Height(label,420,ForgeUI.Small,FontStyle.Bold);
                var name=ForgeUI.Label("RegionName"+i,p,label,textX,posY+64,420,height,ForgeUI.Small,ForgeUI.Cream,FontStyle.Bold,TextAnchor.UpperCenter);
                var outline=name.gameObject.AddComponent<Outline>();outline.effectColor=ForgeUI.Background;outline.effectDistance=new Vector2(2,-2);
            }
            y+=mapHeight+Gap;
            y=Paragraph(p,"MapLegend","Gold: current frontier · Check: restored · Lock: a future chapter",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            y=Paragraph(p,"MapNextUnlock",sim.ChapterUnlock,y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            y=AddAction(p,"MapContinue","Continue · "+sim.NextStep.Button,y,ResumeJourney);
            y=AddAction(p,"MapHeroes","Prepare your hero ›",y,ShowHeroes,false);
            y=AddAction(p,"MapQuests","Quest journal ›",y,()=>ShowQuests(false),false);
            EndPage(y);
        }
        void ShowRegion(int regionId)
        {
            if(app.Sim.State.ChroniclesEnabled){ShowChronicleRegion(regionId);return;}
            var sim=app.Sim;var region=GameContent.Regions[regionId];
            bool unlocked=regionId<=sim.UnlockedRegion;
            var p=Page("region",unlocked?"Adventure / "+region.Weakness+" weakness":"A frontier ahead",region.Name,()=>ShowRegion(regionId));
            Landscape("RegionBackdrop",p,regionId,0,0,1020,360);
            HandDrawnArt.Person("RegionGuardian",p,"guardians",regionId*4,new Vector2(835,344),256,290);
            float y=Paragraph(p,"RegionLore",region.Description.Split('.')[0]+".",388)+Gap;
            if(!unlocked)
            {
                y=Paragraph(p,"RegionUnlockRequirement",sim.RegionUnlockText(regionId),y,ForgeUI.Subheading,ForgeUI.Gold,true)+Gap;
                y=Paragraph(p,"RegionPreview",region.BossName+" guards this frontier. Its routes supply "+WorkshopSimulation.MaterialName(regionId).ToLowerInvariant()+"; opening the region unlocks four recipes.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
                y=AddAction(p,"LockedRegionJourney","Continue your current journey",y,ResumeJourney);
                EndPage(y);return;
            }
            var hero=sim.State.Campaign.Heroes[sim.State.Campaign.SelectedHero];
            double multiplier=sim.WeaponMultiplier(regionId);
            string element=hero.Weapon==null?"No weapon equipped":GameContent.Recipes[hero.Weapon.RecipeId].Element+" weapon";
            y=Paragraph(p,"PartyStats",GameContent.Heroes[sim.State.Campaign.SelectedHero].Name+" · "+sim.HeroPower+" Power · "+sim.HeroGuard+" Guard\n"+element+" · "+(multiplier>1?"+"+Math.Round((multiplier-1)*100)+"% weapon damage":multiplier<1?"25% damage resisted":"normal damage"),y,ForgeUI.Small,multiplier<1?ForgeUI.Orange:ForgeUI.Teal)+Gap;
            if(hero.Armor==null)y=Paragraph(p,"ArmorWarning","No armor equipped. Craft protection for tougher routes.",y,ForgeUI.Small,ForgeUI.Orange)+Gap;
            y=Paragraph(p,"RegionElements",ElementAdvice(regionId),y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            int[] suggestedPower={15,28,44,62,86},suggestedGuard={9,16,25,35,49};
            y=Paragraph(p,"RegionPreparation","Supply trail suggested: "+suggestedPower[regionId]+" Power · "+suggestedGuard[regionId]+" Guard. Elite and guardian routes are harder.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            y=Paragraph(p,"RegionStoryGoal",sim.GuardianObjective(regionId),y,ForgeUI.Small,ForgeUI.Gold)+Gap;
            string[] names={"Supply trail","Dangerous hunt","Boss · "+region.BossName};
            string[] hints={"2 encounters · Lower risk · Material supply","3 encounters · Elite enemies · More rewards","3 encounters · Region guardian · Advance the story"};
            for(int route=0;route<3;route++)
            {
                int routeId=route;Rule(p,y);y+=Gap;
                y=Paragraph(p,"RouteName"+route,names[route],y,ForgeUI.Subheading,null,true)+12;
                y=Paragraph(p,"RouteHint"+route,hints[route],y,ForgeUI.Small,ForgeUI.Muted)+12;
                var rewards=sim.PreviewRouteRewards(regionId,route);
                y=Paragraph(p,"RouteRewards"+route,rewards.Gold+" gold · "+rewards.Material+" "+WorkshopSimulation.MaterialName(regionId).ToLowerInvariant()+"\n"+rewards.Experience+" XP · "+rewards.Reputation+" reputation",y,ForgeUI.Small,ForgeUI.Gold)+20;
                string blocker=sim.ExpeditionBlocker(regionId,route);
                if(blocker!="")y=Paragraph(p,"RouteBlocked"+route,blocker,y,ForgeUI.Small,ForgeUI.Orange)+16;
                y=AddAction(p,"StartRoute"+route,blocker==""?"Depart · "+names[route]:"Prepare your hero",y,()=>{
                    string reason=sim.ExpeditionBlocker(regionId,routeId);
                    if(reason!=""){if(hero.Weapon==null)ShowHeroes();else Notify(reason);return;}
                    if(app.BeginExpedition(regionId,routeId))ShowBattle();
                },route==0);
            }
            y=Paragraph(p,"ExpeditionHint","Your workshop keeps working while your hero travels.",y,ForgeUI.Small,ForgeUI.Muted);
            EndPage(y);
        }
        string ElementAdvice(int region)
        {
            var sim=app.Sim;string element=GameContent.Regions[region].Weakness;
            int weaponBonus=sim.State.Campaign.SelectedHero==2?60:30,armorBonus=sim.State.Campaign.SelectedHero==2?30:20;
            string weapon=sim.WeaponMultiplier(region)>1?"Your weapon matches: +"+weaponBonus+"% damage.":element+" weapons give +"+weaponBonus+"% damage.";
            string armor=sim.ArmorMultiplier(region)<1?"Your armor matches: "+armorBonus+"% less incoming damage.":element+" armor gives "+armorBonus+"% less incoming damage.";
            return weapon+"\n"+armor;
        }
    }
}
