using LiteNetLib;
using LiteNetLib.Utils;
using SharedProtocol;

const int Port = 9050;
const string ConnectionKey = "RedGuyTakeover_v1";

var rooms = new Dictionary<string, Room>();
var peerToRoom = new Dictionary<int, string>();

EventBasedNetListener listener = new();
NetManager server = new(listener) { AutoRecycle = true };

listener.ConnectionRequestEvent += request =>
{
    request.AcceptIfKey(ConnectionKey);
};

listener.PeerConnectedEvent += peer =>
{
    Console.WriteLine($"[+] Peer connected: {peer.Id} from {peer.Address}");
};

listener.PeerDisconnectedEvent += (peer, info) =>
{
    Console.WriteLine($"[-] Peer disconnected: {peer.Id} ({info.Reason})");
    if (peerToRoom.TryGetValue(peer.Id, out var roomCode))
    {
        peerToRoom.Remove(peer.Id);
        if (rooms.TryGetValue(roomCode, out var room))
        {
            if (room.Host?.Id == peer.Id)
            {
                // Host left — notify client and destroy room
                if (room.Client != null)
                {
                    var writer = new NetDataWriter();
                    writer.Put((byte)PacketType.PeerLeft);
                    var pkt = new RoomErrorPacket { Message = "Host disconnected" };
                    pkt.Serialize(writer);
                    room.Client.Send(writer, DeliveryMethod.ReliableOrdered);
                    peerToRoom.Remove(room.Client.Id);
                }
                rooms.Remove(roomCode);
                Console.WriteLine($"[R] Room {roomCode} destroyed (host left)");
            }
            else if (room.Client?.Id == peer.Id)
            {
                room.Client = null;
                // Notify host
                var writer = new NetDataWriter();
                writer.Put((byte)PacketType.PeerLeft);
                var pkt = new PeerJoinedPacket { PlayerName = "Player 2" };
                pkt.Serialize(writer);
                room.Host?.Send(writer, DeliveryMethod.ReliableOrdered);
                Console.WriteLine($"[R] Client left room {roomCode}");
            }
        }
    }
};

listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
{
    if (reader.AvailableBytes < 1) return;
    var packetType = (PacketType)reader.GetByte();

    switch (packetType)
    {
        case PacketType.CreateRoom:
        {
            var pkt = new CreateRoomPacket();
            pkt.Deserialize(reader);

            string code = GenerateRoomCode();
            while (rooms.ContainsKey(code)) code = GenerateRoomCode();

            var room = new Room { Host = peer, HostName = pkt.PlayerName, Code = code };
            rooms[code] = room;
            peerToRoom[peer.Id] = code;

            var resp = new NetDataWriter();
            resp.Put((byte)PacketType.RoomCreated);
            new RoomCreatedPacket { RoomCode = code }.Serialize(resp);
            peer.Send(resp, DeliveryMethod.ReliableOrdered);

            Console.WriteLine($"[R] Room {code} created by {pkt.PlayerName}");
            break;
        }

        case PacketType.JoinRoom:
        {
            var pkt = new JoinRoomPacket();
            pkt.Deserialize(reader);

            string code = pkt.RoomCode.ToUpper().Trim();
            if (!rooms.TryGetValue(code, out var room))
            {
                var err = new NetDataWriter();
                err.Put((byte)PacketType.RoomError);
                new RoomErrorPacket { Message = "Room not found" }.Serialize(err);
                peer.Send(err, DeliveryMethod.ReliableOrdered);
                break;
            }

            if (room.Client != null)
            {
                var err = new NetDataWriter();
                err.Put((byte)PacketType.RoomError);
                new RoomErrorPacket { Message = "Room is full" }.Serialize(err);
                peer.Send(err, DeliveryMethod.ReliableOrdered);
                break;
            }

            room.Client = peer;
            room.ClientName = pkt.PlayerName;
            peerToRoom[peer.Id] = code;

            // Tell client they joined
            var joinResp = new NetDataWriter();
            joinResp.Put((byte)PacketType.RoomJoined);
            new RoomJoinedPacket { HostName = room.HostName }.Serialize(joinResp);
            peer.Send(joinResp, DeliveryMethod.ReliableOrdered);

            // Tell host someone joined
            var hostNotify = new NetDataWriter();
            hostNotify.Put((byte)PacketType.PeerJoined);
            new PeerJoinedPacket { PlayerName = pkt.PlayerName }.Serialize(hostNotify);
            room.Host?.Send(hostNotify, DeliveryMethod.ReliableOrdered);

            Console.WriteLine($"[R] {pkt.PlayerName} joined room {code}");
            break;
        }

        // Forward game packets to the other peer in the room
        case PacketType.GameState:
        case PacketType.PlayerInput:
        case PacketType.GameStart:
        case PacketType.GameOver:
        case PacketType.PlayerReady:
        {
            if (!peerToRoom.TryGetValue(peer.Id, out var roomCode)) break;
            if (!rooms.TryGetValue(roomCode, out var room)) break;

            // Rebuild the full packet (type byte + remaining data)
            var forward = new NetDataWriter();
            forward.Put((byte)packetType);
            forward.Put(reader.RawData, reader.Position, reader.AvailableBytes);

            var target = (room.Host?.Id == peer.Id) ? room.Client : room.Host;
            var method = packetType == PacketType.GameState
                ? DeliveryMethod.Sequenced
                : DeliveryMethod.ReliableOrdered;
            target?.Send(forward, method);
            break;
        }
    }
};

server.Start(Port);
Console.WriteLine($"Relay server started on port {Port}");
Console.WriteLine("Press Ctrl+C to stop");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.IsCancellationRequested)
{
    server.PollEvents();
    Thread.Sleep(15);
}

server.Stop();
Console.WriteLine("Server stopped.");

static string GenerateRoomCode()
{
    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    var rng = new Random();
    return new string(Enumerable.Range(0, 5).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
}

class Room
{
    public NetPeer? Host;
    public NetPeer? Client;
    public string HostName = "";
    public string ClientName = "";
    public string Code = "";
}
