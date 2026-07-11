using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 20-second turn timer. Attach next to GameManager.
/// GameManager subscribes to OnTurnTimerExpired and auto-folds / auto-ends.
/// </summary>
public sealed class TurnTimer : MonoBehaviour
{
    public const float DefaultTurnDuration = 20f;

    [SerializeField] private float turnDurationSeconds = DefaultTurnDuration;

    private Coroutine _timerCoroutine;
    private Skeleton _currentPlayer;

    public float TurnDurationSeconds
    {
        get => turnDurationSeconds;
        set => turnDurationSeconds = Mathf.Max(0f, value);
    }
    public float RemainingSeconds { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action<Skeleton, float> OnTurnTimerStarted;
    public event Action<float> OnTurnTimerTick;
    public event Action<Skeleton> OnTurnTimerExpired;

    public void StartTurnTimer(Skeleton player)
    {
        StopTurnTimer();
        _currentPlayer = player;
        RemainingSeconds = turnDurationSeconds;
        IsRunning = true;
        OnTurnTimerStarted?.Invoke(player, turnDurationSeconds);
        _timerCoroutine = StartCoroutine(RunTimer());
    }

    public void StopTurnTimer()
    {
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
        IsRunning = false;
        RemainingSeconds = 0f;
    }

    private IEnumerator RunTimer()
    {
        while (RemainingSeconds > 0f)
        {
            yield return null;
            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
            OnTurnTimerTick?.Invoke(RemainingSeconds);
        }
        _timerCoroutine = null;
        IsRunning = false;
        OnTurnTimerExpired?.Invoke(_currentPlayer);
    }
}
