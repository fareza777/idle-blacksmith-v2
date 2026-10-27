using UnityEngine;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        void ShowRestoration()
        {
            ShowRestorationRegion(-1);
        }
        void ShowRestorationRegion(int selectedRegion)
        {
            var sim=app.Sim;var p=Page("restoration","A town worth returning to",selectedRegion<0?"People & places":WorkshopSimulation.Restorations[selectedRegion].Name,()=>ShowRestorationRegion(selectedRegion));
            float y=Paragraph(p,"RestorationIntro","Rescue the people of each region, then build with them. Every restored landmark improves the workshop or your expedition party.",0)+Gap;
            for(int region=0;region<6;region++)
            {
                int id=region;if(selectedRegion>=0&&id!=selectedRegion)continue;var d=WorkshopSimulation.Restorations[id];int level=sim.State.World.Projects[id];var preview=sim.PreviewRestoration(id);
                Rule(p,y);y+=Gap;Landscape("RestorationLandscape"+id,p,id,0,y,1020,300);
                DrawRestorationProgress(p,id,level,0,y+12,1020,276);y+=328;
                y=Paragraph(p,"RestorationName"+id,d.Name+" · "+level+" / 3\n"+preview.StageName,y,ForgeUI.Subheading,ForgeUI.Gold,true)+12;
                y=Paragraph(p,"RestorationDescription"+id,d.Description+"\n"+d.Benefit,y,ForgeUI.Body)+16;
                bool rescued=sim.State.Campaign.QuestClaims[GameContent.SideQuestId(id,1)];
                if(level<3)y=Paragraph(p,"RestorationCost"+id,"Stage "+preview.NextStage+": "+preview.GoldCost+" gold + "+preview.MaterialCost+" "+WorkshopSimulation.MaterialName(id).ToLowerInvariant()+"\nOwned: "+WorkshopSimulation.Number(sim.State.Gold)+" gold · "+sim.State.Campaign.Materials[id]+" materials",y,ForgeUI.Small,ForgeUI.Teal)+16;
                if(preview.Blocker.Length>0)y=Paragraph(p,"RestorationPrerequisite"+id,preview.Blocker,y,ForgeUI.Small,level==3?ForgeUI.Teal:ForgeUI.Orange)+16;
                var build=WideButton(p,"RestoreProject"+id,level==3?"Fully restored":"Build stage "+preview.NextStage+" · "+WorkshopSimulation.RestorationStageName(preview.NextStage),y,()=>{if(sim.TryRestoreProject(id)){app.Save();app.Play("coin");RebuildCurrentPage();}else Notify(sim.PreviewRestoration(id).Blocker);});build.interactable=preview.Eligible;y+=build.GetComponent<RectTransform>().rect.height+Gap;
                modalRefresh.Add(()=>{bool eligible=sim.PreviewRestoration(id).Eligible;build.interactable=eligible;if(eligible!=preview.Eligible)RequestPageRebuild();});
                if(level<3&&id<=sim.UnlockedRegion)
                {
                    y=AddAction(p,"TownMaterials"+id,"Gather "+WorkshopSimulation.MaterialName(id).ToLowerInvariant()+" · Supply trail",y,()=>ShowSagaDeparture(id,0),false);
                    y=AddAction(p,"TownFunding"+id,"Earn restoration gold · Guild orders",y,ShowGuildOrders,false);
                }
                y=Paragraph(p,"NpcTrust"+id,d.Npc+" · Trust "+sim.State.World.Trust[id]+" / 4\nEach trust point reduces this landmark's gold cost by 5%.",y,ForgeUI.Small,ForgeUI.Muted)+16;
                for(int topic=0;topic<2;topic++)
                {
                    int conversation=topic;if(!sim.CanTalk(id,topic))continue;
                    y=AddAction(p,"TalkNpc"+id+"Topic"+topic,"Speak with "+d.Npc,y,()=>ShowNpcConversation(id,conversation),false);
                }
                if(!rescued&&id<=sim.UnlockedRegion)y=AddAction(p,"RescueForTown"+id,"Prepare the rescue expedition",y,()=>ShowSagaDeparture(id,1),false);
                int rescueQuest=GameContent.SideQuestId(id,1);
                if(!rescued&&sim.IsQuestReady(rescueQuest))y=AddAction(p,"ClaimTownRescue"+id,"Welcome the rescued townsfolk home",y,()=>ShowQuestDetail(rescueQuest));
            }
            if(selectedRegion>=0)y=AddAction(p,"AllTownProjects","Visit all town projects",y,ShowRestoration,false);
            EndPage(y);
        }
        void ShowNpcConversation(int region,int topic)
        {
            var sim=app.Sim;var d=WorkshopSimulation.Restorations[region];
            var p=Page("npc",GameContent.Regions[region].Name,d.Npc,()=>ShowNpcConversation(region,topic));
            Landscape("NpcHome",p,region,0,0,1020,410);
            HandDrawnArt.Piece("NpcPortrait",p,"people",region,310,10,400,400);
            HandDrawnArt.Piece("NpcLandmark",p,"landmarks",region*2+(sim.State.World.Projects[region]>=2?1:0),740,130,260,250);
            ForgeUI.Fade("NpcPortraitFoot",p,0,354,1020,56,ForgeUI.Background,true);
            float y=Paragraph(p,"NpcQuestion",WorkshopSimulation.NpcQuestion(region,topic),438,ForgeUI.Subheading,ForgeUI.Cream,true)+Gap;
            y=Paragraph(p,"NpcHistory",topic==0?"The trail is open again. The next decision belongs to the people who live here.":"Your rescue party brought the missing townsfolk home. Their work will become part of this place's future.",y,ForgeUI.Body,ForgeUI.Muted)+Gap;
            string[] choices={topic==0?"Invest in the people":"Welcome them into the guild",topic==0?"Support the returning traders":"Help their new business"};
            for(int option=0;option<2;option++)
            {
                int choice=option;y=Paragraph(p,"NpcChoiceEffect"+choice,choice==0?"Trust +2 · Receive 3 regional materials":"Trust +1 · Receive "+(region+1)*45+" gold",y,ForgeUI.Small,ForgeUI.Teal)+16;
                var button=WideButton(p,"NpcChoice"+choice,choices[choice],y,()=>{if(sim.TalkToNpc(region,topic,choice)){app.Save();app.Play("coin");ShowRestoration();}});button.interactable=sim.CanTalk(region,topic);y+=button.GetComponent<RectTransform>().rect.height+Gap;
            }
            EndPage(y);
        }
        void ShowLegends()
        {
            var sim=app.Sim;var p=Page("mastery","The guild's next horizon","Legends of the forge",ShowLegends);
            float y=Paragraph(p,"MasteryOverview",sim.LongTermGoal+"\n"+sim.State.Saga.Mastery+" mastery · Legacy "+sim.State.Saga.Legacy,0,ForgeUI.Body,ForgeUI.Gold)+Gap;
            int companions=0;foreach(var hero in sim.State.Campaign.Heroes)if(hero.Recruited)companions++;
            y=Paragraph(p,"MasteryCollection",sim.MasterworkRecipes+" / 108 patterns mastered · "+companions+" / 6 companions",y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            y=Paragraph(p,"MasteryGuide","Each region has a story guardian and a legendary guardian. Defeat them, create their named commissions, and raise challenge ranks for stronger encounters and richer returns.",y)+Gap;
            for(int boss=0;boss<12;boss++)
            {
                int id=boss;var d=ChronicleContent.Bosses[id];int recipe=sim.LegendaryRecipeId(id);bool defeated=sim.State.Saga.BossMastery[id]>0,delivered=sim.State.World.LegendaryClaims[id];
                Rule(p,y);y+=Gap;
                HandDrawnArt.Person("LegendPortrait"+id,p,"boss-"+(id/4),(id%4)*4,new Vector2(160,y+260),290,270);
                float h=ForgeUI.Height(d.Name,690,ForgeUI.Subheading,UnityEngine.FontStyle.Bold);ForgeUI.Label("LegendName"+id,p,d.Name,330,y,690,h,ForgeUI.Subheading,ForgeUI.Cream,UnityEngine.FontStyle.Bold);
                float ch=ForgeUI.Height("Defeated "+sim.State.Saga.BossMastery[id]+" times",690,ForgeUI.Small);ForgeUI.Label("LegendWins"+id,p,"Defeated "+sim.State.Saga.BossMastery[id]+" times",330,y+h+12,690,ch,ForgeUI.Small,defeated?ForgeUI.Teal:ForgeUI.Muted);y+=Mathf.Max(280,h+ch+12)+16;
                y=Paragraph(p,"LegendCounter"+id,d.Counter,y,ForgeUI.Small,ForgeUI.Muted)+16;
                string challengeBlocker=sim.ExpeditionBlocker(d.Region,id%2==0?2:3);
                if(challengeBlocker.Length>0)y=Paragraph(p,"LegendChallengeBlocker"+id,challengeBlocker,y,ForgeUI.Small,ForgeUI.Orange)+16;
                y=AddAction(p,"ChallengeLegend"+id,id%2==0?"Prepare the story guardian":"Prepare the legend",y,()=>ShowSagaDeparture(d.Region,id%2==0?2:3),false);
                y=Paragraph(p,"LegendaryRequest"+id,"Commission: quality 3 artisan "+GameContent.Recipes[recipe].Name+"\nReward: "+(d.Region+1)*750+" gold + 5 mastery",y,ForgeUI.Small,ForgeUI.Gold)+16;
                string deliveryBlocker=sim.LegendaryContractBlocker(id);
                if(!delivered&&deliveryBlocker.Length>0)y=Paragraph(p,"LegendDeliveryBlocker"+id,deliveryBlocker,y,ForgeUI.Small,ForgeUI.Orange)+16;
                var delivery=WideButton(p,"LegendaryDeliver"+id,delivered?"Legendary commission fulfilled":"Deliver the requested creation",y,()=>{if(sim.ClaimLegendaryContract(id)){app.Save();app.Play("coin");RebuildCurrentPage();}else Notify(sim.LegendaryContractBlocker(id));});delivery.interactable=deliveryBlocker.Length==0;y+=delivery.GetComponent<RectTransform>().rect.height+Gap;
                modalRefresh.Add(()=>{string current=sim.LegendaryContractBlocker(id);delivery.interactable=current.Length==0;if((current.Length==0)!=(deliveryBlocker.Length==0))RequestPageRebuild();});
                if(defeated&&!delivered)
                {
                    var pattern=sim.RecipeWorldGoal(recipe);y=Paragraph(p,"LegendRecipeCost"+id,pattern.Detail+(pattern.Blocker.Length>0?"\n"+pattern.Blocker:""),y,ForgeUI.Small,ForgeUI.Teal)+16;
                    y=AddAction(p,"LegendaryCraft"+id,"View the requested recipe",y,()=>ShowArtisan(recipe),false);
                }
            }
            Rule(p,y);y+=Gap;y=Paragraph(p,"LegacyDescription","A lasting legacy",y,ForgeUI.Subheading,ForgeUI.Gold,true)+16;
            y=Paragraph(p,"LegacyPromise","After completing the story and defeating all twelve guardians, spend "+12*(sim.State.Saga.Legacy+1)+" mastery to add a legacy. Each legacy adds 2% party damage. Your heroes, collection, story and town stay with you.",y)+Gap;
            y=AddAction(p,"AdvanceLegacy","Establish the next legacy",y,()=>{if(sim.AdvanceLegacy()){app.Save();app.Play("coin");RebuildCurrentPage();}else Notify("Complete the story, defeat all twelve guardians and gather the required mastery first.");});EndPage(y);
        }
    }
}
