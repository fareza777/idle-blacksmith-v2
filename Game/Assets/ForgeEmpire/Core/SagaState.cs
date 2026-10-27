using System;
using System.Collections.Generic;

namespace ForgeEmpire
{
    public enum SagaPhase { Choice, Combat, Camp, Result }
    public enum SagaEventKind { Windup, Hit, Skill, Heal, Block, Break, Phase, Result }
    [Serializable] public sealed class SagaCombatEvent
    {
        public int Id,Tick,SourceSlot=-1,TargetSlot=-1,EnemySlot=-1;
        public double Amount;
        public SagaEventKind Kind;
        // Enemy identifies the recipient; SourceSlot -1 identifies the enemy actor.
        public bool Enemy;
        public string Text="";
    }
    [Serializable] public sealed class SagaState
    {
        public int[] Party={0,-1,-1};
        public SagaRunState Run;
        public int[] BossMastery=new int[12];
        public int Mastery,Legacy;
    }
    [Serializable] public sealed class SagaUnitState
    {
        public int HeroId=-1,Power,Guard,Attacks,Effects,AttackTicks,WeaponRecipe=-1,ArmorRecipe=-1,ActionUntilTick;
        public int NextAttackTick,WindupTick,ImpactTick,TargetEnemy=-1;
        public double HP,MaxHP,Cooldown,Barrier,Protection=1,Damage=1,CounterSeconds;
        public string Action="ready";
    }
    [Serializable] public sealed class SagaEnemyState
    {
        public int EnemyId=-1,BossId=-1,FrostUntilTick,ChargedUntilTick;
        public double HP,MaxHP,Guard,Barrier;
        public string Role="Vanguard";
    }
    [Serializable] public sealed class SagaRunState
    {
        public int Region,Route,Modifier,Room,TotalRooms,ClearedRooms,EnemyId=-1,BossId=-1,BossPhase,Mechanic=-1;
        public int RewardGold,RewardMaterial,BonusGold,BonusMaterial,Tick,RoomTick,PulseCount,Pressure,ResponseMask,Seed;
        public int TimingVersion,EventSerial,ThreatStartTick,ThreatImpactTick,ThreatTarget=-1,NextEnemyTick,NextMechanicTick;
        public int SkillReadyTick;
        public int BreakUntilTick,BreakRecoveryUntilTick;
        public int FormationVersion,FocusEnemy,ThreatEnemy=-1,ThreatSupportTarget=-1,EnemyTurn;
        public SagaEnemyState[] Enemies=new SagaEnemyState[0];
        // The mirror detects edits made by released scalar save/fixture callers.
        public int MirrorEnemyId=-1,MirrorBossId=-1;
        public double MirrorHP,MirrorMaxHP,MirrorGuard,MirrorBarrier;
        public int PendingJourney,JourneyUsedMask,Boon=-1,BoonUntilRoom=-1;
        public int[] RoomChoices=new[]{-1,-1,-1,-1,-1};
        public string ThreatAction="strike";
        public double EnemyHP,EnemyMaxHP,EnemyBarrier,EnemyGuard,TickRemainder,SlowSeconds,DisruptSeconds,GuardSeconds;
        public double BreakMeter;
        public SagaPhase Phase;
        public SagaUnitState[] Units=new SagaUnitState[3];
        public List<SagaCombatEvent> Events=new List<SagaCombatEvent>();
        public string Telegraph="",LastEvent="The trail begins.";
        public bool Won,CampUsed,Reborn,Paused,ThreatMechanic;
    }
}
