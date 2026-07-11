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
    private readonly RaycastHit[] _raycastResults = new RaycastHit[32];
    private PlayerInventoryOwner _inventoryOwner = null!;
    private CameraController? _cameraController;
    private UIStateController? _uiStateController;
    private List<Interaction> _interactions = new();
    private readonly Dictionary<GameObject, IList<Interaction>> _interactionsByObject = new();

    public IReadOnlyList<Interaction> Interactions => _interactions.AsReadOnly();
    public IReadOnlyDictionary<GameObject, IList<Interaction>> InteractionsByObject => _interactionsByObject;
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
        _interactions.Clear();
        _interactionsByObject.Clear();

        if (skeleton == null) return;

        // Ghosts can only interact with card stacks to reveal them
        if (skeleton.IsGhost)
        {
            GatherGhostInteractions(skeleton);
            if (InputKeyUtils.WasPressedThisFrame(interactionKey))
                OpenInteractions();
            return;
        }

        GatherInteractions(skeleton, _cameraController != null && _cameraController.IsFirstPerson);

        if (InputKeyUtils.WasPressedThisFrame(interactionKey))
        {
            OpenInteractions();
            return;
        }

        if (!IsAnyUiOpen()
            && TryGetMouseButtonInteractionType(out InteractionType interactionType))
        {
            if (TryGetSingleMouseButtonInteraction(out Interaction interaction))
            {
                interaction.Callback(interactionType);
                return;
            }

            if (!HasMouseButtonInteractions())
            {
                DropSelectedHandItem(interactionType);
            }
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

    private void GatherInteractions(Skeleton player, bool firstPerson)
    {
        if (firstPerson && player.Body != null && player.Body.HasSkull())
        {
            if (TryGetFirstPersonInteractable(out IInteractable? firstPersonInteractable)
                && firstPersonInteractable != null)
            {
                AddInteractions(player, firstPersonInteractable);
            }

            return;
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
                AddInteractions(player, interactable);
            }
        }

        Array.Clear(_overlapResults, 0, hitCount);
    }

    private void GatherGhostInteractions(Skeleton ghost)
    {
        var ghostCtrl = ghost.Body?.GetComponent<GhostController>();
        if (ghostCtrl == null || ghostCtrl.HasUsedReveal) return;

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            interactionRange,
            _overlapResults,
            interactionLayerMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider candidate = _overlapResults[i];
            if (candidate == null) continue;

            CardStack stack = candidate.GetComponentInParent<CardStack>();
            if (stack == null) continue;

            _interactions.Add(new Interaction(
                "Card Stack",
                "Reveal Permanently",
                _ => ghostCtrl.TryRevealCardStack(stack),
                false));
        }

        Array.Clear(_overlapResults, 0, hitCount);
    }

    private void AddInteractions(Skeleton player, IInteractable interactable)
    {
        IList<Interaction> interactions = interactable.GetInteractions(player);
        if (interactions.Count == 0)
        {
            return;
        }

        _interactions.AddRange(interactions);
        if (!TryGetInteractableGameObject(interactable, out GameObject sourceObject))
        {
            return;
        }

        if (!_interactionsByObject.TryGetValue(sourceObject, out IList<Interaction> objectInteractions))
        {
            objectInteractions = new List<Interaction>();
            _interactionsByObject.Add(sourceObject, objectInteractions);
        }

        for (int i = 0; i < interactions.Count; i++)
        {
            objectInteractions.Add(interactions[i]);
        }
    }

    private static bool TryGetInteractableGameObject(IInteractable interactable, out GameObject sourceObject)
    {
        if (interactable is Component component && component != null)
        {
            sourceObject = component.gameObject;
            return true;
        }

        sourceObject = null!;
        return false;
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

    private bool HasMouseButtonInteractions()
    {
        for (int i = 0; i < _interactions.Count; i++)
        {
            if (_interactions[i].AllowMouseButtonInteraction)
            {
                return true;
            }
        }

        return false;
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

    private bool IsAnyUiOpen()
    {
        return _uiStateController != null && _uiStateController.AnyUiOpen;
    }

    private void DropSelectedHandItem(InteractionType interactionType)
    {
        PlayerHand? hand = interactionType switch
        {
            InteractionType.LeftHand => _inventoryOwner.leftHand,
            InteractionType.RightHand => _inventoryOwner.rightHand,
            _ => null
        };

        IItem? item = hand?.Item;
        if (hand == null || item == null || item is CardsItem)
        {
            return;
        }

        ItemUtils.DropItem(item, hand.transform.position, hand.transform.rotation);
        hand.SetItem(null);
    }

    private bool TryGetFirstPersonInteractable(out IInteractable? interactable)
    {
        interactable = null;
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return false;
        }

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int hitCount = Physics.RaycastNonAlloc(
            ray,
            _raycastResults,
            interactionRange,
            interactionLayerMask,
            QueryTriggerInteraction.Collide);

        IInteractable? closestInteractable = null;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _raycastResults[i];
            if (hit.collider == null || IsOwnCollider(hit.collider))
            {
                continue;
            }

            IInteractable hitInteractable = hit.collider.GetComponentInParent<IInteractable>();
            if (hitInteractable != null && hit.distance < closestDistance)
            {
                closestInteractable = hitInteractable;
                closestDistance = hit.distance;
            }
        }

        Array.Clear(_raycastResults, 0, hitCount);
        interactable = closestInteractable;
        return interactable != null;
    }

    private bool IsOwnCollider(Collider candidate)
    {
        return candidate.transform == transform || candidate.transform.IsChildOf(transform);
    }
}
