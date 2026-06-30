using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Multiplayer;

public class MultiplayerGameBootstrapper : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private NetworkGameState networkGameState;

    private Dictionary<NetworkPlayer, Skeleton> _playerToSkeleton = new();

    private void Start()
    {
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        yield return null;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            Debug.LogError("[MultiplayerBootstrapper] NetworkManager не запущен.");
            yield break;
        }

        if (gameManager == null || networkGameState == null)
        {
            Debug.LogError("[MultiplayerBootstrapper] Не назначены GameManager или NetworkGameState.");
            yield break;
        }

        while (FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Length == 0)
        {
            yield return null;
        }

        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[MultiplayerBootstrapper] Инициализация игры на сервере.");
            SetupGame();
        }
        else
        {
            Debug.Log("[MultiplayerBootstrapper] Инициализация игры на клиенте.");
            SetupGame();
        }
    }

    private void SetupGame()
    {
        NetworkPlayer[] networkPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        
        if (networkPlayers.Length == 0)
        {
            Debug.LogError("[MultiplayerBootstrapper] Не найдено ни одного NetworkPlayer.");
            return;
        }

        Debug.Log($"[MultiplayerBootstrapper] Найдено {networkPlayers.Length} игроков.");

        var teams = new List<Team>();
        var players = new List<Skeleton>();

        foreach (NetworkPlayer networkPlayer in networkPlayers)
        {
            Team team = new Team();
            teams.Add(team);

            Skeleton skeleton = new Skeleton(team);
            team.AddSkeleton(skeleton);
            players.Add(skeleton);

            SkeletonBody body = networkPlayer.GetComponentInChildren<SkeletonBody>();
            if (body != null)
            {
                SkeletonStakeLinker.RegisterBodyAssets(team, skeleton, body);
                Debug.Log($"[MultiplayerBootstrapper] Игрок {networkPlayer.PlayerName} - зарегистрировано {team.Assets.Count} активов из тела.");
            }
            else
            {
                Debug.LogWarning($"[MultiplayerBootstrapper] У игрока {networkPlayer.PlayerName} нет SkeletonBody.");
            }

            _playerToSkeleton[networkPlayer] = skeleton;

            Debug.Log($"[MultiplayerBootstrapper] Игрок {networkPlayer.PlayerName} (ID: {networkPlayer.ClientId}, Index: {networkPlayer.PlayerIndex}) зарегистрирован.");
        }

        Skeleton localPlayer = players[0];

        gameManager.EnableNetworkMode(networkGameState);
        gameManager.StartGame(teams, players, localPlayer);

        Debug.Log($"[MultiplayerBootstrapper] Игра запущена: {teams.Count} команд, {players.Count} игроков");
    }

    public Skeleton GetSkeletonForPlayer(NetworkPlayer networkPlayer)
    {
        return _playerToSkeleton.TryGetValue(networkPlayer, out Skeleton skeleton) ? skeleton : null;
    }

    public NetworkPlayer GetNetworkPlayerForSkeleton(Skeleton skeleton)
    {
        foreach (var kvp in _playerToSkeleton)
        {
            if (kvp.Value == skeleton)
                return kvp.Key;
        }
        return null;
    }
}