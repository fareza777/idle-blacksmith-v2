using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        readonly ChronicleActor[] sagaActors=new ChronicleActor[3];
        readonly Text[] sagaVitals=new Text[3];
        readonly RectTransform[] sagaHealth=new RectTransform[3];
        readonly Button[] sagaSkills=new Button[3];
        readonly int[] sagaLastAttacks=new int[3];
        RawImage sagaEnemy,sagaThreat,sagaShadow;
        Text sagaEnemyVitals,sagaTell,sagaEvent;
        RectTransform sagaEnemyHealth;
        ChronicleAtmosphere sagaAtmosphere;
        SagaPhase sagaShownPhase;
        float sagaStageAt,sagaPreviousEnemyHP,sagaEnemyHurtAt=-10;
        public void ShowSaga()
        {
            var sim=app.Sim;if(!sim.SagaInProgress){ShowWorld();return;}var run=sim.State.Saga.Run;
            sagaShownPhase=run.Phase;sagaStageAt=sceneTime;sagaPreviousEnemyHP=(float)run.EnemyHP;sagaEnemyHurtAt=-10;
            string title=run.Phase==SagaPhase.Result?(run.Won?"The party returns":"A chance to reforge"):run.Phase==SagaPhase.Choice?"The road divides":run.Phase==SagaPhase.Camp?"A quiet shelter":sim.SagaEncounterName;
            var p=Page("saga",GameContent.Regions[run.Region].Name+" / Room "+(run.Room+1)+" of "+run.TotalRooms,title,ShowSaga);
            var stage=ForgeUI.Rect("SagaStage",p,0,0,1020,574);var landscape=Landscape("SagaLandscape",stage,run.Region,0,0,1020,574);
            sagaAtmosphere=new ChronicleAtmosphere(stage,landscape,run.Region);
            sagaThreat=ForgeUI.Light("SagaThreat",stage,640,120,360,360);sagaThreat.color=Color.clear;
            for(int slot=0;slot<3;slot++)
            {
                var u=run.Units[slot];sagaActors[slot]=null;if(u.HeroId<0)continue;
                sagaLastAttacks[slot]=u.Attacks;
                ForgeUI.Light("SagaPartyShadow"+slot,stage,40+slot*176,478-(slot==1?30:0),150,24).color=new Color(0,0,0,.55f);
                var h=sim.State.Campaign.Heroes[u.HeroId];
                sagaActors[slot]=new ChronicleActor("SagaActor"+slot,stage,u.HeroId,h.Weapon,h.Armor,new Vector2(115+slot*176,492-(slot==1?30:0)),258);
            }
            sagaEnemy=null;
            if((run.Phase==SagaPhase.Combat||run.Phase==SagaPhase.Result)&&(run.EnemyId>=0||run.BossId>=0))
            {
                sagaShadow=ForgeUI.Light("SagaEnemyShadow",stage,690,477,230,34);sagaShadow.color=new Color(0,0,0,.6f);
                string sheet=run.BossId>=0?"boss-"+(run.BossId/4):"enemy-"+run.Region;
                int frame=run.BossId>=0?(run.BossId%4)*4:(run.EnemyId%8)*4;
                sagaEnemy=HandDrawnArt.Person("SagaEnemy",stage,sheet,frame,new Vector2(815,495),run.BossId>=0?360:300,run.BossId>=0?360:300);
            }
            ForgeUI.Fade("SagaStageFoot",stage,0,510,1020,64,ForgeUI.Background,true);
            float y=574+Gap;
            for(int slot=0;slot<3;slot++)
            {
                var u=run.Units[slot];string text=u.HeroId<0?"Empty slot":GameContent.Heroes[u.HeroId].Name+"\n"+Math.Ceiling(u.HP)+" / "+Math.Ceiling(u.MaxHP)+"\nBarrier "+Math.Ceiling(u.Barrier);
                float h=ForgeUI.Height("Rowan\n999 / 999\nBarrier 999",324,ForgeUI.Small,FontStyle.Bold);
                sagaVitals[slot]=ForgeUI.Label("SagaVitals"+slot,p,text,slot*348,y,324,h,ForgeUI.Small,ForgeUI.Cream,FontStyle.Bold,TextAnchor.UpperCenter);
                ForgeUI.Fill("SagaHealthTrack"+slot,p,slot*348,y+h+12,324,16,ForgeUI.Panel);
                sagaHealth[slot]=ForgeUI.Fill("SagaHealth"+slot,p,slot*348,y+h+12,324,16,ForgeUI.Teal).rectTransform;
            }
            y+=ForgeUI.Height("Rowan\n999 / 999\nBarrier 999",324,ForgeUI.Small,FontStyle.Bold)+56;
            if(run.Phase==SagaPhase.Combat)
            {
                sagaEnemyVitals=ForgeUI.Label("SagaEnemyVitals",p,"",0,y,1020,ForgeUI.Height("Guardian · Phase 3 / 3\n99999 / 99999 health · 999 barrier",1020,ForgeUI.Small),ForgeUI.Small,ForgeUI.Orange);y+=ForgeUI.Height("Guardian · Phase 3 / 3\n99999 / 99999 health · 999 barrier",1020,ForgeUI.Small)+12;
                ForgeUI.Fill("SagaEnemyTrack",p,0,y,1020,20,ForgeUI.Panel);sagaEnemyHealth=ForgeUI.Fill("SagaEnemyHealth",p,0,y,1020,20,ForgeUI.Orange).rectTransform;y+=48;
                float tellH=ForgeUI.Height(run.BossId>=0?ChronicleContent.Bosses[run.BossId].Telegraph:"The enemy prepares a charged attack.",1020,ForgeUI.Body,FontStyle.Bold);
                sagaTell=ForgeUI.Label("SagaTelegraph",p,"",0,y,1020,tellH,ForgeUI.Body,ForgeUI.Gold,FontStyle.Bold);y+=tellH+Gap;
                y=Paragraph(p,"SagaMechanicHelp",sim.SagaMechanicHint,y,ForgeUI.Small,ForgeUI.Teal)+Gap;
                int skillColumns=ForgeUI.TextScale>1.25f?1:3;
                float skillWidth=(1020-(skillColumns-1)*24)/skillColumns,skillRow=ActionHeight;
                for(int slot=0;slot<3;slot++)skillRow=Mathf.Max(skillRow,ForgeUI.Height(sim.SagaSkillName(slot)+"\n10s",skillWidth-48,ForgeUI.Small,FontStyle.Bold)+24);
                for(int slot=0;slot<3;slot++)
                {
                    int index=slot;sagaSkills[slot]=ForgeUI.Button("SagaSkill"+slot,p,sim.SagaSkillName(slot),slot%skillColumns*(skillWidth+24),y+slot/skillColumns*(skillRow+16),skillWidth,skillRow,ForgeUI.Gold,ForgeUI.Background,()=>{if(sim.UsePartySkill(index)){app.Save();app.Play("forge");Refresh();}},ForgeUI.Small);
                }
                y+=(3/skillColumns)*skillRow+(3/skillColumns-1)*16+Gap;
                for(int slot=0;slot<3;slot++)if(run.Units[slot].HeroId>=0)y=Paragraph(p,"SagaSkillHelp"+slot,GameContent.Heroes[run.Units[slot].HeroId].Name+" · "+sim.SagaSkillName(slot)+"\n"+sim.SagaSkillDescription(slot),y,ForgeUI.Small,ForgeUI.Muted)+16;
                y=Paragraph(p,"SagaIdleNote","Automatic attacks continue while you visit the workshop. The next room waits for your decision.",y,ForgeUI.Small,ForgeUI.Muted)+Gap;
            }
            else if(run.Phase==SagaPhase.Choice||run.Phase==SagaPhase.Camp)
            {
                y=Paragraph(p,"SagaRoadStory",run.LastEvent,y,ForgeUI.Body,ForgeUI.Gold)+Gap;
                y=Paragraph(p,"SagaRoadGuide",sim.SagaNextAction,y,ForgeUI.Small,ForgeUI.Muted)+Gap;
                string[] names=run.Phase==SagaPhase.Camp?new[]{"Rest · Recover 32% health","Search the shelter · +2 materials","Chart a shortcut · Gold & ready skills"}:new[]{run.Room==run.TotalRooms-1&&run.Route>=2?"Face the guardian":"Follow the safe trail","Stop at a shelter","Answer a rescue call · Higher risk"};
                string[] hints=run.Phase==SagaPhase.Camp?new[]{"A meal and a quiet watch restore injuries. Fallen companions can recover here.","Useful local supplies await in an old storeroom.","Earn a trader's purse and ready every companion skill."}:new[]{"Fight a normal encounter and keep the party's remaining health.","Use one shelter before the final room. Rest, gather supplies or plan a shortcut.","The enemy has 25% more health. Win for extra gold and regional materials."};
                for(int choice=0;choice<3;choice++)
                {
                    int index=choice;y=Paragraph(p,"SagaPathHelp"+choice,hints[choice],y,ForgeUI.Small,ForgeUI.Muted)+16;
                    var button=WideButton(p,"SagaPath"+choice,names[choice],y,()=>{if(sim.ChooseSagaPath(index)){app.Save();app.Play("click");RequestPageRebuild(true);Refresh();}});
                    button.interactable=run.Phase==SagaPhase.Camp||choice!=1||(run.Room>0&&run.Room<run.TotalRooms-1&&!run.CampUsed);y+=button.GetComponent<RectTransform>().rect.height+Gap;
                }
            }
            else
            {
                y=Paragraph(p,"SagaResult",run.Won?"+"+run.RewardGold+" gold · +"+run.RewardMaterial+" "+WorkshopSimulation.MaterialName(run.Region).ToLowerInvariant()+"\nExperience is shared by every companion who joined.":"Every companion and equipped item returns safely. Forge a stronger build, recruit support, or use the shelter on your next attempt.",y,ForgeUI.Body,ForgeUI.Teal)+Gap;
                if(run.BossId>=0&&run.Won)y=Paragraph(p,"SagaMasteryReward","Guardian mastery +"+(1+run.Modifier)+" · A legendary contract becomes available.",y,ForgeUI.Small,ForgeUI.Gold)+Gap;
                y=AddAction(p,"SagaCollect",run.Won?"Collect and continue":"Return to the guild",y,()=>{if(sim.ClaimSaga()){app.Save();app.Play("coin");ResumeJourney();}});
            }
            sagaEvent=ForgeUI.Label("SagaEvent",p,run.LastEvent,0,y,1020,ForgeUI.Height(run.LastEvent,1020,ForgeUI.Small),ForgeUI.Small,ForgeUI.Muted);y+=ForgeUI.Height(run.LastEvent,1020,ForgeUI.Small)+Gap;
            y=AddAction(p,"SagaWorkshop","Visit the workshop",y,()=>{CloseModal();journeyFollowing=false;Refresh();},false);
            modalRefresh.Add(RefreshSaga);EndPage(y);RefreshSaga();TickSaga(0);
        }
        void RefreshSaga()
        {
            var sim=app.Sim;if(!sim.SagaInProgress){RequestPageRebuild();return;}var r=sim.State.Saga.Run;
            if(r.Phase!=sagaShownPhase){RequestPageRebuild(true);return;}
            for(int slot=0;slot<3;slot++)
            {
                var u=r.Units[slot];if(!sagaVitals[slot])continue;
                sagaVitals[slot].text=u.HeroId<0?"Empty slot":GameContent.Heroes[u.HeroId].Name+"\n"+Math.Ceiling(u.HP)+" / "+Math.Ceiling(u.MaxHP)+"\nBarrier "+Math.Ceiling(u.Barrier);
                sagaHealth[slot].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,324*(float)(u.HP/Math.Max(1,u.MaxHP)));
                if(r.Phase==SagaPhase.Combat&&sagaSkills[slot]){sagaSkills[slot].interactable=u.HeroId>=0&&u.HP>0&&u.Cooldown<=0;sagaSkills[slot].GetComponentInChildren<Text>().text=sim.SagaSkillName(slot)+(u.Cooldown>0?"\n"+Math.Ceiling(u.Cooldown)+"s":"");}
            }
            if(r.Phase==SagaPhase.Combat)
            {
                sagaEnemyVitals.text=(r.BossId>=0?"Guardian · Phase "+(r.BossPhase+1)+" / 3":"Encounter "+(r.Room+1))+"\n"+Math.Ceiling(r.EnemyHP)+" / "+Math.Ceiling(r.EnemyMaxHP)+" health"+(r.EnemyBarrier>0?" · "+Math.Ceiling(r.EnemyBarrier)+" barrier":"");
                sagaEnemyHealth.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1020*(float)(r.EnemyHP/Math.Max(1,r.EnemyMaxHP)));
                sagaTell.text=string.IsNullOrEmpty(r.Telegraph)?"Automatic attacks · Skills ready below":r.Telegraph;
            }
        }
        void TickSaga(float delta)
        {
            if(modalKind!="saga"||!app.Sim.SagaInProgress)return;var run=app.Sim.State.Saga.Run;bool reduced=app.Sim.State.ReducedMotion;
            sagaAtmosphere?.Tick(sceneTime,reduced);
            for(int slot=0;slot<3;slot++)
            {
                var actor=sagaActors[slot];if(actor==null||!actor.Body)continue;string action=run.Units[slot].Action;
                if(run.Phase==SagaPhase.Choice||run.Phase==SagaPhase.Camp)action=run.Units[slot].HP>0?"ready":"defeat";
                actor.ShowBarrier(run.Units[slot].Barrier,run.Units[slot].MaxHP);
                if(run.Units[slot].Attacks>sagaLastAttacks[slot])
                {
                    sagaLastAttacks[slot]=run.Units[slot].Attacks;int recipe=run.Units[slot].WeaponRecipe;
                    var kind=recipe>=0?GameContent.Recipes[recipe].Kind:WeaponKind.Sword;
                    app.Play(kind==WeaponKind.Bow?"impact-wood":kind==WeaponKind.Staff?"impact-crystal":kind==WeaponKind.Hammer?"impact-stone":"impact-iron");
                }
                bool arriving=app.enabled&&run.Phase==SagaPhase.Combat&&run.RoomTick<6&&!reduced;
                actor.Pose(arriving?"walk":action,sceneTime,reduced,run.RoomTick*8);
            }
            if(!sagaEnemy)return;
            if(run.EnemyHP<sagaPreviousEnemyHP)sagaEnemyHurtAt=sceneTime;sagaPreviousEnemyHP=(float)run.EnemyHP;
            int pose=run.Phase==SagaPhase.Result&&run.Won?3:sceneTime-sagaEnemyHurtAt<.25f?2:!string.IsNullOrEmpty(run.Telegraph)?1:0;
            string sheet=run.BossId>=0?"boss-"+(run.BossId/4):"enemy-"+run.Region;int frame=(run.BossId>=0?run.BossId%4:run.EnemyId%8)*4+pose;
            float recoil=reduced?0:Mathf.Max(0,1-(sceneTime-sagaEnemyHurtAt)/.25f)*8;
            HandDrawnArt.Pose(sagaEnemy,sheet,frame,new Vector2(815+recoil,495),run.BossId>=0?360:300,run.BossId>=0?360:300);
            if(sagaThreat)sagaThreat.color=new Color(1,.65f,.25f,!string.IsNullOrEmpty(run.Telegraph)?.12f:0);
        }
    }
}
