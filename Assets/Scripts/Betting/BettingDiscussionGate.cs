#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CardGameRound = CardGame.Round;
using Player = Skeleton;

public sealed class BettingDiscussionGate : MonoBehaviour
{
    public const float DefaultDiscussionDurationSeconds = 30f;

    [SerializeField]
    private float discussionDurationSeconds = DefaultDiscussionDurationSeconds;

    private Coroutine? discussionCoroutine;

    public float DiscussionDurationSeconds
    {
        get => discussionDurationSeconds;
        set => discussionDurationSeconds = Mathf.Max(0f, value);
    }

    public CardGameRound? CurrentRound { get; private set; }
    public bool IsDiscussionActive { get; private set; }
    public float DiscussionRemainingSeconds { get; private set; }
    public bool ArePlayersSeated { get; private set; }

    public event Action<float>? OnDiscussionStarted;
    public event Action<float>? OnDiscussionTimerChanged;
    public event Action<List<Player>>? OnPlayersSeated;
    public event Action<CardGameRound>? OnDiscussionCompleted;

    public void StartPostDealDiscussion(CardGameRound round)
    {
        if (round == null)
            throw new ArgumentNullException(nameof(round));

        StopDiscussionTimer();

        CurrentRound = round;
        IsDiscussionActive = true;
        ArePlayersSeated = false;
        SetDiscussionRemaining(DiscussionDurationSeconds);

        OnDiscussionStarted?.Invoke(DiscussionDurationSeconds);
        discussionCoroutine = StartCoroutine(RunDiscussionTimer());
    }

    public void ForceSeatPlayersAndOpenBetting()
    {
        ForceCompleteDiscussion();
    }

    public void ForceCompleteDiscussion()
    {
        StopDiscussionTimer();
        CompleteDiscussion();
    }

    public void StopDiscussion()
    {
        StopDiscussionTimer();
        CurrentRound = null;
        IsDiscussionActive = false;
        SetDiscussionRemaining(0f);
        ArePlayersSeated = false;
    }

    private IEnumerator RunDiscussionTimer()
    {
        while (DiscussionRemainingSeconds > 0f)
        {
            yield return null;
            SetDiscussionRemaining(DiscussionRemainingSeconds - Time.deltaTime);
        }

        discussionCoroutine = null;
        CompleteDiscussion();
    }

    private void StopDiscussionTimer()
    {
        if (discussionCoroutine == null)
            return;

        StopCoroutine(discussionCoroutine);
        discussionCoroutine = null;
    }

    private void CompleteDiscussion()
    {
        if (CurrentRound == null || !IsDiscussionActive)
            return;

        IsDiscussionActive = false;
        ArePlayersSeated = true;
        SetDiscussionRemaining(0f);

        OnPlayersSeated?.Invoke(CurrentRound.ActivePlayers.ToList());
        OnDiscussionCompleted?.Invoke(CurrentRound);
    }

    private void SetDiscussionRemaining(float remainingSeconds)
    {
        float clamped = Mathf.Max(0f, remainingSeconds);
        if (Mathf.Approximately(DiscussionRemainingSeconds, clamped))
            return;

        DiscussionRemainingSeconds = clamped;
        OnDiscussionTimerChanged?.Invoke(DiscussionRemainingSeconds);
    }
}
