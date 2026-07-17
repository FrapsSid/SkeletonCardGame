using UnityEngine;
using System.Collections.Generic;

namespace Multiplayer
{
    public class SpawnPointManager : MonoBehaviour
    {
        public static SpawnPointManager Instance { get; private set; }

        [SerializeField] private Transform[] playerSpawnPoints;

        private int _nextIndex;

        private void Awake()
        {
            Instance = this;

            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            {
                var points = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
                var list = new List<Transform>();
                foreach (var p in points)
                {
                    if (p.gameObject.name.StartsWith("Spawn"))
                        list.Add(p.transform);
                }
                playerSpawnPoints = list.ToArray();
            }
        }

        public Transform GetNextSpawnPoint()
        {
            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0) return null;

            var point = playerSpawnPoints[_nextIndex % playerSpawnPoints.Length];
            _nextIndex++;
            return point;
        }
    }
}