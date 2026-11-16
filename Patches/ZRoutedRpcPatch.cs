using System;
using HarmonyLib;
using MassFixStructure.Network;

namespace MassFixStructure.Patches
{
    [HarmonyPatch(typeof(ZRoutedRpc))]
    public static class ZRoutedRpcPatch
    {
        [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(bool) })]
        [HarmonyPostfix]
        public static void Constructor_Postfix(ZRoutedRpc __instance)
        {
            try
            {
                NetworkManager.RegisterRPCs(__instance);
            }
            catch (Exception ex)
            {
                MassFixStructurePlugin.Log?.LogError($"Failed to register RPCs in ZRoutedRpc constructor: {ex}");
            }
        }

        [HarmonyPatch("AddPeer")]
        [HarmonyPostfix]
        public static void AddPeer_Postfix(ZNetPeer peer)
        {
            try
            {
                NetworkManager.OnPeerConnected(peer);
            }
            catch (Exception ex)
            {
                MassFixStructurePlugin.Log?.LogError($"Failed to handle OnPeerConnected: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(Game), "Start")]
    public static class GameStartPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            // Reset network state on joining or starting world
            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
                NetworkManager.ServerHasMod = true;
            }
            else
            {
                NetworkManager.ServerHasMod = false;
            }
        }
    }
}
