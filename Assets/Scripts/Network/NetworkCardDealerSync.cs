#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
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

        gm.OnRoundReset += HandleRoundReset;
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
            gm.OnMatchEnded -= HandleMatchEnded;
            gm.OnRoundReset -= HandleRoundReset;
            if (gm.CardGame != null)
            {
                gm.CardGame.OnTableCardsDealt -= HandleTableCardsDealt;
                gm.CardGame.OnCardTaken -= HandleCardTaken;
                gm.CardGame.OnPotResolved -= HandlePotResolved;
            }
        }
    }

    private void HandleGameCreated(CardGame game)
    {
        Debug.Log("[NetworkCardDealerSync] HandleGameCreated: subscribing to events");
        game.OnTableCardsDealt += HandleTableCardsDealt;
        game.OnCardTaken += HandleCardTaken;
        game.OnPotResolved += HandlePotResolved;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.OnCardDealCompleted += HandleInitialDealCompleted;
            gm.OnMatchEnded += HandleMatchEnded;
            gm.OnRoundReset += HandleRoundReset;
        }

        _initialDealSent = false;
    }

    private void HandleRoundReset()
    {
        _initialDealSent = false;
        Debug.Log("[NetworkCardDealerSync] Round reset, will resend initial deal");
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
        SyncAllPlayerHandsClientRpc(allData.ToArray());
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

    private void HandleTableCardsDealt(IReadOnlyList<CardData> cards)
    {
        if (!IsServer)
            return;

        Debug.Log($"[NetworkCardDealerSync] Table cards dealt: {cards.Count}, sending to clients");

        var gm = FindFirstObjectByType<GameManager>();
        int generation = gm != null ? gm.RoundGeneration : 0;

        var networkCards = new CardNetworkData[cards.Count];
        for (int i = 0; i < cards.Count; i++)
            networkCards[i] = new CardNetworkData(cards[i]);

        ShowTableCardsClientRpc(networkCards, generation);
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
    private void SyncAllPlayerHandsClientRpc(PlayerCardsData[] playerCards)
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

        foreach (var data in playerCards)
        {
            if (data.PlayerIndex >= playersList.Count)
                continue;

            var skeleton = playersList[data.PlayerIndex];
            skeleton.Hand.Clear();
            foreach (var card in data.Cards)
                skeleton.Hand.AddCard(card.ToCardData());
        }

        int cardsPerPlayer = playerCards.Length > 0 ? playerCards[0].Cards.Length : 0;
        cardDealer.DealCardsToPlayers(playersList, cardsPerPlayer);
        Debug.Log($"[NetworkCardDealerSync] Client: full hand sync received for {playerCards.Length} players");
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

    [ClientRpc]
    private void ShowTableCardsClientRpc(CardNetworkData[] cards, int generation)
    {
        ResolveCardDealer();
        if (IsHost)
            return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
            return;

        if (gm.CardGame == null)
            return;

        if (!gm.IsNetworkMode)
        {
            var cardDataList = new List<CardData>(cards.Length);
            for (int i = 0; i < cards.Length; i++)
                cardDataList.Add(cards[i].ToCardData());
            gm.ClientDealTableCards(cardDataList);
            return;
        }

        if (generation < gm.RoundGeneration)
        {
            Debug.LogWarning($"[NetworkCardDealerSync] Ignoring stale table cards RPC (gen={generation}, current={gm.RoundGeneration})");
            return;
        }

        bool generationAdvanced = generation > gm.RoundGeneration;
        gm.AdvanceRoundGeneration(generation);

        var dealCards = new List<CardData>(cards.Length);
        for (int i = 0; i < cards.Length; i++)
            dealCards.Add(cards[i].ToCardData());

        if (generationAdvanced)
        {
            Debug.Log($"[NetworkCardDealerSync] Client: round advanced to gen {generation}, clearing table and dealing {dealCards.Count} table cards");
            gm.ClientClearTableAndDeal(dealCards);
        }
        else
        {
            Debug.Log($"[NetworkCardDealerSync] Client: dealing {dealCards.Count} table cards (gen={generation})");
            gm.ClientDealTableCards(dealCards);
        }
    }

    [ClientRpc]
    private void ClearTableClientRpc(int generation)
    {
        if (IsHost)
            return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null || !gm.IsNetworkMode)
            return;

        if (generation < gm.RoundGeneration)
        {
            Debug.LogWarning($"[NetworkCardDealerSync] Ignoring stale clear table RPC (gen={generation}, current={gm.RoundGeneration})");
            return;
        }

        gm.AdvanceRoundGeneration(generation);
        gm.ClientClearTable();
        Debug.Log($"[NetworkCardDealerSync] Client: cleared table for generation {generation}");
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

    private void HandlePotResolved(IReadOnlyList<Team> winners, IReadOnlyList<StakeAsset> resolvedAssets)
    {
        Debug.Log($"[NetworkCardDealerSync] HandlePotResolved: IsServer={IsServer}, resolvedAssets.Count={resolvedAssets.Count}");
        if (!IsServer) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning("[NetworkCardDealerSync] HandlePotResolved: GameManager not found");
            return;
        }

        var playerIndices = new List<int>();
        var bodyPartTypes = new List<int>();

        foreach (var asset in resolvedAssets)
        {
            Debug.Log($"[NetworkCardDealerSync] Checking asset: bodyPart={asset.bodyPart != null}, sourceOwner={asset.sourceOwner != null}, bodyPart.Item={asset.bodyPart?.Item != null}");
            if (asset.bodyPart != null && asset.sourceOwner != null && asset.bodyPart.Item != null)
            {
                int playerIndex = -1;
                for (int i = 0; i < gm.Players.Count; i++)
                {
                    if (gm.Players[i] == asset.sourceOwner)
                    {
                        playerIndex = i;
                        break;
                    }
                }
                if (playerIndex >= 0)
                {
                    playerIndices.Add(playerIndex);
                    bodyPartTypes.Add((int)asset.bodyPart.Item.Type);
                    Debug.Log($"[NetworkCardDealerSync] Found body part to remove: player {playerIndex}, type {asset.bodyPart.Item.Type}");
                }
            }
        }

        if (playerIndices.Count > 0)
        {
            Debug.Log($"[NetworkCardDealerSync] Syncing {playerIndices.Count} body part removals");
            SyncBodyPartRemovalsClientRpc(playerIndices.ToArray(), bodyPartTypes.ToArray());
        }
        else
        {
            Debug.Log("[NetworkCardDealerSync] No body parts to sync");
        }
    }

    [ClientRpc]
    private void SyncBodyPartRemovalsClientRpc(int[] playerIndices, int[] bodyPartTypes)
    {
        Debug.Log($"[NetworkCardDealerSync] SyncBodyPartRemovalsClientRpc: IsHost={IsHost}, playerIndices.Length={playerIndices.Length}");
        if (IsHost) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning("[NetworkCardDealerSync] SyncBodyPartRemovalsClientRpc: GameManager not found");
            return;
        }

        for (int i = 0; i < playerIndices.Length; i++)
        {
            int playerIndex = playerIndices[i];
            int bodyPartType = bodyPartTypes[i];

            if (playerIndex >= gm.Players.Count)
            {
                Debug.LogWarning($"[NetworkCardDealerSync] SyncBodyPartRemovalsClientRpc: playerIndex {playerIndex} out of range");
                continue;
            }

            var skeleton = gm.Players[playerIndex];
            var body = skeleton.Body;
            if (body != null)
            {
                Debug.Log($"[NetworkCardDealerSync] Client: removing body part {(BodyPartType)bodyPartType} from player {playerIndex}");
                body.RemovePart((BodyPartType)bodyPartType);
            }
            else
            {
                Debug.LogWarning($"[NetworkCardDealerSync] SyncBodyPartRemovalsClientRpc: skeleton.Body is null for player {playerIndex}");
            }
        }
    }

    private void HandleMatchEnded(MatchEndResult result)
    {
        if (!IsServer) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return;

        int winningTeamIndex = -1;
        if (result.WinningTeam != null)
        {
            for (int i = 0; i < gm.Teams.Count; i++)
            {
                if (gm.Teams[i] == result.WinningTeam)
                {
                    winningTeamIndex = i;
                    break;
                }
            }
        }

        Debug.Log($"[NetworkCardDealerSync] Match ended, winner team: {winningTeamIndex}");
        SyncMatchEndClientRpc(winningTeamIndex);
    }

    [ClientRpc]
    internal void SyncTurnClientRpc(int playerIndex, ulong clientId)
    {
        if (IsHost) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return;

        Debug.Log($"[NetworkCardDealerSync] Client: sync turn to player {playerIndex} (clientId={clientId})");
        gm.ClientSyncTurn(playerIndex, clientId);
    }

    [ClientRpc]
    private void SyncMatchEndClientRpc(int winningTeamIndex)
    {
        if (IsHost) return;

        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null) return;

        Debug.Log($"[NetworkCardDealerSync] Client: match ended, winner team: {winningTeamIndex}");
        gm.ClientCompleteMatch(winningTeamIndex);
    }

    }