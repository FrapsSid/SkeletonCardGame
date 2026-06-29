using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Pickupable : MonoBehaviour {
    [Header("Item")] public ItemData itemData;
    public CardData cardData;
    [Min(1)] public int quantity = 1;
    public bool isPickupable = true;
    [Min(0f)] public float interactionRadius = 2f;

    [Header("Focus Visuals")] public bool tintRenderersOnFocus = true;
    public bool addTriggerColliderIfMissing = true;
    public Color focusTint = new Color(1f, 0.9f, 0.35f, 1f);

    public event Action<PlayerInventoryOwner> OnPickedUp;
    public event Action<PlayerInventoryOwner, string> OnPickupFailed;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propertyBlock;
    private bool _isFocused;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Reset() {
        EnsureDiscoveryCollider();
    }

    private void Awake() {
        EnsureDiscoveryCollider();
        CacheRenderers();
        SetFocused(false, KeyCode.E);
    }

    private void OnValidate() {
        quantity = Mathf.Max(1, quantity);
        interactionRadius = Mathf.Max(0f, interactionRadius);
    }

    public bool Pickup(PlayerInventoryOwner player) {
        if (!player) {
            return FailPickup(null, "Player is null");
        }

        if (!isPickupable) {
            return FailPickup(player, "Item is not pickupable");
        }

        if (!itemData && cardData == null) {
            return FailPickup(player, "Item data is null");
        }

        if (!IsPlayerInRange(player)) {
            return FailPickup(player, "Player is too far");
        }

        if (!player.TryPickup(this)) {
            return FailPickup(player, "No room in hand or inventory");
        }

        OnPickedUp?.Invoke(player);
        Destroy(gameObject);
        return true;
    }

    public PickupDropVisual CaptureDropVisual() {
        var prefab = itemData != null && itemData.dropPrefab ? itemData.dropPrefab : null;
        return new PickupDropVisual(prefab, transform.localScale, focusTint, tintRenderersOnFocus);
    }

    public void SetPickupable(bool state) {
        isPickupable = state;

        if (!isPickupable) {
            SetFocused(false, KeyCode.E);
        }
    }

    public bool IsPlayerInRange(PlayerInventoryOwner player) {
        return player && Vector3.Distance(transform.position, player.transform.position) <= interactionRadius;
    }

    public void SetFocused(bool focused, KeyCode pickupKey) {
        _isFocused = focused && isPickupable;
        ApplyFocusTint(_isFocused);
    }

    private bool FailPickup(PlayerInventoryOwner player, string reason) {
        OnPickupFailed?.Invoke(player, reason);
        return false;
    }

    private void CacheRenderers() {
        _renderers = GetComponentsInChildren<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();
    }

    private void ApplyFocusTint(bool focused) {
        if (!tintRenderersOnFocus || _renderers == null) {
            return;
        }

        foreach (Renderer itemRenderer in _renderers) {
            if (!itemRenderer) {
                continue;
            }

            itemRenderer.GetPropertyBlock(_propertyBlock);
            if (focused) {
                _propertyBlock.SetColor(BaseColorId, focusTint);
                _propertyBlock.SetColor(ColorId, focusTint);
            }
            else {
                _propertyBlock.Clear();
            }

            itemRenderer.SetPropertyBlock(_propertyBlock);
        }
    }

    private void EnsureDiscoveryCollider() {
        if (!addTriggerColliderIfMissing || GetComponentInChildren<Collider>() != null) {
            return;
        }

        SphereCollider discoveryCollider = gameObject.AddComponent<SphereCollider>();
        discoveryCollider.isTrigger = true;
        discoveryCollider.radius = Mathf.Max(0.1f, interactionRadius);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = isPickupable ? Color.yellow : Color.gray;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
