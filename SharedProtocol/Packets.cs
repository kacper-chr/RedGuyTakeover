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
    public bool ActivateBlink;
    public float BlinkAimX;
    public float BlinkAimY;
    public bool PlaceTurret;
    public float TurretAimX;
    public float TurretAimY;
    public bool ActivateDecoy;
    public float DecoyAimX;
    public float DecoyAimY;
    public bool ActivateSpeedTrap;
    public float SpeedTrapAimX;
    public float SpeedTrapAimY;

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
        writer.Put(ActivateBlink);
        writer.Put(BlinkAimX);
        writer.Put(BlinkAimY);
        writer.Put(PlaceTurret);
        writer.Put(TurretAimX);
        writer.Put(TurretAimY);
        writer.Put(ActivateDecoy);
        writer.Put(DecoyAimX);
        writer.Put(DecoyAimY);
        writer.Put(ActivateSpeedTrap);
        writer.Put(SpeedTrapAimX);
        writer.Put(SpeedTrapAimY);
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
        ActivateBlink = reader.GetBool();
        BlinkAimX = reader.GetFloat();
        BlinkAimY = reader.GetFloat();
        PlaceTurret = reader.GetBool();
        TurretAimX = reader.GetFloat();
        TurretAimY = reader.GetFloat();
        ActivateDecoy = reader.GetBool();
        DecoyAimX = reader.GetFloat();
        DecoyAimY = reader.GetFloat();
        ActivateSpeedTrap = reader.GetBool();
        SpeedTrapAimX = reader.GetFloat();
        SpeedTrapAimY = reader.GetFloat();
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
    public float ClientDashCooldown;

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
    // Effect flag bitmask per enemy:
    //   bit 0 = runner, bit 1 = berserker, bit 2 = parasitic,
    //   bit 3 = phasing, bit 4 = visible
    public byte[] EnemyEffectFlags;
    // Enemy health encoded as byte (health * 16, clamped 0-255)
    public byte[] EnemyHealthPacked;

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

    // Reload state
    public bool Reloading;
    public float ReloadProgress; // 0-1

    // Shared upgrade visuals (synced so client can render correctly)
    public bool FlameWall;
    public int OrbitCount;
    public float OrbitAngle;
    public float OrbitRadiusBonus;
    public float PlayerSize;

    // Turret positions for client rendering
    public int TurretCount;
    public float[] TurretX;
    public float[] TurretY;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(HostX); writer.Put(HostY);
        writer.Put(HostHealth); writer.Put(HostMaxHealth);
        writer.Put(HostScore);
        writer.Put(HostDashing);

        writer.Put(ClientX); writer.Put(ClientY);
        writer.Put(ClientHealth); writer.Put(ClientMaxHealth);
        writer.Put(ClientDashing);
        writer.Put(ClientDashCooldown);

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

        writer.Put((ushort)EnemyCount);
        for (int i = 0; i < EnemyCount; i++)
        {
            // ushort-encoded normalized coordinates (0-1 -> 0-65535)
            writer.Put(NormToUShort(EnemyX[i])); writer.Put(NormToUShort(EnemyY[i]));
            writer.Put(EnemyAlive[i]);
            writer.Put((byte)EnemyType[i]);
            writer.Put(EnemyEffectFlags != null && i < EnemyEffectFlags.Length ? EnemyEffectFlags[i] : (byte)0);
            writer.Put(EnemyHealthPacked != null && i < EnemyHealthPacked.Length ? EnemyHealthPacked[i] : (byte)0);
        }

        writer.Put((ushort)BulletCount);
        for (int i = 0; i < BulletCount; i++)
        {
            writer.Put(NormToUShort(BulletX[i])); writer.Put(NormToUShort(BulletY[i]));
        }

        writer.Put((ushort)CoinCount);
        for (int i = 0; i < CoinCount; i++)
        {
            writer.Put(NormToUShort(CoinX[i])); writer.Put(NormToUShort(CoinY[i]));
        }

        writer.Put((ushort)EnemyBulletCount);
        for (int i = 0; i < EnemyBulletCount; i++)
        {
            writer.Put(NormToUShort(EnemyBulletX[i])); writer.Put(NormToUShort(EnemyBulletY[i]));
        }

        writer.Put(HostDead);
        writer.Put(ClientDead);

        writer.Put(Reloading);
        writer.Put(ReloadProgress);

        writer.Put(FlameWall);
        writer.Put(OrbitCount);
        writer.Put(OrbitAngle);
        writer.Put(OrbitRadiusBonus);
        writer.Put(PlayerSize);

        writer.Put((byte)TurretCount);
        for (int i = 0; i < TurretCount; i++)
        {
            writer.Put(TurretX[i]); writer.Put(TurretY[i]);
        }
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
        ClientDashCooldown = reader.GetFloat();

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

        EnemyCount = reader.GetUShort();
        EnemyX = new float[EnemyCount];
        EnemyY = new float[EnemyCount];
        EnemyAlive = new bool[EnemyCount];
        EnemyType = new int[EnemyCount];
        EnemyEffectFlags = new byte[EnemyCount];
        EnemyHealthPacked = new byte[EnemyCount];
        for (int i = 0; i < EnemyCount; i++)
        {
            EnemyX[i] = UShortToNorm(reader.GetUShort()); EnemyY[i] = UShortToNorm(reader.GetUShort());
            EnemyAlive[i] = reader.GetBool();
            EnemyType[i] = reader.GetByte();
            EnemyEffectFlags[i] = reader.GetByte();
            EnemyHealthPacked[i] = reader.GetByte();
        }

        BulletCount = reader.GetUShort();
        BulletX = new float[BulletCount];
        BulletY = new float[BulletCount];
        for (int i = 0; i < BulletCount; i++)
        {
            BulletX[i] = UShortToNorm(reader.GetUShort()); BulletY[i] = UShortToNorm(reader.GetUShort());
        }

        CoinCount = reader.GetUShort();
        CoinX = new float[CoinCount];
        CoinY = new float[CoinCount];
        for (int i = 0; i < CoinCount; i++)
        {
            CoinX[i] = UShortToNorm(reader.GetUShort()); CoinY[i] = UShortToNorm(reader.GetUShort());
        }

        EnemyBulletCount = reader.GetUShort();
        EnemyBulletX = new float[EnemyBulletCount];
        EnemyBulletY = new float[EnemyBulletCount];
        for (int i = 0; i < EnemyBulletCount; i++)
        {
            EnemyBulletX[i] = UShortToNorm(reader.GetUShort()); EnemyBulletY[i] = UShortToNorm(reader.GetUShort());
        }

        HostDead = reader.GetBool();
        ClientDead = reader.GetBool();

        Reloading = reader.GetBool();
        ReloadProgress = reader.GetFloat();

        FlameWall = reader.GetBool();
        OrbitCount = reader.GetInt();
        OrbitAngle = reader.GetFloat();
        OrbitRadiusBonus = reader.GetFloat();
        PlayerSize = reader.GetFloat();

        TurretCount = reader.GetByte();
        TurretX = new float[TurretCount];
        TurretY = new float[TurretCount];
        for (int i = 0; i < TurretCount; i++)
        {
            TurretX[i] = reader.GetFloat(); TurretY[i] = reader.GetFloat();
        }
    }

    private static ushort NormToUShort(float v)
    {
        if (v < 0f) v = 0f; else if (v > 1f) v = 1f;
        return (ushort)(v * 65535f);
    }

    private static float UShortToNorm(ushort v) => v / 65535f;
}
