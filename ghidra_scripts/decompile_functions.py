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

FUNCTION_NAMES = [
    # --- Round 1: data model (analyzed 2026-07-16, see RESEARCH_NOTES.md) ---
    "PassengerManager$$IncomingCOM",
    "PassengerManager$$Doit",
    "PassengerManager$$TIME_CHECK_IMPL",
    "PassengerManager$$GenComTowns",
    "PassengerManager$$SetFsFriend",
    "TownFunction$$DeleteThis",

    # --- Round 2: orchestrator / display trigger (analyzed 2026-07-16) ---
    "MB_PassThroughNPC$$CheckOverlap",   # despawns ghosts too close to player
    "MB_PassThroughNPC$$Gen",            # builds one ghost visual (no Doit)
    "MB_PassThroughNPC$$GetReady",       # spawns a ghost visual
    "MB_PassThroughNPC$$Update",         # per-frame driver
    "MB_FieldUI$$PassingEachOther",      # player crosses a soul
    "MB_FieldUI$$OnPassingEachOther",    # Doit() -> AddReinforcer(1) = +1 pop
    "PassengerManager$$UpdateService",   # online/network service tick (not spawner)
    "PassengerManager$$NEXT_SET",        # cadence wrapper (c_setPassengerSpan)
    "PassengerManager$$Loaded",          # rebuilds COMS from guest list on load
    "PassengerManager$$GetCount",        # orchestrator: min(TOWN_PS_LEFT[town], pool)
    "PassengerControl$$WriteRead",       # save (de)serialization of the record

    # --- Round 3: cadence-span value ---
    # static readonly TimeSpan c_setPassengerSpan is set in the static ctor;
    # decompiling it should reveal the actual refresh period (ticks/args).
    "PassengerManager$$.cctor",
]

# --- Round 3: recover the call graph for the two open unknowns ---
# Who calls GetCount() (how often -> per-visit vs per-frame), and who calls the
# ghost spawner GetReady()/CheckOverlap() (the visual passing-soul cadence).
CALLERS_OF = [
    "PassengerManager$$GetCount",
    "MB_PassThroughNPC$$GetReady",
    "MB_PassThroughNPC$$CheckOverlap",
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
