using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    public class RelayTransport : NetworkTransport
    {
        [SerializeField] private string relayAddress = "10.93.27.48";
        [SerializeField] private ushort relayPort = 7800;

        private ClientWebSocket _socket;
        private Thread _receiveThread;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<TransportEvent> _events = new();
        private bool _isHost;
        private string _roomCode;
        private bool _connected;

        public string RoomCode => _roomCode;
        public event Action<string> OnRoomCodeReceived;

        public void SetRoomCode(string code) => _roomCode = code;

        public override void Initialize(NetworkManager networkManager) { }

        public override void Shutdown()
        {
            CloseSocket();
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (!_connected) return;
            if (_isHost)
                SendRaw(new JObject { ["type"] = "send", ["to"] = (int)clientId, ["data"] = "" });
        }

        public override void DisconnectLocalClient()
        {
            CloseSocket();
        }

        public override ulong GetCurrentRtt(ulong clientId) => 0;

        public override bool StartClient()
        {
            _isHost = false;
            return ConnectAndJoin();
        }

        public override bool StartServer()
        {
            _isHost = true;
            return ConnectAndHost();
        }

        private bool ConnectAndHost()
        {
            if (!Connect())
                return false;

            SendRaw(new JObject { ["type"] = "host" });

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (_roomCode == null && DateTime.UtcNow < deadline && _connected)
                Thread.Sleep(10);

            return _roomCode != null;
        }

        private bool ConnectAndJoin()
        {
            if (!Connect())
                return false;

            SendRaw(new JObject { ["type"] = "join", ["room"] = _roomCode });

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline && _connected)
            {
                if (_events.TryPeek(out var e) && e.Type == NetworkEvent.Connect)
                    return true;
                Thread.Sleep(10);
            }
            return false;
        }

        private bool Connect()
        {
            try
            {
                _cts = new CancellationTokenSource();
                _socket = new ClientWebSocket();
                var uri = new Uri($"ws://{relayAddress}:{relayPort}");
                Debug.Log($"[Relay] Connecting to {uri}...");
                var task = _socket.ConnectAsync(uri, _cts.Token);
                if (!task.Wait(TimeSpan.FromSeconds(10)))
                {
                    Debug.LogError("[Relay] Connection timed out");
                    return false;
                }
                _connected = true;
                Debug.Log("[Relay] Connected");
                _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "RelayRecv" };
                _receiveThread.Start();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Relay] Connect failed: {e.Message}");
                CloseSocket();
                return false;
            }
        }

        private void ReceiveLoop()
        {
            var buf = new byte[65536];
            var sb = new StringBuilder();
            var pingCts = new CancellationTokenSource();
            try
            {
                while (_connected && _socket.State == WebSocketState.Open)
                {
                    try
                    {
                        pingCts.CancelAfter(TimeSpan.FromSeconds(30));
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, pingCts.Token);
                        var result = _socket.ReceiveAsync(new ArraySegment<byte>(buf), linkedCts.Token).GetAwaiter().GetResult();
                        pingCts.Cancel();
                        pingCts.Dispose();
                        pingCts = new CancellationTokenSource();

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _connected = false;
                            break;
                        }
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            sb.Append(Encoding.UTF8.GetString(buf, 0, result.Count));
                            if (result.EndOfMessage)
                            {
                                ProcessMessage(sb.ToString());
                                sb.Clear();
                            }
                        }
                    }
                    catch (OperationCanceledException) when (pingCts.IsCancellationRequested && !_cts.IsCancellationRequested)
                    {
                        pingCts.Dispose();
                        pingCts = new CancellationTokenSource();
                        SendRaw(new JObject { ["type"] = "ping" });
                    }
                }
            }
            catch (Exception e)
            {
                if (_connected)
                    Debug.LogError($"[Relay] Receive error: {e.Message}");
            }
            finally
            {
                pingCts?.Dispose();
                _connected = false;
                _events.Enqueue(new TransportEvent { Type = NetworkEvent.Disconnect, ClientId = ServerClientId });
            }
        }

        private void ProcessMessage(string raw)
        {
            try
            {
                var data = JObject.Parse(raw);
                switch (data["type"]?.ToString())
                {
                    case "host_ok":
                        _roomCode = data["room"]?.ToString();
                        Debug.Log($"[Relay] Room created: {_roomCode}");
                        OnRoomCodeReceived?.Invoke(_roomCode);
                        _events.Enqueue(new TransportEvent { Type = NetworkEvent.Connect, ClientId = ServerClientId });
                        break;
                    case "join_ok":
                        Debug.Log("[Relay] Joined room");
                        _events.Enqueue(new TransportEvent { Type = NetworkEvent.Connect, ClientId = ServerClientId });
                        break;
                    case "client_connected":
                        int newId = data["client_id"]?.Value<int>() ?? 0;
                        Debug.Log($"[Relay] Client {newId} connected");
                        _events.Enqueue(new TransportEvent { Type = NetworkEvent.Connect, ClientId = (ulong)newId });
                        break;
                    case "data":
                        string b64 = data["data"]?.ToString();
                        int from = data["from"]?.Value<int>() ?? 0;
                        byte[] payload = Convert.FromBase64String(b64);
                        _events.Enqueue(new TransportEvent
                        {
                            Type = NetworkEvent.Data,
                            ClientId = _isHost ? (ulong)from : ServerClientId,
                            Payload = payload,
                        });
                        break;
                    case "client_disconnected":
                        int dcId = data["client_id"]?.Value<int>() ?? 0;
                        Debug.Log($"[Relay] Client {dcId} disconnected");
                        _events.Enqueue(new TransportEvent { Type = NetworkEvent.Disconnect, ClientId = (ulong)dcId });
                        break;
                    case "room_closed":
                        Debug.Log("[Relay] Room closed");
                        _connected = false;
                        _events.Enqueue(new TransportEvent { Type = NetworkEvent.Disconnect, ClientId = ServerClientId });
                        break;
                    case "error":
                        Debug.LogError($"[Relay] Server error: {data["message"]}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Relay] Parse error: {e.Message}");
            }
        }

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery)
        {
            if (!_connected || _socket?.State != WebSocketState.Open) return;
            string b64 = Convert.ToBase64String(payload.Array, payload.Offset, payload.Count);
            if (_isHost)
                SendRaw(new JObject { ["type"] = "send", ["to"] = (int)clientId, ["data"] = b64 });
            else
                SendRaw(new JObject { ["type"] = "send", ["data"] = b64 });
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            if (_events.TryDequeue(out var e))
            {
                clientId = e.ClientId;
                payload = e.Payload != null ? new ArraySegment<byte>(e.Payload) : default;
                receiveTime = Time.realtimeSinceStartup;
                return e.Type;
            }
            clientId = default;
            payload = default;
            receiveTime = default;
            return NetworkEvent.Nothing;
        }

        public override ulong ServerClientId => 0;

        private void SendRaw(JObject msg)
        {
            if (_socket?.State != WebSocketState.Open) return;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(msg.ToString(Formatting.None));
                _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                    _cts?.Token ?? CancellationToken.None).Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Relay] Send error: {e.Message}");
            }
        }

        private void CloseSocket()
        {
            _connected = false;
            try
            {
                _cts?.Cancel();
                if (_socket?.State == WebSocketState.Open)
                    _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None)
                        .Wait(TimeSpan.FromSeconds(2));
            }
            catch { }
            finally
            {
                _socket?.Dispose();
                _socket = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void OnDestroy() => CloseSocket();

        private struct TransportEvent
        {
            public NetworkEvent Type;
            public ulong ClientId;
            public byte[] Payload;
        }
    }
}
