using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkPlayer : NetworkBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private CharacterController characterController;
        [Tooltip("Контейнер с камерами и cinemachine")]
        [SerializeField] private GameObject cameraRig;
        public ulong ClientId { get; private set; }
        public bool IsLocalPlayer => IsOwner;

        public event Action OnPlayerSpawned;
        public event Action OnPlayerDespawned;

        public override void OnNetworkSpawn()
        {
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
            }

            OnPlayerSpawned?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            bool isOwner = IsOwner;
            if (isOwner)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            OnPlayerDespawned?.Invoke();
        }
    }
}
