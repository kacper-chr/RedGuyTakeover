using LiteNetLib;
using LiteNetLib.Utils;
using SharedProtocol;

namespace gamething;

public class NetworkManager
{
    private const string ConnectionKey = "RedGuyTakeover_v1";
    private const int ServerPort = 9050;

    private EventBasedNetListener _listener;
    private NetManager _client;
    private NetPeer? _serverPeer;
    private NetDataWriter _writer = new();

    public bool IsConnected => _serverPeer != null && _serverPeer.ConnectionState == ConnectionState.Connected;
    public bool IsHost { get; private set; }
    public bool IsInRoom { get; private set; }
    public bool PeerReady { get; private set; }
    public string RoomCode { get; private set; } = "";
    public string PeerName { get; private set; } = "";
    public string ErrorMessage { get; private set; } = "";
    public string StatusMessage { get; private set; } = "Disconnected";

    // Events
    public event Action<GameStatePacket>? OnGameStateReceived;
    public event Action<PlayerInputPacket>? OnPlayerInputReceived;
    public event Action? OnGameStartReceived;
    public event Action? OnPeerJoined;
    public event Action? OnPeerLeft;
    public event Action? OnRoomCreated;
    public event Action? OnRoomJoined;
    public event Action<string>? OnError;

    public NetworkManager()
    {
        _listener = new EventBasedNetListener();
        _client = new NetManager(_listener) { AutoRecycle = true, IPv6Enabled = false };

        _listener.PeerConnectedEvent += peer =>
        {
            _serverPeer = peer;
            StatusMessage = "Connected to relay";
        };

        _listener.PeerDisconnectedEvent += (peer, info) =>
        {
            _serverPeer = null;
            IsInRoom = false;
            PeerReady = false;
            StatusMessage = "Disconnected";
            if (info.Reason != DisconnectReason.DisconnectPeerCalled)
            {
                ErrorMessage = $"Disconnected: {info.Reason}";
                OnError?.Invoke(ErrorMessage);
            }
        };

        _listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
        {
            if (reader.AvailableBytes < 1) return;
            var packetType = (PacketType)reader.GetByte();
            HandlePacket(packetType, reader);
        };
    }

    public void Connect(string serverIp)
    {
        if (_client.IsRunning) _client.Stop();
        _client.Start();
        _serverPeer = null;
        IsInRoom = false;
        PeerReady = false;
        ErrorMessage = "";
        StatusMessage = "Connecting...";
        _client.Connect(serverIp, ServerPort, ConnectionKey);
    }

    public void Disconnect()
    {
        _serverPeer?.Disconnect();
        _serverPeer = null;
        IsInRoom = false;
        PeerReady = false;
        StatusMessage = "Disconnected";
        _client.Stop();
    }

    public void CreateRoom(string playerName)
    {
        if (!IsConnected) return;
        IsHost = true;
        _writer.Reset();
        _writer.Put((byte)PacketType.CreateRoom);
        new CreateRoomPacket { PlayerName = playerName }.Serialize(_writer);
        _serverPeer!.Send(_writer, DeliveryMethod.ReliableOrdered);
        StatusMessage = "Creating room...";
    }

    public void JoinRoom(string roomCode, string playerName)
    {
        if (!IsConnected) return;
        IsHost = false;
        _writer.Reset();
        _writer.Put((byte)PacketType.JoinRoom);
        new JoinRoomPacket { RoomCode = roomCode, PlayerName = playerName }.Serialize(_writer);
        _serverPeer!.Send(_writer, DeliveryMethod.ReliableOrdered);
        StatusMessage = "Joining room...";
    }

    public void SendGameState(GameStatePacket state)
    {
        if (!IsConnected || !IsInRoom) return;
        _writer.Reset();
        _writer.Put((byte)PacketType.GameState);
        state.Serialize(_writer);
        _serverPeer!.Send(_writer, DeliveryMethod.Sequenced);
    }

    public void SendPlayerInput(PlayerInputPacket input)
    {
        if (!IsConnected || !IsInRoom) return;
        _writer.Reset();
        _writer.Put((byte)PacketType.PlayerInput);
        input.Serialize(_writer);
        _serverPeer!.Send(_writer, DeliveryMethod.Sequenced);
    }

    public void SendGameStart()
    {
        if (!IsConnected || !IsInRoom) return;
        _writer.Reset();
        _writer.Put((byte)PacketType.GameStart);
        _serverPeer!.Send(_writer, DeliveryMethod.ReliableOrdered);
    }

    public void PollEvents()
    {
        if (_client.IsRunning)
            _client.PollEvents();
    }

    private void HandlePacket(PacketType type, NetDataReader reader)
    {
        switch (type)
        {
            case PacketType.RoomCreated:
            {
                var pkt = new RoomCreatedPacket();
                pkt.Deserialize(reader);
                RoomCode = pkt.RoomCode;
                IsInRoom = true;
                StatusMessage = $"Room {RoomCode} — waiting for player...";
                OnRoomCreated?.Invoke();
                break;
            }

            case PacketType.RoomJoined:
            {
                var pkt = new RoomJoinedPacket();
                pkt.Deserialize(reader);
                PeerName = pkt.HostName;
                IsInRoom = true;
                StatusMessage = $"Joined {PeerName}'s room";
                OnRoomJoined?.Invoke();
                break;
            }

            case PacketType.RoomError:
            {
                var pkt = new RoomErrorPacket();
                pkt.Deserialize(reader);
                ErrorMessage = pkt.Message;
                StatusMessage = pkt.Message;
                OnError?.Invoke(pkt.Message);
                break;
            }

            case PacketType.PeerJoined:
            {
                var pkt = new PeerJoinedPacket();
                pkt.Deserialize(reader);
                PeerName = pkt.PlayerName;
                PeerReady = true;
                StatusMessage = $"{PeerName} joined!";
                OnPeerJoined?.Invoke();
                break;
            }

            case PacketType.PeerLeft:
            {
                PeerReady = false;
                PeerName = "";
                StatusMessage = "Peer disconnected";
                OnPeerLeft?.Invoke();
                break;
            }

            case PacketType.GameState:
            {
                var pkt = new GameStatePacket();
                pkt.Deserialize(reader);
                OnGameStateReceived?.Invoke(pkt);
                break;
            }

            case PacketType.PlayerInput:
            {
                var pkt = new PlayerInputPacket();
                pkt.Deserialize(reader);
                OnPlayerInputReceived?.Invoke(pkt);
                break;
            }

            case PacketType.GameStart:
            {
                OnGameStartReceived?.Invoke();
                break;
            }
        }
    }
}
