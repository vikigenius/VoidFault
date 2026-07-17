# Research notes: Norende Colony population / Passing Souls

Investigation log for "can we make villagers/Passing Souls appear in town on a
reasonable cadence." Kept so the next attempt doesn't restart from zero — this
took a lot of signature-only guessing before finding real dead ends.

## Terminology

Three names for the same underlying entity, at different layers:

- **"Passing Soul"** — the actual in-game/localization term. Confirmed via
  `BDFFHD-Modding/tools/dump/Common_en/MessageTable/MenuMessageData.json`,
  ID 456: `"Update Passing Soul Data"`.
- **"Passenger"** — the internal code name (`PassengerManager`, `FriendState`,
  `PassengerControl`).
- **"Ghost"** — informal description of how they look in the world (translucent
  visitor NPC). Not a term the game's own strings use anywhere.

Important distinction found late: a `FriendState` record can be classified as
either a **Friend** (battle-summon-eligible, `GetFriendSummoner()`,
`GetFriend()`/`SetFriend(bool)`) or a **Guest** (population-only,
`IsGuestPlayerType()`). Same underlying class, two different roles. The
in-game Friend Menu has separate "Friend Menu" and "Guest Menu" sections
(`MenuMessageData.json` IDs 440/441).

## Confirmed facts

- `ColonyData.population` (public `int` field) is the actual villager count.
  `COLONYDATA_POPULATIONMAX = 999`, `COLONYDATA_POPULATIONDEFAULT = 1`.
- Population growth is at least partly **story-driven**: found
  `AMX_AddColonyReinforcer(AmxParameter p)` — an `AMX_`-prefixed function,
  meaning it's callable from the game's story/event scripting system. Specific
  plot beats very plausibly grant villagers directly, independent of any
  online mechanic.
- Recruiting via the in-game Friend Menu is the normal, confirmed-working
  player path — 3 villagers (1 friend bot + 2 guests) were recruited this way
  in town 1, entirely through normal play.
- The "Update via Internet" / "Update via Local Wireless" / "Update Passing
  Soul Data" strings exist in the data (`MenuMessageData.json` 454–458) but
  have **no wired-up UI** in this remaster — confirmed by checking in-game,
  no such button exists anywhere in the actual Friend Menu. Leftover 3DS
  localization text (3DS had real "Local Wireless"/StreetPass hardware; Steam
  doesn't).
- The game **explicitly tells the player Guests turn off while offline** —
  confirmed, by-design behavior. Not caused by mods, not an anti-tamper/DRM
  block.
- Population, once earned, **persists regardless of online/offline status**.
- The passenger runtime state is a save-serialized record, `PassengerControl`
  (`IBSIO`), reachable at `Hikari+0x138`. Its fields (offsets from `dump.cs`):
  `LEFT_COMS` @0x10 (int, global COM counter), `NEXT_UPLOAD` @0x18,
  `NEXT_SET` @0x20 (DateTime cadence deadlines), `TOWN_PS_LEFT` @0x28
  (`sbyte[]`, per-town budget of souls still to reveal), `COMS` @0x30
  (`Stack<FriendState>[]`, per-town offline-dummy pool). `PassengerManager`'s
  `TOWN_PS_LEFT`/`COMS`/`LEFT_COMS`/`NEXT_SET` properties just proxy into this.
- Offset → field map for the decompiled functions (from `dump.cs` layout):
  `PassengerManager+0x88` = `bCOMS_MODE` (offline dummy mode gate),
  `+0xa8` = `bUseNet` (online mode gate), `+0x98` = `m_incomings`
  (`List<UserMetaCore>[]`, per-town online download pool),
  `+0x90` = `m_friends`. Cadence spans: `c_setPassengerSpan` @0x30,
  `c_downloadFriendSpan` @0x38 (TimeSpan consts).

## What we tried, in order

1. **Hypothesis:** code mods block internet access needed for Passing Soul
   recruitment. **Test:** fully disabled BepInEx via `doorstop_config.ini`
   (`enabled=false`), tested in-game (leave/re-enter town). **Result:** still
   no new passengers, fully vanilla. Mods are not the cause.
2. Investigated `PassengerManager` in `dump.cs` — found the online/offline
   "COM" (dummy passenger) system: cadence timers (`TIME_CHECK_IMPL` +
   `c_setPassengerSpan`/`c_downloadFriendSpan`/etc.), `EnableTowns`/
   `EnableTownFlags` (per-town story gating, values not readable from
   signatures alone), `TOWN_PS_LEFT`/`LEFT_COMS`/`COMS` (a per-town pool of
   pre-generated dummy `FriendState`s, sourced from `DummyPassengersTable`/
   `DummyPassengersJob` game data).
3. Attempted to Harmony-hook `TownFunction`'s constructor (fires on entering a
   town) to call `PassengerManager.IncomingCOM()` on our own cadence.
   **Failed at runtime**: Il2CppInterop couldn't initialize the patch
   (`"failed to init patch ... Derived classes must provide an
   implementation"`), silently fell back to a handler that never actually
   fires. Confirmed this is a **runtime** Il2CppInterop hook-init failure, not
   a compile error (build succeeds clean either way) — a known category of
   Harmony/IL2CPP interop limitation for specific methods/classes on this
   Unity 6 build (matches `bravely-default-mod`'s own documented experience:
   several of their hooks needed raw native-pointer hooking instead of
   Harmony for the exact same reason).
4. Pivoted to hooking `TownFunction.DeleteThis()` (a regular instance method,
   not a constructor) instead. This **did** initialize and fire successfully
   — confirming the earlier failure was specific to hooking `.ctor()`, not
   `TownFunction` as a class.
5. Called `PassengerManager.IncomingCOM()` from that hook, logging
   `GetCount()` before/after plus `IsOnline`/`IsBusy`/`IsNegotiation`/
   `IsReadyFriendAndGuset`. **Result, perfectly repeatable across many
   cycles:** entering a town always logged `GetCount() 0 -> 0`; leaving a town
   always logged `GetCount() 0 -> 1`. All four status flags read `false`
   every time. Population never moved from 3 through any of this.
6. Initially misread the deterministic pattern as "IncomingCOM works even
   offline." **This was wrong** — population never changing doesn't
   invalidate that reading (population requires a separate, manual
   recruit-by-interacting step), but the *perfect determinism itself* is the
   real tell: real dummy-passenger generation, drawing from a finite/gated
   pool, should show at least some attempt-to-attempt variability. A count
   that flips the same way on the same transition, every single time, looks
   like simple state/lifecycle bookkeeping, not content generation.
7. **Decisive catch:** ghosts/passengers only exist inside towns — the World
   Map cannot spawn them. But the `0 -> 1` happens specifically when leaving
   *into* the World Map, where no ghost could possibly exist. That directly
   contradicts "this represents a spawned ghost."
8. **Current leading theory:** `GetCount()` most likely reflects the number
   of **Friends available to Friend-Summon** — a World-Map-specific battle
   mechanic (see `NemesisMessage00`–`12` in `ColonyShare.MessageData.
   MessageId` — Friend Summon is tied to Nemesis encounters while traveling).
   `0` in town (not a summon context), `1` on the map (matches having exactly
   1 registered Friend — the "friend bot"). If true, `IncomingCOM()`/
   `GetCount()` were never the right lever for colony population at all, and
   we were reading an unrelated system's counter.

## Decompile results (2026-07-16) — mechanism resolved

Lifted the `bravely-default-mod` Ghidra flow (`import_il2cpp_labels.py` then
`decompile_functions.py`) and decompiled six functions
(`BDFFHD-dump/decompiled_output.txt`). `dump.cs` is **symbols only** — method
bodies are empty stubs, so it gives layout/offsets but *no call graph*; callers
have to come from Ghidra. The decompile turned two prior guesses into facts:

- **The pipeline is two separate stages, previously conflated:**
  - `IncomingCOM()` = **assignment/plumbing, not spawn.** It clears every
    non-current town's `COMS` stack, walks the master `List<FriendState>` at
    `Hikari+0xa0`, and for each `IsGuestPlayerType()` guest pushes it onto a
    *random* enabled town's `COMS[town]` stack (town pool from
    `GenComTowns()`). It then writes the pushed count into `LEFT_COMS`.
    So **`GetCount()` == `LEFT_COMS` == guests-distributed-to-town-stacks** —
    not friend-summon count (theory #8 was wrong) and not content generation.
    With a near-empty guest pool offline it distributes ~nothing, which is why
    the counter barely moved. This confirms IncomingCOM was never the lever.
  - `Doit()` = **the atomic produce-one-passenger op** (the lever we wanted).
    Two branches, both keyed on current town index and both decrementing
    `TOWN_PS_LEFT[town]`:
    - Branch 1 — **COM/dummy path**, gated by `bCOMS_MODE`: requires
      `COMS[town]` non-empty and `TOWN_PS_LEFT[town] >= 1`, pops one
      `FriendState` off `COMS[town]`, `SetNewIcon(1)`, returns it.
    - Branch 2 — **online/download path**, gated by `bUseNet` + `bNonStash`:
      picks a random `UserMetaCore` from `m_incomings[town]`,
      `GenFromUserMetaCore()`, `RemoveAt`, decrements `TOWN_PS_LEFT`. New
      records route to **`GameData.SetFsNonFriend` (guest/population)** or
      `SetFsFriend` (friend roster) per the `b__9` check; existing records
      `UpdateState` + `RenewABILINK`.
- **Why nothing spawns offline is now concrete, not mysterious:** `Doit`
  returns `0` unless `TOWN_PS_LEFT[town] >= 1` **and** the town's `COMS`/
  `m_incomings` pool is non-empty. Offline the pool is empty → IncomingCOM
  fills no stacks → budget stays 0 → `Doit` no-ops. Data starvation, not a
  code block / DRM.
- **`SetFsNonFriend` is the safe guest/population registration** — the clean
  counterpart to the roster-poisoning `SetFsFriend`. Branch 2 shows the full
  legit guest flow: `GenFromUserMetaCore → SetNewIcon → SetFsNonFriend`.
- **`SetFsFriend` confirmed dangerous** (decompile lines 535–558): sets
  `Friendship=1` and calls `GameData.SetFsFriend` → writes the battle-summon
  roster. The earlier instinct to hold off was correct.
- `TIME_CHECK_IMPL(now, ref deadline, span, force)`: if `force`, sets
  `deadline = now + span`; returns `deadline < now | force`. Plain cadence
  gate — the `NEXT_SET`/`NEXT_DOWNLOAD`/`NEXT_FRIEND` wrappers drive it with
  `c_setPassengerSpan`/`c_downloadFriendSpan`.
- `GenComTowns()` confirms enabled towns come from a story-gated bool array
  (`BoolArray.get_Item`), empty-list fallback of `[0]`.
- `TownFunction.DeleteThis()` confirms note #7: on town exit it calls
  `MB_FieldUI.ClearPassenger` and resets the static flag at `TownFunction+0x31`
  — passengers are town-scene-only, torn down on exit; `MB_FieldUI` is the
  display side (consumer of `Doit()`'s output).

### Where all-towns-have-a-value nets out
`TOWN_PS_LEFT` (`sbyte[]`), `COMS` (`Stack[]`), `m_incomings` (`List[]`) are
per-town arrays sized to the town count in the `PassengerControl` save record,
so a **slot exists for every town**. But content is only populated for
story-**enabled** towns (`GenComTowns` filters them out otherwise). For our
patch this mostly *doesn't* matter: both the game and `Doit` index by the
**current** town (via the `FindIndex` predicate), so we always act on wherever
the player is — no need to enumerate towns, and blanket-seeding gated towns is
pointless (they're excluded from distribution/display). "Works in every town"
= yes, via current-town targeting.

## Open questions / where we left off

The data model is now fully understood; the remaining gap is the
**orchestrator**, and `dump.cs` can't close it (symbols only — re-run the
Ghidra decompile with an expanded `FUNCTION_NAMES` to get these bodies):

- **Who calls `Doit()`, and on what trigger/cadence?** This is what actually
  makes a soul appear in-town and hands the `FriendState` to `MB_FieldUI`.
  Likely `UpdateService()` (the tick loop running the `NEXT_SET` cadence)
  and/or a town/field-UI passenger controller. Decompile `UpdateService`,
  `NEXT_SET`, `GetCount`, and find `Doit`'s caller (look on the `MB_FieldUI` /
  town-passenger side).
- **Where does `TOWN_PS_LEFT[town]` get its budget set/replenished?** Only its
  *decrement* (in `Doit`) has been seen. This per-town budget is the most
  direct "how many souls per town" quantity lever — need the writer/initializer
  and its default value.
- **How does `COMS[town]` get seeded offline?** IncomingCOM only redistributes
  the existing `Hikari+0xa0` guest pool; the offline dummy source is a separate,
  not-yet-read step. NOTE: the earlier note about `DummyPassengersTable`/
  `DummyPassengersJob` is **unverified** — grep finds no `DummyPassenger*`
  symbol anywhere in this dump, so that name was likely misremembered. Find the
  real source when decompiling `UpdateService`/`Loaded`.

### Candidate levers for the "increase passing-soul rate" patch
Ranked, given what's confirmed vs still-guessed:
1. **Cadence span** (`c_setPassengerSpan` / `next_set`): global, town-agnostic,
   trivial to shrink — but only speeds *timing*; useless if the pool/budget is
   the real offline bottleneck (it is). Not sufficient alone offline.
2. **Per-town budget + seed** (write `TOWN_PS_LEFT[currentTown]`, seed
   `COMS[currentTown]` from DummyPassengers, set `bCOMS_MODE`, drive `Doit`):
   most direct "make N souls appear," but re-implements the orchestrator and
   still needs the confirmed display path (`Doit` → `MB_FieldUI` add-passenger).
   Do **not** ship this on signature guessing — that's how the last two dead
   ends happened. Decompile the orchestrator first, then hook it with a
   configurable multiplier.
- Simpler fallback still on the table, fully understood: directly increment
  `ColonyData.population`. Less immersive (no ghost/recruit flow) but a plain
  field we control.

## (superseded) earlier open questions

- Never found the actual passenger-spawns-into-a-town-scene logic.
  `TownEventLayoutCtrl` (tightly coupled to `TownFunction` via a private
  `m_pTownFunction` property) was a candidate but untested — it risks the
  same Il2CppInterop hook-init failure for the same underlying reason.
- `Doit()` (`public static FriendState Doit()`) and `SetFsFriend(FriendState)`
  are more atomic-looking candidates than `IncomingCOM()` — but `SetFsFriend`
  ("Fs" = "Friend State", matching a separate `m_FsFriendArray` seen
  elsewhere) is suspected to register into the **Friend** list specifically
  (battle-summon roster), not the Guest/population list. Real risk of writing
  bad/synthetic data into the actual Friend-Summon roster if used carelessly.
  **Not tested** — deliberately held off given that risk.
- Signature-only guessing has now produced two dead ends in a row
  (constructor-hook failure, then the wrong-mechanic misread on
  `IncomingCOM`/`GetCount`). Further progress on the "genuine, ghost-recruit"
  approach almost certainly needs actually reading decompiled logic — see
  `GHIDRA_SETUP.md`.
- Simpler fallback still on the table, no further investigation needed:
  directly increment `ColonyData.population` ourselves. No ghost/recruit
  flow, less immersive, but a plain field we fully understand and control.

## Reusable technique notes

- `dump.cs` (Il2CppDumper output) is **symbols/layout only** — field offsets,
  method signatures, type indices, but empty method bodies. Great for offset
  → field-name mapping (how the raw decompile addresses were resolved above),
  useless for call graphs. Callers/logic require the Ghidra decompile pass
  (`decompile_functions.py`, edit `FUNCTION_NAMES`), lifted from
  `bravely-default-mod`.
- IL2CPP method names typically use a `$$` separator once imported into
  Ghidra/IDA (`ClassName$$MethodName`).
- `AccessTools.TypeByName` + `AccessTools.Method`/`Constructor` (reflection-
  based Harmony targeting) sidesteps referencing certain complex
  IL2CPP-interop types directly via `typeof()` in C# source, which can be
  fragile for large/heavily-interfaced classes.
- `AccessTools.TypeByName`'s internal all-assembly type scan can throw (and
  Harmony catches) `ReflectionTypeLoadException` from a completely unrelated
  broken type in some *other* loaded assembly (e.g. a compiler-generated
  `<>c` closure in `UnityEngine.CoreModule`). This is benign noise from the
  scan itself, not a sign our own patch failed — check whether the intended
  type/method actually resolved before treating it as an error.
