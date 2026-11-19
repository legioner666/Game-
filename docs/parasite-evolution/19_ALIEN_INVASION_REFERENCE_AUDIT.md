# Alien Invasion RPG Idle Space — external reference audit (2026-11-19)

## Scope and verification
- Official Android listing: https://play.google.com/store/apps/details?id=com.multicastgames.venomSurvive — package `com.multicastgames.venomSurvive`, currently published by CrazyLabs LTD. The store listed a November 9, 2026 update, 10M+ installs, ads and in-app purchases when checked.
- Official iOS listing: https://apps.apple.com/us/app/alien-invasion-rpg-idle-space/id6443697602 — publisher Crazy Labs.
- Historical developer credited as Multicast Games in a publisher/UA case study and the open-source Morpeh README.
- GameRefinery/Liftoff 2024 report: https://mma.prnewswire.com/media/2396345/2024_Casual_Gaming_Apps_Report___ENG.pdf — character moves over an open field, collects, returns resources to silo, powers up; competitive events used for retention (pages 25–26 by printed page number).
- Reddit community feedback: https://www.reddit.com/r/AlienInvasionRPG/comments/1mlp205/ and https://www.reddit.com/r/AlienInvasionRPG/comments/1x5e8i5/ ; comments on repetitious progression and ambiguous idle behavior. Community sentiment is anecdotal, not telemetry.
- Official source code **not located** in checked public searches; absence of an indexed repository is not definitive proof of absence.
- Unrelated example: https://github.com/cykod/AlienInvasion — MIT/GPL sample HTML5 shooter; **not** the CrazyLabs product.
- Morpeh open-source Unity ECS library: https://github.com/scellecs/morpeh — README lists this game as a user and states MIT license. This does NOT make Alien Invasion's game code MIT/open-source or verify its exact internal architecture.
- No publisher APK/IPA/Unity project was downloaded or executed here. **No dynamic binary review, decompilation, permission audit, network trace or device benchmark** is claimed.

## Feature reference and distinct implementation

| Observed design pattern | Our original design | Code status |
|---|---|---|
| Auto-targeting and several catching arms | Slime senses eligible samples and extends organic absorption tendrils | Existing capture + new capacity-aware selection |
| Capacity and return to silo/base | Nutrient cargo and safe-zone digestion | Already implemented |
| Cargo delivery assistants | Unlockable **organic relay**: symbiotic transport network sends already collected resources at timed intervals, not an offscreen farmer | This PR: economy+gameplay+shop |
| Catch radius / efficiency improvements | Genomic upgrades and active tentacle movement skills | Prototype implemented |
| Region progression / bosses | Three contiguous connected lab sectors and mechanized Warden | Prototype implemented |
| Time-gated or paid progression | Short optional upgrades, no ads/real money/paid currency, readable HUD | Design policy, balance to validate |

## Explicit originality boundaries
- Reuse general mechanics and progression lessons, **not** copyrighted art, rigging, code, audio, exact maps, logo, promotional screenshots, distinctive copied UI, character silhouette or store listing.
- Do not import a proprietary APK, decompile it to harvest game files or introduce unlicensed assets.
- A library with a verified permissive license (e.g. Morpeh MIT) may be evaluated, but the project currently has ~22 enemies: introducing a new ECS dependency is premature without profiling. If adopted later, preserve notices and verify dependencies.
- Existing alien spider imagery is a useful *negative* reference: our creature is **a low asymmetrical amorphous slime** with retractile, unevenly sized spider-positioned support limbs, no fixed spokes, no humanoid torso.
- Do not use `Alien Invasion` as the shipped game name, clone store icon or claim sponsorship.

## Original implementation: Organic Relay
- Unlocks at organism level 2 in safe-zone shop.
- Provisional, reviewable in-game prices: level 1 60 genome, level 2 150 genome; cycles 30s / 18s while the character is active outside the base.
- Transfers **already captured** cargo into the bank, never creates resources, never consumes paid currency; the base-return quest counter is separate.
- Transfer preserves resource balance, respects max bank, commits atomically via existing save transaction, and has an independent counter and cooldown field.
- New save fields default to zero for pre-existing v1 JSON saves; no schema migration to v2 and no overwrite of unsupported future schema.
- Cooldown progresses only during active gameplay, not while paused or app closed. This is intentionally **not true background idle farming**.

## Testing status / remaining risks
- Standalone .NET 8 tests exercise actual economy source; CI also checks Unity source structure, GUIDs and setup script.
- No claim of Unity compilation, package import, Editor visual tests, Android build, frame pacing or input correctness until a supported Unity Editor is available.
- Critical follow-up: import in Unity 6000.3.11f1, run EditMode tests, test old v1 saves after JsonUtility deserialization, test UI fit (720p landscape and phone safe areas), test cooldown persistence, FPS/battery and base deposits.
- Run original playtest before adding further systems. Avoid speculative architectural rewrite.
