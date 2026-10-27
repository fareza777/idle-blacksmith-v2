# Illustrated world — production prompt set, 25 October 2026

Generated with the built-in imagegen tool, followed by targeted built-in image edits for atlas spacing and pose cleanup. Final PNG pixels are copied unchanged into `Game/Assets/Resources/Art/HandDrawn`. Original source pixel art remains in its original location. `tools/analyze-adventure-art.py` reads alpha/components and writes crop/anchor metadata; it never paints, resizes or rewrites PNGs.

## Shared direction

Production art for Ember & Oak, a portrait fantasy blacksmith RPG. Premium traditional storybook illustration: fine warm ink contours, textured gouache, muted teal/umber shadows, ivory/brass highlights, grounded anatomy, worn oak/leather/forged metal. Readable silhouettes at small display sizes. No pixel blocks, glossy 3D, logos, watermarks, baked UI, text, labels or numbers.

## equipment.png

One transparent4-column x5-row atlas, stable row-major IDs0–19. Columns: sword, axe, mail torso, protective ward/mantle. Every item complete, centered and isolated with transparent gutters; no background/card/shadow. Later tiers become more intricate without giant effect clouds.

| Row | Sword | Axe | Mail | Ward |
| --- | --- | --- | --- | --- |
| Emberwood | Dependable iron longsword, leather grip, simple brass guard | Rimewood frost axe, pale steel, worn wood, cold sap inlay | Riveted iron chainmail and leather belt | Sage/ice-blue quilted protective mantle |
| Marsh | Reedfire copper/steel blade, restrained ember inlay | Thunderfen dark teal axe, brass lightning inlay | Bronze Reedfire mail, warm red cloth | Teal Thunderfen mantle, storm stitching |
| Frostpeak | Dawnfire silver blade, amber glass | Broad ice-blue Glacier axe | Silver/amber Dawnfire mail | Glacier mantle, white wool trim |
| Caldera | Winterheart crystal-cored silver-blue sword | Black Pyrebreaker axe, orange core | Silver-blue crystal Winterheart plates | Red/black Pyrebreaker mantle, bronze plates |
| Starfall | Storm-silver Skybreaker sword, obsidian grip | Nightstar obsidian/violet-star axe | Celestial silver/black Skybreaker mail | Midnight Nightstar mantle, star-metal trim |

Final cleanup prompt: preserve exact twenty designs/order/materials; shrink and center every item in its own cell, keep every blade/handle/garment intact and increase transparent margins. No added items, grid or labels. The resulting visual4x5 packing uses alpha-derived individual bounds rather than assuming mathematically identical UV cells.

## enemies.png

Transparent4-column x5-row animation atlas. Each row is one consistent identity; all source art faces left. Columns are ready, attack-left, hurt/recoil, defeated (no gore). Keep body scale and planted base consistent. Rows: Cinderroot Stalker (charred-root/moss wild boar), Mistbound Ferryman (hooded drowned ferryman with oar), Icebound Sentinel (frost knight with sword/shield), Slag Colossus (furnace rock construct), Void Sentry (midnight armored spear sentry).

## guardians.png

Same four-pose layout, five distinct imposing bosses: Thornback (ancient boar with charred oak-thorn crown and ivory tusks); Mire Warden (reed/wood marsh spirit with bronze ferry bell in ribbed chest); Frostfang (fallen mountain king in ice crown, silver-blue armor, greatsword and fur mantle); Cinder Colossus (huge basalt/bronze furnace golem with molten fists); Starbound Sentinel (regal midnight/silver guardian with celestial halberd, violet cape and broken astrolabe halo).

Final actor cleanup prompt: preserve identities, painting style, rows and four poses. Increase transparent space, keep all weapons/horns/capes inside each sprite, remove broad magical slash clouds, smoke and particles, remove arrows from hurt poses. No overlapping figures, shadow platforms, scenery, labels or checkerboard. Keep defeated figures on a consistent base. Semantic foot positions and row reference scales are recorded in JSON and checked in native rendering.

Final packing correction: substantially repack the same twenty poses into a contact sheet with large empty gutters; each complete silhouette including weapons must be half its previous width/height, centered at four evenly spaced column centers and five row centers. Keep whole spear tips/butts and ice blades; rotate ready weapons more upright and shorten attack reach. The game scales each sprite independently. Selected atlases are accepted only if every opaque connected silhouette is fully contained in its crop and neighboring crop rectangles remain disjoint.

## worldmap.png

Portrait4:5 bird's-eye illuminated travel atlas. Amber woodland and blacksmith hamlet at lower left; misty reed-water ferry landing at lower right; icy mountain watchtower at upper left; volcanic furnace gates at upper right; distant celestial citadel at top center. Connect all five with a winding trail, bridges, mountain switchbacks and pale stone stairs. Warm subtle parchment edges, finely inked geography, atmospheric depth, tiny quiet compass ornament. No baked pins, names, locks, numbers, buttons, panels or characters. Final live pin positions are calibrated to landmarks in the selected painting, not blindly to the original prompt coordinates.

## Regional landscapes

Five separate full-bleed landscape16:9 paintings. Low side-on2D battle camera; quiet continuous ground across bottom25%, foot positions at x24%/78%, y82% from top. Detail in middle/far distance and side framing. No characters, monsters, text or UI.

- **emberwood.png:** amber oaks and teal pines at dusk; old stone trade bridge, charcoal camp, small distant fire/lanterns; worn earth/stone clearing.
- **marsh.png:** sage reeds, misty turquoise water, old bronze ferry bell and wooden landing; willow trees/stone arch, damp solid stone causeway under fighters.
- **frostpeak.png:** pale dawn mountain pass, snow peaks, stone watchtower with amber beacon, frost and banners around a clear snow-dusted terrace.
- **caldera.png:** basalt furnace courtyard, bronze pressure gates, distant volcanic glow and side lava channels; clear flat stone under fighters.
- **starfall.png:** midnight royal terrace, ruined celestial arches, observatory astrolabe and fallen star over blue-violet citadel; moonlit obsidian/silver stone floor.

Banner crops preserve image aspect and favor landscape focal height; battle crops preserve the approximately82% ground line. Android uses ETC2 RGBA8 with2048 maximum texture dimension; UVs stay normalized, and new atlas metadata also records source dimensions so pose size survives platform downsampling.
