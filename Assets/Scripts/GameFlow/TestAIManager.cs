#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TestAIManager : MonoBehaviour
{
    [SerializeField]
    private GameManager? gameManager;

    [SerializeField]
    private List<int> controlledPlayerIndexes = new List<int>();

    [SerializeField]
    private float actionDelaySeconds = 0.25f;

    private readonly List<Skeleton> controlledPlayers = new List<Skeleton>();
    private Coroutine? actionCoroutine;

    private void OnEnable()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        gameManager.OnGameStarted += HandleGameStarted;
        gameManager.OnTurnStarted += HandleTurnStarted;

        if (gameManager.CardGame != null)
            RefreshControlledPlayers();
    }

    private void OnDisable()
    {
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        if (gameManager == null)
            return;

        gameManager.OnGameStarted -= HandleGameStarted;
        gameManager.OnTurnStarted -= HandleTurnStarted;
    }

    private void HandleGameStarted(Assets.Scripts.CardGame.CardGame game)
    {
        RefreshControlledPlayers();
    }

    private void HandleTurnStarted(Skeleton player)
    {
        if (!controlledPlayers.Contains(player))
            return;

        if (actionCoroutine != null)
            StopCoroutine(actionCoroutine);

        actionCoroutine = StartCoroutine(PlayTurn(player));
    }

    private IEnumerator PlayTurn(Skeleton player)
    {
        if (actionDelaySeconds > 0f)
            yield return new WaitForSeconds(actionDelaySeconds);
        else
            yield return null;

        actionCoroutine = null;
        if (gameManager == null || gameManager.ActiveTurnPlayer != player)
            yield break;

        CardGameTurnAction action = PickRandomAction(player);
        DeclaredCombinationTier tier = PickRandomTier();

        switch (action)
        {
            case CardGameTurnAction.Raise:
                gameManager.TryPerformTurnAction(player, action, tier, gameManager.GetSuggestedRaiseStake(player));
                break;
            case CardGameTurnAction.Call:
            case CardGameTurnAction.AllIn:
            case CardGameTurnAction.TakeCard:
            case CardGameTurnAction.Fold:
                gameManager.TryPerformTurnAction(player, action, tier, 0);
                break;
        }

        if (gameManager.ActiveTurnPlayer != player)
            yield break;

        if (!gameManager.TryPerformTurnAction(player, CardGameTurnAction.EndTurn, tier, 0))
        {
            if (!gameManager.TryPerformTurnAction(player, CardGameTurnAction.Call, tier, 0))
                gameManager.TryPerformTurnAction(player, CardGameTurnAction.AllIn, tier, 0);

            if (gameManager.ActiveTurnPlayer == player)
                gameManager.TryPerformTurnAction(player, CardGameTurnAction.EndTurn, tier, 0);
        }
    }

    private void RefreshControlledPlayers()
    {
        controlledPlayers.Clear();
        if (gameManager == null)
            return;

        IReadOnlyList<Skeleton> players = gameManager.Players;
        if (controlledPlayerIndexes.Count == 0)
        {
            for (int i = 0; i < players.Count; i++)
            {
                Skeleton player = players[i];
                if (player != gameManager.CurrentPlayer)
                    controlledPlayers.Add(player);
            }

            return;
        }

        foreach (int index in controlledPlayerIndexes)
        {
            if (index >= 0 && index < players.Count)
                controlledPlayers.Add(players[index]);
        }
    }

    private CardGameTurnAction PickRandomAction(Skeleton player)
    {
        List<CardGameTurnAction> actions = gameManager != null
            ? gameManager.GetValidTurnActions(player)
            : new List<CardGameTurnAction>();

        actions.Remove(CardGameTurnAction.EndTurn);
        if (actions.Count == 0)
            return CardGameTurnAction.EndTurn;

        return actions[Random.Range(0, actions.Count)];
    }

    private static DeclaredCombinationTier PickRandomTier()
    {
        return (DeclaredCombinationTier)Random.Range(
            (int)DeclaredCombinationTier.Easy,
            (int)DeclaredCombinationTier.Hard + 1);
    }
}
