using UnityEngine;
using UnityEngine.SceneManagement;
using Multiplayer;

public class MainMenuUINetwork : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button customGameButton;
    [SerializeField] private UnityEngine.UI.Button joinButton;
    [SerializeField] private TMPro.TextMeshProUGUI statusText;

    private void Start()
    {
        Debug.Log($"[MainMenu] Start: customGameButton={customGameButton}, joinButton={joinButton}, lobbyManager={LobbyManager.Instance}");
        customGameButton.onClick.AddListener(OnCustomGameClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private void OnDestroy()
    {
        customGameButton?.onClick.RemoveListener(OnCustomGameClicked);
        joinButton?.onClick.RemoveListener(OnJoinClicked);

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyCreated -= HandleLobbyCreated;
            LobbyManager.Instance.OnError -= HandleError;
        }
    }

    private void OnCustomGameClicked()
    {
        Debug.Log("[MainMenu] Custom Game clicked");

        if (LobbyManager.Instance == null)
        {
            Debug.LogWarning("[MainMenu] LobbyManager.Instance is NULL");
            SetStatus("Lobby manager not available");
            return;
        }

        Debug.Log($"[MainMenu] LobbyManager found, calling CreateLobby (server={LobbyManager.Instance.ServerAddress})");
        customGameButton.interactable = false;
        SetStatus("Creating lobby...");

        LobbyManager.Instance.OnLobbyCreated -= HandleLobbyCreated;
        LobbyManager.Instance.OnLobbyCreated += HandleLobbyCreated;
        LobbyManager.Instance.OnError -= HandleError;
        LobbyManager.Instance.OnError += HandleError;

        LobbyManager.Instance.CreateLobby();
    }

    private void HandleLobbyCreated(string code, int port)
    {
        LobbyManager.Instance.OnLobbyCreated -= HandleLobbyCreated;
        LobbyManager.Instance.OnError -= HandleError;

        Debug.Log($"[MainMenu] Lobby created: {code} on port {port}");
        SceneManager.LoadScene("Custom Game");
    }

    private void HandleError(string error)
    {
        LobbyManager.Instance.OnLobbyCreated -= HandleLobbyCreated;
        LobbyManager.Instance.OnError -= HandleError;

        customGameButton.interactable = true;
        SetStatus($"Failed: {error}");
    }

    private void OnJoinClicked()
    {
        SceneManager.LoadScene("Join");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
