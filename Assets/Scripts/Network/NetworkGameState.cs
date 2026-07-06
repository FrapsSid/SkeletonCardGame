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

        public CardGame.GamePhase CurrentPhase => _currentPhase.Value;
        public int CurrentTurnPlayerIndex => _currentTurnPlayerIndex.Value;
        public ulong CurrentTurnClientId => _currentTurnClientId.Value;

        public event System.Action<CardGame.GamePhase> OnPhaseChanged;
        public event System.Action<int, ulong> OnCurrentTurnChanged;

        private void Awake()
        {
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
        }

        public override void OnNetworkDespawn()
        {
            _currentPhase.OnValueChanged -= HandlePhaseChanged;
            _currentTurnPlayerIndex.OnValueChanged -= HandleCurrentTurnPlayerIndexChanged;
            _currentTurnClientId.OnValueChanged -= HandleCurrentTurnClientIdChanged;
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
    }
}
