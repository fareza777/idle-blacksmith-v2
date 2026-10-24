# A workshop with a purpose

The player asks for actual gameplay, a readable workflow and lasting progression. The existing simulation already has crafting, combat, thirty stories and five regions, but those systems are disconnected in the interface. Continue the approved hand-drawn workshop and remove the need to guess where to go next.

## Direction

Author seven milestones per chapter: arm a hero, claim the crafting story, scout and claim the supply story, equip armor, rescue townsfolk, develop workshop automation, defeat the guardian and claim the chapter unlock. Five chapters expose 35 milestones. Earlier chapters stay complete after unlocking the next. Equipment milestones are remembered after successful equipping so swapping heroes cannot erase progress. Existing saves infer earned milestones without losing money, inventory, quests or equipment.

The chapter objective supplies the actual next action, target recipe, needed materials, intended route and reward. A guided dock follows ore → ingots → forge → equip → expedition without changing pages to perform production. Tapping a world station enters free play; tapping Continue journey resumes guidance. Opening the journal shows the current objective, milestone progress, the next unlock and all five chapters. No dashboard panels cover the workshop.

Manual forging reveals the actual item, quality and stat gains. Equip or reserve/deliver it directly from the result. Automated forging never interrupts play. A completed story or order shows a clear result and a Continue journey action. Battle results retain their real rewards and direct the player toward the next objective.

## Repeatable work

A guild order board offers three deterministic contracts from unlocked recipes: weapons, armor and a quality challenge. Accept at most one, with an explicit recipe, quantity, minimum quality, gold and reputation. No timers or expiry. A contract survives reload and keeps its original terms when a chapter unlocks. The merchant cannot sell matching reserved stock. Delivery consumes only the required unequipped items, lowest qualifying quality first, pays once, then rotates offers. Canceling releases reservations without changing inventory. Contracts can fund required hires and remain available after the story ends. Reputation has visible rank titles; a mastery page exposes recipe, town, hero and story collection goals.

## Constraints and decisions

- Unity 6000.3.20f1, existing C# 7.3-compatible core, native uGUI, Windows and Android.
- Preserve stable recipe/quest IDs, battle/economy rules and legacy save compatibility.
- Existing English game copy remains consistent. Communicate with the user in Indonesian.
- Body text >=42 design units, supports 100/125/150% text and short portrait screens.
- Keep the hand-drawn art, unboxed workshop and reduced-motion preference.
- Builder choice: chapter workshop targets are miner, smelter, blacksmith, merchant, then all four stations at level 5. Costs remain the existing costs.
- No Git checkout exists. Use QA source backups and manifests instead of commits/worktrees.
- User has already explicitly requested execution; continue without another design approval round.

## Proof

Exercise fresh play from zero resources through the first chapter without changing screens to hunt for a task, all five chapter objectives, defeat/retry, contract reservation/delivery/cancel/reload, old saves, automation while pages are open and long-term goals after the final boss. Verify actual rendered UI and rebuild both platforms.
