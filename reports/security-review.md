# Security review, September 2026

A review of this fork, the 18 commits where it diverges from `fchb1239/PhotonVR`, and
everything the package sends over the network. All of it is fixed here.

Start with the part that matters most: **this fork did not compile.** Five hard
errors. The two worst bugs came from the fork rather than from upstream. So if
PhotonVR is running in your shipped game, it is not this repository.

| | Count |
|---|---|
| Fixed here | 22 |
| Secrets found in history | 0 |
| Upstream commits not yet taken | 0 |

---

## Why it did not compile

Commit `7601848` reverted `PhotonVRManager.cs` to the upstream lineage from August
2022. Every other file stayed on upstream `main` from August 2023. The two halves
then disagreed about how cosmetics work.

The 2022 manager used a `PhotonVRCosmeticsData` class that upstream deleted in
`5de9216`, thirteen months before the revert. Nothing here defines it. That alone is
four errors. The same revert deleted `CheckDefaultValues()`, which
`PhotonVRManagerGUI.cs:33` still calls, for a fifth. A sixth shows up on Unity 2021.2
and newer, where `PrefabStageUtility` moved namespace.

That split broke two contracts as well. The manager wrote cosmetics as a JSON
**string** while `Player/PhotonVRPlayer.cs:87` read them as a
**`Dictionary<string, string>`**, so the cast threw every time. And the manager saved
to `PlayerPrefs` as JSON while `Saving/PhotonVRValueSaver.cs`, still sitting there,
expected its own comma-joined format.

I fixed this by moving the manager forward to the dictionary API the rest of the
package already used, rather than restoring the deleted class. That touches one file
instead of six, leaves prefabs alone, and makes the README's cosmetics examples
correct again. They had been wrong since the revert.

---

## The manager never started

`PhotonVRManager.cs:54` declared `public static TextMeshPro LogText`. Unity does not
serialize static fields, so the Inspector could not assign it and nothing else did.
Null in every build.

`Start()` wrote to it on line 69. Four lines in. The manager threw before
`DontDestroyOnLoad`, before `Connect()`, before loading saved values. The package did
not connect at all.

It is an instance field now, and writes go through a `Status()` helper that copes
with it being unset.

---

## Any player could crash the room

Two bugs that only bite in multiplayer, which is presumably how they survived.

`Player/PhotonVRPlayer.cs:83` and `:87` cast client-authored values without checking
them. Photon custom properties are written entirely by the client that owns them. A
missing key gave `null`, a wrong type gave the wrong cast, and both threw. The method
runs from `Awake()` on every avatar spawn, so one malformed value broke rendering for
everyone else in the room.

`RPCRefreshPlayerValues` had no sender check. PUN does not validate who sent an RPC,
and any client can invoke one on any PhotonView it names. Put those two together and
one player can make every other client re-run the throwing code on demand, in a loop.
A `Debug.Log` sitting inside the cosmetics loop made each call cost more than it
looked.

The reason this fired in ordinary play is `PhotonVRManager.cs:283-284`. It mutated
the Hashtable that `CustomProperties` returns and never called
`SetCustomProperties`, so `Colour` and `Cosmetics` never reached the server. Every
remote player read a missing key. That one is in upstream too.

The reads now use `TryGetValue` with type checks and try/catch. The RPC takes a
`PhotonMessageInfo` and rejects anyone who is not the owner, with a cooldown on top.
The log is out of the loop, the RPC targets `Others` rather than `All` because the
sender does not need a round trip to update itself, and there is a real
`SetCustomProperties` call.

---

## Room codes

`new System.Random().Next(99999)` gave about 100,000 codes, unpadded, so anywhere
from one to five characters. On Unity's Mono runtime `System.Random()` seeds from
`Environment.TickCount`, so two clients whose matchmaking failed in the same tick
produced the same code. Matchmaking failures cluster, which makes that the ordinary
case rather than bad luck. Nothing overrode `OnCreateRoomFailed`, so a collision left
the client stuck in `ConnectionState.Error` with no retry and no avatar.

The namespacing was worse. `JoinPrivateRoom` and matchmade rooms shared one set of
names, and private rooms used the same short codes as public ones. `IsVisible = false`
hides a room from the lobby list but does nothing to stop a join by name. Anyone could
walk the code space into any live public room, in any queue, on any game version, and
skip the `queue` and `version` filters entirely.

Codes are now six characters from a 32-symbol alphabet, about 1.07 billion of them,
drawn from `RandomNumberGenerator`. The alphabet drops I, O, 0 and 1, because people
read these out loud. Public and private rooms carry different prefixes, and
`OnCreateRoomFailed` retries with a fresh code.

---

## Also fixed

**The duplicate-manager guard could never run.** `if (Manager == null)` / `else if
(StartInOfflineMode == true)` / `else if (== false)` / `else`. A bool has two values,
so the last branch was dead. That cost two things. A second manager was never
rejected and went on to connect again, and `StartInOfflineMode` only ever applied to
that duplicate, never to the real manager. It is a normal singleton check now, and it
destroys the duplicate instead of calling `Application.Quit()`. Quitting the game over
a scene-authoring mistake is worse than the mistake, and a scene reload with
`DontDestroyOnLoad` can trigger one.

**Display names could carry TMP markup.** The name came from `PlayerPrefs`, which is
editable on a sideloaded headset, and went straight into a TextMeshPro above the
player's head with rich text on and no length cap. A name carrying `<size=5000>` or
`<sprite>` renders on every other client in the room. It is stripped and capped at 16
characters going in, stripped again coming out, and `richText` is off on the
nameplate. The proper fix is to take the name from your PlayFab profile instead of
local storage.

Colours had no clamp either, so `{"r":99999,...}` produced a super-white material. On
a project with bloom or HDR that is a whiteout applied to everyone in the room. Now
clamped on write and again on read, since remote values arrive over the network
rather than from local prefs.

`MaxPlayers` went through an unchecked `int` to `byte` cast, so 256 wrapped to 0,
which PUN reads as unlimited. An uncapped room will take a Quest down. Clamped to
1-255. In a similar vein, `CreateRoom` could run with null options and fall back to
PUN's defaults, which also means no player cap. It falls back to real ones now.

Three lifecycle bugs. `SwitchScenes` never left the current room before joining a new
one, which PUN refuses outright, so it leaves first and joins from `OnLeftRoom`.
`PlayerSpawner.OnLeftRoom` called `PhotonNetwork.Destroy` after leaving, which PUN
also rejects, logging an error on every single room exit. PUN already cleans up
objects you instantiated, so that call is gone, along with a leak on rejoin. And
corrupt `PlayerPrefs` threw straight out of `Start()`, leaving the connection half
started. Both reads are wrapped now, and saved values load *before* connecting.
Loading them afterwards meant `OnConnectedToMaster` published defaults and threw away
whatever the player had chosen.

**`PhotonVRValueSaver` corrupted its own data.** It joined key names with commas, so
any key containing a comma broke the index. `("Cosmetic","s")` and `("Cosmetics","")`
wrote to the same pref. Reading an absent key split `""` into `[""]` and handed back a
dictionary holding one bogus entry instead of an empty one. Rewritten as a single JSON
blob.

`PhotonVRPlayerName` dereferenced two references every frame on every remote avatar
without checking either, turning one unassigned slot into an exception per frame per
player. It and `PhotonVRPlayerBody` also built quaternions from raw x/y/z/w
components, which does not give you a normalised rotation.

**The `Testing/` components shipped in release builds.** Their inspectors sat behind
`#if UNITY_EDITOR` but the components themselves did not. Both are guarded now.

**`Connect()` silently discarded custom authentication.** It sets
`PhotonNetwork.AuthValues = null`. With `ConnectOnAwake` defaulting to true, a project
that authenticates through PlayFab and then touches `Connect()` or `ChangeServers()`
drops to an anonymous connection with nothing logged. It warns now. If you
authenticate, set `ConnectOnAwake` to false and use `ConnectAuthenticated`.

Last one, small but telling: `Manager.State = ConnectionState.Generating_Roomcode;`
sat after a `return`, so `Generating_Roomcode` was a state the machine could never
enter.

---

## What held up

**No secrets, ever.** Full history of this fork and upstream, and there is no Photon
AppId, token, key or webhook in either. No `ProjectSettings/`, `PhotonServerSettings`
or `.asset` file has ever been committed on any branch.

**The fork fixed a real upstream leak.** Upstream logs the live Photon AppId and
VoiceAppId to the Unity player log, which anyone can read over adb on a Quest. Commit
`7601848` replaced that with `Debug.Log("Connecting")`. The README never mentions it,
which is a shame, because it is the one security improvement the fork made and it is
worth keeping.

Very little here touches the network at all. One RPC and one `Instantiate`. No
`AllBuffered`, no `RaiseEvent`, no `IPunObservable`, no ownership transfer, no
`AllocateViewID`, and no reflection, file I/O or dynamic loading at runtime. That
small footprint is why this review is as short as it is, and why the fixes above are
mostly about validating what arrives rather than reducing what is exposed.

Nothing to pull from upstream either. `fchb1239/PhotonVR` has not committed since
2023-08-09, which is exactly the fork point. And the fork never weakened
authentication along the way. `ConnectAuthenticated` matches upstream's.

---

## Still open

**Nothing checks cosmetic ownership.** The pref file, `SetCosmetic` and the custom
property are client-authored end to end, so any player can equip any cosmetic id for
free. PhotonVR cannot fix this on its own. Your game has to check the equipped set
against its own inventory before calling `SetCosmetics`, and remote clients should
render only ids confirmed against a server-signed entitlement.

**`Application.version` as a matchmaking filter is client-authored** and a modded APK
can say whatever it likes. That is inherent to client-side Photon matchmaking, and
only server-side room-creation webhooks fix it.

**Photon's RPC allow-list is not configured.** Turn it on in the PUN settings. Until
you do, every `[PunRPC]` in your whole project is callable by anyone in the room, not
only the one in this package.

---

## How this was checked

I compiled two assemblies with Roslyn against stand-ins for the Unity, PUN2 and
TextMeshPro APIs. One covers the runtime scripts. The other covers the editor scripts
under `UNITY_EDITOR` with Unity 2021.2+ defines. Before the fixes the runtime
assembly produced four errors and the editor assembly five. Both now build with no
errors and no warnings.

33 assertions cover the room code space and alphabet, display name sanitisation,
colour clamping, room size clamping, room name prefixes, and the saved-cosmetics
round trip including comma-containing keys and corrupt input. They compile the real
files in place, so they always test what is committed:

```
cd Tests~ && dotnet run --project test.csproj
```

Writing them caught a stub of mine that returned its input instead of clamping, which
had three assertions passing for the wrong reason. Worth admitting, because it is the
whole argument for running tests rather than reading code and nodding.

**Not verified by execution.** I never stood up a Photon room. So I reasoned the
runtime consequences from Unity and PUN2 semantics rather than watching them happen:
the `Start()` exception aborting `Connect()`, the cast failures on remote clients, PUN
doing no owner validation on RPC dispatch, and `System.Random` seeding from the clock
on Mono. That last one is also why the tests demonstrate the narrow code space instead
of the seeding collision. .NET 8 seeds `System.Random` randomly, so a test project
running there cannot reproduce what Unity does.

---

Reviewed against upstream [fchb1239/PhotonVR](https://github.com/fchb1239/PhotonVR)
at `67a5888`. Anything marked as also present upstream is not this fork's doing.
