```text
/make-plan Redesign Ember & Oak adventure and equipment presentation. Current design failed audit at19/30 with critical gaps in aesthetics, understandability and truthful consequence wording.

Verdict paragraph:
REDESIGN the adventure and equipment presentation (19/30), preserving the approved workshop, readable UI system, saves and working progression.

Why redesign: the scoped total is below20; painted heroes currently fight pixel enemies over pixel landscapes and the map hides future destinations.

Preserve: the unboxed hand-drawn workshop and three primary nav destinations (Runtime/WorkshopView.cs:108-244, RedesignSmoke.cs:43-52); readable42/48/56/72 type and palette (ForgeUI.cs:9-18); five chapters/35milestones and stable recipe/save IDs (Core/GameContent.cs:35-66, WorkshopSimulation.Progression.cs). Retest current workflow and text-size probes.

Discard: horizontal pixel strips as live campaign/equipment rendering (WorkshopView.Campaign.cs:10-24); anonymous locked-map nodes (Campaign.cs:218-225); boss as a merely enlarged ordinary sprite and rectangle threat fill (Battle.cs:29,117-124).

Top moves:
1. #3 Aesthetic: Replace live enemy, guardian, equipment, map and regional landscape art with one ink/gouache family. Evidence:E1.
2. #4 Understandable: Name every frontier and show current objective, claimed completion and truthful unlock requirements on the map. Evidence:E2.
3. #6 Honest: Correct guardian/repeat/final unlock promises, weapon-versus-armor effects, order acceptance and reserved-stock explanations. Evidence:E4.
4. #8 Thorough: Ground actor poses/shadows and contact motion, use distinct guardian art, preserve reduced motion and make focus visible. Evidence:E1/E5.
5. #9 Resource care: Cache atlas/landscape resources, validate every crop/frame and check final mobile memory/size before delivery. Evidence:E6.

Prioritize a legible map, desirable equipment and coherent combat. New flow: objective and destination on map → named region/preparation → selected route → painted combat/clear rewards → current journey. Keep free play. New art cannot break old saves. Validate generated alpha/cell bounds, all20stable item IDs, ordinary/guardian/wave changes, locked preview/departure, text100/125/150%, short screens, Reduced motion and actual Windows/Android players. Non-goals: new economy, new balance, new chapters, redesigning approved workshop. Cutover: all live consumers (including rack/flights) use the new assets together; legacy source art remains archival, not a user-visible theme switch.
```
