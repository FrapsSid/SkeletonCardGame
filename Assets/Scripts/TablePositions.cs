#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class TablePositions : MonoBehaviour
{
    private const int TableCardPositionCount = 5;
    private const int MinimumPlayerCount = 1;

    [Header("Table Cards")]
    [SerializeField] private Transform?[] tableCardPositions = new Transform?[TableCardPositionCount];

    [Header("Players")]
    [SerializeField, Min(MinimumPlayerCount)] private int playerCount = 2;
    [SerializeField, Min(0f)] private float playerPositionRadius = 3f;
    [SerializeField, Min(0f)] private float dealPositionRadius = 1.25f;
    [SerializeField] private float heightOffset;

    [SerializeField, HideInInspector] private List<Transform> playerPositions = new();
    [SerializeField, HideInInspector] private List<Transform> playerDealCardPositions = new();

    [Header("Gizmos")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private float gizmoSize = 0.2f;
    [SerializeField] private Color playerPositionColor = new(0.2f, 0.8f, 0.2f, 0.5f);
    [SerializeField] private Color dealCardPositionColor = new(0.2f, 0.5f, 0.9f, 0.5f);
    [SerializeField] private Color tableCardPositionColor = new(0.9f, 0.8f, 0.2f, 0.5f);

    private readonly Dictionary<Skeleton, int> _playerIndices = new();

    public IReadOnlyList<Transform?> TableCardPositions => tableCardPositions;
    public int PlayerCount
    {
        get => playerCount;
        set
        {
            int normalizedValue = Mathf.Max(MinimumPlayerCount, value);
            if (playerCount == normalizedValue)
            {
                return;
            }

            playerCount = normalizedValue;
            RebuildCalculatedPositions();
        }
    }

    public IReadOnlyList<Transform> PlayerPositions => playerPositions;

    public IReadOnlyList<Transform> PlayerDealCardPositions => playerDealCardPositions;

    private bool _isRebuilding;

    private void OnEnable()
    {
        EnsureTableCardPositionCount();
        RebuildCalculatedPositions();
    }

    private void OnValidate()
    {
        EnsureTableCardPositionCount();
        playerCount = Mathf.Max(MinimumPlayerCount, playerCount);
        playerPositionRadius = Mathf.Max(0f, playerPositionRadius);
        dealPositionRadius = Mathf.Max(0f, dealPositionRadius);
        RebuildCalculatedPositions();
    }

    public Transform GetTableCardPosition(int index)
    {
        if (index < 0 || index >= TableCardPositionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Table card position index must be between 0 and {TableCardPositionCount - 1}.");
        }

        Transform? position = tableCardPositions[index];
        if (position == null)
        {
            throw new InvalidOperationException($"Table card position {index + 1} is not assigned.");
        }

        return position;
    }

    public Transform GetPlayerPosition(int playerIndex)
    {
        return GetCalculatedPosition(playerPositions, playerIndex, nameof(playerIndex));
    }

    public Transform GetPlayerPosition(Skeleton player)
    {
        return GetPlayerPosition(GetPlayerIndex(player));
    }

    public Transform GetPlayerDealCardPosition(int playerIndex)
    {
        return GetCalculatedPosition(playerDealCardPositions, playerIndex, nameof(playerIndex));
    }

    public Transform GetPlayerDealCardPosition(Skeleton player)
    {
        return GetPlayerDealCardPosition(GetPlayerIndex(player));
    }

    public bool TryGetTableCardPosition(int index, out Transform? position)
    {
        if (index < 0 || index >= TableCardPositionCount)
        {
            position = null;
            return false;
        }

        position = tableCardPositions[index];
        return position != null;
    }

    public void SetPlayers(IReadOnlyList<Skeleton> players)
    {
        if (players == null)
        {
            throw new ArgumentNullException(nameof(players));
        }

        PlayerCount = players.Count;
        _playerIndices.Clear();

        for (int i = 0; i < players.Count; i++)
        {
            Skeleton player = players[i];
            if (player == null)
            {
                throw new ArgumentException("Player list cannot contain null players.", nameof(players));
            }

            if (!_playerIndices.TryAdd(player, i))
            {
                throw new ArgumentException("Player list cannot contain the same player twice.", nameof(players));
            }
        }
    }

    public void ClearPlayers()
    {
        _playerIndices.Clear();
    }

    public bool TryGetPlayerIndex(Skeleton player, out int playerIndex)
    {
        if (player == null)
        {
            playerIndex = -1;
            return false;
        }

        return _playerIndices.TryGetValue(player, out playerIndex);
    }

    public int GetPlayerIndex(Skeleton player)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (!_playerIndices.TryGetValue(player, out int playerIndex))
        {
            throw new InvalidOperationException("Player is not assigned to a table position.");
        }

        return playerIndex;
    }

    private Transform GetCalculatedPosition(List<Transform> positions, int index, string parameterName)
    {
        if (index < 0 || index >= positions.Count)
        {
            throw new ArgumentOutOfRangeException(parameterName, index, $"Player index must be between 0 and {positions.Count - 1}.");
        }

        return positions[index];
    }

    private void RebuildCalculatedPositions()
    {
        if (_isRebuilding)
            return;

        _isRebuilding = true;
        try
        {
            playerCount = Mathf.Max(MinimumPlayerCount, playerCount);
            int requiredCount = playerCount;
            EnsureMarkerCount(playerPositions, requiredCount, "Player Position");
            EnsureMarkerCount(playerDealCardPositions, requiredCount, "Player Deal Card Position");

            for (int i = 0; i < requiredCount; i++)
            {
                Vector3 direction = GetDirectionForPlayer(i, requiredCount);
                ApplyMarkerTransform(playerPositions[i], direction, playerPositionRadius);
                ApplyDealCardMarkerTransform(playerDealCardPositions[i], direction, dealPositionRadius);
            }
        }
        finally
        {
            _isRebuilding = false;
        }
    }

    private void EnsureTableCardPositionCount()
    {
        if (tableCardPositions.Length == TableCardPositionCount)
        {
            return;
        }

        Array.Resize(ref tableCardPositions, TableCardPositionCount);
    }

    private void EnsureMarkerCount(List<Transform> markers, int requiredCount, string markerName)
    {
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            if (markers[i] == null)
            {
                markers.RemoveAt(i);
            }
        }

        while (markers.Count < requiredCount)
        {
            markers.Add(CreateMarker($"{markerName} {markers.Count + 1}"));
        }

        for (int i = 0; i < markers.Count; i++)
        {
            markers[i].name = $"{markerName} {i + 1}";
            markers[i].SetParent(transform, true);
        }

        for (int i = markers.Count - 1; i >= requiredCount; i--)
        {
            DestroyGeneratedObject(markers[i].gameObject);
            markers.RemoveAt(i);
        }
    }

    private Transform CreateMarker(string markerName)
    {
        GameObject marker = new GameObject(markerName);
        marker.transform.SetParent(transform, false);
        return marker.transform;
    }

    private void ApplyMarkerTransform(Transform marker, Vector3 direction, float radius)
    {
        marker.localPosition = direction * radius + Vector3.up * heightOffset;
        marker.localRotation = Quaternion.LookRotation(-direction, Vector3.up);
    }

    private void ApplyDealCardMarkerTransform(Transform marker, Vector3 direction, float radius)
    {
        marker.localPosition = direction * radius + Vector3.up * heightOffset;
        marker.localRotation = Quaternion.Euler(90f, 0f, GetDealCardRotationZ(direction));
    }

    private static float GetDealCardRotationZ(Vector3 direction)
    {
        float playerAngle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
        return playerAngle + 90f;
    }

    private static Vector3 GetDirectionForPlayer(int playerIndex, int playerCount)
    {
        float angle = (-90f + 360f * playerIndex / playerCount) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    [ContextMenu("Snap Table Card Positions To Surface")]
    private void SnapTableCardPositionsToSurfaceFromContextMenu()
    {
        EnsureTableCardPositionCount();
        SnapTableCardPositionsToSurface();
    }
    private void SnapTableCardPositionsToSurface()
    {
        for (int i = 0; i < tableCardPositions.Length; i++)
        {
            Transform? marker = tableCardPositions[i];
            if (marker == null)
            {
                continue;
            }

            if (TryGetSurfacePoint(marker.position, out Vector3 surfacePoint))
            {
                marker.position = surfacePoint;
            }
        }
    }

    private bool TryGetSurfacePoint(Vector3 worldPosition, out Vector3 surfacePoint)
    {
        surfacePoint = default;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            return false;
        }

        Ray ray = new Ray(worldPosition + Vector3.up * 10f, Vector3.down);
        float bestDistance = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                continue;
            }

            if (!collider.Raycast(ray, out RaycastHit hit, 20f))
            {
                continue;
            }

            if (hit.distance >= bestDistance)
            {
                continue;
            }

            bestDistance = hit.distance;
            surfacePoint = hit.point;
            found = true;
        }

        return found;
    }

    private static void DestroyGeneratedObject(UnityEngine.Object? target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Позиции игроков — зелёные сферы
        Gizmos.color = playerPositionColor;
        for (int i = 0; i < playerPositions.Count; i++)
        {
            Transform? pos = playerPositions[i];
            if (pos != null)
            {
                Gizmos.DrawSphere(pos.position, gizmoSize);
                Gizmos.DrawWireSphere(pos.position, gizmoSize);
            }
        }

        // Позиции раздачи карт — синие кубы
        Gizmos.color = dealCardPositionColor;
        for (int i = 0; i < playerDealCardPositions.Count; i++)
        {
            Transform? pos = playerDealCardPositions[i];
            if (pos != null)
            {
                Vector3 size = Vector3.one * gizmoSize * 1.4f;
                Gizmos.DrawCube(pos.position, size);
                Gizmos.DrawWireCube(pos.position, size);
            }
        }

        // Общие карты на столе — жёлтые кубы
        Gizmos.color = tableCardPositionColor;
        for (int i = 0; i < tableCardPositions.Length; i++)
        {
            Transform? pos = tableCardPositions[i];
            if (pos != null)
            {
                Vector3 size = Vector3.one * gizmoSize * 1.2f;
                Gizmos.DrawCube(pos.position, size);
                Gizmos.DrawWireCube(pos.position, size);
            }
        }

        // Линии от игрока к его позиции карт
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        for (int i = 0; i < playerPositions.Count && i < playerDealCardPositions.Count; i++)
        {
            Transform? player = playerPositions[i];
            Transform? deal = playerDealCardPositions[i];
            if (player != null && deal != null)
            {
                Gizmos.DrawLine(player.position, deal.position);
            }
        }
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        GUIStyle style = new(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter
        };

        // Подписи позиций игроков
        style.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
        for (int i = 0; i < playerPositions.Count; i++)
        {
            Transform? pos = playerPositions[i];
            if (pos != null)
            {
                Handles.Label(
                    pos.position + Vector3.up * (gizmoSize + 0.15f),
                    $"Player {i + 1}", style);
            }
        }

        // Подписи позиций раздачи
        style.normal.textColor = new Color(0.2f, 0.5f, 0.9f);
        for (int i = 0; i < playerDealCardPositions.Count; i++)
        {
            Transform? pos = playerDealCardPositions[i];
            if (pos != null)
            {
                Handles.Label(
                    pos.position + Vector3.up * (gizmoSize + 0.15f),
                    $"Deal {i + 1}", style);
            }
        }

        // Подписи общих карт
        style.normal.textColor = new Color(0.8f, 0.7f, 0.2f);
        for (int i = 0; i < tableCardPositions.Length; i++)
        {
            Transform? pos = tableCardPositions[i];
            if (pos != null)
            {
                Handles.Label(
                    pos.position + Vector3.up * (gizmoSize + 0.15f),
                    $"Table {i + 1}", style);
            }
        }
    }
    #endif
}
