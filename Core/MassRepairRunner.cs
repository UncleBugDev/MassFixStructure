using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MassFixStructure.Configuration;
using UnityEngine;

namespace MassFixStructure.Core
{
    public class MassRepairRunner : MonoBehaviour
    {
        public static MassRepairRunner Instance { get; private set; } = null!;

        public bool IsRunning { get; private set; }

        private static readonly Func<Player, float>? s_getBuildStaminaDelegate;

        static MassRepairRunner()
        {
            try
            {
                var method = AccessTools.Method(typeof(Player), "GetBuildStamina");
                if (method != null)
                {
                    s_getBuildStaminaDelegate = AccessTools.MethodDelegate<Func<Player, float>>(method);
                }
            }
            catch (Exception ex)
            {
                MassFixStructurePlugin.Log?.LogWarning($"Could not bind GetBuildStamina delegate: {ex.Message}");
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        public static float GetPlayerBuildStamina(Player player)
        {
            if (s_getBuildStaminaDelegate != null && player != null)
            {
                return s_getBuildStaminaDelegate(player);
            }
            return 5f;
        }

        public void ExecuteRepair(
            Player player,
            ItemDrop.ItemData toolItem,
            Piece originPiece,
            List<WearNTear> piecesToRepair)
        {
            if (piecesToRepair == null || piecesToRepair.Count == 0)
            {
                if (ModConfig.ShowHudNotification.Value && player != null && originPiece != null)
                {
                    player.Message(MessageHud.MessageType.TopLeft, originPiece.m_name + " $msg_doesnotneedrepair");
                }
                return;
            }

            // Trigger player swing animation and look direction
            player.FaceLookDirection();
            if (toolItem.m_shared.m_attack != null)
            {
                player.GetZAnim()?.SetTrigger(toolItem.m_shared.m_attack.m_attackAnimation);
            }

            // Audio & visual effects at origin piece
            if (ModConfig.SoundMode.Value == PlaySoundMode.SingleAtOrigin && originPiece != null && originPiece.m_placeEffect != null)
            {
                originPiece.m_placeEffect.Create(
                    originPiece.transform.position,
                    originPiece.transform.rotation,
                    null,
                    1f,
                    -1,
                    player.GetZDOID());
            }

            // Handle base cost immediately
            CostMode costMode = ModConfig.Cost.Value;
            if (costMode == CostMode.VanillaPerSwing || costMode == CostMode.PerPieceScaled)
            {
                player.UseStamina(GetPlayerBuildStamina(player));
                if (toolItem.m_shared.m_attack != null)
                {
                    player.UseEitr(toolItem.m_shared.m_attack.m_attackEitr);
                }

                if (toolItem.m_shared.m_useDurability)
                {
                    toolItem.m_durability -= toolItem.m_shared.m_useDurabilityDrain * Game.m_durabilityRate;
                }
            }

            bool stagger = ModConfig.StaggerBatchesOverFrames.Value;
            int batchSize = ModConfig.BatchSizePerFrame.Value;

            if (!stagger || piecesToRepair.Count <= batchSize)
            {
                RunImmediate(player, toolItem, piecesToRepair, costMode);
            }
            else
            {
                StartCoroutine(RunBatched(player, toolItem, piecesToRepair, batchSize, costMode));
            }
        }

        private void RunImmediate(
            Player player,
            ItemDrop.ItemData toolItem,
            List<WearNTear> pieces,
            CostMode costMode)
        {
            int repairedCount = 0;
            bool toolBrokenHalt = false;

            float extraStamina = GetPlayerBuildStamina(player) * ModConfig.PerPieceStaminaRatio.Value;
            float extraDurability = (toolItem.m_shared.m_useDurability
                ? toolItem.m_shared.m_useDurabilityDrain * Game.m_durabilityRate
                : 0f) * ModConfig.PerPieceDurabilityRatio.Value;

            for (int i = 0; i < pieces.Count; i++)
            {
                WearNTear wnt = pieces[i];
                if (wnt == null || !wnt)
                {
                    continue;
                }

                ZNetView nview = wnt.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid())
                {
                    continue;
                }

                // Ward access double check
                if (!PrivateArea.CheckAccess(wnt.transform.position, 0f, flash: false))
                {
                    continue;
                }

                if (ModConfig.PreventToolBreak.Value && toolItem.m_shared.m_useDurability && toolItem.m_durability <= 1.0f)
                {
                    toolBrokenHalt = true;
                    break;
                }

                if (wnt.Repair())
                {
                    repairedCount++;

                    if (costMode == CostMode.PerPieceScaled && i > 0)
                    {
                        if (extraStamina > 0f)
                        {
                            player.UseStamina(extraStamina);
                        }

                        if (extraDurability > 0f && toolItem.m_shared.m_useDurability)
                        {
                            toolItem.m_durability -= extraDurability;
                        }
                    }
                }
            }

            ShowCompletionHud(player, repairedCount, toolBrokenHalt);
        }

        private IEnumerator RunBatched(
            Player player,
            ItemDrop.ItemData toolItem,
            List<WearNTear> pieces,
            int batchSize,
            CostMode costMode)
        {
            IsRunning = true;
            int repairedCount = 0;
            bool toolBrokenHalt = false;

            float extraStamina = GetPlayerBuildStamina(player) * ModConfig.PerPieceStaminaRatio.Value;
            float extraDurability = (toolItem.m_shared.m_useDurability
                ? toolItem.m_shared.m_useDurabilityDrain * Game.m_durabilityRate
                : 0f) * ModConfig.PerPieceDurabilityRatio.Value;

            int processedInCurrentFrame = 0;

            for (int i = 0; i < pieces.Count; i++)
            {
                if (player == null || player.IsDead() || !player.IsItemEquiped(toolItem))
                {
                    break;
                }

                WearNTear wnt = pieces[i];
                if (wnt == null || !wnt)
                {
                    continue;
                }

                ZNetView nview = wnt.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid())
                {
                    continue;
                }

                // Check ward access
                if (!PrivateArea.CheckAccess(wnt.transform.position, 0f, flash: false))
                {
                    continue;
                }

                // Check tool break protection
                if (ModConfig.PreventToolBreak.Value && toolItem.m_shared.m_useDurability && toolItem.m_durability <= 1.0f)
                {
                    toolBrokenHalt = true;
                    break;
                }

                if (wnt.Repair())
                {
                    repairedCount++;

                    if (costMode == CostMode.PerPieceScaled && i > 0)
                    {
                        if (extraStamina > 0f)
                        {
                            player.UseStamina(extraStamina);
                        }

                        if (extraDurability > 0f && toolItem.m_shared.m_useDurability)
                        {
                            toolItem.m_durability -= extraDurability;
                        }
                    }
                }

                processedInCurrentFrame++;
                if (processedInCurrentFrame >= batchSize)
                {
                    processedInCurrentFrame = 0;
                    yield return null;
                }
            }

            IsRunning = false;
            ShowCompletionHud(player, repairedCount, toolBrokenHalt);
        }

        private void ShowCompletionHud(Player? player, int repairedCount, bool toolBrokenHalt)
        {
            if (player == null || !ModConfig.ShowHudNotification.Value)
            {
                return;
            }

            if (toolBrokenHalt)
            {
                player.Message(MessageHud.MessageType.Center, $"MassFix: Stopped to protect tool durability! ({repairedCount} repaired)");
            }
            else if (repairedCount > 0)
            {
                player.Message(MessageHud.MessageType.TopLeft, $"MassFix: Repaired {repairedCount} pieces");
            }
            else
            {
                player.Message(MessageHud.MessageType.TopLeft, "$msg_doesnotneedrepair");
            }
        }
    }
}
