# Visual evidence — Ember & Oak / Idle Blacksmith

- Audit date: 2026-10-24. Evidence only; no scores or verdict.
- Scope: current `Game/Assets/ForgeEmpire/Runtime/ForgeUI.cs`, all five current `WorkshopView*.cs` partial files, supporting asset-import/error sources, and the five QA screenshots below. The `QA/campaign-baseline` copies were excluded from source counts.
- `INFERRED` means read or calculated from source, without reproducing the state in a running build. Screenshot observations are separately marked `OBSERVED`. No game source or save was edited.

## Sources consulted

| Source | Lines / inspected area |
| --- | --- |
| [ForgeUI.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/ForgeUI.cs:9>) | 9–19 palette; 21–30 rectangle coordinates; 41–59 font, overflow, button colors/navigation; 63–80 image/frame helpers |
| [WorkshopView.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:10>) | 10; 52–89 canvas and screen fitting; 92–231 header, world, dock, navigation; 240–259 selected station and failed prerequisites; 267–315 updates; 340–387 labels and notifications |
| [WorkshopView.Modals.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Modals.cs:13>) | 13–30 shared modal; 40–164 welcome, crew, orders, inventory, settings, offline states |
| [WorkshopView.Campaign.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Campaign.cs:9>) | 9–35 sprite/scroll helpers; 38–208 quests, recipes, heroes, world, routes |
| [WorkshopView.Battle.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Battle.cs:18>) | 18–89 battle layout, state text, controls, effects |
| [WorkshopView.Town.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopView.Town.cs:14>) | 14–67 sprites, weather, world clock and expedition shortcut |
| [WorkshopApp.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Runtime/WorkshopApp.cs:103>) | 103–121 validation/success notifications; 150–155 save error |
| [ForgeBuild.cs](<D:/Idle Blacksmith/Game/Assets/ForgeEmpire/Editor/ForgeBuild.cs:47>) | 47–53 pixel-art import settings |
| [initial-install workshop screenshot](<D:/Idle Blacksmith/QA/campaign-android-initial-install-workshop.png>) | Entire 1080 × 2400 PNG; header, world, dock, navigation |
| [hero loadout screenshot](<D:/Idle Blacksmith/QA/campaign-android-golden-hero-loadout.png>) | Entire 1080 × 2400 PNG; modal, equipment, two actions |
| [battle-live screenshot](<D:/Idle Blacksmith/QA/campaign-android-golden-skill-patrol-battle-live.png>) | Entire 1080 × 2400 PNG; **visible state is victory** |
| [recipes screenshot](<D:/Idle Blacksmith/QA/campaign-android-golden-recipes.png>) | Entire 1080 × 2400 PNG; recipe list, selected/locked controls, bottom clipping |
| [back-to-workshop screenshot](<D:/Idle Blacksmith/QA/campaign-android-back-to-workshop.png>) | Entire 1080 × 2400 PNG; **visible state is world-map modal** |

## Screen units and fitting

- **INFERRED:** Native Unity screen-space-overlay canvas, with no CSS or JavaScript UI. The root starts at 1080 × 2160 reference units; every rectangle is anchored/pivoted at its upper-left. Sources: `WorkshopView.cs:55–63`; `ForgeUI.cs:21–29`.
- **INFERRED:** Effective scale = `Screen.safeArea.width / 1080`; root height = `safeArea.height / scale`; root position follows the safe-area center. Thus the numbers below are reference-layout units, equal to screenshot pixels only at a 1080-pixel safe width. Sources: `WorkshopView.cs:76–88`.
- **INFERRED:** Screen/safe-area changes rerun fitting and close an open modal; world viewport, dock, navigation and toast positions are adjusted. Sources: `WorkshopView.cs:83–89,269`.
- **INFERRED:** World art is 1080 × 1665 units inside a vertically scrollable, clamped and masked viewport of height `root.height − 486`. A selected station can scroll toward 55% of viewport height when the world is taller than its viewport. Sources: `WorkshopView.cs:129–136,240–246`.
- **INFERRED:** All text uses `LegacyRuntime.ttf`, horizontal wrapping, and vertical truncation; the audited text helper contains no best-fit, OS text-size, or device-density conversion. Sources: `ForgeUI.cs:41–48`.
- **INFERRED:** Normalizing the 1080-wide layout to a hypothetical 360-pixel safe width applies a one-third scale: the 20–33-unit small/body sizes become 6.67–11 pixels, the 48-unit headings become 16 pixels, and the 52-unit offline amount becomes 17.33 pixels. This is a mathematical width normalization, not a measured Android dp/sp result. Sources: `WorkshopView.cs:79–81`; type-size citations below.

## Spacing scale observed

- **INFERRED, reference-unit array:** `[2, 3, 4, 6, 12, 13, 15, 16, 17, 18, 20, 21, 24, 25, 27, 32, 36, 37, 38, 45, 56, 90]`.
- Method: deduplicated the explicit border widths, repeated row/column gaps, shared button padding and principal dock/modal insets listed below. This is an observed subset of actual spacing, **not** a declared global token scale; arbitrary absolute coordinates and component dimensions were not counted as spacing.

| Spacing | Usage and calculation | Source |
| --- | --- | --- |
| 2 | Frame stroke | `ForgeUI.cs:79–80` |
| 3 | Header rule; dock top accent | `WorkshopView.cs:107,203` |
| 4 | Modal top accent | `WorkshopView.Modals.cs:27` |
| 6 | Button label left/right inset; label width = button width − 12 | `ForgeUI.cs:58` |
| 12 | Resource column gap = 285 − (26 + 247); inventory row gap = 132 − 120 | `WorkshopView.cs:103–106`; `WorkshopView.Modals.cs:122,125` |
| 13 | Recipe row gap = 187 − 174 | `WorkshopView.Campaign.cs:93` |
| 15 | Navigation left inset and column gap = 214 − 199 | `WorkshopView.cs:227` |
| 16 | Quest row gap = 202 − 186; quest tab gap = 488 − (34 + 438) | `WorkshopView.Campaign.cs:41–42,48` |
| 17 | Crew row gap = 184 − 167 | `WorkshopView.Modals.cs:57–58` |
| 18 | Expedition route gap = 205 − 187 | `WorkshopView.Campaign.cs:199–200` |
| 20 | Hero-tile gap = 307 − 287 | `WorkshopView.Campaign.cs:135` |
| 21 | Dock button gap = 830 − (535 + 274) | `WorkshopView.cs:207,210` |
| 24 | Main dock side inset | `WorkshopView.cs:200` |
| 25 / 27 | Dock heading / supporting text left insets | `WorkshopView.cs:204–206` |
| 32 | Shared list viewport horizontal inset | `WorkshopView.Campaign.cs:30`; `WorkshopView.Modals.cs:88` |
| 36 / 38 | Modal heading / eyebrow left insets | `WorkshopView.Modals.cs:28–29` |
| 37 | Close-button right inset = 968 − (859 + 72) | `WorkshopView.Modals.cs:30` |
| 45 / 90 | Oversized modal viewport top/bottom margins / total vertical margin | `WorkshopView.Modals.cs:18–23` |
| 56 | Modal left/right screen inset: (1080 − 968) / 2 | `WorkshopView.Modals.cs:19,24` |

## Type scale observed

- **INFERRED, reference-unit array:** `[20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 48, 52]` — **16 unique sizes**.
- Method: parsed all `ForgeUI.Label(...)` font-size arguments and `ForgeUI.Button(...)` explicit font-size arguments in the five current `WorkshopView*.cs` files; included the helper's default button size 26 (`ForgeUI.cs:52`). Empty-label buttons still reference that size. Does not include unrelated editor/QA code.

| Size | Representative use | Exact source |
| --- | --- | --- |
| 20 | App/modal eyebrow | `WorkshopView.cs:95`; `WorkshopView.Modals.cs:28` |
| 21 | Bottom navigation; hero role; version | `WorkshopView.cs:229`; `WorkshopView.Campaign.cs:138`; `WorkshopView.Modals.cs:146` |
| 22 | Recipe attributes/cost; dock hint | `WorkshopView.Campaign.cs:96–97`; `WorkshopView.cs:206` |
| 23 | Quest summary; station badge/info; skill help | `WorkshopView.cs:122,178,205`; `WorkshopView.Battle.cs:42` |
| 24 | Recipe SELECT and quest CLAIM controls | `WorkshopView.Campaign.cs:53,98` |
| 25 | Materials summary; hero equipment name; upgrade | `WorkshopView.Campaign.cs:89,157`; `WorkshopView.cs:210` |
| 26 | Default button; hero equipment hint | `ForgeUI.cs:52`; `WorkshopView.Campaign.cs:146` |
| 27 | Hero actions; world action/hint | `WorkshopView.Campaign.cs:145–147,186–187` |
| 28 | Hero name; battle health/loot | `WorkshopView.Campaign.cs:137`; `WorkshopView.Battle.cs:35,44` |
| 29 | Recipe name, hero lore, quest title | `WorkshopView.Campaign.cs:50,95,141` |
| 30 | Resource values; hero stats; battle status | `WorkshopView.cs:114`; `WorkshopView.Campaign.cs:142`; `WorkshopView.Battle.cs:24` |
| 31 | Welcome/recruit text | `WorkshopView.Modals.cs:44`; `WorkshopView.Campaign.cs:165` |
| 32 | Recipe description; map region numbers | `WorkshopView.Campaign.cs:118,182` |
| 33 | Selected station title; order title | `WorkshopView.cs:204`; `WorkshopView.Modals.cs:74` |
| 48 | App title and every modal heading | `WorkshopView.cs:96`; `WorkshopView.Modals.cs:29` |
| 52 | Offline gold amount | `WorkshopView.Modals.cs:161` |

## Distinct referenced color count

- **INFERRED: 20 unique literal hex color values** — 8 named palette values plus 12 additional inline values.
- Method: regex-extracted and case-normalized unique `Hex("...")` arguments across `ForgeUI.cs` and the five current `WorkshopView*.cs` files; checked all eight named palette values have UI call-site references. No `oklch` tokens exist in this scope. Count is source references, not a claim that all twenty are simultaneously visible.
- Exclusions: transparent `Color.clear`, `new Color(...)` RGBA tints/effects, ColorBlock multiplication, texture/atlas pixels, antialiasing, and composited colors. These mean the rendered image contains more than twenty colors.

| Hex | Role | Definition or inline source |
| --- | --- | --- |
| `#111a22` | Background; dark action text | `ForgeUI.cs:9`; e.g. `WorkshopView.cs:94,207` |
| `#1c2932` | Main panel | `ForgeUI.cs:10`; e.g. `WorkshopView.Modals.cs:19,24` |
| `#35464b` | Lines / inactive structures | `ForgeUI.cs:11`; e.g. `WorkshopView.cs:107,176` |
| `#e4bb78` | Gold accent/action fill | `ForgeUI.cs:12`; e.g. `WorkshopView.cs:203,207` |
| `#f1ead9` | Primary cream text | `ForgeUI.cs:13`; e.g. `WorkshopView.cs:96,204` |
| `#91a4a7` | Secondary text | `ForgeUI.cs:14`; e.g. `WorkshopView.cs:205,229` |
| `#75c5ad` | Teal action/progress/stat text | `ForgeUI.cs:15`; e.g. `WorkshopView.cs:206,217` |
| `#efa66a` | Enemy HP / incoming-attack text | `ForgeUI.cs:16`; `WorkshopView.Battle.cs:34,67` |
| `#243038` | Journey card | `WorkshopView.cs:119` |
| `#19252d` | World station badges | `WorkshopView.cs:175` |
| `#31464b` | Upgrade / trading commissions | `WorkshopView.cs:210`; `WorkshopView.Campaign.cs:61` |
| `#243b3d` | Workshop navigation tile | `WorkshopView.cs:227` |
| `#293b43` | Close modal button | `WorkshopView.Modals.cs:30` |
| `#23343c` | Crew / inventory rows | `WorkshopView.Modals.cs:58,125` |
| `#314d50` | Settings toggles | `WorkshopView.Modals.cs:153` |
| `#24343c` | Quest/recipe/hero/route cards | `WorkshopView.Campaign.cs:48,93,135,154,200` |
| `#37544f` | Selected hero tile | `WorkshopView.Campaign.cs:135` |
| `#33534e` | Brace/recover buttons | `WorkshopView.Battle.cs:41` |
| `#263a42` | World-map shortcut | `WorkshopView.Town.cs:30,65` |
| `#43634f` | Finished-expedition shortcut | `WorkshopView.Town.cs:65` |

- **INFERRED:** Button highlighted tint `(1.13,1.13,1.13,1)`, pressed tint `(.78,.85,.85,1)`, disabled tint `(.6,.65,.65,.6)`; modal shade `(.025,.045,.06,.88)`. Sources: `ForgeUI.cs:56`; `WorkshopView.Modals.cs:17`.
- **INFERRED:** Other non-hex colors affect smoke, forge glow, night/rain, guard/hit flashes. Sources: `WorkshopView.cs:151,153,301`; `WorkshopView.Town.cs:23–25,38`; `WorkshopView.Battle.cs:29–30,88–89`.

## Source-derived primary text contrast

- **INFERRED:** Computation used 8-bit source sRGB values, channel linearization `c/12.92` for `c ≤ .04045`, otherwise `((c+.055)/1.055)^2.4`, relative luminance weights `.2126/.7152/.0722`, and `(Llighter+.05)/(Ldarker+.05)`.
- **INFERRED:** Lowest nominal, enabled, solid foreground/background pair **among the checked controls is 3.7235:1**, gold `#e4bb78` on completed-expedition shortcut `#43634f` (`WorkshopView.Town.cs:30,65`). The global lowest rendered ratio remains **unknown**, because some text overlays art and state tints/compositing are not measured here.

| Foreground / background | Ratio | Source of pairing |
| --- | ---: | --- |
| Cream `#f1ead9` / Background `#111a22` | 14.6575:1 | App heading `WorkshopView.cs:94,96` |
| Cream / Panel `#1c2932` | 12.3964:1 | Dock title `WorkshopView.cs:200,204`; modal title `WorkshopView.Modals.cs:24,29` |
| Gold `#e4bb78` / Panel | 8.2671:1 | Modal eyebrow `WorkshopView.Modals.cs:24,28` |
| Muted `#91a4a7` / Panel | 5.7143:1 | Dock supporting text `WorkshopView.cs:200,205`; hero hint `WorkshopView.Campaign.cs:146` |
| Teal `#75c5ad` / Panel | 7.3243:1 | Dock hint `WorkshopView.cs:200,206`; hero stats `WorkshopView.Campaign.cs:142` |
| Background / Gold | 9.7750:1 | Mine/context action `WorkshopView.cs:207` |
| Background / Teal | 8.6602:1 | Hero/world action `WorkshopView.Campaign.cs:147,187` |
| Gold / Upgrade `#31464b` | 5.5392:1 | Upgrade `WorkshopView.cs:210` |
| Cream / Card `#24343c` | 10.7335:1 | Recipe names `WorkshopView.Campaign.cs:93,95` |
| Teal / Card | 6.3418:1 | Recipe attributes `WorkshopView.Campaign.cs:93,96` |
| Muted / Card | 4.9478:1 | Recipe cost `WorkshopView.Campaign.cs:93,97` |
| Cream / SelectedHero `#37544f` | 6.8848:1 | Hero name `WorkshopView.Campaign.cs:135,137` |
| Gold / SelectedHero | 4.5914:1 | Hero role `WorkshopView.Campaign.cs:135,138` |
| Cream / Skill `#33534e` | 7.0381:1 | Brace/recover `WorkshopView.Battle.cs:41` |
| Muted / Close `#293b43` | 4.4822:1 | Modal X `WorkshopView.Modals.cs:30` |
| Teal / Toggle `#314d50` | 4.4828:1 | ON/OFF settings `WorkshopView.Modals.cs:153–154` |
| Gold / MapShortcut `#263a42` | 6.6116:1 | World shortcut `WorkshopView.Town.cs:30,65` |
| Gold / FinishedShortcut `#43634f` | **3.7235:1** | Expedition result shortcut `WorkshopView.Town.cs:30,65` |

- Caveats: these ratios are source calculations for normal, opaque states; they do not establish a WCAG pass/fail on actual device text. Unity reference-unit font sizes are not CSS pixels, points, Android sp or dp. Hover/pressed/disabled tints, platform color space, scaling, antialiasing and screenshot rendering were not included.
- **INFERRED:** The world-clock label is cream directly over the diorama (`WorkshopView.Town.cs:28–29`), and map names are cream/muted directly over the map (`WorkshopView.Campaign.cs:177–184`). Their backgrounds vary by artwork/location; no single source-token contrast ratio applies.
- **INFERRED:** Nightfall is created as a full-scene overlay after station badges: `BuildDiorama()` precedes `BuildTownLife()` (`WorkshopView.cs:64,69`), badges are scene children (`WorkshopView.cs:175–180`), and Nightfall is subsequently added to that same scene (`WorkshopView.Town.cs:23`). Its alpha varies up to .15 (`WorkshopView.Town.cs:38`). Consequently the nominal station-plaque palette also undergoes this overlay, while the world-clock HUD is created separately under the viewport (`WorkshopView.Town.cs:28–29`).

## Six-state checklist

| State | Present / missing | Evidence and limits |
| --- | --- | --- |
| Empty | **Present — INFERRED** | Inventory icon and “Your next masterpiece starts at the forge. / Craft a sword to see it here.” appear when stock count is zero; sell is disabled. `WorkshopView.Modals.cs:95–109`. Hero slots use “No equipment”: `WorkshopView.Campaign.cs:157`. No inspected screenshot shows the inventory-empty modal. |
| Loading | **Missing explicit loading presentation in audited UI — INFERRED** | No loading/skeleton/progress state for UI/asset initialization in the inspected helpers/views; `Resources.Load` and UI construction are synchronous (`WorkshopView.cs:52–73,135`; `ForgeUI.cs:43,65`). Gameplay work progress exists (`WorkshopView.cs:290,312,353,367`) and is not asset loading. Startup before UI construction was not runtime-tested. |
| Error | **Present — INFERRED** | Failed prerequisites produce help toast (`WorkshopView.cs:254–257`); recipe/equip/recruit validation produces messages (`WorkshopApp.cs:105,110,115,120`); save IOException shows “Couldn't save. Please check your device storage.” (`WorkshopApp.cs:155`). Shared toast is cream, 29 units, expires after 3.5 seconds (`WorkshopView.cs:70,313,383–386`). No persistent error panel or inspected error screenshot. |
| Success | **Present — OBSERVED + INFERRED** | Victory heading, reward amount and COLLECT LOOT in `QA/campaign-android-golden-skill-patrol-battle-live.png`, central status and lower action area. Text/control mappings: `WorkshopView.Battle.cs:56,66,68–69`. Quest-ready summary and QUESTS + visible behind hero modal in `QA/campaign-android-golden-hero-loadout.png`; mapping `WorkshopView.cs:374–376`. |
| Focus | **Missing explicit keyboard/controller focus treatment — INFERRED** | Every helper-created button uses `Navigation.Mode.None`; helper configures hover/pressed/disabled colors but no focus ring or custom selected treatment (`ForgeUI.cs:55–58`). Station selection corners (`WorkshopView.cs:164–169,355–359`) and selected hero/card states (`WorkshopView.Campaign.cs:135`) exist, but represent game selection. No runtime focus traversal was tested. |
| Disabled | **Present — OBSERVED + INFERRED** | Muted CHAPTER 2 recipe buttons in `QA/campaign-android-golden-recipes.png`, lower half; `interactable = unlocked && !forgeRunning` (`WorkshopView.Campaign.cs:101–102`). Victory screenshot shows inactive skill buttons (`WorkshopView.Battle.cs:62`). Source disabled tint `.6/.65/.65/.6` (`ForgeUI.cs:56`); materials/work/upgrade gating `WorkshopView.cs:366–369`. |

## Pixel art dimensions and scaling

- **MEASURED FILE METADATA:** Actual PNG headers were read using Pillow; the following are asset dimensions, not screen measurements. Asset root: `D:/Idle Blacksmith/Game/Assets/Resources/Art/`.

| Assets | Source atlas / frame dimensions | Display dimensions and source |
| --- | --- | --- |
| `workshop.png` | 384 × 592 | 1080 × 1665 = 2.8125×; `WorkshopView.cs:10,134–136,194–195` |
| `miner.png`, `merchant.png`, `customer.png` | Atlases 288 × 56 / 192 × 56 / 288 × 56; frame 48 × 56 | 135 × 157.5; `WorkshopView.cs:137,140–141,186–189` |
| `fire.png` | 192 × 40; frame 32 × 40 | 90 × 112.5; `WorkshopView.cs:138,186–189` |
| `blacksmith.png` | 512 × 64; frame 64 × 64 | 180 × 180; `WorkshopView.cs:139,186–189` |
| `smoke.png`, `sparks.png` | 192 × 48 / 240 × 48; frames 32 × 48 / 48 × 48 | 90 × 135 / 135 × 135; `WorkshopView.cs:151–152,186–189` |
| `icons.png` | 256 × 32; eight 32 × 32 icons | Common sizes 34 resources, 40 badges, 42 navigation, 44 header; `ForgeUI.cs:63–67`; `WorkshopView.cs:98–102,113,177,228` |
| `equipment.png` | 640 × 32; twenty 32 × 32 icons | 85 recipe rows, 105 hero gear, 280 recipe detail; `WorkshopView.Campaign.cs:23–25,94,117,155` |
| `heroes.png` | 768 × 64; twelve 64 × 64 frames | 153 portraits; 208 battle hero; `WorkshopView.Campaign.cs:136`; `WorkshopView.Battle.cs:27` |
| `enemies.png` | 1600 × 80; twenty 80 × 80 frames | 184 normal / 250 final boss; `WorkshopView.Battle.cs:28,83–85` |
| `battlefields.png` | 1600 × 180; five 320 × 180 landscapes | 900 × 506 battle image; `WorkshopView.Battle.cs:25–26` |
| `worldmap.png` | 384 × 480 | Maximum 912 × 1140, capped by modal height; `WorkshopView.Campaign.cs:175–177` |

- **INFERRED:** Import workflow explicitly uses point filtering, no mipmaps, clamp wrapping, no NPOT rescale and uncompressed textures (`ForgeBuild.cs:47–53`). The displayed sizes above include fractional/non-integer multiples of source pixels.
- **OBSERVED:** QA workshop, loadout and battle images show distinct blocky art edges alongside smoother text; source cropping/display sizes vary across sprites. No visual claim is made about smaller devices or animation fidelity from still images.

## Modal dimensions

- **INFERRED:** Every modal is 968 units wide, inset 56 from the 1080-unit root; common heading 48, eyebrow 20, close control 72 × 66. A requested height greater than `root.height − 90` uses a vertically scrollable masked viewport at y=45 with height `root.height − 90`; otherwise the whole dialog is vertically centered. Sources: `WorkshopView.Modals.cs:16–30`.

| Modal | Requested height | Source |
| --- | --- | --- |
| Offline | 893 | `WorkshopView.Modals.cs:159` |
| Orders | 970 | `WorkshopView.Modals.cs:72` |
| Welcome | 1040 | `WorkshopView.Modals.cs:42` |
| Recruit | 1080 | `WorkshopView.Campaign.cs:163` |
| Settings | 1140 | `WorkshopView.Modals.cs:140` |
| Crew | 1220 | `WorkshopView.Modals.cs:52` |
| Quest / recipe detail | 1260 | `WorkshopView.Campaign.cs:68,116` |
| Inventory | 1370 | `WorkshopView.Modals.cs:86` |
| Battle | 1550 | `WorkshopView.Battle.cs:23` |
| Heroes | 1570 | `WorkshopView.Campaign.cs:131` |
| Region routes | 1590 | `WorkshopView.Campaign.cs:193` |
| Quest list | min(1740, root.height − 100) | `WorkshopView.Campaign.cs:40` |
| World map | min(1750, root.height − 80) | `WorkshopView.Campaign.cs:175` |
| Recipes | min(1760, root.height − 100) | `WorkshopView.Campaign.cs:88` |

## Screenshot regions: legibility and hierarchy facts

- Region coordinates below are approximate original 1080 × 2400 PNG pixels, with upper-left origin. Images were viewed at a 922 × 2048 preview; regions are intentionally broad rather than pretending to be pixel-exact measurements.

| Screenshot / region | OBSERVED facts | Related source (INFERRED where numerical) |
| --- | --- | --- |
| [Initial workshop](<D:/Idle Blacksmith/QA/campaign-android-initial-install-workshop.png>), x0–1080, y125–465 | Brand heading, three square icon-only header controls, four resource tiles, quest title/objective, and chapter/reputation precede the world. Quest title is gold; objective is cream. | `WorkshopView.cs:94–124`: title 48, resource values 30, quest title 29, objective 23, chapter 22. |
| Initial workshop, x0–1080, y465–1980 | Diorama contains labeled MINE, HEARTH, FORGE and MARKET plaques and a WORLD MAP action. Selected mine has gold corner brackets. Characters, tools, trees and building art occupy most of this region. Labels sit on solid dark plaques; world clock sits directly on sky/art. | `WorkshopView.cs:164–180`; `WorkshopView.Town.cs:28–30`. |
| Initial workshop, x24–1056, y2050–2275 | Dock shows “Iron mine”, duration, yield and a teal hint on left; MINE ORE gold action and UPGRADE 20 G on right. Dark gap separates world-art bottom from dock. | `WorkshopView.cs:200–217`: heading 33, info 23, hint 22, action 274 × 112, upgrade 180 × 112. |
| Initial workshop, x0–1080, y2280–2400 | Five bottom destinations have both icon and uppercase word; Workshop is the only filled/teal tile. A Development Build watermark sits bottom-right. | `WorkshopView.cs:222–230`: 21-unit nav text. |
| Initial workshop, x215–1080, y2280–2400 | QUESTS uses an ingot-shaped icon, HEROES a pickaxe, and WORLD a gear. The same pickaxe and gear also appear in the upper header. | `WorkshopView.cs:223`: icon indices `[4,1,2,5,7]` for `[WORKSHOP,QUESTS,RECIPES,HEROES,WORLD]`; header crew/settings reuse indices 5/7 (`WorkshopView.cs:98,102`); resource Ingots uses index 1 (`WorkshopView.cs:105`). This records the icon-to-word mapping without assuming user comprehension. |
| [Hero loadout](<D:/Idle Blacksmith/QA/campaign-android-golden-hero-loadout.png>), x56–1024, y475–2055 | Dark shade covers the workshop. Modal has gold top rule, gold eyebrow, cream title and X. Rowan is a tinted selected tile; Lyra/Kael show recruitment costs. Lore is a multi-line paragraph, followed by teal stats. | `WorkshopView.Modals.cs:17,26–30`; `WorkshopView.Campaign.cs:131–142`. |
| Hero loadout, x95–985, y1200–1915 | Two equipment cards show slot, item icon and name. Gold EQUIP FROM INVENTORY precedes a three-line muted hint and teal CHOOSE AN EXPEDITION. Neither hint nor either action is visibly clipped in this still. | `WorkshopView.Campaign.cs:143–157`: lore 29, stats 30, hint 26, actions 27. |
| [Recipes](<D:/Idle Blacksmith/QA/campaign-android-golden-recipes.png>), x56–1024, y380–2145 | Gold eyebrow, cream heading and gold two-line material summary precede repeated rows. Rows pair a pixel icon with cream name, teal attributes, muted cost and a right-aligned action. SELECT and SELECTED share the same gold fill. Locked rows use dull CHAPTER 2 controls. | `WorkshopView.Campaign.cs:88–108`: row 904 × 174 at 187-unit intervals; name 29; attributes/cost 22; button 187 × 85, 24-unit text. |
| Recipes, x88–990, y1970–2110 | Bottom “Thunderfen Ward” row is partially visible; its cost is cut by the list viewport. No scrollbar is visible at the right edge. | `WorkshopView.Campaign.cs:28–35,90`: masked vertical ScrollRect; no scrollbar is constructed by that helper. This partial row is a scroll boundary, not evidence that the entire list is inaccessible. |
| [Battle-live](<D:/Idle Blacksmith/QA/campaign-android-golden-skill-patrol-battle-live.png>), x56–1024, y485–2005 | This screenshot shows “VICTORY · YOUR CRAFT PREVAILED”, enemy 0/28 HP, hero 101/105 HP, subdued skill controls, reward amounts and gold COLLECT LOOT. The file's name does not establish a live encounter state. | `WorkshopView.Battle.cs:24–46,56–69`. |
| Battle-live, x90–990, y735–1605 | Large illustrated battle stage precedes two health bars with numeric/name labels, then a row of three skill buttons with separate muted descriptions. Enemy health bar is empty and hero bar is nearly full. | `WorkshopView.Battle.cs:25–44`: battlefield 900 × 506; HP tracks 416 × 16; three skill buttons 286 × 113; help 23. |
| [Back-to-workshop](<D:/Idle Blacksmith/QA/campaign-android-back-to-workshop.png>), x56–1024, y385–2145 | This screenshot still shows the “Beyond the forge” world-map modal over a shaded workshop. Unlocked Emberwood uses a gold number tile; later numbered regions are muted. Region names lie over map art. A two-line progression hint and teal PREPARE YOUR HERO sit below. | `WorkshopView.Campaign.cs:175–187`. Filename is not proof that Android Back dismissed the modal. |

## Per-principle facts for orchestrator synthesis

| Principle | Facts only |
| --- | --- |
| #1 Innovative | Custom pixel-art diorama and overlay panels are present in the initial workshop screenshot; no peer-product comparison was performed. |
| #2 Useful | Workshop selected-station dock and MINE ORE are visible in the initial workshop bottom region; recipe buttons can return focus to the forge (`WorkshopView.Campaign.cs:98`). |
| #3 Aesthetic | Twenty referenced hex tokens, sixteen type sizes and the explicitly cited spacing subset above; shared modal geometry and source/screenshot hierarchy are documented. |
| #4 Understandable | Station names, task verbs, resources, recipe cost/stats and disabled CHAPTER 2 wording are visible in cited screenshot regions. Actual first-use comprehension was not tested. |
| #5 Unobtrusive | Workshop artwork fills most of the center; a full-screen shade dims underlying artwork/chrome when dialogs open (`WorkshopView.Modals.cs:17`; all four modal screenshots). |
| #6 Honest | The screenshot filenames “battle-live” and “back-to-workshop” do not match their visible state; this is an evidence-label limitation, not a product-label finding. Source-only inferred versus observed claims are separated. |
| #7 Long-lasting | Pixel art, flat rectangular panels, an eight-value named palette and one built-in font appear in source; future visual longevity was not empirically tested. |
| #8 Thorough | Explicit empty, error, success and disabled states found; explicit asset-loading and keyboard/controller-focus presentation absent in inspected sources. Error/empty states have source evidence only. |
| #9 Environmentally friendly | PNG texture sizes and point/no-mipmap import configuration are documented; this task did not measure GPU usage, power, frame rate or executable weight. |
| #10 As little design as possible | Multiple persistent information bands and actions are visible in initial workshop screenshot; no removability judgment or element count is supplied by this visual evidence task. |

## Known gaps

- Native Unity game: browser computed CSS styles, DOM, JavaScript bytes and browser accessibility APIs do not describe this UI. All numeric style facts above are marked source-derived reference units.
- No new runtime build, emulator interaction, keyboard/controller traversal, orientation change, smaller-device capture or OS text-scaling check was performed. Still screenshots cannot prove transition timing, touch target behavior or all failure states.
- Screenshot provenance is QA development-build captures; exact source/build identity was not established. Current source may differ from the captured build.
- Older compact Android screenshots with a four-tab navigation were excluded; the inspected initial-install screenshot and current source both show five tabs.
- Minimum rendered contrast over texture-backed text and all hover/pressed/disabled/composited frames was not measured. The 3.7235:1 value is a reproducible normal-state token-pair calculation, not a complete minimum over every rendered pixel/state.
- The requested short “initial-install-workshop.png” exists as `QA/campaign-android-initial-install-workshop.png`; that exact file was inspected. All five inspected files are 1080 × 2400.
- No subjective score, verdict, redesign proposal, game source edit or save-state edit is included.

