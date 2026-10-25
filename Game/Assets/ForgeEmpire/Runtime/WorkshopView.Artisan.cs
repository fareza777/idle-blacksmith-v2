using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        int catalogRegion,catalogSlot=-1;
        RectTransform artisanCursor;
        Text artisanTiming;
        public void ShowArtisan(int recipeId=-1)
        {
            var sim=app.Sim;bool active=sim.MasterworkActive;
            int id=active?sim.State.Artisan.Project.RecipeId:recipeId>=0?recipeId:sim.IsRunning(Station.Forge)?sim.State.PendingRecipe:sim.State.SelectedRecipe;
            var recipe=GameContent.Recipes[id];var project=sim.State.Artisan.Project;
            bool paid=sim.IsRunning(Station.Forge)&&sim.State.PendingRecipe==id;
            var p=Page("artisan","Forge / "+(active?project.Stage.ToString():"Choose an alloy"),recipe.Name,()=>ShowArtisan(id));
            ForgeUI.Picture("ArtisanWorkbench",p,HandDrawnArt.Texture("forge-closeup"),0,0,1020,574);
            HandDrawnArt.Icon("ArtisanPiece",p,id,360,230,300,230);
            ForgeUI.Fade("WorkbenchFoot",p,0,430,1020,144,ForgeUI.Background,true);
            float y=574+Gap;
            y=Paragraph(p,"ArtisanSteps","01 Alloy  →  02 Shape  →  03 Quench  →  04 Rune",y,ForgeUI.Small,ForgeUI.Gold)+Gap;
            if(!active)
            {
                y=Paragraph(p,"ArtisanCost",paid?"This piece's recipe materials are already reserved. Take over shaping; only an alloy's extra ingots cost more.":"Recipe: "+sim.RecipeCost(id)+"\nChoose an alloy to reserve the materials once. The piece waits safely whenever you leave this bench.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
                bool waiting=sim.IsRunning(Station.Forge)&&!paid;
                if(waiting)
                {
                    y=Paragraph(p,"ArtisanBusy","The worker is finishing "+GameContent.Recipes[sim.State.PendingRecipe].Name+". Pause the forge worker to keep the bench free after this piece.",y,ForgeUI.Small,ForgeUI.Orange)+Gap;
                    if(sim.State.Automated[2]&&!sim.State.AutomationPaused[2])y=AddAction(p,"ArtisanPauseWorker","Pause the forge worker after this piece",y,()=>{if(sim.SetAutomationPaused(Station.Forge,true)){app.Save();RebuildCurrentPage();}},false);
                    modalRefresh.Add(()=>{if(!sim.IsRunning(Station.Forge)||sim.State.PendingRecipe==id)RequestPageRebuild();});
                }
                foreach(var option in WorkshopSimulation.AlloyOptions)
                {
                    int alloy=option.Id;Rule(p,y);y+=Gap;
                    y=Paragraph(p,"AlloyDescription"+alloy,option.Name+"\n"+option.Description,y,ForgeUI.Body)+16;
                    var button=WideButton(p,"BeginMasterwork"+alloy,(paid?"Take over · ":"Begin · ")+option.Name,y,()=>{
                        if(sim.BeginMasterwork(id,alloy)){app.Save();app.Play("smelt");RebuildCurrentPage();}
                        else Notify(sim.WorkBlocker(Station.Forge)==""?"Reserve the recipe materials and extra alloy ingots first.":sim.WorkBlocker(Station.Forge));
                    });button.interactable=sim.IsRecipeUnlocked(id)&&!waiting;y+=button.GetComponent<RectTransform>().rect.height+Gap;
                }
                y=AddAction(p,"RoutineForge","Make an ordinary piece instead",y,()=>{if(sim.SelectRecipe(id)&&sim.TryStart(Station.Forge)){app.Save();ResumeJourney();}else Notify(sim.WorkBlocker(Station.Forge));},false);
                EndPage(y);return;
            }
            y=Paragraph(p,"ArtisanStage","Step "+(project.Stage==ArtisanStage.Heat?1:project.Stage==ArtisanStage.Shape?2:project.Stage==ArtisanStage.Quench?3:4)+" · "+project.Stage,y,ForgeUI.Subheading,ForgeUI.Teal,true)+16;
            y=Paragraph(p,"ArtisanAlloy",WorkshopSimulation.AlloyOptions[project.Alloy].Name+" · "+project.Quality+" / 3 quality\nMaterials already reserved. Your other workers keep producing.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            if(project.Stage==ArtisanStage.Heat||project.Stage==ArtisanStage.Shape)
            {
                y=Paragraph(p,"ArtisanInstruction",project.Stage==ArtisanStage.Heat?"Bring the billet to a steady orange heat. Then shape it with three deliberate strikes.":"Strike as the marker reaches the gold center. Three accurate strikes make quality 3; each strike stays part of this piece.",y)+Gap;
                ForgeUI.Fill("ArtisanTimingTrack",p,0,y,1020,32,ForgeUI.Panel);ForgeUI.Fill("ArtisanSweetSpot",p,418,y-8,184,48,ForgeUI.Gold);
                artisanCursor=ForgeUI.Fill("ArtisanCursor",p,0,y-16,12,64,ForgeUI.Cream).rectTransform;y+=92;
                artisanTiming=ForgeUI.Label("ArtisanTimingText",p,project.Strikes+" / 3 shaping strikes",0,y,1020,ForgeUI.Height("3 / 3 shaping strikes",1020,ForgeUI.Small),ForgeUI.Small,ForgeUI.Teal);y+=ForgeUI.Height("3 / 3 shaping strikes",1020,ForgeUI.Small)+Gap;
                y=AddAction(p,"ArtisanStrike",project.Stage==ArtisanStage.Heat?"Heat the billet":"Strike the anvil",y,()=>{
                    float accuracy=1-Mathf.Abs(Mathf.PingPong(sceneTime*.72f,1)-.5f)*2;
                    var before=project.Stage;
                    if(sim.StrikeMasterwork(accuracy)){app.Save();app.Play("forge");if(project.Stage!=before)RebuildCurrentPage();else artisanTiming.text=project.Strikes+" / 3 shaping strikes · "+project.Quality+" quality";}
                });
            }
            else if(project.Stage==ArtisanStage.Quench)
            {
                foreach(var option in WorkshopSimulation.TemperOptions)
                {int choice=option.Id;y=Paragraph(p,"TemperInfo"+choice,option.Name+"\n"+option.Description,y,ForgeUI.Body)+16;y=AddAction(p,"ArtisanQuench"+choice,"Quench · "+option.Name,y,()=>{if(sim.QuenchMasterwork(choice)){app.Save();app.Play("smelt");RebuildCurrentPage();}});}
            }
            else if(project.Stage==ArtisanStage.Engrave)
            {
                foreach(var option in WorkshopSimulation.RuneOptions)
                {int choice=option.Id;y=Paragraph(p,"RuneInfo"+choice,option.Name+" · "+option.GoldCost+" gold\n"+option.Description,y,ForgeUI.Body)+16;var button=WideButton(p,"ArtisanRune"+choice,"Engrave · "+option.Name,y,()=>{if(sim.EngraveMasterwork(choice)){app.Save();app.Play("click");RebuildCurrentPage();}});button.interactable=sim.State.Gold>=option.GoldCost;y+=button.GetComponent<RectTransform>().rect.height+Gap;}
            }
            else if(project.Stage==ArtisanStage.Ready)
            {
                var preview=new CraftedItem{RecipeId=id,Quality=project.Quality,Alloy=project.Alloy,Temper=project.Temper,Rune=project.Rune,Masterwork=true};
                y=Paragraph(p,"ArtisanFinishedBuild",WorkshopSimulation.ItemBuildDescription(preview),y,ForgeUI.Body,ForgeUI.Teal)+Gap;
                y=AddAction(p,"ArtisanFinish","Finish and keep this creation",y,()=>{if(sim.CompleteMasterwork()){app.Save();app.Play("coin");ShowCraftResult(sim.State.Inventory[sim.State.Inventory.Count-1]);}});
            }
            y=AddAction(p,"ArtisanPause","Leave the bench · Progress is saved",y,ResumeJourney,false);EndPage(y);
        }
        void TickArtisan()
        {
            if(modalKind!="artisan"||!artisanCursor)return;
            var v=artisanCursor.anchoredPosition;v.x=Mathf.PingPong(sceneTime*.72f,1)*1008;artisanCursor.anchoredPosition=v;
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
