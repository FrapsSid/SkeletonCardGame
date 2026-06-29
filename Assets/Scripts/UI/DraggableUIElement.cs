using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("UI/Draggable UI Element")]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class DraggableUIElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Target")]
    [SerializeField] private RectTransform target;
    [SerializeField] private RectTransform dragBounds = null;
    [SerializeField] private Canvas canvas;

    [Header("Behavior")]
    [SerializeField] private bool draggable = true;
    [SerializeField] private bool keepInsideBounds = true;
    [SerializeField] private bool bringToFrontOnBeginDrag = true;
    [SerializeField] private bool disableRaycastsWhileDragging = false;
    [SerializeField] private PointerEventData.InputButton dragButton = PointerEventData.InputButton.Left;

    [Header("Axis Locks")]
    [SerializeField] private bool lockHorizontal = false;
    [SerializeField] private bool lockVertical = false;

    private RectTransform parentRect;
    private CanvasGroup canvasGroup;
    private Vector2 pointerOffset;
    private Vector3 startLocalPosition;
    private bool isDragging;
    private bool originalBlocksRaycasts = true;

    public bool IsDragging => isDragging;

    private void Reset()
    {
        target = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }
    }

    public void SetDraggable(bool value)
    {
        draggable = value;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ResolveReferences();

        if (!draggable || target == null || parentRect == null || eventData.button != dragButton)
        {
            return;
        }

        Camera eventCamera = GetEventCamera(eventData);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventCamera, out Vector2 localPointerPosition))
        {
            return;
        }

        isDragging = true;
        startLocalPosition = target.localPosition;
        pointerOffset = (Vector2)target.localPosition - localPointerPosition;

        if (bringToFrontOnBeginDrag)
        {
            target.SetAsLastSibling();
        }

        if (disableRaycastsWhileDragging && canvasGroup != null)
        {
            originalBlocksRaycasts = canvasGroup.blocksRaycasts;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || target == null || parentRect == null)
        {
            return;
        }

        Camera eventCamera = GetEventCamera(eventData);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventCamera, out Vector2 localPointerPosition))
        {
            return;
        }

        Vector3 nextPosition = localPointerPosition + pointerOffset;
        nextPosition.z = target.localPosition.z;

        if (lockHorizontal)
        {
            nextPosition.x = startLocalPosition.x;
        }

        if (lockVertical)
        {
            nextPosition.y = startLocalPosition.y;
        }

        target.localPosition = nextPosition;

        if (keepInsideBounds)
        {
            ClampInsideBounds();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (disableRaycastsWhileDragging && canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = originalBlocksRaycasts;
        }

        isDragging = false;
    }

    private void ResolveReferences()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }

        if (target != null)
        {
            parentRect = target.parent as RectTransform;
        }

        if (canvas == null)
        {
            canvas = target != null ? target.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = target != null ? target.GetComponent<CanvasGroup>() : GetComponent<CanvasGroup>();
        }
    }

    private Camera GetEventCamera(PointerEventData eventData)
    {
        if (eventData.pressEventCamera != null)
        {
            return eventData.pressEventCamera;
        }

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return canvas.worldCamera;
        }

        return null;
    }

    private void ClampInsideBounds()
    {
        RectTransform boundsRect = dragBounds != null ? dragBounds : parentRect;
        if (target == null || boundsRect == null || parentRect == null)
        {
            return;
        }

        Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(boundsRect, target);
        Rect bounds = boundsRect.rect;
        Vector3 correction = Vector3.zero;

        if (targetBounds.min.x < bounds.xMin)
        {
            correction.x = bounds.xMin - targetBounds.min.x;
        }
        else if (targetBounds.max.x > bounds.xMax)
        {
            correction.x = bounds.xMax - targetBounds.max.x;
        }

        if (targetBounds.min.y < bounds.yMin)
        {
            correction.y = bounds.yMin - targetBounds.min.y;
        }
        else if (targetBounds.max.y > bounds.yMax)
        {
            correction.y = bounds.yMax - targetBounds.max.y;
        }

        if (correction == Vector3.zero)
        {
            return;
        }

        Vector3 worldCorrection = boundsRect.TransformVector(correction);
        Vector3 parentCorrection = parentRect.InverseTransformVector(worldCorrection);

        target.localPosition += parentCorrection;
    }
}
