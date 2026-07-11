#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Inventory Canvas UI")]
[DisallowMultipleComponent]
public class InventoryCanvasUI : MonoBehaviour
{
    private const string RuntimeItemViewName = "Runtime Item View";
    private const int PreviewLayer = 31;

    [Header("Inventory")]
    [SerializeField] private PlayerInventoryOwner? inventoryOwner;
    [SerializeField] private Inventory? inventory;
    [SerializeField] private bool autoBindRuntimeInventory = true;

    [Header("Canvas")]
    [SerializeField] private RectTransform? inventoryPanel;
    [SerializeField] private Canvas? rootCanvas;
    [SerializeField] private CanvasGroup? canvasGroup;
    [SerializeField] private RectTransform[] slotRoots = new RectTransform[0];

    [Header("Preview")]
    [SerializeField, Min(64)] private int previewTextureSize = 256;
    [SerializeField] private Color emptySlotColor = new(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color draggedSlotColor = new(1f, 1f, 1f, 0.9f);

    private readonly List<SlotView> slotViews = new();
    private readonly List<RaycastResult> raycastResults = new();
    private int draggingSlotIndex = -1;

    public Inventory? Inventory => inventory;

    public bool IsVisible
    {
        get
        {
            bool canvasVisible = rootCanvas == null || rootCanvas.enabled;
            bool groupVisible = canvasGroup == null || canvasGroup.alpha > 0.001f;
            return isActiveAndEnabled && canvasVisible && groupVisible;
        }
    }

    private void Reset()
    {
        ResolveCanvasReferences();
        ResolveSlotRoots();
    }

    private void Awake()
    {
        ResolveCanvasReferences();
        ResolveSlotRoots();
        EnsureSlotViews();
        TryBindInventory();
        Refresh();
    }

    private void Update()
    {
        if (autoBindRuntimeInventory && inventory == null)
        {
            TryBindInventory();
        }

        if (IsVisible)
        {
            Refresh();
        }
    }

    private void OnDestroy()
    {
        ClearSlotViews();
    }

    public void Show()
    {
        // Ghosts have no physical inventory
        if (IsLocalPlayerGhost())
        {
            Hide();
            return;
        }

        ResolveCanvasReferences();

        if (rootCanvas != null)
        {
            rootCanvas.enabled = true;
        }

        GraphicRaycaster? raycaster = GetGraphicRaycaster();
        if (raycaster != null)
        {
            raycaster.enabled = true;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        Refresh();
    }

    private bool IsLocalPlayerGhost()
    {
        return inventoryOwner?.OwnerSkeleton?.IsGhost == true;
    }

    public void Hide()
    {
        ResolveCanvasReferences();

        if (rootCanvas != null)
        {
            rootCanvas.enabled = false;
        }

        GraphicRaycaster? raycaster = GetGraphicRaycaster();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void Refresh()
    {
        EnsureSlotViews();

        for (int i = 0; i < slotViews.Count; i++)
        {
            IItem? item = inventory != null && i < inventory.Items.Length ? inventory.Items[i] : null;
            SetSlotView(slotViews[i], item, i == draggingSlotIndex);
        }
    }

    public void ClickSlot(InventoryCanvasSlot slot, PointerEventData eventData)
    {
        if (inventory == null || inventoryOwner == null || slot == null || !slot.BelongsTo(this))
        {
            return;
        }

        PlayerHand? hand = eventData.button switch
        {
            PointerEventData.InputButton.Left => inventoryOwner.leftHand,
            PointerEventData.InputButton.Right => inventoryOwner.rightHand,
            _ => null
        };

        if (hand == null)
        {
            return;
        }

        inventory.SwapWithHand(hand, slot.SlotIndex);
        Refresh();
    }

    public void BeginDragSlot(InventoryCanvasSlot slot, PointerEventData eventData)
    {
        if (inventory == null || slot == null || !slot.BelongsTo(this) || !HasItem(slot.SlotIndex))
        {
            return;
        }

        draggingSlotIndex = slot.SlotIndex;
        Refresh();
    }

    public void DragSlot(InventoryCanvasSlot slot, PointerEventData eventData)
    {
    }

    public void EndDragSlot(InventoryCanvasSlot slot, PointerEventData eventData)
    {
        if (inventory == null || draggingSlotIndex < 0)
        {
            draggingSlotIndex = -1;
            Refresh();
            return;
        }

        int sourceSlotIndex = draggingSlotIndex;
        draggingSlotIndex = -1;

        if (TryGetSlotUnderPointer(eventData, out int targetSlotIndex) && targetSlotIndex != sourceSlotIndex)
        {
            IItem? targetItem = inventory.Items[targetSlotIndex];
            inventory.Items[targetSlotIndex] = inventory.Items[sourceSlotIndex];
            inventory.Items[sourceSlotIndex] = targetItem;
        }
        else if (!IsPointerInsideInventory(eventData))
        {
            IItem? item = inventory.Items[sourceSlotIndex];
            if (item != null)
            {
                inventory.Items[sourceSlotIndex] = null;
                ItemUtils.DropItem(item, transform.position, transform.rotation);
            }
        }

        Refresh();
    }

    public bool TryGetSlotIndex(InventoryCanvasSlot slot, out int slotIndex)
    {
        slotIndex = slot != null && slot.BelongsTo(this) ? slot.SlotIndex : -1;
        return slotIndex >= 0;
    }

    private bool HasItem(int slotIndex)
    {
        return inventory != null
            && slotIndex >= 0
            && slotIndex < inventory.Items.Length
            && inventory.Items[slotIndex] != null;
    }

    private bool TryBindInventory()
    {
        if (inventoryOwner != null)
        {
            inventory = inventoryOwner.Inventory != null
                ? inventoryOwner.Inventory
                : inventoryOwner.GetComponent<Inventory>();
            return inventory != null;
        }

        if (inventory != null)
        {
            inventoryOwner = inventoryOwner != null ? inventoryOwner : inventory.GetComponent<PlayerInventoryOwner>();
            return true;
        }

        PlayerInventoryOwner? foundOwner = FindFirstObjectByType<PlayerInventoryOwner>();
        if (foundOwner != null)
        {
            inventoryOwner = foundOwner;
            inventory = foundOwner.Inventory != null ? foundOwner.Inventory : foundOwner.GetComponent<Inventory>();
            return inventory != null;
        }

        inventory = FindFirstObjectByType<Inventory>();
        return inventory != null;
    }

    private void EnsureSlotViews()
    {
        ResolveSlotRoots();
        int requiredCount = inventory != null ? inventory.MaxSlots : slotRoots.Length;

        while (slotViews.Count > requiredCount)
        {
            int lastIndex = slotViews.Count - 1;
            slotViews[lastIndex].Dispose();
            slotViews.RemoveAt(lastIndex);
        }

        while (slotViews.Count < requiredCount)
        {
            RectTransform? root = slotViews.Count < slotRoots.Length ? slotRoots[slotViews.Count] : null;
            if (root == null)
            {
                break;
            }

            slotViews.Add(new SlotView(this, root, slotViews.Count, previewTextureSize));
        }
    }

    private void SetSlotView(SlotView view, IItem? item, bool isDragging)
    {
        view.Background.color = isDragging ? draggedSlotColor : emptySlotColor;
        if (view.Item == item)
        {
            view.Root.gameObject.SetActive(view.Index < (inventory?.MaxSlots ?? slotRoots.Length));
            return;
        }

        view.SetItem(item);
    }

    private void ResolveCanvasReferences()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
            }
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (inventoryPanel == null)
        {
            inventoryPanel = transform as RectTransform;
        }
    }

    private void ResolveSlotRoots()
    {
        if (slotRoots != null && slotRoots.Length > 0)
        {
            return;
        }

        List<RectTransform> found = new();
        foreach (Transform child in transform)
        {
            if (child is RectTransform childRect && child.name.StartsWith("Slot "))
            {
                found.Add(childRect);
            }
        }

        found.Sort((left, right) => GetSlotNumber(left.name).CompareTo(GetSlotNumber(right.name)));
        slotRoots = found.ToArray();
    }

    private bool TryGetSlotUnderPointer(PointerEventData eventData, out int slotIndex)
    {
        slotIndex = -1;
        if (eventData == null || EventSystem.current == null)
        {
            return false;
        }

        raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        foreach (RaycastResult result in raycastResults)
        {
            InventoryCanvasSlot slot = result.gameObject.GetComponentInParent<InventoryCanvasSlot>();
            if (slot != null && slot.BelongsTo(this))
            {
                slotIndex = slot.SlotIndex;
                return true;
            }
        }

        return false;
    }

    private bool IsPointerInsideInventory(PointerEventData eventData)
    {
        if (inventoryPanel == null || eventData == null)
        {
            return false;
        }

        Camera? eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;
        return RectTransformUtility.RectangleContainsScreenPoint(inventoryPanel, eventData.position, eventCamera);
    }

    private void ClearSlotViews()
    {
        foreach (SlotView slotView in slotViews)
        {
            slotView.Dispose();
        }

        slotViews.Clear();
    }

    private GraphicRaycaster? GetGraphicRaycaster()
    {
        if (rootCanvas != null)
        {
            return rootCanvas.GetComponent<GraphicRaycaster>();
        }

        return GetComponent<GraphicRaycaster>();
    }

    private static int GetSlotNumber(string slotName)
    {
        string[] parts = slotName.Split(' ');
        return parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int number) ? number : int.MaxValue;
    }

    private sealed class SlotView
    {
        private readonly Camera _camera;
        private readonly RenderTexture _texture;
        private GameObject? _itemView;

        public SlotView(InventoryCanvasUI owner, RectTransform root, int index, int textureSize)
        {
            Root = root;
            Index = index;
            Slot = root.GetComponent<InventoryCanvasSlot>() ?? root.gameObject.AddComponent<InventoryCanvasSlot>();
            Slot.Initialize(owner, index);

            Background = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            Background.raycastTarget = true;

            RawImage = EnsureRawImage(root);
            _texture = new RenderTexture(textureSize, textureSize, 16);
            RawImage.texture = _texture;

            GameObject cameraObject = new($"Inventory Slot Camera {index + 1}");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            _camera = cameraObject.AddComponent<Camera>();
            _camera.enabled = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.cullingMask = 1 << PreviewLayer;
            _camera.targetTexture = _texture;
            _camera.orthographic = true;
            _camera.orthographicSize = 1.4f;
            _camera.transform.position = new Vector3(index * 4f, -1000f, -4f);
            _camera.transform.rotation = Quaternion.identity;
        }

        public RectTransform Root { get; }
        public int Index { get; }
        public InventoryCanvasSlot Slot { get; }
        public Image Background { get; }
        public RawImage RawImage { get; }
        public IItem? Item { get; private set; }

        public void SetItem(IItem? item)
        {
            ClearItemView();
            Item = item;

            if (item == null)
            {
                RawImage.enabled = false;
                return;
            }

            _itemView = item.CreateInventoryView();
            if (_itemView == null)
            {
                RawImage.enabled = false;
                return;
            }

            RawImage.enabled = true;
            _itemView.name = RuntimeItemViewName;
            _itemView.hideFlags = HideFlags.HideAndDontSave;
            _itemView.transform.SetPositionAndRotation(_camera.transform.position + Vector3.forward * 2f, Quaternion.identity);
            _itemView.transform.localScale = Vector3.one;
            SetLayerRecursively(_itemView, PreviewLayer);
            DisablePhysics(_itemView);
            FitItemView(_itemView, _camera.transform.position + Vector3.forward * 2f);
        }

        public void Dispose()
        {
            ClearItemView();

            if (_camera != null)
            {
                Object.Destroy(_camera.gameObject);
            }

            if (_texture != null)
            {
                _texture.Release();
                Object.Destroy(_texture);
            }
        }

        private void ClearItemView()
        {
            if (_itemView == null)
            {
                return;
            }

            Object.Destroy(_itemView);
            _itemView = null;
        }

        private static RawImage EnsureRawImage(RectTransform root)
        {
            Transform existing = root.Find(RuntimeItemViewName);
            GameObject rawImageObject = existing != null
                ? existing.gameObject
                : new GameObject(RuntimeItemViewName, typeof(RectTransform), typeof(RawImage));
            rawImageObject.transform.SetParent(root, false);

            RawImage rawImage = rawImageObject.GetComponent<RawImage>();
            rawImage.raycastTarget = false;

            RectTransform rect = rawImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            return rawImage;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void DisablePhysics(GameObject target)
        {
            foreach (Collider collider in target.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            foreach (Rigidbody body in target.GetComponentsInChildren<Rigidbody>())
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        private static void FitItemView(GameObject target, Vector3 center)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            target.transform.position += center - bounds.center;
            float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxSize > 0.001f)
            {
                target.transform.localScale *= 1.6f / maxSize;
            }
        }
    }
}
