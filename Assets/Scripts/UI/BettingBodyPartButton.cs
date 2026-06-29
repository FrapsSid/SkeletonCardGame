using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Betting Body Part Button")]
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class BettingBodyPartButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private BettingCanvasUI bettingUI = null;
    [SerializeField] private BodyPartType bodyPartType = BodyPartType.Head;

    private Button button = null;
    private RectTransform dragGhost = null;
    private Canvas dragCanvas = null;
    private bool dragging;

    public BodyPartType BodyPartType => bodyPartType;

    private void Awake()
    {
        ResolveButton();
    }

    private void OnEnable()
    {
        RegisterClick();
    }

    private void OnDisable()
    {
        UnregisterClick();
        DestroyDragGhost();
    }

    public void Configure(BettingCanvasUI ui, BodyPartType partType)
    {
        bettingUI = ui;
        bodyPartType = partType;
        ResolveButton();
        RegisterClick();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = bettingUI != null && bettingUI.CanDragBodyPart(bodyPartType);
        if (!dragging)
            return;

        CreateDragGhost(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || dragGhost == null)
            return;

        dragGhost.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragging && bettingUI != null && eventData != null && eventData.pointerCurrentRaycast.gameObject != null)
        {
            BettingTierDropTarget target = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<BettingTierDropTarget>();
            if (target != null)
                bettingUI.DropBodyPartOnTier(bodyPartType, target.Tier);
        }

        dragging = false;
        DestroyDragGhost();
    }

    private void HandleClicked()
    {
        if (bettingUI != null)
            bettingUI.ToggleBodyPartSelection(bodyPartType);
    }

    private void CreateDragGhost(PointerEventData eventData)
    {
        DestroyDragGhost();

        dragCanvas = GetComponentInParent<Canvas>();
        if (dragCanvas == null)
            return;

        GameObject ghost = new GameObject($"{name} Drag Ghost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        dragGhost = ghost.GetComponent<RectTransform>();
        dragGhost.SetParent(dragCanvas.transform, false);
        dragGhost.SetAsLastSibling();
        dragGhost.position = eventData.position;

        RectTransform sourceRect = transform as RectTransform;
        if (sourceRect != null)
            dragGhost.sizeDelta = sourceRect.rect.size;

        Image ghostImage = ghost.GetComponent<Image>();
        Image sourceImage = GetComponent<Image>();
        if (sourceImage != null)
        {
            ghostImage.sprite = sourceImage.sprite;
            ghostImage.type = sourceImage.type;
            ghostImage.preserveAspect = sourceImage.preserveAspect;
            ghostImage.color = sourceImage.color;
        }
        else if (button != null && button.targetGraphic != null)
        {
            ghostImage.color = button.targetGraphic.color;
        }

        CanvasGroup group = ghost.GetComponent<CanvasGroup>();
        group.alpha = 0.65f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    private void DestroyDragGhost()
    {
        if (dragGhost != null)
            Destroy(dragGhost.gameObject);

        dragGhost = null;
        dragCanvas = null;
    }

    private void ResolveButton()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void RegisterClick()
    {
        ResolveButton();
        if (button == null)
            return;

        button.onClick.RemoveListener(HandleClicked);
        button.onClick.AddListener(HandleClicked);
    }

    private void UnregisterClick()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }
}
