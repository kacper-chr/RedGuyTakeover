using LiteNetLib;
using LiteNetLib.Utils;
using SharedProtocol;

namespace gamething;

public class EmbeddedRelay
{
    public const int Port = 9050;
    private const string ConnectionKey = "RedGuyTakeover_v1";

    private NetManager? _server;
    private EventBasedNetListener? _listener;
    private Thread? _pollThread;
    private volatile bool _running;

    private readonly Dictionary<string, Room> _rooms = new();
    private readonly Dictionary<int, string> _peerToRoom = new();

    public bool IsRunning => _running;

    public void Start()
    {
        if (_running) return;

        _listener = new EventBasedNetListener();
        _server = new NetManager(_listener) { AutoRecycle = true, IPv6Enabled = false };

        _listener.ConnectionRequestEvent += request =>
        {
            request.AcceptIfKey(ConnectionKey);
        };

        _listener.PeerDisconnectedEvent += (peer, info) =>
        {
            HandleDisconnect(peer);
        };

        _listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
        {
            if (reader.AvailableBytes < 1) return;
            var packetType = (PacketType)reader.GetByte();
            HandlePacket(peer, packetType, reader);
        };

        _server.Start(Port);
        _running = true;

        _pollThread = new Thread(PollLoop) { IsBackground = true, Name = "RelayPoll" };
        _pollThread.Start();
    }

    public void Stop()
    {
        _running = false;
        _server?.Stop();
        _rooms.Clear();
        _peerToRoom.Clear();
    }

    private void PollLoop()
    {
        while (_running)
        {
            try { _server?.PollEvents(); }
            catch { }
            Thread.Sleep(15);
        }
    }

    private void HandleDisconnect(NetPeer peer)
    {
        if (!_peerToRoom.TryGetValue(peer.Id, out var roomCode)) return;
        _peerToRoom.Remove(peer.Id);

        if (!_rooms.TryGetValue(roomCode, out var room)) return;

        if (room.Host?.Id == peer.Id)
        {
            if (room.Client != null)
            {
                var w = new NetDataWriter();
                w.Put((byte)PacketType.PeerLeft);
                new RoomErrorPacket { Message = "Host disconnected" }.Serialize(w);
                room.Client.Send(w, DeliveryMethod.ReliableOrdered);
                _peerToRoom.Remove(room.Client.Id);
            }
            _rooms.Remove(roomCode);
        }
        else if (room.Client?.Id == peer.Id)
        {
            room.Client = null;
            var w = new NetDataWriter();
            w.Put((byte)PacketType.PeerLeft);
            new PeerJoinedPacket { PlayerName = "Player 2" }.Serialize(w);
            room.Host?.Send(w, DeliveryMethod.ReliableOrdered);
        }
    }

    private void HandlePacket(NetPeer peer, PacketType type, NetDataReader reader)
    {
        switch (type)
        {
            case PacketType.CreateRoom:
            {
                var pkt = new CreateRoomPacket();
                pkt.Deserialize(reader);

                string code = GenerateCode();
                while (_rooms.ContainsKey(code)) code = GenerateCode();

                _rooms[code] = new Room { Host = peer, HostName = pkt.PlayerName, Code = code };
                _peerToRoom[peer.Id] = code;

                var resp = new NetDataWriter();
                resp.Put((byte)PacketType.RoomCreated);
                new RoomCreatedPacket { RoomCode = code }.Serialize(resp);
                peer.Send(resp, DeliveryMethod.ReliableOrdered);
                break;
            }

            case PacketType.JoinRoom:
            {
                var pkt = new JoinRoomPacket();
                pkt.Deserialize(reader);
                string code = pkt.RoomCode.ToUpper().Trim();

                if (!_rooms.TryGetValue(code, out var room))
                {
                    SendError(peer, "Room not found");
                    break;
                }
                if (room.Client != null)
                {
                    SendError(peer, "Room is full");
                    break;
                }

                room.Client = peer;
                room.ClientName = pkt.PlayerName;
                _peerToRoom[peer.Id] = code;

                var joinResp = new NetDataWriter();
                joinResp.Put((byte)PacketType.RoomJoined);
                new RoomJoinedPacket { HostName = room.HostName }.Serialize(joinResp);
                peer.Send(joinResp, DeliveryMethod.ReliableOrdered);

                var hostNotify = new NetDataWriter();
                hostNotify.Put((byte)PacketType.PeerJoined);
                new PeerJoinedPacket { PlayerName = pkt.PlayerName }.Serialize(hostNotify);
                room.Host?.Send(hostNotify, DeliveryMethod.ReliableOrdered);
                break;
            }

            case PacketType.GameState:
            case PacketType.PlayerInput:
            case PacketType.GameStart:
            case PacketType.GameOver:
            case PacketType.PlayerReady:
            {
                if (!_peerToRoom.TryGetValue(peer.Id, out var roomCode)) break;
                if (!_rooms.TryGetValue(roomCode, out var room)) break;

                var forward = new NetDataWriter();
                forward.Put((byte)type);
                forward.Put(reader.RawData, reader.Position, reader.AvailableBytes);

                var target = (room.Host?.Id == peer.Id) ? room.Client : room.Host;
                var method = type == PacketType.GameState
                    ? DeliveryMethod.Sequenced
                    : DeliveryMethod.ReliableOrdered;
                target?.Send(forward, method);
                break;
            }
        }
    }

    private static void SendError(NetPeer peer, string message)
    {
        var w = new NetDataWriter();
        w.Put((byte)PacketType.RoomError);
        new RoomErrorPacket { Message = message }.Serialize(w);
        peer.Send(w, DeliveryMethod.ReliableOrdered);
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new Random();
        return new string(Enumerable.Range(0, 5).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }

    private class Room
    {
        public NetPeer? Host;
        public NetPeer? Client;
        public string HostName = "";
        public string ClientName = "";
        public string Code = "";
    }
}
