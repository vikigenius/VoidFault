# VoidFault

A personal BepInEx mod for **Bravely Default: Flying Fairy HD** (Steam, Unity 6 / IL2CPP).

## What it does

| Feature | Effect |
|---|---|
| **MAtk Scaling** | Recalculates Magic Attack from equipment, support abilities, and job level, instead of the base game formula. |
| **JP Up (Everyone)** | Every character earns a bonus to JP gained after battle, on top of anything the vanilla "JP Up" support ability already grants. Default **+20%**, and it stacks if a character has the real ability equipped too. |

## Requirements

- BepInEx 6 (Unity IL2CPP build, Build 697+) installed into the game directory, next to `BDFFHD.exe`
- Game launched at least once after installing BepInEx, so `BepInEx\interop\` is generated

This mod assumes BepInEx itself has been patched with the Il2CppInterop injector fix — it does **not** ship its own copy of that workaround.

## Installing

1. Build the project (see below), or drop a pre-built `VoidFault.dll`.
2. Copy the output into `BepInEx\plugins\VoidFault\VoidFault.dll`.
3. Launch the game. Check `BepInEx\LogOutput.log` for:
   ```
   [Info: VoidFault] Hello from VoidFault v1.0.0!
   [Info: VoidFault] Patches applied.
   ```

### Building from source

```bash
cd VoidFault
dotnet build
```

The `.csproj` builds straight into `BepInEx\plugins\VoidFault\`. Edit `<GameDir>` in `VoidFault.csproj` if your Steam library isn't at the default path.

## Configuration

Settings are written to `BepInEx\config\com.voidfault.mod.cfg` after the first run:

```ini
[JPUp]
## Grant every character a bonus to JP earned after battle, stacking with the vanilla JP Up ability.
Enabled = true

## Percentage of earned JP added as a bonus (e.g. 20 = +20%).
BonusPercent = 20
```

Set `Enabled = false` to turn the JP bonus off entirely, or change `BonusPercent` to any value — no rebuild required. Config changes are picked up on the next game launch.

## Uninstalling

Delete `BepInEx\plugins\VoidFault\` and, if you want to remove the settings too, `BepInEx\config\com.voidfault.mod.cfg`.
