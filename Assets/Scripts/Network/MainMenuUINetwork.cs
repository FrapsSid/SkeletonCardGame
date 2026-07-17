using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro;
using Multiplayer;

public class MainMenuUINetwork : MonoBehaviour
{
    [SerializeField] private Button customGameButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button relayHostButton;

    private Button logoutButton;
    private TextMeshProUGUI welcomeText;
    private TMP_FontAsset _font;
    private GameObject _logoutUIRoot;

    private readonly Color _accentColor = new Color(0f, 1f, 0.651f, 1f);

    void Start()
    {
        _font = Resources.Load<TMP_FontAsset>("Fonts/PlayfairDisplaySC-Bold SDF");

        customGameButton.onClick.AddListener(OnCustomGameClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        if (relayHostButton != null)
            relayHostButton.onClick.AddListener(OnRelayHostClicked);

        CreateLogoutUI();
        UpdateAuthState();
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

    void OnLogoutClicked()
    {
        AuthManager.Instance.Logout();
        SceneManager.LoadScene("Auth");
    }

    private void OnDestroy()
    {
        if (_logoutUIRoot != null)
            Destroy(_logoutUIRoot);
    }

    private void CreateLogoutUI()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        _logoutUIRoot = new GameObject("LogoutUI");
        _logoutUIRoot.transform.SetParent(canvas.transform, false);
        var rootRt = _logoutUIRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // Welcome text - top right
        var welcomeGo = new GameObject("WelcomeText");
        welcomeGo.transform.SetParent(_logoutUIRoot.transform, false);
        welcomeText = welcomeGo.AddComponent<TextMeshProUGUI>();
        welcomeText.fontSize = 16;
        welcomeText.alignment = TextAlignmentOptions.TopRight;
        welcomeText.color = Color.white;
        if (_font != null) welcomeText.font = _font;
        var welcomeRt = welcomeGo.GetComponent<RectTransform>();
        welcomeRt.anchorMin = new Vector2(1f, 1f);
        welcomeRt.anchorMax = new Vector2(1f, 1f);
        welcomeRt.pivot = new Vector2(1f, 1f);
        welcomeRt.anchoredPosition = new Vector2(-50f, -10f);
        welcomeRt.sizeDelta = new Vector2(300f, 30f);

        // Logout button - bottom left, 50px from edges
        var btnGo = new GameObject("LogoutButton");
        btnGo.transform.SetParent(_logoutUIRoot.transform, false);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = Color.clear;

        logoutButton = btnGo.AddComponent<Button>();
        var btnColors = logoutButton.colors;
        btnColors.normalColor = Color.clear;
        btnColors.highlightedColor = new Color(0f, 1f, 0.651f, 0.15f);
        btnColors.pressedColor = new Color(0f, 1f, 0.651f, 0.25f);
        btnColors.selectedColor = new Color(0f, 1f, 0.651f, 0.1f);
        logoutButton.colors = btnColors;
        logoutButton.onClick.AddListener(OnLogoutClicked);

        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0f, 0f);
        btnRt.anchorMax = new Vector2(0f, 0f);
        btnRt.pivot = new Vector2(0f, 0f);
        btnRt.anchoredPosition = new Vector2(50f, 50f);
        btnRt.sizeDelta = new Vector2(120f, 35f);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = "LOGOUT";
        label.fontSize = 16;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        if (_font != null) label.font = _font;
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        // Shadow on hover (green glow)
        var shadow = btnGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 1f, 0.651f, 0f);
        shadow.effectDistance = new Vector2(2f, -2f);

        // Animate shadow on hover via EventTrigger
        var enterEvent = new UnityEngine.EventSystems.EventTrigger.Entry();
        enterEvent.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        enterEvent.callback.AddListener((_) =>
        {
            shadow.effectColor = new Color(0f, 1f, 0.651f, 0.8f);
        });

        var exitEvent = new UnityEngine.EventSystems.EventTrigger.Entry();
        exitEvent.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exitEvent.callback.AddListener((_) =>
        {
            shadow.effectColor = new Color(0f, 1f, 0.651f, 0f);
        });

        var trigger = btnGo.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        trigger.triggers.Add(enterEvent);
        trigger.triggers.Add(exitEvent);
    }

    private void UpdateAuthState()
    {
        if (AuthManager.Instance != null && AuthManager.Instance.IsAuthenticated)
        {
            if (welcomeText != null)
                welcomeText.text = $"Welcome, {AuthManager.Instance.PlayerNickname}!";
            if (logoutButton != null)
                logoutButton.gameObject.SetActive(true);
        }
        else
        {
            if (welcomeText != null)
                welcomeText.text = "";
            if (logoutButton != null)
                logoutButton.gameObject.SetActive(false);
        }
    }
}
