#nullable enable

using System.Collections.Generic;
using Interactions;
using UnityEngine;

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
        }
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

        System.Array.Clear(_overlapResults, 0, hitCount);
        return interactions;
    }

    private void OpenInteractions()
    {
        if (_interactions.Count == 0)
        {
            return;
        }

        if (_interactions.Count == 1)
        {
            _interactions[0].Callback(InteractionType.Other);
            return;
        }

        _uiStateController?.OpenInteractionMenu(_interactions);
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
