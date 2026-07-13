using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct CardNetworkData : INetworkSerializable
{
    public int Suit;
    public int Value;

    public CardNetworkData(CardData card)
    {
        Suit = (int)card.Suit;
        Value = (int)card.Value;
    }

    public CardData ToCardData()
    {
        return new CardData((CardSuit)Suit, (CardValue)Value);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Suit);
        serializer.SerializeValue(ref Value);
    }
}

public struct PlayerCardsData : INetworkSerializable
{
    public int PlayerIndex;
    public CardNetworkData[] Cards;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerIndex);
        serializer.SerializeValue(ref Cards);
    }
}

public class NetworkCardDealerSync : NetworkBehaviour
{
    [SerializeField] private CardDealer? cardDealer;

    private bool _initialDealSent;

    public override void OnNetworkSpawn()
    {
        ResolveCardDealer();
        _initialDealSent = false;

        if (!IsServer)
            return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
            return;

        if (gm.CardGame != null)
            HandleGameCreated(gm.CardGame);
        else
            gm.OnGameCreated += HandleGameCreated;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.OnGameCreated -= HandleGameCreated;
            gm.OnCardDealCompleted -= HandleInitialDealCompleted;
            if (gm.CardGame != null)
            {
                gm.CardGame.OnTableCardsDealt -= HandleTableCardsDealt;
                gm.CardGame.OnCardTaken -= HandleCardTaken;
            }
        }
    }

    private void HandleGameCreated(CardGame game)
    {
        game.OnTableCardsDealt += HandleTableCardsDealt;
        game.OnCardTaken += HandleCardTaken;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
            gm.OnCardDealCompleted += HandleInitialDealCompleted;

        _initialDealSent = false;
    }

    private void HandleInitialDealCompleted()
    {
        if (!IsServer)
            return;

        if (_initialDealSent)
            return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm?.CardGame == null)
            return;

        _initialDealSent = true;

        var allData = new List<PlayerCardsData>();
        var playersList = new List<Skeleton>(gm.Players);

        for (int i = 0; i < playersList.Count; i++)
        {
            var cards = playersList[i].Hand.GetCards();
            var netCards = new CardNetworkData[cards.Count];
            for (int j = 0; j < cards.Count; j++)
                netCards[j] = new CardNetworkData(cards[j]);

            allData.Add(new PlayerCardsData { PlayerIndex = i, Cards = netCards });
        }

        Debug.Log($"[NetworkCardDealerSync] Initial deal: sending {allData.Count} player hands");
        ShowPlayerCardsClientRpc(allData.ToArray());
    }

    private void HandleCardTaken(Skeleton player, CardData card)
    {
        if (!IsServer)
            return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
            return;

        int playerIndex = -1;
        for (int i = 0; i < gm.Players.Count; i++)
        {
            if (gm.Players[i] == player)
            {
                playerIndex = i;
                break;
            }
        }
        if (playerIndex < 0)
            return;

        Debug.Log($"[NetworkCardDealerSync] Player {playerIndex} took a card, sending incremental update");
        DealTakenCardClientRpc(playerIndex, new CardNetworkData(card));
    }

    [ClientRpc]
    private void ShowPlayerCardsClientRpc(PlayerCardsData[] playerCards)
    {
        ResolveCardDealer();
        if (IsHost)
            return;
        if (cardDealer == null)
            return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm?.CardGame == null)
            return;

        var playersList = new List<Skeleton>(gm.Players);

        cardDealer.ClearPlayerCards();

        int cardsPerPlayer = 0;
        foreach (var data in playerCards)
        {
            if (data.PlayerIndex >= playersList.Count)
                continue;

            var skeleton = playersList[data.PlayerIndex];
            skeleton.Hand.Clear();
            foreach (var card in data.Cards)
                skeleton.Hand.AddCard(card.ToCardData());

            if (data.Cards.Length > cardsPerPlayer)
                cardsPerPlayer = data.Cards.Length;
        }

        cardDealer.DealCardsToPlayers(playersList, cardsPerPlayer);
    }

    [ClientRpc]
    private void DealTakenCardClientRpc(int playerIndex, CardNetworkData card)
    {
        ResolveCardDealer();
        if (IsHost)
            return;
        if (cardDealer == null)
            return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm?.CardGame == null)
            return;

        var playersList = new List<Skeleton>(gm.Players);
        if (playerIndex >= playersList.Count)
            return;

        var skeleton = playersList[playerIndex];
        var cardData = card.ToCardData();
        skeleton.Hand.AddCard(cardData);

        Debug.Log($"[NetworkCardDealerSync] Client: player {playerIndex} took a card, dealing incrementally");
        cardDealer.DealCardToPlayer(skeleton, cardData);
    }

    private void HandleTableCardsDealt(IReadOnlyList<CardData> cards)
    {
        if (!IsServer)
            return;

        Debug.Log($"[NetworkCardDealerSync] Table cards dealt: {cards.Count}, sending to clients");

        var networkCards = new CardNetworkData[cards.Count];
        for (int i = 0; i < cards.Count; i++)
            networkCards[i] = new CardNetworkData(cards[i]);

        ShowTableCardsClientRpc(networkCards);
    }

    [ClientRpc]
    private void ShowTableCardsClientRpc(CardNetworkData[] cards)
    {
        ResolveCardDealer();
        if (IsHost)
            return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
            return;

        var cardDataList = new List<CardData>(cards.Length);
        for (int i = 0; i < cards.Length; i++)
            cardDataList.Add(cards[i].ToCardData());

        Debug.Log($"[NetworkCardDealerSync] Client: dealing {cardDataList.Count} table cards");
        gm.ClientDealTableCards(cardDataList);
    }

    private void ResolveCardDealer()
    {
        if (IsUsableCardDealer(cardDealer))
            return;

        CardDealer? activeDealer = FindFirstObjectByType<CardDealer>();
        if (IsUsableCardDealer(activeDealer))
        {
            cardDealer = activeDealer;
            return;
        }

        CardDealer[] allDealers = FindObjectsByType<CardDealer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allDealers.Length; i++)
        {
            CardDealer dealer = allDealers[i];
            if (IsUsableCardDealer(dealer))
            {
                cardDealer = dealer;
                return;
            }
        }
    }

    private static bool IsUsableCardDealer(CardDealer? dealer)
    {
        return dealer != null && dealer.isActiveAndEnabled && dealer.gameObject.activeInHierarchy;
    }
}
