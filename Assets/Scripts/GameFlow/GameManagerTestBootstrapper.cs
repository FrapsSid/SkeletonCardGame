#nullable enable
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(GameManager))]
public sealed class GameManagerTestBootstrapper : MonoBehaviour
{
    [SerializeField]
    private bool startOnStart = true;
    [SerializeField] private SkeletonBody? skeletonBodyPrefab;
    [SerializeField] private Transform[] seatPositions = new Transform[0];

    private GameManager? gameManager;
    private int _nextSeatIndex;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    private void Start()
    {
        if (startOnStart)
            StartTestGame();
    }

    public void StartTestGame()
    {
        GameManager manager = gameManager != null ? gameManager : GetComponent<GameManager>();
        gameManager = manager;
        _nextSeatIndex = 0;

        var teams = new List<Team>();
        var players = new List<Skeleton>();

        bool useRealBodies = skeletonBodyPrefab != null;

        Team firstTeam = useRealBodies ? new Team() : CreateTestTeam();
        Team secondTeam = useRealBodies ? new Team() : CreateTestTeam();

        teams.Add(firstTeam);
        teams.Add(secondTeam);

        Skeleton firstPlayer = CreatePlayer(firstTeam);
        Skeleton secondPlayer = CreatePlayer(secondTeam);

        players.Add(firstPlayer);
        players.Add(secondPlayer);

        manager.StartGame(teams, players, players[0]);

        if (useRealBodies)
        {
            SpawnAndLinkBody(firstTeam, firstPlayer);
            SpawnAndLinkBody(secondTeam, secondPlayer);
        }
    }

    private void SpawnAndLinkBody(Team team, Skeleton player)
    {
        SkeletonBody? body = SpawnSkeletonBody();
        if (body == null)
            return;

        StartCoroutine(LinkBodyNextFrame(team, player, body));
    }

    private IEnumerator LinkBodyNextFrame(Team team, Skeleton player, SkeletonBody body)
    {
        yield return null;
        SkeletonStakeLinker.RegisterBodyAssets(team, player, body);
    }

    private SkeletonBody? SpawnSkeletonBody()
    {
        if (skeletonBodyPrefab == null)
            return null;

        Transform? seat = _nextSeatIndex < seatPositions.Length ? seatPositions[_nextSeatIndex] : null;
        _nextSeatIndex++;

        return seat != null
            ? Instantiate(skeletonBodyPrefab, seat.position, seat.rotation)
            : Instantiate(skeletonBodyPrefab);
        EnsureTestAiTurnAdapter();
        manager.StartGame(teams, players, players[0]);

        GameUIManager? ui = FindFirstObjectByType<GameUIManager>();
        if (ui == null)
            return;

        ui.RefreshGameManager();
        ui.EnterGameHud();
    }

    private static Team CreateTestTeam()
    {
        var team = new Team();
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.Soul, 1));
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.Soul, 2));
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.OtherTeamAsset, 3));
        return team;
    }

    private static Skeleton CreatePlayer(Team team)
    {
        var player = new Skeleton(team);
        team.AddSkeleton(player);
        return player;
    }

    private void EnsureTestAiTurnAdapter()
    {
        if (GetComponent<TestAiTurnAdapter>() == null)
            gameObject.AddComponent<TestAiTurnAdapter>();
    }
}
