# Living Forge 0.8 design

The user approved every recommendation from the 0.7 visual/gameplay audit with “kerjakan semua”. This is execution authorization, including routine design choices, existing draft PR delivery and connected-device update from prior turns. No additional design approval is required. Preserve the hand-drawn ink/gouache world and completely featureless human faces.

## Outcome

Combat occupies the main viewport with grounded, weapon-specific motion, targetable small enemy formations and visible synergy. Creation shows an actual workpiece changing shape and has category-specific technique. The atlas becomes a navigable journey; restoration changes the world through all three tiers. Existing goals, contracts and mastery explain a useful next creation.

## Combat authority and journey

- Keep the fixed .1-second simulation, once-only material/reward settlement and pause/save guarantees. Released content IDs, all twelve guardian counters, legacy runs and late-game builds remain supported.
- Add up to three enemies with guardian encounters remaining focused single bosses. The first supply encounter stays approachable; later rooms and rescue/elite routes introduce formations scaled to party size. Shield bearer, healer and ranged threat have real, announced effects. Changing focus cannot redirect an already announced enemy impact.
- `SagaRunState.Enemies` contains up to three `SagaEnemyState` records. `FocusEnemy` selects the recipient of party hits; `ThreatEnemy` records an announced attack's source. Existing scalar enemy fields remain a synchronized compatibility view. `SagaEnemyCount`, `SagaEnemyAt(int slot)` and `SelectSagaEnemy(int slot)` expose the authoritative formation. An event `EnemySlot` identifies its enemy recipient (Enemy=true) or source (Enemy=false). Events remain bounded, ordered and resumable.
- New `SagaEnemyState` fields include EnemyId, BossId, HP, MaxHP, Guard, Barrier and a readable Role. Actors may derive presentation from events/schedule; UI never applies damage.
- Make Frost, heavy weapon stagger and lightning interact visibly, with counter/defensive/recovery builds remaining useful. Explain actual equipped synergies before departure and in combat details. Log real post-defense damage, healing and triggered effects.
- Preserve choices0 safe/1 camp/2 rescue for existing callers. Add optional merchant/event/elite choices at eligible nonfinal rooms and a bounded, persistent choice of temporary expedition boons. Expose choice title/description/eligibility so UI and tests agree. A pending event survives save and pays/advances once. Use existing gold/material/mastery rather than adding currencies.

## Crafting authority and presentation

- Keep paid alloy, quench, rune and equip/keep APIs and control IDs. Introduce optional technique fields with old-save defaults. Techniques are actual stat/effect tradeoffs and previews must exactly equal delivered items.
- Weapon shaping emphasizes a strike sequence, armor emphasizes balanced fitting, accessories/staves emphasize engraving. Provide clear cues, a forgiving assisted/reduced-motion path and truthful persisted feedback. No passive manufacture of perfect strikes.
- Production workpiece sheets show material, rough blank, shaped piece, cooled piece and finished detail. Choose a relevant family (blade, hammer, pole/staff, bow, mail/plate, leather, robe/accessory), with the actual chosen equipment reserved for final assembly/reveal. Perspective places workpieces on the anvil; hammer contact matches the indicated work area.
- Replace old pixel sparks/smoke used by new scenes with coherent hand-painted VFX. Store original generation provenance, copy PNGs unchanged, analyze alpha only to write layout metadata.

## Runtime visual system

- Make the arena dominate available vertical space on ordinary portrait displays; preserve a fully visible arena and reachable commands at compact1280 and text100/125/150%. Keep main text readable, remove duplicated always-visible explanations/vitals, expose detailed numbers through tactics.
- Use per-pose skeletal/attachment metadata, grounded feet, a weapon grip and direction per weapon family, drawn key poses and restrained secondary motion. Preserve complete-body armor. A lightweight 2D pose rig within the current UI renderer is an implementation choice; an engine/package migration is not required for the result.
- Rebuild enemy art at useful source resolution, preferably individual/four-pose sheets per creature if atlas density makes silhouettes soft. Use consistent contour, palette and light; foreground actors remain separated from scenery.
- Weapon hit, enemy windup, contact/recoil, recovery, stagger, death and phase changes must have distinct readable poses/cues. Reduced motion preserves targeting and outcomes.

## Living atlas and progress

- Provide map pan/zoom with light hand-drawn pins, clear current position/travel path and eligible encounter/reward preview. Keep ordinary button navigation accessible and distinguish locked regions without opaque circles covering landmarks.
- Show actual route progress and event/merchant/elite/guardian/camp nodes from saved state. All route choices and rewards invoke real simulation APIs.
- Give each restoration project ruined/tier1/tier2/tier3 visual progression. Reflect earned town work in the workshop with staged props/activity, not invented unlocks.
- Connect current milestone, contracts, material source and guardian/mastery reward through actionable destination links. Explain a useful next recipe/build with its real costs/requirements. Existing six-region journey must remain completable from an ordinary fresh save.

## Verification and delivery

Focused red/green pure-C# tests cover formation focus, locked-source impacts, empty/dead slots, all counters, synergy amounts, event/boon transaction once-only, pause/reload/batch equivalence, technique preview/cost/feedback, and restoration/action links. Native probes exercise actual controls and inspect battle/crafting/map/restoration at150% and Reduced motion. Visually inspect new artwork and motion frames. Freeze every Game source/asset during Unity builds. Release-build Windows/Android, run current and legacy suites, package/hash, validate staged source, push existing feat/illustrated-world and update draft PR1. Install -r normally on the authorized physical phone if connected; never QA flags on it. No human enjoyment/retention or physical FPS claims without those observations.
