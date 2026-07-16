# Ghidra setup for BDFFHD (GameAssembly.dll)

Il2CppDumper (`BDFFHD-dump/`) only gives us type/method **signatures** — names, fields, RVAs — never bodies, because BDFFHD is IL2CPP: the actual game logic is AOT-compiled to native x64 inside `GameAssembly.dll`, not IL. There's no bytecode left to decompile with a .NET tool like dnSpy. To read real method logic, we need a native decompiler pointed at the binary itself. This is that setup.

See `RESEARCH_NOTES.md` for why we ended up needing this (multiple signature-only guesses on the Colony/Passenger system hit dead ends).

## Prerequisites

- **Ghidra** — free, from https://ghidra-sre.org
- **A JDK** — check the Ghidra release notes for the exact minimum version it needs (this has changed across Ghidra versions; recent releases want JDK 17 or 21)
- **`GameAssembly.dll`** — from the actual game install (`<GameDir>\GameAssembly.dll`, same one `il2cpp.h`/`dump.cs`/`script.json` in `BDFFHD-dump/` were generated from)
- The existing `BDFFHD-dump/` output (`script.json`, `il2cpp.h`) — already in this repo tree, no need to regenerate

## Steps

1. **Install Ghidra + JDK**, launch Ghidra.
2. **File → New Project** (Non-Shared is fine for solo use).
3. **File → Import File**, select `GameAssembly.dll`. Ghidra should auto-detect it as a PE, x86-64. Accept the defaults (Language should read `x86:LE:64:default`).
4. **Double-click the imported binary** to open it in CodeBrowser. When prompted to analyze, say yes and use the default analyzers.
5. **Let full auto-analysis run.** This is the expensive part — budget several hours for a binary this size (the struktured-labs project's own notes cite ~5 hours / 18,430 seconds analyzing this exact file). Run it overnight rather than waiting on it.
6. **Import IL2CPP names** once analysis finishes: **Window → Script Manager**, add `ghidra_scripts/` (this repo) as a Script Directory, find `import_il2cpp_labels.py` in the list, and run it. It reads `script.json` and labels every method/string/metadata entry it can — also a long step given ~200K methods in this binary, but far shorter than the initial analysis. (On Ghidra 12.x this needs the `# @runtime PyGhidra` directive — see the gotcha section below if you hit a Python/Jython error here.)
7. **Find a function by name**: press `G` (Go To) and type the label, or open **Window → Symbol Table** and filter. Names come from `script.json`, which typically uses the `ClassName$$MethodName` convention for IL2CPP methods (e.g. `PassengerManager$$IncomingCOM`).
8. **Read the logic**: select the function, the **Decompile** panel shows Ghidra's C-like pseudocode. It won't look like clean C# — generic variable names, unrolled control flow — but the actual branches, comparisons, and field accesses are real.

## Tips

- Save the Ghidra project (it auto-saves as a `.gpr`); keep the project directory around so you never redo the multi-hour analysis pass for the same binary.
- If a function doesn't turn up by name search (label import can occasionally miss a symbol), fall back to the RVA in the matching `dump.cs` comment (e.g. `// RVA: 0x740FB0`) — after import, cross-reference addresses directly via Go To.
- Cross-references (**right-click a function → Show References To**) are how you find every caller of something like `PassengerManager.IncomingCOM()` — useful for figuring out what's actually supposed to invoke it, and from where.

## Ghidra 12.x and Jython — the full chain of gotchas

Ghidra 11.3+ made PyGhidra (real CPython 3) the default Python engine. This was a multi-step fight to get working on Windows; recording the whole chain since any one of these steps alone doesn't fully explain the errors you'd see.

1. **Running a classic Jython-style script unmodified** fails with `"Ghidra was not started with PyGhidra. Python is not available"` — it silently gets routed to PyGhidra with no Python bridged in.
2. **Adding `# @runtime Jython` as the script's first line** (the tempting fix) doesn't help on Ghidra 12.x — Jython isn't bundled by default anymore, so this just produces a clearer error: `"In order to use Jython based scripts you must install the Jython Ghidra Extension, or (recommended) port your script to PyGhidra or Java."` Take Ghidra's recommendation.
3. **Actual fix, part 1 — port the script.** Both scripts in `ghidra_scripts/` start with `# @runtime PyGhidra` instead. PyGhidra scripts get the same flat-API globals Jython did (`currentProgram`, `monitor`, `createLabel`, `getFunctionAt`, `createFunction`, `setEOLComment`) automatically. Two real porting gotchas found along the way:
   - Drop any `.encode("utf-8")` calls on strings passed into Ghidra's Java API — under real Python 3 that's a `bytes` object where the API wants `str`. (Neither script needed this fix, but it's the most likely next thing to bite.)
   - **Bare Java package references fail** (`NameError: name 'ghidra' not defined`). Jython auto-exposes any Java package as a bare dotted name (`ghidra.program.model.symbol.SourceType.USER_DEFINED` just worked); PyGhidra doesn't — you need an explicit `from ghidra.program.model.symbol import SourceType` first, same as any normal Python import. `import_il2cpp_labels.py` needed this fix for `SourceType`.
4. **Actual fix, part 2 — get a real Python bridged into Ghidra at all.** Even with the script ported correctly, you'll still hit `"Ghidra was not started with PyGhidra. Python is not available"` until an actual CPython 3 interpreter with the `pyghidra` package is available and discoverable when Ghidra launches:
   - Install `pyghidra` offline, from Ghidra's own bundled wheel (no internet needed): `uv pip install --no-index --find-links "<GhidraInstallDir>\Ghidra\Features\PyGhidra\pypkg\dist" pyghidra` (or plain `pip install` with the same `--no-index -f <path>` flags if you have pip).
   - This needs a working Python 3 + a C++ toolchain if it has to compile a dependency (`jpype1`) from source. Picking a mainstream Python version (3.12 was used here) usually avoids that entirely, since `jpype1` ships pre-built Windows wheels for most current versions — check before assuming you need to install Visual Studio's "Desktop development with C++" workload.
   - `uv python install 3.12 --default` (uv 0.8+) makes that Python genuinely PATH-discoverable system-wide (Windows Registry via PEP 514), not just visible inside an activated project venv — install `pyghidra` into *that* environment (`uv pip install --system ...`), then launch `ghidraRun.bat` from the same terminal session so its PATH-based Python detection has the best chance of finding it.
