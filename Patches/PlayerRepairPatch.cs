using System;
using System.Collections.Generic;
using HarmonyLib;
using MassFixStructure.Configuration;
using MassFixStructure.Core;
using UnityEngine;

namespace MassFixStructure.Patches
{
    [HarmonyPatch(typeof(Player), "Repair")]
    public static class PlayerRepairPatch
    {
        private static readonly Func<Player, Piece, bool>? s_checkCanRemovePieceDelegate;

        static PlayerRepairPatch()
        {
            try
            {
                var method = AccessTools.Method(typeof(Player), "CheckCanRemovePiece", new[] { typeof(Piece) });
                if (method != null)
                {
                    s_checkCanRemovePieceDelegate = AccessTools.MethodDelegate<Func<Player, Piece, bool>>(method);
                }
            }
            catch (Exception ex)
            {
                MassFixStructurePlugin.Log?.LogWarning($"Could not bind CheckCanRemovePiece delegate: {ex.Message}");
            }
        }

        [HarmonyPrefix]
        public static bool Prefix(Player __instance, ItemDrop.ItemData toolItem, Piece repairPiece)
        {
            if (!ModConfig.ModEnabled.Value)
            {
                return true;
            }

            if (!__instance.InPlaceMode())
            {
                return true;
            }

            // Modifier key check if HoldModifierKey mode is configured
            if (ModConfig.Activation.Value == ActivationMode.HoldModifierKey)
            {
                if (!Input.GetKey(ModConfig.ModifierKey.Value))
                {
                    return true; // Fall back to vanilla single repair
                }
            }

            Piece hoveringPiece = repairPiece != null ? repairPiece : __instance.GetHoveringPiece();
            if (hoveringPiece == null)
            {
                return true;
            }

            // Check if player has permission to remove/repair hovering piece
            if (s_checkCanRemovePieceDelegate != null)
            {
                if (!s_checkCanRemovePieceDelegate(__instance, hoveringPiece))
                {
                    return true;
                }
            }
            else if (hoveringPiece.m_craftingStation != null && ModConfig.RequireWorkbench.Value)
            {
                if (!CraftingStation.HaveBuildStationInRange(hoveringPiece.m_craftingStation.m_name, __instance.transform.position) &&
                    !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
                {
                    return true;
                }
            }

            if (!PrivateArea.CheckAccess(hoveringPiece.transform.position))
            {
                return true;
            }

            if (toolItem == null)
            {
                return true;
            }

            // Tool break prevention
            if (ModConfig.PreventToolBreak.Value && toolItem.m_shared.m_useDurability && toolItem.m_durability <= 1.0f)
            {
                __instance.Message(MessageHud.MessageType.Center, "$msg_toolbroken");
                return false;
            }

            // If a mass repair batch is currently executing, suppress duplicate triggers
            if (MassRepairRunner.Instance != null && MassRepairRunner.Instance.IsRunning)
            {
                return false;
            }

            WearNTear originWnt = hoveringPiece.GetComponent<WearNTear>();
            if (originWnt == null)
            {
                return true;
            }

            // Find all damaged pieces to repair
            List<WearNTear> piecesToRepair = StructureGraphTraverser.FindPiecesToRepair(
                __instance,
                originWnt,
                ModConfig.Mode.Value,
                ModConfig.SearchRadius.Value,
                ModConfig.MaxRepairsPerSwing.Value,
                ModConfig.MaxSearchVisitedNodes.Value,
                ModConfig.RequireWorkbench.Value
            );

            // Execute mass repair via Runner
            if (MassRepairRunner.Instance != null)
            {
                MassRepairRunner.Instance.ExecuteRepair(__instance, toolItem, hoveringPiece, piecesToRepair);
            }

            // Suppress vanilla single-repair execution
            return false;
        }
    }
}
