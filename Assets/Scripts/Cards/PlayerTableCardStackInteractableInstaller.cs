#nullable enable

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerTableCardStackInteractableInstaller : MonoBehaviour
{
    [SerializeField] private PlayerTableCardStacks? playerTableCardStacks;
    [SerializeField] private TablePositions? tablePositions;
    [SerializeField] private GameManager? gameManager;

    private void Reset()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (gameManager != null)
        {
            gameManager.OnGameCreated += HandleGameCreated;
        }

        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnGameCreated -= HandleGameCreated;
        }
    }

    public void Refresh()
    {
        ResolveReferences();
        if (playerTableCardStacks == null || tablePositions == null || gameManager == null)
        {
            return;
        }

        IReadOnlyList<Skeleton> players = gameManager.Players;
        IReadOnlyList<Transform> positions = tablePositions.PlayerDealCardPositions;

        for (int i = 0; i < positions.Count; i++)
        {
            Transform position = positions[i];
            PlayerTableCardStackInteractable? interactable = position.GetComponent<PlayerTableCardStackInteractable>();
            if (i >= players.Count)
            {
                interactable?.ClearOwner();
                continue;
            }

            if (interactable == null)
            {
                interactable = position.gameObject.AddComponent<PlayerTableCardStackInteractable>();
            }

            interactable.Initialize(playerTableCardStacks, players[i]);
        }
    }

    private void ResolveReferences()
    {
        playerTableCardStacks ??= GetComponent<PlayerTableCardStacks>();
        playerTableCardStacks ??= FindFirstObjectByType<PlayerTableCardStacks>();
        tablePositions ??= playerTableCardStacks != null ? playerTableCardStacks.TablePositions : null;
        tablePositions ??= GetComponent<TablePositions>();
        tablePositions ??= FindFirstObjectByType<TablePositions>();
        gameManager ??= FindFirstObjectByType<GameManager>();
    }

    private void HandleGameCreated(CardGame game)
    {
        Refresh();
    }
}
