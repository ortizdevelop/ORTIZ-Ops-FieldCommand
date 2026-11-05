<div align="center">

# ORTIZ Ops - FieldCommand

**Buy, place and command your own army in Nuclear Option**

[![Game](https://img.shields.io/badge/Nuclear%20Option-0.34.6-2b5797)](https://store.steampowered.com/app/2168680/Nuclear_Option/)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4-6f42c1)](https://github.com/BepInEx/BepInEx)
[![NOMM](https://img.shields.io/badge/install%20via-NOMM-2ea44f)](https://github.com/Combat787/NOMM)
[![License](https://img.shields.io/badge/license-MIT-lightgrey)](LICENSE)

**English** · [Русский](#-русский)

</div>

---

Spend your money on **ground units, warships, static air defence and supply depots**, put them exactly where you want — on the map or with a free camera — and give them orders like in an RTS. On top of that, call in **stratagems**: artillery barrages, carpet bombing, missile strikes, tactical and strategic nukes and parachute airdrops.

> [!IMPORTANT]
> **Single-player / host only.** Every feature works only when *you* are the host. On a dedicated or someone else's server the mod does nothing, so it can't give anyone an advantage on servers they don't control.

> [!NOTE]
> The interface is in **English** by default and can be switched to **Russian** — with the *Language* button in the purchase menu or in F1 → *0. Language*. All settings can be changed in the BepInEx Configuration Manager (F1).

## Contents
- [Features](#features)
- [Controls](#controls)
- [Purchase menu](#purchase-menu)
- [Stratagems](#stratagems)
- [Settings](#settings)
- [Installation](#installation)
- [Building from source](#building-from-source)

## Features

| | |
|---|---|
| 🛒 **Buy units** | Every ground vehicle, ship and static defence available in the game, priced at its in-game cost (multiplier configurable). |
| 📍 **Place them yourself** | On the map (click) or with a free camera (aim + key). Rotate before placing. Ships only on water deep enough, vehicles only on land. |
| 🗺️ **Own territory only** | Near your airbases (separate radius for ground and naval units) and near your own units, never too close to the enemy. All radii configurable. |
| 🏳️ **Faction-locked** | BDF buys only BDF equipment, PALA only PALA. Ownership is learned from the game's own missions and convoys; manual overrides in config. |
| 👤 **For yourself or for the faction** | *For me*: units hold position and wait for your orders. *For faction*: the AI decides where they go. Separate limits (60 each by default, up to 150) — set them with sliders right in the purchase menu. |
| 🎯 **RTS orders on the map** | Move, waypoints, hold, focus fire from current position, advance to firing range, automatic missile defence, paid rearm on the spot, scrap with refund. |
| 🎮 **Command mode (F6)** | F6 turns on command mode: a top menu (*Purchase / supply*, *Units*, *Stratagems*) and RTS control of your units right in the 3D view — box select, move in formation (drag RMB to set facing), Shift+RMB waypoints, RMB on an enemy to attack, enemy icons, camera on the mouse wheel. |
| 🔇 **Fire only on order** | Units in this mode don't shoot at anything by themselves — only at the target or area you give them. |
| 💥 **Fire at area** | Artillery, guns, rocket launchers and ships shell a circle you mark on the map or with the camera — whether there is an enemy there or not. |
| 📡 **Weapon range on the map** | Select your units and their firing range is drawn around them. Point at an enemy — green lines show who will fire, red ones who can't reach (and how much is missing). |
| 🟧 **Your units stand out** | Units you bought for yourself have their own colour on the map, on the HUD markers and in the Ground Control RTS view; in command mode the game's own unit icon is shown above each of them in the 3D view. |
| 🚙 **Playable KAR / BOTE units** | Pick modules for every hardpoint, buy and spawn right inside. Get out — the vehicle stays; get back in from the menu. |
| 🏗️ **Defences and supplies** | SAM sites, gun positions, radars, depots, ammo / fuel / repair containers. Supplies don't use unit slots. Stopped units near a friendly ammo container, depot or supply truck are **rearmed automatically**; bought supply trucks drive to units that need ammo on their own. |
| ☢️ **Stratagems** | Artillery, carpet bombing, missile strike, tactical and strategic nuke, airdrop. Point or area targeting with adjustable radius. |
| 💰 **Kill rewards** | Get a share of the reward when your units (default 50 %) or stratagems (default 15 %) destroy enemies or missiles. |

## Controls

All keys can be rebound in the Configuration Manager (F1).

### General
| Key | Action |
|---|---|
| **F6** | Command mode on / off (top menu + unit control) |
| **J** | Get out of the current KAR / BOTE vehicle (it stays where it is) |

### Placing units and targeting stratagems
| Key | Action |
|---|---|
| **LMB** | Place the unit / choose the target — on the map, or under the mouse cursor when placing with the camera |
| **G** / **Enter** | Place the unit / choose the target at the camera's aim point |
| **R** / **Q** | Rotate the unit (or the strike / airdrop direction) clockwise / counter-clockwise: tap — 45° step, hold — keeps turning |
| **T** | Switch stratagem targeting: point ⇄ area |
| **]** / **=** / **Num+** | Increase the area radius |
| **[** / **-** / **Num-** | Decrease the area radius |
| **I** | Fire mode of the new unit: at will ⇄ on order |
| **X** / **Backspace** | Cancel |

### Command mode (3D view)
| Key | Action |
|---|---|
| **LMB** | Select one of your units (click on empty ground — deselect) |
| **LMB drag** | Box select; **Shift** — add / remove |
| **RMB** | Move the selection there, in formation |
| **RMB hold + drag** | Move there and face the arrow |
| **Alt** + order | Drive along the roads (by default your units drive straight across country) |
| **Shift+RMB** | Add a waypoint (units drive the points one after another) |
| **RMB on an enemy** | Attack: units in range fire, the others advance to ~80 % of their reach |
| **Ctrl+RMB on enemies** | Target list: one salvo at each target in turn (also on the map) |
| **Ctrl+1…0** / **1…0** | Remember the selection as a group / select the group (**Shift** — add, twice — camera to it) |
| **Ctrl+C** | Quick buy: one more unit of the selected type, placed right away |
| **X** / **N** | Stop! / Cease fire (cancel targets and area fire, switch to fire on order) |
| **Mouse wheel held** | Turn the free camera (RMB is used for orders in this mode) |
| **K / Z / I / H / L / Y / Delete** | Same orders as on the map, for the selected units (K — enemy under the cursor) |
| **F6** | Leave command mode (during placement — cancel and return to it) |

### Orders on the map (select your units first)
| Key | Action |
|---|---|
| — | Selecting units shows their **weapon range**; pointing at an enemy shows who can hit it |
| **RMB** | Move (**Shift+RMB** — add a waypoint) |
| **H** | Toggle *hold position* |
| **K** | Fire at the enemy / missile under the cursor **from the current position** (artillery and SAMs engage at max range). On empty ground — clear the target |
| **Shift+K** | Advance to firing range and attack |
| **L** | Automatic missile defence — units pick and shoot down enemy missiles themselves (strategic and nuclear first) |
| **I** | Toggle *fire only on order*: the units don't shoot by themselves, only at the target (K), area (Z) or missiles (L) you give |
| **Z** | *Fire at area*: then **LMB** on the map (or aim with the free camera and **LMB** / **G**) — shell the circle; **[ ]** — radius, **X** / **RMB** — cancel. **K** on empty ground — cease fire |
| **Y** | Rearm the selected own units right where they stand, for money |
| **Delete** | Scrap the selected purchased units (refund 50 % by default) |

## Purchase menu

**F6** opens command mode with a bar at the top: **Purchase / supply** (ground, naval, defences / depots, playable), **Units** (everything you bought) and **Stratagems**. Clicking a section opens its panel, clicking it again collapses it — you keep selecting and ordering units outside the panel. The panel has a flat dark design with a side menu of sub-tabs; at the bottom of the side menu are your budget, faction and the money earned from kills. The accent colour (orange / cyan / lime) is set in F1 → *0. Interface*. At the top are the *For me / For the faction* switch and two sliders (with −10 / − / + / +10 buttons) for **your unit limit** and the **faction unit limit**, 1–150 each. Tabs:

- **Ground** — AA, radars, artillery, tanks, IFVs, logistics… grouped by category.
- **Naval** — carriers, LHAs, cruisers, destroyers, frigates, corvettes, support ships.
- **Defences / depots** — static SAMs, gun positions, radars, depots and containers.
- **Playable** — KAR (ground) and BOTE (naval) units you can drive yourself: *Modules & buy* → choose a module for each hardpoint with ◄ ► → place on the map or with the camera → you spawn inside.
- **Mine** — every unit you bought: hold/release, stop, missile defence, show on map, get in (playable units), **paid rearm** on the spot (*Rearm $X*, *Rearm all*), scrap.
- **Stratagems** — call-in support, see below.

The window can be resized by dragging its bottom-right corner. Naval supply containers / pallets are placed on water. Units are shown as tiles with type, faction and price; **Camera** and **On map** start placement. The **EN / RU** button switches the language.

With the camera you place with the **mouse**: point and click LMB (or G). When the menu closes or placement ends, the cursor goes back to the game's state.

### Your units on screen
- Units you bought **for yourself** are painted in their own colour on the map and on the HUD markers (also in the Ground Control RTS view). Default — orange; F1 → *0. Interface* → *My units colour*.
- In command mode (menu open or placing) the game's own outlined unit icon is drawn above each of your units in the 3D view, tinted in your colour; faction-bought units — in soft green. *Markers above my units*: CommandMode / Always / Off.

### Command mode
Press **F6**: the cursor stays and you control **only the units you bought for yourself** in the 3D view — with the panels collapsed or outside them. Your units always show their icons in your colour in this mode, spotted enemies — their hostile icons in red. The free camera turns with the **mouse wheel held**. Selected units get green brackets and (for up to 6) a ring of their weapon reach. **RMB** sends them in a grid formation facing the direction of travel — hold RMB and drag to choose which way they will face (an arrow shows it); **RMB on an enemy** attacks it; **Shift+RMB** queues waypoints — routes are drawn as dashed lines in 3D and on the map and disappear when a unit gets another order. Every order key works on the selection, so you can mark strike areas with **Z** right from the camera. Opening the map keeps the same selection there (and back), so targets and areas can be given on the map too. Your ground units drive **straight across country** to the ordered point; hold **Alt** to make them use the roads. The top bar shows the sections, the selection, your budget and *Exit*. The **bottom bar** has a button for every order (Attack, Strike area, Stop!, Cease fire, fire mode, Hold, ABM, Rearm) with its key, and the mouse / camera help. Routes end with an arrow showing which way the unit will face. When buying units for yourself you choose their **fire mode** (*fire at will* / *fire on order*) in the purchase panel or the placement bar, or with **I** while placing. **Ctrl+1…0** makes groups (1…0 selects, twice — camera to the group), **Ctrl+C** buys one more of the selected unit, **Ctrl+RMB** on several enemies gives a target list — one salvo at each in turn. A badge over each of your units shows its fire mode: orange crosshair — fire at will, grey crossed-out crosshair — fire on order (also on the map).

### Fire only on order and fire at area
- **Fire only on order** — **I** on the map or the *Fire: on order / Fire: free* button in the *Mine* tab. Such units ignore enemies until you order: a target (**K**), an area (**Z**) or missile defence (**L**). *New units: fire only on order* in F1 → *4. Behaviour* puts every new purchase in this mode.
- **Fire at area** — select units, press **Z**: a circle follows the cursor on the map (or the camera's aim point in free camera / with the menu open), lines show which units reach it. **LMB** (or **G** with the camera) — fire. Guns are laid with a ballistic solution onto random points of the circle; rocket launchers raise their launchers and their rockets, guided shells and ballistic missiles fly to the circle by themselves. Units out of range are skipped. Radius **[ ]** (default 150 m), duration 60 s, then they return to normal; **K** on empty ground stops earlier. Active areas are drawn on the map and in the 3D view.

### Weapon range on the map
Select your units on the big map:
- **solid ring** — reach against ground and sea targets (lightly filled);
- **dashed inner ring** — minimum range (e.g. artillery can't fire closer);
- **dashed cyan ring** — reach against air targets, if different;
- with up to 4 units selected the range is written above the ring.

Point at an enemy: a **green** line from every unit that can fire at it right now, **red** from those that can't; the label says *IN RANGE*, *in range for 2 of 3* or *OUT OF RANGE — 3.4 km short*. Dashed lines lead to targets already ordered. Only weapons with ammo count. F1 → *0. Interface* → *Weapon range on map*.

If another mod (e.g. Ground Control RTS) changes vehicle prices, FieldCommand still sells vehicles and ships at the game's original prices (defences and supply keep the current ones) — F1 → *2. Economy* → *Original game prices for vehicles and ships*.

## Stratagems

Choose a stratagem → **Target on map** or **Target with camera**. The strike lands after a delay (5 s by default). Every stratagem has its own price and cooldown.

| Stratagem | What it does | Price | Cooldown |
|---|---|---|---|
| **Artillery** | A barrage of shells (12 by default, 1–60 per salvo) on a point or spread over the circle | 15 M | 60 s |
| **Carpet bombing** | Bombs in a line (14 by default, 4–40 per salvo) — ~200 m in point mode or the full circle diameter, along the arrow (R to rotate). The line is shown on the map and in the camera | 35 M | 120 s |
| **Missile strike** | Missiles diving vertically (4 by default, 1–16 per salvo): point — all into the nearest visible enemy; area — into visible enemies in the circle, with lead on moving targets | 40 M | 90 s |
| **Tactical nuke** | One tactical nuclear warhead | 250 M | 300 s |
| **Strategic nuke** | One strategic nuclear warhead | 800 M | 900 s |
| **Airdrop** | 1–6 ground vehicles of your choice on real parachutes, anywhere on land — even behind enemy lines. They land in a line along the arrow (R/Q), the drop line and every landing spot are shown on the map and in the camera. Choose **for me** or **for the faction**. Price = unit price × count × 1.3 | — | 90 s |

For every strike you can pick the munition with ◄ ► (or **auto**).

For artillery, carpet bombing and missile strikes you also choose **how many projectiles per salvo** and **how many salvos** (1–10, 4 s apart by default). The price grows proportionally: *price = base price × (projectiles ÷ default amount) × salvos*. Prices in the table are for the default amount and one salvo.

## Settings

Open **F1 → ORTIZ Ops - FieldCommand**. The most useful options:

| Section | Setting | Default |
|---|---|---|
| Interface | Accent colour (orange / cyan / lime) | Orange |
| Interface | Highlight my units / my units colour | on / orange |
| Interface | Markers above my units / above faction-bought units | CommandMode / on |
| Interface | Weapon range on map | on |
| Economy | Funds source (personal / faction treasury) | Personal |
| Economy | Price multiplier / minimum price | ×1 / 0.5 M |
| Economy | Own unit limit / faction unit limit (1–150, also adjustable right in the F6 menu) | 60 / 60 |
| Economy | Original game prices for vehicles and ships (if another mod changes them) | on |
| Economy | Paid rearm: price multiplier | ×1 |
| Economy | Refund when scrapping | 50 % |
| Economy | Kill reward share (own units) / also for faction units | 50 % / on |
| Territory | Own territory only | on |
| Territory | Radius from airbase: ground / naval | 12 km / 30 km |
| Territory | Radius from own units / min. distance to enemy | 3 km / 2 km |
| Territory | Min. water depth for ships | 8 m |
| Behaviour | Spawn in *hold position* | on |
| Behaviour | Rotation step / rotation speed when held | 45° / 120°/s |
| Behaviour | New units: fire only on order | off |
| Behaviour | Own units drive straight (off road) / command mode: enemy icons / camera on the mouse wheel / groups on 1-0 / fire mode badges | on / on / on / on / on |
| Behaviour | Area fire radius / duration | 150 m / 60 s |
| Behaviour | Resupply from nearby ammo sources / resupply radius | on / 150 m |
| Behaviour | Supply trucks work on their own (ignore hold position) | on |
| Behaviour | Attack standoff: ground / naval | 1.5 km / 9 km |
| Factions | Only own faction's equipment / show other faction (greyed out) | on / off |
| Factions | Manual overrides (`key:BDF, key:PALA, key:ALL`) | — |
| Playable | Enable KAR / BOTE units | on |
| Defences | Enable / show civil buildings / buildings can be captured | on / off / off |
| Stratagems | Strike delay | 5 s |
| Stratagems | Spawn height above ground / never below (above sea level) | 800 m / 3500 m |
| Stratagems | Fall speed / interval between salvos | 300 m/s / 4 s |
| Stratagems | Airdrop height / airdrop markup | 150 m / ×1.3 |
| Stratagems | Stratagem kill reward | 15 % |
| Stratagems | Price, cooldown, munition, projectiles per salvo and salvos for every stratagem | see table above |

## Installation

**Recommended:** install with [Nuclear Option Mod Manager (NOMM)](https://github.com/Combat787/NOMM) — search for **ORTIZ Ops - FieldCommand**. NOMM also keeps it up to date.

**Manually:** install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases), download `ORTIZ-Ops-FieldCommand.dll` from [Releases](../../releases) and put it in `Nuclear Option/BepInEx/plugins/ORTIZ-Ops-FieldCommand/`.

**Optional:** [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) for the F1 settings window; **KAR** and **BOTE** for playable units.

> Upgrading from an older build named *FieldCommand* or *ORTIZ.FieldCommand*: delete the old `BepInEx/plugins/FieldCommand` / `BepInEx/plugins/ORTIZ.FieldCommand` folder. Your settings are carried over automatically.

## Building from source

```
dotnet build -c Release -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option"
```
Game and BepInEx assemblies are referenced from your local installation and are not part of this repository.

---

<a id="-русский"></a>
## 🇷🇺 Русский

Покупай за свои деньги **наземную технику, корабли, стационарную ПВО и склады**, ставь их ровно туда, куда нужно — на карте или свободной камерой — и командуй ими как в RTS. А ещё вызывай **стратегемы**: артудар, ковровую бомбардировку, ракетный удар, тактический и стратегический ядерный удар и десант на парашютах.

> [!IMPORTANT]
> **Только одиночная игра или если ты хост.** На выделенном или чужом сервере мод ничего не делает.

> [!NOTE]
> По умолчанию интерфейс на **английском**. Включить **русский**: кнопка «Language» внизу меню покупки (F6) или F1 → *0. Language* → *Russian*. Названия настроек в окне F1 переводятся после перезапуска игры.

### Возможности
- 🛒 **Покупка юнитов** — вся наземная техника, флот и стационарная оборона игры по их игровой цене (множитель настраивается).
- 📍 **Ручная расстановка** — на карте (клик) или камерой (прицел + клавиша), с поворотом. Корабли — только на достаточной глубине, техника — только на суше.
- 🗺️ **Только своя территория** — рядом со своими авиабазами (отдельный радиус для наземки и флота) и своими юнитами, не ближе заданной дистанции к врагу.
- 🏳️ **Только своя фракция** — BDF покупает технику BDF, PALA — PALA. Ручная привязка в настройках.
- 👤 **Себе или фракции** — «себе»: стоят на месте и ждут приказов; «фракции»: ими управляет ИИ. Отдельные лимиты (по 60, максимум 150) — меняются ползунками прямо в меню покупки.
- 🎯 **Приказы на карте** — движение, маршрут, держать позицию, огонь с места, выдвижение на дистанцию огня, перехват ракет, платное пополнение на месте, списание с возвратом денег.
- 🎮 **Режим командира (F6)** — верхнее меню («Покупка / снабжение», «Юниты», «Стратегемы») и управление своими юнитами прямо в 3D как в RTS: рамка, движение строем, маршрут через Shift+ПКМ, направление — протянуть ПКМ, атака — ПКМ по врагу, значки врагов, поворот камеры колесом.
- 🔇 **Огонь только по приказу** — юниты в этом режиме сами ни по кому не стреляют, только по цели или области, которую ты укажешь.
- 💥 **Огонь по площади** — артиллерия, орудия, РСЗО и корабли обстреливают круг, указанный на карте или камерой, — есть там враг или нет.
- 📡 **Дальность огня на карте** — выдели свои юниты, и вокруг них видна зона поражения. Наведи на врага: зелёные линии — кто откроет огонь, красные — кто не достаёт (и сколько не хватает).
- 🟧 **Своя техника заметна** — купленное себе выделяется своим цветом на карте, метках HUD и в Ground Control RTS; в режиме командира над каждым юнитом в 3D — родной значок игры твоего цвета.
- 🚙 **Играбельная техника KAR / BOTE** — выбор модулей на каждую точку подвеса, сразу садишься внутрь, можно выйти и сесть снова.
- 🏗️ **Оборона и снабжение** — ЗРК, огневые точки, радары, склады, контейнеры с боеприпасами/топливом/ремонтом. Снабжение не занимает слоты юнитов. Стоящие юниты рядом с союзным контейнером боеприпасов, складом или машиной снабжения **пополняют боезапас автоматически**; купленные машины снабжения сами ездят к тем, кому нужны патроны.
- ☢️ **Стратегемы** — артудар, ковровая бомбардировка, ракетный удар, тактический и стратегический ядерный удар, десант. По точке или по области с регулируемым радиусом.
- 💰 **Награды** — доля награды за уничтожения твоими юнитами (50 %) и стратегемами (15 %).

### Управление
| Клавиша | Действие |
|---|---|
| **F6** | Режим командира вкл/выкл (верхнее меню + управление юнитами) |
| **ЛКМ** / рамка (режим командира) | Выделить свои юниты; **Shift** — добавить/убрать |
| **ПКМ** / **Shift+ПКМ** (режим командира) | Двигаться строем / добавить точку маршрута |
| **Зажать ПКМ и потянуть** | Двигаться и встать лицом по стрелке |
| **Alt** + приказ | Ехать по дорогам (по умолчанию свои юниты едут напрямую через поля) |
| **ПКМ по врагу** | Атаковать: кто достаёт — стреляет, остальные выдвигаются на ~80 % своей дальности |
| **X** / **N** | Стоп! / Прекратить огонь (отменить цели и огонь по площади, включить огонь по приказу) |
| **Ctrl+ПКМ по врагам** | Список целей: по одному залпу по каждой по очереди (и на карте) |
| **Ctrl+1…0** / **1…0** | Запомнить выделенных как группу / выделить группу (**Shift** — добавить, дважды — камера к группе) |
| **Ctrl+C** | Быстрая покупка: ещё один юнит того же типа, сразу к расстановке |
| **I** (при расстановке) | Режим огня нового юнита: свободный ⇄ по приказу |
| **Зажатое колесо** | Поворот свободной камеры (ПКМ в этом режиме — для приказов) |
| **J** | Выйти из техники KAR / BOTE |
| **ЛКМ** | Поставить юнит / выбрать цель — на карте или под курсором мыши при расстановке камерой |
| **G** / **Enter** | Поставить юнит / выбрать цель в точку прицела камеры |
| **R** / **Q** | Повернуть юнит или направление удара/десанта по/против часовой: нажатие — шаг 45°, зажать — крутится сам |
| **T** | Стратегема: точка ⇄ область |
| **]** **=** **Num+** / **[** **-** **Num-** | Увеличить / уменьшить радиус области |
| **X** / **Backspace** | Отмена |
| — | Выделение своих юнитов показывает их **дальность огня**, наведение на врага — кто его достаёт |
| **ПКМ** (**Shift+ПКМ**) | Двигаться (добавить точку маршрута) |
| **H** | Держать позицию |
| **K** | Огонь по врагу/ракете под курсором с текущей позиции; по пустому месту — сбросить цель |
| **Shift+K** | Выдвинуться на дистанцию огня и атаковать |
| **L** | Автоматический перехват ракет (стратегические и ядерные — первыми) |
| **I** | Огонь только по приказу: сами не стреляют, только по цели (K), области (Z) или ракетам (L) |
| **Z** | Огонь по площади: дальше **ЛКМ** по карте (или навести свободную камеру и **ЛКМ** / **G**); **[ ]** — радиус, **X** / **ПКМ** — отмена. **K** по пустому месту — прекратить огонь |
| **Y** | Пополнить боезапас выделенных своих юнитов на месте за деньги |
| **Delete** | Списать выделенных купленных юнитов с возвратом 50 % |

### Меню (F6)
**F6** включает режим командира: сверху полоса с разделами **«Покупка / снабжение»** (наземка, флот, оборона/склады, играбельная), **«Юниты»** (всё купленное) и **«Стратегемы»**. Клик по разделу открывает его панель, повторный — сворачивает; вне панели можно выделять юниты и отдавать приказы.
Окно в тёмном «плоском» стиле с боковым меню вкладок; внизу меню — бюджет, фракция и заработок. Акцентный цвет (оранжевый / бирюзовый / лаймовый) — F1 → *0. Interface*. Своя купленная техника выделяется своим цветом на карте и метках (по умолчанию оранжевый, *My units colour*); в режиме командира над ней в 3D рисуется родной значок игры (*Markers above my units*: CommandMode / Always / Off), над купленным для фракции — мягким зелёным. Камерой юниты ставятся **мышкой**: навёл и ЛКМ (или G). Если другой мод (например, Ground Control RTS) меняет цены, техника и флот продаются по исходным ценам игры. Вверху — переключатель «Себе / Фракции» и ползунки **лимита своих юнитов** и **лимита юнитов фракции** (1–150, кнопки −10 / − / + / +10).
Вкладки: **Наземная**, **Флот**, **Оборона/склады** (морские контейнеры — на воду), **Играбельная** (выбор модулей → покупка → появляешься внутри), **Мои** (список купленного: держать/отпустить, стоп, ПРО, на карте, сесть, **платное пополнение боезапаса** на месте — «Пополнить $X», «Пополнить всех», на карте клавиша **Y**), **Стратегемы**. Окно растягивается за правый нижний угол.

### Режим командира
**F6**: курсор остаётся, и ты управляешь **только юнитами, купленными себе**, прямо в 3D — при свёрнутых панелях или вне их. В этом режиме у своих юнитов всегда видны их значки твоего цвета, а у обнаруженных врагов — их значки красным. Камера поворачивается **зажатым колесом**. Выделенные отмечены зелёными уголками и (до 6 штук) кольцом дальности оружия. **ПКМ** — двигаться строем в сторону движения (зажми ПКМ и потяни — стрелка покажет, куда юниты будут смотреть), **ПКМ по врагу** — атака, **Shift+ПКМ** — добавить точку маршрута; маршруты видны пунктиром в 3D и на карте и сбрасываются, если юнит получил другой приказ. Все клавиши приказов (K, Z, I, H, L, Y, Delete) работают на выделенных — например, область удара можно указать **Z** прямо с камеры. При открытии карты там выделяются те же юниты (и обратно), так что цель и область можно указывать и на карте. Свои наземные юниты едут к точке **напрямую через поля**; с зажатым **Alt** — по дорогам. Вверху — разделы меню, число выделенных, бюджет и «Выход». **Внизу** — кнопки всех приказов (Атака, Площадь, Стоп!, Прекратить огонь, режим огня, Держать, ПРО, Пополнить) с их клавишами и подсказка по мыши и камере. Маршрут заканчивается стрелкой — куда будет смотреть юнит. При покупке юнитов себе выбирается **режим огня** («свободный» / «по приказу») — в панели покупки или в полосе расстановки, либо клавишей **I** при расстановке. **Ctrl+1…0** — группы (1…0 — выделить, дважды — камера к группе), **Ctrl+C** — купить ещё такой же, **Ctrl+ПКМ** по нескольким врагам — список целей, по залпу по каждой по очереди. Значок над каждым своим юнитом показывает режим огня: оранжевый прицел — свободный, серый перечёркнутый — по приказу (и на карте). **F6** — выйти из режима.

### Огонь по приказу и по площади
**Огонь только по приказу** — клавиша **I** на карте или кнопка «Огонь: приказ / сам» во вкладке «Мои». Такие юниты не стреляют, пока ты не дашь цель (**K**), область (**Z**) или не включишь перехват ракет (**L**). Настройка *New units: fire only on order* сразу включает режим для всех новых покупок.

**Огонь по площади** — выдели юниты и нажми **Z**: за курсором на карте (или за точкой прицела свободной камеры / при открытом меню) идёт круг, линии показывают, кто до него достаёт. **ЛКМ** (или **G** в камере) — огонь. Орудия наводятся по баллистике в случайные точки круга, РСЗО поднимают пусковые, а их ракеты, управляемые снаряды и баллистические ракеты сами летят в круг. Кто не достаёт — пропускается. Радиус **[ ]** (по умолчанию 150 м), длительность 60 с, потом юниты возвращаются в обычный режим; **K** по пустому месту — прекратить раньше. Активные области видны на карте и в 3D.

### Дальность огня на карте
Выдели свои юниты на большой карте: **сплошное кольцо** — дальность по наземным и морским целям, **пунктир внутри** — минимальная дальность (ближе, например, артиллерия не стреляет), **голубой пунктир** — дальность по воздушным целям. При выделении до 4 юнитов дальность подписана над кругом. Наведи на врага: **зелёная** линия — этот юнит может стрелять прямо сейчас, **красная** — не достаёт; подпись: «В ЗОНЕ», «достают 2 из 3» или «ВНЕ ДОСЯГАЕМОСТИ — не хватает 3,4 км». Пунктиром — линии к уже назначенным целям. Учитывается только оружие с боезапасом. Отключить: F1 → *0. Interface* → *Weapon range on map*.

### Стратегемы
| Стратегема | Что делает | Цена | Перезарядка |
|---|---|---|---|
| Артудар | Снаряды (по умолчанию 12, 1–60 в залпе) в точку или по кругу | 15 млн | 60 с |
| Ковровая бомбардировка | Бомбы (по умолчанию 14, 4–40 в залпе) полосой по стрелке (R — повернуть), полоса видна на карте и в камере | 35 млн | 120 с |
| Ракетный удар | Ракеты (по умолчанию 4, 1–16 в залпе) отвесно, с упреждением по движущимся целям | 40 млн | 90 с |
| Тактический ЯУ | 1 тактический ядерный заряд | 250 млн | 300 с |
| Стратегический ЯУ | 1 стратегический ядерный заряд | 800 млн | 900 с |
| Десант | 1–6 единиц наземной техники на парашютах в любую точку суши, цепочкой вдоль стрелки (R/Q); линия и точки посадки видны на карте и в камере; **себе или фракции** | цена × кол-во × 1,3 | 90 с |

Боеприпас для каждого удара можно выбрать кнопками ◄ ► (или **авто**).

Для артудара, ковровой бомбардировки и ракетного удара выбирается **количество снарядов в залпе** и **число залпов** (1–10, по умолчанию с интервалом 4 с). Цена растёт пропорционально: *цена = базовая × (снарядов ÷ стандартное количество) × залпов*. В таблице — цены за стандартное количество и один залп.

### Настройки
**F1 → ORTIZ Ops - FieldCommand**: огонь по приказу и по площади (радиус, длительность), интерфейс (акцентный цвет, цвет своих юнитов, метки, дальность огня), источник денег, множитель цены, исходные цены, множитель платного пополнения, лимиты юнитов, % возврата и наград, радиусы территории, дистанции атаки, фракции, высоты и скорость снарядов, цены и перезарядки стратегем — см. таблицу [Settings](#settings) выше.

### Установка
Через **NOMM** — найди **ORTIZ Ops - FieldCommand**. Вручную — положи `ORTIZ-Ops-FieldCommand.dll` из [Releases](../../releases) в `BepInEx/plugins/ORTIZ-Ops-FieldCommand/`. Если стояла старая версия *FieldCommand* или *ORTIZ.FieldCommand* — удали её папку, настройки перенесутся сами.

---

<div align="center">

Made by **ORTIZ** · [MIT License](LICENSE)

</div>
