# Illustrated World Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task by task. Steps use checkbox syntax for tracking.

**Goal:** Deliver consistent generated enemy, guardian, equipment, map and landscape art with clearer exploration and polished combat feedback.

**Architecture:** Extend HandDrawnArt with cached atlas/art helpers and generated metadata. Route existing campaign/equipment consumers through those helpers, then refine map status and combat motion without altering simulation or save IDs.

**Tech Stack:** Unity 6000.3.20f1, C# 7.3, uGUI, built-in imagegen, Node native/core/art tests, read-only Python image analysis.

**Spec:** docs/superpowers/specs/2026-10-25-illustrated-world-design.md

## Global constraints

Preserve the approved workshop, all existing saves and stable IDs; Windows and Android ARM64/API26+; portrait; minimum base text42; 100/125/150% scaling and Reduced motion. Version0.5.0/code5. Do not change economy/combat balance. Generated pixels stay unchanged; cropping uses UV metadata.

## Review focus

Atlas row/pose ordering, alpha or neighboring-cell bleed; raw direct UV writes missed in rack/flights; banner aspect cropping; map preview/departure for locked regions; live wave changes and reduced motion; short screens/large text; new texture memory/cache behavior.

### Task 1: Art production and asset wiring

Files: new HandDrawn equipment/enemies/guardians/worldmap and five landscapes with JSON metadata; ArtSource/illustrated-world-generation.md; tools/analyze-adventure-art.py; Runtime/HandDrawnArt.cs; Runtime/WorkshopView.Campaign.cs; Runtime/WorkshopView.cs; tests/illustrated-art.test.mjs; Editor/ForgeBuild.cs.

Interfaces: HandDrawnArt.Icon/PoseIcon for centered aspect-fit equipment; existing Person/Pose for foot-anchored enemies/guardians; HandDrawnArt.Landscape for region crop; cached map texture. Preserve current EquipmentIcon and CampaignSprite call signatures.

- [x] Finish the cited design audit and handoff; review this spec/plan, recording settled implementation calls.
- [x] Generate and inspect every atlas/landscape/map; copy the selected outputs; derive and validate alpha/UV/pivot metadata without changing pixels.
- [x] Add meaningful failing art validation: correct counts, nonempty transparent cells, bounded/cell-isolated UVs, ordinary/guardian distinct assets, item ordering metadata and landscape aspect handling.
- [x] Route every live art consumer, including rack/flights, through new helpers; add asset verification/import settings and pass art tests.

### Task 2: Exploration and combat polish

Files: Runtime/WorkshopView.Campaign.cs; Runtime/WorkshopView.Battle.cs; Runtime/WorkshopView.Motion.cs; Runtime/ForgeUI.cs; new Tests/IllustratedWorldSmoke.cs; WorkshopApp.cs QA argument isolation; Editor/ForgeBuild.cs.

Interfaces: retain Region0..4, StartRoute0..2, BattleSkill0..2, WorldMap, BattleHero and BattleEnemy names. New --illustrated-smoke uses its own QA save path. Existing simulation supplies chapter, quests, battle wave and health; no new permanent gameplay state.

- [x] Add native checks for all-region map names/status, locked preview blocking, correct ordinary/guardian textures/poses, equipment art in rack/flights/detail and all regional landscapes.
- [x] Implement truthful map status/objective/unlock preview and retained direct navigation.
- [x] Implement grounded enemy/guardian pose changes and eased contact/shadows, replace box-shaped enemy alert, make reduced-motion behavior explicit; improve focus visibility.
- [x] Capture five regions, items, map progression and large/compact screens; run existing native workflow/regression probes and inspect screenshots.

### Task 3: Verification and delivery

- [x] Run core, original art and illustrated-art tests; get an independent whole-change review and address material findings.
- [x] Freeze source; build/test final Windows and Android, package Windows, record hashes/test evidence and APK size.
- [x] Update README/prompt/guide/release evidence and publish a reviewable feature branch/PR to the existing repository. Open the refreshed Windows game; update the connected phone if it remains available under the earlier installation authorization. Phone was absent at the final check; APK retained for installation.

Final version0.5.0: core132/0, art31/0; Windows and Android each1177illustrated +787workflow +875regression. Native screenshots and source-export tests passed; material independent-review finding fixed. Delivery evidence is in QA/illustrated-20261025 and docs/VERIFICATION-0.5.0.md.
