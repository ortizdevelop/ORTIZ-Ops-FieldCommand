# Changelog
## 0.20.0
- **Ordered targets are fired at by every weapon that reaches them** — including nuclear, hypersonic, ballistic and other strategic launchers that the AI would not pick by itself. Fire-control batteries (MLRS, missile launchers) plan the salvo right after the order instead of waiting for their own timer.
- **Fire at area works with all launchers**: projectiles whose seeker can't fly to a point are now steered to the circle by the mod (cruise missiles excepted). Nuclear launchers can shell an area too.
- **Fire mode at purchase**: *Fire at will / Fire on order* switch in the purchase panel and the placement bar — e.g. buy artillery that won't shoot until you order.
- **Bottom command bar** in command mode: buttons for every order (Attack, Strike area, **Stop!**, **Cease fire**, fire mode, Hold, ABM, Rearm) with their keys, and the mouse / camera help as text. New keys: **X** — stop, **N** — cease fire (cancels targets and area fire, switches to fire on order).
- Routes show an **arrow** at the end — which way the unit will face. With a chosen facing, units now come in from behind the point along the arrow, so they stop facing it instead of turning on the spot.
- (0.19.0) One selection for the 3D view and the map; area-fire guidance fixed for rockets / guided shells / ballistic missiles; your ground units drive straight across fields (Alt — roads).
## 0.19.0
- **One selection for the 3D view and the map**: opening the map in command mode selects the same units there, so map orders (RMB move, K target, Z area…) go to them; what you select on the map comes back to the 3D view.
- **Fire at area with rockets, guided shells and ballistic missiles**: the aim point is now given to the projectile right at launch and kept every frame, ballistic missiles plan their launch angle for the real distance — they no longer turn around mid-air or fly a wrong trajectory.
- **Off-road driving**: your move orders to your purchased ground units are driven straight across fields instead of detouring along roads. Hold **Alt** while ordering to use the roads. Option *Own units drive straight (off road)* in F1 → *4. Behaviour*.
## 0.18.0
- Command mode: **RMB on an enemy — attack** with the selected units (no map selection needed); units in range fire, the rest advance to ~80 % of their reach (artillery no longer drives up to 1.5 km).
- Waypoints: **Shift+RMB** (was Ctrl+RMB). **Hold RMB and drag** — the units move there and end up facing the arrow.
- The free camera turns with the **mouse wheel held** in command mode (RMB is for orders). Option in F1 → *4. Behaviour*.
- **Enemy icons** in command mode: spotted enemies are shown with the game's hostile icons in red.
- Fire at area: guns are laid with the game's own aiming formula (as when they shoot at a target) and fire only after the barrel has stayed on the aim for 0.6 s — no more shots into the sky or the ground while the turret is still turning. The log lists how each weapon is used for area fire.
## 0.17.0
- **F6 is now command mode**: a bar at the top with **Purchase / supply**, **Units** and **Stratagems** — a click opens the panel, another click collapses it. Selecting units with the mouse, moving them, waypoints and all order keys work right away, outside the panels too. The separate Shift+F6 mode is gone.
- Your units' icons are shown all the time in command mode (with Shift+F6 they were missing).
## 0.16.0
- **Command mode** (**Shift+F6** or the *Command mode* button in the menu): the purchase menu closes and you command your purchased units right in the 3D view, like in an RTS. **LMB** — select a unit, **drag** — box select (**Shift** — add / remove), **RMB** — move (in formation), **Ctrl+RMB** — add a waypoint. Routes and order points are drawn in 3D and on the map; selected units get green brackets and their weapon reach ring. All order keys work on this selection: **K** attack the enemy under the cursor, **Z** fire at area (the circle is shown in 3D), **I** fire only on order, **H** hold, **L** missile defence, **Y** rearm, **Delete** scrap. **F6** — back to the menu, **Shift+F6** or *Exit* — leave. Only units you bought for yourself can be selected.
## 0.15.0
- **Fire only on order** (**I** on the map, or *Fire: on order* in the *Mine* tab): the unit doesn't shoot at anything by itself — only at the target you give it (**K**), an area (**Z**) or incoming missiles if missile defence (**L**) is on. Option *New units: fire only on order* in F1 → *4. Behaviour*.
- **Fire at area** (**Z**): select your units on the map, press Z and click on the map — or aim with the free camera and click / press G. Artillery, guns, rocket launchers and ships shell the circle whether there is an enemy there or not: guns are aimed with a ballistic solution at random points of the circle, rockets / guided shells / ballistic missiles fly to them. Radius with **[ ]** (default 150 m), duration 60 s (F1 → *4. Behaviour*). The circle and who can reach it are shown while aiming; active areas are drawn on the map and in 3D. **K** on empty ground — cease fire.
## 0.14.2
- **Weapon range on the map**: select your units on the big map and their firing range is drawn around them — solid ring for ground/sea targets, dashed inner ring for the minimum range (artillery), dashed cyan ring for air targets. Point at an enemy and you see straight away who can hit it: green lines — will fire, red — out of range (with how much is missing). Lines to already ordered targets are shown too. F1 → *0. Interface* → *Weapon range on map*.
- Unit markers: icons that the game stores on a black square are now drawn transparent.
## 0.14.1
- Unit markers now use the game's own outlined HUD icons (the same ones the game and Ground Control RTS show), drawn right at the unit, sized like the game's markers (respects the *HMD icon size* setting) and tinted in your colour.
## 0.14.0
- **Camera placement with the mouse again**: while placing units, containers, ships or stratagems with the camera the cursor is shown — point with the mouse and click **LMB** (or G / Enter). When placement ends or the menu (F6) closes, the cursor goes back to the game's state (hidden in flight).
- **RTS-style markers** above your purchased units in the 3D view: the unit's own map symbol in your colour (faction-bought units in soft green). Shown in command mode — while the purchase menu is open or you are placing. F1 → *0. Interface* → *Markers above my units* (CommandMode / Always / Off), *Markers above faction-bought units*.
## 0.13.1
- Fixed: after closing the purchase menu (F6) the mouse cursor stayed on screen until the map was opened and closed. The cursor now goes back to the game's own state right away.
## 0.13.0
- **Paid rearm without containers**: *Rearm $X* on every unit in the *Mine* tab, *Rearm all* at the top, or select your units on the map and press **Y**. Price = the game's cost of the missing rounds × multiplier (*2. Economy* → *Paid rearm: price multiplier*, ×1). Nuclear rounds are not sold.
- **Naval supply containers and pallets** are placed on water now (they were refused as "land only"); ships rearm from them.
- *Mine* tab: two-line unit cards — long names no longer push the buttons out of the window.
- The purchase window is wider and can be **resized** by dragging its bottom-right corner.
## 0.12.1
- Rotation while placing: **R** — clockwise, **Q** — counter-clockwise (new setting *Rotate back*). Tap — one step, hold — the unit keeps turning (*Rotation speed when held*, 120°/s by default).
- Units bought for yourself are also painted in your colour in the Ground Control (RTS) commander view.
## 0.12.0
- Units bought **for yourself** get their own colour on the map and on the 3D HUD markers. Default — orange; F1 → *0. Interface* → *My units colour* / *Highlight my units*.
- Vehicles and ships are sold at the game's **original prices** even when another mod (Ground Control RTS) raises them. Defences and supply keep the current prices. Setting: *2. Economy* → *Original game prices for vehicles and ships*.
## 0.11.0
- **New interface**: flat dark design with a side menu, unit tiles, orange accent (orange / cyan / lime — F1 → *0. Interface* → *Accent colour*). *Camera* is the main buy button.
- Stratagems tab, loadout window, placement panel and messages restyled.
- Airdrop: vehicles land in a line along the arrow and face that way; the drop line and every landing spot are shown on the map and in the camera. New choice: airdrop **for me** or **for the faction**.
## 0.10.3
- Fixed: resupply stopped working as soon as an *Ammo box* (or another container without an access point) was on the map — every check failed with an error. Such containers now work as ammo sources too.
- Fixed: playable KAR / BOTE-type vehicles bought with the mod (they are aircraft-based internally) were never rearmed. They are now rearmed when standing still near an ammo source.
- Units whose weapon stations the game's own rearm routine can't handle are refilled directly.
- Diagnostics in `BepInEx/LogOutput.log`: a resupply summary once a minute, and the list of units hidden from the purchase menu (other faction's equipment / mission event content) with the reason.
## 0.10.2
- Fixed: units did not rearm from purchased ammo containers, depots and supply trucks and just stood still with empty magazines. Now any stopped friendly vehicle or ship that is short of ammo is rearmed by the nearest friendly ammo source within **150 m** (configurable), using the game's own rearm rules.
- Fixed: supply trucks bought *for me* spawned in hold position, which switched off the game's supply AI. They now drive to friendly units that need ammo on their own, like in the base game.
- New settings in *4. Behaviour*: *Resupply from nearby ammo sources*, *Resupply radius, m*, *Supply trucks work on their own*.
## 0.10.1
- Fixed: in 0.10.0 the mod stopped loading after the language setting (the F6 menu did nothing and F1 showed only the language). Two setting names contained an apostrophe, which BepInEx does not allow. All your settings are kept.
## 0.10.0
- **English interface** (default) with a **Russian** option: all windows, buttons, tips, messages, unit groups and stratagems are translated. Switch with the *Language* button in the purchase menu or F1 → *0. Language* (applies instantly).
- F1 settings now have English names (Russian names are shown when the language is Russian, after a restart). Your existing values are carried over automatically.
## 0.9.4
- Rebuilt for game version 0.34.6. No gameplay changes.
## 0.9.3
- Stratagems: choose the number of shells / bombs / missiles per salvo and the number of salvos (1–10) for artillery, carpet bombing and missile strikes. The price scales with the total amount. New setting: interval between salvos (default 4 s).
## 0.9.2
- Unit limits for yourself and for the faction can now be set right in the purchase menu (sliders + −10/−/+/+10 buttons), up to **150** each.
## 0.9.1
- Renamed to **ORTIZ Ops - FieldCommand** (DLL `ORTIZ-Ops-FieldCommand.dll`). Settings are kept.
## 0.9.0
- Renamed to **ORTIZ Field Command** (GUID `ortiz.fieldcommand`). Settings of older builds are migrated automatically.
- First public release.
