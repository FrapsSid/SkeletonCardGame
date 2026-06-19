using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public Image iconImage;
    public TMP_Text countText;
    public Button dropButton;

    private InventoryUI _owner;
    private int _slotIndex;

    public void Initialize(InventoryUI owner, int slotIndex) {
        _owner = owner;
        _slotIndex = slotIndex;

        if (dropButton) {
            dropButton.onClick.RemoveAllListeners();
            dropButton.onClick.AddListener(() => _owner.DropSlot(_slotIndex));
        }
    }

    public void SetSlot(InventorySlot slot) {
        bool hasItem = slot != null && !slot.IsEmpty && slot.quantity > 0;
        bool hasIcon = hasItem && slot.itemData && slot.itemData.icon;

        if (iconImage is not null) {
            iconImage.enabled = hasItem;
            iconImage.sprite = hasIcon ? slot.itemData.icon : null;
            iconImage.color = hasIcon ? Color.white : new Color(1f, 1f, 1f, 0.18f);
        }

        if (countText) {
            countText.text = hasItem && slot.quantity > 1 ? slot.quantity.ToString() : string.Empty;
        }

        if (dropButton) {
            dropButton.gameObject.SetActive(hasItem);
            dropButton.interactable = hasItem;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        _owner?.ShowTooltip(_slotIndex);
    }

    public void OnPointerExit(PointerEventData eventData) {
        _owner?.ClearHoveredSlot(_slotIndex);
    }

    public bool IsDropButtonObject(GameObject target) {
        return dropButton && target && target.transform.IsChildOf(dropButton.transform);
    }
}
