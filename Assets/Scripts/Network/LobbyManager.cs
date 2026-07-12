using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Multiplayer
{
    public sealed class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        [SerializeField] private string serverAddress = "10.93.27.48";
        [SerializeField] private int managerPort = 7700;

        public string ServerAddress => serverAddress;
        public string CurrentLobbyCode { get; private set; }
        public int CurrentLobbyPort { get; private set; }

        public event Action<string, int> OnLobbyCreated;
        public event Action<int> OnLobbyFound;
        public event Action<string> OnLobbyNotFound;
        public event Action<string> OnError;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[LobbyManager] Duplicate instance, destroying");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[LobbyManager] Awake: Instance set, server={serverAddress}:{managerPort}");
        }

        public void CreateLobby()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using var client = new TcpClient();
                    client.Connect(serverAddress, managerPort);
                    using var stream = client.GetStream();

                    byte[] request = Encoding.UTF8.GetBytes("CREATE\n");
                    stream.Write(request, 0, request.Length);

                    string response = ReadAll(stream);

                    string code = ParseField(response, "CODE");
                    string portStr = ParseField(response, "PORT");

                    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(portStr))
                    {
                        UnityMainThread.Execute(() => OnError?.Invoke("Invalid server response"));
                        return;
                    }

                    int port = int.Parse(portStr);
                    Debug.Log($"[LobbyManager] Created lobby {code} on port {port}");
                    UnityMainThread.Execute(() =>
                    {
                        CurrentLobbyCode = code;
                        CurrentLobbyPort = port;
                        OnLobbyCreated?.Invoke(code, port);
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LobbyManager] Create failed: {e.Message}");
                    UnityMainThread.Execute(() => OnError?.Invoke($"Connection failed: {e.Message}"));
                }
            });
        }

        public void FindLobby(string code)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    using var client = new TcpClient();
                    client.Connect(serverAddress, managerPort);
                    using var stream = client.GetStream();

                    byte[] request = Encoding.UTF8.GetBytes($"JOIN:{code.ToUpper()}\n");
                    stream.Write(request, 0, request.Length);

                    string response = ReadAll(stream);

                    if (response.StartsWith("NOT_FOUND"))
                    {
                        Debug.Log($"[LobbyManager] Lobby {code} not found");
                        UnityMainThread.Execute(() => OnLobbyNotFound?.Invoke(code));
                        return;
                    }

                    string portStr = ParseField(response, "PORT");
                    if (string.IsNullOrEmpty(portStr))
                    {
                        UnityMainThread.Execute(() => OnError?.Invoke("Invalid server response"));
                        return;
                    }

                    int port = int.Parse(portStr);
                    Debug.Log($"[LobbyManager] Found lobby {code} on port {port}");
                    UnityMainThread.Execute(() =>
                    {
                        CurrentLobbyCode = code.ToUpper();
                        CurrentLobbyPort = port;
                        OnLobbyFound?.Invoke(port);
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LobbyManager] Find failed: {e.Message}");
                    UnityMainThread.Execute(() => OnError?.Invoke($"Connection failed: {e.Message}"));
                }
            });
        }

        private static string ParseField(string response, string field)
        {
            string prefix = $"{field}:";
            foreach (string line in response.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith(prefix))
                    return trimmed.Substring(prefix.Length);
            }
            return string.Empty;
        }

        private static string ReadAll(NetworkStream stream)
        {
            stream.ReadTimeout = 5000;
            var sb = new StringBuilder();
            byte[] buf = new byte[256];
            try
            {
                while (true)
                {
                    int read = stream.Read(buf, 0, buf.Length);
                    if (read == 0) break;
                    sb.Append(Encoding.UTF8.GetString(buf, 0, read));
                }
            }
            catch (System.IO.IOException) { }
            catch (System.Net.Sockets.SocketException) { }
            return sb.ToString();
        }
    }
}
