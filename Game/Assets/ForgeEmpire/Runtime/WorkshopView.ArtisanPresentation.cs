using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        ArtisanBenchScene artisanBench;
        ArtisanBenchScene.Cue pendingArtisanCue;
        float pendingArtisanCueAt;

        void QueueArtisanCue(ArtisanBenchScene.Cue cue)
        {pendingArtisanCue=cue;pendingArtisanCueAt=sceneTime;}

        void RebuildArtisanPage()
        {
            RebuildCurrentPage();
            if(pageFade)pageFade.alpha=1;
            if(modal)modal.anchoredPosition=Vector2.zero;
            RestoreScroll(0);
        }

        RectTransform ArtisanActionDock(string name,float height)
        {
            var viewport=pageScroll.viewport;
            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(140,viewport.rect.height-height));
            var dock=ForgeUI.Fill(name,modal,30,modal.rect.height-height,1020,height,ForgeUI.Background).rectTransform;
            ForgeUI.Ornament(dock,0,0,1020);return dock;
        }

        void ArtisanShapingDock(MasterworkProject project)
        {
            var sim=app.Sim;
            var method=WorkshopSimulation.ArtisanMethodFor(project.RecipeId);
            bool guided=sim.State.Artisan.Guided||sim.State.ReducedMotion;
            string action=method==ArtisanMethod.Fitting?"Fit":method==ArtisanMethod.Engraving?"Trace":"Strike";
            string area=ArtisanBenchScene.WorkAreaName(project.RecipeId,project.Strikes);
            var last=project.Strikes>0&&project.Feedback!=null?project.Feedback[project.Strikes-1]:null;
            string caption=last!=null?"Last "+Mathf.RoundToInt((float)last.Accuracy*100)+"% · Quality "+project.Quality+" / 3":guided?"Guided · 86% per tap":"Aim for the gold mark";
            float textH=ForgeUI.Height(caption,1020,ForgeUI.Small);
            float strikeH=Mathf.Max(ActionHeight,ForgeUI.Height(action+" · "+area+" · 3 / 3",972,ForgeUI.Body,FontStyle.Bold)+30);
            float guideH=Mathf.Max(70,ForgeUI.Height(sim.State.ReducedMotion?"Guided placement · Reduced motion":"Guided placement: ON",972,ForgeUI.Small,FontStyle.Bold)+20);
            var dock=ArtisanActionDock("ArtisanCommandDock",textH+strikeH+guideH+(guided?66:122));
            artisanTiming=ForgeUI.Label("ArtisanTimingText",dock,caption,0,14,1020,textH,ForgeUI.Small,ForgeUI.Teal);
            float y=textH+32;
            float aim=(float)WorkshopSimulation.ArtisanTimingTarget(project.RecipeId,project.Strikes);
            if(!guided)
            {
                ForgeUI.Fill("ArtisanTimingTrack",dock,0,y,1020,24,ForgeUI.Surface);ForgeUI.Fill("ArtisanSweetSpot",dock,(aim-.09f)*1008+6,y-8,181.44f,40,ForgeUI.Gold);
                artisanCursor=ForgeUI.Fill("ArtisanCursor",dock,0,y-14,12,52,ForgeUI.Cream).rectTransform;y+=58;
            }
            ForgeUI.Button("ArtisanStrike",dock,action+" · "+area+" · "+(project.Strikes+1)+" / 3",0,y,1020,strikeH,ForgeUI.Gold,ForgeUI.Background,()=>{
                double accuracy=guided?.86:WorkshopSimulation.ArtisanTimingAccuracy(project.RecipeId,project.Strikes,WorkshopSimulation.ArtisanTimingMarker(project.RecipeId,sceneTime));
                if(sim.StrikeMasterwork(accuracy)){app.Save();app.Play("forge");QueueArtisanCue(ArtisanBenchScene.Cue.Strike);RebuildArtisanPage();}
            });y+=strikeH+10;
            var guide=ForgeUI.Button("ArtisanGuided",dock,sim.State.ReducedMotion?"Guided placement · Reduced motion":"Guided placement: "+(guided?"ON":"OFF"),0,y,1020,guideH,ForgeUI.Surface,ForgeUI.Cream,()=>{sim.State.Artisan.Guided=!sim.State.Artisan.Guided;app.Save();RebuildArtisanPage();},ForgeUI.Small);
            guide.interactable=!sim.State.ReducedMotion;
        }

        static string ArtisanMethodTitle(int recipeId)
        {return WorkshopSimulation.ArtisanMethodFor(recipeId)==ArtisanMethod.Fitting?"Balanced fitting":WorkshopSimulation.ArtisanMethodFor(recipeId)==ArtisanMethod.Engraving?"Channel engraving":"Strike sequence";}

        void BuildArtisanBench(Transform parent,CraftedItem item,ArtisanStage stage,int strikes,float height,bool target=false,bool result=false)
        {
            artisanBench=new ArtisanBenchScene(parent,item,stage,strikes,height,target,result);
            if(pendingArtisanCue!=ArtisanBenchScene.Cue.None)
            {
                artisanBench.Play(pendingArtisanCue,pendingArtisanCueAt);
                pendingArtisanCue=ArtisanBenchScene.Cue.None;
            }
            artisanBench.Tick(sceneTime,app.Sim.State.ReducedMotion);
        }

        static string ArtisanStageName(ArtisanStage stage,int recipeId=0)
        {
            var method=WorkshopSimulation.ArtisanMethodFor(recipeId);
            return stage==ArtisanStage.Heat?"1 / 4 · Warm":stage==ArtisanStage.Shape?"2 / 4 · "+(method==ArtisanMethod.Fitting?"Fit":method==ArtisanMethod.Engraving?"Trace":"Shape"):stage==ArtisanStage.Quench?"3 / 4 · Quench":stage==ArtisanStage.Engrave?"4 / 4 · Rune":"Ready to reveal";
        }

        float ArtisanSteps(Transform parent,float y,ArtisanStage stage,int recipeId=0)
        {
            var method=WorkshopSimulation.ArtisanMethodFor(recipeId);
            string[] labels={"Warm",method==ArtisanMethod.Fitting?"Fit":method==ArtisanMethod.Engraving?"Trace":"Shape","Quench","Rune"};
            float height=0;foreach(string label in labels)height=Mathf.Max(height,ForgeUI.Height(label,243,ForgeUI.Small,FontStyle.Bold));
            int step=stage==ArtisanStage.Heat?0:stage==ArtisanStage.Shape?1:stage==ArtisanStage.Quench?2:3;
            for(int i=0;i<4;i++)
            {
                Color tone=i<step||stage>=ArtisanStage.Ready?ForgeUI.Teal:i==step?ForgeUI.Gold:ForgeUI.Muted;
                ForgeUI.Label("ArtisanStep"+i,parent,labels[i],i*259,y,243,height,ForgeUI.Small,tone,FontStyle.Bold,TextAnchor.MiddleCenter);
                ForgeUI.Fill("ArtisanStepProgress"+i,parent,i*259,y+height+8,243,i==step?6:3,tone);
            }
            return y+height+22;
        }

        static string ArtisanStats(CraftedItem item)
        {return WorkshopSimulation.ArtisanStat(item,true)+" Power · "+WorkshopSimulation.ArtisanStat(item,false)+" Guard";}

        static string ArtisanCostText(ArtisanQuote quote)
        {
            string cost=quote.Ingots+" ingots";
            if(quote.RareCost>0)cost+=" + "+quote.RareCost+" "+WorkshopSimulation.MaterialName(quote.RareRegion).ToLowerInvariant();
            return quote.Ingots==0&&quote.RareCost==0?"Already paid · no extra cost":"Pay now: "+cost;
        }

        float ArtisanChoiceRow(Transform parent,string actionId,string title,CraftedItem build,string detail,string verb,float y,Action choose,Func<bool> eligible=null,bool stats=true)
        {
            Rule(parent,y);y+=18;
            const float textWidth=642;
            float top=y;
            y=Paragraph(parent,actionId+"Title",title,y,ForgeUI.Body,ForgeUI.Cream,true,0,textWidth)+8;
            if(stats)y=Paragraph(parent,actionId+"Preview",ArtisanStats(build),y,ForgeUI.Small,ForgeUI.Teal,false,0,textWidth)+8;
            if(stats&&WorkshopSimulation.ItemTechniqueEffect(build)!=GearEffect.None)y=Paragraph(parent,actionId+"Affinities",WorkshopSimulation.ItemAffinitiesDescription(build),y,ForgeUI.Small,ForgeUI.Teal,false,0,textWidth)+8;
            if(stats)
            {
                var campaign=app.Sim.State.Campaign;var hero=campaign.Heroes[campaign.SelectedHero];
                var slot=GameContent.Recipes[build.RecipeId].Slot;
                var compare=slot==GearSlot.Weapon?hero.Weapon:slot==GearSlot.Armor?hero.Armor:hero.Accessory;
                int power=WorkshopSimulation.ArtisanStat(build,true)-WorkshopSimulation.ArtisanStat(compare,true);
                int guard=WorkshopSimulation.ArtisanStat(build,false)-WorkshopSimulation.ArtisanStat(compare,false);
                string delta=GameContent.Heroes[campaign.SelectedHero].Name+": "+Signed(power)+" Power · "+Signed(guard)+" Guard";
                y=Paragraph(parent,actionId+"Tradeoff",delta,y,ForgeUI.Small,ForgeUI.Muted,false,0,textWidth)+8;
            }
            y=Paragraph(parent,actionId+"Cost",detail,y,ForgeUI.Small,ForgeUI.Muted,false,0,textWidth)+14;
            const float width=354;
            float h=Mathf.Max(ActionHeight,ForgeUI.Height(verb,width-48,ForgeUI.Body,FontStyle.Bold)+28);
            var button=ForgeUI.Button(actionId,parent,verb,666,top,width,h,ForgeUI.Gold,ForgeUI.Background,choose);
            if(eligible!=null){button.interactable=eligible();modalRefresh.Add(()=>button.interactable=eligible());}
            return Mathf.Max(y,top+h)+24;
        }

        static string ArtisanLastStrike(MasterworkProject project)
        {
            string mark=WorkshopSimulation.ArtisanMethodFor(project.RecipeId)==ArtisanMethod.Fitting?"Fit":WorkshopSimulation.ArtisanMethodFor(project.RecipeId)==ArtisanMethod.Engraving?"Trace":"Strike";
            if(project.Strikes==0)return "Three "+mark.ToLowerInvariant()+" marks · quality grows with accurate placement.";
            var feedback=project.Feedback!=null&&project.Feedback.Length>=project.Strikes?project.Feedback[project.Strikes-1]:null;
            if(feedback==null)return project.Strikes+" / 3 strikes saved · quality "+project.Quality+" / 3";
            string rating=feedback.Accuracy>=.82?"True mark":feedback.Accuracy>=.5?"Steady mark":"Wide mark";
            return mark+" "+feedback.Strike+" · "+rating+" · "+(feedback.QualityAfter>feedback.QualityBefore?"+1 quality":feedback.Accuracy>=.82?"quality at maximum":"quality held");
        }

        float ArtisanStrikeHistory(Transform parent,MasterworkProject project,float y)
        {
            string mark=WorkshopSimulation.ArtisanMethodFor(project.RecipeId)==ArtisanMethod.Fitting?"Fit":WorkshopSimulation.ArtisanMethodFor(project.RecipeId)==ArtisanMethod.Engraving?"Trace":"Strike";
            y=Paragraph(parent,"ArtisanTechniqueTitle","Your "+ArtisanMethodTitle(project.RecipeId).ToLowerInvariant()+" marks",y,ForgeUI.Small,ForgeUI.Gold,true)+12;
            for(int i=0;i<project.Strikes;i++)
            {
                var f=project.Feedback!=null&&i<project.Feedback.Length?project.Feedback[i]:null;
                string caption=f==null?mark+" "+(i+1)+" · earlier saved technique":
                    mark+" "+f.Strike+" · "+Mathf.RoundToInt((float)f.Accuracy*100)+"% accuracy · "+(f.QualityAfter>f.QualityBefore?"+1 quality":f.Accuracy>=.82?"quality at maximum":"quality held");
                y=Paragraph(parent,"ArtisanStrikeRecord"+i,caption,y,ForgeUI.Small,f!=null&&f.QualityAfter>f.QualityBefore?ForgeUI.Teal:ForgeUI.Muted)+10;
            }
            return y;
        }

        void ShowArtisanResult(CraftedItem item)
        {
            var sim=app.Sim;var recipe=GameContent.Recipes[item.RecipeId];
            artisanCursor=null;artisanTiming=null;
            var p=Page("craft-result","Creation "+item.Provenance+" / From your forge",recipe.Name,()=>ShowArtisanResult(item));
            float sceneHeight=root.rect.height<1600?290:430;
            BuildArtisanBench(p,item,ArtisanStage.Complete,3,sceneHeight,false,true);
            float y=Paragraph(p,"CraftResultQuality",WorkshopSimulation.QualityName(item.Quality)+" · Quality "+item.Quality+" / 3",sceneHeight+16,ForgeUI.Subheading,ForgeUI.Gold,true)+14;
            y=Paragraph(p,"CraftResultStats",ArtisanStats(item),y,ForgeUI.Body,ForgeUI.Teal,true)+14;
            int index=sim.State.Inventory.IndexOf(item);
            string hero=GameContent.Heroes[sim.State.Campaign.SelectedHero].Name;
            if(index>=0)y=Paragraph(p,"CraftResultComparison",hero+": "+Signed(sim.EquipmentDelta(index,true))+" Power · "+Signed(sim.EquipmentDelta(index,false))+" Guard",y,ForgeUI.Small,ForgeUI.Cream)+18;
            float resultActionH=Mathf.Max(ActionHeight,ForgeUI.Height("Keep in storage",450,ForgeUI.Body,FontStyle.Bold)+28);
            var actions=ArtisanActionDock("ArtisanResultCommands",resultActionH+32);
            var equip=ForgeUI.Button("CraftResultEquip",actions,"Equip "+hero,0,16,498,resultActionH,ForgeUI.Gold,ForgeUI.Background,()=>{
                string blocker=sim.CreationEquipBlocker(item);if(blocker.Length>0){Notify(blocker);RefreshDialog();return;}
                int current=sim.State.Inventory.IndexOf(item);if(!app.Equip(current))return;
                if(journeyFollowing&&sim.State.TotalCrafted<=1)sim.State.KeepCraftedEquipment=false;
                app.Save();ResumeJourney();
            });
            string initialBlocker=sim.CreationEquipBlocker(item);
            float blockerHeight=Mathf.Max(ForgeUI.Height("Finish the expedition before changing equipment.",1020,ForgeUI.Small),ForgeUI.Height("Free a storage slot for the replaced equipment.",1020,ForgeUI.Small));
            var reason=ForgeUI.Label("CraftResultEquipReason",p,initialBlocker,0,y,1020,blockerHeight,ForgeUI.Small,ForgeUI.Orange);
            if(initialBlocker.Length>0)y+=blockerHeight+12;
            ForgeUI.Button("CraftResultContinue",actions,"Keep in storage",522,16,498,resultActionH,ForgeUI.Surface,ForgeUI.Cream,()=>{
                int current=sim.State.Inventory.IndexOf(item);if(current>=0)sim.TryProtectItem(current,true);
                app.Save();ResumeJourney();
            });
            y=Paragraph(p,"CraftResultBuild",WorkshopSimulation.AlloyOptions[item.Alloy].Name+" · "+WorkshopSimulation.TemperOptions[item.Temper].Name+"\n"+WorkshopSimulation.TechniqueOptionsFor(item.RecipeId)[item.Technique].Name+" · "+WorkshopSimulation.RuneOptions[item.Rune].Name+" · "+recipe.Element,y,ForgeUI.Small,ForgeUI.Cream)+14;
            y=Paragraph(p,"CraftResultEffect",WorkshopSimulation.ItemAffinitiesDescription(item),y,ForgeUI.Small,ForgeUI.Teal)+18;
            if(sim.IsCommissionReserved(item))y=Paragraph(p,"CraftResultPurpose","This piece also qualifies for your guild order. Keep it in storage to deliver it.",y,ForgeUI.Small,ForgeUI.Muted)+18;
            var history=sim.State.Artisan.LastCreation;
            if(history!=null&&history.CreationNumber==item.Provenance&&history.RecipeId==item.RecipeId)y=ArtisanStrikeHistory(p,history,y);
            modalRefresh.Add(()=>{
                string blocker=sim.CreationEquipBlocker(item);equip.interactable=blocker.Length==0;
                reason.text=blocker;
                if((initialBlocker.Length==0)!=(blocker.Length==0))RequestPageRebuild();
            });
            EndPage(y);TickArtisan();
        }
    }
}
