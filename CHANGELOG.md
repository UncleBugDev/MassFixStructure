# Changelog

All notable changes to **MassFixStructure** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-11-16

### Initial Release

#### Features
- **Smart Connected Structure Repair (Default Mode):**
  - Targets the struck piece and traverses all contiguous, touching, and overlapping pieces belonging to the same building via non-allocating Breadth-First Search (BFS).
  - Repairs entire castles, roofings, palisades, and complex architectural assemblies in one strike.
- **Radius-Based Repair Mode:**
  - Configurable spherical radius search to quickly restore all damaged structures within range.
- **Combined / Hybrid Mode:**
  - Repairs connected pieces plus any adjacent structural elements in the immediate vicinity.
- **Zero-Lag Coroutine Batching:**
  - Employs a frame-budgeted asynchronous repair runner (`StaggerBatchesOverFrames`).
  - Spreads repair operations across successive game frames when repairing hundreds of pieces, eliminating framerate hitches, packet drops, and network desync.
- **Intelligent Audio & Visual Throttling:**
  - Replaces hundreds of simultaneous deafening repair sounds with a single throttled audio trigger or periodic cluster chimes.
  - Throttled repair flash VFX to prevent GPU particle spikes.
- **Ward & Private Area Grief Protection:**
  - Strictly validates `PrivateArea.CheckAccess` on every piece prior to repairing. Prevents unauthorized repairs or interactions inside protected territories.
- **Configurable Economy & Tool Balance:**
  - `DurabilityDrainRatio`: Tune how much tool durability is deducted (default 1:1, configurable from 0% free to custom multipliers).
  - `StaminaDrainRatio`: Configurable stamina cost per repaired item or flat cost.
  - `MaxPiecesLimit`: Hard safety cap per repair pulse (default: 500 pieces).
- **Comprehensive Configuration & Hot-Reloading:**
  - All settings fully managed via BepInEx config file and synchronized in real-time.
