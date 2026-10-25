using System;

namespace ForgeEmpire
{
    public sealed partial class WorkshopSimulation
    {
        private const double BattleTick = .1;
        private const int BattleLimitTicks = 12000;
        private static readonly int[] EnemyHealth = { 28, 70, 110, 170, 250, 340 };
        private static readonly int[] EnemyPower = { 14, 24, 36, 50, 68, 88 };
        private static readonly string[] ResistedElements = { "Flame", "Frost", "Frost", "Flame", "Void", "Storm" };
        private static readonly string[] EnemyNames = { "Cinderroot Stalker", "Mistbound Ferryman", "Icebound Sentinel", "Slag Colossus", "Void Sentry", "Sunwrought Keeper" };

        public int UnlockedRegion { get { return State.Campaign.Chapter; } }
        public int HeroPower { get { return PowerOf(State.Campaign.SelectedHero); } }
        public int HeroGuard { get { return GuardOf(State.Campaign.SelectedHero); } }
        public int HeroMaxHealth { get { return HealthOf(State.Campaign.SelectedHero); } }
        public int BattleWaveCount { get { return WaveCount(State.Campaign.Battle.Route); } }
        public string EncounterName
        {
            get
            {
                ExpeditionState b = State.Campaign.Battle;
                if(SagaInProgress)return SagaEncounterName;
                if (!b.Active && !b.Finished) return "No expedition";
                return b.Route == 2 && b.Wave == 2 ? GameContent.Regions[b.Region].BossName : (b.Route == 1 ? "Elite " : "") + EnemyNames[b.Region];
            }
        }
        public QuestDefinition MainQuest
        {
            get
            {
                for(int region=0;region<GameContent.RegionCount;region++)for(int step=0;step<3;step++)
                {int id=GameContent.MainQuestId(region,step);if(!State.Campaign.QuestClaims[id])return GameContent.Quests[id];}
                return null;
            }
        }
        public bool CanCraftSelected
        {
            get
            {
                if (IsRunning(Station.Forge) || !IsRecipeUnlocked(State.SelectedRecipe) || State.Inventory.Count >= InventoryCapacity) return false;
                RecipeDefinition recipe = GameContent.Recipes[State.SelectedRecipe];
                return State.Ingots >= recipe.Ingots && (recipe.RareCost == 0 || State.Campaign.Materials[recipe.Region - 1] >= recipe.RareCost);
            }
        }

        public bool IsRecipeUnlocked(int id)
        {
            return ValidRecipe(id) && GameContent.Recipes[id].Region <= UnlockedRegion;
        }

        public bool SelectRecipe(int id)
        {
            if (IsRunning(Station.Forge) || !IsRecipeUnlocked(id)) return false;
            State.SelectedRecipe = id;
            return true;
        }
        public bool PlanRecipe(int id)
        {
            // The in-flight job already owns PendingRecipe and its reserved materials.
            if(!IsRecipeUnlocked(id))return false;
            State.SelectedRecipe=id;return true;
        }

        public bool TryEquip(int inventoryIndex)
        {
            CampaignState c = State.Campaign;
            if (c.Battle.Active || SagaInProgress || inventoryIndex < 0 || inventoryIndex >= State.Inventory.Count) return false;
            HeroState hero = c.Heroes[c.SelectedHero];
            CraftedItem item = State.Inventory[inventoryIndex];
            if (!hero.Recruited || item == null || !ValidRecipe(item.RecipeId)) return false;
            var slot=GameContent.Recipes[item.RecipeId].Slot;
            CraftedItem previous = slot==GearSlot.Weapon?hero.Weapon:slot==GearSlot.Armor?hero.Armor:hero.Accessory;
            int resultingCount = State.Inventory.Count - 1 + (previous == null ? 0 : 1);
            if (resultingCount + (IsRunning(Station.Forge) ? 1 : 0) > InventoryCapacity) return false;
            State.Inventory.RemoveAt(inventoryIndex);
            if (previous != null) State.Inventory.Add(previous);
            if(slot==GearSlot.Weapon)hero.Weapon=item;else if(slot==GearSlot.Armor)hero.Armor=item;else hero.Accessory=item;
            CaptureJourney();
            return true;
        }

        public bool TryRecruit(int heroIndex)
        {
            if (heroIndex < 0 || heroIndex >= GameContent.HeroCount || SagaInProgress || State.Campaign.Heroes[heroIndex].Recruited) return false;
            int cost = GameContent.Heroes[heroIndex].Cost;
            if (State.Gold < cost) return false;
            State.Gold -= cost;
            State.Campaign.Heroes[heroIndex].Recruited = true;
            return true;
        }

        public bool SelectHero(int heroIndex)
        {
            if (heroIndex < 0 || heroIndex >= GameContent.HeroCount || !State.Campaign.Heroes[heroIndex].Recruited || State.Campaign.Battle.Active || SagaInProgress) return false;
            State.Campaign.SelectedHero = heroIndex;
            return true;
        }

        public bool StartExpedition(int region, int route)
        {
            if(State.ChroniclesEnabled)return StartSaga(region,route);
            CampaignState c = State.Campaign;
            if (region < 0 || region > c.Chapter || region >= GameContent.RegionCount || route < 0 || route > 2 || c.Battle.Active || c.Battle.Finished) return false;
            HeroState hero = c.Heroes[c.SelectedHero];
            if (!hero.Recruited || (hero.Weapon == null && (region != 0 || route != 0))) return false;
            ExpeditionState b = new ExpeditionState { Active = true, Region = region, Route = route, HeroIndex = c.SelectedHero, EnemyKind = region };
            c.Battle = b;
            SnapshotHero(b);
            b.HeroMaxHP = b.SnapshotHealth;
            b.HeroHP = b.HeroMaxHP;
            SpawnEnemy(b);
            SetRewards(b);
            return true;
        }

        public bool UseBattleSkill(int skill)
        {
            if(SagaInProgress)return UsePartySkill(skill);
            ExpeditionState b = State.Campaign.Battle;
            if (!b.Active || skill < 0 || skill > 2) return false;
            switch (skill)
            {
                case 0:
                    if (b.StrikeCooldown > ClockTolerance) return false;
                    b.StrikeCooldown = 4;
                    DamageEnemy(b, b.SnapshotDamage * 1.8);
                    break;
                case 1:
                    if (b.GuardCooldown > ClockTolerance) return false;
                    b.GuardCooldown = 6;
                    b.GuardSeconds = 1.8;
                    break;
                case 2:
                    if (b.HealsUsed >= 1 || b.HealCooldown > ClockTolerance || b.HeroHP >= b.HeroMaxHP) return false;
                    b.HealsUsed++;
                    b.HealCooldown = 30;
                    b.HeroHP = Math.Min(b.HeroMaxHP, b.HeroHP + b.HeroMaxHP * .4);
                    break;
            }
            return true;
        }

        public bool ClaimExpedition()
        {
            if(SagaInProgress)return ClaimSaga();
            CampaignState c = State.Campaign;
            ExpeditionState b = c.Battle;
            if (b.Active || !b.Finished) return false;
            bool win = b.Won && ValidBattleIdentity(b) && b.Wave == WaveCount(b.Route) - 1 && b.EnemyHP == 0 && b.HeroHP > 0;
            // Clearing first makes a terminal result a single-use reward, including after saves.
            c.Battle = new ExpeditionState();
            if (!win)
            {
                c.LastLostRegion=b.Region;c.LastLostRoute=b.Route;c.RetryLevel=Math.Min(30,c.Heroes[b.HeroIndex].Level+1);
                return true;
            }
            if(c.RetryLevel>0&&c.LastLostRegion==b.Region&&c.LastLostRoute==b.Route)c.RetryLevel=0;
            SetRewards(b);
            AddGold(b.RewardGold);
            c.Materials[b.Region] = Increment(c.Materials[b.Region], b.RewardMaterial);
            c.Reputation = Increment(c.Reputation, (b.Region + 1) * (b.Route + 1) * 2);
            c.ExpeditionsWon = Increment(c.ExpeditionsWon, 1);
            c.RegionWins[b.Region] = Increment(c.RegionWins[b.Region], 1);
            if (b.Route == 1) c.EliteWins[b.Region] = Increment(c.EliteWins[b.Region], 1);
            if (b.Route == 2) c.BossKills[b.Region] = Increment(c.BossKills[b.Region], 1);
            HeroState hero = c.Heroes[b.HeroIndex];
            hero.Experience = Increment(hero.Experience, b.RewardExperience);
            while (hero.Level < 30 && hero.Experience >= hero.Level * 30)
            {
                hero.Experience -= hero.Level * 30;
                hero.Level++;
            }
            if (hero.Level == 30) hero.Experience = 0;
            return true;
        }

        public bool IsQuestAvailable(int id)
        {
            if (id < 0 || id >= GameContent.QuestCount || State.Campaign.QuestClaims[id]) return false;
            QuestDefinition quest = GameContent.Quests[id];
            if (quest.Region > UnlockedRegion) return false;
            return !quest.Main || (MainQuest != null && MainQuest.Id == id);
        }

        public int QuestProgress(int id)
        {
            if (id < 0 || id >= GameContent.QuestCount) return 0;
            QuestDefinition q = GameContent.Quests[id];
            CampaignState c = State.Campaign;
            if (c.QuestClaims[id]) return q.Target;
            int progress = 0;
            switch (q.Kind)
            {
                case QuestKind.Craft:
                    foreach(var recipe in GameContent.Recipes)if(recipe.Region==q.Region)progress=Increment(progress,c.CraftedCounts[recipe.Id]);
                    break;
                case QuestKind.Expedition: progress = c.RegionWins[q.Region]; break;
                case QuestKind.Boss: progress = c.BossKills[q.Region]; break;
                case QuestKind.Rescue: progress = c.EliteWins[q.Region]; break;
                case QuestKind.OreSupply: progress = State.Ore; break;
                case QuestKind.Delivery:
                case QuestKind.QualityDelivery:
                    foreach (CraftedItem item in State.Inventory)
                        if (DeliveryMatches(item, q)) progress++;
                    break;
            }
            return Math.Min(q.Target, progress);
        }

        public bool IsQuestReady(int id)
        {
            return IsQuestAvailable(id) && QuestProgress(id) >= GameContent.Quests[id].Target;
        }

        public bool ClaimQuest(int id)
        {
            if (!IsQuestReady(id)) return false;
            QuestDefinition q = GameContent.Quests[id];
            CampaignState c = State.Campaign;
            if (q.Kind == QuestKind.OreSupply) State.Ore -= q.Target;
            if (q.Kind == QuestKind.Delivery || q.Kind == QuestKind.QualityDelivery)
            {
                int remaining = q.Target;
                for (int i = State.Inventory.Count - 1; i >= 0 && remaining > 0; i--)
                {
                    if (!DeliveryMatches(State.Inventory[i], q)) continue;
                    State.Inventory.RemoveAt(i);
                    remaining--;
                }
            }
            c.QuestClaims[id] = true;
            AddGold(q.RewardGold);
            c.Reputation = Increment(c.Reputation, q.RewardReputation);
            c.Materials[q.Region] = Increment(c.Materials[q.Region], q.RewardMaterial);
            c.TownLevel = Math.Min(GameContent.RegionCount, c.TownLevel + q.RewardTown);
            if (q.Main && q.Kind == QuestKind.Boss) c.Chapter = Math.Min(GameContent.RegionCount-1, q.Region + 1);
            CaptureJourney();
            return true;
        }

        private static bool DeliveryMatches(CraftedItem item, QuestDefinition q)
        {
            return item != null && item.RecipeId == q.RecipeId && item.Quality >= q.Quality;
        }

        private int PowerOf(int heroIndex)
        {
            HeroState h = State.Campaign.Heroes[heroIndex];
            return GameContent.Heroes[heroIndex].Power + (h.Level - 1) * 2 + GearStat(h.Weapon, true) + GearStat(h.Armor, true)+GearStat(h.Accessory,true);
        }

        private int GuardOf(int heroIndex)
        {
            HeroState h = State.Campaign.Heroes[heroIndex];
            return GameContent.Heroes[heroIndex].Guard + h.Level - 1 + GearStat(h.Weapon, false) + GearStat(h.Armor, false)+GearStat(h.Accessory,false);
        }

        private int HealthOf(int heroIndex)
        {
            return GameContent.Heroes[heroIndex].Health + (State.Campaign.Heroes[heroIndex].Level - 1) * 8;
        }

        private static int GearStat(CraftedItem item, bool power)
        {
            return ArtisanStat(item,power);
        }

        private void SnapshotHero(ExpeditionState b)
        {
            HeroState hero = State.Campaign.Heroes[b.HeroIndex];
            b.SnapshotPower = PowerOf(b.HeroIndex);
            b.SnapshotGuard = GuardOf(b.HeroIndex);
            b.SnapshotHealth = HealthOf(b.HeroIndex);
            b.AttackTicks = b.HeroIndex == 1 ? 8 : b.HeroIndex == 2 ? 12 : 10;
            string weakness = GameContent.Regions[b.Region].Weakness;
            string element = hero.Weapon == null ? "Iron" : GameContent.Recipes[hero.Weapon.RecipeId].Element;
            double multiplier = element == weakness ? (b.HeroIndex == 2 ? 1.6 : 1.3) : element == ResistedElements[b.Region] ? .75 : 1;
            b.SnapshotDamage = b.SnapshotPower * multiplier;
            string protection = hero.Armor == null ? "Iron" : GameContent.Recipes[hero.Armor.RecipeId].Element;
            b.SnapshotProtection = protection == weakness ? (b.HeroIndex == 2 ? .7 : .8) : 1;
        }

        private static int WaveCount(int route) { return route == 0 ? 2 : 3; }

        private static double EnemyMaxHealth(ExpeditionState b)
        {
            double scale = b.Route == 0 ? 1 : b.Route == 1 ? 1.25 : b.Wave == 2 ? 3 : 1.4;
            return EnemyHealth[b.Region] * scale;
        }

        private static void SpawnEnemy(ExpeditionState b)
        {
            b.EnemyKind = b.Region;
            b.EnemyMaxHP = EnemyMaxHealth(b);
            b.EnemyHP = b.EnemyMaxHP;
            b.WaveTicks = 0;
        }

        private static void SetRewards(ExpeditionState b)
        {
            b.RewardGold = (b.Region + 1) * (40 + 35 * b.Route);
            b.RewardMaterial = 4 + b.Route * 3;
            b.RewardExperience = 20 + 20 * b.Route + b.Region * (10 + b.Route * 5);
        }

        private static void FinishBattle(ExpeditionState b, bool won)
        {
            b.Active = false;
            b.Finished = true;
            b.Won = won;
            b.TickRemainder = 0;
            if (!won) b.RewardGold = b.RewardMaterial = b.RewardExperience = 0;
        }

        private static void DamageEnemy(ExpeditionState b, double damage)
        {
            b.EnemyHP = Math.Max(0, b.EnemyHP - damage);
            if (b.EnemyHP > 0) return;
            if (b.Wave == WaveCount(b.Route) - 1) FinishBattle(b, true);
            else { b.Wave++; SpawnEnemy(b); }
        }

        private void AdvanceCampaign(double seconds)
        {
            ExpeditionState b = State.Campaign.Battle;
            if (!b.Active) return;
            double accumulated = b.TickRemainder + seconds;
            int ticks = (int)Math.Floor((accumulated + ClockTolerance) / BattleTick);
            b.TickRemainder = Math.Max(0, accumulated - ticks * BattleTick);
            for (int i = 0; i < ticks && b.Active; i++)
            {
                b.TotalTicks++;
                b.WaveTicks++;
                b.Elapsed = b.TotalTicks * BattleTick;
                b.StrikeCooldown = Math.Max(0, b.StrikeCooldown - BattleTick);
                b.GuardCooldown = Math.Max(0, b.GuardCooldown - BattleTick);
                b.HealCooldown = Math.Max(0, b.HealCooldown - BattleTick);
                if (b.TotalTicks % b.AttackTicks == 0) DamageEnemy(b, b.SnapshotDamage);
                if (!b.Active) break;
                // Every third enemy attack is heavy; Elapsed % 6 exposes its telegraph.
                if (b.TotalTicks % 20 == 0)
                {
                    double scale = b.Route == 0 ? 1 : b.Route == 1 ? 1.15 : b.Wave == 2 ? 1.6 : 1.25;
                    double damage = Math.Max(2, EnemyPower[b.Region] * scale - b.SnapshotGuard * 1.1);
                    if (b.TotalTicks % 60 == 0) damage *= 1.6;
                    damage *= b.SnapshotProtection;
                    if (b.GuardSeconds > ClockTolerance) damage *= .3;
                    b.HeroHP = Math.Max(0, b.HeroHP - damage);
                }
                b.GuardSeconds = Math.Max(0, b.GuardSeconds - BattleTick);
                if (b.HeroHP <= 0 || b.TotalTicks >= BattleLimitTicks) FinishBattle(b, false);
            }
        }

        private void NormalizeCampaign()
        {
            if (State.Campaign == null) State.Campaign = new CampaignState();
            CampaignState c = State.Campaign;
            c.Materials = NormalizeCounters(c.Materials, GameContent.RegionCount);
            c.CraftedCounts = NormalizeCounters(c.CraftedCounts, GameContent.RecipeCount);
            c.RegionWins = NormalizeCounters(c.RegionWins, GameContent.RegionCount);
            c.EliteWins = NormalizeCounters(c.EliteWins, GameContent.RegionCount);
            c.BossKills = NormalizeCounters(c.BossKills, GameContent.RegionCount);
            c.QuestClaims = ResizeCampaign(c.QuestClaims, GameContent.QuestCount);
            c.Heroes = ResizeCampaign(c.Heroes, GameContent.HeroCount);
            c.Reputation = Clamp(c.Reputation, 0, MaxCounter);
            c.ExpeditionsWon = Clamp(c.ExpeditionsWon, 0, MaxCounter);
            // Chapters and restoration are consequences, not independently forgeable unlocks.
            c.Chapter = 0;
            for(int region=0;region<GameContent.RegionCount;region++)
            {bool complete=true;for(int step=0;step<3;step++)if(!c.QuestClaims[GameContent.MainQuestId(region,step)])complete=false;if(!complete)break;c.Chapter=Math.Min(GameContent.RegionCount-1,region+1); }
            c.TownLevel = 0;
            for(int i=0;i<GameContent.QuestCount;i++)if(!GameContent.Quests[i].Main&&c.QuestClaims[i])c.TownLevel+=GameContent.Quests[i].RewardTown;
            for (int i = 0; i < GameContent.HeroCount; i++)
            {
                if (c.Heroes[i] == null) c.Heroes[i] = new HeroState();
                HeroState h = c.Heroes[i];
                if (i == 0) h.Recruited = true;
                h.Level = Clamp(h.Level, 1, 30);
                h.Experience = Clamp(h.Experience, 0, h.Level == 30 ? 0 : h.Level * 30 - 1);
                h.Weapon = NormalizeGear(h.Weapon, GearSlot.Weapon);
                h.Armor = NormalizeGear(h.Armor, GearSlot.Armor);
                h.Accessory=NormalizeGear(h.Accessory,GearSlot.Accessory);
            }
            if (c.SelectedHero < 0 || c.SelectedHero >= GameContent.HeroCount || !c.Heroes[c.SelectedHero].Recruited) c.SelectedHero = 0;
            foreach (CraftedItem item in State.Inventory) if (!ValidRecipe(item.RecipeId)) item.RecipeId = 0;
            if (!IsRecipeUnlocked(State.SelectedRecipe)) State.SelectedRecipe = 0;
            if (!IsRunning(Station.Forge) || !IsRecipeUnlocked(State.PendingRecipe)) State.PendingRecipe = 0;
            NormalizeBattle();
        }

        private static CraftedItem NormalizeGear(CraftedItem item, GearSlot slot)
        {
            // JsonUtility represents null nested objects as empty field records on reload.
            if (item == null || (string.IsNullOrWhiteSpace(item.Name) && item.Value <= 0)) return null;
            if (!ValidRecipe(item.RecipeId)) item.RecipeId = 0;
            RecipeDefinition recipe = GameContent.Recipes[item.RecipeId];
            if (recipe.Slot != slot) return null;
            item.Quality = Clamp(item.Quality, 0, 3);
            item.Value = Clamp(item.Value, 0, MaxItemValue);
            if (string.IsNullOrWhiteSpace(item.Name)) item.Name = recipe.Name;
            else if (item.Name.Length > 64) item.Name = item.Name.Substring(0, 64);
            return item;
        }

        private bool ValidBattleIdentity(ExpeditionState b)
        {
            return b.Region >= 0 && b.Region < GameContent.RegionCount && b.Region <= UnlockedRegion && b.Route >= 0 && b.Route <= 2 &&
                b.HeroIndex >= 0 && b.HeroIndex < GameContent.HeroCount && State.Campaign.Heroes[b.HeroIndex].Recruited;
        }

        private void NormalizeBattle()
        {
            CampaignState c = State.Campaign;
            if(State.Saga!=null&&State.Saga.Run!=null&&State.Saga.Run.TotalRooms>0)return;
            ExpeditionState b = c.Battle;
            if (b == null || (!b.Active && !b.Finished) || !ValidBattleIdentity(b) || b.Wave < 0 || b.Wave >= WaveCount(b.Route))
            {
                c.Battle = new ExpeditionState();
                return;
            }
            // Reject impossible combatant health before clamping can turn it into a victory.
            bool invalidHealth = !Finite(b.HeroHP) || b.HeroHP <= 0 || !Finite(b.EnemyHP) || b.EnemyHP < 0 ||
                (b.Active && b.EnemyHP == 0);
            // A finished result may outlive its loadout; retain the battle's own bounded snapshot.
            HeroDefinition hero = GameContent.Heroes[b.HeroIndex];
            b.SnapshotPower = Clamp(b.SnapshotPower, hero.Power, 1000);
            b.SnapshotGuard = Clamp(b.SnapshotGuard, hero.Guard, 1000);
            b.SnapshotHealth = Clamp(b.SnapshotHealth, hero.Health, hero.Health + 232);
            b.SnapshotDamage = Clamp(b.SnapshotDamage, b.SnapshotPower * .75, b.SnapshotPower * (b.HeroIndex == 2 ? 1.6 : 1.3));
            b.SnapshotProtection = Clamp(b.SnapshotProtection, b.HeroIndex == 2 ? .7 : .8, 1);
            b.AttackTicks = b.HeroIndex == 1 ? 8 : b.HeroIndex == 2 ? 12 : 10;
            b.EnemyKind = b.Region;
            b.HeroMaxHP = b.SnapshotHealth;
            b.HeroHP = Clamp(b.HeroHP, 0, b.HeroMaxHP);
            b.EnemyMaxHP = EnemyMaxHealth(b);
            b.EnemyHP = Clamp(b.EnemyHP, 0, b.EnemyMaxHP);
            b.TotalTicks = Clamp(b.TotalTicks, 0, BattleLimitTicks);
            b.WaveTicks = Clamp(b.WaveTicks, 0, b.TotalTicks);
            b.Elapsed = b.TotalTicks * BattleTick;
            b.TickRemainder = Clamp(b.TickRemainder, 0, BattleTick - ClockTolerance);
            b.HealsUsed = Clamp(b.HealsUsed, 0, 1);
            b.StrikeCooldown = Clamp(b.StrikeCooldown, 0, 4);
            b.GuardCooldown = Clamp(b.GuardCooldown, 0, 6);
            b.HealCooldown = Clamp(b.HealCooldown, 0, 30);
            b.GuardSeconds = Clamp(b.GuardSeconds, 0, 1.8);
            SetRewards(b);
            if (invalidHealth) FinishBattle(b, false);
            else if (b.Finished)
            {
                FinishBattle(b, b.Won && b.HeroHP > 0 && b.EnemyHP == 0 && b.Wave == WaveCount(b.Route) - 1);
            }
            else
            {
                b.Won = false;
                if (b.TotalTicks >= BattleLimitTicks) FinishBattle(b, false);
            }
        }

        private static bool ValidRecipe(int id) { return id >= 0 && id < GameContent.RecipeCount; }

        private static T[] ResizeCampaign<T>(T[] values, int length)
        {
            if (values != null && values.Length == length) return values;
            T[] result = new T[length];
            if (values != null) Array.Copy(values, result, Math.Min(length, values.Length));
            return result;
        }

        private static int[] NormalizeCounters(int[] values, int length)
        {
            values = ResizeCampaign(values, length);
            for (int i = 0; i < length; i++) values[i] = Clamp(values[i], 0, MaxCounter);
            return values;
        }
    }
}
