# @runtime Jython
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
    "PassengerManager$$IncomingCOM",
    "PassengerManager$$Doit",
    "PassengerManager$$TIME_CHECK_IMPL",
    "PassengerManager$$GenComTowns",
    "PassengerManager$$SetFsFriend",
    "TownFunction$$DeleteThis",
]

OUTPUT_PATH = r"/Users/vikash.balasubramani/Projects/BDFFHD/VoidFault/ghidra_scripts/decompiled_output.txt"

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
