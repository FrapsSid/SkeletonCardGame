using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Multiplayer;

public class JoinGameUINetwork : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_InputField addressInputField;
    [SerializeField] private Button connectButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        backButton.onClick.AddListener(OnBackClicked);
        connectButton.onClick.AddListener(OnConnectClicked);

        addressInputField.text = "";
        addressInputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Room code";

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyFound += HandleLobbyFound;
            LobbyManager.Instance.OnLobbyNotFound += HandleLobbyNotFound;
            LobbyManager.Instance.OnError += HandleError;
        }
    }

    private void OnDestroy()
    {
        backButton?.onClick.RemoveListener(OnBackClicked);
        connectButton?.onClick.RemoveListener(OnConnectClicked);

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyFound -= HandleLobbyFound;
            LobbyManager.Instance.OnLobbyNotFound -= HandleLobbyNotFound;
            LobbyManager.Instance.OnError -= HandleError;
        }
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene("Main Menu");
    }

    private void OnConnectClicked()
    {
        if (NetworkManager.Singleton != null
            && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsListening))
        {
            Debug.LogWarning("Already connected or listening.");
            return;
        }

        string code = addressInputField.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            SetStatus("Enter a room code");
            return;
        }

        if (LobbyManager.Instance == null)
        {
            SetStatus("Lobby manager not available");
            return;
        }

        connectButton.interactable = false;
        SetStatus($"Looking up lobby {code}...");
        LobbyManager.Instance.FindLobby(code);
    }

    private void HandleLobbyFound(int port)
    {
        SetStatus($"Connecting to port {port}...");
        NetworkGameManager.Instance.JoinGameAtAddress(LobbyManager.Instance.ServerAddress, (ushort)port);
    }

    private void HandleLobbyNotFound(string code)
    {
        connectButton.interactable = true;
        SetStatus($"Lobby {code} not found");
    }

    private void HandleError(string error)
    {
        connectButton.interactable = true;
        SetStatus($"Error: {error}");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
