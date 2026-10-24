# Readable Workshop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Deliver the approved readable, warm, coherent Unity interface and the complete forge → equip → expedition → reward loop.

**Architecture:** Preserve the deterministic simulation and existing pixel art. Add a small Core presentation/advice layer for real requirements, gear protection, material names and next-step guidance; rebuild the existing UI partials with shared type/spacing controls, three primary destinations, and explicit contextual back navigation.

**Tech Stack:** Unity 6000.3.20f1, C# 7.3-compatible Core, uGUI, existing point-filtered sprite atlases; bundled Roslyn/.NET Core test runner; native Windows and Android builds.

**Spec:** `DESIGN-IS-2026-10-24/03-verdict.md`, approved by the user's instruction to implement the recommendations. That instruction authorizes implementation and routine choices; no repeated approval gate is required.

## Global Constraints

- Keep all existing saves, recipe/hero/region IDs, economic costs, combat effects and offline limits compatible.
- Three main destinations: Workshop / Heroes / Adventure. Keep the game's existing English product language consistent; conversation/reporting stays Indonesian.
- At root width1080 use body48, secondary42, heading72; text settings125%/150% resize flow-based rows rather than clipping them.
- Use the existing navy/cream/gold/teal identity, warm active work light, stronger foreground separation, quiet background, and role-specific icons.
- Controls target at least144 root units in height (48 at normalized width360); validate physical/density behavior rather than equating Unity units to dp.
- Keep test data isolated from the user's save before startup; retain baseline source archive and build outputs until verification completes.
- No new monetization, regions, heroes, recipes, engine migration, or broad economic rebalance.

## Review Focus

- Old JSON lacks new preferences/protected flags: default safely and preserve progression/equipment.
- Merchant skips protected stock, including unattended/offline cycles; selected-item sale never consumes another item after a live list changes.
- Opening nested panels or changing text size retains context/scroll and leaves an always reachable Back/Close path on short screens.
- Active expeditions prevent equipment/hero changes with a correct explanation; victory/defeat/claims route to the appropriate next step.
- Large text and long dynamic names/material quantities remain readable with no overlapping controls at supported portrait sizes.

## Task 1 — Core safety, preferences, and contextual advice

**Files:** modify Core/WorkshopState.cs, WorkshopSimulation.cs; create Core/WorkshopSimulation.Guidance.cs and tests/PresentationTests.cs; register new tests in WorkshopSimulationTests.cs.

**Interfaces:** public bool CraftedItem.Protected; WorkshopState.TextSize (0..2), ReducedMotion; TryProtectItem(int,bool), TrySellItem(int), NextSaleIndex, SuggestedAction (read-only guidance data), GearPower/Guard, comparison deltas, craft/route blocker text, upgrade preview and material/quality naming.

- [x] Write real behavior tests for protected sale FIFO, explicit sale, stale index rejection, persistence/normalization, route/equip blockers, next-step guidance through crafted/equipped/battle/claimed state, and comparison/upgrade predictions.
- [x] Run `node tools/test-core.mjs redesign-red`; watch new behavior fail while94 baseline cases pass.
- [x] Implement only needed additive fields/methods; use existing simulation mutations for production/combat.
- [x] Run `node tools/test-core.mjs redesign-core`; all tests must pass.

## Task 2 — Readable UI foundation and workshop

**Files:** Runtime/ForgeUI.cs, WorkshopView.cs, WorkshopView.Town.cs; new Runtime/WorkshopView.Navigation.cs; Runtime/WorkshopApp.cs for isolated startup and accurate callbacks.

**Interfaces:** shared text scale, flow layout helpers, purpose icons; full-screen contextual pages with persistent three-tab navigation; HistoryEntry captures page kind/arguments and scroll offsets; Back restores prior context; responsive workshop dock exposes ingredients, recipe picker, action, upgrade preview and merchant pause.

- [x] Add a development-only redesign verification entry that asserts rendered control behavior/geometry against the current old UI; capture its failure before replacing UI.
- [x] Rebuild type/spacing, header, guide card, stage selector, active work composition and dock; preserve sprite animation/game-world interactions while moving labels above night shade.
- [x] Provide in-game text-size/reduced-motion options; keep primary controls keyboard traversable and initial focus explicit.
- [x] Compile all runtime sources; verify no compile errors and capture the workshop for visual inspection.

## Task 3 — Recipe and hero workspaces

**Files:** WorkshopView.Modals.cs, WorkshopView.Campaign.cs, Navigation partial.

**Interfaces:** recipe group expand state and detailed costs; hero inventory shows selected hero, current stats, item comparison, protect and sell actions; selection identity is stable under live production; context-preserving navigation to adventure.

- [x] Add Unity assertions for recipe selection → forge, inventory compare/protect/equip, back-to-hero and onward adventure.
- [x] Build readable scrollable recipe/crew/settings/hero/inventory pages with persistent chrome and content-sized rows.
- [x] Expose three item purposes, exact sale target, protection, merchant pause and proper expedition locks.
- [x] Exercise all controls using uGUI raycast dispatch; verify long lists, short screens and large text.

## Task 4 — Guided progression, quests and adventure

**Files:** Campaign partial, Navigation partial, WorkshopApp callbacks and Guidance consumer.

**Interfaces:** actionable guide generated from real state; quest journal available from guide; route readiness/expected rewards/weakness; contextual quest claims after boss victory.

- [x] Add integration assertions for manual first sword, equip, route departure, result collection and pending story claim.
- [x] Implement clear first-session steps; fold future quests/recipes without making them inaccessible; make locks name actual prerequisites.
- [x] Build region selection and routes with rewards, hero readiness and correct failure destinations.
- [x] Run core and native integration tests; ensure one-time claims and save round trips remain correct.

## Task 5 — Combat feedback and visual polish

**Files:** WorkshopView.Battle.cs, Town partial and shared visual helpers.

**Interfaces:** legible HP/skills/cooldowns, enemy-local heavy attack indicator, damage/guard feedback, state-dependent footer/result action; reduced motion preserves state information.

- [x] Retain existing skill mechanics, verify state-specific availability, improve stage/character emphasis and visual feedback.
- [x] Victory offers loot and next story progress; defeat routes directly to preparation.
- [x] Inspect and repair screenshots at normal,125%,150% text and short portrait layouts.

## Task 6 — Verification, review and deliverable builds

**Files:** new Tests/RedesignSmoke.cs, editor build hooks; QA/redesign-20261024 outputs; update existing smoke paths only for intentional new navigation.

- [x] Build Windows development player and run the isolated new native smoke through rendered UI controls.
- [x] Run all Core tests and appropriate art/asset checks; retain command logs and screenshots.
- [x] Perform one fresh read-only code review against the source baseline and this plan, then fix material issues with regression tests.
- [x] Produce verified Windows build and Android APK, inspect available Android emulator if reachable; otherwise report physical-device limits accurately.
- [x] Record delivered behavior, checks and artifact paths in a concise delivery note.

No Git repository is present. Use the verified source archive and manifest under QA/redesign-20261024 as the review/rollback baseline; do not invent commits or Git worktrees.

