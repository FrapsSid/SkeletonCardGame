using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Multiplayer;

public class MainMenuUINetwork : MonoBehaviour
{
    [SerializeField] private Button customGameButton;
    [SerializeField] private Button joinButton;
    void Start()
    {
        customGameButton.onClick.AddListener(OnCustomGameClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    void OnCustomGameClicked()
    {
        NetworkGameManager.Instance.HostGame();
        Debug.Log($"[UI] After HostGame: IsServer={NetworkManager.Singleton.IsServer}, ActiveScene={SceneManager.GetActiveScene().name}");
        SceneManager.LoadScene("Custom Game");
    }

    void OnJoinClicked()
    {
        SceneManager.LoadScene("Join");
    }
}
