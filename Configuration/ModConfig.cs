using BepInEx.Configuration;
using MassFixStructure.Core;
using UnityEngine;

namespace MassFixStructure.Configuration
{
    public static class ModConfig
    {
        // 1. General
        public static ConfigEntry<bool> ModEnabled = null!;
        public static ConfigEntry<RepairMode> Mode = null!;
        public static ConfigEntry<ActivationMode> Activation = null!;
        public static ConfigEntry<KeyCode> ModifierKey = null!;

        // 2. Search & Limits
        public static ConfigEntry<int> MaxRepairsPerSwing = null!;
        public static ConfigEntry<float> SearchRadius = null!;
        public static ConfigEntry<int> MaxSearchVisitedNodes = null!;
        public static ConfigEntry<bool> RequireWorkbench = null!;

        // 3. Performance & Network
        public static ConfigEntry<bool> StaggerBatchesOverFrames = null!;
        public static ConfigEntry<int> BatchSizePerFrame = null!;

        // 4. Costs & Protection
        public static ConfigEntry<CostMode> Cost = null!;
        public static ConfigEntry<float> PerPieceStaminaRatio = null!;
        public static ConfigEntry<float> PerPieceDurabilityRatio = null!;
        public static ConfigEntry<bool> PreventToolBreak = null!;

        // 5. Feedback & UI
        public static ConfigEntry<PlaySoundMode> SoundMode = null!;
        public static ConfigEntry<bool> ShowHudNotification = null!;

        public static void Initialize(ConfigFile config)
        {
            // General
            ModEnabled = config.Bind(
                "1 - General",
                "ModEnabled",
                true,
                "Master toggle for the MassFixStructure mod."
            );

            Mode = config.Bind(
                "1 - General",
                "RepairMode",
                RepairMode.Connected,
                "Algorithm used to find pieces to repair:\n" +
                "- Connected: Traverses physically touching structural pieces (recommended).\n" +
                "- ConnectedSameCategory: Connected BFS restricted to the same piece category (e.g. only Building or Furniture).\n" +
                "- Radius: All damaged pieces within SearchRadius."
            );

            Activation = config.Bind(
                "1 - General",
                "ActivationMode",
                ActivationMode.AlwaysOnRepair,
                "When mass repair triggers:\n" +
                "- AlwaysOnRepair: Every hammer repair action repairs the structure.\n" +
                "- HoldModifierKey: Only triggers when holding the ModifierKey (otherwise standard single repair)."
            );

            ModifierKey = config.Bind(
                "1 - General",
                "ModifierKey",
                KeyCode.LeftShift,
                "Key to hold for mass repair when ActivationMode is set to HoldModifierKey."
            );

            // Search & Limits
            MaxRepairsPerSwing = config.Bind(
                "2 - Search & Limits",
                "MaxRepairsPerSwing",
                200,
                new ConfigDescription("Maximum number of damaged pieces to fix in a single repair action.", new AcceptableValueRange<int>(1, 1000))
            );

            SearchRadius = config.Bind(
                "2 - Search & Limits",
                "SearchRadius",
                30.0f,
                new ConfigDescription("Maximum distance (in meters) from the targeted piece to include in the repair search.", new AcceptableValueRange<float>(5f, 100f))
            );

            MaxSearchVisitedNodes = config.Bind(
                "2 - Search & Limits",
                "MaxSearchVisitedNodes",
                1000,
                new ConfigDescription("Safety limit on the number of piece nodes visited during connected graph traversal.", new AcceptableValueRange<int>(50, 5000))
            );

            RequireWorkbench = config.Bind(
                "2 - Search & Limits",
                "RequireWorkbench",
                true,
                "If true, repairs will respect workbench/crafting station coverage required by the piece."
            );

            // Performance & Network
            StaggerBatchesOverFrames = config.Bind(
                "3 - Performance & Network",
                "StaggerBatchesOverFrames",
                true,
                "If true, spreads piece repairs across consecutive frames to prevent server/network packet loss on large structures."
            );

            BatchSizePerFrame = config.Bind(
                "3 - Performance & Network",
                "BatchSizePerFrame",
                20,
                new ConfigDescription("Number of pieces repaired per physics/render frame when batching is active.", new AcceptableValueRange<int>(1, 100))
            );

            // Costs & Protection
            Cost = config.Bind(
                "4 - Costs & Protection",
                "CostMode",
                CostMode.VanillaPerSwing,
                "How stamina and hammer durability are consumed:\n" +
                "- VanillaPerSwing: Standard single swing cost for the whole cluster.\n" +
                "- PerPieceScaled: Extra small stamina and durability cost per additional piece repaired.\n" +
                "- Free: No stamina or tool durability consumed."
            );

            PerPieceStaminaRatio = config.Bind(
                "4 - Costs & Protection",
                "PerPieceStaminaRatio",
                0.05f,
                new ConfigDescription("Multiplier for additional stamina consumed per extra piece if CostMode is PerPieceScaled (e.g. 0.05 = 5% of base swing stamina).", new AcceptableValueRange<float>(0f, 1f))
            );

            PerPieceDurabilityRatio = config.Bind(
                "4 - Costs & Protection",
                "PerPieceDurabilityRatio",
                0.05f,
                new ConfigDescription("Multiplier for additional durability drained per extra piece if CostMode is PerPieceScaled.", new AcceptableValueRange<float>(0f, 1f))
            );

            PreventToolBreak = config.Bind(
                "4 - Costs & Protection",
                "PreventToolBreak",
                true,
                "If true, aborts repair early if the hammer durability falls to 1.0 or below, preventing it from breaking."
            );

            // Feedback & UI
            SoundMode = config.Bind(
                "5 - Feedback & UI",
                "SoundMode",
                PlaySoundMode.SingleAtOrigin,
                "Audio and visual particle feedback for mass repair."
            );

            ShowHudNotification = config.Bind(
                "5 - Feedback & UI",
                "ShowHudNotification",
                true,
                "Show a consolidated HUD message (e.g. 'MassFix: Repaired 42 pieces') upon completion."
            );
        }
    }
}
