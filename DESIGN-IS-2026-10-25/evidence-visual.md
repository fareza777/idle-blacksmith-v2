# Visual evidence — current 0.4.0

## Sources consulted

- Actual native Android captures (1080 x 2400) under `QA/progression-20261024/android/regression/`: `battle-active.png`, `recipes.png`, `adventure.png`, `hero-prepared.png`, `workshop-start.png`. Tool previews were resized to 922 x 2048; no pixel-level measurement was taken from those resized previews.
- `Game/Assets/ForgeEmpire/Runtime/ForgeUI.cs:9-18,30-40,67-92,119-133,158-190` — tokens, font layout, button state colors, medallions and generated glyphs.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Navigation.cs:33-34,46-87,122-166,177-190` — spacing, page layout, scrolling and focus management.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Campaign.cs:10-24,110-124,152-196,205-228` — art selection, equipment/hero sizes, map locations and labels.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Battle.cs:24-43,91-111,127-145` — stage, actor sizes, state copy and battle art frames.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Modals.cs:97-109,126-147`; `WorkshopApp.cs:117-132,167`; `WorkshopView.Progression.cs:120-160` — empty, blocked, disabled and result states.
- `Game/Assets/ForgeEmpire/Editor/ForgeBuild.cs:48-52,75-82` — texture filtering policy and asset verification.
- PNG IHDR headers read directly for the six art sheets listed below. No images were edited or generated for this audit.

## Concrete findings

### A. The visible campaign surfaces use two distinct rendering media

OBSERVED: `battle-active.png`, main illustrated stage (roughly y=490-1060 in the original 2400px capture), shows a painted Rowan beside a visibly pixelated boar-like enemy, trees, tent, fire and landscape. `hero-prepared.png`, central hero illustration, places the same painted Rowan over the same pixel landscape. `recipes.png` shows visibly pixelated sword, axe and armor on otherwise large smooth typography. `adventure.png` uses a pixel map with a baked decorative border. `workshop-start.png` uses a painted workshop and painted workers/walkers.

SOURCE: `WorkshopView.Campaign.cs:12-14` routes only `asset=="heroes"` to `HandDrawnArt.Person`; every other campaign sheet loads `Art/<asset>`. `ForgeBuild.cs:50` uses Bilinear only for paths containing `/HandDrawn/`, Point elsewhere. This is an explicit source-backed medium/filter difference, not an inference from a screenshot alone.

### B. Existing campaign imagery is magnified from small source frames

MEASURED: PNG dimensions from IHDR, with display factors calculated from source UV count and dimensions (these factors describe design units, not physical device density):

| Sheet | Source dimensions | Frame dimensions | Current displayed dimensions / factor |
| --- | --- | --- | --- |
| `Art/equipment.png` | 640 x 32 | 32 x 32, 20 columns | Recipe row 144 x 144 (4.5x); hero gear 160 x 160 (5x); recipe detail 320 x 320 (10x) |
| `Art/enemies.png` | 1600 x 80 | 80 x 80, 20 columns | Ordinary actor 230 x 230 (2.875x); final guardian 320 x 320 (4x) |
| `Art/battlefields.png` | 1600 x 180 | 320 x 180, 5 columns | Battle 1020 x 574 (about 3.19x) |
| `Art/worldmap.png` | 384 x 480 | Entire image | 1020 x 1275 (2.65625x) |
| `Art/HandDrawn/heroes.png` | 1448 x 1086 | Uses per-frame atlas metadata | Painted character shapes fitted by `HandDrawnArt` |
| `Art/HandDrawn/workshop.png` | 1010 x 1557 | Entire image | Painted scene with viewport zoom |

SOURCE: `WorkshopView.Campaign.cs:10-24,115,132,189,212-213`; `WorkshopView.Battle.cs:25-30,136-138`.

### C. Map destination information is unevenly displayed

OBSERVED: `adventure.png`, map area, contains the name Emberwood and its numbered medallion. Four future positions display small lock glyphs. The future destination names are absent in that capture.

SOURCE: `WorkshopView.Campaign.cs:214-224`: all five locations are enumerated, but `if(!unlocked)` draws a lock and immediately continues. Only unlocked destinations receive `RegionName<i>` labels and click handlers. `adventure.layout.tsv` likewise records only `RegionName0` in the fresh profile. No current-goal, completed-guardian or next-unlock status is drawn on the map by this method.

### D. UI symbols remain 16px point-filtered glyphs

OBSERVED: back arrow, navigation symbols, health/recover cross, lock, and equipment-empty symbol show discrete square stair steps in all five captures. SOURCE: `ForgeUI.cs:158-190` builds 16 x 16 masks and sets `FilterMode.Point`; medallions use those glyphs at scaled sizes (`ForgeUI.cs:119-126`). This is distinct from the Bilinear painted actor/scene assets.

## Spacing and type

SOURCE / INFERRED DESIGN UNITS: the main canvas uses width1080, page content1020, horizontal page inset30, page-heading inset204 and heading width834 (`WorkshopView.Navigation.cs:129-147`). Repeated geometry includes 24px button text inset, 28 x textScale gap, 30px page inset, 36px page-bottom reserve, 48px total button text inset, 52px header allowance, 112px back medallion, minimum132px action height, 174px navigation height. Smaller spacings visible in construction include4,8,10,12,14,16,20 and24; these are direct component placements, not one globally enforced spacing token system (`WorkshopView.Navigation.cs:33-34,53-74,136-165`; `WorkshopView.Campaign.cs:112-126`; `WorkshopView.cs:25`).

SOURCE: named type scale =42 (Small),48 (Body),56 (Subheading),72 (Heading), multiplied by TextScale1/1.25/1.5 (`ForgeUI.cs:18,29`). Paragraph line spacing1.12, minimum measured-height multiplier1.22 (`ForgeUI.cs:34,39,74`). Lato-Regular supplies body/small; Spectral-SemiBold supplies sizes>=56 (`ForgeUI.cs:43-46,72`). Compact page roots under1600 switch heading72 to56 (`WorkshopView.Navigation.cs:134`).

OBSERVED: all five inspected captures have readable large copy without a covering rectangular dashboard; the main recipe list continues vertically below the viewport, with the next item partly cut by the scroll viewport above persistent navigation. This continuation is visible in `recipes.png`; scrolling behavior is provided by the clamped ScrollRect and RectMask2D (`WorkshopView.Navigation.cs:143-147`). Font-scale150% and short-height captures exist but were not visually inspected by this agent.

## Referenced colors and text contrast

MEASURED FROM TOKENS: nine named hex color tokens are declared in `ForgeUI.cs:9-17`: Background#181c1c, Panel#242a28, Line#55564a, Gold#e5be76, Cream#f8eed8, Muted#bcc1b3, Teal#9fd1b5, Orange#f0a46c, Surface#303a34. These are the named referenced palette count, not a count of every raster pixel or all ad-hoc RGBA tints. Additional state/overlay colors are literal RGBA values (`ForgeUI.cs:86,122-124,132`; `WorkshopView.Battle.cs:27-31`).

Solid-text contrast computed with sRGB linearization and relative luminance:

| Text / solid background | Contrast |
| --- | --- |
| Cream / Background |14.91:1|
| Muted / Background |9.35:1|
| Gold / Background |9.79:1|
| Teal / Background |10.04:1|
| Orange / Background |8.37:1|
| Background / Gold primary button |9.79:1|

The lowest of those primary solid token pairings is8.37:1. Each exceeds4.5:1 for body text. This does not establish a minimum across text painted over a changing illustration, fading/tinted disabled states, anti-aliasing, or every animation frame. `TextShadow` exists for map/workshop labels (`ForgeUI.cs:130-133`; `WorkshopView.Campaign.cs:224`). No sampled on-art contrast minimum was measured.

## State checklist

| State | Present / missing evidence |
| --- | --- |
| Empty | PRESENT: inventory shows `EmptyStock` illustration and actionable sentence (`WorkshopView.Modals.cs:107-109`); hero has `None equipped` (`WorkshopView.Campaign.cs:181-193`), observed in `hero-prepared.png` armor area. |
| Loading | NO EXPLICIT loading state found in inspected UI. These screens load local Resources synchronously (`WorkshopView.Campaign.cs:12-14`), so no network-loading state was observed. Initial cold-start UX was not inspected. |
| Error / blocked | PRESENT: `Notify` explains blocked crafting, equipment switching, recruitment and expedition (`WorkshopApp.cs:117-132`); save failure reports device-storage issue (`WorkshopApp.cs:167`). |
| Success | PRESENT: actual craft result and progression reward pages (`WorkshopView.Progression.cs:120-160`); battle completion alters state/reward controls (`WorkshopView.Battle.cs:91-111`). These result captures were not visually inspected in this audit. |
| Focus | MANAGEMENT PRESENT but DISTINCT SELECTED VISUAL MISSING/ROUGH: page selects Back and scrolls the selected control into view (`WorkshopView.Navigation.cs:149,177-190`); Button selectedColor is white, the default normal tint is also white, and no OnSelect focus outline handler is present in inspected helpers (`ForgeUI.cs:86-87`; `ForgeButtonFeedback.cs:6-36`). Pointer press animation exists; no keyboard-focus screenshot was taken. |
| Disabled | PRESENT: Button disabled tint (`ForgeUI.cs:86`), unavailable battle skills and heal-used/cooldown labels (`WorkshopView.Battle.cs:95-101`), equipment/sell restrictions (`WorkshopView.Modals.cs:130,143-147`), locked future-map glyph (`WorkshopView.Campaign.cs:219`). Children labels are separate Text objects, while the disabled tint modifies only targetGraphic. |

## Known gaps

- This is a source + existing-native-capture audit, not a live device session. Interaction success, motion timing and end-to-end gameplay are outside this visual-only evidence pass.
- No 150%/compact visual inspection, keyboard navigation exercise, battery measurement, cold-start frame timing, minimum text-on-art contrast sampling, or user comprehension study was conducted.
- No scores, verdict, generated art, or implementation edits are included in this evidence report.
