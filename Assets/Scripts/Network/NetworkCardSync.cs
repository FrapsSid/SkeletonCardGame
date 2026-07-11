using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    public sealed class NetworkCardSync : NetworkBehaviour
    {
        [SerializeField] private CardDealer cardDealer;
        [SerializeField] private NetworkGameState networkGameState;

        public void SyncTableCards(IReadOnlyList<CardData> cards)
        {
            if (!IsServer) return;

            int[] cardIds = new int[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                cardIds[i] = CardToId(cards[i]);
            }

            SyncTableCardsClientRpc(cardIds);
        }

        public void SyncNewTableCard(CardData card)
        {
            if (!IsServer) return;
            SyncNewTableCardClientRpc(CardToId(card));
        }

        public void SyncPrivateCards(ulong clientId, IReadOnlyList<CardData> cards)
        {
            if (!IsServer) return;

            int[] cardIds = new int[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                cardIds[i] = CardToId(cards[i]);
            }

            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };

            SyncPrivateCardsClientRpc(cardIds, clientRpcParams);
        }

        public void SyncNewPrivateCard(ulong clientId, CardData card)
        {
            if (!IsServer) return;

            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };

            SyncNewPrivateCardClientRpc(CardToId(card), clientRpcParams);
        }

        public void TriggerDealAnimation(int playerIndex, bool isFaceDown)
        {
            if (!IsServer) return;
            TriggerDealAnimationClientRpc(playerIndex, isFaceDown);
        }

        public void TriggerTableDealAnimation(int cardCount)
        {
            if (!IsServer) return;
            TriggerTableDealAnimationClientRpc(cardCount);
        }

        [ClientRpc]
        private void SyncTableCardsClientRpc(int[] cardIds)
        {
            var cards = new List<CardData>();
            foreach (int id in cardIds)
            {
                cards.Add(IdToCard(id));
            }
            Debug.Log($"[NetworkCardSync] Received {cards.Count} table cards");
        }

        [ClientRpc]
        private void SyncNewTableCardClientRpc(int cardId)
        {
            CardData card = IdToCard(cardId);
            Debug.Log($"[NetworkCardSync] New table card: {card}");
        }

        [ClientRpc]
        private void SyncPrivateCardsClientRpc(int[] cardIds,
            ClientRpcParams clientRpcParams = default)
        {
            var cards = new List<CardData>();
            foreach (int id in cardIds)
            {
                cards.Add(IdToCard(id));
            }
            Debug.Log($"[NetworkCardSync] Received {cards.Count} private cards");
        }

        [ClientRpc]
        private void SyncNewPrivateCardClientRpc(int cardId,
            ClientRpcParams clientRpcParams = default)
        {
            CardData card = IdToCard(cardId);
            Debug.Log($"[NetworkCardSync] Drew: {card}");
        }

        [ClientRpc]
        private void TriggerDealAnimationClientRpc(int playerIndex, bool isFaceDown)
        {
            Debug.Log($"[NetworkCardSync] Deal animation for player {playerIndex}");
        }

        [ClientRpc]
        private void TriggerTableDealAnimationClientRpc(int cardCount)
        {
            Debug.Log($"[NetworkCardSync] Table deal animation: {cardCount} cards");
        }

        private static int CardToId(CardData card)
        {
            return ((int)card.Suit * 13) + ((int)card.Value - 2);
        }

        private static CardData IdToCard(int id)
        {
            CardSuit suit = (CardSuit)(id / 13);
            CardValue value = (CardValue)((id % 13) + 2);
            return new CardData(suit, value);
        }
    }
}
