using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHand))]
public class PlayerHandInput : MonoBehaviour {
    [SerializeField] private UIStateController uiStateController;

    public KeyCode selectPreviousKey = KeyCode.Z;
    public KeyCode selectNextKey = KeyCode.X;
    public KeyCode moveSelectedToInventoryKey = KeyCode.R;
    public KeyCode dropSelectedKey = KeyCode.Q;

    private PlayerHand _hand;
    private PlayerInventoryOwner _owner;

    private void Awake() {
        if (uiStateController == null) {
            uiStateController = FindFirstObjectByType<UIStateController>();
        }

        _hand = GetComponent<PlayerHand>();
        _owner = GetComponent<PlayerInventoryOwner>();
    }

    private void Update() {
        if (_hand == null) {
            return;
        }

        bool inventoryOpen = uiStateController.IsInventoryOpen;

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
}

