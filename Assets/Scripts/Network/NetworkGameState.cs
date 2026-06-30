using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    public class NetworkGameState : NetworkBehaviour
    {
        public static NetworkGameState Instance { get; private set; }

        private readonly NetworkVariable<CardGame.GamePhase> _currentPhase = new(
            CardGame.GamePhase.DealingCards,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public CardGame.GamePhase CurrentPhase => _currentPhase.Value;

        public event System.Action<CardGame.GamePhase> OnPhaseChanged;

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
        }

        public override void OnNetworkDespawn()
        {
            _currentPhase.OnValueChanged -= HandlePhaseChanged;
        }

        private void HandlePhaseChanged(CardGame.GamePhase oldValue, CardGame.GamePhase newValue)
        {
            OnPhaseChanged?.Invoke(newValue);
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
    }
}