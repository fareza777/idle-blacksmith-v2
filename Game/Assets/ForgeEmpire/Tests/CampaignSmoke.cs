#if UNITY_EDITOR || DEVELOPMENT_BUILD || FORGE_QA
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire.Tests
{
    public sealed partial class WorkshopSmoke
    {
        IEnumerator CampaignFlow()
        {
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var simField=typeof(WorkshopApp).GetField("<Sim>k__BackingField",flags);
            var saveField=typeof(WorkshopApp).GetField("savePath",flags);
            var original=app.Sim;string originalPath=app.SavePath;
            var sim=new WorkshopSimulation(new WorkshopState{Gold=2400,Ingots=100,TutorialStep=1});
            try {
                saveField.SetValue(app,Path.Combine(output,"campaign-ui-fixture.json"));simField.SetValue(app,sim);app.Save();app.View.CloseModal();app.View.Refresh();
                var emptyReload=new WorkshopSimulation(JsonUtility.FromJson<WorkshopState>(JsonUtility.ToJson(sim.State)));
                Check(emptyReload.State.Campaign.Heroes[0].Weapon==null&&emptyReload.State.Campaign.Heroes[0].Armor==null,"Unity save converted empty equipment slots into free gear");
                Check(emptyReload.HeroPower==GameContent.Heroes[0].Power,"Empty gear gained combat power after Unity reload");
                var dock=GameObject.Find("ContextDock").GetComponent<RectTransform>();
                var dockHit=Raycast(ScreenPoint(dock,new Vector2(100,-24))).pointerCurrentRaycast.gameObject;
                Check(dockHit==dock.gameObject||dockHit.transform.IsChildOf(dock),"Dock title tap reached obscured workshop controls");
                Vector2 fullSize=app.View.Root.sizeDelta;
                app.View.Root.sizeDelta=new Vector2(1080,1280);app.View.ShowInventory();yield return null;
                var shortAction=GameObject.Find("InventorySell").GetComponent<RectTransform>();
                var outerScroll=shortAction.GetComponentInParent<ScrollRect>();
                Check(outerScroll!=null&&outerScroll.content.rect.height>outerScroll.viewport.rect.height,"Oversized dialog has no scrolling path to its bottom actions");
                outerScroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;
                var actionPoint=Center(shortAction);var bounds=new Vector3[4];outerScroll.viewport.GetWorldCorners(bounds);
                Check(actionPoint.y>=bounds[0].y&&actionPoint.y<=bounds[1].y,"Short portrait dialog action remains outside its viewport after scrolling");
                app.View.CloseModal();app.View.Root.sizeDelta=fullSize;
                Click("TabRECIPES");yield return null;
                Check(GameObject.Find("RecipeSelect19")!=null,"Recipe ledger omitted the final region's equipment");
                Check(!GameObject.Find("RecipeSelect19").GetComponent<Button>().interactable,"Locked legendary recipe can be selected");
                var list=GameObject.Find("RecipeList").GetComponent<ScrollRect>();
                DragInventory(list);Check(list.content.anchoredPosition.y>1,"Long recipe ledger cannot scroll");
                Click("CloseModal");
                for(int recipe=0;recipe<3;recipe++) {
                    Click("TabRECIPES");yield return null;
                    Click("RecipeSelect"+recipe);yield return null;
                    Check(sim.State.SelectedRecipe==recipe&&!app.View.HasModal,"Recipe selection did not return to the real forge");
                    Check(app.View.SelectedStation==Station.Forge,"Recipe selection did not focus the forge");
                    Click("ContextAction");
                    Check(sim.IsRunning(Station.Forge),"Selected recipe did not begin crafting");
                    sim.Advance(sim.GetDuration(Station.Forge)+.01);app.View.Tick(.01f);yield return null;
                    Check(sim.State.Inventory[sim.State.Inventory.Count-1].RecipeId==recipe,"Forge produced a different recipe than selected");
                    if(recipe==0) {
                        Click("TabQUESTS");yield return null;
                        Click("QuestClaim0");Check(sim.State.Campaign.QuestClaims[0],"First main quest did not claim after real crafting");
                        Check(!sim.ClaimQuest(0),"Main quest can pay twice");
                        ScreenCapture.CaptureScreenshot(Path.Combine(output,"campaign-main-quests.png"));yield return null;Click("CloseModal");
                    }
                }
                Click("TabINVENTORY");yield return null;
                Click("InventoryEquip1");yield return null;
                Check(sim.State.Campaign.Heroes[0].Weapon!=null&&sim.State.Campaign.Heroes[0].Weapon.RecipeId==1,"Inventory did not equip the selected weapon");
                Click("InventoryEquip1");yield return null;
                Check(sim.State.Campaign.Heroes[0].Armor!=null&&sim.State.Campaign.Heroes[0].Armor.RecipeId==2,"Armor did not occupy the armor slot");
                Click("CloseModal");Click("TabHEROES");yield return null;
                Check(GameObject.Find("HeroStats").GetComponent<Text>().text.Contains(sim.HeroPower+" ATK"),"Hero panel does not reflect equipment power");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"campaign-hero-equipment.png"));yield return null;
                Click("HeroSelect1");yield return null;Click("RecruitHero1");yield return null;
                Check(sim.State.Campaign.Heroes[1].Recruited,"Recruit hero button did not consume recruitment cost");
                Click("HeroSelect0");yield return null;Click("CloseModal");
                Click("TabWORLD");yield return null;
                Check(GameObject.Find("Region0").GetComponent<Button>().interactable&&!GameObject.Find("Region1").GetComponent<Button>().interactable,"Map chapter gates are incorrect");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"campaign-worldmap.png"));yield return null;
                Click("Region0");yield return null;Click("StartRoute0");yield return null;
                Check(sim.State.Campaign.Battle.Active,"Route departure did not start an expedition");
                var heroRect=GameObject.Find("BattleHero").GetComponent<RectTransform>();
                var enemyRect=GameObject.Find("BattleEnemy").GetComponent<RectTransform>();
                Check(Mathf.Abs(-heroRect.anchoredPosition.y+heroRect.rect.height*57/64-399)<2,"Hero is not grounded on the battlefield");
                Check(Mathf.Abs(-enemyRect.anchoredPosition.y+enemyRect.rect.height*.9f-399)<2,"Enemy is not grounded on the battlefield");
                Click("BattleSkill0");Click("BattleSkill1");
                Check(sim.State.Campaign.Battle.StrikeCooldown>0&&sim.State.Campaign.Battle.GuardSeconds>0,"Battle skill buttons did not affect combat state");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"campaign-battle-active.png"));yield return null;
                for(int tick=0;tick<2400&&sim.State.Campaign.Battle.Active;tick++) {
                    sim.UseBattleSkill(0);sim.UseBattleSkill(1);
                    if(sim.State.Campaign.Battle.HeroHP<sim.State.Campaign.Battle.HeroMaxHP*.5)sim.UseBattleSkill(2);
                    sim.Advance(.1);
                }
                app.View.Tick(.01f);app.View.Refresh();yield return null;
                Check(sim.State.Campaign.Battle.Finished&&sim.State.Campaign.Battle.Won,"Equipped hero did not complete supply route");
                int essence=sim.State.Campaign.Materials[0];
                Click("ClaimExpedition");yield return null;
                Check(sim.State.Campaign.Materials[0]>essence&&!sim.State.Campaign.Battle.Finished,"Expedition loot did not arrive exactly once");
                Check(!sim.ClaimExpedition(),"Expedition can pay its reward twice");
                Click("CloseModal");Click("TabQUESTS");yield return null;
                Click("QuestClaim1");Check(sim.State.Campaign.QuestClaims[1],"Real expedition did not advance the main campaign");
                Click("SideQuestTab");yield return null;
                Check(GameObject.Find("QuestOpen15")!=null,"Side quest stories are missing");
                Click("QuestOpen15");yield return null;
                Check(GameObject.Find("QuestConsequence").GetComponent<Text>().text.Length>20,"Side quest does not explain its world consequence");
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"campaign-side-quest.png"));yield return null;
                Click("CloseModal");app.Save();
                var restored=new WorkshopSimulation(WorkshopSave.Load(app.SavePath,out bool recovered));
                Check(restored.State.Campaign.QuestClaims[1]&&restored.State.Campaign.Heroes[0].Weapon.RecipeId==1&&restored.State.Campaign.Materials[0]>0,"Campaign save lost quest, gear or expedition loot");
            }
            finally {simField.SetValue(app,original);saveField.SetValue(app,originalPath);app.View.CloseModal();app.View.Refresh();}
        }
    }
}
#endif
