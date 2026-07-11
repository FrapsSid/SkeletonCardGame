using System;
using System.Collections;
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
        [SerializeField] private GameManager gameManager;
        [SerializeField] private NetworkGameState networkGameStatePrefab;

        [Header("Dedicated Server")]
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private int minPlayersToStart = 2;
        [SerializeField] private float lobbyTimeout = 120f;

        private readonly List<ulong> _connectedClients = new();
        private bool _gameStarted;
        public IReadOnlyList<ulong> ConnectedClients => _connectedClients;
        private bool _isDisconnecting;

        // Dedicated server support
        private bool _isDedicatedServer;
        public bool IsDedicatedServer => _isDedicatedServer;
        private readonly HashSet<ulong> _disconnectedClients = new();

        // Lobby state (dedicated server)
        private readonly List<NetworkPlayer> _lobbyPlayers = new();
        private float _lobbyTimer;

        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnected;
        public event Action<DisconnectReason> OnDisconnected;
        public event Action OnCustomGameStarted;
        public event Action<NetworkPlayer> OnPlayerJoinedLobby;
        public event Action<NetworkPlayer> OnPlayerLeftLobby;

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

            Debug.Log("[NetworkGameManager] Start: Subscribing to callbacks");
            NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;

            CheckDedicatedServerMode();
        }

        private void OnDisable()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApproveConnection;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
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
                _lobbyPlayers.Clear();
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
                if (_isDedicatedServer &&
                    _disconnectedClients.Remove(request.ClientNetworkId))
                {
                    response.Approved = true;
                    response.CreatePlayerObject = false;
                    response.Reason = string.Empty;
                    response.Pending = false;
                    return;
                }

                response.Approved = false;
                response.CreatePlayerObject = false;
                response.Reason = "Game already started";
                response.Pending = false;
                return;
            }

            if (_connectedClients.Count >= maxPlayers)
            {
                response.Approved = false;
                response.CreatePlayerObject = false;
                response.Reason = "Server full";
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
            if (_gameStarted && _isDedicatedServer)
            {
                _disconnectedClients.Add(clientId);
                Debug.Log($"[NGM] Client {clientId} disconnected mid-game — will allow reconnection");
            }

            var lobbyPlayer = _lobbyPlayers.Find(p => p.ClientId == clientId);
            if (lobbyPlayer != null)
            {
                _lobbyPlayers.Remove(lobbyPlayer);
                OnPlayerLeftLobby?.Invoke(lobbyPlayer);
            }

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

            if (_isDedicatedServer && !_gameStarted)
            {
                StartCoroutine(WaitForPlayerObject(clientId));
            }
        }

        private IEnumerator WaitForPlayerObject(ulong clientId)
        {
            NetworkPlayer player = null;
            float timeout = 5f;
            float elapsed = 0f;

            while (player == null && elapsed < timeout)
            {
                foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                {
                    if (p.ClientId == clientId)
                    {
                        player = p;
                        break;
                    }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (player != null)
            {
                _lobbyPlayers.Add(player);
                OnPlayerJoinedLobby?.Invoke(player);
                Debug.Log($"[NGM] Player joined lobby: {player.PlayerName}");
                CheckLobbyReady();
            }
        }

        private void CheckLobbyReady()
        {
            if (_gameStarted || _isDedicatedServer == false) return;
            if (_lobbyPlayers.Count < minPlayersToStart) return;

            Debug.Log($"[NGM] Lobby ready: {_lobbyPlayers.Count}/{minPlayersToStart} players");
            StartGame();
        }

        public void StartGame()
        {
            if (_gameStarted) return;
            _gameStarted = true;
            CustomGameStarted();
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

            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerGameTest", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        private void OnCustomGameStartedReceived(ulong senderId, FastBufferReader reader)
        {
            OnCustomGameStarted?.Invoke();
        }

        private void SpawnPlayersOnSceneLoad(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (sceneName != "MultiplayerGameTest") return;

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

        public void CheckDedicatedServerMode()
        {
            _isDedicatedServer = Application.isBatchMode;

            string[] args = System.Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg.ToLower() == "-server" || arg.ToLower() == "--server")
                {
                    _isDedicatedServer = true;
                    break;
                }
            }

            if (_isDedicatedServer)
            {
                Debug.Log("[NGM] Running in dedicated server mode");
                Application.targetFrameRate = 30;
                QualitySettings.vSyncCount = 0;
                StartDedicatedServer();
            }
        }

        public void StartDedicatedServer()
        {
            ushort port = defaultPort;
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].ToLower() == "-port" &&
                    ushort.TryParse(args[i + 1], out ushort p))
                    port = p;
            }

            var transport = NetworkManager.Singleton
                .GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Port = port;
                transport.ConnectionData.ServerListenAddress = "0.0.0.0";
            }

            Debug.Log($"[NGM] Dedicated server starting on port {port}");
            NetworkManager.Singleton.StartServer();
        }
    }
}