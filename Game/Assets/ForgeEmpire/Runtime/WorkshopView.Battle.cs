using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed partial class WorkshopView
    {
        RawImage battleHero,battleEnemy;
        Image battleHit,battleGuard,enemyAlert;
        RectTransform heroHealthFill,enemyHealthFill;
        Text battleState,battleVitals,battleEnemyName,battleLoot,battleFooter,enemyTell,heroDamage,enemyDamage;
        readonly Button[] skillButtons=new Button[3];
        Button battleClaim;
        double shownHeroHP,shownEnemyHP;
        float heroHurtUntil,enemyHurtUntil;
        float battleLootTop;

        public void ShowBattle()
        {
            var b=app.Sim.State.Campaign.Battle;
            if(!b.Active&&!b.Finished){ShowWorld();return;}
            var p=Page("battle","On an expedition",GameContent.Regions[b.Region].Name,ShowBattle);
            float stateHeight=ForgeUI.Height("Victory · Your craft prevailed",1020,ForgeUI.Subheading,FontStyle.Bold);
            battleState=ForgeUI.Label("BattleState",p,"",0,0,1020,stateHeight,ForgeUI.Subheading,ForgeUI.Gold,FontStyle.Bold);
            float y=stateHeight+Gap;
            var stage=ForgeUI.Rect("BattleStage",p,0,y,1020,574);
            CampaignSprite("Battlefield",stage,"battlefields",b.Region,5,0,0,1020,574);
            enemyAlert=ForgeUI.Fill("EnemyThreat",stage,670,190,300,300,new Color(1,.55f,.2f,0));
            battleHero=CampaignSprite("BattleHero",stage,"heroes",b.HeroIndex*4,12,90,183,300,300);
            battleEnemy=CampaignSprite("BattleEnemy",stage,"enemies",b.Region*4,20,700,243,230,230);
            battleGuard=ForgeUI.Fill("BattleGuard",stage,78,174,324,324,new Color(.45f,.85f,.8f,0));
            battleHit=ForgeUI.Fill("BattleHit",stage,0,0,1020,574,new Color(1,.76f,.4f,0));
            enemyTell=ForgeUI.Label("EnemyTelegraph",stage,"",430,18,570,176,ForgeUI.Body,ForgeUI.Orange,FontStyle.Bold,TextAnchor.MiddleCenter);
            heroDamage=ForgeUI.Label("HeroDamage",stage,"",60,100,360,150,ForgeUI.Subheading,ForgeUI.Orange,FontStyle.Bold,TextAnchor.MiddleCenter);
            enemyDamage=ForgeUI.Label("EnemyDamage",stage,"",640,100,360,150,ForgeUI.Subheading,ForgeUI.Cream,FontStyle.Bold,TextAnchor.MiddleCenter);
            y+=598;
            ForgeUI.Fill("HeroHealthTrack",p,0,y,498,24,ForgeUI.Panel);ForgeUI.Fill("EnemyHealthTrack",p,522,y,498,24,ForgeUI.Panel);
            heroHealthFill=ForgeUI.Fill("HeroHealth",p,0,y,498,24,ForgeUI.Teal).rectTransform;
            enemyHealthFill=ForgeUI.Fill("EnemyHealth",p,522,y,498,24,ForgeUI.Orange).rectTransform;y+=42;
            float healthHeight=Mathf.Max(ForgeUI.Height(app.Sim.EncounterName+"\n9999 / 9999 health",498,ForgeUI.Small,FontStyle.Bold),ForgeUI.Height(GameContent.Regions[b.Region].BossName+"\n9999 / 9999 health",498,ForgeUI.Small,FontStyle.Bold))+12;
            battleVitals=ForgeUI.Label("HeroVitals",p,"",0,y,498,healthHeight,ForgeUI.Small,ForgeUI.Cream,FontStyle.Bold);
            battleEnemyName=ForgeUI.Label("EnemyVitals",p,"",522,y,498,healthHeight,ForgeUI.Small,ForgeUI.Cream,FontStyle.Bold,TextAnchor.UpperRight);
            battleVitals.alignment=TextAnchor.UpperLeft;y+=healthHeight+20;
            string[] names={"Strike","Brace","Recover"};string[] symbols={"strike","hero","heal"};
            string[] descriptions={"1.8× damage","70% less damage","40% heal · Once"};
            float skillHeight=142+ForgeUI.Size(ForgeUI.Small)*2.8f;
            float descriptionHeight=0;
            for(int i=0;i<3;i++)
            {
                int id=i;
                skillButtons[i]=ForgeUI.Medallion("BattleSkill"+i,p,96+i*348,y,132,()=>app.BattleSkill(id),symbols[i]);
                var caption=skillButtons[i].GetComponentInChildren<Text>();caption.text=names[i];caption.fontSize=ForgeUI.Size(ForgeUI.Small);caption.color=ForgeUI.Cream;
                caption.rectTransform.anchoredPosition=new Vector2(-96,-146);caption.rectTransform.sizeDelta=new Vector2(324,skillHeight-142);caption.alignment=TextAnchor.UpperCenter;
                float h=ForgeUI.Height(descriptions[i],324,ForgeUI.Small);
                ForgeUI.Label("SkillHelp"+i,p,descriptions[i],i*348,y+skillHeight+16,324,h,ForgeUI.Small,ForgeUI.Muted,FontStyle.Normal,TextAnchor.UpperCenter);
                descriptionHeight=Mathf.Max(h,descriptionHeight);
            }
            y+=skillHeight+descriptionHeight+Gap+16;
            battleLootTop=y;
            float lootHeight=ForgeUI.Size(ForgeUI.Body)*6;
            battleLoot=ForgeUI.Label("BattleLoot",p,"",0,y,1020,lootHeight,ForgeUI.Body,ForgeUI.Teal,FontStyle.Normal,TextAnchor.UpperLeft);y+=lootHeight+Gap;
            battleClaim=WideButton(p,"ClaimExpedition","",y,()=>{
                bool won=app.Sim.State.Campaign.Battle.Won;
                var result=app.Sim.State.Campaign.Battle;
                if(!app.CollectExpedition())return;
                ResumeJourney();
                if(!won){ShowProgressReward("A stronger plan for the next attempt","Your hero and equipment are safe.","Improve the quality of your gear and earn a hero level on a supply trail. Then return to the challenge.");return;}
                var next=app.Sim.NextStep;
                if(next.Action==GuideAction.ClaimQuest)ShowQuestDetail(next.QuestId);
                else ShowProgressReward("Supplies brought home","+"+result.RewardGold+" gold · +"+result.RewardExperience+" hero XP\n+"+result.RewardMaterial+" "+WorkshopSimulation.MaterialName(result.Region).ToLowerInvariant(),"Your workshop has new materials. Put them toward equipment, an order or the next chapter.");
            });y+=ActionHeight+Gap;
            float footerHeight=ForgeUI.Size(ForgeUI.Small)*4;
            battleFooter=ForgeUI.Label("BattleIdleHint",p,"",0,y,1020,footerHeight,ForgeUI.Small,ForgeUI.Muted,FontStyle.Normal,TextAnchor.UpperLeft);y+=footerHeight;
            shownHeroHP=b.HeroHP;shownEnemyHP=b.EnemyHP;heroHurtUntil=enemyHurtUntil=0;
            modalRefresh.Add(RefreshBattle);EndPage(y);TickBattle(0);
        }
        void RefreshBattle()
        {
            var b=app.Sim.State.Campaign.Battle;
            battleState.text=b.Finished?(b.Won?"Victory · Your craft prevailed":"Retreat · Reforge and return"):"Encounter "+(b.Wave+1)+" / "+app.Sim.BattleWaveCount;
            battleVitals.text=GameContent.Heroes[b.HeroIndex].Name+"\n"+Math.Ceiling(b.HeroHP)+" / "+Math.Ceiling(b.HeroMaxHP)+" health";
            battleEnemyName.text=app.Sim.EncounterName+"\n"+Math.Ceiling(b.EnemyHP)+" / "+Math.Ceiling(b.EnemyMaxHP)+" health";
            double[] cooldowns={b.StrikeCooldown,b.GuardCooldown,b.HealCooldown};string[] names={"Strike","Brace","Recover"};
            for(int i=0;i<3;i++)
            {
                bool used=i==2&&b.HealsUsed>0;
                skillButtons[i].interactable=b.Active&&cooldowns[i]<=0&&!used&&(i!=2||b.HeroHP<b.HeroMaxHP);
                skillButtons[i].GetComponentInChildren<Text>().text=used?"Heal used":cooldowns[i]>0?(i==0?"Strike":names[i])+"\n"+Math.Ceiling(cooldowns[i])+"s":names[i];
            }
            bool incoming=b.Active&&b.Elapsed%6>=4.5;
            int reputation=(b.Region+1)*(2+b.Route*2);
            battleLoot.text=b.Finished?(b.Won?"+"+b.RewardGold+" gold\n+"+b.RewardMaterial+" "+WorkshopSimulation.MaterialName(b.Region).ToLowerInvariant()+"\n+"+b.RewardExperience+" hero XP · +"+reputation+" reputation":"Your hero and equipment are safe.\nNo loot this time. Forge stronger gear or match the region's weakness."):incoming?"Heavy attack in "+WorkshopSimulation.Number(6-b.Elapsed%6)+"s\n"+(b.GuardSeconds>0?"Braced · Incoming damage reduced":"Use Brace now to reduce the impact."):b.GuardSeconds>0?"Braced · Incoming damage reduced":"Auto attacks are active. Time your skills to improve the outcome.";
            battleLoot.color=incoming?ForgeUI.Orange:ForgeUI.Teal;
            battleClaim.interactable=b.Finished;
            battleClaim.gameObject.SetActive(b.Finished);
            battleClaim.GetComponentInChildren<Text>().text=b.Finished?(b.Won?"Collect rewards":"Prepare your hero"):"Expedition in progress";
            battleFooter.text=b.Finished?(b.Won?"Collect your rewards to continue the story.":"Choose equipment, then return to try again."):"You can open Workshop below. This expedition keeps going.";
            float lootHeight=ForgeUI.Height(battleLoot.text,1020,ForgeUI.Body);
            Place(battleLoot.rectTransform,0,battleLootTop,1020,lootHeight);
            float claimY=battleLootTop+lootHeight+Gap;
            Place(battleClaim.GetComponent<RectTransform>(),0,claimY,1020,ActionHeight);
            float footerY=b.Finished?claimY+ActionHeight+Gap:claimY,footerHeight=ForgeUI.Height(battleFooter.text,1020,ForgeUI.Small);
            Place(battleFooter.rectTransform,0,footerY,1020,footerHeight);
            modalBody.sizeDelta=new Vector2(1020,Mathf.Max(footerY+footerHeight+36,pageScroll.viewport.rect.height));
            enemyTell.text=incoming?"Heavy hit · "+WorkshopSimulation.Number(6-b.Elapsed%6)+"s":"";
            enemyAlert.color=new Color(1,.55f,.2f,incoming?.12f:0);
        }
        void TickBattle(float delta)
        {
            if(modalKind!="battle"||!battleHero)return;
            var b=app.Sim.State.Campaign.Battle;bool still=app.Sim.State.ReducedMotion;
            if(b.HeroHP<shownHeroHP){heroHurtUntil=sceneTime+.6f;heroDamage.text="−"+Math.Ceiling(shownHeroHP-b.HeroHP);app.Play("mine");}
            if(b.EnemyHP<shownEnemyHP){enemyHurtUntil=sceneTime+.6f;enemyDamage.text="−"+Math.Ceiling(shownEnemyHP-b.EnemyHP);app.Play("forge");}
            shownHeroHP=b.HeroHP;shownEnemyHP=b.EnemyHP;
            int heroPose=b.Finished&&b.Won?3:heroHurtUntil>sceneTime?2:enemyHurtUntil>sceneTime?1:0;
            int enemyPose=b.Finished&&b.Won?3:enemyHurtUntil>sceneTime?2:heroHurtUntil>sceneTime||b.Active&&b.Elapsed%6>=4.5?1:0;
            HandDrawnArt.Pose(battleHero,"heroes",b.HeroIndex*4+heroPose,new Vector2(240,471),300,300);
            battleEnemy.uvRect=new Rect((b.Region*4+enemyPose)/20f,0,1f/20,1);
            float size=b.Route==2&&b.Wave==app.Sim.BattleWaveCount-1?320:230;
            battleEnemy.rectTransform.sizeDelta=new Vector2(size,size);
            float impact=still?0:Mathf.Sin(Mathf.Clamp01((enemyHurtUntil-sceneTime)/.6f)*Mathf.PI)*18;
            battleHero.rectTransform.anchoredPosition=new Vector2(240+impact,-471);
            battleEnemy.rectTransform.anchoredPosition=new Vector2(700-(size-230)*.5f+impact*.5f,-(450-size*.9f));
            heroHealthFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,498*Mathf.Clamp01((float)(b.HeroHP/Math.Max(1,b.HeroMaxHP))));
            enemyHealthFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,498*Mathf.Clamp01((float)(b.EnemyHP/Math.Max(1,b.EnemyMaxHP))));
            battleGuard.color=new Color(.45f,.85f,.8f,b.GuardSeconds>0?.18f:0);
            battleHit.color=new Color(1,.76f,.4f,!still&&enemyHurtUntil-sceneTime>.48f?.12f:0);
            if(heroHurtUntil<=sceneTime)heroDamage.text="";
            if(enemyHurtUntil<=sceneTime)enemyDamage.text="";
        }
    }
}
