#nullable enable

using System;
using System.Collections.Generic;
using Interactions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Interaction Menu UI")]
[DisallowMultipleComponent]
public sealed class InteractionMenuUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Canvas? targetCanvas;
    [SerializeField] private GraphicRaycaster? targetGraphicRaycaster;
    [SerializeField] private CanvasGroup? targetCanvasGroup;

    [Header("Buttons")]
    [SerializeField] private Transform? buttonRoot;
    [SerializeField] private Button buttonPrefab = null!;
    [Min(0f)] [SerializeField] private float verticalSpacing = 8f;

    [Header("State")]
    [SerializeField] private bool closeOnStart = true;

    private readonly List<GameObject> createdButtons = new();

    public event Action? InteractionSelected;
    public bool IsVisible
    {
        get
        {
            bool canvasVisible = targetCanvas == null || targetCanvas.enabled;
            bool groupVisible = targetCanvasGroup == null || targetCanvasGroup.alpha > 0.001f;
            return isActiveAndEnabled && canvasVisible && groupVisible;
        }
    }

    private void Reset()
    {
        targetCanvas = GetComponent<Canvas>();
        targetGraphicRaycaster = GetComponent<GraphicRaycaster>();
        targetCanvasGroup = GetComponent<CanvasGroup>();
        buttonRoot = transform;
    }

    private void Awake()
    {
        ResolveReferences(true);
        if (closeOnStart)
            Hide();
    }

    public void Show(IList<Interaction> interactions)
    {
        ResolveReferences(true);
        ClearButtons();

        if (buttonPrefab == null)
        {
            Debug.LogWarning($"{nameof(InteractionMenuUI)} on {name} has no button prefab.", this);
            SetOpen(true);
            return;
        }

        Transform root = buttonRoot != null ? buttonRoot : transform;
        for (int i = 0; i < interactions.Count; i++)
        {
            Interaction interaction = interactions[i];
            Button button = Instantiate(buttonPrefab, root);
            createdButtons.Add(button.gameObject);
            PositionButton(button, i);
            SetButtonText(button, interaction.Text);
            ConfigureButton(button, interaction);
        }

        SetOpen(true);
    }

    public void Hide()
    {
        ResolveReferences(true);
        SetOpen(false);
    }

    private void ConfigureButton(Button button, Interaction interaction)
    {
        button.onClick.RemoveAllListeners();

        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();
        AddClick(trigger, eventData => HandleClick(button, interaction, eventData));
    }

    private void HandleClick(Button button, Interaction interaction, BaseEventData eventData)
    {
        if (!button.IsInteractable() || eventData is not PointerEventData pointerEventData)
            return;

        InteractionType interactionType = pointerEventData.button switch
        {
            PointerEventData.InputButton.Left => InteractionType.LeftHand,
            PointerEventData.InputButton.Right => InteractionType.RightHand,
            _ => InteractionType.Other
        };

        if (interactionType == InteractionType.Other)
            return;

        try
        {
            interaction.Callback(interactionType);
        }
        finally
        {
            InteractionSelected?.Invoke();
        }
    }

    private void SetOpen(bool open)
    {
        if (targetCanvas != null)
            targetCanvas.enabled = open;

        if (targetGraphicRaycaster != null)
            targetGraphicRaycaster.enabled = open;

        if (targetCanvasGroup != null)
        {
            targetCanvasGroup.alpha = open ? 1f : 0f;
            targetCanvasGroup.interactable = open;
            targetCanvasGroup.blocksRaycasts = open;
        }
    }

    private void ClearButtons()
    {
        for (int i = 0; i < createdButtons.Count; i++)
        {
            if (createdButtons[i] != null)
                Destroy(createdButtons[i]);
        }

        createdButtons.Clear();
    }

    private void PositionButton(Button button, int index)
    {
        RectTransform? rectTransform = button.transform as RectTransform;
        if (rectTransform == null)
            return;

        float height = Mathf.Max(rectTransform.rect.height, LayoutUtility.GetPreferredHeight(rectTransform));
        if (height <= 0f)
            height = rectTransform.sizeDelta.y;

        rectTransform.anchoredPosition -= new Vector2(0f, index * (height + verticalSpacing));
    }

    private void ResolveReferences(bool addMissingCanvasGroup)
    {
        if (targetCanvas == null)
            targetCanvas = GetComponent<Canvas>();

        if (targetGraphicRaycaster == null)
            targetGraphicRaycaster = GetComponent<GraphicRaycaster>();

        if (targetCanvasGroup == null)
            targetCanvasGroup = GetComponent<CanvasGroup>();

        if (targetCanvasGroup == null && addMissingCanvasGroup)
            targetCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (buttonRoot == null)
            buttonRoot = transform;
    }

    private static void AddClick(EventTrigger trigger, Action<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new() { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener(eventData => callback(eventData));
        trigger.triggers.Add(entry);
    }

    private static void SetButtonText(Button button, string text)
    {
        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = text;
            return;
        }

        Text uiText = button.GetComponentInChildren<Text>(true);
        if (uiText != null)
            uiText.text = text;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences(false);
    }
#endif
}
