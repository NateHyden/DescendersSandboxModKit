# Descenders Sandbox

A MelonLoader mod menu for Descenders, packed with tweaks, tools, and game modifiers to mess around with in **Free Ride and Bike Parks**.

> ⚠️ This mod is intended for use in **Free Ride and Bike Park** sessions only. Features will not work in career mode, events, or online lobbies. This mod is client side only.

Press **F6** in-game to open the menu.

---

## Features

### 🚲 Bike Physics
Tune every aspect of how your bike handles.

- **Acceleration** — Boost your pedal power
- **Max Speed** — Set a custom top speed or remove the cap entirely
- **No Bail** — Makes it harder to fall off
- **Auto Balance** — Helps keep you upright
- **Landing Impact** — Adjust how forgiving bail detection is on landings
- **Tyre Pressure** — Change grip and handling feel
- **Brake Fade** — Simulate brake fade over time
- **Brake Balance** — Shift braking bias front or rear
- **Instant Respawn** — Skip the bail animation and get back riding immediately
- **Sticky Tyres** — Extra grip on all surfaces
- **Ice Mode** — Minimal grip for a slidey, chaotic ride

### 🛠️ Bike Setup
- **Bike Switcher** — Change your bike type on the fly
- **Trick Set Swap** — Borrow another bike's trick set
- **Bike Size** — Scale the bike up or down
- **Wheel Size** — Adjust front and rear wheel size independently
- **Wide Tyres** — Inflate the tyre width
- **Invisible Bike** — Hide the bike mesh
- **Bike Damage** — Visual damage tinting on bail
- **Bike Torch** — Toggle the headlight

### 🎮 Movement & Controls
- **Spin Force** — Amplify bar spins
- **Hop Force** — Adjust bunny hop height
- **Wheelie Force** — Make wheelies easier or harder
- **Wheelie Angle Limit** — Cap or expand how far back you can lean
- **Wheelie HUD** — On-screen angle readout while wheeling
- **Lean Strength** — Adjust mid-air lean sensitivity
- **Air Control** — Fine tune how much you can steer in the air
- **Pump Strength** — Adjust how effective pumping through terrain is
- **Reverse Steering** — Flip your handlebar input
- **Cut Brakes** — Disable braking entirely
- **Near Miss Sensitivity** — Change how close is "close enough" for near miss scoring
- **Center of Mass** — Shift weight distribution on the bike
- **Suspension** — Tune stiffness, damping, and travel

### 🌍 World
- **Gravity** — Crank it up for moon physics or down for heavy riding
- **Time of Day** — Scrub through the day/night cycle
- **Sky Colours** — Tint the sky and lighting
- **Fog** — Add atmospheric fog
- **Trees & Foliage** — Toggle environmental props
- **Turbo Wind** — Crank up wind intensity
- **Exploding Props** — Props launch on impact
- **Storm** — Toggle storm weather

### 🗺️ Map
- **Map Changer** — Load any base game map or bike park instantly
- **Load from Seed** — Enter a seed number to load a specific procedural map

### 🎭 Fun
- **Player Size** — Scale your rider up or down
- **Invisible Player** — Hide the rider mesh
- **Moon Mode** — Low gravity preset
- **Mirror Mode** — Flip the world horizontally
- **Fly Mode** — Detach from the ground and fly freely
- **Drunk Mode** — Wobbly, disorienting controls
- **Camera Shake** — Add intensity to the camera
- **FOV** — Adjust field of view

### 👗 Outfit
- **Outfit Presets** — Save and load full rider outfit combinations

### 💬 Chat
- In-menu chat system for communicating with other Descenders Sandbox users nearby

### 🎮 Modes
Custom game modes you can run in free ride:

- **Earthquake Mode** — Ground shakes at set intervals with adjustable intensity and frequency
- **Police Chase Mode** — Hazards hunt you down with difficulty scaling
- **Trick Attack Mode** — Race to hit a score target before time runs out
- **Boulder Dodge Mode** — Boulders rain down — dodge them for as long as you can
- **Survival Mode** — Stay on your bike and don't bail

### 👻 Ghost Replay
- Record a run and replay your ghost alongside you for self-competition

### 🎯 Teleport
- **Visual Player Finder** — Spot nearby players with tracers and distance display
- **Teleport to Player** — Jump to another player's position
- **Teleport to Checkpoint** — Jump to any checkpoint in the map

### 📸 Screenshot
- **Screenshot Mode** — Hide the HUD and UI for clean captures, triggered via D-Pad Up on controller

### 🖥️ Session
- **Session HUD** — On-screen tracker showing current run stats, top speed, longest airtime, peak G-force, bails, and session timer
- **Speedrun Timer** — A clean on-screen timer for self-timed runs

### 🎨 Graphics
- **Bloom, Ambient Occlusion, Depth of Field, Vignette, Chromatic Aberration** — Toggle post processing effects individually
- **Quality** — Switch render quality preset
- **Hide Game HUD** — Remove the in-game interface

### ⭐ Favourites
Pin any setting to your Favourites tab for quick access without digging through menus.

---

## Requirements

- **Descenders** (Steam or Xbox PC / Game Pass)
- **MelonLoader 0.5.7** — download from [github.com/LavaGang/MelonLoader/releases](https://github.com/LavaGang/MelonLoader/releases)
  - When installing, select version **0.5.7** from the dropdown — other versions will not work

---

## Installation — Steam

1. Run the MelonLoader installer — it will auto-detect your Descenders installation. Select version **0.5.7** from the dropdown
2. Launch the game once to let MelonLoader set up its folders, then close it
3. Download `DescendersSandbox.dll` from the [Releases page](https://github.com/NateHyden/Descenders-Sandbox/releases)
4. Drop it into your `Descenders/Mods/` folder
5. Launch the game and press **F6** to open the menu

If updating from an older version, delete any previous `DescendersModMenu.dll`, `DescendersToolKit.dll`, or `DescendersSandbox.dll` from your Mods folder first.

---

## Installation — Xbox PC / Game Pass

MelonLoader works on the Xbox PC (Game Pass) version but needs a couple of extra steps due to Windows App Store permissions.

**Recommended: Move the game out of WindowsApps first**

The default install location (`C:\Program Files\WindowsApps`) is heavily permission-locked. It's much easier to move the game first:

1. Open the Xbox app → right-click Descenders → **Manage → Files → Change drive**
2. Select a drive you own (e.g. `D:\`) and confirm
3. If in doubt, uninstall and reinstall to your chosen drive — your saved data will not be lost

**Finding your game files (if already installed)**

Same process as above — right-click Descenders → **Manage → Files**, but click **Browse** instead of Change drive. This opens the install folder directly in Explorer.

If you're stuck in the default WindowsApps location, right-click the folder → Properties → Security → Edit → add your user account and grant Full Control.

**Installing MelonLoader**

1. Run the [MelonLoader 0.5.7](https://github.com/LavaGang/MelonLoader/releases) installer
2. MelonLoader won't auto-detect the Xbox version — click **Add Game** and navigate to `Descenders.exe` manually
3. Make sure version **0.5.7** is selected, then install
4. Launch the game once to populate MelonLoader's folders, then close it
5. Download `DescendersSandbox.dll` from the [Releases page](https://github.com/NateHyden/Descenders-Sandbox/releases) and drop it into the `Mods` folder
6. Launch the game and press **F6**

> ⚠️ The Xbox / Game Pass version is largely untested. Most features work but some behaviour may differ from the Steam version.

---

## Building from Source

1. Clone this repository
2. Open `GamePath.props` in the project root and set `<DescendersPath>` to your Descenders install folder
3. Open `DescendersSandbox.sln` in Visual Studio
4. Build — the DLL copies to your `Mods` folder automatically

**Common install locations**

| Platform | Path |
|---|---|
| Steam (default) | `C:\Program Files (x86)\Steam\steamapps\common\Descenders` |
| Steam (custom library) | `D:\SteamLibrary\steamapps\common\Descenders` |
| Xbox / Game Pass | `C:\XboxGames\Descenders\Content` |

Not sure where your install is? In Steam, right-click Descenders → **Manage → Browse local files** and copy the path from Explorer.

---

## Links

- **Discord:** [discord.gg/rHvCrBdqaR](https://discord.gg/rHvCrBdqaR)
- **Nexus Mods:** [nexusmods.com/descenders/mods/8](https://www.nexusmods.com/descenders/mods/8)
