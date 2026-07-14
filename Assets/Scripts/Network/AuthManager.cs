using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public string ApiBaseUrl { get; set; } = "https://skeletongame.necr0manth.dev";

    public string Token { get; private set; }
    public string PlayerNickname { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public event Action OnAuthSuccess;
    public event Action OnAuthLoggedOut;
    public event Action<string> OnDiscordStatus;

    private const string TokenKey = "auth_token";
    private const string NicknameKey = "auth_nickname";
    private const int DiscordCallbackPort = 19876;

    private TcpListener _tcpListener;
    private Thread _listenerThread;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSavedAuth();
    }

    private void OnApplicationQuit()
    {
        StopLocalServer();
    }

    private void LoadSavedAuth()
    {
        Token = PlayerPrefs.GetString(TokenKey, "");
        PlayerNickname = PlayerPrefs.GetString(NicknameKey, "");
        if (IsAuthenticated)
            Debug.Log($"[Auth] Loaded saved session for '{PlayerNickname}'");
    }

    // ── Discord OAuth via local TCP server ───────────────────────

    public void LoginWithDiscord()
    {
        OnDiscordStatus?.Invoke("Starting local server...");

        try
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, DiscordCallbackPort);
            _tcpListener.Start();
            Debug.Log($"[Auth] TCP server started on port {DiscordCallbackPort}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Auth] Failed to start TCP server: {ex.Message}");
            OnDiscordStatus?.Invoke("Failed to start local server");
            return;
        }

        _listenerThread = new Thread(ListenForCallback);
        _listenerThread.IsBackground = true;
        _listenerThread.Start();

        string redirectUri = $"http://127.0.0.1:{DiscordCallbackPort}/callback";
        string url = $"{ApiBaseUrl}/auth/login?redirect_uri={Uri.EscapeDataString(redirectUri)}";

        Debug.Log($"[Auth] Opening browser: {url}");
        OnDiscordStatus?.Invoke("Opening browser...");
        Application.OpenURL(url);
    }

    private void ListenForCallback()
    {
        try
        {
            using var client = _tcpListener.AcceptTcpClient();
            using var stream = client.GetStream();

            var request = new StreamReader(stream, Encoding.UTF8);
            string requestLine = request.ReadLine();
            Debug.Log($"[Auth] Received: {requestLine}");

            string token = null;
            string nickname = null;

            if (requestLine != null && requestLine.StartsWith("GET "))
            {
                string path = requestLine.Split(' ')[1];
                int qIndex = path.IndexOf('?');
                if (qIndex >= 0)
                {
                    string queryString = path.Substring(qIndex + 1);
                    foreach (string pair in queryString.Split('&'))
                    {
                        var parts = pair.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = Uri.UnescapeDataString(parts[0]);
                            string val = Uri.UnescapeDataString(parts[1]);
                            if (key == "token") token = val;
                            if (key == "nickname") nickname = val;
                        }
                    }
                }
            }

            string html = "<html><body style='background:#1a1a2e;color:#fff;font-family:sans-serif;display:flex;justify-content:center;align-items:center;height:100vh;margin:0'>" +
                "<div style='text-align:center'><h1 style='color:#00ffa6'>Login successful!</h1><p>You can close this tab and return to the game.</p></div>" +
                "</body></html>";
            string response = $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {Encoding.UTF8.GetByteCount(html)}\r\nConnection: close\r\n\r\n{html}";
            byte[] buffer = Encoding.UTF8.GetBytes(response);
            stream.Write(buffer, 0, buffer.Length);

            if (!string.IsNullOrEmpty(token))
            {
                Token = token;
                PlayerNickname = nickname ?? "Player";
                PlayerPrefs.SetString(TokenKey, Token);
                PlayerPrefs.SetString(NicknameKey, PlayerNickname);
                PlayerPrefs.Save();
                Debug.Log($"[Auth] Discord login: '{PlayerNickname}'");
                OnAuthSuccess?.Invoke();
            }
            else
            {
                Debug.LogWarning("[Auth] Callback received but no token in URL");
            }
        }
        catch (Exception ex)
        {
            if (_tcpListener != null)
                Debug.LogError($"[Auth] TCP server error: {ex.Message}");
        }
    }

    private void StopLocalServer()
    {
        try { _tcpListener?.Stop(); } catch { }
        _tcpListener = null;
    }

    // ── Login / Register ─────────────────────────────────────────

    public void Register(string username, string password, string nickname, Action<bool, string> callback)
    {
        var body = JsonUtility.ToJson(new RegisterPayload
        {
            username = username,
            password = password,
            nickname = nickname
        });
        StartCoroutine(PostRequest($"{ApiBaseUrl}/auth/register", body, (success, json) =>
        {
            if (success)
            {
                var msg = JsonUtility.FromJson<MessageResponse>(json);
                callback?.Invoke(true, msg.message);
            }
            else
            {
                var err = ParseError(json);
                callback?.Invoke(false, err);
            }
        }));
    }

    public void Login(string username, string password, Action<bool, string> callback)
    {
        var body = JsonUtility.ToJson(new LoginPayload
        {
            username = username,
            password = password
        });
        StartCoroutine(PostRequest($"{ApiBaseUrl}/auth/login/local", body, (success, json) =>
        {
            if (success)
            {
                var resp = JsonUtility.FromJson<LoginResponse>(json);
                Token = resp.access_token;
                PlayerNickname = resp.player;
                PlayerPrefs.SetString(TokenKey, Token);
                PlayerPrefs.SetString(NicknameKey, PlayerNickname);
                PlayerPrefs.Save();
                Debug.Log($"[Auth] Logged in as '{PlayerNickname}'");
                OnAuthSuccess?.Invoke();
                callback?.Invoke(true, $"Welcome, {PlayerNickname}!");
            }
            else
            {
                var err = ParseError(json);
                callback?.Invoke(false, err);
            }
        }));
    }

    public void Logout()
    {
        Token = "";
        PlayerNickname = "";
        PlayerPrefs.DeleteKey(TokenKey);
        PlayerPrefs.DeleteKey(NicknameKey);
        PlayerPrefs.Save();
        Debug.Log("[Auth] Logged out");
        OnAuthLoggedOut?.Invoke();
    }

    // ── HTTP Helpers ─────────────────────────────────────────────

    private string ParseError(string json)
    {
        if (string.IsNullOrEmpty(json)) return "Unknown error";

        try
        {
            var errDetail = JsonUtility.FromJson<ErrorResponse>(json);
            if (!string.IsNullOrEmpty(errDetail.detail))
                return errDetail.detail;
        }
        catch { }

        try
        {
            var errArray = JsonUtility.FromJson<ErrorResponseArray>(json);
            if (errArray.detail != null && errArray.detail.Length > 0)
                return errArray.detail[0].msg;
        }
        catch { }

        return json;
    }

    private IEnumerator PostRequest(string url, string jsonBody, Action<bool, string> callback)
    {
        using var request = new UnityWebRequest(url, "POST");
        var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        if (IsAuthenticated)
            request.SetRequestHeader("Authorization", $"Bearer {Token}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            callback?.Invoke(true, request.downloadHandler.text);
        else
            callback?.Invoke(false, request.downloadHandler.text);

        request.Dispose();
    }

    [Serializable]
    private class RegisterPayload
    {
        public string username;
        public string password;
        public string nickname;
    }

    [Serializable]
    private class LoginPayload
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class LoginResponse
    {
        public string access_token;
        public string token_type;
        public string player;
    }

    [Serializable]
    private class MessageResponse
    {
        public string message;
    }

    [Serializable]
    private class ErrorResponse
    {
        public string detail;
    }

    [Serializable]
    private class ErrorResponseArray
    {
        public ErrorDetail[] detail;
    }

    [Serializable]
    private class ErrorDetail
    {
        public string msg;
    }
}
