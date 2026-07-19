#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerTableCardStacks : MonoBehaviour
{
    private static readonly Quaternion FaceDownRotation = Quaternion.Euler(0f, 180f, 0f);

    [SerializeField] private TablePositions? tablePositions;
    [SerializeField] private CardStack? cardStackPrefab;
    [SerializeField] private Transform? stackParent;

    private readonly Dictionary<Skeleton, CardStack> stacksByPlayer = new();

    public TablePositions? TablePositions => tablePositions;
    public CardStack? CardStackPrefab => cardStackPrefab;

    private void Reset()
    {
        tablePositions = GetComponent<TablePositions>();
    }

    public void SetPlayers(IReadOnlyList<Skeleton> players)
    {
        RequireTablePositions().SetPlayers(players);
    }

    public void AddCard(Skeleton player, CardData card)
    {
        if (card == null)
        {
            throw new ArgumentNullException(nameof(card));
        }

        CardStack stack = GetOrCreateStack(player);
        stack.AddCard(card);
    }

    public void PlaceStack(Skeleton player, IReadOnlyList<CardData> cards)
    {
        AddCards(player, cards);
    }

    public bool HasCards(Skeleton player)
    {
        return TryGetStack(player, out CardStack? stack)
            && stack != null
            && stack.Cards.Count > 0;
    }

    public bool TryTakeCards(Skeleton player, out List<CardData> cards)
    {
        cards = new List<CardData>();
        if (!TryGetStack(player, out CardStack? stack) || stack == null)
        {
            return false;
        }

        cards = stack.GetCards();
        if (cards.Count == 0)
        {
            return false;
        }

        RemoveStack(player);
        return true;
    }

    public void AddCards(Skeleton player, IReadOnlyList<CardData> cards)
    {
        if (cards == null)
        {
            throw new ArgumentNullException(nameof(cards));
        }

        CardStack stack = GetOrCreateStack(player);
        List<CardData> stackCards = stack.GetCards();
        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];
            if (card == null)
            {
                throw new ArgumentException("Card list cannot contain null cards.", nameof(cards));
            }

            stackCards.Add(card);
        }

        stack.SetCards(stackCards);
    }

    public void RegisterStack(Skeleton player, CardStack stack)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (stack == null)
        {
            throw new ArgumentNullException(nameof(stack));
        }

        stacksByPlayer[player] = stack;
        MoveStackToPlayerPosition(player, stack);
    }

    public bool TryGetStack(Skeleton player, out CardStack? stack)
    {
        if (player == null)
        {
            stack = null;
            return false;
        }

        if (!stacksByPlayer.TryGetValue(player, out stack))
        {
            return false;
        }

        if (stack != null)
        {
            return true;
        }

        stacksByPlayer.Remove(player);
        return false;
    }

    public CardStack GetStack(Skeleton player)
    {
        if (!TryGetStack(player, out CardStack? stack) || stack == null)
        {
            throw new InvalidOperationException("Player does not have a table card stack.");
        }

        return stack;
    }

    public CardStack? TakeStack(Skeleton player)
    {
        if (!TryGetStack(player, out CardStack? stack))
        {
            return null;
        }

        stacksByPlayer.Remove(player);
        return stack;
    }

    public bool RemoveStack(Skeleton player)
    {
        CardStack? stack = TakeStack(player);
        if (stack == null)
        {
            return false;
        }

        DestroyGeneratedObject(stack.gameObject);
        return true;
    }

    public void Clear()
    {
        foreach (CardStack stack in stacksByPlayer.Values)
        {
            if (stack != null)
            {
                DestroyGeneratedObject(stack.gameObject);
            }
        }

        stacksByPlayer.Clear();
    }

    private CardStack GetOrCreateStack(Skeleton player)
    {
        if (TryGetStack(player, out CardStack? stack) && stack != null)
        {
            return stack;
        }

        CardStack createdStack = CreateStack(player);
        stacksByPlayer[player] = createdStack;
        return createdStack;
    }

    private CardStack CreateStack(Skeleton player)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (cardStackPrefab == null)
        {
            throw new InvalidOperationException("PlayerTableCardStacks needs a CardStack prefab.");
        }

        Transform parent = stackParent != null ? stackParent : transform;
        CardStack stack = Instantiate(cardStackPrefab, parent);
        stack.SetOwner(player);
        stack.name = $"{cardStackPrefab.name} ({RequireTablePositions().GetPlayerIndex(player) + 1})";
        MoveStackToPlayerPosition(player, stack);
        return stack;
    }

    private void MoveStackToPlayerPosition(Skeleton player, CardStack stack)
    {
        Transform position = RequireTablePositions().GetPlayerDealCardPosition(player);
        stack.transform.SetPositionAndRotation(position.position, position.rotation * FaceDownRotation);
    }

    private TablePositions RequireTablePositions()
    {
        if (tablePositions == null)
        {
            throw new InvalidOperationException("PlayerTableCardStacks needs TablePositions.");
        }

        return tablePositions;
    }

    private static void DestroyGeneratedObject(UnityEngine.Object? target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
