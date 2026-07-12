using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Multiplayer;

public class CustomGameUINetwork : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerEntryPrefab;

    private readonly List<GameObject> _playerEntries = new();
    private bool _connected;

    private void Start()
    {
        backButton.onClick.AddListener(OnBackClicked);
        startButton.onClick.AddListener(OnStartClicked);

        startButton.gameObject.SetActive(false);

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnDisconnected += HandleDisconnected;
            NetworkGameManager.Instance.OnCustomGameStarted += HandleCustomGameStarted;
            NetworkGameManager.Instance.OnBecameLobbyHost += HandleBecameLobbyHost;
            NetworkGameManager.Instance.OnPlayerJoinedLobby += HandlePlayerJoined;
            NetworkGameManager.Instance.OnPlayerLeftLobby += HandlePlayerLeft;
        }

        string lobbyCode = LobbyManager.Instance != null ? LobbyManager.Instance.CurrentLobbyCode : "";
        if (roomCodeText != null)
            roomCodeText.text = $"Room Code: {lobbyCode}";

        SetStatus("Connecting...");

        int port = LobbyManager.Instance != null ? LobbyManager.Instance.CurrentLobbyPort : 0;
        string address = LobbyManager.Instance != null ? LobbyManager.Instance.ServerAddress : "10.93.27.48";

        if (port > 0)
        {
            NetworkGameManager.Instance.JoinGameAtAddress(address, (ushort)port);
        }
        else
        {
            SetStatus("No lobby port available");
        }
    }

    private void OnDestroy()
    {
        backButton?.onClick.RemoveListener(OnBackClicked);
        startButton?.onClick.RemoveListener(OnStartClicked);

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnDisconnected -= HandleDisconnected;
            NetworkGameManager.Instance.OnCustomGameStarted -= HandleCustomGameStarted;
            NetworkGameManager.Instance.OnBecameLobbyHost -= HandleBecameLobbyHost;
            NetworkGameManager.Instance.OnPlayerJoinedLobby -= HandlePlayerJoined;
            NetworkGameManager.Instance.OnPlayerLeftLobby -= HandlePlayerLeft;
        }
    }

    private void OnBackClicked()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkGameManager.Instance.Disconnect();
        }
        else
        {
            SceneManager.LoadScene("Main Menu");
        }
    }

    private void OnStartClicked()
    {
        if (!NetworkGameManager.Instance.IsLobbyHost) return;
        NetworkGameManager.Instance.RequestStartGame();
        SetStatus("Starting game...");
    }

    private void HandleBecameLobbyHost()
    {
        startButton.gameObject.SetActive(true);
        SetStatus("You are the host! Click Start when ready.");
        AddLocalPlayerEntry("Player 1");
    }

    private void HandleDisconnected(DisconnectReason reason)
    {
        _connected = false;
        startButton.gameObject.SetActive(false);
        SetStatus($"Disconnected: {reason}");
        ClearPlayerList();
    }

    private void HandleCustomGameStarted()
    {
        Debug.Log("Game started!");
    }

    private void HandlePlayerJoined(NetworkPlayer player)
    {
        AddPlayerEntry(player.PlayerName);
    }

    private void HandlePlayerLeft(NetworkPlayer player)
    {
        RemovePlayerEntry(player.PlayerName);
    }

    private void AddLocalPlayerEntry(string name)
    {
        if (playerListContainer == null || playerEntryPrefab == null) return;

        foreach (var entry in _playerEntries)
        {
            var tmp = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null && tmp.text == name) return;
        }

        GameObject obj = Instantiate(playerEntryPrefab, playerListContainer);
        var label = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = name;
        _playerEntries.Add(obj);
    }

    private void AddPlayerEntry(string name)
    {
        if (playerListContainer == null || playerEntryPrefab == null) return;

        foreach (var entry in _playerEntries)
        {
            var tmp = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null && tmp.text == name) return;
        }

        GameObject obj = Instantiate(playerEntryPrefab, playerListContainer);
        var label = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = name;
        _playerEntries.Add(obj);
    }

    private void RemovePlayerEntry(string name)
    {
        for (int i = _playerEntries.Count - 1; i >= 0; i--)
        {
            if (_playerEntries[i] == null)
            {
                _playerEntries.RemoveAt(i);
                continue;
            }
            var tmp = _playerEntries[i].GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null && tmp.text == name)
            {
                Destroy(_playerEntries[i]);
                _playerEntries.RemoveAt(i);
                return;
            }
        }
    }

    private void ClearPlayerList()
    {
        foreach (var entry in _playerEntries)
        {
            if (entry != null) Destroy(entry);
        }
        _playerEntries.Clear();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log($"[CustomGame] {message}");
    }
}
