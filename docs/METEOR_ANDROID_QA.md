# Meteor — Android acceptance checklist (PR #11)

**Purpose:** verify the owner's reported problem is fixed on a physical phone. This is a test plan, **not proof of testing**.

## Setup
Install the APK from the `gpt/meteor-aim-vfx` branch. Confirm its build commit is the latest branch SHA. Existing player progress should be preserved; avoid deleting application data. Play level 2 or 3 (Meteor is not unlocked at level 1).

## Manual playtest
1. In an active wave, tap **Meteor**. A glowing ring indicating the **3.8-unit AoE radius** and the UA/EN aim hint should appear. No enemy HP should change yet.
2. Touch the battlefield at a chosen position, optionally drag a finger to reposition the ring, and release. A fireball must visibly fall from above for roughly **0.6 seconds**, followed by a bright impact and expanding shockwave.
3. At **impact**, not earlier, enemies inside the ring should lose **150 HP** (subject to game effects). Enemies outside must not lose HP from the spell.
4. Meteor enters **25 s cooldown** after confirming the chosen location. It should not be possible to cast again until ready.
5. Tap Meteor twice without selecting the battlefield: aiming must cancel without damage or cooldown.
6. Start aiming, then pause or end a wave: targeting should cancel cleanly and joystick control should return.
7. Verify that Freeze still slows enemies for 5 s and has 30 s cooldown.
8. Confirm UI text in **UA** and **EN**; buttons do not overlap the Android navigation area; if aiming feels obstructed, take a screenshot.
9. Test against a moving group, near the map edge and when no enemies are visible; an empty hit can legitimately report `affected=0`.
10. Copy game session report. For a completed Meteor impact the log should contain `spell_cast type=meteor affected=N`. For Freeze it should contain `spell_cast type=freeze affected=N`.
11. Compare FPS and frame-time hitches in wave 5 with the prior release using the same phone and similar conditions. Send real observations, not simulated bot FPS.

## Automation / static checks for Claude
- Godot editor/headless import and gameplay parse on the branch, including `meteor_fx.gd`.
- `--autoplay --fast=4 --level=0/1/2` should be unaffected (bot never uses spells).
- Screen-ray intersections must use the actual game camera with y=0 ground; reject touches outside the map.
- Event handling must not steal touches on the spell buttons and must leave joystick usable after casting/cancel.
- Check for multiple simultaneous meteors, abrupt wave finish, pause during falling meteor, and proper cleanup of short-lived meshes.
- Draw calls stay low; no imported particles, textures, or shaders required.

## Known limits
- The reticle is an early functional design, not final UI polish.
- No physical Android input/performance test has been run by GPT.
- GitHub Actions success confirms APK build, not the on-screen UX.
