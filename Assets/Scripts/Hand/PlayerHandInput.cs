using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHand))]
public class PlayerHandInput : MonoBehaviour {
    public KeyCode selectPreviousKey = KeyCode.Z;
    public KeyCode selectNextKey = KeyCode.X;
    public KeyCode moveSelectedToInventoryKey = KeyCode.R;
    public KeyCode dropSelectedKey = KeyCode.Q;

    private PlayerHand _hand;
    private PlayerInventoryOwner _owner;

    private void Awake() {
        _hand = GetComponent<PlayerHand>();
        _owner = GetComponent<PlayerInventoryOwner>();
    }

    private void Update() {
        if (_hand == null) {
            return;
        }

        bool inventoryOpen = InventoryUI.IsAnyInventoryOpen;

        if (InputKeyUtils.WasPressedThisFrame(selectPreviousKey)) {
            _hand.SelectPrevious();
        }

        if (InputKeyUtils.WasPressedThisFrame(selectNextKey)) {
            _hand.SelectNext();
        }

        for (int i = 0; i < _hand.maxSlots && i < 9; i++) {
            if (InputKeyUtils.WasPressedThisFrame((KeyCode)((int)KeyCode.Alpha1 + i))) {
                _hand.SelectSlot(i);
            }
        }

        if (InputKeyUtils.WasPressedThisFrame(moveSelectedToInventoryKey) && _owner != null) {
            _hand.TryMoveSelectedToInventory(_owner.Inventory);
        }

        if (InputKeyUtils.WasPressedThisFrame(dropSelectedKey) && !inventoryOpen) {
            _hand.DropSelected();
        }
    }

    private static bool IsPointerOverInventorySlot() {
        if (EventSystem.current == null || Mouse.current == null) {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current) {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        for (int i = 0; i < results.Count; i++) {
            if (results[i].gameObject.GetComponentInParent<InventorySlotUI>() != null) {
                return true;
            }
        }

        return false;
    }
}

