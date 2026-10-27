using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        readonly ChronicleActor[] combatActors=new ChronicleActor[3];
        readonly RectTransform[] combatDockHealth=new RectTransform[3];
        readonly float[] combatDisplayedHealth=new float[3];
        readonly Button[] combatSkills=new Button[3],combatTargets=new Button[3];
        readonly RawImage[] combatEnemies=new RawImage[3],combatEnemyHalos=new RawImage[3];
        readonly RectTransform[] combatEnemyBars=new RectTransform[3],combatFocusMarks=new RectTransform[3],combatThreatMarks=new RectTransform[3];
        readonly Vector2[] combatEnemyFeet=new Vector2[3],combatEnemyPoints=new Vector2[3];
        readonly float[] combatEnemySizes=new float[3],combatEnemyPaintedHeights=new float[3],combatLastHP=new float[3],combatShownHP=new float[3],combatHurtAt=new float[3],combatContactAt=new float[3];
        readonly int[] combatLockedTargets=new int[3];
        readonly string[] shortSkills={"Guard","Focus","Break","Heal","Parry","Ward"};
        ChronicleBattleFX combatFX;
        ChronicleAtmosphere combatWeather;
        ChroniclePauseGlyph combatPause;
        Button combatPauseButton;
        RectTransform combatThreatBar,combatBreakBar;
        Text combatIntent,combatEnemyValue;
        float combatClock,combatStageHeight,combatEndAt=-1,combatViewportHeight,combatTargetWidth;
        int combatPoseSerial,combatPreviousThreat=-1;
        bool sagaCombatPage;
        public int SagaPresentedEventSerial { get { return combatFX==null?0:combatFX.PresentedSerial; } }
        public int SagaActiveEffectCount { get { return combatFX==null?0:combatFX.ActiveCount; } }
        public int SagaFlyingProjectileCount { get { return combatFX==null?0:combatFX.FlyingCount; } }
        public int SagaLastEnemyEffectRecipient { get { return combatFX==null?-1:combatFX.LastEnemyRecipient; } }
        public int SagaLastEnemyEffectSource { get { return combatFX==null?-1:combatFX.LastEnemySource; } }
        public float SagaArenaHeight { get { return combatStageHeight; } }
        public float SagaArenaViewportRatio { get { return combatStageHeight/Mathf.Max(1,combatViewportHeight); } }
        public int SagaPresentedFocus { get { return app.Sim.SagaInProgress?app.Sim.State.Saga.Run.FocusEnemy:-1; } }
        public int SagaPresentedThreat { get { var r=app.Sim.State.Saga.Run;return r!=null&&r.ThreatImpactTick>r.RoomTick?r.ThreatEnemy:-1; } }
        public Vector2 SagaTargetPosition(int slot){return slot>=0&&slot<3?combatEnemyFeet[slot]:Vector2.zero;}
        public float SagaTargetSize(int slot){return slot>=0&&slot<3?combatEnemySizes[slot]:0;}
        public int SagaHeroLockedTarget(int slot){return slot>=0&&slot<3?combatLockedTargets[slot]:-1;}
        static float CombatX(int slot){return slot==0?500:slot==1?340:180;}
        static string CombatEnemyName(SagaEnemyState enemy)
        {return enemy==null?"Enemy":enemy.BossId>=0?ChronicleContent.Bosses[enemy.BossId].Name:ChronicleContent.Enemies[enemy.EnemyId].Name;}
        static string CombatThreatName(SagaRunState run)
        {
            if(!run.ThreatMechanic)return run.ThreatAction=="heal"?"Heal":run.ThreatAction=="protect"?"Shield":run.ThreatAction=="ranged"?"Ranged shot":"Strike";
            return run.Mechanic==11?(run.PulseCount%2==0?"Violet seal":"Golden seal"):
                new[]{"Thorns","Roots","Bell","Venom","Ice charge","Mirror","Pressure","Rekindle","Star beam","Void shield","Radiant pulse"}[Mathf.Clamp(run.Mechanic,0,10)];
        }
        string CombatHeadline(SagaRunState run)
        {
            if(run.Paused)return "Battle paused\nTap play when ready";
            if(run.BreakUntilTick>run.RoomTick)return "Guard broken · +35% damage\nThe party has an opening";
            if(run.ThreatImpactTick<=run.RoomTick)return run.RoomTick<8?"The party takes position\nChoose a foe to focus":"The enemy recovers\nChoose a foe to focus";
            var source=app.Sim.SagaEnemyAt(run.ThreatEnemy);bool support=run.ThreatAction=="heal"||run.ThreatAction=="protect";var recipient=app.Sim.SagaEnemyAt(run.ThreatSupportTarget);
            string target=support?(run.ThreatSupportTarget+1)+" "+(recipient==null?"ally":recipient.Role):run.ThreatTarget<0?"Whole party":GameContent.Heroes[run.Units[run.ThreatTarget].HeroId].Name;
            string title=run.ThreatMechanic?CombatThreatName(run):(run.ThreatEnemy+1)+" "+(source==null?"Enemy":source.Role)+" · "+CombatThreatName(run);
            return title+" → "+target+"\n"+(support?"Focus support":CombatResponse(run))+" · "+((run.ThreatImpactTick-run.RoomTick)*.1).ToString("0.0")+"s";
        }
        static string CombatResponse(SagaRunState run)
        {
            if(!run.ThreatMechanic)return "Guard / skills";
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
            var sim=app.Sim;var run=sim.State.Saga.Run;sagaCombatPage=true;combatClock=0;combatEndAt=-1;combatPoseSerial=run.EventSerial;combatPreviousThreat=-1;
            var p=Page("saga",GameContent.Regions[run.Region].Name+" · Room "+(run.Room+1)+" / "+run.TotalRooms,run.BossId>=0?"Guardian":"On the road",ShowSaga);
            // A single context line gives compact displays their battle space back.
            var section=modal.Find("PageSection").GetComponent<Text>();var heading=modal.Find("PageHeading").GetComponent<Text>();
            heading.text=section.text;heading.font=section.font;heading.fontSize=ForgeUI.Size(ForgeUI.Small);section.gameObject.SetActive(false);
            float header=Mathf.Max(128,ForgeUI.Height(heading.text,834,ForgeUI.Small,FontStyle.Bold)+32);
            Place(heading.rectTransform,174,16,834,header-32);Place((RectTransform)modal.Find("PageBack"),30,6,112,112);
            Place((RectTransform)modal.Find("Hairline"),30,header-4,1020,2);Place((RectTransform)modal.Find("Diamond"),535,header-8,10,10);
            Place(pageScroll.viewport,30,header+12,1020,Mathf.Max(180,modal.rect.height-header-24));
            float pauseSize=86*ForgeUI.TextScale,skillW=(1020-pauseSize-48)/3;
            float skillH=Mathf.Max(102*ForgeUI.TextScale,ForgeUI.Height("Guard\n10s",skillW-24,ForgeUI.Small,FontStyle.Bold)+22);
            float toolH=Mathf.Max(70*ForgeUI.TextScale,ForgeUI.Height("Tactics & journal",498,ForgeUI.Small)+12),dockH=skillH+toolH+40;
            var viewport=pageScroll.viewport;viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(180,viewport.rect.height-dockH));combatViewportHeight=viewport.rect.height;
            var dock=ForgeUI.Fill("SagaControls",modal,30,modal.rect.height-dockH,1020,dockH,ForgeUI.Background).rectTransform;ForgeUI.Ornament(dock,0,0,1020);
            for(int slot=0;slot<3;slot++)
            {
                int index=slot;var u=run.Units[slot];string caption=u.HeroId<0?"—":shortSkills[u.HeroId];
                combatSkills[slot]=ForgeUI.Button("SagaSkill"+slot,dock,caption,(2-slot)*(skillW+16),12,skillW,skillH,ForgeUI.Gold,ForgeUI.Background,()=>{if(sim.UsePartySkill(index)){app.Save();Refresh();}},ForgeUI.Small);
                var label=combatSkills[slot].GetComponentInChildren<Text>();label.rectTransform.anchoredPosition=new Vector2(12,0);label.rectTransform.sizeDelta=new Vector2(skillW-24,skillH-12);
                combatDockHealth[slot]=ForgeUI.Fill("CommandHealth"+slot,combatSkills[slot].transform,12,skillH-10,skillW-24,5,ForgeUI.Teal).rectTransform;
                combatLockedTargets[slot]=u.TargetEnemy>=0?u.TargetEnemy:run.FocusEnemy;
                if(u.TargetEnemy<0&&(u.Action=="attack"||u.Action=="cast"))foreach(var e in run.Events)
                    if(e.SourceSlot==slot&&e.Enemy&&e.EnemySlot>=0&&e.Kind==SagaEventKind.Hit&&e.Tick>=run.Tick-5)combatLockedTargets[slot]=e.EnemySlot;
                combatDisplayedHealth[slot]=(float)(u.HP/Math.Max(1,u.MaxHP));combatActors[slot]=null;
            }
            combatPauseButton=ForgeUI.Medallion("SagaPause",dock,1020-pauseSize,12+(skillH-pauseSize)*.5f,pauseSize,()=>{if(sim.SetSagaPaused(!run.Paused)){app.Save();Refresh();}});
            var pauseGlyph=ForgeUI.Rect("PauseSymbol",combatPauseButton.transform,pauseSize*.3f,pauseSize*.3f,pauseSize*.4f,pauseSize*.4f);combatPause=pauseGlyph.gameObject.AddComponent<ChroniclePauseGlyph>();combatPause.color=ForgeUI.Gold;combatPause.raycastTarget=false;
            ForgeUI.Button("SagaDetails",dock,"Tactics & journal",0,skillH+22,510,toolH,Color.clear,ForgeUI.Cream,()=>{sim.SetSagaPaused(true);app.Save();ShowSagaJournal();},ForgeUI.Small);
            ForgeUI.Button("SagaWorkshop",dock,"Workshop",534,skillH+22,486,toolH,Color.clear,ForgeUI.Muted,()=>{CloseModal();journeyFollowing=false;Refresh();},ForgeUI.Small);
            float intentH=ForgeUI.Height("Radiant pulse → Whole party\nGuard / Ward · 3.0s",1020,ForgeUI.Small,FontStyle.Bold);
            combatIntent=ForgeUI.Label("SagaTelegraph",p,CombatHeadline(run),0,0,1020,intentH,ForgeUI.Small,ForgeUI.Gold,FontStyle.Bold);
            float y=intentH+8;ForgeUI.Fill("SagaIntentTrack",p,0,y,1020,7,ForgeUI.Panel);combatThreatBar=ForgeUI.Fill("SagaIntentProgress",p,0,y,1,7,ForgeUI.Orange).rectTransform;y+=17;
            float vitalH=ForgeUI.Height("Focus 1 · 99999 / 99999",1020,ForgeUI.Small);
            combatStageHeight=Mathf.Max(160,viewport.rect.height-y-vitalH-17);
            var stage=ForgeUI.Rect("SagaStage",p,0,y,1020,combatStageHeight);var landscape=HandDrawnArt.Landscape("SagaLandscape",stage,run.Region,0,0,1020,combatStageHeight,true);combatWeather=new ChronicleAtmosphere(stage,landscape,run.Region);
            int count=Mathf.Max(1,sim.SagaEnemyCount);combatTargetWidth=(1020-(count-1)*18)/count;
            float targetH=ForgeUI.Height("Protector",combatTargetWidth-50,ForgeUI.Small,FontStyle.Bold)+20;
            float ground=Mathf.Max(70,combatStageHeight-targetH-28),front=combatStageHeight-22,heroH=Mathf.Min(380,ground*.76f);
            for(int slot=0;slot<3;slot++)
            {
                combatEnemies[slot]=null;combatTargets[slot]=null;combatHurtAt[slot]=combatContactAt[slot]=-10;combatEnemySizes[slot]=0;
                var u=run.Units[slot];if(u.HeroId<0)continue;var h=sim.State.Campaign.Heroes[u.HeroId];float foot=front-slot*Mathf.Min(52,ground*.09f),size=heroH*(1-slot*.07f);
                combatActors[slot]=new ChronicleActor("SagaActor"+slot,stage,u.HeroId,h.Weapon,h.Armor,new Vector2(CombatX(slot),foot),size);
            }
            // Back-to-front depth and fixed body caps leave a clear front contact lane.
            for(int slot=2;slot>=0;slot--)
            {
                var foe=sim.SagaEnemyAt(slot);if(foe==null)continue;bool boss=foe.BossId>=0;
                float size=count==1?Mathf.Min(boss?420:330,ground*.78f):Mathf.Min(slot==0?260:slot==1?230:215,ground*(slot==0?.62f:slot==1?.51f:.40f));
                float x=count==1?815:slot==0?720:slot==1?930:790,foot=front-(count==1||slot==0?0:slot==1?Mathf.Min(150,ground*.22f):Mathf.Min(330,ground*.62f));
                combatEnemyFeet[slot]=new Vector2(x,foot);combatEnemyPoints[slot]=new Vector2(x,foot-size*.55f);combatEnemySizes[slot]=size;
                ForgeUI.Light(slot==0?"SagaEnemyShadow":"SagaEnemyShadow"+slot,stage,x-size*.32f,foot-6,size*.64f,18).color=new Color(0,0,0,.50f);
                combatEnemyHalos[slot]=ForgeUI.Light("SagaThreatHalo"+slot,stage,x-size*.55f,foot-size,size*1.1f,size);combatEnemyHalos[slot].color=Color.clear;
                string sheet=boss?"boss-"+(foe.BossId/4):FoeAppearance.Sheet(foe.EnemyId);int frame=boss?foe.BossId%4*4:FoeAppearance.Frame(foe.EnemyId,0);
                combatEnemies[slot]=HandDrawnArt.Person(slot==0?"SagaEnemy":"SagaEnemy"+slot,stage,sheet,frame,combatEnemyFeet[slot],size,size);
                combatEnemyPaintedHeights[slot]=combatEnemies[slot].rectTransform.rect.height;combatEnemyPoints[slot]=new Vector2(x,foot-combatEnemyPaintedHeights[slot]*.55f);
                combatLastHP[slot]=(float)foe.HP;combatShownHP[slot]=(float)(foe.HP/Math.Max(1,foe.MaxHP));
                var marker=ForgeUI.Label("SagaEnemyNumber"+slot,stage,(slot+1).ToString(),x-30,foot+1,60,ForgeUI.Size(32)*1.2f,32,ForgeUI.Cream,FontStyle.Bold,TextAnchor.MiddleCenter);ForgeUI.TextShadow(marker);
            }
            ForgeUI.Fade("SagaStageFoot",stage,0,combatStageHeight-28,1020,28,ForgeUI.Background,true);
            combatFX=new ChronicleBattleFX(stage,combatActors,combatEnemyPoints,run.EventSerial);
            for(int slot=0;slot<count&&slot<3;slot++)
            {
                int index=slot;var foe=sim.SagaEnemyAt(slot);if(foe==null)continue;float x=slot*(combatTargetWidth+18);
                combatTargets[slot]=ForgeUI.Button("SagaTarget"+slot,stage,foe.Role,x,0,combatTargetWidth,targetH,new Color(.055f,.075f,.065f,.80f),ForgeUI.Cream,()=>{if(sim.SelectSagaEnemy(index)){app.Save();RefreshSagaCombat();}},ForgeUI.Small);
                var label=combatTargets[slot].GetComponentInChildren<Text>();label.rectTransform.anchoredPosition=new Vector2(42,0);label.rectTransform.sizeDelta=new Vector2(combatTargetWidth-50,targetH-12);
                ForgeUI.Label("SagaTargetNumber"+slot,combatTargets[slot].transform,(slot+1).ToString(),6,3,36,ForgeUI.Size(28)*1.2f,28,ForgeUI.Muted,FontStyle.Bold,TextAnchor.UpperLeft);
                ForgeUI.Fill("SagaTargetHealthTrack"+slot,combatTargets[slot].transform,8,targetH-9,combatTargetWidth-16,5,ForgeUI.Panel);
                combatEnemyBars[slot]=ForgeUI.Fill("SagaTargetHealth"+slot,combatTargets[slot].transform,8,targetH-9,combatTargetWidth-16,5,ForgeUI.Orange).rectTransform;
                combatFocusMarks[slot]=ForgeUI.Fill("SagaFocusMark"+slot,combatTargets[slot].transform,0,0,4,targetH,ForgeUI.Gold).rectTransform;
                combatThreatMarks[slot]=ForgeUI.Fill("SagaThreatMark"+slot,combatTargets[slot].transform,0,0,combatTargetWidth,4,ForgeUI.Orange).rectTransform;
            }
            y+=combatStageHeight+8;combatEnemyValue=ForgeUI.Label("SagaEnemyVitals",p,"",0,y,1020,vitalH,ForgeUI.Small,ForgeUI.Orange);y+=vitalH+5;
            ForgeUI.Fill("SagaBreakTrack",p,0,y,1020,4,ForgeUI.Panel);combatBreakBar=ForgeUI.Fill("SagaBreakProgress",p,0,y,1,4,ForgeUI.Teal).rectTransform;y+=4;
            modalRefresh.Add(RefreshSagaCombat);EndPage(y-36);pageScroll.vertical=false;RefreshSagaCombat();TickSagaCombat(0);
        }
        void ShowSagaJournal()
        {
            var sim=app.Sim;if(!sim.SagaInProgress){ShowWorld();return;}var r=sim.State.Saga.Run;var p=Page("saga-journal","Battle is paused",sim.SagaEncounterName,ShowSagaJournal);float y=0;
            y=Paragraph(p,"SagaJournalCounter",sim.SagaMechanicHint,y,ForgeUI.Body,ForgeUI.Gold)+Gap;
            y=Paragraph(p,"SagaJournalTargetHelp","Tap a numbered enemy control to change focus. Gold marks the selected foe; orange marks the announced threat. A strike already being prepared keeps its original target.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            for(int slot=0;slot<sim.SagaEnemyCount;slot++)
            {
                var e=sim.SagaEnemyAt(slot);if(e==null)continue;string text=(slot+1)+" · "+CombatEnemyName(e)+" · "+e.Role+(slot==r.FocusEnemy?" · Focus":"")+"\n"+Math.Ceiling(e.HP)+" / "+Math.Ceiling(e.MaxHP)+" health · Guard "+Math.Ceiling(e.Guard)+(e.Barrier>0?" · Shield "+Math.Ceiling(e.Barrier):"");
                if(slot==r.ThreatEnemy&&r.ThreatImpactTick>r.RoomTick)text+="\nPreparing "+CombatThreatName(r);
                y=Paragraph(p,"SagaJournalEnemy"+slot,text,y,ForgeUI.Small,ForgeUI.Orange)+Gap;
            }
            y=Paragraph(p,"SagaJournalBreak","Party hits and skills build stagger. A broken enemy cannot attack for two seconds and takes 35% more damage. After recovering, it briefly resists another break.",y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            if(sim.SagaSynergyHint.Length>0)y=Paragraph(p,"SagaJournalSynergy",sim.SagaSynergyHint,y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            if(r.Boon>=0)y=Paragraph(p,"SagaJournalBoon",sim.SagaBoonTitle(r.Boon)+" · through room "+(r.BoonUntilRoom+1)+"\n"+sim.SagaBoonDescription(r.Boon),y,ForgeUI.Small,ForgeUI.Teal)+Gap;
            for(int slot=0;slot<3;slot++)if(r.Units[slot].HeroId>=0)
            {
                var u=r.Units[slot];y=Paragraph(p,"SagaVitals"+slot,GameContent.Heroes[u.HeroId].Name+" · "+Math.Ceiling(u.HP)+" / "+Math.Ceiling(u.MaxHP)+" health"+(u.Barrier>0?" · Shield "+Math.Ceiling(u.Barrier):""),y,ForgeUI.Small,ForgeUI.Cream)+12;
                y=Paragraph(p,"SagaSkillHelp"+slot,sim.SagaSkillName(slot)+" · "+sim.SagaSkillDescription(slot),y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            }
            y=Paragraph(p,"SagaJournalHeading","Recent actions",y,ForgeUI.Subheading,ForgeUI.Gold,true)+Gap;
            int start=Math.Max(0,r.Events.Count-12);for(int i=start;i<r.Events.Count;i++){var e=r.Events[i];y=Paragraph(p,"SagaJournalEvent"+e.Id,(e.Tick*.1).ToString("0.0")+"s · "+e.Text+(e.Amount>0?" · "+Math.Ceiling(e.Amount):""),y,ForgeUI.Small,ForgeUI.Muted)+16;}
            y=AddAction(p,"SagaJournalReturn","Return to the battle",y,ShowSaga);EndPage(y);
        }
        void RefreshSagaCombat()
        {
            var sim=app.Sim;if(!sim.SagaInProgress){RequestPageRebuild(true);return;}var r=sim.State.Saga.Run;
            for(int slot=0;slot<3;slot++)
            {
                var u=r.Units[slot];combatSkills[slot].interactable=sim.CanUseSagaSkill(slot);combatSkills[slot].GetComponentInChildren<Text>().text=u.HeroId<0?"—":shortSkills[u.HeroId]+(u.Cooldown>0?"\n"+Math.Ceiling(u.Cooldown)+"s":"");
                bool response=u.HeroId>=0&&r.ThreatMechanic&&r.ThreatImpactTick>r.RoomTick&&CombatResponse(r).Contains(shortSkills[u.HeroId]);combatSkills[slot].GetComponent<Image>().color=response?Color.Lerp(ForgeUI.Gold,ForgeUI.Teal,.35f):ForgeUI.Gold;
                var foe=sim.SagaEnemyAt(slot);if(foe==null||!combatTargets[slot])continue;bool threat=slot==r.ThreatEnemy&&r.ThreatImpactTick>r.RoomTick,focus=slot==r.FocusEnemy;
                combatTargets[slot].interactable=r.Phase==SagaPhase.Combat&&foe.HP>0;combatTargets[slot].GetComponentInChildren<Text>().color=foe.HP<=0?ForgeUI.Muted:threat?ForgeUI.Orange:focus?ForgeUI.Gold:ForgeUI.Cream;
                combatTargets[slot].GetComponentInChildren<Text>().text=foe.HP<=0?"Defeated":foe.Role;combatFocusMarks[slot].gameObject.SetActive(focus);combatThreatMarks[slot].gameObject.SetActive(threat);
            }
            combatIntent.text=CombatHeadline(r);combatIntent.color=r.Paused?ForgeUI.Teal:r.ThreatMechanic?ForgeUI.Orange:ForgeUI.Gold;
            combatThreatBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1020*(float)sim.SagaThreatProgress);combatBreakBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1020*(float)sim.SagaBreakProgress);
            var focused=sim.SagaEnemyAt(r.FocusEnemy);double hp=focused==null?r.EnemyHP:focused.HP,max=focused==null?r.EnemyMaxHP:focused.MaxHP;
            combatEnemyValue.text=(r.BossId>=0?"Phase "+(r.BossPhase+1)+" / 3":"Focus "+(r.FocusEnemy+1))+" · "+Math.Ceiling(hp)+" / "+Math.Ceiling(max);combatPause.ShowPaused(r.Paused);
            if(r.Phase!=SagaPhase.Combat)
            {
                if(combatEndAt<0)combatEndAt=combatClock;foreach(var skill in combatSkills)skill.interactable=false;combatPauseButton.interactable=false;
                combatIntent.text=r.Won||r.Phase==SagaPhase.Choice?"Encounter cleared\nThe party regroups":"The party returns safely";combatIntent.color=ForgeUI.Teal;if(combatClock-combatEndAt>=.85f)RequestPageRebuild(true);
            }
        }
        void TickSagaCombat(float delta)
        {
            if(modalKind!="saga"||!app.Sim.SagaInProgress||combatFX==null)return;var sim=app.Sim;var run=sim.State.Saga.Run;bool reduced=sim.State.ReducedMotion;if(!run.Paused)combatClock+=delta;combatWeather.Tick(combatClock,reduced);
            if(run.Phase!=SagaPhase.Combat){if(combatEndAt<0)combatEndAt=combatClock;if(combatClock-combatEndAt>=.85f)RequestPageRebuild(true);}
            foreach(var e in run.Events)if(e.Id>combatPoseSerial)
            {
                combatPoseSerial=e.Id;if(e.SourceSlot<0&&e.Kind==SagaEventKind.Hit&&!e.Enemy&&e.EnemySlot>=0&&e.EnemySlot<3)combatContactAt[e.EnemySlot]=combatClock;
                if(e.SourceSlot<0&&e.Enemy&&(e.Kind==SagaEventKind.Heal||e.Kind==SagaEventKind.Block)&&combatPreviousThreat>=0)combatContactAt[combatPreviousThreat]=combatClock;
            }
            if(run.ThreatImpactTick>run.RoomTick)combatPreviousThreat=run.ThreatEnemy;
            for(int slot=0;slot<3;slot++)
            {
                var u=run.Units[slot];if(u.TargetEnemy>=0&&u.TargetEnemy<sim.SagaEnemyCount)combatLockedTargets[slot]=u.TargetEnemy;else if(u.Action=="ready")combatLockedTargets[slot]=run.FocusEnemy;
                if(combatActors[slot]!=null)
                {
                    int target=Mathf.Clamp(combatLockedTargets[slot],0,2);var feet=combatEnemyFeet[target];combatActors[slot].SetContactTarget(feet,combatEnemySizes[target],combatEnemyPaintedHeights[target]);
                    combatActors[slot].ShowBarrier(u.Barrier,u.MaxHP);combatActors[slot].Pose(run.Phase==SagaPhase.Choice&&u.HP>0?"victory":u.Action,combatClock,reduced,0,Mathf.Max(.4f,(u.ImpactTick-u.WindupTick)*.1f));
                }
                float hp=(float)(u.HP/Math.Max(1,u.MaxHP));combatDisplayedHealth[slot]=reduced||delta<=0?hp:Mathf.Lerp(combatDisplayedHealth[slot],hp,1-Mathf.Exp(-delta*9));combatDockHealth[slot].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,(combatSkills[slot].GetComponent<RectTransform>().rect.width-24)*combatDisplayedHealth[slot]);
                var foe=sim.SagaEnemyAt(slot);if(foe==null||!combatEnemies[slot])continue;if(foe.HP<combatLastHP[slot])combatHurtAt[slot]=combatClock;combatLastHP[slot]=(float)foe.HP;
                bool threat=run.ThreatEnemy==slot&&run.ThreatImpactTick>run.RoomTick;float contact=combatClock-combatContactAt[slot];int pose=foe.HP<=0?5:run.BreakUntilTick>run.RoomTick?3:combatClock-combatHurtAt[slot]<.22f?3:contact<.13f?2:contact<.45f?4:threat?1:0;
                bool boss=foe.BossId>=0;string sheet=boss?"boss-"+(foe.BossId/4):FoeAppearance.Sheet(foe.EnemyId);int frame=boss?foe.BossId%4*4+(pose==5?3:pose==3?2:pose==1?1:0):FoeAppearance.Frame(foe.EnemyId,pose);
                float recoil=reduced?0:Mathf.Max(0,1-(combatClock-combatHurtAt[slot])/.22f)*7;HandDrawnArt.Pose(combatEnemies[slot],sheet,frame,combatEnemyFeet[slot]+new Vector2(recoil,0),combatEnemySizes[slot],combatEnemySizes[slot]);
                combatEnemies[slot].color=foe.HP<=0?new Color(.75f,.75f,.7f,.62f):foe.FrostUntilTick>run.RoomTick?new Color(.70f,.89f,1,1):Color.white;
                combatEnemyHalos[slot].color=new Color(threat?1:.85f,threat?.48f:.73f,threat?.20f:.35f,foe.HP>0?(threat?.27f:run.FocusEnemy==slot?.12f:0):0);
                float value=(float)(foe.HP/Math.Max(1,foe.MaxHP));combatShownHP[slot]=reduced||delta<=0?value:Mathf.Lerp(combatShownHP[slot],value,1-Mathf.Exp(-delta*8));combatEnemyBars[slot].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,(combatTargetWidth-16)*combatShownHP[slot]);
            }
            combatFX.Consume(run,combatClock,app.enabled,app.Play);combatFX.Tick(combatClock,reduced);
        }
    }
}
