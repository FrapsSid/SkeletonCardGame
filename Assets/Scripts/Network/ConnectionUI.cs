using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace Multiplayer
{
    /// <summary>
    /// Простейший UI подключения. Повесить на Canvas, перетащить ссылки в инспекторе.
    /// Использует TextMeshPro — если в проекте его нет, замените TMP_InputField/TMP_Text
    /// на обычные InputField/Text из UnityEngine.UI.
    /// </summary>
    public class ConnectionUI : MonoBehaviour
    {
        [Header("Host / Join / Disconnect")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_InputField addressInputField;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject background;
        [Header("Список игроков")]
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private TMP_Text playerListEntryPrefab; // префаб одной строки списка

        private void Start()
        {
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnPlayerConnected += HandlePlayerListChanged;
                NetworkGameManager.Instance.OnPlayerDisconnected += HandlePlayerListChanged;
                NetworkGameManager.Instance.OnDisconnected += HandleDisconnected;
            }
        }
        private void OnEnable()
        {
            background.SetActive(true);
            hostButton.onClick.AddListener(OnHostClicked);
            joinButton.onClick.AddListener(OnJoinClicked);
            disconnectButton.onClick.AddListener(OnDisconnectClicked);
        }

        private void OnDestroy()
        {
            hostButton.onClick.RemoveListener(OnHostClicked);
            joinButton.onClick.RemoveListener(OnJoinClicked);
            disconnectButton.onClick.RemoveListener(OnDisconnectClicked);

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnPlayerConnected -= HandlePlayerListChanged;
                NetworkGameManager.Instance.OnPlayerDisconnected -= HandlePlayerListChanged;
                NetworkGameManager.Instance.OnDisconnected -= HandleDisconnected;
            }
        }

        private void OnHostClicked()
        {
            if (_disconnectCoroutine != null)
            {
                StopCoroutine(_disconnectCoroutine);
            }
            NetworkGameManager.Instance.HostGame();
            background.SetActive(false);
            statusText.gameObject.SetActive(false);
        }

        private void OnJoinClicked()
        {
            if (_disconnectCoroutine != null)
            {
                StopCoroutine(_disconnectCoroutine);
            }
            string address = string.IsNullOrWhiteSpace(addressInputField.text) ? "127.0.0.1" : addressInputField.text;
            NetworkGameManager.Instance.JoinGame(address);
            background.SetActive(false);
            statusText.gameObject.SetActive(false);
        }

        private void OnDisconnectClicked()
        {
            NetworkGameManager.Instance.Disconnect();
        }

        private void HandlePlayerListChanged(NetworkPlayer _) => RefreshPlayerList();

        private Coroutine _disconnectCoroutine;
        private void HandleDisconnected(DisconnectReason reason)
        {
            if (_disconnectCoroutine != null)
            {
                StopCoroutine(_disconnectCoroutine);
            }
            _disconnectCoroutine = StartCoroutine(ShowDisconnectMessage(reason));
        }

        private IEnumerator ShowDisconnectMessage(DisconnectReason reason)
        {
            background.SetActive(true);
            RefreshPlayerList();
            statusText.gameObject.SetActive(true);
            statusText.text = $"Session ended: {reason}";
            
            yield return new WaitForSeconds(5f);
            
            statusText.gameObject.SetActive(false);
        }
        private void RefreshPlayerList()
        {
            foreach (Transform child in playerListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var player in NetworkGameManager.Instance.ConnectedPlayers)
            {
                var entry = Instantiate(playerListEntryPrefab, playerListContainer);
                entry.text = $"- {player.PlayerName} (id {player.ClientId})";
            }
        }
    }
}
