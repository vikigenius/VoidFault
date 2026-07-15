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

## Open questions / where we left off

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
