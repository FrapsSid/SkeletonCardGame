using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CardGameRound = Assets.Scripts.CardGame.CardGame.Round;
using Player = Skeleton;

public class BettingDiscussionGate : MonoBehaviour
{
    public const float DefaultDiscussionDurationSeconds = 30f;
    
    [SerializeField]
    private float discussionDurationSeconds = DefaultDiscussionDurationSeconds;

    public float DiscussionDurationSeconds
    {
        get => discussionDurationSeconds;
        set => discussionDurationSeconds = Mathf.Max(0f, value);
    }

    public CardGameRound CurrentRound { get; private set; }
    public bool IsDiscussionActive { get; private set; }
    public bool ArePlayersSeated { get; private set; }

    public event Action<float> OnDiscussionStarted;
    public event Action<List<Player>> OnPlayersSeated;
    public event Action<CardGameRound> OnBettingOpenedAfterDiscussion;

    private Coroutine discussionCoroutine;

    public void StartPostDealDiscussion(CardGameRound round)
    {
        if (round == null)
            throw new ArgumentNullException(nameof(round));

        if (discussionCoroutine != null)
            StopCoroutine(discussionCoroutine);

        CurrentRound = round;
        IsDiscussionActive = true;
        ArePlayersSeated = false;

        OnDiscussionStarted?.Invoke(discussionDurationSeconds);
        discussionCoroutine = StartCoroutine(RunDiscussionTimer());
    }

    public void ForceSeatPlayersAndOpenBetting()
    {
        if (discussionCoroutine != null)
        {
            StopCoroutine(discussionCoroutine);
            discussionCoroutine = null;
        }

        CompleteDiscussionAndOpenBetting();
    }

    private IEnumerator RunDiscussionTimer()
    {
        if (discussionDurationSeconds > 0f)
            yield return new WaitForSeconds(discussionDurationSeconds);

        discussionCoroutine = null;
        CompleteDiscussionAndOpenBetting();
    }

    private void CompleteDiscussionAndOpenBetting()
    {
        if (CurrentRound == null)
            return;

        IsDiscussionActive = false;
        ArePlayersSeated = true;

        OnPlayersSeated?.Invoke(CurrentRound.ActivePlayers.ToList());

        CurrentRound.StartBettingRound();
        OnBettingOpenedAfterDiscussion?.Invoke(CurrentRound);
    }
}
