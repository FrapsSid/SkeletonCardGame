#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CardGame;

[RequireComponent(typeof(BettingDiscussionGate))]
public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private CardDealer? cardDealer;

    private readonly List<Team> _teams = new();
    private readonly List<Skeleton> _players = new();
    private BettingDiscussionGate _bettingDiscussionGate = null!;
    private Coroutine? _roundFlowCoroutine;
    private Coroutine? _tableDealCoroutine;
    private Coroutine? _takenCardDealCoroutine;
    private Coroutine? _restartRoundCoroutine;
    private int _activeCardDealerWaits;
    private bool _roundResolved;
    private bool _waitingForTableDealAnimation;
    private readonly MatchEndEvaluator _matchEndEvaluator = new();
    private MatchEndResult? _matchEndResult;

    public CardGame? CardGame { get; private set; }
    public Skeleton? LocalPlayer { get; private set; }
    public IReadOnlyList<Team> Teams => _teams;
    public IReadOnlyList<Skeleton> Players => _players;
    public bool IsCardDealInProgress => _activeCardDealerWaits > 0;
    public bool IsMatchEnded => _matchEndResult != null;
    public MatchEndResult? CurrentMatchEndResult => _matchEndResult;
    public event Action<CardGame>? OnGameCreated;
    public event Action? OnCardDealCompleted;
    public event Action<MatchEndResult>? OnMatchEnded;

    [SerializeField] private Multiplayer.NetworkGameState networkGameState;
    [SerializeField] private bool autoInstallParticipantHud = true;
    [SerializeField] private bool autoInstallWorldNameplates = true;
    [SerializeField] private bool autoInstallRoundStateHud = true;
    [SerializeField] private bool autoInstallMatchEndOverlay = true;
    private bool _isNetworkMode;

    private void Awake()
    {
        _bettingDiscussionGate = GetComponent<BettingDiscussionGate>() ?? throw new NullReferenceException(nameof(BettingDiscussionGate));
        cardDealer ??= GetComponent<CardDealer>();

// ISSUE 20 DEBUG
#if UNITY_EDITOR
        if (GetComponent<ViolationGameBridge>() == null)
            gameObject.AddComponent<ViolationGameBridge>();
#endif
    }

    private void OnDestroy()
    {
        Unsubscribe();
        UnsubscribeFromDiscussionGate();
        StopRoundFlow();
        StopTableDeal();
        StopTakenCardDeal();
        StopRestartRound();
        _bettingDiscussionGate.StopDiscussion();
        if (networkGameState != null)
        {
            networkGameState.OnPhaseChanged -= HandleNetworkPhaseChanged;
        }
    }

    public void StartGame(IEnumerable<Team> gameTeams, IEnumerable<Skeleton> gamePlayers, Skeleton localPlayer)
    {
        if (gameTeams == null)
            throw new ArgumentNullException(nameof(gameTeams));
        if (gamePlayers == null)
            throw new ArgumentNullException(nameof(gamePlayers));

        _teams.Clear();
        _players.Clear();
        _teams.AddRange(gameTeams);
        _players.AddRange(gamePlayers);
        LocalPlayer = localPlayer;

        StartGame();
    }

    public void StartGame()
    {
        if (_teams.Count == 0 || _players.Count == 0)
            throw new InvalidOperationException("GameManager needs teams and players before starting a game.");
        if (LocalPlayer == null || !_players.Contains(LocalPlayer))
            throw new InvalidOperationException("GameManager needs a local player from the player list before starting a game.");

        Unsubscribe();
        UnsubscribeFromDiscussionGate();
        StopRoundFlow();
        StopTableDeal();
        StopTakenCardDeal();
        StopRestartRound();

        SubscribeToDiscussionGate();
        _roundResolved = false;
        _waitingForTableDealAnimation = false;
        _matchEndResult = null;

        CardGame = new CardGame(_teams, _players);
        Subscribe(CardGame);
        EnsurePlayerPresentationUi();
        OnGameCreated?.Invoke(CardGame);
        if (_isNetworkMode && !IsServer())
        {
            PrepareCardDealerForRound();
            return;
        }
        StartRoundFlow(CardGame);
    }

    private void StartRoundFlow(CardGame game)
    {
        StopRoundFlow();
        _roundFlowCoroutine = StartCoroutine(StartRoundFlowRoutine(game));
    }

    private IEnumerator StartRoundFlowRoutine(CardGame game)
    {
        PrepareCardDealerForRound();

        game.DealPlayersCards();
        yield return DealInitialPlayerCards();

        if (CardGame != game || game.phase != GamePhase.ShowingCombinations)
        {
            _roundFlowCoroutine = null;
            yield break;
        }

        game.ShowCombinations();
        game.StartRound();
        _roundFlowCoroutine = null;
    }

    private void Subscribe(CardGame game)
    {
        game.OnPhaseChanged += HandlePhaseChanged;
        game.OnRoundEnded += HandleRoundEnded;
        game.OnTableCardsDealt += HandleTableCardsDealt;
        game.OnCardTaken += HandleCardTaken;
        game.OnBettingRoundEnded += HandleBettingRoundEnded;
        game.OnTurnStarted += HandleTurnStarted;
    }

    private void Unsubscribe()
    {
        if (CardGame == null)
            return;

        CardGame.OnPhaseChanged -= HandlePhaseChanged;
        CardGame.OnRoundEnded -= HandleRoundEnded;
        CardGame.OnTableCardsDealt -= HandleTableCardsDealt;
        CardGame.OnCardTaken -= HandleCardTaken;
        CardGame.OnBettingRoundEnded -= HandleBettingRoundEnded;
        CardGame.OnTurnStarted -= HandleTurnStarted;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (_isNetworkMode && !IsServer())
        {
            return;
        }
        if (_isNetworkMode && networkGameState != null)
        {
            networkGameState.SetPhase(phase);
            if (phase != GamePhase.Betting)
                networkGameState.ClearCurrentTurn();
        }

        CardGame? game = CardGame;
        if (game?.round == null)
            return;

        if (phase == GamePhase.BettingRoundStart)
        {
            if (_waitingForTableDealAnimation)
                return;

            StartBettingDiscussion(game.round);
        }
        else if (phase == GamePhase.AddingCards)
        {
            _waitingForTableDealAnimation = true;
            game.DealTableCards();
        }
        else if (phase == GamePhase.End && !_roundResolved)
        {
            _roundResolved = true;
            game.round.DetermineWinners();
            game.round.ResolvePot();
        }
    }
    private void HandleRoundEnded(RoundResult result)
    {
        ClearNetworkCurrentTurn();
        ClearHeldCardItems();
        StopRestartRound();

        MatchEndResult? matchEndResult = _matchEndEvaluator.Evaluate(_teams);
        if (matchEndResult != null && matchEndResult.HasWinner)
        {
            CompleteMatch(matchEndResult);
            return;
        }

        _restartRoundCoroutine = StartCoroutine(RestartRoundAfterRoundEnded());
    }

    private void HandleBettingRoundEnded(Round round)
    {
        ClearNetworkCurrentTurn();
    }

    private void HandleTurnStarted(Skeleton player)
    {
        PublishNetworkCurrentTurn(player);
    }

    private void ClearHeldCardItems()
    {
        foreach (Skeleton player in _players)
        {
            PlayerInventoryOwner inventoryOwner = player.InventoryOwner;
            if (inventoryOwner == null)
                continue;

            ClearHeldCardItem(inventoryOwner.leftHand);
            ClearHeldCardItem(inventoryOwner.rightHand);
        }
    }

    private static void ClearHeldCardItem(PlayerHand hand)
    {
        if (hand != null && hand.Item is CardsItem)
        {
            hand.SetItem(null);
        }
    }

    private void HandleTableCardsDealt(IReadOnlyList<CardData> cards)
    {
        StopTableDeal(false);
        _tableDealCoroutine = StartCoroutine(DealTableCardsThenStartDiscussion(cards));
    }

    private void HandleCardTaken(Skeleton player, CardData card)
    {
        if (cardDealer == null)
            return;

        StopTakenCardDeal();
        _takenCardDealCoroutine = StartCoroutine(DealTakenCard(player, card));
    }

    private IEnumerator RestartRoundAfterRoundEnded()
    {
        yield return null;
        _restartRoundCoroutine = null;

        CardGame? game = CardGame;
        if (game == null || game.phase != GamePhase.End)
            yield break;
        if (IsMatchEnded)
            yield break;

        game.ResetRound();
        _roundResolved = false;
        StartRoundFlow(game);
    }

    private void StartBettingDiscussion(Round round)
    {
        _bettingDiscussionGate.StopDiscussion();
        _bettingDiscussionGate.StartPostDealDiscussion(round);
    }

    private void HandleDiscussionCompleted(Round round)
    {
        CardGame? game = CardGame;
        if (game?.round != round || game.phase != GamePhase.BettingRoundStart)
            return;

        game.StartBettingRound();
    }

    private void SubscribeToDiscussionGate()
    {
        _bettingDiscussionGate.OnDiscussionCompleted += HandleDiscussionCompleted;
    }

    private void UnsubscribeFromDiscussionGate()
    {
        _bettingDiscussionGate.OnDiscussionCompleted -= HandleDiscussionCompleted;
    }

    private void StopRestartRound()
    {
        if (_restartRoundCoroutine == null)
            return;

        StopCoroutine(_restartRoundCoroutine);
        _restartRoundCoroutine = null;
    }

    private void CompleteMatch(MatchEndResult result)
    {
        if (result == null || _matchEndResult != null)
            return;

        _matchEndResult = result;
        StopRoundFlow();
        StopTableDeal();
        StopTakenCardDeal();
        StopRestartRound();
        _bettingDiscussionGate.StopDiscussion();
        ClearNetworkCurrentTurn();
        OnMatchEnded?.Invoke(result);
    }

    private void PrepareCardDealerForRound()
    {
        if (cardDealer == null)
            return;

        cardDealer.SetPlayers(_players);
        cardDealer.ClearTable();
        cardDealer.ClearPlayerCards();
    }

    private IEnumerator DealInitialPlayerCards()
    {
        CardDealer? dealer = cardDealer;
        if (dealer == null)
            yield break;

        yield return WaitForDealer(dealer, () => dealer.DealCardsToPlayers(_players, 2));
    }

    private IEnumerator DealTableCardsThenStartDiscussion(IReadOnlyList<CardData> cards)
    {
        CardDealer? dealer = cardDealer;
        if (dealer != null)
        {
            yield return WaitForDealer(dealer, () => dealer.DealCardsToTable(cards));
        }

        _waitingForTableDealAnimation = false;
        _tableDealCoroutine = null;

        CardGame? game = CardGame;
        if (game?.round == null || game.phase != GamePhase.BettingRoundStart)
            yield break;

        StartBettingDiscussion(game.round);
    }

    private IEnumerator DealTakenCard(Skeleton player, CardData card)
    {
        CardDealer? dealer = cardDealer;
        if (dealer != null)
        {
            yield return WaitForDealer(dealer, () => dealer.DealCardToPlayer(player, card));
        }

        _takenCardDealCoroutine = null;
    }

    private IEnumerator WaitForDealer(CardDealer dealer, Action startDeal)
    {
        bool completed = false;

        void HandleDealCompleted()
        {
            completed = true;
        }

        _activeCardDealerWaits++;
        dealer.OnDealCompleted += HandleDealCompleted;
        try
        {
            startDeal();
            while (!completed)
            {
                yield return null;
            }
        }
        finally
        {
            dealer.OnDealCompleted -= HandleDealCompleted;
            _activeCardDealerWaits = Math.Max(0, _activeCardDealerWaits - 1);
            if (_activeCardDealerWaits == 0)
                OnCardDealCompleted?.Invoke();
        }
    }

    private void StopRoundFlow()
    {
        if (_roundFlowCoroutine == null)
            return;

        StopCoroutine(_roundFlowCoroutine);
        _roundFlowCoroutine = null;
    }

    private void StopTableDeal(bool resetWaiting = true)
    {
        if (_tableDealCoroutine == null)
        {
            if (resetWaiting)
                _waitingForTableDealAnimation = false;

            return;
        }

        StopCoroutine(_tableDealCoroutine);
        _tableDealCoroutine = null;
        if (resetWaiting)
            _waitingForTableDealAnimation = false;
    }

    private void StopTakenCardDeal()
    {
        if (_takenCardDealCoroutine == null)
            return;

        StopCoroutine(_takenCardDealCoroutine);
        _takenCardDealCoroutine = null;
    }

// <Сетевая часть
    private void EnsurePlayerPresentationUi()
    {
        PlayerPresentationRegistry.EnsureDefaultFor(this);

        if (Application.isBatchMode)
            return;

        if (autoInstallParticipantHud)
            ParticipantsHudUI.EnsureDefaultFor(this);

        if (autoInstallWorldNameplates)
            PlayerWorldNameplateManager.EnsureDefaultFor(this);

        if (autoInstallRoundStateHud)
            RoundStateHudUI.EnsureDefaultFor(this);

        if (autoInstallMatchEndOverlay)
            MatchEndOverlayUI.EnsureDefaultFor(this);
    }

    private void PublishNetworkCurrentTurn(Skeleton player)
    {
        if (!_isNetworkMode || networkGameState == null || !IsServer() || player == null)
            return;

        int playerIndex = _players.IndexOf(player);
        ulong clientId = player.HasNetworkClientId
            ? player.NetworkClientId
            : Multiplayer.NetworkGameState.NoCurrentTurnClientId;

        networkGameState.SetCurrentTurn(playerIndex, clientId);
    }

    private void ClearNetworkCurrentTurn()
    {
        if (!_isNetworkMode || networkGameState == null || !IsServer())
            return;

        networkGameState.ClearCurrentTurn();
    }

    public void EnableNetworkMode(Multiplayer.NetworkGameState gameState)
    {
        _isNetworkMode = true;
        networkGameState = gameState;
        
        if (IsNetworkClient())
        {
            networkGameState.OnPhaseChanged += HandleNetworkPhaseChanged;
        }
    }

    private bool IsNetworkClient()
    {
        return networkGameState != null && networkGameState.IsSpawned;
    }

    private void HandleNetworkPhaseChanged(CardGame.GamePhase newPhase)
    {
        if (CardGame == null) return;
        
        if (!IsServer())
        {
            Debug.Log($"[GameManager] Network phase changed to {newPhase}");
        }
    }

    private bool IsServer()
    {
        return networkGameState != null && networkGameState.IsServer;
    }
// Сетевая часть>
}
