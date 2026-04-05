namespace SharedProtocol;

public enum PacketType : byte
{
    // Relay management
    CreateRoom = 1,
    JoinRoom = 2,
    RoomCreated = 3,
    RoomJoined = 4,
    RoomError = 5,
    PeerJoined = 6,
    PeerLeft = 7,

    // Game data (forwarded by relay)
    GameState = 10,
    PlayerInput = 11,
    GameStart = 12,
    GameOver = 13,
}
