#nullable enable

using System.Collections.Generic;
using System.Text;
using Interactions;
using TMPro;
using UnityEngine;

[AddComponentMenu("UI/Interaction Hint UI")]
[DisallowMultipleComponent]
public sealed class InteractionHintUI : MonoBehaviour
{
    private const string DefaultInteractionText = "Взаимодействовать";
    private const string GenericInteractionText = "взаимодействовать";
    private const string MouseButtonsText = "ЛКМ/ПКМ";

    [SerializeField] private TMP_Text hintText = null!;
    [SerializeField] private GameManager? gameManager;

    private readonly StringBuilder stringBuilder = new();

    private void Awake()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        ResolveReferences();

        if (hintText == null)
            return;

        hintText.text = BuildHint();
    }

    private string BuildHint()
    {
        PlayerInteractor? playerInteractor = GetPlayerInteractor();
        if (playerInteractor == null || !playerInteractor.HasInteractions)
            return string.Empty;

        stringBuilder.Clear();
        AppendKeyboardHint(playerInteractor);

        if (playerInteractor.TryGetSingleMouseButtonInteraction(out Interaction mouseInteraction))
        {
            if (stringBuilder.Length > 0)
                stringBuilder.AppendLine();

            AppendHintLine(MouseButtonsText, mouseInteraction.Source, mouseInteraction.Text);
        }

        return stringBuilder.ToString();
    }

    private void AppendKeyboardHint(PlayerInteractor playerInteractor)
    {
        string keyText = playerInteractor.interactionKey.ToString();
        if (playerInteractor.TryGetSingleInteraction(out Interaction interaction))
        {
            AppendHintLine(keyText, interaction.Source, interaction.Text);
            return;
        }

        if (!playerInteractor.ShouldOpenInteractionMenu())
            return;

        IReadOnlyList<Interaction> interactions = playerInteractor.Interactions;
        string? source = TryGetSharedSource(interactions, out string sharedSource) ? sharedSource : null;
        string action = source == null ? GenericInteractionText : DefaultInteractionText;
        AppendHintLine(keyText, source, action);
    }

    private void AppendHintLine(string buttons, string? source, string action)
    {
        stringBuilder.Append(buttons);
        stringBuilder.Append(" - ");

        if (!string.IsNullOrWhiteSpace(source))
        {
            stringBuilder.Append(source);
            stringBuilder.Append(": ");
        }

        stringBuilder.Append(action);
    }

    private void ResolveReferences()
    {
        if (hintText == null)
            hintText = GetComponent<TMP_Text>();
    }

    private PlayerInteractor? GetPlayerInteractor()
    {
        PlayerInventoryOwner? inventoryOwner = gameManager != null && gameManager.LocalPlayer != null
            ? gameManager.LocalPlayer.InventoryOwner
            : null;

        return inventoryOwner != null ? inventoryOwner.GetComponent<PlayerInteractor>() : null;
    }

    private static bool TryGetSharedSource(IReadOnlyList<Interaction> interactions, out string source)
    {
        source = string.Empty;
        if (interactions.Count == 0 || string.IsNullOrWhiteSpace(interactions[0].Source))
            return false;

        source = interactions[0].Source;
        for (int i = 1; i < interactions.Count; i++)
        {
            if (interactions[i].Source != source)
                return false;
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif
}
