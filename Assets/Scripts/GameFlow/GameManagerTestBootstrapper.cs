#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GameManager))]
public sealed class GameManagerTestBootstrapper : MonoBehaviour
{
    [SerializeField]
    private bool startOnStart = true;
    [SerializeField] private SkeletonBody? skeletonBodyPrefab;
    [SerializeField] private SkeletonBody? AIskeletonBodyPrefab;
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

        Team firstTeam = new Team();
        Team secondTeam = new Team();

        teams.Add(firstTeam);
        teams.Add(secondTeam);

        Skeleton firstPlayer = CreatePlayer(firstTeam);
        Skeleton secondPlayer = CreatePlayer(firstTeam);
        Skeleton thirdPlayer = CreatePlayer(secondTeam);
        Skeleton fourthPlayer = CreatePlayer(secondTeam);

        players.Add(firstPlayer);
        players.Add(secondPlayer);
        players.Add(thirdPlayer);
        players.Add(fourthPlayer);

        if (!useRealBodies)
        {
            RegisterTestAssets(firstTeam, firstPlayer);
            RegisterTestAssets(firstTeam, secondPlayer);
            RegisterTestAssets(secondTeam, thirdPlayer);
            RegisterTestAssets(secondTeam, fourthPlayer);
        }

        EnsureTestAiTurnAdapter();
        manager.StartGame(teams, players, players[0]);

        if (useRealBodies)
        {
            SpawnAndLinkBody(firstTeam, firstPlayer, isAI: false);
            SpawnAndLinkBody(firstTeam, secondPlayer, isAI: true);
            SpawnAndLinkBody(secondTeam, thirdPlayer, isAI: true);
            SpawnAndLinkBody(secondTeam, fourthPlayer, isAI: true);
        }

    }

    private void SpawnAndLinkBody(Team team, Skeleton player, bool isAI)
    {
        SkeletonBody? body = SpawnSkeletonBody(isAI);
        if (body == null)
            return;

        StartCoroutine(LinkBodyNextFrame(team, player, body));
    }

    private IEnumerator LinkBodyNextFrame(Team team, Skeleton player, SkeletonBody body)
    {
        yield return null;
        player.SetBody(body);

        PlayerInventoryOwner inventoryOwner = body.GetComponent<PlayerInventoryOwner>();
        if (inventoryOwner != null)
            inventoryOwner.AssignSkeleton(player);

        SkeletonStakeLinker.RegisterBodyAssets(team, player, body);
    }

    private SkeletonBody? SpawnSkeletonBody(bool isAI)
    {
        SkeletonBody? prefab = isAI ? AIskeletonBodyPrefab : skeletonBodyPrefab;
        if (prefab == null)
            return null;

        Transform? seat = _nextSeatIndex < seatPositions.Length ? seatPositions[_nextSeatIndex] : null;
        _nextSeatIndex++;

        return seat != null
            ? Instantiate(prefab, seat.position, seat.rotation)
            : Instantiate(prefab);
    }

    private static void RegisterTestAssets(Team team, Skeleton player)
    {
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.Soul, 6, sourceOwner: player));
        team.RegisterAsset(new StakeAsset(team, StakeAssetType.OtherTeamAsset, 3));
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
