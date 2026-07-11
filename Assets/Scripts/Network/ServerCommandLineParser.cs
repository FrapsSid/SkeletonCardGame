using System;
using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer
{
    public static class ServerCommandLineParser
    {
        public struct ServerConfig
        {
            public bool IsDedicatedServer;
            public ushort Port;
            public int MaxPlayers;
            public int MinPlayersToStart;
            public float LobbyTimeout;
            public string ServerName;
            public bool EnableLogging;
            public string LogPath;
        }

        public static ServerConfig Parse()
        {
            var config = new ServerConfig
            {
                IsDedicatedServer = false,
                Port = 7777,
                MaxPlayers = 8,
                MinPlayersToStart = 2,
                LobbyTimeout = 120f,
                ServerName = "Skeleton Card Server",
                EnableLogging = true,
                LogPath = "server_logs"
            };

            string[] args = Environment.GetCommandLineArgs();
            var argDict = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();

                if (arg == "-server" || arg == "--server" || arg == "-batchmode")
                {
                    config.IsDedicatedServer = true;
                    continue;
                }

                if (arg == "-nolog")
                {
                    config.EnableLogging = false;
                    continue;
                }

                if (arg.StartsWith("-") && i + 1 < args.Length)
                {
                    string key = arg.TrimStart('-');
                    string value = args[i + 1];
                    argDict[key] = value;
                    i++;
                }
            }

            if (argDict.TryGetValue("port", out string portStr) &&
                ushort.TryParse(portStr, out ushort port))
            {
                config.Port = port;
            }

            if (argDict.TryGetValue("maxplayers", out string maxPlayersStr) &&
                int.TryParse(maxPlayersStr, out int maxPlayers))
            {
                config.MaxPlayers = Mathf.Clamp(maxPlayers, 2, 100);
            }

            if (argDict.TryGetValue("minplayers", out string minPlayersStr) &&
                int.TryParse(minPlayersStr, out int minPlayers))
            {
                config.MinPlayersToStart = Mathf.Clamp(minPlayers, 2, config.MaxPlayers);
            }

            if (argDict.TryGetValue("lobbytimeout", out string timeoutStr) &&
                float.TryParse(timeoutStr, out float timeout))
            {
                config.LobbyTimeout = Mathf.Max(0f, timeout);
            }

            if (argDict.TryGetValue("servername", out string serverName))
            {
                config.ServerName = serverName;
            }

            if (argDict.TryGetValue("logpath", out string logPath))
            {
                config.LogPath = logPath;
            }

            return config;
        }

        public static void LogConfig(ServerConfig config)
        {
            Debug.Log("=== Server Configuration ===");
            Debug.Log($"Dedicated Server: {config.IsDedicatedServer}");
            Debug.Log($"Port: {config.Port}");
            Debug.Log($"Max Players: {config.MaxPlayers}");
            Debug.Log($"Min Players to Start: {config.MinPlayersToStart}");
            Debug.Log($"Lobby Timeout: {config.LobbyTimeout}s");
            Debug.Log($"Server Name: {config.ServerName}");
            Debug.Log($"Logging: {config.EnableLogging}");
            Debug.Log("============================");
        }

        // Example usage for building command line:
        // ./GameServer -server -port 7777 -maxplayers 8 -minplayers 2 -lobbytimeout 120
    }
}
