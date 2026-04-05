using LiteNetLib.Utils;

namespace SharedProtocol;

public struct CreateRoomPacket : INetSerializable
{
    public string PlayerName;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(PlayerName ?? "");
    }

    public void Deserialize(NetDataReader reader)
    {
        PlayerName = reader.GetString();
    }
}

public struct JoinRoomPacket : INetSerializable
{
    public string RoomCode;
    public string PlayerName;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RoomCode ?? "");
        writer.Put(PlayerName ?? "");
    }

    public void Deserialize(NetDataReader reader)
    {
        RoomCode = reader.GetString();
        PlayerName = reader.GetString();
    }
}

public struct RoomCreatedPacket : INetSerializable
{
    public string RoomCode;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RoomCode ?? "");
    }

    public void Deserialize(NetDataReader reader)
    {
        RoomCode = reader.GetString();
    }
}

public struct RoomJoinedPacket : INetSerializable
{
    public string HostName;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(HostName ?? "");
    }

    public void Deserialize(NetDataReader reader)
    {
        HostName = reader.GetString();
    }
}

public struct RoomErrorPacket : INetSerializable
{
    public string Message;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Message ?? "");
    }

    public void Deserialize(NetDataReader reader)
    {
        Message = reader.GetString();
    }
}

public struct PeerJoinedPacket : INetSerializable
{
    public string PlayerName;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(PlayerName ?? "");
    }

    public void Deserialize(NetDataReader reader)
    {
        PlayerName = reader.GetString();
    }
}

public struct PlayerInputPacket : INetSerializable
{
    public float MoveX;
    public float MoveY;
    public float AimX;
    public float AimY;
    public bool Shooting;
    public bool Dashing;
    public bool ActivateSuper;
    public bool ActivateWall;
    public float WallAimX;
    public float WallAimY;
    public int UpgradePurchaseIndex; // -1 = none, 0+ = upgrade index

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(MoveX);
        writer.Put(MoveY);
        writer.Put(AimX);
        writer.Put(AimY);
        writer.Put(Shooting);
        writer.Put(Dashing);
        writer.Put(ActivateSuper);
        writer.Put(ActivateWall);
        writer.Put(WallAimX);
        writer.Put(WallAimY);
        writer.Put(UpgradePurchaseIndex);
    }

    public void Deserialize(NetDataReader reader)
    {
        MoveX = reader.GetFloat();
        MoveY = reader.GetFloat();
        AimX = reader.GetFloat();
        AimY = reader.GetFloat();
        Shooting = reader.GetBool();
        Dashing = reader.GetBool();
        ActivateSuper = reader.GetBool();
        ActivateWall = reader.GetBool();
        WallAimX = reader.GetFloat();
        WallAimY = reader.GetFloat();
        UpgradePurchaseIndex = reader.GetInt();
    }
}

public struct GameStatePacket : INetSerializable
{
    // Host player
    public float HostX, HostY;
    public float HostHealth, HostMaxHealth;
    public int HostScore;
    public bool HostDashing;

    // Client player (controlled by host simulation)
    public float ClientX, ClientY;
    public float ClientHealth, ClientMaxHealth;
    public bool ClientDashing;

    // Game info
    public float TimeAlive;
    public int TotalKills;
    public bool BossActive;
    public float BossX, BossY, BossHealth, BossMaxHealth;

    // Abilities
    public bool SuperActive;
    public float SuperTimer;
    public float SuperCooldown;
    public bool WallActive;
    public float WallTimer;
    public bool BoxWall;

    // Wall data (up to 4 box walls or 1 temp wall)
    public int WallCount;
    public float[] WallX;
    public float[] WallY;
    public float[] WallWidth;
    public float[] WallHeight;
    public float[] WallAngle;

    // Enemy count + packed data
    public int EnemyCount;
    public float[] EnemyX;
    public float[] EnemyY;
    public bool[] EnemyAlive;
    public int[] EnemyType;

    // Bullet count + packed data
    public int BulletCount;
    public float[] BulletX;
    public float[] BulletY;

    // Coin count + packed data
    public int CoinCount;
    public float[] CoinX;
    public float[] CoinY;

    // Enemy bullet count + packed data
    public int EnemyBulletCount;
    public float[] EnemyBulletX;
    public float[] EnemyBulletY;

    // Player death flags
    public bool HostDead;
    public bool ClientDead;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(HostX); writer.Put(HostY);
        writer.Put(HostHealth); writer.Put(HostMaxHealth);
        writer.Put(HostScore);
        writer.Put(HostDashing);

        writer.Put(ClientX); writer.Put(ClientY);
        writer.Put(ClientHealth); writer.Put(ClientMaxHealth);
        writer.Put(ClientDashing);

        writer.Put(TimeAlive);
        writer.Put(TotalKills);
        writer.Put(BossActive);
        if (BossActive)
        {
            writer.Put(BossX); writer.Put(BossY);
            writer.Put(BossHealth); writer.Put(BossMaxHealth);
        }

        writer.Put(SuperActive);
        writer.Put(SuperTimer);
        writer.Put(SuperCooldown);
        writer.Put(WallActive);
        writer.Put(WallTimer);
        writer.Put(BoxWall);

        writer.Put(WallCount);
        for (int i = 0; i < WallCount; i++)
        {
            writer.Put(WallX[i]); writer.Put(WallY[i]);
            writer.Put(WallWidth[i]); writer.Put(WallHeight[i]);
            writer.Put(WallAngle[i]);
        }

        writer.Put(EnemyCount);
        for (int i = 0; i < EnemyCount; i++)
        {
            writer.Put(EnemyX[i]); writer.Put(EnemyY[i]);
            writer.Put(EnemyAlive[i]);
            writer.Put((byte)EnemyType[i]);
        }

        writer.Put(BulletCount);
        for (int i = 0; i < BulletCount; i++)
        {
            writer.Put(BulletX[i]); writer.Put(BulletY[i]);
        }

        writer.Put(CoinCount);
        for (int i = 0; i < CoinCount; i++)
        {
            writer.Put(CoinX[i]); writer.Put(CoinY[i]);
        }

        writer.Put(EnemyBulletCount);
        for (int i = 0; i < EnemyBulletCount; i++)
        {
            writer.Put(EnemyBulletX[i]); writer.Put(EnemyBulletY[i]);
        }

        writer.Put(HostDead);
        writer.Put(ClientDead);
    }

    public void Deserialize(NetDataReader reader)
    {
        HostX = reader.GetFloat(); HostY = reader.GetFloat();
        HostHealth = reader.GetFloat(); HostMaxHealth = reader.GetFloat();
        HostScore = reader.GetInt();
        HostDashing = reader.GetBool();

        ClientX = reader.GetFloat(); ClientY = reader.GetFloat();
        ClientHealth = reader.GetFloat(); ClientMaxHealth = reader.GetFloat();
        ClientDashing = reader.GetBool();

        TimeAlive = reader.GetFloat();
        TotalKills = reader.GetInt();
        BossActive = reader.GetBool();
        if (BossActive)
        {
            BossX = reader.GetFloat(); BossY = reader.GetFloat();
            BossHealth = reader.GetFloat(); BossMaxHealth = reader.GetFloat();
        }

        SuperActive = reader.GetBool();
        SuperTimer = reader.GetFloat();
        SuperCooldown = reader.GetFloat();
        WallActive = reader.GetBool();
        WallTimer = reader.GetFloat();
        BoxWall = reader.GetBool();

        WallCount = reader.GetInt();
        WallX = new float[WallCount];
        WallY = new float[WallCount];
        WallWidth = new float[WallCount];
        WallHeight = new float[WallCount];
        WallAngle = new float[WallCount];
        for (int i = 0; i < WallCount; i++)
        {
            WallX[i] = reader.GetFloat(); WallY[i] = reader.GetFloat();
            WallWidth[i] = reader.GetFloat(); WallHeight[i] = reader.GetFloat();
            WallAngle[i] = reader.GetFloat();
        }

        EnemyCount = reader.GetInt();
        EnemyX = new float[EnemyCount];
        EnemyY = new float[EnemyCount];
        EnemyAlive = new bool[EnemyCount];
        EnemyType = new int[EnemyCount];
        for (int i = 0; i < EnemyCount; i++)
        {
            EnemyX[i] = reader.GetFloat(); EnemyY[i] = reader.GetFloat();
            EnemyAlive[i] = reader.GetBool();
            EnemyType[i] = reader.GetByte();
        }

        BulletCount = reader.GetInt();
        BulletX = new float[BulletCount];
        BulletY = new float[BulletCount];
        for (int i = 0; i < BulletCount; i++)
        {
            BulletX[i] = reader.GetFloat(); BulletY[i] = reader.GetFloat();
        }

        CoinCount = reader.GetInt();
        CoinX = new float[CoinCount];
        CoinY = new float[CoinCount];
        for (int i = 0; i < CoinCount; i++)
        {
            CoinX[i] = reader.GetFloat(); CoinY[i] = reader.GetFloat();
        }

        EnemyBulletCount = reader.GetInt();
        EnemyBulletX = new float[EnemyBulletCount];
        EnemyBulletY = new float[EnemyBulletCount];
        for (int i = 0; i < EnemyBulletCount; i++)
        {
            EnemyBulletX[i] = reader.GetFloat(); EnemyBulletY[i] = reader.GetFloat();
        }

        HostDead = reader.GetBool();
        ClientDead = reader.GetBool();
    }
}
