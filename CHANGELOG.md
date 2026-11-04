# Changelog
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
