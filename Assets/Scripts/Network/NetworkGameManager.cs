using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Multiplayer
{
    public enum DisconnectReason
    {
        Unknown,
        HostShutdown,
        ClientDisconnected,
        SessionFull
    }

    [RequireComponent(typeof(NetworkManager))]
    public class NetworkGameManager : MonoBehaviour
    {
        public static NetworkGameManager Instance { get; private set; }

        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private ushort defaultPort = 7777;

        public int MaxPlayers => maxPlayers;

        private readonly List<NetworkPlayer> _connectedPlayers = new List<NetworkPlayer>();
        public List<NetworkPlayer> ConnectedPlayers => _connectedPlayers;

        public event Action<NetworkPlayer> OnPlayerConnected;
        public event Action<NetworkPlayer> OnPlayerDisconnected;
        public event Action<DisconnectReason> OnDisconnected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApproveConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }

        public void HostGame()
        {
            NetworkManager.Singleton.StartHost();
        }

        public void JoinGame(string address)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = address;
                transport.ConnectionData.Port = defaultPort;
            }
            NetworkManager.Singleton.StartClient();
        }

        public void Disconnect()
        {
            if (NetworkManager.Singleton == null) return;
            if (NetworkManager.Singleton.ShutdownInProgress) return;

            bool wasHost = NetworkManager.Singleton.IsHost;
            NetworkManager.Singleton.Shutdown();
            _connectedPlayers.Clear();
            OnDisconnected?.Invoke(wasHost ? DisconnectReason.HostShutdown : DisconnectReason.ClientDisconnected);
        }

        public void RegisterPlayer(NetworkPlayer player)
        {
            if (_connectedPlayers.Contains(player)) return;
            _connectedPlayers.Add(player);
            OnPlayerConnected?.Invoke(player);
        }

        public void UnregisterPlayer(NetworkPlayer player)
        {
            if (!_connectedPlayers.Remove(player)) return;
            OnPlayerDisconnected?.Invoke(player);
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            bool roomFull = _connectedPlayers.Count >= maxPlayers;

            response.Approved = !roomFull;
            response.CreatePlayerObject = !roomFull;
            response.Reason = roomFull ? "Session is full" : string.Empty;
            response.Pending = false;

            if (roomFull)
            {
                OnDisconnected?.Invoke(DisconnectReason.SessionFull);
            }
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.Singleton.LocalClientId) return;
            Disconnect();
        }
    }
}
