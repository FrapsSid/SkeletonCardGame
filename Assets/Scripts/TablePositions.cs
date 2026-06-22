#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

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

    public Transform GetPlayerDealCardPosition(int playerIndex)
    {
        return GetCalculatedPosition(playerDealCardPositions, playerIndex, nameof(playerIndex));
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
        playerCount = Mathf.Max(MinimumPlayerCount, playerCount);
        int requiredCount = playerCount;
        EnsureMarkerCount(playerPositions, requiredCount, "Player Position");
        EnsureMarkerCount(playerDealCardPositions, requiredCount, "Player Deal Card Position");

        for (int i = 0; i < requiredCount; i++)
        {
            Vector3 direction = GetDirectionForPlayer(i, requiredCount);
            ApplyMarkerTransform(playerPositions[i], direction, playerPositionRadius);
            ApplyMarkerTransform(playerDealCardPositions[i], direction, dealPositionRadius);
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

    private static Vector3 GetDirectionForPlayer(int playerIndex, int playerCount)
    {
        float angle = (-90f + 360f * playerIndex / playerCount) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
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
}
