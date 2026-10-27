using System;

namespace ForgeEmpire
{
    public enum GearSlot { Weapon, Armor, Accessory }

    [Serializable]
    public sealed class CampaignState
    {
        public int Chapter;
        public int Reputation;
        public int TownLevel;
        public int SelectedHero;
        public int ExpeditionsWon;
        public int[] Materials = new int[6];
        public int[] CraftedCounts = new int[108];
        public int[] BestCraftQuality = new int[108];
        public int[] RegionWins = new int[6];
        public int[] EliteWins = new int[6];
        public int[] BossKills = new int[6];
        public bool[] QuestClaims = new bool[36];
        public HeroState[] Heroes = new HeroState[6];
        public ExpeditionState Battle = new ExpeditionState();
        public bool[] JourneyMilestones = new bool[42];
        public int LastLostRegion, LastLostRoute, RetryLevel;
    }

    [Serializable]
    public sealed class HeroState
    {
        public bool Recruited;
        public int Level = 1;
        public int Experience;
        public CraftedItem Weapon;
        public CraftedItem Armor;
        public CraftedItem Accessory;
    }

    [Serializable]
    public sealed class ExpeditionState
    {
        public bool Active;
        public bool Finished;
        public bool Won;
        public int Region;
        public int Route;
        public int Wave;
        public int HeroIndex;
        public int EnemyKind;
        public int HealsUsed;
        public double HeroHP;
        public double HeroMaxHP;
        public double EnemyHP;
        public double EnemyMaxHP;
        public double Elapsed;
        public double TickRemainder;
        public double StrikeCooldown;
        public double GuardCooldown;
        public double HealCooldown;
        public double GuardSeconds;
        public int RewardGold;
        public int RewardMaterial;
        public int RewardExperience;
        // Public only for fields-only save serialization; battle logic owns these snapshots.
        public int SnapshotPower;
        public int SnapshotGuard;
        public int SnapshotHealth;
        public int AttackTicks;
        public double SnapshotDamage;
        public double SnapshotProtection;
        public int TotalTicks;
        public int WaveTicks;
    }
}
