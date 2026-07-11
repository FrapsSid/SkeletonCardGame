#nullable disable
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Combinations;

namespace Multiplayer
{
    [Serializable]
    public struct NetworkCombinationData : INetworkSerializable, IEquatable<NetworkCombinationData>
    {
        public int CombinationType;
        public int Difficulty;
        public int RequiredValueCount;
        public int RequiredSuitCount;
        public bool IsAnti;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref CombinationType);
            serializer.SerializeValue(ref Difficulty);
            serializer.SerializeValue(ref RequiredValueCount);
            serializer.SerializeValue(ref RequiredSuitCount);
            serializer.SerializeValue(ref IsAnti);
        }

        public bool Equals(NetworkCombinationData other)
        {
            return CombinationType == other.CombinationType &&
                   Difficulty == other.Difficulty &&
                   RequiredValueCount == other.RequiredValueCount &&
                   RequiredSuitCount == other.RequiredSuitCount &&
                   IsAnti == other.IsAnti;
        }
    }

    public sealed class NetworkRoundSync : NetworkBehaviour
    {
        [SerializeField] private GameManager gameManager;

        // Round combinations - synced at round start
        private NetworkList<NetworkCombinationData> _roundCombinations;
        
        // Scores per team
        private NetworkList<int> _teamScores;
        
        // Round state
        private NetworkVariable<int> _roundNumber = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> _roundActive = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public event Action<RoundCombinationSet> OnCombinationsReceived;
        public event Action<Dictionary<int, int>> OnScoresChanged;
        public event Action<int> OnRoundStarted;
        public event Action<RoundResult> OnRoundEnded;

        public int RoundNumber => _roundNumber.Value;
        public bool IsRoundActive => _roundActive.Value;

        private void Awake()
        {
            _roundCombinations = new NetworkList<NetworkCombinationData>();
            _teamScores = new NetworkList<int>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            _roundCombinations.OnListChanged += HandleCombinationsChanged;
            _teamScores.OnListChanged += HandleScoresChanged;
            _roundNumber.OnValueChanged += HandleRoundNumberChanged;
        }

        // Server: Set round combinations
        public void SetRoundCombinations(RoundCombinationSet combos)
        {
            if (!IsServer || combos == null) return;

            _roundCombinations.Clear();

            // Add scoring combinations
            foreach (var (combo, difficulty) in combos.GetScoringCombinations())
            {
                _roundCombinations.Add(SerializeCombination(combo, difficulty, false));
            }

            // Add anti-combination
            if (combos.antiCombination != null)
            {
                _roundCombinations.Add(SerializeCombination(
                    combos.antiCombination, 
                    CombinationDifficulty.Anti, 
                    true));
            }
        }

        // Server: Update scores
        public void UpdateScores(Dictionary<Team, int> scores)
        {
            if (!IsServer) return;

            _teamScores.Clear();
            foreach (var team in gameManager.Teams)
            {
                int score = scores.TryGetValue(team, out int s) ? s : 0;
                _teamScores.Add(score);
            }
        }

        // Server: Start new round
        public void StartRound(int roundNumber)
        {
            if (!IsServer) return;
            
            _roundNumber.Value = roundNumber;
            _roundActive.Value = true;
            
            // Reset scores
            _teamScores.Clear();
            for (int i = 0; i < gameManager.Teams.Count; i++)
            {
                _teamScores.Add(0);
            }
        }

        // Server: End round
        public void EndRound(RoundResult result)
        {
            if (!IsServer) return;
            
            _roundActive.Value = false;
            
            // Sync final scores
            UpdateScores(result.scores);
            
            // Broadcast result
            BroadcastRoundResultClientRpc(
                GetWinnerIndices(result.winners),
                GetScoreArray(result.scores)
            );
        }

        [ClientRpc]
        private void BroadcastRoundResultClientRpc(int[] winnerIndices, int[] scores)
        {
            Debug.Log($"[NetworkRoundSync] Round ended - Winners: {string.Join(", ", winnerIndices)}");
            
            var result = new RoundResult();
            for (int i = 0; i < scores.Length && i < gameManager.Teams.Count; i++)
            {
                result.scores[gameManager.Teams[i]] = scores[i];
            }
            
            result.winners = new List<Team>();
            foreach (int idx in winnerIndices)
            {
                if (idx >= 0 && idx < gameManager.Teams.Count)
                {
                    result.winners.Add(gameManager.Teams[idx]);
                }
            }
            
            OnRoundEnded?.Invoke(result);
        }

        // Event handlers
        private void HandleCombinationsChanged(NetworkListEvent<NetworkCombinationData> changeEvent)
        {
            // Reconstruct combination set on clients
            Debug.Log($"[NetworkRoundSync] Received {_roundCombinations.Count} combinations");
        }

        private void HandleScoresChanged(NetworkListEvent<int> changeEvent)
        {
            var scores = new Dictionary<int, int>();
            for (int i = 0; i < _teamScores.Count; i++)
            {
                scores[i] = _teamScores[i];
            }
            OnScoresChanged?.Invoke(scores);
        }

        private void HandleRoundNumberChanged(int oldValue, int newValue)
        {
            OnRoundStarted?.Invoke(newValue);
        }

        // Serialization helpers
        private NetworkCombinationData SerializeCombination(
            Combination combo, CombinationDifficulty difficulty, bool isAnti)
        {
            // Simplified - actual implementation depends on Combination structure
            return new NetworkCombinationData
            {
                CombinationType = 0, // Would need type mapping
                Difficulty = (int)difficulty,
                IsAnti = isAnti,
                RequiredValueCount = 0,
                RequiredSuitCount = 0
            };
        }

        private int[] GetWinnerIndices(List<Team> winners)
        {
            if (winners == null) return new int[0];
            
            var indices = new int[winners.Count];
            for (int i = 0; i < winners.Count; i++)
            {
                indices[i] = IndexOfTeam(gameManager.Teams, winners[i]);
            }
            return indices;
        }

        private static int IndexOfTeam(IReadOnlyList<Team> list, Team item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item)) return i;
            }
            return -1;
        }

        private int[] GetScoreArray(Dictionary<Team, int> scores)
        {
            var arr = new int[gameManager.Teams.Count];
            for (int i = 0; i < gameManager.Teams.Count; i++)
            {
                arr[i] = scores.TryGetValue(gameManager.Teams[i], out int s) ? s : 0;
            }
            return arr;
        }
    }
}
