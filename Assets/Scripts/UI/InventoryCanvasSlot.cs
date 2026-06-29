using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("UI/Inventory Canvas Slot")]
[DisallowMultipleComponent]
public class InventoryCanvasSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InventoryCanvasUI owner;

    public int SlotIndex { get; private set; } = -1;

    public void Initialize(InventoryCanvasUI inventoryOwner, int slotIndex)
    {
        owner = inventoryOwner;
        SlotIndex = slotIndex;
    }

    public bool BelongsTo(InventoryCanvasUI inventoryOwner)
    {
        return owner == inventoryOwner;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        owner?.BeginDragSlot(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner?.DragSlot(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        owner?.EndDragSlot(this, eventData);
    }
}
