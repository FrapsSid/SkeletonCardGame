using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInventoryOwner))]
public class PlayerInteractor : MonoBehaviour {
    [Min(0f)] public float interactionRange = 2.5f;
    public KeyCode pickupKey = KeyCode.E;
    public LayerMask pickupLayerMask = ~0;

    public event Action<Pickupable> OnInteractionAvailable;
    public event Action OnInteractionUnavailable;

    public Pickupable CurrentTarget => _currentTarget;

    private PlayerInventoryOwner _player;
    private SkeletonBody _skeletonBody;
    private Pickupable _currentTarget;
    private readonly Collider[] _overlapResults = new Collider[32];

    private void Awake() {
        _player = GetComponent<PlayerInventoryOwner>();
        _skeletonBody = GetComponent<SkeletonBody>();
    }

    private void Update() {
        if (!CanInteract()) {
            SetCurrentTarget(null);
            return;
        }
        UpdateCurrentTarget();

        if (InputKeyUtils.WasPressedThisFrame(pickupKey)) {
            TryPickup();
        }
    }

    public Pickupable FindNearestPickupable() {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            interactionRange,
            _overlapResults,
            pickupLayerMask,
            QueryTriggerInteraction.Collide);

        Pickupable nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < hitCount; i++) {
            Collider candidateCollider = _overlapResults[i];
            if (candidateCollider == null) {
                continue;
            }

            Pickupable candidate = candidateCollider.GetComponentInParent<Pickupable>();
            if (candidate == null || !candidate.isPickupable || !candidate.IsPlayerInRange(_player)) {
                continue;
            }

            float distanceSqr = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr) {
                nearest = candidate;
                nearestDistanceSqr = distanceSqr;
            }
        }

        Array.Clear(_overlapResults, 0, hitCount);
        return nearest;
    }

    public bool TryPickup() {
        Pickupable target = FindNearestPickupable();
        if (target == null) {
            return false;
        }

        bool wasPickedUp = target.Pickup(_player);
        if (wasPickedUp) {
            SetCurrentTarget(null);
        }

        return wasPickedUp;
    }

    private void UpdateCurrentTarget() {
        SetCurrentTarget(FindNearestPickupable());
    }

    private void SetCurrentTarget(Pickupable target) {
        if (_currentTarget == target) {
            if (_currentTarget != null) {
                _currentTarget.SetFocused(true, pickupKey);
            }

            return;
        }

        if (_currentTarget != null) {
            _currentTarget.SetFocused(false, pickupKey);
        }

        _currentTarget = target;

        if (_currentTarget != null) {
            _currentTarget.SetFocused(true, pickupKey);
            OnInteractionAvailable?.Invoke(_currentTarget);
        }
        else {
            OnInteractionUnavailable?.Invoke();
        }
    }

    private void OnEnable() {
        if (_skeletonBody != null) {
            _skeletonBody.OnBodyChanged += HandleBodyChanged;
        }
    }
    private void OnDisable() {
        if (_skeletonBody != null) {
            _skeletonBody.OnBodyChanged -= HandleBodyChanged;
        }
        SetCurrentTarget(null);
    }
    private void HandleBodyChanged() {
        if (!CanInteract()) {
            SetCurrentTarget(null);
        }
    }
    private bool CanInteract() {
        return _skeletonBody == null || _skeletonBody.GetArmCount() > 0;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}