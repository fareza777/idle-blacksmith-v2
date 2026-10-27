#if UNITY_EDITOR || DEVELOPMENT_BUILD || FORGE_QA
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgeEmpire.Tests
{
    public sealed partial class ChroniclesSmoke
    {
        // These fixtures use the isolated QA save. Room/resource setup is not earned campaign evidence.
        IEnumerator CheckLivingForge()
        {
            var formations=CheckFormationControls();while(formations.MoveNext())yield return formations.Current;
            var actions=CheckAutomaticWeaponControls();while(actions.MoveNext())yield return actions.Current;
            var journey=CheckJourneyControls();while(journey.MoveNext())yield return journey.Current;
            var crafts=CheckTechniqueControls();while(crafts.MoveNext())yield return crafts.Current;
            var atlas=CheckAtlasControls();while(atlas.MoveNext())yield return atlas.Current;
            var town=CheckRestorationControls();while(town.MoveNext())yield return town.Current;
        }

        void StepLiving(float seconds=.1f)
        {app.Sim.Advance(seconds);app.View.Tick(seconds);app.View.Refresh();}

        IEnumerator CheckAutomaticWeaponControls()
        {
            var sim=app.Sim;var party=(int[])sim.State.Saga.Party.Clone();
            foreach(int hero in new[]{2,0})
            {
                ClearLivingRun();var saved=sim.State.Campaign.Heroes[hero].Weapon;
                sim.State.Campaign.Heroes[hero].Weapon=Gear(hero==2?0:26);sim.State.Saga.Party=new[]{hero,-1,-1};
                Check(sim.StartSaga(0,0),"real automatic weapon fixture departure");app.View.ShowSaga();yield return null;Click("SagaPath0");yield return null;
                StepLiving(.8f);yield return null;StepLiving(.4f);yield return null;
                var u=sim.State.Saga.Run.Units[0];Check(u.Attacks==1&&u.Action==(hero==2?"attack":"cast"),"automatic action follows locked weapon rather than hero identity");
                var weapon=GameObject.Find("SagaActor0Weapon");Check(weapon&&weapon.activeInHierarchy,"actual automatic contact keeps its melee or casting weapon visible");
                Capture(hero==2?"27-real-auto-kael-melee":"27-real-auto-rowan-staff");yield return null;
                ClearLivingRun();sim.State.Campaign.Heroes[hero].Weapon=saved;
            }
            sim.State.Saga.Party=party;
        }

        void ClearLivingRun()
        {app.Sim.State.Saga.Run=null;app.Sim.State.Campaign.Battle=new ExpeditionState();}

        IEnumerator OpenFormationFixture(int room)
        {
            ClearLivingRun();Check(app.Sim.StartSaga(0,0),"formation fixture departure");
            var r=app.Sim.State.Saga.Run;r.Room=room;r.ClearedRooms=room;
            app.View.ShowSaga();yield return null;Click("SagaPath0");yield return null;
            Check(app.Sim.SagaEnemyCount==(room==1?2:3),"authored formation count for room "+room);
            // Delay automatic hero attacks while observing independently scheduled enemy support.
            foreach(var u in r.Units)if(u.HeroId>=0){u.NextAttackTick=r.RoomTick+10000;u.ImpactTick=0;u.TargetEnemy=-1;u.Action="ready";}
        }

        IEnumerator CheckFormationControls()
        {
            for(int room=1;room<=2;room++)
            {
                var start=OpenFormationFixture(room);while(start.MoveNext())yield return start.Current;
                var sim=app.Sim;var r=sim.State.Saga.Run;
                if(room==1){r.Enemies[0].HP=r.Enemies[0].MaxHP*.5;app.View.Refresh();}
                for(int slot=0;slot<sim.SagaEnemyCount;slot++)
                {
                    Click("SagaTarget"+slot);app.View.Tick(0);yield return null;
                    Check(r.FocusEnemy==slot&&app.View.SagaPresentedFocus==slot,"target control selects saved slot "+slot);
                    Check(GameObject.Find("SagaFocusMark"+slot),"focus marker follows actual selected slot");CheckFormationBars();
                }
                Click("SagaTarget0");yield return null;CheckArenaVisible("formation "+sim.SagaEnemyCount);
                Capture("21-formation-"+sim.SagaEnemyCount+"-controls");yield return null;
                bool support=false,ranged=room==1;
                for(int ticks=0;ticks<400&&(!support||!ranged);ticks++)
                {
                    StepLiving();CheckImpactReadability();
                    bool desired=r.ThreatImpactTick>r.RoomTick&&(r.ThreatAction==(room==1?"heal":"protect")||room==2&&r.ThreatAction=="ranged");
                    if(desired)
                    {
                        int source=r.ThreatEnemy,target=r.ThreatSupportTarget,partyTarget=r.ThreatTarget,impact=r.ThreatImpactTick,serial=r.EventSerial;
                        string action=r.ThreatAction;double before=target>=0?(action=="heal"?r.Enemies[target].HP:r.Enemies[target].Barrier):r.Units[partyTarget].HP;
                        Check(app.View.SagaPresentedThreat==source,"announced source marker follows saved threat");
                        int focus=(source+1)%sim.SagaEnemyCount;Click("SagaTarget"+focus);yield return null;
                        Check(r.ThreatEnemy==source&&r.ThreatSupportTarget==target&&r.ThreatTarget==partyTarget&&r.ThreatImpactTick==impact,"focus switch redirected an announced enemy action");
                        Capture("21-formation-"+sim.SagaEnemyCount+"-"+action+"-promise");yield return null;
                        while(r.RoomTick<impact&&r.Phase==SagaPhase.Combat){StepLiving();yield return null;}
                        bool actual=false;
                        foreach(var e in r.Events)if(e.Id>serial&&e.SourceSlot<0&&e.EnemySlot==(target>=0?target:source)&&e.Enemy==(target>=0)&&e.Kind==(action=="heal"?SagaEventKind.Heal:action=="protect"?SagaEventKind.Block:SagaEventKind.Hit))actual=true;
                        Check(actual,"actual "+action+" contact must identify locked source/recipient");
                        if(target>=0)
                        {
                            double after=action=="heal"?r.Enemies[target].HP:r.Enemies[target].Barrier;
                            Check(after>before&&after-before<=r.Enemies[target].MaxHP*(action=="heal"?.1:.12)+.0001,"real support amount reaches saved ally");
                            Check(app.View.SagaLastEnemyEffectRecipient==target,"support paint reached a different enemy slot");support=true;
                        }
                        else
                        {
                            Check(r.Units[partyTarget].HP<before,"real ranged contact must reduce locked companion HP");
                            Check(app.View.SagaLastEnemyEffectSource==source,"ranged paint used focus instead of source");ranged=true;
                        }
                        app.View.Tick(0);CheckFormationBars();CheckImpactReadability();Capture("21-formation-"+sim.SagaEnemyCount+"-"+action+"-contact");yield return null;
                    }
                    yield return null;
                }
                Check(support&&ranged,"formation roles must actually resolve within their ordinary schedule");
                Click("SagaTarget0");yield return null;r.Units[0].NextAttackTick=r.RoomTick+1;
                for(int ticks=0;ticks<80&&r.Units[0].ImpactTick<=r.RoomTick;ticks++){StepLiving();yield return null;}
                var hero=r.Units[0];Check(hero.ImpactTick>r.RoomTick&&hero.TargetEnemy==0,"actual hero starts a locked windup");
                int locked=hero.TargetEnemy,heroImpact=hero.ImpactTick,heroSerial=r.EventSerial;Click("SagaTarget1");yield return null;
                Check(hero.TargetEnemy==locked&&app.View.SagaHeroLockedTarget(0)==locked,"focus switch stole pending hero recipient");
                Capture("21-formation-"+sim.SagaEnemyCount+"-hero-locked");yield return null;
                while(r.RoomTick<heroImpact){StepLiving();yield return null;}
                bool hit=false;foreach(var e in r.Events)if(e.Id>heroSerial&&e.SourceSlot==0&&e.Enemy&&e.EnemySlot==locked&&e.Kind==SagaEventKind.Hit)hit=true;
                Check(hit&&app.View.SagaHeroLockedTarget(0)==locked,"hero contact must retain the promised recipient");
                Check(app.View.SagaLastEnemyEffectRecipient==locked,"hero hit paint must follow actual recipient");
                int tick=r.Tick;Click("SagaPause");yield return null;StepLiving(.5f);Check(r.Tick==tick&&r.Paused,"formation tactical pause advanced simulation");
                Click("SagaDetails");yield return null;Check(GameObject.Find("SagaJournalEnemy1")&&r.Paused,"formation journal exposes actual independent enemies");
                Click("SagaJournalReturn");yield return null;Check(app.View.SagaActiveEffectCount==0&&app.View.SagaPresentedEventSerial==r.EventSerial,"formation reopen replayed old effects");
                Click("SagaPause");yield return null;Check(!r.Paused,"formation explicit play failed");ClearLivingRun();
            }
        }

        void CheckFormationBars()
        {
            var r=app.Sim.State.Saga.Run;app.View.Tick(0);
            for(int slot=0;slot<app.Sim.SagaEnemyCount;slot++)
            {
                var e=r.Enemies[slot];var bar=GameObject.Find("SagaTargetHealth"+slot).GetComponent<RectTransform>();var track=GameObject.Find("SagaTargetHealthTrack"+slot).GetComponent<RectTransform>();
                Check(Math.Abs(bar.rect.width-track.rect.width*e.HP/e.MaxHP)<.1,"enemy bar differs from saved slot "+slot);
                Check(app.View.SagaTargetSize(slot)>0&&app.View.SagaTargetSize(slot)<=330,"foe presentation size cap");
                Check(app.View.SagaTargetPosition(slot).y<=app.View.SagaArenaHeight,"foe feet outside arena");
            }
            string value=GameObject.Find("SagaEnemyVitals").GetComponent<Text>().text;
            Check(value.Contains(Math.Ceiling(r.Enemies[r.FocusEnemy].HP)+" / "+Math.Ceiling(r.Enemies[r.FocusEnemy].MaxHP)),"focused numeric health differs from saved enemy");
        }

        void CheckArenaVisible(string context)
        {
            Canvas.ForceUpdateCanvases();var arena=GameObject.Find("SagaStage").GetComponent<RectTransform>();var viewport=arena.GetComponentInParent<ScrollRect>().viewport;
            var corners=new Vector3[4];arena.GetWorldCorners(corners);
            foreach(var corner in corners){var point=viewport.InverseTransformPoint(corner);Check(point.x>=viewport.rect.xMin-1&&point.x<=viewport.rect.xMax+1&&point.y>=viewport.rect.yMin-1&&point.y<=viewport.rect.yMax+1,context+" arena rect must be completely visible");}
            Check(GameObject.Find("SagaSkill0").GetComponentInParent<ScrollRect>()==null,context+" commands must remain in sticky dock");
        }

        IEnumerator CheckCompactFormation()
        {
            var start=OpenFormationFixture(2);while(start.MoveNext())yield return start.Current;
            for(int slot=0;slot<3;slot++){Click("SagaTarget"+slot);yield return null;Check(app.Sim.State.Saga.Run.FocusEnemy==slot,"compact150 target control "+slot);}
            CheckArenaVisible("compact150 formation");CheckFormationBars();CheckImpactReadability();
            Check(app.View.SagaArenaViewportRatio>=.40f,"compact150 arena must retain a substantial visible scene");
            Capture("28-compact150-three-foe-controls");yield return null;
            app.Sim.State.ReducedMotion=true;StepLiving();
            Check(app.View.SagaFlyingProjectileCount==0,"Reduced motion must suppress traveling projectiles");
            Check(Mathf.Abs(GameObject.Find("SagaActor0").GetComponent<RectTransform>().anchoredPosition.x-500)<.001f,"compact Reduced motion keeps grounded actor0 home");
            Capture("28-compact150-reduced-motion");yield return null;
            app.Sim.State.ReducedMotion=false;ClearLivingRun();
        }

        IEnumerator CheckJourneyControls()
        {
            var sim=app.Sim;var state=sim.State;
            for(int path=3;path<=4;path++)for(int choice=0;choice<3;choice++)
            {
                ClearLivingRun();Check(sim.StartSaga(0,2),"optional journey fixture departure");var r=state.Saga.Run;r.Room=1;r.ClearedRooms=1;
                foreach(var u in r.Units)if(u.HeroId>=0)u.HP=u.MaxHP*.5;
                double gold=state.Gold;int material=state.Campaign.Materials[0];app.View.ShowSaga();yield return null;Click("SagaPath"+path);yield return null;
                Check(r.PendingJourney==path&&r.Room==1,"optional path must create a saved pending transaction");
                // Boon choices retain SagaPath0..2 as the released actual button IDs.
                Check(sim.SagaBoonPending&&sim.CanChooseSagaPath(choice),"pending boon eligibility");
                double hp=r.Units[0].HP,max=r.Units[0].MaxHP;
                Capture("22-"+(path==3?"trader":"waystone")+"-choice-"+choice);yield return null;
                Click("SagaPath"+choice);yield return null;
                int boon=path==3?(choice==0?-1:choice==1?0:1):choice;
                Check(r.PendingJourney==0&&r.Room==2&&r.ClearedRooms==2&&r.RoomChoices[1]==path,"optional choice must advance exactly one room");
                Check(state.Gold==gold-(path==3&&choice==1?25:0)&&state.Campaign.Materials[0]==material-(path==3&&choice==2?1:0),"optional choice exact resource cost");
                Check(r.Boon==boon&&(boon<0||r.BoonUntilRoom==3),"optional choice actual saved boon and expiry");
                Check(Math.Abs(r.Units[0].HP-(boon==1?Math.Min(max,hp+max*.12):hp))<.0001,"Shelter actual heal once");
                string settled=JsonUtility.ToJson(state);Check(!sim.ChooseSagaBoon(choice)&&JsonUtility.ToJson(state)==settled,"repeated boon payment must be a no-op");
                Click("SagaPath0");yield return null;Check(r.Phase==SagaPhase.Combat&&r.Room==2,"released safe path still starts combat after settlement");ClearLivingRun();
            }
        }

        void SetArtisanMarkTime()
        {
            var p=app.Sim.State.Artisan.Project;if(p==null||p.Stage!=ArtisanStage.Shape)return;
            float time=(float)(WorkshopSimulation.ArtisanTimingTarget(p.RecipeId,p.Strikes)/WorkshopSimulation.ArtisanTimingSpeed(p.RecipeId));
            typeof(WorkshopView).GetField("sceneTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(app.View,time);
        }

        void CheckWorkpiece(int recipe,ArtisanStage stage,int strikes)
        {
            int family=(int)WorkshopSimulation.ArtisanWorkpieceFor(recipe);string sheet=family<4?"workpieces-metal":"workpieces-fine";int row=family%4;
            var image=GameObject.Find("ArtisanWorkpiece");Check(image,"unfinished workpiece is a distinct authored image");
            CheckFrame(image.GetComponent<RawImage>(),sheet,row*5+WorkshopSimulation.ArtisanWorkpieceStage(stage,strikes));
            var bench=GameObject.Find("ArtisanBenchScene").GetComponent<RectTransform>();
            var center=bench.InverseTransformPoint(image.GetComponent<RectTransform>().TransformPoint(image.GetComponent<RectTransform>().rect.center));
            Check(Mathf.Abs(center.x-510)<1&&Mathf.Abs(center.y+bench.rect.height*.53f)<1,"actual workpiece stays centered on the anvil under the tool");
            Check(!GameObject.Find("ArtisanPiece"),"finished equipment appeared before assembly");
        }

        IEnumerator CheckTechniqueControls()
        {
            var sim=app.Sim;sim.State.Ingots=100;sim.State.Gold=Math.Max(sim.State.Gold,10000);
            foreach(int recipe in new[]{0,31,26,25})
            {
                sim.State.Artisan.Guided=false;app.View.ShowArtisan(recipe);yield return null;Click("ArtisanTechnique2");yield return null;
                var quote=sim.QuoteMasterwork(recipe,0);int ingots=sim.State.Ingots,rare=quote.RareRegion<0?0:sim.State.Campaign.Materials[quote.RareRegion];
                Click("BeginMasterwork0");yield return null;var p=sim.State.Artisan.Project;
                Check(p.Technique==2&&sim.State.Ingots==ingots-quote.Ingots&&(quote.RareRegion<0||sim.State.Campaign.Materials[quote.RareRegion]==rare-quote.RareCost),"actual technique2 preserves exact paid quote");
                CheckWorkpiece(recipe,ArtisanStage.Heat,0);sim.Advance(.21);Click("ArtisanStrike");yield return null;CheckWorkpiece(recipe,ArtisanStage.Shape,0);
                for(int strike=0;strike<3;strike++)
                {
                    if(strike==1){Click("ArtisanGuided");yield return null;Check(sim.State.Artisan.Guided,"actual guided toggle");}
                    if(strike==2){Click("ArtisanGuided");yield return null;}
                    int count=p.Strikes,quality=p.Quality;sim.Advance(2);app.View.Tick(2);Check(p.Strikes==count&&p.Quality==quality,"waiting must not manufacture guided marks");
                    SetArtisanMarkTime();app.View.Tick(0);
                    if(strike!=1)
                    {
                        var cursor=GameObject.Find("ArtisanCursor").GetComponent<RectTransform>();var sweetSpot=GameObject.Find("ArtisanSweetSpot").GetComponent<RectTransform>();
                        Check(Mathf.Abs(cursor.anchoredPosition.x+cursor.rect.width*.5f-sweetSpot.anchoredPosition.x-sweetSpot.rect.width*.5f)<.1f,"manual marker and actual gold center agree at category-specific timing");
                    }
                    else Check(!GameObject.Find("ArtisanCursor"),"guided marks must not display a false moving marker");
                    Click("ArtisanStrike");app.View.Tick(.15f);yield return null;
                    Check(p.Strikes==strike+1&&p.Feedback[strike]!=null,"real action records one deliberate mark");
                    Check(Math.Abs(p.Feedback[strike].Accuracy-(strike==1?.86:1))<.0001,"manual/guided accuracy must match actual input");
                    CheckWorkpiece(recipe,p.Stage,p.Strikes);
                    var tool=GameObject.Find(WorkshopSimulation.ArtisanMethodFor(recipe)==ArtisanMethod.Engraving?"ArtisanEngravingTool":"ArtisanHammer").GetComponent<RectTransform>();
                    var piece=GameObject.Find("ArtisanItemOnAnvil").GetComponent<RectTransform>();var surface=GameObject.Find("ArtisanWorkpiece").GetComponent<RectTransform>();
                    float expected=piece.anchoredPosition.x+(ArtisanBenchScene.WorkAreaAim(recipe,strike)-.5f)*surface.rect.width;
                    Check(Mathf.Abs(tool.anchoredPosition.x-expected)<1&&Mathf.Abs(tool.anchoredPosition.y-piece.anchoredPosition.y)<1,"painted tool pivot must contact the actual working area");
                    Capture("23-craft-"+recipe+"-stage-"+(strike+1));yield return null;
                }
                Click("ArtisanQuench0");yield return null;CheckWorkpiece(recipe,ArtisanStage.Engrave,3);
                double gold=sim.State.Gold;Click("ArtisanRune1");yield return null;Check(sim.State.Gold==gold-12,"Storm rune actual paid cost");
                var preview=WorkshopSimulation.PreviewMasterwork(recipe,p.Quality,p.Alloy,p.Temper,p.Rune,p.Technique);
                CheckEquipment(GameObject.Find("ArtisanPiece").GetComponent<RawImage>(),recipe);Click("ArtisanFinish");yield return null;
                var item=sim.State.Inventory[sim.State.Inventory.Count-1];Check(item.RecipeId==recipe&&item.Technique==2&&item.Rune==1&&item.Quality==3,"delivered specialized build");
                Check(WorkshopSimulation.ArtisanStat(item,true)==WorkshopSimulation.ArtisanStat(preview,true)&&WorkshopSimulation.ArtisanStat(item,false)==WorkshopSimulation.ArtisanStat(preview,false),"preview and delivered stats differ");
                Check(WorkshopSimulation.ItemEffect(item)==WorkshopSimulation.ItemEffect(preview)&&WorkshopSimulation.ItemTechniqueEffect(item)==WorkshopSimulation.ItemTechniqueEffect(preview)&&WorkshopSimulation.ItemTechniqueEffect(item)!=GearEffect.None&&WorkshopSimulation.ItemTechniqueEffect(item)!=WorkshopSimulation.ItemEffect(item),"both distinct actual affinities survive delivery");
                string effects=GameObject.Find("CraftResultEffect").GetComponent<Text>().text;Check(effects==WorkshopSimulation.ItemAffinitiesDescription(item),"finished text must describe both actual affinities");
                Capture("23-craft-"+recipe+"-delivered");yield return null;
                Click(recipe==0?"CraftResultEquip":"CraftResultContinue");yield return null;
                Check(recipe==0?sim.State.Campaign.Heroes[sim.State.Campaign.SelectedHero].Weapon==item:sim.State.Inventory.Contains(item)&&item.Protected,"actual Equip/Keep result action");
            }
        }

        IEnumerator CheckAtlasControls()
        {
            var sim=app.Sim;ClearLivingRun();Check(sim.StartSaga(0,2),"atlas saved journey fixture");var r=sim.State.Saga.Run;r.Room=1;r.ClearedRooms=1;
            app.View.ShowSaga();yield return null;Click("SagaPath4");yield return null;Click("SagaPath2");yield return null;Click("SagaOpenAtlas");yield return null;
            var nav=GameObject.Find("AtlasViewport").GetComponent<ChronicleAtlasNavigation>();float zoom=nav.Zoom;Vector2 original=nav.Pan;
            Click("AtlasZoomIn");yield return null;Check(nav.Zoom>zoom,"actual atlas zoom-in control");
            Click("AtlasPan0");yield return null;Click("AtlasPan1");yield return null;Check(nav.Pan!=original,"actual atlas pan controls");
            Click("AtlasPan2");yield return null;Click("AtlasPan3");yield return null;
            Click("AtlasZoomOut");yield return null;Check(Mathf.Abs(nav.Zoom-zoom)<.001f,"actual atlas zoom-out control");Click("AtlasRecenter");yield return null;
            Check(nav.Zoom==1&&Vector2.Distance(nav.Pan,original)<1,"actual atlas party recenter restores saved position");
            Click("AtlasZoomIn");yield return null;Canvas.ForceUpdateCanvases();var viewport=nav.GetComponent<RectTransform>();var outer=viewport.GetComponentInParent<ScrollRect>();
            float range=Mathf.Max(0,outer.content.rect.height-outer.viewport.rect.height),center=outer.viewport.InverseTransformPoint(viewport.TransformPoint(viewport.rect.center)).y;
            outer.StopMovement();outer.content.anchoredPosition=new Vector2(0,Mathf.Clamp(outer.content.anchoredPosition.y+outer.viewport.rect.center.y-center,0,range));Canvas.ForceUpdateCanvases();
            var point=RectTransformUtility.WorldToScreenPoint(null,viewport.TransformPoint(viewport.rect.center));Vector2 before=nav.Pan;
            var data=new PointerEventData(EventSystem.current){position=point,delta=new Vector2(-130,-110),button=PointerEventData.InputButton.Left,pointerId=-1};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject)==nav.gameObject,"atlas drag surface must actually receive the pointer");
            ExecuteEvents.Execute(nav.gameObject,data,ExecuteEvents.beginDragHandler);ExecuteEvents.Execute(nav.gameObject,data,ExecuteEvents.dragHandler);ExecuteEvents.Execute(nav.gameObject,data,ExecuteEvents.endDragHandler);
            Check(nav.Pan!=before&&viewport.GetComponentInParent<ScrollRect>().enabled,"atlas real drag handler moves map and restores page scrolling");
            Check(GameObject.Find("AtlasSavedBoon").GetComponent<Text>().text.Contains("Clarity")&&GameObject.Find("SavedRoomTitle1").GetComponent<Text>().text=="Waystone","atlas must read saved route and boon");
            Capture("24-atlas-saved-journey-zoom-pan");yield return null;Click("AtlasResumeSavedJourney");yield return null;Check(GameObject.Find("SagaOpenAtlas")&&r.Room==2,"atlas resume uses current saved expedition");ClearLivingRun();
            foreach(var goal in sim.WorldGoalLinks)if(goal.Eligible)
            {
                app.View.ShowWorld();yield return null;Click("WorldGoal"+goal.Kind);yield return null;
                Check(!GameObject.Find("AtlasConnectionsHeading")&&!GameObject.Find("AtlasZoomIn"),"eligible world goal must open its real destination "+goal.Kind);
            }
            app.View.ShowWorld();yield return null;Click("AtlasRegionButton0");yield return null;Check(GameObject.Find("ChronicleRoute0"),"accessible region button opens actual route preview");
        }

        IEnumerator CheckRestorationControls()
        {
            var sim=app.Sim;var state=sim.State;int[] previous=(int[])state.World.Projects.Clone();
            // All-region visual fixtures cover 24 distinct stage groups; paid region1 checks follow below.
            for(int tier=0;tier<=3;tier++)
            {
                for(int region=0;region<6;region++)state.World.Projects[region]=tier;
                app.View.Refresh();app.View.ShowWorld();yield return null;Click("AtlasTown");yield return null;
                for(int region=0;region<6;region++)
                {
                    Check(GameObject.Find("RestorationStage"+region+"Tier"+tier),"all-region restoration visual group "+region+" / "+tier);
                    if(tier>0)
                    {
                        var prop=GameObject.Find("RestorationMaterials"+region).GetComponent<RectTransform>();
                        Check(prop.rect.width>1&&prop.rect.height>1&&!float.IsNaN(prop.rect.width),"legacy prop metadata must render a real finite restoration object");
                    }
                    for(int stage=1;stage<=3;stage++)Check((bool)GameObject.Find("EarnedTownRegion"+region+"Tier"+stage)==(stage==tier),"workshop only shows the saved earned stage");
                }
                Capture("25-render-fixture-restoration-tier-"+tier);yield return null;
            }
            Array.Copy(previous,state.World.Projects,6);state.World.Projects[1]=0;app.View.Refresh();
            for(int tier=1;tier<=3;tier++)
            {
                app.View.ShowWorld();yield return null;Click("AtlasTown");yield return null;var quote=sim.PreviewRestoration(1);double gold=state.Gold;int stock=state.Campaign.Materials[1];
                Check(quote.Eligible&&quote.NextStage==tier,"actual restoration quote eligibility");Click("RestoreProject1");yield return null;
                Check(state.World.Projects[1]==tier&&state.Gold==gold-quote.GoldCost&&state.Campaign.Materials[1]==stock-quote.MaterialCost,"actual restoration control exact paid tier "+tier);
                app.View.Refresh();Check(GameObject.Find("EarnedTownRegion1Tier"+tier),"paid restoration activates earned workshop scene");
                Capture("25-paid-restoration-tier-"+tier);yield return null;
            }
            var complete=sim.PreviewRestoration(1);double finalGold=state.Gold;int finalStock=state.Campaign.Materials[1];
            Check(!complete.Eligible&&complete.GoldCost==0&&complete.MaterialCost==0&&!sim.TryRestoreProject(1)&&state.Gold==finalGold&&state.Campaign.Materials[1]==finalStock,"complete restoration cannot charge a fourth tier");
            for(int region=0;region<6;region++)
            {
                var blank=BlankGallery("Render fixture · "+WorkshopSimulation.Restorations[region].Name);while(blank.MoveNext())yield return blank.Current;
                var parent=GameObject.Find("PageContent").GetComponent<RectTransform>();var draw=typeof(WorkshopView).GetMethod("DrawRestorationProgress",BindingFlags.NonPublic|BindingFlags.Instance);
                Check(draw!=null,"production restoration scene helper exists");
                for(int tier=0;tier<=3;tier++)
                {
                    float x=tier%2*510,y=160+tier/2*620;
                    ForgeUI.Label("RestorationGalleryCaption"+tier,parent,tier+" · "+WorkshopSimulation.RestorationStageName(tier),x,y-50,490,110,ForgeUI.Small,ForgeUI.Gold,FontStyle.Bold,TextAnchor.UpperCenter);
                    draw.Invoke(app.View,new object[]{parent,region,tier,x,y+70,490f,440f});
                }
                Capture("25-render-fixture-all-stages-region-"+region);yield return null;
            }
        }

        IEnumerator BlankGallery(string title)
        {
            app.View.ShowHeroes();yield return null;var parent=GameObject.Find("PageContent").GetComponent<RectTransform>();
            foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}yield return null;
            parent.sizeDelta=new Vector2(1020,1490);parent.anchoredPosition=Vector2.zero;
            ForgeUI.Label("LivingGalleryTitle",parent,title,0,0,1020,100,ForgeUI.Subheading,ForgeUI.Gold,FontStyle.Bold);
        }

        IEnumerator CheckFoePoseGallery()
        {
            string[] states={"Ready","Windup","Contact","Hurt","Recover","Defeat"};
            for(int sheet=0;sheet<24;sheet++)
            {
                var blank=BlankGallery("Creature poses · sheet "+(sheet+1)+" / 24");while(blank.MoveNext())yield return blank.Current;
                var parent=GameObject.Find("PageContent").GetComponent<RectTransform>();
                for(int frame=0;frame<12;frame++)
                {
                    int id=sheet*2+frame/6,pose=frame%6;string key=FoeAppearance.Sheet(id);float x=170+frame%3*340,y=370+frame/3*330;
                    var layout=JsonUtility.FromJson<HandDrawnArt.Layout>(Resources.Load<TextAsset>("Art/HandDrawn/"+key+"-layout").text);Check(layout.columns==3&&layout.rows==4&&layout.frames.Length==12,"each foe atlas must be three columns by four rows of twelve painted poses");
                    var reference=layout.frames[frame/6*6];Check(Mathf.Abs(layout.frames[frame].referenceWidth-reference.referenceWidth)<.01f&&Mathf.Abs(layout.frames[frame].referenceHeight-reference.referenceHeight)<.01f,"all six creature poses share a stable reference scale");
                    var image=HandDrawnArt.Person("FoeGallery"+frame,parent,key,FoeAppearance.Frame(id,pose),new Vector2(x,y),250,250);CheckFrame(image,key,frame);
                    ForgeUI.Label("FoeGalleryCaption"+frame,parent,(id+1)+" · "+states[pose],frame%3*340,y+8,340,64,ForgeUI.Small,ForgeUI.Muted,FontStyle.Normal,TextAnchor.MiddleCenter);
                }
                Capture("26-foe-pose-sheet-"+sheet.ToString("00"));yield return null;
            }
        }

        IEnumerator CheckHandAttachmentGallery()
        {
            int[] poses={0,2,3,4},armor={-1,2,31,3};string[] states={"Ready","Windup","Contact","Follow"},families={"Base","Mail","Leather","Robe"};
            for(int hero=0;hero<6;hero++)
            {
                var blank=BlankGallery("Painted hands · "+GameContent.Heroes[hero].Name);while(blank.MoveNext())yield return blank.Current;
                var parent=GameObject.Find("PageContent").GetComponent<RectTransform>();
                for(int family=0;family<4;family++)
                {
                    ForgeUI.Label("GripFamily"+family,parent,families[family],family*255,100,255,64,ForgeUI.Small,ForgeUI.Gold,FontStyle.Bold,TextAnchor.MiddleCenter);
                    for(int state=0;state<4;state++)
                    {
                        float y=360+state*320;var actor=new ChronicleActor("GripGallery"+family+"-"+state,parent,hero,Gear(0),armor[family]<0?null:Gear(armor[family]),new Vector2(family*255+78,y),155);
                        GalleryPose(actor,poses[state]);Check(Vector2.Distance(actor.HandPosition,actor.ContactPoint)<.1f,"every outfit's visible handle follows its measured palm");
                        ForgeUI.Label("GripCaption"+family+"-"+state,parent,states[state],family*255,y+10,255,64,ForgeUI.Small,ForgeUI.Muted,FontStyle.Normal,TextAnchor.MiddleCenter);
                    }
                }
                Capture("29-painted-hands-"+hero);yield return null;
            }
        }

        IEnumerator CheckWeaponMotionGallery()
        {
            for(int kind=0;kind<8;kind++)
            {
                int recipe=-1;foreach(var r in GameContent.Recipes)if(r.Slot==GearSlot.Weapon&&(int)r.Kind==kind){recipe=r.Id;break;}Check(recipe>=0,"weapon family recipe "+kind);
                var blank=BlankGallery(((WeaponKind)kind)+" · actual weapon motion");while(blank.MoveNext())yield return blank.Current;
                var parent=GameObject.Find("PageContent").GetComponent<RectTransform>();var actor=new ChronicleActor("MotionActor",parent,0,Gear(recipe),Gear(2),new Vector2(340,800),390);
                var recipient=HandDrawnArt.Person("MotionReferenceFoe",parent,FoeAppearance.Sheet(0),FoeAppearance.Frame(0,0),new Vector2(795,700),280,280);actor.SetContactTarget(new Vector2(795,700),0,recipient.rectTransform.rect.height);
                actor.Pose("windup",0,false);actor.Pose("windup",.6f,false);Check(actor.Body is PaintedBodyImage,"actor uses jointed painted body with RawImage UV compatibility");
                var heldLayout=JsonUtility.FromJson<HandDrawnArt.Layout>(Resources.Load<TextAsset>("Art/HandDrawn/held-weapons-layout").text);Check(heldLayout.columns==4&&heldLayout.rows==2&&heldLayout.frames.Length==8,"held weapons use exactly eight family paintings in a four-by-two atlas");
                var weapon=GameObject.Find("MotionActorWeapon").GetComponent<RawImage>();CheckFrame(weapon,"held-weapons",kind,kind==(int)WeaponKind.Bow);
                if(kind==(int)WeaponKind.Bow)Check(GameObject.Find("MotionActorBowString").GetComponent<Graphic>(),"bow uses a real drawable string");
                float readyAngle=weapon.rectTransform.localEulerAngles.z;Capture("27-weapon-"+kind+"-windup");yield return null;
                actor.Pose("attack",.6f,false);
                Check(Vector2.Distance(actor.HandPosition,actor.ContactPoint)<.1f,"held handle stays in the measured painted palm at actual impact");
                if(kind!=(int)WeaponKind.Bow&&kind!=(int)WeaponKind.Staff)Check(Mathf.Abs(actor.WeaponTip.x-795)<5&&Mathf.Abs(actor.Position.y-700)<1,"rendered melee tip meets the two-dimensional recipient at the actual impact");
                if(kind!=(int)WeaponKind.Bow&&kind!=(int)WeaponKind.Staff)Check(Mathf.Abs(actor.WeaponTip.y-(700-recipient.rectTransform.rect.height*.78f))<5,"actual tip height must meet painted enemy rather than pass over it");
                actor.Pose("attack",.75f,false);float contactAngle=weapon.rectTransform.localEulerAngles.z;
                Check(Vector2.Distance(actor.HandPosition,actor.ContactPoint)<.1f,"held handle stays in the measured painted palm during follow-through");
                Check(Mathf.Abs(Mathf.DeltaAngle(readyAngle,contactAngle))>1||kind==(int)WeaponKind.Bow,"weapon family has distinct windup/contact trajectory");
                if(kind!=(int)WeaponKind.Bow&&kind!=(int)WeaponKind.Staff)Check(actor.WeaponTip.x>=0&&actor.WeaponTip.x<=1020,"contact follow-through keeps the weapon within the arena");
                Capture("27-weapon-"+kind+"-contact");yield return null;
                actor.Pose("ready",1,false);actor.Pose("ready",1.3f,false);Capture("27-weapon-"+kind+"-recovery");yield return null;
                actor.Pose("ready",1.5f,false);Check(Vector2.Distance(actor.Position,actor.Feet)<1,"weapon recovery returns to grounded home");
                if(kind!=(int)WeaponKind.Bow&&kind!=(int)WeaponKind.Staff)
                for(int hero=0;hero<6;hero++)foreach(float height in new[]{180f,380f})
                {
                    var probe=new ChronicleActor("ContactProbe",parent,hero,Gear(recipe),Gear(2),new Vector2(500,800),height);
                    float time=0;
                    for(int target=0;target<4;target++)
                    {
                        float tx=target==0||target==3?720:target==1?930:790,ty=target==0||target==3?800:target==1?650:470,painted=target==3?90:target==0?260:target==1?230:215;
                        probe.SetContactTarget(new Vector2(tx,ty),target==0||target==3?260:target==1?230:215,painted);
                        probe.Pose("windup",time,false);probe.Pose("windup",time+.6f,false);probe.Pose("attack",time+.7f,false);
                        Check(Mathf.Abs(probe.WeaponTip.x-tx)<5&&Mathf.Abs(probe.Position.y-ty)<1,"every hero/family/size tip meets each actual formation depth");
                        Check(probe.WeaponTip.y>=ty-painted*.97f-2&&probe.WeaponTip.y<=ty-painted*.15f+2,"rendered contact misses painted body: hero="+hero+" family="+kind+" height="+height+" target="+target+" tip="+probe.WeaponTip+" body="+painted);
                        Check(Vector2.Distance(probe.HandPosition,probe.ContactPoint)<.1f,"thrust may not detach the held handle from its painted palm");
                        probe.Pose("ready",time+1,false);probe.Pose("ready",time+1.5f,false);Check(Vector2.Distance(probe.Position,probe.Feet)<1,"every contact origin returns home");time+=2;
                    }
                    var go=probe.Body.transform.parent.gameObject;go.SetActive(false);Destroy(go);
                }
            }
        }
    }
}
#endif
