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
    public sealed class ProgressionSmoke : MonoBehaviour
    {
        WorkshopApp app;string output;int checks;
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--progression-smoke")<0)yield break;
            output=Application.isMobilePlatform?Path.Combine(Application.persistentDataPath,"progression-qa"):Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/progression-20261024"));
            Directory.CreateDirectory(output);var run=Run();
            while(true)
            {
                bool more=false;object current=null;Exception failure=null;
                try{more=run.MoveNext();if(more)current=run.Current;}catch(Exception error){failure=error;}
                if(failure!=null){Finish(false,failure.ToString());yield break;}
                if(!more)break;yield return current;
            }
            Finish(true,"");
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);
            app=FindFirstObjectByType<WorkshopApp>();Check(app&&app.Sim!=null,"startup");app.enabled=false;
            Check(app.SavePath==Path.Combine(output,"isolated-workshop.json"),"save isolation");
            Click("Begin");yield return null;
            Check(GameObject.Find("SelectedStationTitle").GetComponent<Text>().text.Contains("Iron Sword"),"opening has no concrete playable goal");
            Capture("01-first-goal");yield return null;
            Click("GuideAction");yield return null;Check(GameObject.Find("JourneyProgress"),"chapter journal missing");
            Capture("02-chapter-journey");yield return null;Click("JourneyContinue");yield return null;
            var production=CraftGuided();while(production.MoveNext())yield return production.Current;
            Check(GameObject.Find("CraftResultEquip"),"manual forging has no equip result");
            Check(app.Sim.State.Inventory.Count==1&&app.Sim.State.Inventory[0].Protected,"guided equipment not reserved");
            Capture("03-craft-result");yield return null;Click("CraftResultEquip");yield return null;
            Check(app.Sim.State.Campaign.Heroes[0].Weapon!=null&&app.Sim.ChapterProgress==1,"direct equip did not advance");
            Click("ContextAction");yield return null;Check(GameObject.Find("JourneyReward"),"story claim has no result");
            Capture("04-story-reward");yield return null;Click("ContinueJourney");yield return null;
            Check(app.Sim.NextStep.Action==GuideAction.Adventure&&app.Sim.NextStep.Route==0,"first scout destination");
            Click("ContextAction");yield return null;Capture("05-departure");yield return null;Click("JourneyDepart");yield return null;
            Fight();app.View.Refresh();yield return null;Check(app.Sim.State.Campaign.Battle.Won,"first scout victory");
            Click("ClaimExpedition");yield return null;Click("QuestDetailClaim");yield return null;Click("ContinueJourney");yield return null;
            Check(app.Sim.NextStep.RecipeId==2,"scouting should lead to protection");
            production=CraftGuided();while(production.MoveNext())yield return production.Current;
            Click("CraftResultEquip");yield return null;
            Check(app.Sim.State.Campaign.Heroes[0].Armor!=null&&app.Sim.NextStep.Route==1,"armor should lead to rescue");
            Click("ContextAction");yield return null;Click("JourneyDepart");yield return null;Fight();app.View.Refresh();yield return null;
            Check(app.Sim.State.Campaign.Battle.Won,"rescue victory");Click("ClaimExpedition");yield return null;
            Check(GameObject.Find("QuestGoal"),"rescue loot did not lead to town story");Click("QuestDetailClaim");yield return null;
            Check(app.Sim.State.Campaign.TownLevel==1,"rescue did not restore town");
            Capture("06-town-restored");yield return null;Click("ContinueJourney");yield return null;
            Check(app.Sim.NextStep.Action==GuideAction.Hire,"economy development goal missing");
            Click("ContextAction");yield return null;Check(app.Sim.State.Automated[0],"guided worker hire");Click("ContinueJourney");yield return null;
            Check(app.Sim.NextStep.Route==2,"guardian goal routed to wrong expedition");
            Click("ContextAction");yield return null;Click("JourneyDepart");yield return null;Fight();app.View.Refresh();yield return null;
            Check(app.Sim.State.Campaign.Battle.Won,"guardian victory");Click("ClaimExpedition");yield return null;Click("QuestDetailClaim");yield return null;
            Check(app.Sim.UnlockedRegion==1&&app.Sim.State.Campaign.TownLevel==1,"first chapter did not unlock next content");
            Capture("07-new-chapter");yield return null;Click("ContinueJourney");yield return null;
            Click("GuideAction");yield return null;Click("JourneyOrders");yield return null;
            Check(GameObject.Find("GuildOffer0Reward"),"no explicit guild order terms");Capture("08-order-board");yield return null;
            Click("AcceptGuildOrder0");yield return null;
            Check(app.Sim.ActiveCommission!=null&&app.Sim.State.TrackCommission,"order did not become active project");
            int orderRecipe=app.Sim.ActiveCommission.RecipeId;var offer=app.Sim.ActiveCommission;
            production=CraftGuided();while(production.MoveNext())yield return production.Current;
            Check(app.Sim.CommissionProgress==offer.Quantity,"crafted output not ready for order");
            Capture("09-order-ready");yield return null;double before=app.Sim.State.Gold;
            Click("CraftResultContinue");yield return null;
            Check(app.Sim.ActiveCommission==null&&app.Sim.State.ContractsCompleted==1,"order not completed");
            Check(app.Sim.State.Gold>=before+offer.Gold&&!app.Sim.TryDeliverCommission(),"order paid incorrectly or twice");
            Capture("10-order-delivered");yield return null;Click("ContinueJourney");yield return null;
            // Free play and journey restoration are separate deliberate actions.
            app.View.FocusStation(Station.Market);app.View.Refresh();
            Check(GameObject.Find("UpgradeSelected"),"free play station details unavailable");
            Check(app.Sim.SelectRecipe(1),"manual recipe fixture");
            app.View.ShowSettings();yield return null;Click("TextSize1");yield return null;Click("PageBack");yield return null;
            Check(GameObject.Find("UpgradeSelected"),"text preference change lost free play context");
            Check(app.Sim.State.SelectedRecipe==1,"text preference change replaced the player's chosen recipe");
            Click("GuideAction");yield return null;Click("JourneyContinue");yield return null;
            Check(!GameObject.Find("UpgradeSelected"),"resume did not restore focused journey");
            // Lose deliberately, then prove that the next screen explains recovery.
            Check(app.Sim.StartExpedition(1,0),"retry fixture");app.Sim.State.Campaign.Battle.HeroHP=1;app.Sim.Advance(20);
            app.View.ShowBattle();yield return null;Click("ClaimExpedition");yield return null;
            Check(app.Sim.State.Campaign.RetryLevel>0&&GameObject.Find("JourneyConsequence"),"defeat has no recovery plan");
            Capture("11-recovery-plan");yield return null;Click("ContinueJourney");yield return null;
            app.View.ShowSettings();yield return null;Click("TextSize2");yield return null;
            app.View.ShowJourney();yield return null;Capture("12-journey-large-text");yield return null;
            Click("JourneyOrders");yield return null;Capture("13-orders-large-text");yield return null;
            app.View.Root.sizeDelta=new Vector2(1080,1280);app.View.ShowJourney();yield return null;Capture("14-journey-compact");yield return null;
            Click("JourneyMastery");yield return null;Check(GameObject.Find("RecipeCollectionTitle"),"masterwork collection missing");
            Capture("15-mastery-compact");yield return null;
            app.View.ShowGuildOrders();yield return null;Click("AcceptGuildOrder2");yield return null;
            app.Sim.State.Levels[2]=25;app.Sim.State.Automated[2]=true;app.Sim.State.Ingots=10;
            app.Sim.State.Campaign.Materials[0]=10;app.Sim.State.TrackCommission=false;app.Sim.Advance(.01);app.Sim.State.TrackCommission=true;app.View.Refresh();yield return null;
            Check(app.Sim.IsRunning(Station.Forge)&&!app.Sim.State.ManualForge,"automatic job fixture");
            Check(GameObject.Find("Strike").GetComponentInChildren<Text>().text=="Temper by hand","automatic job has no clear quality takeover");
            Click("Strike");yield return null;
            Check(app.Sim.State.ManualForge&&app.Sim.GetDuration(Station.Forge)>=6,"manual takeover has no playable timing window");
            Capture("16-manual-tempering");yield return null;
            var accepted=app.Sim.ActiveCommission;app.Save();
            var restored=new WorkshopSimulation(WorkshopSave.Load(app.SavePath,out bool recovered));
            Check(restored.ActiveCommission!=null&&restored.ActiveCommission.RecipeId==accepted.RecipeId&&restored.State.Campaign.JourneyMilestones[0],"progression save reload");
            Check(restored.State.Campaign.Heroes[0].Armor!=null&&restored.State.TextSize==2,"existing progress lost");
            var screenshot=new Texture2D(2,2);float deadline=Time.realtimeSinceStartup+8;
            while(!File.Exists(Path.Combine(output,"01-first-goal.png"))&&Time.realtimeSinceStartup<deadline)yield return new WaitForSecondsRealtime(.1f);
            Check(screenshot.LoadImage(File.ReadAllBytes(Path.Combine(output,"01-first-goal.png"))),"screenshot missing");
            int light=0;var pixels=screenshot.GetPixels32();for(int i=0;i<pixels.Length;i+=97)light=Math.Max(light,Math.Max(pixels[i].r,Math.Max(pixels[i].g,pixels[i].b)));
            Check(light>30,"game did not render");Destroy(screenshot);
        }
        IEnumerator CraftGuided()
        {
            int actions=0;
            while(actions++<100)
            {
                var advice=app.Sim.NextStep;
                Check(advice.Action==GuideAction.Mine||advice.Action==GuideAction.Smelt||advice.Action==GuideAction.Forge,"project left production unexpectedly: "+advice.Title);
                if(advice.Action==GuideAction.Forge)
                {
                    Click("ContextAction");yield return null;Check(app.Sim.IsRunning(Station.Forge),"guided forge did not start");
                    Click("Strike");
                    for(int i=0;i<3;i++){app.Sim.Advance(.21);app.Sim.TryStrike(1);}
                    app.Sim.Advance(app.Sim.GetDuration(Station.Forge)+.01);app.View.Tick(0);app.View.Refresh();yield return null;
                    Check(GameObject.Find("CraftResultQuality"),"crafted result did not appear");yield break;
                }
                var station=advice.Station;
                if(!app.Sim.IsRunning(station))Click("ContextAction");
                app.Sim.Advance(app.Sim.GetDuration(station)+.01);app.View.Refresh();yield return null;
            }
            throw new Exception("production workflow stalled");
        }
        void Fight()
        {
            int ticks=0;
            while(app.Sim.State.Campaign.Battle.Active&&ticks++<12000)
            {
                var b=app.Sim.State.Campaign.Battle;app.Sim.UseBattleSkill(0);
                if(b.Elapsed%6>=4.5)app.Sim.UseBattleSkill(1);
                if(b.HeroHP<b.HeroMaxHP*.5)app.Sim.UseBattleSkill(2);
                app.Sim.Advance(.1);
            }
        }
        void Capture(string name)
        {
            app.View.Refresh();app.View.Tick(0);Canvas.ForceUpdateCanvases();
            var page=app.View.Root.Find("Page");var nav=app.View.Root.Find("Navigation");var layout=new List<string>();
            foreach(var label in app.View.Root.GetComponentsInChildren<Text>())
            {
                if(page&&!label.transform.IsChildOf(page)&&!label.transform.IsChildOf(nav)||string.IsNullOrEmpty(label.text))continue;
                layout.Add(label.name+"\t"+label.rectTransform.rect.width+"\t"+label.rectTransform.rect.height+"\t"+label.text.Replace("\n"," / "));
                Check(label.fontSize>=42,"small text: "+label.name);
                Check(label.preferredHeight<=label.rectTransform.rect.height+3,"clipped text: "+label.name+" needs "+label.preferredHeight+" has "+label.rectTransform.rect.height);
            }
            File.WriteAllLines(Path.Combine(output,name+".layout.tsv"),layout);
            ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?Path.Combine("progression-qa",name+".png"):Path.Combine(output,name+".png"));
        }
        void Click(string name)
        {
            var control=GameObject.Find(name);Check(control,"Missing control: "+name);var button=control.GetComponent<Button>();
            Check(button&&button.IsInteractable(),"Unavailable control: "+name);Canvas.ForceUpdateCanvases();
            var rect=button.GetComponent<RectTransform>();var scroll=button.GetComponentInParent<ScrollRect>();
            if(scroll&&scroll.enabled&&scroll.content&&rect.IsChildOf(scroll.content))
            {
                float range=Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height);
                float center=scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
                scroll.StopMovement();scroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(scroll.content.anchoredPosition.y+scroll.viewport.rect.center.y-center,0,range));Canvas.ForceUpdateCanvases();
            }
            var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left,pointerId=-1};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==control,"Obscured control: "+name);
            ExecuteEvents.Execute(control,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(control,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(control,data,ExecuteEvents.pointerClickHandler);
        }
        void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
        void Finish(bool passed,string error)
        {
            File.WriteAllText(Path.Combine(output,"progression-result.json"),"{\"passed\":"+(passed?"true":"false")+",\"checks\":"+checks+",\"error\":\""+error.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"}");
            Debug.Log((passed?"PROGRESSION_PASS":"PROGRESSION_FAIL")+" "+checks+" "+error);Application.Quit(passed?0:1);
        }
    }
}
#endif
