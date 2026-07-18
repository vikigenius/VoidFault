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

# Passenger-souls investigation (rounds 1-3) is complete -- shipped, and its
# decompiled_output is in git history. Left here commented for provenance:
#   PassengerManager$$IncomingCOM/Doit/TIME_CHECK_IMPL/GenComTowns/SetFsFriend/
#   UpdateService/NEXT_SET/Loaded/GetCount/.cctor, TownFunction$$DeleteThis,
#   MB_PassThroughNPC$$CheckOverlap/Gen/GetReady/Update,
#   MB_FieldUI$$PassingEachOther/OnPassingEachOther, PassengerControl$$WriteRead
#   (+ CALLERS_OF GetCount/GetReady/CheckOverlap -> TownFunction$$UpdatePhase).

FUNCTION_NAMES = [
    # --- Round 5: weapon-special "Spirit" (SP) reset-on-equip investigation ---
    # SPIRIT is CharacterState.SPIRIT (int[] per hand, 0..1000); actions charge
    # it (CheckSpiritDefault etc.), the weapon special unlocks when it's full.
    # Hypothesis: equipping a weapon zeroes that hand's Spirit -> lost progress.
    # Confirm the reset site before patching.
    "CharacterState$$SetRHAND",     # right-hand equip setter (suspected reset)
    "CharacterState$$SetLHAND",     # left-hand equip setter
    "CharacterState$$SetSPIRIT",    # the Spirit setter/zeroer
    "CharacterState$$GetSPIRIT",    # reader (for reference)
]

# Recover every place Spirit is set/zeroed -- especially the equip reset that
# clears a hand's gauge on weapon change.
CALLERS_OF = [
    "CharacterState$$SetSPIRIT",
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
