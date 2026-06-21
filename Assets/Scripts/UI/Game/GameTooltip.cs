using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameTooltip : MonoBehaviour
{
    public static GameTooltip Instance { get; private set; }

    private RectTransform rect;
    private TMP_Text titleText;
    private TMP_Text bodyText;

    public static GameTooltip Create(Transform parent)
    {
        RectTransform tooltipRect = GameUIFactory.Panel(parent, "Tooltip", new Color(0.02f, 0.02f, 0.02f, 0.94f));
        GameTooltip tooltip = tooltipRect.gameObject.AddComponent<GameTooltip>();
        tooltip.Initialize();
        return tooltip;
    }

    private void Initialize()
    {
        Instance = this;
        rect = GetComponent<RectTransform>();
        GameUIFactory.Anchor(rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(360f, 132f));

        VerticalLayoutGroup layout = GameUIFactory.VerticalLayout(gameObject, 8f);
        layout.padding = new RectOffset(18, 18, 14, 14);

        titleText = GameUIFactory.Text(transform, "Title", string.Empty, 22f, TextAlignmentOptions.Left, GameUITheme.Cyan);
        bodyText = GameUIFactory.Text(transform, "Body", string.Empty, 18f, TextAlignmentOptions.TopLeft, GameUITheme.White, FontStyles.Normal);
        bodyText.textWrappingMode = TextWrappingModes.Normal;

        gameObject.SetActive(false);
    }

    public void Show(string title, string body, Vector2 screenPosition)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
            return;

        titleText.text = title ?? string.Empty;
        titleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(title));
        bodyText.text = body ?? string.Empty;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Position(screenPosition);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Position(Vector2 screenPosition)
    {
        Vector2 size = rect.sizeDelta;
        float x = Mathf.Clamp(screenPosition.x + 24f, 8f, Screen.width - size.x - 8f);
        float y = Mathf.Clamp(screenPosition.y - 18f, size.y + 8f, Screen.height - 8f);
        rect.position = new Vector3(x, y, 0f);
    }
}
