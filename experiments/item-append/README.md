# Append-based item mods — in-game verification experiment

Tests whether the game honors **appended rows** in BTBF tables (Ghidra Round 7c
showed the loaders trust the file's record count). If it works, item mods can be
non-destructive — no dummy-repurposing, no deleting shop items.

Built with vanilla baselines from `BDFFHD-dump/data_dump` and `btb_tool import
--allow-grow`. **Back up your game first** (or Steam → Verify Integrity to
restore). Files go under `<GameDir>/BDFFHD_Data/StreamingAssets/Common_en/`.

Two tiers — **test `shop-only` first** (lowest risk), then `full`.

Each tier ships an `orig/Common_en/` with the **verified-vanilla** version of every
file it changes (same relative paths), so you can always undo by copying `orig/`
back over `Common_en/`. (Reconstructed vanilla, verified to dump identically to
`data_dump`; for a guaranteed byte-perfect restore use Steam → Verify Integrity.)

---

## Test 1 — `shop-only/`  (appends a row to a shop .spb)

Copy `shop-only/Common_en/` over `Common_en/`. Overwrites:
- `Shop/TW_13_Item.spb` — Florem item shop, grown 11 → **12** rows (vanilla list
  unchanged, **Petal Token (ITEM_ID 40126) appended** as the 12th slot).
- `Paramater/ItemTable.btb` — Petal Token (rec 435) made buyable (BUY 500,
  sellable). Still 597 rows (no ItemTable append here).

**Verify in-game:** enter the **Florem item shop**. The vanilla items — including
the **Potion** — should all still be there, PLUS a **Petal Token (500 pg)** at the
bottom.
- Potion present + Petal Token present (12 items) → **.spb append WORKS**.
- Petal Token missing / shop shows 11 / shop or game crashes → **.spb append not honored** (fall back to the destructive slot-replace).

---

## Test 2 — `full/`  (also appends a new row to ItemTable + DetailInfoItemTable)

Copy `full/Common_en/` over `Common_en/`. Overwrites the two above PLUS:
- `Paramater/ItemTable.btb` — 597 → **598** rows. Dummy "Key Item Dmmy 10"
  (id 90050) is **kept**; a new **Magnifying Glass** is **appended** with
  **id 40156** (TYPE 16 battle item, USE_ABI 101 = Examine, **ENABLE=1**).
  Two findings baked in here:
  - **ID range picks the journal subsection.** The encyclopedia buckets by item-id
    range: 40xxx = the 55 "Consumables". The dummy's id (90xxx = key items) is not
    a displayed bucket, so a 90xxx item counts at the top level (notification) but
    never shows in a subsection. A **40xxx id** puts it in Consumables. `40156` is
    the next free id after the last vanilla journal consumable (40155).
  - **ENABLE must be 1** or the item is filtered out of the subsection listing
    (all vanilla journal items are ENABLE=1). ENABLE=0 does NOT block buying/using.
  Both ItemTable and DetailInfoItemTable are **sorted by id and keyed by id**
  (DetailInfo has an `itID` field), so append + write re-sorts them correctly — no
  positional alignment needed, but the detail row's `itID` MUST equal the item id.
- `Paramater/DetailInfoItemTable.btb` — 597 → 598, MG name/description appended
  with `itID=40156`.
- `Colony/PlantParameter.btb` — colony/Adventurer shop slot (rec 8) sells id **40156**.
- `Paramater/DNoteItemTable.btb` — D's Journal item map, grown 314 → **315**
  (appends `ID 40156 -> INDEX 314`). Built from vanilla JSON (pure ints), verified.
- `DReportTable/DTableItemData.btb` — the ACTUAL journal category table (fields
  `index`, `pText`, `category`); grown 314 → **315** appending
  `{index: 315, category: 16 (consumables), pText: <flavor text>}`. Ghidra Round 8
  (`UpdatePictureBookItemCategory`) showed the category lists/counts are built from
  THIS table (iterated by array count), not DNoteItemTable — which is why earlier
  attempts got a notification but no listing. `DTableItemData.index` (1-based)
  links to `DNoteItemTable.INDEX` (0-based) offset by 1, so index 315 ↔ INDEX 314.

**Verify in-game (each observable tests a different thing):**
1. **Game boots / loads a save without crashing** → a 598-row ItemTable loads at
   all (the core "can we grow ItemTable" question).
2. **Adventurer / Trader Village (colony) shop** shows **Magnifying Glass (20 pg)**
   → the appended ItemTable row is loaded AND looked up by ID from the shop.
3. Buy it; its **menu name/description** read "Magnifying Glass" / "Displays
   various information about an enemy…" → the DetailInfoItemTable `itID=40156` row
   is found by id.
4. **Use it in battle** → triggers Examine (enemy HP/weaknesses) → the appended
   item is fully functional.
5. **D's Journal → Items → Consumables** → the Consumables total goes 55 → **56**
   and the Magnifying Glass is listed (as its flavor text, or "???" until obtained)
   → the DTableItemData append is the piece that actually populates the subsection.
- Any of these failing (esp. #1 a crash on load) → ItemTable append isn't safely
  honored; keep the dummy-repurpose approach for new items. Note which step fails.

---

## If it works
Fold the append approach into the real combined mod (drop the destructive
potion-replace and the dummy-repurpose). If only Test 1 works, use append for
shops but keep dummy-repurpose for brand-new items.

## Notes
- The `full` ItemTable keeps the dummy (90050) intact and uses a fresh id (90051),
  so nothing vanilla is overwritten — purely additive.
- New items have **no D's Journal entry** (same limitation as the original
  dummy-repurpose mod).
- Restore: copy vanilla `Common_en` back, or Steam → Verify Integrity.
