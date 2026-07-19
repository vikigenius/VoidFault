# @runtime PyGhidra
# -*- coding: utf-8 -*-
"""
Ghidra script: decompiles functions to a plain text file.

Two modes, both write to OUTPUT_PATH:
  1. FUNCTION_NAMES -- exact IL2CPP labels (ClassName$$MethodName) to decompile.
  2. CALLERS_OF     -- for each label, use Ghidra's reference DB to find every
     function that CALLS it and decompile those. This is how we recover the
     call graph: dump.cs is symbols-only (empty method bodies), so it can't tell
     us who calls a function -- but Ghidra's analysis can.

Run via Window -> Script Manager, after import_il2cpp_labels.py has been run
once (so functions are named). Edit the lists / OUTPUT_PATH as needed.
"""
from ghidra.app.decompiler import DecompInterface

# CONVENTION: rounds ACCUMULATE -- keep each round's targets in the lists below and
# just append new ones. Decompiling is fast and the output isn't large, so we favor
# one cumulative decompiled_output.txt over per-round churn. Only remove a round's
# targets when it's explicitly settled (then note it in the provenance block below).
# Rounds 1-6 were removed under that rule before this convention; kept as comments.

# Passenger-souls investigation (rounds 1-3) is complete -- shipped, and its
# decompiled_output is in git history. Left here commented for provenance:
#   PassengerManager$$IncomingCOM/Doit/TIME_CHECK_IMPL/GenComTowns/SetFsFriend/
#   UpdateService/NEXT_SET/Loaded/GetCount/.cctor, TownFunction$$DeleteThis,
#   MB_PassThroughNPC$$CheckOverlap/Gen/GetReady/Update,
#   MB_FieldUI$$PassingEachOther/OnPassingEachOther, PassengerControl$$WriteRead
#   (+ CALLERS_OF GetCount/GetReady/CheckOverlap -> TownFunction$$UpdatePhase).

# Round 5 (weapon-special "Spirit" reset-on-equip) is complete -- confirmed the
# reset is UIRoot.Equipment.FinisherSpiritsCheck; that output is in git history.

# Round 6 (battle-results EXP/JP display vs reward mismatch) is complete -- output
# in git history. Involved BtlSequenceCtrl$$CreateResultData, BtlResultCtrl$$
# ReviseAddEXP/ReviseAddJEXP (+ CALLERS_OF those -> BtlResultCtrl$$Update).

FUNCTION_NAMES = [
    # --- Round 7a: Black Resonance scaling for a LONE user (data-mod question) ---
    # Black Resonance is one ability (1141), Black Mage's specialty, learned at
    # job Lv8. Its black-magic damage bonus lives in CorrectionData.magSympathy[0..3],
    # indexed by number of OTHER allies (excluding self) that have it set:
    #   vanilla [10,110,115,120] = 0 others "no effect", then x1.10/1.15/1.20.
    # The _0 slot looks dead: a naive multiply by 10/100 would nuke a solo user to
    # x0.10, which does NOT happen in vanilla -> the solo case must be guarded.
    # The BlackResonance ref-mod sets _0 -> 110 hoping a lone Black Mage gets x1.1.
    # QUESTION: does GetMagicSympathy read magSympathy[0] and apply it when
    # otherCount==0 (mod works), or hard-return 1.0 for the solo case (mod's _0
    # edit is inert)? Read the branch on the ally count and how the array is indexed.
    "BtlCharaManager$$GetMagicSympathy",   # RVA 0x9B0210, returns float

    # --- Round 7b: where "% Up" stat passives actually apply (data-mod question) ---
    # The passive "Speed 10/20/30% Up" (Thief, IDs 1522/1523/1528) and
    # "Evade 10/20/30% Up" (Ninja, IDs 1222/1225/1227) carry NO value in
    # SupportAbility.btb -- every CHANGE_* field is 100 (no-op), and there is no
    # SupportType enum entry for a generic speed/evade up. So the effect must be
    # applied in native stat-calc, keyed by ability ID. Confirm that and read the
    # exact percentages / how the equipped-ability list is scanned.
    # GOAL: know whether the effect is ID-hardcoded (=> can't repurpose an ability's
    # effect via a pure data mod, only its name/doc/cost/icon; effect change needs
    # a code mod or a JobTable learn-slot swap to an already-working ability).
    "CharacterState$$GetAGI",              # RVA 0x63A800 - final agility (how STATUSUP feeds in)
    "CharacterState$$GetDOD",              # RVA 0x63CC70 - dodge/evasion (Evade % Up) -- CONFIRMED:
                                           # hardcodes Evade IDs 1222/1225/1227 x1.1/1.2/1.3.
    # GetSTATUSUP_AGI turned out to be a trivial getter (return field 0xf0), and its
    # setter is likewise a folded one-liner Ghidra won't surface (CALLERS_OF NOT FOUND).
    # So the "Speed % Up" (1522/1523/1528) logic must be in SetParam (computes params,
    # has an isIgnoreSpecial flag) or applied inline in the action-speed getter the way
    # GetDOD applies Evade % inline. Check both for the hardcoded 1522/1523/1528 branches.
    "CharacterState$$SetParam",            # computes/applies stat params incl. specials
    "CharacterState$$GetACTSPD",           # RVA 0x63A720 - action speed (Speed % Up may apply here)

    # --- Round 7c: can we ADD rows to data tables? (item-mod question) ---
    # Item mods (MagnifyingGlass, BuyablePetalTokens) all assume "can't add new
    # rows", so they repurpose a dummy item / replace a shop slot (destructive).
    # But our baseline ItemTable dump had an APPENDED row ("Copilot's Edge"),
    # suggesting a row was added -- so verify whether the game honors it.
    # All BTBF tables load via the shared generic BTBdata.Builder<T>: if it reads
    # dataNum from the file header and allocates that many records, appended rows
    # load for ANY table for free (the only remaining risk is consumers that
    # hardcode a count or index a fixed range).
    "BTBdata$$Builder<ItemTable>",         # shared table loader - does it trust header dataNum?
    # Town item shops are .spb (BTBF) files loaded here; shows whether the shop
    # item count comes from the file (append-friendly) or a fixed expectation.
    "ShopDataTable$$LoadShopImpl",         # loads TW_*_Item.spb etc.
    # NOTE: Builder<T> is a shared generic body; if the <ItemTable> label doesn't
    # resolve, decompile any BTBdata$$Builder<...> instantiation - same native code.
]

# Callers of GetMagicSympathy show where the multiplier is applied to black-magic
# damage -- confirms the array indexing / any solo-case guard in situ.
CALLERS_OF = [
    "BtlCharaManager$$GetMagicSympathy",
    # (SetSTATUSUP_AGI CALLERS_OF removed: the setter is a folded one-liner Ghidra
    #  doesn't surface as a named function -> TARGET NOT FOUND. Chase Speed % via
    #  SetParam / GetACTSPD in FUNCTION_NAMES instead.)
]

# Functions the label import named but Ghidra never turned into a function object
# (they sit in code regions auto-analysis didn't fully carve up), so they come
# back NOT FOUND by name. Resolve them by RVA (from dump.cs / script.json) and
# create the function if one doesn't exist there. RVA is relative to the image
# base; VA = imageBase + RVA. (Empty now -- 0x544050 was just a trivial getter.)
BY_RVA = [
]

OUTPUT_PATH = r"C:\Users\maste\Documents\Modding\BDFFHD\BDFFHD-dump\decompiled_output.txt"

ifc = DecompInterface()
ifc.openProgram(currentProgram)

fm = currentProgram.getFunctionManager()
ref_mgr = currentProgram.getReferenceManager()

by_name = {}
for f in fm.getFunctions(True):
    by_name.setdefault(f.getName(), []).append(f)


def decompile_to(out, func, seen):
    key = func.getEntryPoint().toString()
    if key in seen:
        out.write("=== %s @ %s (already decompiled above) ===\n\n" % (func.getName(), key))
        return
    seen.add(key)
    out.write("=== %s @ %s ===\n" % (func.getName(), key))
    result = ifc.decompileFunction(func, 60, monitor)
    if result.decompileCompleted():
        out.write(result.getDecompiledFunction().getC())
    else:
        out.write("DECOMPILE FAILED: %s\n" % result.getErrorMessage())
    out.write("\n\n")
    print("Decompiled: %s" % func.getName())


def find_callers(func):
    callers = {}
    for ref in ref_mgr.getReferencesTo(func.getEntryPoint()):
        caller = fm.getFunctionContaining(ref.getFromAddress())
        if caller is not None:
            callers[caller.getEntryPoint().toString()] = caller
    return list(callers.values())


with open(OUTPUT_PATH, "w") as out:
    seen = set()

    for name in FUNCTION_NAMES:
        matches = by_name.get(name)
        if not matches:
            out.write("=== %s: NOT FOUND ===\n\n" % name)
            print("NOT FOUND: %s" % name)
            continue
        for func in matches:
            decompile_to(out, func, seen)

    for label, rva in BY_RVA:
        addr = currentProgram.getImageBase().add(rva)
        func = getFunctionAt(addr)
        if func is None:
            func = createFunction(addr, label)   # carve one out if analysis missed it
        if func is None:
            func = fm.getFunctionContaining(addr)  # fall back to enclosing function
        if func is None:
            out.write("=== %s @ rva %#x: NO FUNCTION (create failed) ===\n\n" % (label, rva))
            print("NO FUNCTION at %#x for %s" % (rva, label))
            continue
        decompile_to(out, func, seen)

    for name in CALLERS_OF:
        out.write("########## CALLERS OF %s ##########\n\n" % name)
        targets = by_name.get(name)
        if not targets:
            out.write("=== %s: TARGET NOT FOUND (cannot find callers) ===\n\n" % name)
            print("TARGET NOT FOUND: %s" % name)
            continue
        found_any = False
        for tf in targets:
            for caller in find_callers(tf):
                found_any = True
                decompile_to(out, caller, seen)
        if not found_any:
            out.write("=== no direct callers found for %s "
                      "(may be invoked only via a method-pointer table) ===\n\n" % name)
            print("No callers found for %s" % name)

print("Wrote output to %s" % OUTPUT_PATH)
