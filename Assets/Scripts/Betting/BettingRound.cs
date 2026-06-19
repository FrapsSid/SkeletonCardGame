using System;
using System.Collections.Generic;
using System.Linq;

using Player = Skeleton;

public class BettingRound
{
    public Dictionary<Player, PlayerBetState> playerStates = new Dictionary<Player, PlayerBetState>();
    public int currentParticipationPrice;
    public List<StakeAsset> currentPot = new List<StakeAsset>();
    public List<Player> activePlayers = new List<Player>();
    public bool isBettingOpen;

    public event Action<BettingRound> OnBettingOpened;
    public event Action<Player, DeclaredCombinationTier> OnTargetDeclared;
    public event Action<Player, DeclaredCombinationTier> OnTargetUpgraded;
    public event Action<Player, int> OnPriceMatched;
    public event Action<Player, int> OnPriceRaised;
    public event Action<Player> OnPlayerFolded;
    public event Action<List<Team>, List<StakeAsset>> OnPotResolved;

    private readonly Dictionary<Player, Team> playerTeams = new Dictionary<Player, Team>();

    public BettingRound()
    {
    }

    public BettingRound(IEnumerable<Player> players)
    {
        if (players == null)
        {
            return;
        }

        foreach (var player in players)
        {
            RegisterPlayer(player, null);
        }
    }

    public BettingRound(IEnumerable<Team> teams)
    {
        RegisterTeams(teams);
    }

    public void RegisterTeams(IEnumerable<Team> teams)
    {
        if (teams == null)
        {
            return;
        }

        foreach (var team in teams)
        {
            if (team?.Skeletons == null)
            {
                continue;
            }

            foreach (var player in team.Skeletons)
            {
                RegisterPlayer(player, team);
            }
        }
    }

    public void RegisterPlayer(Player player, Team team)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (!playerStates.TryGetValue(player, out PlayerBetState state))
        {
            playerStates[player] = new PlayerBetState();
        }

        if (!activePlayers.Contains(player))
        {
            activePlayers.Add(player);
        }

        if (team != null)
        {
            team.AddSkeleton(player);
            playerTeams[player] = team;
        }
    }

    public void OpenBetting()
    {
        isBettingOpen = true;

        foreach (var state in playerStates.Values)
        {
            if (state == null || state.hasFolded)
            {
                continue;
            }
        }

        OnBettingOpened?.Invoke(this);
    }

    public void CloseBetting()
    {
        isBettingOpen = false;
    }

    public void SetInitialDeclaration(Player player, DeclaredCombinationTier target)
    {
        EnsureBettingOpen();
        PlayerBetState state = EnsurePlayerCanAct(player);

        if (state.HasDeclaredTarget)
        {
            throw new InvalidOperationException("hasDeclaredTarget");
        }

        state.declaredTarget = target;

        OnTargetDeclared?.Invoke(player, target);
    }

    public void MatchCurrentPrice(Player player, List<StakeAsset> assets)
    {
        EnsureBettingOpen();
        PlayerBetState state = EnsurePlayerCanAct(player);

        CommitAssets(player, state, assets);

        if (state.committedValue < currentParticipationPrice)
        {
            throw new InvalidOperationException("committedValue < currentParticipationPrice");
        }

        OnPriceMatched?.Invoke(player, currentParticipationPrice);
    }

    public void UpdateBet(Player player, int newBet, List<StakeAsset> assets, DeclaredCombinationTier newTier)
    {
        EnsureBettingOpen();
        PlayerBetState state = EnsurePlayerCanAct(player);
        if (!state.HasDeclaredTarget)
        {
            SetInitialDeclaration(player, newTier);
        }
        else
        {
            UpgradeTarget(player, newTier, new List<StakeAsset>());
        }
    }

    public void RaiseCurrentPrice(Player player, int newPrice, List<StakeAsset> assets)
    {
        EnsureBettingOpen();
        PlayerBetState state = EnsurePlayerCanAct(player);

        if (newPrice <= currentParticipationPrice)
        {
            throw new ArgumentException("new price < current price", nameof(newPrice));
        }

        CommitAssets(player, state, assets);

        if (state.committedValue < newPrice)
        {
            throw new InvalidOperationException("committedValue < newPrice");
        }

        currentParticipationPrice = newPrice;

        foreach (var activePlayer in activePlayers)
        {
            PlayerBetState activeState = playerStates[activePlayer];
        }

        OnPriceRaised?.Invoke(player, newPrice);
    }

    public void UpgradeTarget(Player player, DeclaredCombinationTier newTarget, List<StakeAsset> optionalAssets)
    {
        EnsureBettingOpen();
        PlayerBetState state = EnsurePlayerCanAct(player);

        if (!state.HasDeclaredTarget)
        {
            throw new InvalidOperationException("not hasDeclaredTarget");
        }

        if (!IsValidUpgrade((DeclaredCombinationTier)state.declaredTarget, newTarget))
        {
            throw new InvalidOperationException("target can only be upgraded Easy -> Medium -> Hard or Medium -> Hard");
        }

        CommitAssets(player, state, optionalAssets);
        state.declaredTarget = newTarget;

        OnTargetUpgraded?.Invoke(player, newTarget);
    }

    public void EndTurn(Player player)
    {
        EnsureBettingOpen();
        PlayerBetState state = EnsurePlayerCanAct(player);

        if (state.committedValue < currentParticipationPrice)
        {
            throw new InvalidOperationException("Player can end turn only after matching the current participation price");
        }

    }

    public void Fold(Player player)
    {
        EnsureBettingOpen();
        PlayerBetState state = EnsurePlayerCanAct(player);

        state.hasFolded = true;
        activePlayers.Remove(player);

        OnPlayerFolded?.Invoke(player);
    }

    public bool AreAllActivePlayersMatched()
    {
        foreach (var player in activePlayers)
        {
            if (!playerStates.TryGetValue(player, out PlayerBetState state))
            {
                return false;
            }

            if (state.hasFolded)
            {
                continue;
            }

            if (state.committedValue < currentParticipationPrice)
            {
                return false;
            }
        }

        return true;
    }

    public void ResolvePot(List<Team> winners)
    {
        if (winners == null || winners.Count == 0)
        {
            throw new ArgumentException("At least 1 winning team is required", nameof(winners));
        }

        var validWinners = winners.Where(winner => winner != null).Distinct().ToList();

        if (validWinners.Count == 0)
        {
            throw new ArgumentException("At least 1 non-null winning team is required to resolve the pot", nameof(winners));
        }

        List<StakeAsset> resolvedAssets = new List<StakeAsset>(currentPot);

        if (validWinners.Count == 1)
        {
            TransferAssets(resolvedAssets, validWinners[0]);
        }
        else
        {
            SplitPotBetweenWinners(resolvedAssets, validWinners);
        }

        currentPot.Clear();

        foreach (var state in playerStates.Values)
        {
            if (state == null)
            {
                continue;
            }

            state.committedAssets.Clear();
            state.committedValue = 0;
        }

        CloseBetting();
        OnPotResolved?.Invoke(validWinners, resolvedAssets);
    }

    public int CalculateStakeValue(List<StakeAsset> assets)
    {
        return CalculateStakeValue((IEnumerable<StakeAsset>)assets);
    }

    public static int CalculateStakeValue(IEnumerable<StakeAsset> assets)
    {
        if (assets == null)
        {
            return 0;
        }

        int value = 0;

        foreach (var asset in assets)
        {
            if (asset == null)
            {
                continue;
            }

            value += asset.stakeValue;
        }

        return value;
    }

    private void CommitAssets(Player player, PlayerBetState state, List<StakeAsset> assets)
    {
        if (assets == null || assets.Count == 0)
        {
            return;
        }

        var playerTeam = GetPlayerTeam(player);

        if (playerTeam == null)
        {
            throw new InvalidOperationException("Player must belong to a team before staking team assets");
        }

        var seenThisAction = new HashSet<StakeAsset>();

        foreach (var asset in assets)
        {
            ValidateStakeAsset(playerTeam, asset);
            state.committedAssets.Add(asset);
            state.committedValue += Math.Max(0, asset.stakeValue);

            if (!currentPot.Contains(asset))
            {
                currentPot.Add(asset);
            }
        }
    }

    private void ValidateStakeAsset(Team playerTeam, StakeAsset asset)
    {
        if (asset == null)
        {
            throw new ArgumentException("asset == null");
        }

        if (asset.owningTeam != playerTeam)
        {
            throw new InvalidOperationException("player team is not owning");
        }

        if (!currentPot.Contains(asset))
        {
            throw new InvalidOperationException("Asset already staked or blocked by another action");
        }

        if (asset.stakeValue <= 0)
        {
            throw new InvalidOperationException("stake asset should be > 0");
        }
    }

    private PlayerBetState EnsurePlayerCanAct(Player player)
    {
        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (!playerStates.TryGetValue(player, out PlayerBetState state))
        {
            state = new PlayerBetState();
            playerStates[player] = state;
        }

        if (state.hasFolded || !activePlayers.Contains(player))
        {
            throw new InvalidOperationException("Folded or inactive player cannot act in the current betting round");
        }

        return state;
    }

    private void EnsureBettingOpen()
    {
        if (!isBettingOpen)
        {
            throw new InvalidOperationException("Betting unavailable while betting is open");
        }
    }

    private Team GetPlayerTeam(Player player)
    {
        if (playerTeams.TryGetValue(player, out Team team))
        {
            return team;
        }

        foreach (var candidateTeam in playerTeams.Values)
        {
            if (candidateTeam != null && candidateTeam.HasPlayer(player))
            {
                playerTeams[player] = candidateTeam;
                return candidateTeam;
            }
        }

        return null;
    }

    private static bool IsValidUpgrade(DeclaredCombinationTier currentTarget, DeclaredCombinationTier newTarget)
    {
        return newTarget > currentTarget;
    }

    private static void TransferAssets(List<StakeAsset> assets, Team winner)
    {
        foreach (var asset in assets)
        {
            if (asset == null) continue;

            asset.TransferOwnership(winner);
        }
    }

    private static void SplitPotBetweenWinners(List<StakeAsset> assets, List<Team> winners)
    {
        var assignedValues = new Dictionary<Team, int>();

        foreach (var winner in winners)
        {
            assignedValues[winner] = 0;
        }

        foreach (var asset in assets.OrderByDescending(asset => asset != null ? asset.stakeValue : 0))
        {
            if (asset == null)
            {
                continue;
            }

            var targetWinner = assignedValues
                .OrderBy(pair => pair.Value)
                .ThenBy(pair => winners.IndexOf(pair.Key))
                .First()
                .Key;

            asset.TransferOwnership(targetWinner);
            assignedValues[targetWinner] += Math.Max(0, asset.stakeValue);
        }
    }
}
