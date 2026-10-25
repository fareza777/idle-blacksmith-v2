# Authoring the Chronicles catalog

Edit `content/chronicles.json`. Each enemy has a unique authored silhouette/name, region, one of eight behavior archetypes, health, armor and attack cadence. Each guardian references one of twelve implemented mechanics and must include a readable telegraph plus a useful counter. Equipment defines its slot, weapon family, affinity, costs and real stats.

Run `node tools/validate-chronicle-content.mjs --write` to validate and compile the JSON into the pure-C# catalog. Run without `--write` to check that the runtime matches the source. The validator rejects ID drift, missing rows, duplicate names, invalid regions, unknown effects and invalid timing/costs. Original recipes 0–19 and quests 0–29 remain in GameContent for save compatibility. Never renumber released IDs.

Add art using the built-in image-generation workflow documented in ArtSource/chronicles-generation-final.json and ArtSource/faceless-direction.json. Human faces must stay plain without eyes, nose or mouth. Copy the selected original PNG unchanged. `tools/analyze-chronicle-art.py` only analyzes alpha and writes UV/feet metadata; it never edits image pixels. Atlas order is four columns: ready, attack, hurt, defeated for creatures. A hero has twenty poses documented in the generation manifest. Inspect each atlas and native render before accepting a catalog change.

Validation: `node --test tests/chronicle-content.test.mjs`; `node tools/test-core.mjs authoring`; then the native Chronicles probe. A content row passing validation does not establish encounter balance: play the route, use its counter, and test both the expected build and a weaker build.
