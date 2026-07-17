using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace Multiplayer
{
    public class CustomGameUINetwork : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button backButton;
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private TMP_Text roomCodeText;

        private RelayTransport _relayTransport;

        private void Start()
        {
            Debug.Log("Joined Custom Game scene");

            backButton.onClick.AddListener(OnBackClicked);
            startButton.onClick.AddListener(OnStartClicked);

            NetworkGameManager.Instance.OnDisconnected += HandleDisconnected;
            NetworkGameManager.Instance.OnCustomGameStarted += HandleCustomGameStarted;

            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            startButton.gameObject.SetActive(isHost);

            _relayTransport = NetworkManager.Singleton?.GetComponent<RelayTransport>();
            UpdateRoomCodeDisplay();
        }

        private void OnDestroy()
        {
            backButton?.onClick.RemoveListener(OnBackClicked);
            startButton?.onClick.RemoveListener(OnStartClicked);

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnDisconnected -= HandleDisconnected;
                NetworkGameManager.Instance.OnCustomGameStarted -= HandleCustomGameStarted;
            }
        }

        private void UpdateRoomCodeDisplay()
        {
            if (roomCodeText == null) return;

            if (_relayTransport != null && !string.IsNullOrEmpty(_relayTransport.RoomCode))
            {
                roomCodeText.text = $"Room Code: {_relayTransport.RoomCode}";
                roomCodeText.gameObject.SetActive(true);
            }
            else
            {
                roomCodeText.gameObject.SetActive(false);
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
}
