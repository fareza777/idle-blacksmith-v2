#if UNITY_EDITOR || DEVELOPMENT_BUILD || FORGE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace ForgeEmpire.Tests
{
    public sealed class IllustratedWorldSmoke : MonoBehaviour
    {
        WorkshopApp app;
        string output;
        int checks;
        long textureBytes;
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--illustrated-smoke")<0)yield break;
            output=Application.isMobilePlatform?Path.Combine(Application.persistentDataPath,"illustrated-qa"):Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/illustrated-20261025"));
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
        static CraftedItem Gear(int id)
        {
            var recipe=GameContent.Recipes[id];
            return new CraftedItem{RecipeId=id,Name=recipe.Name,Quality=3,Value=recipe.Value*7/4,Protected=true};
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);
            app=FindFirstObjectByType<WorkshopApp>();Check(app&&app.Sim!=null,"Game did not start");
            app.enabled=false;app.View.CraftResultsEnabled=false;
            Check(app.SavePath==Path.Combine(output,"isolated-workshop.json"),"Art QA did not isolate its save");
            Click("Begin");yield return null;
            var sim=app.Sim;var state=sim.State;var campaign=state.Campaign;
            app.View.ShowWorld();yield return null;
            Check(GameObject.Find("WorldMap").GetComponent<RawImage>().texture==HandDrawnArt.Texture("worldmap"),"Map is not the new painting");
            for(int i=0;i<5;i++)Check(GameObject.Find("RegionName"+i).GetComponent<Text>().text==GameContent.Regions[i].Name,"Unnamed frontier "+i);
            Check(GameObject.Find("MapObjective"),"Map has no current goal");
            Capture("01-map-first-chapter");yield return null;
            Click("Region4");yield return null;
            Check(GameObject.Find("RegionUnlockRequirement").GetComponent<Text>().text.Contains(GameContent.Quests[11].Title),"Locked preview has wrong unlock");
            Check(!GameObject.Find("StartRoute0")&&!campaign.Battle.Active&&sim.UnlockedRegion==0,"Locked preview bypassed progression");
            Capture("02-locked-frontier-preview");yield return null;
            app.View.CloseModal();

            campaign.Chapter=4;for(int i=0;i<12;i++)campaign.QuestClaims[i]=true;
            state.Gold=10000;state.Ingots=100;for(int i=0;i<5;i++)campaign.Materials[i]=100;
            campaign.Heroes[0].Level=20;campaign.Heroes[0].Weapon=Gear(16);campaign.Heroes[0].Armor=Gear(18);
            for(int i=0;i<20;i++)state.Inventory.Add(Gear(i));
            app.View.ShowRecipes();yield return null;
            for(int i=0;i<20;i++)CheckFrame(GameObject.Find("RecipeIcon"+i).GetComponent<RawImage>(),"equipment",i);
            Capture("03-illustrated-recipes");yield return null;
            Click("Recipe17");yield return null;CheckFrame(GameObject.Find("RecipeDetailIcon").GetComponent<RawImage>(),"equipment",17);
            Capture("04-nightstar-equipment-detail");yield return null;
            app.View.ShowInventory();yield return null;
            for(int i=0;i<20;i++)CheckFrame(GameObject.Find("ItemIcon"+i).GetComponent<RawImage>(),"equipment",i);
            app.View.ShowHeroes();yield return null;
            CheckFrame(GameObject.Find("WeaponIcon").GetComponent<RawImage>(),"equipment",16);
            CheckFrame(GameObject.Find("ArmorIcon").GetComponent<RawImage>(),"equipment",18);
            Capture("05-hero-and-equipment");yield return null;
            Click("NavWorkshop");yield return null;app.View.Refresh();
            for(int i=0;i<3;i++)CheckFrame(GameObject.Find("RackSword"+i).GetComponent<RawImage>(),"equipment",i);
            app.View.FocusStation(Station.Forge);sim.PlanRecipe(19);state.KeepCraftedEquipment=true;
            Check(sim.TryStart(Station.Forge,true),"Could not begin equipment flight fixture");
            app.View.Tick(0);sim.Advance(sim.GetDuration(Station.Forge)+.01);app.View.Tick(.01f);
            bool flight=false;
            foreach(var image in app.View.Root.GetComponentsInChildren<RawImage>())
                if(image.name.StartsWith("MaterialTransit")&&image.texture==HandDrawnArt.Texture("equipment")){CheckFrame(image,"equipment",19);flight=true;}
            Check(flight,"Crafted-item flight still uses old art or is missing");

            string[] landscapeNames={"emberwood","marsh","frostpeak","caldera","starfall"};
            for(int region=0;region<5;region++)
            {
                campaign.Battle=new ExpeditionState();
                Check(sim.StartExpedition(region,2),"Could not start region "+region);
                app.View.ShowBattle();yield return null;
                var b=campaign.Battle;
                Check(GameObject.Find("Battlefield").GetComponent<RawImage>().texture==HandDrawnArt.Texture(landscapeNames[region]),"Wrong regional painting");
                CheckFrame(GameObject.Find("BattleEnemy").GetComponent<RawImage>(),"enemies",region*4);
                Check(GameObject.Find("EnemyThreat").GetComponent<RawImage>()!=null&&GameObject.Find("EnemyGroundShadow"),"Threat box returned or ground shadow missing");
                Capture("06-region-"+region+"-ordinary");yield return null;
                b.EnemyHP-=1;app.View.Tick(.01f);
                CheckFrame(GameObject.Find("BattleEnemy").GetComponent<RawImage>(),"enemies",region*4+2);
                Check(GameObject.Find("BattleHero").GetComponent<RectTransform>().anchoredPosition.x>240,"Hero strike has no contact motion");
                Check(Mathf.Abs(GameObject.Find("BattleEnemy").GetComponent<RectTransform>().anchoredPosition.y+471)<.01f,"Enemy feet leave the ground");
                app.View.Tick(.7f);
                Check(Mathf.Abs(GameObject.Find("BattleHero").GetComponent<RectTransform>().anchoredPosition.x-240)<.01f,"Strike does not settle");
                // Render fixtures exercise live wave switches; core tests separately prove combat outcomes.
                b.Wave=2;b.EnemyHP=b.EnemyMaxHP;app.View.Tick(.01f);
                CheckFrame(GameObject.Find("BattleEnemy").GetComponent<RawImage>(),"guardians",region*4);
                Check(GameObject.Find("EnemyDamage").GetComponent<Text>().text=="","Old wave damage leaked onto the guardian");
                Capture("07-region-"+region+"-guardian");yield return null;
                b.Elapsed=5.8;app.View.Refresh();app.View.Tick(.01f);
                CheckFrame(GameObject.Find("BattleEnemy").GetComponent<RawImage>(),"guardians",region*4+1);
                state.ReducedMotion=true;app.View.Tick(.1f);
                var enemy=GameObject.Find("BattleEnemy").GetComponent<RawImage>();
                Check((enemy.rectTransform.anchoredPosition-new Vector2(790,-471)).sqrMagnitude<.01f,"Reduced motion moves the enemy's feet");
                Check(GameObject.Find("BattleHit").GetComponent<RawImage>().color.a==0,"Reduced motion still flashes");
                state.ReducedMotion=false;b.Active=false;b.Finished=true;b.Won=true;b.EnemyHP=0;app.View.Refresh();app.View.Tick(.01f);
                CheckFrame(enemy,"guardians",region*4+3);
            }
            campaign.Battle=new ExpeditionState();
            app.View.ShowWorld();yield return null;Capture("08-map-final-frontier");yield return null;
            campaign.QuestClaims[12]=campaign.QuestClaims[13]=campaign.QuestClaims[14]=true;app.View.ShowWorld();yield return null;Capture("09-map-valley-restored");yield return null;
            app.View.ShowSettings();yield return null;Click("TextSize2");yield return null;
            app.View.ShowWorld();yield return null;Capture("10-map-large-text");yield return null;
            app.View.Root.sizeDelta=new Vector2(1080,1280);
            app.View.ShowWorld();yield return null;Capture("11-map-compact-large-text");yield return null;
            Click("Region4");yield return null;Capture("12-region-compact-large-text");yield return null;
            app.View.ShowRecipes();yield return null;Click("Recipe19");yield return null;Capture("13-equipment-compact-large-text");yield return null;
            foreach(string name in new[]{"equipment","enemies","guardians","worldmap","emberwood","marsh","frostpeak","caldera","starfall"})textureBytes+=Profiler.GetRuntimeMemorySizeLong(HandDrawnArt.Texture(name));
            app.Save();
        }
        void CheckFrame(RawImage image,string sheet,int index)
        {
            Check(image.texture==HandDrawnArt.Texture(sheet),"Wrong illustrated texture: "+image.name);
            var layout=JsonUtility.FromJson<HandDrawnArt.Layout>(Resources.Load<TextAsset>("Art/HandDrawn/"+sheet+"-layout").text);
            var frame=layout.frames[index];var uv=image.uvRect;
            Check(Mathf.Abs(uv.x-frame.x)+Mathf.Abs(uv.y-frame.y)+Mathf.Abs(uv.width-frame.w)+Mathf.Abs(uv.height-frame.h)<.0001f,"Wrong atlas frame: "+image.name+" / "+index);
            float aspect=frame.w*layout.width/(frame.h*layout.height),actual=image.rectTransform.rect.width/image.rectTransform.rect.height;
            Check(Mathf.Abs(aspect-actual)<.01f,"Stretched illustrated frame: "+image.name);
        }
        void Capture(string name)
        {
            app.View.Refresh();app.View.Tick(0);Canvas.ForceUpdateCanvases();
            var page=app.View.Root.Find("Page");var navigation=app.View.Root.Find("Navigation");
            foreach(var label in app.View.Root.GetComponentsInChildren<Text>())
            {
                if(page&&!label.transform.IsChildOf(page)&&!label.transform.IsChildOf(navigation)||string.IsNullOrEmpty(label.text))continue;
                Check(label.fontSize>=42,"Small text: "+label.name);
                Check(label.preferredHeight<=label.rectTransform.rect.height+3,"Clipped text: "+label.name+" needs "+label.preferredHeight+" has "+label.rectTransform.rect.height);
            }
            ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?Path.Combine("illustrated-qa",name+".png"):Path.Combine(output,name+".png"));
        }
        void Click(string name)
        {
            var control=GameObject.Find(name);Check(control,"Missing control: "+name);var button=control.GetComponent<Button>();
            Check(button&&button.IsInteractable(),"Unavailable control: "+name);Canvas.ForceUpdateCanvases();
            var rect=button.GetComponent<RectTransform>();var scroll=button.GetComponentInParent<ScrollRect>();
            if(scroll&&scroll.enabled&&scroll.content&&rect.IsChildOf(scroll.content))
            {
                float range=Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height),center=scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
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
            File.WriteAllText(Path.Combine(output,"illustrated-result.json"),"{\"passed\":"+(passed?"true":"false")+",\"checks\":"+checks+",\"textureBytes\":"+textureBytes+",\"error\":\""+error.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"}");
            Debug.Log((passed?"ILLUSTRATED_PASS":"ILLUSTRATED_FAIL")+" "+checks+" "+error);Application.Quit(passed?0:1);
        }
    }
}
#endif
