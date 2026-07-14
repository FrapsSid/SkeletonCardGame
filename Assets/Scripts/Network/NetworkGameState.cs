using System.Linq;
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

        private readonly NetworkVariable<int> _currentParticipationPrice = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public CardGame.GamePhase CurrentPhase => _currentPhase.Value;
        public int CurrentTurnPlayerIndex => _currentTurnPlayerIndex.Value;
        public ulong CurrentTurnClientId => _currentTurnClientId.Value;
        public int CurrentParticipationPrice => _currentParticipationPrice.Value;

        public event System.Action<CardGame.GamePhase> OnPhaseChanged;
        public event System.Action<int, ulong> OnCurrentTurnChanged;
        public event System.Action<int> OnParticipationPriceChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            _currentPhase.OnValueChanged += HandlePhaseChanged;
            _currentTurnPlayerIndex.OnValueChanged += HandleCurrentTurnPlayerIndexChanged;
            _currentTurnClientId.OnValueChanged += HandleCurrentTurnClientIdChanged;
            _currentParticipationPrice.OnValueChanged += HandleParticipationPriceChanged;
        }

        public override void OnNetworkDespawn()
        {
            _currentPhase.OnValueChanged -= HandlePhaseChanged;
            _currentTurnPlayerIndex.OnValueChanged -= HandleCurrentTurnPlayerIndexChanged;
            _currentTurnClientId.OnValueChanged -= HandleCurrentTurnClientIdChanged;
            _currentParticipationPrice.OnValueChanged -= HandleParticipationPriceChanged;
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

        private void HandleParticipationPriceChanged(int oldValue, int newValue)
        {
            OnParticipationPriceChanged?.Invoke(newValue);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetPhaseServerRpc(CardGame.GamePhase newPhase)
        {
            if (!IsServer) return;
            _currentPhase.Value = newPhase;
        }

        public void SetPhase(CardGame.GamePhase newPhase)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkGameState] Only server can set phase");
                return;
            }
            _currentPhase.Value = newPhase;
        }

        public void SetCurrentTurn(int playerIndex, ulong clientId)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkGameState] Only server can set current turn");
                return;
            }

            _currentTurnPlayerIndex.Value = playerIndex;
            _currentTurnClientId.Value = clientId;
        }

        public void ClearCurrentTurn()
        {
            SetCurrentTurn(NoCurrentTurnPlayerIndex, NoCurrentTurnClientId);
        }

        public void SetParticipationPrice(int price)
        {
            if (!IsServer)
            {
                return;
            }
            _currentParticipationPrice.Value = price;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SubmitBetServerRpc(int[] bodyPartTypeValues, int tierValue, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            ulong senderClientId = rpcParams.Receive.SenderClientId;
            ulong currentTurnClientId = _currentTurnClientId.Value;

            if (senderClientId != currentTurnClientId)
            {
                Debug.LogWarning($"[NetworkGameState] SubmitBet rejected: sender {senderClientId} != currentTurn {currentTurnClientId}");
                return;
            }

            var gm = FindFirstObjectByType<GameManager>();
            gm?.ProcessNetworkBet(bodyPartTypeValues, tierValue);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SubmitTurnActionServerRpc(int actionType, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            var gm = FindFirstObjectByType<GameManager>();
            if (gm == null) return;

            ulong senderClientId = rpcParams.Receive.SenderClientId;
            ulong currentTurnClientId = _currentTurnClientId.Value;

            // Primary validation: sender must be the current player
            if (senderClientId == currentTurnClientId)
            {
                gm?.ProcessNetworkTurnAction(actionType);
                return;
            }

            // Fallback: check if NetworkGameState is out of sync but GameManager knows the correct current player
            if (currentTurnClientId == NoCurrentTurnClientId && gm.CardGame?.round != null)
            {
                var currentPlayer = gm.CardGame.round.CurrentPlayer;
                if (currentPlayer != null && currentPlayer.HasNetworkClientId && currentPlayer.NetworkClientId == senderClientId)
                {
                    Debug.LogWarning($"[NetworkGameState] Turn action allowed via fallback: sender {senderClientId} matches GameManager current player {senderClientId}");
                    gm.ProcessNetworkTurnAction(actionType);
                    return;
                }
            }

            // Debug: log all player NetworkClientIds for debugging
            if (gm.CardGame?.round != null)
            {
                Debug.LogWarning($"[NetworkGameState] Turn action REJECTED: sender={senderClientId}, currentTurnClientId={currentTurnClientId}, playerIndex={_currentTurnPlayerIndex.Value}. Players: {string.Join(", ", gm.Players.Select(p => $"{p.NetworkClientId}(hasId:{p.HasNetworkClientId})"))}");
            }
            else
            {
                Debug.LogWarning($"[NetworkGameState] Turn action REJECTED: sender={senderClientId}, currentTurnClientId={currentTurnClientId}, playerIndex={_currentTurnPlayerIndex.Value}, round=null");
            }
        }
    }
}
