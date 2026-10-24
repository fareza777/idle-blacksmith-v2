# Gameplay and copy evidence — 24 October 2026

Evidence gathering only. This document contains source observations and proposed plain wording. It contains no design scores or verdict. No gameplay code, settings, or save data were changed.

## Inspected scope and source keys

All source references below use these exact current-project paths. Numeric references after a source key are inclusive source line ranges; they do not refer to the QA baseline copy.

| Key | Source |
|---|---|
| Content | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/GameContent.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/GameContent.cs>) |
| State | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopState.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopState.cs>) |
| Campaign | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/CampaignState.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/CampaignState.cs>) |
| Sim | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopSimulation.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopSimulation.cs>) |
| Combat | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopSimulation.Campaign.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopSimulation.Campaign.cs>) |
| App | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopApp.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopApp.cs>) |
| View | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs>) |
| Modals | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Modals.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Modals.cs>) |
| Screens | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Campaign.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Campaign.cs>) |
| Battle UI | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Battle.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Battle.cs>) |
| Town UI | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Town.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Town.cs>) |
| UI helper | [D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/ForgeUI.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/ForgeUI.cs>) |

Also inspected WorkshopSave.cs for the local-save flow. Source findings are not claims about what a player actually noticed, understood, or enjoyed on a device.

## First session and onboarding

| Fact | Evidence |
|---|---|
| A new workshop starts with 40 gold, no ore, ingots, or inventory, all four stations at level 1, no automated stations, selected recipe 0, and TutorialStep 0. | State:20–38,49–50; C# default values for uninitialized numeric and Boolean fields. |
| Rowan is recruited automatically; the initial hero has no weapon or armor. His base values are 3 power, 3 guard, 105 health. | Campaign:26–32; Combat:341–370; Content:69–74. |
| Initial recipe 0 is the Iron Sword: 2 ingots, no rare ingredient, base value 24 gold, 12 power. All four Emberwood recipes are initially unlocked because recipe region 0 is at or below chapter 0. | Content:38–41; Combat:36–55,355–357. |
| The first launch shows one welcome modal. Its large loop is “MINE  >  SMELT  >  FORGE  >  SELL”. Its prose also says to craft gear, arm heroes, rebuild towns, follow quests, equip a hero, then choose a world-map route. | App:20–39; Modals:40–47. |
| “LIGHT THE FORGE” only sets TutorialStep from 0 to 1, closes the modal, saves, and plays a sound. A source search found no later contextual tutorial step or TutorialStep-dependent guidance in the inspected Core/Runtime files. TutorialStep otherwise exists as a field and is normalized. The workshop begins focused on Mine; the quest bar points to the first crafting quest. | Modals:47; State:37; Sim:312; View:37,117–125,262–265,372–378. |
| To produce the first default sword manually, the resource arithmetic requires two mining cycles, two smelting cycles, and one forge cycle. Selling it requires a sixth station start. This is a source-derived action count, excluding strikes, upgrades, navigation, and overlapping cycles. | Sim:45–74,239–264; Content:38. |
| Each trip gives 2 ore; smelting consumes 2 ore and produces 1 ingot. The base cycle times are 1.6 seconds Mine, 2.4 seconds Smelter, 4.5 seconds Forge, 2 seconds Market. | Sim:21,54–62,173–183,239–264. |
| Starting wealth can buy one of the Mine/Smelter/Market upgrades (20/30/35 gold), but cannot buy the 45-gold Forge upgrade or any worker. Worker prices are 60/90/140/110 gold. Spending therefore competes with saving for automation and heroes immediately. | State:20; Sim:22–23,77–97,186–195; Content:72–73. |
| The first main quest is to craft any Emberwood recipe, rewarding 30 gold and 5 reputation on claim. It does not require a sale. A free unarmed Emberwood Supply Trail is permitted, but every other region/route combination requires a weapon. | Content:78; Combat:92–105,162–197,200–222. |
| The onboarding sale chain presents only one destination for crafted gear, while the campaign also requires retaining gear for equipping and deliveries. Equipping removes an item from stock; delivery removes a matching unequipped item. The Crew modal explicitly tells players to pause the merchant to retain gear, but this explanation lives behind the crew icon. | Modals:45–46,53; Combat:58–72,170–192,200–227; Sim:66–69. |

Suggested onboarding wording, for later implementation review: “Mine ore → smelt ingots → forge gear. Equip your hero or save gear for town requests; sell spare gear for gold.” Follow with a concrete first objective: “Make an Iron Sword: mine twice, smelt twice, then tap the forge. Open Heroes to equip it.” This is a proposed instruction, not an existing guided sequence.

## Ongoing production, chores, and economic decisions

| System | Source facts and practical implication |
|---|---|
| Manual work | A station needs a tap for each new cycle until its worker is hired. Inputs are removed at cycle start. Tapping a running forge triggers a strike; taps on another running station simply fail to start another cycle. Sources: Sim:45–74; App:53–69. |
| Timing strikes | Every accepted strike adds 10% forge progress, with a 0.2-second minimum interval. Accuracy at least 0.82 adds one quality, capped at 3, and increments combo; a less accurate strike resets combo. The marker and highlighted timing window appear only while the selected station is forging. Sources: Sim:110–126; View:209–215,297–300,361–371. |
| Quality payoff | Quality 0/1/2/3 becomes COMMON/FINE/SUPERIOR/MASTERWORK in inventory. Each step adds roughly 25% to base item value and equipment power/guard, using integer arithmetic. The forge shows numeric quality, while inventory shows only the names. Sources: Sim:248–255; Combat:247–251; Modals:99,127–129; View:371. |
| Automation | Hiring is permanent; each worker automatically starts its station when inputs are available. Pause prevents new automatic jobs but lets the current reserved job finish. Manually tapping a paused station still works. Sources: Sim:88–107,220–225; State:26–30; View:371; Modals:53,62–63; App:98–101. |
| Merchant allocation | A sale reserves and removes the oldest stock item immediately, then grants gold when its cycle ends. Automatic market starts are attempted before the other stations. There is no per-item sell button or keep/favorite switch in the inspected inventory UI; its sale action is “SELL NEXT ITEM”. Equipping a replacement puts the old equipped item back at the end of stock. Sources: Sim:66–69,220–225,261–264; Combat:58–72; Modals:84–135. |
| Production constraints | Forge needs the selected recipe's ingots, previous-region material if applicable, and a free stock slot. Stock capacity is 120. At level 1, a continuously supplied smelter produces one ingot per 2.4 seconds; the default sword consumes two ingots per 4.5-second forge cycle. Thus its sustained material supply is slower than its unassisted forge cycle. This rate comparison is derived from the source, not measured in play. Sources: Sim:11,21,54–62,173–183; Combat:36–42; Content:38–57. |
| Later ingredients | The first region's four recipes need no rare ingredient. Each later region's recipes cost 2 or 3 units of the previous region's material and progressively 4–11 ingots. Expeditions and specified side quests supply these materials. Recipe choice therefore changes both material demand and battle strength. Sources: Content:38–57,94–108; Combat:134–158,200–221. |
| Upgrades | All station upgrades shorten duration by dividing it by 1 + 0.14 × (level − 1), to level 25. Forge duration also depends on recipe region/type. Market upgrades additionally add 8% sale value per level above 1. Upgrade price grows by a factor of 1.55 per level. The dock displays current duration and upgrade cost; it does not show the next duration or the market value bonus. Sources: Sim:10,173–195,261–264; View:364,368–369. |
| Gear/hero choice | The player chooses a recipe, equipment for the selected hero, and one of three heroes. Rowan is free; Lyra costs 180 gold, Kael 360. An inventory EQUIP action targets the selected hero; the inventory modal itself does not name that hero. Sources: Content:69–74; Combat:58–89; Screens:129–167; Modals:123–131. |
| Offline behavior | The same simulation advances for at most 8 hours, including running manual work, automated production chains, and the active battle. The return modal appears after at least 60 seconds away and reports gold plus elapsed minutes; it explains the 8-hour crew limit. Gold is already applied before the modal opens; its button only closes it. Sources: Sim:17,129–170; App:29–38,165–175; Modals:157–163. |

## Quests, unlocks, rewards, and battle agency

| System | Evidence |
|---|---|
| Main campaign | Five regions, 20 recipes, 15 ordered main quests: craft regional gear → win and claim a regional expedition → defeat and claim the boss route. Only the earliest unclaimed main quest is available. Progress counters are cumulative, so earlier completed crafts/wins can satisfy a newly available quest. Sources: Content:36–92; Combat:28–33,162–197. |
| Actual unlock trigger | A boss win alone does not advance Chapter. Claiming the completed boss story quest increments it; four recipes then unlock because their region is at or below Chapter. The world-map hint states both boss defeat and story-quest claim. Sources: Combat:46–48,200–221; Screens:180–186. |
| Town requests | Fifteen side quests are region-gated but do not require sequential side-quest completion. They include item delivery, quality 2+ armor delivery, elite-route rescue, and an 8-common-ore delivery. Deliveries consume stock; ore delivery consumes ore on claim. Five rescue requests each add one TownLevel. Sources: Content:94–108; Combat:162–192,200–227. |
| Visible restoration | TownLevel is stored up to 5, but this view creates only three restoration overlays and displays each for TownLevel > 0/1/2. No additional fourth/fifth visual restoration is specified here. Sources: Campaign:12; Combat:220,358–359; Town UI:8,17–20,58–67. |
| Trading commissions | A separate panel is reached through Quests → TRADING COMMISSIONS. Cumulative sale thresholds are 3, 5, 7, …; rewards are 40, 65, 90, … gold. Claiming is manual. The panel says it counts all sold equipment. Source: Sim:208–217; Screens:61; Modals:70–81. |
| Battle preparation | Each route uses one selected hero. Equipment and hero selection are locked during an active expedition; combat stats are snapshotted at departure. Starting an expedition has no entry-cost deduction in its method. Only the initial Emberwood Supply Trail permits no weapon. Sources: Combat:58–105,254–267; Screens:143–148. |
| Route structure | Supply Trail has 2 encounters; Dangerous Hunt and Boss have 3. Elite enemies have 1.25× base health; boss-route normal encounters 1.4×; the final boss 3×. Route cards describe relative risk but show no exact expected gold/material/XP before departure. Sources: Combat:269–289; Screens:196–205. |
| Auto combat | Rowan attacks every 1 second, Lyra every 0.8, Kael every 1.2. Enemies attack every 2 seconds, with every third attack 1.6× heavier. A 20-minute limit ends an unfinished battle as a loss. Combat continues when its modal is closed and while offline simulation advances. Sources: Content:71–73; Combat:7–8,254–266,309–337; Sim:159–169; Battle UI:46. |
| Active skills | POWER STRIKE immediately deals 1.8× snapshotted damage, with a 4-second cooldown. BRACE reduces damage received to 30% for 1.8 seconds, with a 6-second cooldown. RECOVER heals 40% max HP once per expedition. The UI telegraphs a heavy attack during the last 1.5 seconds before each 6-second point, then tells the player to BRACE. Sources: Combat:108–131,326–336; Battle UI:37–42,59–69. |
| Elements | Matching the region weakness multiplies weapon damage by 1.3, or 1.6 for Kael; a resisted element multiplies damage by 0.75. Matching armor reduces incoming damage by 20%, or 30% for Kael. Item and hero descriptions explain many of these effects. Sources: Combat:11,254–267; Content:38–57,62–73. |
| Expedition reward cadence | Rewards are granted only when the finished result is collected. Region number r = 1–5: Supply gives 40r gold, 4 material, 20 + 10(r−1) XP; Hunt gives 75r gold, 7 material, 40 + 15(r−1) XP; Boss gives 110r gold, 10 material, 60 + 20(r−1) XP. Reputation on collection is 2r/4r/6r respectively. Claiming a loss clears the result and gives no loot. No gear-loss or gold-loss penalty appears in ClaimExpedition. Sources: Combat:134–158,285–298. |
| Experience | A hero levels when experience reaches current level × 30, gaining 2 base power, 1 guard, and 8 health per level; level cap 30. Hero screen shows current XP but not the XP threshold. Sources: Combat:151–158,230–244; Screens:148. |
| Campaign completion | Final story consequence explicitly says the campaign ends and all routes remain open for mastery and trade. The quest bar then reads “A kingdom reforged” / “Explore, master recipes and help your townsfolk”. Sources: Content:92; View:375–376. |

## Navigation and locked-state explanations

| Entry/state | Source facts |
|---|---|
| Main navigation | Five labeled destinations: WORKSHOP closes the modal; QUESTS opens main-story list; RECIPES, HEROES, WORLD open those modals. Crew, inventory, and settings are icon-only header buttons. The stock rack also opens inventory; the town noticeboard opens town requests. Sources: View:97–102,182,220–230; Town UI:22. |
| Modal navigation | One modal replaces another. There is an X close button and a tap-outside shade. Device Back/Escape closes an open modal. No modal history stack or “back to previous panel” behavior exists here. Sources: Modals:13–37; View:315. |
| Workshop missing input | Tapping an under-supplied station gives a toast naming 2 ore, selected-recipe ingots plus essence, full racks, or a need to craft equipment. Dock buttons are disabled and say NEED 2 ORE, NEED MATERIALS, RACKS FULL, or NO EQUIPMENT. These disabled dock buttons do not invoke the WorldTap explanation. Sources: View:251–259,360–371. |
| Recipe lock | The recipe list button displays CHAPTER n; its detail button says UNLOCK IN CHAPTER n. While forging, the detail says FINISH CURRENT CRAFT. Selecting a recipe is disabled during an active forge cycle. Sources: Screens:99–108,121–125; Combat:51–55. |
| Main-quest lock | A locked list row displays Chapter n / region, even when its real blocker is an earlier main quest in the same chapter. Detail adds “Continue the earlier story first”, but does not name that earlier quest. Completed quests also get that suffix because availability returns false for already claimed quests. Sources: Screens:54–58,77–81; Combat:162–167. |
| World lock | Future region buttons are disabled. The map supplies a global explanation: defeat the boss and claim its story quest. The locked region itself has no interactive explanation. Sources: Screens:179–187. |
| Route/start failure | DEPART buttons remain enabled for a hero without a required weapon; a failed start gives the same “Prepare your hero and collect any previous expedition result.” toast for several causes. World access redirects to the existing battle/result when one exists. Sources: Screens:173–175,198–205; App:118–121; Combat:92–105. |
| Hero switch during battle | The hero tile handler shows the recruit modal whenever SelectHero fails. SelectHero also fails during an active battle, even for an already recruited hero. The resulting recruit button is disabled for that already owned hero. This path supplies no explicit “finish expedition before switching heroes” message. Sources: Screens:135,160–167; Combat:85–89. |

## Copy observations and proposed plain wording

These are wording proposals for review only. Confirmed behavior mismatches are distinguished from added explanation.

| Existing text / location | Source observation | Proposed wording |
|---|---|---|
| “MINE > SMELT > FORGE > SELL” — Modals:45 | The displayed chain omits the retained-gear choices explained above. | “Mine → Smelt → Forge → Equip or sell”. Add “Save requested gear for town deliveries.” |
| “Sold. Your reputation is growing.” — App:48 | Confirmed unsupported reward claim: market completion adds gold and TotalSold, but no reputation (Sim:261–264). Reputation increments are in expedition collection and quest claims (Combat:146,218). | “Sold. Gold added to your workshop.” |
| “Each delivery builds your reputation.” — Modals:79 | Confirmed unsupported reward claim in trading commissions: ClaimOrder adds only gold and OrdersCompleted (Sim:212–217). | “Sell equipment to earn commission rewards. All equipment sales count.” |
| “Forges whenever two ingots are ready.” — Modals:55 | Confirmed mismatch after changing recipe: costs range from 2 to 11 ingots, sometimes plus material (Content:38–57; Combat:36–42). | “Forges your selected recipe when its materials are ready.” |
| “SELL SWORD” — View:31,367 | Market action uses this label for all stock, including mail and wards (Sim:66–69; Content:38–57). | “SELL NEXT ITEM” or “SELL OLDEST GEAR”. |
| “Craft a sword to see it here.” — Modals:96 | Armor can also appear in this inventory. | “Craft equipment to see it here.” |
| “Emberwood essence”, other region-name essence costs — Screens:97,120; View:256 | Content calls materials cinders, marsh glass, peak crystals, obsidian, star shards. UI cost and loot descriptions call the same counters essence. Recipe header abbreviates them to Emberwood / Whispering / Frostpeak / Ashen / Starfall with quantities (Screens:107–108). | Choose one visible material name per counter, for example “2 cinders”, and reuse it in recipes, inventory totals, quests, and loot. |
| “quality 2+” — Content:99,102,105,108 | Numeric crafting quality turns into a named inventory tier without numeric equivalence (Modals:99,128). | “Deliver one unequipped Superior or Masterwork Thunderfen Ward.” Use each quest's actual item name. |
| “ATK / DEF”, “POWER / GUARD”, “HP”, “XP”, “REP” — Screens:96,119,148,205; View:378 | The same combat dimensions use two naming systems. XP has no next-level denominator. | Reuse “Power”, “Guard”, “Health”, “Experience”, “Reputation”; show “20 / 30 XP” if compact labels are needed. |
| “Strike on green for stronger gear.” — Modals:46 | Accurate summary, but does not explain the cap or why every strike moves progress. | “Strike when the marker reaches green. Perfect hits raise quality, up to 3; every hit speeds the craft.” |
| “COMBO” / “GOOD STRIKE” — App:69; View:371 | Good/off-window hits reset combo; perfect hits raise it. Quality is the item benefit. No separate combo reward appears in TryStrike. | “Perfect hit · Quality 2/3”. If combo remains, explain “Consecutive perfect hits”. |
| “Crew hired. This station now works automatically.” — App:90 | Automatic starts still wait for inputs; the Crew explanation already mentions pausing merchant to keep gear (Modals:53). | “Worker hired. This station starts automatically when materials are ready.” |
| “EQUIP” — Modals:131 | Applies to selected hero, but inventory header does not name that hero. | “EQUIP ROWAN”, dynamically using the selected hero name. |
| “UNLOCK IN CHAPTER n” — Screens:124 | True, but a chapter advances through the boss story-quest claim. | “Unlock after [previous boss quest]”. Keep the chapter number as secondary context. |
| “Continue the earlier story first” — Screens:81 | Generic and also shown on claimed quests because availability is false. | For an unclaimed blocked quest: “First claim: [required quest title]”. For a completed quest: “Completed”. |
| “DEPART” — Screens:203 | Button does not name the missing-weapon requirement in advance. | When blocked: “EQUIP A WEAPON FIRST”, linking to Heroes/Inventory. |
| “Burst damage”, “Reduce incoming hits”, “One heal per expedition” — Battle UI:38 | Skills have exact effects in Combat:114–128,333. | “1.8× damage · 4s cooldown”; “70% less damage for 1.8s · 6s cooldown”; “Restore 40% health · once per expedition”. |
| “+ … ESSENCE / HERO XP” — Battle UI:66 | Victory also awards reputation; result text omits it. Exact material identity is implicit in region. | “+40 gold · +4 cinders · +20 Rowan XP · +2 reputation” using current values. |
| “UPGRADE / n G” — View:369 | Shows price, but not the resulting duration or market sale bonus. | “Upgrade: 2.4s → 2.1s · 30 gold”; show the market's additional sale-value effect. |
| “Your workshop progressed for n minutes.” — Modals:162 | Time is capped elapsed simulation time, even when no automatic chain can run; gold may be zero and material changes are not listed. | “You were away for n minutes. Offline progress is limited to 8 hours.” Then display actual gold/material changes. |

## String catalog

[runtime-ui-string-catalog.tsv](<D:/Idle Blacksmith/DESIGN-IS-2026-10-24/runtime-ui-string-catalog.tsv>) contains 423 source entries across 10 source files. Columns include full file path, exact source line, rendering channel, static/dynamic/content classification, source text or expression, and literal fragments.

The catalog was mechanically extracted from text arguments to Label, Button, Dialog, SettingsToggle, Notify, runtime text updates, runtime string arrays, Content's recipe/region/hero/quest fields, encounter names, and enums printed into labels. Content-defined names and prose are separate rows. Dynamic expressions retain their variables and formatting so counts, selected names, cooldowns, and conditional labels are distinguishable from fixed copy. Empty placeholders and internal UI object identifiers are excluded. Renderer parameters and repeated updates can produce multiple entries for the same visible label. Array members are cataloged as a group; a literal fragment can be formatting syntax rather than a separately displayed sentence. This is a source catalog, not an on-device export or a localization-ready table.

## Unsupported claims and coercive-flow scope

The two reputation promises above are unsupported by the inspected reward mutations. “Forges whenever two ingots are ready” and sword-only sale wording also disagree with general equipment behavior.

No payment shop, premium currency, rewarded ad, limited-time sale, forced subscription, streak-loss mechanic, energy purchase, purchase countdown, or externally directed purchase link was found in the inspected Core and Runtime gameplay paths. Source search also found no IAP, advertising, UnityWebRequest, Analytics, Application.OpenURL, or HTTP call in those directories. This is a bounded absence finding; it does not audit third-party packages, build configuration, or a remotely modified binary. HIRE and RECRUIT spend the earned local Gold field in the inspected code. The merchant's automatic consumption of useful gear has an existing pause control and explicit explanatory copy in Crew; no intent is inferred from that behavior.

## Known gaps

- No device playthrough, fresh-save run, touch interaction, readability timing, comprehension test, retention data, or combat-balance measurement was performed for this evidence task.
- Source proves which strings can be rendered, not which are visible without scrolling or whether they clip on a given screen. UI helper:47–48 wraps horizontally and truncates vertically; actual long quest/recipe prose needs visual checking.
- No claim is made that the proposed wording alone resolves the underlying navigation or allocation behavior.
- No save data was read or modified. QA baseline files, tests, art text baked into textures, editor tooling, package code, and network/build configuration are outside this report's main inspection scope.
