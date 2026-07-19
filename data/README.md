# VoidFault — Data Mod

Balance tweaks delivered as replacement `.btb` data tables (no code / BepInEx
needed for this part). This is separate from the VoidFault BepInEx plugin — it
just drops modified game tables into `StreamingAssets/`.

## What's in this release

A merge of two community reference mods, combined so they coexist:

- **Summoner rebalance** (from *SummonerTweaks*) — support-ability cost cuts,
  a buffed Convert MP, summon-command debuffs, and amp rebalancing.
- **Black Mage / Black Resonance rescale** (from *BlackResonance*) — makes the
  Black Resonance specialty actually work for a lone user.

Both mods edited `DetailInfoSupportTable`; they touch **different records**, so
they are merged here into one file (no conflict).

## Layout

```
data/
  Common_en/          <- the MOD: modified tables, copy these into the game
    Paramater/ ...
    Battle/ ...
  orig/
    Common_en/        <- BACKUP: vanilla version of every file the mod changes
      Paramater/ ...
      Battle/ ...
```

Every file under `Common_en/` has a matching file under `orig/Common_en/` at the
same relative path.

## Installing

Copy the `Common_en/` tree into the game's streaming assets, overwriting:

```
<GameDir>/BDFFHD_Data/StreamingAssets/Common_en/
```

Back up the game's own files first (or rely on Steam "verify integrity").

## Restoring / uninstalling

Copy `orig/Common_en/` over `StreamingAssets/Common_en/` — same paths, so it's a
straight overwrite that returns exactly the tables this mod touched to vanilla.

**About `orig/`:** these are *reconstructed* vanilla tables — produced by
reverting each modded field back to its vanilla value and verified to dump
identically to the vanilla data. They restore original behavior on a drop-in.
They are **not** guaranteed byte-identical to the game's shipped files (the
string tables may carry harmless unused bytes). For a guaranteed byte-perfect
restore, use Steam → Verify Integrity of Game Files.

## Convention for future additions (follow this)

Whenever a new file is added to `Common_en/`, add its vanilla counterpart to
`orig/Common_en/` at the same path in the same change. Reconstruct it with:

```bash
cd BDFFHD-Modding/tools
uv run --python 3.12 python btb_tool.py import \
  <path>/data/Common_en/<sub>/<File>.btb \
  <path>/BDFFHD-Modding/tools/dump/Common_en/<sub>/<File>.json \
  -o <path>/data/orig/Common_en/<sub>/<File>.btb
```

(That imports the vanilla JSON dump onto the modded `.btb`, reverting every
changed field.) Then verify it dumps back to the vanilla JSON with no
named-field differences before committing.

## Exact changes

### `Common_en/Paramater/SupportAbility.btb` — 16 records
Support-ability cost cuts and amp rebalancing (Summoner focus):
- **Convert MP** — `COST 3→2`, `TARGET_MP 1→10` (restores 10% of damage, not 1%).
- Cost cuts: Save TM/BM/SM/WM MP `2→1`, Save Magic MP `3→2`, Time Slip `3→1`,
  Convergence `3→1`, BP Skill Amp `3→1`, Pierce M.Defense `3→2`,
  Summon in Pinch `2→1`, Summoning Amp `2→1`, Summoning Surge `3→2`,
  Max Black Magic `3→2`, Max Summoning `3→2`.
- Amp rebalance: Black Magic Amp / Summoning Amp / Max Black Magic / Max
  Summoning — higher `DAMAGE_RATE`, lower `MPCOST_RATE`.

### `Common_en/Paramater/CommandAbility.btb` — 5 records
The five elemental summons (Promethean Fire, Ziusudra's Sin, Girtablulu,
Deus Ex, Hresvelgr) gain a debuff: `CHANGE_* 100→85` (a 15% stat cut on the
relevant stat) and `EFF_TURN 0→4`, with updated in-battle `DOC`.

### `Common_en/Paramater/JobTable15.btb` — 2 records
Summoner learn-order swap: ability `1402 ↔ 1002` between two level slots
(brings Convert MP earlier).

### `Common_en/Paramater/DetailInfoCommandTable.btb` — 5 records
Menu description text for the five summons, updated to match the new debuffs.

### `Common_en/Paramater/DetailInfoSupportTable.btb` — 5 records (merged)
Menu description text updates:
- 4 records from the Summoner rebalance (Convert MP, Black Magic Amp,
  Max Black Magic, Summoning Amp — matching their numeric changes).
- 1 record from the Black Resonance rescale (rec 36).

### `Common_en/Battle/CorrectionData.btb` — 1 record
Black Resonance scaling: `magSympathy[0..3]: [10,110,115,120] → [110,120,130,140]`
(x1.1 / 1.2 / 1.3 / 1.4). Makes the ability effective even with a single user.
See the repo-root `DATA_MODDING_GUIDE.md` for the open question about how the
lone-user (`magSympathy_0`) case is actually applied in the engine.

## Provenance / rebuilding

Built from the vanilla tables plus the two reference mods under
`BDFFHD-dump/refmods/`. The only merged file (`DetailInfoSupportTable`) was
produced by importing the merged JSON in `datamod-work/` onto the SummonerTweaks
`.btb`. Every record diff vs vanilla was verified. Tooling and workflow:
repo-root `DATA_MODDING_GUIDE.md`.

Future balance work (a targeted support-ability overhaul) is scoped in the
repo-root `DATA_REBALANCE_PLAN.md` — intentionally **not** part of this release.
