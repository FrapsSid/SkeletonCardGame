using System.Collections;
using Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class JoinGameScreen : GameUIScreen
{
    public override ScreenId Id => ScreenId.JoinGame;

    private TMP_InputField codeInput;
    private TMP_Text statusText;
    private bool waitingForConnection;
    private Coroutine timeoutCoroutine;

    protected override void Build()
    {
        GameUIFactory.Backdrop(transform, darkness: 0.64f);

        RectTransform panel = GameUIFactory.Panel(transform, "JoinPanel", GameUITheme.Panel);
        GameUIFactory.Anchor(panel, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        TMP_Text title = GameUIFactory.Text(panel, "Title", "ПРИСОЕДИНИТЬСЯ", 60f, TextAlignmentOptions.Center, GameUITheme.White);
        GameUIFactory.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(0f, 90f));

        codeInput = CreateInput(panel);
        GameUIFactory.Anchor(codeInput.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 64f), new Vector2(560f, 92f));

        Button join = GameUIFactory.Button(panel, "JoinButton", "ПРИСОЕДИНИТЬСЯ", Join, true);
        GameUIFactory.Anchor(join.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(420f, 62f));

        statusText = GameUIFactory.Text(panel, "Status", string.Empty, 24f, TextAlignmentOptions.Center, GameUITheme.Blue, FontStyles.Bold);
        GameUIFactory.Anchor(statusText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -96f), new Vector2(0f, 42f));

        Button back = GameUIFactory.Button(panel, "BackButton", "НАЗАД", () => UI.OpenScreen(ScreenId.MainMenu), true);
        GameUIFactory.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(260f, 60f));
    }

    protected override void OnShow()
    {
        waitingForConnection = false;
        statusText.text = string.Empty;
        SubscribeNetwork();
    }

    protected override void OnHide()
    {
        UnsubscribeNetwork();
        if (timeoutCoroutine != null)
            StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = null;
    }

    private TMP_InputField CreateInput(Transform parent)
    {
        RectTransform root = GameUIFactory.CreateRect("LobbyCodeInput", parent);
        Image image = GameUIFactory.Image(root, new Color(0.28f, 0.28f, 0.28f, 0.9f));
        GameUIFactory.UseRoundedSprite(image);

        TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        input.characterLimit = 64;

        TMP_Text text = GameUIFactory.Text(root, "Text", string.Empty, 36f, TextAlignmentOptions.Center, GameUITheme.White, FontStyles.Bold);
        GameUIFactory.Stretch(text.rectTransform, 22f, 0f, 22f, 0f);

        TMP_Text placeholder = GameUIFactory.Text(root, "Placeholder", "ВВЕДИТЕ КОД ЛОББИ...", 34f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.82f), FontStyles.Bold);
        GameUIFactory.Stretch(placeholder.rectTransform, 22f, 0f, 22f, 0f);

        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
    }

    private void Join()
    {
        string code = codeInput != null ? codeInput.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            statusText.color = GameUITheme.Red;
            statusText.text = "Ошибка: введите код лобби";
            return;
        }

        if (NetworkGameManager.Instance == null)
        {
            statusText.color = GameUITheme.Red;
            statusText.text = "Ошибка: NetworkGameManager не найден";
            return;
        }

        waitingForConnection = true;
        statusText.color = GameUITheme.Blue;
        statusText.text = "Подключение...";
        NetworkGameManager.Instance.JoinGame(code);

        if (timeoutCoroutine != null)
            StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(ConnectionTimeout());
    }

    private IEnumerator ConnectionTimeout()
    {
        yield return new WaitForSeconds(6f);
        if (!waitingForConnection)
            yield break;

        statusText.color = GameUITheme.Red;
        statusText.text = "Ошибка: комната не найдена";
        waitingForConnection = false;
    }

    private void SubscribeNetwork()
    {
        if (NetworkGameManager.Instance == null)
            return;

        NetworkGameManager.Instance.OnPlayerConnected += HandlePlayerConnected;
        NetworkGameManager.Instance.OnDisconnected += HandleDisconnected;
    }

    private void UnsubscribeNetwork()
    {
        if (NetworkGameManager.Instance == null)
            return;

        NetworkGameManager.Instance.OnPlayerConnected -= HandlePlayerConnected;
        NetworkGameManager.Instance.OnDisconnected -= HandleDisconnected;
    }

    private void HandlePlayerConnected(NetworkPlayer player)
    {
        if (!waitingForConnection)
            return;

        waitingForConnection = false;
        UI.JoinLobbyAsGuest(false);
    }

    private void HandleDisconnected(DisconnectReason reason)
    {
        if (!waitingForConnection)
            return;

        waitingForConnection = false;
        statusText.color = GameUITheme.Red;
        statusText.text = reason == DisconnectReason.SessionFull
            ? "Ошибка: комната заполнена"
            : "Ошибка: неверный код лобби";
    }
}
