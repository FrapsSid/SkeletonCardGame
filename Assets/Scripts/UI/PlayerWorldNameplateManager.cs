using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Player World Nameplate Manager")]
[DisallowMultipleComponent]
public sealed class PlayerWorldNameplateManager : MonoBehaviour
{
    private sealed class NameplateView
    {
        public GameObject Root;
        public Canvas Canvas;
        public TMP_Text Text;
    }

    [Header("Game")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerPresentationRegistry presentationRegistry;
    [SerializeField] private Camera targetCamera;

    [Header("Nameplate")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.22f, 0f);
    [SerializeField] private float fontSize = 24f;
    [SerializeField] private float worldScale = 0.01f;
    [SerializeField] private int maxVisiblePlayers = 4;
    [SerializeField] private float refreshIntervalSeconds = 0.25f;

    private readonly Dictionary<Skeleton, NameplateView> nameplates = new();
    private readonly List<Skeleton> visiblePlayers = new();
    private GameManager subscribedManager;
    private float nextRefreshTime;

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
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
        ClearNameplates();
    }

    private void LateUpdate()
    {
        ResolveCamera();

        if (Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + refreshIntervalSeconds;
            Refresh();
        }

        UpdateNameplateTransforms();
    }

    public static PlayerWorldNameplateManager EnsureDefaultFor(GameManager manager)
    {
        PlayerWorldNameplateManager nameplateManager = FindFirstObjectByType<PlayerWorldNameplateManager>();
        if (nameplateManager != null)
        {
            nameplateManager.AssignGameManager(manager);
            return nameplateManager;
        }

        GameObject managerObject = new GameObject("Player World Nameplates");
        nameplateManager = managerObject.AddComponent<PlayerWorldNameplateManager>();
        nameplateManager.AssignGameManager(manager);
        return nameplateManager;
    }

    public void AssignGameManager(GameManager manager)
    {
        gameManager = manager;
        ResolveReferences();
        SubscribeToManager(gameManager);
        Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();
        visiblePlayers.Clear();

        if (gameManager == null)
        {
            ClearNameplates();
            return;
        }

        int count = Mathf.Min(maxVisiblePlayers, gameManager.Players.Count);
        for (int i = 0; i < count; i++)
        {
            Skeleton player = gameManager.Players[i];
            if (player != null && player.Body != null)
                visiblePlayers.Add(player);
        }

        RemoveMissingNameplates();

        foreach (Skeleton player in visiblePlayers)
        {
            NameplateView view = GetOrCreateNameplate(player);
            PlayerPresentation presentation = presentationRegistry != null ? presentationRegistry.Resolve(player) : null;

            string displayName = presentation != null ? presentation.DisplayName : string.Empty;
            string betInfo = ResolveBetInfo(player);

            view.Text.text = string.IsNullOrEmpty(betInfo) 
                ? displayName 
                : $"{displayName}\n{betInfo}";
                
            view.Text.color = presentation != null ? presentation.TeamColor : Color.white;
            view.Root.SetActive(true);
        }
    }

    private string ResolveBetInfo(Skeleton player)
    {
        CardGame game = gameManager != null ? gameManager.CardGame : null;
        if (game == null || game.round == null)
            return string.Empty;

        if (!game.round.playerStates.TryGetValue(player, out PlayerBetState state))
            return string.Empty;

        if (state.hasFolded)
            return "FOLD";

        int betAmount = state.committedValue;
        string tierText = state.declaredTarget.HasValue ? state.declaredTarget.Value.ToString() : "-";
        
        return $"Bet: {betAmount} | {tierText}";
    }

    private void UpdateNameplateTransforms()
    {
        foreach (KeyValuePair<Skeleton, NameplateView> pair in nameplates)
        {
            Skeleton player = pair.Key;
            NameplateView view = pair.Value;
            if (player == null || player.Body == null || view?.Root == null)
                continue;

            view.Root.transform.position = ResolveNameplatePosition(player);

            if (targetCamera != null)
            {
                view.Root.transform.rotation = targetCamera.transform.rotation;
                if (view.Canvas != null)
                    view.Canvas.worldCamera = targetCamera;
            }
        }
    }

    private NameplateView GetOrCreateNameplate(Skeleton player)
    {
        if (nameplates.TryGetValue(player, out NameplateView view) && view?.Root != null)
            return view;

        GameObject root = new GameObject("Player Nameplate", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(220f, 42f);
        root.transform.localScale = Vector3.one * worldScale;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        canvas.worldCamera = targetCamera;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        GameObject textObject = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = fontSize;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.16f;
        text.raycastTarget = false;

        view = new NameplateView
        {
            Root = root,
            Canvas = canvas,
            Text = text
        };
        nameplates[player] = view;
        return view;
    }

    private Vector3 ResolveNameplatePosition(Skeleton player)
    {
        if (player?.Body == null)
            return Vector3.zero;

        Renderer[] renderers = player.Body.GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) + worldOffset;

        Transform anchor = player.Body.headBone != null ? player.Body.headBone : player.Body.transform;
        return anchor.position + new Vector3(worldOffset.x, 0.8f + worldOffset.y, worldOffset.z);
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (presentationRegistry == null)
            presentationRegistry = gameManager != null
                ? PlayerPresentationRegistry.EnsureDefaultFor(gameManager)
                : FindFirstObjectByType<PlayerPresentationRegistry>();

        ResolveCamera();
    }

    private void ResolveCamera()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            targetCamera = Camera.main;
    }

    private void RemoveMissingNameplates()
    {
        List<Skeleton> removedPlayers = null;
        foreach (KeyValuePair<Skeleton, NameplateView> pair in nameplates)
        {
            if (!visiblePlayers.Contains(pair.Key))
            {
                removedPlayers ??= new List<Skeleton>();
                removedPlayers.Add(pair.Key);
            }
        }

        if (removedPlayers == null)
            return;

        foreach (Skeleton player in removedPlayers)
        {
            if (nameplates.TryGetValue(player, out NameplateView view) && view?.Root != null)
                Destroy(view.Root);

            nameplates.Remove(player);
        }
    }

    private void ClearNameplates()
    {
        foreach (NameplateView view in nameplates.Values)
        {
            if (view?.Root != null)
                Destroy(view.Root);
        }

        nameplates.Clear();
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
    }

    private void UnsubscribeFromManager()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.OnGameCreated -= HandleGameCreated;
        subscribedManager = null;
    }

    private void SubscribeToRegistry(PlayerPresentationRegistry registry)
    {
        if (registry == null)
            return;

        registry.PresentationChanged -= HandlePresentationChanged;
        registry.PresentationChanged += HandlePresentationChanged;
    }

    private void UnsubscribeFromRegistry()
    {
        if (presentationRegistry == null)
            return;

        presentationRegistry.PresentationChanged -= HandlePresentationChanged;
    }

    private void HandleGameCreated(CardGame game)
    {
        Refresh();
    }

    private void HandlePresentationChanged(Skeleton player)
    {
        Refresh();
    }
}
