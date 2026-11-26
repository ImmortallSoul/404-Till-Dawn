# Assets used by the prototype

Most assets below are **CC0 1.0 Universal** (public domain dedication). The Fab and
HomkaPlay additions are free downloads under their own royalty-free / Fab Standard
licenses; they are credited separately and none of the reference game's assets,
Mixamo or paid packs were used.

## 3D weapon models

- **Ultimate Gun Pack** / **50 Low Poly Guns** by **Quaternius**
  - Source: https://quaternius.itch.io/50-lowpoly-guns
  - Also: https://quaternius.com/packs/ultimategun.html
  - License: CC0 1.0
  - Files used: `Revolver_1.fbx`, `Shotgun_1.fbx`, `SubmachineGun_1.fbx`
  - Project paths: `Assets/Resources/Weapons/Revolver.fbx`, `Shotgun.fbx`, `DualSmgs.fbx`

- **Blaster Kit** by **Kenney**
  - Source: https://kenney.nl/assets/blaster-kit
  - License: CC0 1.0 — https://kenney.nl/license
  - Files used: `blaster-m.fbx`, `blaster-p.fbx`, `blaster-c.fbx`, `blaster-j.fbx`, `Textures/colormap.png`
  - Project paths: `Assets/Resources/Weapons/Batgun.fbx`, `FlameCannon.fbx`, `GrenadeLauncher.fbx`, `WateringGun.fbx`, `Textures/colormap.png`
  - Full license text: `Assets/Credits/KENNEY_BLASTER_KIT_LICENSE.txt`

- **KayKit Skeletons — FREE** by **Kay Lousberg**
  - Source: https://kaylousberg.itch.io/kaykit-skeletons
  - License: CC0 1.0
  - Files used as a ready weapon model: `Skeleton_Staff.fbx`, `skeleton_texture.png`
  - Project paths: `Assets/Resources/Weapons/KayKitStaff.fbx`, `skeleton_texture.png`
  - Used with the `Staff` viewmodel. The Crossbow viewmodel now uses the separate
    CreativeTrio model below; KayKit skeleton geometry remains only for characters.

- **Crossbow** by **CreativeTrio**
  - Source: https://poly.pizza/m/kHb0kA11oD
  - License: CC0 1.0
  - Project path: `Assets/Resources/Weapons/Crossbow.fbx`
  - Used with the `Crossbow` viewmodel.

- **GIFT Fantasy Dao Sword Kryven Blade — PBR Game Ready** by **IronRealm-Forge**
  - Source: https://www.fab.com/listings/0191eee4-8c1c-41dd-ad5c-16180f9158ec
  - License: Fab Standard License (free tier; commercial/private use, modification and
    embedding allowed; no standalone resale). EULA: https://www.fab.com/eula
  - Project path: `Assets/Resources/Weapons/KryvenBlade/`
  - Used with the `CycloneSword` viewmodel. The supplied Built-in Standard material,
    emissive map and green runes/eyes are preserved.

- **KayKit Fantasy Weapons Bits FREE** by **Kay Lousberg**
  - Source: https://kaylousberg.itch.io/fantasy-weapons-bits
  - License: CC0 1.0
  - Files used: `KayKitSwordC.fbx`, `weapons_bits_texture.png`
  - Project paths: `Assets/Resources/Weapons/KayKitSwordC.fbx`, `weapons_bits_texture.png`
  - Kept as an unused fallback; the active `CycloneSword` viewmodel uses Kryven Blade.

## Character models for summons and elites

- **KayKit Skeletons — FREE** by **Kay Lousberg**
  - Source: https://kaylousberg.itch.io/kaykit-skeletons
  - License: CC0 1.0
  - Files used: `Skeleton_Mage.fbx`, `Skeleton_Minion.fbx`, `Skeleton_Rogue.fbx`, `Skeleton_Warrior.fbx`, `skeleton_texture.png`
  - Project paths: `Assets/Resources/Characters/`
  - Runtime loader: `Assets/Scripts/CharacterVisualCatalog.cs`
  - Small summons, elites/bosses and ordinary small hordes receive ready models; procedural meshes remain only as a fallback when a resource is missing.

## Character animations

- **KayKit Character Animations — FREE** by **Kay Lousberg**
  - Source: https://kaylousberg.itch.io/kaykit-character-animations
  - License: CC0 1.0
  - Files used: `Rig_Medium_General.fbx`, `Rig_Medium_MovementBasic.fbx`, `Rig_Medium_MovementAdvanced.fbx`, `Rig_Medium_CombatMelee.fbx`, `Rig_Medium_CombatRanged.fbx`, `Rig_Medium_Special.fbx`
  - Project paths: `Assets/Resources/Animations/Rig_Medium/`
  - Runtime aliases in `Assets/Scripts/CharacterAnimationController.cs`:
    - `Spawn` → `Spawn_Ground`
    - `Idle` → `Idle_A`
    - `Walk` → `Walking_A`
    - `Run` → `Running_A`
    - `Attack` → melee punch / ranged shoot / magic summon depending on profile
    - `Hit` → `Hit_A`
    - `Death` → `Death_A`
    - `Reload` → `Ranged_1H_Reload`
    - `Shoot` → `Ranged_1H_Shoot`
    - `Magic` → `Ranged_Magic_Spellcasting`

## Creature models for summons

- **Fantasy Creature Pack** by **Quaternius**
  - Source: https://poly.pizza/m/VBvzjFIYws (Dragon), https://poly.pizza/m/hNO9XvjlKa (Bat)
  - License: CC0 1.0
  - Project paths: `Assets/Resources/Creatures/Dragon.fbx`, `Assets/Resources/Creatures/Bat.fbx`
  - Used with the `Dragon` and `FrenzyBat` summons instead of skeleton substitutes.

- **Blue Butterfly** by **Google Data Arts Team**
  - Source: https://poly.pizza/m/7PxWOlSbXf
  - License: CC BY 3.0 — https://creativecommons.org/licenses/by/3.0/
  - Project path: `Assets/Resources/Creatures/Butterfly.fbx`
  - Used with the `Butterfly` summon instead of a skeleton substitute.

## Music and sound effects

- **Music Jingles** by **Kenney**
  - Source: https://kenney.nl/assets/music-jingles
  - License: CC0 1.0 — https://kenney.nl/license
  - Project path: `Assets/Resources/Audio/Music/`
  - Used for menu, run, final-minute and dawn music states.

- **Sci-Fi Sounds** by **Kenney**
  - Source: https://kenney.nl/assets/sci-fi-sounds
  - License: CC0 1.0
  - Project path: `Assets/Resources/Audio/Sfx/`
  - Used for: `gun_revolver_*`, `gun_shotgun_*`, `gun_smg_*`, `gun_grenade_*`, `gun_staff_0`, `gun_water_*`, `flame_loop`, `summon_spawn_*`, `summon_attack`, `boss_spawn`, `phased`

- **Impact Sounds** by **Kenney**
  - Source: https://kenney.nl/assets/impact-sounds
  - License: CC0 1.0
  - Project path: `Assets/Resources/Audio/Sfx/` (`enemy_hit_*`, `enemy_death_*`, `hurt`, `footstep_*`)

- **RPG Audio** by **Kenney**
  - Source: https://kenney.nl/assets/rpg-audio
  - License: CC0 1.0
  - Project path: `Assets/Resources/Audio/Sfx/`
  - Used for: `reload_start_*`, `reload_end_0`, `gun_crossbow_*`, `gun_bat_*`, `gun_knife_*`, `gun_sword_*`

- **Interface Sounds** by **Kenney**
  - Source: https://kenney.nl/assets/interface-sounds
  - License: CC0 1.0
  - Project path: `Assets/Resources/Audio/Sfx/`
  - Used for: `card_select`, `xp_*`, `heal`, `ui_click`, `ui_back`, `gun_staff_1`, `gun_staff_2`

Runtime mixer and event map: `Assets/Scripts/AudioManager.cs`.

- **500 Free Essential Game SFX Starter Pack** by **HomkaPlay**
  - Source: https://homkaplay.itch.io/500-free-essential-game-sfx-starter-pack-royalty-free-sound-effects
  - License: royalty-free (AI assisted), free download; no attribution requirement stated.
  - Selected files: `FreeSFX_UI_*`, `FreeSFX_Magic_Fantasy_*`,
    `FreeSFX_Nature_Creatures_*`, `FreeSFX_Footsteps_Foley_*`.
  - Project paths: `Assets/Resources/Audio/Sfx/extra_ui_*.wav`,
    `extra_magic_*.wav`, `extra_nature_*.wav`, `extra_step_*.wav`.
  - Runtime cue groups: `extra_ui`, `extra_magic`, `extra_nature`, `extra_step`.
    These are **additive**: none of the 11 existing weapon cues were replaced.

## Fab free-tier models (Fab Standard License)

These listings were downloaded from their official Fab pages under the free
Personal tier. The paid Professional tier was **not** used. Their meshes are
recolored at runtime with this project's Built-in-pipeline materials, so the
original URP/HDRP materials and showcase scenes were deliberately not imported.

- **Stylized Low Poly Skeleton** by **SazenGames**
  - Source: https://www.fab.com/listings/f1efdc03-9272-4757-828c-a1b6ad7ebf2d
  - License: Fab Standard License, free Personal tier.
  - Files used: `Skeleton_Model_110.fbx`, `Skeleton_spawn/idle/walk_forward/run_forward/death/take_damage/slash01/slash02/throw_projectiles.fbx`
  - Project paths: `Assets/Resources/Characters/FabSkeleton/Skeleton_Model.fbx`,
    `Assets/Resources/Animations/FabSkeleton/`
  - Used for the full-body player avatar and the first-person skeletal hands with
    matching animations; the WRAD/PSX arm packs remain only as unused legacy files.

- **Halloween – Scary Pumpkins Kit – GameReady** by **PossumStore**
  - Source: https://www.fab.com/listings/191f1521-7c12-47fd-96c3-de945bc9bd7b
  - License: Fab Standard License, free Personal tier.
  - Files used: `SM_Pumpkin01.fbx` … `SM_Pumpkin04.fbx`
  - Project paths: `Assets/Resources/Pumpkins/SM_Pumpkin_01.fbx` … `SM_Pumpkin_04.fbx`
  - Used for `PumpkinPatch` props and `EnemyKind.Pumpkin`.

- **(FREE) Realistic Rocks – Asset LowPoly** by **PizzaAndGames**
  - Source: https://www.fab.com/listings/7da69e24-a9fc-453e-aa2b-858da05ab2bc
  - License: Fab Standard License, free Personal tier.
  - Files used: `SM_LittleRock_01.fbx` … `SM_LittleRock_06.fbx`
  - Project paths: `Assets/Resources/Rocks/SM_LittleRock_01.fbx` … `SM_LittleRock_06.fbx`
  - Used as solid, collider-backed rocks on all maps.

- **Simple Lowpoly Nature Pack** by **Reckera Studios**
  - Source: https://www.fab.com/listings/5c6b2404-c67b-499b-9c98-c646e7767bb4
  - License: Fab Standard License, free Personal tier.
  - Files used: `SM_Oak_Tree_01/02.fbx`, `SM_Pine_Tree_01/02.fbx`,
    `SM_Plant_01/02/03.fbx`, `SM_Tree_Stump_01/02.fbx`
  - Project paths: `Assets/Resources/Nature/`
  - Used for the dense forest, bushes and stumps.

Runtime selection: `Assets/Scripts/ArenaBuilder.cs`,
`Assets/Scripts/CharacterVisualCatalog.cs`, `Assets/Scripts/PlayerAvatarController.cs`
and `Assets/Scripts/CharacterAnimationController.cs`.

## PBR textures

- **ambientCG** — https://ambientcg.com/
- License: CC0 1.0 — https://docs.ambientcg.com/license

| Project texture | ambientCG ID |
|---|---|
| `Ground_Color.jpg`, `Ground_Normal.jpg`, `Ground_Roughness.jpg` | Ground037 |
| `Grass_Color.jpg`, `Grass_Normal.jpg`, `Grass_Roughness.jpg` | Grass004 |
| `Stone_Color.jpg`, `Stone_Normal.jpg`, `Stone_Roughness.jpg` | Rock030 |
| `Wood_Color.jpg`, `Wood_Normal.jpg`, `Wood_Roughness.jpg` | WoodFloor037 |
| `Metal_Color.jpg`, `Metal_Normal.jpg`, `Metal_Roughness.jpg` | Metal032 |
| `Fabric_Color.jpg`, `Fabric_Normal.jpg`, `Fabric_Roughness.jpg` | Fabric030 |
| `Leather_Color.jpg`, `Leather_Normal.jpg`, `Leather_Roughness.jpg` | Leather001 |
| `Ceramic_Color.jpg`, `Ceramic_Normal.jpg`, `Ceramic_Roughness.jpg` | Tiles141 (glazed ceramic-style tiles) |

Only `Color` and `NormalDX` maps (renamed to `*_Normal.jpg`) are currently applied at runtime.
Roughness maps are kept for future material polish and do not affect gameplay.

## Viewmodel hands and weapon effects

- **First-person hands**: the current `Assets/Scripts/ViewModelHandAdapter.cs` uses
  the FabSkeleton skeleton model credited above, so the hands are skeletal rather than
  the older cloth-cuff/low-poly arm packs. The following CC0 packs are kept only as
  unused legacy assets and are not attached to the viewmodel.
- **WRAD ARMS** by **wriks** — https://wriks.itch.io/wrad-arms — CC0 1.0
  - Legacy paths: `Assets/Resources/ViewModels/Hands/WradArms/WradArms.fbx`, `arm_albedo_pale.png`, `arm_albedo_dark.png`
- **PSX First Person Arms Free** by **Drill Impact** — https://drillimpact.itch.io/psx-first-person-arms-free — CC0 1.0
  - Legacy paths: `Assets/Resources/ViewModels/Hands/PsxArms/PsxArms.fbx`, `arms_01.png`, `arms_gloves_01.png`
- One/two-hand policy is enforced in `Assets/Scripts/ViewModelPoseCatalog.cs`:
  Revolver, Batgun, Staff, SalvoKnife and CycloneSword are one-handed; Shotgun,
  Crossbow, FlameCannon, GrenadeLauncher and WateringGun are two-handed; DualSmgs has
  one hand per barrel. Recoil, reload, equip, walk and staff cast animations move the
  imported hands together with the weapon.
- **Particle Pack** by **Kenney** — https://kenney.nl/assets/particle-pack — CC0 1.0
  - Project path: `Assets/Resources/Vfx/Particles/`
  - Used for the continuous flamethrower stream, explosion fire bursts, muzzle/spark accents
    and the staff/cyclone energy glow. The FlameCannon no longer spawns projectile spheres.

## Player avatar, arena and interface

- A full-body player avatar is instantiated from ready skeleton models at runtime
  (`Assets/Scripts/PlayerAvatarController.cs`). It lives on a dedicated layer that the
  first-person camera excludes, while the rear-view mirror camera and shadows include it.
  The procedural capsule is disabled when a ready model is available.
- Every enemy tier — including ordinary small hordes — now prefers imported character
  models through `CharacterVisualCatalog.CreateEnemy()`. `EnemyKind.Pumpkin` prefers the
  pumpkin kit; procedural meshes remain only as a fallback when a resource is missing.
- Arena props prefer ready tree/bush/stump/rock/pumpkin models via `ArenaBuilder.cs`.
  If a pack is absent, runtime-generated primitives keep the map playable.
- Spawns are rejected inside the camera's horizontal FOV cone (plus a margin) and inside
  solid props; wide temple corridors and forest clearings keep the map traversable.
- `F1` opens a pause panel with music/SFX/UI volume, mouse sensitivity, Y inversion,
  next-run darkness and a confirmed exit to the main menu. Values persist in `PlayerPrefs`.

## Summon scythe and softened weapon sounds

- **Scythe** by **Quaternius** — https://poly.pizza/m/yE9TKCewO8
  - License: CC0 1.0 — https://creativecommons.org/publicdomain/zero/1.0/
  - Project path: `Assets/Resources/Summons/ReapersEdge.fbx`
  - The previous Meshy FBX converted to an empty Unity prefab, so the summon now uses
    this valid CC0 low-poly scythe. It stays a floating weapon only: no ghost body,
    and `SummonManager.AnimateScythe()` swings the pivot because the mesh is static.
- **500 Free Essential Game SFX Starter Pack** by **HomkaPlay** - https://homkaplay.itch.io/500-free-essential-game-sfx-starter-pack
  - License: see the pack's free-use terms; personal/non-commercial use is allowed.
  - Project paths: `Assets/Resources/Audio/Sfx/WeaponSoft/gun_*_soft_*.wav`
  - Twenty trimmed, faded and gain-reduced clips replace the ten one-shot weapon cues
    in `AudioManager.cs`. Source categories are `Weapons_Combat`, `Magic_Fantasy`
    and `Nature_Creatures`. The original Kenney clips remain available for shared cues.
  - `flame_loop.ogg` is unchanged: the pack has no seamless flame loop.

## Notes

- No assets from the reference game were used.
- No paid packs were used.
- No Mixamo assets were used.
- If any resource is missing, the code falls back to runtime-generated materials and meshes,
  so the game still runs.
