# VoidFault

A personal BepInEx mod for **Bravely Default: Flying Fairy HD** (Steam, Unity 6 / IL2CPP).

## What it does

| Feature | Effect |
|---|---|
| **MAtk Scaling** | Recalculates Magic Attack from equipment, support abilities, and job level, instead of the base game formula. |
| **JP Up (Everyone)** | Every character earns a bonus to JP gained after battle, on top of anything the vanilla "JP Up" support ability already grants. Default **+20%**, and it stacks if a character has the real ability equipped too. |
| **Gold Up** | Bonus to Gil earned after battle. Default **+20%**. |

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

Edit `<GameDir>` in `VoidFault.csproj` if your Steam library isn't at the default path. On every build, an MSBuild target automatically copies `VoidFault.dll` (+ PDB) from the normal `bin\Debug\net6.0\` output into `BepInEx\plugins\VoidFault\` — no manual copying needed.

## Configuration

Settings are written to `BepInEx\config\com.voidfault.mod.cfg` the first time the mod loads:

```ini
[JPUp]
## Grant every character a bonus to JP earned after battle, stacking with the vanilla JP Up ability.
Enabled = true

## Percentage of earned JP added as a bonus (e.g. 20 = +20%).
BonusPercent = 20

[GoldUp]
## Grant a bonus to Gil earned after battle.
Enabled = true

## Percentage of earned Gil added as a bonus (e.g. 20 = +20%).
BonusPercent = 20

[Debug]
## Log per-call details for JP Up / Gold Up / MAtk Scaling patches. Off by default.
## Note: MAtk Scaling logs on every GetMATK call, which fires constantly outside battle
## too (menus, tooltips) — expect a lot of log lines while this is on.
Enabled = false
```

Set either feature's `Enabled = false` to turn that bonus off entirely, or change its `BonusPercent` to any value — no rebuild required. Config changes are picked up on the next game launch.

Set `[Debug] Enabled = true` to log what each patch is actually doing to `BepInEx\LogOutput.log` (useful for confirming a patch fires or checking the numbers look right) — leave it off otherwise, especially since MAtk Scaling's hook fires very frequently outside battle too.

A copy of this default lives in [`config/com.voidfault.mod.cfg`](config/com.voidfault.mod.cfg) in this repo — copy it straight into `BepInEx\config\` if you want the file to exist before first launch, or to reset back to defaults later.

## Uninstalling

Delete `BepInEx\plugins\VoidFault\` and, if you want to remove the settings too, `BepInEx\config\com.voidfault.mod.cfg`.

## Developer docs

- [`LEARNINGS.md`](LEARNINGS.md) — modding tricks/gotchas picked up while building this (IL2CppDumper vs. decompilers, Harmony/IL2CPP quirks, project setup rationale).
- [`RESEARCH_NOTES.md`](RESEARCH_NOTES.md) — investigation log for the Norende Colony/Passing Soul population system: what's confirmed, what we tried, where it's stuck.
- [`GHIDRA_SETUP.md`](GHIDRA_SETUP.md) — step-by-step Ghidra setup for reading `GameAssembly.dll`'s actual decompiled logic, for when signature-only guessing (`dump.cs`) isn't enough.
