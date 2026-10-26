# Battle and forge clarity implementation plan

> **For agentic workers:** use focused parallel implementers for independent battle and artisan modules, then a fresh whole-change review. Keep Game source and assets frozen during builds.

**Goal:** Make combat readable and interactive, paint worn armor into the hero's body, and make creation communicate choices, technique and results.

**Architecture:** Preserve the pure-C# simulation as authority. Add deterministic combat timing/event contracts and persistent artisan feedback; runtime consumes them for readable presentation. Replace armor icon overlays with full painted outfit sheets selected by equipped recipe.

**Tech stack:** Unity 6000.3.20f1, C# 7.3/uGUI, built-in imagegen PNG, Node/Python read-only asset validation.

**Spec:** docs/superpowers/specs/2026-10-26-battle-forge-design.md

## Constraints and review focus

- Stable released IDs, normal saves preserved, featureless human faces, 100/125/150% text and Reduced motion.
- Source baseline dc069460; use existing warm Unity checkout and attached PR. No concurrent Unity builds or Android probes during editor shutdown.
- Reopening/resuming cannot replay hits, duplicate loot or charge a craft twice.
- Paused battle cannot block workshop time or skip a pending impact after reload.
- All guardian counters must remain usable at slower cadence, including group attacks and empty slots.
- An armor family must render for every action/hero, including a fallen or walking hero; no icon fallback.
- Strike feedback/preview must match real stats, including after leaving/reloading and when equip is locked by a run.

## Task 1 — combat timing and event authority

Files: Core/SagaState.cs, Core/WorkshopSimulation.Saga.cs or focused partial, tests/SagaTests.cs, tests/ChronicleJourneyTests.cs.
Interfaces: produce the event/pause/threat/break APIs declared in the spec; retain old methods.
- [x] Add focused tests for baseline pacing, staggered impacts, announced targets, pause/workshop independence, break recovery and save equivalence; observe new requirements fail.
- [x] Implement arrival/windup/contact cadence and party-size encounter balance; retain all twelve mechanic counters and once-only result handling.
- [x] Run pure-C# suite with a unique label, measure ordinary/guardian fixtures and earned campaign; record precise timing and migration decisions.
- [x] Review code and integrate confirmed issues with red/green evidence.

## Task 2 — meaningful creation presentation

Files: Core/ArtisanState.cs, Core/WorkshopSimulation.Artisan.cs, Runtime/WorkshopView.Artisan.cs, new focused presentation helper; tests/ArtisanTests.cs. Narrow ShowCraftResult delegate in WorkshopView.Progression.cs (verified method location).
Interfaces: preserve current paid APIs and action names; publish persisted strike feedback and truthful preview from the same stat function.
- [x] Test feedback persistence, stage/cost authority and preview equality before implementing new state behavior.
- [x] Build item-focused four-step bench, previews, input/strike feedback, deliberate stage/reveal transitions and eligible equip/keep result.
- [x] Run pure tests; root exercises actual controls, stage return, final result and large text in native builds.
- [x] Review/integrate without shared-file edits to battle or actor code.

## Task 3 — painted worn armor

Files: Runtime/ChronicleActor.cs, new wardrobe mapping helper, HandDrawn art PNG/layout/meta, tools/analyze-chronicle-art.py, new wardrobe art tests, ArtSource production manifest.
Interfaces: `ChronicleWardrobe.Sheet(int heroId, int armorRecipe)` returns existing unarmored or new worn sheet. Recipe mapping follows declared families; no inventory armor image is created in an actor.
- [x] Generate eighteen complete twenty-pose outfit atlases using the current hero sheets as identity/pose references. Inspect every source; preserve featureless faces and alpha.
- [x] Analyze source alpha into layouts, validate full silhouettes/nonoverlap/provenance and sufficient resolution; regenerate defective sheets.
- [x] Select worn body sheet in ChronicleActor and remove armor overlay code. Align weapon motion to painted contact poses and keep feet grounded.
- [x] Validate all 360 worn poses and native ready/attack/brace/hurt/walk/defeat cases.

## Task 4 — readable battle scene

Files: Runtime/WorkshopView.Saga.cs, new focused combat FX helper, actor integration, ForgeUI helpers only if necessary.
Consumes: Task 1's timing and event APIs and Task 3's complete actor.
- [x] Prioritize arena, enemy intent/target/progress, health and three short skill verbs. Add tactical pause and optional detail/journal view.
- [x] Consume each new event once for actual damage/heal/block feedback, attack contact/projectiles, break/phase cues and smoothed HP; no gameplay mutation from tween completion.
- [x] Keep skills reachable on compact and 150% layouts; reduced motion preserves meaning without moving the camera/actors.

## Task 5 — native verification and delivery

Files: Tests/ChroniclesSmoke.cs and a focused new native probe, Editor/ForgeBuild.cs, README, guide/verification docs and screenshots.
- [x] Run current core/content/art suites and the new wardrobe checks. Update native probes for intentional new timing and actual outfit sheets, never bypass real costs/results.
- [x] Freeze Game source/assets; release-build Windows. Run normal/new flow plus legacy native suites and inspect motion frames and 150% text.
- [x] Repair material findings, then release-build Android and run emulator probes after editor exit. Independently review source/art/runtime evidence.
- [x] Package/hash Windows/APK, verify staging export, update docs/version and prepare delivery on the existing branch/draft PR.
- [ ] Install with -r on the authorized physical phone and open a normal session. The phone was not connected at the final ADB check; the tested APK is ready.

Final verified evidence: Windows04 and Android01 each pass139 Unity asset/save checks and6681 new native checks. Original Windows navigation/progression/art suites pass1211/793/2019; Android passes1204/793/2019. Staging export passes271 core and91 asset/content/audio tests. See docs/VERIFICATION-0.7.0.md for package hashes, measured fixtures and limits. Core source and visual evidence received separate independent reviews with no remaining material findings.
