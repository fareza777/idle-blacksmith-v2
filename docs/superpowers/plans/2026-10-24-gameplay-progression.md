# Gameplay and progression implementation plan

> **For agentic workers:** Use superpowers:executing-plans. Steps use checkbox syntax for tracking.

**Goal:** Make every activity serve an understandable next objective and an enduring chapter, workshop or mastery goal.

**Architecture:** Add a core progression advisor and persistent guild contracts alongside the existing simulation. Present them in an unboxed guided dock, chapter journal, order board and actual crafting/reward results. Retain free play and the existing combat, navigation and save system.

**Tech Stack:** Unity 6000.3.20f1, C# 7.3 core, uGUI, bundled Roslyn test runner.

**Spec:** `docs/superpowers/specs/2026-10-24-gameplay-progression-design.md`

## Global constraints

Preserve stable IDs and save progress, approved art, >=42 text, 100/125/150% sizing, reduced motion, Windows and Android. No Git checkout; backed up to `QA/progression-20261024/before-gameplay.zip`.

## Review focus

Inventory changes between rendering and clicking; legacy/null/malformed contract fields; chapter changes during an order; blocked forge and material shortages; switching heroes or opening menus during production.

### Task 1: Core goals and contracts

Files: create `Core/WorkshopSimulation.Progression.cs`, `Core/WorkshopSimulation.Contracts.cs`, `Core/ProgressionState.cs`, `tests/ProgressionTests.cs`; modify state, guidance and simulation mutation hooks.

Interfaces: `JourneyMilestone`, `ChapterProgress`, `ChapterTitle`, `ChapterUnlock`, `MilestoneTitle(int)`, `MilestoneComplete(int)`, expanded `NextStepAdvice` with RecipeId, Region, Route, ItemIndex, Station; `CommissionOffer(int)`, `TryAcceptCommission(int)`, `CommissionProgress`, `TryDeliverCommission()`, `CancelCommission()`, `IsCommissionReserved(CraftedItem)`.

- [x] Add behavioral tests and observe expected failures: material-to-equipment steps, correct boss/rescue route, milestones never regress, all chapters, endgame; contracts reserve/deliver once, no partial consumption, quality ordering, cancellation, save normalization and immutable accepted terms.
- [x] Implement pure advice, remembered achievements, normalized contracts, protected sales and additive save fields.
- [x] Run `node tools/test-core.mjs progression-core`; expect all existing and new tests pass.

### Task 2: Playable workflow and results

Files: create `Runtime/WorkshopView.Progression.cs`; modify view, navigation, modals, campaign, battle, app.

Consumes Task 1 interfaces. Produces `ShowJourney()`, `ResumeJourney()`, `ShowGuildOrders()`, guided action routing and craft/reward result pages.

- [x] Write native UI probes for the objective button, direct equip/result, order reservation/delivery and journal; exercise new behaviors in actual players.
- [x] Implement guided dock and explicit free-play escape, first-session start, 35-milestone journal, order board, rank/mastery and meaningful result continuation.
- [x] Verify against production continuing during pages, large text, compact portrait and defeat navigation.

### Task 3: Complete flow verification and delivery

Files: native tests/build setup, QA evidence, Windows ZIP and Android APK.

- [x] Run core suite and actual Windows UI flow from fresh save through chapter unlock, contract and mastery.
- [x] Get independent review; repair material findings and verify.
- [x] Build and test Android, capture readable screens, rebuild/package final Windows and APK.
- [x] Record verification and final hashes; launch refreshed preview and deliver concise links.

Completed as v0.4.0. Final evidence and artifact hashes: `QA/progression-20261024/delivery.md` and `QA/progression-20261024/final-build-manifest.json`. The normal Windows player is running with the user's regular save path; QA runs used isolated profiles.
