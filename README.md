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
| 🎯 **RTS orders on the map** | Move, waypoints, hold, focus fire from current position, advance to firing range, automatic missile defence, scrap with refund. |
| 🚙 **Playable KAR / BOTE units** | Pick modules for every hardpoint, buy and spawn right inside. Get out — the vehicle stays; get back in from the menu. |
| 🏗️ **Defences and supplies** | SAM sites, gun positions, radars, depots, ammo / fuel / repair containers. Supplies don't use unit slots. Stopped units near a friendly ammo container, depot or supply truck are **rearmed automatically**; bought supply trucks drive to units that need ammo on their own. |
| ☢️ **Stratagems** | Artillery, carpet bombing, missile strike, tactical and strategic nuke, airdrop. Point or area targeting with adjustable radius. |
| 💰 **Kill rewards** | Get a share of the reward when your units (default 50 %) or stratagems (default 15 %) destroy enemies or missiles. |

## Controls

All keys can be rebound in the Configuration Manager (F1).

### General
| Key | Action |
|---|---|
| **F6** | Open / close the purchase menu |
| **J** | Get out of the current KAR / BOTE vehicle (it stays where it is) |

### Placing units and targeting stratagems
| Key | Action |
|---|---|
| **LMB** | Place the unit / choose the target on the map |
| **G** | Place the unit / choose the target at the camera's aim point |
| **R** / **Q** | Rotate the unit (or the strike / airdrop direction) clockwise / counter-clockwise: tap — 45° step, hold — keeps turning |
| **T** | Switch stratagem targeting: point ⇄ area |
| **]** / **=** / **Num+** | Increase the area radius |
| **[** / **-** / **Num-** | Decrease the area radius |
| **X** / **Backspace** | Cancel |

### Orders on the map (select your units first)
| Key | Action |
|---|---|
| **RMB** | Move (**Shift+RMB** — add a waypoint) |
| **H** | Toggle *hold position* |
| **K** | Fire at the enemy / missile under the cursor **from the current position** (artillery and SAMs engage at max range). On empty ground — clear the target |
| **Shift+K** | Advance to firing range and attack |
| **L** | Automatic missile defence — units pick and shoot down enemy missiles themselves (strategic and nuclear first) |
| **Y** | Rearm the selected own units right where they stand, for money |
| **Delete** | Scrap the selected purchased units (refund 50 % by default) |

## Purchase menu

The menu (F6) has a flat dark design with a side menu of tabs; at the bottom of the side menu are your budget, faction and the money earned from kills. The accent colour (orange / cyan / lime) is set in F1 → *0. Interface*. At the top are the *For me / For the faction* switch and two sliders (with −10 / − / + / +10 buttons) for **your unit limit** and the **faction unit limit**, 1–150 each. Tabs:

- **Ground** — AA, radars, artillery, tanks, IFVs, logistics… grouped by category.
- **Naval** — carriers, LHAs, cruisers, destroyers, frigates, corvettes, support ships.
- **Defences / depots** — static SAMs, gun positions, radars, depots and containers.
- **Playable** — KAR (ground) and BOTE (naval) units you can drive yourself: *Modules & buy* → choose a module for each hardpoint with ◄ ► → place on the map or with the camera → you spawn inside.
- **Mine** — every unit you bought: hold/release, stop, missile defence, show on map, get in (playable units), **paid rearm** on the spot (*Rearm $X*, *Rearm all*), scrap.
- **Stratagems** — call-in support, see below.

The window can be resized by dragging its bottom-right corner. Naval supply containers / pallets are placed on water. Units are shown as tiles with type, faction and price; **Camera** and **On map** start placement. The **EN / RU** button switches the language.

Units you bought **for yourself** are painted in their own colour on the map and on the 3D markers (also in the Ground Control RTS view). In command mode (menu open or placing) an RTS-style symbol is shown above each of your units in the 3D view (*Markers above my units*: CommandMode / Always / Off). With the camera you place with the **mouse**: point and click LMB (or G). Default — orange; change it in F1 → *0. Interface* → *My units colour*.

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
| Economy | Funds source (personal / faction treasury) | Personal |
| Economy | Price multiplier / minimum price | ×1 / 0.5 M |
| Economy | Own unit limit / faction unit limit (1–150, also adjustable right in the F6 menu) | 60 / 60 |
| Economy | Refund when scrapping | 50 % |
| Economy | Kill reward share (own units) / also for faction units | 50 % / on |
| Territory | Own territory only | on |
| Territory | Radius from airbase: ground / naval | 12 km / 30 km |
| Territory | Radius from own units / min. distance to enemy | 3 km / 2 km |
| Territory | Min. water depth for ships | 8 m |
| Behaviour | Spawn in *hold position* | on |
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
- 🎯 **Приказы на карте** — движение, маршрут, держать позицию, огонь с места, выдвижение на дистанцию огня, перехват ракет, списание с возвратом денег.
- 🚙 **Играбельная техника KAR / BOTE** — выбор модулей на каждую точку подвеса, сразу садишься внутрь, можно выйти и сесть снова.
- 🏗️ **Оборона и снабжение** — ЗРК, огневые точки, радары, склады, контейнеры с боеприпасами/топливом/ремонтом. Снабжение не занимает слоты юнитов. Стоящие юниты рядом с союзным контейнером боеприпасов, складом или машиной снабжения **пополняют боезапас автоматически**; купленные машины снабжения сами ездят к тем, кому нужны патроны.
- ☢️ **Стратегемы** — артудар, ковровая бомбардировка, ракетный удар, тактический и стратегический ядерный удар, десант. По точке или по области с регулируемым радиусом.
- 💰 **Награды** — доля награды за уничтожения твоими юнитами (50 %) и стратегемами (15 %).

### Управление
| Клавиша | Действие |
|---|---|
| **F6** | Меню покупки |
| **J** | Выйти из техники KAR / BOTE |
| **ЛКМ** / **G** | Поставить юнит / выбрать цель — на карте / в точку прицела камеры |
| **R** / **Q** | Повернуть юнит или направление удара/десанта по/против часовой: нажатие — шаг 45°, зажать — крутится сам |
| **T** | Стратегема: точка ⇄ область |
| **]** **=** **Num+** / **[** **-** **Num-** | Увеличить / уменьшить радиус области |
| **X** / **Backspace** | Отмена |
| **ПКМ** (**Shift+ПКМ**) | Двигаться (добавить точку маршрута) |
| **H** | Держать позицию |
| **K** | Огонь по врагу/ракете под курсором с текущей позиции; по пустому месту — сбросить цель |
| **Shift+K** | Выдвинуться на дистанцию огня и атаковать |
| **L** | Автоматический перехват ракет (стратегические и ядерные — первыми) |
| **Y** | Пополнить боезапас выделенных своих юнитов на месте за деньги |
| **Delete** | Списать выделенных купленных юнитов с возвратом 50 % |

### Меню (F6)
Окно в тёмном «плоском» стиле с боковым меню вкладок; внизу меню — бюджет, фракция и заработок. Акцентный цвет (оранжевый / бирюзовый / лаймовый) — F1 → *0. Interface*. Своя купленная техника выделяется своим цветом на карте и метках (по умолчанию оранжевый, *My units colour*). Если другой мод (например, Ground Control RTS) меняет цены, техника и флот продаются по исходным ценам игры. Вверху — переключатель «Себе / Фракции» и ползунки **лимита своих юнитов** и **лимита юнитов фракции** (1–150, кнопки −10 / − / + / +10).
Вкладки: **Наземная**, **Флот**, **Оборона/склады** (морские контейнеры — на воду), **Играбельная** (выбор модулей → покупка → появляешься внутри), **Мои** (список купленного: держать/отпустить, стоп, ПРО, на карте, сесть, **платное пополнение боезапаса** на месте — «Пополнить $X», «Пополнить всех», на карте клавиша **Y**), **Стратегемы**. Окно растягивается за правый нижний угол.

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
**F1 → ORTIZ Ops - FieldCommand**: источник денег, множитель цены, лимиты юнитов, % возврата и наград, радиусы территории, дистанции атаки, фракции, высоты и скорость снарядов, цены и перезарядки стратегем — см. таблицу [Settings](#settings) выше.

### Установка
Через **NOMM** — найди **ORTIZ Ops - FieldCommand**. Вручную — положи `ORTIZ-Ops-FieldCommand.dll` из [Releases](../../releases) в `BepInEx/plugins/ORTIZ-Ops-FieldCommand/`. Если стояла старая версия *FieldCommand* или *ORTIZ.FieldCommand* — удали её папку, настройки перенесутся сами.

---

<div align="center">

Made by **ORTIZ** · [MIT License](LICENSE)

</div>
