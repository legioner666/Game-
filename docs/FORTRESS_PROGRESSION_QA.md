# Crown Rush — Fortress Workshop acceptance test (PR: gpt/fortress-progression)

**Date:** 2026-11-15. Applies to the existing 3-level game, with no new art or audio.  
**New system:** earned stars are *not consumed*. They remain the best-score progress and unlocking condition; they also supply an investment budget for permanent mastery.

## Expected behavior

1. New save: `Settings.stars = [0,0,0]`, all upgrades rank 0. Main menu has **Майстерня фортеці / Fortress Workshop** and the workshop shows **0 of 0** free stars. Upgrades disabled. Existing levels and sound settings load normally.
2. First win at level 1 (e.g. 2 stars): workshop displays **2 of 2** free stars. Crossbow rank 1 costs 1 star, rank 2 costs 2 more. Player can buy rank 1; remaining 1 star. The level's 2 recorded stars must still display in selection and level 2 must remain unlocked.
3. Upgrade costs: rank 0→1 costs 1; 1→2 costs 2; 2→3 costs 3; max rank 3. Investment never exceeds total best-earned stars. Rank 3 button disabled and shows max.
4. Each purchased rank changes future **human** runs only: Crossbow = +6% damage per rank, Cannon = +5% splash radius per rank, Barracks = +8% allied max HP per rank. Existing in-run L1–L3 building prices and level upgrades are unchanged.
5. Quit and reopen app: bought ranks, best stars, language, sound and vibration are preserved in the existing `user://settings.cfg`. No account/network.
6. Workshop **«Повернути зірки» / Reset upgrades** refunds all allocations at no cost; earned stars and unlocked levels remain unchanged.
7. For `--autoplay --fast=4 --level=0/1/2`, gameplay must still use baseline damage/radius/HP even if the saved player has tech upgrades. This avoids invalidating bot baseline comparisons.
8. Check UA/EN: text fits within portrait 1080×2400, back navigation and Android system back work, touch target sizes readable.

## Static checks for Claude
- Godot 4.4.1 headless import + full APK export. Check `progression.gd` syntax, lambda capture, `ConfigFile` persistence.
- Run bot 3 levels on the branch; compare with base at same random seed where possible; do not assume bot win percentage based on a single randomized run.
- Generate screenshots of the workshop with no stars, 1+ spent, max rank (can use isolated QA setting or temporary local test data, never commit user save files).
- Verify the new menu page does not block level selection, pause or settings.
- Record frame time if the new scene is held open; no additional draw calls in actual gameplay beyond trivial scalar multipliers.

## Non-goals
- Additional maps, hero experience, a paid currency, consumable stars, new building models, original music and full visual redesign belong to separate PRs.
- Do not merge until Claude has reviewed CI and the owner has inspected the Android APK.
