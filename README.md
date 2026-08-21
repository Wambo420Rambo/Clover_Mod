# CloverMod

CloverMod is an in-game utility, cheat, and quality-of-life menu for **CloverPit**. Version 2.0 rebuilds the original plugin around validated game actions, persistent settings, focused Harmony patches, and a recoverable menu hotkey.

> CloverMod can directly change active-run data and unlock achievements. Back up important saves before experimenting with extreme values.

## Quick start

1. Install **BepInEx 5.x (Mono)** for CloverPit and launch the game once.
2. Copy `CloverMod.dll` into `CloverPit/BepInEx/plugins/`.
3. Start CloverPit.
4. Press **M** to open CloverMod. **Insert** is the default recovery key.

The BepInEx log should contain `Clover Mod v2.0.0 loaded`. Keep only one copy of `CloverMod.dll` in the plugins folder.

## Features

### Currency, rates, and multipliers

- Add exact coin and clover-ticket amounts.
- Add `10^n` BigInteger coin and multiplier values.
- Change pattern and symbol multipliers.
- Configure interest, 666 chance, its maximum, and the red-button multiplier.
- Add charm slots, free store restocks, rounds, and spins.

### Charms

- Search all charms by name.
- Filter to charms owned in the current run.
- See owned, equipped, drawer, unlock, and charge information.
- Unlock, equip, recharge, modify, or discard a selected charm.
- Recharge every charm and equip all corpse pieces.
- Set activation luck, charm luck, and store luck with descriptions in the menu.

### Symbols and patterns

- Edit symbol spawn weights as percentages.
- Lock individual weights while the remaining unlocked fields are redistributed to exactly `100%`.
- Use `0%` for a symbol without breaking automatic redistribution.
- Load and normalize the current in-game weights or equalize all unlocked fields.
- Edit symbol coin values and pattern values.

### Run tools

- Inspect the current seed, coins, debt, deposit, Deadline level, rounds, spins, interest, luck, and multipliers.
- Copy the current seed.
- Safely edit active-run values with validation.
- Apply Normal and Lucky presets or save a persistent Custom preset.
- Undo the latest supported reversible numeric change.
- Trigger phone actions and optionally unlock achievements.

### Slot and memory cards

- Optional **Auto mode** starts the next slot spin when the machine is ready.
- Set owned and victory counts for every memory card.
- Optionally prevent owned memory-card counts from decreasing.

Auto mode uses CloverPit's normal spin call. Costs, statistics, charms, results, and slot animations continue through the normal game logic.

## Quality-of-life options

All QoL switches are **off by default** and can be changed under `Extras → Quality of life` or in `BepInEx/config/Clovermod.cfg`.

| Option | Default | Behavior |
| --- | ---: | --- |
| Auto-skip intro | Off | Skips scene 1 and enters the main game automatically. |
| Auto-complete corpse | Off | Once per run, puts missing skeleton limbs into available drawers. |
| Skip memory-pack punch | Off | Removes the pack-punch animation. |
| Auto-flip pack cards | Off | Requests normal flips for face-down cards during pack deals. |
| Fast memory-pack flow | Off | Shortens waits and continues non-dialogue pack prompts automatically. |
| Phase speed profiles | Off | Applies the configured speed for normal play, gambling, post-jackpot animations, cutscenes, and charm discards. |

Fast memory packs preserve the original deal coroutine and leave the Yes/No deal decision to the player.

The default phase-profile values are:

| Phase | Speed |
| --- | ---: |
| Normal game and animations | 1x |
| Gambling animations | 4x |
| Animations after the first recorded jackpot | 10x |
| Cutscenes | 3x |
| Charm-discard burst | 4x |

Phase profiles override the two manual speed sliders while enabled.

## Controls and configuration

| Setting | Default | Description |
| --- | ---: | --- |
| `MenuKey` | `M` | Opens and closes CloverMod. Existing F2 defaults migrate to M. |
| `FallbackMenuKey` | `Insert` | Recovery key if the primary binding is unavailable. Set to `None` to disable. |
| `PauseWhileOpen` | On | Pauses gameplay while the CloverMod menu is open. |
| `AutoSlotMode` | Off | Automatically starts the next slot spin. |
| `UnlimitedMemoryCards` | Off | Prevents owned memory-card counts from being spent. |

Both menu keys can also be rebound inside CloverMod. If neither binding works, close the game and set either key in `BepInEx/config/Clovermod.cfg` to a valid `UnityEngine.KeyCode`, such as `F4`, `Home`, or `Insert`.

## Safety behavior

- Payout and charm animations temporarily use at most **4x animation speed** to avoid black screens. The selected target is restored after the animation.
- Camera movement is clamped to prevent high-speed transitions from overshooting outside the room.
- Global game speed and payout/transition animation speed are separate controls.
- Activation, charm, and store luck accept `0.5` to `100,000`. Vanilla is `1.0`; CloverPit itself clamps these values to at least `0.5`.
- Deadline and Deposit edits require two clicks within six seconds.
- Charm discard and achievement unlocking also require confirmation because their effects cannot be safely undone.
- Undo stores only the latest supported numeric change. Unlocks, achievements, charm discard, and other irreversible actions are not included.

Extreme values may still cause long base-game payout sequences, UI layout problems, or save data the original game was not designed to handle.

## Requirements

- CloverPit
- BepInEx 5.x for the Mono build of the game

**CloverAPI is not required.** CloverMod does not add custom charms or persistent game content.

## Installation

The default Steam installation is:

```text
C:\Program Files (x86)\Steam\steamapps\common\CloverPit
```

Install the DLL here:

```text
CloverPit\BepInEx\plugins\CloverMod.dll
```

After launching the game, configuration is stored here:

```text
CloverPit\BepInEx\config\Clovermod.cfg
```

Version 2.0 automatically removes obsolete Skip Reel, Skip Winning Animation, Turbo, and scientific-notation configuration entries left by older builds.

## Troubleshooting

- **Menu does not open:** try `Insert`, then verify `MenuKey` and `FallbackMenuKey` in the configuration file.
- **Plugin does not load:** check `BepInEx/LogOutput.log` for `Clover Mod v2.0.0 loaded` and confirm that BepInEx is the Mono build.
- **Duplicate behavior or patches:** remove older CloverMod DLLs and keep only `BepInEx/plugins/CloverMod.dll`.
- **Speed behaves unexpectedly:** disable automatic phase speed profiles before using the manual global and animation-speed sliders.

## Building from source

The project targets .NET Framework 4.8 and references assemblies from a local CloverPit installation. Proprietary game DLLs are not committed to this repository.

```powershell
dotnet restore CloverMod.sln
dotnet build CloverMod.sln -c Release
```

For a non-default installation path:

```powershell
dotnet build CloverMod.sln -c Release -p:GameDir="D:\Games\CloverPit"
```

Alternatively, set the `CLOVERPIT_DIR` environment variable. The compiled plugin is written to `bin/Release/CloverMod.dll`.

## Project structure

```text
Configuration/  Persistent BepInEx settings
Core/           Validated game actions and QoL state handling
Patches/        Focused Harmony patches and safety fixes
UI/             Unity OnGUI/GUILayout menu and hotkey rebinding
Plugin.cs       Plugin lifecycle and update routing
```

The menu uses Unity's built-in `OnGUI`/`GUILayout` API from `UnityEngine.IMGUIModule.dll`. It does **not** use the unrelated third-party ImGui.NET package.

## License

MIT. See [LICENSE](LICENSE).
