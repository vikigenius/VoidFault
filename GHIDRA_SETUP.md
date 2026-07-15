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
6. **Import IL2CPP names** once analysis finishes: **Window → Script Manager**, create a new script (Python/Jython), paste in `ghidra_scripts/import_il2cpp_labels.py` from this repo, and run it. It reads `script.json` and labels every method/string/metadata entry it can — also a long step given ~200K methods in this binary, but far shorter than the initial analysis.
7. **Find a function by name**: press `G` (Go To) and type the label, or open **Window → Symbol Table** and filter. Names come from `script.json`, which typically uses the `ClassName$$MethodName` convention for IL2CPP methods (e.g. `PassengerManager$$IncomingCOM`).
8. **Read the logic**: select the function, the **Decompile** panel shows Ghidra's C-like pseudocode. It won't look like clean C# — generic variable names, unrolled control flow — but the actual branches, comparisons, and field accesses are real.

## Tips

- Save the Ghidra project (it auto-saves as a `.gpr`); keep the project directory around so you never redo the multi-hour analysis pass for the same binary.
- If a function doesn't turn up by name search (label import can occasionally miss a symbol), fall back to the RVA in the matching `dump.cs` comment (e.g. `// RVA: 0x740FB0`) — after import, cross-reference addresses directly via Go To.
- Cross-references (**right-click a function → Show References To**) are how you find every caller of something like `PassengerManager.IncomingCOM()` — useful for figuring out what's actually supposed to invoke it, and from where.
