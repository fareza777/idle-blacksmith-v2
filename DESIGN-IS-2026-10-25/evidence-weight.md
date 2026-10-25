# Weight and friction evidence — v0.4.0

## Sources consulted

- `Game/Assets/ForgeEmpire/Runtime/WorkshopApp.cs:20–61`.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.cs:59–91,281–350,398–404`.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Town.cs:20–35,40–73`.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Motion.cs:13–39`.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Navigation.cs:120–150,167–174`.
- `Game/Assets/ForgeEmpire/Runtime/HandDrawnArt.cs:18–26`.
- `Game/Assets/ForgeEmpire/Runtime/WorkshopView.Campaign.cs:10–20`.
- `Game/Assets/ForgeEmpire/Editor/ForgeBuild.cs:32,47–52`.
- File byte counts from filesystem; PNG dimensions read directly from each IHDR without altering pixels.

## Required metrics and concrete findings

| Metric | Value and method |
| --- | --- |
| Initial JavaScript bytes | **N/A**: native Unity/C# game, no browser bundle. |
| Primary-view network requests | **0 app-initiated requests INFERRED**, not packet-captured. Full `Game/Assets/ForgeEmpire` source search found no UnityWebRequest, HttpClient, WWW, WebRequest, Socket or HTTP URL references. Art/audio are loaded from Resources (`WorkshopApp.cs:42–44`, `WorkshopView.cs:149`). Unity engine/platform traffic was not measured. |
| Time to interactive | **Unmeasured**. Startup performs save load/offline simulation, synchronous Resources audio loads, UI construction, save write and welcome/offline routing (`WorkshopApp.cs:25–48`). No frame timestamp measures first interactive UI in the inspected evidence. |
| APK bytes | **52,729,407** (`Builds/Android/ForgeEmpire.apk`). |
| Windows ZIP bytes | **75,947,956** (`Builds/ForgeEmpire-Windows.zip`). |
| Resources folder source bytes | **14,385,649**, recursively including metadata/layout/font/audio. |
| All PNG source bytes | **11,103,923** across 23 PNG files. Sum of `width×height×4` = **49,191,304 bytes**, an RGBA32 estimate, not measured GPU residency. |

### Texture measurements relevant to generated art

| Source | PNG bytes | Dimensions | RGBA32 estimate |
| --- | ---: | --- | ---: |
| HandDrawn/workshop | 2,780,303 | 1010×1557 | 6,290,280 |
| HandDrawn/workers | 1,577,124 | 1586×992 | 6,293,248 |
| HandDrawn/walkers | 1,952,763 | 1586×992 | 6,293,248 |
| HandDrawn/idles | 989,078 | 1122×1402 | 6,292,176 |
| HandDrawn/heroes | 1,861,454 | 1448×1086 | 6,290,112 |
| HandDrawn/props | 1,711,140 | 1254×1254 | 6,290,064 |
| Legacy enemies | 25,406 | 1600×80 (20×80px frames) | 512,000 |
| Legacy equipment | 6,762 | 640×32 (20×32px frames) | 81,920 |
| Legacy battlefields | 41,952 | 1600×180 (5×320px frames) | 1,152,000 |
| Legacy worldmap | 44,538 | 384×480 | 737,280 |

Editor setup forces all Art textures **Uncompressed**, no mipmaps, Clamp, non-readable, max size 4096. HandDrawn gets Bilinear, original strips Point (`ForgeBuild.cs:47–52`). Therefore compressed PNG byte size alone understates a high-resolution replacement's memory footprint. Actual runtime formats, driver memory, loading spikes and compression quality were not profiled.

`HandDrawnArt.Get` loads texture+layout lazily and retains both in a static Dictionary with no eviction (`HandDrawnArt.cs:18–26`). Existing CampaignSprite uses synchronous `Resources.Load` each construction, backed by Unity's resource handling (`Campaign.cs:10–20`); actual repeated disk IO cannot be inferred from call count. Page rebuild destroys old Page objects and creates a new hierarchy (`Navigation.cs:120–147,167–174`).

### Idle animation and update counts

- **Seven continuous presentation channels in the normal non-reduced, no-job workshop, source-inferred:** 3 townsfolk walk/turn channels, 1 customer walk/turn, 1 smoke rise/alpha, 1 night-shade phase, and 1 forge timing-marker phase (`Town.cs:40–68`; `WorkshopView.cs:317–326`). The timing marker transform is updated even while its parent is hidden. Night shading moves very slowly, and actors pause at endpoints, so seven channels does not mean seven visible movements at every instant.
- Weather adds **20 raindrop transforms** only during one clock interval out of four (`Town.cs:53–57`; array size `:9`). Active production adds up to three visible worker-pose channels (miner/smith/merchant), forge sparks/glow and an 8-image flight pool (`WorkshopView.cs:28–38,175–179,289–333`). Smelter's old fire actor is hidden (`:155`).
- Workshop simulation/view updates continue when a fullscreen Page exists: `WorkshopApp.Update` calls View.Tick unconditionally after simulation (`WorkshopApp.cs:51–61`), and View.Tick calls TownLife and worker loops even when HasModal (`WorkshopView.cs:281–337`). Battle drawing itself early-returns unless its page is visible (`Battle.cs:109–112`).
- UI Refresh runs every **0.2 seconds**; save write every **10 seconds** (`WorkshopView.cs:337`, `WorkshopApp.cs:45,61`). These are scheduling facts, not CPU/GC measurements.
- Target is **60 fps**, vSync disabled (`WorkshopApp.cs:22`); Reduced Motion with no active forge/battle lowers target to **30 fps** (`:55–56`). Desktop runInBackground=true (`ForgeBuild.cs:32`).

### Reduced-motion gating

- Town/customer sample time is forced to zero, weather is hidden, night-shade phase fixed (`Town.cs:43–64`). Worker poses are forced to frame 0 (`WorkshopView.cs:299`).
- Smoke gets a fixed pose/alpha, optional sparks are disabled, new material flights are skipped (`WorkshopView.cs:322–326,344`).
- Camera tween and page fade become immediate; normal durations are 0.24 s and 0.18 s (`Motion.cs:13–39`).
- Essential forge timing indicator still runs; battle pose/state feedback remains while movement/flash is gated (`WorkshopView.cs:317–319`; `Battle.cs:111–129`).

### Notifications / badges / modal count on initial load

- Fresh save: **1 fullscreen welcome Page**, created after the base workshop (`WorkshopApp.cs:47`). Existing save away ≥60 s: **1 fullscreen offline summary**; otherwise **0 startup pages**. Page creation first destroys the previous page (`Navigation.cs:120–124`), so these are not a stacked modal pile.
- Startup toast: **0 normally**, **1 if backup recovery occurred** (`WorkshopApp.cs:48`). The single toast panel is initially inactive (`WorkshopView.cs:85–87`); Notify reuses it and sets an expiry of **3.5 s** (`:398–404`).
- **0 alert-badge objects found** in inspected startup constructors. Chapter/step and resource values are task/status text, not unread notifications (`WorkshopView.cs:108–137`, `Navigation.cs:35–43`). This is a source classification, not a measured user-attention study.

## Per-principle facts (no scores)

- #2/#4: first-load and return summaries can occupy one page; they replace one another rather than stack.
- #5/#9: seven idle channels are configured; animation is gated by Reduced Motion, but covered workshop work still updates.
- #8/#9: old enemy/equipment art occupies tiny native frames; larger replacements affect the uncompressed memory estimate and synchronous loading path.
- #6: startup TTI, real hardware GPU/CPU/battery cost and true network activity are explicitly unmeasured.

## Known gaps

No fresh native launch performance trace, Android GPU memory/battery measurement, packet capture, draw-call count, GC allocation profile, or FPS distribution was collected. Source PNG size and RGBA estimates are reproducible file measurements; they are not APK texture runtime memory claims. Web-specific JS/network-bundle scoring thresholds do not directly describe this native game.
