using System;
using System.Collections.Generic;

namespace ForgeEmpire
{
    public enum Station { Mine = 0, Smelter = 1, Forge = 2, Market = 3 }

    [Serializable]
    public sealed class CraftedItem
    {
        public string Name;
        public int Value;
        public int Quality;
        public int RecipeId;
        public bool Protected;
    }

    [Serializable]
    public sealed class WorkshopState
    {
        public double Gold = 40;
        public int Ore;
        public int Ingots;
        public List<CraftedItem> Inventory = new List<CraftedItem>();
        public int[] Levels = { 1, 1, 1, 1 };
        public bool[] Automated = new bool[4];
        // Owned workers finish reserved work; pause only prevents automatic starts.
        public bool[] AutomationPaused = new bool[4];
        public bool[] Running = new bool[4];
        // Normalized cycle progress (0..1), retained across upgrades and saves.
        public double[] Progress = new double[4];
        public int TotalMined;
        public int TotalCrafted;
        public int TotalSold;
        public int OrdersCompleted;
        public int Combo;
        public int BestCombo;
        public int TutorialStep;
        public long LastSavedUtc;
        public bool SoundEnabled = true;
        public bool MusicEnabled = true;
        public bool HapticsEnabled = true;
        public int TextSize;
        public bool ReducedMotion;
        public bool KeepCraftedEquipment;
        public bool PendingProtection;
        public bool ManualForge;
        public double LifetimeGold;
        public double SimulationTime;
        public int PendingQuality;
        public double LastStrikeTime = -100;
        // Base item value reserved at sale start; market bonus applies on delivery.
        public int PendingSaleValue;
        public CampaignState Campaign;
        public int SelectedRecipe;
        public int PendingRecipe;
        public GuildContract Contract;
        public int ContractsCompleted;
        public bool TrackCommission;
        public bool ReserveJourneyGear;
        public int ReservedJourneyRecipe;
    }
}
