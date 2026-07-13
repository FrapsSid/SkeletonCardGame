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
            Debug.LogError("[MultiplayerBootstrapper] No NetworkPlayers found.");
            return;
        }

        System.Array.Sort(networkPlayers, (a, b) => a.ClientId.CompareTo(b.ClientId));

        Debug.Log($"[MultiplayerBootstrapper] Found {networkPlayers.Length} players.");

        var teams = new List<Team>();
        var players = new List<Skeleton>();
        PlayerPresentationRegistry presentationRegistry = PlayerPresentationRegistry.EnsureDefaultFor(gameManager);
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        Skeleton localPlayer = null;

        foreach (NetworkPlayer networkPlayer in networkPlayers)
        {
            Team team = new Team();
            teams.Add(team);

            Skeleton skeleton = new Skeleton(team);
            skeleton.SetNetworkClientId(networkPlayer.ClientId);
            team.AddSkeleton(skeleton);
            players.Add(skeleton);
            presentationRegistry.RegisterNetworkPlayer(skeleton, networkPlayer);

            SkeletonBody body = networkPlayer.GetComponentInChildren<SkeletonBody>();
            if (body != null)
            {
                SkeletonStakeLinker.RegisterBodyAssets(team, skeleton, body);
                skeleton.SetBody(body);
                PlayerInventoryOwner inventoryOwner = body.GetComponent<PlayerInventoryOwner>();
                if (inventoryOwner != null)
                    inventoryOwner.AssignSkeleton(skeleton);
                Debug.Log($"[MultiplayerBootstrapper] Player {networkPlayer.ClientId} - registered {team.Assets.Count} assets.");
            }
            else
            {
                Debug.LogWarning($"[MultiplayerBootstrapper] Player {networkPlayer.ClientId} has no SkeletonBody.");
            }

            if (networkPlayer.ClientId == localClientId)
                localPlayer = skeleton;

            _playerToSkeleton[networkPlayer] = skeleton;

            Debug.Log($"[MultiplayerBootstrapper] Player (ID: {networkPlayer.ClientId}) registered.");
        }

        if (localPlayer == null)
        {
            Debug.LogError("[MultiplayerBootstrapper] Could not find local player!");
            localPlayer = players[0];
        }

        gameManager.EnableNetworkMode(networkGameState);
        gameManager.StartGame(teams, players, localPlayer);

        Debug.Log($"[MultiplayerBootstrapper] Game started: {teams.Count} teams, {players.Count} players");
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
