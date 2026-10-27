using System;

namespace ForgeEmpire
{
    public sealed partial class WorkshopSimulation
    {
        public int SagaEnemyCount { get { return SagaInProgress&&State.Saga.Run.Enemies!=null?State.Saga.Run.Enemies.Length:0; } }
        public SagaEnemyState SagaEnemyAt(int slot)
        {
            if(!SagaInProgress||slot<0||slot>=SagaEnemyCount)return null;
            return State.Saga.Run.Enemies[slot];
        }
        public bool SelectSagaEnemy(int slot)
        {
            if(!SagaInProgress)return false;var r=State.Saga.Run;SyncSagaFormation();var e=SagaEnemyAt(slot);
            if(r.Phase!=SagaPhase.Combat||e==null||e.HP<=0||slot==r.FocusEnemy)return false;
            r.FocusEnemy=slot;ReadSagaEnemyView();PublishSagaBattle();return true;
        }
        int FirstLivingSagaEnemy()
        {
            var r=State.Saga.Run;for(int i=0;i<r.Enemies.Length;i++)if(r.Enemies[i]!=null&&r.Enemies[i].HP>0)return i;return -1;
        }
        void RememberSagaEnemyView()
        {
            var r=State.Saga.Run;r.MirrorEnemyId=r.EnemyId;r.MirrorBossId=r.BossId;r.MirrorHP=r.EnemyHP;r.MirrorMaxHP=r.EnemyMaxHP;r.MirrorGuard=r.EnemyGuard;r.MirrorBarrier=r.EnemyBarrier;
        }
        void ReadSagaEnemyView()
        {
            var r=State.Saga.Run;var e=SagaEnemyAt(r.FocusEnemy);if(e==null)return;
            r.EnemyId=e.EnemyId;r.BossId=e.BossId;r.EnemyHP=e.HP;r.EnemyMaxHP=e.MaxHP;r.EnemyGuard=e.Guard;r.EnemyBarrier=e.Barrier;RememberSagaEnemyView();
        }
        void WriteSagaEnemyView(int slot)
        {
            var r=State.Saga.Run;var e=SagaEnemyAt(slot);if(e==null)return;
            e.EnemyId=r.EnemyId;e.BossId=r.BossId;e.HP=r.EnemyHP;e.MaxHP=r.EnemyMaxHP;e.Guard=r.EnemyGuard;e.Barrier=r.EnemyBarrier;
        }
        void SyncSagaFormation()
        {
            var r=State.Saga.Run;if(r.FormationVersion==0)
            {
                r.FormationVersion=1;r.FocusEnemy=0;
                if(r.EnemyMaxHP>0)r.Enemies=new[]{new SagaEnemyState{EnemyId=r.EnemyId,BossId=r.BossId,HP=r.EnemyHP,MaxHP=r.EnemyMaxHP,Guard=r.EnemyGuard,Barrier=r.EnemyBarrier,Role="Vanguard"}};
                RememberSagaEnemyView();return;
            }
            if(r.Enemies==null)r.Enemies=new SagaEnemyState[0];
            if(r.EnemyId!=r.MirrorEnemyId||r.BossId!=r.MirrorBossId||r.EnemyHP!=r.MirrorHP||r.EnemyMaxHP!=r.MirrorMaxHP||r.EnemyGuard!=r.MirrorGuard||r.EnemyBarrier!=r.MirrorBarrier)
            {
                // Released callers edit the scalar view directly; only that recipient changes.
                if(r.BossId>=0&&r.BossId<12)r.EnemyId=-1;
                WriteSagaEnemyView(r.FocusEnemy);RememberSagaEnemyView();
            }
            var focus=SagaEnemyAt(r.FocusEnemy);if(focus==null||focus.HP<=0){int living=FirstLivingSagaEnemy();if(living>=0)r.FocusEnemy=living;}
            ReadSagaEnemyView();CancelLostSagaContacts();
        }
        void CancelLostSagaContacts()
        {
            var r=State.Saga.Run;if(r.Phase!=SagaPhase.Combat)return;
            foreach(var unit in r.Units)if(unit.ImpactTick>0)
            {
                var recipient=SagaEnemyAt(unit.TargetEnemy);if(unit.HeroId<0||unit.HP<=0||recipient==null||recipient.HP<=0){unit.ImpactTick=0;unit.TargetEnemy=-1;}
            }
            if(r.ThreatImpactTick<=0)return;var source=SagaEnemyAt(r.ThreatEnemy);
            bool lost=source==null||source.HP<=0;
            if(r.ThreatAction=="heal"||r.ThreatAction=="protect"){var recipient=SagaEnemyAt(r.ThreatSupportTarget);lost|=recipient==null||recipient.HP<=0;}
            if(r.ThreatTarget>=0)lost|=r.ThreatTarget>=3||r.Units[r.ThreatTarget].HeroId<0||r.Units[r.ThreatTarget].HP<=0;
            if(!lost)return;
            r.ThreatImpactTick=0;r.ThreatStartTick=0;r.ThreatTarget=-1;r.ThreatEnemy=-1;r.ThreatSupportTarget=-1;r.ThreatAction="strike";r.ThreatMechanic=false;r.Telegraph="";r.ResponseMask=0;
            r.NextEnemyTick=Math.Max(r.NextEnemyTick,r.RoomTick+SagaEnemyWindupTicks);
        }
        bool NormalizeSagaFormation()
        {
            var r=State.Saga.Run;SyncSagaFormation();
            if(r.Enemies.Length>3)Array.Resize(ref r.Enemies,3);
            for(int slot=0;slot<r.Enemies.Length;slot++)
            {
                var e=r.Enemies[slot];if(e==null)continue;
                bool boss=e.BossId>=0&&e.BossId<12&&e.BossId==r.Region*2+(r.Route==3?1:0)&&r.Route>=2&&r.Room==r.TotalRooms-1;
                bool enemy=e.EnemyId>=0&&e.EnemyId<48&&ChronicleContent.Enemies[e.EnemyId].Region==r.Region;
                if((!boss&&!enemy)||!Finite(e.HP)||!Finite(e.MaxHP)||e.MaxHP<=0)return false;
                if(boss){e.EnemyId=-1;e.Role="Vanguard";if(r.Enemies.Length!=1)return false;}else e.BossId=-1;
                e.MaxHP=Clamp(e.MaxHP,1,60000);e.HP=Clamp(e.HP,0,e.MaxHP);e.Guard=Clamp(e.Guard,0,150);e.Barrier=Clamp(e.Barrier,0,e.MaxHP*.4);
                e.FrostUntilTick=Clamp(e.FrostUntilTick,0,r.RoomTick+30);e.ChargedUntilTick=Clamp(e.ChargedUntilTick,0,r.RoomTick+40);
                if(e.Role!="Healer"&&e.Role!="Protector"&&e.Role!="Ranged")e.Role="Vanguard";
            }
            r.FocusEnemy=Clamp(r.FocusEnemy,0,Math.Max(0,r.Enemies.Length-1));r.EnemyTurn=Clamp(r.EnemyTurn,0,MaxCounter);
            int live=FirstLivingSagaEnemy();if(r.Phase==SagaPhase.Combat&&live<0)return false;
            if(SagaEnemyAt(r.FocusEnemy)==null||SagaEnemyAt(r.FocusEnemy).HP<=0){if(live>=0)r.FocusEnemy=live;}
            ReadSagaEnemyView();return r.Phase!=SagaPhase.Combat||SagaEnemyAt(r.FocusEnemy)!=null;
        }
        void CreateSagaFormation(int choice)
        {
            var r=State.Saga.Run;
            int size=r.BossId>=0||r.Room==0&&r.Route==0&&choice==0?1:r.Room>=2||choice==2||choice==5?3:2;
            double total=r.EnemyMaxHP*(size>1?1.08:1);var result=new SagaEnemyState[size];int leader=r.EnemyId;
            for(int slot=0;slot<size;slot++)
            {
                double share=size==1?1:size==2?(slot==0?.65:.35):(slot==0?.5:.25);
                int id=slot==0?leader:r.Region*8+((leader%8)+slot*2)%8;
                string role=slot==0?"Vanguard":size==2?"Healer":slot==1?"Protector":"Ranged";
                double hp=Math.Max(1,Math.Round(total*share));
                result[slot]=new SagaEnemyState{EnemyId=id,BossId=slot==0?r.BossId:-1,HP=hp,MaxHP=hp,Guard=slot==0?r.EnemyGuard:ChronicleContent.Enemies[id].Guard,Barrier=slot==0?r.EnemyBarrier:0,Role=role};
            }
            r.Enemies=result;r.FormationVersion=1;r.FocusEnemy=0;r.EnemyTurn=0;ReadSagaEnemyView();
        }
        int SagaThreatSource()
        {
            var r=State.Saga.Run;for(int n=0;n<r.Enemies.Length;n++)
            {
                int slot=(r.EnemyTurn+n)%r.Enemies.Length;var e=r.Enemies[slot];if(e!=null&&e.HP>0){r.EnemyTurn=(slot+1)%r.Enemies.Length;return slot;}
            }
            return -1;
        }
        int SagaSupportRecipient(bool healing)
        {
            var r=State.Saga.Run;int target=-1;double lowest=1;
            for(int slot=0;slot<r.Enemies.Length;slot++)
            {
                var e=r.Enemies[slot];if(e==null||e.HP<=0)continue;double fraction=healing?e.HP/e.MaxHP:e.Barrier/(e.MaxHP*.18);
                if(fraction<lowest){lowest=fraction;target=slot;}
            }
            return target;
        }
        bool ResolveSagaSupport()
        {
            var r=State.Saga.Run;if(r.ThreatAction!="heal"&&r.ThreatAction!="protect")return false;
            var source=SagaEnemyAt(r.ThreatEnemy);var target=SagaEnemyAt(r.ThreatSupportTarget);
            if(source==null||source.HP<=0||target==null||target.HP<=0)return true;
            if(r.DisruptSeconds>0){SagaEvent(SagaEventKind.Block,-1,-1,0,false,"Support disrupted",r.ThreatEnemy);return true;}
            bool heal=r.ThreatAction=="heal";double amount=heal?Math.Min(target.MaxHP-target.HP,target.MaxHP*.1):Math.Min(target.MaxHP*.18-target.Barrier,target.MaxHP*.12);
            amount=Math.Max(0,amount);if(heal)target.HP+=amount;else target.Barrier+=amount;
            if(amount>0)SagaEvent(heal?SagaEventKind.Heal:SagaEventKind.Block,-1,-1,amount,true,heal?"Healer restores an ally":"Protector shields an ally",r.ThreatSupportTarget);
            ReadSagaEnemyView();return true;
        }
        public string SagaSynergyHint
        {
            get
            {
                bool frost=false,heavy=false,rune=false,defense=false;
                for(int slot=0;slot<3;slot++)
                {
                    int hero=PartyHero(slot);if(hero<0)continue;int recipe=SagaInProgress?State.Saga.Run.Units[slot].WeaponRecipe:State.Campaign.Heroes[hero].Weapon==null?-1:State.Campaign.Heroes[hero].Weapon.RecipeId;
                    if(!ValidRecipe(recipe))continue;var d=GameContent.Recipes[recipe];int effects=0;
                    if(SagaInProgress)effects=State.Saga.Run.Units[slot].Effects;
                    else {var equipped=State.Campaign.Heroes[hero];foreach(var item in new[]{equipped.Weapon,equipped.Armor,equipped.Accessory}){effects|=1<<(int)ItemEffect(item);effects|=1<<(int)ItemTechniqueEffect(item);}}
                    frost|=d.Element=="Frost"||(effects&(1<<(int)GearEffect.Frostbite))!=0;
                    heavy|=d.Kind==WeaponKind.Hammer||d.Kind==WeaponKind.Greatsword||d.Kind==WeaponKind.Axe;
                    rune|=d.Kind==WeaponKind.Staff||(effects&(1<<(int)GearEffect.ChainLightning))!=0;
                    int defensive=(1<<(int)GearEffect.BraceBarrier)|(1<<(int)GearEffect.Counter)|(1<<(int)GearEffect.Mend)|(1<<(int)GearEffect.Shelter)|(1<<(int)GearEffect.LifeOnKill);
                    defense|=hero==0||hero==3||hero==4||hero==5||(effects&defensive)!=0;
                }
                return (frost?"Frost chills a target for 3s. ":"")+(heavy?"Heavy weapons shatter chilled targets: +25% damage and +24 stagger. ":"")+(rune?"Runes or lightning exploit a shattered target: +30% damage. ":"")+(defense?"Protection, barriers and recovery preserve the party through announced threats.":"");
            }
        }
    }
}
