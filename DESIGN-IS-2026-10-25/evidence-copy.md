# Copy and honesty evidence — current 0.4.0

## Sources consulted

- `Runtime/WorkshopView.Campaign.cs:8–258`: sprites, quests, recipes, heroes, world and routes.
- `Runtime/WorkshopView.Battle.cs:19–140`: skills, threat telegraph, loot, recovery and result actions.
- `Runtime/WorkshopView.Progression.cs:16–298`: guided actions, craft/reward results, chapter journal, orders, mastery and departure.
- `Runtime/WorkshopView.Modals.cs:20–201`: work descriptions, crew, storage, settings and offline results.
- `Core/GameContent.cs:34–118`: 20 recipes, five regions, three heroes and 30 quest definitions.
- `Core/WorkshopSimulation.Progression.cs:7–196`, `Core/WorkshopSimulation.Guidance.cs:27–143`, `Core/WorkshopSimulation.Campaign.cs:7–26,117–165,212–244,265–355,365–372`, `Core/WorkshopSimulation.Contracts.cs:7–83`: actual unlocks, effects, reservations, rewards and promises.
- Main workshop/navigation, app notifications and town helper source are also included in the catalog.

All paths above are relative to `Game/Assets/ForgeEmpire/`. These are source-observed facts; no new live player session was driven for this subaudit.

## Complete literal inventory

`runtime-copy-string-catalog.tsv` records all 1,399 C# string literals in 14 scoped runtime/content/simulation files, with exact repository path, 1-based line, literal and source line. It deliberately includes internal UI IDs, resource names and number formats alongside visible copy, so visible fragments cannot be accidentally omitted. Dynamically composed messages are represented by their literal fragments and the source expression; actual names, counts, rewards and selected hero are substituted at runtime. Comments and character literals are excluded.

## Concrete findings

### C1 — The map does not name locked destinations or the immediate route objective

`Runtime/WorkshopView.Campaign.cs:209` titles the page “Beyond the forge” under “Five frontiers await”. `:216–225` creates five region positions, but `:220` renders only a lock glyph and continues for locked regions; only unlocked regions receive name labels at `:223–225`. The map's party line at `:211–212` contains hero/power/guard rather than the active chapter objective. The chapter journal separately exposes `NextStep.Title`, `NextStep.Detail` and `ChapterUnlock` at `Runtime/WorkshopView.Progression.cs:178–181`. Therefore the map itself cannot identify the next locked place or its required story claim. This is a discoverability gap, not a deceptive claim.

### C2 — The departure unlock promise is unconditional, including the final guardian and repeat routes

`Runtime/WorkshopView.Progression.cs:284` uses “Break the guardian's hold and open the next chapter.” for every `route==2`. `Core/WorkshopSimulation.Campaign.cs:155–162` pays expedition loot and records boss kills; the actual chapter change occurs when the main boss quest is claimed at `:228–233`, and the chapter caps at region four. `Core/WorkshopSimulation.Progression.cs:9–11` and `Runtime/WorkshopView.Progression.cs:192` likewise say “Defeat … to … unlock” without the claim step. Suggested exact distinction: current unclaimed guardian → “Defeat the guardian, then claim the story reward to open [next region].”; final guardian → “Free the valley, then claim the final story reward.”; completed guardian → “Return for materials, gold and hero XP.”

### C3 — Weapon damage and armor protection are grouped as “matching equipment”

`Runtime/WorkshopView.Progression.cs:286` says “Matching equipment gives a damage bonus.” when the selected weapon does not match the weakness. Actual effects are separate at `Core/WorkshopSimulation.Campaign.cs:274–279`: weapon multiplier 1.3 (Kael 1.6), armor multiplier 0.8 (Kael 0.7); armor does not add that damage bonus. The guide should name the weapon effect and armor protection separately. Existing hero description documents Kael's exceptions correctly at `Core/GameContent.cs:73`.

### C4 — “If you retreat” implies a control that the battle page does not offer

`Runtime/WorkshopView.Progression.cs:294` says gear is safe “if you retreat”. `Runtime/WorkshopView.Battle.cs:45–56,63–76` offers Strike/Brace/Recover and a result claim button, but no retreat action. `Core/WorkshopSimulation.Campaign.cs:349–351` automatically finishes as a loss on zero health or time limit. `Runtime/WorkshopView.Battle.cs:81` calls that outcome “Retreat”. “If you lose” or “after a setback” describes the available behavior more directly.

### C5 — Later milestones still call every chapter's patron a warden

`Core/WorkshopSimulation.Progression.cs:38` returns “Earn the warden's backing” for step one in every chapter. The actual patrons are Warden Elowen, Ferryman Oren, Captain Sera, Furnacekeeper Bram and Archivist Nessa (`Core/GameContent.cs:78,81,84,87,90`). Using the current patron's name or the actual story title would make chapter progression and new world art tell the same story.

### C6 — Journey-reserved equipment has no matching storage explanation

`Runtime/WorkshopView.Modals.cs:119` appends “Reserved for order” for guild-reserved stock only. The sell button at `:143` is disabled for both `IsCommissionReserved` and `IsJourneyReserved`, while `:131` shows Keep/Kept from the item's separate `Protected` flag. Core journey reservation is a separate state test at `Core/WorkshopSimulation.Contracts.cs:53–56`. An unprotected item matching the guided project can therefore have a disabled Sell control without the reason appearing in its metadata. Suggested visible status: “For your current journey” when `IsJourneyReserved` is true and no order reservation applies.

### C7 — Accept-order gold label is ambiguous beside other paid actions

`Runtime/WorkshopView.Progression.cs:228` labels accepting an offer “Accept · [gold] gold”. `Core/WorkshopSimulation.Contracts.cs:41–46` accepts without charging gold; gold is paid later at `:75–78`. The offer already lists `+gold · +reputation` in `Runtime/WorkshopView.Progression.cs:244`. “Accept order” avoids resembling hire/upgrade purchase labels (`Core/WorkshopSimulation.Progression.cs:193–195`). This is ambiguity, not hidden cost: the visible terms and actual reward are present.

### C8 — Suggested preparation is authored but removed from the region screen

`Core/GameContent.cs:62–66` includes suggested supply power/guard values and elemental resistances in the region description. `Runtime/WorkshopView.Campaign.cs:236` displays only `region.Description.Split('.')[0]`, removing those suggestions. The page does show the selected hero's raw stats and weapon multiplier at `:240–242`, but offers no explicit readiness comparison. A concise preparation/weakness summary near the new region artwork would expose existing useful information without restoring a long paragraph.

## Claims checked against behavior

- Strike “1.8× damage”, Brace “70% less damage”, Recover “40% heal · Once” (`Runtime/WorkshopView.Battle.cs:45–46`) match `Core/WorkshopSimulation.Campaign.cs:117–138` and the guard multiplier at `:346`.
- Route reward gold/material/XP/reputation (`Runtime/WorkshopView.Campaign.cs:249–250`) comes from `PreviewRouteRewards` (`Core/WorkshopSimulation.Guidance.cs:64–68`), using the same `SetRewards` function as collection (`Core/WorkshopSimulation.Campaign.cs:155–158,301–306`). The finished battle reputation expression (`Runtime/WorkshopView.Battle.cs:92`) is algebraically the same value.
- Quest rewards/consequences at `Runtime/WorkshopView.Campaign.cs:80–82` and `Runtime/WorkshopView.Progression.cs:99–104` match actual gold, reputation, material and town mutations (`Core/WorkshopSimulation.Campaign.cs:228–233`). No repeat payment occurs after the claim flag.
- “Orders never expire” and “Matching equipment is reserved automatically” (`Runtime/WorkshopView.Progression.cs:206,217`) match persistent accepted contract terms without expiry and `IsCommissionReserved` (`Core/WorkshopSimulation.Contracts.cs:48–52`). Delivery selects the lowest qualifying quality from storage and excludes equipped gear (`:66–78`).
- “Your hero and equipment are safe” on defeat (`Runtime/WorkshopView.Battle.cs:68,93`) matches the loss result's retry metadata and preserved hero equipment (`Core/WorkshopSimulation.Campaign.cs:149–152`).

## Inflation / dark-pattern checks

- No unsupported marketing superlative found in the scoped product copy. “Masterwork”, guild ranks and mastery counts refer to explicit game states (`Core/WorkshopSimulation.Guidance.cs:28–31`, `Core/WorkshopSimulation.Contracts.cs:7–16`, `Runtime/WorkshopView.Progression.cs:255–265`).
- No timer expiry, fake scarcity, forced purchase, hidden monetary price or confirmshaming found in these sources. Guild offers have explicit quantities/quality/reward and a free release action (`Runtime/WorkshopView.Progression.cs:202–244`; `Core/WorkshopSimulation.Contracts.cs:78–82`).
- “Ward”, “Guard”, “Brace”, “Superior (2/3)” are genre terms with contextual explanations; the most consequential wording gap is C3 rather than the use of those terms alone.

## Known gaps

- No first-time-player comprehension study; clarity findings are source-inferred.
- No localization audit. Product text is English while the project guide and user conversation are Indonesian.
- No new physical-device gameplay run. This audit does not replace native layout or interaction verification after the art overhaul.
- The catalog is a complete literal superset for the stated 14 files, not a claim to enumerate every possible runtime-composed full sentence or every file outside this scope.
