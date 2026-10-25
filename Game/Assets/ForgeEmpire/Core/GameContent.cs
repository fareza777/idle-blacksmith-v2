namespace ForgeEmpire
{
    public sealed class RecipeDefinition
    {
        public int Id, Region, Ingots, RareCost, Value, Power, Guard, Icon;
        public GearSlot Slot;
        public WeaponKind Kind;
        public GearEffect Effect;
        public string Name, Description, Element;
    }

    public sealed class RegionDefinition
    {
        public int Id;
        public string Name, Description, BossName, Weakness;
    }

    public sealed class HeroDefinition
    {
        public int Id, Cost, Power, Guard, Health;
        public string Name, Role, Description;
    }

    public enum QuestKind { Craft, Expedition, Boss, Delivery, Rescue, OreSupply, QualityDelivery }

    public sealed class QuestDefinition
    {
        public int Id, Region, Target, RewardGold, RewardReputation;
        public bool Main;
        public string Title, Npc, Description, Objective, Consequence;
        public QuestKind Kind;
        public int RecipeId, Quality, RewardMaterial, RewardTown;
    }

    public static partial class GameContent
    {
        // Stable atlas/save ordering: sword, axe, mail, ward in each region.
        static readonly RecipeDefinition[] BaseRecipes =
        {
            Recipe(0, 0, GearSlot.Weapon, "Iron Sword", "A dependable neutral blade. Quality improves power; frost is stronger against Emberwood's burning beasts.", "Iron", 2, 0, 24, 12, 0),
            Recipe(1, 0, GearSlot.Weapon, "Rimewood Axe", "Cold sap bites through Emberwood and Caldera flames. Frost deals 30% more there, but marsh and Frostpeak resist it.", "Frost", 3, 0, 32, 11, 0),
            Recipe(2, 0, GearSlot.Armor, "Riveted Mail", "Neutral iron rings add reliable guard in every region. Quality reinforces each ring without elemental resistance.", "Iron", 3, 0, 30, 0, 6),
            Recipe(3, 0, GearSlot.Armor, "Rimewood Ward", "A cold-sap mantle: 20% less incoming damage in Emberwood and Caldera, with lighter physical guard than mail.", "Frost", 3, 0, 36, 0, 5),
            Recipe(4, 1, GearSlot.Weapon, "Reedfire Sword", "Emberwood cinders ignite marsh poison and Frostpeak ice: Flame gains 30% damage there. Burning regions resist it.", "Flame", 4, 2, 64, 24, 0),
            Recipe(5, 1, GearSlot.Weapon, "Thunderfen Axe", "Cinder-tempered iron channels Storm. Strong raw power; Storm gains 30% against Starfall's void sentries.", "Storm", 5, 3, 76, 27, 0),
            Recipe(6, 1, GearSlot.Armor, "Reedfire Mail", "Cinders burn away marsh venom and repel Frostpeak cold. Flame armor cuts incoming damage there by 20%.", "Flame", 4, 2, 68, 0, 12),
            Recipe(7, 1, GearSlot.Armor, "Thunderfen Ward", "Dense storm-thread provides high guard. Its lightning veil cuts Starfall damage by 20%; cinders fund the weave.", "Storm", 5, 3, 80, 0, 14),
            Recipe(8, 2, GearSlot.Weapon, "Dawnfire Sword", "Marsh glass focuses Flame into an ice-cleaving edge. Deals 30% more in Frostpeak and the marsh; Caldera resists it.", "Flame", 6, 2, 124, 38, 0),
            Recipe(9, 2, GearSlot.Weapon, "Glacier Axe", "A heavy Frost edge: 30% stronger in Emberwood and Caldera. Frostpeak ice and marsh spirits resist its chill.", "Frost", 7, 3, 144, 42, 0),
            Recipe(10, 2, GearSlot.Armor, "Dawnfire Mail", "Marsh glass stores a steady warmth. Flame reduces Frostpeak and marsh damage by 20% in addition to guard.", "Flame", 6, 2, 130, 0, 20),
            Recipe(11, 2, GearSlot.Armor, "Glacier Ward", "A thick Frost barrier made from marsh glass. Cuts incoming Emberwood and Caldera damage by 20%.", "Frost", 7, 3, 150, 0, 22),
            Recipe(12, 3, GearSlot.Weapon, "Winterheart Sword", "Frostpeak crystals hold an unmelting Frost edge. Deals 30% more against Caldera and Emberwood flames.", "Frost", 8, 2, 220, 56, 0),
            Recipe(13, 3, GearSlot.Weapon, "Pyrebreaker Axe", "A powerful Flame axe with a crystal core. Excels against marsh and Frostpeak, but Caldera resists Flame.", "Flame", 9, 3, 248, 60, 0),
            Recipe(14, 3, GearSlot.Armor, "Winterheart Mail", "Frostpeak crystal plates absorb heat. Frost cuts Caldera and Emberwood incoming damage by 20%.", "Frost", 8, 2, 228, 0, 30),
            Recipe(15, 3, GearSlot.Armor, "Pyrebreaker Ward", "Heavy Flame plates grant high guard. Their warmth cuts marsh and Frostpeak damage by 20%, not Caldera fire.", "Flame", 9, 3, 256, 0, 34),
            Recipe(16, 4, GearSlot.Weapon, "Skybreaker Sword", "Caldera obsidian conducts Storm through the void. Deals 30% more against Starfall's sentries and sovereign.", "Storm", 10, 2, 368, 80, 0),
            Recipe(17, 4, GearSlot.Weapon, "Nightstar Axe", "Void-forged obsidian grants enormous raw power. Starfall resists Void by 25%; choose Storm to break its wards.", "Void", 11, 3, 404, 86, 0),
            Recipe(18, 4, GearSlot.Armor, "Skybreaker Mail", "A Storm lattice grounded in Caldera obsidian. Reduces incoming Starfall damage by 20% as well as adding guard.", "Storm", 10, 2, 380, 0, 44),
            Recipe(19, 4, GearSlot.Armor, "Nightstar Ward", "The heaviest physical guard in the forge. Void offers no elemental protection from Starfall; Storm does.", "Void", 11, 3, 420, 0, 48)
        };

        static readonly RegionDefinition[] BaseRegions =
        {
            new RegionDefinition { Id = 0, Name = "Emberwood", BossName = "The Thornback", Weakness = "Frost", Description = "Burning roots choke the old road. Frost breaks their heat; Flame is resisted. Supply: 2 waves, suggested power 15 and guard 9. Unarmed Rowan can scout but will struggle. Cinders forge marsh equipment." },
            new RegionDefinition { Id = 1, Name = "Whispering Marsh", BossName = "The Mire Warden", Weakness = "Flame", Description = "Lost ferries drift through venomous mist. Flame sears the spirits; Frost is resisted. Supply: suggested power 28 and guard 16. Marsh glass forges Frostpeak equipment." },
            new RegionDefinition { Id = 2, Name = "Frostpeak", BossName = "The Frostfang", Weakness = "Flame", Description = "An icebound watchtower guards the mountain pass. Flame shatters its curse; Frost is resisted. Supply: suggested power 44 and guard 25. Peak crystals forge Caldera equipment." },
            new RegionDefinition { Id = 3, Name = "Ashen Caldera", BossName = "The Cinder Colossus", Weakness = "Frost", Description = "The mountain's ancient furnace has awakened. Frost quenches its sentries; Flame is resisted. Supply: suggested power 62 and guard 35. Obsidian forges Starfall equipment." },
            new RegionDefinition { Id = 4, Name = "Starfall Citadel", BossName = "The Starbound Sentinel", Weakness = "Storm", Description = "A fallen star feeds the citadel's void wards. Storm disrupts them; Void is resisted. Supply: suggested power 86 and guard 49. Star shards reward the restored town and endgame trade." }
        };

        static readonly HeroDefinition[] BaseHeroes =
        {
            new HeroDefinition { Id = 0, Name = "Rowan", Role = "Guardian", Cost = 0, Power = 3, Guard = 3, Health = 105, Description = "The town's last guardian. High health and guard, one attack per second. Reliable with mail; an unarmed supply scout can lose, so forge a weapon first." },
            new HeroDefinition { Id = 1, Name = "Lyra", Role = "Ranger", Cost = 180, Power = 8, Guard = 1, Health = 85, Description = "A pathfinder searching for the lost caravans. Attacks every 0.8 seconds with high power, but lower health and guard demand armor and well-timed guarding." },
            new HeroDefinition { Id = 2, Name = "Kael", Role = "Runesmith", Cost = 360, Power = 6, Guard = 2, Health = 95, Description = "Keeper of the shattered star-runes. Attacks every 1.2 seconds. Matching a region's weakness grants 60% bonus damage instead of 30%; elemental armor protects 30% instead of 20%." }
        };

        static readonly QuestDefinition[] BaseQuests =
        {
            Quest(0, 0, true, QuestKind.Craft, "A Blade for the Road", "Warden Elowen", "The Thornback has driven the charcoal wagons home. Elowen needs proof that your cold forge can arm a scout.", "Craft any Emberwood recipe.", "Elowen funds your first scouting party and opens the road ledger.", 1, 30, 5),
            Quest(1, 0, true, QuestKind.Expedition, "Beyond the Burn Line", "Warden Elowen", "Smoke hides the way to the old bridge. Return alive from an expedition with news of the burning woods.", "Win and claim an Emberwood expedition.", "A safe trail lets charcoal wagons reach the workshop again.", 1, 45, 8),
            Quest(2, 0, true, QuestKind.Boss, "The Heart of the Fire", "Warden Elowen", "The Thornback nests at the bridge. End its frenzy so the ferrymen can reach their stranded families.", "Defeat and claim the Thornback boss route.", "The bridge reopens: Whispering Marsh and its four recipes unlock.", 1, 80, 15),
            Quest(3, 1, true, QuestKind.Craft, "Light in the Reeds", "Ferryman Oren", "Oren's lantern cannot pierce the living mist. He asks for cinder-tempered equipment before risking another crossing.", "Craft any Whispering Marsh recipe.", "Oren supplies a lantern and pays for a safer marsh crossing.", 1, 60, 8),
            Quest(4, 1, true, QuestKind.Expedition, "The Missing Ferry", "Ferryman Oren", "A ferry bell still rings where no boat is visible. Follow it through the mist and bring a route home.", "Win and claim a Whispering Marsh expedition.", "The ferry landing returns to service and traders pay your passage.", 1, 85, 12),
            Quest(5, 1, true, QuestKind.Boss, "Silence the False Bell", "Ferryman Oren", "The Mire Warden imitates the ferry bell to lure travelers under. Oren begs you to break its hold on the crossing.", "Defeat and claim the Mire Warden boss route.", "The true bell rings again: Frostpeak and its four recipes unlock.", 1, 140, 20),
            Quest(6, 2, true, QuestKind.Craft, "A Warm Edge", "Captain Sera", "The pass watch has frozen its blades into their scabbards. Sera asks you to temper marsh glass into mountain gear.", "Craft any Frostpeak recipe.", "The watch funds your climb and equips the signal station.", 1, 100, 12),
            Quest(7, 2, true, QuestKind.Expedition, "The Silent Watch", "Captain Sera", "No beacon has answered from the upper pass in weeks. Climb through the icebound sentries to relight the trail.", "Win and claim a Frostpeak expedition.", "A lit beacon guides provision sleds back to the watch.", 1, 135, 16),
            Quest(8, 2, true, QuestKind.Boss, "Break the Winter Oath", "Captain Sera", "The Frostfang still guards a kingdom that no longer exists. Release his watch before the pass freezes forever.", "Defeat and claim the Frostfang boss route.", "The thaw reveals the furnace road: Ashen Caldera and its recipes unlock.", 1, 210, 25),
            Quest(9, 3, true, QuestKind.Craft, "An Unmelting Promise", "Furnacekeeper Bram", "Bram's cooling crews cannot approach the ancient furnace. Temper peak crystal into gear that can bear the heat.", "Craft any Ashen Caldera recipe.", "Bram pays the forge to equip a furnace inspection party.", 1, 150, 16),
            Quest(10, 3, true, QuestKind.Expedition, "Walk the Cooling Channels", "Furnacekeeper Bram", "The channels that once cooled the mountain are buried in living slag. Reopen a path to the pressure gates.", "Win and claim an Ashen Caldera expedition.", "The cooling channels flow again and Bram releases emergency funds.", 1, 185, 20),
            Quest(11, 3, true, QuestKind.Boss, "The Furnace Unbound", "Furnacekeeper Bram", "A molten colossus guards the pressure heart. Break its armor before a second eruption destroys the restored road.", "Defeat and claim the Cinder Colossus boss route.", "The eruption subsides: Starfall Citadel and its four recipes unlock.", 1, 300, 30),
            Quest(12, 4, true, QuestKind.Craft, "Steel Against the Sky", "Archivist Nessa", "Nessa reads the fallen star's defenses in a broken astrolabe. Obsidian equipment may finally carry a charge through them.", "Craft any Starfall Citadel recipe.", "Nessa pledges the archive treasury to the final ascent.", 1, 220, 20),
            Quest(13, 4, true, QuestKind.Expedition, "The Stair of Fallen Kings", "Archivist Nessa", "Every stair remembers a ruler consumed by the star. Establish a living route through the empty royal guard.", "Win and claim a Starfall Citadel expedition.", "The citadel stair opens to scholars and returning families.", 1, 275, 25),
            Quest(14, 4, true, QuestKind.Boss, "A Town Beneath the Stars", "Archivist Nessa", "The Starbound Sentinel offers endless power at the cost of the valley. Break its throne and bring the sky back to your town.", "Defeat and claim the Starbound Sentinel boss route.", "The valley is free. The campaign ends; every route remains open for mastery and trade.", 1, 500, 50),

            Quest(15, 0, false, QuestKind.Delivery, "Mara's First Patrol", "Mara the Miller", "Mara's son has joined the bridge patrol with a wooden practice blade. She offers her savings for a real Iron Sword.", "Deliver one unequipped Iron Sword.", "The sword leaves your inventory for the patrol; Mara pays gold and gives two cinders.", 1, 35, 6, 0, 0, 2),
            Quest(16, 0, false, QuestKind.Rescue, "The Charcoal Children", "Tamsin", "Tamsin's apprentices hid in a kiln when the roots awakened. An elite sweep can reach them before the smoke does.", "Win and claim the Emberwood elite route.", "The apprentices return, restore the town hearth, and bring three cinders. Town restoration rises.", 1, 55, 10, 0, 0, 3, 1),
            Quest(17, 0, false, QuestKind.OreSupply, "Nails for the Bridge", "Carpenter Pell", "Pell has timber but no nails. Bring common iron ore so he can repair the bridge without taking your finished weapons.", "Deliver eight common iron ore.", "Eight ore become bridge nails; Pell pays gold and shares two cinders from the old supports.", 8, 30, 6, 0, 0, 2),
            Quest(18, 1, false, QuestKind.Delivery, "Oren's Last Lantern", "Iva the Lamplighter", "Iva's brother follows a false light each night. A Reedfire Sword will let her burn the spirit reeds around his door.", "Deliver one unequipped Reedfire Sword.", "Iva takes the blade, lights the landing, and pays gold plus two marsh glass.", 1, 70, 10, 4, 0, 2),
            Quest(19, 1, false, QuestKind.Rescue, "A Voice in the Rushes", "Healer Fen", "Fen hears her missing mentor singing beyond the safe channel. An elite escort can cut through the mimics holding him.", "Win and claim the Whispering Marsh elite route.", "The mentor returns to reopen the town clinic, bringing three marsh glass and restoration.", 1, 95, 14, 0, 0, 3, 1),
            Quest(20, 1, false, QuestKind.QualityDelivery, "A Veil for the Bellkeeper", "Bellkeeper Daro", "Daro refuses to abandon the real ferry bell. Only a carefully struck Thunderfen Ward can keep its echoes from his mind.", "Deliver a quality 2+ Thunderfen Ward.", "Daro wears the delivered ward at his post and pays gold with two marsh glass.", 1, 110, 15, 7, 2, 2),
            Quest(21, 2, false, QuestKind.Delivery, "The Avalanche Rope", "Climber Edda", "Edda's rescue rope is trapped in cursed ice. A Dawnfire Sword can free it without dropping the climbers below.", "Deliver one unequipped Dawnfire Sword.", "Edda takes the sword, recovers the rope, and pays gold plus two peak crystals.", 1, 120, 14, 8, 0, 2),
            Quest(22, 2, false, QuestKind.Rescue, "The Last Beacon Tender", "Signalman Tor", "Tor's sister has kept the upper beacon burning alone. Break the elite siege and bring her down before the lamp runs dry.", "Win and claim the Frostpeak elite route.", "The tender returns to restore the town watchtower, carrying three peak crystals.", 1, 155, 18, 0, 0, 3, 1),
            Quest(23, 2, false, QuestKind.QualityDelivery, "A Cradle of Ice", "Naturalist Una", "Una found a firebird egg in the snow. A flawless cold ward will carry it safely through the village without burning homes.", "Deliver a quality 2+ Glacier Ward.", "Una consumes the ward as a safe cradle and rewards gold with two peak crystals.", 1, 170, 19, 11, 2, 2),
            Quest(24, 3, false, QuestKind.Delivery, "A Cut Through the Slag", "Miner Jory", "Jory's father's tools lie behind a hot slag wall. He asks for a Winterheart Sword rather than risking another bare-handed dig.", "Deliver one unequipped Winterheart Sword.", "Jory takes the sword, reclaims his tools, and pays gold plus two obsidian.", 1, 180, 18, 12, 0, 2),
            Quest(25, 3, false, QuestKind.Rescue, "The Cooling Crew", "Engineer Ves", "Ves's crew is trapped between two broken pressure gates. Clear the elite channel before the furnace vents again.", "Win and claim the Ashen Caldera elite route.", "The rescued engineers restore the town aqueduct and bring three obsidian.", 1, 225, 22, 0, 0, 3, 1),
            Quest(26, 3, false, QuestKind.QualityDelivery, "Bram's Winter Refuge", "Cook Anja", "Anja shelters mountain refugees in a drafty cellar. A carefully tempered Pyrebreaker Ward can warm the room without open flames.", "Deliver a quality 2+ Pyrebreaker Ward.", "Anja installs the delivered ward in the refuge and pays gold plus two obsidian.", 1, 240, 23, 15, 2, 2),
            Quest(27, 4, false, QuestKind.Delivery, "The Astronomer's Daughter", "Astronomer Sol", "Sol's daughter is bound to a broken observatory lens. A Skybreaker Sword can discharge it without shattering her memories.", "Deliver one unequipped Skybreaker Sword.", "Sol takes the sword, frees his daughter, and pays gold plus two star shards.", 1, 260, 22, 16, 0, 2),
            Quest(28, 4, false, QuestKind.Rescue, "The Names Still Living", "Scribe Mira", "Mira has found living prisoners behind the citadel's royal names. Lead an elite rescue before the archive erases them.", "Win and claim the Starfall Citadel elite route.", "The returned scribes restore the town library and bring three star shards.", 1, 315, 26, 0, 0, 3, 1),
            Quest(29, 4, false, QuestKind.QualityDelivery, "A Roof for Tomorrow", "Builder Hal", "Hal wants the rebuilt town hall to withstand falling star debris. A masterfully struck Nightstar Ward can anchor its roof.", "Deliver a quality 2+ Nightstar Ward.", "Hal installs the delivered ward above the hall and pays gold with two star shards.", 1, 350, 30, 19, 2, 2)
        };

        private static RecipeDefinition Recipe(int id, int region, GearSlot slot, string name, string description, string element, int ingots, int rare, int value, int power, int guard)
        {
            return new RecipeDefinition { Id = id, Region = region, Slot = slot, Name = name, Description = description, Element = element, Ingots = ingots, RareCost = rare, Value = value, Power = power, Guard = guard, Icon = id };
        }

        private static QuestDefinition Quest(int id, int region, bool main, QuestKind kind, string title, string npc, string description, string objective, string consequence, int target, int gold, int reputation, int recipe = 0, int quality = 0, int material = 0, int town = 0)
        {
            return new QuestDefinition { Id = id, Region = region, Main = main, Kind = kind, Title = title, Npc = npc, Description = description, Objective = objective, Consequence = consequence, Target = target, RewardGold = gold, RewardReputation = reputation, RecipeId = recipe, Quality = quality, RewardMaterial = material, RewardTown = town };
        }
    }
}
