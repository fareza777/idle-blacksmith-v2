using System;
using System.Collections.Generic;

namespace ForgeEmpire
{
    public sealed partial class WorkshopSimulation
    {
        const int SagaTimingVersion=1;
        const int SagaArrivalTicks=8;
        const int SagaWindupTicks=4;
        const int SagaEnemyWindupTicks=12;
        const int SagaMechanicWindupTicks=30;
        static int SagaAttackCadence(int hero){return new[]{22,19,26,24,17,25}[hero];}
        public bool CanUseSagaSkill(int slot)
        {
            if(!SagaInProgress||slot<0||slot>2)return false;var r=State.Saga.Run;var u=r.Units[slot];
            return r.Phase==SagaPhase.Combat&&!r.Paused&&r.RoomTick>=r.SkillReadyTick&&r.RoomTick>=SagaArrivalTicks&&u.HeroId>=0&&u.HP>0&&u.Cooldown<=ClockTolerance;
        }
        public bool SetSagaPaused(bool paused)
        {
            if(!SagaInProgress||State.Saga.Run.Phase!=SagaPhase.Combat||State.Saga.Run.Paused==paused)return false;
            State.Saga.Run.Paused=paused;PublishSagaBattle();return true;
        }
        public string SagaIntent
        {
            get
            {
                if(!SagaInProgress)return "";var r=State.Saga.Run;
                if(r.Phase!=SagaPhase.Combat)return SagaNextAction;
                if(r.Paused)return "Paused · Plan your next move.";
                if(r.BreakUntilTick>r.RoomTick)return "Staggered · Strike while the enemy is vulnerable!";
                if(r.ThreatImpactTick>r.RoomTick)return r.Telegraph;
                return r.RoomTick<SagaArrivalTicks?"The party takes position.":"The enemy recovers. Ready your next response.";
            }
        }
        public double SagaThreatProgress
        {
            get
            {
                if(!SagaInProgress)return 0;var r=State.Saga.Run;
                return r.Phase!=SagaPhase.Combat||r.ThreatImpactTick<=r.RoomTick?0:Clamp((double)(r.RoomTick-r.ThreatStartTick)/Math.Max(1,r.ThreatImpactTick-r.ThreatStartTick),0,1);
            }
        }
        public int SagaThreatTarget { get { return !SagaInProgress||State.Saga.Run.ThreatImpactTick<=State.Saga.Run.RoomTick?-1:State.Saga.Run.ThreatTarget; } }
        public double SagaBreakProgress
        {
            get
            {
                if(!SagaInProgress)return 0;var r=State.Saga.Run;
                return r.BreakUntilTick>r.RoomTick?1:Clamp(r.BreakMeter/100,0,1);
            }
        }
        void SagaEvent(SagaEventKind kind,int source,int target,double amount,bool enemy,string text)
        {
            var r=State.Saga.Run;
            r.EventSerial++;
            r.Events.Add(new SagaCombatEvent{Id=r.EventSerial,Tick=r.Tick,SourceSlot=source,TargetSlot=target,Amount=amount,Kind=kind,Enemy=enemy,Text=text});
            if(r.Events.Count>24)r.Events.RemoveRange(0,r.Events.Count-24);
        }
        int SagaSlot(SagaUnitState unit){return Array.IndexOf(State.Saga.Run.Units,unit);}
        int SagaHeroSlot(int hero){for(int i=0;i<3;i++)if(State.Saga.Run.Units[i].HeroId==hero)return i;return -1;}
        void ClearSagaPendingActions()
        {
            var r=State.Saga.Run;r.ThreatStartTick=0;r.ThreatImpactTick=0;r.ThreatTarget=-1;r.ThreatMechanic=false;r.ResponseMask=0;r.Telegraph="";
            foreach(var unit in r.Units)unit.ImpactTick=0;
        }
        void HealSagaUnit(SagaUnitState unit,double amount,int source,string text)
        {
            double healed=Math.Min(unit.MaxHP-unit.HP,amount);unit.HP+=healed;
            if(healed>ClockTolerance)SagaEvent(SagaEventKind.Heal,source,SagaSlot(unit),healed,false,text);
        }
        void StartSagaCadence(bool migrating=false)
        {
            var r=State.Saga.Run;r.TimingVersion=SagaTimingVersion;
            r.ThreatStartTick=0;r.ThreatImpactTick=0;r.ThreatTarget=-1;r.ThreatMechanic=false;r.Telegraph="";
            r.NextEnemyTick=r.RoomTick+16;r.NextMechanicTick=r.BossId>=0?r.RoomTick+30:0;
            r.SkillReadyTick=r.RoomTick+SagaArrivalTicks;
            r.BreakMeter=0;r.BreakUntilTick=0;r.BreakRecoveryUntilTick=0;r.ResponseMask=0;
            for(int slot=0;slot<3;slot++)
            {
                var unit=r.Units[slot];unit.NextAttackTick=r.RoomTick+SagaArrivalTicks+slot*5;unit.WindupTick=0;unit.ImpactTick=0;
                if(migrating&&unit.HeroId>=0)
                {
                    unit.AttackTicks=SagaAttackCadence(unit.HeroId);
                    if(HasEffect(unit,GearEffect.Haste))unit.AttackTicks=Math.Max(14,(int)Math.Round(unit.AttackTicks*.85));
                    unit.Action=unit.HP>0?"ready":"defeat";unit.ActionUntilTick=r.RoomTick;
                }
            }
        }
        void NormalizeSagaCadence()
        {
            var r=State.Saga.Run;
            r.Paused=r.Paused&&r.Phase==SagaPhase.Combat;
            if(r.Events==null)r.Events=new List<SagaCombatEvent>();
            int previous=0,lastTick=-1;var events=new List<SagaCombatEvent>();
            for(int index=Math.Max(0,r.Events.Count-24);index<r.Events.Count;index++)
            {
                var e=r.Events[index];
                if(e==null||e.Id<=previous||e.Id>MaxCounter||e.Tick<lastTick||e.Tick<0||e.Tick>r.Tick||e.SourceSlot< -1||e.SourceSlot>2||e.TargetSlot< -1||e.TargetSlot>2||
                    (int)e.Kind<0||(int)e.Kind>(int)SagaEventKind.Result||!Finite(e.Amount)||e.Amount<0)continue;
                e.Amount=Clamp(e.Amount,0,60000);if(e.Text==null)e.Text="";if(e.Text.Length>200)e.Text=e.Text.Substring(0,200);
                events.Add(e);previous=e.Id;lastTick=e.Tick;
            }
            if(events.Count>24)events.RemoveRange(0,events.Count-24);r.Events=events;
            r.EventSerial=Math.Max(previous,Clamp(r.EventSerial,0,MaxCounter));
            if(r.TimingVersion!=SagaTimingVersion){StartSagaCadence(true);return;}
            r.BreakMeter=Clamp(r.BreakMeter,0,100);r.BreakUntilTick=Clamp(r.BreakUntilTick,0,r.RoomTick+20);
            r.SkillReadyTick=Clamp(r.SkillReadyTick,0,r.RoomTick+SagaArrivalTicks);
            r.BreakRecoveryUntilTick=Clamp(r.BreakRecoveryUntilTick,r.BreakUntilTick,r.RoomTick+70);
            if(r.BreakRecoveryUntilTick>r.RoomTick)r.BreakMeter=0;
            r.NextEnemyTick=Clamp(r.NextEnemyTick,0,r.RoomTick+80);r.NextMechanicTick=r.BossId<0?0:Clamp(r.NextMechanicTick,0,r.RoomTick+120);
            bool threat=r.Phase==SagaPhase.Combat&&r.ThreatImpactTick>r.RoomTick&&r.ThreatImpactTick<=r.RoomTick+30&&r.ThreatStartTick>=0&&r.ThreatStartTick<=r.RoomTick&&
                r.ThreatImpactTick-r.ThreatStartTick==(r.ThreatMechanic?SagaMechanicWindupTicks:SagaEnemyWindupTicks)&&r.ThreatTarget>=-1&&r.ThreatTarget<3&&
                (r.ThreatTarget<0)==(r.ThreatMechanic&&SagaMechanicHitsParty(r.Mechanic))&&
                (r.ThreatTarget<0||r.Units[r.ThreatTarget].HeroId>=0&&r.Units[r.ThreatTarget].HP>0)&&(!r.ThreatMechanic||r.BossId>=0);
            if(!threat)
            {
                bool interrupted=r.ThreatImpactTick!=0;
                r.ThreatImpactTick=0;r.ThreatStartTick=0;r.ThreatTarget=-1;r.ThreatMechanic=false;r.Telegraph="";r.ResponseMask=0;
                if(interrupted)r.NextEnemyTick=Math.Max(r.NextEnemyTick,r.RoomTick+SagaEnemyWindupTicks);
            }
            else UpdateSagaTelegraph();
            foreach(var u in r.Units)
            {
                u.AttackTicks=Clamp(u.AttackTicks,14,30);u.NextAttackTick=Clamp(u.NextAttackTick,0,r.RoomTick+60);
                // Two other reserved contacts may extend the four-tick windup by up to ten ticks.
                bool pending=u.HeroId>=0&&u.HP>0&&u.ImpactTick>r.RoomTick&&u.ImpactTick<=r.RoomTick+14&&u.WindupTick<=r.RoomTick&&u.WindupTick>=0;
                if(!pending)u.ImpactTick=0;
                u.WindupTick=Clamp(u.WindupTick,0,r.RoomTick);
                u.ActionUntilTick=Clamp(u.ActionUntilTick,0,r.RoomTick+14);
            }
        }
        void BuildSagaBreak(double amount)
        {
            var r=State.Saga.Run;if(r.Phase!=SagaPhase.Combat||r.RoomTick<r.BreakRecoveryUntilTick)return;
            r.BreakMeter=Math.Min(100,r.BreakMeter+amount);if(r.BreakMeter<100)return;
            r.BreakMeter=0;r.BreakUntilTick=r.RoomTick+20;r.BreakRecoveryUntilTick=r.RoomTick+70;
            // A broken windup is canceled. Its replacement must be announced afresh.
            if(r.ThreatMechanic)r.NextMechanicTick=r.BreakUntilTick+8;
            r.ThreatImpactTick=0;r.ThreatStartTick=0;r.ThreatTarget=-1;r.ThreatMechanic=false;r.ResponseMask=0;r.Telegraph="";
            r.NextEnemyTick=r.BreakUntilTick+12;
            r.LastEvent="Stagger! The enemy is vulnerable for 2 seconds.";
            SagaEvent(SagaEventKind.Break,-1,-1,0,true,r.LastEvent);
        }
        void AdvanceSagaHeroes()
        {
            var r=State.Saga.Run;
            for(int slot=0;slot<3&&r.Phase==SagaPhase.Combat;slot++)
            {
                var u=r.Units[slot];if(u.HeroId<0||u.HP<=0){u.ImpactTick=0;continue;}
                u.Cooldown=Math.Max(0,u.Cooldown-.1);u.CounterSeconds=Math.Max(0,u.CounterSeconds-.1);
                if(r.RoomTick>=u.ActionUntilTick)u.Action="ready";
                if(u.ImpactTick>0&&r.RoomTick>=u.ImpactTick)
                {
                    u.ImpactTick=0;u.Action=u.HeroId==2?"cast":"attack";u.ActionUntilTick=r.RoomTick+5;
                    HitSagaEnemy(u,1);continue;
                }
                if(u.ImpactTick>0||r.RoomTick<u.NextAttackTick)continue;
                u.WindupTick=r.RoomTick;int impact=r.RoomTick+SagaWindupTicks;
                bool conflict;do
                {
                    conflict=false;foreach(var other in r.Units)if(other!=u&&other.ImpactTick>0&&Math.Abs(other.ImpactTick-impact)<3){impact=other.ImpactTick+3;conflict=true;break;}
                }while(conflict);
                u.ImpactTick=impact;u.NextAttackTick=impact+u.AttackTicks-SagaWindupTicks;
                u.Action="windup";u.ActionUntilTick=impact;
                SagaEvent(SagaEventKind.Windup,slot,-1,0,true,GameContent.Heroes[u.HeroId].Name+" prepares a strike.");
            }
        }
        bool SagaMechanicHitsParty(int mechanic){return mechanic!=0&&mechanic!=4&&mechanic!=5;}
        int SelectSagaTarget()
        {
            var r=State.Saga.Run;int target=-1;
            for(int i=0;i<3;i++)if(r.Units[i].HeroId>=0&&r.Units[i].HP>0&&(target<0||r.Units[i].HeroId==0))target=i;
            int archetype=r.EnemyId<0?-1:ChronicleContent.Enemies[r.EnemyId].Archetype;
            if(archetype==0||archetype==7)for(int i=0;i<3;i++)if(r.Units[i].HeroId>=0&&r.Units[i].HP>0&&r.Units[i].HP/r.Units[i].MaxHP<r.Units[target].HP/r.Units[target].MaxHP)target=i;
            return target;
        }
        void UpdateSagaTelegraph()
        {
            var r=State.Saga.Run;string attack;
            if(r.ThreatMechanic)attack=r.Mechanic==11?(r.PulseCount%2==0?"Violet seal · Use Focus before it closes.":"Golden seal · Protect the party before impact."):ChronicleContent.Bosses[r.BossId].Telegraph;
            else attack=r.BossId>=0?"The guardian prepares a strike.":"Enemy strike incoming.";
            r.Telegraph=attack+" · "+(r.ThreatTarget<0?"Whole party":GameContent.Heroes[r.Units[r.ThreatTarget].HeroId].Name);
        }
        void BeginSagaThreat(bool mechanic)
        {
            var r=State.Saga.Run;r.ThreatMechanic=mechanic;r.ThreatStartTick=r.RoomTick;
            r.ThreatImpactTick=r.RoomTick+(mechanic?SagaMechanicWindupTicks:SagaEnemyWindupTicks);
            r.ThreatTarget=mechanic&&SagaMechanicHitsParty(r.Mechanic)?-1:SelectSagaTarget();
            if(mechanic)r.ResponseMask=0;
            UpdateSagaTelegraph();SagaEvent(SagaEventKind.Windup,-1,r.ThreatTarget,0,false,r.Telegraph);
        }
        void AdvanceSagaThreat()
        {
            var r=State.Saga.Run;if(r.BreakUntilTick>r.RoomTick)return;
            if(r.ThreatImpactTick>0)
            {
                if(r.RoomTick<r.ThreatImpactTick)return;
                bool mechanic=r.ThreatMechanic;int target=r.ThreatTarget;
                EnemySagaAttack(mechanic,target);
                if(mechanic){r.PulseCount++;r.ResponseMask=0;r.NextMechanicTick=r.RoomTick+50;}
                int cadence=r.BossId>=0?42:ChronicleContent.Enemies[r.EnemyId].AttackTicks+18;
                if(r.EnemyId>=0&&ChronicleContent.Enemies[r.EnemyId].Archetype==5&&r.EnemyHP<r.EnemyMaxHP*.5)cadence=Math.Max(30,cadence-6);
                if(r.SlowSeconds>0)cadence+=4;
                r.NextEnemyTick=r.RoomTick+cadence-SagaEnemyWindupTicks;
                r.ThreatStartTick=0;r.ThreatImpactTick=0;r.ThreatTarget=-1;r.ThreatMechanic=false;r.Telegraph="";
                return;
            }
            if(r.BossId>=0&&r.RoomTick>=r.NextMechanicTick){BeginSagaThreat(true);return;}
            if(r.RoomTick<r.NextEnemyTick)return;
            // Keep one announced impact at a time; reserve the full guardian window.
            if(r.BossId>=0&&r.RoomTick+SagaEnemyWindupTicks>=r.NextMechanicTick)return;
            BeginSagaThreat(false);
        }
    }
}
