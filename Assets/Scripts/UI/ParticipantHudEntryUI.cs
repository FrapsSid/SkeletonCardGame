using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Participant HUD Entry")]
[DisallowMultipleComponent]
public sealed class ParticipantHudEntryUI : MonoBehaviour
{
    private const float RowWidth = 336f;
    private const float RowHeight = 73f;
    private const float RowPaddingLeft = 11f;
    private const float RowPaddingRight = 14f;
    private const float RowPaddingVertical = 8f;
    private const float RowSpacing = 11f;
    private const float MarkerSize = 17f;
    private const float AvatarFrameSize = 50f;
    private const float AvatarImagePadding = 7f;
    private const float NameMinWidth = 182f;
    private const float NamePreferredHeight = 53f;
    private const float NameFontSizeMin = 14f;
    private const float NameFontSizeMax = 22f;

    private static Sprite circleSprite;
    private static Sprite fallbackAvatarSprite;

    [Header("Fields")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Graphic turnMarker;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nameText;

    [Header("Colors")]
    [SerializeField] private Color normalBackgroundColor = new Color(0.03f, 0.035f, 0.04f, 0.82f);
    [SerializeField] private Color activeBackgroundColor = new Color(0.12f, 0.10f, 0.04f, 0.92f);
    [SerializeField] private Color inactiveMarkerColor = new Color(1f, 1f, 1f, 0.16f);
    [SerializeField] private Color activeMarkerColor = new Color(1f, 0.78f, 0.22f, 1f);

    private void Awake()
    {
        ResolveReferences();
    }

    public static ParticipantHudEntryUI CreateDefault(Transform parent)
    {
        GameObject rowObject = new GameObject("Participant Entry", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowObject.transform.SetParent(parent, false);

        HorizontalLayoutGroup rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;

        ParticipantHudEntryUI entry = rowObject.AddComponent<ParticipantHudEntryUI>();
        entry.BuildGeneratedView();
        entry.ApplyGeneratedSizing();
        return entry;
    }

    public void Bind(PlayerPresentation presentation, bool isActiveTurn)
    {
        ResolveReferences();
        BuildGeneratedView();
        ApplyGeneratedSizing();
        ApplyCustomFont();

        if (nameText != null)
        {
            nameText.text = presentation != null ? presentation.DisplayName : string.Empty;
            nameText.color = presentation != null ? presentation.TeamColor : Color.white;
        }

        if (avatarImage != null)
        {
            avatarImage.sprite = presentation != null && presentation.AvatarSprite != null
                ? presentation.AvatarSprite
                : GetFallbackAvatarSprite();
            avatarImage.preserveAspect = true;
        }

        SetActiveTurn(isActiveTurn);
    }

    public void SetActiveTurn(bool isActiveTurn)
    {
        if (backgroundImage != null)
            backgroundImage.color = isActiveTurn ? activeBackgroundColor : normalBackgroundColor;

        if (turnMarker != null)
            turnMarker.color = isActiveTurn ? activeMarkerColor : inactiveMarkerColor;
    }

    private void BuildGeneratedView()
    {
        if (avatarImage != null && nameText != null && turnMarker != null)
            return;

        backgroundImage ??= GetComponent<Image>();
        if (backgroundImage != null)
            backgroundImage.color = normalBackgroundColor;

        if (turnMarker == null)
        {
            GameObject markerObject = new GameObject("Turn Marker", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            markerObject.transform.SetParent(transform, false);

            Image markerImage = markerObject.GetComponent<Image>();
            markerImage.sprite = GetCircleSprite();
            markerImage.color = inactiveMarkerColor;
            markerImage.preserveAspect = true;
            markerImage.raycastTarget = false;
            turnMarker = markerImage;

        }

        if (avatarImage == null)
        {
            GameObject maskObject = new GameObject("Avatar Mask", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(LayoutElement));
            maskObject.transform.SetParent(transform, false);

            Image maskImage = maskObject.GetComponent<Image>();
            maskImage.sprite = GetCircleSprite();
            maskImage.color = Color.white;

            Mask mask = maskObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;


            GameObject avatarObject = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatarObject.transform.SetParent(maskObject.transform, false);

            avatarImage = avatarObject.GetComponent<Image>();
            avatarImage.sprite = GetFallbackAvatarSprite();
            avatarImage.preserveAspect = true;
            avatarImage.raycastTarget = false;
            avatarImage.color = Color.white;

            RectTransform avatarRect = avatarImage.rectTransform;
            avatarRect.anchorMin = Vector2.zero;
            avatarRect.anchorMax = Vector2.one;
        }

        if (nameText == null)
        {
            GameObject nameObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            nameObject.transform.SetParent(transform, false);

            nameText = nameObject.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = NameFontSizeMin;
            nameText.fontSizeMax = NameFontSizeMax;
            nameText.fontStyle = FontStyles.Bold;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.raycastTarget = false;
            nameText.color = Color.white;
        }
    }

    private void ApplyCustomFont()
    {
        if (nameText == null) return;
        TMP_FontAsset customFont = Resources.Load<TMP_FontAsset>("Fonts/PlayfairDisplaySC-Regular SDF");
        
        if (customFont == null)
        {
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var font in allFonts)
            {
                if (font.name == "PlayfairDisplaySC-Regular SDF")
                {
                    customFont = font;
                    break;
                }
            }
        }

        if (customFont != null)
        {
            nameText.font = customFont;
        }
        else
        {
            Debug.LogError("Font PlayfairDisplaySC-Regular SDF not found");
        }
    }

    private void ApplyGeneratedSizing()
    {
        RectTransform rowRect = transform as RectTransform;
        if (rowRect != null)
            rowRect.sizeDelta = new Vector2(RowWidth, RowHeight);

        HorizontalLayoutGroup rowLayout = GetComponent<HorizontalLayoutGroup>();
        if (rowLayout != null)
        {
            rowLayout.padding = new RectOffset(
                Mathf.RoundToInt(RowPaddingLeft),
                Mathf.RoundToInt(RowPaddingRight),
                Mathf.RoundToInt(RowPaddingVertical),
                Mathf.RoundToInt(RowPaddingVertical));
            rowLayout.spacing = RowSpacing;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
        }

        LayoutElement rowLayoutElement = GetComponent<LayoutElement>();
        if (rowLayoutElement != null)
        {
            rowLayoutElement.preferredHeight = RowHeight;
            rowLayoutElement.minHeight = RowHeight;
            rowLayoutElement.preferredWidth = RowWidth;
            rowLayoutElement.minWidth = RowWidth;
            rowLayoutElement.flexibleWidth = 1f;
        }

        ApplySquareLayout(turnMarker != null ? turnMarker.transform as RectTransform : null, MarkerSize);

        RectTransform avatarMaskRect = avatarImage != null
            ? avatarImage.transform.parent as RectTransform
            : transform.Find("Avatar Mask") as RectTransform;
        ApplySquareLayout(avatarMaskRect, AvatarFrameSize);

        if (avatarImage != null)
        {
            avatarImage.preserveAspect = true;
            RectTransform avatarRect = avatarImage.rectTransform;
            avatarRect.anchorMin = Vector2.zero;
            avatarRect.anchorMax = Vector2.one;
            avatarRect.offsetMin = new Vector2(AvatarImagePadding, AvatarImagePadding);
            avatarRect.offsetMax = new Vector2(-AvatarImagePadding, -AvatarImagePadding);
        }

        if (nameText != null)
        {
            nameText.fontSizeMin = NameFontSizeMin;
            nameText.fontSizeMax = NameFontSizeMax;

            LayoutElement nameLayout = nameText.GetComponent<LayoutElement>();
            if (nameLayout != null)
            {
                nameLayout.minWidth = NameMinWidth;
                nameLayout.flexibleWidth = 1f;
                nameLayout.preferredHeight = NamePreferredHeight;
            }
        }
    }

    private static void ApplySquareLayout(RectTransform rectTransform, float size)
    {
        if (rectTransform == null)
            return;

        rectTransform.sizeDelta = new Vector2(size, size);

        LayoutElement layout = rectTransform.GetComponent<LayoutElement>();
        if (layout == null)
            return;

        layout.preferredWidth = size;
        layout.preferredHeight = size;
        layout.minWidth = size;
        layout.minHeight = size;
    }

    private void ResolveReferences()
    {
        backgroundImage ??= GetComponent<Image>();
        if (turnMarker == null)
            turnMarker = transform.Find("Turn Marker")?.GetComponent<Graphic>();
        if (avatarImage == null)
            avatarImage = transform.Find("Avatar Mask/Avatar")?.GetComponent<Image>();
        if (nameText == null)
            nameText = transform.Find("Name")?.GetComponent<TMP_Text>();
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite == null)
            circleSprite = CreateCircleSprite(new Color(1f, 1f, 1f, 1f), 64);

        return circleSprite;
    }

    private static Sprite GetFallbackAvatarSprite()
    {
        if (fallbackAvatarSprite == null)
            fallbackAvatarSprite = CreateFallbackAvatarSprite(128);

        return fallbackAvatarSprite;
    }

    private static Sprite CreateCircleSprite(Color fillColor, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;
        float radiusSqr = radius * radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distanceSqr = (new Vector2(x, y) - center).sqrMagnitude;
                texture.SetPixel(x, y, distanceSqr <= radiusSqr ? fillColor : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateFallbackAvatarSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color teal = new Color(0.25f, 0.68f, 0.72f, 1f);
        Color bone = new Color(0.96f, 0.96f, 0.9f, 1f);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.47f;
        float radiusSqr = radius * radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y);
                bool inside = (point - center).sqrMagnitude <= radiusSqr;
                Color color = inside ? teal : Color.clear;

                if (inside && IsSkullFeature(point, size))
                    color = bone;

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static bool IsSkullFeature(Vector2 point, int size)
    {
        Vector2 leftEye = new Vector2(size * 0.38f, size * 0.57f);
        Vector2 rightEye = new Vector2(size * 0.62f, size * 0.57f);
        Vector2 nose = new Vector2(size * 0.5f, size * 0.39f);

        float eyeRadius = size * 0.09f;
        float noseWidth = size * 0.045f;
        float noseHeight = size * 0.11f;

        if ((point - leftEye).sqrMagnitude <= eyeRadius * eyeRadius)
            return true;
        if ((point - rightEye).sqrMagnitude <= eyeRadius * eyeRadius)
            return true;
        if (Mathf.Abs(point.x - nose.x) <= noseWidth && point.y <= nose.y + noseHeight && point.y >= nose.y - noseHeight)
            return true;

        return point.y > size * 0.25f
            && point.y < size * 0.29f
            && point.x > size * 0.34f
            && point.x < size * 0.66f;
    }
}
