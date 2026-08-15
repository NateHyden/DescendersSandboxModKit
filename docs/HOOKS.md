# DescendersSandbox — Assembly-CSharp hook map

**Purpose:** When a game update renames obfuscated members, feed this doc (and/or `hooks.json`) to an AI or human so they can remap broken features without rediscovering everything from scratch.

**Companion file:** [`hooks.json`](hooks.json) — same records, machine-readable.

**Last inventoried:** 2026-08-15 (mod ~1.8.0 era). Re-run inventory after big game patches.

---

## How to report a breakage (paste this to the AI)

Copy the template below. Attach whatever you have. More signal = faster fix.

```text
GAME UPDATE / BROKEN FEATURE REPORT
===================================
Mod version:          (e.g. 1.8.0)
Game / Melon version: (if known)
Platform:             Steam | Xbox Game Pass
What broke:           (feature name(s) from the menu)
Symptoms:             (null, silent no-op, MelonLogger error text, NRE, …)
When:                 (menu open / map load / bail / multiplayer / …)

AssemblyScanner:
- Ran? yes/no
- Path: UserData/DescendersModMenu/AssemblyReport.txt
- Paste FAILED / WARN lines here

Logs:
- Paste MelonLoader console lines / Latest.log snippets for the feature

dnSpy / dump (optional but gold):
- Old name from HOOKS.md id: …
- New candidate name(s) you found: …
- Declaring type: …
- Signature (params / return / static?): …
- Nearby stable anchors (readable fields, string keys, joint names): …

hooks.json ids that might be involved:
- (e.g. vehicle.acceleration, photon.inRoom)

I want you to: remap the hook(s) and patch the listed source_files.
```

### What the AI should do with that report

1. Look up each **id** in `hooks.json` / sections below.
2. Prefer **fingerprint** over the old `current_name` (names lie after patches).
3. Prefer **high-stability anchors** first: English API names, Prefs keys, Transform names (`backWheel_Jnt`), `laxjiuc`, `GameModifier[].name` strings, method signatures.
4. Run / cite `AssemblyScanner` failures as the priority list.
5. Change the string(s) in `source_files`, keep discovery-by-signature paths where they already exist (e.g. ModChat Photon).
6. Do **not** use Melon `Type`/`MethodInfo` `==` — use `(object)x == null` (project rule).

### Remap priority after a game update

1. `AssemblyScanner` → fix anything **FAILED**.
2. Photon: rediscover by **delegate signature** (`Invoke(byte, object, int)`), then re-bind properties.
3. Session: `StartNewSession(World, Sandbox, …)` → session-data fields by nested types.
4. Prefer **FindFieldByType** (`GameModifier[]`, `BikeType`, `List<Checkpoint>`) over gobbledygook names.
5. Keep English keys: `TOTALREP`, `WHEELIEBALANCE`, joint names, `laxjiuc`.

---

## Stability legend

| Level | Meaning |
|-------|---------|
| **high** | Readable English name, or signature discovery already in code |
| **medium** | Obfuscated, but strong type/signature/nearby anchors |
| **low** | Mostly a gobbledygook string — expect to break on updates |

---

## Shared anchors (rarely rename)

| Anchor | Use |
|--------|-----|
| GO name `Player_Human` | Local rider |
| Types `Vehicle`, `VehicleController`, `Wheel`, `Cyclist`, `PlayerInfoImpact`, `SessionManager`, `GameData`, `StateMachine` | Compile-time / typeof |
| Methods `Bail`, `FixedUpdate`, `Reset(bool)`, `IsHumanControlled`, `Nobail`, `RespawnAtStartLine`, `RespawnOnTrack`, `StartNewSession`, `PushState`, `ToggleControl`, `UpdateCompass`, `InMPRaceMode` | Harmony / direct |
| Prefs keys `TOTALREP`, `SPONSOR_*`, `UnlockAll`, … | Career |
| Modifier `.name` strings `WHEELIEBALANCE`, `AIRCORRECTION`, … | GameModifiers |
| Transform `backWheel_Jnt` / `frontWheel_Jnt` | Wheel bones |
| Field `laxjiuc` on PlayerInfoImpact | Photon player → name |
| Session field chain `SessionManager.\u0083ESVMoz` → `vebf\u0081kn` | Current level / seed |
| Singleton accessor often `\u005B\u007EqsVD\u007C` (`[~qsVD\|`) or `get_SP` | Singletons |

Also: in-game **AssemblyScanner** (`Mods/System/AssemblyScanner.cs`) writes `UserData/DescendersModMenu/AssemblyReport.txt`.

---

## Photon / ModChat / ModDetection / Telemetry

| id | Feature(s) | Kind | Current name | Stability | Fingerprint / rediscovery |
|----|------------|------|--------------|-----------|---------------------------|
| `photon.network.type` | ModChat, ModDetection, Telemetry | type | `upVWa\u0084E` | medium | Sealed abstract class; nested MulticastDelegate `Invoke(byte,object,int)`. ModChat prefers signature discovery. |
| `photon.localPlayer` | same | property | `gQ\u0060\u0083tus` | low | Public **static property** (not field) on PhotonNetwork type |
| `photon.playerList` | ModChat, ModDetection | property | `CoH\u007C\u007EDq` | low | Public static; value is Array |
| `photon.room` | ModChat | property | `wkT\u0080REz` | low | Room object; `.name` / `.Name` / ToString |
| `photon.offlineMode` | ModChat | property | `CEcjsH\u0083` | low | Static bool |
| `photon.inRoom` | ModChat, ModDetection | property | `La\u0080lETO` | low | Gate for RaiseEvent |
| `photon.connectionState` | ModChat | property | `W\u007Dikkp\u0080` | low | Connection state detailed |
| `photon.onEventCall` | ModChat | event/field | `fu\u0080P\u0084yF` (sig) | medium | Static MulticastDelegate `Invoke(byte,object,int)`; NonPublic |
| `photon.raiseEvent` | ModChat | method | `nO\u0084yY\u005Bu` (sig) | medium | Public static `(byte, object, bool, options)` |
| `photon.raiseEventOptions.default` | ModChat | field | on options type | medium | Public static field of RaiseEventOptions type |
| `photon.player.nickName` | ModChat, ModDetection, Telemetry | property | `DiQND\u0080L` | low | On Photon player instance |
| `photon.player.customProperties` | ModDetection | property | `ttXJk\u007Bh` | low | Hashtable; key `"DescMM"` |
| `photon.player.setCustomProperties` | ModDetection | method | `KxvEguU` | low | `(Hashtable, Hashtable, bool)` |
| `photon.hashtable.type` | ModChat | type | `ExitGames.Client.Photon.Hashtable` | high | Photon3Unity3D — not obfuscated |

**Sources:** `Mods/System/ModChat.cs`, `Mods/System/ModDetection.cs`, `Core/Telemetry.cs`

---

## Session / Map / Seed / State

| id | Feature(s) | Kind | Current name | Stability | Fingerprint |
|----|------------|------|--------------|-----------|-------------|
| `session.startNewSession` | MapChanger | method | `StartNewSession` | high | `(World, enum Sandbox, int, List<GameModifier>)` |
| `session.sessionData` | MapChanger, SkyColours, SpeedrunTimer | field | `\u0083ESVMoz` | medium | On SessionManager; type has `vebf\u0081kn` and/or `skY\u0080uh\u007C` |
| `session.currentLevel` | MapChanger, SkyColours | field | `vebf\u0081kn` | low | On session data |
| `session.elapsedTime` | SpeedrunTimer | field | `skY\u0080uh\u007C` | low | Public Double on session data |
| `levelinfo.type` | MapChanger | type | `\u0081wiWlGz` | medium | AssemblyScanner “Level info type” |
| `levelinfo.fromSeed` | MapChanger | method | `Fm\u007DOWd\u0060` | medium | Public static `(long)` |
| `levelinfo.getSeed` | MapChanger | method | `digk\u0084\u007FK` (sig) | medium | Only public no-arg → `Int64` |
| `levelinfo.world` | MapChanger | field | `g\u005ErFwSM` | low | World enum on level |
| `levelinfo.visualModifier` | SkyColours Storm | field | `\u007CzvoQ\u0084\u005B` | low | VisualModifier on level |
| `gamedata.bonusLevels` | MapChanger | field | `FqVmLOT` | medium | BonusLevelInfo[]; children `levelName`, `customSeed`, `world` |
| `statemachine.pushState` | MapChanger, StateNavigator, OutfitPage | method | `PushState` / `get_SP` | high | Enum members `Generating`, `Sandbox`, `Customization` |
| `statemachine.currentState` | OutfitPage | property | `\u005EtrLeIp` | low | For PopStateBackTo |
| `singleton.staticInstance` | MapChanger, PerkMenu, CareerReset | field | `\u005B\u007EqsVD\u007C` | medium | Or Singleton\<T\> by return type |
| `permagui.instance` | MapChanger | field/method | `\u005B\u007EqsVD\u007C` + `ShowInactivityWarning` | medium | May vanish; skip safely |
| `multimanager.inactivityTimers` | MapChanger | field | `murgZZE`, `kVhi…` | low | Reset to avoid LAST STAND |
| `playerinfo.hcqLastStand` | MapChanger | field | `HCq…xy` | low | Set `-1` before StartNewSession |
| `ui_freeridebikeparks.onEnable` | MapChanger | method | `UI_FreerideBikeParks.OnEnable` | high | Harmony timing for park list |
| `devcommands.loadLevel` | MapChanger | method | `DevCommandsGameplay.LoadLevel` / `AddScore` | high | Readable |

**Sources:** `Mods/World/MapChanger.cs`, `Mods/World/SkyColours.cs`, `Mods/Player/SpeedrunTimer.cs`, `Mods/System/StateNavigator.cs`, `UI/Pages/OutfitPage.cs`

---

## Bike physics

| id | Feature(s) | Kind | Current name | Stability | Fingerprint |
|----|------------|------|--------------|-----------|-------------|
| `vehicle.type` | many | type | `Vehicle` | high | AssemblyScanner |
| `vehicle.acceleration` | Acceleration | field | `cPkCE^\u0081` | low | AssemblyScanner “Acceleration field”; re-apply LateUpdate |
| `vehicle.drag` | MaxSpeedMultiplier | field | (discover; ~`ei[frnu`) | medium | Public Single default ~0.06 (0.001–0.12) |
| `vehicle.speedCapMethod` | NoSpeedCap | method | `E??Kza` len 7 | medium | Private void 0-param; StartsWith `E` EndsWith `Kza` |
| `vehicle.inputAcceleration` | NoSpeedCap | property | `j[fCiJt` | medium | Writable Single StartsWith `j` |
| `vehicle.steer` | ReverseSteering, RubberBand, BikeDamage | property | `swebLyg` | low | Float property |
| `vehicle.lean` | ReverseSteering, RubberBand | property | `c\u007Bv\u007DlhG` | low | Float property |
| `vehicle.wobble` | NoSpeedWobbles | property | contains `kM` | medium | Writable Single; Name.Contains(`kM`) |
| `vehicle.groundGrip` | IceMode | property | `n\u0080jDpmV` | low | Do **not** use `eSXpeQc` (overwritten pre-postfix) |
| `wheel.rollFriction` | IceMode, TyrePressure | property | `WbmnXfG` | low | On Wheel |
| `vehicle.onGround` | AirControl, Wheelie, SessionTrackers, BouncyBike | property | StartsWith `T` | medium | Bool; comment `TDEX…` |
| `vehicle.brakeInput` | QuickBrake | property | `NYsPlot` | medium | Direct access |
| `vehicle.physEnabled` | FlyMode | field | `bYxcVhv` | low | Bool |
| `vehicle.playerInfoLink` | ESP, TeleportToPlayer | field | `\u0080ioTpiS` | low | Vehicle → PlayerInfoImpact |
| `vehicle.hgIcHdS` | SpectateMode | method | `hgIcHdS` | medium | Public `(bool)` network pose reset |
| `vehiclecontroller.tilt` | NoSpeedCap, brakes, steer mods | property | StartsWith `d` | medium | VC → Vehicle by FieldType name `Vehicle` |
| `cyclist.bail` | SessionTrackers, InstantRespawn, SlowMo | method | `Cyclist.Bail` | high | Harmony |
| `cyclist.bailThreshold` | LandingImpact | field | `cxW\u005Em\u005Bm` | medium | Public float default ~15 |
| `cyclist.movement.*` | Movement | field | see hooks.json | low | **Runtime subclass only** — AssemblyScanner WARN |
| `wheel.travel` / `stiffness` / `damping` | Suspension | field | `xL\u007BgJGT`, `p\u007EmkyX\u007B`, `YrKDSPL` | low/med | AssemblyScanner; defaults 0.5 / 50 / 5 |
| `wheel.radius` | WheelSize | field | `HqsqNkJ` | low | Public float |
| `bikeanimation.bones` | WheelSize, WideTyres, BikeDamage | field | `YLzyVuM` / `RCNLpue` or discover | medium | Prefer `backWheel_Jnt` / `frontWheel_Jnt` |
| `tricks.multiplier` | TrickMultiplier | property | `FnHLcjK` | low | AssemblyScanner |
| `tricks.maxCap` | TrickMultiplier | field | `uDh\u005DdJt` | low | Runtime subclass — live only |
| `gamedata.modifiers` | GameModifiers, NoSpeedWobbles | field | `\u0081jU\u0080h\u0084c` | low name / **high keys** | Match `GameModifier.name` English strings; or FindFieldByType |
| `bikecamera.shakeVectors` | NoSpeedWobbles, Earthquake, FOV, Drunk | field | first Vector3s / CameraAngle | medium | Discover by type on BikeCamera |

**Sources:** `Mods/Bike/*`, `Mods/System/GameModifiers.cs`, `Mods/Other/RubberBandSteering.cs`, `Mods/System/AssemblyScanner.cs`

---

## Player / Career / Perks / Outfit

| id | Feature(s) | Kind | Current name | Stability | Fingerprint |
|----|------------|------|--------------|-----------|-------------|
| `playerinfoimpact` APIs | many | methods | `IsHumanControlled`, `Nobail`, `Respawn*`, … | high | AssemblyScanner |
| `playerinfo.laxjiuc` | ESP, TeleportToPlayer | field | `laxjiuc` | **high** | Best player-name remap anchor; ToString ≈ name |
| `playerinfo.spectateName` | SpectateMode | field | `a\u005EsXf\u0083Y` | low | Alternate name field |
| `playerinfo.stats` / in-game rep | CareerReset, MapChanger | field | `d\u0082kxXXv`, `LgqK\u005DLp` | low | ObscuredInt; name Contains `LgqK` |
| `obscuredint.convert` | CareerReset, MapChanger | method | `DZlraRf` | medium | Match ReturnType `int` vs `ObscuredInt` |
| `career.backendRep` | CareerReset | property | `M\u0083\u007B\u007E\u005DEe` | low | DevCommandsBackEnd lifetime rep |
| `career.currentTeam` | CareerReset | property | `lno\u0082zMq` | low | Sponsor team id |
| `gamedata.teams` | CareerReset | field | `D\u0083nWNgg` | low | TeamInfo[] |
| `perkmenu.roster` | PerkMenu | field | *(by type)* | medium | FindFieldByType `GameModifier[]` — no hardcoded name |
| `customization.unlockAll` | CareerReset | property | `mZVyMyX` | medium | Prefs key `UnlockAll` |
| `bikeswitcher.playerObject` | BikeSwitcher | field | `W\u0082oQHKm` | low | GO with PlayerCustomization |
| `bikeswitcher.bikeType` | BikeSwitcher, TrickSetSwap | field | `dzQf\u0082nw` | medium | FieldType `BikeType` |
| `prefsmanager.keys` | BikeSwitcher, CareerReset | keys | `TOTALREP`, … | **high** | English Prefs keys |

**Sources:** `Mods/System/CareerReset.cs`, `Mods/System/PerkMenu.cs`, `Mods/Bike/BikeSwitcher.cs`, `Mods/Tracking/ESP.cs`, …

---

## World / TOD / Weather / Graphics

| id | Feature(s) | Kind | Current name | Stability | Fingerprint |
|----|------------|------|--------------|-----------|-------------|
| `tod_sky` | TimeOfDay, SkyColours | type/fields | `TOD_Sky`, `Cycle`, `Hour` | high | AssemblyScanner; may be outside Assembly-CSharp |
| `effectlist.spawn` | SkyColours | method | `TLJ\u0081Hrt` | low | NonPublic; Harmony |
| `effectlist.envFlags` | SkyColours | field | `\u007Ejl\u0082liu` | low | bool[]; `[7]` ≈ storm |
| `cameraeffects.removeActive` | SkyColours | method | `RemoveActiveEffects` | **high** | Was obfuscated `S\u0083wckiX` — post-update rename example |
| `audiomanager.volume` | Music | method | `SetCategoryVolume` / `GetCategoryVolume` | high | Category enum value 1 = Music |
| `graphics.ppProfile` | Graphics | field | `RzjbfkQ` | medium | On `PostProcessingBehaviour`; child `bloom` etc. readable |
| `terraininfo.getSurfaceInfoAt` | BlizzardDial | method | `TerrainInfo.GetSurfaceInfoAt` | high | Harmony force snow |
| Trees / TurboWind | Trees, TurboWind | Unity API | Terrain / WindZone | high | **Not** Assembly-CSharp remap |

**Sources:** `Mods/World/SkyColours.cs`, `Mods/World/TimeOfDay.cs`, `Mods/World/Music.cs`, `Mods/System/Graphics.cs`, `Mods/World/BlizzardDial.cs`

---

## Spectate / ESP / Teleport / Compass / MP guard

| id | Feature(s) | Kind | Current name | Stability | Fingerprint |
|----|------------|------|--------------|-----------|-------------|
| `spectate.keyframeApply` | SpectateMode | method | `\u007Fg\u0084zUF\u0083`, `NH\u007CIuw\u0081` | low | VehicleReplay; Xbox names may differ; **do not** patch `rjcGHqt` |
| `spectate.bufferLimit` | SpectateMode | field | `ZbiDa\u005EI` | low | VehicleNetworking static int ~50 |
| `spectate.playerInfo.bIvwNah` | SpectateMode | field | `bIvwNah` | low | Must be non-null for valid remote |
| `checkpoint.list` | TeleportToCheckpoint | field | `\u0083b]sfXb` | low | Static `List<Checkpoint>` |
| `vehicleevents.cpIndex` | SessionTrackers | property | `]w\u0082Jbz}` | medium | Int32 auto-prop; name starts `]w`; **not a field** |
| `compass.icon` | CompassAlwaysOn | field | `\u007FcDpHh\u007C` | low | Built from chars 0x7F / 0x7C; method `UpdateCompass` high |
| `multimanager.raceMode` | MultiplayerMenuGuard | method | `InMPRaceMode`, `GetMultiPlayerSessionType` | high | Readable |
| `esp.worldObjectTypes` | ESP | types | Checkpoint, FinishLine, … | high | Compile-time typeof — rename = build break |

**Sources:** `Mods/System/SpectateMode.cs`, `Mods/Tracking/ESP.cs`, `Mods/System/TeleportToCheckpoint.cs`, `Mods/Tracking/SessionTrackers.cs`, `Mods/System/CompassAlwaysOn.cs`, `Mods/System/MultiplayerMenuGuard.cs`

---

## Harmony patch targets (quick index)

| Target | Features |
|--------|----------|
| `Vehicle.FixedUpdate` | NoSpeedWobbles, AutoBalance, WheelieAngleLimit, DrunkMode, IceMode (vehicle), … |
| `VehicleController.FixedUpdate` | NoSpeedCap VC, QuickBrake, CutBrakes, BrakeFade, ReverseSteering, RubberBand, BikeDamage |
| `Vehicle` private `E…Kza` | NoSpeedCap |
| `Vehicle.Reset(bool)` | NoBail |
| `Vehicle.hgIcHdS(bool)` | SpectateMode |
| `VehicleReplay` keyframe methods | SpectateMode |
| `Wheel.FixedUpdate` | IceMode, TyrePressure |
| `Cyclist.Bail` | SessionTrackers |
| `PlayerInfoImpact.Respawn*` | GhostReplay, SlowMoOnBail |
| `TOD_Sky.LateUpdate` / `EffectList.*` | SkyColours |
| `UI_InGame.UpdateCompass` | CompassAlwaysOn |
| `UI_FreerideBikeParks.OnEnable` | MapChanger |
| `TerrainInfo.GetSurfaceInfoAt` | BlizzardDial |

Patches are registered from `Core/ModEntry.cs`.

---

## Features with little / no Assembly-CSharp reflection (N/A)

Pure Unity / UI / Melon / mode logic — **do not expect obfuscation remaps** (unless they call into hooked mods):

Fog, Gravity, MoonMode, DiscoMode, HeadlightsOnly, SlowMotion, MirrorMode, InvisiblePlayer/Bike, PlayerSize/BikeSize, StickyTyres, CenterOfMass, HoverMode, SpiderBike, BikeTorch, TopSpeed HUD, BlackDeath, Survival / PoliceChase / TrickAttack (orchestration), Avalanche / BoulderDodge / ExplodingProps / ObjectPlacer / NearMiss (mostly Unity), Confetti / TrailPainter / Airhorn / BigHead / Chaos / Random\*, UIRemover, MenuCustomiser, GamepadCursor, Favourites, AllModsSwitch, UpdateChecker, Telemetry prefs, BikeStats persistence, most UI pages/HUDs, Trees / TurboWind (Unity Terrain).

Earthquake / FOV / Drunk may still touch **BikeCamera** discovery (`bikecamera.shakeVectors`).

---

## Maintaining this doc

When you add a new reflection hook:

1. Add a record to **`hooks.json`** (canonical).
2. Add a row to the matching table here.
3. If there is a static check, extend **`AssemblyScanner`**.
4. Prefer discovery-by-signature / FindFieldByType over hardcoding a new gobbledygook name when practical.

When a patch remaps a name: update `current_name` and leave a one-line note under `notes` (“was X in 1.8.0”).
