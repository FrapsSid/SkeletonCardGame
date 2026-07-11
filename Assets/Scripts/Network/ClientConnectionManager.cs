using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Multiplayer
{
    public sealed class ClientConnectionManager : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private string defaultServerAddress = "127.0.0.1";
        [SerializeField] private ushort defaultPort = 7777;
        [SerializeField] private float connectionTimeout = 10f;
        [SerializeField] private int maxReconnectAttempts = 3;
        [SerializeField] private float reconnectDelay = 2f;

        [Header("References")]
        [SerializeField] private NetworkManager networkManager;

        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Reconnecting,
            Failed
        }

        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public string CurrentServerAddress { get; private set; }
        public ushort CurrentPort { get; private set; }
        public int ReconnectAttempts { get; private set; }

        public event Action<ConnectionState> OnStateChanged;
        public event Action OnConnected;
        public event Action<string> OnConnectionFailed;
        public event Action OnDisconnected;

        private Coroutine _connectionCoroutine;
        private bool _intentionalDisconnect;

        private void Awake()
        {
            if (networkManager == null)
            {
                networkManager = NetworkManager.Singleton;
            }
        }

        private void OnEnable()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback += HandleConnected;
                networkManager.OnClientDisconnectCallback += HandleDisconnected;
            }
        }

        private void OnDisable()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= HandleConnected;
                networkManager.OnClientDisconnectCallback -= HandleDisconnected;
            }
        }

        public void Connect(string address, ushort port)
        {
            if (State == ConnectionState.Connecting || State == ConnectionState.Connected)
            {
                Debug.LogWarning("[ClientConnection] Already connecting or connected");
                return;
            }

            CurrentServerAddress = address;
            CurrentPort = port;
            _intentionalDisconnect = false;
            ReconnectAttempts = 0;

            StopConnectionCoroutine();
            _connectionCoroutine = StartCoroutine(ConnectRoutine());
        }

        public void ConnectToDefault()
        {
            Connect(defaultServerAddress, defaultPort);
        }

        public void Disconnect()
        {
            _intentionalDisconnect = true;
            StopConnectionCoroutine();

            if (networkManager != null && networkManager.IsClient)
            {
                networkManager.Shutdown();
            }

            SetState(ConnectionState.Disconnected);
        }

        private IEnumerator ConnectRoutine()
        {
            SetState(ConnectionState.Connecting);

            var transport = networkManager.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(CurrentServerAddress, CurrentPort);
            }

            Debug.Log($"[ClientConnection] Connecting to {CurrentServerAddress}:{CurrentPort}");

            bool started = networkManager.StartClient();
            if (!started)
            {
                OnConnectionFailed?.Invoke("Failed to start client");
                SetState(ConnectionState.Failed);
                yield break;
            }

            float elapsed = 0f;
            while (!networkManager.IsConnectedClient && elapsed < connectionTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!networkManager.IsConnectedClient)
            {
                networkManager.Shutdown();
                OnConnectionFailed?.Invoke("Connection timeout");
                SetState(ConnectionState.Failed);
            }

            _connectionCoroutine = null;
        }

        private IEnumerator ReconnectRoutine()
        {
            while (ReconnectAttempts < maxReconnectAttempts)
            {
                ReconnectAttempts++;
                SetState(ConnectionState.Reconnecting);

                Debug.Log($"[ClientConnection] Reconnect attempt {ReconnectAttempts}/{maxReconnectAttempts}");

                yield return new WaitForSeconds(reconnectDelay);

                var transport = networkManager.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.SetConnectionData(CurrentServerAddress, CurrentPort);
                }

                networkManager.StartClient();

                float elapsed = 0f;
                while (!networkManager.IsConnectedClient && elapsed < connectionTimeout)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (networkManager.IsConnectedClient)
                {
                    _connectionCoroutine = null;
                    yield break;
                }

                networkManager.Shutdown();
            }

            OnConnectionFailed?.Invoke("Max reconnection attempts exceeded");
            SetState(ConnectionState.Failed);
            _connectionCoroutine = null;
        }

        private void HandleConnected(ulong clientId)
        {
            if (clientId != networkManager.LocalClientId) return;

            Debug.Log("[ClientConnection] Connected to server");
            ReconnectAttempts = 0;
            SetState(ConnectionState.Connected);
            OnConnected?.Invoke();
        }

        private void HandleDisconnected(ulong clientId)
        {
            if (clientId != networkManager.LocalClientId) return;

            Debug.Log("[ClientConnection] Disconnected from server");

            if (_intentionalDisconnect)
            {
                SetState(ConnectionState.Disconnected);
                OnDisconnected?.Invoke();
                return;
            }

            if (ReconnectAttempts < maxReconnectAttempts)
            {
                StopConnectionCoroutine();
                _connectionCoroutine = StartCoroutine(ReconnectRoutine());
            }
            else
            {
                SetState(ConnectionState.Failed);
                OnDisconnected?.Invoke();
            }
        }

        private void SetState(ConnectionState newState)
        {
            if (State == newState) return;
            State = newState;
            OnStateChanged?.Invoke(newState);
        }

        private void StopConnectionCoroutine()
        {
            if (_connectionCoroutine != null)
            {
                StopCoroutine(_connectionCoroutine);
                _connectionCoroutine = null;
            }
        }

        public string GetConnectionStatus()
        {
            return State switch
            {
                ConnectionState.Disconnected => "Disconnected",
                ConnectionState.Connecting => $"Connecting to {CurrentServerAddress}:{CurrentPort}...",
                ConnectionState.Connected => $"Connected to {CurrentServerAddress}:{CurrentPort}",
                ConnectionState.Reconnecting => $"Reconnecting... ({ReconnectAttempts}/{maxReconnectAttempts})",
                ConnectionState.Failed => "Connection failed",
                _ => "Unknown"
            };
        }
    }
}
