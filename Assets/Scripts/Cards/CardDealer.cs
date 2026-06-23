#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CardDealer : MonoBehaviour
{
    private static readonly Quaternion FaceDownRotation = Quaternion.Euler(0f, 180f, 0f);

    [Header("References")]
    [SerializeField] private Transform? dealStart;
    [SerializeField] private WorldCardView? cardPrefab;
    [SerializeField] private CardAtlas? cardAtlas;
    [SerializeField] private TablePositions? tablePositions;
    [SerializeField] private PlayerTableCardStacks? playerTableCardStacks;
    [SerializeField] private Transform? cardParent;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float cardMoveDuration = 0.35f;
    [SerializeField, Min(0f)] private float cardDealInterval = 0.08f;

    private readonly List<WorldCardView> _tableCards = new();
    private int _dealtTableCardCount;
    private bool _isDealing;

    public event Action<CardData>? OnTableCardDealt;
    public event Action<IReadOnlyList<CardData>>? OnTableCardsDealt;
    public event Action<Skeleton, CardData>? OnPlayerCardDealt;
    public event Action? OnDealCompleted;

    public IReadOnlyList<WorldCardView> TableCards => _tableCards;

    private void Reset()
    {
        tablePositions = FindFirstObjectByType<TablePositions>();
        playerTableCardStacks = FindFirstObjectByType<PlayerTableCardStacks>();
    }

    public void SetPlayers(IReadOnlyList<Skeleton> players)
    {
        if (players == null)
        {
            throw new ArgumentNullException(nameof(players));
        }

        RequireTablePositions().SetPlayers(players);
        if (playerTableCardStacks != null)
        {
            playerTableCardStacks.SetPlayers(players);
        }
    }

    public Coroutine DealCardsToTable(IReadOnlyList<CardData> cards)
    {
        if (cards == null)
        {
            throw new ArgumentNullException(nameof(cards));
        }

        return StartCoroutine(DealCardsToTableRoutine(cards));
    }

    public Coroutine DealCardToTable(CardData card)
    {
        if (card == null)
        {
            throw new ArgumentNullException(nameof(card));
        }

        return DealCardsToTable(new[] { card });
    }

    public Coroutine DealCardsToPlayers(IReadOnlyList<Skeleton> players, int cardsPerPlayer)
    {
        if (players == null)
        {
            throw new ArgumentNullException(nameof(players));
        }

        if (cardsPerPlayer < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cardsPerPlayer), cardsPerPlayer, "Cards per player cannot be negative.");
        }

        return StartCoroutine(DealCardsToPlayersRoutine(players, cardsPerPlayer));
    }

    public Coroutine DealCardToPlayer(Skeleton player, CardData card)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (card == null)
        {
            throw new ArgumentNullException(nameof(card));
        }

        return StartCoroutine(DealCardToPlayerRoutine(player, card, true));
    }

    public void ClearTable()
    {
        for (int i = _tableCards.Count - 1; i >= 0; i--)
        {
            WorldCardView card = _tableCards[i];
            if (card != null)
            {
                DestroyGeneratedObject(card.gameObject);
            }
        }

        _tableCards.Clear();
        _dealtTableCardCount = 0;
    }

    public void ClearPlayerCards()
    {
        playerTableCardStacks?.Clear();
    }

    private IEnumerator DealCardsToTableRoutine(IReadOnlyList<CardData> cards)
    {
        BeginDeal();
        List<CardData> dealtCards = new(cards.Count);
        try
        {
            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                if (card == null)
                {
                    throw new ArgumentException("Card list cannot contain null cards.", nameof(cards));
                }

                yield return DealCardToTableRoutine(card);
                dealtCards.Add(card);
                yield return WaitBetweenCards(i, cards.Count);
            }
        }
        finally
        {
            EndDeal();
        }

        OnTableCardsDealt?.Invoke(dealtCards);
        OnDealCompleted?.Invoke();
    }

    private IEnumerator DealCardToTableRoutine(CardData card)
    {
        Transform destination = RequireTablePositions().GetTableCardPosition(_dealtTableCardCount);
        _dealtTableCardCount++;

        WorldCardView movingCard = CreateCard(card, false);
        yield return MoveCard(movingCard.transform, destination.position, destination.rotation);

        _tableCards.Add(movingCard);
        OnTableCardDealt?.Invoke(card);
    }

    private IEnumerator DealCardsToPlayersRoutine(IReadOnlyList<Skeleton> players, int cardsPerPlayer)
    {
        BeginDeal();
        try
        {
            for (int cardIndex = 0; cardIndex < cardsPerPlayer; cardIndex++)
            {
                for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
                {
                    Skeleton player = players[playerIndex];
                    if (player == null)
                    {
                        throw new ArgumentException("Player list cannot contain null players.", nameof(players));
                    }

                    List<CardData> handCards = player.Hand.GetCards();
                    if (cardIndex >= handCards.Count)
                    {
                        throw new InvalidOperationException("Player does not have enough cards in hand to deal.");
                    }

                    yield return DealCardToPlayerRoutine(player, handCards[cardIndex], false);
                    bool isLastCard = cardIndex == cardsPerPlayer - 1 && playerIndex == players.Count - 1;
                    if (!isLastCard)
                    {
                        yield return WaitForSeconds(cardDealInterval);
                    }
                }
            }

        }
        finally
        {
            EndDeal();
        }

        OnDealCompleted?.Invoke();
    }

    private IEnumerator DealCardToPlayerRoutine(Skeleton player, CardData card, bool raiseBatchCompleted)
    {
        if (raiseBatchCompleted)
        {
            BeginDeal();
        }

        bool completed = false;
        bool raiseCompletedEvent = false;
        WorldCardView? movingCard = null;
        try
        {
            Transform destination = RequireTablePositions().GetPlayerDealCardPosition(player);
            movingCard = CreateCard(card, true);
            yield return MoveCard(movingCard.transform, destination.position, destination.rotation * FaceDownRotation);

            DestroyGeneratedObject(movingCard.gameObject);
            PlayerTableCardStacks stacks = RequirePlayerTableCardStacks();
            stacks.AddCard(player, card);
            stacks.GetStack(player).transform.SetPositionAndRotation(destination.position, destination.rotation * FaceDownRotation);
            OnPlayerCardDealt?.Invoke(player, card);

            completed = true;
            raiseCompletedEvent = raiseBatchCompleted;
        }
        finally
        {
            if (!completed && movingCard != null)
            {
                DestroyGeneratedObject(movingCard.gameObject);
            }

            if (raiseBatchCompleted)
            {
                EndDeal();
            }
        }

        if (raiseCompletedEvent)
        {
            OnDealCompleted?.Invoke();
        }
    }

    private WorldCardView CreateCard(CardData card, bool faceDown)
    {
        if (card == null)
        {
            throw new ArgumentNullException(nameof(card));
        }

        Transform start = RequireDealStart();
        WorldCardView prefab = RequireCardPrefab();
        Transform parent = cardParent != null ? cardParent : transform;
        WorldCardView cardView = Instantiate(prefab, parent);
        cardView.transform.SetPositionAndRotation(start.position, GetCardRotation(start.rotation, faceDown));
        ApplyCard(cardView, card);
        return cardView;
    }

    private void ApplyCard(WorldCardView cardView, CardData card)
    {
        if (cardAtlas != null)
        {
            cardView.Initialize(cardAtlas, card);
            return;
        }

        cardView.SetCard(card);
    }

    private IEnumerator MoveCard(Transform card, Vector3 destinationPosition, Quaternion destinationRotation)
    {
        Vector3 startPosition = card.position;
        Quaternion startRotation = card.rotation;
        float duration = Mathf.Max(0f, cardMoveDuration);

        if (duration <= 0f)
        {
            card.SetPositionAndRotation(destinationPosition, destinationRotation);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            card.SetPositionAndRotation(
                Vector3.Lerp(startPosition, destinationPosition, t),
                Quaternion.Slerp(startRotation, destinationRotation, t));
            yield return null;
        }

        card.SetPositionAndRotation(destinationPosition, destinationRotation);
    }

    private IEnumerator WaitBetweenCards(int cardIndex, int cardCount)
    {
        if (cardIndex >= cardCount - 1)
        {
            yield break;
        }

        yield return WaitForSeconds(cardDealInterval);
    }

    private static WaitForSeconds WaitForSeconds(float seconds)
    {
        return new WaitForSeconds(Mathf.Max(0f, seconds));
    }

    private static Quaternion GetCardRotation(Quaternion baseRotation, bool faceDown)
    {
        return faceDown ? baseRotation * FaceDownRotation : baseRotation;
    }

    private void BeginDeal()
    {
        if (_isDealing)
        {
            throw new InvalidOperationException("CardDealer is already dealing cards.");
        }

        _isDealing = true;
    }

    private void EndDeal()
    {
        _isDealing = false;
    }

    private Transform RequireDealStart()
    {
        if (dealStart == null)
        {
            throw new InvalidOperationException("CardDealer needs a deal start transform.");
        }

        return dealStart;
    }

    private WorldCardView RequireCardPrefab()
    {
        if (cardPrefab == null)
        {
            throw new InvalidOperationException("CardDealer needs a WorldCardView prefab.");
        }

        return cardPrefab;
    }

    private TablePositions RequireTablePositions()
    {
        if (tablePositions == null)
        {
            throw new InvalidOperationException("CardDealer needs TablePositions.");
        }

        return tablePositions;
    }

    private PlayerTableCardStacks RequirePlayerTableCardStacks()
    {
        if (playerTableCardStacks == null)
        {
            throw new InvalidOperationException("CardDealer needs PlayerTableCardStacks.");
        }

        return playerTableCardStacks;
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
