using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayer : NetworkBehaviour
    {
        public enum Team
        {
            None = 0,
            TeamA = 1,
            TeamB = 2
        }

        [SerializeField] private PlayerController playerController;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private CharacterController characterController;
        [Tooltip("Контейнер с камерами и cinemachine")]
        [SerializeField] private GameObject cameraRig;

        private readonly NetworkVariable<FixedString64Bytes> _playerName = new NetworkVariable<FixedString64Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<Team> _assignedTeam = new NetworkVariable<Team>(
            Team.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> _playerIndex = new(
            -1, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
        );
        public int PlayerIndex => _playerIndex.Value;

        public ulong ClientId { get; private set; }
        public string PlayerName => _playerName.Value.ToString();
        public Team AssignedTeam => _assignedTeam.Value;
        public new bool IsLocalPlayer => IsOwner;

        public event Action OnPlayerSpawned;
        public event Action OnPlayerDespawned;
        public event Action<string> OnPlayerNameChanged;

        public override void OnNetworkSpawn()
        {
            _playerName.OnValueChanged += HandlePlayerNameChanged;
            ClientId = OwnerClientId;
            gameObject.name = $"Player_{OwnerClientId}";

            bool isOwner = IsOwner;

            if (playerController != null) playerController.enabled = isOwner;
            if (cameraController != null) cameraController.enabled = isOwner;
            if (inputReader != null) inputReader.enabled = isOwner;
            if (characterController != null) characterController.enabled = isOwner;
            if (cameraRig != null) cameraRig.SetActive(isOwner);
            if (isOwner)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (IsServer)
            {
                var spawnPoint = SpawnPointManager.Instance != null
                    ? SpawnPointManager.Instance.GetNextSpawnPoint()
                    : null;

                if (spawnPoint != null)
                {
                    if (characterController != null) characterController.enabled = false;
                    transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
                    if (characterController != null) characterController.enabled = isOwner;
                }

                if (_playerName.Value.IsEmpty)
                {
                    _playerName.Value = new FixedString64Bytes($"Player {OwnerClientId}");
                }
            }
            if (IsServer && _playerIndex.Value == -1)
            {
                _playerIndex.Value = (int)OwnerClientId;
            }

            OnPlayerSpawned?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            _playerName.OnValueChanged -= HandlePlayerNameChanged;
            bool isOwner = IsOwner;
            if (isOwner)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            OnPlayerDespawned?.Invoke();
        }

        [ServerRpc]
        public void RequestSetNameServerRpc(string newName, ServerRpcParams rpcParams = default)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;
            _playerName.Value = new FixedString64Bytes(newName.Length > 63 ? newName[..63] : newName);
        }

        private void HandlePlayerNameChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
        {
            OnPlayerNameChanged?.Invoke(newName.ToString());
        }
    }
}
