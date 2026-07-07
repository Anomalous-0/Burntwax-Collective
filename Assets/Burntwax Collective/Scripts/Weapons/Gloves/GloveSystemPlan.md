# Glove System Plan

Assumption for this pass: gloves become a dedicated third weapon mode, not just a cosmetic overlay. That matches the existing `Gloves` and `GloveShoot` input actions already in the project.

1. Add `Gloves` to `Arsenal.WeaponMode` and make `Arsenal` the single authority for switching between guns, katana, and gloves.
2. Decide the glove gameplay loop up front: whether gloves are hitscan, projectile, charge-based, cooldown-based, or ammo-based. That choice defines whether the glove machine needs reload, cooldown, or charge states.
3. Create a glove presentation root in the player rig, similar to the current gun and katana setup, with its own model reference, optional VFX spawn points, and impulse source.
4. Build the animator contract before wiring behavior. At minimum, add glove clips and states for `equip`, `idle`, `aim`, `fire`, `reload` or `cooldown`, and `stow`.
5. Hook `InputManager.useGloves` into `Arsenal` so glove select works the same way as the current gun and katana switching flow.
6. Route `InputManager.gloveShootIsPressed` through `GloveStateMachine` instead of piggybacking on the gun attack path. That keeps glove combat rules isolated and easier to tune.
7. Give `GloveStateMachine` ownership of glove runtime flags such as `FullyEquipped`, `IsFiring`, `IsReloading`, and any future `CanChain`, `HasCharge`, or `IsCoolingDown` state.
8. Add animation events for `EndEquip`, `EndFire`, and `EndReload` the same way guns and katana already use animation events to unlock gameplay windows.
9. Decide how gloves interact with the existing arms controller:
   - Reuse the current `Arms.controller` with new glove states if the same first-person rig is shared.
   - Split gloves to a dedicated animator/controller if the hand poses and timing differ too much from guns and katana.
10. When the glove weapon is active, disable or stow the current gun/katana presentation cleanly so only one weapon family owns the hands at a time.
11. Add glove-specific ScriptableObject data once the combat loop is decided. The likely fields are damage, fire cadence, range, VFX, SFX, cooldown, recoil/impulse, and animation state names.
12. Mirror the current save/load approach only after gloves can actually be equipped. Save the selected glove, current mode, and any runtime combat state that should persist.
13. Finish with scene wiring and validation:
   - assign the glove model and animator,
   - confirm the correct state transitions,
   - verify switching from each weapon family,
   - verify animation events restore control,
   - verify save/load restores the glove selection cleanly.

Suggested implementation order:

1. Extend `Arsenal` mode switching.
2. Create glove animator states and clips.
3. Wire `GloveStateMachine.Spawn`, `RequestEquip`, `RequestFire`, and `RequestStow`.
4. Implement glove combat behavior.
5. Add save/load support.
