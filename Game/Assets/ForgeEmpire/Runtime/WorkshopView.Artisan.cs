using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        int catalogRegion,catalogSlot=-1;
        int artisanTechnique,artisanTechniqueRecipe=-1;
        RectTransform artisanCursor;
        Text artisanTiming;
        public void ShowArtisan(int recipeId=-1)
        {
            var sim=app.Sim;bool active=sim.MasterworkActive;
            int id=active?sim.State.Artisan.Project.RecipeId:recipeId>=0?recipeId:sim.IsRunning(Station.Forge)?sim.State.PendingRecipe:sim.State.SelectedRecipe;
            id=Mathf.Clamp(id,0,GameContent.Recipes.Length-1);
            if(artisanTechniqueRecipe!=id){artisanTechniqueRecipe=id;artisanTechnique=0;}
            var recipe=GameContent.Recipes[id];var project=sim.State.Artisan.Project;
            var quote=sim.QuoteMasterwork(id,0);bool paid=quote.Reserved;
            artisanCursor=null;artisanTiming=null;
            var p=Page("artisan","Forge / "+(active?ArtisanStageName(project.Stage,id):"Choose your approach"),recipe.Name,()=>ShowArtisan(id));
            if(active&&root.rect.height<1600)
            {
                var section=modal.Find("PageSection").GetComponent<Text>();var heading=modal.Find("PageHeading").GetComponent<Text>();
                heading.font=section.font;heading.fontSize=ForgeUI.Size(ForgeUI.Small);section.gameObject.SetActive(false);
                float header=Mathf.Max(128,ForgeUI.Height(recipe.Name,834,ForgeUI.Small,FontStyle.Bold)+32);
                Place(heading.rectTransform,174,16,834,header-32);Place((RectTransform)modal.Find("PageBack"),30,6,112,112);
                Place((RectTransform)modal.Find("Hairline"),30,header-4,1020,2);Place((RectTransform)modal.Find("Diamond"),535,header-8,10,10);
                Place(pageScroll.viewport,30,header+12,1020,Mathf.Max(180,modal.rect.height-header-24));
            }
            var preview=active?WorkshopSimulation.PreviewMasterwork(id,project.Quality,project.Alloy,project.Temper,project.Rune,project.Technique):WorkshopSimulation.PreviewMasterwork(id,quote.Quality,0,0,0,artisanTechnique);
            float sceneHeight=root.rect.height<1600?(ForgeUI.TextScale>1.25f?220:270):390;
            BuildArtisanBench(p,preview,active?project.Stage:ArtisanStage.Heat,active?project.Strikes:0,sceneHeight,!active);
            float y=ArtisanSteps(p,sceneHeight+12,active?project.Stage:ArtisanStage.Heat,id)+18;
            if(!active)
            {
                string have="Have "+sim.State.Ingots+" ingots · "+WorkshopSimulation.Number(sim.State.Gold)+" gold";
                if(recipe.RareCost>0)have+=" · "+sim.State.Campaign.Materials[recipe.Region-1]+" "+WorkshopSimulation.MaterialName(recipe.Region-1).ToLowerInvariant();
                var available=ForgeUI.Label("ArtisanAvailable",p,have,0,y,1020,ForgeUI.Height(have,1020,ForgeUI.Small),ForgeUI.Small,ForgeUI.Muted);
                y+=available.rectTransform.rect.height+12;
                y=Paragraph(p,"ArtisanTechniqueLead",ArtisanMethodTitle(id)+" · choose your approach before paying",y,ForgeUI.Small,ForgeUI.Gold,true)+12;
                foreach(var technique in WorkshopSimulation.TechniqueOptionsFor(id))
                {
                    int selection=technique.Id;bool selected=artisanTechnique==selection;
                    string label=(selected?"Selected · ":"")+technique.Name;
                    float h=Mathf.Max(ActionHeight,ForgeUI.Height(label,972,ForgeUI.Small,FontStyle.Bold)+28);
                    ForgeUI.Button("ArtisanTechnique"+selection,p,label,0,y,1020,h,selected?ForgeUI.Gold:ForgeUI.Surface,selected?ForgeUI.Background:ForgeUI.Cream,()=>{artisanTechnique=selection;RebuildArtisanPage();},ForgeUI.Small);y+=h+10;
                    if(selected)y=Paragraph(p,"ArtisanTechniqueDescription",technique.Description+"\nNo extra materials. This approach is saved when you begin.",y,ForgeUI.Small,ForgeUI.Teal)+18;
                }
                y=Paragraph(p,"ArtisanCost",paid?"Recipe paid · only the alloy extra is charged now.":"Choose an alloy. Pay its materials once to begin.",y,ForgeUI.Small,ForgeUI.Cream)+18;
                if(sim.State.Inventory.Count>=120&&!paid)y=Paragraph(p,"ArtisanStorageFull","Storage full · equip, deliver or sell a piece first.",y,ForgeUI.Small,ForgeUI.Orange)+18;
                bool waiting=sim.IsRunning(Station.Forge)&&!paid;
                if(waiting)
                {
                    y=Paragraph(p,"ArtisanBusy","Worker finishing "+GameContent.Recipes[sim.State.PendingRecipe].Name+".",y,ForgeUI.Small,ForgeUI.Orange)+18;
                    if(sim.State.Automated[2]&&!sim.State.AutomationPaused[2])y=AddAction(p,"ArtisanPauseWorker","Pause worker after this piece",y,()=>{if(sim.SetAutomationPaused(Station.Forge,true)){app.Save();RebuildCurrentPage();}},false);
                }
                foreach(var option in WorkshopSimulation.AlloyOptions)
                {
                    int alloy=option.Id;var cost=sim.QuoteMasterwork(id,alloy);
                    var choice=WorkshopSimulation.PreviewMasterwork(id,cost.Quality,alloy,0,0,artisanTechnique);
                    y=ArtisanChoiceRow(p,"BeginMasterwork"+alloy,option.Name,choice,ArtisanCostText(cost),paid?"Take over":"Begin",y,()=>{
                        if(sim.BeginMasterwork(id,alloy,artisanTechnique)){app.Save();app.Play("smelt");QueueArtisanCue(ArtisanBenchScene.Cue.Heat);RebuildArtisanPage();}
                        else Notify(sim.QuoteMasterwork(id,alloy).Blocker);
                    },()=>sim.QuoteMasterwork(id,alloy).CanBegin);
                }
                y=Paragraph(p,"ArtisanRuneCost","Rune step later: Unbound is free; a special rune costs 12 gold.",y,ForgeUI.Small,ForgeUI.Muted)+18;
                y=AddAction(p,"RoutineForge",paid?"Continue ordinary production":"Make an ordinary piece",y,()=>{
                    if(sim.IsRunning(Station.Forge)&&sim.State.PendingRecipe==id){ResumeJourney();return;}
                    if(sim.SelectRecipe(id)&&sim.TryStart(Station.Forge)){app.Save();ResumeJourney();}else Notify(sim.WorkBlocker(Station.Forge));
                },false);
                modalRefresh.Add(()=>{
                    available.text="Have "+sim.State.Ingots+" ingots · "+WorkshopSimulation.Number(sim.State.Gold)+" gold"+(recipe.RareCost>0?" · "+sim.State.Campaign.Materials[recipe.Region-1]+" "+WorkshopSimulation.MaterialName(recipe.Region-1).ToLowerInvariant():"");
                    bool nowPaid=sim.IsRunning(Station.Forge)&&sim.State.PendingRecipe==id&&sim.State.Progress[2]<1;
                    if(nowPaid!=paid||waiting!=(sim.IsRunning(Station.Forge)&&!nowPaid)||ForgeUI.Height(available.text,1020,ForgeUI.Small)>available.rectTransform.rect.height)RequestPageRebuild();
                });
                EndPage(y);return;
            }
            y=Paragraph(p,"ArtisanAlloy",WorkshopSimulation.AlloyOptions[project.Alloy].Name+" · "+WorkshopSimulation.QualityName(project.Quality)+" "+project.Quality+" / 3",y,ForgeUI.Small,ForgeUI.Gold)+10;
            y=Paragraph(p,"ArtisanChosenTechnique",WorkshopSimulation.TechniqueOptionsFor(id)[project.Technique].Name+" · "+ArtisanMethodTitle(id),y,ForgeUI.Small,ForgeUI.Cream)+10;
            y=Paragraph(p,"ArtisanCurrentPreview",ArtisanStats(preview),y,ForgeUI.Body,ForgeUI.Teal,true)+10;
            y=Paragraph(p,"ArtisanCurrentAffinities",WorkshopSimulation.ItemAffinitiesDescription(preview),y,ForgeUI.Small,ForgeUI.Teal)+18;
            if(project.Stage==ArtisanStage.Heat)
            {
                y=AddAction(p,"ArtisanStrike",WorkshopSimulation.ArtisanMethodFor(id)==ArtisanMethod.Strike?"Heat the billet":"Warm the fittings",y,()=>{
                    if(sim.StrikeMasterwork(1)){app.Save();app.Play("smelt");QueueArtisanCue(ArtisanBenchScene.Cue.Heat);RebuildArtisanPage();}
                });
                y=Paragraph(p,"ArtisanInstruction","Warm the workpiece, then "+(WorkshopSimulation.ArtisanMethodFor(id)==ArtisanMethod.Fitting?"balance three fitting marks.":WorkshopSimulation.ArtisanMethodFor(id)==ArtisanMethod.Engraving?"trace three channels.":"shape it with three deliberate strikes."),y,ForgeUI.Small,ForgeUI.Muted)+18;
            }
            else if(project.Stage==ArtisanStage.Shape)
            {
                var method=WorkshopSimulation.ArtisanMethodFor(id);
                string instruction=method==ArtisanMethod.Fitting?"Balance the left seam, right seam, then center. Tap as the slower marker enters each gold target.":
                    method==ArtisanMethod.Engraving?"Trace the outer channel, inner channel, then center seal. Tap as the marker reaches the moving gold channel.":
                    "Strike the marked heel, middle, then tip. Tap as the marker reaches the center gold target.";
                if(sim.State.Artisan.Guided||sim.State.ReducedMotion)instruction=(method==ArtisanMethod.Fitting?"Balance the left seam, right seam, then center. ":
                    method==ArtisanMethod.Engraving?"Trace the outer channel, inner channel, then center seal. ":"Strike the marked heel, middle, then tip. ")+"Guided placement records a steady 86% mark for each deliberate tap.";
                y=Paragraph(p,"ArtisanMethodInstruction",instruction,y,ForgeUI.Small,ForgeUI.Muted)+18;
                ArtisanShapingDock(project);
            }
            else if(project.Stage==ArtisanStage.Quench)
            {
                y=Paragraph(p,"ArtisanTimingText",ArtisanLastStrike(project),y,ForgeUI.Small,ForgeUI.Teal)+18;
                foreach(var option in WorkshopSimulation.TemperOptions)
                {
                    int choice=option.Id;var build=WorkshopSimulation.PreviewMasterwork(id,project.Quality,project.Alloy,choice,project.Rune,project.Technique);
                    y=ArtisanChoiceRow(p,"ArtisanQuench"+choice,option.Name,build,"No extra materials","Quench",y,()=>{
                        if(sim.QuenchMasterwork(choice)){app.Save();app.Play("smelt");QueueArtisanCue(ArtisanBenchScene.Cue.Quench);RebuildArtisanPage();}
                    });
                }
            }
            else if(project.Stage==ArtisanStage.Engrave)
            {
                y=Paragraph(p,"ArtisanGold","Have "+WorkshopSimulation.Number(sim.State.Gold)+" gold · choose the finished effect",y,ForgeUI.Small,ForgeUI.Muted)+18;
                foreach(var option in WorkshopSimulation.RuneOptions)
                {
                    int choice=option.Id;var build=WorkshopSimulation.PreviewMasterwork(id,project.Quality,project.Alloy,project.Temper,choice,project.Technique);
                    y=ArtisanChoiceRow(p,"ArtisanRune"+choice,option.Name,build,WorkshopSimulation.ItemAffinitiesDescription(build)+"\n"+(option.GoldCost==0?"Free · keep pattern affinity":option.GoldCost+" gold · changes primary affinity"),choice==0?"Keep affinity":"Engrave",y,()=>{
                        if(sim.EngraveMasterwork(choice)){app.Save();app.Play("click");QueueArtisanCue(ArtisanBenchScene.Cue.Rune);RebuildArtisanPage();}
                        else Notify("This rune needs "+option.GoldCost+" gold.");
                    },()=>sim.State.Gold>=option.GoldCost,false);
                }
            }
            else if(project.Stage==ArtisanStage.Ready)
            {
                var finish=WideButton(p,"ArtisanFinish","Reveal your creation",y,()=>{
                    if(sim.CompleteMasterwork()){app.Save();app.Play("coin");QueueArtisanCue(ArtisanBenchScene.Cue.Reveal);ShowCraftResult(sim.State.Inventory[sim.State.Inventory.Count-1]);}
                    else Notify("Free a storage slot, then reveal this creation.");
                });y+=finish.GetComponent<RectTransform>().rect.height+Gap;
                modalRefresh.Add(()=>finish.interactable=sim.MasterworkActive&&project.Stage==ArtisanStage.Ready&&sim.State.Inventory.Count<120);
                y=Paragraph(p,"ArtisanFinishedBuild",WorkshopSimulation.TemperOptions[project.Temper].Name+" · "+WorkshopSimulation.RuneOptions[project.Rune].Name+"\n"+WorkshopSimulation.ItemAffinitiesDescription(preview),y,ForgeUI.Small,ForgeUI.Teal)+18;
                if(sim.State.Inventory.Count>=120)y=Paragraph(p,"ArtisanStorageFull","Storage full. Your paid creation will wait here.",y,ForgeUI.Small,ForgeUI.Orange)+18;
            }
            y=AddAction(p,"ArtisanPause","Save & leave the bench",y,ResumeJourney,false);
            y=Paragraph(p,"ArtisanSaved","Materials paid · this step is saved · other workers continue.",y,ForgeUI.Small,ForgeUI.Muted)+18;
            if(project.Strikes>0)y=ArtisanStrikeHistory(p,project,y);
            EndPage(y);TickArtisan();
        }
        void TickArtisan()
        {
            if(modalKind!="artisan"&&modalKind!="craft-result")return;
            if(artisanBench!=null&&artisanBench.IsAlive)artisanBench.Tick(sceneTime,app.Sim.State.ReducedMotion);
            if(modalKind=="artisan"&&artisanCursor&&app.Sim.MasterworkActive)
            {var v=artisanCursor.anchoredPosition;v.x=(float)WorkshopSimulation.ArtisanTimingMarker(app.Sim.State.Artisan.Project.RecipeId,sceneTime)*1008;artisanCursor.anchoredPosition=v;}
        }
        void ShowChronicleCatalog()
        {
            var sim=app.Sim;catalogRegion=Mathf.Clamp(catalogRegion,0,5);
            var p=Page("catalog","108 patterns / "+GameContent.Regions[catalogRegion].Name,"The forgebook",ShowChronicleCatalog);
            ForgeUI.Button("CatalogPrevious",p,"‹ Region",0,0,324,ActionHeight,ForgeUI.Surface,ForgeUI.Cream,()=>{catalogRegion=(catalogRegion+5)%6;RebuildCurrentPage();});
            ForgeUI.Button("CatalogNext",p,"Region ›",696,0,324,ActionHeight,ForgeUI.Surface,ForgeUI.Cream,()=>{catalogRegion=(catalogRegion+1)%6;RebuildCurrentPage();});
            ForgeUI.Label("CatalogRegion",p,(catalogRegion+1)+" / 6",348,0,324,ActionHeight,ForgeUI.Body,ForgeUI.Gold,FontStyle.Bold,TextAnchor.MiddleCenter);
            float y=ActionHeight+Gap;
            string[] filters={"All","Weapons","Armor","Charms"};
            int filterColumns=ForgeUI.TextScale>1.25f?2:4;
            float filterWidth=(1020-(filterColumns-1)*12)/filterColumns;
            float filterHeight=ActionHeight;foreach(string label in filters)filterHeight=Mathf.Max(filterHeight,ForgeUI.Height(label,filterWidth-48,ForgeUI.Small,FontStyle.Bold)+24);
            for(int i=0;i<4;i++){int slot=i-1;ForgeUI.Button("CatalogFilter"+i,p,filters[i],i%filterColumns*(filterWidth+12),y+i/filterColumns*(filterHeight+12),filterWidth,filterHeight,catalogSlot==slot?ForgeUI.Gold:ForgeUI.Surface,catalogSlot==slot?ForgeUI.Background:ForgeUI.Cream,()=>{catalogSlot=slot;RebuildCurrentPage();},ForgeUI.Small);}y+=(4/filterColumns)*filterHeight+(4/filterColumns-1)*12+Gap;
            if(catalogRegion>sim.UnlockedRegion)y=Paragraph(p,"CatalogLocked",sim.RegionUnlockText(catalogRegion),y,ForgeUI.Small,ForgeUI.Orange)+Gap;
            y=Paragraph(p,"CatalogMaterials","Available: "+sim.State.Ingots+" ingots · "+sim.State.Inventory.Count+" / 120 storage\nOrdinary worker output is automatic. Named creations use the four-stage artisan bench.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            foreach(var definition in GameContent.Recipes)
            {
                var recipe=definition;if(recipe.Region!=catalogRegion||catalogSlot>=0&&(int)recipe.Slot!=catalogSlot)continue;
                Rule(p,y);y+=Gap;HandDrawnArt.Icon("CatalogIcon"+recipe.Id,p,recipe.Id,0,y,180,180);
                float nameH=ForgeUI.Height(recipe.Name,810,ForgeUI.Subheading,FontStyle.Bold);
                ForgeUI.Label("CatalogName"+recipe.Id,p,recipe.Name,210,y,810,nameH,ForgeUI.Subheading,ForgeUI.Cream,FontStyle.Bold);
                float metaH=ForgeUI.Height(recipe.Element+" · "+recipe.Slot,810,ForgeUI.Small);
                ForgeUI.Label("CatalogKind"+recipe.Id,p,recipe.Element+" · "+recipe.Slot,210,y+nameH+10,810,metaH,ForgeUI.Small,ForgeUI.Teal);y+=Mathf.Max(180,nameH+metaH+10)+16;
                y=Paragraph(p,"CatalogEffect"+recipe.Id,WorkshopSimulation.EffectDescription(recipe.Effect),y,ForgeUI.Small,ForgeUI.Muted)+16;
                y=Paragraph(p,"CatalogCollection"+recipe.Id,sim.State.Campaign.CraftedCounts[recipe.Id]==0?"Not yet forged":sim.State.Campaign.BestCraftQuality[recipe.Id]>=3?"Pattern mastered · Quality 3":"Best quality "+sim.State.Campaign.BestCraftQuality[recipe.Id]+" / 3",y,ForgeUI.Small,sim.State.Campaign.BestCraftQuality[recipe.Id]>=3?ForgeUI.Gold:ForgeUI.Muted)+12;
                y=Paragraph(p,"CatalogCost"+recipe.Id,sim.RecipeCost(recipe.Id)+" · "+recipe.Power+" Power / "+recipe.Guard+" Guard",y,ForgeUI.Small)+20;
                float rowHeight=Mathf.Max(ActionHeight,Mathf.Max(ForgeUI.Height("Artisan bench",450,ForgeUI.Body,FontStyle.Bold),ForgeUI.Height("Set worker pattern",450,ForgeUI.Body,FontStyle.Bold))+24);
                var craft=ForgeUI.Button("CatalogCraft"+recipe.Id,p,"Artisan bench",0,y,498,rowHeight,ForgeUI.Gold,ForgeUI.Background,()=>ShowArtisan(recipe.Id));craft.interactable=sim.IsRecipeUnlocked(recipe.Id);
                var routine=ForgeUI.Button("CatalogChoose"+recipe.Id,p,"Set worker pattern",522,y,498,rowHeight,ForgeUI.Surface,ForgeUI.Cream,()=>{if(sim.PlanRecipe(recipe.Id)){app.Save();Notify("Next worker pattern: "+recipe.Name);}});routine.interactable=sim.IsRecipeUnlocked(recipe.Id);y+=rowHeight+Gap;
            }
            EndPage(y);
        }
    }
}
