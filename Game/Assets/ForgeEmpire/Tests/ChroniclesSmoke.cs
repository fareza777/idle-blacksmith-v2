#if UNITY_EDITOR || DEVELOPMENT_BUILD || FORGE_QA
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
    public sealed class ChroniclesSmoke : MonoBehaviour
    {
        WorkshopApp app;string output;int checks;
        IEnumerator Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--chronicles-smoke")<0)yield break;
            output=Application.isMobilePlatform?Path.Combine(Application.persistentDataPath,"chronicles-qa"):Path.GetFullPath(Path.Combine(Application.dataPath,"../../../QA/chronicles-20261025"));Directory.CreateDirectory(output);
            var routine=Run();while(true)
            {
                bool more=false;object current=null;Exception failure=null;
                try{more=routine.MoveNext();if(more)current=routine.Current;}catch(Exception ex){failure=ex;}
                if(failure!=null){Finish(false,failure.ToString());yield break;}if(!more)break;yield return current;
            }
            Finish(true,"");
        }
        static CraftedItem Gear(int id){var r=GameContent.Recipes[id];return new CraftedItem{RecipeId=id,Name=r.Name,Quality=3,Value=r.Value*7/4,Protected=true};}
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);app=FindFirstObjectByType<WorkshopApp>();Check(app&&app.Sim!=null,"startup");app.enabled=false;app.View.CraftResultsEnabled=false;
            Check(app.SavePath==Path.Combine(output,"isolated-workshop.json"),"normal save must be isolated before loading");Click("Begin");yield return null;Capture("01-workshop");yield return null;
            var sim=app.Sim;var state=sim.State;state.Ingots=10;state.Gold=1000;
            app.Work(Station.Forge);yield return null;Click("BeginMasterwork0");yield return null;
            Click("PageBack");yield return null;app.View.Refresh();Click("ContextAction");yield return null;Check(GameObject.Find("ArtisanWorkbench"),"continue masterwork lost its artisan page");
            for(int i=0;i<4;i++)
            {
                sim.Advance(.21);typeof(WorkshopView).GetField("sceneTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(app.View,.5f/.72f);Click("ArtisanStrike");yield return null;
            }
            Check(state.Artisan.Project.Stage==ArtisanStage.Quench,"four stage craft flow");Capture("02-artisan-quench");yield return null;
            Click("ArtisanQuench0");yield return null;Click("ArtisanRune1");yield return null;Click("ArtisanFinish");yield return null;
            Check(state.TotalCrafted==1&&state.Inventory[0].Masterwork&&state.Inventory[0].Quality==3,"authored creation");Capture("03-crafted-build");yield return null;
            Click("CraftResultEquip");yield return null;Check(state.Campaign.Heroes[0].Weapon!=null,"equip creation");
            state.Gold=10000;Check(sim.TryRecruit(1)&&sim.TryRecruit(3),"recruit distinct roles");
            state.Campaign.Heroes[0].Armor=Gear(2);state.Campaign.Heroes[1].Weapon=Gear(24);state.Campaign.Heroes[1].Armor=Gear(31);state.Campaign.Heroes[3].Weapon=Gear(26);state.Campaign.Heroes[3].Armor=Gear(32);
            app.View.ShowHeroes();yield return null;Check(GameObject.Find("NavHeroes").transform.Find("TabLabel").GetComponent<Text>().color==ForgeUI.Gold,"party navigation context");Click("PartyAssign1");yield return null;Click("AssignHero1");yield return null;Click("PartyAssign2");yield return null;Click("AssignHero3");yield return null;
            Check(state.Saga.Party[1]==1&&state.Saga.Party[2]==3,"three actual assigned companions");Capture("04-party-loadouts");yield return null;
            app.View.ShowWorld();yield return null;Capture("05-six-region-atlas");yield return null;Click("AtlasRegion0");yield return null;Click("ChronicleRoute0");yield return null;Click("SagaDepart");yield return null;
            Check(sim.SagaInProgress&&state.Saga.Run.Phase==SagaPhase.Choice,"departure waits for a path");Click("SagaPath0");yield return null;Check(GameObject.Find("PageContent").GetComponent<RectTransform>().anchoredPosition.y<1,"new encounter resets scroll");Capture("06-party-combat");yield return null;
            Click("SagaSkill0");yield return null;Click("SagaSkill1");yield return null;
            Check(!sim.SelectHero(1)&&!sim.SetParty(2,2),"party locked during run");
            while(state.Saga.Run.Phase==SagaPhase.Combat){sim.Advance(.1);app.View.Tick(.1f);app.View.Refresh();yield return null;}
            app.View.ShowSaga();yield return null;Click("SagaPath1");yield return null;Capture("07-camp-choice");yield return null;Click("SagaPath0");yield return null;Click("SagaPath0");yield return null;
            for(int ticks=0;ticks<2000&&state.Saga.Run.Phase==SagaPhase.Combat;ticks++){for(int slot=0;slot<3;slot++)sim.UsePartySkill(slot);sim.Advance(.1);app.View.Tick(.1f);app.View.Refresh();yield return null;}
            Check(state.Saga.Run.Won,"actual three-room victory");app.View.ShowSaga();yield return null;Capture("08-expedition-result");yield return null;Click("SagaCollect");yield return null;
            Check(!sim.SagaInProgress&&state.Campaign.RegionWins[0]==1,"one collected result");
            Check(sim.ClaimQuest(0)&&sim.ClaimQuest(1),"real craft and expedition objectives");
            // Remaining captures are explicitly render fixtures, not balance or earned-unlock evidence.
            for(int q=0;q<GameContent.QuestCount;q++)state.Campaign.QuestClaims[q]=true;state.Campaign.Chapter=5;state.Campaign.TownLevel=6;
            for(int hero=0;hero<6;hero++){state.Campaign.Heroes[hero].Recruited=true;state.Campaign.Heroes[hero].Level=20;state.Campaign.Heroes[hero].Weapon=Gear(16);state.Campaign.Heroes[hero].Armor=Gear(18);}
            for(int region=0;region<6;region++)state.Campaign.Materials[region]=100;
            app.View.ShowWorld();yield return null;Click("AtlasTown");yield return null;Click("RestoreProject0");yield return null;
            Check(state.World.Projects[0]==1,"restoration changes state");Click("TalkNpc0Topic0");yield return null;Capture("09b-npc-conversation");yield return null;Click("NpcChoice0");yield return null;Check(state.World.Trust[0]==2,"NPC choice effect");Capture("09-restoration-and-people");yield return null;
            app.View.ShowWorld();yield return null;Click("AtlasMastery");yield return null;Capture("10-twelve-legends");yield return null;
            for(int hero=0;hero<6;hero++)
            {
                app.View.ShowHeroes();yield return null;Click("Companion"+hero);yield return null;
                var body=GameObject.Find("CompanionActorBody").GetComponent<RawImage>();Check(body.texture==HandDrawnArt.Texture("hero-"+new[]{"rowan","lyra","kael","mira","toren","edda"}[hero]),"unique hero art "+hero);
                Capture("11-companion-"+hero);yield return null;
            }
            app.View.ShowRecipes();yield return null;
            for(int group=0;group<6;group++)
            {
                int region=(5+group)%6;
                foreach(var recipe in GameContent.Recipes)if(recipe.Region==region)
                {var icon=GameObject.Find("CatalogIcon"+recipe.Id);Check(icon,"recipe visible "+recipe.Id);CheckEquipment(icon.GetComponent<RawImage>(),recipe.Id);}
                if(group==0)Capture("12-dawnforge-recipes");Click("CatalogNext");yield return null;
            }
            for(int region=0;region<6;region++)
            {
                for(int species=0;species<8;species++)
                {
                    state.Campaign.ExpeditionsWon=species;Check(sim.StartSaga(region,0),"creature fixture start");sim.ChooseSagaPath(0);app.View.ShowSaga();yield return null;
                    var image=GameObject.Find("SagaEnemy").GetComponent<RawImage>();CheckFrame(image,"enemy-"+region,species*4);
                    if(species==0){Capture("13-region-"+region);yield return null;}
                    state.Saga.Run=null;state.Campaign.Battle=new ExpeditionState();
                }
                for(int variant=0;variant<2;variant++)
                {
                    int boss=region*2+variant;sim.StartSaga(region,variant==0?2:3);state.Saga.Run.Room=4;state.Saga.Run.ClearedRooms=4;sim.ChooseSagaPath(0);app.View.ShowSaga();yield return null;
                    CheckFrame(GameObject.Find("SagaEnemy").GetComponent<RawImage>(),"boss-"+(boss/4),(boss%4)*4);Capture("14-guardian-"+boss);yield return null;
                    state.ReducedMotion=true;state.Saga.Run.EnemyHP-=1;app.View.Tick(.01f);var actor=GameObject.Find("SagaActor0").GetComponent<RectTransform>();Check(Mathf.Abs(actor.anchoredPosition.x-115)<.001f,"reduced motion displacement");state.ReducedMotion=false;
                    state.Saga.Run=null;state.Campaign.Battle=new ExpeditionState();
                }
            }
            app.View.ShowSettings();yield return null;Click("TextSize2");yield return null;app.View.Root.sizeDelta=new Vector2(1080,1280);
            app.View.ShowHeroes();yield return null;Capture("15-compact-party-large-text");yield return null;
            app.View.ShowRecipes();yield return null;Capture("16-compact-catalog-large-text");yield return null;
            app.View.ShowWorld();yield return null;Capture("17-compact-atlas-large-text");yield return null;
            Check(sim.SetParty(2,2)&&sim.StartSaga(5,2),"large-text runebreak fixture");sim.ChooseSagaPath(0);app.View.ShowSaga();yield return null;Click("SagaSkill2");yield return null;
            Check(state.Saga.Run.Units[2].Cooldown>0,"runebreak cooldown fixture");Capture("18-compact-combat-large-text");yield return null;
            state.Saga.Run=null;state.Campaign.Battle=new ExpeditionState();
            foreach(string prefix in new[]{"region-","ambience-","boss-"})for(int r=0;r<6;r++)Check(Resources.Load<AudioClip>("Audio/Chronicles/"+prefix+r),"regional audio");
            app.Save();
        }
        void CheckEquipment(RawImage image,int id){CheckFrame(image,id<20?"equipment":"gear-"+((id-20)/20),id<20?id:(id-20)%20);}
        void CheckFrame(RawImage image,string sheet,int frame)
        {
            Check(image.texture==HandDrawnArt.Texture(sheet),"wrong sheet "+sheet);var layout=JsonUtility.FromJson<HandDrawnArt.Layout>(Resources.Load<TextAsset>("Art/HandDrawn/"+sheet+"-layout").text);var f=layout.frames[frame];var uv=image.uvRect;
            Check(Mathf.Abs(uv.x-f.x)+Mathf.Abs(uv.y-f.y)+Mathf.Abs(uv.width-f.w)+Mathf.Abs(uv.height-f.h)<.0001f,"wrong crop "+sheet+" / "+frame);
            Check(Mathf.Abs(image.rectTransform.rect.width/image.rectTransform.rect.height-f.w*layout.width/(f.h*layout.height))<.02f,"stretched sprite "+sheet);
        }
        void Capture(string name)
        {
            Debug.Log("CHRONICLES_CAPTURE "+name);
            app.View.Refresh();app.View.Tick(0);Canvas.ForceUpdateCanvases();var page=app.View.Root.Find("Page");var nav=app.View.Root.Find("Navigation");
            foreach(var label in app.View.Root.GetComponentsInChildren<Text>())
            {
                if(page&&!label.transform.IsChildOf(page)&&!label.transform.IsChildOf(nav)||string.IsNullOrEmpty(label.text))continue;
                Check(label.fontSize>=42,"small text "+label.name);Check(label.preferredHeight<=label.rectTransform.rect.height+3,"clipped "+label.transform.parent.name+"/"+label.name+" needs "+label.preferredHeight+" has "+label.rectTransform.rect.height+" text="+label.text);
                if(label.transform.parent.name.StartsWith("CatalogFilter")||label.transform.parent.name.StartsWith("SagaSkill"))
                {
                    var measure=label.GetGenerationSettings(Vector2.zero);measure.horizontalOverflow=HorizontalWrapMode.Overflow;measure.verticalOverflow=VerticalWrapMode.Overflow;
                    foreach(string word in label.text.Split(new[]{' ','\n'},StringSplitOptions.RemoveEmptyEntries))
                        Check(new TextGenerator().GetPreferredWidth(word,measure)/label.pixelsPerUnit<=label.rectTransform.rect.width+1,"split button word "+label.transform.parent.name+" / "+word);
                }
            }
            ScreenCapture.CaptureScreenshot(Application.isMobilePlatform?Path.Combine("chronicles-qa",name+".png"):Path.Combine(output,name+".png"));
        }
        void Click(string name)
        {
            var control=GameObject.Find(name);Check(control,"missing button "+name);var button=control.GetComponent<Button>();Check(button&&button.IsInteractable(),"unavailable "+name);Canvas.ForceUpdateCanvases();
            var rect=button.GetComponent<RectTransform>();var scroll=button.GetComponentInParent<ScrollRect>();
            if(scroll&&scroll.enabled&&scroll.content&&rect.IsChildOf(scroll.content))
            {float range=Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height),center=scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;scroll.StopMovement();scroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(scroll.content.anchoredPosition.y+scroll.viewport.rect.center.y-center,0,range));Canvas.ForceUpdateCanvases();}
            var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left,pointerId=-1};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==control,"obscured "+name);ExecuteEvents.Execute(control,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(control,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(control,data,ExecuteEvents.pointerClickHandler);
        }
        void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
        void Finish(bool passed,string error)
        {
            File.WriteAllText(Path.Combine(output,"chronicles-result.json"),"{\"passed\":"+(passed?"true":"false")+",\"checks\":"+checks+",\"error\":\""+error.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n")+"\"}");
            Debug.Log((passed?"CHRONICLES_PASS":"CHRONICLES_FAIL")+" "+checks+" "+error);Application.Quit(passed?0:1);
        }
    }
}
#endif
