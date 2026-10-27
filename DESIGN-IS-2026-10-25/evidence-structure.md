# Structural evidence — v0.4.0

## Sources consulted

- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:59–91,108–189,204–244,341–361`.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Navigation.cs:44–88,120–150,167–174`.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Campaign.cs:10–24,98–146,149–229,232–260`.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Battle.cs:19–76,83–103,113–124`.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Town.cs:20–36`.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Progression.cs:46–54,197,266,295`.
- `Game/Assets/ForgeEmpire/Runtime/ForgeUI.cs:82–91,119–127,153–164`.
- `Game/Assets/ForgeEmpire/Core/GameContent.cs:35–66`.
- `Game/Assets/ForgeEmpire/Tests/RedesignSmoke.cs:41–52,75–104,153–176,193–207,211–232`.
- `Game/Assets/ForgeEmpire/Tests/ProgressionSmoke.cs:32–109,151–179`.
- `Game/Assets/ForgeEmpire/Editor/ForgeBuild.cs:47–52,73–83`.

All counts below are **INFERRED from constructors and visibility predicates**, not a new live hierarchy dump. They count Button objects, separately identifying active/interactable status; they do not claim every active object is simultaneously visible through a scroll mask.

## Concrete findings

### Interactive elements

| Surface | Constructed / active Button count | Source and arithmetic |
| --- | --- | --- |
| Workshop | 20 constructed; 14–18 active configured controls across height/station/guide states | Header: Settings, GuideAction, TabQUESTS, TabCREW = 4 (`WorkshopView.cs:112,130,136–137`). Diorama: 4 station hit areas + StockRack + CustomerVisit + TownNoticeboard = 7 (`:183–189`, `Town.cs:30`). Dock: ContextAction, Strike, UpgradeSelected, TabRECIPES, CraftPurpose, MerchantPause = 6 (`:212–222`). Navigation = 3 (`:230–244`). Active dock subsets and short-screen Crew hiding are `Navigation.cs:47–80`; action/Strike are mutually exclusive (`Progression.cs:49–54`, `WorkshopView.cs:381`). Guided tall workshop has 15 active objects; short workshop hides Crew for 14. Free-play Forge at rest has 18. Disabled working action buttons can reduce interactable count further. |
| World | U + 6, where U is unlocked regions (1–5): 7–11 | U pins in a 5-iteration loop, 2 trailing actions, PageBack and 3 navigation buttons (`Campaign.cs:213–228`; `Navigation.cs:138`; `WorkshopView.cs:230–244`). Locked nodes are glyphs, not Buttons (`Campaign.cs:218–219`). Active/finished battles redirect this entry to battle instead (`:207–208`). |
| Recipes | 4 + 2R + T active objects, R visible recipes (4–20), T toggle (0 or 1) | Two actions per visible row, plus PageBack and 3 navigation buttons (`Campaign.cs:109–130`). First chapter normally has R=4, T=1 => 13; exposing all locked recipes creates 45. At full unlock without the toggle, 44. Recipe-select buttons are disabled for locked recipes or active Forge; Details remains a separate button (`:123–128`). |
| Battle | 8 constructed incl. navigation; 7 active while fighting, 8 on result | Three skills, one result button, PageBack, 3 primary nav (`Battle.cs:48–63`; `Navigation.cs:138`; `WorkshopView.cs:239`). Result button is hidden before finish (`Battle.cs:95–96`). While active, 0–3 skills can be interactable depending on cooldown, HP and heal use; after finish all 3 are disabled, leaving 5 interactable controls including result/nav/Back (`:84–99`). |

Page creation disables covered workshop input with a CanvasGroup (`Navigation.cs:132–133`), so underlying 20 objects are excluded from page interaction counts. Scroll containers add drag affordances, rather than Buttons: workshop world (`WorkshopView.cs:144–147`) and page viewport (`Navigation.cs:143–147`).

### Component depth

Maximum observed constructor path among these primary surfaces is **7 parent edges / 8 GameObjects including owner**: `WorkshopUI → Workshop → WorkshopViewport → WorkshopContent → WorldViewport → Diorama → WorldMine → Label`. Creation is `WorkshopApp.cs:44`, `WorkshopView.cs:68–76,142–147,183–185`, and Button-label child creation `ForgeUI.cs:88`. Page art path has 6 edges: `WorkshopUI → Workshop → Page → PageViewport → PageContent → BattleStage → BattleEnemy` (`Navigation.cs:129–147`, `Battle.cs:27–31`). Other components/scripts on these same GameObjects do not add Transform depth.

### Repeated purposes

- **Three explicit quest-journal entry controls**: workshop TabQUESTS (`WorkshopView.cs:136`), world MapQuests (`Campaign.cs:228`), journey JourneyStories (`Progression.cs:197`), all call `ShowQuests(false)`. Quest-page Story tab also calls this method to switch tab (`Campaign.cs:33`).
- **Four explicit hero-page entry controls**: bottom Heroes (`WorkshopView.cs:234–239`), world MapHeroes (`Campaign.cs:227`), mastery MasteryHeroes (`Progression.cs:266`), and departure DepartureEquipment (`Progression.cs:295`). Recipe/route fallback logic when an unarmed hero is encountered (`Campaign.cs:71,255`) is conditional navigation rather than another persistent affordance.
- **Two explicit recipe entry controls**: workshop TabRECIPES (`WorkshopView.cs:217`) and empty-storage InventoryRecipes (`Modals.cs:110`). Recipe fallback via guided default (`Progression.cs:91`) is conditional, not an extra persistent control.
- Inventory is explicitly opened from StockRack and HeroEquipment (`WorkshopView.cs:188`, `Campaign.cs:177`), with conditional ContextAction fallback (`Navigation.cs:98,102`).

The numbered lists distinguish simultaneous chrome from context-specific links; they are counts of entry definitions, not a claim that all are redundant.

### Unused / inactive presentation objects

- **One proven permanently hidden object in inspected code:** `JourneyDetail` is constructed as a 1×1 label and immediately deactivated (`WorkshopView.cs:135`); a full runtime-folder search finds no subsequent use apart from the field declaration (`:14`).
- **Unused import/dead-property total: not measured.** No C# semantic unused-symbol analyzer was run. Text search alone is not sufficient to assert a global zero, especially across partial-class files.

## Art consumers and stable mappings

- `CampaignSprite` special-cases only `heroes` into HandDrawnArt; all other assets currently load `Resources/Art/<asset>` with horizontal `frame/count` UVs (`Campaign.cs:10–14`). `Landscape` additionally crops the shared 5-region battlefield strip vertically to fit a banner (`:16–20`).
- `EquipmentIcon` clamps stable recipe IDs 0–19 and maps them into a **20-cell horizontal strip** (`Campaign.cs:22–24`). It is used in recipes/detail (`:115,137`), hero gear (`:188`), storage (`Modals.cs:108,117`), mastery/result/order pages (`Progression.cs`, via the same helper), and workshop stock (`WorkshopView.cs:173`).
- Equipment also has **two direct UV writes outside EquipmentIcon**: material-flight creation sets `Art/equipment` and `recipe/20` (`WorkshopView.cs:347–348`), and live workshop racks overwrite their UV from inventory RecipeId (`:361`). A multi-row replacement cannot rely on changing the helper alone.
- Stable order is sword, axe, mail, ward per region, five regions (`GameContent.cs:35–58`). Region IDs/names are Emberwood 0, Whispering Marsh 1, Frostpeak 2, Ashen Caldera 3, Starfall Citadel 4 (`:60–66`).
- Enemy strip is **5 regional creatures × 4 poses**, not 20 different species. `enemyPose` chooses ready/attack/hurt/defeated positions, then UV `(Region*4 + pose)/20` (`Battle.cs:117–119`). Boss final-wave distinction in this view changes size 230 → 320 (`:120`), while encounter text comes from simulation (`:86`).
- World map is one strip frame, drawn 1020×1275 from source 384×480; five pin coordinates are fixed in those source coordinates (`Campaign.cs:212–224`). Locked regions get lock glyphs; only unlocked regions get name labels (`:218–225`).
- Hero landscape, quest landscape, region backdrop, battle stage, journey and departure/result art share the battlefield/landscape helper (`Campaign.cs:78,169,235`; `Battle.cs:28`; `Progression.cs`), so a shared helper reaches multiple pages.
- Current Editor verification explicitly requires legacy `Art/enemies`, `equipment`, `worldmap`, `battlefields` to exist with Point filtering (`ForgeBuild.cs:73–77`); generated HandDrawn textures are a separate Bilinear list (`:80–83`).

## Native QA contracts and coverage

- `RedesignSmoke.cs:43` asserts exactly 3 primary navigation buttons; `:48–52` asserts GuideAction, no StationTabs/GoldPanel, no permanent ContextDock Image, and workshop world occupying ≥80% root height.
- Existing flow uses `Region0`, `StartRoute0`, `StartRoute1`, `BattleSkill0/1`, `ClaimExpedition`, and screenshot names adventure/routes/battle-active/victory/defeat (`:75–104`). Those names are behavioral hooks.
- Text-size/reduced-motion state and short-screen scroll/focus remain asserted (`:153–176`); all captured meaningful labels must be at least 42 design units, fit their allocated height, and remain inside workshop horizontal bounds (`:193–207`). Click probes scroll targets and verify actual EventSystem raycast is unobscured (`:211–232`).
- Progression probes exercise direct equip, story continuation, guided departure, chapter unlock, order delivery/reservation, recovery, 150% text and compact pages (`ProgressionSmoke.cs:32–109`).
- **Gap:** neither inspected native suite asserts enemy texture identity, per-frame enemy foot pivots, generated equipment frame identity, map art/pin correspondence, or landscape style. Screenshot capture and text checks alone do not establish those properties.

## Per-principle facts (no scores)

- #2/#4: guided primary action, region pins and real recipe/skill/result controls are explicit; counts vary with progression and scroll state.
- #5/#10: workshop configured controls range 14–18, while pages disable underlying interaction; artwork itself is non-raycast (`ForgeUI.cs:161–164`).
- #3/#7: shared art helper still routes enemy/equipment/map/landscapes to the original pixel strips while heroes/workshop use generated HandDrawn sources.
- #6: region labels do not appear on locked locations; their marker communicates only locked status.
- #8: native tests cover text/layout/click/state flows, but not the five new-art properties listed above.

## Known gaps

No new live Unity hierarchy dump, keyboard traversal of every surface, semantic unused-code analyzer, or runtime game profiler was run by this evidence worker. Counts are source-derived and do not imply all scroll rows fit onscreen at once. The audit describes v0.4.0 before this pass's product edits.
