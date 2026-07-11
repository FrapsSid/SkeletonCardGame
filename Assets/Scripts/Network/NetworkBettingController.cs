using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    public sealed class NetworkBettingController : NetworkBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private NetworkGameState networkGameState;

        public event Action<ulong, int, DeclaredCombinationTier> OnBetReceived;
        public event Action<ulong> OnFoldReceived;
        public event Action<ulong, CardData> OnCardDrawn;

        private Dictionary<ulong, NetworkPlayer> _playersByClientId = new();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                RegisterAllPlayers();
            }
        }

        private void RegisterAllPlayers()
        {
            foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
            {
                _playersByClientId[player.ClientId] = player;
            }
        }

        public void ProcessBetRequest(ulong clientId, int betValue, int combinationTier, int[] assetIds)
        {
            if (!IsServer) return;

            var game = gameManager?.CardGame;
            var round = game?.round;
            if (round == null) return;

            Skeleton player = FindSkeletonForClient(clientId);
            if (player == null || round.CurrentPlayer != player)
            {
                RejectBetClientRpc(clientId, "Not your turn");
                return;
            }

            var combination = (DeclaredCombinationTier)combinationTier;
            var assets = ResolveAssetsForPlayer(player, assetIds);

            if (!round.CanBet(player, betValue, assets, combination))
            {
                RejectBetClientRpc(clientId, "Invalid bet");
                return;
            }

            round.Bet(player, betValue, assets, combination);

            networkGameState.SetBettingPrice(round.currentParticipationPrice);

            BroadcastBetClientRpc(clientId, betValue, combinationTier);

            OnBetReceived?.Invoke(clientId, betValue, combination);
        }

        public void ProcessFoldRequest(ulong clientId)
        {
            if (!IsServer) return;

            var game = gameManager?.CardGame;
            var round = game?.round;
            if (round == null) return;

            Skeleton player = FindSkeletonForClient(clientId);
            if (player == null || round.CurrentPlayer != player)
            {
                RejectActionClientRpc(clientId, "Not your turn");
                return;
            }

            round.Fold(player);

            BroadcastFoldClientRpc(clientId);
            OnFoldReceived?.Invoke(clientId);
        }

        public void ProcessDrawCardRequest(ulong clientId)
        {
            if (!IsServer) return;

            var game = gameManager?.CardGame;
            var round = game?.round;
            if (round == null) return;

            Skeleton player = FindSkeletonForClient(clientId);
            if (player == null || round.CurrentPlayer != player)
            {
                RejectActionClientRpc(clientId, "Not your turn");
                return;
            }

            if (!round.CanTakeCard(player))
            {
                RejectActionClientRpc(clientId, "Cannot draw card");
                return;
            }

            round.TakeCard(player);

            var cards = player.Hand.GetCards();
            if (cards.Count > 0)
            {
                CardData drawnCard = cards[cards.Count - 1];
                SendDrawnCardClientRpc(CardToId(drawnCard),
                    new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new[] { clientId }
                        }
                    });
                OnCardDrawn?.Invoke(clientId, drawnCard);
            }
        }

        public void ProcessEndTurnRequest(ulong clientId)
        {
            if (!IsServer) return;

            var game = gameManager?.CardGame;
            var round = game?.round;
            if (round == null) return;

            Skeleton player = FindSkeletonForClient(clientId);
            if (player == null || round.CurrentPlayer != player)
            {
                RejectActionClientRpc(clientId, "Not your turn");
                return;
            }

            if (!round.HasMatchedBet(player))
            {
                RejectActionClientRpc(clientId, "Must match bet first");
                return;
            }

            round.EndTurn(player);
        }

        [ClientRpc]
        private void RejectBetClientRpc(ulong targetClientId, string reason,
            ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.LocalClientId != targetClientId) return;
            Debug.LogWarning($"[NetworkBetting] Bet rejected: {reason}");
        }

        [ClientRpc]
        private void RejectActionClientRpc(ulong targetClientId, string reason,
            ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.LocalClientId != targetClientId) return;
            Debug.LogWarning($"[NetworkBetting] Action rejected: {reason}");
        }

        [ClientRpc]
        private void BroadcastBetClientRpc(ulong clientId, int betValue, int combinationTier)
        {
            Debug.Log($"[NetworkBetting] Player {clientId} bet {betValue}");
        }

        [ClientRpc]
        private void BroadcastFoldClientRpc(ulong clientId)
        {
            Debug.Log($"[NetworkBetting] Player {clientId} folded");
        }

        [ClientRpc]
        private void SendDrawnCardClientRpc(int cardId, ClientRpcParams clientRpcParams = default)
        {
            CardData card = IdToCard(cardId);
            Debug.Log($"[NetworkBetting] Drew card: {card}");
        }

        private Skeleton FindSkeletonForClient(ulong clientId)
        {
            foreach (var player in gameManager?.Players ?? Array.Empty<Skeleton>())
            {
                if (player.HasNetworkClientId && player.NetworkClientId == clientId)
                    return player;
            }
            return null;
        }

        private IList<StakeAsset> ResolveAssets(int[] assetIds)
        {
            // This should not be called directly without a player context
            // The actual resolution happens in ProcessBetRequest via ResolveAssetsForPlayer
            if (assetIds == null || assetIds.Length == 0) return new List<StakeAsset>();
            
            var assets = new List<StakeAsset>();
            return assets;
        }
        
        private IList<StakeAsset> ResolveAssetsForPlayer(Skeleton player, int[] assetIds)
        {
            if (assetIds == null || assetIds.Length == 0) return new List<StakeAsset>();
            
            var assets = new List<StakeAsset>();
            if (player?.team?.Assets == null) return assets;
            
            foreach (int assetId in assetIds)
            {
                if (assetId >= 0 && assetId < player.team.Assets.Count)
                {
                    var asset = player.team.Assets[assetId];
                    if (asset != null)
                        assets.Add(asset);
                }
            }
            return assets;
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
