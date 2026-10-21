# Burntwax combat handoff — 2026-10-18

## Read this first

This is a durable summary of a long development conversation, not a claim that every historical feature was re-tested today. Current source is authoritative. Read the relevant scripts before editing; inspect dirty changes and preserve them. Paths below are relative to the repository, normally under `Assets/Burntwax Collective/Scripts`.

User wants small, explained steps, not a sweeping combat rewrite. Announce changes before making them. Always list what changed and show relevant code afterward. Recently the user switched to guided learning: give one manageable edit, explain why, then wait for their compile/test result. Do not silently take over editing; they explicitly authorize it when wanted (most recently the attack-buffer change). No commits/pushes without request. The user edits animations/scenes themselves.

## Current weapon identity and design

- Player-facing name: **Wakizashi**. Code prefix: **DualBlade**. Previously Katana.
- Q switches between guns and the paired blades. One weapon/state machine controls both blades.
- Glove use stows the left blade, never drops it. Right blade can still attack. Left returns after the glove finishes lowering.
- User moved blade models under their physical hands to animate them individually. Keep model roots/detector points correctly assigned; do not undo that hierarchy.
- **Final damage decision: one hit per target per attack across BOTH blades.** Each blade sweeps independently but shares the owner's target set. A proposal for independent per-blade hits was explicitly rejected before implementation.
- Light and Heavy have separate configured damage. Heavy can be tuned to twice Light; do not hard-code doubling or add damage again for the second blade.
- Existing damage is attack-strength-based, NOT blade-speed-based.
- Current inputs: release a click without qualifying movement => Light; qualifying directional flick while held => immediate Heavy. No slow-pullback/reversal requirement anymore.
- Directionless Light is valid. `DualBladeAttackIntent` has `wasCreated` set internally by its constructor so `default` stays invalid.
- Old HeavyCharge/reversal code, fields and comments remain in places. They have not been comprehensively cleaned up. Audit before removing, especially recovery branches.

## Main script map

`Weapons/DualBlade/` contains:

- `DualBladeGestureRecognizer`: time-window mouse velocity, one emitted intent per press, Light on release, immediate directional Heavy. Keep its frame-time-aware VelocityWindow.
- `DualBladeAttackIntent`: strength and direction data, no animation playback.
- `State Machine/DualBladeStateMachine`: owner/config selection, attack sequence, shared target set, animation requests/events, resources, parry recoil and saves.
- `DualBladeSlashStateBase`: common Light/Heavy execution, queued input, chaining and cleanup.
- `DualBladeReadyState`: reads intents and routes to concrete LightSlash/HeavySlash states.
- `DualBladeDetector`: points along curved blade, overlap capsules and sweeps; ignores player and glove-controlled object. Shared dedup through owner, then `HitResolver`. Forwards enemy parry reactions to owner even when located under a hand socket.
- `DualBladeParryDefense`: player defense/block/perfect-parry behavior.
- `Configs/DualBladeScriptableObject`: pair-level data, not model spawning. `UseLeftBlade`, shared damage/impact/parry/gesture/feedback configs, primary AnimationConfig and optional RightBladeOnlyAnimationConfig.
- `Configs/DualBladeAnimationConfig`: directional Heavy entries and newly added Light entry list; each entry reuses SlashState with Animator state name, clip, blend duration, chain blend duration and chain start.
- `Configs/DualBladeImpactConfig`: directional impulse strength, separate Light/Heavy away and directional components.
- `Weapons/Arsenal.cs`: active weapon selection, suppression and unified weapon save data.

## Animation work: present state and pitfalls

- Use individual Animator states for Light variants, NOT a randomizing blend tree. No transition arrows needed; code crossfades directly.
- User created `combo_1_DualBlade`, `combo_2_DualBlade`, `combo_3_DualBlade` in Arms.controller. Assets were moved by user into `Animations/Wakizashi/slash/light/`, Heavy into `slash/heavy/`. Config assets are in `Assets/Burntwax Collective/Wakizashi/` (not the old DualBlade asset folder).
- `PlayDualBladeAnimation` now chooses a random Light entry when attack strength is Light, otherwise uses directional lookup. Random selection can repeat the previous animation; no repeat avoidance implemented.
- Selection uses right-only animation profile when appropriate. That profile needs its own Light entries if assigned.
- Light entries intentionally have NO direction field. Proposed next work: Light impulse follows actual movement of the blade registering the first accepted hit, with a sensible fallback on first damage-window frame. **This impulse change has been discussed, not implemented.** Heavy retains gesture-selected direction.
- `SlashState.GetChainStartTime` clamps skip time before first OpenDamageWindow event using the assigned clip. Animator state name and clip assignment must match.
- Events: OpenDamageWindow, CloseDamageWindow, SlashChain, EndSlash. SlashChain allows early continuation; EndSlash finishes the action.
- Empty combo_2/3 clips once caused apparently stuck Ready poses: input actually entered LightSlash, but empty clips had no motion or EndSlash. Restricting the list to combo_1 fixed it. Later inspection showed events on combo_2/3; do not assume they remain empty now.
- At last timing inspection: combo_1 SlashChain ~0.333s, combo_2/3 ~0.183s, Heavy ~0.267s. All can be edited by user; inspect actual clips.
- The selector can return false for missing configuration/state, while EnterSlash still sets isSlashing=true. Robust playback-failure recovery remains a potential improvement, not completed.
- Event acceptance checks action type, Animator state hash and normalized time to reject outgoing crossfade events. Avoid simplifying without testing same-state chaining.
- User saw right-hand twisting during animation authoring. Saved combo_1 used Euler curves with right hand near X=88 degrees and substantial changes on all axes. Suggested testing Quaternion interpolation on a duplicate; did NOT alter rig/clip. `ref_hand_L/R` are IK targets in the saved Arms prefab. If target is smooth but hand isn't, inspect IK separately.

## Most recent implemented change: short input buffer

User found buffering too forgiving. Implemented in DualBladeGestureConfig and DualBladeSlashStateBase:

- `attackBufferDuration` defaults to **0.12 seconds**, Inspector-tunable and clamped >=0.
- Deadline is `Time.unscaledTime + duration` (real time, unaffected by slow motion).
- UpdateSlash expires pending input before transition checks.
- Recognizer continues observing input even with a pending attack, so releases aren't missed.
- Further recognized inputs while buffer is occupied are discarded; they do not extend its deadline.
- SlashChain remains the gate. A completed attack can also transition to a still-valid buffered attack.
- Light buffer starts at release; Heavy buffer starts when its gesture qualifies.
- Compile passed with only pre-existing MainMenu.cs CS1998 warning; play-mode feel has not yet been confirmed by user after this edit.
- Possible future audit: old IsInitialSlashQualified -> HeavyCharge / held-input -> Ready recovery branches remain and should be reviewed alongside expired Heavy inputs, rather than assuming full input cleanup is finished.

## Other recent fixes

- Arsenal formerly required inactive blades' fullyEquipped flag even while holding a gun, blocking Q. Now CanSwitchActiveWeapon checks active weapon readiness and preserves attack/parry/recoil restrictions. Ownership validated before hiding gun; blades can equip without guns; Q leaves blades equipped if no gun exists.
- Stow entry/exit now use IsSprintOrDashActive, traversing actual movement states, rather than held Shift priority. Actual blade mode uses dash. Idle animation currently may reuse equip's settled pose at 0.275s; user can assign dedicated Idle.
- Glove losing held/attracted object goes directly to Unequipping, not delayed Idle. Checks destroyed/disabled objects and broken roots. GloveBaseState supports leaving a substate for a root state, exiting Equipped and stopping its timer. Normal launch/drop/baseball flows were left unchanged.
- Name migration updated code, input action DualBladeSelect, Animator names, serialized references and asset paths, preserving .meta GUIDs. FormerlySerializedAs and MovedFrom retain old names intentionally. SaveSystem translates legacy JSON keys katanaData/ownsKatana/katanaID. Arsenal enum numeric modes remain Guns=0, DualBlade=1, Glove=2.

## Universal damage / impact architecture

Core files in `Combat/Universal/`:

1. An attack source builds `DamageInfo`: damage/type, hit position/normal/direction, source, instigator and impulse.
2. `HitResolver.ResolveHit` consults parent `IHitDefense` first. Defense can modify or cancel the hit. A parry notifies source/instigator `IParryReactionReceiver`.
3. For an uncancelled hit: `SurfaceImpactSystem.PlayImpact` handles presentation, `ImpactResolver.TryApplyImpact` handles force, `DamageResolver.TryApplyDamage` handles health/object response.
4. `ImpactResolver` uses parent `IImpactReceiver` if provided; otherwise applies AddForceAtPosition with ForceMode.Impulse to attached non-kinematic Rigidbody.
5. Therefore a crate does NOT need IImpactReceiver to receive force. Its TakeDamage does not need to apply that same force again.

Goal: guns, blades, glove blasts, explosions and thrown props use shared hit infrastructure instead of weapon scripts knowing every target class. Target-specific breakage belongs to target components. Preserve separation of health, force, defense and surface feedback.

`Surface Impacts/SurfaceImpactSystem.cs` and surface assets handle material/damage-type presentation (sounds, decals, VFX). Slash-direction decals were requested as optional. Earlier minor collision sounds were changed to prioritize the struck surface. Full two-material response (wood crate sound plus glass shatter, for example) is still a future design concern; do not imply that overhaul is done.

## Physics props, breakage and explosions

`Combat/Velocity Based/`:

- RigidbodyImpactReporter: collision reporting / minor interaction presentation route.
- VelocityImpactDamageSource and VelocityImpactDamageConfig: major damage-capable collisions. Distinguish these from low-speed minor sound/VFX and from dedicated bullet/blast/blade hit sources so hits aren't duplicated.
- Current config has minimumImpactSpeed (default 6), fullDamageSpeed (25), damageBySpeed curve, damage at full speed, self-damage at full speed, armed lifetime and impact limit. Objects have material/hardness/type customization; inspect source for exact current settings.
- User initially wanted fixed object damage; final direction combined configurable object damage/hardness with speed scaling. Sharp/dull/no-damage must remain configurable; never flatten all props into one default.
- Fast thrown objects can damage themselves as well as struck targets, e.g. crate/barrel breaking on a wall.
- Minor collision effects should work on loose rigidbodies even against static surfaces, without necessarily causing damage.
- Player fall damage is separate (`Player/Movement/PlayerFallDamage.cs`, config); do not treat player landing as merely adding generic thrown-prop components.
- SharpObjectEmbedding specializes sharp objects and layer-specific embedding; preserve configurable forward/up directions and release before glove attraction. Procedural slicing/dismemberment is not implemented.

`Destructable Objects/Breakable.cs` owns common health/break lifecycle, IsBroken and Broken event. Crate and Explosive specialize it; user wanted ONE behavior component per specialized breakable, not a redundant Crate + Breakable stack. Explosive supports explosion behavior/radius visualization; historical work added an ignited countdown after near-death/other explosion, with normal damage still able to explode it sooner. Verify current configs before altering.

## Glove, parry and projectile rules to preserve

- Glove physical launch uses camera aiming; should launch where player aims, not auto-return to original enemy. Plasma and physical launches are distinct.
- Attraction retains its custom nonlinear motion; do NOT replace it with the general CurvedRigidbodyFlight implementation.
- Attraction speed responds to CURRENT distance; random angular speed is scaled by Rigidbody mass. Holding settles to configurable orientation/size-dependent pose.
- Held objects parent to glove, restore original hierarchy on release, and clean up safely on break/destruction. Consume pickups/equip weapons must remain working.
- Baseball and enemy-projectile perfect parry are separate mechanics. Baseball bats an attracting object; ordinary held objects are excluded except ~0.25s forgiveness just after entering Hold. Ordinary blade slashes ignore glove-controlled objects.
- Perfect parry returns enemy projectiles; configurable speed and lower/tunable return arc were requested and implemented historically. Enemy throws and perfect-parry returns may spin by mass; baseball should NOT get that random spin.
- CurvedRigidbodyFlight is general launched-object flight; gravity should restore when an arcing object collides. Enemy head aim targets support returns. Preserve distinct glove manual aim vs parry retaliation targeting.
- Guard is weaker than perfect parry and has limited uses. Each use recovers sequentially using recovery delay. Normal-mode perfect parry restores ONE guard. Future difficulty policy: easy restores all, normal one, hard none. Do not implement this policy expansion without request.
- Perfect parry/baseball share desired feedback; camera FOV kick is zoom OUT, also used for sprint/dash/mantle.

## Enemy / HUD / save context

Historical work established enemy health and hierarchical movement/action framework, melee execution integrated with states and loadout/configs, code crossfades, and animation-event damage windows. Attack windup is a timing phase within an animation, not necessarily a separate clip. Enemy parry recovery was fixed to use an independent recovery timer instead of depending on interrupted attack animation events.

Enemy throw behavior should respect melee-range boundary and death; configs/loadout matter. Alien evolution framework is a future extension for adapting based on player behavior; do not assume full adaptive combat exists. Friendly fire was not a priority. Humans and aliens both matter.

Player resource UI moved toward reusable widgets/sources for health, ammo, dash and guard, with future glove-energy HUD support. Save system received practical fixes for currently existing objects; future procedural destruction serialization is explicitly deferred. SMG is no longer in game; don't focus fixes on it.

## Proposed future mechanics — NOT implemented

- Authored Light combo sequences instead of random independent animations.
- Building combo progress toward a directional Heavy finisher, OR an empowered version of always-available Heavy. User has not settled that choice.
- Dash-click spear/thrust attack. Other brainstorms: aerial plunge, upward launcher, sideways wall slam, parry riposte, launch-prop -> attract -> baseball.
- One click should select one action; contextual action priority must prevent simultaneous normal attack and special attack.
- If progress is hit-based, count successful attacks, not every collider/enemy struck by one sweep.
- Light per-blade-motion impulse, animation no-repeat selection, obsolete gesture/charge cleanup, robust failed-animation recovery remain possible next tasks, not silently completed work.

## Verification and resuming

This handoff was saved on 2026-10-18. Current buffer code and universal resolver flow were inspected while writing it. Many earlier sections reflect conversation history; recheck exact current implementation before promising behavior.

Prior compilation method: Unity 6000.4.0f1's NetCoreRuntime/dotnet.exe plus DotNetSdkRoslyn/csc.dll, using Library/Bee/artifacts/1900b0aE.dag/Burntwax.Core.rsp with output/refout/pdb redirected to a unique temporary folder. Some cached response paths needed Katana -> DualBlade and DualBladeBladeDetector -> DualBladeDetector substitution in a TEMP copy. Don't modify Unity's live Library binaries. Discover installed/current paths rather than assuming these stay valid. Compilation isn't play-mode verification.

Recent Git incident: an empty stale .git/index.lock blocked operations. After explicit approval and checking no Git process, only that lock was removed; user confirmed success. No push/commit was performed by assistant. Never delete .git/index or reset the worktree as a substitute.

Suggested new-chat prompt: "Read Docs/Combat-Handoff-2026-10-18.md, then inspect the relevant current files. Continue our Wakizashi/DualBlade and universal damage work. Keep changes small, explain them, and default to guiding me through edits unless I ask you to implement. Our latest implemented change was the 0.12-second attack buffer; we are developing Light combos and directional Heavy attacks."
