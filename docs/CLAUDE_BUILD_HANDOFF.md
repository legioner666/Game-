# Claude QA handoff — Crown Rush baseline builds and SFX candidate

**Date:** 2026-11-15  
**Status:** handoff for Claude's independent review. No merge into main by GPT.  
**Current candidate:** `gpt/sfx-pack`, forked from main commit `03928d16189194c4248b638349c7e01e701a87c2`. Candidate includes existing main gameplay/progression/music and a distinct, compatible SFX pack for [Issue #16](https://github.com/legioner666/Game-/issues/16).

## Two previous APKs supplied for comparison

| Label | Release | Artifact | Release target SHA |
|---|---|---|---|
| **A — Workshop prototype** | [build-gpt-fortress-progression](https://github.com/legioner666/Game-/releases/tag/build-gpt-fortress-progression) | [APK A](https://github.com/legioner666/Game-/releases/download/build-gpt-fortress-progression/crown-rush-stress.apk) | `518a65bda36c38059e149cef79ddbf2d607f7ebd` |
| **B — Original soundtrack** | [build-gpt-original-soundtrack](https://github.com/legioner666/Game-/releases/tag/build-gpt-original-soundtrack) | [APK B](https://github.com/legioner666/Game-/releases/download/build-gpt-original-soundtrack/crown-rush-stress.apk) | `cb7df12d4a22d1d0a00074dee0149ff8403653e0` |

Both are **debug-signed historical artifacts**, not production builds. A and B are separate snapshots: one does **not** contain all of the other's changes.

**Git history nuance:** PR #12 was closed without GitHub reporting a merge, but its workshop implementation appears in current `main` (e.g. `godot/progression.gd`). PR #14 was merged and its soundtrack is in main. Do not overwrite current main files with old APK branch files to 'restore' these features.

## New QA candidate C

- Branch: `gpt/sfx-pack`
- APK release when branch CI finishes: [build-gpt-sfx-pack](https://github.com/legioner666/Game-/releases/tag/build-gpt-sfx-pack)
- APK: [Crown Rush SFX QA candidate](https://github.com/legioner666/Game-/releases/download/build-gpt-sfx-pack/crown-rush-stress.apk)
- Source change: **only** `godot/sfx.gd` plus this handoff note; do not touch game.gd, models.gd, workflows, keys or export presets.

### Sounds active now (existing gameplay calls)

`coin`, `tick`, `build`, `shoot`, `hero_shoot`, `boom`, `kill`, `hit`, `spawn`, `win`, `lose`, `click`.

Existing names still work; they now have more varied original synth texture/harmonics. The original procedural menu/combat music in `soundscape.gd` is unchanged.

### Newly available optional cues (NOT wired into gameplay yet)

`crossbow`, `cannon`, `arrow_hit`, `enemy_death`, `upgrade`, `freeze`, `meteor`, `ui_confirm`, `ui_cancel`, `castle_hit`.

**Claude owns the integration into `game.gd` and models.** Please wire cues to their respective event sites only after reviewing UX and SFX frequency. If the event is not used yet, do not claim it is audible in the candidate APK. New tones are generated once, spread across frames rather than all during the startup frame. Measure actual startup time and first spell hitch on Android; these are implementation goals, **not confirmed timings**.

## Required tests for Claude

1. **Integrity:** confirm release tag `build-gpt-sfx-pack` points to the candidate's latest head SHA, APK exists and installs over the existing debug-signed app.
2. **Import and smoke:** Godot 4.x import/parse; launch menu, switch to gameplay, finish at least one run; no runtime errors. Run bot on all 3 levels and compare with pinned baseline using identical settings/seed where possible.
3. **Sound quality:** A/B/C at the same Android device volume. Verify coin, deposit, build, shot, cannon and victory/defeat have a distinct sound. Verify that switching Sound OFF immediately silences music and effects and ON restores them. Check first gameplay frame, screen pause, minimized app and reopen.
4. **Performance:** measure the time before menu appears; **target ≤0.3 s *added* audio generation cost**, not a verified metric. Check 1% low FPS and longest frame on late waves and the first cast of Freeze/Meteor. Procedural clips must not create lag spikes.
5. **CPU/Audio:** no pop/click at loop boundaries, excessive sounds overlapping, audio volume clipping, unintended long tails or duplicated sounds.
6. **Source and security:** no external sound assets, network, workflow, keystore or export changes. Keep signing debug only; do not expose secrets.

## Other work is independent

- [Issue #15](https://github.com/legioner666/Game-/issues/15): nine levels — separate `levels.gd` branch, not included in candidate C.
- [Issue #17](https://github.com/legioner666/Game-/issues/17): hero XP/talents — separate implementation, not included in candidate C.
- [Issue #18](https://github.com/legioner666/Game-/issues/18): new L1–L3 tower models — new `models_towers.gd`, Claude to integrate, not included in candidate C.
- [PR #11](https://github.com/legioner666/Game-/pull/11): Meteor aim/VFX is a separate branch and must not be assumed included in C unless merged into base.
- [Issue #13](https://github.com/legioner666/Game-/issues/13): overall roadmap.

**Handoff request:** Claude to compare A/B/C, review `sfx.gd`, listen on device, optimize/rework if needed in a `claude/...` branch and leave comments on the corresponding PR. The owner approves the final merge.
