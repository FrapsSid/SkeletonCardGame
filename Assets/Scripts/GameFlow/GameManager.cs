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
    private int _roundGeneration;

    public CardGame? CardGame { get; private set; }
    public Skeleton? LocalPlayer { get; private set; }
    public IReadOnlyList<Team> Teams => _teams;
    public IReadOnlyList<Skeleton> Players => _players;
    public bool IsCardDealInProgress => _activeCardDealerWaits > 0;
    public bool IsMatchEnded => _matchEndResult != null;
    public MatchEndResult? CurrentMatchEndResult => _matchEndResult;
    public bool IsNetworkMode => _isNetworkMode;
    public int RoundGeneration => _roundGeneration;
    public event Action<CardGame>? OnGameCreated;
    public event Action? OnCardDealCompleted;
    public event Action<MatchEndResult>? OnMatchEnded;
    public event Action? OnRoundReset;

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
        ResolveActiveCardDealer();

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
            networkGameState.OnCurrentTurnChanged -= HandleNetworkTurnChanged;
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
        ResolveActiveCardDealer();

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

        ProcessPhaseLogic(phase);
    }

    private void ProcessPhaseLogic(GamePhase phase)
    {
        CardGame? game = CardGame;
        if (game == null)
            return;

    if (phase == GamePhase.DealingCards)
    {
        _roundResolved = false;
        _waitingForTableDealAnimation = false;
        Debug.Log($"[GameManager] ProcessClientPhase(DealingCards): IsServer={IsServer()}, players={_players.Count}, cardDealer={cardDealer != null}");
        if (!IsServer() && _players.Count > 0)
        {
            foreach (var player in _players)
                player.Hand.Clear();
            game.ResetRoundForClient();
            StopTableDeal(false);
            StopTakenCardDeal();
            if (cardDealer != null)
            {
                cardDealer.ClearPlayerCards();
                cardDealer.ClearTable();
                Debug.Log("[GameManager] Client: cleared hands, table stacks, and table cards");
            }
            _roundGeneration++;
        }
        return;
    }

        if (!IsServer())
        {
            ProcessClientPhase(phase, game);
            return;
        }

        if (game.round == null)
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
            Debug.Log($"[GameManager] ProcessPhaseLogic(End): IsServer={IsServer()}, round={game.round != null}");
            if (game.round != null)
            {
                game.round.DetermineWinners();
                game.round.ResolvePot();
            }
        }
    }

    private void ProcessClientPhase(GamePhase phase, CardGame game)
    {
        if (phase == GamePhase.ShowingCombinations)
        {
            if (game.round == null)
                game.ShowCombinations();
        }
        else if (phase == GamePhase.RoundStart)
        {
            if (game.round != null && game.phase == GamePhase.RoundStart)
                game.StartRound();
        }
        else if (phase == GamePhase.BettingRoundStart)
        {
            if (_waitingForTableDealAnimation)
                return;
            if (game.round == null)
                game.ShowCombinations();
            if (game.phase == GamePhase.RoundStart)
                game.StartRound();
            else if (game.round != null)
                game.NotifyRoundStarted();
            StartBettingDiscussion(game.round);
        }
        else if (phase == GamePhase.Betting)
        {
            _bettingDiscussionGate.StopDiscussion();
            if (game.round != null && game.phase == GamePhase.BettingRoundStart)
                game.StartBettingRound();
        }
        else if (phase == GamePhase.AddingCards)
        {
            _waitingForTableDealAnimation = true;
        }
        else if (phase == GamePhase.End && !_roundResolved)
        {
            _roundResolved = true;
            Debug.Log($"[GameManager] ProcessClientPhase(End): round={game.round != null}");
            // Client does NOT call DetermineWinners() or ResolvePot()
            // Server will send body part removals and match end via RPC
        }
    }
    private void HandleRoundEnded(RoundResult result)
    {
        // global::Audio.AudioHandler.PlayEvent(global::Audio.SoundEvent.RoundEnd);
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
        // global::Audio.AudioHandler.PlayEvent(global::Audio.SoundEvent.TurnChange);
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
        Debug.Log($"[GameManager] HandleTableCardsDealt called with {cards.Count} cards, cardDealer={cardDealer != null}");
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
        _roundGeneration++;
        OnRoundReset?.Invoke();
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

    public void ClientCompleteMatch(int winningTeamIndex)
    {
        if (IsServer()) return;

        Team winner = winningTeamIndex >= 0 && winningTeamIndex < _teams.Count
            ? _teams[winningTeamIndex]
            : null;
        var result = new MatchEndResult(winner, new List<Team>(), new List<Team>());
        CompleteMatch(result);
    }

    private void PrepareCardDealerForRound()
    {
        ResolveActiveCardDealer();

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

    private void ResolveActiveCardDealer()
    {
        if (IsUsableCardDealer(cardDealer))
            return;

        CardDealer? activeDealer = FindFirstObjectByType<CardDealer>();
        if (IsUsableCardDealer(activeDealer))
        {
            cardDealer = activeDealer;
            return;
        }

        CardDealer[] allDealers = FindObjectsByType<CardDealer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allDealers.Length; i++)
        {
            CardDealer dealer = allDealers[i];
            if (IsUsableCardDealer(dealer))
            {
                cardDealer = dealer;
                return;
            }
        }
    }

    private static bool IsUsableCardDealer(CardDealer? dealer)
    {
        return dealer != null && dealer.isActiveAndEnabled && dealer.gameObject.activeInHierarchy;
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

        // Also send explicit sync RPC for reliability
        var sync = FindFirstObjectByType<NetworkCardDealerSync>();
        if (sync != null)
        {
            sync.SyncTurnClientRpc(playerIndex, clientId);
        }
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
        networkGameState.OnPhaseChanged += HandleNetworkPhaseChanged;
        networkGameState.OnCurrentTurnChanged += HandleNetworkTurnChanged;
    }

    private bool IsNetworkClient()
    {
        return networkGameState != null && networkGameState.IsSpawned;
    }

    private void HandleNetworkPhaseChanged(CardGame.GamePhase newPhase)
    {
        Debug.Log($"[GameManager] HandleNetworkPhaseChanged: newPhase={newPhase}, IsServer={IsServer()}");
        if (CardGame == null) return;
        
        if (!IsServer())
        {
            CardGame.SyncPhase(newPhase);
            ProcessPhaseLogic(newPhase);
        }
    }

    private void HandleNetworkTurnChanged(int playerIndex, ulong clientId)
    {
        Debug.Log($"[GameManager] HandleNetworkTurnChanged: playerIndex={playerIndex}, clientId={clientId}, IsServer={IsServer()}");
        if (IsServer()) return;
        if (CardGame?.round == null) return;
        if (playerIndex < 0 || playerIndex >= _players.Count) return;

        CardGame.SyncTurn(playerIndex);
    }

    public void ClientDealTableCards(List<CardData> cards)
    {
        if (IsServer()) return;
        StopTableDeal(false);
        _tableDealCoroutine = StartCoroutine(ClientDealTableCardsRoutine(cards));
    }

    public void ClientSyncTurn(int playerIndex, ulong clientId)
    {
        if (IsServer()) return;
        if (CardGame?.round == null) return;
        if (playerIndex < 0 || playerIndex >= _players.Count) return;

        CardGame.SyncTurn(playerIndex);
        Debug.Log($"[GameManager] Client: synced turn to player {playerIndex} (clientId={clientId})");
    }

    public void AdvanceRoundGeneration(int generation)
    {
        if (generation > _roundGeneration)
            _roundGeneration = generation;
    }

    public void ClientClearTable()
    {
        if (IsServer()) return;
        if (cardDealer != null)
        {
            cardDealer.ClearTable();
            Debug.Log("[GameManager] Client: cleared table (explicit)");
        }
    }

    public void ClientClearTableAndDeal(List<CardData> cards)
    {
        if (IsServer()) return;
        ClientClearTable();
        ClientDealTableCards(cards);
    }

    private IEnumerator ClientDealTableCardsRoutine(List<CardData> cards)
    {
        Debug.Log($"[GameManager] ClientDealTableCardsRoutine: {cards.Count} cards");
        CardDealer? dealer = cardDealer;
        if (dealer != null)
            yield return WaitForDealer(dealer, () => dealer.DealCardsToTable(cards));
        else
            Debug.LogWarning("[GameManager] ClientDealTableCardsRoutine: cardDealer is null");

        _waitingForTableDealAnimation = false;
        _tableDealCoroutine = null;
        Debug.Log("[GameManager] ClientDealTableCardsRoutine: table deal complete");

        CardGame? game = CardGame;
        if (game?.round == null || game.phase != GamePhase.BettingRoundStart)
        {
            Debug.Log($"[GameManager] ClientDealTableCardsRoutine: skipping discussion (round={game?.round != null}, phase={game?.phase})");
            yield break;
        }

        Debug.Log("[GameManager] ClientDealTableCardsRoutine: starting betting discussion");
        StartBettingDiscussion(game.round);
    }

    public void RequestSkipTurn() => RequestTurnAction(0);
    public void RequestTakeCard() => RequestTurnAction(1);
    public void RequestFold() => RequestTurnAction(2);

    public void RequestBet(List<BodyPartType> partTypes, DeclaredCombinationTier tier)
    {
        if (CardGame?.round == null) return;

        ProcessNetworkBetFromTypes(partTypes, tier);

        if (_isNetworkMode && !IsServer())
        {
            int[] partTypeValues = new int[partTypes.Count];
            for (int i = 0; i < partTypes.Count; i++)
                partTypeValues[i] = (int)partTypes[i];
            networkGameState.SubmitBetServerRpc(partTypeValues, (int)tier);
        }
    }

    public void ProcessNetworkBet(int[] bodyPartTypeValues, int tierValue)
    {
        if (!IsServer()) return;

        var partTypes = new List<BodyPartType>();
        foreach (int v in bodyPartTypeValues)
            partTypes.Add((BodyPartType)v);

        ProcessNetworkBetFromTypes(partTypes, (DeclaredCombinationTier)tierValue);
    }

    private void ProcessNetworkBetFromTypes(List<BodyPartType> partTypes, DeclaredCombinationTier tier)
    {
        var round = CardGame?.round;
        if (round == null) return;
        var player = round.CurrentPlayer;
        if (player == null) return;

        var assets = new List<StakeAsset>();
        foreach (BodyPartType partType in partTypes)
        {
            foreach (var asset in player.team.Assets)
            {
                if (asset.bodyPart != null
                    && asset.bodyPart.Item.Type == partType
                    && asset.bodyPart.State == BodyPartState.Attached)
                {
                    assets.Add(asset);
                    break;
                }
            }
        }

        int selectedValue = 0;
        foreach (var asset in assets)
            if (asset != null) selectedValue += asset.stakeValue;

        try
        {
            if (selectedValue > round.currentParticipationPrice && round.CanRaise(player, assets, tier))
                round.Raise(player, assets, tier);
            else
                round.Call(player, assets, tier);

            if (_isNetworkMode && networkGameState != null)
                networkGameState.SetParticipationPrice(round.currentParticipationPrice);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameManager] ProcessNetworkBet failed: {e.Message}");
        }
    }

    private void RequestTurnAction(int actionType)
    {
        if (CardGame?.round == null) return;
        if (_isNetworkMode && !IsServer())
            networkGameState.SubmitTurnActionServerRpc(actionType);
        else
            ProcessNetworkTurnAction(actionType);
    }

    public void ProcessNetworkTurnAction(int actionType)
    {
        if (!IsServer()) return;
        if (CardGame?.round == null) return;

        var round = CardGame.round;
        var player = round.CurrentPlayer;
        if (player == null) return;

        try
        {
            switch (actionType)
            {
                case 0: round.EndTurn(player); break;
                case 1: round.TakeCard(player); break;
                case 2: round.Fold(player); break;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameManager] Turn action {actionType} failed: {e.Message}");
        }
    }

    private bool IsServer()
    {
        return !_isNetworkMode || (networkGameState != null && networkGameState.IsServer);
    }
// Сетевая часть>
}
