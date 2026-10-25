using System;

namespace ForgeEmpire
{
    public enum SagaPhase { Choice, Combat, Camp, Result }
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
        public double HP,MaxHP,Cooldown,Barrier,Protection=1,Damage=1,CounterSeconds;
        public string Action="ready";
    }
    [Serializable] public sealed class SagaRunState
    {
        public int Region,Route,Modifier,Room,TotalRooms,ClearedRooms,EnemyId=-1,BossId=-1,BossPhase,Mechanic=-1;
        public int RewardGold,RewardMaterial,BonusGold,BonusMaterial,Tick,RoomTick,PulseCount,Pressure,ResponseMask,Seed;
        public double EnemyHP,EnemyMaxHP,EnemyBarrier,EnemyGuard,TickRemainder,SlowSeconds,DisruptSeconds,GuardSeconds;
        public SagaPhase Phase;
        public SagaUnitState[] Units=new SagaUnitState[3];
        public string Telegraph="",LastEvent="The trail begins.";
        public bool Won,CampUsed,Reborn;
    }
}
