using System.Collections;
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
    void Start()
    {
        backButton.onClick.AddListener(OnBackClicked);
        connectButton.onClick.AddListener(OnConnectClicked);

        addressInputField.text = "127.0.0.1";

        NetworkGameManager.Instance.OnDisconnected += HandleDisconnected;
    }

    private void OnDestroy()
    {
        backButton?.onClick.RemoveListener(OnBackClicked);
        connectButton?.onClick.RemoveListener(OnConnectClicked);

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnDisconnected -= HandleDisconnected;
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

        string address = string.IsNullOrWhiteSpace(addressInputField.text)
            ? "127.0.0.1"
            : addressInputField.text;

        Debug.Log($"Connecting to {address}...");
        NetworkGameManager.Instance.JoinGame(address);
        Debug.Log($"[UI] After JoinGame: IsClient={NetworkManager.Singleton.IsClient}");
    }

    private void HandleDisconnected(DisconnectReason reason)
    {
        Debug.LogWarning($"Connection failed: {reason}");
    }
}
