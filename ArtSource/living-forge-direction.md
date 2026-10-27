# Living Forge selected artwork

Production uses the built-in `image_gen.imagegen` tool. The exact selected prompts, source paths and original PNG SHA-256 values are in `living-forge-final.json`. Selected RGBA PNG bytes are copied unchanged to `Game/Assets/Resources/Art/HandDrawn`. Alpha analysis writes JSON crop/feet metadata only; it never paints, repacks or resizes PNG pixels.

Keep ink/gouache contours, warm material detail and distinct regional palettes. Human faces must remain completely featureless or covered, including enemy humanoids. Hero armor remains part of each full-body worn pose. Inventory icons must not be pasted onto actors as armor.

| Sheet | Grid | Reading order |
| --- | --- | --- |
| `foes-{region}-{pair}` | 3 × 4 | Creature A ready/windup/contact; A hurt/recovery/defeat; creature B same two rows |
| `workpieces-metal` | 5 × 4 | Blade, hammer, pole, mail; each material/rough/shaped/assembled/polished |
| `workpieces-fine` | 5 × 4 | Bow, leather, robe, accessory; same five stages |
| `forge-fx` | 4 × 3 | Four sparks, four steam, four rune/reveal frames |
| `held-weapons` | 4 × 2 | Sword/Axe/Bow/Hammer, Staff/single Dagger/Greatsword/Spear |

`FoeAppearance` selects one pair sheet and six contiguous pose indices per creature. Guardians retain their existing four-pose sheets. All six poses of a creature use its ready pose reference size, independent of physical atlas rows. Maintain empty transparent gutters around whole silhouettes, including long weapons, capes and tails; intersecting rectangular crops must be regenerated through imagegen.

`WeaponPoseRig` supplies family motion; the exact grip locations come from `painted-grips-source.json`. Every one of the 24 original base/worn body sheets was inspected independently: 20 poses and two palm points per pose, in original PNG pixels. The offhand reuses the visible palm when it is occluded. `tools/analyze-painted-grips.py` checks original hashes, crop bounds and opaque hand pixels, then writes normalized runtime `painted-grips.json`; it never changes PNGs. Keep hand annotations aligned to each actual painting rather than copying one generic body rig.

`PaintedBodyImage` supplies restrained torso/clothes joints with feet fixed. The held handle follows the same transformed palm point. Spear thrust moves body and hand together; a weapon must never slide out of the palm. Melee placement solves its actual tip at impact and scales the actor along formation depth. Held bow art is unstrung and mirrored in runtime; `BowStringGraphic` draws the string/arrow. These family paintings represent weapon silhouettes rather than unique art for every recipe.

Run `tools/analyze-living-art.py`, `tools/analyze-painted-grips.py`, then `node --test tests/living-art.test.mjs tests/painted-grips.test.mjs`. Review the original sheets and native pose galleries. Bounds and endpoint tests do not establish hand attachment or animation quality; inspect the visible handle/palm contact and real windup/contact/recovery frames too.
