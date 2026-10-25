using System;
using System.Globalization;

namespace ForgeEmpire
{
    public enum GuideAction { Mine, Smelt, Forge, Equip, ClaimQuest, Adventure, Battle, CollectLoot, Upgrade, Recipe, Hire, Commission, DeliverCommission }

    public sealed class NextStepAdvice
    {
        public GuideAction Action;
        public string Title;
        public string Detail;
        public string Button;
        public int QuestId = -1;
        public int Stage;
        public int RecipeId = -1, Region, Route, ItemIndex = -1;
        public Station Station;
    }

    public sealed class ExpeditionRewardPreview
    {
        public int Gold,Material,Experience,Reputation;
    }

    public sealed partial class WorkshopSimulation
    {
        static readonly string[] MaterialNames = { "Cinders", "Marsh glass", "Peak crystals", "Obsidian", "Star shards" };
        static readonly string[] QualityNames = { "Common", "Fine", "Superior", "Masterwork" };

        public static string MaterialName(int region) { return MaterialNames[Math.Max(0, Math.Min(4, region))]; }
        public static string QualityName(int quality) { return QualityNames[Math.Max(0, Math.Min(3, quality))]; }
        public static string Number(double value) { return value.ToString("0.#", CultureInfo.InvariantCulture); }

        public int NextSaleIndex
        {
            get
            {
                for (int i = 0; i < State.Inventory.Count; i++)
                    if (!State.Inventory[i].Protected && !IsCommissionReserved(State.Inventory[i]) && !IsJourneyReserved(State.Inventory[i])) return i;
                return -1;
            }
        }

        public bool TryProtectItem(int index, bool protect)
        {
            if (index < 0 || index >= State.Inventory.Count) return false;
            State.Inventory[index].Protected = protect;
            return true;
        }

        public bool TrySellItem(int index)
        {
            if (IsRunning(Station.Market) || index < 0 || index >= State.Inventory.Count || State.Inventory[index].Protected || IsCommissionReserved(State.Inventory[index]) || IsJourneyReserved(State.Inventory[index])) return false;
            State.PendingSaleValue = State.Inventory[index].Value;
            State.Inventory.RemoveAt(index);
            State.Progress[(int)Station.Market] = 0;
            State.Running[(int)Station.Market] = true;
            return true;
        }

        public int ItemPower(CraftedItem item) { return GearStat(item, true); }
        public int ItemGuard(CraftedItem item) { return GearStat(item, false); }
        public double SaleValue(CraftedItem item) { return item==null?0:item.Value*(1+.08*(State.Levels[(int)Station.Market]-1)); }
        public ExpeditionRewardPreview PreviewRouteRewards(int region,int route)
        {
            if(region<0||region>=5||route<0||route>2)return null;
            var preview=new ExpeditionState{Region=region,Route=route};SetRewards(preview);
            return new ExpeditionRewardPreview{Gold=preview.RewardGold,Material=preview.RewardMaterial,Experience=preview.RewardExperience,Reputation=(region+1)*(route+1)*2};
        }
        public int EquipmentDelta(int index, bool power)
        {
            if (index < 0 || index >= State.Inventory.Count) return 0;
            var item = State.Inventory[index];
            var hero = State.Campaign.Heroes[State.Campaign.SelectedHero];
            var previous = GameContent.Recipes[item.RecipeId].Slot == GearSlot.Weapon ? hero.Weapon : hero.Armor;
            return GearStat(item, power) - GearStat(previous, power);
        }

        public double NextDuration(Station station)
        {
            if (!ValidStation(station)) return 0;
            int level = State.Levels[(int)station];
            double duration=level >= MaxLevel ? GetDuration(station) : GetDuration(station) * (1 + .14 * (level - 1)) / (1 + .14 * level);
            return station==Station.Forge&&State.ManualForge?Math.Max(6,duration):duration;
        }

        public string RecipeCost(int id)
        {
            if (!ValidRecipe(id)) return "Choose a recipe";
            var recipe = GameContent.Recipes[id];
            return recipe.Ingots + " iron ingots" + (recipe.RareCost > 0 ? " + " + recipe.RareCost + " " + MaterialName(recipe.Region - 1).ToLowerInvariant() : "");
        }

        public string WorkBlocker(Station station)
        {
            if (!ValidStation(station)) return "Choose a station.";
            if (IsRunning(station)) return station == Station.Forge ? "Strike in the bright zone to improve quality." : "This job is already in progress.";
            if (station == Station.Smelter && State.Ore < 2) return "Need " + (2 - State.Ore) + " more ore. Start at the mine.";
            if (station == Station.Forge)
            {
                if (State.Inventory.Count >= InventoryCapacity) return "Storage is full. Equip, deliver or sell equipment.";
                var recipe = GameContent.Recipes[State.SelectedRecipe];
                if (State.Ingots < recipe.Ingots) return "Need " + (recipe.Ingots - State.Ingots) + " more ingots. Use the smelter.";
                if (recipe.RareCost > 0 && State.Campaign.Materials[recipe.Region - 1] < recipe.RareCost)
                    return "Need " + (recipe.RareCost - State.Campaign.Materials[recipe.Region - 1]) + " more " + MaterialName(recipe.Region - 1).ToLowerInvariant() + ". Earn them on expeditions.";
            }
            if (station == Station.Market && NextSaleIndex < 0)
                return State.Inventory.Count == 0 ? "No equipment to sell. Craft at the forge." : "All stock is kept. Unmark an item in storage to sell it.";
            return "";
        }

        public string ExpeditionBlocker(int region, int route)
        {
            if (region < 0 || region >= 5 || route < 0 || route > 2) return "Choose an available route.";
            if (State.Campaign.Battle.Active) return "An expedition is already underway.";
            if (State.Campaign.Battle.Finished) return "Collect your previous expedition result first.";
            if (region > UnlockedRegion) return "First claim: " + GameContent.Quests[(region - 1) * 3 + 2].Title + ".";
            if (State.Campaign.Heroes[State.Campaign.SelectedHero].Weapon == null && (region != 0 || route != 0)) return "Equip a weapon before taking this route.";
            return "";
        }

        public string QuestBlocker(int id)
        {
            if (id < 0 || id >= GameContent.Quests.Length) return "Quest unavailable.";
            if (State.Campaign.QuestClaims[id]) return "Completed";
            var quest = GameContent.Quests[id];
            if (quest.Region > UnlockedRegion) return "First claim: " + GameContent.Quests[(quest.Region - 1) * 3 + 2].Title;
            if (quest.Main && MainQuest != null && MainQuest.Id != id) return "First complete: " + MainQuest.Title;
            return quest.Objective;
        }

        public double ArmorMultiplier(int region)
        {
            if(region<0||region>=5)return 1;
            var hero=State.Campaign.Heroes[State.Campaign.SelectedHero];
            string element=hero.Armor==null?"Iron":GameContent.Recipes[hero.Armor.RecipeId].Element;
            return element==GameContent.Regions[region].Weakness?(State.Campaign.SelectedHero==2?.7:.8):1;
        }
        public double WeaponMultiplier(int region)
        {
            if (region < 0 || region >= 5) return 1;
            var hero = State.Campaign.Heroes[State.Campaign.SelectedHero];
            string element = hero.Weapon == null ? "Iron" : GameContent.Recipes[hero.Weapon.RecipeId].Element;
            return element == GameContent.Regions[region].Weakness ? (State.Campaign.SelectedHero == 2 ? 1.6 : 1.3) : element == ResistedElements[region] ? .75 : 1;
        }

        public NextStepAdvice NextStep
        {
            get { return ProgressionAdvice(); }
        }
    }
}
