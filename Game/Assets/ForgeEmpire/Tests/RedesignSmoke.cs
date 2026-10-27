#if UNITY_EDITOR || DEVELOPMENT_BUILD || FORGE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgeEmpire.Tests
{
    public sealed class RedesignSmoke : MonoBehaviour
    {
        WorkshopApp app;
        int checks;
        string output;
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--redesign-smoke")<0)yield break;
            output=Application.isMobilePlatform?Path.Combine(Application.persistentDataPath,"redesign-qa"):Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/redesign-20261024"));
            Directory.CreateDirectory(output);
            var run=Run();
            while(true)
            {
                bool more=false;object current=null;Exception failure=null;
                try{more=run.MoveNext();if(more)current=run.Current;}catch(Exception error){failure=error;}
                if(failure!=null){Finish(false,failure.ToString());yield break;}
                if(!more)break;
                yield return current;
            }
            Finish(true,"");
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);
            app=FindFirstObjectByType<WorkshopApp>();
            Check(app&&app.Sim!=null,"Game did not start");
            // Advance gameplay explicitly so slow screenshot frames cannot finish a job between UI assertions.
            app.enabled=false;
            app.View.CraftResultsEnabled=false;
            Check(app.SavePath==Path.Combine(output,"isolated-workshop.json"),"QA did not isolate its save before startup");
            var navigation=app.View.Root.Find("Navigation");
            Check(navigation&&navigation.GetComponentsInChildren<Button>(true).Length==3,"Expected three clear primary destinations");
            Check(GameObject.Find("Begin"),"First session has no start action");
            Click("Begin");yield return null;
            app.View.FocusStation(Station.Mine);
            Capture("workshop-start");yield return null;
            Check(GameObject.Find("GuideAction"),"Actionable next step is missing");
            Check(!GameObject.Find("StationTabs")&&!GameObject.Find("GoldPanel"),"Dashboard panels returned to the workshop");
            Check(GameObject.Find("ContextDock").GetComponent<Image>()==null,"A permanent text panel is covering the workshop");
            var world=GameObject.Find("WorldViewport").GetComponent<RectTransform>();
            Check(world.rect.height>=app.View.Root.rect.height*.8f,"Workshop illustration does not dominate the screen");
            Click("UpgradeSelected");yield return null;
            double upgradeGold=app.Sim.State.Gold;
            Click("ConfirmUpgrade");yield return null;
            Check(app.Sim.State.Levels[0]==2&&app.Sim.State.Gold==upgradeGold-20,"Upgrade detail did not apply its displayed cost");
            Click("PageBack");yield return null;
            for(int i=0;i<2;i++)
            {
                app.View.FocusStation(Station.Mine);Click("ContextAction");app.Sim.Advance(2);app.View.Refresh();yield return null;
                app.View.FocusStation(Station.Smelter);Click("ContextAction");app.Sim.Advance(3);app.View.Refresh();yield return null;
            }
            app.View.FocusStation(Station.Forge);Click("ContextAction");yield return null;
            Check(app.Sim.IsRunning(Station.Forge),"Forge action did not reserve real materials");
            Capture("forging");yield return null;
            Click("Strike");app.Sim.Advance(6);app.View.Refresh();yield return null;
            Check(app.Sim.State.Inventory.Count==1&&app.Sim.State.Inventory[0].Protected,"First equipment was not kept for the hero");
            app.View.ShowHeroes();app.View.ShowInventory();yield return null;
            Capture("storage-comparison");yield return null;
            Click("InventoryEquip0");yield return null;
            Check(app.Sim.State.Campaign.Heroes[0].Weapon!=null,"Equip action failed");
            Click("PageBack");yield return null;
            Check(GameObject.Find("HeroStats"),"Back from storage lost hero preparation context");
            Capture("hero-prepared");yield return null;
            Click("NavWorkshop");yield return null;
            app.View.ResumeJourney();Click("ContextAction");yield return null;Click("ContinueJourney");yield return null;
            Check(app.Sim.State.Campaign.QuestClaims[0],"Guide did not claim the earned first quest");
            app.View.ShowWorld();yield return null;
            Check(GameObject.Find("Region0"),"Prepared guide did not open adventure");
            Capture("adventure");yield return null;
            Click("Region0");yield return null;
            Capture("routes");yield return null;
            Click("StartRoute0");yield return null;
            Check(app.Sim.State.Campaign.Battle.Active,"Departure failed");
            Click("BattleSkill0");Click("BattleSkill1");yield return null;
            Capture("battle-active");yield return null;
            while(app.Sim.State.Campaign.Battle.Active)
            {
                app.Sim.UseBattleSkill(0);
                if(app.Sim.State.Campaign.Battle.Elapsed%6>=4.5)app.Sim.UseBattleSkill(1);
                if(app.Sim.State.Campaign.Battle.HeroHP<app.Sim.State.Campaign.Battle.HeroMaxHP*.5)app.Sim.UseBattleSkill(2);
                app.Sim.Advance(.1);
            }
            app.View.Refresh();yield return null;
            Check(app.Sim.State.Campaign.Battle.Won,"Armed hero did not win first supply route");
            Capture("battle-victory");yield return null;
            Click("ClaimExpedition");yield return null;
            Check(!app.Sim.State.Campaign.Battle.Finished&&app.Sim.State.Campaign.Materials[0]>=4,"Loot was not collected");
            Check(!app.Sim.ClaimExpedition(),"Result paid twice");
            Click("QuestDetailClaim");yield return null;
            Check(app.Sim.State.Campaign.QuestClaims[1],"Expedition reward did not lead to its story claim");
            Click("NavAdventure");yield return null;Click("Region0");yield return null;Click("StartRoute1");yield return null;
            app.Sim.State.Campaign.Battle.HeroHP=1;app.Sim.Advance(20);app.View.Refresh();yield return null;
            Check(app.Sim.State.Campaign.Battle.Finished&&!app.Sim.State.Campaign.Battle.Won,"Low-health expedition fixture did not retreat");
            Capture("battle-defeat");yield return null;Click("ClaimExpedition");yield return null;
            Check(GameObject.Find("JourneyConsequence"),"Defeat did not explain preparation");
            Click("ContinueJourney");app.View.ShowHeroes();yield return null;
            app.View.CloseModal();
            for(int i=0;i<3;i++)
            {
                app.View.FocusStation(Station.Mine);Click("ContextAction");app.Sim.Advance(2);app.View.Refresh();yield return null;
                app.View.FocusStation(Station.Smelter);Click("ContextAction");app.Sim.Advance(3);app.View.Refresh();yield return null;
            }
            app.View.ShowRecipes();yield return null;Capture("recipes");yield return null;
            Click("RecipeSelect2");yield return null;
            Click("ContextAction");app.Sim.Advance(6);app.View.Refresh();yield return null;
            app.View.ShowInventory();yield return null;
            Check(GameObject.Find("ItemCompare0").GetComponent<Text>().text.Contains("+6"),"Armor comparison omitted actual Guard gain");
            Click("InventoryProtect0");yield return null;
            Check(!app.Sim.State.Inventory[0].Protected,"Keep toggle did not release selected gear");
            Click("InventorySell0");yield return null;
            Check(app.Sim.IsRunning(Station.Market)&&app.Sim.State.PendingSaleValue==30,"Selected armor was not reserved for sale");
            Check(app.Sim.State.Campaign.Heroes[0].Weapon!=null,"Storage sale consumed equipped weapon");
            app.Sim.Advance(3);app.View.Refresh();yield return null;
            app.Sim.State.Automated[2]=true;app.Sim.State.Ingots=6;app.Sim.Advance(.01);app.Sim.Advance(5);
            app.View.ShowInventory();yield return null;
            var firstStock=app.Sim.State.Inventory[0];app.Sim.Advance(5);
            Check(app.Sim.State.Inventory.Count==2,"Live production fixture did not add a second item");
            app.Sim.TryProtectItem(0,false);Check(app.Sim.TrySellItem(0),"Live stock fixture did not reserve its first item");
            Click("InventoryEquip0");yield return null;
            Check(app.Sim.State.Campaign.Heroes[0].Armor==null&&!app.Sim.State.Inventory.Contains(firstStock),"Stale inventory control equipped the next item instead");
            app.View.Refresh();yield return null;
            Check(GameObject.Find("InventoryEquip0"),"Live inventory list did not refresh after production and sale");
            app.Sim.State.Automated[2]=false;
            app.View.ShowHeroes();app.View.ShowInventory();
            app.View.ShowSettings();yield return null;
            Click("TextSize1");yield return null;
            Check(app.Sim.State.TextSize==1,"125% text setting failed");
            Capture("settings-large");yield return null;
            Click("PageBack");yield return null;Check(GameObject.Find("StockCapacity"),"Text-size rebuild lost inventory Back history");
            Click("PageBack");yield return null;Check(GameObject.Find("HeroStats"),"Text-size rebuild lost hero Back history");
            app.View.ShowOffline(0,120);yield return null;Click("PageBack");yield return null;
            Check(GameObject.Find("HeroStats"),"Offline summary Back lost hero preparation context");
            app.View.ShowSettings();yield return null;
            Click("TextSize2");yield return null;
            Check(app.Sim.State.TextSize==2,"150% text setting failed");
            Click("MotionToggle");yield return null;
            Check(app.Sim.State.ReducedMotion,"Reduced motion did not persist");
            app.View.ShowInventory();yield return null;
            Capture("storage-xl");yield return null;
            app.View.ShowRecipes();yield return null;Capture("recipes-xl");yield return null;
            app.View.ShowHeroes();yield return null;Capture("hero-xl");yield return null;
            app.View.CloseModal();app.View.FocusStation(Station.Forge);yield return null;
            app.Sim.State.Gold=999900;app.Sim.State.Ore=999900;app.Sim.State.Ingots=999900;app.View.Refresh();
            Capture("workshop-xl");yield return null;
            app.View.Root.sizeDelta=new Vector2(1080,1280);
            app.View.FocusStation(Station.Forge);Click("ContextAction");yield return null;
            Check(app.Sim.IsRunning(Station.Forge),"Short-screen forge did not start");
            Capture("workshop-short-xl-forging");yield return null;
            Click("Strike");Click("TabRECIPES");yield return null;
            Check(GameObject.Find("CraftMaterials"),"Short-screen recipe control is obscured");
            Check(!GameObject.Find("WorldMine").GetComponent<Button>().IsInteractable(),"Covered workshop remains keyboard-interactable");
            app.View.ShowSettings();yield return null;
            Capture("settings-short-xl");yield return null;
            var toggle=GameObject.Find("MotionToggle").GetComponent<RectTransform>();
            Check(toggle.GetComponentInParent<ScrollRect>()!=null,"Short page has no path to settings controls");
            Check(GameObject.Find("PageBack"),"Back control vanished on a short page");
            var keyboardTarget=GameObject.Find("HapticsToggle");
            EventSystem.current.SetSelectedGameObject(keyboardTarget);app.View.Tick(0);
            var settingsScroll=keyboardTarget.GetComponentInParent<ScrollRect>();
            var selectedBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(settingsScroll.viewport,keyboardTarget.GetComponent<RectTransform>());
            Check(selectedBounds.min.y>=settingsScroll.viewport.rect.yMin-2&&selectedBounds.max.y<=settingsScroll.viewport.rect.yMax+2,"Keyboard focus did not scroll the selected setting into view");
            settingsScroll.verticalNormalizedPosition=1;app.View.Tick(0);
            Check(settingsScroll.content.anchoredPosition.y<2,"Persistent selection pulled the page back after a manual scroll");
            app.Save();
            var restored=new WorkshopSimulation(WorkshopSave.Load(app.SavePath,out bool recovered));
            Check(restored.State.TextSize==2&&restored.State.ReducedMotion&&restored.State.Campaign.Heroes[0].Weapon!=null,"Preferences/gear lost after Unity save reload");
            var screenshot=new Texture2D(2,2);
            float screenshotDeadline=Time.realtimeSinceStartup+8;
            while(!File.Exists(Path.Combine(output,"workshop-start.png"))&&Time.realtimeSinceStartup<screenshotDeadline)yield return new WaitForSecondsRealtime(.1f);
            Check(screenshot.LoadImage(File.ReadAllBytes(Path.Combine(output,"workshop-start.png"))),"Workshop screenshot missing");
            var pixels=screenshot.GetPixels32();int brightest=0;
            for(int i=0;i<pixels.Length;i+=97)brightest=Math.Max(brightest,Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b)));
            Check(brightest>30,"Captured screenshot is blank; the game must render in a visible window");
            Destroy(screenshot);
        }
        void Capture(string name)
        {
            app.View.Refresh();app.View.Tick(0);
            Canvas.ForceUpdateCanvases();var page=app.View.Root.Find("Page");var nav=app.View.Root.Find("Navigation");
            var layout=new List<string>();
            foreach(var label in app.View.Root.GetComponentsInChildren<Text>())
            {
                if(page&&!label.transform.IsChildOf(page)&&!label.transform.IsChildOf(nav))continue;
                if(string.IsNullOrEmpty(label.text))continue;
                layout.Add(label.transform.parent.name+"/"+label.name+"\t"+label.rectTransform.rect.width+"\t"+label.rectTransform.rect.height+"\t"+label.alignment+"\t"+label.text.Replace("\n"," / "));
                Check(label.fontSize>=42,"Important text is too small: "+label.name);
                Check(label.preferredHeight<=label.rectTransform.rect.height+3,"Text clipped: "+label.transform.parent.name+"/"+label.name+" needs "+label.preferredHeight+" but has "+label.rectTransform.rect.height);
                if(!page)
                {
                    var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(app.View.Root,label.rectTransform);
                    Check(bounds.min.x>=app.View.Root.rect.xMin-2&&bounds.max.x<=app.View.Root.rect.xMax+2,"Workshop text extends beyond the screen: "+label.name);
                }
            }
            File.WriteAllLines(Path.Combine(output,name+".layout.tsv"),layout);
            // Android appends persistentDataPath itself; a rooted path would be appended twice.
            ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?Path.Combine("redesign-qa",name+".png"):Path.Combine(output,name+".png"));
        }
        void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
        void Click(string name)
        {
            var control=GameObject.Find(name);Check(control,"Missing control: "+name);
            var button=control.GetComponent<Button>();Check(button&&button.IsInteractable(),"Unavailable control: "+name);
            Canvas.ForceUpdateCanvases();var rect=button.GetComponent<RectTransform>();
            var scroll=button.GetComponentInParent<ScrollRect>();
            if(scroll&&scroll.enabled&&scroll.content&&rect.IsChildOf(scroll.content))
            {
                float range=Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height);
                float center=scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
                float offset=Mathf.Clamp(scroll.content.anchoredPosition.y+scroll.viewport.rect.center.y-center,0,range);
                scroll.StopMovement();scroll.content.anchoredPosition=new Vector2(0,offset);Canvas.ForceUpdateCanvases();
            }
            var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left,pointerId=-1};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==control,"Control is obscured: "+name);
            ExecuteEvents.Execute(control,data,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(control,data,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(control,data,ExecuteEvents.pointerClickHandler);
        }
        void Finish(bool passed,string error)
        {
            File.WriteAllText(Path.Combine(output,"native-result.json"),"{\"passed\":"+(passed?"true":"false")+",\"checks\":"+checks+",\"error\":\""+error.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"}");
            Debug.Log((passed?"REDESIGN_PASS":"REDESIGN_FAIL")+" "+checks+" "+error);
            Application.Quit(passed?0:1);
        }
    }
}
#endif
