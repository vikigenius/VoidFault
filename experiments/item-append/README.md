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
  (rec 596, id 90050) is **kept**; a new **Magnifying Glass** is **appended**
  (rec 597, **new id 90051**, TYPE 16 battle item, USE_ABI 101 = Examine).
- `Paramater/DetailInfoItemTable.btb` — 597 → 598, MG name/description appended
  at rec 597 (parallel index).
- `Colony/PlantParameter.btb` — colony/Adventurer shop slot (rec 8) now sells
  id **90051** (the appended Magnifying Glass).
- `Paramater/DNoteItemTable.btb` — D's Journal item map, grown 314 → **315**
  (appends `ID 90051 -> INDEX 314`) so the Magnifying Glass gets an encyclopedia
  entry. Reconstructed from vanilla JSON (pure ints, no strings), verified.

**Verify in-game (each observable tests a different thing):**
1. **Game boots / loads a save without crashing** → a 598-row ItemTable loads at
   all (the core "can we grow ItemTable" question).
2. **Adventurer / Trader Village (colony) shop** shows **Magnifying Glass (20 pg)**
   → the appended ItemTable row is loaded AND looked up by ID from the shop.
3. Buy it; its **menu name/description** read "Magnifying Glass" / "Displays
   various information about an enemy…" → the appended DetailInfoItemTable row is
   indexed correctly (parallel to ItemTable).
4. **Use it in battle** → triggers Examine (enemy HP/weaknesses) → the appended
   item is fully functional.
5. **D's Journal / encyclopedia** → the Magnifying Glass now has an item note
   (from the appended `DNoteItemTable` row + its `DetailInfoItemTable` text) →
   appending to the journal map works. (Buyable + usable were already confirmed;
   this step is the remaining unknown — the journal UI may have its own INDEX or
   count expectations.)
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
