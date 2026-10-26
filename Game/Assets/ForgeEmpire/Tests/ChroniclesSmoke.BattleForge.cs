#if UNITY_EDITOR || DEVELOPMENT_BUILD || FORGE_QA
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire.Tests
{
    public sealed partial class ChroniclesSmoke
    {
        IEnumerator CheckCombatPresentation()
        {
            var sim=app.Sim;var run=sim.State.Saga.Run;
            Check(GameObject.Find("SagaPause")&&GameObject.Find("SagaControls"),"persistent battle commands");
            foreach(var image in GameObject.Find("SagaStage").GetComponentsInChildren<RawImage>())Check(!image.name.EndsWith("Armor",StringComparison.Ordinal),"inventory armor icon remains in battle");
            int tick=run.Tick;double ore=sim.State.Ore;sim.TryStart(Station.Mine);double progress=sim.State.Progress[0];
            Click("SagaPause");Check(run.Paused,"actual pause button");sim.Advance(1.2);app.View.Tick(1.2f);app.View.Refresh();
            Check(run.Tick==tick,"pause advanced combat");Check(sim.State.Ore>ore||sim.State.Progress[0]>progress,"pause stopped the mine");Capture("06a-tactical-pause");yield return null;
            Click("SagaPause");Check(!run.Paused,"actual resume button");
            bool windup=false,impact=false,threat=false;
            for(int i=0;i<70&&run.Phase==SagaPhase.Combat;i++)
            {
                sim.Advance(.1);app.View.Tick(.1f);app.View.Refresh();
                CheckImpactReadability();
                bool captured=false;
                if(!threat&&run.ThreatImpactTick>run.RoomTick){threat=true;Check(!string.IsNullOrEmpty(sim.SagaIntent)&&sim.SagaThreatProgress>=0&&sim.SagaThreatProgress<=1,"announced intent/progress");Capture("06b-enemy-intent");captured=true;}
                foreach(var unit in run.Units)
                {
                    if(!captured&&!windup&&unit.HeroId>=0&&unit.Action=="windup"){windup=true;Capture("06c-hero-approach");captured=true;}
                    if(!captured&&!impact&&unit.HeroId>=0&&unit.Action=="attack"){impact=true;Capture("06d-contact-and-damage");captured=true;}
                }
                yield return null;if(windup&&impact&&threat)break;
            }
            Check(windup&&impact&&threat,"readable anticipation contact and threat all occur");
            Check(app.View.SagaPresentedEventSerial==run.EventSerial,"event presentation cursor missed an impact");
            app.View.ShowSaga();yield return null;
            Check(app.View.SagaPresentedEventSerial==run.EventSerial&&app.View.SagaActiveEffectCount==0,"reopening replays old hit effects");
            Click("SagaDetails");yield return null;Check(run.Paused&&GameObject.Find("SagaJournalHeading"),"journal must pause actual battle");Capture("06e-tactics-journal");yield return null;
            Click("SagaJournalReturn");yield return null;Click("SagaPause");Check(!run.Paused,"return from tactics resumes on explicit play");
        }

        void CheckImpactReadability()
        {
            var stage=GameObject.Find("SagaStage").GetComponent<RectTransform>();var labels=GameObject.Find("SagaImpactFX").GetComponentsInChildren<Text>();
            for(int i=0;i<labels.Length;i++)if(labels[i].color.a>.05f)
            {
                var a=RectTransformUtility.CalculateRelativeRectTransformBounds(stage,labels[i].rectTransform);
                for(int j=i+1;j<labels.Length;j++)if(labels[j].color.a>.05f)
                {
                    var b=RectTransformUtility.CalculateRelativeRectTransformBounds(stage,labels[j].rectTransform);
                    Check(a.max.x<=b.min.x||b.max.x<=a.min.x||a.max.y<=b.min.y||b.max.y<=a.min.y,"consecutive damage and block values must remain separately readable");
                }
            }
        }

        static void GalleryPose(ChronicleActor actor,int pose)
        {
            string action=pose==0?"ready":pose<=2?"windup":pose<=5?"attack":pose==6?"brace":pose==7?"parry":pose==8?"hurt":pose==9?"stagger":pose==10?"cast":pose==11?"heal":pose==12?"victory":pose==13?"defeat":"walk";
            float time=pose==2?.28f:pose==4?.2f:pose==5?.4f:0;
            actor.Pose(action,0,pose>=14,pose>=14?(pose-14)*8:0);actor.Pose(action,time,pose>=14,pose>=14?(pose-14)*8:0);
        }

        IEnumerator CheckWornPoseGallery()
        {
            int[] recipes={2,31,3};
            for(int hero=0;hero<6;hero++)for(int family=0;family<3;family++)
            {
                app.View.ShowHeroes();yield return null;var parent=GameObject.Find("PageContent").GetComponent<RectTransform>();
                foreach(Transform child in parent){child.gameObject.SetActive(false);Destroy(child.gameObject);}yield return null;
                parent.sizeDelta=new Vector2(1020,1490);parent.anchoredPosition=Vector2.zero;
                ForgeUI.Label("WornGalleryTitle",parent,GameContent.Heroes[hero].Name+" · "+ChronicleWardrobe.Families[family],0,0,1020,100,ForgeUI.Subheading,ForgeUI.Gold,FontStyle.Bold);
                for(int pose=0;pose<20;pose++)
                {
                    float x=127+pose%4*255,y=310+pose/4*270;
                    var actor=new ChronicleActor("WornActor"+pose,parent,hero,null,Gear(recipes[family]),new Vector2(x,y),185);GalleryPose(actor,pose);
                    string sheet=ChronicleWardrobe.Sheet(hero,recipes[family]);Check(actor.BodySheet==sheet,"incorrect wardrobe family");CheckFrame(actor.Body,sheet,pose);
                    foreach(var image in actor.Body.transform.parent.GetComponentsInChildren<RawImage>())Check(!image.name.EndsWith("Armor",StringComparison.Ordinal),"pasted icon in worn pose "+pose);
                    ForgeUI.Label("WornPoseNumber"+pose,parent,(pose+1).ToString(),pose%4*255,y+5,255,64,ForgeUI.Small,ForgeUI.Muted,FontStyle.Normal,TextAnchor.MiddleCenter);
                }
                Capture("19-worn-"+ChronicleWardrobe.Heroes[hero]+"-"+ChronicleWardrobe.Families[family]);yield return null;
            }
        }

        IEnumerator CheckLargeArtisan()
        {
            var sim=app.Sim;sim.State.Ingots=100;app.View.ShowArtisan(31);yield return null;Capture("20a-large-creation-quote");yield return null;
            var quote=sim.QuoteMasterwork(31,1);double gold=sim.State.Gold;int ingots=sim.State.Ingots;Click("BeginMasterwork1");yield return null;
            Check(sim.State.Ingots==ingots-quote.Ingots&&sim.State.Gold==gold,"large-text alloy cost matches actual reservation");
            for(int i=0;i<4;i++)
            {
                sim.Advance(.21);typeof(WorkshopView).GetField("sceneTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(app.View,.5f/.72f);Click("ArtisanStrike");app.View.Tick(.18f);yield return null;
                if(i==2)
                {
                    var bench=GameObject.Find("ArtisanWorkbench").GetComponent<RectTransform>();var viewport=bench.GetComponentInParent<ScrollRect>().viewport;var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,bench);
                    Check(bounds.min.y>=viewport.rect.yMin-1&&bounds.max.y<=viewport.rect.yMax+1,"large-text shaping must keep the whole item bench visible");
                    Check(GameObject.Find("ArtisanStrike").GetComponentInParent<ScrollRect>()==null,"shaping action must stay reachable beside its visible bench");Capture("20b-large-shaping-feedback");yield return null;
                }
            }
            Capture("20c-large-quench-choice");yield return null;Click("ArtisanQuench1");app.View.Tick(.2f);yield return null;Capture("20d-large-rune-preview");yield return null;
            Click("ArtisanRune2");yield return null;Capture("20e-large-ready-reveal");yield return null;Click("ArtisanFinish");app.View.Tick(.4f);yield return null;Capture("20f-large-finished-creation");yield return null;
            var item=sim.State.Inventory[sim.State.Inventory.Count-1];Check(item.RecipeId==31&&item.Alloy==1&&item.Temper==1&&item.Rune==2&&item.Quality==3,"real large-text creation matches selected build");
            Click("CraftResultContinue");yield return null;
        }
    }
}
#endif
