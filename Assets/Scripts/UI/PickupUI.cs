using TMPro;
using UnityEngine;

public class PickupUI : MonoBehaviour {
    [Header("Source")] public PlayerInteractor interactor;

    [Header("UI")] public GameObject hintRoot;
    public CanvasGroup hintCanvasGroup;
    public TMP_Text hintText;
    public string hintFormat = "{0}\n[{1}] Pick up";
    public string fallbackItemName = "Item";

    private PlayerInteractor _subscribedInteractor;
    private Pickupable _target;

    private void Awake() {
        if (hintRoot == null) {
            hintRoot = gameObject;
        }

        if (hintCanvasGroup == null && hintRoot != null) {
            hintCanvasGroup = hintRoot.GetComponent<CanvasGroup>();
        }

        TryBindInteractor();
        HideHint();
    }

    private void OnEnable() {
        SubscribeToInteractor(interactor);
        RefreshFromInteractor();
    }

    private void OnDisable() {
        SubscribeToInteractor(null);
        HideHint();
    }

    private void Update() {
        if (interactor == null && TryBindInteractor()) {
            SubscribeToInteractor(interactor);
        }

        if (_target != null) {
            UpdateHintText(_target);
        }
    }

    private bool TryBindInteractor() {
        if (interactor != null) {
            return true;
        }

        interactor = FindFirstObjectByType<PlayerInteractor>();
        return interactor != null;
    }

    private void SubscribeToInteractor(PlayerInteractor target) {
        if (_subscribedInteractor == target) {
            return;
        }

        if (_subscribedInteractor != null) {
            _subscribedInteractor.OnInteractionAvailable -= ShowHint;
            _subscribedInteractor.OnInteractionUnavailable -= HideHint;
        }

        _subscribedInteractor = target;

        if (_subscribedInteractor != null && isActiveAndEnabled) {
            _subscribedInteractor.OnInteractionAvailable += ShowHint;
            _subscribedInteractor.OnInteractionUnavailable += HideHint;
        }
    }

    private void RefreshFromInteractor() {
        if (interactor != null && interactor.CurrentTarget != null) {
            ShowHint(interactor.CurrentTarget);
        }
        else {
            HideHint();
        }
    }

    private void ShowHint(Pickupable target) {
        _target = target;
        
        UpdateHintText(target);
        SetHintVisible(target);
    }

    private void HideHint() {
        _target = null;
        SetHintVisible(false);
    }

    private void SetHintVisible(bool visible) {
        if (hintCanvasGroup) {
            hintCanvasGroup.alpha = visible ? 1f : 0f;
            hintCanvasGroup.interactable = false;
            hintCanvasGroup.blocksRaycasts = false;
        }

        if (hintRoot && hintRoot != gameObject && !hintCanvasGroup) {
            hintRoot.SetActive(visible);
        }

        if (hintText) {
            hintText.enabled = visible;
        }
    }

    private void UpdateHintText(Pickupable target) {
        if (!hintText || target == null) {
            return;
        }

        string itemName = target.itemData && !string.IsNullOrWhiteSpace(target.itemData.itemName)
            ? target.itemData.itemName
            : fallbackItemName;
        KeyCode pickupKey = interactor ? interactor.pickupKey : KeyCode.E;
        hintText.text = string.Format(hintFormat, itemName, pickupKey);
    }
}