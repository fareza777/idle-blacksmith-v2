# Chronicles Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task by task. Steps use checkbox syntax for tracking.

**Goal:** Deliver the eight approved gameplay, world, visual and production improvements as a coherent playable expansion.

**Architecture:** Preserve production/save authority and stable IDs, add pure-C# artisan and saga modules, then integrate their state into focused runtime pages. Art and audio are presentation assets; gameplay never depends on an animation completing.

**Tech Stack:** Unity 6000.3.20f1, C# 7.3 pure simulation, uGUI, Node content checks, generated hand-drawn PNG assets.

**Spec:** docs/superpowers/specs/2026-10-25-chronicles-design.md

## Global constraints

- Six regions, six heroes, 108 recipes, 48 enemy types, twelve bosses; three party slots.
- Stable original IDs, no normal-save reset, no external dependency for simulation.
- Windows and Android portrait; text at least 42 reference pixels and 100/125/150 percent size settings.
- Keep assets frozen during Unity builds, isolate native test saves, retain reduced motion.
- Run Android probes after Unity has fully exited: the editor shuts down ADB on exit and can interrupt a concurrent install.
- User has already approved all recommendations and implementation. No additional implementation approval gate.

## Review focus

- Partially populated or corrupt old saves must normalize without changing original item identity.
- Reloading at every craft/room boundary cannot repay costs or duplicate equipment/loot.
- Offline production cannot complete a masterwork that is waiting for a player choice.
- Fast repeated clicks and route changes cannot mutate a party snapshot or bypass an encounter.
- Locked regions, endgame states, large text and small screens always show a clear reachable next action.

## Tasks

- [x] 1. Add shared contracts and content migration. Add expanded-count/migration tests, verify failure, implement definitions and bounds. Validate all IDs and references, preserve the existing regression suite.
- [x] 2. Implement paid multi-stage crafting, real alloy/temper/rune effects and item descriptions. Test stage rejection, immutable recipe, reload/offline wait and once-only delivery. Integrate production hooks and a close-up forge page.
- [x] 3. Implement three-hero saga, branches, 48 enemies and twelve three-phase boss encounters. Test snapshots, each mechanic, loss/retry, room reload and once-only results. Integrate the adventure/party flow.
- [x] 4. Implement restoration, NPC choices, legendary goals, mastery modifiers and optional legacy without deleting progress. Test prerequisites, costs, one-time benefits, challenge progression and preserved collection. Show actual restored world states.
- [x] 5. Generate coherent equipment/enemy/hero/world assets, validate crop/alpha/source provenance, attach loadout weapons/armor and animate grounded character actions. Inspect all new actors and regions in native rendering.
- [x] 6. Add regional music, ambience and material impacts with settings/focus behavior. Verify assets and runtime selection without claiming studio recordings.
- [x] 7. Add content authoring/validation and human playtest kit with local session evidence. Validate malformed catalogs and document useful authoring workflow.
- [x] 8. Run pure core/art checks, Windows native new-flow and legacy probes, Android build and emulator probes. Repair all material failures, perform independent review, update player guide/version/evidence, push branch and update attached draft PR. Install on the explicitly authorized phone if connected.
