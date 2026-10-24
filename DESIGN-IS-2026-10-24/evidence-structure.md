# Structural and interaction evidence

- Audit date: 2026-10-24, Asia/Jakarta.
- Evidence mode: static source inspection; no game code, build, or user save was changed.
- Instructions: no `AGENTS.md` found at `D:/AGENTS.md`, repository root, or the `Game/Assets/ForgeEmpire/Runtime` ancestor directories inspected.
- Counting convention: a Button means a constructed Unity `Button`, including disabled controls and rows outside a scroll viewport. A ScrollRect is listed separately. This is **not** a claim that every counted control is visible at once. Modal counts exclude the workshop controls retained under the full-screen modal shade. Runtime visible/tappable counts depend on safe-area dimensions, scroll offset, and game state.
- No principle scores or verdicts are assigned in this evidence report.

## Sources consulted

- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:52-89,92-184,198-265,267-315,340-390` — UI creation, layout, navigation, station interaction and state refresh.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Modals.cs:13-165` — modal lifecycle, welcome, crew, commissions, inventory, settings, offline notice.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Campaign.cs:28-205` — quest, recipe, hero, world and route screens.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Battle.cs:18-89` — battle controls and their states.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Town.cs:14-30,58-67` — noticeboard and world shortcut.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/ForgeUI.cs:21-80` — generated hierarchy, labels, shared Button helper and navigation configuration.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopApp.cs:20-50,53-136,150-175` — startup, action dispatch, save and feedback behavior.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopState.cs:20-50` — fresh state defaults.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopSimulation.cs:21-35,45-126,173-225,228-265` — economy, resource reservation, automation and quality.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopSimulation.Campaign.cs:14-105,108-228,341-370` — recipe, equipment, hero, expedition and quest gates.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/GameContent.cs:36-108` — 20 recipes, 3 heroes, 5 regions, 15 main and 15 side quests.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Workshop.unity:135-176,204` — authored scene contains game and QA components and camera; runtime creates the EventSystem.
- `D:/Idle Blacksmith/Game/Library/PackageCache/com.unity.ugui@e20f1880fa04/Runtime/UGUI/UI/Core/Selectable.cs:798,906-1032,1200-1210` — local Unity UI behavior when navigation is None.
- `D:/Idle Blacksmith/Game/Library/PackageCache/com.unity.ugui@e20f1880fa04/Runtime/UGUI/EventSystem/InputModules/StandaloneInputModule.cs:268-281` — initial selection behavior.
- `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Tests/WorkshopSmoke.cs:63-64,114,133,155-190,247-267` and `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Tests/CampaignSmoke.cs:28-47,101` — references checked to avoid incorrectly classifying public inspection properties as dead code. Tests were read, not executed by this evidence worker.

## Total interactive-element count

### Workshop and actual initial load

| Surface/component | Constructed Buttons | Evidence |
|---|---:|---|
| Diorama: four station hotspots, four corresponding badges, stock rack, customer | 10 | `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:172-183` |
| Header: crew, inventory, settings | 3 | `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:97-102` |
| Journey / current quest card | 1 | `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:117-124` |
| Dock: action, strike, upgrade | 3 constructed; **2 active** | `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:207-211,360-370`; action and strike are mutually exclusive at line 365. |
| Bottom navigation | 5 | `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:220-231` |
| Town noticeboard, expedition shortcut | 2 | `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Town.cs:22-30` |
| **Workshop total** | **24 constructed; 23 active** | Sum of rows above. The workshop also has **1 ScrollRect** at `WorkshopView.cs:129-134`. |

- Fresh state **after welcome dismissal**, before work begins: 23 active Buttons are interactable. Mine is selected (`WorkshopView.cs:37`); fresh gold is 40 and levels are all 1 (`WorkshopState.cs:20-28`); Mine costs 20 gold to upgrade (`WorkshopSimulation.cs:21-23,186-190`); Mine supplies are unrestricted (`WorkshopView.cs:234-237`); action and upgrade availability are set at `WorkshopView.cs:360-369`. Later play can disable action or upgrade controls. Clipped/offscreen Buttons are still included in this constructed-control count.
- Actual first-ever load presents **Welcome** over that workshop: **3 modal Buttons** — shade dismissal, X dismissal, Begin — rather than exposing all workshop controls to pointer input. Startup condition: `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopApp.cs:38`; modal shade/X: `WorkshopView.Modals.cs:13-30`; Begin: `WorkshopView.Modals.cs:40-47`.
- Returning after at least 60 seconds can show the offline notice with 3 Buttons (`WorkshopApp.cs:38,175`; `WorkshopView.Modals.cs:157-163`).

### Navigation/modal surfaces

- Every modal receives **2 dismissal Buttons**: full-screen `ModalShade` and X (`D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Modals.cs:13-30`). Counts below include these two.
- Each dialog can add **1 outer ScrollRect** if its requested height exceeds `root.rect.height - 90` (`WorkshopView.Modals.cs:18-24`). The “inner scrolling” column counts scrolls created specifically by that screen, separately from this conditional outer scroll.

| Modal surface | Buttons including 2 dismissals | Inner ScrollRects | State-dependent facts and exact sources |
|---|---:|---:|---|
| Welcome | 3 | 0 | Begin plus base 2. `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Modals.cs:40-47` |
| Crew | 6 | 0 | Four hire/pause/resume controls. Fresh gold 40 is below all hire prices 60/90/140/110, so only 2 dismissals are enabled initially. `WorkshopView.Modals.cs:50-67`; `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopSimulation.cs:23` |
| Royal commissions | 3 | 0 | Claim disabled until order ready; base 2 enabled when not ready. `WorkshopView.Modals.cs:70-80`; `WorkshopSimulation.cs:208-210` |
| Inventory | 3 + N, N = stock count, 0..120 | 1 | Two dismissals + Sell + one Equip per item. Empty initial inventory: 3 Buttons, only 2 dismissals enabled. Full inventory: 123 Buttons constructed across scroll content. Equip Buttons remain enabled while an active expedition causes the action to reject. `WorkshopView.Modals.cs:84-134`; `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopApp.cs:108-111`; `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopSimulation.Campaign.cs:58-72` |
| Settings | 5 | 0 | Three state toggles. `WorkshopView.Modals.cs:138-154` |
| Offline notice | 3 | 0 | Back to forge plus base 2. `WorkshopView.Modals.cs:157-163` |
| Main story quest list | 35 | 1 | 15 rows x (open + claim) + 2 category tabs + commissions + base 2. At fresh start all claims disabled: 20 Buttons enabled across full scroll content. `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Campaign.cs:38-62`; 15 main definitions `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/GameContent.cs:78-92` |
| Town requests list | 35 | 1 | Same formula with 15 side quests; claim availability varies with stock and progress. Fresh start: 20 enabled across full scroll content. `WorkshopView.Campaign.cs:38-62`; 15 side definitions `GameContent.cs:94-108` |
| Quest detail | 5 | 0 | Claim + Recipes + World + base 2. Claim disabled until ready, so normally 4 enabled when incomplete. `WorkshopView.Campaign.cs:65-83` |
| Recipes | 42 | 1 | 20 rows x (detail + select) + base 2. At fresh chapter 1, idle forge: 4 selection controls enabled, 20 detail rows enabled, 2 dismissals = 26 enabled across full scroll content. During forging all 20 select controls disabled. `WorkshopView.Campaign.cs:86-110`; `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/GameContent.cs:36-57`; unlock logic `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopSimulation.Campaign.cs:46-54` |
| Recipe detail | 3 | 0 | Select disabled if locked/forge busy, leaving 2 enabled. `WorkshopView.Campaign.cs:113-126` |
| Heroes | 7 | 0 | Three hero tiles + inventory + world + base 2. Tiles remain clickable during active expeditions, although selection can fail. `WorkshopView.Campaign.cs:129-149`; selection gate `WorkshopSimulation.Campaign.cs:85-89`; 3 hero definitions `GameContent.cs:69-74` |
| Hero recruitment | 3 | 0 | Recruit enabled only when unowned and affordable. `WorkshopView.Campaign.cs:160-168` |
| World map | 8 | 0 | Five region pins + Prepare hero + base 2. Chapter 1 with no battle: one pin enabled, so 4 enabled. An active or uncollected finished battle routes this entry point to Battle instead. `WorkshopView.Campaign.cs:171-187`; region unlock `WorkshopSimulation.Campaign.cs:14` |
| Region / route choice | 5 | 0 | Three Depart controls + base 2. All Depart controls are enabled in the UI; the simulation rejects routes requiring a weapon when none is equipped. `WorkshopView.Campaign.cs:190-205`; `WorkshopSimulation.Campaign.cs:92-105` |
| Battle / result | 6 | 0 | Three skills + claim + base 2. Active battle: 2 dismissals + 0..3 skills enabled, claim disabled; at a new full-health battle, attack/brace enabled and recover disabled (4 total enabled). Finished battle: 2 dismissals + claim enabled (3 total). `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Battle.cs:37-69`; battle initializes at full HP `WorkshopSimulation.Campaign.cs:98-103` |

## Max nesting depth of the primary UI tree

- Convention: `Workshop` RectTransform is depth 0; depth counts parent-child edges. This excludes the parent `WorkshopUI` GameObject holding the Canvas. Include text/icon/fill leaf GameObjects; exclude Unity component internals.
- Workshop maximum: **5 edges / 6 GameObjects**, e.g. `Workshop -> WorldViewport -> Diorama -> StationBadge0 -> BadgeMine -> Label`. Hierarchy creation: `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:61,129-134,175-180`; Button adds Label child: `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/ForgeUI.cs:54-58`. Selection corner decorations also reach depth 5 (`WorkshopView.cs:164-169`).
- Ordinary quest/recipe list: **7 edges / 8 GameObjects**, e.g. `Workshop -> Modal -> Dialog -> QuestList -> QuestListContent -> QuestOpen0 -> QuestClaim0 -> Label`. Modal creation: `WorkshopView.Modals.cs:16-24`; list viewport/content: `WorkshopView.Campaign.cs:28-35`; row/nested claim: `WorkshopView.Campaign.cs:48-53`; child label: `ForgeUI.cs:58`.
- Maximum ascertainable source branch: **8 edges / 9 GameObjects** for Inventory on a short safe area: `Workshop -> Modal -> DialogViewport -> Dialog -> InventoryViewport -> InventoryList -> Item0 -> InventoryEquip0 -> Label`. Outer wrapper: `WorkshopView.Modals.cs:18-24`; inventory hierarchy `:88-94,123-131`; Label `ForgeUI.cs:58`. Outer wrapping applies to Inventory when logical root height is below 1460 (1370 > height - 90). The repository's campaign smoke reads this branch using root height 1280: `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Tests/CampaignSmoke.cs:28-36`.
- These are construction-tree counts, not evidence by themselves that a user-facing flow is overcomplicated.

## Repeated-pattern count

- Post-welcome, selected Mine: **7 same-purpose groups, 9 extra entry points beyond one per group**. This counts callbacks with the same destination/work purpose; it is not a recommendation to remove all redundant touch access.

| Repeated purpose | Affordances / count | Extras | Evidence |
|---|---|---:|---|
| Work selected Mine | WorldMine + BadgeMine + ContextAction: 3 | 2 | `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:37,172-180,207,251-259`; app work `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopApp.cs:53-60` |
| Work Smelter | WorldSmelter + BadgeSmelter: 2 | 1 | `WorkshopView.cs:172-180,251-259` |
| Work Forge | WorldForge + BadgeForge: 2 | 1 | `WorkshopView.cs:172-180,251-259` |
| Work Market | WorldMarket + BadgeMarket + CustomerVisit: 3 | 2 | `WorkshopView.cs:172-183,251-259` |
| Inventory | Header bag + StockRack: 2 | 1 | `WorkshopView.cs:99-100,182` |
| Main quests | Journey card + Quests nav: 2 | 1 | `WorkshopView.cs:119,223-227,262-264` |
| World map / battle entry | World nav + expedition shortcut: 2 | 1 | `WorkshopView.cs:223-227`; `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Town.cs:30,63-65` |

- Each modal also repeats dismissal through X and outside shade: **1 extra dismissal affordance per modal** (`D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Modals.cs:17,30`). Escape provides an additional input path (`WorkshopView.cs:315`).
- Quest reward claiming repeats on list and detail for each of 30 quests (`WorkshopView.Campaign.cs:53,74`), though the screens are never simultaneous. Recipe selection repeats on list/detail for each of 20 recipes (`:98,121`).
- In the initial workshop, the Workshop tab is an active Button whose callback is `CloseModal`, so it has no modal to close after welcome has already been dismissed (`WorkshopView.cs:223-227`; `WorkshopView.Modals.cs:34-37`).

## Primary flow actions and screen transitions

### Production -> equip -> expedition

- Basic manual production can be completed on the workshop itself. Fresh Iron Sword costs 2 ingots (`D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/GameContent.cs:38`); each mine cycle yields 2 ore and each smelt consumes 2 ore for 1 ingot (`WorkshopSimulation.cs:54-62,239-245`). With zero initial materials (`WorkshopState.cs:21-23`), creating this sword requires **5 station-start actions**: Mine, Smelt, Mine, Smelt, Forge, with waits between resource dependencies; timed strikes are optional extra actions. World taps start work directly (`WorkshopView.cs:251-259`; `WorkshopApp.cs:53-60`).
- Optional manual strike speeds forge progress by .10 and raises quality for accuracy >= .82 (`WorkshopSimulation.cs:110-126`); quality affects value and equipment stats, so sale and expedition are distinct choices for the same item (`WorkshopSimulation.cs:249-255`; `WorkshopSimulation.Campaign.cs:58-72,230-239`).
- Shortest authored route from workshop, assuming crafted item and already selected hero: **6 pointer actions** — open Inventory, Equip, dismiss Inventory, World, region pin, Depart — to start an equipped expedition. Inventory equipment handler rebuilds Inventory (`WorkshopView.Modals.cs:131`); Inventory has Sell and dismissal but no onward Heroes/World action (`:84-134`). World/region/start routing: `WorkshopView.Campaign.cs:171-205`.
- Following the Heroes screen's preparation path: Heroes -> Equip from inventory -> Equip stays on Inventory -> dismiss to Workshop -> reopen Heroes -> Choose an expedition -> region -> Depart. This is **8 pointer actions from Workshop**. The extra reopening is a source-level detour, not a tested player mistake (`WorkshopView.Campaign.cs:129-149`; `WorkshopView.Modals.cs:131,34-37`).
- Recipe selection has a useful direct return: selecting on either list/detail closes its modal and focuses Forge (`WorkshopView.Campaign.cs:98,121`). Recipe selection does not itself start the craft; its materials instruction says to tap the forge (`:108`).

### Modal replacement and loss of prior context

- Every new `Dialog()` first calls `CloseModal()`; there is no modal history stack in that constructor or close handler (`D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Modals.cs:13-37`). Opening Quest detail replaces the quest list (`WorkshopView.Campaign.cs:48,65-68`); closing detail returns to the workshop, so revisiting the list requires reopening Quests. Same pattern applies to recipe detail and hero recruitment (`:93,113-116,135,160-166`).
- The full-screen shade intercepts input over workshop navigation (`WorkshopView.Modals.cs:16-17`); it closes the modal, rather than forwarding a covered navigation click. The existing smoke test explicitly checks that tapping a covered tab closes the modal without opening that tab (`D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Tests/WorkshopSmoke.cs:186-190`).
- Long dialogs put the X inside scrollable `Dialog` content (`WorkshopView.Modals.cs:18-30`), so after scrolling down that X can move outside the visible viewport. Shade dismissal and Escape remain separate exit paths. This is inferred from the parent hierarchy; it was not tested on a device.

### Reward and campaign progression

- Expeditions run while closed (`D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Battle.cs:46`; progression `D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopSimulation.cs:159-162`). The world entry point always shows an active/uncollected result instead of the map (`WorkshopView.Campaign.cs:173-174`).
- Result collection calls `CloseModal(); ShowWorld();` (`WorkshopView.Battle.cs:45`), thus returning to the map even for the defeat label “RETURN & PREPARE” (`:69`). From that map, Prepare hero is a separate action (`WorkshopView.Campaign.cs:187`).
- Winning/claiming a boss expedition increments boss kills and grants loot, but does not advance chapter (`WorkshopSimulation.Campaign.cs:134-159`). Chapter advances only when the corresponding main boss quest is claimed (`:200-221`). The world map explains this two-stage condition (`WorkshopView.Campaign.cs:186`).
- Best case with the boss quest already current: after collecting the result and reaching World, opening the newly eligible region requires **5 additional navigation/claim actions before selecting it** — dismiss World, open Quests, Claim boss quest, dismiss Quests, reopen World. The map itself provides only pins, Prepare hero and base dismissal controls (`WorkshopView.Campaign.cs:171-187`); claims are on the quest list/detail (`:53,74`). This is a measurable transition sequence, not a judgment that every reward claim should be automatic.
- Commission rewards are under Quests -> Trading commissions, although the workshop resource loop uses Mine/Smelt/Forge/Sell (`WorkshopView.cs:223-227`; `WorkshopView.Campaign.cs:61`; `WorkshopView.Modals.cs:70-80`).

### Competing item destinations and state feedback

- Sale consumes the **oldest inventory item immediately at sale start**, before its timed payout (`WorkshopSimulation.cs:66-70,261-264`); automation starts the Market before other workers (`:220-225`). Equipping removes an item from inventory (`WorkshopSimulation.Campaign.cs:58-72`), and delivery quest readiness checks unequipped inventory (`:186-190,205-214`). Crew explanation tells players to pause the merchant to preserve gear (`WorkshopView.Modals.cs:53`), but pause/resume is available in Crew rather than the selected Market dock (`:62-63`; `WorkshopView.cs:198-217,364-371`).
- Inventory rows show name, quality and gold value, then Equip; they do **not** display slot, element, ATK/DEF or a before/after hero comparison in their construction (`WorkshopView.Modals.cs:123-131`). Those attributes live in recipe and hero views (`WorkshopView.Campaign.cs:95-97,118-120,143-148`).
- All route Depart controls look enabled even if an unarmed hero is prohibited from hunt/boss routes (`WorkshopView.Campaign.cs:198-203`; `WorkshopSimulation.Campaign.cs:95-97`). Failure message is shared: “Prepare your hero and collect any previous expedition result.” (`WorkshopApp.cs:118-121`).
- All hero tiles remain clickable during an active expedition. `SelectHero` returns false when any battle is active (`WorkshopSimulation.Campaign.cs:85-89`), and the UI interprets every false result as opening the recruitment dialog (`WorkshopView.Campaign.cs:135`). Consequently even an already recruited hero can open a disabled Recruit dialog while battle is active (`:166-167`). This is a concrete label/action-state mismatch supported by both branches.
- Inventory Equip controls likewise remain clickable during an active expedition (`WorkshopView.Modals.cs:131`) and reject through a toast (`WorkshopApp.cs:108-111`; simulation gate `WorkshopSimulation.Campaign.cs:61`).
- Toast duration is 3.5 seconds (`WorkshopView.cs:383-387`), and the toast GameObject is constructed before later startup Welcome/Offline modals (`WorkshopView.cs:70`; `WorkshopApp.cs:35-39`). No persistent error summary is authored in these views.

## Accessibility facts

- Native Unity uGUI; **ARIA landmark count and HTML skip-link presence are N/A**. They are web concepts and are not counted as missing native controls. The actual factory constructs GameObjects with `Image`, `Text`, `RawImage` and `Button` (`D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/ForgeUI.cs:21-74`).
- **Authored focus order: none.** Every Button is assigned `Navigation.Mode.None` (`ForgeUI.cs:57`); the runtime creates an EventSystem/StandaloneInputModule without assigning an initial selected control (`WorkshopView.cs:55-57`). No `SetSelectedGameObject`, control `.Select()` or additional gameplay key binding was found in `Game/Assets/ForgeEmpire/Runtime`; the only `Input.GetKeyDown` is Escape (`WorkshopView.cs:315`).
- Local Unity source confirms None navigation is skipped when finding another selectable (`D:/Idle Blacksmith/Game/Library/PackageCache/com.unity.ugui@e20f1880fa04/Runtime/UGUI/UI/Core/Selectable.cs:798`), and pointer-down assigns selection only when navigation mode is not None (`:1200-1207`). StandaloneInputModule initial selection uses current/first selected object (`D:/Idle Blacksmith/Game/Library/PackageCache/com.unity.ugui@e20f1880fa04/Runtime/UGUI/EventSystem/InputModules/StandaloneInputModule.cs:277-281`).

| Primary action | Keyboard-only path from fresh UI authored? | Source |
|---|---|---|
| Begin tutorial | No | Button `WorkshopView.Modals.cs:47`; shared `ForgeUI.cs:57` |
| Mine / Smelt / Forge / Sell | No | `WorkshopView.cs:174-183,207`; shared `ForgeUI.cs:57` |
| Timed strike / Upgrade | No | `WorkshopView.cs:209-210`; shared `ForgeUI.cs:57` |
| Open Crew / Inventory / Settings / Quests / Recipes / Heroes / World | No | `WorkshopView.cs:97-101,223-227`; shared `ForgeUI.cs:57` |
| Hire / pause / resume | No | `WorkshopView.Modals.cs:62`; shared `ForgeUI.cs:57` |
| Equip / select recipe / select hero / choose region / Depart | No | `WorkshopView.Modals.cs:131`; `WorkshopView.Campaign.cs:98,121,135,182,203`; shared `ForgeUI.cs:57` |
| Quest / commission / expedition reward claims | No | `WorkshopView.Campaign.cs:53,74`; `WorkshopView.Modals.cs:78`; `WorkshopView.Battle.cs:45`; shared `ForgeUI.cs:57` |
| Battle skills | No | `WorkshopView.Battle.cs:41`; shared `ForgeUI.cs:57` |
| Dismiss current modal | **Yes, Escape / platform back mapping** | `WorkshopView.cs:315`; Android prevents app exit via back at `WorkshopApp.cs:24`. Actual hardware back behavior was not measured. |

- Pointer hover/press/disabled color changes are authored (`ForgeUI.cs:56`); there is no custom focus ring or focus-return management in the modal code (`WorkshopView.Modals.cs:13-37`). This is not a claim that Unity has no default selection color.
- Three header controls have blank Button text and only an icon (`WorkshopView.cs:97-102`); world station/rack/customer/noticeboard hotspots similarly have blank text (`:174-183`; `WorkshopView.Town.cs:22`). No explicit native accessibility labels/nodes are created by the inspected view/factory code. OS screen reader exposure was not measured.
- Label font size is literal and vertical overflow truncates (`ForgeUI.cs:41-48`); root scaling follows only safe-area width (`WorkshopView.cs:76-82`). Settings provide sound, music and haptics only (`WorkshopView.Modals.cs:140-146`); no in-game font-size or UI-scale preference is authored there. Device density and OS accessibility text-scale behavior were not measured.
- Timing marker uses a teal “perfect” region and gold moving marker (`WorkshopView.cs:212-215,297-299`); strike instruction explicitly says “Strike on green” (`WorkshopView.Modals.cs:46`; `WorkshopApp.cs:58,69`). Text displays resulting quality/combo (`WorkshopView.cs:371`). Nonvisual timing feedback or keyboard strike input is not authored in the inspected code.
- Contrast is not measured by this structural worker; use visual evidence for color-token contrast rather than treating these code facts as a WCAG contrast determination.

## Dead-prop / unused-import check

- C# Unity has no React-style component props; that metric does not directly apply.
- **Unused imports: 0 obvious cases found by manual inspection** across the 5 `WorkshopView*.cs` files, `ForgeUI.cs`, `WorkshopApp.cs`, and consulted Core files. Each namespace has a visible type/function use. This is a source review result, not a compiler/analyzer diagnostic count.
- **Proven dead public properties: not conclusively measured.** One **unreferenced-source candidate** is `WorkshopSimulation.JourneyStep` (`D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Core/WorkshopSimulation.cs:27-35`). A project Assets search for `JourneyStep` found its declaration and a differently purposed UI object-name string (`WorkshopView.cs:123`), no property access. This does not prove reflection/external consumers absent and is not used as a user-facing design defect.
- Do not call `Root`, `SelectedStation`, `HasModal`, `StrikeAccuracy` or `SavePath` dead: they have test/runtime consumers (`WorkshopSmoke.cs:63-64,114,133,155-190`; `CampaignSmoke.cs:28-47,101`; `WorkshopApp.cs:65`).

## Per-principle factual mapping (no scores)

- #2 Useful: core workshop production needs no modal; equipping and departure cross Inventory/World/Region; hero-led preparation has a return-to-workshop detour; keyboard action paths are not authored.
- #4 Understandable: 23 active workshop Buttons, 7 repeated-purpose groups, blank icon/hotspot labels, item decisions spread over Inventory/Recipes/Heroes, enabled controls can reject later, all navigation destinations are replacement modals.
- #5 Unobtrusive: count evidence distinguishes constructed content from visible content; do not equate all 42 recipe controls with simultaneous visual clutter.
- #6 Honest: route/Equip/hero selection state mismatches have paired UI/simulation evidence; defeat “RETURN & PREPARE” opens World before optional hero preparation.
- #8 Thorough: shared disabled/press/hover treatment exists; Escape and shade exit exist; modal focus, keyboard order and item-stat comparisons are absent from inspected authoring code; error feedback is transient.
- #9 Resources/attention: repeated screen creation destroys prior dialog (`WorkshopView.Modals.cs:15,37`); list counts are source quantities, not measured memory/CPU costs.
- #10 As little design as possible: 9 extra workshop entry points and 2 dismissal Buttons per modal; redundancy can improve direct touch access, so removability needs user-flow validation.

## Known gaps

- No runtime input test, screen reader inspection, physical phone readability check, telemetry, first-time player observation or save inspection was performed by this worker.
- Visible-control counts, overlap, actual touch-target size in dp, text clipping and scrolling reachability require a specified device/safe-area and runtime state; constructed-control counts do not settle those questions.
- No real accessibility API tree was collected. “No authored keyboard path” is evidenced source behavior; OS assistive input overlays could add interaction paths outside this code.
- No Roslyn unused-code analysis or whole-binary reflection analysis was run; dead-code certainty is intentionally limited.
- Source structure does not establish player comprehension, aesthetic quality, runtime performance or a principle score by itself.
