using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Match End Overlay UI")]
[DisallowMultipleComponent]
public sealed class MatchEndOverlayUI : MonoBehaviour
{
    private static readonly Vector2 ContentSize = new Vector2(680f, 340f);
    private static readonly Vector2 TitleSize = new Vector2(620f, 112f);
    private static readonly Vector2 AvatarsSize = new Vector2(420f, 178f);

    [Header("Game")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerPresentationRegistry presentationRegistry;

    [Header("Fields")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private RectTransform avatarsRoot;

    [Header("Fallback UI")]
    [SerializeField] private bool buildRuntimeLayoutWhenMissing = true;

    private static Sprite circleSprite;
    private static Sprite fallbackAvatarSprite;

    private GameManager subscribedManager;
    private PlayerPresentationRegistry subscribedRegistry;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        SetVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeToManager(gameManager);
        SubscribeToRegistry(presentationRegistry);
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromManager();
        UnsubscribeFromRegistry();
    }

    public static MatchEndOverlayUI EnsureDefaultFor(GameManager manager)
    {
        MatchEndOverlayUI overlay = FindFirstObjectByType<MatchEndOverlayUI>();
        if (overlay != null)
        {
            overlay.AssignGameManager(manager);
            return overlay;
        }

        GameObject canvasObject = new GameObject("Match End Overlay Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlayObject = new GameObject("Match End Overlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(MatchEndOverlayUI));
        overlayObject.transform.SetParent(canvasObject.transform, false);

        overlay = overlayObject.GetComponent<MatchEndOverlayUI>();
        overlay.AssignGameManager(manager);
        overlay.ConfigureRuntimeRect();
        overlay.ResolveReferences();
        overlay.Refresh();
        return overlay;
    }

    public void AssignGameManager(GameManager manager)
    {
        gameManager = manager;
        presentationRegistry = manager != null ? PlayerPresentationRegistry.EnsureDefaultFor(manager) : presentationRegistry;
        ResolveReferences();
        SubscribeToManager(gameManager);
        SubscribeToRegistry(presentationRegistry);
        Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();

        MatchEndResult result = gameManager != null ? gameManager.CurrentMatchEndResult : null;
        if (result == null || !result.HasWinner)
        {
            SetVisible(false);
            ClearAvatars();
            return;
        }

        SetVisible(true);
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.text = ResolveTitleText(result);
            titleText.color = Color.white;
        }

        RebuildAvatars(result.WinningTeam);
    }

    private void ConfigureRuntimeRect()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (presentationRegistry == null)
            presentationRegistry = gameManager != null
                ? PlayerPresentationRegistry.EnsureDefaultFor(gameManager)
                : FindFirstObjectByType<PlayerPresentationRegistry>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (titleText == null)
            titleText = transform.Find("Content/Title")?.GetComponent<TMP_Text>();

        if (avatarsRoot == null)
            avatarsRoot = transform.Find("Content/Avatars") as RectTransform;

        if (buildRuntimeLayoutWhenMissing && (titleText == null || avatarsRoot == null))
            BuildRuntimeLayout();

        ApplyRuntimeLayout();
    }

    private void BuildRuntimeLayout()
    {
        ConfigureRuntimeRect();

        Image overlayImage = GetComponent<Image>();
        if (overlayImage != null)
            overlayImage.color = new Color(0f, 0f, 0f, 0.58f);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        RectTransform contentRect;
        Transform contentTransform = transform.Find("Content");
        if (contentTransform == null)
        {
            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(transform, false);
            contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            contentRect = contentTransform as RectTransform;
        }

        if (contentRect != null)
        {
            VerticalLayoutGroup oldLayout = contentRect.GetComponent<VerticalLayoutGroup>();
            if (oldLayout != null)
                oldLayout.enabled = false;
        }

        if (titleText == null)
        {
            GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            titleObject.transform.SetParent(contentRect, false);

            titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.text = "VICTORY";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 48f;
            titleText.fontSizeMax = 84f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.raycastTarget = false;
            titleText.color = Color.white;
            titleText.outlineColor = Color.black;
            titleText.outlineWidth = 0.14f;

            LayoutElement titleLayout = titleObject.GetComponent<LayoutElement>();
            titleLayout.preferredHeight = 96f;
            titleLayout.minHeight = 96f;
            titleLayout.flexibleWidth = 1f;
        }

        if (avatarsRoot == null)
        {
            GameObject avatarsObject = new GameObject("Avatars", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            avatarsObject.transform.SetParent(contentRect, false);
            avatarsRoot = avatarsObject.GetComponent<RectTransform>();

            HorizontalLayoutGroup layout = avatarsObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = avatarsObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement avatarsLayout = avatarsObject.GetComponent<LayoutElement>();
            avatarsLayout.preferredHeight = 174f;
            avatarsLayout.minHeight = 174f;
            avatarsLayout.flexibleWidth = 1f;
        }

        ApplyRuntimeLayout();
    }

    private void ApplyRuntimeLayout()
    {
        RectTransform contentRect = transform.Find("Content") as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, 12f);
            contentRect.sizeDelta = ContentSize;

            VerticalLayoutGroup oldLayout = contentRect.GetComponent<VerticalLayoutGroup>();
            if (oldLayout != null)
                oldLayout.enabled = false;
        }

        if (titleText != null)
        {
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 96f);
            titleRect.sizeDelta = TitleSize;

            LayoutElement titleLayout = titleText.GetComponent<LayoutElement>();
            if (titleLayout != null)
                titleLayout.ignoreLayout = true;
        }

        if (avatarsRoot != null)
        {
            avatarsRoot.anchorMin = new Vector2(0.5f, 0.5f);
            avatarsRoot.anchorMax = new Vector2(0.5f, 0.5f);
            avatarsRoot.pivot = new Vector2(0.5f, 0.5f);
            avatarsRoot.anchoredPosition = new Vector2(0f, -58f);
            avatarsRoot.sizeDelta = AvatarsSize;

            LayoutElement avatarsLayout = avatarsRoot.GetComponent<LayoutElement>();
            if (avatarsLayout != null)
                avatarsLayout.ignoreLayout = true;
        }
    }

    private void RebuildAvatars(Team winningTeam)
    {
        if (avatarsRoot == null)
            return;

        ClearAvatars();

        if (winningTeam == null)
            return;

        foreach (Skeleton player in winningTeam.Skeletons)
        {
            if (player == null)
                continue;

            PlayerPresentation presentation = presentationRegistry != null
                ? presentationRegistry.Resolve(player)
                : null;

            CreateAvatarSlot(avatarsRoot, presentation, player);
        }
    }

    private string ResolveTitleText(MatchEndResult result)
    {
        Skeleton localPlayer = gameManager != null ? gameManager.LocalPlayer : null;
        if (localPlayer == null || result == null || result.WinningTeam == null)
            return "VICTORY";

        return result.WinningTeam.HasPlayer(localPlayer) ? "VICTORY" : "DEFEAT";
    }

    private void ClearAvatars()
    {
        if (avatarsRoot == null)
            return;

        for (int i = avatarsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = avatarsRoot.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    private static void CreateAvatarSlot(Transform parent, PlayerPresentation presentation, Skeleton player)
    {
        GameObject slotObject = new GameObject("Winner Avatar", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        slotObject.transform.SetParent(parent, false);

        LayoutElement slotLayout = slotObject.GetComponent<LayoutElement>();
        slotLayout.preferredWidth = 150f;
        slotLayout.minWidth = 150f;
        slotLayout.preferredHeight = 174f;
        slotLayout.minHeight = 174f;

        VerticalLayoutGroup slotGroup = slotObject.GetComponent<VerticalLayoutGroup>();
        slotGroup.spacing = 10f;
        slotGroup.childAlignment = TextAnchor.UpperCenter;
        slotGroup.childControlWidth = false;
        slotGroup.childControlHeight = false;
        slotGroup.childForceExpandWidth = false;
        slotGroup.childForceExpandHeight = false;

        Color teamColor = presentation != null ? presentation.TeamColor : Color.white;

        GameObject frameObject = new GameObject("Avatar Frame", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        frameObject.transform.SetParent(slotObject.transform, false);
        Image frameImage = frameObject.GetComponent<Image>();
        frameImage.sprite = GetCircleSprite();
        frameImage.color = teamColor;
        frameImage.raycastTarget = false;
        ApplySquareLayout(frameObject.GetComponent<RectTransform>(), 112f);

        GameObject maskObject = new GameObject("Avatar Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
        maskObject.transform.SetParent(frameObject.transform, false);
        RectTransform maskRect = maskObject.GetComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = new Vector2(8f, 8f);
        maskRect.offsetMax = new Vector2(-8f, -8f);

        Image maskImage = maskObject.GetComponent<Image>();
        maskImage.sprite = GetCircleSprite();
        maskImage.color = Color.white;

        Mask mask = maskObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject avatarObject = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
        avatarObject.transform.SetParent(maskObject.transform, false);
        Image avatarImage = avatarObject.GetComponent<Image>();
        avatarImage.sprite = presentation != null && presentation.AvatarSprite != null
            ? presentation.AvatarSprite
            : GetFallbackAvatarSprite();
        avatarImage.preserveAspect = true;
        avatarImage.raycastTarget = false;
        avatarImage.color = Color.white;

        RectTransform avatarRect = avatarImage.rectTransform;
        avatarRect.anchorMin = Vector2.zero;
        avatarRect.anchorMax = Vector2.one;
        avatarRect.offsetMin = Vector2.zero;
        avatarRect.offsetMax = Vector2.zero;

        GameObject nameObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        nameObject.transform.SetParent(slotObject.transform, false);
        TextMeshProUGUI nameText = nameObject.GetComponent<TextMeshProUGUI>();
        nameText.text = presentation != null ? presentation.DisplayName : "Player";
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 14f;
        nameText.fontSizeMax = 24f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.raycastTarget = false;
        nameText.color = teamColor;
        nameText.outlineColor = Color.black;
        nameText.outlineWidth = 0.08f;

        LayoutElement nameLayout = nameObject.GetComponent<LayoutElement>();
        nameLayout.preferredWidth = 150f;
        nameLayout.minWidth = 150f;
        nameLayout.preferredHeight = 42f;
        nameLayout.minHeight = 42f;
    }

    private void SubscribeToManager(GameManager manager)
    {
        if (subscribedManager == manager)
            return;

        UnsubscribeFromManager();
        subscribedManager = manager;
        if (subscribedManager == null)
            return;

        subscribedManager.OnGameCreated += HandleGameCreated;
        subscribedManager.OnMatchEnded += HandleMatchEnded;
    }

    private void UnsubscribeFromManager()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.OnGameCreated -= HandleGameCreated;
        subscribedManager.OnMatchEnded -= HandleMatchEnded;
        subscribedManager = null;
    }

    private void SubscribeToRegistry(PlayerPresentationRegistry registry)
    {
        if (subscribedRegistry == registry)
            return;

        UnsubscribeFromRegistry();
        subscribedRegistry = registry;
        if (subscribedRegistry == null)
            return;

        subscribedRegistry.PresentationChanged += HandlePresentationChanged;
    }

    private void UnsubscribeFromRegistry()
    {
        if (subscribedRegistry == null)
            return;

        subscribedRegistry.PresentationChanged -= HandlePresentationChanged;
        subscribedRegistry = null;
    }

    private void HandleGameCreated(CardGame game)
    {
        Refresh();
    }

    private void HandleMatchEnded(MatchEndResult result)
    {
        Refresh();
    }

    private void HandlePresentationChanged(Skeleton player)
    {
        if (gameManager != null && gameManager.IsMatchEnded)
            Refresh();
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
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

    private static Sprite GetCircleSprite()
    {
        if (circleSprite == null)
            circleSprite = CreateCircleSprite(new Color(1f, 1f, 1f, 1f), 96);

        return circleSprite;
    }

    private static Sprite GetFallbackAvatarSprite()
    {
        if (fallbackAvatarSprite == null)
            fallbackAvatarSprite = Resources.Load<Sprite>("UI/SkeletonAvatarPlaceholder");

        if (fallbackAvatarSprite == null)
            fallbackAvatarSprite = CreateCircleSprite(new Color(0.12f, 1f, 0.72f, 1f), 128);

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
}
