using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class GameUIFactory
{
    private static Sprite roundedSprite;
    private static Sprite circleSprite;

    public static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static RectTransform Stretch(RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        return rect;
    }

    public static RectTransform Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    public static Image Image(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    public static TMP_Text Text(Transform parent, string name, string value, float size, TextAlignmentOptions alignment, Color color, FontStyles style = FontStyles.Bold)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    public static Button Button(Transform parent, string name, string label, UnityAction onClick, bool textOnly = false)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = textOnly ? new Color(0f, 0f, 0f, 0f) : GameUITheme.ButtonBase;
        if (!textOnly)
            UseRoundedSprite(image);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null)
            button.onClick.AddListener(onClick);

        GameStyledButton styled = rect.gameObject.AddComponent<GameStyledButton>();
        styled.SetTextButtonMode(textOnly);
        if (!textOnly)
            AddGlow(rect.gameObject, GameUITheme.CyanSoft, GameUITheme.CyanDeepGlow, new Vector2(2f, -2f));

        TMP_Text text = Text(rect, "Label", label, textOnly ? 46f : 30f, TextAlignmentOptions.Center, GameUITheme.White);
        Stretch(text.rectTransform, 8f, 0f, 8f, 0f);
        styled.SetLabel(text);

        return button;
    }

    public static RectTransform Panel(Transform parent, string name, Color color, bool outlined = true)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = Image(rect, color);
        UseRoundedSprite(image);
        image.raycastTarget = true;

        if (outlined)
            AddGlow(rect.gameObject, GameUITheme.CyanSoft, GameUITheme.CyanDeepGlow, new Vector2(2f, -2f));

        return rect;
    }

    public static RectTransform Backdrop(Transform parent, string name = "Backdrop", float darkness = 0.68f)
    {
        RectTransform root = CreateRect(name, parent);
        Stretch(root);
        Image baseImage = Image(root, new Color(0.012f, 0.014f, 0.014f, 1f));
        baseImage.raycastTarget = false;

        RectTransform leftWash = CreateRect("LeftWash", root);
        Stretch(leftWash, 0f, 0f, 0f, 0f);
        Image leftImage = Image(leftWash, new Color(0f, 0.18f, 0.14f, 0.18f));
        leftImage.raycastTarget = false;

        RectTransform dim = CreateRect("Dim", root);
        Stretch(dim);
        Image dimImage = Image(dim, new Color(0f, 0f, 0f, darkness));
        dimImage.raycastTarget = false;

        RectTransform table = CreateRect("TableHint", root);
        Anchor(table, new Vector2(0.52f, 0.2f), new Vector2(1.08f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image tableImage = Image(table, new Color(0.32f, 0.2f, 0.08f, 0.18f));
        tableImage.raycastTarget = false;
        table.localRotation = Quaternion.Euler(0f, 0f, -5f);

        CreateCardHint(root, new Vector2(0.77f, 0.45f), -8f);
        CreateCardHint(root, new Vector2(0.86f, 0.29f), 10f);
        CreateCardHint(root, new Vector2(0.68f, 0.28f), -13f);

        return root;
    }

    public static void AddGlow(GameObject target, Color outlineColor, Color shadowColor, Vector2 outlineDistance)
    {
        Outline outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = outlineDistance;

        Shadow shadow = target.GetComponent<Shadow>() ?? target.AddComponent<Shadow>();
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(0f, 0f);
    }

    public static void UseRoundedSprite(Image image)
    {
        if (image == null)
            return;

        image.sprite = GetRoundedSprite();
        image.type = UnityEngine.UI.Image.Type.Sliced;
    }

    public static void UseCircleSprite(Image image)
    {
        if (image == null)
            return;

        image.sprite = GetCircleSprite();
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.preserveAspect = true;
    }

    public static VerticalLayoutGroup VerticalLayout(GameObject target, float spacing, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = alignment;
        layout.spacing = spacing;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        return layout;
    }

    public static HorizontalLayoutGroup HorizontalLayout(GameObject target, float spacing, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        HorizontalLayoutGroup layout = target.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = alignment;
        layout.spacing = spacing;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        return layout;
    }

    public static LayoutElement Layout(GameObject target, float width = -1f, float height = -1f, float flexibleWidth = 0f, float flexibleHeight = 0f)
    {
        LayoutElement element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
        if (width >= 0f)
            element.preferredWidth = width;
        if (height >= 0f)
            element.preferredHeight = height;
        element.flexibleWidth = flexibleWidth;
        element.flexibleHeight = flexibleHeight;
        return element;
    }

    public static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
    }

    public static void SetButtonText(Button button, string value)
    {
        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = value;
    }

    private static void CreateCardHint(Transform parent, Vector2 anchor, float rotation)
    {
        RectTransform card = CreateRect("CardHint", parent);
        Anchor(card, anchor, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 156f));
        Image image = Image(card, new Color(0.82f, 0.82f, 0.76f, 0.12f));
        image.raycastTarget = false;
        card.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private static Sprite GetRoundedSprite()
    {
        if (roundedSprite != null)
            return roundedSprite;

        const int size = 64;
        const int radius = 10;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeRoundedUiSprite";
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color solid = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool left = x < radius;
                bool right = x >= size - radius;
                bool bottom = y < radius;
                bool top = y >= size - radius;

                bool inside = true;
                if ((left || right) && (bottom || top))
                {
                    float cx = left ? radius : size - radius - 1;
                    float cy = bottom ? radius : size - radius - 1;
                    float dx = x - cx;
                    float dy = y - cy;
                    inside = dx * dx + dy * dy <= radius * radius;
                }

                texture.SetPixel(x, y, inside ? solid : clear);
            }
        }

        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        roundedSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
        roundedSprite.hideFlags = HideFlags.HideAndDontSave;
        return roundedSprite;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 96;
        float radius = (size - 2f) * 0.5f;
        Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RuntimeCircleUiSprite";
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }

        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        circleSprite.hideFlags = HideFlags.HideAndDontSave;
        return circleSprite;
    }
}
