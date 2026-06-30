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
    [SerializeField] private CardDealer cardDealer;

    public override void OnNetworkSpawn()
    {
        if (cardDealer == null)
            cardDealer = FindFirstObjectByType<CardDealer>();

        Debug.Log($"[NetworkCardDealerSync] OnNetworkSpawn: IsServer={IsServer}, IsHost={IsHost}, IsClient={IsClient}, cardDealer={cardDealer != null}");

        if (!IsServer) return;

        var gm = FindFirstObjectByType<GameManager>();
        Debug.Log($"[NetworkCardDealerSync] GameManager found: {gm != null}, CardGame exists: {gm?.CardGame != null}");
        if (gm.CardGame != null)
        {
            HandleGameCreated(gm.CardGame);
        }
        else
        {
            gm.OnGameCreated += HandleGameCreated;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
            gm.OnGameCreated -= HandleGameCreated;
            gm.OnCardDealCompleted -= HandleCardDealCompleted;
    }

    private void HandleGameCreated(CardGame game)
    {
        Debug.Log("[NetworkCardDealerSync] HandleGameCreated: подписка на OnTableCardsDealt");
        game.OnTableCardsDealt += HandleTableCardsDealt;
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
            gm.OnCardDealCompleted += HandleCardDealCompleted;
    }

    private void HandleCardDealCompleted()
    {
        if (!IsServer) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm?.CardGame == null) return;

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

        Debug.Log($"[NetworkCardDealerSync] Отправляю карты {allData.Count} игрокам");
        ShowPlayerCardsClientRpc(allData.ToArray());
    }
    [ClientRpc]
    private void ShowPlayerCardsClientRpc(PlayerCardsData[] playerCards)
    {
        if (IsHost) return;
        if (cardDealer == null) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm?.CardGame == null) return;

        var playersList = new List<Skeleton>(gm.Players);

        int cardsPerPlayer = 0;
        foreach (var data in playerCards)
        {
            if (data.PlayerIndex >= playersList.Count) continue;

            var skeleton = playersList[data.PlayerIndex];
            foreach (var card in data.Cards)
                skeleton.Hand.AddCard(card.ToCardData());

            if (data.Cards.Length > cardsPerPlayer)
                cardsPerPlayer = data.Cards.Length;
        }
        cardDealer.DealCardsToPlayers(playersList, cardsPerPlayer);
    }

    private void HandleTableCardsDealt(IReadOnlyList<CardData> cards)
    {
        if (!IsServer) return;

        Debug.Log($"[NetworkCardDealerSync] Сервер раздал {cards.Count} карт на стол, отправка ClientRpc");

        var networkCards = new CardNetworkData[cards.Count];
        for (int i = 0; i < cards.Count; i++)
            networkCards[i] = new CardNetworkData(cards[i]);

        ShowTableCardsClientRpc(networkCards);
    }

    [ClientRpc]
    private void ShowTableCardsClientRpc(CardNetworkData[] cards)
    {
        Debug.Log($"[NetworkCardDealerSync] Клиент получил {cards.Length} карт: IsHost={IsHost}, cardDealer={cardDealer != null}");

        if (IsHost) return;
        if (cardDealer == null) 
        {
            Debug.LogWarning("[NetworkCardDealerSync] cardDealer is null.");
            return;
        }

        var cardDataList = new List<CardData>(cards.Length);
        for (int i = 0; i < cards.Length; i++)
            cardDataList.Add(cards[i].ToCardData());

        Debug.Log($"[NetworkCardDealerSync] Вызов CardDealer.DealCardsToTable с {cardDataList.Count} картами");
        cardDealer.DealCardsToTable(cardDataList);
    }
}