using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Multiplayer;

public class MultiplayerGameBootstrapper : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private NetworkGameState networkGameState;
    [SerializeField] private NetworkBettingController networkBettingController;
    [SerializeField] private NetworkRoundSync networkRoundSync;
    [SerializeField] private NetworkAssetSync networkAssetSync;
    [SerializeField] private NetworkCardSync networkCardSync;

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
            Debug.LogError("[MultiplayerBootstrapper] NetworkManager not running.");
            yield break;
        }

        if (gameManager == null || networkGameState == null)
        {
            Debug.LogError("[MultiplayerBootstrapper] GameManager or NetworkGameState not assigned.");
            yield break;
        }

        while (FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).Length == 0)
        {
            yield return null;
        }

        SetupGame();
    }

    private void SetupGame()
    {
        NetworkPlayer[] networkPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);

        if (networkPlayers.Length == 0)
        {
            Debug.LogError("[MultiplayerBootstrapper] No NetworkPlayers found.");
            return;
        }

        Debug.Log($"[MultiplayerBootstrapper] Found {networkPlayers.Length} players.");

        var teams = new List<Team>();
        var players = new List<Skeleton>();
        PlayerPresentationRegistry presentationRegistry = PlayerPresentationRegistry.EnsureDefaultFor(gameManager);

        // Auto-wire network components if not assigned
        if (networkBettingController == null)
            networkBettingController = FindFirstObjectByType<NetworkBettingController>();
        if (networkRoundSync == null)
            networkRoundSync = FindFirstObjectByType<NetworkRoundSync>();
        if (networkAssetSync == null)
            networkAssetSync = FindFirstObjectByType<NetworkAssetSync>();
        if (networkCardSync == null)
            networkCardSync = FindFirstObjectByType<NetworkCardSync>();

        // Wire SerializeField references on network components
        WireNetworkComponents();

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
                skeleton.SetBody(body);
                SkeletonStakeLinker.RegisterBodyAssets(team, skeleton, body);
                Debug.Log($"[MultiplayerBootstrapper] Player {networkPlayer.ClientId} - registered {team.Assets.Count} assets.");
            }
            else
            {
                Debug.LogWarning($"[MultiplayerBootstrapper] Player {networkPlayer.ClientId} has no SkeletonBody.");
            }

            _playerToSkeleton[networkPlayer] = skeleton;

            Debug.Log($"[MultiplayerBootstrapper] Player (ID: {networkPlayer.ClientId}) registered.");
        }

        Skeleton localPlayer = players[0];

        gameManager.EnableNetworkMode(networkGameState);
        gameManager.StartGame(teams, players, localPlayer);

        Debug.Log($"[MultiplayerBootstrapper] Game started: {teams.Count} teams, {players.Count} players");
    }

    private void WireNetworkComponents()
    {
        if (networkBettingController != null)
        {
            SetPrivateField(networkBettingController, "gameManager", gameManager);
            SetPrivateField(networkBettingController, "networkGameState", networkGameState);
        }
        if (networkRoundSync != null)
        {
            SetPrivateField(networkRoundSync, "gameManager", gameManager);
        }
        if (networkAssetSync != null)
        {
            SetPrivateField(networkAssetSync, "gameManager", gameManager);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
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
