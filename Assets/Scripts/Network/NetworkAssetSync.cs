using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    [Serializable]
    public struct NetworkAssetData : INetworkSerializable, IEquatable<NetworkAssetData>
    {
        public int AssetId;
        public int AssetType;
        public int StakeValue;
        public int OwningTeamIndex;
        public int SourcePlayerIndex;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref AssetId);
            serializer.SerializeValue(ref AssetType);
            serializer.SerializeValue(ref StakeValue);
            serializer.SerializeValue(ref OwningTeamIndex);
            serializer.SerializeValue(ref SourcePlayerIndex);
        }

        public bool Equals(NetworkAssetData other)
        {
            return AssetId == other.AssetId;
        }
    }

    public sealed class NetworkAssetSync : NetworkBehaviour
    {
        [SerializeField] private GameManager gameManager;

        private NetworkList<NetworkAssetData> _networkAssets;

        public event Action OnAssetsChanged;

        private void Awake()
        {
            _networkAssets = new NetworkList<NetworkAssetData>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _networkAssets.OnListChanged += HandleAssetsChanged;

            if (IsServer)
            {
                SyncAllAssets();
            }
        }

        public override void OnNetworkDespawn()
        {
            _networkAssets.OnListChanged -= HandleAssetsChanged;
            base.OnNetworkDespawn();
        }

        public void SyncAllAssets()
        {
            if (!IsServer) return;

            _networkAssets.Clear();

            int assetId = 0;
            foreach (var team in gameManager.Teams)
            {
                int teamIndex = IndexOfList(gameManager.Teams, team);

                foreach (var asset in team.Assets)
                {
                    int sourcePlayerIndex = -1;
                    if (asset.sourceOwner != null)
                    {
                        sourcePlayerIndex = IndexOfList(gameManager.Players, asset.sourceOwner);
                    }

                    _networkAssets.Add(new NetworkAssetData
                    {
                        AssetId = assetId++,
                        AssetType = (int)asset.assetType,
                        StakeValue = asset.stakeValue,
                        OwningTeamIndex = teamIndex,
                        SourcePlayerIndex = sourcePlayerIndex
                    });
                }
            }
        }

        public void UpdateAssetOwnership(StakeAsset asset, Team newOwner)
        {
            if (!IsServer) return;

            int newOwnerIndex = IndexOfList(gameManager.Teams, newOwner);

            for (int i = 0; i < _networkAssets.Count; i++)
            {
                var data = _networkAssets[i];
                if (data.AssetType == (int)asset.assetType &&
                    data.StakeValue == asset.stakeValue)
                {
                    data.OwningTeamIndex = newOwnerIndex;
                    _networkAssets[i] = data;
                    break;
                }
            }

            BroadcastOwnershipChangeClientRpc(
                (int)asset.assetType,
                asset.stakeValue,
                newOwnerIndex
            );
        }

        [ClientRpc]
        private void BroadcastOwnershipChangeClientRpc(int assetType, int stakeValue,
            int newOwnerTeamIndex)
        {
            Debug.Log($"[NetworkAssetSync] Asset {(StakeAssetType)assetType} ({stakeValue}) " +
                     $"transferred to team {newOwnerTeamIndex}");
        }

        private void HandleAssetsChanged(NetworkListEvent<NetworkAssetData> changeEvent)
        {
            OnAssetsChanged?.Invoke();
        }

        public List<NetworkAssetData> GetAllAssets()
        {
            var list = new List<NetworkAssetData>();
            foreach (var asset in _networkAssets)
            {
                list.Add(asset);
            }
            return list;
        }

        public List<NetworkAssetData> GetTeamAssets(int teamIndex)
        {
            var list = new List<NetworkAssetData>();
            foreach (var asset in _networkAssets)
            {
                if (asset.OwningTeamIndex == teamIndex)
                {
                    list.Add(asset);
                }
            }
            return list;
        }

        private static int IndexOfList<T>(IReadOnlyList<T> list, T item) where T : class
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item)) return i;
            }
            return -1;
        }
    }
}
