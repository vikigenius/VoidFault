# @runtime PyGhidra
# -*- coding: utf-8 -*-
"""
Ghidra script: decompiles a fixed list of functions (by their imported
IL2CPP label name, ClassName$$MethodName) and writes the pseudo-C output to
a plain text file. Run via Window -> Script Manager, after
import_il2cpp_labels.py has already been run once (so functions are named
and exist at all).

Edit FUNCTION_NAMES / OUTPUT_PATH below as needed for whatever you're
investigating next.
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

    # --- Round 2: the orchestrator / display trigger (remaining gaps) ---
    # Who calls Doit() and turns a FriendState into a visible passing-soul NPC.
    # MB_PassThroughNPC is the ghost NPC (SetGhostAlphaScale/FadeOut); these are
    # the prime suspects for calling PassengerManager.Doit():
    "MB_PassThroughNPC$$CheckOverlap",   # static; likely the field-side spawn/cadence check
    "MB_PassThroughNPC$$Gen",            # builds one NPC -- expected Doit() caller
    "MB_PassThroughNPC$$GetReady",       # readies a passing NPC
    "MB_PassThroughNPC$$Update",         # per-frame driver
    # Walk-into-a-soul recruit path (the actual "recruit" moment):
    "MB_FieldUI$$PassingEachOther",
    "MB_FieldUI$$OnPassingEachOther",

    # Cadence tick + per-town budget (TOWN_PS_LEFT) source. TOWN_PS_LEFT has no
    # setter of its own, so PassengerManager writes it directly -- find where:
    "PassengerManager$$UpdateService",   # service tick running the NEXT_* cadence timers
    "PassengerManager$$NEXT_SET",        # cadence wrapper (c_setPassengerSpan / next_set)
    "PassengerManager$$Loaded",          # may init TOWN_PS_LEFT budget on save load
    "PassengerManager$$GetCount",        # confirm GetCount() == LEFT_COMS

    # Save (de)serialization of the PassengerControl record -- shows how
    # COMS / TOWN_PS_LEFT are persisted and what their defaults are:
    "PassengerControl$$WriteRead",
]

OUTPUT_PATH = r"C:\Users\maste\Documents\Modding\BDFFHD\BDFFHD-dump\decompiled_output.txt"

ifc = DecompInterface()
ifc.openProgram(currentProgram)

fm = currentProgram.getFunctionManager()
all_funcs = list(fm.getFunctions(True))

with open(OUTPUT_PATH, "w") as out:
    for name in FUNCTION_NAMES:
        matches = [f for f in all_funcs if f.getName() == name]
        if not matches:
            out.write("=== %s: NOT FOUND ===\n\n" % name)
            print("NOT FOUND: %s" % name)
            continue
        for func in matches:
            out.write("=== %s @ %s ===\n" % (func.getName(), func.getEntryPoint()))
            result = ifc.decompileFunction(func, 60, monitor)
            if result.decompileCompleted():
                out.write(result.getDecompiledFunction().getC())
            else:
                out.write("DECOMPILE FAILED: %s\n" % result.getErrorMessage())
            out.write("\n\n")
            print("Decompiled: %s" % name)

print("Wrote output to %s" % OUTPUT_PATH)
