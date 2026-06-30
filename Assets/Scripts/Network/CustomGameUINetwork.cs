using System.Collections;
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

    private void Start()
    {
        Debug.Log("Joined Custom Game scene");

        backButton.onClick.AddListener(OnBackClicked);
        startButton.onClick.AddListener(OnStartClicked);

        NetworkGameManager.Instance.OnDisconnected += HandleDisconnected;
        NetworkGameManager.Instance.OnCustomGameStarted += HandleCustomGameStarted;

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        startButton.gameObject.SetActive(isHost);
    }

    private void OnDestroy()
    {
        backButton?.onClick.RemoveListener(OnBackClicked);
        startButton?.onClick.RemoveListener(OnStartClicked);

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.OnDisconnected -= HandleDisconnected;
            NetworkGameManager.Instance.OnGameStarted -= HandleGameStarted;
            NetworkGameManager.Instance.OnCustomGameStarted -= HandleCustomGameStarted;
        }
    }

    private void OnBackClicked()
    {
        bool isConnected = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isConnected)
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
        if (!NetworkManager.Singleton.IsHost) return;
        NetworkGameManager.Instance.CustomGameStarted();
    }

    private void HandleDisconnected(DisconnectReason reason)
    {
        Debug.Log($"Session ended: {reason}. Returning to Main Menu.");
        StartCoroutine(ReturnToMainMenu());
    }

    private void HandleGameStarted()
    {
        Debug.Log("Host Started the game");
    }

    private System.Collections.IEnumerator ReturnToMainMenu()
    {
        yield return null;
        SceneManager.LoadScene("Main Menu");
    }

    private void HandleCustomGameStarted()
    {
        Debug.Log("Host Started the custom game");
    }
}