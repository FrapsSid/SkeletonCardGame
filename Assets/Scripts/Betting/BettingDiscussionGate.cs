using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Player = Skeleton;

public class BettingDiscussionGate : MonoBehaviour {
    public const float DefaultDiscussionDurationSeconds = 30f;
    
    [SerializeField]
    private float discussionDurationSeconds = DefaultDiscussionDurationSeconds;

    public float DiscussionDurationSeconds {
        get => discussionDurationSeconds;
        set => discussionDurationSeconds = Mathf.Max(0f, value);
    }

    public BettingRound CurrentRound { get; private set; }
    public bool IsDiscussionActive { get; private set; }
    public bool ArePlayersSeated { get; private set; }

    public event Action<float> OnDiscussionStarted;
    public event Action<List<Player>> OnPlayersSeated;
    public event Action<BettingRound> OnBettingOpenedAfterDiscussion;

    private Coroutine discussionCoroutine;

    public void StartPostDealDiscussion(BettingRound bettingRound) {
        if (bettingRound == null) {
            throw new ArgumentNullException(nameof(bettingRound));
        }

        if (discussionCoroutine != null) {
            StopCoroutine(discussionCoroutine);
        }

        CurrentRound = bettingRound;
        CurrentRound.CloseBetting();
        IsDiscussionActive = true;
        ArePlayersSeated = false;

        OnDiscussionStarted?.Invoke(discussionDurationSeconds);
        discussionCoroutine = StartCoroutine(RunDiscussionTimer());
    }

    public void ForceSeatPlayersAndOpenBetting() {
        if (discussionCoroutine != null) {
            StopCoroutine(discussionCoroutine);
            discussionCoroutine = null;
        }

        CompleteDiscussionAndOpenBetting();
    }

    private IEnumerator RunDiscussionTimer() {
        if (discussionDurationSeconds > 0f) {
            yield return new WaitForSeconds(discussionDurationSeconds);
        }

        discussionCoroutine = null;
        CompleteDiscussionAndOpenBetting();
    }

    private void CompleteDiscussionAndOpenBetting() {
        if (CurrentRound == null) {
            return;
        }

        IsDiscussionActive = false;
        ArePlayersSeated = true;

        OnPlayersSeated?.Invoke(new List<Player>(CurrentRound.activePlayers));

        CurrentRound.OpenBetting();
        OnBettingOpenedAfterDiscussion?.Invoke(CurrentRound);
    }
}
