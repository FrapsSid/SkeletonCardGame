#nullable enable

using System;
using System.Collections.Generic;
using Interactions;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInventoryOwner))]
public class PlayerInteractor : MonoBehaviour
{
    public KeyCode interactionKey = KeyCode.E;
    [Min(0f)] public float interactionRange = 2.5f;
    public LayerMask interactionLayerMask = ~0;

    private readonly Collider[] _overlapResults = new Collider[32];
    private PlayerInventoryOwner _inventoryOwner = null!;
    private CameraController? _cameraController;
    private UIStateController? _uiStateController;
    private List<Interaction> _interactions = new();

    public IReadOnlyList<Interaction> Interactions => _interactions.AsReadOnly();
    public bool HasInteractions => _interactions.Count > 0;

    private void Awake()
    {
        _inventoryOwner = GetComponent<PlayerInventoryOwner>();
        _cameraController = GetComponent<CameraController>();
        _uiStateController = FindAnyObjectByType<UIStateController>();
    }

    private void Update()
    {
        Skeleton? skeleton = _inventoryOwner.OwnerSkeleton;
        _interactions = skeleton == null
            ? new List<Interaction>()
            : GatherInteractions(skeleton, _cameraController != null && _cameraController.IsFirstPerson);

        if (InputKeyUtils.WasPressedThisFrame(interactionKey))
        {
            OpenInteractions();
            return;
        }

        if (TryGetMouseButtonInteractionType(out InteractionType interactionType)
            && TryGetSingleMouseButtonInteraction(out Interaction interaction))
        {
            interaction.Callback(interactionType);
        }
    }

    public bool TryGetSingleInteraction(out Interaction interaction)
    {
        if (_interactions.Count != 1)
        {
            interaction = default!;
            return false;
        }

        interaction = _interactions[0];
        return true;
    }

    public bool ShouldOpenInteractionMenu()
    {
        return _interactions.Count > 1;
    }

    private List<Interaction> GatherInteractions(Skeleton player, bool firstPerson)
    {
        List<Interaction> interactions = new();
        if (firstPerson
            && TryGetFirstPersonInteractable(out IInteractable? firstPersonInteractable)
            && firstPersonInteractable != null)
        {
            interactions.AddRange(firstPersonInteractable.GetInteractions(player));
            return interactions;
        }

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            interactionRange,
            _overlapResults,
            interactionLayerMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider candidate = _overlapResults[i];
            if (candidate == null)
            {
                continue;
            }

            IInteractable interactable = candidate.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactions.AddRange(interactable.GetInteractions(player));
            }
        }

        Array.Clear(_overlapResults, 0, hitCount);
        return interactions;
    }

    private void OpenInteractions()
    {
        if (!HasInteractions)
        {
            return;
        }

        if (TryGetSingleInteraction(out Interaction interaction))
        {
            interaction.Callback(InteractionType.Other);
            return;
        }

        if (ShouldOpenInteractionMenu())
        {
            _uiStateController?.OpenInteractionMenu(_interactions);
        }
    }

    public bool TryGetSingleMouseButtonInteraction(out Interaction interaction)
    {
        interaction = default!;
        bool found = false;
        for (int i = 0; i < _interactions.Count; i++)
        {
            Interaction candidate = _interactions[i];
            if (!candidate.AllowMouseButtonInteraction)
            {
                continue;
            }

            if (found)
            {
                interaction = default!;
                return false;
            }

            interaction = candidate;
            found = true;
        }

        return found;
    }

    private static bool TryGetMouseButtonInteractionType(out InteractionType interactionType)
    {
        if (WasMouseButtonPressedThisFrame(0))
        {
            interactionType = InteractionType.LeftHand;
            return true;
        }

        if (WasMouseButtonPressedThisFrame(1))
        {
            interactionType = InteractionType.RightHand;
            return true;
        }

        interactionType = InteractionType.Other;
        return false;
    }

    private static bool WasMouseButtonPressedThisFrame(int button)
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            return button switch
            {
                0 => mouse.leftButton.wasPressedThisFrame,
                1 => mouse.rightButton.wasPressedThisFrame,
                _ => false
            };
        }
#endif

        try
        {
            return Input.GetMouseButtonDown(button);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private bool TryGetFirstPersonInteractable(out IInteractable? interactable)
    {
        interactable = null;
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return false;
        }

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayerMask, QueryTriggerInteraction.Collide))
        {
            return false;
        }

        interactable = hit.collider.GetComponentInParent<IInteractable>();
        return interactable != null;
    }
}
