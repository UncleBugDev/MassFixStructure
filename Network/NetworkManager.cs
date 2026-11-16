using System;
using System.Collections.Generic;
using HarmonyLib;
using MassFixStructure.Configuration;
using MassFixStructure.Core;
using UnityEngine;

namespace MassFixStructure.Network
{
    public static class NetworkManager
    {
        public const string RPC_SERVER_HELLO = "MassFix_ServerHello";
        public const string RPC_CLIENT_HELLO = "MassFix_ClientHello";
        public const string RPC_BATCH_REPAIR = "MassFix_BatchRepair";
        public const string RPC_SYNC_REPAIRS = "MassFix_SyncRepairs";

        public static bool ServerHasMod { get; set; } = false;
        public static int ServerMaxRepairs { get; private set; } = 200;
        public static float ServerSearchRadius { get; private set; } = 30f;
        public static bool ServerRequireWorkbench { get; private set; } = true;

        // Anti-spam rate limiting on server: maps peer UID to last repair timestamp
        private static readonly Dictionary<long, float> s_peerLastRepairTime = new Dictionary<long, float>();
        private const float MIN_REPAIR_INTERVAL = 0.8f;

        // Fast cached delegate for WearNTear.SetHealthVisual(float, bool)
        private static readonly Action<WearNTear, float, bool>? s_setHealthVisualDelegate;

        static NetworkManager()
        {
            try
            {
                var method = AccessTools.Method(typeof(WearNTear), "SetHealthVisual", new[] { typeof(float), typeof(bool) });
                if (method != null)
                {
                    s_setHealthVisualDelegate = AccessTools.MethodDelegate<Action<WearNTear, float, bool>>(method);
                }
            }
            catch (Exception ex)
            {
                MassFixStructurePlugin.Log?.LogWarning($"Could not bind SetHealthVisual delegate: {ex.Message}");
            }
        }

        public static void RegisterRPCs(ZRoutedRpc routedRpc)
        {
            if (routedRpc == null) return;

            routedRpc.Register<ZPackage>(RPC_SERVER_HELLO, RPC_HandleServerHello);
            routedRpc.Register<ZPackage>(RPC_CLIENT_HELLO, RPC_HandleClientHello);
            routedRpc.Register<ZPackage>(RPC_BATCH_REPAIR, RPC_HandleBatchRepair);
            routedRpc.Register<ZPackage>(RPC_SYNC_REPAIRS, RPC_HandleSyncRepairs);

            MassFixStructurePlugin.Log?.LogInfo("Registered MassFixStructure routed RPCs.");
        }

        public static void OnPeerConnected(ZNetPeer peer)
        {
            if (peer == null || ZRoutedRpc.instance == null || ZNet.instance == null) return;

            if (ZNet.instance.IsServer())
            {
                // Server sends configuration & limits to the connecting client
                var helloPkg = new ZPackage();
                helloPkg.Write(ModConfig.MaxRepairsPerSwing.Value);
                helloPkg.Write(ModConfig.SearchRadius.Value);
                helloPkg.Write(ModConfig.RequireWorkbench.Value);
                helloPkg.Write(MIN_REPAIR_INTERVAL);

                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, RPC_SERVER_HELLO, helloPkg);
                MassFixStructurePlugin.Log?.LogInfo($"Sent ServerHello to peer {peer.m_uid}");
            }
            else
            {
                // Client introduces itself to server to trigger handshake
                long serverId = GetServerPeerID();
                if (serverId != 0L)
                {
                    var clientPkg = new ZPackage();
                    clientPkg.Write(MassFixStructurePlugin.PluginVersion);
                    ZRoutedRpc.instance.InvokeRoutedRPC(serverId, RPC_CLIENT_HELLO, clientPkg);
                }
            }
        }

        public static long GetServerPeerID()
        {
            if (ZNet.instance == null) return 0L;
            if (ZNet.instance.IsServer())
            {
                return ZNet.GetUID();
            }

            var serverPeer = ZNet.instance.GetServerPeer();
            return serverPeer != null ? serverPeer.m_uid : 0L;
        }

        public static void SendBatchRepair(
            Player player,
            ItemDrop.ItemData toolItem,
            Piece originPiece,
            List<WearNTear> piecesToRepair)
        {
            if (piecesToRepair == null || piecesToRepair.Count == 0 || ZRoutedRpc.instance == null)
            {
                return;
            }

            long serverId = GetServerPeerID();
            if (serverId == 0L)
            {
                return;
            }

            // Trigger player swing animation and look direction locally
            player.FaceLookDirection();
            if (toolItem.m_shared.m_attack != null)
            {
                player.GetZAnim()?.SetTrigger(toolItem.m_shared.m_attack.m_attackAnimation);
            }

            // Play single audio & VFX at origin piece
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

            // Handle costs (stamina & hammer durability)
            CostMode costMode = ModConfig.Cost.Value;
            if (costMode == CostMode.VanillaPerSwing || costMode == CostMode.PerPieceScaled)
            {
                float totalStamina = MassRepairRunner.GetPlayerBuildStamina(player);
                float totalDurability = toolItem.m_shared.m_useDurability
                    ? toolItem.m_shared.m_useDurabilityDrain * Game.m_durabilityRate
                    : 0f;

                if (costMode == CostMode.PerPieceScaled && piecesToRepair.Count > 1)
                {
                    int extraCount = piecesToRepair.Count - 1;
                    totalStamina += totalStamina * ModConfig.PerPieceStaminaRatio.Value * extraCount;
                    totalDurability += totalDurability * ModConfig.PerPieceDurabilityRatio.Value * extraCount;
                }

                player.UseStamina(totalStamina);
                if (toolItem.m_shared.m_attack != null)
                {
                    player.UseEitr(toolItem.m_shared.m_attack.m_attackEitr);
                }

                if (toolItem.m_shared.m_useDurability)
                {
                    toolItem.m_durability -= totalDurability;
                }
            }

            // Pack all ZDOIDs and their target maxHealth into ONE single ZPackage
            var pkg = new ZPackage();
            pkg.Write(piecesToRepair.Count);

            for (int i = 0; i < piecesToRepair.Count; i++)
            {
                WearNTear wnt = piecesToRepair[i];
                ZNetView nview = wnt.GetComponent<ZNetView>();
                pkg.Write(nview.GetZDO().m_uid);
                pkg.Write(wnt.m_health);
            }

            // Send 1 single batched RPC to server
            ZRoutedRpc.instance.InvokeRoutedRPC(serverId, RPC_BATCH_REPAIR, pkg);

            // Client HUD message
            if (ModConfig.ShowHudNotification.Value)
            {
                player.Message(MessageHud.MessageType.TopLeft, $"MassFix: Repaired {piecesToRepair.Count} pieces (Server Synced)");
            }
        }

        // --- RPC Handlers ---

        private static void RPC_HandleServerHello(long sender, ZPackage pkg)
        {
            if (pkg == null) return;

            try
            {
                ServerMaxRepairs = pkg.ReadInt();
                ServerSearchRadius = pkg.ReadSingle();
                ServerRequireWorkbench = pkg.ReadBool();
                pkg.ReadSingle(); // Read cooldown

                ServerHasMod = true;
                MassFixStructurePlugin.Log?.LogInfo($"[MassFixStructure] Handshake complete with server. Server limits: MaxRepairs={ServerMaxRepairs}, Radius={ServerSearchRadius}m, RequireWorkbench={ServerRequireWorkbench}. Custom Batch RPC mode enabled!");
            }
            catch (Exception ex)
            {
                MassFixStructurePlugin.Log?.LogError($"Failed to read ServerHello: {ex}");
            }
        }

        private static void RPC_HandleClientHello(long sender, ZPackage pkg)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || ZRoutedRpc.instance == null) return;

            // Reply with ServerHello
            var helloPkg = new ZPackage();
            helloPkg.Write(ModConfig.MaxRepairsPerSwing.Value);
            helloPkg.Write(ModConfig.SearchRadius.Value);
            helloPkg.Write(ModConfig.RequireWorkbench.Value);
            helloPkg.Write(MIN_REPAIR_INTERVAL);

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, RPC_SERVER_HELLO, helloPkg);
        }

        private static void RPC_HandleBatchRepair(long sender, ZPackage pkg)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || pkg == null || ZDOMan.instance == null) return;

            // 1. Anti-spam / macro cooldown check per player
            float now = Time.time;
            if (s_peerLastRepairTime.TryGetValue(sender, out float lastTime))
            {
                if (now - lastTime < MIN_REPAIR_INTERVAL)
                {
                    MassFixStructurePlugin.Log?.LogWarning($"[MassFix] Peer {sender} repair throttled (interval: {now - lastTime:F2}s < {MIN_REPAIR_INTERVAL}s).");
                    return;
                }
            }
            s_peerLastRepairTime[sender] = now;

            // 2. Read count and clamp to server limits
            int count = pkg.ReadInt();
            int serverMax = ModConfig.MaxRepairsPerSwing.Value;
            if (count <= 0 || count > 1000)
            {
                return;
            }

            var repairedList = new List<(ZDOID id, float hp)>(Math.Min(count, serverMax));

            for (int i = 0; i < count; i++)
            {
                ZDOID zdoid = pkg.ReadZDOID();
                float maxHealth = pkg.ReadSingle();

                if (repairedList.Count >= serverMax)
                {
                    continue; // Skip excess beyond server limit
                }

                ZDO zdo = ZDOMan.instance.GetZDO(zdoid);
                if (zdo == null)
                {
                    continue;
                }

                // Server-side ward permission check
                if (!PrivateArea.CheckAccess(zdo.GetPosition(), 0f, flash: false))
                {
                    continue;
                }

                float currentHealth = zdo.GetFloat(ZDOVars.s_health, maxHealth);
                if (currentHealth < maxHealth - 0.01f)
                {
                    // Directly update ZDO in server memory (automatically synchronized to peers by Valheim)
                    zdo.Set(ZDOVars.s_health, maxHealth);
                    repairedList.Add((zdoid, maxHealth));
                }
            }

            // 3. Broadcast single consolidated sync packet to all clients so local visual models update immediately
            if (repairedList.Count > 0 && ZRoutedRpc.instance != null)
            {
                var syncPkg = new ZPackage();
                syncPkg.Write(repairedList.Count);

                for (int i = 0; i < repairedList.Count; i++)
                {
                    syncPkg.Write(repairedList[i].id);
                    syncPkg.Write(repairedList[i].hp);
                }

                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_SYNC_REPAIRS, syncPkg);
                MassFixStructurePlugin.Log?.LogInfo($"[MassFix Server] Processed batch repair from peer {sender}: {repairedList.Count} pieces restored in 1 atomic packet.");
            }
        }

        private static void RPC_HandleSyncRepairs(long sender, ZPackage pkg)
        {
            // Dedicated server does not need to update visual GameObject meshes
            if (ZNet.instance != null && ZNet.instance.IsDedicated()) return;
            if (pkg == null || ZNetScene.instance == null) return;

            try
            {
                int count = pkg.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    ZDOID zdoid = pkg.ReadZDOID();
                    float health = pkg.ReadSingle();

                    GameObject? go = ZNetScene.instance.FindInstance(zdoid);
                    if (go != null)
                    {
                        WearNTear? wnt = go.GetComponent<WearNTear>();
                        if (wnt != null && s_setHealthVisualDelegate != null)
                        {
                            s_setHealthVisualDelegate(wnt, health, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MassFixStructurePlugin.Log?.LogWarning($"Error in RPC_HandleSyncRepairs: {ex.Message}");
            }
        }
    }
}
