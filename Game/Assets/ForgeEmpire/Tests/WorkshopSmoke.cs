#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgeEmpire.Tests
{
    public sealed partial class WorkshopSmoke : MonoBehaviour
    {
        WorkshopApp app;
        string output;
        int checks;
        readonly List<RaycastResult> hits=new List<RaycastResult>();

        IEnumerator Start()
        {
            var run=Run();
            while(true) {
                bool running=false;object current=null;Exception failure=null;
                try {running=run.MoveNext();if(running)current=run.Current;}
                catch(Exception ex) {failure=ex;}
                if(failure!=null){Debug.LogException(failure);Fail(failure.Message);yield break;}
                if(!running)yield break;
                yield return current;
            }
        }

        IEnumerator Run()
        {
            string[] args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"--smoke")<0&&Array.IndexOf(args,"--capture")<0)yield break;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA"));
            Directory.CreateDirectory(output);
            yield return new WaitForSecondsRealtime(2);
            app=FindFirstObjectByType<WorkshopApp>();
            Check(app&&app.Sim!=null,"Application did not start");
            if(Array.IndexOf(args,"--smoke")>=0) {
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(WorkshopApp).GetField("savePath",flags).SetValue(app,Path.Combine(output,"native-playthrough.json"));
                typeof(WorkshopApp).GetField("<Sim>k__BackingField",flags).SetValue(app,new WorkshopSimulation(new WorkshopState{TutorialStep=1}));
                foreach(string counter in new[]{"lastMined","lastCrafted","lastSold"}) {
                    typeof(WorkshopApp).GetField(counter,flags).SetValue(app,0);
                    typeof(WorkshopView).GetField(counter,flags).SetValue(app.View,0);
                }
                typeof(WorkshopView).GetField("lastTier",flags).SetValue(app.View,0);
                foreach(string field in new[]{"previousRunning","previousProgress"}) {
                    var array=(Array)typeof(WorkshopView).GetField(field,flags).GetValue(app.View);Array.Clear(array,0,array.Length);
                }
                app.View.CloseModal();app.View.Refresh();app.Save();
            }
            if(GameObject.Find("Begin"))Click("Begin");
            if(GameObject.Find("CollectOffline"))Click("CollectOffline");
            yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"workshop-native.png"));
            if(Array.IndexOf(args,"--smoke")<0)yield break;
            yield return new WaitForSecondsRealtime(.3f);
            var world=GameObject.Find("Diorama").GetComponent<RectTransform>();
            Check(Mathf.Abs(app.View.Root.rect.height*app.View.Root.localScale.y-Screen.safeArea.height)<2,"Mobile layout leaves unused vertical safe area");
            Check(world.rect.height/app.View.Root.rect.height>=.65f,"Workshop must be the primary play surface, not a small dashboard image");
            var inventoryCheck=CheckInventoryRouting();
            while(inventoryCheck.MoveNext())yield return inventoryCheck.Current;
            if(!Application.isMobilePlatform) {
                double beforeFocus=app.Sim.State.SimulationTime;
                app.SendMessage("OnApplicationFocus",false);
                System.Threading.Thread.Sleep(350);
                yield return null;
                Check(app.Sim.State.SimulationTime-beforeFocus>=.3,"Desktop idle production stopped when another window gained focus");
                app.SendMessage("OnApplicationFocus",true);
            }
            double beforeStall=app.Sim.State.SimulationTime;
            System.Threading.Thread.Sleep(1100);
            yield return null;
            Check(app.Sim.State.SimulationTime-beforeStall>=1.0,"Foreground stall discarded simulation time (expected at least 1s)");
            double pauseClock=Time.realtimeSinceStartupAsDouble, pauseSimulation=app.Sim.State.SimulationTime;
            app.SendMessage("OnApplicationPause",true);
            long pausedSave=app.Sim.State.LastSavedUtc;
            System.Threading.Thread.Sleep(350);
            app.Save();
            Check(app.Sim.State.LastSavedUtc==pausedSave,"Saving while suspended erased pending offline time");
            app.SendMessage("OnApplicationPause",false);
            yield return null;
            double elapsed=Time.realtimeSinceStartupAsDouble-pauseClock;
            Check(Math.Abs(app.Sim.State.SimulationTime-pauseSimulation-elapsed)<.12,"Resume lost or double-counted suspended time");
            int sold=app.Sim.State.TotalSold;
            int crafted=app.Sim.State.TotalCrafted;
            for(int batch=0;batch<2;batch++) {
                Click("WorldMine");
                yield return new WaitForSecondsRealtime((float)app.Sim.GetDuration(Station.Mine)+.2f);
                Check(app.Sim.State.Ore>=2,"Mine produced no ore");
                Click("WorldSmelter");
                yield return new WaitForSecondsRealtime((float)app.Sim.GetDuration(Station.Smelter)+.2f);
            }
            Check(app.Sim.State.Ingots>=2,"Smelter produced no ingots");
            Click("WorldForge");
            yield return new WaitForSecondsRealtime(.25f);
            var viewFlags=BindingFlags.Instance|BindingFlags.NonPublic;
            var smithTimes=(float[])typeof(WorkshopView).GetField("animationTimes",viewFlags).GetValue(app.View);
            smithTimes[2]=.48f;
            typeof(WorkshopView).GetField("sparkUntil",viewFlags).SetValue(app.View,0f);
            app.View.Tick(0);
            var impact=GameObject.Find("sparks").GetComponent<RawImage>();
            Check(impact.uvRect.x==0,"Automatic hammer impact skipped the first authored spark frame");
            Click("Strike");
            yield return new WaitForSecondsRealtime((float)app.Sim.GetDuration(Station.Forge)+.2f);
            Check(app.Sim.State.TotalCrafted>crafted,"Forge produced no sword");
            Click("TabINVENTORY");yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"inventory-native.png"));
            yield return new WaitForSecondsRealtime(.2f);Click("InventorySell");
            Check(!app.View.HasModal&&app.Sim.IsRunning(Station.Market),"Inventory sell did not start the market and close the modal");
            yield return new WaitForSecondsRealtime((float)app.Sim.GetDuration(Station.Market)+.2f);
            Check(app.Sim.State.TotalSold>sold,"Market sold no equipment");
            int level=app.Sim.State.Levels[0];Click("WorldMine");Click("UpgradeSelected");
            Check(app.Sim.State.Levels[0]==level+1,"Upgrade did not apply");
            Click("TabCREW");yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"crew-native.png"));
            yield return new WaitForSecondsRealtime(.2f);Click("CloseModal");
            Click("TabQUESTS");yield return null;Click("TabORDERS");yield return new WaitForSecondsRealtime(.2f);Click("CloseModal");
            Click("Settings");yield return new WaitForSecondsRealtime(.2f);
            var musicControl=GameObject.Find("MusicToggle");
            bool music=app.Sim.State.MusicEnabled;Click("MusicToggle");
            Check(app.Sim.State.MusicEnabled!=music,"Settings toggle failed");
            Check(musicControl&&musicControl.activeInHierarchy&&GameObject.Find("MusicToggle")==musicControl,"Settings toggle rebuilt the touch target");
            yield return null;
            Click("MusicToggle");
            yield return null;
            Click("CloseModal");
            app.Save();
            var restored=WorkshopSave.Load(app.SavePath,out bool recovered);
            Check(restored.TotalSold==app.Sim.State.TotalSold&&restored.Levels[0]==app.Sim.State.Levels[0],"Saved progression differs");
            var campaign=CampaignFlow();while(campaign.MoveNext())yield return campaign.Current;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"workshop-played.png"));
            for(int i=0;i<30;i++)yield return null;
            double[] frameTimes=new double[240];long beforeMemory=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            int collections=GC.CollectionCount(0);
            for(int i=0;i<frameTimes.Length;i++){yield return null;frameTimes[i]=Time.unscaledDeltaTime*1000;}
            Array.Sort(frameTimes);
            File.WriteAllText(Path.Combine(output,"native-performance.json"),"{\"samples\":240,\"medianMs\":"+frameTimes[120].ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"p95Ms\":"+frameTimes[228].ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"allocatedBytes\":"+UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()+",\"memoryGrowthBytes\":"+(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()-beforeMemory)+",\"gen0Collections\":"+(GC.CollectionCount(0)-collections)+"}");
            File.WriteAllText(Path.Combine(output,"native-smoke-result.json"),"{\"passed\":true,\"checks\":"+checks+",\"nativeTouchValidated\":false,\"sold\":"+restored.TotalSold+",\"crafted\":"+restored.TotalCrafted+",\"mineLevel\":"+restored.Levels[0]+"}");
            Debug.Log("WORKSHOP_SMOKE_PASS: production, UGUI raycast/pointer routing, shade, live inventory, same-count replacement, market availability, viewport drag, scroll preservation/clamp, foreground stall, save. External native touch still requires validation.");
            yield return new WaitForSecondsRealtime(1);
            Application.Quit(0);
        }

        IEnumerator CheckInventoryRouting()
        {
            var simField=typeof(WorkshopApp).GetField("<Sim>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
            Check(simField!=null,"QA cannot locate Sim backing field");
            var original=app.Sim;
            var saveField=typeof(WorkshopApp).GetField("savePath",BindingFlags.Instance|BindingFlags.NonPublic);
            string originalPath=app.SavePath, originalSave=File.ReadAllText(app.SavePath);
            bool enabledBefore=app.enabled;
            var sim=new WorkshopSimulation(new WorkshopState());
            // Lifecycle callbacks still run on disabled components, so isolate persistence too.
            try {
                app.enabled=false;
                saveField.SetValue(app,Path.Combine(output,"temporary-workshop.json"));
                simField.SetValue(app,sim);
                app.View.Refresh();
                app.SendMessage("OnApplicationQuit");
                Check(File.ReadAllText(originalPath)==originalSave,"QA lifecycle callback overwrote the real save");
                Check(WorkshopSave.Load(app.SavePath,out bool fixtureRecovered).Gold==40,"QA lifecycle callback did not save the isolated fixture");
                Click("WorldSmelter");
                Check(app.View.SelectedStation==Station.Smelter&&!sim.IsRunning(Station.Smelter),"Starved station did not focus without consuming resources");
                Check(!GameObject.Find("ContextAction").GetComponent<Button>().interactable,"Starved station offered an enabled production action");
                Click("WorldMine");
                Check(app.View.SelectedStation==Station.Mine&&sim.IsRunning(Station.Mine),"World mine tap did not start real production");
                sim.Advance(sim.GetDuration(Station.Mine));app.View.Refresh();
                Click("WorldSmelter");
                Check(sim.IsRunning(Station.Smelter)&&sim.State.Ore==0,"World smelter did not reserve the mined ore");
                sim.Advance(sim.GetDuration(Station.Smelter));app.View.Refresh();
                Click("UpgradeSelected");
                Check(sim.State.Levels[1]==2&&sim.State.Levels[0]==1,"Context upgrade targeted the wrong station");
                Check(GameObject.Find("BuildingUpgrade1")!=null,"Purchased station upgrade did not change the building visually");
                Click("TabINVENTORY");
                yield return null;
                var dialog=GameObject.Find("Dialog").GetComponent<RectTransform>();
                var data=Raycast(Center(dialog.Find("Heading").GetComponent<RectTransform>()));
                Check(data.pointerCurrentRaycast.gameObject==dialog.gameObject,"Dialog body did not occlude modal shade");
                Check(ExecuteEvents.GetEventHandler<IPointerClickHandler>(data.pointerCurrentRaycast.gameObject)==null,"Dialog body routed a click through to the shade");
                DispatchClick(data);
                Check(app.View.HasModal,"Dialog body tap closed the modal");
                // Use the covered tab's actual center, outside the dialog, to test the shade.
                var shade=GameObject.Find("ModalShade").GetComponent<Button>();
                ClickAt(shade,Center(GameObject.Find("TabCREW").GetComponent<RectTransform>()));
                Check(!app.View.HasModal,"Shade did not dismiss the modal or opened the covered tab");
                Click("TabINVENTORY");
                yield return null;
                dialog=GameObject.Find("Dialog").GetComponent<RectTransform>();
                var viewport=dialog.Find("InventoryViewport");
                Check(viewport!=null,"Empty inventory must create its viewport once");
                var scroll=viewport.GetComponent<ScrollRect>();
                var empty=dialog.Find("EmptyStock").gameObject;
                var emptyText=dialog.Find("EmptyStockText").gameObject;
                var capacity=dialog.Find("StockCapacity").GetComponent<Text>();
                var sell=dialog.Find("InventorySell").GetComponent<Button>();
                CheckInventory(sim,scroll,empty,emptyText,capacity,sell);

                CraftSword(sim,0);
                app.View.Refresh();
                CheckInventory(sim,scroll,empty,emptyText,capacity,sell);
                Check(dialog.Find("InventoryViewport")==viewport,"Populating inventory replaced the viewport");
                for(int i=1;i<8;i++)CraftSword(sim,i%3);
                app.View.Refresh();
                CheckInventory(sim,scroll,empty,emptyText,capacity,sell);
                Check(sim.State.Inventory.Count>=8,"Drag fixture has fewer than eight real crafted items");
                Check(ActiveRow(scroll,7).GetComponent<RectTransform>().anchoredPosition.y<=-scroll.viewport.rect.height,"Eighth item was not below the initial viewport");
                DragInventory(scroll);
                float offset=scroll.content.anchoredPosition.y;
                Check(offset>1,"Viewport drag did not move inventory content");
                Check(Mathf.Abs(offset-(scroll.content.rect.height-scroll.viewport.rect.height))<1,"Viewport drag did not reveal the eighth item at the bottom");
                var firstRow=ActiveRow(scroll,0);
                for(int i=0;i<3;i++)app.View.Refresh();
                Check(ActiveRow(scroll,0)==firstRow,"Unchanged inventory rebuilt item rows");
                Check(Mathf.Abs(scroll.content.anchoredPosition.y-offset)<1,"Unchanged refresh lost scroll offset");

                CraftSword(sim,0);
                app.View.Refresh();
                CheckInventory(sim,scroll,empty,emptyText,capacity,sell);
                Check(Mathf.Abs(scroll.content.anchoredPosition.y-offset)<1,"Growing inventory lost scroll offset");
                DragInventory(scroll);
                offset=scroll.content.anchoredPosition.y;
                firstRow=ActiveRow(scroll,0);
                var firstItem=sim.State.Inventory[0];
                int count=sim.State.Inventory.Count;
                // Replace an item without changing count while the market stays busy.
                StartSword(sim,3);
                sim.Advance((1-sim.GetProgress(Station.Forge))*sim.GetDuration(Station.Forge)-.1);
                Check(sim.TryStart(Station.Market),"Could not start replacement sale");
                sim.Advance(.2);
                Check(sim.State.Inventory.Count==count&&!ReferenceEquals(firstItem,sim.State.Inventory[0]),"Same-count replacement fixture did not replace an item");
                Check(sim.IsRunning(Station.Market),"Replacement fixture market is not busy");
                app.View.Refresh();
                CheckInventory(sim,scroll,empty,emptyText,capacity,sell);
                Check(ActiveRow(scroll,0)!=firstRow,"Same-count replacement did not rebuild ordered rows");
                Check(ActiveRow(scroll,count-1).Find("ItemQuality").GetComponent<Text>().text=="MASTERWORK","Same-count replacement kept stale item details");
                Check(Mathf.Abs(scroll.content.anchoredPosition.y-offset)<1,"Same-count replacement lost scroll offset");
                // Never dispatch an enabled sell here: its application callback saves.
                Check(!sell.IsInteractable(),"Sell remained enabled while market is busy");
                data=Raycast(Center(sell.GetComponent<RectTransform>()));
                Check(ExecuteEvents.GetEventHandler<IPointerClickHandler>(data.pointerCurrentRaycast.gameObject)==sell.gameObject,"Disabled sell was occluded");
                DispatchClick(data);
                Check(app.View.HasModal&&sim.State.Inventory.Count==count,"Disabled sell accepted a click");
                firstRow=ActiveRow(scroll,0);
                sim.Advance(sim.GetDuration(Station.Market));
                app.View.Refresh();
                CheckInventory(sim,scroll,empty,emptyText,capacity,sell);
                Check(ActiveRow(scroll,0)==firstRow,"Market completion rebuilt unchanged rows");

                while(sim.State.Inventory.Count>0) {
                    offset=scroll.content.anchoredPosition.y;
                    Check(sim.TryStart(Station.Market),"Could not sell temporary stock");
                    app.View.Refresh();
                    CheckInventory(sim,scroll,empty,emptyText,capacity,sell);
                    float expected=Mathf.Clamp(offset,0,Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height));
                    Check(Mathf.Abs(scroll.content.anchoredPosition.y-expected)<1,"Shrinking inventory failed to preserve/clamp scroll offset");
                    sim.Advance(sim.GetDuration(Station.Market));
                    app.View.Refresh();
                }
                Check(dialog.Find("InventoryViewport")==viewport&&dialog.Find("EmptyStock").gameObject==empty,"Inventory recreated persistent viewport/empty UI");
                Click("CloseModal");
                app.View.Refresh();
                Check(!app.View.HasModal,"Closed inventory refresh recreated a modal");
                Click("TabCREW");yield return null;
                var nextHire=GameObject.Find("HireSmelter");
                Click("HireMine");
                Check(nextHire&&nextHire.activeInHierarchy&&GameObject.Find("HireSmelter")==nextHire,"Hiring rebuilt the crew dialog and invalidated the next touch target");
                Click("HireSmelter");
                Check(sim.State.Automated[0]&&sim.State.Automated[1],"Consecutive crew touches did not hire both workers");
                double crewGold=sim.State.Gold;Click("HireMine");
                Check(sim.State.AutomationPaused[0]&&sim.State.Automated[0],"Owned worker cannot pause without losing ownership");
                Click("HireMine");Check(!sim.State.AutomationPaused[0]&&sim.State.Gold==crewGold,"Resuming a worker charged a second hiring cost");
                Click("CloseModal");
            }
            finally {
                simField.SetValue(app,original);
                saveField.SetValue(app,originalPath);
                app.enabled=enabledBefore;
                app.View.CloseModal();
                app.View.Refresh();
            }
            Check(ReferenceEquals(app.Sim,original),"QA did not restore the original simulation");
        }

        void StartSword(WorkshopSimulation sim,int quality)
        {
            for(int i=0;i<2;i++) {
                Check(sim.TryStart(Station.Mine),"QA mine did not start");
                sim.Advance(sim.GetDuration(Station.Mine));
                Check(sim.TryStart(Station.Smelter),"QA smelter did not start");
                sim.Advance(sim.GetDuration(Station.Smelter));
            }
            Check(sim.TryStart(Station.Forge),"QA forge did not start");
            for(int i=0;i<quality;i++) {
                sim.Advance(.21);
                Check(sim.TryStrike(1),"QA quality strike did not apply");
            }
        }

        void CraftSword(WorkshopSimulation sim,int quality)
        {
            int before=sim.State.TotalCrafted;
            StartSword(sim,quality);
            sim.Advance(sim.GetDuration(Station.Forge));
            Check(sim.State.TotalCrafted==before+1,"QA forge did not produce a real item");
        }

        Transform ActiveRow(ScrollRect scroll,int index)
        {
            // Destroy is deferred until frame end; ignore already retired rows.
            for(int i=0;i<scroll.content.childCount;i++) {
                var child=scroll.content.GetChild(i);
                if(child.gameObject.activeSelf&&child.name=="Item"+index)return child;
            }
            throw new Exception("Missing inventory row: "+index);
        }

        void CheckInventory(WorkshopSimulation sim,ScrollRect scroll,GameObject empty,GameObject emptyText,Text capacity,Button sell)
        {
            int count=sim.State.Inventory.Count;
            Check(capacity.text==count+" / 120 inventory slots","Inventory slot count is stale");
            Check(empty.activeSelf==(count==0)&&emptyText.activeSelf==(count==0),"Inventory empty-state visibility is stale");
            Check(sell.IsInteractable()==(count>0&&!sim.IsRunning(Station.Market)),"Inventory sell availability is stale");
            int rows=0;
            for(int i=0;i<scroll.content.childCount;i++)if(scroll.content.GetChild(i).gameObject.activeSelf)rows++;
            Check(rows==count,"Inventory row count is stale");
            string[] tiers={"COMMON","FINE","SUPERIOR","MASTERWORK"};
            for(int i=0;i<count;i++) {
                var row=ActiveRow(scroll,i);var item=sim.State.Inventory[i];
                Check(row.Find("ItemName").GetComponent<Text>().text==item.Name&&row.Find("ItemQuality").GetComponent<Text>().text==tiers[item.Quality]&&row.Find("ItemValue").GetComponent<Text>().text==item.Value+" G","Inventory row order/details are stale: "+i);
            }
        }

        void DragInventory(ScrollRect scroll)
        {
            Canvas.ForceUpdateCanvases();
            var viewport=scroll.viewport;
            var data=Raycast(Center(viewport));
            var hit=data.pointerCurrentRaycast.gameObject;
            Check(hit==viewport.gameObject||hit.transform.IsChildOf(viewport),"Scrollable viewport is not the first raycast hit");
            data.pointerDrag=ExecuteEvents.GetEventHandler<IDragHandler>(hit);
            Check(data.pointerDrag==scroll.gameObject&&scroll.isActiveAndEnabled,"Viewport hit does not route to the active ScrollRect");
            ExecuteEvents.Execute(data.pointerDrag,data,ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(data.pointerDrag,data,ExecuteEvents.beginDragHandler);
            data.dragging=true;
            var end=ScreenPoint(viewport,viewport.rect.center+Vector2.up*300);
            data.delta=end-data.position;data.position=end;
            data.pointerCurrentRaycast=Raycast(end).pointerCurrentRaycast;
            var endHit=data.pointerCurrentRaycast.gameObject;
            Check(endHit==viewport.gameObject||endHit.transform.IsChildOf(viewport),"Drag endpoint is occluded");
            ExecuteEvents.Execute(data.pointerDrag,data,ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(data.pointerDrag,data,ExecuteEvents.endDragHandler);
            data.dragging=false;
            scroll.StopMovement();
        }

        Vector2 ScreenPoint(RectTransform rect,Vector2 local)
        {
            var canvas=rect.GetComponentInParent<Canvas>();
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            return RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(local));
        }

        Vector2 Center(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            return ScreenPoint(rect,rect.rect.center);
        }

        PointerEventData Raycast(Vector2 position)
        {
            Check(EventSystem.current!=null,"No EventSystem available for UI routing");
            Canvas.ForceUpdateCanvases();
            var data=new PointerEventData(EventSystem.current) {pointerId=-1,button=PointerEventData.InputButton.Left,position=position,pressPosition=position,eligibleForClick=true,clickCount=1};
            hits.Clear();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0,"No UI raycast hit at "+position);
            data.pointerCurrentRaycast=hits[0];data.pointerPressRaycast=hits[0];
            return data;
        }

        void Click(string name)
        {
            var go=GameObject.Find(name);
            Check(go!=null,"Missing UI control: "+name);
            var button=go.GetComponent<Button>();
            Check(button!=null,"Missing Button: "+name);
            ClickAt(button,Center(button.GetComponent<RectTransform>()));
        }

        void ClickAt(Button button,Vector2 position)
        {
            Check(button.isActiveAndEnabled&&button.IsInteractable(),"Disabled UI control: "+button.name);
            var data=Raycast(position);
            var target=ExecuteEvents.GetEventHandler<IPointerClickHandler>(data.pointerCurrentRaycast.gameObject);
            Check(target==button.gameObject,"First raycast hit does not resolve to intended Button: "+button.name+" (hit "+data.pointerCurrentRaycast.gameObject.name+")");
            DispatchClick(data);
        }

        void DispatchClick(PointerEventData data)
        {
            var hit=data.pointerCurrentRaycast.gameObject;
            data.pointerPress=ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.pointerDownHandler);
            if(!data.pointerPress)data.pointerPress=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
            data.rawPointerPress=hit;
            ExecuteEvents.Execute(data.pointerPress,data,ExecuteEvents.pointerUpHandler);
            var released=Raycast(data.position);
            if(data.pointerPress==ExecuteEvents.GetEventHandler<IPointerClickHandler>(released.pointerCurrentRaycast.gameObject))
                ExecuteEvents.Execute(data.pointerPress,data,ExecuteEvents.pointerClickHandler);
            data.eligibleForClick=false;data.pointerPress=null;data.rawPointerPress=null;
        }

        void Check(bool condition,string reason)
        {
            if(!condition)throw new Exception(reason);
            checks++;
        }

        void Fail(string reason)
        {
            if(app)Debug.Log("SMOKE_STATE suspended="+typeof(WorkshopApp).GetField("suspended",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(app)+" focused="+Application.isFocused+" state="+JsonUtility.ToJson(app.Sim.State));
            string escaped=reason.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n");
            File.WriteAllText(Path.Combine(output,"native-smoke-result.json"),"{\"passed\":false,\"reason\":\""+escaped+"\"}");
            Debug.LogError("WORKSHOP_SMOKE_FAIL: "+reason);Application.Quit(1);
        }
    }
}
#endif
