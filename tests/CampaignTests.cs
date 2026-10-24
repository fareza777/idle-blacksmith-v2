using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using ForgeEmpire;

internal static class CampaignTests
{
    private static readonly JsonSerializerOptions SaveOptions = new JsonSerializerOptions { IncludeFields = true };
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Near(double a, double b, string message)
    {
        Check(!double.IsNaN(b) && !double.IsInfinity(b) && Math.Abs(a - b) < 0.000001, message + ": " + a + " != " + b);
    }
    private static WorkshopSimulation New()
    {
        return new WorkshopSimulation(new WorkshopState { Gold = 1000, Ingots = 1000 });
    }
    private static WorkshopSimulation Reload(WorkshopSimulation s)
    {
        return new WorkshopSimulation(JsonSerializer.Deserialize<WorkshopState>(JsonSerializer.Serialize(s.State, SaveOptions), SaveOptions));
    }
    private static void Forge(WorkshopSimulation s, int recipe, int quality = 0)
    {
        Check(s.SelectRecipe(recipe), "select recipe " + recipe);
        Check(s.CanCraftSelected && s.TryStart(Station.Forge), "forge recipe " + recipe);
        for (int i = 0; i < quality; i++) { s.Advance(.21); Check(s.TryStrike(1), "quality strike"); }
        s.Advance(s.GetDuration(Station.Forge));
        Check(s.State.Inventory[s.State.Inventory.Count - 1].RecipeId == recipe, "correct output recipe");
    }
    private static void Gear(WorkshopSimulation s, int recipe, int quality = 3)
    {
        Forge(s, recipe, quality);
        Check(s.TryEquip(s.State.Inventory.Count - 1), "equip recipe " + recipe);
    }
    private static void Fight(WorkshopSimulation s, int region, int route, bool skills = true)
    {
        Check(s.StartExpedition(region, route), "start region " + region + " route " + route);
        for (int i = 0; i < 12000 && s.State.Campaign.Battle.Active; i++)
        {
            ExpeditionState b = s.State.Campaign.Battle;
            if (skills)
            {
                s.UseBattleSkill(0);
                // Enemies telegraph their heavy third attack; guarding late covers the impact.
                if (b.Elapsed % 6 >= 4.5) s.UseBattleSkill(1);
                if (b.HeroHP < b.HeroMaxHP * .55) s.UseBattleSkill(2);
            }
            s.Advance(.1);
        }
        Check(s.State.Campaign.Battle.Finished, "bounded battle");
    }
    private static void Win(WorkshopSimulation s, int region, int route, bool skills = true)
    {
        Fight(s, region, route, skills);
        Check(s.State.Campaign.Battle.Won, "win region " + region + " route " + route + " heroHP=" + s.State.Campaign.Battle.HeroHP);
        Check(s.ClaimExpedition(), "claim expedition");
    }
    private static void CompleteRegion(WorkshopSimulation s, int r)
    {
        Gear(s, r * 4, 3);
        Gear(s, r * 4 + 2, 3);
        Check(s.ClaimQuest(r * 3), "regional craft quest " + r);
        Win(s, r, 0);
        Check(s.ClaimQuest(r * 3 + 1), "supply quest " + r);
        Win(s, r, 1);
        Check(s.ClaimQuest(16 + r * 3), "rescue story " + r);
        Win(s, r, 2);
        Check(s.UnlockedRegion == r, "boss kill alone does not unlock");
        Check(s.ClaimQuest(r * 3 + 2), "boss quest " + r);
    }
    private static void SameBattle(WorkshopSimulation a, WorkshopSimulation b)
    {
        ExpeditionState x = a.State.Campaign.Battle, y = b.State.Campaign.Battle;
        Check(x.Active == y.Active && x.Finished == y.Finished && x.Won == y.Won, "battle phase equivalence");
        Check(x.Wave == y.Wave && x.Region == y.Region && x.Route == y.Route && x.HealsUsed == y.HealsUsed, "battle identity equivalence");
        Near(x.HeroHP, y.HeroHP, "hero hp"); Near(x.EnemyHP, y.EnemyHP, "enemy hp");
        Near(x.Elapsed, y.Elapsed, "elapsed"); Near(x.TickRemainder, y.TickRemainder, "remainder");
        Near(x.StrikeCooldown, y.StrikeCooldown, "strike cooldown"); Near(x.GuardCooldown, y.GuardCooldown, "guard cooldown");
        Near(x.HealCooldown, y.HealCooldown, "heal cooldown"); Near(x.GuardSeconds, y.GuardSeconds, "guard duration");
        Check(x.RewardGold == y.RewardGold && x.RewardMaterial == y.RewardMaterial && x.RewardExperience == y.RewardExperience, "reward equivalence");
    }

    private static WorkshopSimulation BossReviewFixture(bool finished)
    {
        WorkshopSimulation s = New(); Gear(s, 0); Gear(s, 2);
        Check(s.ClaimQuest(0), "earned craft quest");
        Win(s, 0, 0); Check(s.ClaimQuest(1), "earned expedition quest");
        if (finished) Fight(s, 0, 2);
        else
        {
            Check(s.StartExpedition(0, 2), "start real boss route");
            for (int i = 0; i < 1000 && s.State.Campaign.Battle.Active && s.State.Campaign.Battle.Wave < 2; i++) s.Advance(.1);
        }
        ExpeditionState b = s.State.Campaign.Battle;
        Check(b.Wave == 2 && b.HeroHP > 0, "earned final boss wave");
        Check(finished ? b.Finished && b.Won && b.EnemyHP == 0 : b.Active && b.EnemyHP > 0, "valid source health and phase");
        return s;
    }

    private static void SameCampaignCredits(WorkshopSimulation expected, WorkshopSimulation actual)
    {
        Near(expected.State.Gold, actual.State.Gold, "no unearned gold");
        Near(expected.State.LifetimeGold, actual.State.LifetimeGold, "no unearned lifetime gold");
        CampaignState x = expected.State.Campaign, y = actual.State.Campaign;
        Check(x.Chapter == y.Chapter && x.TownLevel == y.TownLevel && x.Reputation == y.Reputation, "no region or town credit");
        Check(x.ExpeditionsWon == y.ExpeditionsWon, "no expedition credit");
        for (int i = 0; i < 5; i++)
            Check(x.Materials[i] == y.Materials[i] && x.RegionWins[i] == y.RegionWins[i] && x.EliteWins[i] == y.EliteWins[i] && x.BossKills[i] == y.BossKills[i], "no loot or win credit " + i);
        for (int i = 0; i < 3; i++)
            Check(x.Heroes[i].Level == y.Heroes[i].Level && x.Heroes[i].Experience == y.Heroes[i].Experience, "no hero XP " + i);
        for (int i = 0; i < 30; i++) Check(x.QuestClaims[i] == y.QuestClaims[i], "no quest credit " + i);
    }

    private static void RejectBadHealth(double hp, bool hero, bool finished)
    {
        WorkshopSimulation s = BossReviewFixture(finished), before = Reload(s);
        if (hero) s.State.Campaign.Battle.HeroHP = hp; else s.State.Campaign.Battle.EnemyHP = hp;
        s = new WorkshopSimulation(s.State);
        SameCampaignCredits(before, s);
        ExpeditionState b = s.State.Campaign.Battle;
        bool rejected = !b.Active && !b.Won && b.RewardGold == 0 && b.RewardMaterial == 0 && b.RewardExperience == 0;
        s.ClaimExpedition();
        SameCampaignCredits(before, s);
        Check(rejected, "raw invalid health must reject the battle without rewards");
        Check(!s.IsQuestReady(2) && s.QuestProgress(2) == 0 && !s.ClaimQuest(2), "no boss quest credit");
        s = Reload(s); s.Advance(1200);
        Check(!s.ClaimExpedition(), "invalid result cannot replay after reload");
        SameCampaignCredits(before, s);
    }

    internal static void Run(Action<string, Action> test)
    {
        test("campaign content has explicit stable ids and serializable field contracts", delegate
        {
            Check(GameContent.Recipes.Length == 20 && GameContent.Regions.Length == 5 && GameContent.Heroes.Length == 3 && GameContent.Quests.Length == 30, "content counts");
            string[] names = { "Emberwood", "Whispering Marsh", "Frostpeak", "Ashen Caldera", "Starfall Citadel" };
            for (int i = 0; i < 20; i++)
            {
                RecipeDefinition d = GameContent.Recipes[i];
                Check(d.Id == i && d.Region == i / 4 && d.Icon == i, "stable recipe ordering");
                Check(d.Slot == (i % 4 < 2 ? GearSlot.Weapon : GearSlot.Armor), "two weapons and two armors");
                Check(d.Ingots > 0 && (i < 4 ? d.RareCost == 0 : d.RareCost > 0), "tier material costs");
                Check(!string.IsNullOrEmpty(d.Element) && d.Description.Length > 20, "element explanations");
            }
            for (int i = 0; i < 5; i++) Check(GameContent.Regions[i].Name == names[i] && GameContent.Regions[i].Id == i, "region names");
            for (int i = 0; i < 30; i++)
            {
                QuestDefinition q = GameContent.Quests[i];
                Check(q.Id == i && q.Main == (i < 15) && q.Region == (i < 15 ? i / 3 : (i - 15) / 3), "quest ordering");
                Check(q.Title.Length > 3 && q.Npc.Length > 2 && q.Description.Length > 30 && q.Objective.Length > 8 && q.Consequence.Length > 20, "authored quest " + i);
            }
            Check(GameContent.Recipes[0].Value == 24 && GameContent.Recipes[0].Ingots == 2, "legacy iron sword");
            foreach (Type type in new[] { typeof(CampaignState), typeof(HeroState), typeof(ExpeditionState) })
            {
                Check(Attribute.IsDefined(type, typeof(SerializableAttribute)), "serializable " + type.Name);
                Check(type.GetProperties().Length == 0, "state uses fields");
                foreach (FieldInfo f in type.GetFields()) Check(!f.IsInitOnly, "mutable save field " + f.Name);
            }
            WorkshopSimulation s = New();
            Check(s.MainQuest.Id == 0 && s.UnlockedRegion == 0 && s.State.Campaign.Heroes[0].Recruited, "fresh campaign");
            Check(!s.State.Campaign.Heroes[1].Recruited && !s.State.Campaign.Heroes[2].Recruited, "recruits locked");
            Check(s.IsQuestAvailable(0) && !s.IsQuestAvailable(1) && s.IsQuestAvailable(15) && !s.IsQuestAvailable(18), "quest visibility");
        });

        test("campaign recipe selection and reservations cannot duplicate inputs", delegate
        {
            WorkshopSimulation s = New();
            Check(!s.SelectRecipe(-1) && !s.SelectRecipe(20) && !s.SelectRecipe(4), "invalid or locked recipe");
            Check(!s.IsRecipeUnlocked(4) && !s.IsRecipeUnlocked(int.MaxValue), "unlock checks");
            CompleteRegion(s, 0);
            int rare = s.State.Campaign.Materials[0], ingots = s.State.Ingots;
            Check(s.SelectRecipe(4), "new chapter recipe");
            double duration = s.GetDuration(Station.Forge);
            Check(s.TryStart(Station.Forge), "reserve tier recipe");
            Check(s.State.PendingRecipe == 4, "pending snapshot");
            Check(!s.SelectRecipe(0) && !s.TryStart(Station.Forge), "running selection and double start denied");
            Check(s.State.Ingots == ingots - GameContent.Recipes[4].Ingots && s.State.Campaign.Materials[0] == rare - GameContent.Recipes[4].RareCost, "one reservation");
            s.State.SelectedRecipe = 0;
            Near(duration, s.GetDuration(Station.Forge), "inflight duration uses pending");
            s.Advance(.27); s = Reload(s); s.Advance(duration);
            Check(s.State.Inventory[s.State.Inventory.Count - 1].RecipeId == 4 && s.State.Campaign.CraftedCounts[4] == 1, "inflight recipe restored");
            Check(s.State.Ingots == ingots - GameContent.Recipes[4].Ingots && s.State.Campaign.Materials[0] == rare - GameContent.Recipes[4].RareCost, "no reload charge");
            s.State.Campaign.Materials[0] = 0; Check(s.SelectRecipe(5), "recipe known");
            string before = JsonSerializer.Serialize(s.State, SaveOptions);
            Check(!s.CanCraftSelected && !s.TryStart(Station.Forge), "rare shortage");
            Check(before == JsonSerializer.Serialize(s.State, SaveOptions), "failed craft is atomic");
        });

        test("campaign equipment swaps preserve capacity and never sell equipped gear", delegate
        {
            WorkshopSimulation s = New(); Gear(s, 0); int power = s.HeroPower;
            Check(!s.TryEquip(-1) && !s.TryEquip(0), "invalid inventory indices");
            for (int i = 0; i < 119; i++) s.State.Inventory.Add(new CraftedItem { Name = "Iron Sword", Value = 24 });
            Check(s.TryStart(Station.Forge), "reserve last slot");
            Check(s.TryEquip(0), "swap replaces consumed item atomically");
            Check(s.State.Inventory.Count == 119, "reservation preserved"); s.Advance(5);
            Check(s.State.Inventory.Count == 120, "forge stays within cap");
            Check(s.HeroPower < power, "quality improves weapon power");
            CraftedItem equipped = s.State.Campaign.Heroes[0].Weapon;
            Check(s.TryStart(Station.Market), "sell inventory"); s.Advance(2);
            Check(object.ReferenceEquals(equipped, s.State.Campaign.Heroes[0].Weapon), "equipped item not sold");
            Check(s.StartExpedition(0, 0), "battle start");
            Check(!s.TryEquip(0) && !s.SelectHero(0), "no inbattle loadout changes");
        });

        test("legacy max-level crew can pause market to equip and deliver real crafted swords", delegate
        {
            WorkshopState legacy = JsonSerializer.Deserialize<WorkshopState>(
                "{\"Gold\":1000,\"Levels\":[25,25,25,25],\"Automated\":[true,true,true,true]}", SaveOptions);
            WorkshopSimulation s = new WorkshopSimulation(legacy);
            s.Advance(20);
            Check(s.State.TotalCrafted > 10 && s.State.Inventory.Count == 0, "automatic market leaves legacy player no equipment stock");
            for (int i = 0; i < 1000 && (!s.IsRunning(Station.Market) || !s.IsRunning(Station.Forge)); i++)
            {
                s.Advance(.01);
                Check(s.State.Inventory.Count == 0, "forge output consumed within the same Advance");
            }
            Check(s.IsRunning(Station.Market) && s.IsRunning(Station.Forge), "real sale and next forge cycle in flight");
            double gold = s.State.Gold, marketProgress = s.GetProgress(Station.Market);
            int sold = s.State.TotalSold, crafted = s.State.TotalCrafted, pending = s.State.PendingSaleValue;
            Check(WorkshopSimulationTests.SetAutomationPaused(s, Station.Market, true), "pause existing market worker");
            Check(s.State.Automated[3] && WorkshopSimulationTests.Paused(s.State)[3], "worker remains owned and paused");
            Check(s.IsRunning(Station.Market) && s.State.PendingSaleValue == pending && s.State.Inventory.Count == 0, "reserved sale is not cancelled or refunded");
            Near(marketProgress, s.GetProgress(Station.Market), "sale progress retained");
            Near(gold, s.State.Gold, "pause is free");
            Check(!s.TryAutomate(Station.Market), "owned paused worker cannot be rehired");
            s = Reload(s);
            Check(WorkshopSimulationTests.Paused(s.State)[3], "market pause persists");
            double remaining = Math.Max((1 - s.GetProgress(Station.Market)) * s.GetDuration(Station.Market),
                (1 - s.GetProgress(Station.Forge)) * s.GetDuration(Station.Forge));
            s.Advance(remaining);
            Check(s.State.TotalSold == sold + 1 && s.State.TotalCrafted == crafted + 1, "only current sale and forge completed");
            Check(!s.IsRunning(Station.Market) && s.State.PendingSaleValue == 0 && s.State.Inventory.Count == 1, "next sale does not reserve crafted weapon");
            Near(gold + pending * 2.92, s.State.Gold, "only reserved sale pays");
            Check(s.TryEquip(0) && s.State.Inventory.Count == 0, "equip the retained crafted weapon");
            CraftedItem weapon = s.State.Campaign.Heroes[0].Weapon;
            Check(weapon != null && weapon.RecipeId == 0, "actual Iron Sword equipped");
            for (int i = 0; i < 1000 && s.State.Inventory.Count == 0; i++) s.Advance(.01);
            Check(s.State.Inventory.Count == 1 && s.State.Inventory[0].RecipeId == 0, "next Iron Sword stays available");
            gold = s.State.Gold;
            Check(s.IsQuestReady(15) && s.ClaimQuest(15), "deliver the next sword");
            Check(s.State.Inventory.Count == 0 && s.State.TotalSold == sold + 1, "delivery is not an extra market sale");
            Near(gold + 35, s.State.Gold, "delivery reward only");
            for (int i = 0; i < 1000 && s.State.Inventory.Count == 0; i++) s.Advance(.01);
            Check(s.State.Inventory.Count == 1, "stock ready for idle resume");
            gold = s.State.Gold;
            Check(WorkshopSimulationTests.SetAutomationPaused(s, Station.Market, false), "resume owned worker");
            Check(s.IsRunning(Station.Market) && s.State.Inventory.Count == 0 && s.State.PendingSaleValue == 24, "resume immediately reserves next sale");
            Check(!WorkshopSimulationTests.SetAutomationPaused(s, Station.Market, false) && !s.TryAutomate(Station.Market), "no double resume or rehire");
            Near(gold, s.State.Gold, "resume is free");
            s.Advance(s.GetDuration(Station.Market));
            Near(gold + 70.08, s.State.Gold, "resumed sale paid once");
            Check(object.ReferenceEquals(weapon, s.State.Campaign.Heroes[0].Weapon), "equipped weapon never sold");
            Check(s.State.TotalCrafted == s.State.TotalSold + s.State.Inventory.Count + (s.IsRunning(Station.Market) ? 1 : 0) + 2,
                "all crafted swords accounted for including equipment and delivery");
        });

        test("campaign recruits have distinct stats and purchases are atomic", delegate
        {
            WorkshopSimulation s = New(); double gold = s.State.Gold;
            Check(!s.TryRecruit(-1) && !s.TryRecruit(3) && !s.TryRecruit(0) && !s.SelectHero(1), "invalid recruitment");
            int p = s.HeroPower, g = s.HeroGuard, hp = s.HeroMaxHealth;
            Check(s.TryRecruit(1) && !s.TryRecruit(1) && s.SelectHero(1), "recruit Lyra once");
            Near(gold - 180, s.State.Gold, "Lyra cost");
            Check(s.HeroPower > p && s.HeroGuard < g && s.HeroMaxHealth < hp, "ranger distinct");
            Check(s.TryRecruit(2) && s.SelectHero(2), "recruit Kael"); Near(gold - 540, s.State.Gold, "Kael cost");
            Check(s.HeroPower != p && s.HeroMaxHealth != hp, "runesmith distinct");
            s = new WorkshopSimulation(new WorkshopState { Gold = 179 });
            Check(!s.TryRecruit(1), "cannot overdraft"); Near(179, s.State.Gold, "failed recruit untouched");
        });

        test("campaign unarmed defeat can recover through real crafted gear", delegate
        {
            WorkshopSimulation s = New(); Fight(s, 0, 0, false);
            Check(!s.State.Campaign.Battle.Won, "unarmed loses supply run");
            double gold = s.State.Gold;
            Check(!s.StartExpedition(0, 0), "must claim loss first");
            Check(s.ClaimExpedition() && !s.ClaimExpedition(), "clear loss once"); Near(gold, s.State.Gold, "loss no gold");
            Check(s.State.Campaign.ExpeditionsWon == 0 && s.State.Campaign.Materials[0] == 0, "loss no loot");
            Gear(s, 0); Gear(s, 2); Win(s, 0, 0, false);
            Check(s.State.Campaign.ExpeditionsWon == 1 && s.State.Campaign.Materials[0] > 0, "gear wins and earns next tier resources");
        });

        test("campaign previous-tier gear can lose and regional upgrades enable a retry", delegate
        {
            WorkshopSimulation s = New(); CompleteRegion(s, 0);
            Fight(s, 1, 2, false); Check(!s.State.Campaign.Battle.Won, "old gear loses against stronger regional boss");
            Check(s.ClaimExpedition(), "clear defeat");
            Gear(s, 4); Gear(s, 6); Win(s, 1, 2, false);
            Check(s.State.Campaign.BossKills[1] == 1, "upgraded gear wins retry");
        });

        test("campaign elemental armor and runesmith bonuses change real combat", delegate
        {
            WorkshopSimulation neutral = New(), ward = New();
            Gear(neutral, 0, 0); Gear(neutral, 2, 0); Gear(ward, 0, 0); Gear(ward, 3, 0);
            Check(ward.HeroGuard < neutral.HeroGuard, "ward has lower raw guard");
            Check(neutral.StartExpedition(0, 2) && ward.StartExpedition(0, 2), "compare armor");
            neutral.Advance(2); ward.Advance(2);
            Check(ward.State.Campaign.Battle.HeroHP > neutral.State.Campaign.Battle.HeroHP, "frost protection beats extra guard against fire");
            WorkshopSimulation rowan = New(), kael = New(), lyra = New();
            Check(kael.TryRecruit(2) && kael.SelectHero(2) && lyra.TryRecruit(1) && lyra.SelectHero(1), "select specialists");
            Gear(rowan, 1, 0); Gear(kael, 1, 0); Gear(lyra, 1, 0);
            Check(rowan.StartExpedition(0, 2) && kael.StartExpedition(0, 2) && lyra.StartExpedition(0, 2), "specialist battle");
            rowan.Advance(.8); lyra.Advance(.8);
            Check(lyra.State.Campaign.Battle.EnemyHP < rowan.State.Campaign.Battle.EnemyHP, "ranger attacks before guardian");
            rowan.Advance(.4); kael.Advance(1.2);
            double rowanHit = rowan.State.Campaign.Battle.EnemyMaxHP - rowan.State.Campaign.Battle.EnemyHP;
            double kaelHit = kael.State.Campaign.Battle.EnemyMaxHP - kael.State.Campaign.Battle.EnemyHP;
            Near(1.3, rowanHit / rowan.HeroPower, "guardian elemental multiplier");
            Near(1.6, kaelHit / kael.HeroPower, "runesmith elemental multiplier");
        });

        test("campaign element weakness overcomes lower nominal weapon power", delegate
        {
            WorkshopSimulation neutral = New(), frost = New();
            Gear(neutral, 0, 0); Gear(neutral, 2, 0); Gear(frost, 1, 0); Gear(frost, 2, 0);
            Check(frost.HeroPower <= neutral.HeroPower, "frost axe has no raw power advantage");
            Check(neutral.StartExpedition(0, 2) && frost.StartExpedition(0, 2), "boss encounters");
            neutral.Advance(1); frost.Advance(1);
            Check(frost.State.Campaign.Battle.EnemyHP < neutral.State.Campaign.Battle.EnemyHP, "weakness increases real damage");
        });

        test("campaign routes have distinct waves risk rewards and names", delegate
        {
            WorkshopSimulation s = New(); Gear(s, 0); Gear(s, 2);
            int lastGold = 0, lastMaterial = 0; double hp0 = 0;
            for (int route = 0; route < 3; route++)
            {
                Check(s.StartExpedition(0, route), "route start");
                Check(s.BattleWaveCount == (route == 0 ? 2 : 3) && s.EncounterName.Length > 3, "wave and name contract");
                ExpeditionState b = s.State.Campaign.Battle;
                if (route == 0) hp0 = b.EnemyMaxHP; else Check(b.EnemyMaxHP > hp0, "harder route enemies");
                Check(b.RewardGold > lastGold && b.RewardMaterial > lastMaterial, "route rewards scale");
                lastGold = b.RewardGold; lastMaterial = b.RewardMaterial;
                s.Advance(1200); Check(b.Finished, "route finishes"); Check(s.ClaimExpedition(), "route claim");
            }
            Check(!s.StartExpedition(-1, 0) && !s.StartExpedition(5, 0) && !s.StartExpedition(1, 0) && !s.StartExpedition(0, 3), "invalid and gated routes");
        });

        test("campaign skills cooldown guard and limited healing affect battle", delegate
        {
            WorkshopSimulation s = New(); Gear(s, 0, 0); Gear(s, 2, 0);
            Check(!s.UseBattleSkill(0), "idle skill denied"); Check(s.StartExpedition(0, 2), "boss");
            WorkshopSimulation plain = Reload(s); double hp = s.State.Campaign.Battle.EnemyHP;
            Check(s.UseBattleSkill(0) && !s.UseBattleSkill(0), "strike cooldown");
            Check(s.State.Campaign.Battle.EnemyHP < hp, "strike damage");
            Check(!s.UseBattleSkill(-1) && !s.UseBattleSkill(3), "invalid skill");
            s.Advance(1.6); plain.Advance(1.6); Check(s.UseBattleSkill(1) && !s.UseBattleSkill(1), "guard cooldown");
            s.Advance(.4); plain.Advance(.4);
            Check(s.State.Campaign.Battle.HeroHP > plain.State.Campaign.Battle.HeroHP, "guard reduces impact");
            s.Advance(4); hp = s.State.Campaign.Battle.HeroHP;
            Check(s.UseBattleSkill(2) && s.State.Campaign.Battle.HeroHP > hp, "healing restores hp");
            Check(s.State.Campaign.Battle.HeroHP <= s.State.Campaign.Battle.HeroMaxHP && !s.UseBattleSkill(2), "heal capped once");
            s = Reload(s); Check(!s.UseBattleSkill(2), "reload cannot reset heal");
        });

        test("campaign offline fixed ticks match frames and fields-only reload", delegate
        {
            WorkshopSimulation s = New(); Gear(s, 0); Gear(s, 2); Check(s.StartExpedition(0, 2), "boss start");
            s.Advance(.137); Check(s.UseBattleSkill(0) && s.UseBattleSkill(1), "saved active skills");
            WorkshopSimulation batch = Reload(s), frames = Reload(s);
            batch.Advance(3.863);
            for (int i = 0; i < 3863; i++) { frames.Advance(.001); if (i % 331 == 0) frames = Reload(frames); }
            SameBattle(batch, frames);
            double gold = batch.State.Gold;
            batch.ApplyOffline(double.MaxValue);
            for (int i = 0; i < 28800 * 60; i++) frames.Advance(1.0 / 60);
            SameBattle(batch, frames); Check(batch.State.Campaign.Battle.Finished, "one offline result");
            Near(gold, batch.State.Gold, "offline does not auto claim");
            Check(batch.State.Campaign.ExpeditionsWon == 0, "no offline chain");
            Check(batch.ClaimExpedition() && frames.ClaimExpedition(), "claim equal results");
            Near(batch.State.Gold, frames.State.Gold, "equal rewards");
        });

        test("campaign delivery consumes exact item and mine supply has observable rewards", delegate
        {
            WorkshopSimulation s = New(); Forge(s, 1);
            Check(!s.IsQuestReady(15) && !s.ClaimQuest(15), "wrong recipe cannot deliver");
            Forge(s, 0); int count = s.State.Inventory.Count; double gold = s.State.Gold;
            Check(s.QuestProgress(15) == 1 && s.ClaimQuest(15), "deliver iron sword");
            Check(s.State.Inventory.Count == count - 1 && s.State.Inventory[0].RecipeId == 1, "specific item removed");
            Check(s.State.Gold > gold && s.State.Campaign.Reputation > 0, "delivery outcome");
            s = Reload(s); Check(!s.ClaimQuest(15) && !s.IsQuestAvailable(15), "delivery cannot replay");
            Check(!s.IsQuestReady(17), "ore supply missing");
            for (int i = 0; i < 4; i++) { Check(s.TryStart(Station.Mine), "mine supply"); s.Advance(1.6); }
            Check(s.ClaimQuest(17) && s.State.Ore == 0 && s.State.Campaign.Materials[0] > 0, "ore consumed for rare supply");
            Check(s.State.TotalSold == 0, "delivery not a market sale");
        });

        test("campaign all five regions and thirty stories are reachable without injected wins", delegate
        {
            WorkshopSimulation s = New(); Check(s.TryRecruit(1) && s.TryRecruit(2), "earned starting-budget recruits");
            for (int r = 0; r < 5; r++)
            {
                if (r > 0) Check(s.SelectHero(r % 3), "rotate distinct heroes");
                CompleteRegion(s, r);
                Forge(s, r * 4); Check(s.ClaimQuest(15 + r * 3), "regional delivery " + r);
                if (r == 0)
                    for (int i = 0; i < 4; i++) { Check(s.TryStart(Station.Mine), "mine"); s.Advance(1.6); }
                else Forge(s, r * 4 + 3, 2);
                Check(s.ClaimQuest(17 + r * 3), "supply or quality story " + r);
                Forge(s, r * 4 + 1);
                if (r == 0) Forge(s, 3);
                s = Reload(s);
                Check(!s.ClaimQuest(r * 3 + 2), "boss quest cannot replay");
                Console.WriteLine("INFO campaign region " + r + ": wins=" + s.State.Campaign.RegionWins[r] + " material=" + s.State.Campaign.Materials[r] + " gold=" + s.State.Gold);
            }
            Check(s.MainQuest == null && s.UnlockedRegion == 4, "entire main campaign complete");
            Check(s.State.Campaign.ExpeditionsWon == 15 && s.State.Campaign.TownLevel >= 5, "real wins restore town");
            for (int i = 0; i < 30; i++) Check(s.State.Campaign.QuestClaims[i], "all stories claimed " + i);
            for (int i = 0; i < 20; i++) Check(s.State.Campaign.CraftedCounts[i] > 0, "all twenty recipes actually crafted " + i);
            for (int i = 0; i < 3; i++) Check(s.State.Campaign.Heroes[i].Level > 1, "heroes earned experience");
        });

        test("campaign normalization repairs null arrays gear and legacy running forge", delegate
        {
            WorkshopState state = new WorkshopState { Campaign = null, Running = new[] { false, false, true, false }, Progress = new[] { 0.0, 0, .5, 0 } };
            WorkshopSimulation s = new WorkshopSimulation(state); s.Advance(2.25);
            Check(s.State.Inventory[0].RecipeId == 0 && s.State.Inventory[0].Value == 24, "legacy pending sword");
            state = new WorkshopState { SelectedRecipe = 999, PendingRecipe = -5, Campaign = new CampaignState
            {
                Chapter = 999, Reputation = -1, TownLevel = -1, SelectedHero = 999, ExpeditionsWon = -1,
                Materials = new[] { -1, int.MaxValue }, CraftedCounts = null, RegionWins = null,
                EliteWins = new[] { -4 }, BossKills = null, QuestClaims = new bool[1],
                Heroes = new[] { new HeroState { Level = -9, Experience = -5, Weapon = new CraftedItem { RecipeId = 999, Quality = 999, Value = -4 }, Armor = new CraftedItem { RecipeId = 0 } } }, Battle = null
            } };
            state.Inventory.Add(new CraftedItem { RecipeId = -10 }); s = new WorkshopSimulation(state);
            CampaignState c = s.State.Campaign;
            Check(c.Materials.Length == 5 && c.CraftedCounts.Length == 20 && c.QuestClaims.Length == 30 && c.Heroes.Length == 3, "array sizes");
            Check(c.Materials[0] == 0 && c.Materials[1] <= 1000000000 && c.Heroes[0].Recruited, "bounded migration");
            Check(c.Heroes[0].Weapon == null && c.Heroes[0].Armor == null, "empty or invalid gear records must not create equipment");
            Check(state.Inventory[0].RecipeId == 0 && state.SelectedRecipe == 0 && state.PendingRecipe == 0, "recipe ids repaired");
            Check(c.SelectedHero == 0 && c.Heroes[0].Level == 1 && c.Battle != null, "usable default hero");
            Check(!s.IsQuestReady(-1) && !s.IsQuestAvailable(30) && !s.ClaimQuest(999) && s.QuestProgress(-1) == 0, "invalid quest APIs");
        });

        test("campaign normalization rejects invalid battle and bounds forged reward fields", delegate
        {
            WorkshopSimulation s = New(); Gear(s, 0); Gear(s, 2); Check(s.StartExpedition(0, 0), "start");
            s.State.Campaign.Battle.Region = 100; s = Reload(s);
            Check(!s.State.Campaign.Battle.Active && !s.ClaimExpedition(), "invalid region cannot pay");
            Check(s.StartExpedition(0, 0), "retry");
            s.State.Campaign.Battle.HeroHP = double.NaN;
            s.State.Campaign.Battle.StrikeCooldown = double.PositiveInfinity;
            s.State.Campaign.Battle.GuardCooldown = -20; s.State.Campaign.Battle.HealCooldown = double.NaN;
            s.State.Campaign.Battle.TickRemainder = double.MaxValue;
            s = new WorkshopSimulation(s.State);
            Check(!double.IsNaN(s.State.Campaign.Battle.HeroHP) && !double.IsInfinity(s.State.Campaign.Battle.StrikeCooldown), "finite battle repair");
            s.Advance(double.MaxValue); Check(s.ClaimExpedition(), "corrupt health resolves as loss");
            Fight(s, 0, 0); Check(s.State.Campaign.Battle.Won, "legitimate win");
            int reward = s.State.Campaign.Battle.RewardGold;
            s.State.Campaign.Battle.RewardGold = int.MaxValue; s.State.Campaign.Battle.RewardMaterial = int.MaxValue;
            s.State.Campaign.Battle.RewardExperience = int.MaxValue; s = Reload(s);
            double before = s.State.Gold; Check(s.ClaimExpedition(), "claim legitimate result");
            Near(reward, s.State.Gold - before, "canonical reward not save value");
            Check(s.State.Campaign.Materials[0] < 100 && s.State.Campaign.Heroes[0].Level < 10, "bounded loot xp");
            s = Reload(s); Check(!s.ClaimExpedition(), "claimed result cannot replay");
        });

        foreach (bool hero in new[] { false, true })
        foreach (double hp in new[] { -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity, 0.0 })
        {
            string health = double.IsPositiveInfinity(hp) ? "+Inf" : double.IsNegativeInfinity(hp) ? "-Inf" : hp.ToString();
            test("campaign active final-wave " + (hero ? "hero" : "enemy") + " HP " + health + " cannot create rewards", delegate
            {
                RejectBadHealth(hp, hero, false);
            });
        }

        test("campaign finished wins cannot launder invalid raw health through clamping", delegate
        {
            foreach (double hp in new[] { -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                RejectBadHealth(hp, false, true);
                RejectBadHealth(hp, true, true);
            }
        });

        test("campaign legitimate zero-HP boss victory reloads and pays exactly once", delegate
        {
            WorkshopSimulation original = BossReviewFixture(true), s = Reload(original);
            SameBattle(original, s);
            Check(!s.State.Campaign.Battle.Active && s.State.Campaign.Battle.Won && s.State.Campaign.Battle.EnemyHP == 0, "legitimate terminal zero HP remains a win");
            double gold = s.State.Gold;
            int materials = s.State.Campaign.Materials[0];
            Check(s.ClaimExpedition() && !s.ClaimExpedition(), "claim once");
            Near(gold + 110, s.State.Gold, "canonical boss gold");
            Check(s.State.Campaign.Materials[0] == materials + 10 && s.State.Campaign.BossKills[0] == 1, "earned boss loot and credit");
            Check(s.State.Campaign.Heroes[0].Level == 2 && s.State.Campaign.Heroes[0].Experience == 50, "earned boss experience");
            Check(s.IsQuestReady(2) && s.ClaimQuest(2) && s.UnlockedRegion == 1, "earned boss quest still unlocks region");
            Check(!Reload(s).ClaimExpedition(), "terminal reward cannot replay after reload");
        });

        test("campaign finished battle snapshots survive subsequent equipment changes and reload", delegate
        {
            WorkshopSimulation s = New(); Gear(s, 0); Gear(s, 2); Fight(s, 0, 2);
            Check(s.State.Campaign.Battle.Won, "finished win");
            ExpeditionState original = s.State.Campaign.Battle;
            int power = original.SnapshotPower, guard = original.SnapshotGuard;
            double damage = original.SnapshotDamage;
            Gear(s, 0, 0); Gear(s, 2, 0);
            Check(s.HeroPower < power && s.HeroGuard < guard, "new weaker loadout");
            WorkshopSimulation restored = Reload(s);
            Check(restored.State.Campaign.Battle.SnapshotPower == power && restored.State.Campaign.Battle.SnapshotGuard == guard, "terminal snapshot must not be recomputed from new equipment");
            Near(damage, restored.State.Campaign.Battle.SnapshotDamage, "damage snapshot retained");
            SameBattle(s, restored); Check(restored.ClaimExpedition(), "pending result still claimable");
        });

        test("campaign quality delivery rejects weak or equipped wards without consuming them", delegate
        {
            WorkshopSimulation s = New(); CompleteRegion(s, 0);
            Forge(s, 7, 1); int count = s.State.Inventory.Count;
            Check(!s.IsQuestReady(20) && !s.ClaimQuest(20) && s.State.Inventory.Count == count, "insufficient quality remains untouched");
            Gear(s, 7, 2); Check(!s.IsQuestReady(20), "equipped ward cannot be delivered");
            Gear(s, 6, 0); Check(s.IsQuestReady(20), "unequipped qualified ward available");
            count = s.State.Inventory.Count; Check(s.ClaimQuest(20) && s.State.Inventory.Count == count - 1, "quality ward consumed");
            Check(!Reload(s).ClaimQuest(20), "quality story claimed once");
        });

        test("campaign finished loss snapshots never turn into rewards", delegate
        {
            WorkshopSimulation s = New(); Fight(s, 0, 0, false);
            Check(!s.State.Campaign.Battle.Won, "actual defeat");
            s.State.Campaign.Battle.Won = true;
            s.State.Campaign.Battle.RewardGold = int.MaxValue;
            s = Reload(s); double gold = s.State.Gold;
            Check(!s.State.Campaign.Battle.Won && s.ClaimExpedition(), "inconsistent victory rejected");
            Near(gold, s.State.Gold, "zero forged payout");
        });

        test("campaign production workers and active battle share deterministic advance boundaries", delegate
        {
            WorkshopSimulation batch = New(); Gear(batch, 0); Gear(batch, 2);
            for (int i = 0; i < 4; i++) Check(batch.TryAutomate((Station)i), "automation");
            Check(batch.StartExpedition(0, 2), "start battle"); batch.Advance(.137);
            WorkshopSimulation frames = Reload(batch);
            batch.Advance(17.563);
            for (int i = 0; i < 17563; i++) { frames.Advance(.001); if (i % 613 == 0) frames = Reload(frames); }
            SameBattle(batch, frames);
            Check(batch.State.Ingots == frames.State.Ingots && batch.State.Ore == frames.State.Ore && batch.State.TotalCrafted == frames.State.TotalCrafted && batch.State.TotalSold == frames.State.TotalSold, "production counts match");
            Near(batch.State.Gold, frames.State.Gold, "production proceeds match");
            for (int i = 0; i < 4; i++) Near(batch.GetProgress((Station)i), frames.GetProgress((Station)i), "production progress");
            Check(batch.State.Campaign.CraftedCounts[0] == frames.State.Campaign.CraftedCounts[0], "campaign crafts also match");
            Near(batch.State.Campaign.Battle.SnapshotDamage, frames.State.Campaign.Battle.SnapshotDamage, "inflight equipment unchanged by production");
        });

        test("campaign oversized time and invalid inputs remain bounded no-ops", delegate
        {
            WorkshopSimulation s = New(); Check(s.StartExpedition(0, 0), "unarmed run");
            string before = JsonSerializer.Serialize(s.State, SaveOptions);
            foreach (double dt in new[] { 0.0, -1, double.NaN, double.PositiveInfinity }) s.Advance(dt);
            Check(before == JsonSerializer.Serialize(s.State, SaveOptions), "invalid elapsed no change");
            Stopwatch timer = Stopwatch.StartNew(); s.Advance(double.MaxValue); timer.Stop();
            Check(timer.Elapsed.TotalSeconds < 5 && s.State.Campaign.Battle.Finished, "finite bounded battle advance");
            Near(28800, s.State.SimulationTime, "eight hour cap");
            Check(!s.UseBattleSkill(0) && !s.StartExpedition(0, 0), "result locked until claim");
        });
    }
}
