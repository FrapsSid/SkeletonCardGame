using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    public class NetworkGameState : NetworkBehaviour
    {
        public const int NoCurrentTurnPlayerIndex = -1;
        public const ulong NoCurrentTurnClientId = ulong.MaxValue;

        public static NetworkGameState Instance { get; private set; }

        private readonly NetworkVariable<CardGame.GamePhase> _currentPhase = new(
            CardGame.GamePhase.DealingCards,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly NetworkVariable<int> _currentTurnPlayerIndex = new(
            NoCurrentTurnPlayerIndex,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly NetworkVariable<ulong> _currentTurnClientId = new(
            NoCurrentTurnClientId,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Betting state sync
        private readonly NetworkVariable<int> _currentBettingPrice = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly NetworkVariable<int> _bettingRound = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Timer sync
        private readonly NetworkVariable<float> _turnTimeRemaining = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Table cards (serialized as int array)
        private NetworkList<int> _tableCardIds;

        public CardGame.GamePhase CurrentPhase => _currentPhase.Value;
        public int CurrentTurnPlayerIndex => _currentTurnPlayerIndex.Value;
        public ulong CurrentTurnClientId => _currentTurnClientId.Value;
        public int CurrentBettingPrice => _currentBettingPrice.Value;
        public int BettingRound => _bettingRound.Value;
        public float TurnTimeRemaining => _turnTimeRemaining.Value;

        public event Action<CardGame.GamePhase> OnPhaseChanged;
        public event Action<int, ulong> OnCurrentTurnChanged;
        public event Action<int> OnBettingPriceChanged;
        public event Action<float> OnTurnTimerChanged;
        public event Action<List<CardData>> OnTableCardsChanged;

        private void Awake()
        {
            _tableCardIds = new NetworkList<int>();

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            _currentPhase.OnValueChanged += HandlePhaseChanged;
            _currentTurnPlayerIndex.OnValueChanged += HandleCurrentTurnPlayerIndexChanged;
            _currentTurnClientId.OnValueChanged += HandleCurrentTurnClientIdChanged;
            _currentBettingPrice.OnValueChanged += HandleBettingPriceChanged;
            _turnTimeRemaining.OnValueChanged += HandleTurnTimerChanged;
            _tableCardIds.OnListChanged += HandleTableCardsChanged;
        }

        public override void OnNetworkDespawn()
        {
            _currentPhase.OnValueChanged -= HandlePhaseChanged;
            _currentTurnPlayerIndex.OnValueChanged -= HandleCurrentTurnPlayerIndexChanged;
            _currentTurnClientId.OnValueChanged -= HandleCurrentTurnClientIdChanged;
            _currentBettingPrice.OnValueChanged -= HandleBettingPriceChanged;
            _turnTimeRemaining.OnValueChanged -= HandleTurnTimerChanged;
            _tableCardIds.OnListChanged -= HandleTableCardsChanged;
        }

        private void HandlePhaseChanged(CardGame.GamePhase oldValue, CardGame.GamePhase newValue)
        {
            OnPhaseChanged?.Invoke(newValue);
        }

        private void HandleCurrentTurnPlayerIndexChanged(int oldValue, int newValue)
        {
            OnCurrentTurnChanged?.Invoke(newValue, _currentTurnClientId.Value);
        }

        private void HandleCurrentTurnClientIdChanged(ulong oldValue, ulong newValue)
        {
            OnCurrentTurnChanged?.Invoke(_currentTurnPlayerIndex.Value, newValue);
        }

        private void HandleBettingPriceChanged(int oldValue, int newValue)
        {
            OnBettingPriceChanged?.Invoke(newValue);
        }

        private void HandleTurnTimerChanged(float oldValue, float newValue)
        {
            OnTurnTimerChanged?.Invoke(newValue);
        }

        private void HandleTableCardsChanged(NetworkListEvent<int> changeEvent)
        {
            var cards = new List<CardData>();
            foreach (int id in _tableCardIds)
            {
                cards.Add(IdToCard(id));
            }
            OnTableCardsChanged?.Invoke(cards);
        }

        // ── Server methods ─────────────────────────────────
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetPhaseServerRpc(CardGame.GamePhase newPhase)
        {
            if (!IsServer) return;
            _currentPhase.Value = newPhase;
        }

        public void SetPhase(CardGame.GamePhase newPhase)
        {
            if (!IsServer) return;
            _currentPhase.Value = newPhase;
        }

        public void SetCurrentTurn(int playerIndex, ulong clientId)
        {
            if (!IsServer) return;
            _currentTurnPlayerIndex.Value = playerIndex;
            _currentTurnClientId.Value = clientId;
        }

        public void ClearCurrentTurn()
        {
            SetCurrentTurn(NoCurrentTurnPlayerIndex, NoCurrentTurnClientId);
        }

        public void SetBettingPrice(int price)
        {
            if (!IsServer) return;
            _currentBettingPrice.Value = price;
        }

        public void SetBettingRound(int round)
        {
            if (!IsServer) return;
            _bettingRound.Value = round;
        }

        public void SetTurnTimeRemaining(float time)
        {
            if (!IsServer) return;
            _turnTimeRemaining.Value = time;
        }

        public void SetTableCards(IReadOnlyList<CardData> cards)
        {
            if (!IsServer) return;

            _tableCardIds.Clear();
            foreach (var card in cards)
            {
                _tableCardIds.Add(CardToId(card));
            }
        }

        public void AddTableCard(CardData card)
        {
            if (!IsServer) return;
            _tableCardIds.Add(CardToId(card));
        }

        // ── Card serialization helpers ──────────────────────
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
