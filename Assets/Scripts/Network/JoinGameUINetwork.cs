#nullable enable

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Multiplayer;

public class JoinGameUINetwork : MonoBehaviour
{
    private const string ConnectionErrorMessage = "Connection failed";

    [SerializeField] private Button backButton = null!;
    [SerializeField] private Button connectButton = null!;
    [SerializeField] private TMP_InputField addressInputField = null!;
    [SerializeField] private TMP_InputField roomCodeInputField = null!;
    [SerializeField] private TMP_Text connectionErrorText = null!;

    void Start()
    {
        backButton.onClick.AddListener(OnBackClicked);
        connectButton.onClick.AddListener(OnConnectClicked);
        roomCodeInputField.onValidateInput += ConvertRoomCodeCharacterToUppercase;

        addressInputField.text = "127.0.0.1";
        roomCodeInputField.text = "";

        connectionErrorText.text = string.Empty;
        NetworkGameManager.Instance.OnDisconnected += HandleDisconnected;
        NetworkManager.Singleton.OnTransportFailure += HandleTransportFailure;
    }

    private void OnDestroy()
    {
        backButton?.onClick.RemoveListener(OnBackClicked);
        connectButton?.onClick.RemoveListener(OnConnectClicked);
        roomCodeInputField.onValidateInput -= ConvertRoomCodeCharacterToUppercase;

        if (NetworkGameManager.Instance != null)
            NetworkGameManager.Instance.OnDisconnected -= HandleDisconnected;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnTransportFailure -= HandleTransportFailure;
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene("Main Menu");
    }

    private void OnConnectClicked()
    {
        connectionErrorText.text = string.Empty;

        if (NetworkManager.Singleton != null
            && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsListening))
        {
            Debug.LogWarning("Already connected or listening.");
            return;
        }

        try
        {
            // Relay join: room code takes priority
            if (!string.IsNullOrWhiteSpace(roomCodeInputField.text))
            {
                string code = roomCodeInputField.text.Trim().ToUpperInvariant();
                Debug.Log($"[Relay] Joining room {code}...");
                if (!NetworkGameManager.Instance.JoinRelayGame(code))
                    ShowConnectionError();
                return;
            }

            // Direct join: IP address
            string address = string.IsNullOrWhiteSpace(addressInputField.text)
                ? "127.0.0.1"
                : addressInputField.text;

            Debug.Log($"Connecting directly to {address}...");
            if (!NetworkGameManager.Instance.JoinGame(address))
                ShowConnectionError();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            ShowConnectionError();
        }
    }

    private void HandleDisconnected(DisconnectReason reason)
    {
        Debug.LogWarning($"Connection failed: {reason}");
        ShowConnectionError();
    }

    private void HandleTransportFailure() => ShowConnectionError();

    private static char ConvertRoomCodeCharacterToUppercase(
        string _,
        int __,
        char addedCharacter) => char.ToUpperInvariant(addedCharacter);

    private void ShowConnectionError() => connectionErrorText.text = ConnectionErrorMessage;
}
