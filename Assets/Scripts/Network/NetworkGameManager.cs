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

        [Header("Lobby")]
        [SerializeField] private string serverAddress = "10.93.27.48";
        [SerializeField] private int maxLobbyPlayers = 8;
        [SerializeField] private int minPlayersToStart = 2;

        private readonly List<ulong> _connectedClients = new();
        private bool _gameStarted;
        public IReadOnlyList<ulong> ConnectedClients => _connectedClients;
        private bool _isDisconnecting;

        private bool _isDedicatedServer;
        public bool IsDedicatedServer => _isDedicatedServer;
        private readonly HashSet<ulong> _disconnectedClients = new();

        private readonly List<NetworkPlayer> _lobbyPlayers = new();
        private ulong _lobbyHostClientId = ulong.MaxValue;
        private bool _isLobbyHost;

        public bool IsLobbyHost => _isLobbyHost;
        public string ServerAddress => serverAddress;

        public event Action<ulong> OnClientConnected;
        public event Action<ulong> OnClientDisconnected;
        public event Action<DisconnectReason> OnDisconnected;
        public event Action OnCustomGameStarted;
        public event Action OnBecameLobbyHost;
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

        public void JoinGameAtAddress(string address, ushort port)
        {
            Debug.Log($"[NGM] Joining game at {address}:{port}");
            Debug.Log($"[NGM] IsListening={NetworkManager.Singleton.IsListening}, IsClient={NetworkManager.Singleton.IsClient}, IsServer={NetworkManager.Singleton.IsServer}");

            try
            {
                using var testClient = new System.Net.Sockets.UdpClient();
                testClient.Connect(address, port);
                byte[] testData = System.Text.Encoding.ASCII.GetBytes("PING");
                int sent = testClient.Send(testData, testData.Length);
                Debug.Log($"[NGM] Raw UDP test: sent {sent} bytes to {address}:{port}");
                testClient.Close();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NGM] Raw UDP test FAILED: {e.GetType().Name}: {e.Message}");
            }

            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = address;
                transport.ConnectionData.Port = port;
                Debug.Log($"[NGM] Transport set to {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
            }
            else
            {
                Debug.LogError("[NGM] UnityTransport component NOT FOUND when joining!");
            }

            NetworkManager.Singleton.StartClient();
            Debug.Log($"[NGM] StartClient called, IsClient={NetworkManager.Singleton.IsClient}, IsListening={NetworkManager.Singleton.IsListening}");
        }

        public void Disconnect()
        {
            if (_isDisconnecting) return;
            if (NetworkManager.Singleton == null) return;
            if (NetworkManager.Singleton.ShutdownInProgress) return;

            _isDisconnecting = true;
            bool wasHost = NetworkManager.Singleton.IsHost;
            _gameStarted = false;
            _isLobbyHost = false;
            _lobbyHostClientId = ulong.MaxValue;
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

        public void RequestStartGame()
        {
            if (!_isLobbyHost) return;
            if (_gameStarted) return;

            Debug.Log($"[NGM] Lobby host requesting game start");
            var buffer = new FastBufferWriter(1, Allocator.Temp);
            using (buffer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                    "StartGameRequest", NetworkManager.ServerClientId, buffer);
            }
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            if (_gameStarted)
            {
                if (_disconnectedClients.Remove(request.ClientNetworkId))
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

            if (_connectedClients.Count >= maxLobbyPlayers)
            {
                response.Approved = false;
                response.CreatePlayerObject = false;
                response.Reason = "Lobby full";
                response.Pending = false;
                return;
            }

            Debug.Log($"[NGM] Approving client {request.ClientNetworkId}");
            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Reason = string.Empty;
            response.Pending = false;
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            Debug.Log($"[NGM] HandleClientDisconnect clientId={clientId}, _isDedicatedServer={_isDedicatedServer}, IsServer={NetworkManager.Singleton.IsServer}, IsClient={NetworkManager.Singleton.IsClient}, IsListening={NetworkManager.Singleton.IsListening}");

            if (_gameStarted)
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

            _connectedClients.Remove(clientId);
            OnClientDisconnected?.Invoke(clientId);

            if (_isDedicatedServer && clientId == _lobbyHostClientId && !_gameStarted && _connectedClients.Count > 0)
            {
                _lobbyHostClientId = _connectedClients[0];
                Debug.Log($"[NGM] New lobby host: {_lobbyHostClientId}");
                SendYouAreHost(_lobbyHostClientId);
            }

            if (!NetworkManager.Singleton.IsServer)
                Disconnect();
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (_connectedClients.Contains(clientId)) return;
            _connectedClients.Add(clientId);
            OnClientConnected?.Invoke(clientId);

            if (_isDedicatedServer && !_gameStarted)
            {
                if (_lobbyHostClientId == ulong.MaxValue)
                {
                    _lobbyHostClientId = clientId;
                    Debug.Log($"[NGM] First player {clientId} is lobby host");
                    SendYouAreHost(clientId);
                }

                StartCoroutine(WaitForPlayerObject(clientId));
            }
        }

        private void SendYouAreHost(ulong clientId)
        {
            var buffer = new FastBufferWriter(1, Allocator.Temp);
            using (buffer)
            {
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                    "YouAreHost", clientId, buffer);
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
            }
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

        private void OnYouAreHostReceived(ulong senderId, FastBufferReader reader)
        {
            _isLobbyHost = true;
            Debug.Log("[NGM] I am the lobby host!");
            OnBecameLobbyHost?.Invoke();
        }

        private void OnStartGameRequestReceived(ulong senderId, FastBufferReader reader)
        {
            if (!_isDedicatedServer) return;
            if (senderId != _lobbyHostClientId)
            {
                Debug.LogWarning($"[NGM] Non-host {senderId} tried to start game");
                return;
            }

            Debug.Log($"[NGM] Host {senderId} started the game");
            StartGame();
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

        private void RegisterCustomMessageHandlers()
        {
            if (NetworkManager.Singleton?.CustomMessagingManager == null) return;

            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                "YouAreHost", OnYouAreHostReceived);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                "CustomGameStarted", OnCustomGameStartedReceived);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                "StartGameRequest", OnStartGameRequestReceived);
        }

        private void UnregisterCustomMessageHandlers()
        {
            if (NetworkManager.Singleton?.CustomMessagingManager == null) return;

            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("YouAreHost");
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("CustomGameStarted");
            NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("StartGameRequest");
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
            else
            {
                RegisterCustomMessageHandlers();
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

            Debug.Log($"[NGM] StartDedicatedServer: target port = {port}");
            Debug.Log($"[NGM] NetworkManager.Singleton = {(NetworkManager.Singleton != null ? "ok" : "NULL")}");
            Debug.Log($"[NGM] IsListening before start = {NetworkManager.Singleton.IsListening}");

            var transport = NetworkManager.Singleton
                .GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Port = port;
                transport.ConnectionData.ServerListenAddress = "0.0.0.0";
                transport.ConnectionData.Address = "10.93.27.48";
                Debug.Log($"[NGM] Transport configured: addr={transport.ConnectionData.Address}, port={transport.ConnectionData.Port}, listenAddr={transport.ConnectionData.ServerListenAddress}");
            }
            else
            {
                Debug.LogError("[NGM] UnityTransport component NOT FOUND on NetworkManager!");
            }

            Debug.Log($"[NGM] PlayerPrefab = {(NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null ? NetworkManager.Singleton.NetworkConfig.PlayerPrefab.name : "NULL")}");

            RegisterCustomMessageHandlers();

            Debug.Log($"[NGM] Calling StartServer() on port {port}");
            bool result = NetworkManager.Singleton.StartServer();
            Debug.Log($"[NGM] StartServer() returned: {result}");
            Debug.Log($"[NGM] IsListening after start = {NetworkManager.Singleton.IsListening}");
            Debug.Log($"[NGM] IsServer after start = {NetworkManager.Singleton.IsServer}");
        }
    }
}
