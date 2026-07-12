using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Multiplayer;

public class JoinGameUINetwork : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private Button connectButton;
    [SerializeField] private TMP_InputField addressInputField;
    [SerializeField] private TMP_InputField roomCodeInputField;

    void Start()
    {
        backButton.onClick.AddListener(OnBackClicked);
        connectButton.onClick.AddListener(OnConnectClicked);

        if (addressInputField != null)
            addressInputField.text = "127.0.0.1";
        if (roomCodeInputField != null)
            roomCodeInputField.text = "";

        NetworkGameManager.Instance.OnDisconnected += HandleDisconnected;
    }

    private void OnDestroy()
    {
        backButton?.onClick.RemoveListener(OnBackClicked);
        connectButton?.onClick.RemoveListener(OnConnectClicked);

        if (NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.OnDisconnected -= HandleDisconnected;
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

        // Relay join: room code takes priority
        if (roomCodeInputField != null && !string.IsNullOrWhiteSpace(roomCodeInputField.text))
        {
            string code = roomCodeInputField.text.Trim().ToUpper();
            Debug.Log($"[Relay] Joining room {code}...");
            NetworkGameManager.Instance.JoinRelayGame(code);
            return;
        }

        // Direct join: IP address
        string address = string.IsNullOrWhiteSpace(addressInputField != null ? addressInputField.text : "")
            ? "127.0.0.1"
            : addressInputField.text;

        Debug.Log($"Connecting directly to {address}...");
        NetworkGameManager.Instance.JoinGame(address);
    }

    private void HandleDisconnected(DisconnectReason reason)
    {
        Debug.LogWarning($"Connection failed: {reason}");
    }
}
