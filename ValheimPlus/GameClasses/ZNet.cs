using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimPlus.Configurations;
using ValheimPlus.RPC;

// ToDo add packet system to convey map markers
namespace ValheimPlus
{
    [HarmonyPatch(typeof(ZNet))]
    public class HookZNet
    {
        /// <summary>
        /// Hook base GetOtherPublicPlayer method
        /// </summary>
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(ZNet), "GetOtherPublicPlayers", new Type[] { typeof(List<ZNet.PlayerInfo>) })]
        public static void GetOtherPublicPlayers(object instance, List<ZNet.PlayerInfo> playerList) => throw new NotImplementedException();
    }

    /// <summary>
    /// Alter server player limit
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "Awake")]
    public static class ChangeGameServerVariables
    {
        private static void Postfix(ref ZNet __instance)
        {
            if (Configuration.Current.Server.IsEnabled)
            {
                int maxPlayers = Configuration.Current.Server.maxPlayers;
                if (maxPlayers >= 1)
                {
                    // Set Server Instance Max Players
                    __instance.m_serverPlayerLimit = maxPlayers;
                }
            }
        }
    }

    /// <summary>
    /// Alter server player limit
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "FixedUpdate")]
    public static class VPlusServerTicks
    {
        private static void Postfix(ref ZNet __instance)
        {
            if (__instance != null && __instance.IsServer() && Configuration.Current.Server.IsEnabled)
            {
                VPlusServer.instance.FixedUpdate();
            }
        }
    }

    /// <summary>
    /// Sync server client configuration
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    public static class VPlusServerRoutes
    {
        private static void Postfix(ref ZNet __instance, ZRpc rpc)
        {
            ZNetPeer peer = __instance.GetPeer(rpc);
            if (peer == null)
            {
                return;
            }
            if (!ZNet.m_isServer)
            {
                ZLog.Log("> Client will request V+ Config");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "VPlusConfigHandshake", new object[] { new ZPackage() });
            }
            else
            { 
                ZLog.Log("> Server will request to handshake V+ Version");
                rpc.Register<string>("VPlusVersionCheck", new Action<ZRpc, string>(VPlusRPC.VersionCheckServer));
                ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, "VPlusVersionCheck");
            }
        }
    }

    /// <summary>
    /// Load settngs from server instance
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "Shutdown")]
    public static class OnErrorLoadOwnIni
    {
        private static void Prefix(ref ZNet __instance)
        {
            if (!__instance.IsServer())
            {
                // Load the client config file on server ZNet instance exit (server disconnect)
                if (ConfigurationExtra.LoadSettings() != true)
                {
                    Debug.LogError("Error while loading configuration file.");
                }
            }
        }
    }

    /// <summary>
    /// Force player public reference position on
    /// </summary>
    [HarmonyPatch(typeof(ZNet), "SetPublicReferencePosition")]
    public static class PreventPublicPositionToggle
    {
        private static void Postfix(ref bool pub, ref bool ___m_publicReferencePosition)
        {
            if (Configuration.Current.Map.IsEnabled && Configuration.Current.Map.preventPlayerFromTurningOffPublicPosition)
            {
                ___m_publicReferencePosition = true;
            }
        }
    }
}
