using System.Collections.Generic;
using UnityEngine;
using ValheimPlus.Configurations;

namespace ValheimPlus.RPC
{
    public struct VPlusVersion
    {
        public string current;
        public float delay;
        public bool match;
    }

    public class VPlusPlayer
    {
        public long uuid;
        public ZNetPeer peer;
        public VPlusVersion version;
        public bool flagForRemoval;

        public VPlusPlayer(long uuid)
        {
            this.uuid = uuid;
            if (ZNet.instance != null && ZNet.instance.IsServer())
            {
               this.peer = ZNet.instance.GetPeer(uuid);
            }
        }
        public string GetEndpoint()
        {
            if (this.peer != null)
            {
                return this.peer.m_socket.GetEndPointString();
            }
            return null;
        }
    }

    public class VPlusServer
    {
        public static VPlusServer instance
        {
            get
            {
                if (VPlusServer._singleton == null)
                {
                    VPlusServer._singleton = new VPlusServer();
                }
                return VPlusServer._singleton;
            }
        }

        public VPlusServer()
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
            {
                Debug.LogWarning("VPlusServer should not be instancied by a client.");
                return;
            }
            this._players = new Dictionary<long, VPlusPlayer>();
            Debug.Log("VPlusServer started.");
        }

        public VPlusPlayer AddPlayer(long uuid)
        {
            Debug.Log($"VPlusServer: Adding player {uuid}");
            if (!this._players.ContainsKey(uuid))
            {
                VPlusPlayer player = new VPlusPlayer(uuid);
                if (player.peer != null)
                {
                    this._players.Add(uuid, player);
                    Debug.Log($"VPlusServer: Player {uuid} added.");
                    return player;
                }
            }
            return null;
        }

        public VPlusPlayer GetPlayer(long uuid)
        {
            if (this._players.ContainsKey(uuid))
            {
                return this._players[uuid];
            }
            return null;
        }

        public bool UpdatePlayerVersion(long uuid, string version)
        {
            VPlusPlayer player = this.GetPlayer(uuid);
            if (player == null)
            {
                return false;
            }
            player.version.current = version;
            player.version.match = false;
            player.version.delay = 0f;
            return true;
        }

        public void CheckPlayersVersion()
        {
            foreach (KeyValuePair<long, VPlusPlayer> entry in this.GetPlayers())
            {
                VPlusPlayer player = entry.Value;
                if (player.flagForRemoval) continue;
                if (player.version.match) continue;
                if (player.version.current != ValheimPlusPlugin.version)
                {
                    if (player.version.delay >= _maxVersionDelay) 
                    {
                        Debug.Log($"Player {entry.Key} will be kicked for version mistach (waited {player.version.delay}ms). Mine: ${ValheimPlusPlugin.version} His: {player.version.current}");
                        this.RemovePlayer(player);
                    }
                } 
                else
                {
                    player.version.match = true;
                    Debug.Log($"VPlusServer: Player {entry.Key} version is passed.");
                }
            }
        }

        public void RemovePlayer(VPlusPlayer player)
        {
            this.RemovePlayer(player.uuid);
        }

        public void RemovePlayer(long uuid)
        {
            VPlusPlayer player = this.GetPlayer(uuid);
            if (player != null)
            {
                player.flagForRemoval = true;
            }
        }

        private void _removePlayers()
        {
            List<VPlusPlayer> playersToBeRemoved = new List<VPlusPlayer>();
            foreach (KeyValuePair<long, VPlusPlayer> entry in this.GetPlayers())
            {
               if (entry.Value.flagForRemoval)
                {
                    playersToBeRemoved.Add(entry.Value);
                }
            }
            foreach(VPlusPlayer player in playersToBeRemoved)
            {
                if (ZNet.instance.IsConnected(player.uuid))
                {
                    ZNet.instance.InternalKick(player.peer);

                }
                this._players.Remove(player.uuid);
            }
        }

        public Dictionary<long, VPlusPlayer> GetPlayers()
        {
            return new Dictionary<long, VPlusPlayer>(_players);
        }

        public void FixedUpdate()
        {
            foreach (KeyValuePair<long, VPlusPlayer> entry in this.GetPlayers())
            {
                VPlusPlayer player = entry.Value;
                if (Configuration.Current.Server.enforceMod)
                {
                    player.version.delay += Time.fixedDeltaTime;
                    this.CheckPlayersVersion();
                }
                _removePlayers();
            }
        }

        private float _maxVersionDelay = 1.0f; // 1 seconds 

        private Dictionary<long, VPlusPlayer> _players;

        private static VPlusServer _singleton;
    }
}