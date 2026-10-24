# Hand-drawn workshop assets

Generated with the built-in imagegen tool for the visual overhaul approved on 24 October 2026. All selected PNGs are copied into `Game/Assets/Resources/Art/HandDrawn`; the original pixel assets are retained. The layout JSON files describe native atlas bounds and foot/hip anchors. The analysis tool reads alpha data and never changes the source pixels.

## Final prompt set

- **workshop.png** — Use the original workshop as an exact composition reference. Repaint the portrait environment as fine ink and textured gouache, with warm amber forge and lantern light, teal/sage forest shadows, worn oak and uneven cobblestones. Preserve the mine upper left, smelter upper right, anvil center and market lower left, including their original normalized coordinates. Leave the upper sky and bottom steps quiet for live UI. No baked people, UI or text.
- **workers.png** — Transparent six-column, three-row full-body animation atlas. Consistent normal adult anatomy, about six heads tall, three-quarter view facing right, warm fine ink and painted cloth/leather. Miner: ready, lift, raised pickaxe, downswing, contact, settle. Blacksmith: equivalent six hammer poses. Shopkeeper: six subtle parcel-offering gestures. Keep feet anchored and every tool inside its frame; no ground, shadows, text or dividers.
- **walkers.png** — Transparent eight-column, four-row walk-cycle atlas. Ore porter in teal vest and slate cap, green-hooded herbalist, burgundy-coated bard with lute, navy-cloaked mature customer. Side profile facing right. Eight successive contact/down/passing/up poses for alternating legs, with opposite arm swing. Consistent identity, body volume and camera scale; no environment or labels.
- **idles.png** — Match the same four walkers in a two-column, four-row atlas. First column: relaxed side-profile standing, both boots flat. Second: a natural near-front head/shoulder turn while both feet stay planted. Preserve outfits, faces, body proportions and scale. Transparent background, no shadows or text.
- **heroes.png** — Match the worker illustration style. Four columns by three rows: ready, attack, defensive recoil, calm victory. Rowan: teal guardian with iron sword and round shield. Lyra: auburn-haired green-cloaked ranger with bow. Kael: bearded navy/brass rune smith with hammer. Clear normal hands, grounded legs, consistent identities, transparent background, no UI or scenery.
- **props.png** — Match the painted workshop. Three-by-three transparent atlas: ore cart, bellows, tool rack; festive pennants, brass guild crest, flower planter; lantern, supply crates, repaired noticeboard with blank parchment. Elevated front view, worn oak and brass, clean isolated silhouettes, no background or text.

## Motion direction

Walking uses traveled distance to select poses, eases into/out of travel, and pauses/turns at endpoints. Atlas frames keep a uniform character scale and use a shared planted-foot origin. Soft contact shadows ground the people. Work cycles have longer anticipation and short contact poses; strikes and sparks share the contact beat. UI movement is limited to a short fade/slide, a small press response, and eased material flights. Reduced motion suppresses the optional movement.

Typography uses Lato and Spectral from Google Fonts. OFL licenses are included beside the font files in `Game/Assets/Resources/Fonts`.
