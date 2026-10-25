# Forge Empire: Chronicles expansion

User direction: implement all eight recommendations following the illustrated-world release. This is an implementation brief, not a claim of AAA production scale.

Latest explicit art direction: all human faces are featureless, with eyes, nose and mouth removed. Preserve hair, head shape, clothing, anatomy and animation. This applies to workers, townsfolk, heroes, NPC portraits and humanoid enemies; animal/creature anatomy remains intact.

## Playable result

- Six persistent regions, six recruitable heroes, parties of up to three, 108 recipes, 48 ordinary enemy definitions and twelve bosses.
- Keep recipe IDs 0–19, regions 0–4, heroes 0–2 and quest IDs 0–29 stable. Append the sixth region and quests; do not reinterpret an old inventory or quest claim.
- A named masterwork goes through heat/alloy, shaping, quenching and engraving. Materials are reserved once, stages survive a save, ordinary worker production remains automatic. Material, temper and rune choices change actual combat stats/effects.
- Three simultaneous party members have distinct role skills. Gear is locked for a run. Damage, cooldowns, injury and rewards are deterministic, bounded and persisted. Each boss has three phases, an announced mechanic and a useful response.
- Expeditions have a short route of branching rooms: combat, camp, treasure, rescue or risk. Health carries between rooms; rewards settle once at the end. The next action is always visible.
- Six restoration projects and recurring named townspeople connect quests, mechanical benefits, dialogue and visible world changes. Optional mastery, modifiers, legendary commissions and legacy provide goals after the story without deleting the player's collection.
- Hand-drawn art remains coherent. Weapons change visually with loadout, characters have readable anticipation/contact/recovery and distinct brace/hurt/cast/victory states, feet remain grounded, reduced motion remains supported. No floating text panels covering the workshop.
- Region ambience, authored musical themes, boss intensity and material-specific impacts obey separate music/sound settings. Large mobile text, portrait layout and touch targets remain readable.
- Content tools validate IDs, references, counts and balance data. Include a playtest checklist and local session metrics; do not imply actual human playtests occurred.

## Integration boundaries

The legacy production clock and saves remain authoritative. New serialized fields have safe defaults. `WorkshopState.ChroniclesEnabled` enables the new adventure path in the normal app. Existing in-flight legacy battles finish using legacy rules. Core modules use C# 7.3 and no Unity APIs. Runtime owns presentation only.

- Content owner: GameContent and ChronicleContent, dynamic normalizers/progression boundaries, content validation and content tests.
- Craft owner: ArtisanState and WorkshopSimulation.Artisan, artisan tests. Root installs clock/completion hooks.
- Combat owner: SagaState and WorkshopSimulation.Saga, saga tests. Root installs legacy routing hooks.
- Root: shared field additions, integration hooks, new UI pages, generated art/animation, audio, native probes, delivery.

## API contract

`GearSlot` appends Accessory. `WeaponKind` is Sword, Axe, Bow, Hammer, Staff, Daggers, Greatsword, Spear, None. `GearEffect` is None, ChainLightning, BraceBarrier, LifeOnKill, ArmorBreak, Mend, Haste, Counter, Burn, Frostbite, Focus, Shelter.

`CraftedItem` appends Alloy, Temper, Rune, Masterwork and Provenance (integers, except Masterwork boolean). HeroState appends Accessory. WorkshopState appends Artisan and Saga states. The constructor invokes their normalizers after legacy normalization. Gear stat calculation includes artisan modifiers and accessory stats; effect lookup is shared by saga combat and item descriptions.

Content exposes `GameContent.RegionCount`, `RecipeCount`, `HeroCount`, `QuestCount`, `MainQuestId(region,step)`, `SideQuestId(region,step)` and `ChronicleContent.Enemies[48]`, `Bosses[12]`. RecipeDefinition adds Kind and Effect. Enemy fields: Id, Region, Archetype, Power, Health, Guard, AttackTicks, Name, Description, Element. Boss fields: Id, Region, Mechanic, Health, Power, Name, Description, Telegraph, Counter.

Artisan API: `NormalizeArtisan()`, `BeginMasterwork(int recipeId,int alloy)`, `StrikeMasterwork(double accuracy)`, `QuenchMasterwork(int temper)`, `EngraveMasterwork(int rune)`, `CompleteMasterwork()`, `MasterworkActive`, `ArtisanStat(CraftedItem,bool power)`, `ItemEffect(CraftedItem)`, `ItemBuildDescription(CraftedItem)`. Stages enum Heat, Shape, Quench, Engrave, Ready, Complete; root controls a paid project through these actions. Advance clocks must leave the reserved forge waiting for input. Normal worker output does not require this flow. All invalid/duplicate actions return false.

Saga API: `NormalizeSaga()`, `SetParty(int slot,int heroId)`, `StartSaga(int region,int route,int modifier=0)`, `ChooseSagaPath(int choice)`, `UsePartySkill(int slot)`, `AdvanceSaga(double seconds)`, `ClaimSaga()`, `SagaInProgress`, `SagaNextAction`, `SagaMechanicHint`. `SagaState.Party[3]`, `Run`, `BossMastery[12]`, `Mastery`, `Legacy`. Run fields include Region, Route, Room, Phase, Units, EnemyId, BossId, EnemyHP, EnemyMaxHP, BossPhase, Telegraph, Mechanic, RewardGold, RewardMaterial. Phase enum Choice, Combat, Camp, Result. Units snapshot HeroId, HP, MaxHP, Power, Guard, Cooldown, Barrier, Attacks, Action. Phase transitions and all party member health are persisted. Combat projects the current encounter into Campaign.Battle for existing effects and counters without using legacy damage logic. Root routes normal StartExpedition/UseBattleSkill/ClaimExpedition to this module, except a legacy battle already in flight.

## Acceptance

Meaningful core tests cover: old-save migration, all content reachable, paid job immutability and one delivery, waiting across offline time, trait effects, three-member snapshots and locked gear, all twelve boss mechanics, route choice consequences, once-only loot, resumable state, optional legacy without loss, malformed values. Native Windows and Android probes exercise the actual new UI flow, readability, all regions/loadouts and reduced motion. Existing regression suites remain passing or receive explicit expanded-count expectations.
