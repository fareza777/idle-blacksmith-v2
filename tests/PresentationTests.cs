using System;
using System.Reflection;
using System.Text.Json;
using ForgeEmpire;

internal static class PresentationTests
{
    static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true };
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static object Call(object target, string name, params object[] args)
    {
        var method = target.GetType().GetMethod(name);
        Check(method != null, "Missing behavior: " + name);
        return method.Invoke(target, args);
    }
    static void Set(object target, string name, object value)
    {
        var field = target.GetType().GetField(name);
        Check(field != null, "Missing persisted field: " + name);
        field.SetValue(target, value);
    }
    static object Read(object target, string name)
    {
        var property = target.GetType().GetProperty(name);
        if (property != null) return property.GetValue(target);
        var field = target.GetType().GetField(name);
        Check(field != null, "Missing read contract: " + name);
        return field.GetValue(target);
    }
    static CraftedItem Item(int recipe, int quality = 0)
    {
        return new CraftedItem { RecipeId = recipe, Name = GameContent.Recipes[recipe].Name, Value = GameContent.Recipes[recipe].Value, Quality = quality };
    }
    static WorkshopSimulation Reload(WorkshopSimulation sim)
    {
        return new WorkshopSimulation(JsonSerializer.Deserialize<WorkshopState>(JsonSerializer.Serialize(sim.State, Json), Json));
    }
    static string Step(WorkshopSimulation sim) { return Read(Read(sim, "NextStep"), "Action").ToString(); }

    public static void Run(Action<string, Action> test)
    {
        test("walkers plant their feet while paused and reverse only at a path endpoint", delegate {
            var type=typeof(WorkshopSimulation).Assembly.GetType("ForgeEmpire.WalkMotion");Check(type!=null,"Missing distance-based walking");
            var sample=type.GetMethod("Sample");Check(sample!=null,"Missing walk sampler");
            Func<double,object> at=time=>sample.Invoke(null,new object[]{time,100.0,20.0,1.0,24.0});
            Check(!(bool)Read(at(.5),"Moving")&&(double)Read(at(.5),"Progress")==0,"walker slid during start pause");
            Check((bool)Read(at(3),"Moving")&&(bool)Read(at(3),"FacingRight"),"outward walk missing");
            Check(!(bool)Read(at(6.5),"Moving")&&(double)Read(at(6.5),"Progress")==1,"walker slid during arrival pause");
            Check((bool)Read(at(8),"Moving")&&!(bool)Read(at(8),"FacingRight"),"walker did not walk home");
            Check(!(bool)Read(at(12.5),"Moving")&&(double)Read(at(12.5),"Progress")==0,"round trip did not settle");
        });
        test("walk pose follows traveled distance when walking speed changes", delegate {
            var type=typeof(WorkshopSimulation).Assembly.GetType("ForgeEmpire.WalkMotion");Check(type!=null,"Missing distance-based walking");
            var method=type.GetMethod("Sample");
            var slow=method.Invoke(null,new object[]{2.0,100.0,20.0,1.0,24.0});
            var fast=method.Invoke(null,new object[]{1.5,100.0,40.0,1.0,24.0});
            Check(Math.Abs((double)Read(slow,"Progress")-(double)Read(fast,"Progress"))<1e-8,"different geometry at same traveled distance");
            Check((int)Read(slow,"Frame")== (int)Read(fast,"Frame"),"feet animate independently of distance");
        });
        test("protected stock is skipped by automatic and manual FIFO sales", delegate {
            var state = new WorkshopState { Gold = 1000 };
            var keep = Item(1); Set(keep, "Protected", true);
            state.Inventory.Add(keep); state.Inventory.Add(Item(0));
            var sim = new WorkshopSimulation(state);
            Check(sim.TryAutomate(Station.Market), "hire merchant");
            sim.Advance(10);
            Check(state.Inventory.Count == 1 && ReferenceEquals(state.Inventory[0], keep), "merchant consumed protected equipment");
            Check(state.TotalSold == 1 && !sim.TryStart(Station.Market), "protected-only stock must block sale");
            sim = Reload(sim);
            Check((bool)Read(sim.State.Inventory[0], "Protected") && !sim.TryStart(Station.Market), "keep status lost after reload");
        });
        test("explicit item sale reserves the selected eligible gear exactly once", delegate {
            var state = new WorkshopState(); state.Inventory.Add(Item(0)); state.Inventory.Add(Item(2));
            var sim = new WorkshopSimulation(state);
            Check((bool)Call(sim, "TrySellItem", 1), "explicit armor sale rejected");
            Check(state.Inventory.Count == 1 && state.Inventory[0].RecipeId == 0 && state.PendingSaleValue == 30, "wrong stock reserved");
            Check(!(bool)Call(sim, "TrySellItem", 0), "double sale while busy");
            sim.Advance(2.01); Check(state.TotalSold == 1 && state.Gold == 70, "wrong explicit sale payout");
            Check(!(bool)Call(sim, "TrySellItem", -1) && !(bool)Call(sim, "TrySellItem", 4), "stale/out-of-range index accepted");
        });
        test("protect control validates stock and prevents explicit sale without removing equip choice", delegate {
            var state = new WorkshopState(); state.Inventory.Add(Item(0));
            var sim = new WorkshopSimulation(state);
            Check((bool)Call(sim, "TryProtectItem", 0, true), "protect failed");
            Check(!(bool)Call(sim, "TrySellItem", 0), "explicit sale ignored protection");
            Check(!(bool)Call(sim, "TryProtectItem", -1, true), "invalid protection changed stock");
            Check(sim.TryEquip(0) && state.Campaign.Heroes[0].Weapon != null, "protected gear cannot equip");
        });
        test("craft output purpose is reserved at start and survives an unfinished save", delegate {
            var state = new WorkshopState { Ingots = 2 };
            Set(state, "KeepCraftedEquipment", true);
            var sim = new WorkshopSimulation(state); Check(sim.TryStart(Station.Forge), "start protected craft");
            Set(state, "KeepCraftedEquipment", false); sim.Advance(1); sim = Reload(sim); sim.Advance(4);
            Check(sim.State.Inventory.Count == 1 && (bool)Read(sim.State.Inventory[0], "Protected"), "craft purpose changed after reservation/reload");
        });
        test("new display settings normalize and survive legacy and current saves", delegate {
            var state = new WorkshopState(); Set(state, "TextSize", 99); Set(state, "ReducedMotion", true);
            var sim = Reload(new WorkshopSimulation(state));
            Check((int)Read(sim.State, "TextSize") == 2 && (bool)Read(sim.State, "ReducedMotion"), "settings not normalized or persisted");
            var legacy = JsonSerializer.Deserialize<WorkshopState>("{\"Gold\":73,\"Levels\":[2,1,1,1],\"Inventory\":[{\"RecipeId\":0,\"Name\":\"Iron Sword\",\"Value\":24}]}", Json);
            sim = new WorkshopSimulation(legacy);
            Check(sim.State.Gold == 73 && sim.State.Levels[0] == 2 && !(bool)Read(sim.State.Inventory[0], "Protected") && (int)Read(sim.State, "TextSize") == 0, "legacy progress/preferences corrupted");
        });
        test("next step follows actual materials then gear and expedition state", delegate {
            var sim = new WorkshopSimulation(new WorkshopState());
            Check(Step(sim) == "Mine", "first action must get ore");
            sim.State.Ore = 4; Check(Step(sim) == "Smelt", "ore should guide to ingots");
            sim.State.Ingots = 2; Check(Step(sim) == "Forge", "supplied workshop should guide to craft");
            Check(sim.TryStart(Station.Forge), "start"); sim.Advance(5);
            Check(Step(sim) == "Equip", "ready sword should guide to equip before sale");
            Check(sim.TryEquip(0), "equip"); Check(Step(sim) == "ClaimQuest", "claim earned first story reward");
            Check(sim.ClaimQuest(0), "claim"); Check(Step(sim) == "Adventure", "equipped hero needs route");
            Check(sim.StartExpedition(0, 0), "start expedition"); Check(Step(sim) == "Battle", "active battle needs correct shortcut");
        });
        test("route blocker explains missing weapon and pending result with no mutation", delegate {
            var sim = new WorkshopSimulation(new WorkshopState());
            Check(((string)Call(sim, "ExpeditionBlocker", 0, 1)).ToLowerInvariant().Contains("weapon"), "missing weapon not explained");
            Check((string)Call(sim, "ExpeditionBlocker", 0, 0) == "", "unarmed supply must remain playable");
            Check(sim.StartExpedition(0, 0), "start supply");
            Check(((string)Call(sim, "ExpeditionBlocker", 0, 0)).ToLowerInvariant().Contains("expedition"), "active expedition not explained");
            Check(sim.State.Campaign.Battle.Active && sim.State.Gold == 40, "advice mutated gameplay");
        });
        test("equipment comparison and upgrade preview predict actual mutations", delegate {
            var state = new WorkshopState { Gold = 1000 }; state.Inventory.Add(Item(0)); state.Inventory.Add(Item(1, 2));
            var sim = new WorkshopSimulation(state); Check(sim.TryEquip(0), "equip baseline");
            Check((int)Call(sim, "EquipmentDelta", 0, true) == 4, "quality comparison should be16minus12");
            int before = sim.HeroPower; Check(sim.TryEquip(0) && sim.HeroPower - before == 4, "comparison differs from actual equip");
            double next = (double)Call(sim, "NextDuration", Station.Smelter);
            Check(sim.TryUpgrade(Station.Smelter) && Math.Abs(sim.GetDuration(Station.Smelter) - next) < .000001, "upgrade preview differs from actual duration");
        });
        test("sale preview includes market upgrades and matches the paid amount", delegate {
            var state = new WorkshopState { Levels = new[] { 1, 1, 1, 3 } }; var gear = Item(2); state.Inventory.Add(gear);
            var sim = new WorkshopSimulation(state);
            double preview = (double)Call(sim, "SaleValue", gear);
            Check(Math.Abs(preview - 34.8) < .000001, "market bonus omitted from sale preview");
            Check((bool)Call(sim, "TrySellItem", 0), "sale failed"); sim.Advance(3);
            Check(Math.Abs(state.Gold - 40 - preview) < .000001, "paid value differs from preview");
        });
        test("route reward preview is accurate without starting or paying an expedition", delegate {
            var sim = new WorkshopSimulation(new WorkshopState());
            var reward = Call(sim, "PreviewRouteRewards", 2, 1);
            Check((int)Read(reward, "Gold") == 225 && (int)Read(reward, "Material") == 7 && (int)Read(reward, "Experience") == 70 && (int)Read(reward, "Reputation") == 12, "route forecast is wrong");
            Check(!sim.State.Campaign.Battle.Active && sim.State.Gold == 40 && sim.State.Campaign.Materials[2] == 0, "preview paid/started a battle");
            Check(Call(sim, "PreviewRouteRewards", -1, 0) == null, "invalid preview accepted");
        });
    }
}
