using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Multiplayer;

public class MainMenuUINetwork : MonoBehaviour
{
    [SerializeField] private Button customGameButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button relayHostButton;

    void Start()
    {
        customGameButton.onClick.AddListener(OnCustomGameClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        if (relayHostButton != null)
            relayHostButton.onClick.AddListener(OnRelayHostClicked);
    }

    void OnCustomGameClicked()
    {
        NetworkGameManager.Instance.HostGame();
        Debug.Log($"[UI] After HostGame: IsServer={NetworkManager.Singleton.IsServer}");
        SceneManager.LoadScene("Custom Game");
    }

    void OnJoinClicked()
    {
        SceneManager.LoadScene("Join");
    }

    void OnRelayHostClicked()
    {
        NetworkGameManager.Instance.HostRelayGame();
        Debug.Log($"[UI] After HostRelayGame: IsServer={NetworkManager.Singleton.IsServer}");
        SceneManager.LoadScene("Custom Game");
    }
}
