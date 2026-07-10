#nullable enable

using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHand : MonoBehaviour {
    [SerializeField] private Transform? heldItemAnchor;
    [SerializeField] private Vector3 heldItemLocalPosition = new Vector3(0.35f, -0.2f, 0.65f);
    [SerializeField] private Vector3 heldItemLocalEulerAngles = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 heldItemLocalScale = Vector3.one;

    private IItem? _item;
    private GameObject? _heldItemVisual;

    public IItem? Item => _item;
    public bool HasItem => _item != null;

    public bool ContainsHeldItemRenderer(Renderer renderer) {
        return _heldItemVisual != null
            && (renderer.transform == _heldItemVisual.transform || renderer.transform.IsChildOf(_heldItemVisual.transform));
    }

    private void Awake() {
        RefreshHeldItemVisual();
    }

    private void OnDestroy() {
        ClearHeldItemVisual();
    }

    private static readonly int HasItemHash = Animator.StringToHash("HasItem");
    private static readonly int PickupHash = Animator.StringToHash("Pickup");

    public void SetItem(IItem? item) {
        _item = item;
        RefreshHeldItemVisual();

        var body = GetComponentInParent<SkeletonBody>();
        if (body == null) return;
        var animator = body.GetComponentInChildren<Animator>();
        if (animator == null) return;

        bool hasItem = item != null;
        animator.SetBool(HasItemHash, hasItem);
        if (hasItem)
            animator.SetTrigger(PickupHash);
    }

    private void RefreshHeldItemVisual() {
        ClearHeldItemVisual();

        if (_item == null) {
            return;
        }

        _heldItemVisual = _item.CreateHeldObject();
        if (_heldItemVisual == null) {
            return;
        }

        Transform anchor = GetHeldItemAnchor();
        _heldItemVisual.transform.SetParent(anchor, false);
        _heldItemVisual.transform.localPosition = Vector3.zero;
        _heldItemVisual.transform.localRotation = Quaternion.identity;
        _heldItemVisual.transform.localScale = heldItemLocalScale;
        DisableHeldPhysics(_heldItemVisual);
    }

    private Transform GetHeldItemAnchor() {
        if (heldItemAnchor != null) {
            return heldItemAnchor;
        }

        Transform existing = transform.Find("HeldItemAnchor");
        if (existing != null) {
            heldItemAnchor = existing;
            return heldItemAnchor;
        }

        GameObject anchorObject = new GameObject("HeldItemAnchor");
        heldItemAnchor = anchorObject.transform;
        heldItemAnchor.SetParent(transform, false);
        heldItemAnchor.localPosition = heldItemLocalPosition;
        heldItemAnchor.localRotation = Quaternion.Euler(heldItemLocalEulerAngles);
        heldItemAnchor.localScale = Vector3.one;
        return heldItemAnchor;
    }

    private void ClearHeldItemVisual() {
        if (_heldItemVisual == null) {
            return;
        }

        if (Application.isPlaying) {
            Destroy(_heldItemVisual);
        }
        else {
            DestroyImmediate(_heldItemVisual);
        }

        _heldItemVisual = null;
    }

    private static void DisableHeldPhysics(GameObject heldObject) {
        Pickupable[] pickups = heldObject.GetComponentsInChildren<Pickupable>();
        for (int i = 0; i < pickups.Length; i++) {
            pickups[i].enabled = false;
        }

        Rigidbody[] bodies = heldObject.GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < bodies.Length; i++) {
            bodies[i].isKinematic = true;
            bodies[i].useGravity = false;
            bodies[i].linearVelocity = Vector3.zero;
            bodies[i].angularVelocity = Vector3.zero;
        }

        Collider[] colliders = heldObject.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++) {
            colliders[i].enabled = false;
        }
    }
}
