using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Unity.Collections;

namespace Multiplayer
{
    public enum DisconnectReason
    {
        Unknown,
        HostShutdown,
        ClientDisconnected,
    }

    [RequireComponent(typeof(NetworkManager))]
    public class NetworkGameManager : MonoBehaviour
    {
        public static NetworkGameManager Instance { get; private set; }
        [SerializeField] private ushort defaultPort = 7777;
        [SerializeField] private GameObject playerPrefab;

        private readonly List<ulong> _connectedClients = new();
        private bool _gameStarted;
        public IReadOnlyList<ulong> ConnectedClients => _connectedClients;
        private bool _isDisconnecting;

        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnected;
        public event Action<DisconnectReason> OnDisconnected;
        public event Action OnCustomGameStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[NetworkGameManager] Start: NetworkManager.Singleton is null");
                return;
            }
            Debug.Log($"[NGM] EnableSceneManagement = {NetworkManager.Singleton.NetworkConfig.EnableSceneManagement}");

            Debug.Log("[NetworkGameManager] Start: Subscribing to callbacks");
            NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApproveConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        }

        // ── Direct connect (existing) ──────────────────────────────────

        public void HostGame()
        {
            EnsureTransport<UnityTransport>();
            NetworkManager.Singleton.StartHost();
        }

        public void JoinGame(string address)
        {
            var transport = EnsureTransport<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = address;
                transport.ConnectionData.Port = defaultPort;
            }
            NetworkManager.Singleton.StartClient();
        }

        // ── Relay connect (new) ────────────────────────────────────────

        public void HostRelayGame()
        {
            var relay = EnsureTransport<RelayTransport>();
            relay.OnRoomCodeReceived += code =>
                Debug.Log($"[NGM] Relay room code: {code}");
            NetworkManager.Singleton.StartHost();
        }

        public void JoinRelayGame(string roomCode)
        {
            var relay = EnsureTransport<RelayTransport>();
            relay.SetRoomCode(roomCode);
            NetworkManager.Singleton.StartClient();
        }

        // ── Transport helper ───────────────────────────────────────────

        private T EnsureTransport<T>() where T : NetworkTransport
        {
            var nm = NetworkManager.Singleton;
            var existing = nm.GetComponent<T>();
            if (existing != null)
            {
                nm.NetworkConfig.NetworkTransport = existing;
                return existing;
            }
            var added = nm.gameObject.AddComponent<T>();
            nm.NetworkConfig.NetworkTransport = added;
            return added;
        }

        // ── Disconnect / shutdown ──────────────────────────────────────

        public void Disconnect()
        {
            if (_isDisconnecting) return;
            if (NetworkManager.Singleton == null) return;
            if (NetworkManager.Singleton.ShutdownInProgress) return;

            _isDisconnecting = true;
            bool wasHost = NetworkManager.Singleton.IsHost;
            _gameStarted = false;
            try
            {
                if (NetworkManager.Singleton.IsListening
                    || NetworkManager.Singleton.IsClient
                    || NetworkManager.Singleton.IsServer
                    || NetworkManager.Singleton.IsHost)
                {
                    NetworkManager.Singleton.Shutdown();
                }

                _connectedClients.Clear();
                OnDisconnected?.Invoke(wasHost ? DisconnectReason.HostShutdown : DisconnectReason.ClientDisconnected);
            }
            finally
            {
                _isDisconnecting = false;
            }
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            if (_gameStarted)
            {
                response.Approved = false;
                response.CreatePlayerObject = false;
                response.Reason = "Game already started";
                response.Pending = false;
                return;
            }

            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Reason = string.Empty;
            response.Pending = false;
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(
                    "CustomGameStarted");
            }
            _connectedClients.Remove(clientId);
            OnClientDisconnected?.Invoke(clientId);

            if (!NetworkManager.Singleton.IsServer)
                Disconnect();
        }
        private void HandleClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                    "CustomGameStarted", OnCustomGameStartedReceived);
            }

            if (_connectedClients.Contains(clientId)) return;
            _connectedClients.Add(clientId);
            OnClientConnected?.Invoke(clientId);
        }

        public void CustomGameStarted()
        {
            if (!NetworkManager.Singleton.IsServer) return;

            _gameStarted = true;

            var buffer = new FastBufferWriter(1, Allocator.Temp);
            using (buffer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
                    "CustomGameStarted", buffer);
            }
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SpawnPlayersOnSceneLoad;

            NetworkManager.Singleton.SceneManager.LoadScene("RelayPlay", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        private void OnCustomGameStartedReceived(ulong senderId, FastBufferReader reader)
        {
            OnCustomGameStarted?.Invoke();
        }

        private void SpawnPlayersOnSceneLoad(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (sceneName != "RelayPlay") return;

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= SpawnPlayersOnSceneLoad;

            SpawnAllPlayers();
        }

        private void SpawnAllPlayers()
        {
            foreach (var clientId in _connectedClients)
            {
                var playerObj = Instantiate(playerPrefab);
                var networkObject = playerObj.GetComponent<NetworkObject>();
                networkObject.SpawnWithOwnership(clientId);
            }
        }
    }
}
