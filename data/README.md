# VoidFault — Data Mod

Balance tweaks and new content delivered as replacement `.btb` data tables. This
drops modified game tables into `StreamingAssets/`. It's mostly independent of the
VoidFault BepInEx plugin, with **one dependency**: the Black Resonance change needs
the plugin's `BlackResonanceSolo` patch to behave correctly (see below).

## What's in this release

A merge of four community reference mods, combined so they coexist:

- **Summoner rebalance** (from *SummonerTweaks*) — support-ability cost cuts,
  a buffed Convert MP, summon-command debuffs, and amp rebalancing.
- **Black Resonance rescale** (from *BlackResonance*) — makes the Black Resonance
  specialty worthwhile (and, with the plugin, work for a lone user).
- **Buyable Petal Tokens** (from *BuyablePetalTokens*) — Petal Tokens become
  purchasable in the Florem item shop. **Non-destructive**: added as a new shop
  slot, so the Potion is kept (the original mod replaced it).
- **Magnifying Glass** (from *MagnifyingGlass*) — a new battle item that uses
  "Examine", sold by the colony/Trader Village Adventurer. **Non-destructive**:
  added as a brand-new item row (the original mod overwrote a dummy key item),
  with a proper D's Journal / encyclopedia entry.

## Layout

```
data/
  Common_en/          <- the MOD: copy this whole tree into the game
    Paramater/  Battle/  Colony/  DReportTable/  Shop/
  orig/
    Common_en/        <- BACKUP: vanilla version of every file the mod changes
```

Every file under `Common_en/` has a matching file under `orig/Common_en/` at the
same relative path.

## Installing

Copy the `Common_en/` tree into the game's streaming assets, overwriting:

```
<GameDir>/BDFFHD_Data/StreamingAssets/Common_en/
```

Back up the game's own files first (or rely on Steam "verify integrity").

For the **Black Resonance solo** behavior, also install the VoidFault plugin
(the `BlackResonanceSolo` patch). Without it, the vanilla engine only applies the
scaling with 2+ Black Resonance users; the plugin fixes the lone-user case and
uses all four `magSympathy` values.

## Restoring / uninstalling

Copy `orig/Common_en/` over `StreamingAssets/Common_en/` — same paths, a straight
overwrite that returns exactly the tables this mod touched to vanilla.

**About `orig/`:** these are *reconstructed* vanilla tables, verified to dump
identically to the vanilla data. They restore original behavior on a drop-in but
are not guaranteed byte-identical (string tables may carry harmless unused bytes).
For a byte-perfect restore, use Steam → Verify Integrity of Game Files.

## Convention for future additions (follow this)

Whenever a new file is added to `Common_en/`, add its vanilla counterpart to
`orig/Common_en/` at the same path in the same change, and verify it dumps back to
vanilla with no named-field differences. Reconstruct the vanilla version by
importing the vanilla JSON (**baseline: `BDFFHD-dump/data_dump/`**, which is clean
— the older `tools/dump` ItemTable is contaminated) onto the modded `.btb`. New
rows are added with `btb_tool import --allow-grow`; the game honors appended rows
(the loaders read the record count from the file header).

## Exact changes

### Summoner rebalance
- **`Paramater/SupportAbility.btb`** — Convert MP (`COST 3→2`, `TARGET_MP 1→10`);
  cost cuts (Save TM/BM/SM/WM MP `2→1`, Save Magic MP `3→2`, Time Slip `3→1`,
  Convergence `3→1`, BP Skill Amp `3→1`, Pierce M.Defense `3→2`, Summon in Pinch
  `2→1`, Summoning Amp `2→1`, Summoning Surge `3→2`, Max Black/Summoning `3→2`);
  amp rebalance (higher `DAMAGE_RATE`, lower `MPCOST_RATE`). *(Also now carries the
  Petal Token change below, since both live in ItemTable/SupportAbility tables.)*
- **`Paramater/CommandAbility.btb`** — the five elemental summons get a 15% debuff
  (`CHANGE_* 100→85`, `EFF_TURN 0→4`) + updated `DOC`.
- **`Paramater/JobTable15.btb`** — Summoner learn-order swap (`1402 ↔ 1002`) to
  bring Convert MP earlier.
- **`Paramater/DetailInfoCommandTable.btb`** — summon menu text matching the debuffs.

### Black Resonance rescale
- **`Battle/CorrectionData.btb`** — `magSympathy[0..3]: [10,110,115,120] →
  [110,120,130,140]`. With the `BlackResonanceSolo` plugin patch this reads as
  x1.1 / 1.2 / 1.3 / 1.4 for 1 / 2 / 3 / 4 users. (Vanilla data has an effective
  x0.10 bug at 2 users; this fixes it.)
- **`Paramater/DetailInfoSupportTable.btb`** — support-ability menu text: 4 records
  from the Summoner rebalance + 1 for Black Resonance (merged; disjoint records).

### Buyable Petal Tokens (non-destructive)
- **`Paramater/ItemTable.btb`** — Petal Token (id 40126) made buyable
  (`BUY 500`, sellable).
- **`Shop/TW_13_Item.spb`** — Florem item shop grown 11 → 12 rows; Petal Token
  appended as the new slot (Potion kept).

### Magnifying Glass (non-destructive new item)
A brand-new consumable, id **40156**, appended across five tables (the dummy key
item is left untouched):
- **`Paramater/ItemTable.btb`** — new item row (TYPE 16 battle item, `USE_ABI 101`
  = Examine, `ENABLE=1`).
- **`Paramater/DetailInfoItemTable.btb`** — in-menu name/description (`itID 40156`).
- **`Paramater/DNoteItemTable.btb`** — journal id→index map (`40156 → INDEX 314`).
- **`DReportTable/DTableItemData.btb`** — journal category + flavor text
  (`index 315`, `category 16` = Consumables). This is what makes it list in the
  encyclopedia's Consumables subsection.
- **`Colony/PlantParameter.btb`** — sold by the colony/Adventurer shop.

## Provenance / rebuilding

Built from vanilla baselines (`BDFFHD-dump/data_dump/`) plus the four reference
mods under `BDFFHD-dump/refmods/`, using `btb_tool.py` (append rows via
`--allow-grow`). Every change was verified by dumping the result and diffing
against vanilla. Tooling and workflow: repo-root `DATA_MODDING_GUIDE.md`.

Future balance work (a targeted support-ability overhaul) is scoped in the
repo-root `DATA_REBALANCE_PLAN.md` — intentionally **not** part of this release.
