# Illustrated world — design

The user requests an audit and implementation of further improvements, specifically AI-generated enemies, equipment and the world map. Earlier direction remains warm ink and gouache, believable anatomy, readable large text, restrained motion and a workshop free of obstructive text boxes. Implementation proceeds in the existing Unity project; this is the implementer's art direction within the authorized request.

## Intended result

Adventure and crafting should look like the same illustrated game as the approved workshop. Equipment should look worth making. Each frontier should have a recognizable landscape, ordinary foe and distinct guardian. The map should explain where the player is, what is open and what comes next. The existing five-chapter gameplay, economy and saves remain intact.

## Assets

Generate via the built-in imagegen tool, preserve original generated pixels, copy selected images into `Game/Assets/Resources/Art/HandDrawn`, and save prompts in ArtSource. Use real alpha for actor/item atlases. Derive UV bounds/foot pivots in metadata; never paint/retouch the images with scripts.

- `equipment`: 4 columns x 5 rows, stable IDs 0–19 in row-major order. Each row is a region, with sword, axe, mail, ward. Match the exact names/materials/elements in GameContent. Items must have different readable silhouettes, restrained elemental accents and no baked UI/card backgrounds.
- `enemies`: 4 columns x 5 rows; five regional ordinary foes, each with ready, attack-left, hurt, defeated poses. Cinderroot Stalker (ember/root boar), Mistbound Ferryman (hooded drowned ferryman), Icebound Sentinel (frost armored knight), Slag Colossus (small furnace rock construct), Void Sentry (starless armored sentry).
- `guardians`: the same layout, distinct boss designs: Thornback, Mire Warden, Frostfang, Cinder Colossus, Starbound Sentinel. Readable mass/detail changes, not an enlarged ordinary sprite.
- `worldmap`: portrait illustrated map with five landmarks. Normalized top-left pin positions: Emberwood (.20,.80), marsh (.68,.67), Frostpeak (.245,.465), caldera (.716,.323), citadel (.49,.131). A winding trail connects them. No baked words, pins, locks, UI or numbers.
- Five separate landscape paintings (`emberwood`, `marsh`, `frostpeak`, `caldera`, `starfall`): side-on 16:9 combat clearing with a quiet ground plane at about 82% image height. Distinct amber woodland, misty ferry marsh, frozen watchtower, lava furnace, and midnight celestial citadel. No characters or UI. Crop to fill hero/region/journal banners using source UVs.

Use the approved painted hero atlas as style reference. Keep generated art legible when downscaled. Preserve old pixel sources as archival/source material; all live campaign art consumers must use the new illustrated assets.

## Presentation and flow

- Central atlas helpers preserve aspect ratios and use stable row-major IDs. New images must not be stretched into square item/actor rectangles. Every actor pose uses a common foot origin and per-row reference scale.
- All equipment consumers switch together: recipes, detail, inventory, hero slots, craft results, collections, guild orders, workshop rack and material flights.
- Regional landscapes replace combat and every Landscape banner. The map is a painting with live buttons and text on top.
- Map names are visible for all five frontiers; current, cleared, available and locked states are distinct. Locked frontiers may be previewed, but departure stays gated by actual progression. Show a truthful current objective and the requirement for the next frontier.
- Combat uses ordinary foes for supply/elite encounters and the separate guardian for the final boss wave. Grounded actor shadows, short anticipation/lunge/recoil/settle and health feedback should make contact readable. Telegraphs use restrained light, not a rectangular orange panel covering the enemy. Reduced motion removes positional effects/flashes while retaining readable state and damage information.
- Maintain primary navigation and UI object names needed by regression probes. Improve focus visibility using the existing controls without adding chrome to the workshop.

## Constraints and acceptance

Unity 6000.3.20f1, Windows and Android ARM64/API26+, portrait, existing stable recipe/region/save IDs, 42-unit minimum text at base size, 100/125/150% text scale, Reduced motion. No economy/combat balance or chapter changes in this art pass. Version 0.5.0 / Android code 5.

Native captures must cover ordinary and guardian combat across five regions, all 20 items, the map at several progression states, large/compact text, and all existing workflow checks. Inspect generated alpha/cell bounds and actual screenshots. Review memory/loading and APK size; avoid per-frame asset loads or texture processing. Build Windows/APK, test actual players and preserve normal saves. Existing Git commit is the source rollback point; keep the current Unity checkout/cache for native build work.
