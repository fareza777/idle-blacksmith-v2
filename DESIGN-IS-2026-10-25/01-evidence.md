# Consolidated evidence — 0.4.0, before changes

## E1 — Mixed media and undersized source art

Actual native captures `QA/progression-20261024/android/regression/battle-active.png`, `hero-prepared.png`, `recipes.png`, `adventure.png` show painted heroes beside pixel enemies, landscapes, 32px equipment and a 384x480 pixel map. `Runtime/WorkshopView.Campaign.cs:10-24` only routes heroes to the HandDrawn helper. Equipment is enlarged 4.5x–10x. Ordinary and boss enemy art is the same regional sprite, with the boss enlarged. See evidence-visual.md and evidence-structure.md for exact source/region references.

## E2 — Map hides the journey

`WorkshopView.Campaign.cs:205-228` shows only unlocked names; later areas are anonymous locks. The map does not expose the active objective or earned frontier completion. The existing journal already supplies the required truthful state. Evidence: copy C1, structure art-consumers section.

## E3 — Useful core and restrained workshop

The current dock routes a real resource-to-gear-to-expedition loop; native probes verify equip, reward, unlock, order and recovery actions. The workshop has no permanent ContextDock image and three primary navigation destinations (`RedesignSmoke.cs:43-52`). Pages disable covered workshop interaction. Contextual links add preparation steps but no decoy purchase or fabricated reward was found. Evidence: structure counts/QA contracts and copy claims-checked.

## E4 — Small but meaningful wording gaps

Departure promises a chapter for every boss, even repeat/final bosses, while unlocking actually needs a story claim; weapon damage and armor protection are conflated; loss is called retreat despite no retreat control. Order acceptance gold resembles a cost; later chapter patrons are all called wardens; some journey-reserved stock lacks its reason. Evidence: evidence-copy.md C2-C7, with both label and mutation citations.

## E5 — Typography, accessibility and detail

The shared type scale is42/48/56/72 with100/125/150% settings. Nine named colors; lowest checked primary solid text contrast8.37:1. Empty/blocked/success/disabled states and scroll focus management exist. Distinct keyboard focus visuals are rough; cold-start/loading presentation and on-art contrast were not measured. Evidence: evidence-visual.md state table/tokens. Back/nav/skill symbols remain16px point-filtered masks.

## E6 — Native weight and motion

APK52,729,407bytes;23PNG sources11,103,923compressed bytes /49,191,304estimated RGBA bytes. HandDrawn textures are cached and uncompressed. Covered workshop keeps ticking; normal idle has seven motion channels plus20conditional raindrops, with reduced-motion gating. TTI/GPU/battery unmeasured; JS/network metrics are inapplicable to this native offline game. Evidence: evidence-weight.md.

## Evidence limits

Reports are source and actual prior native-capture audits; no fresh usability study or device profiling is claimed. Counts are dynamic/source-derived. Full reports: evidence-structure.md, evidence-visual.md, evidence-copy.md, evidence-weight.md. Literal superset catalog: runtime-copy-string-catalog.tsv.
