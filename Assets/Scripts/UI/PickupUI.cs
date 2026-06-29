using TMPro;
using UnityEngine;

public class PickupUI : MonoBehaviour {
    [Header("Source")] public PlayerInteractor interactor;

    [Header("UI")] public GameObject pickupRoot;
    public CanvasGroup pickupCanvasGroup;
    public TMP_Text titleText;
    public TMP_Text pickupText;
    public string titleFormat = "{0}";
    public string pickupFormat = "[ {1} ] Pick up";
    public string baseItemName = "Item";

    private PlayerInteractor _subscribedInteractor;
    private Pickupable _target;

    private void Awake() {
        if (pickupRoot == null) {
            pickupRoot = gameObject;
        }

        if (pickupCanvasGroup == null && pickupRoot != null) {
            pickupCanvasGroup = pickupRoot.GetComponent<CanvasGroup>();
        }

        if (titleText == null && pickupRoot != null) {
            titleText = pickupRoot.GetComponentInChildren<TMP_Text>(true);
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
        if (pickupCanvasGroup) {
            pickupCanvasGroup.alpha = visible ? 1f : 0f;
            pickupCanvasGroup.interactable = false;
            pickupCanvasGroup.blocksRaycasts = false;
        }

        if (pickupRoot && pickupRoot != gameObject && !pickupCanvasGroup) {
            pickupRoot.SetActive(visible);
        }

        if (titleText) {
            titleText.enabled = visible;
            pickupText.enabled = visible;
        }
    }

    private void UpdateHintText(Pickupable target) {
        if (!titleText || !pickupText || !target) {
            return;
        }

        string itemName = GetTargetDisplayName(target);
        KeyCode pickupKey = interactor ? interactor.pickupKey : KeyCode.E;
        titleText.text = string.Format(titleFormat + "\n", itemName, pickupKey);
        pickupText.text = string.Format(pickupFormat, itemName, pickupKey);
    }

    private string GetTargetDisplayName(Pickupable target) {
        if (target == null) {
            return baseItemName;
        }

        if (target.cardData != null && (target.itemData == null || target.itemData.category == ItemCategory.Card)) {
            return $"{target.cardData.Value} of {target.cardData.Suit}";
        }

        if (target.itemData != null && !string.IsNullOrWhiteSpace(target.itemData.itemName)) {
            return target.itemData.itemName;
        }

        return baseItemName;
    }
}