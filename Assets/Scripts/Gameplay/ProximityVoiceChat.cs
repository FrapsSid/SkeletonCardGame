using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Calculates per-player voice volume based on distance.
/// Hook into Vivox / Photon Voice / Dissonance to set volumes.
/// </summary>
public sealed class ProximityVoiceChat : MonoBehaviour
{
    [SerializeField] private float hearingDistance = 3f;
    [SerializeField] private float whisperDistance = 1f;
    [SerializeField] private AnimationCurve volumeFalloff =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly Dictionary<Skeleton, float> _playerVolumes = new();

    public float GetVolumeForPlayer(Skeleton listener, Skeleton speaker)
    {
        if (listener == null || speaker == null) return 0f;
        if (listener == speaker) return 1f;

        if (listener.IsGhost) return 1f;
        if (listener.Body == null || speaker.Body == null) return 0f;

        float distance = Vector3.Distance(
            listener.Body.transform.position,
            speaker.Body.transform.position);

        if (distance > hearingDistance) return 0f;
        if (distance <= whisperDistance) return 1f;

        float t = (distance - whisperDistance) /
                  (hearingDistance - whisperDistance);
        return volumeFalloff.Evaluate(1f - t);
    }

    public void UpdateVoiceVolumes(
        IReadOnlyList<Skeleton> players, Skeleton localPlayer)
    {
        foreach (var player in players)
        {
            if (player == localPlayer) continue;
            float volume = GetVolumeForPlayer(localPlayer, player);
            _playerVolumes[player] = volume;
        }
    }
}
