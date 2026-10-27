# Battle and forge clarity — v0.7

The user asks to improve battle that feels too fast and unclear, remove armor that floats like a pasted item icon, make battles more exciting, and improve item creation. They have already directed implementation and delegated design choices to the assistant throughout this project. Continue implementation without another approval round.

Baseline: dc069460, version 0.6.0. Keep Unity 6000.3.20f1, Windows/Android portrait, hand-drawn art and completely featureless human faces. Preserve existing item/hero/content IDs and ordinary save progress. Existing artwork stays available as unarmored appearance; normal player saves are never QA fixtures.

## Battle

Keep semi-automatic combat. Give actions readable anticipation, contact and recovery, stagger the three companions' attacks, and announce an enemy's actual target before damage resolves. Do not slow the workshop clock. Normal same-region fights should give players time to notice and respond, roughly 15–30 seconds with a sensible loadout; guardian encounters should sustain multiple decisions. These are design targets to measure, not a claim that every overgeared replay takes that long.

Core time remains deterministic 0.1-second ticks. New fights use a short arrival/opening grace, ordinary hero cadence around 1.7–2.6 seconds, enemy anticipation of at least 1.2 seconds, and a three-second guardian response window. Balance health by authored region/route and occupied party size, not by copying player damage into enemy health. Preserve the value of better equipment. Guard/heal skill durations must remain meaningful at the slower cadence.

Add a persistent tactical pause that freezes saga combat while workshop production continues, an actual stagger/break mechanic built by party actions, a brief vulnerability window and bounded recovery so the enemy cannot be permanently locked. Telegraphs must predict the attack that actually lands, including group attacks. Skills during a mechanic's announced window count; earlier skills do not silently answer a later mechanic.

Publish a bounded ordered event history for presentation: windup, hit, skill/heal/block, break, phase and result. It records actual damage/healing after defenses. Reopening a page must not replay old effects or duplicate simulation rewards. A live scene may animate numbers, contact flashes, guard arcs, projectiles and smoothly following health bars; none of these effects owns gameplay state.

The battle scene prioritizes arena, party health, enemy intent/progress and reachable skills. Put detailed descriptions in an optional journal/guide rather than a wall of text before the controls. Short button verbs (Guard, Focus, Break, Heal, Parry, Ward) keep the three skills readable at 150%; full ability names remain in details. Offer a clear pause/resume control. Reduced motion removes displacement/shake and retains static cues.

## Armor

Remove the inventory armor RawImage layer entirely. Create complete painted outfit atlases for all six heroes, with three silhouette families: mail/plate, fitted leather and runic robe/mantle. Armor must wrap torso, shoulders and limbs and follow every pose as one coherent painted body; no detached breastplate, hanger, mannequin or loose inventory cutout.

Use the existing twenty pose identities/order for every outfit. Keep each hero's hair, build, body proportions, gloves/boots and featureless face consistent. All production sheets have genuine alpha and complete isolated silhouettes. Use built-in imagegen, copy original PNG bytes unchanged and analyze alpha only for layout. Worn armor family follows equipped recipe: Mail/Scalemail → mail, Scout Leather → leather, Ward/Runic Robe → robe. Fine item shape remains visible in equipment detail; do not claim 108 unique body outfits.

Runtime selects the painted sheet for both party portraits and combat. Equipment stats and selected recipe remain authoritative. Existing held weapon presentation may be refined, but armor can never fall back to a pasted icon when an outfit is missing. Missing outfit must be detected during build/native validation.

## Item creation

Keep paid artisan stage authority and once-only delivery. Rework the bench around the item and four clear steps: Heat, Shape, Quench, Rune. Show available materials and exact cost before beginning, live power/guard/effect previews for choices, current quality and progress, and immediate readable feedback for each timed strike. Save the real strike feedback so leaving/reloading does not invent results.

Use the existing completed item artwork as the final target, with progressive reveal/heat, hammer anticipation/contact, sparks and quench/engraving transitions on the illustrated close-up bench. Present choice buttons compactly with tradeoffs beside a visible preview. The finished creation should receive a deliberate reveal, show its alloy/temper/rune and final stats, and allow equip/keep with actual eligibility. Keep automatic worker production distinct and reachable. Do not add duplicated material charges, forced waiting, random destructive failures or a second currency.

## Shared interfaces / ownership

Battle implementer owns SagaState.cs, WorkshopSimulation.Saga.cs, new Saga presentation-contract core partials and SagaTests/ChronicleJourneyTests. Add:
- `SagaCombatEvent` with Id, Tick, SourceSlot, TargetSlot, double Amount, Kind, Enemy and Text; `SagaEventKind` enum. `SagaRunState.Events` bounded to at most 24, monotonic EventSerial, Paused, break/stagger state. Amount is real post-defense magnitude, SourceSlot -1 for enemy, Enemy denotes recipient is enemy, TargetSlot -1 with Enemy=false denotes whole party. Windup carries the same target contract; presentation animates its source.
- `SetSagaPaused(bool)`, `SagaIntent` string, `SagaThreatProgress` double in [0,1], `SagaThreatTarget` int (-1 for group), `SagaBreakProgress` double in [0,1]. Expose enough event/action timing for visual anticipation. Retain old public methods.
- A version marker/default migration for old in-flight runs, with bounded normalization and no duplicate hit at resume.

Artisan implementer owns ArtisanState.cs, WorkshopSimulation.Artisan.cs, WorkshopView.Artisan.cs, a new Artisan presentation helper if needed, ArtisanTests.cs. It may add one delegated ShowCraftResult hook in WorkshopView.Progression.cs only (actual method location verified). Keep current native action IDs where sensible. It must document any new strike-time/cooldown field consumed by native probes. Core item metadata changes in shared WorkshopState.cs must be requested from root instead of editing concurrently.

Root owns ChronicleActor, worn armor assets/layout tooling/tests, battle UI/effects, native probes, version/build/delivery and integration hooks. No agent runs Unity or imagegen or mutates unrelated files. No Game source/assets edits during Unity builds. Core test outputs use a unique run label to avoid shared DLL races. Android native tests run after Unity exits because editor shutdown closes ADB.

## Acceptance and review focus

- Measured encounter cadence, staggered strikes, telegraph target/timing, pause with workshop clock continuing, one-time impact/rewards, bounded break and save/reload frame equivalence.
- All twelve mechanics retain useful real counters, camp and full six-region earned journey remain reachable; no blanket weakening of assertions to hide pacing regressions.
- Every hero × three armor families × twenty poses loads, has valid isolated crops and no armor icon child in the actor. Inspect multiple poses in native rendering, not only ready pose.
- Crafting actual inputs, choice previews, strike feedback, once-only result/equip, reload and 100/125/150% layouts. Touch controls must be reachable without covering the central scene with text boxes.
- Final Windows/Android builds and native new-flow/legacy regressions, independent code/visual review, honest documentation, update the existing GitHub branch/PR, install the new APK on the previously authorized connected phone when present.
