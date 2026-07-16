# @runtime PyGhidra
# -*- coding: utf-8 -*-
"""
Ghidra script: labels functions/strings/metadata in GameAssembly.dll using
Il2CppDumper's script.json (from BDFFHD-dump/). Run via Window -> Script
Manager after GameAssembly.dll has been imported and fully auto-analyzed.

Adjust SCRIPT_JSON below if BDFFHD-dump/ lives somewhere else relative to
this repo.
"""
import json

from ghidra.program.model.symbol import SourceType

SCRIPT_JSON = r"/Users/vikash.balasubramani/Projects/BDFFHD/BDFFHD-dump/script.json"

USER_DEFINED = SourceType.USER_DEFINED
baseAddress = currentProgram.getImageBase()


def get_addr(rva):
    return baseAddress.add(rva)


def set_name(addr, name):
    createLabel(addr, name.replace(" ", "-"), True, USER_DEFINED)


def make_function(start):
    if getFunctionAt(start) is None:
        createFunction(start, None)


print("Loading script.json from: %s" % SCRIPT_JSON)
data = json.loads(open(SCRIPT_JSON, "rb").read().decode("utf-8"))
print("Loaded script.json successfully")

if "ScriptMethod" in data:
    methods = data["ScriptMethod"]
    monitor.initialize(len(methods))
    monitor.setMessage("Labeling %d methods" % len(methods))
    for m in methods:
        addr = get_addr(m["Address"])
        set_name(addr, m["Name"])
        make_function(addr)
        monitor.incrementProgress(1)
    print("Methods done: %d" % len(methods))

if "ScriptString" in data:
    strings = data["ScriptString"]
    monitor.initialize(len(strings))
    monitor.setMessage("Labeling %d strings" % len(strings))
    for i, s in enumerate(strings, start=1):
        addr = get_addr(s["Address"])
        createLabel(addr, "StringLiteral_%d" % i, True, USER_DEFINED)
        setEOLComment(addr, s["Value"])
        monitor.incrementProgress(1)
    print("Strings done: %d" % len(strings))

if "ScriptMetadata" in data:
    metas = data["ScriptMetadata"]
    monitor.initialize(len(metas))
    monitor.setMessage("Labeling %d metadata entries" % len(metas))
    for meta in metas:
        addr = get_addr(meta["Address"])
        set_name(addr, meta["Name"])
        setEOLComment(addr, meta["Name"])
        monitor.incrementProgress(1)
    print("Metadata done: %d" % len(metas))

if "ScriptMetadataMethod" in data:
    metaMethods = data["ScriptMetadataMethod"]
    monitor.initialize(len(metaMethods))
    monitor.setMessage("Labeling %d metadata methods" % len(metaMethods))
    for mm in metaMethods:
        addr = get_addr(mm["Address"])
        set_name(addr, mm["Name"])
        setEOLComment(addr, mm["Name"])
        monitor.incrementProgress(1)
    print("Metadata methods done: %d" % len(metaMethods))

if "Addresses" in data:
    addrs = data["Addresses"]
    monitor.initialize(len(addrs))
    monitor.setMessage("Creating %d functions" % len(addrs))
    for a in addrs:
        make_function(get_addr(a))
        monitor.incrementProgress(1)
    print("Functions done: %d" % len(addrs))

print("IL2CPP label import complete!")
