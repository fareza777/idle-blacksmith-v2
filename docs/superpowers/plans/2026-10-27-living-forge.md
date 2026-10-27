# Living Forge implementation plan

> **For agentic workers:** use focused parallel implementers for the independent combat, artisan and living-world modules under superpowers:dispatching-parallel-agents. Root implements shared visual/art/native integration, reviews each module and obtains a fresh whole-change review. Preserve the user's continuous execution instruction.

**Goal:** Implement all approved 0.7 recommendations as playable, visually coherent Living Forge0.8.

**Architecture:** Pure-C# simulation owns formations, targeting, technique and journey transactions. Focused runtime partials render the same contracts. Existing content/save IDs and paid APIs remain compatible; new state normalizes safely.

**Tech stack:** Unity6000.3.20f1, C#7.3/uGUI, built-in imagegen, Node/Python read-only art validation.

**Spec:** docs/superpowers/specs/2026-10-27-living-forge-design.md

## Constraints and review focus

- Baseline d091d32, clean existing feature branch/PR1; use the warm checkout to preserve Unity/Android build state. No concurrent Unity builds, Android tests after editor exit.
- Featureless faces, hand-drawn medium, text100/125/150, Reduced motion and normal saves.
- Focus change while a strike is pending must not steal or duplicate that impact; kill a nonfocused healer without losing the leader/boss state.
- Persisted pending boon/merchant/technique never charges, heals or grants twice; blocked actions are no-ops.
- Old scalar enemy fixture/save data must normalize without resurrecting dead opponents or auto-paying rewards.
- Newly applied equipment techniques and temporary boons must match preview, damage and exported saves; timed effects cannot be driven by tweens.
- Map/scenery cannot display earned progress or rewards that the save does not own.

## Task1 — formation, synergy and expedition choices

Own Core/SagaState.cs, Core/WorkshopSimulation.Saga.cs, Core/WorkshopSimulation.SagaPresentation.cs, new focused Core partials, tests/SagaTests.cs/new battle tests, Runtime/WorkshopView.Saga.cs choice branch only. Root owns combat partial/FX/actor.

Produces the exact formation/event API in spec and choice labels/eligibility plus boon selection API. Tell root the final signatures before UI integration.
- [x] Write and run failing focused formation, target/source lock, support role, synergy and once-only choice/boon tests; retain existing counter and migration tests.
- [x] Implement synchronized single-enemy compatibility view, actual2–3 enemy formations, bounded support actions and focused hits with shared readable threat scheduling.
- [x] Implement explicit Frost/heavy/lightning interactions, meaningful defensive/recovery alternatives and saved journey choices/temporary boons. Build choice UI from actual API state.
- [x] Run unique-label core suite, inspect earned campaign/timing, self-review and report exact APIs and changed files.

## Task2 — technique and real workpiece creation

Own ArtisanState.cs, WorkshopSimulation.Artisan.cs, WorkshopView.Artisan.cs, WorkshopView.ArtisanPresentation.cs, ArtisanBenchScene.cs, focused new helpers, tests/ArtisanTests.cs/new tests. May add narrowly scoped technique fields in WorkshopState.CraftedItem. Root owns artwork generation/selection; use a documented workpiece/VFX contract.
- [x] Write failing technique/category/preview/delivery/old-save/no-double-charge tests; observe meaningful failures.
- [x] Implement category-specific cues and real tradeoffs with persistent chosen technique and feedback; retain original APIs/IDs and assisted/reduced-motion usability.
- [x] Replace finished-icon masking during shaping with workpiece stages and proper contact; final item/reveal uses actual equipment. Hand-painted FX, actual choice costs and clear result comparison.
- [x] Run unique-label suite and self-review. Report art frame contract and final APIs immediately.

## Task3 — atlas, restoration and actionable goals

Own Runtime/WorkshopView.ChronicleWorld.cs, WorkshopView.Restoration.cs, focused new atlas/workshop helpers, Core/WorkshopSimulation.World.cs/new goal helper, tests/ChroniclesTests.cs/new world tests. Avoid Saga/Artisan/actor/combat files and shared WorkshopView main unless a narrow hook is coordinated.
- [x] Write failing tests for earned goal/material/contract links and all three restoration tiers; verify old-save bounds and once-only costs.
- [x] Add usable pan/zoom, light pins, route/position/goal preview and saved-state journey display consuming Task1 APIs when available.
- [x] Make all restoration tiers visually distinct and add earned workshop props/activity; connect contract→material→recipe→guardian→town actions using existing costs/unlocks.
- [x] Exercise fresh six-region progression through core tests, compile runtime and self-review. Report asset/helper contracts and any narrow root hook needed.

## Task4 — artwork, pose rig and immersive battle

Root owns ChronicleActor.cs/new rig/attachment helpers, WorkshopView.SagaCombat.cs, ChronicleBattleFX.cs, ChronicleAtmosphere.cs, selected generated Resources/Art assets/layouts/meta, analyzer and art tests/provenance.
- [x] Generate and inspect reference-aligned workpiece stages, coherent VFX and sharper enemy art. Copy only selected original PNGs; alpha analysis writes metadata only. Coordinate contracts with Tasks2/3.
- [x] Add grounded pose rig, per-family grip/contact/aim trajectories, secondary motion and correct cast/death states; meaningful tests for attachment metadata and native motion checks.
- [x] Recompose arena and controls, render/select up to3 real opponents, show exact threat source/target, focused health and synergy/boon triggers. Journal holds detailed data, compact150 layout stays usable.
- [x] Integrate and review each module; resolve contract conflicts in ledger and run whole source/art/content suite.

## Task5 — verification, review and delivery

Root owns Editor/ForgeBuild.cs, native QA probes, version/docs/screenshots/packages and repository delivery.
- [x] Add real-control tests for focus, support roles, route/boon choices, technique, stage evolution, pan/zoom and all restoration tiers. Preserve meaningful existing probes.
- [x] Freeze Game; build Windows, run new/legacy suites, inspect final screenshots/motion and repair observed defects with focused evidence.
- [x] Freeze Game; build Android, run emulator tests only after Unity exits and test normal OS-touch onboarding.
- [x] Fresh independent whole-change source/visual review, resolve material findings and verify final source export.
- [ ] Package/hash, docs0.8/versioncode8, commit/push existing branch/update attached draft PR. Install normally on connected authorized phone if available; report exact device limitation otherwise.

Final evidence: Core332/0; assets143/0, repeated from staged-source export; Unity196 checks on Windows07/Android02; nativeLivingForge15239 on both targets; legacy1211/793/2019 on both. Independent source and final12-image attachment review closed material findings. WindowsZIP147entries/CRC andAPK0.8/code8 verified. Physical phone absent, APK delivery retained; push/update draftPR follows existing authorization.
