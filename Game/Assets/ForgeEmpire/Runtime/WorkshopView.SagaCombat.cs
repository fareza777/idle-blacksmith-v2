using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        readonly ChronicleActor[] combatActors=new ChronicleActor[3];
        readonly Text[] combatVitals=new Text[3];
        readonly RectTransform[] combatHealth=new RectTransform[3];
        readonly RectTransform[] combatDockHealth=new RectTransform[3];
        readonly float[] combatDisplayedHealth=new float[3];
        readonly Button[] combatSkills=new Button[3];
        readonly string[] shortSkills={"Guard","Focus","Break","Heal","Parry","Ward"};
        ChronicleBattleFX combatFX;
        ChronicleAtmosphere combatWeather;
        ChroniclePauseGlyph combatPause;
        Button combatPauseButton;
        RawImage combatEnemy,combatThreatHalo;
        RectTransform combatEnemyHealth,combatStageHealth,combatThreatBar,combatBreakBar;
        Text combatIntent,combatEnemyValue,combatCommandNote;
        float combatClock,combatStageHeight,combatEnemyShown,combatLastEnemyHP,combatHurtAt=-10,combatEndAt=-1;
        bool sagaCombatPage;
        public int SagaPresentedEventSerial { get { return combatFX==null?0:combatFX.PresentedSerial; } }
        public int SagaActiveEffectCount { get { return combatFX==null?0:combatFX.ActiveCount; } }
        static float CombatX(int slot){return slot==0?500:slot==1?340:180;}
        static string CombatThreatName(SagaRunState run)
        {
            if(!run.ThreatMechanic)return "Strike";
            return run.Mechanic==11?(run.PulseCount%2==0?"Violet seal":"Golden seal"):
                new[]{"Thorns","Roots","Bell","Venom","Ice charge","Mirror","Pressure","Rekindle","Star beam","Void shield","Radiant pulse"}[Mathf.Clamp(run.Mechanic,0,10)];
        }
        string CombatHeadline(SagaRunState run)
        {
            if(run.Paused)return "Battle paused · Plan your next move";
            if(run.BreakUntilTick>run.RoomTick)return "Guard broken · +35% damage";
            if(run.ThreatImpactTick<=run.RoomTick)return run.RoomTick<8?"The party takes position":"The enemy recovers";
            string target=run.ThreatTarget<0?"Whole party":GameContent.Heroes[run.Units[run.ThreatTarget].HeroId].Name;
            return CombatThreatName(run)+" → "+target+"\n"+CombatResponse(run)+" · "+((run.ThreatImpactTick-run.RoomTick)*.1).ToString("0.0")+"s";
        }
        static string CombatResponse(SagaRunState run)
        {
            if(!run.ThreatMechanic)return "Impact";
            switch(run.Mechanic)
            {
                case 0:case 6:return "Focus / Break";
                case 1:case 3:case 10:return "Guard / Ward";
                case 2:return "Break";
                case 4:return "Parry / Guard";
                case 5:case 9:return "Focus";
                case 7:return "Break / Frost";
                case 8:return "Break / Storm";
                case 11:return run.PulseCount%2==0?"Focus":"Guard / Ward";
                default:return "Impact";
            }
        }
        void ShowSagaCombat()
        {
            var sim=app.Sim;var run=sim.State.Saga.Run;sagaCombatPage=true;combatClock=0;combatHurtAt=-10;combatEndAt=-1;
            var p=Page("saga",GameContent.Regions[run.Region].Name+" / Room "+(run.Room+1)+" of "+run.TotalRooms,sim.SagaEncounterName,ShowSaga);
            float skillH=Mathf.Max(ActionHeight,ForgeUI.Height("Guard\n10s",224,ForgeUI.Small,FontStyle.Bold)+24),dockH=skillH+32;
            var viewport=pageScroll.viewport;viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(160,viewport.rect.height-dockH));
            var dock=ForgeUI.Fill("SagaControls",modal,30,modal.rect.height-dockH,1020,dockH,ForgeUI.Background).rectTransform;ForgeUI.Ornament(dock,0,0,1020);
            float pauseSize=86*ForgeUI.TextScale,skillW=(1020-pauseSize-48)/3;
            for(int slot=0;slot<3;slot++)
            {
                int index=slot;var u=run.Units[slot];string caption=u.HeroId<0?"—":shortSkills[u.HeroId];
                combatSkills[slot]=ForgeUI.Button("SagaSkill"+slot,dock,caption,(2-slot)*(skillW+16),16,skillW,skillH,ForgeUI.Gold,ForgeUI.Background,()=>{if(sim.UsePartySkill(index)){app.Save();Refresh();}},ForgeUI.Small);
                combatDockHealth[slot]=ForgeUI.Fill("CommandHealth"+slot,combatSkills[slot].transform,12,skillH-10,skillW-24,5,ForgeUI.Teal).rectTransform;
            }
            var pause=ForgeUI.Medallion("SagaPause",dock,1020-pauseSize,16+(skillH-pauseSize)*.5f,pauseSize,()=>{if(sim.SetSagaPaused(!run.Paused)){app.Save();Refresh();}});
            combatPauseButton=pause;
            var pauseGlyph=ForgeUI.Rect("PauseSymbol",pause.transform,pauseSize*.3f,pauseSize*.3f,pauseSize*.4f,pauseSize*.4f);combatPause=pauseGlyph.gameObject.AddComponent<ChroniclePauseGlyph>();combatPause.color=ForgeUI.Gold;combatPause.raycastTarget=false;

            float intentH=ForgeUI.Height("Radiant pulse → Whole party\nFocus / Break · 3.0s",1020,ForgeUI.Small,FontStyle.Bold);
            combatIntent=ForgeUI.Label("SagaTelegraph",p,CombatHeadline(run),0,0,1020,intentH,ForgeUI.Small,ForgeUI.Gold,FontStyle.Bold);
            float y=intentH+12;ForgeUI.Fill("SagaIntentTrack",p,0,y,1020,10,ForgeUI.Panel);combatThreatBar=ForgeUI.Fill("SagaIntentProgress",p,0,y,1,10,ForgeUI.Orange).rectTransform;y+=26;
            combatStageHeight=Mathf.Clamp(viewport.rect.height-y-4,220,root.rect.height<1600?340:430);
            var stage=ForgeUI.Rect("SagaStage",p,0,y,1020,combatStageHeight);var landscape=Landscape("SagaLandscape",stage,run.Region,0,0,1020,combatStageHeight);combatWeather=new ChronicleAtmosphere(stage,landscape,run.Region);
            float foot=combatStageHeight*.90f,actorH=combatStageHeight*.66f;
            combatThreatHalo=ForgeUI.Light("SagaThreat",stage,690,foot-200,250,240);combatThreatHalo.color=Color.clear;
            for(int slot=0;slot<3;slot++)
            {
                combatActors[slot]=null;var u=run.Units[slot];if(u.HeroId<0)continue;var h=sim.State.Campaign.Heroes[u.HeroId];
                ForgeUI.Light("SagaPartyShadow"+slot,stage,CombatX(slot)-65,foot-10,130,20).color=new Color(0,0,0,.45f);
                combatActors[slot]=new ChronicleActor("SagaActor"+slot,stage,u.HeroId,h.Weapon,h.Armor,new Vector2(CombatX(slot),foot),actorH);combatActors[slot].SetStrikeTarget(680);
            }
            ForgeUI.Light("SagaEnemyShadow",stage,710,foot-8,200,24).color=new Color(0,0,0,.5f);
            string sheet=run.BossId>=0?"boss-"+(run.BossId/4):"enemy-"+run.Region;int frame=(run.BossId>=0?run.BossId%4:run.EnemyId%8)*4;
            float enemySize=run.BossId>=0?actorH*1.2f:actorH;
            combatEnemy=HandDrawnArt.Person("SagaEnemy",stage,sheet,frame,new Vector2(815,foot),enemySize,enemySize);
            ForgeUI.Fill("SagaStageHealthTrack",stage,680,16,320,8,ForgeUI.Panel);combatStageHealth=ForgeUI.Fill("SagaStageHealth",stage,680,16,320,8,ForgeUI.Orange).rectTransform;
            ForgeUI.Fade("SagaStageFoot",stage,0,combatStageHeight-50,1020,50,ForgeUI.Background,true);
            combatFX=new ChronicleBattleFX(stage,combatActors,new Vector2(815,foot),run.EventSerial);
            combatLastEnemyHP=(float)run.EnemyHP;combatEnemyShown=(float)(run.EnemyHP/Math.Max(1,run.EnemyMaxHP));y+=combatStageHeight+12;
            float vitalH=ForgeUI.Height("Rowan\n999 / 999\nShield 999",324,ForgeUI.Small,FontStyle.Bold);
            for(int slot=0;slot<3;slot++)
            {
                int column=2-slot;combatVitals[slot]=ForgeUI.Label("SagaVitals"+slot,p,"",column*348,y,324,vitalH,ForgeUI.Small,ForgeUI.Cream,FontStyle.Bold,TextAnchor.UpperCenter);
                ForgeUI.Fill("SagaHealthTrack"+slot,p,column*348,y+vitalH+8,324,12,run.Units[slot].HeroId<0?Color.clear:ForgeUI.Panel);
                combatHealth[slot]=ForgeUI.Fill("SagaHealth"+slot,p,column*348,y+vitalH+8,324,12,run.Units[slot].HeroId<0?Color.clear:ForgeUI.Teal).rectTransform;combatDisplayedHealth[slot]=(float)(run.Units[slot].HP/Math.Max(1,run.Units[slot].MaxHP));
            }
            y+=vitalH+36;float enemyH=ForgeUI.Height("Guardian · Phase 3 / 3\n99999 / 99999 health · Shield 999",1020,ForgeUI.Small);
            combatEnemyValue=ForgeUI.Label("SagaEnemyVitals",p,"",0,y,1020,enemyH,ForgeUI.Small,ForgeUI.Orange);y+=enemyH+8;
            ForgeUI.Fill("SagaEnemyTrack",p,0,y,1020,16,ForgeUI.Panel);combatEnemyHealth=ForgeUI.Fill("SagaEnemyHealth",p,0,y,1020,16,ForgeUI.Orange).rectTransform;y+=32;
            y=Paragraph(p,"SagaBreakCaption","Break the enemy's guard · Skills build stagger",y,ForgeUI.Small,ForgeUI.Teal)+8;
            ForgeUI.Fill("SagaBreakTrack",p,0,y,1020,8,ForgeUI.Panel);combatBreakBar=ForgeUI.Fill("SagaBreakProgress",p,0,y,1,8,ForgeUI.Teal).rectTransform;y+=28;
            float noteH=ForgeUI.Height("Paused · Workshop production continues. Tap play to resume.",1020,ForgeUI.Small);
            combatCommandNote=ForgeUI.Label("SagaCommandNote",p,"",0,y,1020,noteH,ForgeUI.Small,ForgeUI.Muted);y+=noteH+Gap;
            y=AddAction(p,"SagaDetails","Tactics and battle journal",y,()=>{sim.SetSagaPaused(true);app.Save();ShowSagaJournal();},false);
            y=AddAction(p,"SagaWorkshop","Visit the workshop",y,()=>{CloseModal();journeyFollowing=false;Refresh();},false);
            modalRefresh.Add(RefreshSagaCombat);EndPage(y);RefreshSagaCombat();TickSagaCombat(0);
        }
        void ShowSagaJournal()
        {
            var sim=app.Sim;if(!sim.SagaInProgress){ShowWorld();return;}var r=sim.State.Saga.Run;
            var p=Page("saga-journal","Battle is paused","Read the encounter",ShowSagaJournal);float y=0;
            y=Paragraph(p,"SagaJournalCounter",sim.SagaMechanicHint,y,ForgeUI.Body,ForgeUI.Gold)+Gap;
            y=Paragraph(p,"SagaJournalBreak","Party hits and skills build stagger. A broken enemy cannot attack for two seconds and takes 35% more damage. After recovering, it briefly resists another break.",y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            for(int slot=0;slot<3;slot++)if(r.Units[slot].HeroId>=0)y=Paragraph(p,"SagaSkillHelp"+slot,GameContent.Heroes[r.Units[slot].HeroId].Name+" · "+sim.SagaSkillName(slot)+"\n"+sim.SagaSkillDescription(slot),y,ForgeUI.Small)+Gap;
            y=Paragraph(p,"SagaJournalHeading","Recent actions",y,ForgeUI.Subheading,ForgeUI.Gold,true)+Gap;
            int start=Math.Max(0,r.Events.Count-8);for(int i=start;i<r.Events.Count;i++){var e=r.Events[i];y=Paragraph(p,"SagaJournalEvent"+e.Id,(e.Tick*.1).ToString("0.0")+"s · "+e.Text+(e.Amount>0?" · "+Math.Ceiling(e.Amount):""),y,ForgeUI.Small,ForgeUI.Muted)+16;}
            y=AddAction(p,"SagaJournalReturn","Return to the battle",y,ShowSaga);EndPage(y);
        }
        void RefreshSagaCombat()
        {
            var sim=app.Sim;if(!sim.SagaInProgress){RequestPageRebuild(true);return;}var r=sim.State.Saga.Run;
            for(int slot=0;slot<3;slot++)
            {
                var u=r.Units[slot];combatVitals[slot].text=u.HeroId<0?"":GameContent.Heroes[u.HeroId].Name+"\n"+Math.Ceiling(u.HP)+" / "+Math.Ceiling(u.MaxHP)+(u.Barrier>0?"\nShield "+Math.Ceiling(u.Barrier):"");
                combatSkills[slot].interactable=sim.CanUseSagaSkill(slot);combatSkills[slot].GetComponentInChildren<Text>().text=u.HeroId<0?"—":shortSkills[u.HeroId]+(u.Cooldown>0?"\n"+Math.Ceiling(u.Cooldown)+"s":"");
                bool isTarget=r.ThreatImpactTick>r.RoomTick&&(sim.SagaThreatTarget<0||sim.SagaThreatTarget==slot);
                combatVitals[slot].color=isTarget?ForgeUI.Orange:ForgeUI.Cream;
                bool response=u.HeroId>=0&&r.ThreatMechanic&&r.ThreatImpactTick>r.RoomTick&&CombatResponse(r).Contains(shortSkills[u.HeroId]);
                combatSkills[slot].GetComponent<Image>().color=response?Color.Lerp(ForgeUI.Gold,ForgeUI.Teal,.35f):ForgeUI.Gold;
            }
            combatIntent.text=CombatHeadline(r);
            combatIntent.color=r.Paused?ForgeUI.Teal:r.ThreatMechanic?ForgeUI.Orange:ForgeUI.Gold;
            combatThreatBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1020*(float)sim.SagaThreatProgress);combatBreakBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1020*(float)sim.SagaBreakProgress);
            combatEnemyValue.text=(r.BossId>=0?"Guardian · Phase "+(r.BossPhase+1)+" / 3":"Enemy")+"\n"+Math.Ceiling(r.EnemyHP)+" / "+Math.Ceiling(r.EnemyMaxHP)+" health"+(r.EnemyBarrier>0?" · Shield "+Math.Ceiling(r.EnemyBarrier):"");
            combatCommandNote.text=r.Paused?"Paused · Workshop production continues. Tap play to resume.":"Automatic strikes · Choose skills as the enemy prepares its attack.";combatPause.ShowPaused(r.Paused);
            if(r.Phase!=SagaPhase.Combat)
            {
                if(combatEndAt<0)combatEndAt=combatClock;
                foreach(var skill in combatSkills)skill.interactable=false;combatPauseButton.interactable=false;
                combatIntent.text=r.Won||r.Phase==SagaPhase.Choice?"Encounter cleared · The party regroups.":"The party returns safely.";combatIntent.color=ForgeUI.Teal;
                if(combatClock-combatEndAt>=.85f)RequestPageRebuild(true);
            }
        }
        void TickSagaCombat(float delta)
        {
            if(modalKind!="saga"||!app.Sim.SagaInProgress||!combatEnemy)return;var sim=app.Sim;var run=sim.State.Saga.Run;
            bool reduced=sim.State.ReducedMotion;if(!run.Paused)combatClock+=delta;combatWeather.Tick(combatClock,reduced);
            if(run.Phase!=SagaPhase.Combat){if(combatEndAt<0)combatEndAt=combatClock;if(combatClock-combatEndAt>=.85f)RequestPageRebuild(true);}
            combatFX.Consume(run,combatClock,app.enabled,app.Play);combatFX.Tick(combatClock,reduced);
            for(int slot=0;slot<3;slot++)
            {
                var u=run.Units[slot];if(combatActors[slot]!=null){combatActors[slot].ShowBarrier(u.Barrier,u.MaxHP);combatActors[slot].Pose(run.Phase==SagaPhase.Choice&&u.HP>0?"victory":u.Action,combatClock,reduced,0,Mathf.Max(.4f,(u.ImpactTick-u.WindupTick)*.1f));}
                float hp=(float)(u.HP/Math.Max(1,u.MaxHP));combatDisplayedHealth[slot]=reduced||delta<=0?hp:Mathf.Lerp(combatDisplayedHealth[slot],hp,1-Mathf.Exp(-delta*9));combatHealth[slot].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,324*combatDisplayedHealth[slot]);
                combatDockHealth[slot].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,(combatSkills[slot].GetComponent<RectTransform>().rect.width-24)*combatDisplayedHealth[slot]);
            }
            if(run.EnemyHP<combatLastEnemyHP)combatHurtAt=combatClock;combatLastEnemyHP=(float)run.EnemyHP;
            int pose=run.EnemyHP<=0?3:run.BreakUntilTick>run.RoomTick?2:combatClock-combatHurtAt<.2f?2:run.ThreatImpactTick>run.RoomTick?1:0;
            string sheet=run.BossId>=0?"boss-"+(run.BossId/4):"enemy-"+run.Region;int frame=(run.BossId>=0?run.BossId%4:run.EnemyId%8)*4+pose;
            float actorH=combatStageHeight*.66f,enemySize=run.BossId>=0?actorH*1.2f:actorH,recoil=reduced?0:Mathf.Max(0,1-(combatClock-combatHurtAt)/.2f)*7;
            HandDrawnArt.Pose(combatEnemy,sheet,frame,new Vector2(815+recoil,combatStageHeight*.90f),enemySize,enemySize);
            combatThreatHalo.color=new Color(1,.62f,.28f,run.ThreatImpactTick>run.RoomTick?.12f:0);
            float target=(float)(run.EnemyHP/Math.Max(1,run.EnemyMaxHP));combatEnemyShown=reduced||delta<=0?target:Mathf.Lerp(combatEnemyShown,target,1-Mathf.Exp(-delta*8));combatEnemyHealth.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1020*combatEnemyShown);
            combatStageHealth.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,320*combatEnemyShown);
        }
    }
}
