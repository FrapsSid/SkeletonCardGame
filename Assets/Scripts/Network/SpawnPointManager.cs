using UnityEngine;
using System.Collections.Generic;

namespace Multiplayer
{
    public class SpawnPointManager : MonoBehaviour
    {
        public static SpawnPointManager Instance { get; private set; }
 
        private readonly List<Transform> _spawnPoints = new List<Transform>();
        private int _nextIndex;
 
        private void Awake()
        {
            Instance = this;
 
            var points = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            foreach (var p in points)
            {
                _spawnPoints.Add(p.transform);
            }
        }
 
        public Transform GetNextSpawnPoint()
        {
            if (_spawnPoints.Count == 0) return null;
 
            var point = _spawnPoints[_nextIndex % _spawnPoints.Count];
            _nextIndex++;
            return point;
        }
    }
}