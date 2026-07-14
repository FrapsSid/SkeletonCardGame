using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AuthUINetwork : MonoBehaviour
{
    private TMP_FontAsset fontAsset;

    private Canvas _canvas;
    private GameObject _loginPanel;
    private GameObject _registerPanel;
    private TMP_InputField _loginUsername;
    private TMP_InputField _loginPassword;
    private TMP_InputField _regUsername;
    private TMP_InputField _regPassword;
    private TMP_InputField _regNickname;
    private TextMeshProUGUI _statusText;
    private Button _loginBtn;
    private Button _registerBtn;
    private Button _switchToRegBtn;
    private Button _switchToLoginBtn;
    private Button _backBtn;
    private Button _discordBtn;

    private readonly Color _bgColor = new Color(0.122f, 0.110f, 0.110f, 1f);
    private readonly Color _accentColor = new Color(0f, 1f, 0.651f, 1f);
    private readonly Color _inputBg = new Color(0.2f, 0.2f, 0.2f, 1f);
    private readonly Color _white = Color.white;
    private readonly Color _dimWhite = new Color(0.8f, 0.8f, 0.8f, 1f);

    private void Start()
    {
        if (AuthManager.Instance != null && AuthManager.Instance.IsAuthenticated)
        {
            SceneManager.LoadScene("Main Menu");
            return;
        }

        if (AuthManager.Instance != null)
            AuthManager.Instance.OnAuthSuccess += HandleAuthSuccess;

        BuildUI();
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.OnAuthSuccess -= HandleAuthSuccess;
    }

    private void HandleAuthSuccess()
    {
        StartCoroutine(DelayedSceneLoad("Main Menu", 0.5f));
    }

    private void BuildUI()
    {
        fontAsset = Resources.Load<TMP_FontAsset>("Fonts/PlayfairDisplaySC-Bold SDF");

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        _canvas = gameObject.GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        CreateBackground();
        CreateTitle();
        CreateStatusText();
        CreateLoginPanel();
        CreateRegisterPanel();
        CreateBackButton();

        ShowLogin();
    }

    private void CreateBackground()
    {
        var bg = new GameObject("Background");
        bg.transform.SetParent(_canvas.transform, false);
        var img = bg.AddComponent<Image>();
        img.color = _bgColor;
        var rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void CreateTitle()
    {
        var title = CreateText("Title", "BET TO THE BONE", _canvas.transform, 36, _accentColor, TextAlignmentOptions.Center);
        var rt = title.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.78f);
        rt.anchorMax = new Vector2(0.9f, 0.88f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var subtitle = CreateText("Subtitle", "AUTHORIZATION", _canvas.transform, 18, _dimWhite, TextAlignmentOptions.Center);
        var rt2 = subtitle.GetComponent<RectTransform>();
        rt2.anchorMin = new Vector2(0.1f, 0.72f);
        rt2.anchorMax = new Vector2(0.9f, 0.78f);
        rt2.offsetMin = Vector2.zero;
        rt2.offsetMax = Vector2.zero;
    }

    private void CreateStatusText()
    {
        _statusText = CreateText("StatusText", "", _canvas.transform, 14, Color.yellow, TextAlignmentOptions.Center);
        var rt = _statusText.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.18f);
        rt.anchorMax = new Vector2(0.9f, 0.24f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void CreateLoginPanel()
    {
        _loginPanel = CreatePanel("LoginPanel");

        var usernameLabel = CreateText("UserLabel", "Username", _loginPanel.transform, 16, _dimWhite, TextAlignmentOptions.Left);
        StretchToArea(usernameLabel.gameObject, 0.15f, 0.70f, 0.85f, 0.77f);
        _loginUsername = CreateInputField("LoginUsername", _loginPanel.transform, "Enter username...");
        StretchToArea(_loginUsername.gameObject, 0.15f, 0.62f, 0.85f, 0.70f);

        var passLabel = CreateText("PassLabel", "Password", _loginPanel.transform, 16, _dimWhite, TextAlignmentOptions.Left);
        StretchToArea(passLabel.gameObject, 0.15f, 0.54f, 0.85f, 0.61f);
        _loginPassword = CreateInputField("LoginPassword", _loginPanel.transform, "Enter password...", true);
        StretchToArea(_loginPassword.gameObject, 0.15f, 0.46f, 0.85f, 0.54f);

        _loginBtn = CreateButton("LoginBtn", "LOG IN", _loginPanel.transform, _accentColor, Color.black);
        StretchToArea(_loginBtn.gameObject, 0.25f, 0.36f, 0.75f, 0.44f);
        _loginBtn.onClick.AddListener(OnLoginClicked);

        var divider = CreateText("Divider", "- OR -", _loginPanel.transform, 14, new Color(0.5f, 0.5f, 0.5f), TextAlignmentOptions.Center);
        StretchToArea(divider.gameObject, 0.2f, 0.29f, 0.8f, 0.35f);

        _discordBtn = CreateButton("DiscordBtn", "LOGIN WITH DISCORD", _loginPanel.transform, new Color(0.35f, 0.37f, 0.98f), Color.white);
        StretchToArea(_discordBtn.gameObject, 0.2f, 0.20f, 0.8f, 0.28f);
        _discordBtn.onClick.AddListener(OnDiscordClicked);

        _switchToRegBtn = CreateTextButton("SwitchToReg", "Don't have an account? Register", _loginPanel.transform, _dimWhite);
        StretchToArea(_switchToRegBtn.gameObject, 0.1f, 0.10f, 0.9f, 0.18f);
        _switchToRegBtn.onClick.AddListener(() => ShowRegister());
    }

    private void CreateRegisterPanel()
    {
        _registerPanel = CreatePanel("RegisterPanel");

        var regTitle = CreateText("RegTitle", "Create Account", _registerPanel.transform, 20, _white, TextAlignmentOptions.Center);
        StretchToArea(regTitle.gameObject, 0.1f, 0.82f, 0.9f, 0.90f);

        var ruLabel = CreateText("RegUserLabel", "Username", _registerPanel.transform, 16, _dimWhite, TextAlignmentOptions.Left);
        StretchToArea(ruLabel.gameObject, 0.15f, 0.72f, 0.85f, 0.79f);
        _regUsername = CreateInputField("RegUsername", _registerPanel.transform, "Choose a username...");
        StretchToArea(_regUsername.gameObject, 0.15f, 0.63f, 0.85f, 0.72f);

        var rnLabel = CreateText("RegNickLabel", "Nickname", _registerPanel.transform, 16, _dimWhite, TextAlignmentOptions.Left);
        StretchToArea(rnLabel.gameObject, 0.15f, 0.54f, 0.85f, 0.61f);
        _regNickname = CreateInputField("RegNickname", _registerPanel.transform, "Your display name...");
        StretchToArea(_regNickname.gameObject, 0.15f, 0.45f, 0.85f, 0.54f);

        var rpLabel = CreateText("RegPassLabel", "Password", _registerPanel.transform, 16, _dimWhite, TextAlignmentOptions.Left);
        StretchToArea(rpLabel.gameObject, 0.15f, 0.36f, 0.85f, 0.43f);
        _regPassword = CreateInputField("RegPassword", _registerPanel.transform, "Choose a password...", true);
        StretchToArea(_regPassword.gameObject, 0.15f, 0.27f, 0.85f, 0.36f);

        _registerBtn = CreateButton("RegisterBtn", "REGISTER", _registerPanel.transform, _accentColor, Color.black);
        StretchToArea(_registerBtn.gameObject, 0.25f, 0.15f, 0.75f, 0.24f);
        _registerBtn.onClick.AddListener(OnRegisterClicked);

        _switchToLoginBtn = CreateTextButton("SwitchToLogin", "Already have an account? Log in", _registerPanel.transform, _dimWhite);
        StretchToArea(_switchToLoginBtn.gameObject, 0.1f, 0.05f, 0.9f, 0.13f);
        _switchToLoginBtn.onClick.AddListener(() => ShowLogin());
    }

    private void CreateBackButton()
    {
        _backBtn = CreateTextButton("BackBtn", "< Back to Title", _canvas.transform, _dimWhite);
        var rt = _backBtn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.02f, 0.92f);
        rt.anchorMax = new Vector2(0.25f, 0.98f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var tmp = _backBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.alignment = TextAlignmentOptions.Left;
        _backBtn.onClick.AddListener(() => SceneManager.LoadScene("Main Menu"));
    }

    private void ShowLogin()
    {
        _loginPanel.SetActive(true);
        _registerPanel.SetActive(false);
        _statusText.text = "";
    }

    private void ShowRegister()
    {
        _loginPanel.SetActive(false);
        _registerPanel.SetActive(true);
        _statusText.text = "";
    }

    private void OnLoginClicked()
    {
        string user = _loginUsername.text.Trim();
        string pass = _loginPassword.text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            _statusText.text = "Fill in all fields";
            _statusText.color = Color.red;
            return;
        }

        SetLoading(true);
        AuthManager.Instance.Login(user, pass, (ok, msg) =>
        {
            SetLoading(false);
            _statusText.text = msg;
            _statusText.color = ok ? _accentColor : Color.red;
            if (ok)
                StartCoroutine(DelayedSceneLoad("Main Menu", 1f));
        });
    }

    private void OnRegisterClicked()
    {
        string user = _regUsername.text.Trim();
        string pass = _regPassword.text;
        string nick = _regNickname.text.Trim();

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(nick))
        {
            _statusText.text = "Fill in all fields";
            _statusText.color = Color.red;
            return;
        }

        if (pass.Length < 6)
        {
            _statusText.text = "Password must be at least 6 characters";
            _statusText.color = Color.red;
            return;
        }

        SetLoading(true);
        AuthManager.Instance.Register(user, pass, nick, (ok, msg) =>
        {
            SetLoading(false);
            _statusText.text = msg;
            _statusText.color = ok ? _accentColor : Color.red;
            if (ok)
                ShowLogin();
        });
    }

    private void OnDiscordClicked()
    {
        AuthManager.Instance.LoginWithDiscord();
        _statusText.text = "Browser opened. Authorize in Discord...";
        _statusText.color = _accentColor;
    }

    private void SetLoading(bool loading)
    {
        _loginBtn.interactable = !loading;
        _registerBtn.interactable = !loading;
        if (loading)
            _statusText.text = "Loading...";
    }

    private IEnumerator DelayedSceneLoad(string scene, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(scene);
    }

    // ── UI Helpers ───────────────────────────────────────────────

    private GameObject CreatePanel(string name)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(_canvas.transform, false);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.25f, 0.15f);
        rt.anchorMax = new Vector2(0.75f, 0.72f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    private TextMeshProUGUI CreateText(string name, string text, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.enableAutoSizing = false;
        return tmp;
    }

    private TMP_InputField CreateInputField(string name, Transform parent, string placeholder, bool isPassword = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var bg = go.AddComponent<Image>();
        bg.color = _inputBg;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = CreateViewport(go.transform);
        input.textComponent = CreateInputText(go.transform);
        input.placeholder = CreatePlaceholder(go.transform, placeholder);
        input.contentType = isPassword ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;

        var colors = input.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.selectedColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        input.colors = colors;

        return input;
    }

    private RectTransform CreateViewport(Transform parent)
    {
        var go = new GameObject("Viewport");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(10, 2);
        rt.offsetMax = new Vector2(-10, -2);
        return rt;
    }

    private TextMeshProUGUI CreateInputText(Transform parent)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 14;
        tmp.color = _white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        if (fontAsset != null) tmp.font = fontAsset;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return tmp;
    }

    private TextMeshProUGUI CreatePlaceholder(Transform parent, string text)
    {
        var go = new GameObject("Placeholder");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Italic;
        tmp.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        if (fontAsset != null) tmp.font = fontAsset;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return tmp;
    }

    private Button CreateButton(string name, string label, Transform parent, Color bgColor, Color textColor)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);
        var img = btnGo.AddComponent<Image>();
        img.color = bgColor;
        var btn = btnGo.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(btnGo.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) tmp.font = fontAsset;
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return btn;
    }

    private Button CreateTextButton(string name, string label, Transform parent, Color color)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);

        var btn = btnGo.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.1f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.05f);
        btn.colors = colors;

        var img = btnGo.AddComponent<Image>();
        img.color = Color.clear;

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(btnGo.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) tmp.font = fontAsset;
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return btn;
    }

    private void StretchToArea(GameObject go, float xMin, float yMin, float xMax, float yMax)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
