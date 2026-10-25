using System;
using System.Collections.Generic;

namespace ForgeEmpire
{
    // Centralize economy changes so normalized state stays valid.
    public sealed partial class WorkshopSimulation
    {
        public int LastCraftedRecipe { get; private set; }
        private const int StationCount = 4;
        private const int MaxLevel = 25;
        private const int InventoryCapacity = 120;
        private const int MaxCounter = 1000000000;
        private const int MaxOrders = 500000000;
        private const int MaxItemValue = 1000000;
        private const double MaxGold = 1e15;
        private const double MaxSimulationTime = 1e12;
        private const double OfflineLimit = 8 * 60 * 60;
        private const double ClockRebaseThreshold = MaxSimulationTime - OfflineLimit;
        private const double ProgressTolerance = 1e-10;
        private const double ClockTolerance = 1e-9;
        private static readonly double[] BaseDurations = { 1.6, 2.4, 4.5, 2.0 };
        private static readonly double[] BaseUpgradeCosts = { 20, 30, 45, 35 };
        private static readonly double[] AutomationCosts = { 60, 90, 140, 110 };

        public WorkshopState State { get; }
        public int WorkshopTier { get { return State.Levels[0]+State.Levels[1]+State.Levels[2]+State.Levels[3]>=8?1:0; } }
        public int JourneyStep
        {
            get
            {
                if(State.OrdersCompleted==0)return 0;
                if(State.BestCombo<3)return 1;
                if(!Array.Exists(State.Automated,owned=>owned))return 2;
                return WorkshopTier==0?3:4;
            }
        }

        public WorkshopSimulation(WorkshopState state)
        {
            State = state ?? new WorkshopState();
            NormalizeState();
            NormalizeCampaign();
            NormalizeProgression();
        }

        public bool TryStart(Station station,bool manual=false)
        {
            if (!ValidStation(station)) return false;
            int index = (int)station;
            if (State.Running[index]) return false;

            // Reserve inputs here, never again on completion or after a reload.
            switch (station)
            {
                case Station.Smelter:
                    if (State.Ore < 2) return false;
                    State.Ore -= 2;
                    break;
                case Station.Forge:
                    if (!CanCraftSelected) return false;
                    RecipeDefinition recipe = GameContent.Recipes[State.SelectedRecipe];
                    if(!manual)
                    {
                        if(State.ReserveJourneyGear&&State.ReservedJourneyRecipe==recipe.Id)
                            foreach(var item in State.Inventory)if(item.RecipeId==recipe.Id)return false;
                        var order=ActiveCommission;
                        if(State.TrackCommission&&order!=null&&recipe.Id==order.RecipeId&&
                            (order.MinimumQuality>0||CommissionProgress>=order.Quantity))return false;
                    }
                    State.Ingots -= recipe.Ingots;
                    if (recipe.RareCost > 0) State.Campaign.Materials[recipe.Region - 1] -= recipe.RareCost;
                    State.PendingRecipe = recipe.Id;
                    State.PendingQuality = 0;
                    State.ManualForge = manual;
                    State.PendingProtection = State.KeepCraftedEquipment || State.ReserveJourneyGear&&State.ReservedJourneyRecipe==recipe.Id;
                    break;
                case Station.Market:
                    int saleIndex = NextSaleIndex;
                    if (saleIndex < 0) return false;
                    State.PendingSaleValue = State.Inventory[saleIndex].Value;
                    State.Inventory.RemoveAt(saleIndex);
                    break;
            }
            State.Progress[index] = 0;
            State.Running[index] = true;
            return true;
        }

        public bool TryUpgrade(Station station)
        {
            if (!ValidStation(station) || State.Levels[(int)station] >= MaxLevel) return false;
            double cost = GetUpgradeCost(station);
            if (State.Gold < cost) return false;
            State.Gold -= cost;
            State.Levels[(int)station]++;
            CaptureJourney();
            // Progress is normalized, so an in-flight upgrade preserves work done.
            return true;
        }

        public bool TryAutomate(Station station)
        {
            if (!ValidStation(station) || State.Automated[(int)station]) return false;
            double cost = GetAutomationCost(station);
            if (State.Gold < cost) return false;
            State.Gold -= cost;
            State.Automated[(int)station] = true;
            CaptureJourney();
            State.AutomationPaused[(int)station] = false;
            TryStart(station);
            return true;
        }

        public bool SetAutomationPaused(Station station, bool paused)
        {
            if (!ValidStation(station) || !State.Automated[(int)station]) return false;
            int index = (int)station;
            if (State.AutomationPaused[index] == paused) return false;
            State.AutomationPaused[index] = paused;
            if (!paused && !State.Running[index]) TryStart(station);
            return true;
        }

        public bool TryBeginTempering()
        {
            if(!IsRunning(Station.Forge)||State.ManualForge||State.Progress[2]>=1)return false;
            // The automatic shaping phase already paid for the piece. Hand tempering
            // starts a fresh timing window on that same reserved piece.
            State.ManualForge=true;State.Progress[2]=0;State.PendingProtection=true;return true;
        }

        public bool TryStrike(double accuracy)
        {
            if (!Finite(accuracy) || accuracy < 0 || accuracy > 1 || !IsRunning(Station.Forge)) return false;
            if (State.Progress[(int)Station.Forge] >= 1) return false;
            if (State.SimulationTime - State.LastStrikeTime + ClockTolerance < 0.2) return false;

            State.LastStrikeTime = State.SimulationTime;
            State.Progress[(int)Station.Forge] = Math.Min(1, State.Progress[(int)Station.Forge] + 0.10);
            if (accuracy >= 0.82)
            {
                State.Combo = Increment(State.Combo, 1);
                State.BestCombo = Math.Max(State.BestCombo, State.Combo);
                State.PendingQuality = Math.Min(3, State.PendingQuality + 1);
            }
            else State.Combo = 0;
            // Even at progress 1, only Advance may deliver the item.
            return true;
        }

        public void Advance(double seconds)
        {
            if (!Finite(seconds) || seconds <= 0) return;
            seconds = Math.Min(seconds, OfflineLimit);

            StartWorkers();
            double remaining = seconds;
            while (true)
            {
                // Settle simultaneous outputs first so batching cannot change input allocation.
                if (CompleteReadyCycles()) StartWorkers();
                if (remaining <= 0) break;

                double step = remaining;
                bool active = false;
                for (int i = 0; i < StationCount; i++)
                {
                    if (!State.Running[i]) continue;
                    active = true;
                    step = Math.Min(step, (1 - State.Progress[i]) * GetDuration((Station)i));
                }
                if (!active) break;

                for (int i = 0; i < StationCount; i++)
                {
                    if (State.Running[i])
                        State.Progress[i] = Math.Min(1, State.Progress[i] + step / GetDuration((Station)i));
                }
                remaining -= step;
            }
            // Battle stats are snapshotted, so production ordering cannot alter combat.
            AdvanceCampaign(seconds);
            // No wall clock access: the caller owns LastSavedUtc and supplies deltas.
            AdvanceClock(seconds);
        }

        public double ApplyOffline(double seconds)
        {
            if (!Finite(seconds) || seconds <= 0) return 0;
            double before = State.Gold;
            Advance(Math.Min(seconds, OfflineLimit));
            return State.Gold - before;
        }

        public double GetDuration(Station station)
        {
            if (!ValidStation(station)) return 0;
            int index = (int)station;
            double duration = BaseDurations[index];
            if (station == Station.Forge)
            {
                int recipe = IsRunning(Station.Forge) ? State.PendingRecipe : State.SelectedRecipe;
                duration *= 1 + GameContent.Recipes[recipe].Region * .3 + (recipe % 4 == 1 || recipe % 4 == 3 ? .1 : 0);
            }
            duration /= 1 + 0.14 * (State.Levels[index] - 1);
            return station==Station.Forge&&IsRunning(Station.Forge)&&State.ManualForge?Math.Max(6,duration):duration;
        }

        public double GetUpgradeCost(Station station)
        {
            if (!ValidStation(station)) return 0;
            int index = (int)station;
            return BaseUpgradeCosts[index] * Math.Pow(1.55, State.Levels[index] - 1);
        }

        public double GetAutomationCost(Station station)
        {
            return ValidStation(station) ? AutomationCosts[(int)station] : 0;
        }

        public double GetProgress(Station station)
        {
            return IsRunning(station) ? Clamp(State.Progress[(int)station], 0, 1) : 0;
        }

        public bool IsRunning(Station station)
        {
            return ValidStation(station) && State.Running[(int)station];
        }

        public int OrderTarget { get { return 3 + State.OrdersCompleted * 2; } }
        public double OrderReward { get { return 40.0 + State.OrdersCompleted * 25.0; } }
        public bool OrderReady { get { return State.OrdersCompleted < MaxOrders && State.TotalSold >= OrderTarget; } }

        public bool ClaimOrder()
        {
            if (!OrderReady) return false;
            AddGold(OrderReward);
            State.OrdersCompleted++;
            return true;
        }

        private void StartWorkers()
        {
            // Reserve market stock first so the forge can use freed capacity immediately.
            if (State.Automated[(int)Station.Market] && !State.AutomationPaused[(int)Station.Market]) TryStart(Station.Market);
            for (int i = 0; i < (int)Station.Market; i++)
                if (State.Automated[i] && !State.AutomationPaused[i]) TryStart((Station)i);
        }

        private bool CompleteReadyCycles()
        {
            bool completed = false;
            for (int i = 0; i < StationCount; i++)
            {
                if (!State.Running[i] || State.Progress[i] < 1 - ProgressTolerance) continue;
                State.Running[i] = false;
                State.Progress[i] = 0;
                completed = true;
                switch ((Station)i)
                {
                    case Station.Mine:
                        State.Ore = Increment(State.Ore, 2);
                        State.TotalMined = Increment(State.TotalMined, 2);
                        break;
                    case Station.Smelter:
                        State.Ingots = Increment(State.Ingots, 1);
                        break;
                    case Station.Forge:
                        // The active forge owns a reserved inventory slot.
                        RecipeDefinition recipe = GameContent.Recipes[State.PendingRecipe];
                        State.Inventory.Add(new CraftedItem
                        {
                            RecipeId = recipe.Id,
                            Name = recipe.Name,
                            Quality = State.PendingQuality,
                            Value = recipe.Value + recipe.Value * State.PendingQuality / 4,
                            Protected = State.PendingProtection
                        });
                        State.Campaign.CraftedCounts[recipe.Id] = Increment(State.Campaign.CraftedCounts[recipe.Id], 1);
                        State.Campaign.BestCraftQuality[recipe.Id]=Math.Max(State.Campaign.BestCraftQuality[recipe.Id],State.PendingQuality);
                        State.TotalCrafted = Increment(State.TotalCrafted, 1);
                        LastCraftedRecipe = recipe.Id;
                        State.PendingQuality = 0;
                        State.PendingProtection = false;
                        State.ManualForge = false;
                        State.PendingRecipe = 0;
                        break;
                    case Station.Market:
                        AddGold(State.PendingSaleValue * (1 + 0.08 * (State.Levels[i] - 1)));
                        State.PendingSaleValue = 0;
                        State.TotalSold = Increment(State.TotalSold, 1);
                        break;
                }
            }
            return completed;
        }

        private void AddGold(double amount)
        {
            State.Gold = Math.Min(MaxGold, State.Gold + amount);
            State.LifetimeGold = Math.Min(MaxGold, State.LifetimeGold + amount);
        }

        private void AdvanceClock(double seconds)
        {
            // A fixed epoch shift preserves cooldowns and batching without freezing time.
            if (seconds >= ClockRebaseThreshold - State.SimulationTime)
            {
                State.SimulationTime -= ClockRebaseThreshold;
                State.LastStrikeTime = Math.Max(-100, State.LastStrikeTime - ClockRebaseThreshold);
            }
            State.SimulationTime += seconds;
        }

        private void NormalizeState()
        {
            State.Levels = Resize(State.Levels, 1);
            State.Automated = Resize(State.Automated, false);
            State.AutomationPaused = Resize(State.AutomationPaused, false);
            State.Running = Resize(State.Running, false);
            State.Progress = Resize(State.Progress, 0.0);
            for (int i = 0; i < StationCount; i++)
            {
                State.Levels[i] = Clamp(State.Levels[i], 1, MaxLevel);
                if (!State.Automated[i]) State.AutomationPaused[i] = false;
                State.Progress[i] = State.Running[i] ? Clamp(State.Progress[i], 0, 1) : 0;
            }

            State.Gold = Clamp(State.Gold, 0, MaxGold);
            State.LifetimeGold = Clamp(State.LifetimeGold, 0, MaxGold);
            State.Ore = Clamp(State.Ore, 0, MaxCounter);
            State.Ingots = Clamp(State.Ingots, 0, MaxCounter);
            State.TotalMined = Clamp(State.TotalMined, 0, MaxCounter);
            State.TotalCrafted = Clamp(State.TotalCrafted, 0, MaxCounter);
            State.TotalSold = Clamp(State.TotalSold, 0, MaxCounter);
            State.OrdersCompleted = Clamp(State.OrdersCompleted, 0, MaxOrders);
            State.Combo = Clamp(State.Combo, 0, MaxCounter);
            State.BestCombo = Math.Max(State.Combo, Clamp(State.BestCombo, 0, MaxCounter));
            State.TutorialStep = Clamp(State.TutorialStep, 0, 1000);
            State.TextSize = Clamp(State.TextSize, 0, 2);
            State.LastSavedUtc = Math.Max(0, Math.Min(DateTime.MaxValue.Ticks, State.LastSavedUtc));
            State.SimulationTime = Clamp(State.SimulationTime, 0, MaxSimulationTime);
            if (!Finite(State.LastStrikeTime) || State.LastStrikeTime > State.SimulationTime)
                State.LastStrikeTime = -100;
            else State.LastStrikeTime = Math.Max(-100, State.LastStrikeTime);
            AdvanceClock(0);

            if (State.Inventory == null) State.Inventory = new List<CraftedItem>();
            int kept = 0;
            for (int i = 0; i < State.Inventory.Count && kept < InventoryCapacity; i++)
            {
                CraftedItem item = State.Inventory[i];
                if (item == null) continue;
                if (string.IsNullOrWhiteSpace(item.Name)) item.Name = "Iron Sword";
                else if (item.Name.Length > 64) item.Name = item.Name.Substring(0, 64);
                item.Value = Clamp(item.Value, 0, MaxItemValue);
                item.Quality = Clamp(item.Quality, 0, 3);
                State.Inventory[kept++] = item;
            }
            if (kept < State.Inventory.Count) State.Inventory.RemoveRange(kept, State.Inventory.Count - kept);

            // Reject impossible forge reservations without minting refunds.
            if (State.Inventory.Count >= InventoryCapacity)
            {
                State.Running[(int)Station.Forge] = false;
                State.Progress[(int)Station.Forge] = 0;
            }
            State.PendingQuality = IsRunning(Station.Forge) ? Clamp(State.PendingQuality, 0, 3) : 0;
            if (!IsRunning(Station.Forge)){State.PendingProtection = false;State.ManualForge=false;}
            State.PendingSaleValue = IsRunning(Station.Market) ? Clamp(State.PendingSaleValue, 0, MaxItemValue) : 0;
        }

        private static T[] Resize<T>(T[] values, T defaultValue)
        {
            if (values != null && values.Length == StationCount) return values;
            T[] result = new T[StationCount];
            for (int i = 0; i < StationCount; i++)
                result[i] = values != null && i < values.Length ? values[i] : defaultValue;
            return result;
        }

        private static bool ValidStation(Station station)
        {
            return (int)station >= 0 && (int)station < StationCount;
        }

        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static int Increment(int value, int amount)
        {
            return Math.Min(MaxCounter, value + amount);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Finite(value) ? Math.Max(minimum, Math.Min(maximum, value)) : minimum;
        }
    }
}
