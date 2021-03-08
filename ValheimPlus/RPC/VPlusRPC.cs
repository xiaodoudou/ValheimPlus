﻿using System.Collections.Generic;
using System.IO;
using BepInEx;
using ValheimPlus.Configurations;

namespace ValheimPlus.RPC
{
    public class VPlusRPC
    {
        public static void ConfigSync(long sender, ZPackage configPkg)
        {
            if (ZNet.m_isServer) //Server
            {
                if (!Configuration.Current.Server.serverSyncsConfig) return;

                ZPackage pkg = new ZPackage();

                string[] rawConfigData = File.ReadAllLines(ConfigurationExtra.ConfigIniPath);
                List<string> cleanConfigData = new List<string>();

                for (int i = 0; i < rawConfigData.Length; i++)
                {
                    if (rawConfigData[i].Trim().StartsWith(";") ||
                        rawConfigData[i].Trim().StartsWith("#")) continue; //Skip comments

                    if (rawConfigData[i].Trim().IsNullOrWhiteSpace()) continue; //Skip blank lines

                    //Add to clean data
                    cleanConfigData.Add(rawConfigData[i]);
                }

                //Add number of clean lines to package
                pkg.Write(cleanConfigData.Count);

                //Add each line to the package
                foreach (string line in cleanConfigData)
                {
                    pkg.Write(line);

                    ZLog.Log("SENTCONFIG: " + line);
                }

                ZRoutedRpc.instance.InvokeRoutedRPC(sender, "VPlusConfigSync", new object[]
                {
                    pkg
                });

                ZLog.Log("VPlus configuration synced to peer #" + sender);
            }
            else //Client
            {
                if (configPkg != null &&
                    configPkg.Size() > 0 &&
                    sender == ZRoutedRpc.instance.GetServerPeerID()) //Validate the message is from the server and not another client.
                {
                    int numLines = configPkg.ReadInt();

                    if (numLines == 0)
                    {
                        ZLog.LogWarning("Got zero line config file from server. Cannot load.");
                        return;
                    }

                    using (MemoryStream memStream = new MemoryStream())
                    {
                        using (StreamWriter tmpWriter = new StreamWriter(memStream))
                        {
                            for (int i = 0; i < numLines; i++)
                            {
                                string line = configPkg.ReadString();

                                tmpWriter.WriteLine(line);

                                ZLog.Log("CONFIGDATA: " + line);
                            }

                            tmpWriter.Flush(); //Flush to memStream
                            memStream.Position = 0; //Rewind stream

                            Configuration.Current = ConfigurationExtra.LoadFromIni(memStream);

                            // Needed to make sure client is using server configuration as dayLength is setup before
                            // TimeManipulation.SetupDayLength(); DEACTIVATED

                            ZLog.Log("Successfully synced VPlus configuration from server.");
                        }
                    }
                }
            }
        }
        public static void VersionCheckClient(long sender)
        {
            if (sender == ZRoutedRpc.instance.GetServerPeerID())
            {
                ZRpc serverRPC = ZNet.instance.GetServerRPC();
                if (serverRPC != null)
                {
                    serverRPC.Invoke("VPlusVersionCheck", new object[] { ValheimPlusPlugin.version });
                    ZLog.Log($"VPlus version {ValheimPlusPlugin.version} sent to peer #{sender}");
                }
            }
        }

        
        public static void VersionCheckServer(ZRpc sender, string version)
        {
            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            if (peer == null)
            {
                return;
            }
            if (version != null && version.Length > 0 && peer.m_uid != ZRoutedRpc.instance.GetServerPeerID())
            {
                ZLog.Log($"Received VPlus version {version} from peer #{peer.m_uid}");
                VPlusServer.instance.UpdatePlayerVersion(peer.m_uid, version);
            }
        }
    }
}
