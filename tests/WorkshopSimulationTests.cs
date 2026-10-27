using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using ForgeEmpire;

internal static class WorkshopSimulationTests
{
    private static int passed;
    private static int failed;
    private static readonly JsonSerializerOptions SaveOptions = new JsonSerializerOptions { IncludeFields = true };

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception(message + ": expected " + expected + ", got " + actual);
    }

    private static void Near(double expected, double actual, string message, double tolerance = 0.000001)
    {
        if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(expected - actual) > tolerance)
            throw new Exception(message + ": expected " + expected + ", got " + actual);
    }

    private static void Test(string name, Action body)
    {
        try { body(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + " -- " + error.Message); }
    }

    private static CraftedItem Sword(int value = 24, int quality = 0)
    {
        return new CraftedItem { Name = "Iron Sword", Value = value, Quality = quality };
    }

    private static WorkshopSimulation New()
    {
        return new WorkshopSimulation(new WorkshopState());
    }

    private static WorkshopSimulation Workers(bool upgraded = false)
    {
        WorkshopState state = new WorkshopState { Gold = 10000 };
        if (upgraded) state.Levels = new int[] { 25, 13, 22, 19 };
        WorkshopSimulation sim = new WorkshopSimulation(state);
        for (int i = 0; i < 4; i++) Check(sim.TryAutomate((Station)i), "purchase worker " + i);
        return sim;
    }

    private static WorkshopSimulation RoundTrip(WorkshopSimulation simulation)
    {
        string json = JsonSerializer.Serialize(simulation.State, SaveOptions);
        return new WorkshopSimulation(JsonSerializer.Deserialize<WorkshopState>(json, SaveOptions));
    }

    // Reflection lets the regression suite report missing public APIs as RED assertions.
    internal static bool[] Paused(WorkshopState state)
    {
        FieldInfo field = typeof(WorkshopState).GetField("AutomationPaused");
        Check(field != null && field.FieldType == typeof(bool[]) && !field.IsInitOnly && !field.IsStatic,
            "AutomationPaused must be a public mutable bool[] field");
        return (bool[])field.GetValue(state);
    }

    internal static bool SetAutomationPaused(WorkshopSimulation sim, Station station, bool paused)
    {
        MethodInfo method = typeof(WorkshopSimulation).GetMethod("SetAutomationPaused", new[] { typeof(Station), typeof(bool) });
        Check(method != null && method.ReturnType == typeof(bool) && !method.IsStatic,
            "missing public bool SetAutomationPaused(Station, bool)");
        return (bool)method.Invoke(sim, new object[] { station, paused });
    }

    private static void SameState(WorkshopSimulation a, WorkshopSimulation b)
    {
        WorkshopState x = a.State;
        WorkshopState y = b.State;
        Near(x.Gold, y.Gold, "gold", 0.001);
        Near(x.LifetimeGold, y.LifetimeGold, "lifetime gold", 0.001);
        Near(x.SimulationTime, y.SimulationTime, "simulation time", 0.00001);
        Near(x.LastStrikeTime, y.LastStrikeTime, "last strike");
        Equal(x.Ore, y.Ore, "ore");
        Equal(x.Ingots, y.Ingots, "ingots");
        Equal(x.TotalMined, y.TotalMined, "mined");
        Equal(x.TotalCrafted, y.TotalCrafted, "crafted");
        Equal(x.TotalSold, y.TotalSold, "sold");
        Equal(x.PendingSaleValue, y.PendingSaleValue, "reserved sale");
        Equal(x.PendingQuality, y.PendingQuality, "quality");
        Equal(x.Combo, y.Combo, "combo");
        Equal(x.BestCombo, y.BestCombo, "best combo");
        Equal(x.OrdersCompleted, y.OrdersCompleted, "orders");
        Equal(x.Inventory.Count, y.Inventory.Count, "inventory count");
        for (int i = 0; i < 4; i++)
        {
            Equal(x.Levels[i], y.Levels[i], "level " + i);
            Equal(x.Automated[i], y.Automated[i], "worker " + i);
            Equal(a.IsRunning((Station)i), b.IsRunning((Station)i), "running " + i);
            Near(a.GetProgress((Station)i), b.GetProgress((Station)i), "progress " + i, 0.00001);
        }
        for (int i = 0; i < x.Inventory.Count; i++)
        {
            Equal(x.Inventory[i].Name, y.Inventory[i].Name, "item name");
            Equal(x.Inventory[i].Value, y.Inventory[i].Value, "item value");
            Equal(x.Inventory[i].Quality, y.Inventory[i].Quality, "item quality");
        }
    }

    private static void Conservation(WorkshopSimulation sim)
    {
        WorkshopState s = sim.State;
        int smelted = s.Ingots + 2 * (s.TotalCrafted + (sim.IsRunning(Station.Forge) ? 1 : 0));
        Equal(s.TotalMined, s.Ore + 2 * (smelted + (sim.IsRunning(Station.Smelter) ? 1 : 0)), "ore conservation");
        Equal(s.TotalCrafted, s.Inventory.Count + s.TotalSold + (sim.IsRunning(Station.Market) ? 1 : 0), "sword conservation");
        Check(s.Gold >= 0 && s.Ore >= 0 && s.Ingots >= 0, "nonnegative economy");
        Check(s.Inventory.Count <= 120, "inventory cap");
        for (int i = 0; i < 4; i++)
            Check(sim.GetProgress((Station)i) >= 0 && sim.GetProgress((Station)i) <= 1, "normalized progress");
    }

    public static int Main()
    {
        Test("workshop journey advances through earned milestones and survives reload", delegate
        {
            var sim = new WorkshopSimulation(new WorkshopState { Gold = 1000, Ingots = 2, TotalSold = 3 });
            Equal(0, sim.JourneyStep, "commission must be claimed");
            Equal(0, sim.WorkshopTier, "starting building");
            Check(sim.ClaimOrder(), "claim first commission");
            Equal(1, sim.JourneyStep, "masterwork next");
            Check(sim.TryStart(Station.Forge), "forge started");
            for (int i = 0; i < 3; i++) { sim.Advance(.21); Check(sim.TryStrike(1), "perfect hit"); }
            Equal(2, sim.JourneyStep, "crew next");
            Check(sim.TryAutomate(Station.Mine), "hire miner");
            Equal(3, sim.JourneyStep, "expansion next");
            for (int i = 0; i < 3; i++) Check(sim.TryUpgrade((Station)i), "upgrade station");
            Equal(0, sim.WorkshopTier, "three upgrades do not expand the building");
            Check(sim.TryUpgrade(Station.Market), "fourth upgrade");
            Equal(1, sim.WorkshopTier, "building expands after four upgrades");
            Equal(4, sim.JourneyStep, "journey complete");
            var restored = RoundTrip(sim);
            Equal(4, restored.JourneyStep, "earned milestone survives save");
            Equal(1, restored.WorkshopTier, "building survives save");
        });

        Test("later achievements never hide the first unfinished journey milestone", delegate
        {
            var sim = new WorkshopSimulation(new WorkshopState { Levels = new[] { 2, 2, 2, 2 }, Automated = new[] { true, false, false, false }, BestCombo = 3 });
            Equal(0, sim.JourneyStep, "commission still required");
            sim.State.OrdersCompleted = 1;
            Equal(4, sim.JourneyStep, "earlier earned achievements count");
            sim.State.Combo = 0;
            Equal(4, sim.JourneyStep, "a missed strike does not revoke best combo achievement");
            sim.State.BestCombo = 2;
            Equal(1, sim.JourneyStep, "unfinished combo takes precedence");
        });

        Test("defaults and public serializable contract", delegate
        {
            WorkshopSimulation sim = New();
            Equal(40.0, sim.State.Gold, "starting gold");
            Equal(4, sim.State.Levels.Length, "level slots");
            Check(sim.State.SoundEnabled && sim.State.MusicEnabled && sim.State.HapticsEnabled, "settings defaults");
            Check(Attribute.IsDefined(typeof(WorkshopState), typeof(SerializableAttribute)), "state serializable");
            Check(Attribute.IsDefined(typeof(CraftedItem), typeof(SerializableAttribute)), "item serializable");
            FieldInfo sale = typeof(WorkshopState).GetField("PendingSaleValue");
            Check(sale != null && sale.FieldType == typeof(int), "sale value is a public int field");
            Near(1.6, sim.GetDuration(Station.Mine), "mine duration");
            Near(2.4, sim.GetDuration(Station.Smelter), "smelter duration");
            Near(4.5, sim.GetDuration(Station.Forge), "forge duration");
            Near(2, sim.GetDuration(Station.Market), "market duration");
        });

        Test("manual full supply chain earns 24 gold", delegate
        {
            WorkshopSimulation sim = New();
            for (int i = 0; i < 2; i++)
            {
                Check(sim.TryStart(Station.Mine), "start mining"); sim.Advance(1.6);
                Check(sim.TryStart(Station.Smelter), "start smelting"); sim.Advance(2.4);
            }
            Equal(4, sim.State.TotalMined, "units mined");
            Equal(2, sim.State.Ingots, "two ingots");
            Check(sim.TryStart(Station.Forge), "start forge"); sim.Advance(4.5);
            Equal(1, sim.State.TotalCrafted, "crafted");
            Equal("Iron Sword", sim.State.Inventory[0].Name, "item name");
            Equal(24, sim.State.Inventory[0].Value, "base value");
            Check(sim.TryStart(Station.Market), "start market"); sim.Advance(2);
            Near(64, sim.State.Gold, "sale gold"); Near(24, sim.State.LifetimeGold, "earned only");
            Equal(1, sim.State.TotalSold, "sold"); Conservation(sim);
        });

        Test("manual mine completes once without auto restart", delegate
        {
            WorkshopSimulation sim = New();
            Check(sim.TryStart(Station.Mine), "start"); sim.Advance(100);
            Equal(2, sim.State.Ore, "one yield");
            Check(!sim.IsRunning(Station.Mine), "idle after completion");
            Near(0, sim.GetProgress(Station.Mine), "idle progress");
        });

        Test("smelter reserves input once and rejects double click", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Ore = 4 });
            Check(sim.TryStart(Station.Smelter), "start"); Equal(2, sim.State.Ore, "reserved now");
            Check(!sim.TryStart(Station.Smelter), "second start denied"); Equal(2, sim.State.Ore, "no second debit");
            sim.Advance(2.4); Equal(1, sim.State.Ingots, "one ingot"); Equal(2, sim.State.Ore, "no completion debit");
        });

        Test("forge reserves two ingots once", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Ingots = 4 });
            Check(sim.TryStart(Station.Forge), "start"); Equal(2, sim.State.Ingots, "reserved");
            Check(!sim.TryStart(Station.Forge), "double start denied"); sim.Advance(4.5);
            Equal(2, sim.State.Ingots, "no extra debit"); Equal(1, sim.State.Inventory.Count, "one sword");
        });

        Test("mine double click cannot duplicate cycles", delegate
        {
            WorkshopSimulation sim = New(); Check(sim.TryStart(Station.Mine), "start");
            Check(!sim.TryStart(Station.Mine), "second start denied"); sim.Advance(1.6);
            Equal(2, sim.State.Ore, "one output");
        });

        Test("insufficient materials leave state untouched", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Ore = 1, Ingots = 1 });
            Check(!sim.TryStart(Station.Smelter), "ore shortage");
            Check(!sim.TryStart(Station.Forge), "ingot shortage");
            Check(!sim.TryStart(Station.Market), "inventory shortage");
            Equal(1, sim.State.Ore, "ore untouched"); Equal(1, sim.State.Ingots, "ingots untouched");
            Check(sim.TryStart(Station.Mine), "free mine still works");
        });

        Test("market removes oldest item on start and reserves value", delegate
        {
            WorkshopState s = new WorkshopState(); s.Inventory.Add(Sword(42, 3)); s.Inventory.Add(Sword());
            WorkshopSimulation sim = new WorkshopSimulation(s);
            Check(sim.TryStart(Station.Market), "start"); Equal(1, s.Inventory.Count, "removed immediately");
            Equal(24, s.Inventory[0].Value, "oldest first"); Equal(42, s.PendingSaleValue, "value reserved");
            Check(!sim.TryStart(Station.Market), "double sell denied"); Near(40, s.Gold, "not paid yet");
            sim.Advance(2); Near(82, s.Gold, "correct payout"); Equal(0, s.PendingSaleValue, "cleared");
            sim.Advance(20); Near(82, s.Gold, "no duplicate payout");
        });

        Test("partial pending sale survives fields-only JSON round trip", delegate
        {
            WorkshopState s = new WorkshopState(); s.Inventory.Add(Sword(36, 2));
            WorkshopSimulation sim = new WorkshopSimulation(s);
            Check(sim.TryStart(Station.Market), "start"); sim.Advance(0.75);
            WorkshopSimulation loaded = RoundTrip(sim);
            Equal(36, loaded.State.PendingSaleValue, "pending value saved");
            Equal(0, loaded.State.Inventory.Count, "no reinserted item");
            loaded.Advance(1.25); Near(76, loaded.State.Gold, "restored payout");
            Equal(1, loaded.State.TotalSold, "one sale");
            RoundTrip(loaded).Advance(100); Near(76, loaded.State.Gold, "original save independent");
            WorkshopSimulation again = RoundTrip(loaded); again.Advance(100); Near(76, again.State.Gold, "no replay after reload");
        });

        Test("partial smelt and forge survive reload without input re-reservation", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Ore = 2, Ingots = 2 });
            Check(sim.TryStart(Station.Smelter), "smelt"); Check(sim.TryStart(Station.Forge), "forge");
            sim.Advance(1); WorkshopSimulation loaded = RoundTrip(sim); loaded.Advance(3.5);
            Equal(0, loaded.State.Ore, "ore reserved once"); Equal(1, loaded.State.Ingots, "ingots reserved once");
            Equal(1, loaded.State.Inventory.Count, "sword restored");
        });

        Test("upgrade prices use station base and exponential curve", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Gold = 1000 });
            double[] costs = { 20, 30, 45, 35 };
            for (int i = 0; i < 4; i++)
            {
                Near(costs[i], sim.GetUpgradeCost((Station)i), "initial price");
                Check(sim.TryUpgrade((Station)i), "upgrade");
                Near(costs[i] * 1.55, sim.GetUpgradeCost((Station)i), "second price");
            }
            Near(870, sim.State.Gold, "charged once each"); Near(0, sim.State.LifetimeGold, "spending not income");
        });

        Test("upgrade speeds cycle while mine yield stays two", delegate
        {
            WorkshopSimulation sim = New(); Check(sim.TryUpgrade(Station.Mine), "upgrade");
            Near(1.6 / 1.14, sim.GetDuration(Station.Mine), "faster duration");
            Check(sim.TryStart(Station.Mine), "start"); sim.Advance(sim.GetDuration(Station.Mine));
            Equal(2, sim.State.Ore, "constant yield");
        });

        Test("mid-cycle upgrade preserves normalized progress", delegate
        {
            WorkshopSimulation sim = New(); Check(sim.TryStart(Station.Mine), "start"); sim.Advance(0.8);
            Near(0.5, sim.GetProgress(Station.Mine), "half complete");
            Check(sim.TryUpgrade(Station.Mine), "upgrade"); Near(0.5, sim.GetProgress(Station.Mine), "progress retained");
            sim.Advance(sim.GetDuration(Station.Mine) * 0.5); Equal(2, sim.State.Ore, "remaining half uses new speed");
        });

        Test("upgrade caps at level 25", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Gold = 100000000 });
            for (int i = 1; i < 25; i++) Check(sim.TryUpgrade(Station.Forge), "upgrade " + i);
            Equal(25, sim.State.Levels[2], "maximum level"); double gold = sim.State.Gold;
            Check(!sim.TryUpgrade(Station.Forge), "max upgrade denied"); Near(gold, sim.State.Gold, "no charge at cap");
            Near(4.5 / 4.36, sim.GetDuration(Station.Forge), "max speed");
        });

        Test("insufficient funds and exact balance never overdraft", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Gold = 20 });
            Check(!sim.TryUpgrade(Station.Forge), "unaffordable upgrade");
            Check(!sim.TryAutomate(Station.Mine), "unaffordable worker");
            Check(sim.TryUpgrade(Station.Mine), "exact balance upgrade"); Near(0, sim.State.Gold, "zero balance");
            Check(!sim.TryUpgrade(Station.Mine), "no overdraft"); Near(0, sim.State.Gold, "still zero");
        });

        Test("market level multiplier applies to item quality value", delegate
        {
            WorkshopState s = new WorkshopState { Gold = 100 }; s.Inventory.Add(Sword(42, 3));
            WorkshopSimulation sim = new WorkshopSimulation(s); Check(sim.TryUpgrade(Station.Market), "upgrade");
            Check(sim.TryStart(Station.Market), "start"); sim.Advance(sim.GetDuration(Station.Market));
            Near(65 + 42 * 1.08, s.Gold, "quality plus market bonus"); Near(42 * 1.08, s.LifetimeGold, "gross earnings");
        });

        Test("workers cost fixed amounts and cannot be purchased twice", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Gold = 1000 });
            double[] costs = { 60, 90, 140, 110 };
            for (int i = 0; i < 4; i++)
            {
                Near(costs[i], sim.GetAutomationCost((Station)i), "worker price");
                Check(sim.TryAutomate((Station)i), "buy worker");
                Check(!sim.TryAutomate((Station)i), "once only");
                Check(sim.State.Automated[i], "worker flag");
            }
            Near(600, sim.State.Gold, "total charge");
        });

        Test("automation pause API validates ownership and never charges or rehires", delegate
        {
            bool[] defaults = Paused(new WorkshopState());
            Equal(4, defaults.Length, "four default pause slots");
            foreach (bool paused in defaults) Check(!paused, "default automation is not paused");
            WorkshopSimulation fresh = New();
            string before = JsonSerializer.Serialize(fresh.State, SaveOptions);
            foreach (Station station in new[] { (Station)(-1), (Station)4, (Station)int.MaxValue, Station.Mine, Station.Smelter, Station.Forge, Station.Market })
            foreach (bool paused in new[] { true, false })
            {
                Check(!SetAutomationPaused(fresh, station, paused), "invalid or unowned worker rejected");
                Equal(before, JsonSerializer.Serialize(fresh.State, SaveOptions), "rejected setting is atomic");
            }
            double[] costs = { 60, 90, 140, 110 };
            for (int i = 0; i < 4; i++)
            {
                WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Gold = 1000 });
                Station station = (Station)i;
                Paused(sim.State)[i] = true;
                Check(sim.TryAutomate(station), "hire worker once");
                Check(!Paused(sim.State)[i], "new hire clears stale pause");
                Near(1000 - costs[i], sim.State.Gold, "only hiring costs gold");
                Check(!SetAutomationPaused(sim, station, false), "unchanged resume returns false");
                Check(SetAutomationPaused(sim, station, true), "pause owned worker");
                before = JsonSerializer.Serialize(sim.State, SaveOptions);
                Check(!SetAutomationPaused(sim, station, true) && !sim.TryAutomate(station), "no duplicate pause or hire");
                Equal(before, JsonSerializer.Serialize(sim.State, SaveOptions), "no-op leaves entire state untouched");
                Check(sim.State.Automated[i] && SetAutomationPaused(sim, station, false), "resume retains ownership");
                before = JsonSerializer.Serialize(sim.State, SaveOptions);
                Check(!SetAutomationPaused(sim, station, false) && !sim.TryAutomate(station), "no duplicate resume or hire");
                Equal(before, JsonSerializer.Serialize(sim.State, SaveOptions), "resume no-op is atomic");
                Near(1000 - costs[i], sim.State.Gold, "pause and resume are free");
            }
        });

        foreach (Station station in new[] { Station.Mine, Station.Smelter, Station.Forge })
        Test("paused " + station + " finishes reserved work without automatic restart", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Gold = 1000, Ore = 6, Ingots = 6 });
            Check(sim.TryAutomate(station), "hire and reserve cycle");
            double duration = sim.GetDuration(station);
            sim.Advance(duration * .25);
            if (station == Station.Forge) Check(sim.TryStrike(1), "retain pending quality");
            double progress = sim.GetProgress(station), gold = sim.State.Gold;
            int ore = sim.State.Ore, ingots = sim.State.Ingots, quality = sim.State.PendingQuality;
            Check(SetAutomationPaused(sim, station, true), "pause active worker");
            Check(sim.IsRunning(station) && sim.State.Automated[(int)station], "cycle and ownership retained");
            Near(progress, sim.GetProgress(station), "progress not cancelled");
            Equal(ore, sim.State.Ore, "no ore refund"); Equal(ingots, sim.State.Ingots, "no ingot refund");
            Equal(quality, sim.State.PendingQuality, "pending quality retained");
            sim = RoundTrip(sim);
            Check(Paused(sim.State)[(int)station] && sim.IsRunning(station), "active pause survives reload");
            Near(progress, sim.GetProgress(station), "reload retains progress");
            sim.ApplyOffline(duration * 10);
            Check(!sim.IsRunning(station), "only the reserved cycle completes offline");
            Equal(station == Station.Mine ? 8 : station == Station.Smelter ? 4 : 6, sim.State.Ore, "one cycle ore balance");
            Equal(station == Station.Smelter ? 7 : station == Station.Forge ? 4 : 6, sim.State.Ingots, "one cycle ingot balance");
            Equal(station == Station.Forge ? 1 : 0, sim.State.Inventory.Count, "one cycle inventory balance");
            if (station == Station.Forge) Equal(1, sim.State.Inventory[0].Quality, "reserved quality delivered");
            Check(sim.TryStart(station), "manual start works while paused");
            sim.Advance(duration * 10);
            Check(!sim.IsRunning(station) && Paused(sim.State)[(int)station], "manual completion does not resume worker");
            Equal(station == Station.Mine ? 10 : station == Station.Smelter ? 2 : 6, sim.State.Ore, "two cycles ore balance");
            Equal(station == Station.Smelter ? 8 : station == Station.Forge ? 2 : 6, sim.State.Ingots, "two cycles ingot balance");
            Equal(station == Station.Forge ? 2 : 0, sim.State.Inventory.Count, "two cycles inventory balance");
            Check(SetAutomationPaused(sim, station, false) && sim.IsRunning(station), "idle resume starts immediately");
            Check(!sim.TryAutomate(station), "paused worker was not unowned");
            Near(gold, sim.State.Gold, "no pause, resume, or rehire debit");
        });

        Test("automation pause normalizes missing short and orphaned flags and persists settings", delegate
        {
            Paused(new WorkshopState());
            FieldInfo field = typeof(WorkshopState).GetField("AutomationPaused");
            bool[][] saved = { null, new bool[0], new[] { true, true }, new[] { true, true, true, true, true } };
            bool[][] expected = { new bool[4], new bool[4], new[] { true, false, false, false }, new[] { true, false, true, true } };
            for (int n = 0; n < saved.Length; n++)
            {
                WorkshopState state = new WorkshopState { Automated = new[] { true, false, true, true } };
                field.SetValue(state, saved[n]);
                WorkshopSimulation sim = new WorkshopSimulation(state);
                Equal(4, Paused(sim.State).Length, "normalized pause length");
                WorkshopSimulation loaded = RoundTrip(sim);
                for (int i = 0; i < 4; i++)
                {
                    Equal(expected[n][i], Paused(sim.State)[i], "valid owned setting preserved");
                    Equal(expected[n][i], Paused(loaded.State)[i], "fields-only save preserves pause");
                }
                loaded.Advance(.1);
                Equal(!expected[n][0], loaded.IsRunning(Station.Mine), "loaded pause controls automatic start");
            }
            foreach (bool[] workers in new bool[][] { null, new[] { true } })
            {
                WorkshopState state = new WorkshopState { Automated = workers };
                field.SetValue(state, new[] { true, true, true, true });
                WorkshopSimulation sim = new WorkshopSimulation(state);
                for (int i = 0; i < 4; i++)
                    Equal(i == 0 && workers != null, Paused(sim.State)[i], "only an owned worker can retain pause");
            }
        });

        Test("mine worker starts and restarts at exact cycle boundaries", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Gold = 60 });
            Check(sim.TryAutomate(Station.Mine), "worker"); Check(sim.IsRunning(Station.Mine), "starts immediately");
            sim.Advance(4.8); Equal(6, sim.State.Ore, "three cycles");
            Check(sim.IsRunning(Station.Mine), "restarted at boundary"); Near(0, sim.GetProgress(Station.Mine), "fresh cycle");
        });

        Test("starved smelter retries when mine completes", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Gold = 500 });
            Check(sim.TryAutomate(Station.Smelter), "worker"); Check(!sim.IsRunning(Station.Smelter), "starved");
            sim.Advance(10); Check(sim.TryStart(Station.Mine), "manual mine"); sim.Advance(1.6);
            Check(sim.IsRunning(Station.Smelter), "retry at production event"); Equal(0, sim.State.Ore, "reserved once");
            sim.Advance(2.4); Equal(1, sim.State.Ingots, "finished"); Check(!sim.IsRunning(Station.Smelter), "starved again");
        });

        Test("all workers form unattended supply chain", delegate
        {
            WorkshopSimulation sim = Workers(); sim.Advance(60);
            Check(sim.State.TotalSold > 5, "multiple automated sales");
            Near(9600 + sim.State.TotalSold * 24, sim.State.Gold, "all sales accounted"); Conservation(sim);
        });

        Test("strike requires active forge and finite in-range accuracy", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Ingots = 2 });
            Check(!sim.TryStrike(1), "inactive strike denied"); Check(sim.TryStart(Station.Forge), "start");
            double[] bad = { -0.01, 1.01, double.NaN, double.PositiveInfinity, double.NegativeInfinity };
            foreach (double value in bad) Check(!sim.TryStrike(value), "invalid accuracy");
            Near(0, sim.GetProgress(Station.Forge), "invalid strikes do not advance");
            Check(sim.TryStrike(1), "valid strike after rejects"); Near(0.1, sim.GetProgress(Station.Forge), "ten percent");
        });

        Test("strike cooldown uses simulation time at 0.2 seconds", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Ingots = 2 });
            Check(sim.TryStart(Station.Forge), "start"); Check(sim.TryStrike(0.82), "first hit");
            Check(!sim.TryStrike(1), "immediate hit denied"); sim.Advance(0.199);
            Check(!sim.TryStrike(1), "before cooldown denied"); sim.Advance(0.001);
            Check(sim.TryStrike(1), "at cooldown allowed"); Equal(2, sim.State.Combo, "only accepted hits count");
        });

        Test("perfect strike quality caps at three and combo survives completion", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Ingots = 4 });
            Check(sim.TryStart(Station.Forge), "start");
            for (int i = 0; i < 4; i++) { Check(sim.TryStrike(0.82), "perfect"); sim.Advance(0.2); }
            Equal(3, sim.State.PendingQuality, "quality cap"); Equal(4, sim.State.Combo, "combo");
            sim.Advance(4.5); Equal(42, sim.State.Inventory[0].Value, "three-tier price");
            Equal(3, sim.State.Inventory[0].Quality, "item quality"); Equal(0, sim.State.PendingQuality, "quality reset");
            Equal(4, sim.State.Combo, "combo retained"); Equal(4, sim.State.BestCombo, "best combo");
            Check(sim.TryStart(Station.Forge), "next sword"); Check(sim.TryStrike(0.81), "miss accepted");
            Equal(0, sim.State.Combo, "miss resets combo"); Equal(4, sim.State.BestCombo, "best retained");
            sim.Advance(4.5); Equal(24, sim.State.Inventory[1].Value, "no quality leak");
        });

        Test("each quality tier adds exactly 25 percent", delegate
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Ingots = 2 });
                Check(sim.TryStart(Station.Forge), "start");
                for (int i = 0; i < tier; i++) { Check(sim.TryStrike(1), "hit"); sim.Advance(0.2); }
                Check(sim.TryStrike(0), "miss does not erase earned quality"); sim.Advance(4.5);
                Equal(24 + 6 * tier, sim.State.Inventory[0].Value, "tier value");
                Equal(tier, sim.State.Inventory[0].Quality, "tier");
            }
        });

        Test("strike clamps progress but completion occurs only in Advance", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Ingots = 2 });
            Check(sim.TryStart(Station.Forge), "start"); sim.Advance(4.4);
            Check(sim.TryStrike(1), "finishing hit"); Near(1, sim.GetProgress(Station.Forge), "clamped progress");
            Equal(0, sim.State.Inventory.Count, "not completed by strike"); Check(sim.IsRunning(Station.Forge), "still active");
            WorkshopSimulation loaded = RoundTrip(sim); loaded.Advance(0.001);
            Equal(1, loaded.State.Inventory.Count, "completes once in Advance");
            loaded.Advance(100); Equal(1, loaded.State.TotalCrafted, "no duplicate completion");
        });

        Test("strike cooldown and pending quality survive reload", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Ingots = 2 });
            Check(sim.TryStart(Station.Forge), "start"); Check(sim.TryStrike(1), "hit");
            WorkshopSimulation loaded = RoundTrip(sim); Check(!loaded.TryStrike(1), "cooldown retained");
            loaded.Advance(0.2); Check(loaded.TryStrike(1), "cooldown expires");
            loaded.Advance(4.5); Equal(36, loaded.State.Inventory[0].Value, "saved quality");
        });

        Test("saturated clock permits repeated strikes and reload keeps cooldown", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { Ingots = 2, SimulationTime = 1e12 });
            Check(sim.TryStart(Station.Forge), "start"); Check(sim.TryStrike(1), "first saturated-clock hit");
            Check(!sim.TryStrike(1), "immediate hit denied");
            sim.Advance(0.2); Check(sim.TryStrike(1), "second hit after 0.2 seconds");
            WorkshopSimulation loaded = RoundTrip(sim); SameState(sim, loaded);
            Check(!loaded.TryStrike(1), "reload cannot bypass cooldown"); loaded.Advance(0.199);
            Check(!loaded.TryStrike(1), "reload retains full cooldown"); loaded.Advance(0.001);
            Check(loaded.TryStrike(1), "reloaded cooldown expires exactly"); Equal(3, loaded.State.Combo, "accepted hits only");
        });

        Test("constructor rebases near-cap clocks without erasing pending cooldown", delegate
        {
            foreach (double clock in new double[] { 1e12 - 1, 1e12 })
            foreach (double elapsed in new double[] { 0, 0.125 })
            {
                WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState
                {
                    Ingots = 2, SimulationTime = clock, LastStrikeTime = clock - elapsed
                });
                Check(sim.State.SimulationTime < 1e12 - 28800, "constructor leaves clock headroom");
                Near(elapsed, sim.State.SimulationTime - sim.State.LastStrikeTime, "elapsed cooldown preserved", 1e-9);
                Check(sim.TryStart(Station.Forge), "start"); Check(!sim.TryStrike(1), "saved cooldown retained");
                sim = RoundTrip(sim); sim.Advance(0.199 - elapsed);
                Check(!sim.TryStrike(1), "not yet 0.2 seconds"); sim = RoundTrip(sim); sim.Advance(0.001);
                Check(sim.TryStrike(1), "remaining cooldown expires after reload");
            }
        });

        Test("advancing to the old clock cap cannot freeze future strikes", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState
            {
                Ingots = 2, SimulationTime = 1e12 - 28801
            });
            sim.Advance(28800); sim.Advance(1);
            Check(sim.TryStart(Station.Forge), "start"); Check(sim.TryStrike(1), "first hit at old cap");
            sim.Advance(0.2); Check(sim.TryStrike(1), "clock still advances after old cap");
            WorkshopSimulation loaded = RoundTrip(sim); SameState(sim, loaded);
            Check(!loaded.TryStrike(1), "saved hit still cools down");
        });

        Test("clock rebase preserves live cooldown and batch frame equivalence", delegate
        {
            WorkshopSimulation batch = new WorkshopSimulation(new WorkshopState
            {
                Gold = 1000, Ingots = 2, TotalMined = 4, SimulationTime = 1e12 - 28800 - 0.125
            });
            for (int i = 0; i < 4; i++) Check(batch.TryAutomate((Station)i), "worker");
            Check(batch.TryStrike(1), "hit before rebase"); WorkshopSimulation frames = RoundTrip(batch);
            batch.Advance(0.125); frames.Advance(0.0625); frames = RoundTrip(frames); frames.Advance(0.0625);
            SameState(batch, frames);
            Check(batch.State.SimulationTime < 1e12 - 28800, "advance rebases near cap");
            Near(0.125, batch.State.SimulationTime - batch.State.LastStrikeTime, "live cooldown retained", 1e-9);
            batch = RoundTrip(batch); frames = RoundTrip(frames);
            Check(!batch.TryStrike(1) && !frames.TryStrike(1), "rebase does not refresh cooldown");
            batch.Advance(0.074); frames.Advance(0.037); frames.Advance(0.037);
            Check(!batch.TryStrike(1) && !frames.TryStrike(1), "early hit denied after rebase");
            batch.Advance(0.001); frames.Advance(0.0005); frames.Advance(0.0005);
            Check(batch.TryStrike(1) && frames.TryStrike(1), "both cooldowns expire at 0.2 seconds");
            batch.Advance(120);
            for (int i = 0; i < 7200; i++) frames.Advance(1.0 / 60);
            SameState(batch, frames); Conservation(batch); Conservation(frames);
        });

        Test("invalid and backward time inputs are exact no-ops", delegate
        {
            WorkshopSimulation sim = Workers(); sim.Advance(8.25);
            string before = JsonSerializer.Serialize(sim.State, SaveOptions);
            double[] invalid = { 0, -1, -3600, double.NaN, double.PositiveInfinity, double.NegativeInfinity };
            foreach (double seconds in invalid)
            {
                sim.Advance(seconds); Near(0, sim.ApplyOffline(seconds), "no offline gold");
                Equal(before, JsonSerializer.Serialize(sim.State, SaveOptions), "no clock or economy change");
            }
            long saved = sim.State.LastSavedUtc; sim.ApplyOffline(1);
            Equal(saved, sim.State.LastSavedUtc, "caller owns wall clock stamp");
        });

        Test("oversized finite advances match eight-hour automation within bounded runtime", delegate
        {
            foreach (double seconds in new double[] { 28801, 1e20, double.MaxValue })
            {
                // Fail safely before exercising the old unbounded worker loop.
                WorkshopSimulation idle = New(); idle.Advance(seconds);
                Near(28800, idle.State.SimulationTime, "idle elapsed is capped before automation");
                WorkshopSimulation expected = Workers(true); expected.Advance(11.137);
                WorkshopSimulation actual = RoundTrip(expected); expected.Advance(28800);
                Stopwatch timer = Stopwatch.StartNew(); actual.Advance(seconds); timer.Stop();
                Check(timer.Elapsed.TotalSeconds < 10, "oversized advance took " + timer.Elapsed.TotalSeconds + "s");
                SameState(expected, actual); Conservation(actual);
                Console.WriteLine("INFO capped advance " + seconds + ": " + timer.Elapsed.TotalMilliseconds.ToString("F1") + "ms");
            }
        });

        Test("advance limit applies per call rather than to lifetime elapsed", delegate
        {
            WorkshopSimulation sim = New(); long saved = sim.State.LastSavedUtc;
            sim.Advance(1e20); Near(28800, sim.State.SimulationTime, "first capped call");
            sim.Advance(1e20); Near(57600, sim.State.SimulationTime, "second capped call");
            sim.Advance(0.2); Near(57600.2, sim.State.SimulationTime, "normal deltas still accumulate");
            Equal(saved, sim.State.LastSavedUtc, "caller retains wall clock ownership");
        });

        Test("offline completes only the existing manual cycle", delegate
        {
            WorkshopSimulation sim = New(); Check(sim.TryStart(Station.Mine), "start"); sim.Advance(0.8);
            Near(0, sim.ApplyOffline(28800), "no gold from manual mine"); Equal(2, sim.State.Ore, "one manual cycle");
            Check(!sim.IsRunning(Station.Mine), "does not invent workers");
        });

        Test("offline caps at eight hours and returns actual gold delta", delegate
        {
            WorkshopSimulation expected = Workers(true); WorkshopSimulation actual = Workers(true);
            expected.Advance(28800); double before = actual.State.Gold;
            double delta = actual.ApplyOffline(864000); Near(actual.State.Gold - before, delta, "offline delta");
            SameState(expected, actual); Conservation(actual);
        });

        Test("offline preserves partial cycles and matches online advance", delegate
        {
            WorkshopSimulation online = Workers(); online.Advance(11.137);
            WorkshopSimulation offline = RoundTrip(online);
            online.Advance(123.456); offline.ApplyOffline(123.456); SameState(online, offline); Conservation(offline);
        });

        Test("frame subdivision matches batch including exact boundaries", delegate
        {
            WorkshopSimulation batch = Workers(); WorkshopSimulation frames = Workers();
            batch.Advance(120);
            for (int i = 0; i < 7200; i++) frames.Advance(1.0 / 60);
            SameState(batch, frames); Conservation(batch); Conservation(frames);
        });

        Test("eight-hour frame subdivision matches offline batch", delegate
        {
            WorkshopSimulation batch = Workers(); WorkshopSimulation frames = Workers();
            batch.ApplyOffline(28800);
            for (int i = 0; i < 28800 * 60; i++) frames.Advance(1.0 / 60);
            SameState(batch, frames); Conservation(batch); Conservation(frames);
        });

        Test("mixed upgraded random subdivisions and reloads match batch", delegate
        {
            WorkshopSimulation batch = Workers(true); WorkshopSimulation frames = Workers(true);
            batch.Advance(777.777); Random random = new Random(1701); double remaining = 777.777; int steps = 0;
            while (remaining > 0)
            {
                double dt = Math.Min(remaining, 0.001 + random.NextDouble() * 2);
                frames.Advance(dt); remaining -= dt;
                Conservation(frames);
                if (++steps % 31 == 0) frames = RoundTrip(frames);
            }
            SameState(batch, frames);
        });

        Test("eight-hour max-level automation has bounded runtime and conserves materials", delegate
        {
            WorkshopState s = new WorkshopState { Levels = new int[] { 25, 25, 25, 25 }, Gold = 1000 };
            WorkshopSimulation sim = new WorkshopSimulation(s);
            for (int i = 0; i < 4; i++) Check(sim.TryAutomate((Station)i), "worker");
            Stopwatch timer = Stopwatch.StartNew(); sim.ApplyOffline(28800); timer.Stop();
            Check(timer.Elapsed.TotalSeconds < 10, "eight hours took " + timer.Elapsed.TotalSeconds + "s");
            Check(s.TotalSold > 20000, "actually simulated many cycles"); Conservation(sim);
            Console.WriteLine("INFO eight-hour max-level run: " + timer.Elapsed.TotalMilliseconds.ToString("F1") + "ms");
        });

        Test("commission readiness uses cumulative sales and prevents same target replay", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { TotalSold = 2 });
            Equal(3, sim.OrderTarget, "first target"); Near(40, sim.OrderReward, "first reward");
            Check(!sim.OrderReady && !sim.ClaimOrder(), "not ready"); sim.State.TotalSold = 3;
            Check(sim.OrderReady && sim.ClaimOrder(), "claim first"); Near(80, sim.State.Gold, "first payout");
            Equal(1, sim.State.OrdersCompleted, "count"); Equal(5, sim.OrderTarget, "next target");
            Near(65, sim.OrderReward, "next reward"); Check(!sim.ClaimOrder(), "cannot reclaim");
            WorkshopSimulation loaded = RoundTrip(sim); Check(!loaded.ClaimOrder(), "cannot replay after save");
            loaded.State.TotalSold = 5; Check(loaded.ClaimOrder(), "second target");
            Near(145, loaded.State.Gold, "cumulative payouts"); Near(105, loaded.State.LifetimeGold, "reward earnings");
            Equal(5, loaded.State.TotalSold, "sales are never consumed");
        });

        Test("commission backlog pays each distinct target exactly once", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(new WorkshopState { TotalSold = 7 });
            Check(sim.ClaimOrder() && sim.ClaimOrder() && sim.ClaimOrder(), "three eligible targets");
            Check(!sim.ClaimOrder(), "fourth target unavailable"); Equal(9, sim.OrderTarget, "fourth target");
            Near(40 + 40 + 65 + 90, sim.State.Gold, "unique payouts");
        });

        Test("inventory cap blocks forge without consuming ingots", delegate
        {
            WorkshopState s = new WorkshopState { Ingots = 4 };
            for (int i = 0; i < 120; i++) s.Inventory.Add(Sword());
            WorkshopSimulation sim = new WorkshopSimulation(s); Check(!sim.TryStart(Station.Forge), "full inventory");
            Equal(4, s.Ingots, "no reservation"); Check(sim.TryStart(Station.Market), "sell frees a slot immediately");
            Check(sim.TryStart(Station.Forge), "slot now available"); sim.Advance(4.5);
            Equal(120, s.Inventory.Count, "never overflow");
        });

        Test("full-inventory worker retries after market frees a slot", delegate
        {
            WorkshopState s = new WorkshopState { Gold = 1000, Ingots = 6 };
            for (int i = 0; i < 120; i++) s.Inventory.Add(Sword());
            WorkshopSimulation sim = new WorkshopSimulation(s);
            Check(sim.TryAutomate(Station.Forge), "forge worker"); Check(!sim.IsRunning(Station.Forge), "blocked");
            Check(sim.TryAutomate(Station.Market), "market worker"); sim.Advance(0.1);
            Check(sim.IsRunning(Station.Forge), "forge retried"); sim.Advance(20);
            Check(s.Inventory.Count <= 120, "capacity invariant"); Equal(3, s.TotalCrafted, "all ingots used exactly once");
        });

        Test("null state becomes a valid fresh workshop", delegate
        {
            WorkshopSimulation sim = new WorkshopSimulation(null);
            Check(sim.State != null, "null repaired"); Near(40, sim.State.Gold, "new defaults");
            Check(sim.TryStart(Station.Mine), "fresh state usable");
        });

        Test("malformed arrays are resized and bounded while preserving valid entries", delegate
        {
            WorkshopState s = new WorkshopState
            {
                Levels = new int[] { 0, 99, 3 }, Automated = new bool[] { true },
                Running = new bool[] { true, false, false, false, true },
                Progress = new double[] { 0.5, double.NaN, -5, double.PositiveInfinity, 99 }
            };
            WorkshopSimulation sim = new WorkshopSimulation(s);
            Check(object.ReferenceEquals(s, sim.State), "state object retained");
            Equal(4, s.Levels.Length, "levels resized"); Equal(1, s.Levels[0], "level minimum");
            Equal(25, s.Levels[1], "level maximum"); Equal(3, s.Levels[2], "valid level"); Equal(1, s.Levels[3], "missing level");
            Equal(4, s.Automated.Length, "automation resized"); Check(s.Automated[0], "worker retained");
            Equal(4, s.Running.Length, "running resized"); Equal(4, s.Progress.Length, "progress resized");
            Near(0.5, sim.GetProgress(Station.Mine), "valid active progress retained");
            for (int i = 1; i < 4; i++) Near(0, sim.GetProgress((Station)i), "invalid idle progress repaired");
            sim.Advance(0.8); Equal(2, s.Ore, "repaired state runs");
        });

        Test("null arrays and inventory are initialized safely", delegate
        {
            WorkshopState s = new WorkshopState { Levels = null, Automated = null, Running = null, Progress = null, Inventory = null };
            WorkshopSimulation sim = new WorkshopSimulation(s);
            Equal(4, s.Levels.Length, "levels"); Equal(4, s.Running.Length, "running");
            Equal(4, s.Automated.Length, "automated"); Equal(4, s.Progress.Length, "progress");
            Equal(0, s.Inventory.Count, "inventory"); Check(sim.TryStart(Station.Mine), "usable");
        });

        Test("malformed floats counters and orphaned pending data are normalized", delegate
        {
            WorkshopState s = new WorkshopState
            {
                Gold = double.NaN, LifetimeGold = double.PositiveInfinity, Ore = -5, Ingots = -7,
                TotalMined = -1, TotalCrafted = -2, TotalSold = -3, OrdersCompleted = -4,
                Combo = -5, BestCombo = -6, TutorialStep = -7, LastSavedUtc = -8,
                SimulationTime = double.NegativeInfinity, LastStrikeTime = double.NaN,
                PendingQuality = 99, PendingSaleValue = -40,
                SoundEnabled = false, MusicEnabled = false, HapticsEnabled = false
            };
            WorkshopSimulation sim = new WorkshopSimulation(s);
            Near(0, s.Gold, "bad gold"); Near(0, s.LifetimeGold, "bad lifetime"); Near(0, s.SimulationTime, "bad clock");
            Near(-100, s.LastStrikeTime, "bad strike clock"); Equal(0, s.Ore, "ore"); Equal(0, s.Ingots, "ingots");
            Equal(0, s.TotalMined, "mined"); Equal(0, s.TotalCrafted, "crafted"); Equal(0, s.TotalSold, "sold");
            Equal(0, s.OrdersCompleted, "orders"); Equal(0, s.Combo, "combo"); Equal(0, s.BestCombo, "best");
            Equal(0, s.TutorialStep, "tutorial"); Equal(0L, s.LastSavedUtc, "save time");
            Equal(0, s.PendingQuality, "orphaned quality"); Equal(0, s.PendingSaleValue, "orphaned sale");
            Check(!s.SoundEnabled && !s.MusicEnabled && !s.HapticsEnabled, "preferences preserved");
            Check(sim.TryStart(Station.Mine), "repaired state usable");
        });

        Test("extreme save numbers stay bounded without counter overflow", delegate
        {
            WorkshopState s = new WorkshopState
            {
                Gold = double.MaxValue, LifetimeGold = double.MaxValue, Ore = int.MaxValue, Ingots = int.MaxValue,
                TotalMined = int.MaxValue, TotalCrafted = int.MaxValue, TotalSold = int.MaxValue,
                OrdersCompleted = int.MaxValue, Combo = int.MaxValue, BestCombo = 1,
                SimulationTime = double.MaxValue, LastStrikeTime = double.MaxValue,
                TutorialStep = int.MaxValue, LastSavedUtc = long.MaxValue
            };
            WorkshopSimulation sim = new WorkshopSimulation(s);
            Check(s.Gold <= 1e15 && s.LifetimeGold <= 1e15, "bounded money");
            Check(s.Ore <= 1000000000 && s.Ingots <= 1000000000, "bounded resources");
            Check(s.SimulationTime <= 1e12 && s.TutorialStep <= 1000, "bounded metadata");
            Check(s.LastSavedUtc <= DateTime.MaxValue.Ticks, "bounded save stamp");
            Check(s.BestCombo >= s.Combo, "best combo repaired");
            Check(sim.OrderTarget > 0 && sim.OrderReward >= 0, "commission arithmetic safe");
            Check(!sim.OrderReady && !sim.ClaimOrder(), "saturated order cannot be replayed");
            Check(sim.TryStart(Station.Mine), "mine still operable"); sim.Advance(2);
            Check(s.Ore >= 0 && s.TotalMined >= 0, "saturating counters");
        });

        Test("inventory save normalization bounds length items names and values", delegate
        {
            WorkshopState s = new WorkshopState(); s.Inventory.Add(null);
            s.Inventory.Add(new CraftedItem { Name = null, Value = -100, Quality = -1 });
            s.Inventory.Add(new CraftedItem { Name = new string('x', 500), Value = int.MaxValue, Quality = 99 });
            for (int i = 0; i < 200; i++) s.Inventory.Add(Sword());
            WorkshopSimulation sim = new WorkshopSimulation(s); Equal(120, s.Inventory.Count, "save cap");
            foreach (CraftedItem item in s.Inventory)
            {
                Check(item != null && !string.IsNullOrEmpty(item.Name) && item.Name.Length <= 64, "valid item/name");
                Check(item.Value >= 0 && item.Value <= 1000000 && item.Quality >= 0 && item.Quality <= 3, "bounded item");
            }
            Check(sim.TryStart(Station.Market), "normalized sale usable"); sim.Advance(2); Check(s.Gold >= 40, "no negative sale");
        });

        Test("saved completed progress settles exactly once and stale idle progress clears", delegate
        {
            WorkshopState s = new WorkshopState
            {
                Running = new bool[] { true, false, true, false },
                Progress = new double[] { 9, 0.75, 1, 0.5 }, PendingQuality = 99, PendingSaleValue = 100
            };
            WorkshopSimulation sim = new WorkshopSimulation(s);
            Near(1, sim.GetProgress(Station.Mine), "active progress clamped");
            Near(0, sim.GetProgress(Station.Smelter), "idle progress cleared");
            Equal(3, s.PendingQuality, "active quality clamped"); Equal(0, s.PendingSaleValue, "idle sale cleared");
            sim.Advance(0.01); Equal(2, s.Ore, "saved mine completed"); Equal(42, s.Inventory[0].Value, "saved forge completed");
            sim.Advance(100); Equal(1, s.TotalCrafted, "no replay");
        });

        Test("future strike timestamp cannot permanently lock the forge", delegate
        {
            WorkshopState s = new WorkshopState { Ingots = 2, SimulationTime = 5, LastStrikeTime = 1000000 };
            WorkshopSimulation sim = new WorkshopSimulation(s); Check(sim.TryStart(Station.Forge), "start");
            Check(sim.TryStrike(1), "invalid future clock repaired");
        });

        Test("invalid station values cannot mutate state", delegate
        {
            WorkshopSimulation sim = New();
            foreach (Station bad in new Station[] { (Station)(-1), (Station)4, (Station)int.MaxValue })
            {
                Check(!sim.TryStart(bad) && !sim.TryUpgrade(bad) && !sim.TryAutomate(bad), "bad station rejected");
                Check(!sim.IsRunning(bad), "bad station not running"); Near(0, sim.GetProgress(bad), "bad progress");
            }
            Near(40, sim.State.Gold, "gold untouched"); Check(sim.TryStart(Station.Mine), "valid station still works");
        });

        Test("campaign field exists for legacy save migration", delegate
        {
            FieldInfo campaign = typeof(WorkshopState).GetField("Campaign");
            Check(campaign != null, "missing campaign state field");
            Check(campaign.GetValue(New().State) != null, "legacy campaign initialized");
        });
        CampaignTests.Run(Test);
        PresentationTests.Run(Test);
        ProgressionTests.Run(Test);
        ChroniclesTests.Run(Test);
        WardrobeTests.Run(Test);
        ArtisanTests.Run(Test);
        SagaTests.Run(Test);
        ChronicleJourneyTests.Run(Test);

        Console.WriteLine("RESULT " + passed + " passed, " + failed + " failed, " + (passed + failed) + " total");
        return failed == 0 ? 0 : 1;
    }
}
