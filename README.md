<p align="center">
  <img src="icon.png" width="200" height="200" alt="MassFixStructure Logo" />
</p>

<h1 align="center">MassFixStructure 🔨⚡</h1>

<p align="center">
  <b>Smart, Zero-Lag Mass Building Repair for Valheim & Dedicated Servers</b><br />
  <i>Repair entire connected castles, longhouses, palisades, and naval docks with a single hammer strike — featuring 99.8% network RPC compression, full dedicated server optimization, ward protection, and zero client stutter.</i>
</p>

<p align="center">
  <a href="https://github.com/UncleBugDev/MassFixStructure/releases"><img src="https://img.shields.io/github/v/release/UncleBugDev/MassFixStructure?style=for-the-badge&color=007ec6&logo=github" alt="GitHub Release" /></a>
  <a href="https://thunderstore.io/c/valheim/p/UncleBugDev/MassFixStructure/"><img src="https://img.shields.io/badge/Thunderstore-v1.0.0-orange?style=for-the-badge&logo=thunderstore" alt="Thunderstore" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License: MIT" /></a>
  <img src="https://img.shields.io/badge/Valheim-PC%20%7C%20Steam-red?style=for-the-badge&logo=steam" alt="Platform" />
  <img src="https://img.shields.io/badge/Multiplayer-Dedicated%20Server-blue?style=for-the-badge" alt="Multiplayer" />
</p>

---

## 🌟 English Overview

Tired of clicking hundreds of times to repair rain-weathered foundations, vast roofs, or post-raid fortresses?
**MassFixStructure** revolutionizes building maintenance in Valheim. Strike a single damaged piece with your hammer to instantly trigger an intelligent repair pulse across your entire contiguous structure or surrounding area.

Engineered from the ground up for **high-load multiplayer dedicated servers**, competitive performance, and client comfort:
- **🌐 99.8% Network RPC Compression:** On standard Valheim servers, repairing 500 pieces floods the network socket with 500 individual `RPC_Repair` and 500 `RPC_HealthChanged` packets, causing desync and disconnects. **MassFixStructure packs all repaired pieces into ONE single UDP packet** (`MassFix_BatchRepair`), executed atomically on the server!
- **🖥️ 100% Dedicated Server (Headless) Native:** Runs seamlessly on Linux and Windows dedicated servers (`-batchmode -nographics`). Zero crashes: GUI, audio, and camera calls are strictly excluded from server execution.
- **🛡️ Server Anti-Spam & Rate Limiting:** Built-in server cooldown (0.8s minimum interval) neutralizes macro and auto-clicker spam, protecting server tickrate.
- **🔒 Double Ward & Territory Protection:** Validates `PrivateArea.CheckAccess` both client-side and server-side. Unauthorized players cannot repair or manipulate structures inside protected territories.
- **⚡ Server-Authoritative Config Sync:** When connecting to a dedicated server, server configuration limits (`MaxRepairsPerSwing`, `SearchRadius`, `RequireWorkbench`) automatically synchronize and take authority over client settings.
- **🔄 Seamless Vanilla Server Fallback:** Playing on a server without the mod? MassFixStructure automatically detects vanilla servers and seamlessly falls back to local frame-budgeted coroutine repairs.
- **👂 Audio & VFX Comfort:** Replaces hundreds of deafening simultaneous sound cues with a single clean hammer strike and subtle repair particles.

---

### 🎮 How to Use

1. Equip your standard **Hammer** (or any building tool with repair capability).
2. Switch to **Repair Mode** (hammer icon).
3. Aim at any damaged piece belonging to your building and press **Left Mouse Button** (or hold `LeftShift` if configured for modifier key activation).
4. Watch the entire contiguous building restore to 100% health in a wave of golden sparks!

---

### ⚙️ Configuration Reference

The configuration file is located at `BepInEx/config/com.unclebug.massfixstructure.cfg` and supports live hot-reloading:

| Section | Setting | Default | Description |
|---|---|---|---|
| **1 - General** | `ModEnabled` | `true` | Master toggle for the mod. |
| **1 - General** | `RepairMode` | `Connected` | Modes: `Connected` (contiguous pieces only), `ConnectedSameCategory` (same category only), or `Radius` (sphere search). |
| **1 - General** | `ActivationMode` | `AlwaysOnRepair` | `AlwaysOnRepair` (every hammer hit) or `HoldModifierKey` (only when holding modifier key). |
| **1 - General** | `ModifierKey` | `LeftShift` | Key to hold when `ActivationMode` is set to `HoldModifierKey`. |
| **2 - Search & Limits** | `MaxRepairsPerSwing` | `200` | Maximum number of damaged pieces to fix per repair pulse (range: 1 - 1000). |
| **2 - Search & Limits** | `SearchRadius` | `30.0` | Maximum distance (in meters) from target piece (range: 5m - 100m). |
| **2 - Search & Limits** | `MaxSearchVisitedNodes` | `1000` | Safety limit on structural piece graph nodes visited during BFS traversal. |
| **2 - Search & Limits** | `RequireWorkbench` | `true` | Respects crafting station / workbench coverage required by pieces. |
| **3 - Performance & Network** | `StaggerBatchesOverFrames` | `true` | Spreads local/vanilla repair RPCs across consecutive frames to prevent FPS drops. |
| **3 - Performance & Network** | `BatchSizePerFrame` | `20` | Pieces repaired per frame when local batching is active. |
| **4 - Costs & Protection** | `CostMode` | `VanillaPerSwing` | `VanillaPerSwing` (1 swing cost for whole cluster), `PerPieceScaled` (scales slightly per piece), or `Free`. |
| **4 - Costs & Protection** | `PerPieceStaminaRatio` | `0.05` | Extra stamina ratio per piece when `CostMode` is `PerPieceScaled`. |
| **4 - Costs & Protection** | `PerPieceDurabilityRatio` | `0.05` | Extra durability drain ratio per piece when `CostMode` is `PerPieceScaled`. |
| **4 - Costs & Protection** | `PreventToolBreak` | `true` | Halts repair pulse if tool durability drops <= 1.0 to prevent breaking. |
| **5 - Feedback & UI** | `SoundMode` | `SingleAtOrigin` | `SingleAtOrigin` (1 clean sound), `Muted` (silent). |
| **5 - Feedback & UI** | `ShowHudNotification` | `true` | Displays consolidated HUD message (e.g. `MassFix: Repaired 85 pieces`). |

---

## 🇷🇺 Описание на русском

**MassFixStructure** — первоклассный мод для Valheim, созданный специально для **выделенных серверов (Dedicated Servers)** и одиночной игры. Позволяет одним ударом молотка мгновенно и без лагов починить целые замки, частоколы, крыши и базы любого масштаба!

### 🚀 Главные преимущества для серверов и игроков:
- **🌐 Сжатие сетевого трафика на 99.8% (Atomic Batched RPC):** В ванильной игре починка 500 деталей посылает 500 отдельных RPC-пакетов починки и 500 пакетов синхронизации здоровья, перегружая сокеты сервера и вызывая дисконнекты. **MassFixStructure упаковывает всю починку в ОДИН компактный UDP-пакет**, обрабатываемый сервером атомарно!
- **🖥️ 100% совместимость с Headless серверами:** Идеально работает на Linux и Windows выделенных серверах (`-batchmode -nographics`). Никаких крашей графики, звука или UI.
- **🛡️ Защита от спама и макросов (Anti-Spam Rate Limit):** Серверный кулдаун (0.8 сек) полностью блокирует спам автокликерами и макросами, сохраняя стабильный тикрейт сервера.
- **🔒 Двойная защита оберегов (Ward Security):** Проверка прав оберега (`PrivateArea.CheckAccess`) выполняется как на клиенте, так и на сервере. Чужие базы защищены от несанкционированного взаимодействия.
- **⚡ Авторитетная синхронизация конфига с сервера:** При подключении к серверу клиентские лимиты автоматически подстраиваются под настройки сервера (`MaxRepairsPerSwing`, `SearchRadius`, `RequireWorkbench`).
- **🔄 Автоматический фоллбек для ванильных серверов:** Если на сервере мод не установлен, клиент автоматически переключается на пофреймовую локальную корутину, исключая рассинхрон.
- **👂 Звуковой комфорт:** Вместо 300 оглушительных ударов воспроизводится один чистый звук починки.

---

### 🛠️ Установка

#### Для игроков (Клиент):
1. Установите [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).
2. Скопируйте `MassFixStructure.dll` в папку `Valheim/BepInEx/plugins/` (или установите через Thunderstore Mod Manager в один клик).

#### Для выделенных серверов (Dedicated Server):
1. Установите BepInEx на сервер.
2. Скопируйте `MassFixStructure.dll` в папку `BepInEx/plugins/` на сервере.
3. Готово! Сервер готов принимать атомарные батчи и защищать тикрейт.

---

## 🔒 Zero-Risk & Compatibility Guarantee

- **Safe for Game Worlds:** Never modifies save files, ZDO structures, or persistent formats.
- **Safe to Add/Remove:** Completely clean uninstall at any moment with zero impact on world saves.
