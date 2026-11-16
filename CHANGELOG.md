# Changelog

All notable changes to **MassFixStructure** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-16

### Initial Release

#### 🌐 Dedicated Server & Multiplayer Architecture
- **Headless Server Compatibility:**
  - 100% clean execution on headless Linux/Windows dedicated servers (`-batchmode -nographics`).
  - Zero crashes: UI, audio, camera, and HUD logic are strictly guarded against server execution.
- **Atomic Batched RPC Protocol (`MassFix_BatchRepair` & `MassFix_SyncRepairs`):**
  - Reduces 500–1000 individual vanilla repair and health RPCs down to **1 single compact UDP packet** (over 99.8% network traffic reduction).
  - Client packs all target `ZDOID`s into a single `ZPackage`; server updates `ZDOMan` memory atomically and broadcasts 1 consolidated sync packet.
- **Server Anti-Spam & Rate Limiting:**
  - Enforces a per-player cooldown (0.8s minimum interval) on the server to neutralize macro or auto-clicker spam.
- **Server-Side Ward & Territory Security:**
  - Server strictly re-validates `PrivateArea.CheckAccess` before applying health updates to any ZDO.
- **Server-Authoritative Configuration Sync:**
  - Server broadcasts authoritative ceilings (`MaxRepairsPerSwing`, `SearchRadius`, `RequireWorkbench`) upon player connection (`MassFix_ServerHello`). Client limits automatically conform to server policy.
- **Graceful Vanilla Server Fallback:**
  - If connected to a server without MassFixStructure installed, the client automatically falls back to local staggered coroutine repairs without desync.

#### 🔨 Core Repair Features & Modes
- **Smart Connected Structure Repair (Default Mode):**
  - Targets the struck piece and traverses all contiguous, touching, and overlapping pieces belonging to the same building via non-allocating Breadth-First Search (BFS).
  - Repairs entire castles, roofs, palisades, and complex architectural assemblies in one strike.
- **Radius-Based Repair Mode:**
  - Configurable spherical radius search to quickly restore all damaged structures within range.
- **Connected Same Category Mode:**
  - BFS traversal restricted to the same piece category (e.g. only Building or only Furniture).
- **Zero-Lag Coroutine Batching (Client / Vanilla Fallback):**
  - Frame-budgeted asynchronous repair runner (`StaggerBatchesOverFrames`).
  - Spreads repair operations across successive frames when repairing mega-structures, eliminating framerate hitches.
- **Intelligent Audio & Visual Throttling:**
  - Replaces hundreds of simultaneous deafening repair sounds with a single throttled audio trigger at the origin piece.
  - Suppresses screen-blinding particle spam while providing clean visual feedback.
- **Ward & Private Area Grief Protection:**
  - Strictly validates player ward permission (`PrivateArea.CheckAccess`) on every piece prior to repairing.
- **Configurable Economy & Tool Balance:**
  - `DurabilityDrainRatio`: Multiplier for hammer durability consumption (from 0% free to custom multipliers).
  - `StaminaDrainRatio`: Configurable stamina cost per repaired item.
  - `PreventToolBreak`: Automatically halts repair pulse if tool durability drops <= 1.0, protecting valuable equipment.
  - `MaxRepairsPerSwing`: Hard safety cap per repair pulse (default: 200 pieces).
- **Comprehensive Hot-Reloadable Config:**
  - All settings fully managed via BepInEx config file and synchronized in real-time.
