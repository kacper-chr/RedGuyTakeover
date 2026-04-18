using AutoUpdaterDotNET;
using SharedProtocol;

namespace gamething
{
    public partial class gameForm : Form
    {
        private static readonly Random rng = new Random();
        // --- Cached GDI+ resources (avoid allocating per frame) ---
        private Font? _fontUI;
        private Font? _fontUIBold;
        private Font? _fontTitle;
        private Font? _fontTitleLarge;
        private Font? _fontSmall;
        private Font? _fontEmoji;
        private float _cachedScaleY = -1f;

        private Font GetFontUI() { EnsureFontCache(); return _fontUI!; }
        private Font GetFontUIBold() { EnsureFontCache(); return _fontUIBold!; }
        private Font GetFontTitle() { EnsureFontCache(); return _fontTitle!; }
        private Font GetFontTitleLarge() { EnsureFontCache(); return _fontTitleLarge!; }
        private Font GetFontSmall() { EnsureFontCache(); return _fontSmall!; }
        private Font GetFontEmoji() { EnsureFontCache(); return _fontEmoji!; }

        private void EnsureFontCache()
        {
            if (_cachedScaleY == scaleY) return;
            _fontUI?.Dispose(); _fontUIBold?.Dispose(); _fontTitle?.Dispose();
            _fontTitleLarge?.Dispose(); _fontSmall?.Dispose(); _fontEmoji?.Dispose();
            _fontUI = new Font("Arial", 10 * scaleY);
            _fontUIBold = new Font("Arial", 10 * scaleY, FontStyle.Bold);
            _fontTitle = new Font("Arial", 22 * scaleY, FontStyle.Bold);
            _fontTitleLarge = new Font("Arial", 32 * scaleY);
            _fontEmoji = new Font("Segoe UI Emoji", 22 * scaleY, FontStyle.Bold);
            _fontSmall = new Font("Arial", 9 * scaleY);
            _cachedScaleY = scaleY;
        }
        // --- Position of player and enemy ---
        private float posX = 960f;
        private float posY = 540f;
        private List<(float x, float y)> enemies =
[
      (0f, 0f),
];
        // --- Velocity (changed by WASD, applied each tick)  ---
        private float velocityX = 0f;
        private float velocityY = 0f;
        // --- Box appearance ---
        private int boxSize = 30;
        private int playerSize = 30;
        // --- Tweak this to change speed ---
        private float speed = 4.8f;
        private const float enemySpeed = 5f;
        // --- bullet stuff ---
        private List<(float x, float y, float velX, float velY, int bounces)> bullets = new();
        private float bulletSpeed = 15f; // base value, scaled in ResetGame
        private int bulletSize = 6;
        private bool mouseHeld = false;
        private float shootCooldown = 0;
        private float shootRate = 10f / 60f;
        // --- Delta Time ---
        private float deltaTime = 0;
        private DateTime lastTick = DateTime.Now;
        // --- Dash ---
        private bool isDashing = false;
        private float dashTimer = 0f;
        private float dashVelX = 0f;
        private float dashVelY = 0f;
        private float dashDuration = 0.7f;
        private float dashPower = 12;
        private float dashCooldown = 0f;
        private float dashCooldownTime = 3f;
        // --- Score ---
        private int score = 0;
        private float scoreTimer = 0f;
        private float scoreTimerMax = 1;
        // --- Random shete ---
        private bool isPaused = false;
        private float gameStartTimer = 0f;
        private float gameStartDelay = 2f;
        private bool isExiting = false;
        private bool darkMode = false;
        private Color playerColor = Color.FromArgb(0, 50, 255);
        // --- Player Sprite ---
        private Bitmap? playerSpriteCropped = null;
        private Bitmap? _tintedPlayerSprite = null;     // cached colorized copy for current playerColor
        private Color   _tintedForColor       = Color.Empty;
        // Fractions of the cropped sprite image (measured from the blue_guy.png source)
        private const float SpriteBodyCenterFracX = 0.367f; // where the body center sits in cropped image
        private const float SpriteBodyCenterFracY = 0.635f;
        private const float SpriteGunTipFracX     = 0.947f; // where the gun barrel tip is
        private const float SpriteGunTipFracY     = 0.040f;
        private const float SpriteBaseAngle       = -0.803f; // radians: angle from body-center to gun-tip in the unrotated sprite (~-46°)
        private const float SpriteDrawScale       = 2.0f;    // draw sprite at this multiple of playerSize
        private float _lastValidAimAngle           = 0f;
        private float _prevAimAngle                = 0f;     // last tick's aim angle, for angular velocity
        private List<float> enemySmackCooldown    = new List<float>();
        private const float GunSmackAngularVelThreshold = 12f;    // rad/s (~688°/s) -- swing fast to trigger
        private const float GunSmackDamage              = 0.5f;
        private const float GunSmackCooldownTime        = 0.35f; // per-enemy cooldown between smacks
        // --- Red enemy sprite ---
        private Bitmap? redEnemySpriteCropped = null;
        private Bitmap? redParasiticSpriteCropped = null;
        private Bitmap? gunnerSpriteCropped = null;
        private Bitmap? gunnerParasiticSpriteCropped = null;
        private Bitmap? tankSpriteCropped = null;
        private Bitmap? tankParasiticSpriteCropped = null;
        private Bitmap? runnerSpriteCropped = null;
        private Bitmap? runnerParasiticSpriteCropped = null;
        private Bitmap? bossSpriteCropped = null;
        private float bossAimAngle = 0f;
        private Bitmap? cardSpriteBitmap = null;
        private Dictionary<string, Bitmap> tintedCardCache = new Dictionary<string, Bitmap>();
        private const float EnemySpriteBodyCenterFracX = 0.5f;
        private const float EnemySpriteBodyCenterFracY = 0.45f;
        private const float EnemySpriteBaseAngle       = 1.5708f; // π/2 — unrotated sprite faces down in screen space
        // Boss gun-tip in sprite UV space (0..1), relative to body center.
        // Tweak these to align bullet origin with where the gun actually is on boss.png.
        // X: 0.5 = sprite center, >0.5 = right of body (perpendicular to facing dir)
        // Y: 0.5 = body center, 1.0 = bottom of sprite (along facing dir)
        private const float BossGunTipFracX = 0.72f;
        private const float BossGunTipFracY = 0.95f;
        private const float EnemySpriteDrawScale       = 1.9f;   // draw sprite at this multiple of enemy size
        private const float GunnerExtraScale           = 1.15f;  // gunners are a little bigger than regular red guys
        private const float EnemyMaxRotSpeed           = 3.0f;   // rad/s — how fast an enemy can swing to face the player
        private List<float> enemyAimAngle             = new List<float>();
        private string playerName = "YOU";
        private List<(float x, float y, float timer, float maxTimer, int size)> deathFlashes = new List<(float x, float y, float timer, float maxTimer, int size)>();
        private List<(float x, float y, float timer, float maxTimer, int size)> hitFlashes = new List<(float x, float y, float timer, float maxTimer, int size)>();
        // --- Juice / polish state ---
        private float screenShakeAmp = 0f;          // current shake amplitude (px), decays each frame
        private float shakeOffsetX = 0f, shakeOffsetY = 0f;
        private List<(float x, float y, float vy, float timer, float maxTimer, string text, Color color)> damageNumbers = new();
        private float hurtVignette = 0f;            // 0..1, bumped on damage, decays
        private float displayedScore = 0f;          // smooth-ticked score for HUD
        private float displayedHealth = 0f;         // smooth-lerped health for HUD bar
        private List<(float x, float y, float timer, float maxTimer)> coinSparkles = new();
        private List<float> enemySpawnAnim = new(); // 0..1 grow factor parallel to enemies
        private Bitmap? pauseBlurFrame = null;      // cached blurred snapshot when paused
        // Bullet trails — short fading line behind each bullet
        private List<(float x1, float y1, float x2, float y2, float timer, float maxTimer)> bulletTrails = new();
        // Muzzle flashes — brief yellow burst at gun tip on shoot
        private List<(float x, float y, float angle, float timer, float maxTimer)> muzzleFlashes = new();
        // Death fragments — exploding sprite pieces when enemies die
        private List<(float x, float y, float vx, float vy, float angle, float angVel, float timer, float maxTimer, Color color, float size)> deathFragments = new();
        // Combo / kill-streak counter
        private int comboCount = 0;
        private float comboTimer = 0f;             // seconds since last kill — resets combo when expires
        private const float comboWindow = 2.5f;
        private float comboShake = 0f;             // visual pop on the combo HUD
        private int bestCombo = 0;
        private Button? pauseQuitBtn = null;
        // --- Coins ---
        private List<(float x, float y, float velX, float velY)> coins = new List<(float x, float y, float velX, float velY)>();
        private int coinSize = 6;
        private int coinWorth = 6;
        // --- Enemy Respawns ---
        private List<float> enemyRespawnTimers = new List<float>();
        private const float enemyRespawnTime = 3f;
        private List<bool> enemyAlive = new List<bool>();
        private float enemySpawnTimer = 0f;
        private const float enemySpawnRate = 10f;
        // --- Walls ---
        private List<(float x, float y, float width, float height)> walls = new List<(float x, float y, float width, float height)>
        {
        };
        // --- Super ---
        private bool superActive = false;
        private float superTimer = 0f;
        private float superCooldown = 0f;
        private const float superDuration = 10f;
        private float superCooldownTime = 90f;
        // --- Summon Walls ---
        private bool wallActive = false;
        private float wallTimer = 0f;
        private float wallCooldown = 0f;
        private float wallDuration = 20f;
        private const float wallCooldownTime = 5f;
        private (float x, float y, float width, float height, float angle) tempWall;
        private float wallLength = 240f;
        // --- Ammo Stuff ---
        private int ammo = 60;
        private int maxAmmo = 60;
        private bool reloading = false;
        private float reloadTimer = 0f;
        private float reloadTime = 3f;
        // --- Health ---
        private float health = 50;
        private float maxHealth = 50f;
        private float hitCooldown = 0f;
        private const float hitCooldownTime = 0.1f;
        private float regenTimer = 0f;
        private const float regenTime = 5f;
        // --- Upgrades ---
        private HashSet<int> purchasedOneTimeUpgrades = new HashSet<int>();
        private int lifeSteal = 0;
        private float fireRateBonus = 0f;
        private int scorePerSecond = 1;
        private bool ghostDash = false;
        private List<(float x, float y, float timer)> dashTrail = new List<(float x, float y, float timer)>();
        private const float dashTrailDuration = 0.5f;
        private const float dashTrailDamage = 20f;
        private int ricochetBounces = 0;
        private bool smartBounce = false; // bounced bullets seek the nearest enemy
        private bool afterburn = false;
        private bool isAfterburn = false;
        private float afterburnTimer = 0f;
        private const float afterburnDuration = 2f;
        private const float afterburnSpeed = 1.5f;
        private bool blink = false;
        private float blinkCooldown = 0f;
        private float blinkCooldownTime = 60f;
        private bool jackpot = false;
        private bool piercingBullets = false;
        private bool decoy = false;
        private float decoyTimer = 0f;
        private float decoyX = 0f;
        private float decoyY = 0f;
        private bool decoyActive = false;
        private float decoyDuration = 5f;
        private float decoyCooldown = 0f;
        private const float decoyCooldownTime = 20f;
        private bool homing = false;
        private int blowback = 0;
        private const float blowbackRadius = 200f;
        private const float blowbackForce = 15f;
        private bool toughLove = false;
        private int orbitCount = 0;
        private float orbitAngle = 0f;
        private const float orbitRadius = 60f;
        private const float orbitSpeed = 3f;
        private float orbitRadiusBonus = 0f;
        private bool boxWall = false;
        private List<(float x, float y, float width, float height, float angle)> boxWalls = new List<(float x, float y, float width, float height, float angle)>();
        private bool medic = false;
        private bool doubleTap = false;
        private int doubleTapCounter = 0;
        private bool explosiveFinish = false;
        private bool nextBulletIsLast = false;
        private bool flameWall = false;
        private const float flameWallDamageRate = 0.5f;
        private const float flameWallDamage = 1f;
        private List<float> enemyFlameTimers = new List<float>();
        private bool shrapnel = false;
        private bool speedTrap = false;
        private bool speedTrapActive = false;
        private float speedTrapX = 0f;
        private float speedTrapY = 0f;
        private float speedTrapTimer = 0f;
        private float speedTrapCooldown = 0f;
        private const float speedTrapDuration = 5f;
        private const float speedTrapCooldownTime = 20f;
        private const float speedTrapRadius = 150f;
        private const float speedTrapSlowMultiplier = 0.3f;
        private bool thorns = false;
        private bool cashback = false;
        private float cashbackTimer = 0f;
        private float cashbackAmount = 0f;
        private const float cashbackInterval = 30f;
        private float totalSpentSinceLastCashback = 0f;
        private bool orbitalStrike = false;
        private const float orbitalSlowMultiplier = 0.5f;
        private List<int> orbitalSlowedEnemies = new List<int>();
        private List<float> orbitalSlowTimers = new List<float>();

        private bool turret = false;
        private List<(float x, float y)> turrets = new List<(float x, float y)>();
        private List<float> turretShootTimers = new List<float>();
        private const float turretShootRate = 1.5f;
        private const float turretRange = 300f;
        private float turretCooldown = 0f;
        private const float turretCooldownTime = 25f;
        private bool turretActive = false;

        private bool rapidReload = false;

        private bool explosiveOrbit = false;
        private const float explosiveOrbitRadius = 80f;
        private const float explosiveOrbitDamage = 1f;

        private bool ricochetExplosion = false;
        private const float ricochetExplosionRadius = 100f;
        private const float ricochetExplosionDamage = 2f;

        private bool bloodMoney = false;

        private bool parasiteImmune = false;

        private bool lastStand = false;
        // --- Boss ---
        private bool bossAlive = false;
        private float bossX = 0f;
        private float bossY = 0f;
        private float bossHealth = 500f;
        private float bossMaxHealth = 300f;
        private const float bossDamage = 8f;
        private const float bossSize = 240f;
        private const float bossSpeed = 3f;
        private float bossSpawnTimer = 0f;
        private const float bossSpawnInterval = 120f;
        private float bossHitCooldown = 0f;

        private float bossShootTimer = 0f;
        private const float bossShootRate = 2f;
        private float bossBulletSpeed = 40f;
        private float bossBulletHitCooldown = 0f;
        private const float bossBulletHitCooldownTime = 0.3f;
        private float bossOrbitHitCooldown = 0f;
        private const float bossOrbitHitCooldownTime = 0.3f;
        private float bossFlameTimer = 0f;

        private int bossesDefeated = 0;

        private int bossesDefeatedOnDifficulty = 0;
        private float currentBossMaxHealth = 100f;
        private float currentBossShootRate = 2f;
        // --- FPS ---
        private int fpsFrames = 0;
        private float fpsTimer = 0f;
        private float currentFPS = 0f;
        // --- Enemy Buffs ---
        private float enemyBuffTimer = 0f;
        private const float enemyBuffInterval = 30f;
        private float currentEnemySpeed = 5f;
        private float enemyReinforceChance = 0f;
        private const float maxReinforceChance = 0.5f;
        private string buffMessage = "";
        private float buffMessageTimer = 0f;
        private const float buffMessageDuration = 3f;
        private float enemyDamage = 1f;
        // --- Game Over Screen ---
        private int totalKills = 0;
        private float timeAlive = 0f;
        private float totalScore = 0f;
        // --- Stronger Enemies ---
        private List<bool> enemyCanShoot = new List<bool>();
        private List<float> enemyShootTimers = new List<float>();
        private List<(float x, float y, float velX, float velY)> enemyBullets = new List<(float x, float y, float velX, float velY)>();
        private float shootingEnemyChance = 0.02f;
        private const float maxShootingEnemyChance = 0.30f;
        private float enemyBulletSpeed = 12f;
        private const float enemyShootRate = 1f;
        private int enemyBulletSize = 10;
        private const float enemyBulletDamage = 3f;
        private List<bool> enemyIsTank = new List<bool>();
        private float tankEnemyChance = 0.02f;
        private const float maxTankEnemyChance = 0.30f;
        private List<bool> enemyIsRunner = new List<bool>();
        private float runnerEnemyChance = 0.01f;
        private const float maxRunnerEnemyChance = 0.50f;
        private const float runnerSpeedMultiplier = 1.9f;
        // --- Enemy Health ---
        private List<float> enemyHealth = new List<float>();
        // --- Game Scaling ---
        private float scaleX = 1f;
        private float scaleY = 1f;
        private float scale = 1f;
        private const int baseWidth = 1920;
        private const int baseHeight = 1080;
        // --- Main Menu ---
        private bool onMainMenu = true;
        private Button menuPlayBtn = null!;
        private Button menuQuitBtn = null!;

        private float menuPlayerX = 0f;
        private float menuPlayerY = 0f;
        private float menuEnemyX = 0f;
        private float menuEnemyY = 0f;
        private float menuBulletX = 0f;
        private float menuBulletY = 0f;
        private float menuBulletVelX = 0f;
        private float menuBulletVelY = 0f;
        private bool menuBulletActive = false;
        private float menuShootTimer = 0f;
        private const float menuShootRate = 2f;
        private float menuEnemyHitTimer = 0f;
        private float menuEnemyHealth = 2f;
        private float menuEnemyMaxHealth = 2f;
        private int menuEnemyType = 0; // 0 = normal, 1 = runner, 2 = tank
        private bool menuEnemyDead = false;
        private float menuEnemyDeadTimer = 0f;

        private bool sandboxMode = false;

        private bool onPreferences = false;
        private bool showDimOverlay = false;
        private Panel? activeUpgradePanel = null;
        private Button? menuPrefsBtn = null;
        private Button? menuPrefsBackBtn = null;
        // --- Inspecting ---
        private string enemyInspectText = "";
        private float enemyInspectTimer = 0f;
        private const float enemyInspectDuration = 4f;
        private int inspectedEnemyIndex = -1;
        // --- Parasite ---
        private List<bool> enemyIsParasitic = new List<bool>();
        private float parasiticEnemyChance = 0.01f;
        private List<(float x, float y, float velX, float velY, float timer, float spawnDelay, float hitCooldown)> parasites = new();
        private const float parasiteDuration = 5f;
        private const float parasiteSpeed = 1000f;
        private const float parasiteSize = 12f;
        private const float parasiticDecayRate = 0.3f;
        private bool parasiteDecayKill = false;
        // --- Other status effects ---
        private List<bool> enemyIsFrenzied = new List<bool>();
        private List<bool> enemyIsPhasing = new List<bool>();
        private List<bool> enemyIsZigzag = new List<bool>();
        private List<bool> enemyIsCharging = new List<bool>();
        private List<bool> enemyIsArmored = new List<bool>();
        private List<bool> enemyIsRegenerating = new List<bool>();
        private List<bool> enemyIsReflective = new List<bool>();
        private List<bool> enemyIsBerserker = new List<bool>();
        private List<bool> enemyIsCorrupted = new List<bool>();

        private List<bool> enemyArmorBroken = new List<bool>();
        private List<float> enemyChargeCooldown = new List<float>();
        private List<float> enemyChargeTimer = new List<float>();
        private List<bool> enemyIsCharging_Active = new List<bool>();
        private List<float> enemyChargeVelX = new List<float>();
        private List<float> enemyChargeVelY = new List<float>();
        private List<float> enemyFrenziedAngle = new List<float>();
        private List<float> enemyZigzagTimer = new List<float>();
        private List<float> enemyZigzagDirection = new List<float>();
        private List<float> enemyPhasingTimer = new List<float>();
        private List<bool> enemyIsVisible = new List<bool>();
        private List<(float x, float y, float timer)> corruptedTrails = new List<(float x, float y, float timer)>();

        // --- Difficulty ---
        // 0=Easy,1=Beginner,2=Normal,3=Moderate,4=Challenging,5=Hard,6=Expert,7=Extreme,8=Nightmare
        private int difficulty = 0;
        private int highestUnlockedDifficulty = 0;
        private static readonly string[] DifficultyNames = { "Easy", "Beginner", "Normal", "Moderate", "Challenging", "Hard", "Expert", "Extreme", "Nightmare" };
        private static readonly string[] DifficultyStarNames = { "⭐ Easy", "⭐ Beginner", "⭐⭐ Normal", "⭐⭐ Moderate", "⭐⭐ Challenging", "⭐⭐⭐ Hard", "⭐⭐⭐ Expert", "💀 Extreme", "💀 Nightmare" };
        private static readonly Color[] DifficultyColors = {
            Color.LimeGreen, Color.MediumSeaGreen, Color.DodgerBlue, Color.CornflowerBlue,
            Color.Goldenrod, Color.Orange, Color.OrangeRed, Color.Crimson, Color.Red
        };
        private static readonly Color[] DifficultyBgColors = {
            Color.FromArgb(40, 120, 40), Color.FromArgb(30, 100, 60), Color.FromArgb(40, 80, 140),
            Color.FromArgb(50, 70, 120), Color.FromArgb(120, 100, 20), Color.FromArgb(140, 80, 40),
            Color.FromArgb(140, 50, 30), Color.FromArgb(120, 20, 40), Color.FromArgb(120, 20, 20)
        };
        private float bossSpawnInterval_Current = 120f;
        private float scoreMultiplier = 1f;

        private bool showingUnlockAnimation = false;
        private float unlockAnimTimer = 0f;
        private const float unlockAnimDuration = 15f;
        private int unlockedDifficultyIndex = -1;
        private float unlockParticleTimer = 0f;
        private List<(float x, float y, float velX, float velY, float timer, Color color)> unlockParticles = new();

        private int pendingUnlockAnimation = -1;
        // --- Run History ---
        private List<(float score, int kills, float time, int difficulty, bool sandbox, bool multiplayer)> runHistory =
            new List<(float score, int kills, float time, int difficulty, bool sandbox, bool multiplayer)>();
        private const int maxRunHistory = 5;

        // --- Bestiary ---
        private Dictionary<string, int> beastiaryKills = new Dictionary<string, int>()
{
    { "Normal", 0 }, { "Gunner", 0 }, { "Tank", 0 }, { "Runner", 0 },
    { "Parasitic", 0 }, { "Frenzied", 0 }, { "Zigzag", 0 }, { "Charging", 0 },
    { "Armored", 0 }, { "Regenerating", 0 }, { "Reflective", 0 },
    { "Berserker", 0 }, { "Phasing", 0 }, { "Corrupted", 0 }
};
        private bool beastiaryUnlocked = false;

        private Button? menuHistoryBtn = null;
        private Button? menuBestiaryBtn = null;
        private Button? menuAchievementsBtn = null;
        private Button? menuMultiplayerBtn = null;

        // --- Multiplayer ---
        private NetworkManager? netManager = null;
        private EmbeddedRelay? embeddedRelay = null;
        private bool isMultiplayer = false;
        private bool isNetHost = false;

        // Player 2 state (rendered on both host and client)
        private float p2X = 0f, p2Y = 0f;
        private float p2Health = 50f, p2MaxHealth = 50f;
        private bool p2Dashing = false;
        private float p2DashTimer = 0f;
        private float p2DashVelX = 0f;
        private float p2DashVelY = 0f;
        private float p2DashCooldown = 0f;
        private string p2Name = "Player 2";
        private bool p2Dead = false;
        private float p2ShootCooldown = 0f;
        private int p2DoubleTapCounter = 0;
        private float p2HitCooldown = 0f;
        private float p2BlinkCooldown = 0f;
        private float p2TurretCooldown = 0f;
        private int p2Ammo = 60;
        private int p2MaxAmmo = 60;
        private bool p2Reloading = false;
        private float p2ReloadTimer = 0f;
        private Color p2Color_synced = Color.FromArgb(255, 80, 140, 255); // default blue for P2
        private bool hostDead = false;
        private int p2PendingUpgrade = -1; // client sends upgrade purchase to host
        private bool p2PendingSuper = false; // client wants to activate super
        private bool p2PendingWall = false; // client wants to activate wall
        private bool p2PendingDash = false; // client wants to dash (one-shot trigger)
        private bool p2PendingBlink = false; // client wants to blink
        private bool p2PendingTurret = false; // client wants to place turret
        private bool p2PendingDecoy = false; // client wants to place decoy
        private bool p2PendingSpeedTrap = false; // client wants to place speed trap

        // Client-side: latest game state from host
        private GameStatePacket? latestGameState = null;

        // Host-side: latest input from client
        private PlayerInputPacket latestP2Input = new();

        // --- Achievements & Red Coin Shop ---
        private int redCoins = 0;
        private int permSpeedLevel = 0;
        private int permDamageLevel = 0;
        private int permBulletSpeedLevel = 0;
        private Button? menuShopBtn = null;
        private HashSet<string> unlockedAchievements = new HashSet<string>();
        private string achievementToastText = "";
        private string achievementToastIcon = "";
        private float achievementToastTimer = 0f;
        private const float achievementToastDuration = 4f;
        private int totalUpgradesPurchased = 0;
        private int totalCoinsCollected = 0;
        private float totalDashDistance = 0f;
        private int totalBulletsShot = 0;

        private static readonly (string id, string icon, string name, string description, string category, Func<gameForm, bool> condition)[] achievements =
        {
            // Kill achievements
            ("first_blood",    "🗡",  "First Blood",       "Kill your first enemy",              "Kills",    g => g.totalKills >= 1),
            ("serial_killer",  "💀",  "Serial Killer",     "Kill 50 enemies in one run",         "Kills",    g => g.totalKills >= 50),
            ("mass_murderer",  "☠",   "Mass Murderer",     "Kill 200 enemies in one run",        "Kills",    g => g.totalKills >= 200),
            ("genocide",       "🔥",  "Genocide",          "Kill 500 enemies in one run",        "Kills",    g => g.totalKills >= 500),
            ("exterminator",   "⚡",  "Exterminator",      "Kill 1000 enemies in one run",       "Kills",    g => g.totalKills >= 1000),

            // Survival achievements
            ("survivor",       "⏱",   "Survivor",          "Survive for 1 minute",               "Survival", g => g.timeAlive >= 60f),
            ("endurance",      "⏱",   "Endurance",         "Survive for 3 minutes",              "Survival", g => g.timeAlive >= 180f),
            ("marathon",       "⏱",   "Marathon Runner",   "Survive for 5 minutes",              "Survival", g => g.timeAlive >= 300f),
            ("immortal",       "👑",  "Immortal",          "Survive for 10 minutes",             "Survival", g => g.timeAlive >= 600f),
            ("eternal",        "🌟",  "Eternal",           "Survive for 20 minutes",             "Survival", g => g.timeAlive >= 1200f),

            // Boss achievements
            ("boss_slayer",    "👹",  "Boss Slayer",       "Defeat your first boss",             "Boss",     g => g.bossesDefeated >= 1),
            ("boss_hunter",    "👹",  "Boss Hunter",       "Defeat 3 bosses in one run",         "Boss",     g => g.bossesDefeated >= 3),
            ("boss_master",    "👹",  "Boss Master",       "Defeat 5 bosses in one run",         "Boss",     g => g.bossesDefeated >= 5),
            ("boss_legend",    "🏆",  "Boss Legend",       "Defeat 10 bosses in one run",        "Boss",     g => g.bossesDefeated >= 10),

            // Score achievements
            ("pocket_change",  "💲",  "Pocket Change",     "Earn $500 in one run",               "Score",    g => g.totalScore >= 500),
            ("wealthy",        "💲",  "Wealthy",           "Earn $2,000 in one run",             "Score",    g => g.totalScore >= 2000),
            ("rich",           "💰",  "Rich",              "Earn $5,000 in one run",             "Score",    g => g.totalScore >= 5000),
            ("millionaire",    "💎",  "Millionaire",       "Earn $10,000 in one run",            "Score",    g => g.totalScore >= 10000),
            ("bezos",          "🤑",  "Bezos Mode",        "Earn $50,000 in one run",            "Score",    g => g.totalScore >= 50000),

            // Upgrade achievements
            ("first_upgrade",  "⬆",   "First Upgrade",     "Buy your first upgrade",             "Upgrades", g => g.totalUpgradesPurchased >= 1),
            ("shopaholic",     "🛒",  "Shopaholic",        "Buy 10 upgrades in one run",         "Upgrades", g => g.totalUpgradesPurchased >= 10),
            ("maxed_out",      "🛒",  "Maxed Out",         "Buy 20 upgrades in one run",         "Upgrades", g => g.totalUpgradesPurchased >= 20),

            // Ability achievements
            ("orbit_unlocked", "🌑",  "Orbital",           "Unlock orbit bullets",               "Abilities",g => g.orbitCount >= 1),
            ("orbit_master",   "🌑",  "Orbit Master",      "Have 5 orbit bullets",               "Abilities",g => g.orbitCount >= 5),
            ("blink_user",     "🌀",  "Blink User",        "Unlock blink",                       "Abilities",g => g.blink),
            ("ghost",          "👻",  "Ghost",             "Unlock ghost dash",                  "Abilities",g => g.ghostDash),
            ("turret_placer",  "🗼",  "Engineer",          "Place a turret",                     "Abilities",g => g.turrets.Count >= 1),
            ("piercing_user",  "🏹",  "Piercing Shot",     "Unlock piercing bullets",            "Abilities",g => g.piercingBullets),
            ("homing_user",    "🎯",  "Lock On",           "Unlock homing bullets",              "Abilities",g => g.homing),

            // Difficulty achievements
            ("beginner_unlock",  "⭐",  "Baby Steps",        "Unlock Beginner difficulty",          "Difficulty",g => g.highestUnlockedDifficulty >= 1),
            ("normal_unlock",    "⭐",  "Stepping Up",       "Unlock Normal difficulty",            "Difficulty",g => g.highestUnlockedDifficulty >= 2),
            ("moderate_unlock",  "⭐",  "Warming Up",        "Unlock Moderate difficulty",          "Difficulty",g => g.highestUnlockedDifficulty >= 3),
            ("challenging_unlock","⭐⭐","Rising Threat",    "Unlock Challenging difficulty",       "Difficulty",g => g.highestUnlockedDifficulty >= 4),
            ("hard_unlock",      "⭐⭐","Getting Serious",  "Unlock Hard difficulty",              "Difficulty",g => g.highestUnlockedDifficulty >= 5),
            ("expert_unlock",    "⭐⭐⭐","Proven Warrior",  "Unlock Expert difficulty",            "Difficulty",g => g.highestUnlockedDifficulty >= 6),
            ("extreme_unlock",   "💀",  "No Turning Back",   "Unlock Extreme difficulty",           "Difficulty",g => g.highestUnlockedDifficulty >= 7),
            ("nightmare_unlock", "💀",  "Nightmare Fuel",    "Unlock Nightmare difficulty",         "Difficulty",g => g.highestUnlockedDifficulty >= 8),
            ("nightmare_boss",   "🔥",  "True Champion",     "Defeat a boss on Nightmare",          "Difficulty",g => g.difficulty == 8 && g.bossesDefeatedOnDifficulty >= 1),

            // Misc achievements
            ("full_health",    "💚",  "Full Health",       "Reach max HP above 100",             "Misc",     g => g.maxHealth >= 100 && g.health >= g.maxHealth),
            ("close_call",     "😰",  "Close Call",        "Survive with less than 1 HP",        "Misc",     g => g.health > 0 && g.health < 1f && g.timeAlive > 10f),
            ("bullet_hell",    "🔫",  "Bullet Hell",       "Have 50+ bullets on screen",         "Misc",     g => g.bullets.Count >= 50),
            ("coin_collector", "🪙",  "Coin Collector",    "Collect 100 coins in one run",       "Misc",     g => g.totalCoinsCollected >= 100),
            ("coin_hoarder",   "🪙",  "Coin Hoarder",     "Collect 500 coins in one run",       "Misc",     g => g.totalCoinsCollected >= 500),
            ("parasite_immune","🧬",  "Immune System",     "Unlock parasite immunity",           "Misc",     g => g.parasiteImmune),
            ("super_active",   "⚡",  "Super Saiyan",      "Activate Super mode",                "Misc",     g => g.superActive),
        };

        private static int GetAchievementRedCoins(string id)
        {
            return id switch
            {
                // Kill achievements: 1, 2, 5, 10, 20
                "first_blood" => 1, "serial_killer" => 2, "mass_murderer" => 5, "genocide" => 10, "exterminator" => 20,
                // Survival: 1, 3, 5, 10, 20
                "survivor" => 1, "endurance" => 3, "marathon" => 5, "immortal" => 10, "eternal" => 20,
                // Boss: 2, 5, 10, 20
                "boss_slayer" => 2, "boss_hunter" => 5, "boss_master" => 10, "boss_legend" => 20,
                // Score: 1, 2, 5, 10, 25
                "pocket_change" => 1, "wealthy" => 2, "rich" => 5, "millionaire" => 10, "bezos" => 25,
                // Upgrades: 1, 3, 5
                "first_upgrade" => 1, "shopaholic" => 3, "maxed_out" => 5,
                // Abilities: 2 each, turret/piercing/homing 3
                "orbit_unlocked" => 2, "orbit_master" => 5, "blink_user" => 2, "ghost" => 3,
                "turret_placer" => 3, "piercing_user" => 3, "homing_user" => 3,
                // Difficulty: escalating 1-25
                "beginner_unlock" => 1, "normal_unlock" => 2, "moderate_unlock" => 3,
                "challenging_unlock" => 5, "hard_unlock" => 8, "expert_unlock" => 12,
                "extreme_unlock" => 18, "nightmare_unlock" => 25, "nightmare_boss" => 50,
                // Misc: 2-5
                "full_health" => 2, "close_call" => 5, "bullet_hell" => 3,
                "coin_collector" => 2, "coin_hoarder" => 5, "parasite_immune" => 3, "super_active" => 2,
                _ => 1
            };
        }

        private void UnlockAchievement(string id)
        {
            if (sandboxMode) return; // Don't grant achievements in sandbox
            if (difficulty < 1) return; // Only unlock on Normal (1) or higher
            if (unlockedAchievements.Contains(id)) return;
            unlockedAchievements.Add(id);
            int reward = GetAchievementRedCoins(id);
            redCoins += reward;
            SaveAchievements();
            SaveRedCoins();
            var ach = achievements.FirstOrDefault(a => a.id == id);
            if (ach.id != null)
            {
                achievementToastText = $"{ach.name} (+{reward} 🔴)";
                achievementToastIcon = ach.icon;
                achievementToastTimer = achievementToastDuration;
            }
        }

        private void CheckAchievements()
        {
            foreach (var ach in achievements)
            {
                if (!unlockedAchievements.Contains(ach.id) && ach.condition(this))
                    UnlockAchievement(ach.id);
            }
        }

        public gameForm()
        {
            InitializeComponent();
            LoadPlayerSprite();
            LoadRedEnemySprite();
            LoadParasiticEnemySprite();
            LoadGunnerSprite();
            LoadGunnerParasiticSprite();
            LoadTankSprite();
            LoadTankParasiticSprite();
            LoadRunnerSprite();
            LoadRunnerParasiticSprite();
            LoadBossSprite();
            LoadCardSprite();
            this.Text = "Red Guy Takeover ALPHA RELEASE";
            this.ClientSize = new Size(1900, 1080);
            this.WindowState = FormWindowState.Maximized;
            this.Deactivate += (s, e) => { if (!onMainMenu) isPaused = true; };
            this.FormClosing += (s, e) => { if (systemCursorHidden) { Cursor.Show(); systemCursorHidden = false; } };
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.Shown += (s, e) =>
            {
                AutoUpdater.RunUpdateAsAdmin = false;
                AutoUpdater.ReportErrors = true;
                AutoUpdater.ApplicationExitEvent += () =>
                {
                    Application.Exit();
                };
                AutoUpdater.Start("https://raw.githubusercontent.com/kacper-chr/RedGuyTakeover/main/update.xml");
                scaleX = (float)ClientSize.Width / baseWidth;
                scaleY = (float)ClientSize.Height / baseHeight;
                scale = Math.Min(scaleX, scaleY);
                darkMode = true;
                LoadDifficultyUnlocks();
                ApplyDifficulty();
                ResetEnemies();
                ResetGame();
                ShowMainMenu();
                LoadRunHistory();
                LoadBeastiary();
                LoadAchievements();
                LoadRedCoins();
                LoadPlayerName();
            };
            enemyRespawnTimers = new List<float>(new float[enemies.Count]);
            enemyAlive = Enumerable.Repeat(true, enemies.Count).ToList();
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
            this.Paint += Form1_Paint;
            Application.Idle += GameTimer_Tick;
            this.MouseDown += Form1_MouseDown;
            this.MouseUp += Form1_MouseUp;
            this.MouseMove += Form1_MouseMove;
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W: velocityY = -speed; break;
                case Keys.S: velocityY = speed; break;
                case Keys.A: velocityX = -speed; break;
                case Keys.D: velocityX = speed; break;
                case Keys.Space:
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    if (onMainMenu || isPaused) break;
                    if (isMultiplayer && !isNetHost)
                    {
                        // Client: send dash trigger to host; the host runs the dash
                        // simulation and authoritatively syncs isDashing/dashCooldown back.
                        if (dashCooldown <= 0) p2PendingDash = true;
                    }
                    else if (!isDashing && dashCooldown <= 0)
                    {
                        isDashing = true;
                        dashTimer = dashDuration;
                        dashCooldown = dashCooldownTime;
                        float dashLen = (float)Math.Sqrt(velocityX * velocityX + velocityY * velocityY);
                        if (dashLen > 0)
                        {
                            dashVelX = (velocityX / dashLen) * dashPower;
                            dashVelY = (velocityY / dashLen) * dashPower;
                        }
                    }
                    break;
                case Keys.Escape:
                    isPaused = !isPaused;
                    if (isPaused)
                        ShowPauseButtons();
                    else
                        HidePauseButtons();
                    break;
                case Keys.Q:
                    if (isMultiplayer && !isNetHost)
                    {
                        p2PendingSuper = true; // send to host
                    }
                    else if (!superActive && superCooldown <= 0)
                    {
                        superActive = true;
                        superTimer = superDuration;
                        reloading = false;
                        reloadTimer = 0f;
                    }
                    break;
                case Keys.E:
                    if (isMultiplayer && !isNetHost)
                    {
                        p2PendingWall = true; // send to host
                    }
                    else if (!wallActive && wallCooldown <= 0)
                    {
                        wallActive = true;
                        wallTimer = wallDuration;
                        wallCooldown = wallCooldownTime;
                        if (boxWall)
                        {
                            float cx = posX + playerSize / 2;
                            float cy = posY + playerSize / 2;
                            float offset = wallLength / 2;
                            float wLen = wallLength;
                            float wThick = boxSize;
                            boxWalls = new List<(float x, float y, float width, float height, float angle)>
                            {
                                (cx, cy - offset, wLen, wThick, 0f),
                                (cx, cy + offset, wLen, wThick, 0f),
                                (cx - offset, cy, wLen, wThick, (float)(Math.PI / 2)),
                                (cx + offset, cy, wLen, wThick, (float)(Math.PI / 2)),
                            };
                        }
                        else
                        {
                            boxWalls.Clear();
                            float dirX = mousePos.X - (posX + boxSize / 2);
                            float dirY = mousePos.Y - (posY + boxSize / 2);
                            float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
                            float angle = (float)Math.Atan2(dirY, dirX) + (float)(Math.PI / 2);
                            float spawnX = posX + boxSize / 2;
                            float spawnY = posY + boxSize / 2;
                            if (dist > 0)
                            {
                                spawnX = posX + (dirX / dist) * (boxSize * 2);
                                spawnY = posY + (dirY / dist) * (boxSize * 2);
                            }
                            tempWall = (spawnX, spawnY, wallLength, boxSize, angle);
                        }
                    }
                    break;
                case Keys.Tab:
                    e.SuppressKeyPress = true;
                    isPaused = true;
                    velocityX = 0f;
                    velocityY = 0f;
                    ShowUpgradeMenu();
                    // isPaused is cleared in the upgrade menu's close handler
                    break;
                case Keys.F:
                    if (isMultiplayer && !isNetHost)
                    {
                        p2PendingDecoy = true;
                    }
                    else if (decoy && !decoyActive && decoyCooldown <= 0)
                    {
                        decoyActive = true;
                        decoyTimer = decoyDuration;
                        decoyCooldown = decoyCooldownTime;
                        decoyX = mousePos.X;
                        decoyY = mousePos.Y;
                    }
                    break;
                case Keys.G:
                    if (isMultiplayer && !isNetHost)
                    {
                        p2PendingSpeedTrap = true;
                    }
                    else if (speedTrap && !speedTrapActive && speedTrapCooldown <= 0)
                    {
                        speedTrapActive = true;
                        speedTrapTimer = speedTrapDuration;
                        speedTrapX = mousePos.X;
                        speedTrapY = mousePos.Y;
                    }
                    break;
                case Keys.H:
                    if (isMultiplayer && !isNetHost)
                    {
                        p2PendingTurret = true;
                    }
                    else if (turret && turretCooldown <= 0)
                    {
                        turrets.Add((mousePos.X, mousePos.Y));
                        turretShootTimers.Add(0f);
                        turretCooldown = turretCooldownTime;
                    }
                    break;
            }
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            if (isExiting) return;
            DateTime now = DateTime.Now;
            deltaTime = (float)(now - lastTick).TotalSeconds;
            lastTick = now;
            fpsFrames++;
            fpsTimer += deltaTime;
            if (fpsTimer >= 1f)
            {
                currentFPS = fpsFrames / fpsTimer;
                fpsFrames = 0;
                fpsTimer = 0f;
            }
            gameStartTimer += deltaTime;
            if (isPaused)
            {
                if (onMainMenu)
                {
                    if (menuEnemyDead)
                    {
                        menuEnemyDeadTimer -= deltaTime;
                        if (menuEnemyDeadTimer <= 0)
                        {
                            menuEnemyDead = false;
                            int roll = rng.Next(10);
                            menuEnemyType = roll < 2 ? 2 : roll < 4 ? 1 : 0;
                            menuEnemyMaxHealth = menuEnemyType == 2 ? 8f : menuEnemyType == 1 ? 1f : 2f;
                            menuEnemyHealth = menuEnemyMaxHealth;
                            menuEnemyX = ClientSize.Width + 100;
                            menuEnemyY = menuPlayerY;
                        }
                    }
                    else
                    {
                        float enemySpeed = menuEnemyType == 1 ? 180f : menuEnemyType == 2 ? 60f : 100f;
                        float dirX = menuPlayerX - menuEnemyX;
                        float dist = Math.Abs(dirX);
                        if (dist > 5f)
                            menuEnemyX += (dirX / dist) * enemySpeed * deltaTime;

                        menuShootTimer += deltaTime;
                        if (menuShootTimer >= menuShootRate && !menuEnemyDead)
                        {
                            menuShootTimer = 0f;
                            menuBulletX = menuPlayerX + 20;
                            menuBulletY = menuPlayerY;
                            float bDirX = menuEnemyX - menuBulletX;
                            float bDirY = menuEnemyY - menuBulletY;
                            float bDist = (float)Math.Sqrt(bDirX * bDirX + bDirY * bDirY);
                            menuBulletVelX = (bDirX / bDist) * 500f;
                            menuBulletVelY = (bDirY / bDist) * 500f;
                            menuBulletActive = true;
                        }

                        if (menuBulletActive)
                        {
                            menuBulletX += menuBulletVelX * deltaTime;
                            menuBulletY += menuBulletVelY * deltaTime;

                            int eSize = menuEnemyType == 2 ? 50 : menuEnemyType == 1 ? 22 : 30;
                            int bSize = 8;

                            if (menuBulletX + bSize > menuEnemyX - eSize / 2 &&
                                menuBulletX < menuEnemyX + eSize / 2 &&
                                menuBulletY + bSize > menuEnemyY - eSize / 2 &&
                                menuBulletY < menuEnemyY + eSize / 2)
                            {
                                menuBulletActive = false;
                                menuEnemyHitTimer = 0.1f;
                                menuEnemyHealth -= 1f;
                                if (menuEnemyHealth <= 0)
                                {
                                    menuEnemyDead = true;
                                    menuEnemyDeadTimer = 1.5f;
                                }
                            }

                            if (menuBulletX < -100 || menuBulletX > ClientSize.Width + 100 ||
                                menuBulletY < -100 || menuBulletY > ClientSize.Height + 100)
                                menuBulletActive = false;
                        }

                        if (menuEnemyHitTimer > 0)
                            menuEnemyHitTimer -= deltaTime;
                    }

                    if (showingUnlockAnimation)
                    {
                        unlockAnimTimer -= deltaTime;
                        unlockParticleTimer += deltaTime;

                        var newParticles = new List<(float x, float y, float velX, float velY, float timer, Color color)>();
                        foreach (var p in unlockParticles)
                        {
                            float newTimer = p.timer - deltaTime;
                            if (newTimer > 0)
                            {
                                newParticles.Add((
                                    p.x + p.velX * deltaTime,
                                    p.y + p.velY * deltaTime,
                                    p.velX * 0.95f,
                                    p.velY * 0.95f + 50f * deltaTime,
                                    newTimer,
                                    p.color
                                ));
                            }
                        }
                        unlockParticles = newParticles;

                        if (unlockAnimTimer <= 0)
                        {
                            showingUnlockAnimation = false;
                            unlockParticles.Clear();
                        }
                    }
                }
                this.Invalidate();
                // In multiplayer, don't freeze the game — just skip local input
                if (!isMultiplayer)
                    return;
            }
            if (dashCooldown > 0)
                dashCooldown -= deltaTime;
            if (gameStartTimer > gameStartDelay)
                timeAlive += deltaTime;
            if (isDashing)
            {
                dashTimer -= deltaTime;
                float dashProgress = dashTimer / dashDuration;
                posX += dashVelX * dashProgress * deltaTime * 60;
                posY += dashVelY * dashProgress * deltaTime * 60;
                if (ghostDash)
                    dashTrail.Add((posX, posY, dashTrailDuration));
                if (dashTimer <= 0)
                {
                    isDashing = false;
                    dashVelX = 0f;
                    dashVelY = 0f;
                    if (afterburn)
                    {
                        isAfterburn = true;
                        afterburnTimer = afterburnDuration;
                    }
                    if (blowback > 0)
                    {
                        for (int i = 0; i < enemies.Count; i++)
                        {
                            if (!enemyAlive[i]) continue;
                            float dx = enemies[i].x - posX;
                            float dy = enemies[i].y - posY;
                            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (dist < blowbackRadius * scale && dist > 0)
                            {
                                float force = blowbackForce * blowback * scale * (1f - dist / (blowbackRadius * scale));
                                enemies[i] = (
                                    enemies[i].x + (dx / dist) * force,
                                    enemies[i].y + (dy / dist) * force
                                );
                            }
                        }
                    }
                }
            }
            // Skip movement/shooting if dead or paused in multiplayer
            if (hostDead || (isMultiplayer && isPaused)) { velocityX = 0; velocityY = 0; }
            float toughLoveBonus = toughLove ? 1f + (1f - (health / maxHealth)) * 1.5f : 1f;
            float currentSpeed = isAfterburn ? afterburnSpeed : 1f;
            posX += velocityX * deltaTime * 60 * currentSpeed * toughLoveBonus;
            posY += velocityY * deltaTime * 60 * currentSpeed * toughLoveBonus;

            var newTrail = new List<(float x, float y, float timer)>();
            foreach (var t in dashTrail)
            {
                float newTimer = t.timer - deltaTime;
                if (newTimer > 0)
                {
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if (!enemyAlive[i]) continue;
                        if (t.x < enemies[i].x + playerSize / 2 && t.x + playerSize / 2 > enemies[i].x &&
                            t.y < enemies[i].y + playerSize / 2 && t.y + playerSize / 2 > enemies[i].y)
                        {
                            if (hitCooldown <= 0)
                            {
                                float coinX = enemies[i].x + boxSize / 2;
                                float coinY = enemies[i].y + boxSize / 2;
                                coins.Add((coinX, coinY, 0f, 0f));
                                DamageEnemy(i, 2f);
                                enemyRespawnTimers[i] = enemyRespawnTime;
                                totalKills++;
                                health = Math.Min(health + lifeSteal, maxHealth);
                                if (isMultiplayer && !p2Dead) p2Health = Math.Min(p2Health + lifeSteal, p2MaxHealth);
                            }
                        }
                    }
                    if (BossOverlaps(t.x, t.y, playerSize / 2, playerSize / 2) && bossOrbitHitCooldown <= 0)
                    {
                        DamageBoss(1f);
                        bossOrbitHitCooldown = bossOrbitHitCooldownTime;
                    }
                    newTrail.Add((t.x, t.y, newTimer));
                }
            }
            if (isAfterburn)
            {
                afterburnTimer -= deltaTime;
                if (afterburnTimer <= 0)
                    isAfterburn = false;
            }
            if (blinkCooldown > 0)
                blinkCooldown -= deltaTime;
            dashTrail = newTrail;
            if (decoyActive)
            {
                decoyTimer -= deltaTime;
                if (decoyTimer <= 0)
                {
                    decoyActive = false;
                    decoyCooldown = decoyCooldownTime;
                }
            }
            if (decoyCooldown > 0)
                decoyCooldown -= deltaTime;
            if (gameStartTimer > gameStartDelay)
            {
                scoreTimer += deltaTime;
                if (scoreTimer >= scoreTimerMax)
                {
                    score += (int)(scorePerSecond * scoreMultiplier);
                    totalScore += (int)(scorePerSecond * scoreMultiplier);
                    scoreTimer = 0f;
                }
                if (sandboxMode)
                    score = Math.Max(score, 9999999);
            }
            if (gameStartTimer > gameStartDelay)
            {
                enemyBuffTimer += deltaTime;
                if (enemyBuffTimer >= enemyBuffInterval)
                {
                    enemyBuffTimer = 0f;
                    ApplyEnemyBuff();
                }
            }
            if (buffMessageTimer > 0)
                buffMessageTimer -= deltaTime;

            posX = Math.Max(0, Math.Min(posX, ClientSize.Width - boxSize));
            posY = Math.Max(0, Math.Min(posY, ClientSize.Height - boxSize));
            if (CollidesWithWall(posX, posY, boxSize))
            {
                posX -= velocityX * deltaTime * 60;
                posY -= velocityY * deltaTime * 60;
            }
            if (!isDashing)
                (posX, posY) = PushOutOfWalls(posX, posY, boxSize);
            if (mouseHeld && gameStartTimer > gameStartDelay && !reloading && !hostDead && !(isMultiplayer && isPaused))
            {
                if (shootCooldown <= 0 && (ammo > 0 || superActive))
                {
                    if (explosiveFinish && !superActive && ammo == 1)
                        nextBulletIsLast = true;
                    else
                        nextBulletIsLast = false;
                    Shoot();
                    if (!superActive)
                    {
                        ammo--;
                        if (ammo <= 0) reloading = true;
                    }
                    shootCooldown = superActive ? 0f : Math.Max(0.05f, shootRate - fireRateBonus);
                }
                else
                {
                    shootCooldown -= deltaTime;
                }
            }
            if (reloading)
            {
                reloadTimer += deltaTime;
                if (reloadTimer >= reloadTime)
                {
                    ammo = maxAmmo;
                    reloading = false;
                    reloadTimer = 0f;
                }
            }
            if (p2Reloading)
            {
                p2ReloadTimer += deltaTime;
                if (p2ReloadTimer >= reloadTime)
                {
                    p2Ammo = p2MaxAmmo;
                    p2Reloading = false;
                    p2ReloadTimer = 0f;
                }
            }
            if (speedTrapActive)
            {
                speedTrapTimer -= deltaTime;
                if (speedTrapTimer <= 0)
                {
                    speedTrapActive = false;
                    speedTrapCooldown = speedTrapCooldownTime;
                }
            }
            if (speedTrapCooldown > 0)
                speedTrapCooldown -= deltaTime;

            // Boss spawn timer
            if (gameStartTimer > gameStartDelay && !bossAlive)
            {
                bossSpawnTimer += deltaTime;
                if (bossSpawnTimer >= bossSpawnInterval_Current)
                {
                    bossSpawnTimer = 0f;
                    bossAlive = true;
                    bossHealth = currentBossMaxHealth;
                    int side = rng.Next(4);
                    switch (side)
                    {
                        case 0: bossX = 0; bossY = rng.Next(0, ClientSize.Height); break;
                        case 1: bossX = ClientSize.Width; bossY = rng.Next(0, ClientSize.Height); break;
                        case 2: bossX = rng.Next(0, ClientSize.Width); bossY = 0; break;
                        default: bossX = rng.Next(0, ClientSize.Width); bossY = ClientSize.Height; break;
                    }
                    buffMessage = "⚠ BOSS INCOMING!";
                    buffMessageTimer = 3f;
                }
            }

            // Boss movement and logic
            if (bossAlive)
            {
                // Boss targets nearest player
                float bossTgtX = posX, bossTgtY = posY;
                if (isMultiplayer && !p2Dead)
                {
                    float bd1 = hostDead ? float.MaxValue : (posX - bossX) * (posX - bossX) + (posY - bossY) * (posY - bossY);
                    float bd2 = (p2X - bossX) * (p2X - bossX) + (p2Y - bossY) * (p2Y - bossY);
                    if (bd2 < bd1) { bossTgtX = p2X; bossTgtY = p2Y; }
                }
                float bDirX = bossTgtX - bossX;
                float bDirY = bossTgtY - bossY;
                float bDist = (float)Math.Sqrt(bDirX * bDirX + bDirY * bDirY);
                if (bDist > 0)
                {
                    float bossPrevX = bossX;
                    float bossPrevY = bossY;
                    bossX += (bDirX / bDist) * bossSpeed * scale * deltaTime * 60;
                    bossY += (bDirY / bDist) * bossSpeed * scale * deltaTime * 60;
                    // Block the boss from phasing through walls
                    if (CollidesWithWall(bossX, bossY, (int)(bossSize * scale)))
                    {
                        bossX = bossPrevX;
                        bossY = bossPrevY;
                    }
                }

                if (!isDashing &&
                    !hostDead &&
                    bossX < posX + playerSize && bossX + bossSize * scale > posX &&
                    bossY < posY + playerSize && bossY + bossSize * scale > posY)
                {
                    if (bossHitCooldown <= 0)
                    {
                        health -= bossDamage;
                        AddDamageNumber(posX + playerSize / 2, posY, bossDamage, Color.FromArgb(255, 255, 80, 80));
                        AddScreenShake(16f);
                        hurtVignette = Math.Min(1f, hurtVignette + 0.7f);
                        bossHitCooldown = 0.5f;
                        if (thorns) DamageBoss(1f);
                        if (health <= 0)
                            HandlePlayerDeath();
                    }
                }
                if (bossHitCooldown > 0)
                    bossHitCooldown -= deltaTime;

                // Smooth boss aim toward nearest living target — used for sprite rotation + bullet origin.
                {
                    float bcx = bossX + bossSize * scale / 2f;
                    float bcy = bossY + bossSize * scale / 2f;
                    float btx = posX + playerSize / 2f;
                    float bty = posY + playerSize / 2f;
                    if (decoyActive) { btx = decoyX; bty = decoyY; }
                    else if (isMultiplayer && !p2Dead)
                    {
                        float dh = hostDead ? float.MaxValue : (btx - bcx) * (btx - bcx) + (bty - bcy) * (bty - bcy);
                        float dp = (p2X + playerSize / 2f - bcx) * (p2X + playerSize / 2f - bcx) + (p2Y + playerSize / 2f - bcy) * (p2Y + playerSize / 2f - bcy);
                        if (dp < dh) { btx = p2X + playerSize / 2f; bty = p2Y + playerSize / 2f; }
                    }
                    float bossDesired = MathF.Atan2(bty - bcy, btx - bcx);
                    float bossDiff = MathF.IEEERemainder(bossDesired - bossAimAngle, MathF.Tau);
                    float bossMaxStep = 4f * deltaTime;
                    if (MathF.Abs(bossDiff) <= bossMaxStep) bossAimAngle = bossDesired;
                    else bossAimAngle += MathF.Sign(bossDiff) * bossMaxStep;
                }

                bossShootTimer += deltaTime;
                if (bossShootTimer >= currentBossShootRate)
                {
                    bossShootTimer = 0f;
                    float targetX, targetY;
                    if (decoyActive) { targetX = decoyX; targetY = decoyY; }
                    else
                    {
                        targetX = posX + playerSize / 2; targetY = posY + playerSize / 2;
                        if (isMultiplayer && !p2Dead)
                        {
                            float bsd1 = hostDead ? float.MaxValue : (targetX - bossX) * (targetX - bossX) + (targetY - bossY) * (targetY - bossY);
                            float bsd2 = (p2X + playerSize / 2 - bossX) * (p2X + playerSize / 2 - bossX) + (p2Y + playerSize / 2 - bossY) * (p2Y + playerSize / 2 - bossY);
                            if (bsd2 < bsd1) { targetX = p2X + playerSize / 2; targetY = p2Y + playerSize / 2; }
                        }
                    }
                    // Fire along the gun's actual facing direction (bossAimAngle), not the
                    // player's instantaneous position — that way bullets visibly leave the gun.
                    float facX = MathF.Cos(bossAimAngle);
                    float facY = MathF.Sin(bossAimAngle);
                    float bcx = bossX + bossSize * scale / 2f;
                    float bcy = bossY + bossSize * scale / 2f;
                    float originX, originY;
                    if (bossSpriteCropped != null)
                    {
                        // Mirror the math in DrawEnemySprite so the bullet emerges exactly from
                        // the sprite's gun pixel even when the gun is offset from the centerline.
                        float bossExtra = 1.05f / EnemySpriteDrawScale;
                        float drawH = bossSize * scale * EnemySpriteDrawScale * bossExtra;
                        float drawW = drawH * bossSpriteCropped.Width / (float)bossSpriteCropped.Height;
                        // Sprite-local offset from body-center to gun-tip (UV → pixels).
                        float dx = (BossGunTipFracX - EnemySpriteBodyCenterFracX) * drawW;
                        float dy = (BossGunTipFracY - EnemySpriteBodyCenterFracY) * drawH;
                        // Sprite is rotated by (aim - baseAngle). cos/sin of that rotation:
                        float rot = bossAimAngle - EnemySpriteBaseAngle;
                        float cR = MathF.Cos(rot), sR = MathF.Sin(rot);
                        originX = bcx + dx * cR - dy * sR;
                        originY = bcy + dx * sR + dy * cR;
                    }
                    else
                    {
                        float gunReach = bossSize * scale * 0.5f;
                        originX = bcx + facX * gunReach;
                        originY = bcy + facY * gunReach;
                    }
                    float[] angles = { -0.3f, -0.1f, 0.1f, 0.3f };
                    foreach (float offset in angles)
                    {
                        float cos = MathF.Cos(offset);
                        float sin = MathF.Sin(offset);
                        float rotX = facX * cos - facY * sin;
                        float rotY = facX * sin + facY * cos;
                        enemyBullets.Add((
                            originX,
                            originY,
                            rotX * bossBulletSpeed,
                            rotY * bossBulletSpeed
                        ));
                    }
                    _ = targetX; _ = targetY;
                }
            }

            // Enemies
            if (gameStartTimer > gameStartDelay)
            {
                // Only host spawns enemies in MP. Client receives the list via state packet.
                if (!bossAlive && (!isMultiplayer || isNetHost))
                {
                    enemySpawnTimer += deltaTime;
                    if (enemySpawnTimer >= enemySpawnRate)
                    {
                        if (isPaused && !isMultiplayer) return;
                        bool newCanShoot = rng.NextDouble() < shootingEnemyChance;
                        bool newIsTank = !newCanShoot && rng.NextDouble() < tankEnemyChance;
                        bool newIsRunner = !newCanShoot && !newIsTank && rng.NextDouble() < runnerEnemyChance;
                        bool newIsParasitic = rng.NextDouble() < parasiticEnemyChance;
                        enemies.Add((rng.Next(0, ClientSize.Width), 0f));
                        enemyAlive.Add(true);
                        enemyRespawnTimers.Add(0f);
                        enemyCanShoot.Add(newCanShoot);
                        enemyIsTank.Add(newIsTank);
                        enemyIsRunner.Add(newIsRunner);
                        enemyIsParasitic.Add(newIsParasitic);
                        enemyShootTimers.Add(0f);
                        enemyFlameTimers.Add(0f);
                        int newIdx = enemies.Count - 1;
                        InitEnemyEffects(newIdx);
                        enemyHealth.Add(newIsTank ? 8f : newCanShoot ? 4f : newIsRunner ? 1f : 2f);
                        enemySpawnTimer = 0f;
                    }
                }
                SyncEnemyLists();

                for (int i = 0; i < enemies.Count; i++)
                {
                    if (!enemyAlive[i]) continue;
                  
                    float dirX, dirY;
                    if (decoyActive)
                    {
                        dirX = decoyX - enemies[i].x;
                        dirY = decoyY - enemies[i].y;
                    }
                    else
                    {
                        // In multiplayer, target nearest living player
                        float tX = posX + playerSize / 2;
                        float tY = posY + playerSize / 2;
                        if (isMultiplayer && !p2Dead)
                        {
                            float d1 = hostDead ? float.MaxValue : (posX + playerSize / 2 - enemies[i].x) * (posX + playerSize / 2 - enemies[i].x) + (posY + playerSize / 2 - enemies[i].y) * (posY + playerSize / 2 - enemies[i].y);
                            float d2 = (p2X + playerSize / 2 - enemies[i].x) * (p2X + playerSize / 2 - enemies[i].x) + (p2Y + playerSize / 2 - enemies[i].y) * (p2Y + playerSize / 2 - enemies[i].y);
                            if (d2 < d1) { tX = p2X + playerSize / 2; tY = p2Y + playerSize / 2; }
                        }
                        dirX = tX - enemies[i].x;
                        dirY = tY - enemies[i].y;
                    }
                    // Override: move in the direction the enemy is currently facing,
                    // not straight toward the player. Rotation lags via EnemyMaxRotSpeed,
                    // so movement curves smoothly while turning. When close to the target,
                    // blend toward the direct vector so enemies stop circling at melee range.
                    if (!decoyActive && i < enemyAimAngle.Count)
                    {
                        float facX = MathF.Cos(enemyAimAngle[i]);
                        float facY = MathF.Sin(enemyAimAngle[i]);
                        float distToPlayer = MathF.Sqrt(dirX * dirX + dirY * dirY);
                        float directBlend = MathF.Max(0f, MathF.Min(1f, 1f - distToPlayer / (boxSize * 5f)));
                        if (distToPlayer > 0.01f)
                        {
                            float dnx = dirX / distToPlayer;
                            float dny = dirY / distToPlayer;
                            dirX = facX * (1f - directBlend) + dnx * directBlend;
                            dirY = facY * (1f - directBlend) + dny * directBlend;
                        }
                        else
                        {
                            dirX = facX;
                            dirY = facY;
                        }
                    }
                    float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
                    if (dist > 0)
                    {
                        bool isRunner = i < enemyIsRunner.Count && enemyIsRunner[i];
                        float enemyMoveSpeed = isRunner ? currentEnemySpeed * runnerSpeedMultiplier : currentEnemySpeed;
                        if (speedTrapActive)
                        {
                            float dx = enemies[i].x - speedTrapX;
                            float dy = enemies[i].y - speedTrapY;
                            float dist2 = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (dist2 < speedTrapRadius * scale)
                                enemyMoveSpeed *= speedTrapSlowMultiplier;
                        }
                        bool isSlowed = orbitalStrike && orbitalSlowedEnemies.Contains(i);
                        if (isSlowed) enemyMoveSpeed *= orbitalSlowMultiplier;
                        float newEX = enemies[i].x + (dirX / dist) * enemyMoveSpeed * deltaTime * 60;
                        float newEY = enemies[i].y + (dirY / dist) * enemyMoveSpeed * deltaTime * 60;
                        if (!CollidesWithWall(newEX, newEY, boxSize))
                            enemies[i] = (newEX, newEY);
                        else
                        {
                            float slideEX = enemies[i].x + (dirX / dist) * enemyMoveSpeed * deltaTime * 60;
                            if (!CollidesWithWall(slideEX, enemies[i].y, boxSize))
                                enemies[i] = (slideEX, enemies[i].y);
                            else
                            {
                                float slideEY = enemies[i].y + (dirY / dist) * enemyMoveSpeed * deltaTime * 60;
                                if (!CollidesWithWall(enemies[i].x, slideEY, boxSize))
                                    enemies[i] = (enemies[i].x, slideEY);
                            }
                        }
                    }
                    bool isPhased = i < enemyIsPhasing.Count && enemyIsPhasing[i] && !enemyIsVisible[i];
                    if (!hostDead && !isDashing && enemies[i].x < posX + playerSize && enemies[i].x + boxSize > posX &&
                        enemies[i].y < posY + playerSize && enemies[i].y + boxSize > posY)
                    {
                        if (hitCooldown <= 0)
                        {
                            bool isTank = i < enemyIsTank.Count && enemyIsTank[i];
                            bool canShoot = i < enemyCanShoot.Count && enemyCanShoot[i];
                            bool isRunner = i < enemyIsRunner.Count && enemyIsRunner[i];
                            bool isBerserker = i < enemyIsBerserker.Count && enemyIsBerserker[i];
                            float maxHp = isTank ? 8f : canShoot ? 4f : isRunner ? 1f : 2f;
                            bool berserkerActive = isBerserker && enemyHealth[i] < maxHp * 0.5f;
                            float dmg = isTank ? enemyDamage * 3f : enemyDamage;
                            if (berserkerActive) dmg *= 2f;
                            health -= dmg;
                            AddDamageNumber(posX + playerSize / 2, posY, dmg, Color.FromArgb(255, 255, 80, 80));
                            AddScreenShake(10f);
                            hurtVignette = Math.Min(1f, hurtVignette + 0.55f);
                            if (bloodMoney)
                            {
                                int scoreToAdd = (int)(dmg * 2);
                                for (int c = 0; c < scoreToAdd; c++)
                                    coins.Add((posX + playerSize / 2, posY + playerSize / 2, 0f, 0f));
                            }
                            hitCooldown = hitCooldownTime;
                            if (rapidReload)
                                ammo = Math.Min(ammo + 5, maxAmmo);
                            if (thorns)
                                DamageEnemy(i, 1f);
                            if (health <= 0)
                                HandlePlayerDeath();
                        }
                    }
                    // P2 enemy collision (multiplayer host only)
                    if (isMultiplayer && isNetHost && !p2Dead && !p2Dashing &&
                        enemies[i].x < p2X + playerSize && enemies[i].x + boxSize > p2X &&
                        enemies[i].y < p2Y + playerSize && enemies[i].y + boxSize > p2Y)
                    {
                        bool isTank2 = i < enemyIsTank.Count && enemyIsTank[i];
                        float dmg2 = isTank2 ? enemyDamage * 3f : enemyDamage;
                        p2Health -= dmg2 * deltaTime * 2f;
                        if (bloodMoney)
                        {
                            int scoreToAdd = (int)(dmg2 * 2);
                            for (int c = 0; c < scoreToAdd; c++)
                                coins.Add((p2X + playerSize / 2, p2Y + playerSize / 2, 0f, 0f));
                        }
                        if (rapidReload)
                            p2Ammo = Math.Min(p2Ammo + 5, p2MaxAmmo);
                        if (thorns)
                            DamageEnemy(i, 1f);
                        if (p2Health <= 0)
                        {
                            p2Health = 0;
                            p2Dead = true;
                            if (hostDead)
                            {
                                netManager?.SendGameOver();
                                HandleMultiplayerGameOver();
                            }
                        }
                    }
                    if (i < enemyCanShoot.Count && enemyCanShoot[i])
                    {
                        enemyShootTimers[i] += deltaTime;
                        if (enemyShootTimers[i] >= enemyShootRate)
                        {
                            enemyShootTimers[i] = 0f;
                            float targetX, targetY;
                            if (decoyActive) { targetX = decoyX; targetY = decoyY; }
                            else
                            {
                                targetX = posX + playerSize / 2; targetY = posY + playerSize / 2;
                                if (isMultiplayer && !p2Dead)
                                {
                                    float sd1 = hostDead ? float.MaxValue : (targetX - enemies[i].x) * (targetX - enemies[i].x) + (targetY - enemies[i].y) * (targetY - enemies[i].y);
                                    float sd2 = (p2X + playerSize / 2 - enemies[i].x) * (p2X + playerSize / 2 - enemies[i].x) + (p2Y + playerSize / 2 - enemies[i].y) * (p2Y + playerSize / 2 - enemies[i].y);
                                    if (sd2 < sd1) { targetX = p2X + playerSize / 2; targetY = p2Y + playerSize / 2; }
                                }
                            }
                            float sDirX = targetX - enemies[i].x;
                            float sDirY = targetY - enemies[i].y;
                            float sDist = (float)Math.Sqrt(sDirX * sDirX + sDirY * sDirY);
                            if (sDist > 0)
                            {
                                enemyBullets.Add((
                                    enemies[i].x + boxSize / 2,
                                    enemies[i].y + boxSize / 2,
                                    (sDirX / sDist) * enemyBulletSpeed,
                                    (sDirY / sDist) * enemyBulletSpeed
                                ));
                            }
                        }
                    }
                    if (flameWall && wallActive && i < enemyFlameTimers.Count)
                    {
                        bool touchingFlameWall = false;
                        if (boxWall && boxWalls.Count > 0)
                        {
                            foreach (var bw in boxWalls)
                            {
                                float bwLeft = bw.x - bw.width / 2 - boxSize;
                                float bwRight = bw.x + bw.width / 2 + boxSize;
                                float bwTop = bw.y - bw.height / 2 - boxSize;
                                float bwBottom = bw.y + bw.height / 2 + boxSize;
                                if (enemies[i].x < bwRight && enemies[i].x + boxSize > bwLeft &&
                                    enemies[i].y < bwBottom && enemies[i].y + boxSize > bwTop)
                                {
                                    touchingFlameWall = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            float wLeft = tempWall.x - tempWall.width / 2 - boxSize;
                            float wRight = tempWall.x + tempWall.width / 2 + boxSize;
                            float wTop = tempWall.y - tempWall.height / 2 - boxSize;
                            float wBottom = tempWall.y + tempWall.height / 2 + boxSize;
                            if (enemies[i].x < wRight && enemies[i].x + boxSize > wLeft &&
                                enemies[i].y < wBottom && enemies[i].y + boxSize > wTop)
                                touchingFlameWall = true;
                        }
                        if (touchingFlameWall)
                        {
                            enemyFlameTimers[i] += deltaTime;
                            if (enemyFlameTimers[i] >= flameWallDamageRate)
                            {
                                enemyFlameTimers[i] = 0f;
                                DamageEnemy(i, flameWallDamage);
                            }
                        }
                        else
                            enemyFlameTimers[i] = 0f;
                    }

                    // Parasitic decay
                    if (i < enemyIsParasitic.Count && enemyIsParasitic[i] && enemyAlive[i])
                    {
                        enemyHealth[i] -= parasiticDecayRate * deltaTime;
                        if (enemyHealth[i] <= 0)
                        {
                            parasiteDecayKill = true;
                            enemyAlive[i] = false;
                            enemyRespawnTimers[i] = enemyRespawnTime;
                            int flashSize = boxSize;
                            deathFlashes.Add((enemies[i].x + flashSize / 2, enemies[i].y + flashSize / 2, 0.4f, 0.4f, flashSize));
                            totalKills++;
                            parasiteDecayKill = false;
                        }
                    }

                    if (i >= enemyIsFrenzied.Count) continue;

                    // Frenzied - erratic movement
                    if (enemyIsFrenzied[i])
                    {
                        enemyFrenziedAngle[i] += (float)(rng.NextDouble() * 2 - 1) * 5f * deltaTime;
                        float fDirX = (float)Math.Cos(enemyFrenziedAngle[i]);
                        float fDirY = (float)Math.Sin(enemyFrenziedAngle[i]);
                        float blend = 0.6f;
                        float tDirX = dirX / Math.Max(0.01f, dist);
                        float tDirY = dirY / Math.Max(0.01f, dist);
                        fDirX = fDirX * (1f - blend) + tDirX * blend;
                        fDirY = fDirY * (1f - blend) + tDirY * blend;
                        float fLen = (float)Math.Sqrt(fDirX * fDirX + fDirY * fDirY);
                        if (fLen > 0) { fDirX /= fLen; fDirY /= fLen; }
                        bool isRunner = i < enemyIsRunner.Count && enemyIsRunner[i];
                        float fSpeed = isRunner ? currentEnemySpeed * runnerSpeedMultiplier : currentEnemySpeed;
                        float newFX = enemies[i].x + fDirX * fSpeed * deltaTime * 60;
                        float newFY = enemies[i].y + fDirY * fSpeed * deltaTime * 60;
                        if (!CollidesWithWall(newFX, newFY, boxSize))
                            enemies[i] = (newFX, newFY);
                    }

                    // Zigzag movement
                    if (enemyIsZigzag[i])
                    {
                        enemyZigzagTimer[i] += deltaTime;
                        if (enemyZigzagTimer[i] >= 0.5f)
                        {
                            enemyZigzagTimer[i] = 0f;
                            enemyZigzagDirection[i] *= -1f;
                        }
                        float perpX = -dirY / Math.Max(0.01f, dist);
                        float perpY = dirX / Math.Max(0.01f, dist);
                        float zigSpeed = currentEnemySpeed * 0.5f;
                        float newZX = enemies[i].x + perpX * zigSpeed * enemyZigzagDirection[i] * deltaTime * 60;
                        float newZY = enemies[i].y + perpY * zigSpeed * enemyZigzagDirection[i] * deltaTime * 60;
                        if (!CollidesWithWall(newZX, newZY, boxSize))
                            enemies[i] = (newZX, newZY);
                    }

                    // Charging
                    if (enemyIsCharging[i])
                    {
                        if (enemyIsCharging_Active[i])
                        {
                            enemyChargeTimer[i] -= deltaTime;
                            float newCX = enemies[i].x + enemyChargeVelX[i] * deltaTime * 60;
                            float newCY = enemies[i].y + enemyChargeVelY[i] * deltaTime * 60;
                            if (!CollidesWithWall(newCX, newCY, boxSize))
                                enemies[i] = (newCX, newCY);
                            if (enemyChargeTimer[i] <= 0)
                            {
                                enemyIsCharging_Active[i] = false;
                                enemyChargeCooldown[i] = 3f;
                            }
                        }
                        else
                        {
                            enemyChargeCooldown[i] -= deltaTime;
                            if (enemyChargeCooldown[i] <= 0 && dist < 400f * scale)
                            {
                                enemyIsCharging_Active[i] = true;
                                enemyChargeTimer[i] = 0.4f;
                                float cLen = Math.Max(0.01f, dist);
                                enemyChargeVelX[i] = (dirX / cLen) * currentEnemySpeed * 4f;
                                enemyChargeVelY[i] = (dirY / cLen) * currentEnemySpeed * 4f;
                            }
                        }
                    }

                    // Armored - first hit blocked
                    if (enemyIsArmored[i] && !enemyArmorBroken[i])
                    {
                        // Armor is checked in DamageEnemy
                    }

                    // Regenerating
                    if (enemyIsRegenerating[i])
                    {
                        float maxHp = i < enemyIsTank.Count && enemyIsTank[i] ? 8f :
                                      i < enemyCanShoot.Count && enemyCanShoot[i] ? 4f :
                                      i < enemyIsRunner.Count && enemyIsRunner[i] ? 1f : 2f;
                        enemyHealth[i] = Math.Min(maxHp, enemyHealth[i] + 0.5f * deltaTime);
                    }

                    // Phasing
                    if (enemyIsPhasing[i])
                    {
                        enemyPhasingTimer[i] += deltaTime;
                        enemyIsVisible[i] = (int)(enemyPhasingTimer[i] / 1.5f) % 2 == 0;
                        if (!enemyIsVisible[i])
                        {
                            // Skip collision with player while phased
                        }
                    }

                    // Berserker - double speed and damage below 50% HP
                    // Applied during movement and damage sections

                    // Corrupted - leave trail
                    if (enemyIsCorrupted[i])
                    {
                        corruptedTrails.Add((enemies[i].x + boxSize / 2, enemies[i].y + boxSize / 2, 2f));
                    }

                    (float ex, float ey) = PushOutOfWalls(enemies[i].x, enemies[i].y, boxSize);
                    enemies[i] = (ex, ey);
                }
            }
            // Corrupted trails
            var newTrails = new List<(float x, float y, float timer)>();
            foreach (var t in corruptedTrails)
            {
                float newTimer = t.timer - deltaTime;
                if (newTimer > 0)
                {
                    float dx = posX + playerSize / 2 - t.x;
                    float dy = posY + playerSize / 2 - t.y;
                    float dist2 = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (!hostDead && dist2 < boxSize * 2)
                    {
                        if (hitCooldown <= 0)
                        {
                            health -= 0.5f * deltaTime;
                            if (health <= 0)
                                HandlePlayerDeath();
                        }
                    }
                    newTrails.Add((t.x, t.y, newTimer));
                }
            }
            corruptedTrails = newTrails;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (!enemyAlive[i]) continue;
            

                for (int j = i + 1; j < enemies.Count; j++)
                {
                   
                    if (!enemyAlive[j]) continue;
                    float dx = enemies[j].x - enemies[i].x;
                    float dy = enemies[j].y - enemies[i].y;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist < boxSize && dist > 0)
                    {
                        float pushX = (dx / dist) * (boxSize - dist) / 2;
                        float pushY = (dy / dist) * (boxSize - dist) / 2;
                        float newIX = enemies[i].x - pushX;
                        float newIY = enemies[i].y - pushY;
                        float newJX = enemies[j].x + pushX;
                        float newJY = enemies[j].y + pushY;
                        if (!CollidesWithWall(newIX, newIY, boxSize)) enemies[i] = (newIX, newIY);
                        if (!CollidesWithWall(newJX, newJY, boxSize)) enemies[j] = (newJX, newJY);
                    }
                }
            }

            for (int i = 0; i < enemyRespawnTimers.Count; i++)
            {
                if (enemyRespawnTimers[i] > 0)
                {
                    if (!bossAlive)
                        enemyRespawnTimers[i] -= deltaTime;
                    if (enemyRespawnTimers[i] <= 0 && !bossAlive)
                    {
                        bool respawnCanShoot = rng.NextDouble() < shootingEnemyChance;
                        bool respawnIsTank = !respawnCanShoot && rng.NextDouble() < tankEnemyChance;
                        bool respawnIsRunner = !respawnCanShoot && !respawnIsTank && rng.NextDouble() < runnerEnemyChance;
                        bool respawnIsParasitic = rng.NextDouble() < parasiticEnemyChance;
                        enemies[i] = (rng.Next(0, ClientSize.Width - boxSize), 0);
                        enemyAlive[i] = true;
                        enemyRespawnTimers[i] = 0f;
                        enemyCanShoot[i] = respawnCanShoot;
                        if (i < enemyIsTank.Count) enemyIsTank[i] = respawnIsTank;
                        else enemyIsTank.Add(respawnIsTank);
                        if (i < enemyIsRunner.Count) enemyIsRunner[i] = respawnIsRunner;
                        else enemyIsRunner.Add(respawnIsRunner);
                        if (i < enemyIsParasitic.Count) enemyIsParasitic[i] = respawnIsParasitic;
                        else enemyIsParasitic.Add(respawnIsParasitic);
                        InitEnemyEffects(i);
                        enemyShootTimers[i] = 0f;
                        enemyHealth[i] = respawnIsTank ? 8f : respawnCanShoot ? 4f : respawnIsRunner ? 1f : 2f;
                    }
                }
            }

            // Bullets
            var newBullets = new List<(float x, float y, float velX, float velY, int bounces)>();
            foreach (var b in bullets)
            {
                float nx = b.x + b.velX * deltaTime * 60;
                float ny = b.y + b.velY * deltaTime * 60;
                // Trail segment from last position to new — fades over ~0.18s
                bulletTrails.Add((b.x, b.y, nx, ny, 0.18f, 0.18f));
                float newVelX = b.velX;
                float newVelY = b.velY;
                int bounces = b.bounces;
                if (homing)
                {
                    float closestDist = float.MaxValue;
                    float targetX = nx;
                    float targetY = ny;
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if (!enemyAlive[i]) continue;
                        float dx = enemies[i].x - nx;
                        float dy = enemies[i].y - ny;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d < closestDist) { closestDist = d; targetX = enemies[i].x; targetY = enemies[i].y; }
                    }
                    if (bossAlive)
                    {
                        float bdx = bossX - nx;
                        float bdy = bossY - ny;
                        float bd = (float)Math.Sqrt(bdx * bdx + bdy * bdy);
                        if (bd < closestDist) { targetX = bossX; targetY = bossY; }
                    }
                    if (closestDist < float.MaxValue)
                    {
                        float dx = targetX - nx;
                        float dy = targetY - ny;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d > 0)
                        {
                            newVelX += (dx / d) * 1f * deltaTime * 60;
                            newVelY += (dy / d) * 1f * deltaTime * 60;
                            float speed2 = (float)Math.Sqrt(newVelX * newVelX + newVelY * newVelY);
                            if (speed2 > bulletSpeed) { newVelX = (newVelX / speed2) * bulletSpeed; newVelY = (newVelY / speed2) * bulletSpeed; }
                        }
                    }
                }
                else if (smartBounce && bounces > 0)
                {
                    // Smart Bounce: after a ricochet, slowly curve toward the nearest
                    // enemy. The curve strength scales with bullet speed (faster bullet
                    // = sharper turn). Doesn't stack with homing.
                    float closestDist = float.MaxValue;
                    float targetX = nx;
                    float targetY = ny;
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if (!enemyAlive[i]) continue;
                        float dx = enemies[i].x - nx;
                        float dy = enemies[i].y - ny;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d < closestDist) { closestDist = d; targetX = enemies[i].x; targetY = enemies[i].y; }
                    }
                    if (bossAlive)
                    {
                        float bdx = bossX - nx;
                        float bdy = bossY - ny;
                        float bd = (float)Math.Sqrt(bdx * bdx + bdy * bdy);
                        if (bd < closestDist) { closestDist = bd; targetX = bossX; targetY = bossY; }
                    }
                    if (closestDist < float.MaxValue)
                    {
                        float dx = targetX - nx;
                        float dy = targetY - ny;
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d > 0)
                        {
                            // Very slow base turn rate, scaled by bullet speed
                            float turn = 0.2f * (bulletSpeed / 8f);
                            newVelX += (dx / d) * turn * deltaTime * 60;
                            newVelY += (dy / d) * turn * deltaTime * 60;
                            float speed2 = (float)Math.Sqrt(newVelX * newVelX + newVelY * newVelY);
                            if (speed2 > bulletSpeed) { newVelX = (newVelX / speed2) * bulletSpeed; newVelY = (newVelY / speed2) * bulletSpeed; }
                        }
                    }
                }
                if (ricochetBounces > 0 && bounces < ricochetBounces)
                {
                    if (nx < 0 || nx + bulletSize > ClientSize.Width) { newVelX = -newVelX; nx = b.x + newVelX * deltaTime * 60; bounces++; }
                    if (ny < 0 || ny + bulletSize > ClientSize.Height) { newVelY = -newVelY; ny = b.y + newVelY * deltaTime * 60; bounces++; }
                }
                if (CollidesWithWall(nx, ny, bulletSize))
                {
                    if (ricochetBounces > 0 && bounces < ricochetBounces)
                    {
                        float bounceX = b.x + b.velX * deltaTime * 60;
                        float bounceY = b.y - b.velY * deltaTime * 60;
                        if (!CollidesWithWall(bounceX, bounceY, bulletSize))
                            newBullets.Add((bounceX, bounceY, newVelX, -newVelY, bounces + 1));
                        else
                        {
                            float bounceX2 = b.x - b.velX * deltaTime * 60;
                            float bounceY2 = b.y + b.velY * deltaTime * 60;
                            if (!CollidesWithWall(bounceX2, bounceY2, bulletSize))
                                newBullets.Add((bounceX2, bounceY2, -newVelX, newVelY, bounces + 1));
                        }
                    }
                    continue;
                }

                // Boss bullet collision
                if (bossAlive && bossBulletHitCooldown <= 0 &&
                    nx + bulletSize > bossX && nx < bossX + bossSize * scale &&
                    ny + bulletSize > bossY && ny < bossY + bossSize * scale)
                {
                    float bulletDmg = (explosiveFinish && nextBulletIsLast) ? 3f : 1f;
                    if (lastStand && (health <= 15f || (isMultiplayer && p2Health <= 15f))) bulletDmg *= 2f;
                    bossHealth -= bulletDmg;
                    bossBulletHitCooldown = bossBulletHitCooldownTime;
                    if (bossHealth <= 0)
                        HandleBossDefeated();
                    if (!piercingBullets) continue;
                }
                if (bossBulletHitCooldown > 0)
                    bossBulletHitCooldown -= deltaTime;

                bool hitEnemy = false;
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (!enemyAlive[i]) continue;
                    if (nx + bulletSize > enemies[i].x && nx < enemies[i].x + boxSize &&
                        ny + bulletSize > enemies[i].y && ny < enemies[i].y + boxSize)
                    {
                        float bulletDmg = (explosiveFinish && nextBulletIsLast) ? 3f : 1f;
                        if (lastStand && (health <= 15f || (isMultiplayer && p2Health <= 15f))) bulletDmg *= 2f;
                        if (rng.NextDouble() >= enemyReinforceChance)
                            DamageEnemy(i, bulletDmg);
                        if (ricochetExplosion && bounces > 0)
                        {
                            for (int j = 0; j < enemies.Count; j++)
                            {
                                if (j == i || !enemyAlive[j]) continue;
                                float dx = enemies[j].x - nx;
                                float dy = enemies[j].y - ny;
                                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                                if (dist < ricochetExplosionRadius * scale)
                                    DamageEnemy(j, ricochetExplosionDamage, false);
                            }
                            if (bossAlive)
                            {
                                float bcx = bossX + bossSize * scale / 2;
                                float bcy = bossY + bossSize * scale / 2;
                                float bdx = bcx - nx;
                                float bdy = bcy - ny;
                                if (Math.Sqrt(bdx * bdx + bdy * bdy) < ricochetExplosionRadius * scale)
                                    DamageBoss(ricochetExplosionDamage);
                            }
                            deathFlashes.Add((nx, ny, 0.3f, 0.3f, (int)(ricochetExplosionRadius * scale)));
                        }
                        if (!piercingBullets) { hitEnemy = true; break; }
                    }
                }
                if (!hitEnemy)
                {
                    if (ricochetBounces > 0 && bounces < ricochetBounces || (nx > 0 && nx < ClientSize.Width && ny > 0 && ny < ClientSize.Height))
                        newBullets.Add((nx, ny, newVelX, newVelY, bounces));
                }
            }
            bullets = newBullets;

            // Gun smack: rotating the gun fast enough deals chip damage to touched enemies
            if (!hostDead && playerSpriteCropped != null)
            {
                float pcx_gs = posX + playerSize / 2f;
                float pcy_gs = posY + playerSize / 2f;
                float curAim = GetClampedAimAngle(pcx_gs, pcy_gs, playerSize);
                float angDelta = MathF.IEEERemainder(curAim - _prevAimAngle, MathF.Tau);
                float angVel = deltaTime > 0f ? MathF.Abs(angDelta) / deltaTime : 0f;
                _prevAimAngle = curAim;

                for (int i = 0; i < enemySmackCooldown.Count; i++)
                    if (enemySmackCooldown[i] > 0f) enemySmackCooldown[i] -= deltaTime;

                if (angVel > GunSmackAngularVelThreshold)
                {
                    var (smackTipX, smackTipY) = GetGunTipWorldAtAngle(pcx_gs, pcy_gs, playerSize, curAim);
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if (!enemyAlive[i]) continue;
                        if (i < enemySmackCooldown.Count && enemySmackCooldown[i] > 0f) continue;
                        bool isTank_s = i < enemyIsTank.Count && enemyIsTank[i];
                        bool canShoot_s = i < enemyCanShoot.Count && enemyCanShoot[i];
                        bool isRunner_s = i < enemyIsRunner.Count && enemyIsRunner[i];
                        int eSize_s = isTank_s ? boxSize + 20 : canShoot_s ? boxSize + 8 : isRunner_s ? boxSize - 8 : boxSize;
                        if (GunSegmentIntersectsRect(pcx_gs, pcy_gs, smackTipX, smackTipY, enemies[i].x, enemies[i].y, eSize_s))
                        {
                            DamageEnemy(i, GunSmackDamage);
                            if (i < enemySmackCooldown.Count) enemySmackCooldown[i] = GunSmackCooldownTime;
                        }
                    }
                }
            }

            // Update enemy aim angles with a rotation speed limit (smooth turning toward the player).
            // Closer enemies turn faster — keeps them from circling around the player at melee range.
            {
                float px = posX + playerSize / 2f;
                float py = posY + playerSize / 2f;
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (i >= enemyAimAngle.Count) break;
                    if (!enemyAlive[i]) continue;
                    float ecx = enemies[i].x + (boxSize) / 2f;
                    float ecy = enemies[i].y + (boxSize) / 2f;
                    // Pick nearest living target (host or p2) for aim
                    float tx = px, ty = py;
                    if (isMultiplayer && !p2Dead)
                    {
                        float d1h = hostDead ? float.MaxValue : (px - ecx) * (px - ecx) + (py - ecy) * (py - ecy);
                        float d2p = (p2X + playerSize / 2f - ecx) * (p2X + playerSize / 2f - ecx) + (p2Y + playerSize / 2f - ecy) * (p2Y + playerSize / 2f - ecy);
                        if (d2p < d1h) { tx = p2X + playerSize / 2f; ty = p2Y + playerSize / 2f; }
                    }
                    float distToTarget = MathF.Sqrt((tx - ecx) * (tx - ecx) + (ty - ecy) * (ty - ecy));
                    // Boost rotation speed when close (4x at very close range, 1x far away)
                    float proximityBoost = 1f + 6f * MathF.Max(0f, 1f - distToTarget / (boxSize * 6f));
                    float maxStep = EnemyMaxRotSpeed * proximityBoost * deltaTime;
                    float desired = MathF.Atan2(ty - ecy, tx - ecx);
                    float diff = MathF.IEEERemainder(desired - enemyAimAngle[i], MathF.Tau);
                    if (MathF.Abs(diff) <= maxStep)
                        enemyAimAngle[i] = desired;
                    else
                        enemyAimAngle[i] += MathF.Sign(diff) * maxStep;
                }
            }

            // Parasites
            var newParasites = new List<(float x, float y, float velX, float velY, float timer, float spawnDelay, float hitCooldown)>();
            var parasitesCopy = new List<(float x, float y, float velX, float velY, float timer, float spawnDelay, float hitCooldown)>(parasites);
            foreach (var p in parasitesCopy)
            {
                float newTimer = p.timer - deltaTime;
                if (newTimer <= 0) continue;
                float newDelay = Math.Max(0f, p.spawnDelay - deltaTime);
                float newHitCooldown = Math.Max(0f, p.hitCooldown - deltaTime);

                // Chase closest player (host or p2)
                float hostCX = posX + playerSize / 2;
                float hostCY = posY + playerSize / 2;
                float dx = hostCX - p.x;
                float dy = hostCY - p.y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                if (isMultiplayer && isNetHost && !p2Dead)
                {
                    float p2CX = p2X + playerSize / 2;
                    float p2CY = p2Y + playerSize / 2;
                    float dx2 = p2CX - p.x;
                    float dy2 = p2CY - p.y;
                    float dist2 = (float)Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                    if (dist2 < dist)
                    {
                        dx = dx2; dy = dy2; dist = dist2;
                    }
                }

                float newX = p.x;
                float newY = p.y;
                float newVelX = p.velX;
                float newVelY = p.velY;

                if (dist > 0)
                {
                    newVelX = (dx / dist) * parasiteSpeed;
                    newVelY = (dy / dist) * parasiteSpeed;
                }

                newX += newVelX * deltaTime;
                newY += newVelY * deltaTime;

                // Check collision with host
                if (!hostDead && newDelay <= 0 &&
                    newX + parasiteSize > posX && newX < posX + playerSize &&
                    newY + parasiteSize > posY && newY < posY + playerSize)
                {
                    if (!parasiteImmune && newHitCooldown <= 0)
                    {
                        health -= enemyDamage * 0.5f;
                        AddDamageNumber(posX + playerSize / 2, posY, enemyDamage * 0.5f, Color.FromArgb(255, 255, 80, 80));
                        AddScreenShake(6f);
                        hurtVignette = Math.Min(1f, hurtVignette + 0.35f);
                        newHitCooldown = 0.5f;
                        if (health <= 0)
                            HandlePlayerDeath();
                    }
                    newParasites.Add((newX, newY, newVelX, newVelY, newTimer, newDelay, newHitCooldown));
                    continue;
                }

                // Check collision with p2
                if (isMultiplayer && isNetHost && !p2Dead && newDelay <= 0 &&
                    newX + parasiteSize > p2X && newX < p2X + playerSize &&
                    newY + parasiteSize > p2Y && newY < p2Y + playerSize)
                {
                    if (!parasiteImmune && newHitCooldown <= 0)
                    {
                        p2Health -= enemyDamage * 0.5f;
                        newHitCooldown = 0.5f;
                        if (p2Health <= 0)
                        {
                            p2Health = 0;
                            p2Dead = true;
                            if (hostDead)
                            {
                                netManager?.SendGameOver();
                                HandleMultiplayerGameOver();
                            }
                        }
                    }
                    newParasites.Add((newX, newY, newVelX, newVelY, newTimer, newDelay, newHitCooldown));
                    continue;
                }

                newParasites.Add((newX, newY, newVelX, newVelY, newTimer, newDelay, newHitCooldown));
            }
            parasites = newParasites;
            var newEnemyBullets = new List<(float x, float y, float velX, float velY)>();
            foreach (var b in enemyBullets)
            {
                float nx = b.x + b.velX * deltaTime * 60;
                float ny = b.y + b.velY * deltaTime * 60;
                if (CollidesWithWall(nx, ny, enemyBulletSize)) continue;
                if (!hostDead &&
                    nx < posX + playerSize && nx + enemyBulletSize > posX &&
                    ny < posY + playerSize && ny + enemyBulletSize > posY)
                {
                    if (hitCooldown <= 0)
                    {
                        health -= enemyBulletDamage;
                        AddDamageNumber(posX + playerSize / 2, posY, enemyBulletDamage, Color.FromArgb(255, 255, 80, 80));
                        AddScreenShake(8f);
                        hurtVignette = Math.Min(1f, hurtVignette + 0.45f);
                        if (bloodMoney)
                        {
                            int scoreToAdd = (int)(enemyBulletDamage * 2);
                            for (int c = 0; c < scoreToAdd; c++)
                                coins.Add((posX + playerSize / 2, posY + playerSize / 2, 0f, 0f));
                        }
                        if (rapidReload)
                            ammo = Math.Min(ammo + 5, maxAmmo);
                        hitCooldown = hitCooldownTime;
                        if (health <= 0)
                            HandlePlayerDeath();
                    }
                    continue;
                }
                // P2 enemy bullet collision
                if (isMultiplayer && isNetHost && !p2Dead && !p2Dashing && p2HitCooldown <= 0 &&
                    nx < p2X + playerSize && nx + enemyBulletSize > p2X &&
                    ny < p2Y + playerSize && ny + enemyBulletSize > p2Y)
                {
                    p2Health -= enemyBulletDamage;
                    if (bloodMoney)
                    {
                        int scoreToAdd = (int)(enemyBulletDamage * 2);
                        for (int c = 0; c < scoreToAdd; c++)
                            coins.Add((p2X + playerSize / 2, p2Y + playerSize / 2, 0f, 0f));
                    }
                    if (rapidReload)
                        p2Ammo = Math.Min(p2Ammo + 5, p2MaxAmmo);
                    if (thorns) { /* no enemy to damage from bullet */ }
                    p2HitCooldown = hitCooldownTime;
                    if (p2Health <= 0)
                    {
                        p2Health = 0;
                        p2Dead = true;
                        if (hostDead) { netManager?.SendGameOver(); HandleMultiplayerGameOver(); }
                    }
                    continue;
                }
                if (nx > 0 && nx < ClientSize.Width && ny > 0 && ny < ClientSize.Height)
                    newEnemyBullets.Add((nx, ny, b.velX, b.velY));
            }
            enemyBullets = newEnemyBullets;

            var newCoins = new List<(float x, float y, float velX, float velY)>();
            foreach (var c in coins)
            {
                // Attract to closest living player
                float attractX = posX, attractY = posY;
                if (isMultiplayer && isNetHost && !p2Dead)
                {
                    float d1 = hostDead ? float.MaxValue : (posX - c.x) * (posX - c.x) + (posY - c.y) * (posY - c.y);
                    float d2 = (p2X - c.x) * (p2X - c.x) + (p2Y - c.y) * (p2Y - c.y);
                    if (d2 < d1) { attractX = p2X; attractY = p2Y; }
                }
                float dirX = attractX - c.x;
                float dirY = attractY - c.y;
                float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
                float newVelX = c.velX;
                float newVelY = c.velY;
                if (dist > 0) { newVelX += (dirX / dist) * (boxSize / 5f) * deltaTime * 60; newVelY += (dirY / dist) * (boxSize / 5f) * deltaTime * 60; }
                newVelX *= 0.9f;
                newVelY *= 0.9f;
                float nx = c.x + newVelX * deltaTime * 60;
                float ny = c.y + newVelY * deltaTime * 60;
                // Host collision
                bool collected = false;
                if (!hostDead && nx < posX + boxSize && nx + coinSize > posX && ny < posY + boxSize && ny + coinSize > posY)
                    collected = true;
                // P2 collision
                if (!collected && isMultiplayer && isNetHost && !p2Dead &&
                    nx < p2X + boxSize && nx + coinSize > p2X && ny < p2Y + boxSize && ny + coinSize > p2Y)
                    collected = true;
                if (collected)
                {
                    score += coinWorth;
                    totalScore += coinWorth;
                    totalCoinsCollected++;
                    AddCoinSparkle(nx + coinSize / 2, ny + coinSize / 2);
                    if (medic)
                    {
                        health = Math.Min(health + 0.5f, maxHealth);
                        if (isMultiplayer && !p2Dead) p2Health = Math.Min(p2Health + 0.5f, p2MaxHealth);
                    }
                }
                else
                    newCoins.Add((nx, ny, newVelX, newVelY));
            }
            coins = newCoins;

            if (cashback)
            {
                cashbackTimer += deltaTime;
                if (cashbackTimer >= cashbackInterval)
                {
                    cashbackTimer = 0f;
                    int refund = (int)(totalSpentSinceLastCashback * 0.1f);
                    if (refund > 0) { score += refund; totalSpentSinceLastCashback = 0f; }
                }
            }
            if (superActive)
            {
                superTimer -= deltaTime;
                if (superTimer <= 0) { superActive = false; superCooldown = superCooldownTime; if (ammo <= 0) reloading = true; }
            }
            if (superCooldown > 0) superCooldown -= deltaTime;
            if (wallActive)
            {
                wallTimer -= deltaTime;
                if (wallTimer <= 0) { wallActive = false; wallCooldown = wallCooldownTime; }
            }
            if (wallCooldown > 0) wallCooldown -= deltaTime;
            if (hitCooldown > 0) hitCooldown -= deltaTime;
            if (p2HitCooldown > 0) p2HitCooldown -= deltaTime;
            if (p2BlinkCooldown > 0) p2BlinkCooldown -= deltaTime;
            if (p2TurretCooldown > 0) p2TurretCooldown -= deltaTime;
            // Don't tick regen for a dead player (single or multi). Otherwise the
            // dead player keeps gaining health silently.
            if (!hostDead && health > 0)
            {
                regenTimer += deltaTime;
                if (regenTimer >= regenTime) { health = Math.Min(health + 1f, maxHealth); regenTimer = 0f; }
            }
            else
            {
                regenTimer = 0f;
            }

            // Orbit
            if (orbitCount > 0)
            {
                orbitAngle += orbitSpeed * deltaTime;
                float angleStep = (float)(Math.PI * 2 / orbitCount);
                for (int o = 0; o < orbitCount; o++)
                {
                    float angle = orbitAngle + angleStep * o;
                    float currentOrbitRadius = (orbitRadius + orbitRadiusBonus) * scale;
                    float ox = posX + playerSize / 2 + (float)Math.Cos(angle) * currentOrbitRadius;
                    float oy = posY + playerSize / 2 + (float)Math.Sin(angle) * currentOrbitRadius;
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if (!enemyAlive[i]) continue;
                        if (ox > enemies[i].x && ox < enemies[i].x + boxSize &&
                            oy > enemies[i].y && oy < enemies[i].y + boxSize)
                        {
                            DamageEnemy(i, 1f);
                            if (orbitalStrike && !orbitalSlowedEnemies.Contains(i))
                            {
                                orbitalSlowedEnemies.Add(i);
                                orbitalSlowTimers.Add(2f);
                            }
                            if (explosiveOrbit)
                            {
                                for (int j = 0; j < enemies.Count; j++)
                                {
                                    if (j == i || !enemyAlive[j]) continue;
                                    float dx = enemies[j].x - enemies[i].x;
                                    float dy = enemies[j].y - enemies[i].y;
                                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                                    if (dist < explosiveOrbitRadius * scale)
                                        DamageEnemy(j, explosiveOrbitDamage, false);
                                }
                                if (bossAlive)
                                {
                                    float bcx = bossX + bossSize * scale / 2;
                                    float bcy = bossY + bossSize * scale / 2;
                                    float bdx = bcx - (enemies[i].x + boxSize / 2);
                                    float bdy = bcy - (enemies[i].y + boxSize / 2);
                                    if (Math.Sqrt(bdx * bdx + bdy * bdy) < explosiveOrbitRadius * scale)
                                        DamageBoss(explosiveOrbitDamage);
                                }
                            }
                        }
                    }
                    if (bossAlive && bossOrbitHitCooldown <= 0 &&
                        ox > bossX && ox < bossX + bossSize * scale &&
                        oy > bossY && oy < bossY + bossSize * scale)
                    {
                        bossHealth -= 1f;
                        bossOrbitHitCooldown = bossOrbitHitCooldownTime;
                        if (bossHealth <= 0)
                            HandleBossDefeated();
                    }
                }
            }
            // P2 orbit (multiplayer — same orbit upgrades apply to both players)
            if (orbitCount > 0 && isMultiplayer && isNetHost && !p2Dead)
            {
                float angleStep = (float)(Math.PI * 2 / orbitCount);
                for (int o = 0; o < orbitCount; o++)
                {
                    float angle = orbitAngle + angleStep * o + (float)Math.PI; // offset so they don't overlap
                    float currentOrbitRadius = (orbitRadius + orbitRadiusBonus) * scale;
                    float ox = p2X + playerSize / 2 + (float)Math.Cos(angle) * currentOrbitRadius;
                    float oy = p2Y + playerSize / 2 + (float)Math.Sin(angle) * currentOrbitRadius;
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        if (!enemyAlive[i]) continue;
                        if (ox > enemies[i].x && ox < enemies[i].x + boxSize &&
                            oy > enemies[i].y && oy < enemies[i].y + boxSize)
                        {
                            DamageEnemy(i, 1f);
                            if (orbitalStrike && !orbitalSlowedEnemies.Contains(i))
                            {
                                orbitalSlowedEnemies.Add(i);
                                orbitalSlowTimers.Add(2f);
                            }
                            if (explosiveOrbit)
                            {
                                for (int j = 0; j < enemies.Count; j++)
                                {
                                    if (j == i || !enemyAlive[j]) continue;
                                    float dx = enemies[j].x - enemies[i].x;
                                    float dy = enemies[j].y - enemies[i].y;
                                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                                    if (dist < explosiveOrbitRadius * scale)
                                        DamageEnemy(j, explosiveOrbitDamage, false);
                                }
                            }
                        }
                    }
                    if (bossAlive && bossOrbitHitCooldown <= 0 &&
                        ox > bossX && ox < bossX + bossSize * scale &&
                        oy > bossY && oy < bossY + bossSize * scale)
                    {
                        bossHealth -= 1f;
                        bossOrbitHitCooldown = bossOrbitHitCooldownTime;
                        if (bossHealth <= 0)
                            HandleBossDefeated();
                    }
                }
            }

            if (bossOrbitHitCooldown > 0)
                bossOrbitHitCooldown -= deltaTime;

            // Flame wall damages the boss too
            if (bossAlive && flameWall && wallActive)
            {
                bool bossTouchingFlame = false;
                float bs = bossSize * scale;
                if (boxWall && boxWalls.Count > 0)
                {
                    foreach (var bw in boxWalls)
                    {
                        float bwLeft = bw.x - bw.width / 2 - bs;
                        float bwRight = bw.x + bw.width / 2 + bs;
                        float bwTop = bw.y - bw.height / 2 - bs;
                        float bwBottom = bw.y + bw.height / 2 + bs;
                        if (bossX < bwRight && bossX + bs > bwLeft &&
                            bossY < bwBottom && bossY + bs > bwTop)
                        { bossTouchingFlame = true; break; }
                    }
                }
                else
                {
                    float wLeft = tempWall.x - tempWall.width / 2 - bs;
                    float wRight = tempWall.x + tempWall.width / 2 + bs;
                    float wTop = tempWall.y - tempWall.height / 2 - bs;
                    float wBottom = tempWall.y + tempWall.height / 2 + bs;
                    if (bossX < wRight && bossX + bs > wLeft &&
                        bossY < wBottom && bossY + bs > wTop)
                        bossTouchingFlame = true;
                }
                if (bossTouchingFlame)
                {
                    bossFlameTimer += deltaTime;
                    if (bossFlameTimer >= flameWallDamageRate)
                    {
                        bossFlameTimer = 0f;
                        DamageBoss(flameWallDamage);
                    }
                }
                else
                    bossFlameTimer = 0f;
            }
            else
                bossFlameTimer = 0f;
            for (int i = orbitalSlowTimers.Count - 1; i >= 0; i--)
            {
                orbitalSlowTimers[i] -= deltaTime;
                if (orbitalSlowTimers[i] <= 0) { orbitalSlowedEnemies.RemoveAt(i); orbitalSlowTimers.RemoveAt(i); }
            }

            // Turret cooldown
            if (turretCooldown > 0)
                turretCooldown -= deltaTime;

            // Turret shooting
            for (int i = 0; i < turrets.Count; i++)
            {
                turretShootTimers[i] += deltaTime;
                if (turretShootTimers[i] >= turretShootRate)
                {
                    turretShootTimers[i] = 0f;
                    float closestDist = float.MaxValue;
                    int closestEnemy = -1;
                    for (int j = 0; j < enemies.Count; j++)
                    {
                        if (!enemyAlive[j]) continue;
                        float dx = enemies[j].x - turrets[i].x;
                        float dy = enemies[j].y - turrets[i].y;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (dist < turretRange * scale && dist < closestDist)
                        {
                            closestDist = dist;
                            closestEnemy = j;
                        }
                    }
                    if (closestEnemy >= 0)
                    {
                        float dirX = enemies[closestEnemy].x - turrets[i].x;
                        float dirY = enemies[closestEnemy].y - turrets[i].y;
                        float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
                        bullets.Add((turrets[i].x, turrets[i].y,
                            (dirX / dist) * bulletSpeed,
                            (dirY / dist) * bulletSpeed, 0));
                    }
                }
            }

            if (enemyInspectTimer > 0)
                enemyInspectTimer -= deltaTime;

            var newFlashes = new List<(float x, float y, float timer, float maxTimer, int size)>();
            foreach (var f in deathFlashes)
            {
                float newTimer = f.timer - deltaTime;
                if (newTimer > 0)
                    newFlashes.Add((f.x, f.y, newTimer, f.maxTimer, f.size));
            }
            deathFlashes = newFlashes;

            var newHitFlashes = new List<(float x, float y, float timer, float maxTimer, int size)>();
            foreach (var f in hitFlashes)
            {
                float newTimer = f.timer - deltaTime;
                if (newTimer > 0)
                    newHitFlashes.Add((f.x, f.y, newTimer, f.maxTimer, f.size));
            }
            hitFlashes = newHitFlashes;

            if (achievementToastTimer > 0)
                achievementToastTimer -= deltaTime;
            CheckAchievements();

            // --- Multiplayer network sync ---
            if (isMultiplayer && netManager != null)
            {
                try
                {
                    netManager.PollEvents();

                    // Don't send/apply game data during game over (death screen, ready-up)
                    if (!handlingMpGameOver)
                    {
                        if (isNetHost)
                        {
                            ApplyP2Input(latestP2Input);
                            var state = BuildGameStatePacket();
                            netManager.SendGameState(state);
                        }
                        else
                        {
                            // Normalize aim coords to 0-1 so host can scale to its resolution
                            float cw = ClientSize.Width;
                            float ch = ClientSize.Height;
                            var input = new PlayerInputPacket
                            {
                                MoveX = velocityX,
                                MoveY = velocityY,
                                AimX = mousePos.X / cw,
                                AimY = mousePos.Y / ch,
                                Shooting = mouseHeld,
                                Dashing = p2PendingDash,
                                ActivateSuper = p2PendingSuper,
                                ActivateWall = p2PendingWall,
                                WallAimX = mousePos.X / cw,
                                WallAimY = mousePos.Y / ch,
                                UpgradePurchaseIndex = p2PendingUpgrade,
                                ActivateBlink = p2PendingBlink,
                                BlinkAimX = mousePos.X / cw,
                                BlinkAimY = mousePos.Y / ch,
                                PlaceTurret = p2PendingTurret,
                                TurretAimX = mousePos.X / cw,
                                TurretAimY = mousePos.Y / ch,
                                ActivateDecoy = p2PendingDecoy,
                                DecoyAimX = mousePos.X / cw,
                                DecoyAimY = mousePos.Y / ch,
                                ActivateSpeedTrap = p2PendingSpeedTrap,
                                SpeedTrapAimX = mousePos.X / cw,
                                SpeedTrapAimY = mousePos.Y / ch,
                            };
                            p2PendingSuper = false;
                            p2PendingWall = false;
                            p2PendingDash = false;
                            p2PendingUpgrade = -1;
                            p2PendingBlink = false;
                            p2PendingTurret = false;
                            p2PendingDecoy = false;
                            p2PendingSpeedTrap = false;
                            netManager.SendPlayerInput(input);

                            if (latestGameState.HasValue)
                                ApplyGameState(latestGameState.Value);
                        }
                    }
                }
                catch { /* network error, skip frame */ }
            }

            UpdateJuice(deltaTime);
            this.Invalidate();
        }

        private void UpdateJuice(float dt)
        {
            // Screen shake
            if (screenShakeAmp > 0.01f)
            {
                screenShakeAmp = Math.Max(0f, screenShakeAmp - screenShakeAmp * 8f * dt - 30f * dt);
                float a = (float)(rng.NextDouble() * Math.PI * 2);
                shakeOffsetX = (float)Math.Cos(a) * screenShakeAmp;
                shakeOffsetY = (float)Math.Sin(a) * screenShakeAmp;
            }
            else
            {
                screenShakeAmp = 0f;
                shakeOffsetX = 0f;
                shakeOffsetY = 0f;
            }

            // Hurt vignette decays
            if (hurtVignette > 0f)
                hurtVignette = Math.Max(0f, hurtVignette - dt * 1.6f);

            // Smooth score (eases toward target)
            if (Math.Abs(displayedScore - score) < 0.5f) displayedScore = score;
            else displayedScore += (score - displayedScore) * Math.Min(1f, dt * 8f);

            // Smooth health lerp
            if (Math.Abs(displayedHealth - health) < 0.05f) displayedHealth = health;
            else displayedHealth += (health - displayedHealth) * Math.Min(1f, dt * 10f);

            // Damage numbers update
            for (int i = damageNumbers.Count - 1; i >= 0; i--)
            {
                var d = damageNumbers[i];
                d.timer -= dt;
                d.y += d.vy * dt;
                d.vy += 60f * dt; // mild gravity slowdown
                if (d.timer <= 0f) damageNumbers.RemoveAt(i);
                else damageNumbers[i] = d;
            }

            // Coin sparkles
            for (int i = coinSparkles.Count - 1; i >= 0; i--)
            {
                var s = coinSparkles[i];
                s.timer -= dt;
                if (s.timer <= 0f) coinSparkles.RemoveAt(i);
                else coinSparkles[i] = s;
            }

            // Bullet trails fade out
            for (int i = bulletTrails.Count - 1; i >= 0; i--)
            {
                var t = bulletTrails[i];
                t.timer -= dt;
                if (t.timer <= 0f) bulletTrails.RemoveAt(i);
                else bulletTrails[i] = t;
            }

            // Muzzle flashes fade quickly
            for (int i = muzzleFlashes.Count - 1; i >= 0; i--)
            {
                var m = muzzleFlashes[i];
                m.timer -= dt;
                if (m.timer <= 0f) muzzleFlashes.RemoveAt(i);
                else muzzleFlashes[i] = m;
            }

            // Death fragments — physics + decay
            for (int i = deathFragments.Count - 1; i >= 0; i--)
            {
                var f = deathFragments[i];
                f.x += f.vx * dt;
                f.y += f.vy * dt;
                f.vx *= MathF.Max(0f, 1f - dt * 3f);
                f.vy *= MathF.Max(0f, 1f - dt * 3f);
                f.vy += 260f * dt; // gravity
                f.angle += f.angVel * dt;
                f.timer -= dt;
                if (f.timer <= 0f) deathFragments.RemoveAt(i);
                else deathFragments[i] = f;
            }

            // Combo timer — resets streak after window of no kills
            if (comboCount > 0)
            {
                comboTimer += dt;
                if (comboTimer >= comboWindow) { comboCount = 0; comboTimer = 0f; }
            }
            if (comboShake > 0f) comboShake = MathF.Max(0f, comboShake - dt * 4f);

            // Enemy spawn anim sync + ramp
            while (enemySpawnAnim.Count < enemies.Count) enemySpawnAnim.Add(0f);
            while (enemySpawnAnim.Count > enemies.Count) enemySpawnAnim.RemoveAt(enemySpawnAnim.Count - 1);
            for (int i = 0; i < enemySpawnAnim.Count; i++)
            {
                if (i < enemyAlive.Count && !enemyAlive[i]) { enemySpawnAnim[i] = 0f; continue; }
                if (enemySpawnAnim[i] < 1f)
                    enemySpawnAnim[i] = Math.Min(1f, enemySpawnAnim[i] + dt * 4f);
            }
        }

        private void AddDamageNumber(float x, float y, float dmg, Color color)
        {
            damageNumbers.Add((x + (float)(rng.NextDouble() * 16 - 8), y, -60f - (float)rng.NextDouble() * 30f, 0.8f, 0.8f, ((int)Math.Ceiling(dmg)).ToString(), color));
        }

        private void AddScreenShake(float amp)
        {
            if (amp > screenShakeAmp) screenShakeAmp = Math.Min(28f, amp);
        }

        private void AddCoinSparkle(float x, float y)
        {
            for (int i = 0; i < 4; i++)
                coinSparkles.Add((x + (float)(rng.NextDouble() * 18 - 9), y + (float)(rng.NextDouble() * 18 - 9), 0.45f, 0.45f));
        }

        private void Form1_Paint(object? sender, PaintEventArgs e)
        {
            if (onMainMenu)
            {
                e.Graphics.Clear(Color.FromArgb(20, 20, 20));
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Title top left
                e.Graphics.DrawString("RED GUY",
                    new Font("Arial", 72 * scaleY, FontStyle.Bold),
                    new SolidBrush(Color.FromArgb(220, 30, 30)),
                    new RectangleF(40 * scaleX, 40 * scaleY, 800 * scaleX, 120 * scaleY),
                    new StringFormat { Alignment = StringAlignment.Near });

                e.Graphics.DrawString("TAKEOVER",
                    new Font("Arial", 36 * scaleY, FontStyle.Bold),
                    new SolidBrush(Color.FromArgb(180, 180, 180)),
                    new RectangleF(50 * scaleX, 155 * scaleY, 800 * scaleX, 60 * scaleY),
                    new StringFormat { Alignment = StringAlignment.Near });

                e.Graphics.DrawString("ALPHA RELEASE",
                    new Font("Arial", 14 * scaleY),
                    new SolidBrush(Color.FromArgb(100, 100, 100)),
                    new RectangleF(55 * scaleX, 220 * scaleY, 800 * scaleX, 30 * scaleY),
                    new StringFormat { Alignment = StringAlignment.Near });

                // Red coins display (top-right)
                string rcText = $"🔴 {redCoins}";
                using var rcFont = new Font("Segoe UI Emoji", 16 * scaleY, FontStyle.Bold);
                var rcSize2 = e.Graphics.MeasureString(rcText, rcFont);
                float rcX2 = ClientSize.Width - rcSize2.Width - 20 * scaleX;
                float rcY2 = 20 * scaleY;
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(160, 15, 15, 25)),
                    rcX2 - 10 * scaleX, rcY2 - 4 * scaleY, rcSize2.Width + 20 * scaleX, rcSize2.Height + 8 * scaleY);
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(120, 200, 50, 50), 2),
                    rcX2 - 10 * scaleX, rcY2 - 4 * scaleY, rcSize2.Width + 20 * scaleX, rcSize2.Height + 8 * scaleY);
                e.Graphics.DrawString(rcText, rcFont,
                    new SolidBrush(Color.FromArgb(255, 220, 60, 60)),
                    rcX2, rcY2);

                // Controls bottom
                e.Graphics.DrawString("WASD: Move  |  LMB: Shoot  |  Space: Dash  |  Tab: Upgrades  |  ESC: Pause  |  MMB: Inspect",
                    new Font("Arial", 11 * scaleY),
                    new SolidBrush(Color.FromArgb(80, 80, 80)),
                    new RectangleF(0, ClientSize.Height - 40 * scaleY, ClientSize.Width, 30 * scaleY),
                    new StringFormat { Alignment = StringAlignment.Center });

                // Draw player (sprite facing the enemy, or fallback square)
                int mSize = (int)(40 * scale);
                if (playerSpriteCropped != null)
                {
                    float menuAimAngle = (float)Math.Atan2(menuEnemyY - menuPlayerY, menuEnemyX - menuPlayerX);
                    DrawPlayerSprite(e.Graphics, menuPlayerX, menuPlayerY, mSize, menuAimAngle);
                }
                else
                {
                    using var path = new System.Drawing.Drawing2D.GraphicsPath();
                    int r = Math.Max(1, mSize / 5);
                    float px = menuPlayerX - mSize / 2;
                    float py = menuPlayerY - mSize / 2;
                    path.AddArc(px, py, r, r, 180, 90);
                    path.AddArc(px + mSize - r, py, r, r, 270, 90);
                    path.AddArc(px + mSize - r, py + mSize - r, r, r, 0, 90);
                    path.AddArc(px, py + mSize - r, r, r, 90, 90);
                    path.CloseFigure();
                    e.Graphics.FillPath(new SolidBrush(playerColor), path);
                    e.Graphics.DrawPath(Pens.White, path);
                }

                // Draw enemy
                if (!menuEnemyDead)
                {
                    int eSize = menuEnemyType == 2 ? (int)(50 * scale) :
                                menuEnemyType == 1 ? (int)(22 * scale) : (int)(30 * scale);
                    Color eColor = menuEnemyHitTimer > 0 ? Color.White :
                                   menuEnemyType == 2 ? Color.DarkRed :
                                   menuEnemyType == 1 ? Color.HotPink : Color.Red;

                    using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        int r = Math.Max(1, eSize / 5);
                        float ex = menuEnemyX - eSize / 2;
                        float ey = menuEnemyY - eSize / 2;
                        path.AddArc(ex, ey, r, r, 180, 90);
                        path.AddArc(ex + eSize - r, ey, r, r, 270, 90);
                        path.AddArc(ex + eSize - r, ey + eSize - r, r, r, 0, 90);
                        path.AddArc(ex, ey + eSize - r, r, r, 90, 90);
                        path.CloseFigure();
                        e.Graphics.FillPath(new SolidBrush(eColor), path);
                        e.Graphics.DrawPath(Pens.White, path);
                    }

                    // Enemy health bar
                    float menuEnemyHpFill = menuEnemyHealth / menuEnemyMaxHealth;
                    e.Graphics.FillRectangle(Brushes.DarkRed, menuEnemyX - eSize / 2, menuEnemyY - eSize / 2 - 10, eSize, 5);
                    e.Graphics.FillRectangle(Brushes.LimeGreen, menuEnemyX - eSize / 2, menuEnemyY - eSize / 2 - 10, eSize * menuEnemyHpFill, 5);
                }
                else
                {
                    // Death flash
                    if (menuEnemyDeadTimer > 1.0f)
                    {
                        float alpha = (menuEnemyDeadTimer - 1.0f) / 0.5f;
                        int a = (int)(alpha * 255);
                        int eSize = menuEnemyType == 2 ? (int)(50 * scale) :
                                    menuEnemyType == 1 ? (int)(22 * scale) : (int)(30 * scale);
                        e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(a, 255, 200, 0)),
                            menuEnemyX - eSize, menuEnemyY - eSize, eSize * 2, eSize * 2);
                    }
                }

                // Draw bullet
                if (menuBulletActive)
                {
                    int bSize = (int)(8 * scale);
                    e.Graphics.FillRectangle(Brushes.White, menuBulletX - bSize / 2, menuBulletY - bSize / 2, bSize, bSize);
                }
                // Dark overlay for panel-based popups
                if (showDimOverlay)
                {
                    e.Graphics.FillRectangle(
                        new SolidBrush(Color.FromArgb(180, 0, 0, 0)),
                        0, 0, ClientSize.Width, ClientSize.Height);
                }
                // Draw preferences
                if (onPreferences)
                {
                    // Dark overlay
                    e.Graphics.FillRectangle(
                        new SolidBrush(Color.FromArgb(180, 0, 0, 0)),
                        0, 0, ClientSize.Width, ClientSize.Height);

                    // Background panel
                    int panelW = (int)(600 * scaleX);
                    int panelH = (int)(500 * scaleY);
                    int panelX = ClientSize.Width / 2 - panelW / 2;
                    int panelY = ClientSize.Height / 2 - panelH / 2;

                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(240, 30, 30, 40)),
                        panelX, panelY, panelW, panelH);
                    e.Graphics.DrawRectangle(new Pen(Color.FromArgb(100, 100, 120), 2),
                        panelX, panelY, panelW, panelH);

                    // Title
                    e.Graphics.DrawString("PREFERENCES",
                        new Font("Arial", 28 * scaleY, FontStyle.Bold),
                        Brushes.White,
                        new RectangleF(panelX, panelY + 20 * scaleY, panelW, 50 * scaleY),
                        new StringFormat { Alignment = StringAlignment.Center });

                    // Divider
                    e.Graphics.DrawLine(new Pen(Color.FromArgb(80, 80, 100), 1),
                        panelX + 20, panelY + 70 * scaleY,
                        panelX + panelW - 20, panelY + 70 * scaleY);

                    // Color label
                    e.Graphics.DrawString("Player Color:",
                        new Font("Arial", 13 * scaleY, FontStyle.Bold),
                        Brushes.LightGray,
                        new RectangleF(panelX + 20, panelY + 85 * scaleY, panelW, 30 * scaleY),
                        new StringFormat { Alignment = StringAlignment.Near });

                    // Preview box
                    int previewSize = (int)(40 * scale);
                    e.Graphics.FillRectangle(new SolidBrush(playerColor),
                        panelX + panelW - 60 * scaleX, panelY + 80 * scaleY, previewSize, previewSize);
                    e.Graphics.DrawRectangle(Pens.White,
                        panelX + panelW - 60 * scaleX, panelY + 80 * scaleY, previewSize, previewSize);
                    e.Graphics.DrawString("Preview",
                        new Font("Arial", 8 * scaleY),
                        Brushes.Gray,
                        new RectangleF(panelX + panelW - 70 * scaleX, panelY + 125 * scaleY, 80 * scaleX, 20 * scaleY),
                        new StringFormat { Alignment = StringAlignment.Near });

                    // Name label
                    e.Graphics.DrawString("Player Name:",
                        new Font("Arial", 13 * scaleY, FontStyle.Bold),
                        Brushes.LightGray,
                        new RectangleF(panelX + 20, panelY + 330 * scaleY, panelW, 30 * scaleY),
                        new StringFormat { Alignment = StringAlignment.Near });

                    e.Graphics.DrawString("(max 8 characters)",
                        new Font("Arial", 9 * scaleY),
                        Brushes.Gray,
                        new RectangleF(panelX + 20, panelY + 360 * scaleY, panelW, 25 * scaleY),
                        new StringFormat { Alignment = StringAlignment.Near });
                }
                if (!onPreferences && !showDimOverlay)
                {
                    for (int d = 0; d < 9; d++)
                    {
                        bool locked = d > highestUnlockedDifficulty;
                        Color c = locked ? Color.FromArgb(60, 60, 60) : DifficultyColors[d];
                        int col = d % 3;
                        int row = d / 3;
                        float dx = ClientSize.Width * 0.6f + col * 120;
                        float dy = ClientSize.Height / 2 - 80 + row * 45;
                        e.Graphics.DrawString(locked ? "🔒" : "✓",
                            new Font("Arial", 8 * scaleY),
                            new SolidBrush(c), dx, dy);
                        e.Graphics.DrawString(DifficultyNames[d],
                            new Font("Arial", 9 * scaleY, FontStyle.Bold),
                            new SolidBrush(c), dx, dy + 14 * scaleY);
                    }
                }
                if (showingUnlockAnimation)
                {
                    // Dark overlay that fades in and out
                    float elapsed = unlockAnimDuration - unlockAnimTimer;
                    float textScale = elapsed < 0.3f ? Math.Max(0.01f, elapsed / 0.3f * 1.2f) : 1f;
                    float overlayAlpha = elapsed < 0.5f ? elapsed / 0.5f : 1f;
                    int oa = (int)(overlayAlpha * 180);
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(oa, 0, 0, 0)),
                        0, 0, ClientSize.Width, ClientSize.Height);
                    // Background panel
                    float panelFade = Math.Min(1f, (unlockAnimDuration - unlockAnimTimer));
                    int panelAlpha = (int)(Math.Min(1f, panelFade) * 230);
                    int panelW = (int)(700 * scaleX);
                    int panelH = (int)(480 * scaleY);
                    int panelX = ClientSize.Width / 2 - panelW / 2;
                    int panelY = ClientSize.Height / 2 - panelH / 2;
                    e.Graphics.FillRectangle(
                        new SolidBrush(Color.FromArgb(panelAlpha, 15, 15, 25)),
                        panelX, panelY, panelW, panelH);
                    Color panelBorder = unlockedDifficultyIndex >= 0 && unlockedDifficultyIndex < 9
                        ? DifficultyColors[unlockedDifficultyIndex] : Color.White;
                    e.Graphics.DrawRectangle(
                        new Pen(Color.FromArgb(panelAlpha, panelBorder), 3),
                        panelX, panelY, panelW, panelH);
                    // Particles
                    foreach (var p in unlockParticles)
                    {
                        float alpha = Math.Min(1f, p.timer);
                        int a = (int)(alpha * 255);
                        int pSize = (int)(8 * scale);
                        e.Graphics.FillEllipse(
                            new SolidBrush(Color.FromArgb(a, p.color)),
                            p.x - pSize / 2, p.y - pSize / 2, pSize, pSize);
                    }

                    // Shockwave ring
                    float ringProgress = 1f - (unlockAnimTimer / unlockAnimDuration);
                    float ringRadius = ringProgress * ClientSize.Width * 0.8f;
                    int ringAlpha = (int)((1f - ringProgress) * 200);
                    if (ringAlpha > 0)
                    {
                        string[] diffNames = { "EASY", "NORMAL", "HARD", "NIGHTMARE" };
                        Color[] diffColors = { Color.LimeGreen, Color.DodgerBlue, Color.Orange, Color.Red };
                        Color ringColor = unlockedDifficultyIndex >= 0 && unlockedDifficultyIndex < 9
                            ? DifficultyColors[unlockedDifficultyIndex] : Color.White;
                        e.Graphics.DrawEllipse(
                            new Pen(Color.FromArgb(ringAlpha, ringColor), 4),
                            ClientSize.Width / 2f - ringRadius,
                            ClientSize.Height / 2f - ringRadius,
                            ringRadius * 2, ringRadius * 2);
                    }

                    // Main text
                    string unlockedName = unlockedDifficultyIndex >= 0 && unlockedDifficultyIndex < 9
                        ? DifficultyNames[unlockedDifficultyIndex].ToUpper() : "";
                    Color unlockedColor = unlockedDifficultyIndex >= 0 && unlockedDifficultyIndex < 9
                        ? DifficultyColors[unlockedDifficultyIndex] : Color.White;

                    int textAlpha = (int)(Math.Min(1f, overlayAlpha) * 255);

                    e.Graphics.DrawString("DIFFICULTY UNLOCKED",
    new Font("Arial", Math.Max(1f, (float)(22 * scaleY * textScale)), FontStyle.Bold),
    new SolidBrush(Color.FromArgb(textAlpha, Color.White)),
    new RectangleF(0, ClientSize.Height / 2f - 120, ClientSize.Width, 60),
    new StringFormat { Alignment = StringAlignment.Center });

                    e.Graphics.DrawString(unlockedName,
                        new Font("Arial", Math.Max(1f, (float)(60 * scaleY * textScale)), FontStyle.Bold),
                        new SolidBrush(Color.FromArgb(textAlpha, unlockedColor)),
                        new RectangleF(0, ClientSize.Height / 2f - 70, ClientSize.Width, 100),
                        new StringFormat { Alignment = StringAlignment.Center });

                    // What changed
                    string[] changes = unlockedDifficultyIndex switch
                    {
                        1 => new[] { "Speed +17%", "Damage +20%", "Boss Timer: 165s" },
                        2 => new[] { "Speed +67%", "Damage: 1.0", "Boss Timer: 120s", "Parasitic Enemies Spawn" },
                        3 => new[] { "Speed +6%", "Damage +10%", "Boss Timer: 110s", "Parasitic: 2%" },
                        4 => new[] { "Speed +6%", "Damage: 1.3", "Boss Timer: 100s", "Parasitic: 3%" },
                        5 => new[] { "Speed +7%", "Damage: 1.5", "Boss Timer: 90s", "Score: 2x", "Parasitic: 5%" },
                        6 => new[] { "Speed +5%", "Damage: 1.7", "Boss Timer: 80s", "Parasitic: 7%" },
                        7 => new[] { "Speed +6%", "Damage: 1.85", "Boss Timer: 70s", "Parasitic: 8.5%" },
                        8 => new[] { "Speed +4%", "Damage: 2.0", "Boss Timer: 60s", "Parasitic: 10%" },
                        _ => Array.Empty<string>()
                    };

                    for (int ci = 0; ci < changes.Length; ci++)
                    {
                        e.Graphics.DrawString(changes[ci],
                            new Font("Arial", (float)(14 * scaleY), FontStyle.Regular),
                            new SolidBrush(Color.FromArgb(textAlpha, Color.LightGray)),
                            new RectangleF(0, ClientSize.Height / 2f + 50 + ci * 30 * scaleY, ClientSize.Width, 30),
                            new StringFormat { Alignment = StringAlignment.Center });
                    }
                    // Click to continue
                    if (unlockAnimDuration - unlockAnimTimer > 1f)
                    {
                        float blinkAlpha = (float)Math.Abs(Math.Sin((unlockAnimDuration - unlockAnimTimer) * 3f));
                        e.Graphics.DrawString("Click anywhere to continue",
                            new Font("Arial", (float)(13 * scaleY)),
                            new SolidBrush(Color.FromArgb((int)(blinkAlpha * 200), Color.White)),
                            new RectangleF(0, ClientSize.Height - 80, ClientSize.Width, 30),
                            new StringFormat { Alignment = StringAlignment.Center });
                    }
                }
                return;
            }
          
            if (darkMode)
                e.Graphics.Clear(Color.FromArgb(20, 20, 20));
            else
                e.Graphics.Clear(Color.White);

            // Apply screen shake to the world (HUD will be untranslated below)
            var __shakeState = e.Graphics.Save();
            if (shakeOffsetX != 0f || shakeOffsetY != 0f)
                e.Graphics.TranslateTransform(shakeOffsetX, shakeOffsetY);

            Pen borderPen = darkMode ? Pens.Black : Pens.Black;
            Brush textBrush = darkMode ? Brushes.White : Brushes.Black;
            Brush barTextBrush = Brushes.Black;
            String Ufont = "Arial";

            if (speedTrapActive)
            {
                float alpha = speedTrapTimer / speedTrapDuration;
                int a = (int)(alpha * 200);
                float r = speedTrapRadius * scale;
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(a, 100, 0, 200)), speedTrapX - r, speedTrapY - r, r * 2, r * 2);
                e.Graphics.DrawEllipse(new Pen(Color.FromArgb(200, 100, 0, 200), 2), speedTrapX - r, speedTrapY - r, r * 2, r * 2);
            }

            // Draw boss
            if (bossAlive)
            {
                float scaledBossSize = bossSize * scale;
                if (bossSpriteCropped != null)
                {
                    float bcx = bossX + scaledBossSize / 2f;
                    float bcy = bossY + scaledBossSize / 2f;
                    // DrawEnemySprite multiplies size by EnemySpriteDrawScale (1.9). Cancel that
                    // so the sprite roughly fills the bossSize box (slightly larger).
                    float bossExtra = 1.05f / EnemySpriteDrawScale;
                    DrawEnemySprite(e.Graphics, bossSpriteCropped, bcx, bcy, (int)scaledBossSize, bossAimAngle, 255, bossExtra);
                }
                else
                {
                    int br = (int)(scaledBossSize / 5);
                    using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                    {
                        path.AddArc(bossX, bossY, br, br, 180, 90);
                        path.AddArc(bossX + scaledBossSize - br, bossY, br, br, 270, 90);
                        path.AddArc(bossX + scaledBossSize - br, bossY + scaledBossSize - br, br, br, 0, 90);
                        path.AddArc(bossX, bossY + scaledBossSize - br, br, br, 90, 90);
                        path.CloseFigure();
                        e.Graphics.FillPath(new SolidBrush(Color.FromArgb(120, 0, 0)), path);
                        e.Graphics.DrawPath(new Pen(darkMode ? Color.White : Color.Black, 3), path);
                    }
                }
                // Boss health bar
                float bossHpFill = bossHealth / currentBossMaxHealth;
                int bossBarW = (int)scaledBossSize;
                int bossBarH = 8;
                e.Graphics.FillRectangle(Brushes.DarkRed, bossX, bossY - 14, bossBarW, bossBarH);
                e.Graphics.FillRectangle(Brushes.Red, bossX, bossY - 14, bossBarW * bossHpFill, bossBarH);
                e.Graphics.DrawRectangle(borderPen, bossX, bossY - 14, bossBarW, bossBarH);
                e.Graphics.DrawString("BOSS", GetFontUIBold(), Brushes.Red, bossX + scaledBossSize / 2 - 20, bossY - 30);

                // Boss timer bar at top of screen
                int bossBarWidth = (int)(400 * scaleX);
                int bossBarHeight = (int)(20 * scaleY);
                int bossBarX = ClientSize.Width / 2 - bossBarWidth / 2;
                int bossBarY = 10;
                e.Graphics.FillRectangle(Brushes.DarkRed, bossBarX, bossBarY, bossBarWidth, bossBarHeight);
                e.Graphics.FillRectangle(Brushes.Red, bossBarX, bossBarY, (int)(bossBarWidth * bossHpFill), bossBarHeight);
                e.Graphics.DrawRectangle(borderPen, bossBarX, bossBarY, bossBarWidth, bossBarHeight);
                e.Graphics.DrawString("BOSS HP: " + (int)bossHealth + " / " + (int)currentBossMaxHealth, GetFontUIBold(), barTextBrush, bossBarX + bossBarWidth / 2 - 60, bossBarY);
            }
            else if (gameStartTimer > gameStartDelay)
            {
                // Show time until next boss
                float timeLeft = bossSpawnInterval_Current - bossSpawnTimer;
                int bossBarWidth = (int)(400 * scaleX);
                int bossBarHeight = (int)(20 * scaleY);
                int bossBarX = ClientSize.Width / 2 - bossBarWidth / 2;
                int bossBarY = 10;
                float fill = bossSpawnTimer / bossSpawnInterval_Current;
                e.Graphics.FillRectangle(Brushes.Gray, bossBarX, bossBarY, bossBarWidth, bossBarHeight);
                e.Graphics.FillRectangle(Brushes.DarkRed, bossBarX, bossBarY, (int)(bossBarWidth * fill), bossBarHeight);
                e.Graphics.DrawRectangle(borderPen, bossBarX, bossBarY, bossBarWidth, bossBarHeight);
                e.Graphics.DrawString("BOSS IN: " + (int)(bossSpawnInterval_Current - bossSpawnTimer) + "s", GetFontUIBold(), barTextBrush, bossBarX + bossBarWidth / 2 - 40, bossBarY);
            }
            // Draw Player 1
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (!hostDead)
            {
                float cx = posX + playerSize / 2f;
                float cy = posY + playerSize / 2f;
                if (playerSpriteCropped != null)
                {
                    float aimAngle = GetClampedAimAngle(cx, cy, playerSize);
                    int spriteAlpha = isDashing ? 150 : 255;
                    DrawPlayerSprite(e.Graphics, cx, cy, playerSize, aimAngle, spriteAlpha);
                }
                else
                {
                    // fallback rounded rect
                    using var path = new System.Drawing.Drawing2D.GraphicsPath();
                    float r = playerSize / 5f;
                    path.AddArc(posX, posY, r, r, 180, 90);
                    path.AddArc(posX + playerSize - r, posY, r, r, 270, 90);
                    path.AddArc(posX + playerSize - r, posY + playerSize - r, r, r, 0, 90);
                    path.AddArc(posX, posY + playerSize - r, r, r, 90, 90);
                    path.CloseFigure();
                    e.Graphics.FillPath(new SolidBrush(isDashing ? Color.FromArgb(150, playerColor.R, playerColor.G, playerColor.B) : playerColor), path);
                    e.Graphics.DrawPath(borderPen, path);
                }
            }

            // Reload circle near gun tip
            if (reloading && !hostDead)
            {
                float rcSize = 10 * scale;
                float pcx = posX + playerSize / 2f;
                float pcy = posY + playerSize / 2f;
                float aimAngle = GetClampedAimAngle(pcx, pcy, playerSize);
                var (tipX, tipY) = GetGunTipWorldAtAngle(pcx, pcy, playerSize, aimAngle);
                float gap = rcSize * 1.1f;
                float rcCx = tipX + (float)Math.Cos(aimAngle) * gap;
                float rcCy = tipY + (float)Math.Sin(aimAngle) * gap;
                float rcX = rcCx - rcSize / 2;
                float rcY = rcCy - rcSize / 2;
                float progress = reloadTime > 0 ? reloadTimer / reloadTime : 0f;
                int sweepAngle = (int)(360 * progress);
                using var rcPen = new Pen(Color.FromArgb(200, 255, 215, 0), 2.5f * scale);
                e.Graphics.DrawArc(rcPen, rcX, rcY, rcSize, rcSize, -90, sweepAngle);
            }

            // Draw Player 2 in multiplayer
            if (isMultiplayer)
            {
                using (System.Drawing.Drawing2D.GraphicsPath p2path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    float r2 = playerSize / 5;
                    p2path.AddArc(p2X, p2Y, r2, r2, 180, 90);
                    p2path.AddArc(p2X + playerSize - r2, p2Y, r2, r2, 270, 90);
                    p2path.AddArc(p2X + playerSize - r2, p2Y + playerSize - r2, r2, r2, 0, 90);
                    p2path.AddArc(p2X, p2Y + playerSize - r2, r2, r2, 90, 90);
                    p2path.CloseFigure();
                    int p2Alpha = p2Dead ? 60 : 255;
                    Color baseP2Color = isNetHost ? Color.FromArgb(255, 80, 140, 255) : p2Color_synced;
                    Color p2Color = p2Dead ? Color.FromArgb(p2Alpha, 100, 100, 100) :
                        p2Dashing ? Color.FromArgb(150, baseP2Color.R, baseP2Color.G, baseP2Color.B) :
                        Color.FromArgb(p2Alpha, baseP2Color.R, baseP2Color.G, baseP2Color.B);
                    e.Graphics.FillPath(new SolidBrush(p2Color), p2path);
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(40, 80, 160), 2), p2path);
                }
                // P2 name tag
                using (var nameBrush = new SolidBrush(Color.FromArgb(180, 80, 140, 255)))
                {
                    e.Graphics.DrawString(p2Name, GetFontSmall(), nameBrush,
                        p2X + playerSize / 2 - 20 * scale, p2Y - 18 * scale);
                }
                // P2 health bar
                if (p2MaxHealth > 0)
                {
                    float hbW = playerSize + 10 * scale;
                    float hbX = p2X - 5 * scale;
                    float hbY = p2Y + playerSize + 4 * scale;
                    float p2HpFill = Math.Max(0, p2Health / p2MaxHealth);
                    e.Graphics.FillRectangle(Brushes.DarkGray, hbX, hbY, hbW, 4 * scale);
                    e.Graphics.FillRectangle(Brushes.DodgerBlue, hbX, hbY, hbW * p2HpFill, 4 * scale);
                }
                // Reload circle above P2
                if (p2Reloading && !p2Dead)
                {
                    float rcSize = 10 * scale;
                    float rcX = p2X + playerSize / 2 - rcSize / 2;
                    float rcY = p2Y - rcSize - 4 * scale;
                    float progress = reloadTime > 0 ? p2ReloadTimer / reloadTime : 0f;
                    int sweepAngle = (int)(360 * progress);
                    using var rcPen = new Pen(Color.FromArgb(200, 255, 215, 0), 2.5f * scale);
                    e.Graphics.DrawArc(rcPen, rcX, rcY, rcSize, rcSize, -90, sweepAngle);
                }
            }

            // Draw enemies
            for (int i = 0; i < enemies.Count; i++)
            {
                if (!enemyAlive[i]) continue;
                bool isRunner = i < enemyIsRunner.Count && enemyIsRunner[i];
                bool isTank = i < enemyIsTank.Count && enemyIsTank[i];
                bool canShoot = i < enemyCanShoot.Count && enemyCanShoot[i];
                bool isSlowed = orbitalStrike && orbitalSlowedEnemies.Contains(i);
                bool isParasitic = i < enemyIsParasitic.Count && enemyIsParasitic[i];
                bool isPhasing = i < enemyIsPhasing.Count && enemyIsPhasing[i];
                bool isCurrentlyVisible = i < enemyIsVisible.Count ? enemyIsVisible[i] : true;
                int enemyAlpha = isPhasing && !isCurrentlyVisible ? 60 : 255;
                int eSize = isTank ? boxSize + 20 : canShoot ? boxSize + 8 : isRunner ? boxSize - 8 : boxSize;
                int r = Math.Max(1, eSize / 5);

                // Red enemies, gunners, tanks, and runners use rotating sprites. Parasitic variants get parasitic sprites.
                // Gunners are drawn a little larger via GunnerExtraScale; tank/runner already have adjusted eSize.
                bool isPlainNormal = !isSlowed && !isTank && !canShoot && !isRunner;
                bool isPlainGunner = !isSlowed && !isTank && canShoot && !isRunner;
                bool isPlainTank   = !isSlowed && isTank && !canShoot && !isRunner;
                bool isPlainRunner = !isSlowed && !isTank && !canShoot && isRunner;
                Bitmap? enemySprite =
                      isPlainNormal ? (isParasitic ? redParasiticSpriteCropped : redEnemySpriteCropped)
                    : isPlainGunner ? (isParasitic ? gunnerParasiticSpriteCropped : gunnerSpriteCropped)
                    : isPlainTank   ? (isParasitic ? tankParasiticSpriteCropped : tankSpriteCropped)
                    : isPlainRunner ? (isParasitic ? runnerParasiticSpriteCropped : runnerSpriteCropped)
                    : null;
                float spriteExtraScale = isPlainGunner ? GunnerExtraScale : 1f;
                // Spawn-in pop: ease-out elastic-ish using overshoot curve on enemySpawnAnim
                if (i < enemySpawnAnim.Count)
                {
                    float sa = enemySpawnAnim[i];
                    if (sa < 1f)
                    {
                        // 1 - (1-t)^3 with overshoot at end
                        float t = sa;
                        float pop = 1f - (float)Math.Pow(1f - t, 3f);
                        // overshoot: spike at ~0.7 then settle
                        float overshoot = 1f + 0.25f * (float)Math.Sin(t * Math.PI) * (1f - t);
                        spriteExtraScale *= pop * overshoot;
                    }
                }
                if (enemySprite != null)
                {
                    float ecx = enemies[i].x + eSize / 2f;
                    float ecy = enemies[i].y + eSize / 2f;
                    float drawAim = i < enemyAimAngle.Count ? enemyAimAngle[i]
                        : MathF.Atan2((posY + playerSize / 2f) - ecy, (posX + playerSize / 2f) - ecx);
                    DrawEnemySprite(e.Graphics, enemySprite, ecx, ecy, eSize, drawAim, enemyAlpha, spriteExtraScale);
                }
                else
                using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddArc(enemies[i].x, enemies[i].y, r, r, 180, 90);
                    path.AddArc(enemies[i].x + eSize - r, enemies[i].y, r, r, 270, 90);
                    path.AddArc(enemies[i].x + eSize - r, enemies[i].y + eSize - r, r, r, 0, 90);
                    path.AddArc(enemies[i].x, enemies[i].y + eSize - r, r, r, 90, 90);
                    path.CloseFigure();

                    Brush enemyBrush = isSlowed ? new SolidBrush(Color.FromArgb(enemyAlpha, Color.MediumPurple)) :
                                       isParasitic ? new SolidBrush(Color.FromArgb(
                                           enemyAlpha,
                                           isTank ? Color.DarkRed.R : canShoot ? Color.OrangeRed.R : isRunner ? Color.HotPink.R : Color.Red.R,
                                           0,
                                           isTank ? (int)(Color.DarkRed.B * 0.5f + 80) : 80)) :
                                       isTank ? new SolidBrush(Color.FromArgb(enemyAlpha, Color.DarkRed)) :
                                       canShoot ? new SolidBrush(Color.FromArgb(enemyAlpha, Color.OrangeRed)) :
                                       isRunner ? new SolidBrush(Color.FromArgb(enemyAlpha, Color.HotPink)) :
                                       new SolidBrush(Color.FromArgb(enemyAlpha, Color.Red));

                    e.Graphics.FillPath(enemyBrush, path);
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(enemyAlpha, borderPen.Color)), path);
                }

                if (i < enemyHealth.Count)
                {
                    float maxEHp = isTank ? 8f : canShoot ? 4f : isRunner ? 1f : 2f;
                    float enemyHpFill = enemyHealth[i] / maxEHp;
                    int enemyBarW = eSize;
                    float enemyBarH = 4 * scale;
                    float enemyBarX = enemies[i].x;
                    float enemyBarY = enemies[i].y - 8 * scale;
                    e.Graphics.FillRectangle(Brushes.DarkRed, enemyBarX, enemyBarY, enemyBarW, enemyBarH);
                    e.Graphics.FillRectangle(Brushes.LimeGreen, enemyBarX, enemyBarY, enemyBarW * enemyHpFill, enemyBarH);
                    e.Graphics.DrawRectangle(borderPen, enemyBarX, enemyBarY, enemyBarW, enemyBarH);
                }

                // Effect indicators
                float ringOff = 3 * scale;
                float ringSize = eSize + 6 * scale;
                if (i < enemyIsArmored.Count && enemyIsArmored[i] && !enemyArmorBroken[i])
                {
                    e.Graphics.DrawEllipse(new Pen(Color.Silver, 3 * scale),
                        enemies[i].x - ringOff, enemies[i].y - ringOff, ringSize, ringSize);
                }
                if (i < enemyIsCharging.Count && enemyIsCharging[i] && i < enemyIsCharging_Active.Count && enemyIsCharging_Active[i])
                {
                    e.Graphics.DrawEllipse(new Pen(Color.Yellow, 2 * scale),
                        enemies[i].x - ringOff, enemies[i].y - ringOff, ringSize, ringSize);
                }
                if (i < enemyIsReflective.Count && enemyIsReflective[i])
                {
                    e.Graphics.DrawEllipse(new Pen(Color.FromArgb(100, 200, 200, 255), 2 * scale),
                        enemies[i].x - 2 * scale, enemies[i].y - 2 * scale, eSize + 4 * scale, eSize + 4 * scale);
                }
                if (i < enemyIsBerserker.Count && enemyIsBerserker[i])
                {
                    float maxHpDraw = isTank ? 8f : canShoot ? 4f : isRunner ? 1f : 2f;
                    if (i < enemyHealth.Count && enemyHealth[i] < maxHpDraw * 0.5f)
                    {
                        float pulse = (float)Math.Abs(Math.Sin(gameStartTimer * 8f));
                        e.Graphics.DrawEllipse(new Pen(Color.FromArgb((int)(pulse * 255), 255, 50, 0), 2 * scale),
                            enemies[i].x - ringOff, enemies[i].y - ringOff, ringSize, ringSize);
                    }
                }
                if (i < enemyIsRegenerating.Count && enemyIsRegenerating[i])
                {
                    e.Graphics.DrawEllipse(new Pen(Color.FromArgb(80, 0, 255, 100), 1),
                        enemies[i].x - 2, enemies[i].y - 2, eSize + 4, eSize + 4);
                }
                if (i < enemyIsCorrupted.Count && enemyIsCorrupted[i])
                {
                    float pulse2 = (float)Math.Abs(Math.Sin(gameStartTimer * 4f));
                    e.Graphics.DrawEllipse(new Pen(Color.FromArgb((int)(pulse2 * 180), 80, 0, 150), 2),
                        enemies[i].x - 3, enemies[i].y - 3, eSize + 6, eSize + 6);
                }
                if (i < enemyIsZigzag.Count && enemyIsZigzag[i])
                {
                    e.Graphics.DrawEllipse(new Pen(Color.FromArgb(120, Color.Cyan), 1),
                        enemies[i].x - 2, enemies[i].y - 2, eSize + 4, eSize + 4);
                }
                if (i < enemyIsFrenzied.Count && enemyIsFrenzied[i])
                {
                    float pulse3 = (float)Math.Abs(Math.Sin(gameStartTimer * 12f));
                    e.Graphics.DrawEllipse(new Pen(Color.FromArgb((int)(pulse3 * 200), Color.Orange), 1),
                        enemies[i].x - 2, enemies[i].y - 2, eSize + 4, eSize + 4);
                }
            }

            // Corrupted trails
            foreach (var t in corruptedTrails)
            {
                float alpha = t.timer / 2f;
                int a = (int)(alpha * 150);
                int tSize = (int)(boxSize * 0.6f);
                e.Graphics.FillEllipse(
                    new SolidBrush(Color.FromArgb(a, 80, 0, 150)),
                    t.x - tSize / 2, t.y - tSize / 2, tSize, tSize);
            }

            if (enemyInspectTimer > 0 && inspectedEnemyIndex >= 0 &&
    inspectedEnemyIndex < enemies.Count && enemyAlive[inspectedEnemyIndex])
            {
                int i = inspectedEnemyIndex;
                bool isTank = i < enemyIsTank.Count && enemyIsTank[i];
                bool canShoot = i < enemyCanShoot.Count && enemyCanShoot[i];
                bool isRunner = i < enemyIsRunner.Count && enemyIsRunner[i];
                int eSize = isTank ? boxSize + 20 : canShoot ? boxSize + 8 : isRunner ? boxSize - 8 : boxSize;

                string type = isTank ? "TANK" : canShoot ? "GUNNER" : isRunner ? "RUNNER" : "NORMAL";
                float maxHp = isTank ? 8f : canShoot ? 4f : isRunner ? 1f : 2f;
                float dmg = isTank ? enemyDamage * 3f : enemyDamage;
                float spd = isRunner ? currentEnemySpeed * runnerSpeedMultiplier : currentEnemySpeed;

                string inspectText =
                    $"[ {type} ]\n" +
                    $"HP: {enemyHealth[i]:F0} / {maxHp:F0}\n" +
                    $"DMG: {dmg:F0}\n" +
                    $"SPD: {spd:F1}\n" +
                    (canShoot ? "Shoots bullets\n" : "") +
                    (isTank ? "3x player damage\n" : "") +
                    (isRunner ? "2.5x move speed\n" : "");

                string[] lines = inspectText.Split('\n');
                int lineHeight = (int)(18 * scaleY);
                int padding = (int)(8 * scaleX);
                int boxWidth = (int)(160 * scaleX);
                int boxHeight = lines.Length * lineHeight + padding * 2;

                float drawX = enemies[i].x + eSize + 10;
                float drawY = enemies[i].y;
                if (drawX + boxWidth > ClientSize.Width) drawX = enemies[i].x - boxWidth - 10;
                if (drawY + boxHeight > ClientSize.Height) drawY = ClientSize.Height - boxHeight - 10;

                float alpha = Math.Min(1f, enemyInspectTimer / 0.5f);
                int a = (int)(alpha * 220);

                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(a, 20, 20, 30)),
                    drawX, drawY, boxWidth, boxHeight);
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(a, 180, 180, 180)),
                    drawX, drawY, boxWidth, boxHeight);

                for (int li = 0; li < lines.Length; li++)
                {
                    if (string.IsNullOrWhiteSpace(lines[li])) continue;
                    Color lineColor = li == 0 ? Color.Gold : Color.White;
                    e.Graphics.DrawString(lines[li],
                        new Font("Arial", 9 * scaleY, li == 0 ? FontStyle.Bold : FontStyle.Regular),
                        new SolidBrush(Color.FromArgb(a, lineColor)),
                        drawX + padding, drawY + padding + li * lineHeight);
                }
            }
            else
            {
                inspectedEnemyIndex = -1;
            }
            // Orbit bullet
            if (orbitCount > 0)
            {
                float angleStep = (float)(Math.PI * 2 / orbitCount);
                for (int o = 0; o < orbitCount; o++)
                {
                    float angle = orbitAngle + angleStep * o;
                    float currentOrbitRadius = (orbitRadius + orbitRadiusBonus) * scale;
                    float ox = posX + playerSize / 2 + (float)Math.Cos(angle) * currentOrbitRadius;
                    float oy = posY + playerSize / 2 + (float)Math.Sin(angle) * currentOrbitRadius;
                    e.Graphics.FillEllipse(Brushes.Cyan, ox - 6, oy - 6, 12, 12);
                    e.Graphics.DrawEllipse(borderPen, ox - 6, oy - 6, 12, 12);
                }
                // P2 orbit rendering
                if (isMultiplayer && !p2Dead)
                {
                    for (int o = 0; o < orbitCount; o++)
                    {
                        float angle = orbitAngle + angleStep * o + (float)Math.PI;
                        float currentOrbitRadius = (orbitRadius + orbitRadiusBonus) * scale;
                        float ox = p2X + playerSize / 2 + (float)Math.Cos(angle) * currentOrbitRadius;
                        float oy = p2Y + playerSize / 2 + (float)Math.Sin(angle) * currentOrbitRadius;
                        e.Graphics.FillEllipse(Brushes.Cyan, ox - 6, oy - 6, 12, 12);
                        e.Graphics.DrawEllipse(borderPen, ox - 6, oy - 6, 12, 12);
                    }
                }
            }
            // Draw parasites
            foreach (var p in parasites)
            {
                float alpha = Math.Min(1f, p.timer / parasiteDuration);
                int a = (int)(alpha * 255);
                int pSize = Math.Max(15, (int)(parasiteSize * scale));
                e.Graphics.FillEllipse(
                    new SolidBrush(Color.FromArgb(a, 180, 0, 220)),
                    p.x - pSize / 2, p.y - pSize / 2, pSize, pSize);
                e.Graphics.DrawEllipse(
                    new Pen(Color.FromArgb(a, darkMode ? Color.White : Color.Black)),
                    p.x - pSize / 2, p.y - pSize / 2, pSize, pSize);
            }
            foreach (var f in deathFlashes)
            {
                float alpha = f.timer / f.maxTimer;
                int a = (int)(alpha * 255);
                float radius = f.size * (1f + (1f - alpha) * 2f);
                e.Graphics.FillEllipse(
                    new SolidBrush(Color.FromArgb(a, 255, 200, 0)),
                    f.x - radius, f.y - radius, radius * 2, radius * 2);
            }
            foreach (var f in hitFlashes)
            {
                float alpha = f.timer / f.maxTimer;
                int a = (int)(alpha * 180);
                float radius = f.size * 0.6f;
                e.Graphics.FillEllipse(
                    new SolidBrush(Color.FromArgb(a, 255, 255, 255)),
                    f.x - radius, f.y - radius, radius * 2, radius * 2);
            }
            // Death fragments — rotated colored squares
            foreach (var f in deathFragments)
            {
                float alpha = MathF.Max(0f, f.timer / f.maxTimer);
                int a = (int)(alpha * 230);
                var state = e.Graphics.Save();
                e.Graphics.TranslateTransform(f.x, f.y);
                e.Graphics.RotateTransform(f.angle * 180f / MathF.PI);
                using (var b = new SolidBrush(Color.FromArgb(a, f.color)))
                    e.Graphics.FillRectangle(b, -f.size / 2, -f.size / 2, f.size, f.size);
                using (var p = new Pen(Color.FromArgb(a, 0, 0, 0), 1.5f))
                    e.Graphics.DrawRectangle(p, -f.size / 2, -f.size / 2, f.size, f.size);
                e.Graphics.Restore(state);
            }
            // Bullet trails — short fading lines
            foreach (var t in bulletTrails)
            {
                float alpha = MathF.Max(0f, t.timer / t.maxTimer);
                int a = (int)(alpha * 180);
                Color tc = darkMode ? Color.FromArgb(a, 255, 230, 140) : Color.FromArgb(a, 220, 60, 30);
                float w = bulletSize * 0.7f * (0.4f + alpha * 0.6f);
                using var pen = new Pen(tc, w) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
                e.Graphics.DrawLine(pen, t.x1 + bulletSize / 2, t.y1 + bulletSize / 2, t.x2 + bulletSize / 2, t.y2 + bulletSize / 2);
            }
            foreach (var b in bullets)
            {
                e.Graphics.FillRectangle(darkMode ? Brushes.White : Brushes.DarkRed, b.x, b.y, bulletSize, bulletSize);
                e.Graphics.DrawRectangle(darkMode ? Pens.LightGray : Pens.Black, b.x, b.y, bulletSize, bulletSize);
            }
            // Muzzle flashes — bright starburst at gun tip
            foreach (var m in muzzleFlashes)
            {
                float alpha = MathF.Max(0f, m.timer / m.maxTimer);
                int a = (int)(alpha * 255);
                float len = 22f * (0.5f + alpha);
                float wid = 10f * (0.5f + alpha);
                var state = e.Graphics.Save();
                e.Graphics.TranslateTransform(m.x, m.y);
                e.Graphics.RotateTransform(m.angle * 180f / MathF.PI);
                using (var br = new SolidBrush(Color.FromArgb(a, 255, 240, 120)))
                {
                    var pts = new PointF[] {
                        new PointF(0, 0),
                        new PointF(len * 0.55f, -wid / 2),
                        new PointF(len, 0),
                        new PointF(len * 0.55f, wid / 2)
                    };
                    e.Graphics.FillPolygon(br, pts);
                }
                using (var br2 = new SolidBrush(Color.FromArgb((int)(a * 0.8f), 255, 255, 230)))
                    e.Graphics.FillEllipse(br2, -wid * 0.35f, -wid * 0.35f, wid * 0.7f, wid * 0.7f);
                e.Graphics.Restore(state);
            }



            foreach (var b in enemyBullets)
            {
                e.Graphics.FillEllipse(Brushes.OrangeRed, b.x, b.y, enemyBulletSize, enemyBulletSize);
                e.Graphics.DrawEllipse(borderPen, b.x, b.y, enemyBulletSize, enemyBulletSize);
            }

            foreach (var c in coins)
            {
                e.Graphics.FillEllipse(Brushes.Gold, c.x, c.y, coinSize, coinSize);
                e.Graphics.DrawEllipse(borderPen, c.x, c.y, coinSize, coinSize);
            }

            if (decoyActive)
            {
                int alpha = (int)(255 * (decoyTimer / decoyDuration));
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(alpha, 0, 10, 200)), decoyX - 15, decoyY - 15, 30, 30);
                e.Graphics.DrawEllipse(new Pen(Color.FromArgb(alpha, darkMode ? Color.White : Color.Black)), decoyX - 15, decoyY - 15, 30, 30);
            }
            foreach (var t in turrets)
            {
                int tSize = (int)(20 * scale);
                e.Graphics.FillRectangle(Brushes.SlateGray, t.x - tSize / 2, t.y - tSize, tSize, tSize * 2);
                e.Graphics.FillEllipse(Brushes.DimGray, t.x - tSize, t.y - tSize, tSize * 2, tSize * 2);
                e.Graphics.DrawEllipse(borderPen, t.x - tSize, t.y - tSize, tSize * 2, tSize * 2);
                // Range indicator (faint)
                float r = turretRange * scale;
                e.Graphics.DrawEllipse(new Pen(Color.FromArgb(30, 100, 100, 100)), t.x - r, t.y - r, r * 2, r * 2);
            }
            if (isPaused && !onMainMenu)
            {
                if (isMultiplayer)
                {
                    e.Graphics.DrawString("AFK", new Font(Ufont, 32 * scaleY), textBrush, ClientSize.Width / 2 - 40, ClientSize.Height / 2 - 20);
                    e.Graphics.DrawString("Press ESC to resume", GetFontUI(), textBrush, ClientSize.Width / 2 - 50, ClientSize.Height / 2 + 20);
                }
                else
                {
                    e.Graphics.DrawString("Game Paused", new Font(Ufont, 32 * scaleY), textBrush, ClientSize.Width / 2 - 120, ClientSize.Height / 2 - 20);
                    e.Graphics.DrawString("Press ESC", GetFontUI(), textBrush, ClientSize.Width / 2 - 15, ClientSize.Height / 2 + 20);
                }
            }

            foreach (var w in walls)
                e.Graphics.FillRectangle(darkMode ? Brushes.LightGray : Brushes.Black, w.x, w.y, w.width, w.height);

            int barWidth = (int)(200 * scaleX);
            int barHeight = (int)(16 * scaleY);
            int barX = ClientSize.Width / 2 - barWidth / 2;
            int barY = ClientSize.Height - 40;
            e.Graphics.FillRectangle(Brushes.Gray, barX, barY, barWidth, barHeight);
            if (superActive)
            {
                float fill = superTimer / superDuration;
                e.Graphics.FillRectangle(Brushes.Cyan, barX, barY, (int)(barWidth * fill), barHeight);
                e.Graphics.DrawString("SUPER ACTIVE", GetFontUI(), barTextBrush, barX + 50, barY);
            }
            else if (superCooldown > 0)
            {
                float fill = 1f - (superCooldown / superCooldownTime);
                e.Graphics.FillRectangle(Brushes.DarkCyan, barX, barY, (int)(barWidth * fill), barHeight);
                e.Graphics.DrawString("Q: " + (int)superCooldown + "s", GetFontUI(), barTextBrush, barX + 70, barY);
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.Cyan, barX, barY, barWidth, barHeight);
                e.Graphics.DrawString("Q: READY", GetFontUI(), barTextBrush, barX + 65, barY);
            }
            e.Graphics.DrawRectangle(borderPen, barX, barY, barWidth, barHeight);

            int wallBarWidth = (int)(200 * scaleX);
            int wallBarHeight = (int)(16 * scaleY);
            int wallBarX = ClientSize.Width / 2 - wallBarWidth / 2;
            int wallBarY = ClientSize.Height - 70;
            e.Graphics.FillRectangle(Brushes.Gray, wallBarX, wallBarY, wallBarWidth, wallBarHeight);
            if (wallActive)
            {
                float fill = wallTimer / wallDuration;
                e.Graphics.FillRectangle(Brushes.Orange, wallBarX, wallBarY, (int)(wallBarWidth * fill), wallBarHeight);
                e.Graphics.DrawString("WALL: " + (int)wallTimer + "s", GetFontUI(), barTextBrush, wallBarX + 60, wallBarY);
            }
            else if (wallCooldown > 0)
            {
                float fill = 1f - (wallCooldown / wallCooldownTime);
                e.Graphics.FillRectangle(Brushes.DarkOrange, wallBarX, wallBarY, (int)(wallBarWidth * fill), wallBarHeight);
                e.Graphics.DrawString("E: " + (int)wallCooldown + "s", GetFontUI(), barTextBrush, wallBarX + 70, wallBarY);
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.Orange, wallBarX, wallBarY, wallBarWidth, wallBarHeight);
                e.Graphics.DrawString("E: READY", GetFontUI(), barTextBrush, wallBarX + 65, wallBarY);
            }
            e.Graphics.DrawRectangle(borderPen, wallBarX, wallBarY, wallBarWidth, wallBarHeight);

            if (wallActive)
            {
                if (boxWall && boxWalls.Count > 0)
                {
                    foreach (var bw in boxWalls)
                    {
                        System.Drawing.Drawing2D.GraphicsState state = e.Graphics.Save();
                        e.Graphics.TranslateTransform(bw.x, bw.y);
                        e.Graphics.RotateTransform(bw.angle * 180f / (float)Math.PI);
                        e.Graphics.FillRectangle(flameWall ? Brushes.OrangeRed : Brushes.SaddleBrown, -bw.width / 2, -bw.height / 2, bw.width, bw.height);
                        e.Graphics.DrawRectangle(borderPen, -bw.width / 2, -bw.height / 2, bw.width, bw.height);
                        e.Graphics.Restore(state);
                    }
                }
                else
                {
                    System.Drawing.Drawing2D.GraphicsState state = e.Graphics.Save();
                    e.Graphics.TranslateTransform(tempWall.x, tempWall.y);
                    e.Graphics.RotateTransform(tempWall.angle * 180f / (float)Math.PI);
                    e.Graphics.FillRectangle(flameWall ? Brushes.OrangeRed : Brushes.SaddleBrown, -tempWall.width / 2, -tempWall.height / 2, tempWall.width, tempWall.height);
                    e.Graphics.DrawRectangle(borderPen, -tempWall.width / 2, -tempWall.height / 2, tempWall.width, tempWall.height);
                    e.Graphics.Restore(state);
                }
            }
            if (turret)
            {
                int tBarWidth = (int)(200 * scaleX);
                int tBarHeight = (int)(16 * scaleY);
                int tBarX = ClientSize.Width / 2 - tBarWidth / 2;
                int tBarY = ClientSize.Height - 160;

                e.Graphics.FillRectangle(Brushes.Gray, tBarX, tBarY, tBarWidth, tBarHeight);
                if (turretCooldown > 0)
                {
                    float fill = 1f - (turretCooldown / turretCooldownTime);
                    e.Graphics.FillRectangle(Brushes.SlateGray, tBarX, tBarY, (int)(tBarWidth * fill), tBarHeight);
                    e.Graphics.DrawString("H: " + (int)turretCooldown + "s", GetFontUI(), barTextBrush, tBarX + 75, tBarY);
                }
                else
                {
                    e.Graphics.FillRectangle(Brushes.SteelBlue, tBarX, tBarY, tBarWidth, tBarHeight);
                    e.Graphics.DrawString("H: READY", GetFontUI(), barTextBrush, tBarX + 68, tBarY);
                }
                e.Graphics.DrawRectangle(borderPen, tBarX, tBarY, tBarWidth, tBarHeight);
            }
            foreach (var t in dashTrail)
            {
                int alpha = (int)(255 * (t.timer / dashTrailDuration));
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(alpha, 100, 0, 255)), t.x, t.y, playerSize, playerSize);
            }

            // World-space juice: floating damage numbers + coin sparkles
            using (var dmgFont = new Font(Ufont, 14f * scaleY, FontStyle.Bold))
            {
                foreach (var d in damageNumbers)
                {
                    float t = Math.Max(0f, d.timer / d.maxTimer);
                    int alpha = (int)(255 * Math.Min(1f, t * 1.5f));
                    using var br = new SolidBrush(Color.FromArgb(alpha, d.color));
                    e.Graphics.DrawString(d.text, dmgFont, br, d.x, d.y);
                }
            }
            foreach (var sp in coinSparkles)
            {
                float t = Math.Max(0f, sp.timer / sp.maxTimer);
                float r = (1f - t) * 8f * scale + 2f;
                int alpha = (int)(220 * t);
                using var br = new SolidBrush(Color.FromArgb(alpha, 255, 240, 120));
                e.Graphics.FillEllipse(br, sp.x - r, sp.y - r, r * 2, r * 2);
            }

            // Restore from screen-shake transform — HUD draws untranslated
            e.Graphics.Restore(__shakeState);

            // Hurt vignette (drawn over world, under HUD) — 4 cheap edge gradients only.
            if (hurtVignette > 0.01f)
            {
                int va = (int)Math.Min(180, hurtVignette * 200f);
                int cw = ClientSize.Width, ch = ClientSize.Height;
                int band = Math.Min(cw, ch) / 5; // edge thickness
                Color edge = Color.FromArgb(va, 180, 0, 0);
                Color clear = Color.FromArgb(0, 180, 0, 0);
                using (var lg = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, 1, band), edge, clear, System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                    e.Graphics.FillRectangle(lg, 0, 0, cw, band);
                using (var lg = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, ch - band, 1, band), clear, edge, System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                    e.Graphics.FillRectangle(lg, 0, ch - band, cw, band);
                using (var lg = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, band, 1), edge, clear, System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                    e.Graphics.FillRectangle(lg, 0, 0, band, ch);
                using (var lg = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(cw - band, 0, band, 1), clear, edge, System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                    e.Graphics.FillRectangle(lg, cw - band, 0, band, ch);
            }

            int hpBarWidth = (int)(200 * scaleX);
            int hpBarHeight = (int)(32 * scaleY);
            int hpBarX = (int)(20 * scaleX);
            int hpBarY = (int)(30 * scaleY);
            e.Graphics.FillRectangle(Brushes.Gray, hpBarX, hpBarY, hpBarWidth, hpBarHeight);
            float nameFontSize = 14f * scaleY;
            Font nameFont = new Font(Ufont, nameFontSize, FontStyle.Bold);
            SizeF nameSize = e.Graphics.MeasureString(playerName, nameFont);
            int nameBoxWidth = Math.Max(hpBarWidth / 3, (int)(nameSize.Width + 10));
            float hpFillReal = health / maxHealth;
            float hpFill = Math.Max(0f, Math.Min(1f, displayedHealth / maxHealth));
            e.Graphics.DrawRectangle(borderPen, hpBarX, hpBarY - 10 * scaleY, hpBarWidth + 10, hpBarHeight + 60 * scaleY);
            e.Graphics.FillRectangle(Brushes.Goldenrod, hpBarX - 5, hpBarY - 25 * scaleY, nameBoxWidth, hpBarHeight + 75 * scaleY);
            e.Graphics.DrawRectangle(borderPen, hpBarX - 5, hpBarY - 25 * scaleY, nameBoxWidth, hpBarHeight + 75 * scaleY);
            e.Graphics.FillRectangle(Brushes.Goldenrod, hpBarX, hpBarY - 10 * scaleY, hpBarWidth + 10, hpBarHeight + (60 * scaleY));
            e.Graphics.FillRectangle(Brushes.DarkRed, hpBarX, hpBarY, hpBarWidth, hpBarHeight / 2);
            // Trailing "lost-health" yellow ghost between real and displayed
            if (displayedHealth > health)
            {
                using var ghost = new SolidBrush(Color.FromArgb(220, 255, 220, 80));
                e.Graphics.FillRectangle(ghost, hpBarX + (int)(hpBarWidth * hpFillReal), hpBarY, (int)(hpBarWidth * (hpFill - hpFillReal)), hpBarHeight / 2);
            }
            e.Graphics.FillRectangle(Brushes.Lime, hpBarX, hpBarY, (int)(hpBarWidth * Math.Min(hpFill, hpFillReal)), hpBarHeight / 2);
            e.Graphics.DrawString(" " + (int)displayedHealth, GetFontUI(), barTextBrush, hpBarX, hpBarY);
            e.Graphics.DrawRectangle(Pens.Black, hpBarX, hpBarY, hpBarWidth, hpBarHeight / 2);
            e.Graphics.DrawString(playerName, nameFont, barTextBrush, hpBarX + 5 / scaleX, hpBarY - 25 * scaleY);

            float mFill = scoreTimer / scoreTimerMax;
            int mnBarWidth = (int)(200 * scaleX);
            int mnBarHeight = (int)(32 * scaleY);
            int mnBarX = (int)(20 * scaleX);
            int mnBarY = (int)(90 * scaleY);
            e.Graphics.FillRectangle(Brushes.Gray, mnBarX, mnBarY, mnBarWidth, mnBarHeight / 2);
            e.Graphics.FillRectangle(Brushes.LightGreen, mnBarX, mnBarY, (int)(hpBarWidth * mFill), mnBarHeight / 2);
            e.Graphics.DrawRectangle(Pens.Black, mnBarX, mnBarY, mnBarWidth, mnBarHeight / 2);

            int ammoBarWidth = (int)(200 * scaleX);
            int ammoBarHeight = (int)(16 * scaleY);
            int ammoBarX = (int)(20 * scaleX);
            int ammoBarY = (int)(50 * scaleY);
            e.Graphics.FillRectangle(Brushes.Gray, ammoBarX, ammoBarY, ammoBarWidth, ammoBarHeight);
            if (reloading)
            {
                float fill = reloadTimer / reloadTime;
                e.Graphics.FillRectangle(Brushes.Gold, ammoBarX, ammoBarY, (int)(ammoBarWidth * fill), ammoBarHeight);
                e.Graphics.DrawString("RELOADING...", GetFontUI(), barTextBrush, ammoBarX + 50, ammoBarY);
            }
            else
            {
                float fill = (float)ammo / maxAmmo;
                e.Graphics.FillRectangle(Brushes.Yellow, ammoBarX, ammoBarY, (int)(ammoBarWidth * fill), ammoBarHeight);
                e.Graphics.DrawString("AMMO: " + ammo, GetFontUI(), barTextBrush, ammoBarX + 65, ammoBarY);
            }
            e.Graphics.DrawRectangle(Pens.Black, ammoBarX, ammoBarY, ammoBarWidth, ammoBarHeight);

            int dashBarWidth = (int)(200 * scaleX);
            int dashBarHeight = (int)(16 * scaleY);
            int dashBarX = (int)(20 * scaleX);
            int dashBarY = (int)(70 * scaleY);
            e.Graphics.FillRectangle(Brushes.Gray, dashBarX, dashBarY, dashBarWidth, dashBarHeight);
            if (dashCooldown > 0)
            {
                float fill = 1f - (dashCooldown / dashCooldownTime);
                e.Graphics.FillRectangle(Brushes.Purple, dashBarX, dashBarY, (int)(dashBarWidth * fill), dashBarHeight);
                e.Graphics.DrawString("DASH: " + dashCooldown.ToString("F1") + "s", GetFontUI(), barTextBrush, dashBarX + 60, dashBarY);
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.MediumPurple, dashBarX, dashBarY, dashBarWidth, dashBarHeight);
                e.Graphics.DrawString("DASH: READY", GetFontUI(), barTextBrush, dashBarX + 55, dashBarY);
            }
            e.Graphics.DrawRectangle(Pens.Black, dashBarX, dashBarY, dashBarWidth, dashBarHeight);

            e.Graphics.DrawString("$: " + (int)displayedScore, GetFontUI(), Brushes.Black, 22 * scaleX, (int)(90 * scaleY));

            // Combo / kill-streak counter (top-right, with pop shake)
            if (comboCount >= 2)
            {
                float fade = 1f - (comboTimer / comboWindow);
                int alpha = (int)(MathF.Min(1f, fade * 2f) * 255);
                // Size scales up with streak, pops on kill
                float popScale = 1f + comboShake * 0.35f;
                float baseSize = 22f + MathF.Min(18f, comboCount * 1.2f);
                using var comboFont = new Font("Segoe UI", baseSize * popScale, FontStyle.Bold);
                string txt = "x" + comboCount + " COMBO";
                var sz = e.Graphics.MeasureString(txt, comboFont);
                float cx = ClientSize.Width - sz.Width - 22 * scaleX;
                float cy = 90 * scaleY;
                // subtle shake
                float sdx = (float)(rng.NextDouble() * 2 - 1) * comboShake * 3f;
                float sdy = (float)(rng.NextDouble() * 2 - 1) * comboShake * 3f;
                // color ramps with streak
                Color cc = comboCount >= 20 ? Color.Magenta
                         : comboCount >= 10 ? Color.OrangeRed
                         : comboCount >= 5 ? Color.Orange
                         : Color.Gold;
                using (var shadow = new SolidBrush(Color.FromArgb((int)(alpha * 0.7f), 0, 0, 0)))
                    e.Graphics.DrawString(txt, comboFont, shadow, cx + 2 + sdx, cy + 2 + sdy);
                using (var br = new SolidBrush(Color.FromArgb(alpha, cc)))
                    e.Graphics.DrawString(txt, comboFont, br, cx + sdx, cy + sdy);
                // timer bar below
                float cbW = sz.Width;
                float cbH = 4f;
                float cbY = cy + sz.Height;
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb((int)(alpha * 0.3f), 0, 0, 0)), cx + sdx, cbY, cbW, cbH);
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(alpha, cc)), cx + sdx, cbY, cbW * fade, cbH);
            }

            if (blink)
            {
                int blinkBarWidth = (int)(200 * scaleX);
                int blinkBarHeight = (int)(16 * scaleY);
                int blinkBarX = ClientSize.Width / 2 - blinkBarWidth / 2;
                int blinkBarY = ClientSize.Height - 100;
                e.Graphics.FillRectangle(Brushes.Gray, blinkBarX, blinkBarY, blinkBarWidth, blinkBarHeight);
                if (blinkCooldown > 0)
                {
                    float fill = 1f - (blinkCooldown / blinkCooldownTime);
                    e.Graphics.FillRectangle(Brushes.MediumPurple, blinkBarX, blinkBarY, (int)(blinkBarWidth * fill), blinkBarHeight);
                    e.Graphics.DrawString("BLINK: " + (int)blinkCooldown + "s", GetFontUI(), barTextBrush, blinkBarX + 55, blinkBarY);
                }
                else
                {
                    e.Graphics.FillRectangle(Brushes.Purple, blinkBarX, blinkBarY, blinkBarWidth, blinkBarHeight);
                    e.Graphics.DrawString("BLINK: READY", GetFontUI(), barTextBrush, blinkBarX + 50, blinkBarY);
                }
                e.Graphics.DrawRectangle(borderPen, blinkBarX, blinkBarY, blinkBarWidth, blinkBarHeight);
            }

            if (speedTrap)
            {
                int stBarWidth = (int)(200 * scaleX);
                int stBarHeight = (int)(16 * scaleY);
                int stBarX = ClientSize.Width / 2 - stBarWidth / 2;
                int stBarY = ClientSize.Height - 140;
                e.Graphics.FillRectangle(Brushes.Gray, stBarX, stBarY, stBarWidth, stBarHeight);
                if (speedTrapActive)
                {
                    float fill = speedTrapTimer / speedTrapDuration;
                    e.Graphics.FillRectangle(Brushes.MediumPurple, stBarX, stBarY, (int)(stBarWidth * fill), stBarHeight);
                    e.Graphics.DrawString("TRAP: " + speedTrapTimer.ToString("F1") + "s", GetFontUI(), barTextBrush, stBarX + 60, stBarY);
                }
                else if (speedTrapCooldown > 0)
                {
                    float fill = 1f - (speedTrapCooldown / speedTrapCooldownTime);
                    e.Graphics.FillRectangle(Brushes.Purple, stBarX, stBarY, (int)(stBarWidth * fill), stBarHeight);
                    e.Graphics.DrawString("G: " + (int)speedTrapCooldown + "s", GetFontUI(), barTextBrush, stBarX + 75, stBarY);
                }
                else
                {
                    e.Graphics.FillRectangle(Brushes.MediumPurple, stBarX, stBarY, stBarWidth, stBarHeight);
                    e.Graphics.DrawString("G: READY", GetFontUI(), barTextBrush, stBarX + 68, stBarY);
                }
                e.Graphics.DrawRectangle(borderPen, stBarX, stBarY, stBarWidth, stBarHeight);
            }

            if (decoy)
            {
                int decoyBarWidth = (int)(200 * scaleX);
                int decoyBarHeight = (int)(16 * scaleY);
                int decoyBarX = ClientSize.Width / 2 - decoyBarWidth / 2;
                int decoyBarY = ClientSize.Height - 120;
                e.Graphics.FillRectangle(Brushes.Gray, decoyBarX, decoyBarY, decoyBarWidth, decoyBarHeight);
                if (decoyActive)
                {
                    float fill = decoyTimer / decoyDuration;
                    e.Graphics.FillRectangle(Brushes.LimeGreen, decoyBarX, decoyBarY, (int)(decoyBarWidth * fill), decoyBarHeight);
                    e.Graphics.DrawString("DECOY: " + decoyTimer.ToString("F1") + "s", GetFontUI(), barTextBrush, decoyBarX + 55, decoyBarY);
                }
                else if (decoyCooldown > 0)
                {
                    float fill = 1f - (decoyCooldown / decoyCooldownTime);
                    e.Graphics.FillRectangle(Brushes.DarkGreen, decoyBarX, decoyBarY, (int)(decoyBarWidth * fill), decoyBarHeight);
                    e.Graphics.DrawString("F: " + (int)decoyCooldown + "s", GetFontUI(), barTextBrush, decoyBarX + 75, decoyBarY);
                }
                else
                {
                    e.Graphics.FillRectangle(Brushes.LimeGreen, decoyBarX, decoyBarY, decoyBarWidth, decoyBarHeight);
                    e.Graphics.DrawString("F: READY", GetFontUI(), barTextBrush, decoyBarX + 68, decoyBarY);
                }
                e.Graphics.DrawRectangle(borderPen, decoyBarX, decoyBarY, decoyBarWidth, decoyBarHeight);
            }

            // Sandbox Indicator
            if (sandboxMode)
            {
                e.Graphics.DrawString("SANDBOX", new Font("Arial", 10 * scaleY, FontStyle.Bold),
                    textBrush, 22 * scaleX, (int)(110 * scaleY));
            }

            if (buffMessageTimer > 0)
            {
                float alpha = Math.Min(1f, buffMessageTimer / buffMessageDuration);
                int a = (int)(alpha * 255);
                e.Graphics.DrawString(buffMessage, new Font("Segoe UI Emoji", 22 * scaleY, FontStyle.Bold),
                    new SolidBrush(Color.FromArgb(a, darkMode ? Color.White : Color.Black)),
                    ClientSize.Width / 2 - 200, ClientSize.Height / 2 - 60);
            }
            int dc = Math.Clamp(difficulty, 0, 8);
            e.Graphics.DrawString(DifficultyNames[dc].ToUpper(),
                new Font("Arial", 10 * scaleY, FontStyle.Bold),
                new SolidBrush(DifficultyColors[dc]),
                22 * scaleX, (int)(105 * scaleY));

            if (lastStand && (health <= 15f || (isMultiplayer && p2Health <= 15f)))
            {
                float pulse = (float)Math.Abs(Math.Sin(gameStartTimer * 5f));
                int a = (int)(pulse * 180);
                e.Graphics.DrawRectangle(
                    new Pen(Color.FromArgb(a, 255, 50, 50), 4),
                    2, 2, ClientSize.Width - 4, ClientSize.Height - 4);
            }

            // Achievement toast
            if (achievementToastTimer > 0)
            {
                float progress = achievementToastTimer / achievementToastDuration;
                float slideIn = Math.Min(1f, (achievementToastDuration - achievementToastTimer) / 0.3f);
                float fadeOut = Math.Min(1f, achievementToastTimer / 0.5f);
                float alpha = Math.Min(slideIn, fadeOut);
                int toastW = (int)(320 * scaleX);
                int toastH = (int)(60 * scaleY);
                int toastX = ClientSize.Width - toastW - (int)(20 * scaleX);
                int targetY = (int)(20 * scaleY);
                int toastY = targetY - (int)((1f - slideIn) * 80 * scaleY);
                int a = (int)(alpha * 230);
                int aText = (int)(alpha * 255);

                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(a, 20, 20, 35)),
                    toastX, toastY, toastW, toastH);
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(a, 255, 200, 0)),
                    toastX, toastY, 5, toastH);
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(a, 255, 200, 0)),
                    toastX, toastY, toastW, toastH);

                e.Graphics.DrawString(achievementToastIcon,
                    new Font("Segoe UI Emoji", 18 * scaleY),
                    new SolidBrush(Color.FromArgb(aText, Color.White)),
                    toastX + 10 * scaleX, toastY + 8 * scaleY);
                e.Graphics.DrawString("ACHIEVEMENT UNLOCKED",
                    new Font("Arial", 8 * scaleY),
                    new SolidBrush(Color.FromArgb(aText, 255, 200, 0)),
                    toastX + 50 * scaleX, toastY + 8 * scaleY);
                e.Graphics.DrawString(achievementToastText,
                    new Font("Arial", 12 * scaleY, FontStyle.Bold),
                    new SolidBrush(Color.FromArgb(aText, Color.White)),
                    toastX + 50 * scaleX, toastY + 28 * scaleY);
            }

            // Pause blur — overlay cached blurred snapshot on top of world+HUD when pause menu is up.
            if (isPaused && pauseBlurFrame != null && (pauseResumeBtn != null || pauseQuitBtn != null))
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                e.Graphics.DrawImage(pauseBlurFrame, 0, 0, ClientSize.Width, ClientSize.Height);
                using var dim = new SolidBrush(Color.FromArgb(110, 0, 0, 0));
                e.Graphics.FillRectangle(dim, 0, 0, ClientSize.Width, ClientSize.Height);
                using var titleFont = new Font("Arial", 36 * scaleY, FontStyle.Bold);
                e.Graphics.DrawString("PAUSED", titleFont, Brushes.White,
                    new RectangleF(0, ClientSize.Height / 2f - 100 * scaleY, ClientSize.Width, 60 * scaleY),
                    new StringFormat { Alignment = StringAlignment.Center });
            }

            // Custom aim reticle — drawn on top of everything (still in-game only).
            bool showReticle = !onMainMenu && !hostDead && (!isPaused || activeUpgradePanel != null);
            if (showReticle && activeUpgradePanel == null)
            {
                // When the upgrade panel is open, the reticle is drawn by the panel's own
                // Paint handler (since the panel occludes the form).
                DrawAimReticle(e.Graphics, mousePos.X, mousePos.Y);
            }
            // Hide the OS cursor whenever the crosshair is visible.
            if (showReticle && !systemCursorHidden)
            {
                Cursor.Hide();
                systemCursorHidden = true;
            }
            else if (!showReticle && systemCursorHidden)
            {
                Cursor.Show();
                systemCursorHidden = false;
            }
        }

        private bool systemCursorHidden = false;

        private void DrawAimReticle(Graphics g, float mx, float my)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // pulse based on shoot cooldown / reload state
            float pulse = (float)Math.Sin(Environment.TickCount / 180.0) * 0.5f + 0.5f;
            Color ringColor = reloading ? Color.FromArgb(220, 255, 200, 60)
                            : ammo <= 0 ? Color.FromArgb(220, 255, 80, 80)
                            : Color.FromArgb(220, 240, 240, 240);
            float baseR = 14f * scale;
            float r = baseR + pulse * 2f * scale;
            using (var outer = new Pen(ringColor, 2.0f * scale))
                g.DrawEllipse(outer, mx - r, my - r, r * 2, r * 2);
            // crosshair lines with a center gap
            using (var line = new Pen(ringColor, 1.6f * scale))
            {
                float gap = 5f * scale, len = 8f * scale;
                g.DrawLine(line, mx - gap - len, my, mx - gap, my);
                g.DrawLine(line, mx + gap, my, mx + gap + len, my);
                g.DrawLine(line, mx, my - gap - len, mx, my - gap);
                g.DrawLine(line, mx, my + gap, mx, my + gap + len);
            }
            // center dot
            using (var dot = new SolidBrush(Color.FromArgb(255, ringColor)))
                g.FillEllipse(dot, mx - 1.5f * scale, my - 1.5f * scale, 3f * scale, 3f * scale);
        }

        private void Form1_KeyUp(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W: velocityY = IsKeyDown(Keys.S) ? speed : 0f; break;
                case Keys.S: velocityY = IsKeyDown(Keys.W) ? -speed : 0f; break;
                case Keys.A: velocityX = IsKeyDown(Keys.D) ? speed : 0f; break;
                case Keys.D: velocityX = IsKeyDown(Keys.A) ? -speed : 0f; break;
            }
        }

        private bool IsKeyDown(Keys key) => (GetKeyState((int)key) & 0x8000) != 0;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetKeyState(int keyCode);

        private System.Drawing.Point mousePos = System.Drawing.Point.Empty;

        private void Form1_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                mouseHeld = true;
            if (showingUnlockAnimation)
            {
                if (unlockAnimTimer < unlockAnimDuration - 1f)
                {
                    showingUnlockAnimation = false;
                    unlockParticles.Clear();
                    unlockAnimTimer = 0f;
                }
                return;
            }
            else if (e.Button == MouseButtons.Right && blink)
            {
                if (isMultiplayer && !isNetHost)
                {
                    p2PendingBlink = true;
                }
                else if (blinkCooldown <= 0)
                {
                    posX = Math.Max(0, Math.Min(mousePos.X - boxSize / 2, ClientSize.Width - boxSize));
                    posY = Math.Max(0, Math.Min(mousePos.Y - boxSize / 2, ClientSize.Height - boxSize));
                    blinkCooldown = blinkCooldownTime;
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                inspectedEnemyIndex = -1;
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (!enemyAlive[i]) continue;
                    bool isTank = i < enemyIsTank.Count && enemyIsTank[i];
                    bool canShoot = i < enemyCanShoot.Count && enemyCanShoot[i];
                    bool isRunner = i < enemyIsRunner.Count && enemyIsRunner[i];
                    int eSize = isTank ? boxSize + 20 : canShoot ? boxSize + 8 : isRunner ? boxSize - 8 : boxSize;

                    if (mousePos.X > enemies[i].x && mousePos.X < enemies[i].x + eSize &&
                        mousePos.Y > enemies[i].y && mousePos.Y < enemies[i].y + eSize)
                    {
                        inspectedEnemyIndex = i;
                        enemyInspectTimer = enemyInspectDuration;
                        return;
                    }
                }
            }
        }

        private void Form1_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) mouseHeld = false;
        }

        private void Form1_MouseMove(object? sender, MouseEventArgs e) => mousePos = e.Location;

        private void Shoot()
        {
            totalBulletsShot++;
            float pcx = posX + playerSize / 2f;
            float pcy = posY + playerSize / 2f;
            float angle = GetClampedAimAngle(pcx, pcy, playerSize);
            float velX = MathF.Cos(angle) * bulletSpeed;
            float velY = MathF.Sin(angle) * bulletSpeed;
            var (bx, by) = GetGunTipWorldAtAngle(pcx, pcy, playerSize, angle);
            muzzleFlashes.Add((bx, by, angle, 0.08f, 0.08f));
            if (doubleTap)
            {
                doubleTapCounter++;
                if (doubleTapCounter >= 5)
                {
                    doubleTapCounter = 0;
                    float spread = 0.3f;
                    bullets.Add((bx, by, velX, velY, 0));
                    bullets.Add((bx, by,
                        velX * MathF.Cos(spread) - velY * MathF.Sin(spread),
                        velX * MathF.Sin(spread) + velY * MathF.Cos(spread), 0));
                    bullets.Add((bx, by,
                        velX * MathF.Cos(-spread) - velY * MathF.Sin(-spread),
                        velX * MathF.Sin(-spread) + velY * MathF.Cos(-spread), 0));
                    return;
                }
            }
            bullets.Add((bx, by, velX, velY, 0));
        }

        private void LoadRedEnemySprite() => redEnemySpriteCropped = LoadSpriteWithCornerBgRemoval("red_guy.png");
        private void LoadParasiticEnemySprite() => redParasiticSpriteCropped = LoadSpriteWithCornerBgRemoval("red_guy_parasitic.png");
        private void LoadGunnerSprite() => gunnerSpriteCropped = LoadSpriteWithCornerBgRemoval("red_guy_gunner.png");
        private void LoadGunnerParasiticSprite() => gunnerParasiticSpriteCropped = LoadSpriteWithCornerBgRemoval("red_guy_gunner_parasitic.png");
        private void LoadTankSprite() => tankSpriteCropped = LoadSpriteWithCornerBgRemoval("red_guy_tank.png");
        private void LoadTankParasiticSprite() => tankParasiticSpriteCropped = LoadSpriteWithCornerBgRemoval("red_guy_tank_parasitic.png");
        private void LoadRunnerSprite() => runnerSpriteCropped = LoadSpriteWithCornerBgRemoval("red_guy_runner.png");
        private void LoadRunnerParasiticSprite() => runnerParasiticSpriteCropped = LoadSpriteWithCornerBgRemoval("red_guy_runner_parasitic.png");
        private void LoadBossSprite() => bossSpriteCropped = LoadSpriteWithCornerBgRemoval("boss.png");
        private void LoadCardSprite()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "card.png");
                if (File.Exists(path)) cardSpriteBitmap = new Bitmap(path);
            }
            catch { }
        }

        private Bitmap? LoadSpriteWithCornerBgRemoval(string fileName)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName);
                if (!File.Exists(path)) return null;
                using var raw = new Bitmap(path);
                // Sample top-left as background reference and strip matching pixels. Handles
                // light-blue, saturated-blue, near-white, or any uniform background.
                RemoveBackgroundByCorner(raw, tolerance: 60);
                Rectangle bounds = GetSpriteBounds(raw);
                if (bounds.IsEmpty) return null;
                return raw.Clone(bounds, raw.PixelFormat);
            }
            catch { return null; }
        }

        // Strips background by sampling the top-left pixel and clearing any pixel within
        // `tolerance` (per-channel Manhattan distance) of that reference color.
        private static void RemoveBackgroundByCorner(Bitmap bmp, int tolerance)
        {
            // Ensure we can read/write RGBA bytes directly.
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect,
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int stride = Math.Abs(data.Stride);
            byte[] px = new byte[stride * bmp.Height];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, px, 0, px.Length);
            // Reference = top-left pixel (BGRA order)
            byte refB = px[0], refG = px[1], refR = px[2];
            for (int y = 0; y < bmp.Height; y++)
            {
                int row = y * stride;
                for (int x = 0; x < bmp.Width; x++)
                {
                    int o = row + x * 4;
                    int db = Math.Abs(px[o]     - refB);
                    int dg = Math.Abs(px[o + 1] - refG);
                    int dr = Math.Abs(px[o + 2] - refR);
                    if (db <= tolerance && dg <= tolerance && dr <= tolerance)
                        px[o + 3] = 0; // clear alpha
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(px, 0, data.Scan0, px.Length);
            bmp.UnlockBits(data);
        }

        private void DrawEnemySprite(Graphics g, Bitmap sprite, float cx, float cy, int size, float aimAngle, int alpha = 255, float extraScale = 1f)
        {
            float rot = aimAngle - EnemySpriteBaseAngle;
            float drawH = size * EnemySpriteDrawScale * extraScale;
            float drawW = drawH * sprite.Width / (float)sprite.Height;
            float offX = -EnemySpriteBodyCenterFracX * drawW;
            float offY = -EnemySpriteBodyCenterFracY * drawH;
            var saved = g.Transform;
            g.TranslateTransform(cx, cy);
            g.RotateTransform((float)(rot * 180f / Math.PI));
            if (alpha < 255)
            {
                using var ia = new System.Drawing.Imaging.ImageAttributes();
                var cm = new System.Drawing.Imaging.ColorMatrix { Matrix33 = alpha / 255f };
                ia.SetColorMatrix(cm);
                g.DrawImage(sprite,
                    new Rectangle((int)offX, (int)offY, (int)drawW, (int)drawH),
                    0, 0, sprite.Width, sprite.Height,
                    GraphicsUnit.Pixel, ia);
            }
            else
            {
                g.DrawImage(sprite, offX, offY, drawW, drawH);
            }
            g.Transform = saved;
        }

        private void LoadPlayerSprite()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "player_sprite.png");
                if (!File.Exists(path)) return;
                using var raw = new Bitmap(path);
                raw.MakeTransparent(Color.White);
                Rectangle bounds = GetSpriteBounds(raw);
                if (bounds.IsEmpty) return;
                playerSpriteCropped = raw.Clone(bounds, raw.PixelFormat);
            }
            catch { }
        }

        private static Rectangle GetSpriteBounds(Bitmap bmp)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] px = new byte[Math.Abs(data.Stride) * bmp.Height];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, px, 0, px.Length);
            bmp.UnlockBits(data);
            int stride = Math.Abs(data.Stride);
            int minX = bmp.Width, maxX = 0, minY = bmp.Height, maxY = 0;
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                    if (px[y * stride + x * 4 + 3] > 20)
                    {
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }
            return minX > maxX ? Rectangle.Empty : new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        // Returns gun tip world position for a given explicit aim angle (wall-clamp-aware callers use this)
        private (float x, float y) GetGunTipWorldAtAngle(float cx, float cy, int pSize, float aimAngle)
        {
            if (playerSpriteCropped == null) return (cx, cy);
            float rot = aimAngle - SpriteBaseAngle;
            float drawH = pSize * SpriteDrawScale;
            float drawW = drawH * playerSpriteCropped.Width / (float)playerSpriteCropped.Height;
            float dx = (SpriteGunTipFracX - SpriteBodyCenterFracX) * drawW;
            float dy = (SpriteGunTipFracY - SpriteBodyCenterFracY) * drawH;
            float cosR = MathF.Cos(rot), sinR = MathF.Sin(rot);
            return (cx + dx * cosR - dy * sinR, cy + dx * sinR + dy * cosR);
        }

        // Returns gun tip world position using wall-clamped aim angle from current mousePos
        private (float x, float y) GetGunTipWorld(float cx, float cy, int pSize)
        {
            float aimAngle = GetClampedAimAngle(cx, cy, pSize);
            return GetGunTipWorldAtAngle(cx, cy, pSize, aimAngle);
        }

        // Returns a colorized copy of the player sprite tinted to playerColor. Cached and regenerated
        // only when playerColor changes, so the per-pixel pass runs at most once per color pick.
        private Bitmap? GetTintedPlayerSprite()
        {
            if (playerSpriteCropped == null) return null;
            if (_tintedPlayerSprite != null && _tintedForColor.ToArgb() == playerColor.ToArgb())
                return _tintedPlayerSprite;
            _tintedPlayerSprite?.Dispose();
            _tintedPlayerSprite = ColorizeBitmap(playerSpriteCropped, playerColor);
            _tintedForColor = playerColor;
            return _tintedPlayerSprite;
        }

        // Colorizes by replacing each colored pixel's hue + saturation with the target's,
        // while preserving its original lightness (so shading/highlights stay consistent).
        // Near-black outlines are left untouched so the sprite keeps its ink border.
        // Rotate hue by deltaHue degrees, preserve saturation and lightness.
        // Used to retint the (red) card.png to match each upgrade category.
        private static Bitmap HueShiftBitmap(Bitmap src, float deltaHue)
        {
            var copy = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(copy)) g.DrawImage(src, 0, 0, src.Width, src.Height);
            var rect = new Rectangle(0, 0, copy.Width, copy.Height);
            var data = copy.LockBits(rect,
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int stride = Math.Abs(data.Stride);
            byte[] px = new byte[stride * copy.Height];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, px, 0, px.Length);
            for (int y = 0; y < copy.Height; y++)
            {
                int row = y * stride;
                for (int x = 0; x < copy.Width; x++)
                {
                    int o = row + x * 4;
                    if (px[o + 3] == 0) continue;
                    byte bC = px[o], gC = px[o + 1], rC = px[o + 2];
                    RgbToHsl(rC, gC, bC, out float h, out float s, out float l);
                    if (s < 0.04f) continue; // skip neutral grays so outlines/highlights stay clean
                    h = (h + deltaHue) % 360f; if (h < 0) h += 360f;
                    HslToRgb(h, s, l, out byte r2, out byte g2, out byte b2);
                    px[o] = b2; px[o + 1] = g2; px[o + 2] = r2;
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(px, 0, data.Scan0, px.Length);
            copy.UnlockBits(data);
            return copy;
        }

        private Bitmap? GetTintedCardForCategory(string category)
        {
            if (cardSpriteBitmap == null) return null;
            if (tintedCardCache.TryGetValue(category, out var cached)) return cached;
            const float SourceHue = 0f; // card.png is red
            float targetHue = GetCategoryColor(category).GetHue();
            float delta = targetHue - SourceHue;
            // No-op for offensive (already red): just return the source bitmap.
            if (MathF.Abs(((delta + 540f) % 360f) - 180f) < 0.5f || MathF.Abs(delta) < 0.5f)
            {
                tintedCardCache[category] = cardSpriteBitmap;
                return cardSpriteBitmap;
            }
            var tinted = HueShiftBitmap(cardSpriteBitmap, delta);
            tintedCardCache[category] = tinted;
            return tinted;
        }

        private static Bitmap ColorizeBitmap(Bitmap src, Color target)
        {
            var copy = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(copy)) g.DrawImage(src, 0, 0, src.Width, src.Height);

            float tH = target.GetHue();         // 0..360 (returns 0 for grayscale targets)
            float tS = target.GetSaturation();  // 0..1

            var rect = new Rectangle(0, 0, copy.Width, copy.Height);
            var data = copy.LockBits(rect,
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int stride = Math.Abs(data.Stride);
            byte[] px = new byte[stride * copy.Height];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, px, 0, px.Length);
            for (int y = 0; y < copy.Height; y++)
            {
                int row = y * stride;
                for (int x = 0; x < copy.Width; x++)
                {
                    int o = row + x * 4;
                    byte a = px[o + 3];
                    if (a == 0) continue; // transparent — skip
                    byte bC = px[o], gC = px[o + 1], rC = px[o + 2];
                    RgbToHsl(rC, gC, bC, out float h, out float s, out float l);
                    // Preserve near-black outlines (keeps crisp border on any tint).
                    if (l < 0.12f) continue;
                    HslToRgb(tH, tS, l, out byte r2, out byte g2, out byte b2);
                    px[o]     = b2;
                    px[o + 1] = g2;
                    px[o + 2] = r2;
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(px, 0, data.Scan0, px.Length);
            copy.UnlockBits(data);
            return copy;
        }

        private static void RgbToHsl(byte r, byte g, byte b, out float h, out float s, out float l)
        {
            float rf = r / 255f, gf = g / 255f, bf = b / 255f;
            float max = Math.Max(rf, Math.Max(gf, bf));
            float min = Math.Min(rf, Math.Min(gf, bf));
            l = (max + min) / 2f;
            float d = max - min;
            if (d < 1e-6f) { h = 0f; s = 0f; return; }
            s = l > 0.5f ? d / (2f - max - min) : d / (max + min);
            if (max == rf)      h = (gf - bf) / d + (gf < bf ? 6f : 0f);
            else if (max == gf) h = (bf - rf) / d + 2f;
            else                h = (rf - gf) / d + 4f;
            h *= 60f;
        }

        private static void HslToRgb(float h, float s, float l, out byte r, out byte g, out byte b)
        {
            if (s < 1e-6f) { byte v = (byte)Math.Round(l * 255f); r = g = b = v; return; }
            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;
            float hk = h / 360f;
            r = (byte)Math.Round(HueToChannel(p, q, hk + 1f / 3f) * 255f);
            g = (byte)Math.Round(HueToChannel(p, q, hk) * 255f);
            b = (byte)Math.Round(HueToChannel(p, q, hk - 1f / 3f) * 255f);
        }

        private static float HueToChannel(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }

        private void DrawPlayerSprite(Graphics g, float cx, float cy, int pSize, float aimAngle, int alpha = 255)
        {
            var sprite = GetTintedPlayerSprite();
            if (sprite == null) return;
            float rot = aimAngle - SpriteBaseAngle;
            float drawH = pSize * SpriteDrawScale;
            float drawW = drawH * sprite.Width / (float)sprite.Height;
            float offX = -SpriteBodyCenterFracX * drawW;
            float offY = -SpriteBodyCenterFracY * drawH;
            var saved = g.Transform;
            g.TranslateTransform(cx, cy);
            g.RotateTransform((float)(rot * 180f / Math.PI));
            if (alpha < 255)
            {
                using var ia = new System.Drawing.Imaging.ImageAttributes();
                var cm = new System.Drawing.Imaging.ColorMatrix { Matrix33 = alpha / 255f };
                ia.SetColorMatrix(cm);
                g.DrawImage(sprite,
                    new Rectangle((int)offX, (int)offY, (int)drawW, (int)drawH),
                    0, 0, sprite.Width, sprite.Height,
                    GraphicsUnit.Pixel, ia);
            }
            else
            {
                g.DrawImage(sprite, offX, offY, drawW, drawH);
            }
            g.Transform = saved;
        }

        private void ResetGame()
        {
            ResetEnemies();
            bossSpawnInterval_Current = 120f;
            parasites = new List<(float x, float y, float velX, float velY, float timer, float spawnDelay, float hitCooldown)>();
            enemyRespawnTimers = new List<float>(new float[enemies.Count]);
            enemyAlive = new List<bool>(new bool[enemies.Count].Select(b => true).ToArray());
            posX = ClientSize.Width / 2;
            posY = ClientSize.Height / 2;
            velocityX = 0f; velocityY = 0f;
            isDashing = false; dashTimer = 0f; dashVelX = 0f; dashVelY = 0f;
            bullets.Clear();
            score = 0;
            coins = new List<(float x, float y, float velX, float velY)>();
            scoreTimer = 0f; shootCooldown = 0f; gameStartTimer = 0f; bulletSpeed = (15f + permBulletSpeedLevel * 0.5f) * scale;
            superActive = false; superTimer = 0f; superCooldown = 0f; superCooldownTime = 90f;
            mouseHeld = false; wallActive = false; wallTimer = 0f; wallCooldown = 0f;
            maxAmmo = 60; ammo = maxAmmo; reloading = false; reloadTimer = 0f;
            maxHealth = 50f; health = maxHealth; hitCooldown = 0f; regenTimer = 0f;
            dashCooldown = 0f; lifeSteal = 0; fireRateBonus = 0f; scorePerSecond = 1;
            reloadTime = 3f; dashDuration = 0.7f; wallDuration = 20f;
            ghostDash = false; ricochetBounces = 0; smartBounce = false;
            bulletSize = (int)(6 * scale); coinSize = (int)(6 * scale);
            enemyBulletSize = (int)(10 * scale); enemyBulletSpeed = 12f * scale;
            bossBulletSpeed = 40f * scale;
            afterburn = false; isAfterburn = false; afterburnTimer = 0f;
            blink = false; blinkCooldown = 0f; jackpot = false;
            speed = (4.8f + permSpeedLevel * 0.2f) * scale; playerSize = (int)(30 * scale);
            piercingBullets = false; dashTrail.Clear(); scoreTimerMax = 1f;
            enemyBuffTimer = 0f;
            boxSize = (int)(30 * scale); enemyReinforceChance = 0f;
            buffMessage = ""; buffMessageTimer = 0f;
            decoy = false; decoyActive = false; decoyTimer = 0f; decoyCooldown = 0f;
            homing = false; blowback = 0; totalKills = 0; timeAlive = 0f;
            totalUpgradesPurchased = 0; totalCoinsCollected = 0; totalBulletsShot = 0;
            coinWorth = 10; totalScore = 0f; enemyBullets.Clear();
            shootingEnemyChance = 0.05f; toughLove = false; orbitCount = 0; orbitAngle = 0f;
            purchasedOneTimeUpgrades.Clear();
            dashPower = 12 * scale; wallLength = 240f * scale;
            tankEnemyChance = 0.05f;
            boxWall = false; boxWalls.Clear();
            hostDead = false; p2Dead = false;
            p2PendingUpgrade = -1; p2PendingSuper = false; p2PendingWall = false;
            p2Dashing = false; p2DashTimer = 0f; p2DashVelX = 0f; p2DashVelY = 0f; p2DashCooldown = 0f;
            p2ShootCooldown = 0f; p2DoubleTapCounter = 0; p2HitCooldown = 0f;
            p2BlinkCooldown = 0f; p2TurretCooldown = 0f;
            p2PendingBlink = false; p2PendingTurret = false;
            p2PendingDecoy = false; p2PendingSpeedTrap = false;
            p2Ammo = 60; p2MaxAmmo = 60; p2Reloading = false; p2ReloadTimer = 0f;
            runnerEnemyChance = 0.05f;
            medic = false; doubleTap = false; doubleTapCounter = 0;
            explosiveFinish = false; nextBulletIsLast = false;
            flameWall = false; shrapnel = false;
            speedTrap = false; speedTrapActive = false; speedTrapTimer = 0f; speedTrapCooldown = 0f;
            thorns = false; cashback = false; cashbackTimer = 0f; cashbackAmount = 0f;
            totalSpentSinceLastCashback = 0f;
            orbitalStrike = false; orbitalSlowedEnemies.Clear(); orbitalSlowTimers.Clear();
            orbitRadiusBonus = 0f;
            bossAlive = false; bossHealth = bossMaxHealth; bossSpawnTimer = 0f; bossHitCooldown = 0f;
            enemySpawnTimer = 0f;
            bossShootTimer = 0f;
            bossBulletHitCooldown = 0f;
            bossOrbitHitCooldown = 0f;
            bossFlameTimer = 0f;
            bossesDefeated = 0;
            gameStartTimer = 0f;
            turret = false;
            turrets.Clear();
            turretShootTimers.Clear();
            turretCooldown = 0f;
            rapidReload = false;
            explosiveOrbit = false;
            enemyInspectText = "";
            enemyInspectTimer = 0f;
            inspectedEnemyIndex = -1;
            deathFlashes.Clear();
            hitFlashes.Clear();
            bulletTrails.Clear();
            muzzleFlashes.Clear();
            deathFragments.Clear();
            comboCount = 0;
            comboTimer = 0f;
            comboShake = 0f;
            parasites.Clear();
            parasiteDecayKill = false;
            ricochetExplosion = false;
            bloodMoney = false;
            parasiteImmune = false;
            HidePauseButtons();
            unlockParticles.Clear();
            unlockAnimTimer = 0f;
            pendingUnlockAnimation = -1;
            shootRate = 10f / 60f;
            currentBossMaxHealth = 100f;
            currentBossShootRate = 2f;
            ApplyDifficulty();
        }

        private void ResetEnemies()
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            enemies = new List<(float x, float y)>
            {
                (0f, 0f), (0f, h / 2f), (0f, h), (w / 2f, 0f), (w, 0f),
                (w, h / 2f), (w, h), (w / 2f, h), (w / 4f, 0f), (w * 3f / 4f, 0f),
                (0f, h / 4f), (0f, h * 3f / 4f), (w, h / 4f), (w, h * 3f / 4f),
                (w / 4f, h), (w * 3f / 4f, h),
            };
            enemyAlive = Enumerable.Repeat(true, enemies.Count).ToList();
            enemyRespawnTimers = new List<float>(new float[enemies.Count]);
            enemySmackCooldown = new List<float>(new float[enemies.Count]);
            enemyAimAngle      = new List<float>(new float[enemies.Count]);
            enemyCanShoot = enemies.Select(_ => rng.NextDouble() < shootingEnemyChance).ToList();
            enemyIsTank = enemies.Select((_, idx) => !enemyCanShoot[idx] && rng.NextDouble() < tankEnemyChance).ToList();
            enemyIsRunner = enemies.Select((_, idx) => !enemyCanShoot[idx] && !enemyIsTank[idx] && rng.NextDouble() < runnerEnemyChance).ToList();
            enemyShootTimers = new List<float>(new float[enemies.Count]);
            enemyFlameTimers = new List<float>(new float[enemies.Count]);
            enemyIsParasitic = enemies.Select(_ => rng.NextDouble() < parasiticEnemyChance).ToList();
            parasites = new List<(float x, float y, float velX, float velY, float timer, float spawnDelay, float hitCooldown)>();
            parasites.Clear();
            enemyIsFrenzied = new List<bool>(new bool[enemies.Count]);
            enemyIsPhasing = new List<bool>(new bool[enemies.Count]);
            enemyIsZigzag = new List<bool>(new bool[enemies.Count]);
            enemyIsCharging = new List<bool>(new bool[enemies.Count]);
            enemyIsArmored = new List<bool>(new bool[enemies.Count]);
            enemyIsRegenerating = new List<bool>(new bool[enemies.Count]);
            enemyIsReflective = new List<bool>(new bool[enemies.Count]);
            enemyIsBerserker = new List<bool>(new bool[enemies.Count]);
            enemyIsCorrupted = new List<bool>(new bool[enemies.Count]);
            enemyArmorBroken = new List<bool>(new bool[enemies.Count]);
            enemyChargeCooldown = new List<float>(new float[enemies.Count]);
            enemyChargeTimer = new List<float>(new float[enemies.Count]);
            enemyIsCharging_Active = new List<bool>(new bool[enemies.Count]);
            enemyChargeVelX = new List<float>(new float[enemies.Count]);
            enemyChargeVelY = new List<float>(new float[enemies.Count]);
            enemyFrenziedAngle = new List<float>(new float[enemies.Count]);
            enemyZigzagTimer = new List<float>(new float[enemies.Count]);
            enemyZigzagDirection = Enumerable.Repeat(1f, enemies.Count).ToList();
            enemyPhasingTimer = new List<float>(new float[enemies.Count]);
            enemyIsVisible = Enumerable.Repeat(true, enemies.Count).ToList();

            for (int i = 0; i < enemies.Count; i++)
                InitEnemyEffects(i);
            SyncEnemyLists();
            enemyHealth = enemies.Select((_, idx) =>
            {
                if (enemyIsTank[idx]) return 8f;
                if (enemyCanShoot[idx]) return 4f;
                if (enemyIsRunner[idx]) return 1f;
                return 2f;
            }).ToList();
        }

        private bool CollidesWithWall(float x, float y, float size)
        {
            foreach (var w in walls)
                if (x < w.x + w.width && x + size > w.x && y < w.y + w.height && y + size > w.y)
                    return true;
            foreach (var bw in boxWalls)
                if (wallActive && CollidesWithRotatedWall(x, y, size, bw.x, bw.y, bw.width, bw.height, bw.angle))
                    return true;
            if (wallActive)
                if (CollidesWithRotatedWall(x, y, size, tempWall.x, tempWall.y, tempWall.width, tempWall.height, tempWall.angle))
                    return true;
            return false;
        }

        private bool CollidesWithRotatedWall(float x, float y, float size, float wx, float wy, float ww, float wh, float angle)
        {
            float cx = x + size / 2 - wx;
            float cy = y + size / 2 - wy;
            float cos = (float)Math.Cos(-angle);
            float sin = (float)Math.Sin(-angle);
            float lx = cos * cx - sin * cy;
            float ly = sin * cx + cos * cy;
            return Math.Abs(lx) < (ww / 2 + size / 2) && Math.Abs(ly) < (wh / 2 + size / 2);
        }

        private (float x, float y) PushOutOfWalls(float x, float y, float size)
        {
            foreach (var w in walls)
            {
                if (x < w.x + w.width && x + size > w.x && y < w.y + w.height && y + size > w.y)
                {
                    float overlapLeft = (x + size) - w.x;
                    float overlapRight = (w.x + w.width) - x;
                    float overlapTop = (y + size) - w.y;
                    float overlapBottom = (w.y + w.height) - y;
                    float minOverlap = Math.Min(Math.Min(overlapLeft, overlapRight), Math.Min(overlapTop, overlapBottom));
                    if (minOverlap == overlapLeft) x -= overlapLeft;
                    else if (minOverlap == overlapRight) x += overlapRight;
                    else if (minOverlap == overlapTop) y -= overlapTop;
                    else y += overlapBottom;
                }
            }
            if (wallActive)
                (x, y) = PushOutOfRotatedWall(x, y, size, tempWall.x, tempWall.y, tempWall.width, tempWall.height, tempWall.angle);
            foreach (var bw in boxWalls)
                if (wallActive)
                    (x, y) = PushOutOfRotatedWall(x, y, size, bw.x, bw.y, bw.width, bw.height, bw.angle);
            return (x, y);
        }

        private (float x, float y) PushOutOfRotatedWall(float x, float y, float size, float wx, float wy, float ww, float wh, float angle)
        {
            float cx = x + size / 2 - wx;
            float cy = y + size / 2 - wy;
            float cos = (float)Math.Cos(-angle);
            float sin = (float)Math.Sin(-angle);
            float lx = cos * cx - sin * cy;
            float ly = sin * cx + cos * cy;
            float halfW = ww / 2 + size / 2;
            float halfH = wh / 2 + size / 2;
            if (Math.Abs(lx) < halfW && Math.Abs(ly) < halfH)
            {
                float overlapX = halfW - Math.Abs(lx);
                float overlapY = halfH - Math.Abs(ly);
                float pushLX, pushLY;
                if (overlapX < overlapY) { pushLX = overlapX * Math.Sign(lx); pushLY = 0; }
                else { pushLX = 0; pushLY = overlapY * Math.Sign(ly); }
                float cosInv = (float)Math.Cos(angle);
                float sinInv = (float)Math.Sin(angle);
                x += cosInv * pushLX - sinInv * pushLY;
                y += sinInv * pushLX + cosInv * pushLY;
            }
            return (x, y);
        }

        private bool IsPointInAnyWall(float px, float py)
        {
            foreach (var w in walls)
                if (px >= w.x && px <= w.x + w.width && py >= w.y && py <= w.y + w.height)
                    return true;
            foreach (var bw in boxWalls)
                if (wallActive && IsPointInRotatedWall(px, py, bw.x, bw.y, bw.width, bw.height, bw.angle))
                    return true;
            if (wallActive && IsPointInRotatedWall(px, py, tempWall.x, tempWall.y, tempWall.width, tempWall.height, tempWall.angle))
                return true;
            return false;
        }

        private bool IsPointInRotatedWall(float px, float py, float wx, float wy, float ww, float wh, float angle)
        {
            float cx = px - wx;
            float cy = py - wy;
            float cos = MathF.Cos(-angle);
            float sin = MathF.Sin(-angle);
            float lx = cos * cx - sin * cy;
            float ly = sin * cx + cos * cy;
            return MathF.Abs(lx) < ww / 2f && MathF.Abs(ly) < wh / 2f;
        }

        // Sample points along a segment and test whether any lies inside an axis-aligned square.
        private static bool GunSegmentIntersectsRect(float x0, float y0, float x1, float y1, float rx, float ry, float rSize)
        {
            const int samples = 10;
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                float px = x0 + (x1 - x0) * t;
                float py = y0 + (y1 - y0) * t;
                if (px >= rx && px <= rx + rSize && py >= ry && py <= ry + rSize) return true;
            }
            return false;
        }

        // Checks whether the line segment from body center to gun tip at the given angle clears all walls.
        private bool GunSegmentClearsWalls(float cx, float cy, int pSize, float aimAngle)
        {
            if (playerSpriteCropped == null) return true;
            var (tipX, tipY) = GetGunTipWorldAtAngle(cx, cy, pSize, aimAngle);
            const int samples = 10;
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                float px = cx + (tipX - cx) * t;
                float py = cy + (tipY - cy) * t;
                if (IsPointInAnyWall(px, py)) return false;
            }
            return true;
        }

        // Returns aim angle clamped so the gun barrel never clips into any wall.
        // Rotates incrementally from the last valid angle toward the desired angle,
        // stopping at the first step that would intersect a wall -- so the gun slides
        // along walls instead of jumping past them.
        private float GetClampedAimAngle(float cx, float cy, int pSize)
        {
            float desired = MathF.Atan2(mousePos.Y - cy, mousePos.X - cx);
            float current = _lastValidAimAngle;

            // If the stored "last valid" angle is no longer clear (e.g., player just moved
            // into a new obstacle configuration), find a fresh valid angle to start from.
            if (!GunSegmentClearsWalls(cx, cy, pSize, current))
            {
                bool recovered = false;
                for (float a = 0f; a < MathF.Tau; a += 0.05f)
                {
                    if (GunSegmentClearsWalls(cx, cy, pSize, a))
                    {
                        current = a;
                        recovered = true;
                        break;
                    }
                }
                if (!recovered) return _lastValidAimAngle; // completely boxed in
            }

            // Sweep from current toward desired along the shortest arc, stopping at first block.
            float delta = MathF.IEEERemainder(desired - current, MathF.Tau);
            float sign = delta >= 0f ? 1f : -1f;
            float mag = MathF.Abs(delta);
            const float step = 0.03f; // ~1.7° per sub-step
            int steps = (int)MathF.Ceiling(mag / step);
            float best = current;
            for (int i = 1; i <= steps; i++)
            {
                float t = MathF.Min(i * step, mag);
                float candidate = current + sign * t;
                if (GunSegmentClearsWalls(cx, cy, pSize, candidate))
                    best = candidate;
                else
                    break;
            }
            _lastValidAimAngle = best;
            return best;
        }

        private void ShowUpgradeMenu()
        {
            int formW = Math.Min(ClientSize.Width - 40, (int)(900 * scale));
            int formH = Math.Min(ClientSize.Height - 40, (int)(490 * scale));

            Panel upgradeForm = new Panel();
            upgradeForm.Size = new Size(formW, formH);
            upgradeForm.Location = new Point((ClientSize.Width - formW) / 2, (ClientSize.Height - formH) / 2);
            upgradeForm.BackColor = Color.FromArgb(30, 30, 30);
            typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(upgradeForm, true);
            upgradeForm.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int r = (int)(14 * scale);
                var rect = new Rectangle(0, 0, upgradeForm.Width - 1, upgradeForm.Height - 1);
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(rect.X, rect.Y, r, r, 180, 90);
                path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                path.CloseFigure();
                using var bgBrush = new SolidBrush(Color.FromArgb(240, 22, 22, 32));
                g.FillPath(bgBrush, path);
                using var borderPen2 = new Pen(Color.FromArgb(100, 70, 70, 120), 2 * scale);
                g.DrawPath(borderPen2, path);
            };

            float s = scale;

            Button upgradesNavBtn = new Button();
            upgradesNavBtn.Text = "⚔ Upgrades";
            upgradesNavBtn.Size = new Size((int)(120 * s), (int)(30 * s));
            upgradesNavBtn.Location = new Point((int)(20 * s), (int)(10 * s));
            upgradesNavBtn.FlatStyle = FlatStyle.Flat;
            upgradesNavBtn.Font = new Font("Arial", Math.Max(1, (int)(9 * s)));
            upgradesNavBtn.ForeColor = Color.White;
            upgradesNavBtn.BackColor = Color.FromArgb(80, 80, 130);
            upgradesNavBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            upgradeForm.Controls.Add(upgradesNavBtn);

            Button colorNavBtn = new Button();
            colorNavBtn.Text = "🎨 Preferences";
            colorNavBtn.Size = new Size((int)(120 * s), (int)(30 * s));
            colorNavBtn.Location = new Point((int)(150 * s), (int)(10 * s));
            colorNavBtn.FlatStyle = FlatStyle.Flat;
            colorNavBtn.Font = new Font("Arial", Math.Max(1, (int)(9 * s)));
            colorNavBtn.ForeColor = Color.White;
            colorNavBtn.BackColor = Color.FromArgb(50, 50, 50);
            colorNavBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            upgradeForm.Controls.Add(colorNavBtn);

            Label scoreLabel = new Label();
            scoreLabel.Text = "💲: " + score;
            scoreLabel.Font = new Font("Arial", Math.Max(1, (int)(12 * s)), FontStyle.Bold);
            scoreLabel.ForeColor = Color.Gold;
            scoreLabel.Size = new Size((int)(500 * s), (int)(30 * s));
            scoreLabel.Location = new Point((int)(300 * s), (int)(10 * s));
            upgradeForm.Controls.Add(scoreLabel);

            Button darkModeBtn = new Button();
            darkModeBtn.Text = darkMode ? "☀ Light Mode" : "🌙 Dark Mode";
            darkModeBtn.Size = new Size((int)(120 * s), (int)(30 * s));
            darkModeBtn.Font = new Font("Arial", Math.Max(1, (int)(9 * s)));
            darkModeBtn.Location = new Point(upgradeForm.Width - (int)(140 * s), (int)(10 * s));
            darkModeBtn.BackColor = darkMode ? Color.FromArgb(200, 200, 200) : Color.FromArgb(50, 50, 50);
            darkModeBtn.ForeColor = darkMode ? Color.Black : Color.White;
            darkModeBtn.FlatStyle = FlatStyle.Flat;
            darkModeBtn.Click += (s, e) =>
            {
                darkMode = !darkMode;
                darkModeBtn.Text = darkMode ? "☀ Light Mode" : "🌙 Dark Mode";
                darkModeBtn.BackColor = darkMode ? Color.FromArgb(200, 200, 200) : Color.FromArgb(50, 50, 50);
                darkModeBtn.ForeColor = darkMode ? Color.Black : Color.White;
                upgradeForm.BackColor = darkMode ? Color.FromArgb(20, 20, 20) : Color.FromArgb(30, 30, 30);
                scoreLabel.ForeColor = darkMode ? Color.White : Color.Gold;
            };
            upgradeForm.Controls.Add(darkModeBtn);
            darkModeBtn.BringToFront();

            var upgrades = new[]
            {
                new { Title = "Fortification",   Icon = "🧱", Description = "more wall length and uptime",       Cost = 190,  Stack = "Stacks", Category = "Defensive" },
                new { Title = "Tank",            Icon = "🛡", Description = "+10 Max HP",                        Cost = 450,  Stack = "Stacks", Category = "Defensive" },
                new { Title = "Time Travel",     Icon = "⏭️", Description = "-0.05 time refresh",               Cost = 470,  Stack = "Stacks", Category = "Cooldown"   },
                new { Title = "Thruster",        Icon = "🚀", Description = "+1 Bullet Speed",                   Cost = 500,  Stack = "Stacks", Category = "Offensive" },
                new { Title = "Time is Money",   Icon = "★",  Description = "+1 score per money refresh",         Cost = 510,  Stack = "Stacks", Category = "Economy"   },
                new { Title = "Bigger Player",   Icon = "⬛", Description = "Bigger player, bullet, and steps",  Cost = 600,  Stack = "Stacks", Category = "Defensive" },
                new { Title = "Stronger Decoy",  Icon = "🧥", Description = "+1s decoy time",                    Cost = 610,  Stack = "Stacks", Category = "Cooldown"  },
                new { Title = "Minigun Trait",   Icon = "⚡", Description = "become minigun",                    Cost = 890,  Stack = "", Category = "Offensive" },
                new { Title = "Leap of Faith",   Icon = "💨", Description = "+0.1s dash duration",               Cost = 660,  Stack = "Stacks", Category = "Defensive"  },
                new { Title = "Dash Cooldown",   Icon = "⌛", Description = "-0.1s Dash Cooldown",               Cost = 670,  Stack = "Stacks", Category = "Cooldown"  },
                new { Title = "Explosive Finish",Icon = "💥", Description = "Last bullet in mag deals 3x dmg",  Cost = 690,  Stack = "",       Category = "Offensive" },
                new { Title = "Ammo Reload",     Icon = "🔄", Description = "-0.5s reload time",                 Cost = 730,  Stack = "Stacks", Category = "Cooldown" },
                new { Title = "Afterburn",       Icon = "🔥", Description = "+ speed after dash for 2s",         Cost = 750,  Stack = "",       Category = "Defensive" },
                new { Title = "Blink Cooldown",  Icon = "⏲️", Description = "-5s Blink Cooldown",               Cost = 790,  Stack = "Stacks", Category = "Cooldown"  },
                new { Title = "Larger Mag",      Icon = "🔫", Description = "+5 max ammo",                       Cost = 800,  Stack = "Stacks", Category = "Offensive" },
                new { Title = "Tough Love",      Icon = "💪", Description = "Lower HP = faster movement",        Cost = 800,  Stack = "",       Category = "Defensive" },
                new { Title = "Super Instinct",  Icon = "⏱", Description = "-2s Super cooldown",                Cost = 900,  Stack = "Stacks", Category = "Cooldown"  },
                new { Title = "Ricochet",        Icon = "🎯", Description = "+2 bullet bounce",                  Cost = 950,  Stack = "Stacks", Category = "Offensive" },
                new { Title = "Double Tap",      Icon = "🔀", Description = "Every 5th bullet fires 3",          Cost = 950,  Stack = "",       Category = "Offensive" },
                new { Title = "Medic",           Icon = "💊", Description = "Coins restore 0.5 HP",              Cost = 1000, Stack = "",       Category = "Defensive" },
                new { Title = "Jackpot",         Icon = "🎰", Description = "10% chance for 3 coins",            Cost = 1010, Stack = "",       Category = "Economy"   },
                new { Title = "Orbit",           Icon = "🌑", Description = "Bullet orbits the player",          Cost = 1200, Stack = "Stacks", Category = "Offensive" },
                new { Title = "Box Wall",        Icon = "📦", Description = "E spawns a box instead of wall",    Cost = 1230, Stack = "",       Category = "Defensive" },
                new { Title = "Flame Wall",      Icon = "🔥", Description = "Enemies burn when touching wall",   Cost = 1250, Stack = "",       Category = "Offensive" },
                new { Title = "Stonks",          Icon = "↗️", Description = "Coins are worth more",              Cost = 1290, Stack = "Stacks", Category = "Economy"   },
                new { Title = "Ghost Dash",      Icon = "👻", Description = "Dash leaves damage trail",          Cost = 1300, Stack = "",       Category = "Offensive" },
                new { Title = "Decoy",           Icon = "🪆", Description = "F to spawn enemy decoy",            Cost = 1350, Stack = "",       Category = "Defensive" },
                new { Title = "Blink",           Icon = "🌀", Description = "Right click to teleport",           Cost = 1390, Stack = "",       Category = "Defensive" },
                new { Title = "Shrapnel",        Icon = "💢", Description = "Enemies explode on death",          Cost = 1410, Stack = "",       Category = "Offensive" },
                new { Title = "Life Steal",      Icon = "♥",  Description = "+1 HP per kill",                    Cost = 1600, Stack = "Stacks", Category = "Defensive" },
                new { Title = "Piercing",        Icon = "🏹", Description = "Bullets pass through enemies",      Cost = 2200, Stack = "",       Category = "Offensive" },
                new { Title = "Homing",          Icon = "🎯", Description = "Bullets curve to enemies",          Cost = 3100, Stack = "",       Category = "Offensive" },
                new { Title = "Speed Trap",      Icon = "🕸️", Description = "G to slow nearby enemies",         Cost = 950,  Stack = "",       Category = "Defensive" },
                new { Title = "Thorns",          Icon = "🌵", Description = "Enemies take 1 dmg on hit",         Cost = 750,  Stack = "",       Category = "Defensive" },
                new { Title = "Cashback",        Icon = "💸", Description = "10% of costs refunded over 30s",    Cost = 900,  Stack = "",       Category = "Economy"   },
                new { Title = "Orbital Strike",  Icon = "🌑", Description = "Orbit slows enemies by 50%",        Cost = 1300, Stack = "",       Category = "Offensive" },
                new { Title = "Wide Orbit",      Icon = "🌍", Description = "+30 orbit radius",                  Cost = 600,  Stack = "Stacks", Category = "Offensive" },
                new { Title = "Turret",           Icon = "🗼", Description = "H to place a shooting turret", Cost = 1400, Stack = "",       Category = "Offensive" },
                new { Title = "Rapid Reload",     Icon = "🔃", Description = "Taking damage reloads 5 bullets", Cost = 820, Stack = "",    Category = "Offensive" },
                new { Title = "Explosive Orbit",  Icon = "💫", Description = "Orbit bullets explode on hit",   Cost = 1500, Stack = "",    Category = "Offensive" },
                new { Title = "Ricochet Explosion", Icon = "💣", Description = "Bounced bullets explode on final hit", Cost = 1400, Stack = "", Category = "Offensive" },
                new { Title = "Blood Money",        Icon = "🩸", Description = "Gain 1 coin per HP lost",             Cost = 1100, Stack = "", Category = "Economy"  },
                new { Title = "Parasite Immune",    Icon = "🧬", Description = "Parasites can't hurt you",             Cost = 1200, Stack = "", Category = "Defensive"},
                new { Title = "Last Stand", Icon = "💢", Description = "Below 15 HP: bullets deal 2x damage", Cost = 1100, Stack = "", Category = "Offensive" },
                new { Title = "Smart Bounce",    Icon = "🧠", Description = "+1 bounce; bounced bullets seek nearest enemy", Cost = 1220, Stack = "", Category = "Offensive" },
            };

            int cardWidth = (int)(155 * s);
            int cardHeight = (int)(290 * s);
            int cardGap = (int)(15 * s);
            int totalWidth = upgrades.Length * (cardWidth + cardGap) + cardGap;
            int scrollOffset = 0;
            int scrollStep = cardWidth + cardGap;
            int scrollPanelW = (int)(800 * s);
            int maxScroll = Math.Max(0, totalWidth - scrollPanelW);
            string selectedCategory = "All";

            var categories = new[] { "All", "Offensive", "Defensive", "Economy", "Cooldown" };
            List<Button> tabButtons = new List<Button>();
            int tabX = (int)(20 * s);

            Panel scrollPanel = new Panel();
            scrollPanel.Size = new Size(scrollPanelW, cardHeight + (int)(10 * s));
            scrollPanel.Location = new Point((int)(50 * s), (int)(80 * s));
            scrollPanel.BackColor = Color.FromArgb(30, 30, 30);
            upgradeForm.Controls.Add(scrollPanel);

            Panel innerPanel = new Panel();
            innerPanel.Size = new Size(totalWidth, cardHeight + (int)(10 * s));
            innerPanel.Location = new Point(0, 0);
            innerPanel.BackColor = Color.FromArgb(30, 30, 30);
            scrollPanel.Controls.Add(innerPanel);

            Panel colorPanel = new Panel();
            colorPanel.Size = new Size((int)(860 * s), (int)(370 * s));
            colorPanel.Location = new Point((int)(20 * s), (int)(80 * s));
            colorPanel.BackColor = Color.FromArgb(30, 30, 30);
            colorPanel.Visible = false;
            upgradeForm.Controls.Add(colorPanel);

            var presetColors = new[]
            {
                new { Name = "Blue",   Color = Color.FromArgb(0, 50, 255)   },
                new { Name = "Red",    Color = Color.FromArgb(220, 30, 30)  },
                new { Name = "Green",  Color = Color.FromArgb(30, 180, 30)  },
                new { Name = "Purple", Color = Color.FromArgb(140, 0, 220)  },
                new { Name = "Orange", Color = Color.FromArgb(255, 140, 0)  },
                new { Name = "Cyan",   Color = Color.FromArgb(0, 200, 220)  },
                new { Name = "Pink",   Color = Color.FromArgb(255, 80, 180) },
                new { Name = "Yellow", Color = Color.FromArgb(220, 200, 0)  },
                new { Name = "White",  Color = Color.FromArgb(240, 240, 240)},
                new { Name = "Black",  Color = Color.FromArgb(20, 20, 20)   },
                new { Name = "Gold",   Color = Color.FromArgb(212, 175, 55) },
                new { Name = "Teal",   Color = Color.FromArgb(0, 150, 130)  },
            };

            Label colorTitle = new Label();
            colorTitle.Text = "Choose Player Color";
            colorTitle.Font = new Font("Arial", Math.Max(1, (int)(14 * s)), FontStyle.Bold);
            colorTitle.ForeColor = Color.White;
            colorTitle.Size = new Size((int)(400 * s), (int)(30 * s));
            colorTitle.Location = new Point((int)(20 * s), (int)(10 * s));
            colorPanel.Controls.Add(colorTitle);

            Panel previewBox = new Panel();
            previewBox.Size = new Size((int)(60 * s), (int)(60 * s));
            previewBox.BackColor = playerColor;
            previewBox.BorderStyle = BorderStyle.FixedSingle;
            colorPanel.Controls.Add(previewBox);

            Label previewLabel = new Label();
            previewLabel.Text = "Preview";
            previewLabel.ForeColor = Color.LightGray;
            previewLabel.Font = new Font("Arial", Math.Max(1, (int)(9 * s)));
            previewLabel.Size = new Size((int)(60 * s), (int)(20 * s));
            previewLabel.TextAlign = ContentAlignment.MiddleCenter;
            colorPanel.Controls.Add(previewLabel);

            int colorX = (int)(20 * s);
            int colorY = (int)(50 * s);
            int colorBtnSize = (int)(80 * s);
            int colorSpacing = (int)(90 * s);
            int colorMaxX = (int)(650 * s);
            foreach (var preset in presetColors)
            {
                var capturedColor = preset.Color;
                var capturedName = preset.Name;
                Panel colorBtn = new Panel();
                colorBtn.Size = new Size(colorBtnSize, colorBtnSize);
                colorBtn.Location = new Point(colorX, colorY);
                colorBtn.BackColor = preset.Color;
                colorBtn.Cursor = Cursors.Hand;
                colorBtn.BorderStyle = BorderStyle.FixedSingle;
                Label colorName = new Label();
                colorName.Text = capturedName;
                colorName.Font = new Font("Arial", Math.Max(1, (int)(8 * s)));
                colorName.ForeColor = Color.White;
                colorName.BackColor = Color.Transparent;
                colorName.TextAlign = ContentAlignment.BottomCenter;
                colorName.Size = new Size(colorBtnSize, colorBtnSize);
                colorName.Location = new Point(0, 0);
                colorBtn.Controls.Add(colorName);
                colorBtn.Click += (ss, ee) => { playerColor = capturedColor; previewBox.BackColor = capturedColor; };
                colorName.Click += (ss, ee) => { playerColor = capturedColor; previewBox.BackColor = capturedColor; };
                colorBtn.MouseEnter += (ss, ee) => colorBtn.BorderStyle = BorderStyle.Fixed3D;
                colorBtn.MouseLeave += (ss, ee) => colorBtn.BorderStyle = BorderStyle.FixedSingle;
                colorPanel.Controls.Add(colorBtn);
                colorX += colorSpacing;
                if (colorX > colorMaxX) { colorX = (int)(20 * s); colorY += colorSpacing; }
            }

            previewBox.Location = new Point((int)(20 * s), colorY + (int)(95 * s));
            previewLabel.Location = new Point((int)(20 * s), colorY + (int)(158 * s));

            Button customColorBtn = new Button();
            customColorBtn.Text = "🎨 Custom Color...";
            customColorBtn.Size = new Size((int)(160 * s), (int)(35 * s));
            customColorBtn.Font = new Font("Arial", Math.Max(1, (int)(9 * s)));
            customColorBtn.Location = new Point((int)(100 * s), colorY + (int)(95 * s));
            customColorBtn.FlatStyle = FlatStyle.Flat;
            customColorBtn.ForeColor = Color.White;
            customColorBtn.BackColor = Color.FromArgb(60, 60, 60);
            customColorBtn.Click += (ss, ee) =>
            {
                using ColorDialog cd = new ColorDialog();
                cd.Color = playerColor;
                if (cd.ShowDialog() == DialogResult.OK) { playerColor = cd.Color; previewBox.BackColor = cd.Color; }
            };
            colorPanel.Controls.Add(customColorBtn);

            Label nameLabel = new Label();
            nameLabel.Text = "Player Name:";
            nameLabel.Font = new Font("Arial", Math.Max(1, (int)(10 * s)), FontStyle.Bold);
            nameLabel.ForeColor = Color.White;
            nameLabel.Size = new Size((int)(120 * s), (int)(25 * s));
            nameLabel.Location = new Point((int)(20 * s), colorY + (int)(140 * s));
            colorPanel.Controls.Add(nameLabel);

            TextBox nameBox = new TextBox();
            nameBox.Text = playerName;
            nameBox.Font = new Font("Arial", Math.Max(1, (int)(10 * s)));
            nameBox.Size = new Size((int)(150 * s), (int)(25 * s));
            nameBox.Location = new Point((int)(150 * s), colorY + (int)(140 * s));
            nameBox.MaxLength = 8;
            nameBox.BackColor = Color.FromArgb(50, 50, 50);
            nameBox.ForeColor = Color.White;
            nameBox.BorderStyle = BorderStyle.FixedSingle;
            nameBox.TextChanged += (ss, ee) =>
            {
                string input = nameBox.Text.Trim().ToUpper();
                if (!string.IsNullOrWhiteSpace(input) && input != "YOU")
                {
                    playerName = input;
                    nameBox.ForeColor = Color.White;
                }
                else
                {
                    playerName = "YOU";
                    nameBox.ForeColor = Color.Red;
                }
                SavePlayerName();
            };
            colorPanel.Controls.Add(nameBox);

            Label nameLimitLabel = new Label();
            nameLimitLabel.Text = "Max 8 characters";
            nameLimitLabel.Font = new Font("Arial", Math.Max(1, (int)(8 * s)));
            nameLimitLabel.ForeColor = Color.Gray;
            nameLimitLabel.Size = new Size((int)(150 * s), (int)(20 * s));
            nameLimitLabel.Location = new Point((int)(150 * s), colorY + (int)(168 * s));
            colorPanel.Controls.Add(nameLimitLabel);

            void RefreshCardPositions()
            {
                int visibleIndex = 0;
                foreach (Control ctrl in innerPanel.Controls)
                {
                    if (ctrl is Panel c && c.AccessibleName != null)
                    {
                        bool show = selectedCategory == "All" || c.AccessibleName == selectedCategory;
                        c.Visible = show;
                        if (show) { c.Location = new Point(cardGap + visibleIndex * (cardWidth + cardGap), (int)(5 * s)); visibleIndex++; }
                    }
                }
                int newTotal = Math.Max(visibleIndex * (cardWidth + cardGap) + cardGap, scrollPanel.Width);
                innerPanel.Width = newTotal;
                maxScroll = Math.Max(0, newTotal - scrollPanel.Width);
                scrollOffset = 0;
                innerPanel.Location = new Point(0, 0);
            }

            foreach (var cat in categories)
            {
                string capturedCat = cat;
                Button tab = new Button();
                tab.Text = cat;
                tab.Size = new Size((int)(80 * s), (int)(25 * s));
                tab.Location = new Point(tabX, (int)(45 * s));
                tab.FlatStyle = FlatStyle.Flat;
                tab.Font = new Font("Arial", Math.Max(1, (int)(8 * s)));
                tab.ForeColor = Color.White;
                tab.BackColor = cat == "All" ? Color.FromArgb(80, 80, 130) : Color.FromArgb(50, 50, 50);
                tab.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
                tab.Click += (ss, ee) =>
                {
                    selectedCategory = capturedCat;
                    foreach (var tb in tabButtons) tb.BackColor = Color.FromArgb(50, 50, 50);
                    tab.BackColor = Color.FromArgb(80, 80, 130);
                    RefreshCardPositions();
                };
                tabButtons.Add(tab);
                upgradeForm.Controls.Add(tab);
                tab.BringToFront();
                tabX += (int)(85 * s);
            }

            // Hover lift + affordability pulse state, shared with card Paint handlers below.
            var cardLift = new Dictionary<Panel, float>();
            var cardBaseY = new Dictionary<Panel, int>();
            var cardSquish = new Dictionary<Panel, float>(); // 1 right after click, decays to 0
            float[] pulseT = new float[] { 0f };
            int cardBaseYDefault = (int)(5 * s);
            float hoverLiftPx = 10f * s;
            float squishPx = 8f * s;

            System.Windows.Forms.Timer colorTimer = new System.Windows.Forms.Timer();
            colorTimer.Interval = 16; // ~60fps for smooth lift
            colorTimer.Tick += (s, e) =>
            {
                pulseT[0] += 0.12f;
                Point cursorPos = innerPanel.PointToClient(Cursor.Position);
                foreach (Control ctrl in innerPanel.Controls)
                {
                    if (ctrl is Panel c && c.Tag is int cardCost && c.Visible)
                    {
                        if (cardCost == int.MaxValue) { c.BackColor = Color.FromArgb(25, 25, 25); c.Cursor = Cursors.Default; continue; }
                        bool canAfford = score >= cardCost;
                        bool isHovered = c.Bounds.Contains(cursorPos);
                        if (!canAfford) c.BackColor = Color.FromArgb(40, 40, 40);
                        else if (isHovered) c.BackColor = Color.FromArgb(80, 80, 130);
                        else c.BackColor = Color.FromArgb(50, 50, 80);
                        c.Cursor = canAfford ? Cursors.Hand : Cursors.Default;
                        var costLbl = c.Controls.OfType<Label>().LastOrDefault();
                        if (costLbl != null) costLbl.ForeColor = canAfford ? Color.Gold : Color.Gray;

                        // Smooth hover lift toward target offset (+ click squish push-down)
                        if (cardBaseY.TryGetValue(c, out int by))
                        {
                            float target = (canAfford && isHovered) ? -hoverLiftPx : 0f;
                            float curr = cardLift.TryGetValue(c, out float v) ? v : 0f;
                            curr += (target - curr) * 0.25f;
                            if (MathF.Abs(target - curr) < 0.1f) curr = target;
                            cardLift[c] = curr;
                            float sq = cardSquish.TryGetValue(c, out float sv) ? sv : 0f;
                            if (sq > 0f) { sq = Math.Max(0f, sq - 0.12f); cardSquish[c] = sq; }
                            int newTop = by + (int)curr + (int)(sq * squishPx);
                            if (c.Top != newTop) c.Top = newTop;
                        }
                        // Repaint each tick so the pulsing affordability border animates
                        if (canAfford) c.Invalidate();
                    }
                }
                scoreLabel.Text = "💲: " + score;
            };
            colorTimer.Start();
            // colorTimer cleanup handled in close click

            var sortedUpgrades = upgrades
                .Select((u, idx) => new { Upgrade = u, OriginalIndex = idx })
                .OrderBy(x => x.Upgrade.Cost)
                .ToList();

            for (int i = 0; i < sortedUpgrades.Count; i++)
            {
                var upgrade = sortedUpgrades[i].Upgrade;
                int index = sortedUpgrades[i].OriginalIndex;
                int cost = upgrade.Cost;
                bool isStackable = upgrade.Stack == "Stacks";
                string category = upgrade.Category;
                Color catColor = GetCategoryColor(category);
                bool alreadyPurchased = purchasedOneTimeUpgrades.Contains(index);

                Panel card = new Panel();
                card.Size = new Size(cardWidth, cardHeight);
                card.Location = new Point(cardGap + i * (cardWidth + cardGap), (int)(5 * s));
                card.BackColor = alreadyPurchased ? Color.FromArgb(25, 25, 25) : score >= cost ? Color.FromArgb(50, 50, 80) : Color.FromArgb(40, 40, 40);
                var tintedCard = GetTintedCardForCategory(category);
                if (tintedCard != null)
                {
                    card.BackgroundImage = tintedCard;
                    card.BackgroundImageLayout = ImageLayout.Stretch;
                }
                card.Cursor = alreadyPurchased ? Cursors.Default : score >= cost ? Cursors.Hand : Cursors.Default;
                card.Tag = alreadyPurchased ? int.MaxValue : cost;
                card.AccessibleName = category;

                // Title sits inside the dark band at the top of card.png; tinted with category color.
                Label title = new Label();
                title.Text = upgrade.Title;
                title.Font = new Font("Arial", Math.Max(1, (int)(10 * s)), FontStyle.Bold);
                title.ForeColor = catColor;
                title.BackColor = Color.Transparent;
                title.TextAlign = ContentAlignment.MiddleCenter;
                title.Size = new Size(cardWidth, (int)(28 * s));
                title.Location = new Point(0, (int)(4 * s));
                title.AutoSize = false;

                Label icon = new Label();
                icon.Text = upgrade.Icon;
                icon.Font = new Font("Segoe UI Emoji", Math.Max(1, (int)(34 * s)));
                icon.ForeColor = Color.White;
                icon.BackColor = Color.Transparent;
                icon.TextAlign = ContentAlignment.MiddleCenter;
                icon.Size = new Size(cardWidth, (int)(100 * s));
                icon.Location = new Point(0, (int)(45 * s));
                icon.AutoSize = false;

                Label desc = new Label();
                desc.Text = upgrade.Description;
                desc.Font = new Font("Arial", Math.Max(1, (int)(9 * s)));
                desc.ForeColor = Color.LightGray;
                desc.BackColor = Color.Transparent;
                desc.TextAlign = ContentAlignment.MiddleCenter;
                desc.Size = new Size(cardWidth - (int)(10 * s), (int)(60 * s));
                desc.Location = new Point((int)(5 * s), (int)(150 * s));
                desc.AutoSize = false;

                Label stacking = new Label();
                stacking.Text = upgrade.Stack;
                stacking.Font = new Font("Arial", Math.Max(1, (int)(7 * s)));
                stacking.ForeColor = Color.LightGray;
                stacking.BackColor = Color.Transparent;
                stacking.TextAlign = ContentAlignment.MiddleCenter;
                stacking.Size = new Size(cardWidth - (int)(10 * s), (int)(20 * s));
                stacking.Location = new Point((int)(5 * s), (int)(210 * s));
                stacking.AutoSize = false;

                Label categoryLabel = new Label();
                categoryLabel.Text = GetCategoryIcon(category) + " " + category;
                categoryLabel.Font = new Font("Segoe UI Emoji", Math.Max(1, (int)(7 * s)));
                categoryLabel.ForeColor = Color.FromArgb(180, 180, 180);
                categoryLabel.BackColor = Color.Transparent;
                categoryLabel.TextAlign = ContentAlignment.MiddleCenter;
                categoryLabel.Size = new Size(cardWidth, (int)(20 * s));
                categoryLabel.Location = new Point(0, (int)(230 * s));
                categoryLabel.AutoSize = false;

                Label costLabel = new Label();
                costLabel.Text = cost + " pts";
                costLabel.Font = new Font("Arial", Math.Max(1, (int)(9 * s)), FontStyle.Bold);
                costLabel.ForeColor = score >= cost ? Color.Gold : Color.Gray;
                costLabel.BackColor = Color.Transparent;
                costLabel.TextAlign = ContentAlignment.MiddleCenter;
                costLabel.Size = new Size(cardWidth, (int)(30 * s));
                costLabel.Location = new Point(0, (int)(253 * s));
                costLabel.AutoSize = false;

                card.Controls.Add(title);
                card.Controls.Add(icon);
                card.Controls.Add(desc);
                card.Controls.Add(stacking);
                card.Controls.Add(categoryLabel);
                card.Controls.Add(costLabel);

                Panel capturedCard = card;

                EventHandler clickHandler = (s, e) =>
                {
                    if (score < cost) return;
                    if (capturedCard.Tag is int t && t == int.MaxValue) return;
                    // In multiplayer as client, send upgrade to host
                    if (isMultiplayer && !isNetHost)
                    {
                        p2PendingUpgrade = index;
                        score -= cost; // deduct locally so UI updates immediately
                    }
                    else
                    {
                        ApplyUpgradeByIndex(index);
                    }
                    if (!isStackable)
                    {
                        purchasedOneTimeUpgrades.Add(index);
                        capturedCard.Tag = int.MaxValue;
                        capturedCard.BackColor = Color.FromArgb(25, 25, 25);
                        capturedCard.Cursor = Cursors.Default;
                        foreach (Control ctrl in capturedCard.Controls)
                        {
                            if (ctrl is Panel p) p.BackColor = Color.FromArgb(35, 35, 35);
                            else ctrl.ForeColor = Color.FromArgb(60, 60, 60);
                        }
                    }
                };

                card.Click += clickHandler;
                foreach (Control c in card.Controls) c.Click += clickHandler;

                MouseEventHandler squishHandler = (ms, me) =>
                {
                    if (capturedCard.Tag is int tt && tt == int.MaxValue) return;
                    if (score < cost) return;
                    cardSquish[capturedCard] = 1f;
                };
                card.MouseDown += squishHandler;
                foreach (Control c in card.Controls) c.MouseDown += squishHandler;

                // Track baseline Y for hover-lift animation
                cardBaseY[card] = cardBaseYDefault;
                cardLift[card] = 0f;

                // Pulsing green border when affordable — clear "you can buy this" cue
                int captCost = cost;
                bool captPurchased = alreadyPurchased;
                card.Paint += (sender, pe) =>
                {
                    if (captPurchased) return;
                    if (score < captCost) return;
                    float pulse = 0.5f + 0.5f * MathF.Sin(pulseT[0]);
                    int alpha = (int)(140 + 100 * pulse);
                    pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var pen = new Pen(Color.FromArgb(alpha, 90, 240, 130), 2.5f);
                    var rect = new Rectangle(1, 1, card.Width - 3, card.Height - 3);
                    pe.Graphics.DrawRectangle(pen, rect);
                };

                innerPanel.Controls.Add(card);
            }

            Button scrollLeft = new Button();
            scrollLeft.Text = "◀";
            scrollLeft.Size = new Size((int)(40 * s), cardHeight + (int)(10 * s));
            scrollLeft.Location = new Point((int)(5 * s), (int)(80 * s));
            scrollLeft.BackColor = Color.FromArgb(50, 50, 50);
            scrollLeft.ForeColor = Color.White;
            scrollLeft.Font = new Font("Arial", Math.Max(1, (int)(10 * s)));
            scrollLeft.FlatStyle = FlatStyle.Flat;
            scrollLeft.Click += (ss, ee) => { scrollOffset = Math.Max(0, scrollOffset - scrollStep); innerPanel.Location = new Point(-scrollOffset, 0); };
            upgradeForm.Controls.Add(scrollLeft);
            scrollLeft.BringToFront();

            Button scrollRight = new Button();
            scrollRight.Text = "▶";
            scrollRight.Size = new Size((int)(40 * s), cardHeight + (int)(10 * s));
            scrollRight.Location = new Point(formW - (int)(45 * s), (int)(80 * s));
            scrollRight.BackColor = Color.FromArgb(50, 50, 50);
            scrollRight.ForeColor = Color.White;
            scrollRight.Font = new Font("Arial", Math.Max(1, (int)(10 * s)));
            scrollRight.FlatStyle = FlatStyle.Flat;
            scrollRight.Click += (ss, ee) => { scrollOffset = Math.Min(maxScroll, scrollOffset + scrollStep); innerPanel.Location = new Point(-scrollOffset, 0); };
            upgradeForm.Controls.Add(scrollRight);
            scrollRight.BringToFront();

            upgradesNavBtn.Click += (s, e) =>
            {
                scrollPanel.Visible = true; colorPanel.Visible = false;
                upgradesNavBtn.BackColor = Color.FromArgb(80, 80, 130);
                colorNavBtn.BackColor = Color.FromArgb(50, 50, 50);
                foreach (var tb in tabButtons) tb.Visible = true;
                scrollLeft.Visible = true; scrollRight.Visible = true;
            };

            colorNavBtn.Click += (s, e) =>
            {
                scrollPanel.Visible = false; colorPanel.Visible = true;
                colorNavBtn.BackColor = Color.FromArgb(80, 80, 130);
                upgradesNavBtn.BackColor = Color.FromArgb(50, 50, 50);
                foreach (var tb in tabButtons) tb.Visible = false;
                scrollLeft.Visible = false; scrollRight.Visible = false;
            };

            Button close = new Button();
            close.Text = "Close";
            close.Font = new Font("Arial", Math.Max(1, (int)(10 * s)), FontStyle.Bold);
            close.Size = new Size((int)(100 * s), (int)(35 * s));
            close.Location = new Point(upgradeForm.Width / 2 - (int)(50 * s), formH - (int)(60 * s));
            close.BackColor = Color.FromArgb(80, 30, 30);
            close.ForeColor = Color.White;
            close.FlatStyle = FlatStyle.Flat;
            close.Click += (s, e) => { colorTimer.Stop(); this.Controls.Remove(upgradeForm); upgradeForm.Dispose(); activeUpgradePanel = null; showDimOverlay = false; this.Invalidate(); isPaused = false; velocityX = 0f; velocityY = 0f; HidePauseButtons(); this.Focus(); };
            upgradeForm.Controls.Add(close);

            showDimOverlay = true;
            this.Invalidate();
            activeUpgradePanel = upgradeForm;
            this.Controls.Add(upgradeForm);
            upgradeForm.BringToFront();

            // Reticle overlay as a FORM child (not a panel child) so it can move across
            // the whole window and sit above every sibling control. Its Region is
            // shaped like the reticle itself, so all non-reticle pixels are truly
            // cut out — no grey box, buttons show through between the crosshair lines.
            var reticleOverlay = new ReticleOverlay(this);
            reticleOverlay.Size = new Size(80, 80);
            reticleOverlay.Location = new Point(-200, -200);
            this.Controls.Add(reticleOverlay);
            reticleOverlay.BringToFront();

            var reticleTick = new System.Windows.Forms.Timer();
            reticleTick.Interval = 33;
            reticleTick.Tick += (s2, e2) =>
            {
                if (upgradeForm.IsDisposed)
                {
                    reticleTick.Stop(); reticleTick.Dispose();
                    if (!reticleOverlay.IsDisposed) { this.Controls.Remove(reticleOverlay); reticleOverlay.Dispose(); }
                    return;
                }
                var p = this.PointToClient(Cursor.Position);
                reticleOverlay.Location = new Point(p.X - reticleOverlay.Width / 2, p.Y - reticleOverlay.Height / 2);
                reticleOverlay.RefreshShape();
                reticleOverlay.BringToFront();
            };
            reticleTick.Start();

            AnimateZoomIn(upgradeForm);
        }

        private void ApplyEnemyBuff()
        {
            List<int> pool = new List<int>();
            if (currentEnemySpeed < 20f) pool.Add(0);
            if (boxSize > 1) pool.Add(1);
            if (enemyReinforceChance < maxReinforceChance) pool.Add(2);
            if (enemyDamage < 10f) pool.Add(3);
            if (shootingEnemyChance < maxShootingEnemyChance) pool.Add(4);
            if (tankEnemyChance < maxTankEnemyChance) pool.Add(5);
            if (runnerEnemyChance < maxRunnerEnemyChance) pool.Add(6);
            if (pool.Count == 0) return;
            int chosen = pool[rng.Next(pool.Count)];
            switch (chosen)
            {
                case 0: currentEnemySpeed += 0.5f * scale; buffMessage = "⚡ Enemies are faster!"; break;
                case 1: boxSize = Math.Max(2, boxSize - 2); buffMessage = "🔬 Enemies are smaller!"; break;
                case 2: enemyReinforceChance = Math.Min(maxReinforceChance, enemyReinforceChance + 0.05f); buffMessage = "🛡 Enemies are more reinforced!"; break;
                case 3: enemyDamage += 1f; buffMessage = "💢 Enemies hit harder!"; break;
                case 4: shootingEnemyChance = Math.Min(maxShootingEnemyChance, shootingEnemyChance + 0.03f); buffMessage = "🔫 Enemies can shoot more often!"; break;
                case 5: tankEnemyChance = Math.Min(maxTankEnemyChance, tankEnemyChance + 0.03f); buffMessage = "💀 More tanks incoming!"; break;
                case 6: runnerEnemyChance = Math.Min(maxRunnerEnemyChance, runnerEnemyChance + 0.02f); buffMessage = "🏃 More runners incoming!"; break;
            }
            buffMessageTimer = buffMessageDuration;
        }

        Color GetCategoryColor(string category) => category switch
        {
            "Offensive" => Color.FromArgb(140, 40, 40),
            "Defensive" => Color.FromArgb(40, 80, 140),
            "Economy" => Color.FromArgb(140, 120, 20),
            _ => Color.FromArgb(60, 60, 80),
        };

        string GetCategoryIcon(string category) => category switch
        {
            "Offensive" => "⚔",
            "Defensive" => "🛡",
            "Economy" => "💰",
            _ => "⚙",
        };

        private bool ShowDeathScreen()
        {
            bool retry = false;
            bool closed = false;

            int formW = (int)(500 * scale);
            int formH = (int)(380 * scale);
            showDimOverlay = true;
            this.Invalidate();

            Panel deathForm = new Panel();
            deathForm.Size = new Size(formW, formH);
            deathForm.Location = new Point((ClientSize.Width - formW) / 2, (ClientSize.Height - formH) / 2);
            typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(deathForm, true);
            deathForm.BackColor = Color.Transparent;
            deathForm.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int r = (int)(14 * scale);
                var rect = new Rectangle(0, 0, deathForm.Width - 1, deathForm.Height - 1);
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(rect.X, rect.Y, r, r, 180, 90);
                path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                path.CloseFigure();
                using var bgBrush = new SolidBrush(Color.FromArgb(240, 22, 22, 32));
                g.FillPath(bgBrush, path);
                using var borderPen2 = new Pen(Color.FromArgb(120, 140, 40, 40), 2 * scale);
                g.DrawPath(borderPen2, path);
            };

            float s = scale;
            Label titleLabel = new Label();
            titleLabel.Text = "GAME OVER";
            titleLabel.Font = new Font("Arial", (int)(28 * s), FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(200, 50, 50);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Size = new Size((int)(460 * s), (int)(50 * s));
            titleLabel.Location = new Point((int)(20 * s), (int)(20 * s));
            deathForm.Controls.Add(titleLabel);

            Panel divider = new Panel();
            divider.Size = new Size((int)(440 * s), 2);
            divider.Location = new Point((int)(30 * s), (int)(75 * s));
            divider.BackColor = Color.FromArgb(80, 80, 80);
            deathForm.Controls.Add(divider);

            int rowH = (int)(55 * s);
            int rowW = (int)(440 * s);
            Panel statsPanel = new Panel();
            statsPanel.Size = new Size(rowW, (int)(180 * s));
            statsPanel.Location = new Point((int)(30 * s), (int)(85 * s));
            statsPanel.BackColor = Color.FromArgb(40, 40, 40);
            statsPanel.BorderStyle = BorderStyle.None;
            deathForm.Controls.Add(statsPanel);

            int minutes = (int)(timeAlive / 60f);
            int seconds = (int)(timeAlive % 60f);
            var stats = new[]
            {
                new { Icon = "💲", Label = "Money Gained",   Value = totalScore.ToString()        },
                new { Icon = "💀", Label = "Enemies Killed", Value = totalKills.ToString()        },
                new { Icon = "⏱", Label = "Time Spent",     Value = $"{minutes:00}:{seconds:00}" },
            };

            for (int i = 0; i < stats.Length; i++)
            {
                Panel row = new Panel();
                row.Size = new Size(rowW, rowH);
                row.Location = new Point(0, i * (int)(58 * s) + (int)(5 * s));
                row.BackColor = i % 2 == 0 ? Color.FromArgb(45, 45, 55) : Color.FromArgb(38, 38, 48);

                Label iconLbl = new Label();
                iconLbl.Text = stats[i].Icon;
                iconLbl.Font = new Font("Segoe UI Emoji", (int)(18 * s));
                iconLbl.ForeColor = Color.White;
                iconLbl.Size = new Size((int)(50 * s), rowH);
                iconLbl.Location = new Point((int)(15 * s), 0);
                iconLbl.TextAlign = ContentAlignment.MiddleCenter;
                row.Controls.Add(iconLbl);

                Label nameLbl = new Label();
                nameLbl.Text = stats[i].Label;
                nameLbl.Font = new Font("Arial", (int)(11 * s));
                nameLbl.ForeColor = Color.FromArgb(180, 180, 180);
                nameLbl.Size = new Size((int)(200 * s), rowH);
                nameLbl.Location = new Point((int)(70 * s), 0);
                nameLbl.TextAlign = ContentAlignment.MiddleLeft;
                row.Controls.Add(nameLbl);

                Label valueLbl = new Label();
                valueLbl.Text = stats[i].Value;
                valueLbl.Font = new Font("Arial", (int)(14 * s), FontStyle.Bold);
                valueLbl.ForeColor = Color.Gold;
                valueLbl.Size = new Size((int)(150 * s), rowH);
                valueLbl.Location = new Point((int)(270 * s), 0);
                valueLbl.TextAlign = ContentAlignment.MiddleRight;
                row.Controls.Add(valueLbl);

                statsPanel.Controls.Add(row);
            }

            Panel divider2 = new Panel();
            divider2.Size = new Size((int)(440 * s), 2);
            divider2.Location = new Point((int)(30 * s), (int)(275 * s));
            divider2.BackColor = Color.FromArgb(80, 80, 80);
            deathForm.Controls.Add(divider2);

            int btnW = (int)(180 * s);
            int btnH = (int)(45 * s);
            Button retryBtn = new Button();
            retryBtn.Text = "Retry";
            retryBtn.Size = new Size(btnW, btnH);
            retryBtn.Location = new Point((int)(60 * s), (int)(295 * s));
            retryBtn.BackColor = Color.FromArgb(40, 100, 40);
            retryBtn.ForeColor = Color.White;
            retryBtn.FlatStyle = FlatStyle.Flat;
            retryBtn.FlatAppearance.BorderColor = Color.FromArgb(60, 140, 60);
            retryBtn.Font = new Font("Arial", (int)(12 * s), FontStyle.Bold);
            retryBtn.Click += (s2, e) => { retry = true; closed = true; };
            deathForm.Controls.Add(retryBtn);

            Button quitBtn = new Button();
            quitBtn.Text = "Quit";
            quitBtn.Size = new Size(btnW, btnH);
            quitBtn.Location = new Point((int)(260 * s), (int)(295 * s));
            quitBtn.BackColor = Color.FromArgb(100, 40, 40);
            quitBtn.ForeColor = Color.White;
            quitBtn.FlatStyle = FlatStyle.Flat;
            quitBtn.FlatAppearance.BorderColor = Color.FromArgb(140, 60, 60);
            quitBtn.Font = new Font("Arial", (int)(12 * s), FontStyle.Bold);
            quitBtn.Click += (s2, e) => { retry = false; closed = true; };
            deathForm.Controls.Add(quitBtn);

            retryBtn.MouseEnter += (s, e) => retryBtn.BackColor = Color.FromArgb(50, 130, 50);
            retryBtn.MouseLeave += (s, e) => retryBtn.BackColor = Color.FromArgb(40, 100, 40);
            quitBtn.MouseEnter += (s, e) => quitBtn.BackColor = Color.FromArgb(130, 50, 50);
            quitBtn.MouseLeave += (s, e) => quitBtn.BackColor = Color.FromArgb(100, 40, 40);

            this.Controls.Add(deathForm);
            deathForm.BringToFront();
            AnimateZoomIn(deathForm);
            while (!closed) Application.DoEvents();
            this.Controls.Remove(deathForm);
            deathForm.Dispose();
            showDimOverlay = false;
            this.Invalidate();
            this.Focus();
            return retry;
        }

        private void DamageEnemy(int i, float damage, bool canShrapnel = true)
        {
            if (i < 0 || i >= enemies.Count || i >= enemyHealth.Count || i >= enemyAlive.Count) return;
            if (!enemyAlive[i]) return;
            damage += permDamageLevel * 0.1f;

            // Armor check
            if (i < enemyIsArmored.Count && enemyIsArmored[i] && !enemyArmorBroken[i])
            {
                enemyArmorBroken[i] = true;
                hitFlashes.Add((enemies[i].x + boxSize / 2, enemies[i].y + boxSize / 2, 0.2f, 0.2f, boxSize));
                return;
            }

            // Reflective - chance to reflect
            if (i < enemyIsReflective.Count && enemyIsReflective[i])
            {
                if (rng.NextDouble() < 0.2f)
                {
                    health -= damage * 0.5f;
                    hitFlashes.Add((enemies[i].x + boxSize / 2, enemies[i].y + boxSize / 2, 0.1f, 0.1f, boxSize));
                    return;
                }
            }

            // Berserker - half damage when below 50%
            bool isBerserkerEnemy = i < enemyIsBerserker.Count && enemyIsBerserker[i];
            float maxHpB = i < enemyIsTank.Count && enemyIsTank[i] ? 8f :
                           i < enemyCanShoot.Count && enemyCanShoot[i] ? 4f :
                           i < enemyIsRunner.Count && enemyIsRunner[i] ? 1f : 2f;
            if (isBerserkerEnemy && enemyHealth[i] < maxHpB * 0.5f)
                damage *= 0.5f; // Takes less damage when berserk

            enemyHealth[i] -= damage;
            int hFlashSize = i < enemyIsTank.Count && enemyIsTank[i] ? boxSize + 20 :
                 i < enemyCanShoot.Count && enemyCanShoot[i] ? boxSize + 8 :
                 i < enemyIsRunner.Count && enemyIsRunner[i] ? boxSize - 8 : boxSize;
            hitFlashes.Add((enemies[i].x + hFlashSize / 2, enemies[i].y + hFlashSize / 2, 0.1f, 0.1f, hFlashSize));
            AddDamageNumber(enemies[i].x + boxSize / 2, enemies[i].y, damage, Color.FromArgb(255, 255, 220, 80));
            AddScreenShake(2f);
            bool isTank = i < enemyIsTank.Count && enemyIsTank[i];
            bool canShoot = i < enemyCanShoot.Count && enemyCanShoot[i];
            bool isRunner = i < enemyIsRunner.Count && enemyIsRunner[i];

                if (enemyHealth[i] <= 0)
            {
                // Bestiary tracking
                string enemyCategory = isTank ? "Tank" : canShoot ? "Gunner" : isRunner ? "Runner" : "Normal";
                if (beastiaryKills.ContainsKey(enemyCategory))
                    beastiaryKills[enemyCategory]++;
                if (i < enemyIsParasitic.Count && enemyIsParasitic[i] && beastiaryKills.ContainsKey("Parasitic"))
                    beastiaryKills["Parasitic"]++;
                if (i < enemyIsFrenzied.Count && enemyIsFrenzied[i] && beastiaryKills.ContainsKey("Frenzied"))
                    beastiaryKills["Frenzied"]++;
                if (i < enemyIsZigzag.Count && enemyIsZigzag[i] && beastiaryKills.ContainsKey("Zigzag"))
                    beastiaryKills["Zigzag"]++;
                if (i < enemyIsCharging.Count && enemyIsCharging[i] && beastiaryKills.ContainsKey("Charging"))
                    beastiaryKills["Charging"]++;
                if (i < enemyIsArmored.Count && enemyIsArmored[i] && beastiaryKills.ContainsKey("Armored"))
                    beastiaryKills["Armored"]++;
                if (i < enemyIsRegenerating.Count && enemyIsRegenerating[i] && beastiaryKills.ContainsKey("Regenerating"))
                    beastiaryKills["Regenerating"]++;
                if (i < enemyIsReflective.Count && enemyIsReflective[i] && beastiaryKills.ContainsKey("Reflective"))
                    beastiaryKills["Reflective"]++;
                if (i < enemyIsBerserker.Count && enemyIsBerserker[i] && beastiaryKills.ContainsKey("Berserker"))
                    beastiaryKills["Berserker"]++;
                if (i < enemyIsPhasing.Count && enemyIsPhasing[i] && beastiaryKills.ContainsKey("Phasing"))
                    beastiaryKills["Phasing"]++;
                if (i < enemyIsCorrupted.Count && enemyIsCorrupted[i] && beastiaryKills.ContainsKey("Corrupted"))
                    beastiaryKills["Corrupted"]++;
                beastiaryUnlocked = true;
                SaveBeastiary();
                float coinX = enemies[i].x + boxSize / 2;
                float coinY = enemies[i].y + boxSize / 2;
                int coinCount = 1;
                if (i < enemyIsTank.Count && enemyIsTank[i]) coinCount = 3;
                else if (i < enemyCanShoot.Count && enemyCanShoot[i]) coinCount = 2;
                else if (i < enemyIsRunner.Count && enemyIsRunner[i]) coinCount = 1;
                if (jackpot && rng.Next(0, 10) == 0) coinCount *= 3;
                for (int c = 0; c < coinCount; c++)
                    coins.Add((coinX, coinY, 0f, 0f));
                if (shrapnel && canShrapnel)
                {
                    for (int j = 0; j < enemies.Count; j++)
                    {
                        if (j == i || !enemyAlive[j]) continue;
                        float dx = enemies[j].x - enemies[i].x;
                        float dy = enemies[j].y - enemies[i].y;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (dist < 100f) DamageEnemy(j, 1f, false);
                    }
                    if (bossAlive)
                    {
                        float bcx = bossX + bossSize * scale / 2;
                        float bcy = bossY + bossSize * scale / 2;
                        float bdx = bcx - (enemies[i].x + boxSize / 2);
                        float bdy = bcy - (enemies[i].y + boxSize / 2);
                        if (Math.Sqrt(bdx * bdx + bdy * bdy) < 100f) DamageBoss(1f);
                    }
                }
                enemyAlive[i] = false;
                int flashSize = enemyIsTank.Count > i && enemyIsTank[i] ? boxSize + 20 :
                enemyCanShoot.Count > i && enemyCanShoot[i] ? boxSize + 8 :
                enemyIsRunner.Count > i && enemyIsRunner[i] ? boxSize - 8 : boxSize;
                deathFlashes.Add((enemies[i].x + flashSize / 2, enemies[i].y + flashSize / 2, 0.4f, 0.4f, flashSize));
                AddScreenShake(isTank ? 14f : canShoot ? 9f : isRunner ? 5f : 7f);
                // Death ragdoll fragments — pieces flying outward
                {
                    float fcx = enemies[i].x + boxSize / 2f;
                    float fcy = enemies[i].y + boxSize / 2f;
                    Color fragCol = isTank ? Color.DarkRed : canShoot ? Color.OrangeRed : isRunner ? Color.HotPink : Color.Red;
                    int nFrags = isTank ? 8 : 5;
                    for (int f = 0; f < nFrags; f++)
                    {
                        float a = (float)(rng.NextDouble() * Math.PI * 2);
                        float sp = 90f + (float)rng.NextDouble() * 140f;
                        float sz = boxSize * (0.18f + (float)rng.NextDouble() * 0.22f);
                        float ang = (float)(rng.NextDouble() * Math.PI * 2);
                        float av = (float)(rng.NextDouble() * 10f - 5f);
                        deathFragments.Add((fcx, fcy, MathF.Cos(a) * sp, MathF.Sin(a) * sp, ang, av, 0.7f, 0.7f, fragCol, sz));
                    }
                }
                // Combo / kill-streak
                comboCount++;
                comboTimer = 0f;
                if (comboCount > bestCombo) bestCombo = comboCount;
                comboShake = 1f;
                enemyRespawnTimers[i] = enemyRespawnTime;
                totalKills++;
                health = Math.Min(health + lifeSteal, maxHealth);
                if (isMultiplayer && !p2Dead) p2Health = Math.Min(p2Health + lifeSteal, p2MaxHealth);
                if (i < enemyIsParasitic.Count && enemyIsParasitic[i] && !parasiteDecayKill)
                {
                    for (int p = 0; p < 3; p++)
                    {
                        float angle = (float)(p * Math.PI * 2 / 3 + rng.NextDouble() * 0.5f);
                        float velX = (float)Math.Cos(angle) * parasiteSpeed;
                        float velY = (float)Math.Sin(angle) * parasiteSpeed;
                        parasites.Add((enemies[i].x + boxSize / 2, enemies[i].y + boxSize / 2, velX, velY, parasiteDuration, 0.5f, 0f));
                    }
                }
            }
        }

        private void DamageBoss(float dmg)
        {
            if (!bossAlive) return;
            bossHealth -= dmg;
            if (bossHealth <= 0) HandleBossDefeated();
        }

        private bool BossOverlaps(float x, float y, float w, float h)
        {
            if (!bossAlive) return false;
            float bs = bossSize * scale;
            return x < bossX + bs && x + w > bossX && y < bossY + bs && y + h > bossY;
        }

        private void HandleBossDefeated()
        {
            bossAlive = false;
            int waveSize = 20 + bossesDefeated * 5;
            for (int w = 0; w < waveSize; w++)
            {
                bool wCanShoot = rng.NextDouble() < shootingEnemyChance;
                bool wIsTank = !wCanShoot && rng.NextDouble() < tankEnemyChance;
                bool wIsRunner = !wCanShoot && !wIsTank && rng.NextDouble() < runnerEnemyChance;
                bool wIsParasitic = rng.NextDouble() < parasiticEnemyChance;
                int side = rng.Next(4);
                float ex, ey;
                switch (side)
                {
                    case 0: ex = 0; ey = rng.Next(0, ClientSize.Height); break;
                    case 1: ex = ClientSize.Width; ey = rng.Next(0, ClientSize.Height); break;
                    case 2: ex = rng.Next(0, ClientSize.Width); ey = 0; break;
                    default: ex = rng.Next(0, ClientSize.Width); ey = ClientSize.Height; break;
                }
                enemies.Add((ex, ey));
                enemyAlive.Add(true);
                enemyRespawnTimers.Add(0f);
                enemyCanShoot.Add(wCanShoot);
                enemyIsTank.Add(wIsTank);
                enemyIsRunner.Add(wIsRunner);
                enemyIsParasitic.Add(wIsParasitic);
                enemyShootTimers.Add(0f);
                enemyFlameTimers.Add(0f);
                SyncEnemyLists();
                enemyHealth.Add(wIsTank ? 8f : wCanShoot ? 4f : wIsRunner ? 1f : 2f);
            }
            bossesDefeated++;
            bossesDefeatedOnDifficulty++;
            currentBossMaxHealth += 25f;
            currentBossShootRate = Math.Max(0.5f, currentBossShootRate - 0.2f);
            float moneyToGive = 500 * scoreMultiplier;
            score += (int)(scoreMultiplier);
            totalScore += (int)(scoreMultiplier);
            health = maxHealth;
            totalKills++;
            for (int c = 0; c < 10; c++)
                coins.Add((bossX + bossSize * scale / 2, bossY + bossSize * scale / 2, 0f, 0f));
            buffMessage = "💀 BOSS DEFEATED! +$" + moneyToGive + " " + maxHealth + "hp";
            buffMessageTimer = 3f;
            bossShootTimer = 0f;
            if (!sandboxMode)
            {
                // Defeating a boss unlocks the next difficulty
                int nextDiff = difficulty + 1;
                if (nextDiff <= 8 && nextDiff > highestUnlockedDifficulty)
                {
                    highestUnlockedDifficulty = nextDiff;
                    pendingUnlockAnimation = nextDiff;
                }
            }
            SaveDifficultyUnlocks();
            if (difficulty > highestUnlockedDifficulty) highestUnlockedDifficulty = difficulty;
            for (int r = 0; r < enemyRespawnTimers.Count; r++)
            {
                if (enemyRespawnTimers[r] > 0)
                    enemyRespawnTimers[r] = Math.Min(enemyRespawnTimers[r], 3f);
            }
        }

        private void HandlePlayerDeath()
        {
            // Combo ends on death
            comboCount = 0; comboTimer = 0f; comboShake = 0f;
            if (isMultiplayer)
            {
                health = 0;
                if (isNetHost)
                {
                    // Mark host as dead — game continues if client is alive
                    hostDead = true;
                    if (p2Dead)
                    {
                        netManager?.SendGameOver();
                        HandleMultiplayerGameOver();
                    }
                }
                else
                {
                    // Mark client as dead — game continues if host is alive
                    hostDead = true; // "hostDead" on client = "I am dead"
                    if (p2Dead)
                        HandleMultiplayerGameOver();
                }
                return;
            }

            parasites.Clear();
            health = 0;
            isPaused = true;
            ResetEnemies();
            enemySpawnTimer = 0f;
            runHistory.Insert(0, (totalScore, totalKills, timeAlive, difficulty, sandboxMode, false));
            if (runHistory.Count > maxRunHistory) runHistory.RemoveAt(runHistory.Count - 1);
            SaveRunHistory();
            bool retry = ShowDeathScreen();
            lastTick = DateTime.Now;
            int savedUnlock = pendingUnlockAnimation;
            if (retry)
            {
                isPaused = false;
                ApplyDifficulty();
                ResetGame();
                pendingUnlockAnimation = savedUnlock;
            }
            else
            {
                ResetGame();
                ApplyDifficulty();
                pendingUnlockAnimation = savedUnlock;
                ShowMainMenu();
            }
        }

        private bool handlingMpGameOver = false;
        private void HandleMultiplayerGameOver()
        {
            if (handlingMpGameOver) return;
            handlingMpGameOver = true;
            // Close upgrade menu if open
            if (activeUpgradePanel != null)
            {
                this.Controls.Remove(activeUpgradePanel);
                activeUpgradePanel.Dispose();
                activeUpgradePanel = null;
            }
            parasites.Clear();
            health = 0;
            isPaused = true;
            ResetEnemies();
            enemySpawnTimer = 0f;
            runHistory.Insert(0, (totalScore, totalKills, timeAlive, difficulty, sandboxMode, true));
            if (runHistory.Count > maxRunHistory) runHistory.RemoveAt(runHistory.Count - 1);
            SaveRunHistory();
            bool retry = ShowDeathScreen();
            lastTick = DateTime.Now;
            int savedUnlock = pendingUnlockAnimation;
            hostDead = false;
            p2Dead = false;

            if (retry && netManager != null)
            {
                // Stay in multiplayer — show ready-up overlay
                isPaused = true;
                ApplyDifficulty();
                ResetGame();
                pendingUnlockAnimation = savedUnlock;
                ShowMultiplayerReadyUp();
            }
            else
            {
                // Quit to menu — disconnect
                isMultiplayer = false;
                handlingMpGameOver = false;
                if (netManager != null) { netManager.Disconnect(); netManager = null; }
                if (embeddedRelay != null) { embeddedRelay.Stop(); embeddedRelay = null; LanDiscovery.StopHost(); }
                ResetGame();
                ApplyDifficulty();
                pendingUnlockAnimation = savedUnlock;
                ShowMainMenu();
            }
        }

        private void ShowMultiplayerReadyUp()
        {
            showDimOverlay = true;
            this.Invalidate();

            var readyPanel = CreatePaintedOverlay(400, 250);

            float s = scale;
            Label title = new Label();
            title.Text = isNetHost ? "WAITING FOR PLAYER" : "WAITING FOR HOST";
            title.Font = new Font("Arial", Math.Max(1, (int)(16 * s)), FontStyle.Bold);
            title.ForeColor = Color.FromArgb(120, 160, 255);
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Size = new Size(readyPanel.Width - (int)(40 * s), (int)(35 * s));
            title.Location = new Point((int)(20 * s), (int)(20 * s));
            title.BackColor = Color.Transparent;
            readyPanel.Controls.Add(title);

            Label statusLbl = new Label();
            statusLbl.Text = "Press Ready when you want to go again";
            statusLbl.Font = new Font("Arial", Math.Max(1, (int)(10 * s)));
            statusLbl.ForeColor = Color.Gray;
            statusLbl.TextAlign = ContentAlignment.MiddleCenter;
            statusLbl.Size = new Size(readyPanel.Width - (int)(40 * s), (int)(25 * s));
            statusLbl.Location = new Point((int)(20 * s), (int)(60 * s));
            statusLbl.BackColor = Color.Transparent;
            readyPanel.Controls.Add(statusLbl);

            Button readyBtn = new Button();
            readyBtn.Text = "Ready";
            readyBtn.Size = new Size((int)(200 * s), (int)(44 * s));
            readyBtn.Location = new Point((readyPanel.Width - (int)(200 * s)) / 2, (int)(100 * s));
            readyBtn.BackColor = Color.FromArgb(40, 120, 40);
            readyBtn.ForeColor = Color.White;
            readyBtn.FlatStyle = FlatStyle.Flat;
            readyBtn.FlatAppearance.BorderColor = Color.FromArgb(60, 160, 60);
            readyBtn.Font = new Font("Arial", Math.Max(1, (int)(13 * s)), FontStyle.Bold);
            readyBtn.Cursor = Cursors.Hand;
            readyPanel.Controls.Add(readyBtn);

            Button quitBtn = new Button();
            quitBtn.Text = "Quit to Menu";
            quitBtn.Size = new Size((int)(200 * s), (int)(34 * s));
            quitBtn.Location = new Point((readyPanel.Width - (int)(200 * s)) / 2, (int)(155 * s));
            quitBtn.BackColor = Color.FromArgb(80, 30, 30);
            quitBtn.ForeColor = Color.White;
            quitBtn.FlatStyle = FlatStyle.Flat;
            quitBtn.Font = new Font("Arial", Math.Max(1, (int)(10 * s)));
            quitBtn.Cursor = Cursors.Hand;
            readyPanel.Controls.Add(quitBtn);

            bool closed = false;
            bool bothReady = false;
            bool localReady = false;
            bool remoteReady = false;

            readyBtn.Click += (ss, ee) =>
            {
                if (netManager == null) return;
                localReady = true;
                netManager.SendReady();
                readyBtn.Enabled = false;
                readyBtn.Text = "✔ Ready!";
                readyBtn.BackColor = Color.FromArgb(30, 80, 30);
                statusLbl.Text = isNetHost
                    ? $"Waiting for {p2Name} to ready up..."
                    : "Waiting for host to start...";
                statusLbl.ForeColor = Color.LimeGreen;
                if (localReady && remoteReady && isNetHost)
                {
                    bothReady = true;
                    closed = true;
                }
            };

            Action? onReady = null;
            Action? onLeft = null;
            Action? onStart = null;

            onReady = () =>
            {
                this.Invoke(() =>
                {
                    remoteReady = true;
                    statusLbl.Text = isNetHost
                        ? $"{p2Name} is ready!" + (localReady ? "" : " Press Ready!")
                        : "Host is ready!";
                    statusLbl.ForeColor = Color.LimeGreen;
                    if (localReady && remoteReady && isNetHost)
                    {
                        bothReady = true;
                        closed = true;
                    }
                });
            };
            onLeft = () =>
            {
                this.Invoke(() =>
                {
                    statusLbl.Text = "Player disconnected";
                    statusLbl.ForeColor = Color.Red;
                    readyBtn.Enabled = false;
                });
            };
            onStart = () =>
            {
                this.Invoke(() =>
                {
                    bothReady = true;
                    closed = true;
                });
            };

            netManager!.OnPlayerReady += onReady;
            netManager.OnPeerLeft += onLeft;
            if (!isNetHost) netManager.OnGameStartReceived += onStart;

            quitBtn.Click += (ss, ee) => closed = true;

            this.Controls.Add(readyPanel);
            readyPanel.BringToFront();
            AnimateZoomIn(readyPanel);
            while (!closed) Application.DoEvents();
            this.Controls.Remove(readyPanel);
            readyPanel.Dispose();
            showDimOverlay = false;
            this.Invalidate();

            // Unhook events
            netManager!.OnPlayerReady -= onReady;
            netManager.OnPeerLeft -= onLeft;
            if (!isNetHost) netManager.OnGameStartReceived -= onStart;

            handlingMpGameOver = false;
            if (bothReady)
            {
                if (isNetHost) netManager.SendGameStart();
                isPaused = false;
                hostDead = false;
                p2Dead = false;
                ResetGame();
                lastTick = DateTime.Now;
                this.Focus();
                if (isNetHost) { p2X = posX + 50; p2Y = posY; p2Health = maxHealth; p2MaxHealth = maxHealth; }
            }
            else
            {
                // Quit
                isMultiplayer = false;
                if (netManager != null) { netManager.Disconnect(); netManager = null; }
                if (embeddedRelay != null) { embeddedRelay.Stop(); embeddedRelay = null; LanDiscovery.StopHost(); }
                ResetGame();
                ApplyDifficulty();
                ShowMainMenu();
            }
        }

        private void ShowMainMenu()
        {
            lastTick = DateTime.Now;
            onMainMenu = true;
            isPaused = true;

            if (pendingUnlockAnimation >= 0)
            {
                TriggerUnlockAnimation(pendingUnlockAnimation);
                pendingUnlockAnimation = -1;
            }

            menuPlayerX = ClientSize.Width * 0.25f;
            menuPlayerY = ClientSize.Height * 0.6f;
            menuEnemyX = ClientSize.Width * 0.75f;
            menuEnemyY = ClientSize.Height * 0.6f;
            menuEnemyType = 0;
            menuEnemyHealth = 2f;
            menuEnemyMaxHealth = 2f;
            menuEnemyDead = false;
            menuEnemyDeadTimer = 0f;
            menuShootTimer = 0f;
            menuBulletActive = false;
            menuPlayBtn = new Button();
            menuPlayBtn.Text = "▶  Play";
            menuPlayBtn.Size = new Size(250, 55);
            menuPlayBtn.Location = new Point(ClientSize.Width / 2 - 125, ClientSize.Height / 2 + 20);
            menuPlayBtn.BackColor = Color.FromArgb(40, 100, 40);
            menuPlayBtn.ForeColor = Color.White;
            menuPlayBtn.FlatStyle = FlatStyle.Flat;
            menuPlayBtn.FlatAppearance.BorderColor = Color.FromArgb(60, 140, 60);
            menuPlayBtn.Font = new Font("Arial", 16, FontStyle.Bold);
            menuPlayBtn.Cursor = Cursors.Hand;
            menuPlayBtn.Click += (s, e) =>
            {
                menuPlayBtn.Visible = false;
                menuQuitBtn.Visible = false;
                menuPrefsBtn.Visible = false;

                List<Button> diffButtons = new List<Button>();
                Button backBtn2 = new Button();
                for (int d = 0; d < 9; d++)
                {
                    int captured = d;
                    bool locked = d > highestUnlockedDifficulty;
                    Button diffBtn = new Button();
                    diffBtn.Text = locked ? "🔒 " + DifficultyNames[d] : DifficultyStarNames[d];
                    diffBtn.Size = new Size(170, 42);
                    int col = d % 3;
                    int row = d / 3;
                    diffBtn.Location = new Point((int)(40 * scaleX) + col * 180, ClientSize.Height / 2 - 80 + row * 52);
                    diffBtn.BackColor = locked ? Color.FromArgb(40, 40, 40) : DifficultyBgColors[d];
                    diffBtn.ForeColor = locked ? Color.Gray : Color.White;
                    diffBtn.FlatStyle = FlatStyle.Flat;
                    diffBtn.FlatAppearance.BorderColor = locked ? Color.FromArgb(60, 60, 60) : Color.FromArgb(
                        Math.Min(255, DifficultyBgColors[d].R + 30),
                        Math.Min(255, DifficultyBgColors[d].G + 30),
                        Math.Min(255, DifficultyBgColors[d].B + 30));
                    diffBtn.Font = new Font("Arial", 11, FontStyle.Bold);
                    diffBtn.Cursor = locked ? Cursors.Default : Cursors.Hand;
                    diffBtn.Enabled = !locked;

                    diffBtn.Click += (s2, e2) =>
                    {
                        difficulty = captured;

                        // Show endless/sandbox choice
                        foreach (var db in diffButtons)
                            this.Controls.Remove(db);
                        this.Controls.Remove(backBtn2);

                        Button endlessBtn = new Button();
                        endlessBtn.Text = "⚔ Endless";
                        endlessBtn.Size = new Size(250, 55);
                        endlessBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2);
                        endlessBtn.BackColor = Color.FromArgb(40, 80, 140);
                        endlessBtn.ForeColor = Color.White;
                        endlessBtn.FlatStyle = FlatStyle.Flat;
                        endlessBtn.Font = new Font("Arial", 16, FontStyle.Bold);
                        endlessBtn.Cursor = Cursors.Hand;
                        endlessBtn.MouseEnter += (s3, e3) => endlessBtn.BackColor = Color.FromArgb(50, 100, 170);
                        endlessBtn.MouseLeave += (s3, e3) => endlessBtn.BackColor = Color.FromArgb(40, 80, 140);

                        Button sandboxBtn = new Button();
                        sandboxBtn.Text = "🧪 Sandbox";
                        sandboxBtn.Size = new Size(250, 55);
                        sandboxBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 70);
                        sandboxBtn.BackColor = Color.FromArgb(80, 60, 120);
                        sandboxBtn.ForeColor = Color.White;
                        sandboxBtn.FlatStyle = FlatStyle.Flat;
                        sandboxBtn.Font = new Font("Arial", 16, FontStyle.Bold);
                        sandboxBtn.Cursor = Cursors.Hand;
                        sandboxBtn.MouseEnter += (s3, e3) => sandboxBtn.BackColor = Color.FromArgb(100, 80, 150);
                        sandboxBtn.MouseLeave += (s3, e3) => sandboxBtn.BackColor = Color.FromArgb(80, 60, 120);

                        Button backBtn3 = new Button();
                        backBtn3.Text = "◀ Back";
                        backBtn3.Size = new Size(250, 35);
                        backBtn3.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 140);
                        backBtn3.BackColor = Color.FromArgb(60, 60, 60);
                        backBtn3.ForeColor = Color.White;
                        backBtn3.FlatStyle = FlatStyle.Flat;
                        backBtn3.Font = new Font("Arial", 12);
                        backBtn3.Cursor = Cursors.Hand;

                      

                        endlessBtn.Click += (s3, e3) =>
                        {
                            sandboxMode = false;
                            onMainMenu = false;
                            this.Controls.Remove(menuPlayBtn);
                            this.Controls.Remove(menuQuitBtn);
                            this.Controls.Remove(menuPrefsBtn);
                            this.Controls.Remove(endlessBtn);
                            this.Controls.Remove(sandboxBtn);
                            this.Controls.Remove(backBtn3);
                            this.Controls.Remove(menuHistoryBtn);
                            this.Controls.Remove(menuBestiaryBtn);
                            this.Controls.Remove(menuAchievementsBtn);
                            this.Controls.Remove(menuShopBtn);
                            this.Controls.Remove(menuMultiplayerBtn);
                            isPaused = false;
                            ApplyDifficulty();
                            ResetGame();
                        };

                        sandboxBtn.Click += (s3, e3) =>
                        {
                            sandboxMode = true;
                            onMainMenu = false;
                            this.Controls.Remove(menuPlayBtn);
                            this.Controls.Remove(menuQuitBtn);
                            this.Controls.Remove(menuPrefsBtn);
                            this.Controls.Remove(endlessBtn);
                            this.Controls.Remove(sandboxBtn);
                            this.Controls.Remove(backBtn3);
                            this.Controls.Remove(menuHistoryBtn);
                            this.Controls.Remove(menuBestiaryBtn);
                            this.Controls.Remove(menuAchievementsBtn);
                            this.Controls.Remove(menuShopBtn);
                            this.Controls.Remove(menuMultiplayerBtn);
                            isPaused = false;
                            ApplyDifficulty();
                            ResetGame();
                        };

                        backBtn3.Click += (s3, e3) =>
                        {
                            this.Controls.Remove(endlessBtn);
                            this.Controls.Remove(sandboxBtn);
                            this.Controls.Remove(backBtn3);
                            AnimateZoomInGroup(new Control[] { menuPlayBtn, menuQuitBtn, menuPrefsBtn, menuHistoryBtn!, menuBestiaryBtn!, menuAchievementsBtn!, menuShopBtn, menuMultiplayerBtn! });
                        };

                        this.Controls.Add(endlessBtn);
                        this.Controls.Add(sandboxBtn);
                        this.Controls.Add(backBtn3);
                        endlessBtn.BringToFront();
                        sandboxBtn.BringToFront();
                        backBtn3.BringToFront();
                    };

                    diffButtons.Add(diffBtn);
                    this.Controls.Add(diffBtn);
                    diffBtn.BringToFront();
                }

                backBtn2.Text = "◀ Back";
                backBtn2.Size = new Size(250, 35);
                backBtn2.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 - 80 + 3 * 52 + 10);
                backBtn2.BackColor = Color.FromArgb(60, 60, 60);
                backBtn2.ForeColor = Color.White;
                backBtn2.FlatStyle = FlatStyle.Flat;
                backBtn2.Font = new Font("Arial", 12);
                backBtn2.Cursor = Cursors.Hand;
                backBtn2.Click += (s2, e2) =>
                {
                    foreach (var db in diffButtons)
                        this.Controls.Remove(db);
                    this.Controls.Remove(backBtn2);
                    AnimateZoomInGroup(new Control[] { menuPlayBtn, menuQuitBtn, menuPrefsBtn, menuHistoryBtn!, menuBestiaryBtn!, menuAchievementsBtn!, menuShopBtn, menuMultiplayerBtn! });
                };
                this.Controls.Add(backBtn2);
                backBtn2.BringToFront();
            };
            menuPlayBtn.MouseEnter += (s, e) => menuPlayBtn.BackColor = Color.FromArgb(50, 130, 50);
            menuPlayBtn.MouseLeave += (s, e) => menuPlayBtn.BackColor = Color.FromArgb(40, 100, 40);

            menuQuitBtn = new Button();
            menuQuitBtn.Text = "✕  Quit";
            menuQuitBtn.Size = new Size(250, 55);
            menuQuitBtn.Location = new Point(ClientSize.Width / 2 - 125, ClientSize.Height / 2 + 90);
            menuQuitBtn.BackColor = Color.FromArgb(100, 40, 40);
            menuQuitBtn.ForeColor = Color.White;
            menuQuitBtn.FlatStyle = FlatStyle.Flat;
            menuQuitBtn.FlatAppearance.BorderColor = Color.FromArgb(140, 60, 60);
            menuQuitBtn.Font = new Font("Arial", 16, FontStyle.Bold);
            menuQuitBtn.Cursor = Cursors.Hand;
            menuQuitBtn.Click += (s, e) =>
            {
                isExiting = true;
                Application.Exit();
            };
            menuQuitBtn.MouseEnter += (s, e) => menuQuitBtn.BackColor = Color.FromArgb(130, 50, 50);
            menuQuitBtn.MouseLeave += (s, e) => menuQuitBtn.BackColor = Color.FromArgb(100, 40, 40);

            menuPrefsBtn = new Button();
            menuPrefsBtn.Text = "🎨 Preferences";
            menuPrefsBtn.Size = new Size(250, 55);
            menuPrefsBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 160);
            menuPrefsBtn.BackColor = Color.FromArgb(80, 60, 120);
            menuPrefsBtn.ForeColor = Color.White;
            menuPrefsBtn.FlatStyle = FlatStyle.Flat;
            menuPrefsBtn.FlatAppearance.BorderColor = Color.FromArgb(110, 80, 160);
            menuPrefsBtn.Font = new Font("Arial", 16, FontStyle.Bold);
            menuPrefsBtn.Cursor = Cursors.Hand;
            menuPrefsBtn.MouseEnter += (s, e) => menuPrefsBtn.BackColor = Color.FromArgb(100, 80, 150);
            menuPrefsBtn.MouseLeave += (s, e) => menuPrefsBtn.BackColor = Color.FromArgb(80, 60, 120);
            menuPrefsBtn.Click += (s, e) =>
            {
                onPreferences = true;
                menuPlayBtn.Visible = false;
                menuQuitBtn.Visible = false;
                menuPrefsBtn.Visible = false;
                int panelW = (int)(600 * scaleX);
                int panelH = (int)(500 * scaleY);
                int panelX = ClientSize.Width / 2 - panelW / 2;
                int panelY = ClientSize.Height / 2 - panelH / 2;

                menuPrefsBackBtn = new Button();
                menuPrefsBackBtn.Text = "◀ Back";
                menuPrefsBackBtn.Size = new Size(250, 40);
                menuPrefsBackBtn.Location = new Point(panelX + panelW / 2 - 125, panelY + panelH - (int)(60 * scaleY));
                menuPrefsBackBtn.BackColor = Color.FromArgb(60, 60, 60);
                menuPrefsBackBtn.ForeColor = Color.White;
                menuPrefsBackBtn.FlatStyle = FlatStyle.Flat;
                menuPrefsBackBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
                menuPrefsBackBtn.Font = new Font("Arial", 14);
                menuPrefsBackBtn.Cursor = Cursors.Hand;
                menuPrefsBackBtn.Click += (s2, e2) =>
                {
                    onPreferences = false;
                    AnimateZoomInGroup(new Control[] { menuPlayBtn, menuQuitBtn, menuPrefsBtn });
                    this.Controls.Remove(menuPrefsBackBtn);

                    // Remove color buttons
                    var toRemove = this.Controls.OfType<Control>()
                        .Where(c => c.Tag?.ToString() == "prefControl")
                        .ToList();
                    foreach (var c in toRemove)
                        this.Controls.Remove(c);
                };
                this.Controls.Add(menuPrefsBackBtn);
                menuPrefsBackBtn.BringToFront();

                // Add color buttons
                var presetColors = new[]
                {
        new { Name = "Blue",   Color = Color.FromArgb(0, 50, 255)   },
        new { Name = "Red",    Color = Color.FromArgb(220, 30, 30)  },
        new { Name = "Green",  Color = Color.FromArgb(30, 180, 30)  },
        new { Name = "Purple", Color = Color.FromArgb(140, 0, 220)  },
        new { Name = "Orange", Color = Color.FromArgb(255, 140, 0)  },
        new { Name = "Cyan",   Color = Color.FromArgb(0, 200, 220)  },
        new { Name = "Pink",   Color = Color.FromArgb(255, 80, 180) },
        new { Name = "Yellow", Color = Color.FromArgb(220, 200, 0)  },
        new { Name = "White",  Color = Color.FromArgb(240, 240, 240)},
        new { Name = "Black",  Color = Color.FromArgb(20, 20, 20)   },
        new { Name = "Gold",   Color = Color.FromArgb(212, 175, 55) },
        new { Name = "Teal",   Color = Color.FromArgb(0, 150, 130)  },
    };



                int colorX = panelX + (int)(20 * scaleX);
                int colorY = panelY + (int)(120 * scaleY);
                foreach (var preset in presetColors)
                {
                    var capturedColor = preset.Color;
                    Button colorBtn = new Button();
                    colorBtn.Size = new Size(60, 60);
                    colorBtn.Location = new Point(colorX, colorY);
                    colorBtn.BackColor = preset.Color;
                    colorBtn.FlatStyle = FlatStyle.Flat;
                    colorBtn.FlatAppearance.BorderColor = Color.White;
                    colorBtn.Cursor = Cursors.Hand;
                    colorBtn.Tag = "prefControl";
                    colorBtn.Click += (s2, e2) => { playerColor = capturedColor; };
                    colorBtn.MouseEnter += (s2, e2) => colorBtn.FlatAppearance.BorderSize = 3;
                    colorBtn.MouseLeave += (s2, e2) => colorBtn.FlatAppearance.BorderSize = 1;
                    this.Controls.Add(colorBtn);
                    colorBtn.BringToFront();
                    colorX += 70;
                    if (colorX > (int)(40 * scaleX) + 70 * 6)
                    {
                        colorX = (int)(40 * scaleX);
                        colorY += 70;
                    }
                }

                Button customBtn = new Button();
                customBtn.Text = "🎨 Custom";
                customBtn.Size = new Size(130, 40);
                customBtn.Location = new Point(panelX + (int)(20 * scaleX), panelY + (int)(290 * scaleY));
                customBtn.BackColor = Color.FromArgb(60, 60, 60);
                customBtn.ForeColor = Color.White;
                customBtn.FlatStyle = FlatStyle.Flat;
                customBtn.Tag = "prefControl";
                customBtn.Cursor = Cursors.Hand;
                customBtn.Click += (s2, e2) =>
                {
                    using ColorDialog cd = new ColorDialog();
                    cd.Color = playerColor;
                    if (cd.ShowDialog() == DialogResult.OK)
                        playerColor = cd.Color;
                };
                this.Controls.Add(customBtn);
                customBtn.BringToFront();

                // Name box
                TextBox nameBox = new TextBox();
                nameBox.Text = playerName;
                nameBox.Font = new Font("Arial", 14);
                nameBox.Size = new Size(200, 35);
                nameBox.Location = new Point(panelX + (int)(20 * scaleX), panelY + (int)(390 * scaleY));
                nameBox.MaxLength = 8;
                nameBox.BackColor = Color.FromArgb(50, 50, 50);
                nameBox.ForeColor = Color.White;
                nameBox.Tag = "prefControl";
                nameBox.TextChanged += (s2, e2) =>
                {
                    string input = nameBox.Text.Trim().ToUpper();
                    if (!string.IsNullOrWhiteSpace(input) && input != "YOU")
                    {
                        playerName = input;
                        nameBox.ForeColor = Color.White;
                    }
                    else
                    {
                        playerName = "YOU";
                        nameBox.ForeColor = Color.Red;
                    }
                    SavePlayerName();
                };
                this.Controls.Add(nameBox);
                nameBox.BringToFront();
            };
            this.Controls.Add(menuPrefsBtn);
            menuPrefsBtn.BringToFront();

            this.Controls.Add(menuPlayBtn);
            this.Controls.Add(menuQuitBtn);
            menuPlayBtn.BringToFront();
            menuQuitBtn.BringToFront();
            menuPlayBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2);
            menuQuitBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 70);

            EventHandler resizeHandler = null!;
            resizeHandler = (s, e) =>
            {
                menuPlayBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2);
                menuQuitBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 70);
                menuPrefsBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 160);
            };
            menuHistoryBtn = new Button();
            menuHistoryBtn.Text = "📜 Run History";
            // ... rest of setup
            menuHistoryBtn.Size = new Size(250, 45);
            menuHistoryBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 230);
            menuHistoryBtn.BackColor = Color.FromArgb(50, 50, 70);
            menuHistoryBtn.ForeColor = Color.White;
            menuHistoryBtn.FlatStyle = FlatStyle.Flat;
            menuHistoryBtn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 100);
            menuHistoryBtn.Font = new Font("Arial", 13, FontStyle.Bold);
            menuHistoryBtn.Cursor = Cursors.Hand;
            menuHistoryBtn.MouseEnter += (s, e) => menuHistoryBtn.BackColor = Color.FromArgb(70, 70, 100);
            menuHistoryBtn.MouseLeave += (s, e) => menuHistoryBtn.BackColor = Color.FromArgb(50, 50, 70);
            menuHistoryBtn.Click += (s, e) => ShowRunHistory();
            this.Controls.Add(menuHistoryBtn);
            menuHistoryBtn.BringToFront();

            menuBestiaryBtn = new Button();
            menuBestiaryBtn.Text = "📖 Bestiary";
            menuBestiaryBtn.Size = new Size(250, 45);
            menuBestiaryBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 285);
            menuBestiaryBtn.BackColor = Color.FromArgb(50, 50, 70);
            menuBestiaryBtn.ForeColor = Color.White;
            menuBestiaryBtn.FlatStyle = FlatStyle.Flat;
            menuBestiaryBtn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 100);
            menuBestiaryBtn.Font = new Font("Arial", 13, FontStyle.Bold);
            menuBestiaryBtn.Cursor = Cursors.Hand;
            menuBestiaryBtn.MouseEnter += (s, e) => menuBestiaryBtn.BackColor = Color.FromArgb(70, 70, 100);
            menuBestiaryBtn.MouseLeave += (s, e) => menuBestiaryBtn.BackColor = Color.FromArgb(50, 50, 70);
            menuBestiaryBtn.Click += (s, e) => ShowBestiary();
            this.Controls.Add(menuBestiaryBtn);
            menuBestiaryBtn.BringToFront();

            menuAchievementsBtn = new Button();
            menuAchievementsBtn.Text = "🏆 Achievements";
            menuAchievementsBtn.Size = new Size(250, 45);
            menuAchievementsBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 340);
            menuAchievementsBtn.BackColor = Color.FromArgb(50, 50, 70);
            menuAchievementsBtn.ForeColor = Color.White;
            menuAchievementsBtn.FlatStyle = FlatStyle.Flat;
            menuAchievementsBtn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 100);
            menuAchievementsBtn.Font = new Font("Arial", 13, FontStyle.Bold);
            menuAchievementsBtn.Cursor = Cursors.Hand;
            menuAchievementsBtn.MouseEnter += (s, e) => menuAchievementsBtn.BackColor = Color.FromArgb(70, 70, 100);
            menuAchievementsBtn.MouseLeave += (s, e) => menuAchievementsBtn.BackColor = Color.FromArgb(50, 50, 70);
            menuAchievementsBtn.Click += (s, e) => ShowAchievements();
            this.Controls.Add(menuAchievementsBtn);
            menuAchievementsBtn.BringToFront();

            menuShopBtn = new Button();
            menuShopBtn.Text = "🔴 Red Coin Shop";
            menuShopBtn.Size = new Size(250, 45);
            menuShopBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 395);
            menuShopBtn.BackColor = Color.FromArgb(80, 30, 30);
            menuShopBtn.ForeColor = Color.White;
            menuShopBtn.FlatStyle = FlatStyle.Flat;
            menuShopBtn.FlatAppearance.BorderColor = Color.FromArgb(120, 50, 50);
            menuShopBtn.Font = new Font("Arial", 13, FontStyle.Bold);
            menuShopBtn.Cursor = Cursors.Hand;
            menuShopBtn.MouseEnter += (s, e) => menuShopBtn.BackColor = Color.FromArgb(100, 40, 40);
            menuShopBtn.MouseLeave += (s, e) => menuShopBtn.BackColor = Color.FromArgb(80, 30, 30);
            menuShopBtn.Click += (s, e) => ShowRedCoinShop();
            this.Controls.Add(menuShopBtn);
            menuShopBtn.BringToFront();

            menuMultiplayerBtn = new Button();
            menuMultiplayerBtn.Text = "🌐 Multiplayer";
            menuMultiplayerBtn.Size = new Size(250, 45);
            menuMultiplayerBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 450);
            menuMultiplayerBtn.BackColor = Color.FromArgb(40, 80, 120);
            menuMultiplayerBtn.ForeColor = Color.White;
            menuMultiplayerBtn.FlatStyle = FlatStyle.Flat;
            menuMultiplayerBtn.FlatAppearance.BorderColor = Color.FromArgb(60, 110, 160);
            menuMultiplayerBtn.Font = new Font("Arial", 13, FontStyle.Bold);
            menuMultiplayerBtn.Cursor = Cursors.Hand;
            menuMultiplayerBtn.MouseEnter += (s, e) => menuMultiplayerBtn.BackColor = Color.FromArgb(50, 100, 150);
            menuMultiplayerBtn.MouseLeave += (s, e) => menuMultiplayerBtn.BackColor = Color.FromArgb(40, 80, 120);
            menuMultiplayerBtn.Click += (s, e) => ShowMultiplayerMenu();
            this.Controls.Add(menuMultiplayerBtn);
            menuMultiplayerBtn.BringToFront();
            this.ClientSizeChanged += resizeHandler;
            menuPlayBtn.Click += (s, e) => this.ClientSizeChanged -= resizeHandler;
            menuQuitBtn.Click += (s, e) => this.ClientSizeChanged -= resizeHandler;
            menuPrefsBtn.Click += (s, e) => this.ClientSizeChanged -= resizeHandler;

            // Zoom-in the whole main menu group on first show
            AnimateZoomInGroup(new Control[] {
                menuPlayBtn, menuQuitBtn, menuPrefsBtn,
                menuHistoryBtn, menuBestiaryBtn, menuAchievementsBtn,
                menuShopBtn, menuMultiplayerBtn
            });
        }
        private void ApplyDifficulty()
        {
            // Gradual progression across 9 difficulty levels
            float[] speeds =     { 3.0f, 3.5f, 5.0f, 5.3f, 5.6f, 6.0f, 6.3f, 6.7f, 7.0f };
            float[] damages =    { 0.5f, 0.6f, 1.0f, 1.1f, 1.3f, 1.5f, 1.7f, 1.85f, 2.0f };
            float[] bossTimers = { 180f, 165f, 120f, 110f, 100f, 90f,  80f,  70f,  60f };
            float[] bossHps =    { 150f, 180f, 300f, 325f, 350f, 400f, 430f, 465f, 500f };
            float[] bossRates =  { 2.5f, 2.4f, 2.0f, 1.95f, 1.9f, 1.8f, 1.7f, 1.6f, 1.5f };
            float[] scoreMults = { 3.0f, 2.5f, 1.0f, 1.2f, 1.5f, 2.0f, 2.0f, 2.0f, 2.0f };
            float[] parasitic =  { 0f,   0f,   0.01f, 0.02f, 0.03f, 0.05f, 0.07f, 0.085f, 0.1f };

            int d = Math.Clamp(difficulty, 0, 8);
            currentEnemySpeed = speeds[d] * scale;
            enemyDamage = damages[d];
            bossSpawnInterval_Current = bossTimers[d];
            currentBossMaxHealth = bossHps[d];
            currentBossShootRate = bossRates[d];
            scoreMultiplier = scoreMults[d];
            parasiticEnemyChance = parasitic[d];
        }
        private static string GetSaveDir()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RedGuyTakeover");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private string GetSavePath()
        {
            return Path.Combine(GetSaveDir(), "saves.dat");
        }

        private void SaveDifficultyUnlocks()
        {
            string saveData = $"LEVEL:{highestUnlockedDifficulty}";
            File.WriteAllText(GetSavePath(), saveData);
        }

        private void LoadDifficultyUnlocks()
        {
            string path = GetSavePath();
            if (!File.Exists(path)) return;
            string saveData = File.ReadAllText(path);
            // Try new format first
            var match = System.Text.RegularExpressions.Regex.Match(saveData, @"LEVEL:(\d+)");
            if (match.Success)
            {
                highestUnlockedDifficulty = Math.Clamp(int.Parse(match.Groups[1].Value), 0, 8);
            }
            else
            {
                // Legacy format: map old 4-level keywords conservatively
                // so old players still have new difficulties to earn
                if (saveData.Contains("OBLIVION")) highestUnlockedDifficulty = 5;      // had Nightmare → unlock up to Hard
                else if (saveData.Contains("PHANTOM")) highestUnlockedDifficulty = 3;   // had Hard → unlock up to Moderate
                else if (saveData.Contains("CRIMSON")) highestUnlockedDifficulty = 1;   // had Normal → unlock up to Beginner
                else highestUnlockedDifficulty = 0;
            }
        }

        private void SavePlayerName()
        {
            try { File.WriteAllText(Path.Combine(GetSaveDir(), "playername.dat"), playerName); }
            catch { }
        }

        private void LoadPlayerName()
        {
            try
            {
                string path = Path.Combine(GetSaveDir(), "playername.dat");
                if (File.Exists(path))
                {
                    string name = File.ReadAllText(path).Trim().ToUpper();
                    if (!string.IsNullOrWhiteSpace(name) && name != "YOU")
                        playerName = name;
                }
            }
            catch { }
        }
        private Button? pauseResumeBtn = null;
        private void ShowPauseButtons()
        {
            // Snapshot + downscale-blur the current frame for the pause backdrop
            try
            {
                int cw = Math.Max(1, ClientSize.Width);
                int ch = Math.Max(1, ClientSize.Height);
                using var fullBmp = new Bitmap(cw, ch);
                this.DrawToBitmap(fullBmp, new Rectangle(0, 0, cw, ch));
                int dw = Math.Max(1, cw / 14);
                int dh = Math.Max(1, ch / 14);
                var small = new Bitmap(dw, dh);
                using (var sg = Graphics.FromImage(small))
                {
                    sg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                    sg.DrawImage(fullBmp, 0, 0, dw, dh);
                }
                pauseBlurFrame?.Dispose();
                pauseBlurFrame = small;
            }
            catch { }

            pauseResumeBtn = new Button();
            pauseResumeBtn.Text = "▶ Resume";
            pauseResumeBtn.Size = new Size(200, 45);
            pauseResumeBtn.Location = new Point(ClientSize.Width / 2 - 100, ClientSize.Height / 2 + 10);
            pauseResumeBtn.BackColor = Color.FromArgb(40, 100, 40);
            pauseResumeBtn.ForeColor = Color.White;
            pauseResumeBtn.FlatStyle = FlatStyle.Flat;
            pauseResumeBtn.FlatAppearance.BorderColor = Color.FromArgb(60, 140, 60);
            pauseResumeBtn.Font = new Font("Arial", 12, FontStyle.Bold);
            pauseResumeBtn.Cursor = Cursors.Hand;
            pauseResumeBtn.MouseEnter += (s, e) => { if (pauseResumeBtn != null) pauseResumeBtn.BackColor = Color.FromArgb(50, 130, 50); };
            pauseResumeBtn.MouseLeave += (s, e) => { if (pauseResumeBtn != null) pauseResumeBtn.BackColor = Color.FromArgb(40, 100, 40); };
            pauseResumeBtn.Click += (s, e) =>
            {
                HidePauseButtons();
                isPaused = false;
            };


            pauseQuitBtn = new Button();
            pauseQuitBtn.Text = "✕ Quit to Menu";
            pauseQuitBtn.Size = new Size(200, 45);
            pauseQuitBtn.Location = new Point(ClientSize.Width / 2 - 100, ClientSize.Height / 2 + 60);
            pauseQuitBtn.BackColor = Color.FromArgb(100, 40, 40);
            pauseQuitBtn.ForeColor = Color.White;
            pauseQuitBtn.FlatStyle = FlatStyle.Flat;
            pauseQuitBtn.FlatAppearance.BorderColor = Color.FromArgb(140, 60, 60);
            pauseQuitBtn.Font = new Font("Arial", 12, FontStyle.Bold);
            pauseQuitBtn.Cursor = Cursors.Hand;
            pauseQuitBtn.MouseEnter += (s, e) => { if (pauseQuitBtn != null) pauseQuitBtn.BackColor = Color.FromArgb(130, 50, 50); };
            pauseQuitBtn.MouseLeave += (s, e) => { if (pauseQuitBtn != null) pauseQuitBtn.BackColor = Color.FromArgb(100, 40, 40); };
            pauseQuitBtn.Click += (s, e) =>
            {
                HidePauseButtons();
                isPaused = false;
                if (isMultiplayer)
                {
                    isMultiplayer = false;
                    hostDead = false;
                    p2Dead = false;
                    if (netManager != null) { netManager.Disconnect(); netManager = null; }
                    if (embeddedRelay != null) { embeddedRelay.Stop(); embeddedRelay = null; }
                }
                int savedUnlock = pendingUnlockAnimation;
                ResetGame();
                pendingUnlockAnimation = savedUnlock;
                ShowMainMenu();
            };
            this.Controls.Add(pauseResumeBtn);
            this.Controls.Add(pauseQuitBtn);
            pauseResumeBtn.BringToFront();
            pauseQuitBtn.BringToFront();
            AnimateZoomInGroup(new Control[] { pauseResumeBtn, pauseQuitBtn });
        }

        private void HidePauseButtons()
        {
            if (pauseResumeBtn != null)
            {
                this.Controls.Remove(pauseResumeBtn);
                pauseResumeBtn = null;
            }
            if (pauseQuitBtn != null)
            {
                this.Controls.Remove(pauseQuitBtn);
                pauseQuitBtn = null;
            }
            if (pauseBlurFrame != null)
            {
                pauseBlurFrame.Dispose();
                pauseBlurFrame = null;
            }
        }
        private void TriggerUnlockAnimation(int diffIndex)
        {
            unlockedDifficultyIndex = diffIndex;
            showingUnlockAnimation = true;
            unlockAnimTimer = unlockAnimDuration;
            unlockParticles.Clear();

            for (int i = 0; i < 80; i++)
            {
                float angle = (float)(rng.NextDouble() * Math.PI * 2);
                float speed = (float)(rng.NextDouble() * 400 + 100);
                Color dc = unlockedDifficultyIndex >= 0 && unlockedDifficultyIndex < 9
                    ? DifficultyColors[unlockedDifficultyIndex] : Color.White;
                Color[] colors = new[] { dc, Color.FromArgb(Math.Min(255, dc.R + 60), Math.Min(255, dc.G + 60), Math.Min(255, dc.B + 60)), Color.White };
                unlockParticles.Add((
                    ClientSize.Width / 2f,
                    ClientSize.Height / 2f,
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed,
                    (float)(rng.NextDouble() * 2 + 1),
                    colors[rng.Next(colors.Length)]
                ));
            }
        }
        private void SyncEnemyLists()
        {
            while (enemyAlive.Count < enemies.Count) enemyAlive.Add(true);
            while (enemyRespawnTimers.Count < enemies.Count) enemyRespawnTimers.Add(0f);
            while (enemySmackCooldown.Count < enemies.Count) enemySmackCooldown.Add(0f);
            while (enemyAimAngle.Count < enemies.Count) enemyAimAngle.Add(0f);
            while (enemyCanShoot.Count < enemies.Count) enemyCanShoot.Add(rng.NextDouble() < shootingEnemyChance);
            while (enemyIsTank.Count < enemies.Count) enemyIsTank.Add(false);
            while (enemyIsRunner.Count < enemies.Count) enemyIsRunner.Add(false);
            while (enemyIsParasitic.Count < enemies.Count) enemyIsParasitic.Add(rng.NextDouble() < parasiticEnemyChance);
            while (enemyShootTimers.Count < enemies.Count) enemyShootTimers.Add(0f);
            while (enemyFlameTimers.Count < enemies.Count) enemyFlameTimers.Add(0f);
            while (enemyHealth.Count < enemies.Count) enemyHealth.Add(2f);
            while (enemyIsFrenzied.Count < enemies.Count) { enemyIsFrenzied.Add(false); }
            while (enemyIsPhasing.Count < enemies.Count) { enemyIsPhasing.Add(false); }
            while (enemyIsZigzag.Count < enemies.Count) { enemyIsZigzag.Add(false); }
            while (enemyIsCharging.Count < enemies.Count) { enemyIsCharging.Add(false); }
            while (enemyIsArmored.Count < enemies.Count) { enemyIsArmored.Add(false); }
            while (enemyIsRegenerating.Count < enemies.Count) { enemyIsRegenerating.Add(false); }
            while (enemyIsReflective.Count < enemies.Count) { enemyIsReflective.Add(false); }
            while (enemyIsBerserker.Count < enemies.Count) { enemyIsBerserker.Add(false); }
            while (enemyIsCorrupted.Count < enemies.Count) { enemyIsCorrupted.Add(false); }
            while (enemyArmorBroken.Count < enemies.Count) { enemyArmorBroken.Add(false); }
            while (enemyChargeCooldown.Count < enemies.Count) { enemyChargeCooldown.Add(3f); }
            while (enemyChargeTimer.Count < enemies.Count) { enemyChargeTimer.Add(0f); }
            while (enemyIsCharging_Active.Count < enemies.Count) { enemyIsCharging_Active.Add(false); }
            while (enemyChargeVelX.Count < enemies.Count) { enemyChargeVelX.Add(0f); }
            while (enemyChargeVelY.Count < enemies.Count) { enemyChargeVelY.Add(0f); }
            while (enemyFrenziedAngle.Count < enemies.Count) { enemyFrenziedAngle.Add(0f); }
            while (enemyZigzagTimer.Count < enemies.Count) { enemyZigzagTimer.Add(0f); }
            while (enemyZigzagDirection.Count < enemies.Count) { enemyZigzagDirection.Add(1f); }
            while (enemyPhasingTimer.Count < enemies.Count) { enemyPhasingTimer.Add(0f); }
            while (enemyIsVisible.Count < enemies.Count) { enemyIsVisible.Add(true); }
        }
        private bool RollEffect(int minDifficulty)
        {
            if (difficulty < minDifficulty) return false;
            // Gradual effect chances: 0%,0%,5%,6%,8%,10%,12%,13%,15%
            float[] effectChances = { 0f, 0f, 0.05f, 0.06f, 0.08f, 0.10f, 0.12f, 0.13f, 0.15f };
            float chance = effectChances[Math.Clamp(difficulty, 0, 8)];
            return rng.NextDouble() < chance;
        }
        private void InitEnemyEffects(int i)
        {
            while (enemyIsFrenzied.Count <= i) enemyIsFrenzied.Add(false);
            while (enemyIsPhasing.Count <= i) enemyIsPhasing.Add(false);
            while (enemyIsZigzag.Count <= i) enemyIsZigzag.Add(false);
            while (enemyIsCharging.Count <= i) enemyIsCharging.Add(false);
            while (enemyIsArmored.Count <= i) enemyIsArmored.Add(false);
            while (enemyIsRegenerating.Count <= i) enemyIsRegenerating.Add(false);
            while (enemyIsReflective.Count <= i) enemyIsReflective.Add(false);
            while (enemyIsBerserker.Count <= i) enemyIsBerserker.Add(false);
            while (enemyIsCorrupted.Count <= i) enemyIsCorrupted.Add(false);
            while (enemyArmorBroken.Count <= i) enemyArmorBroken.Add(false);
            while (enemyChargeCooldown.Count <= i) enemyChargeCooldown.Add(0f);
            while (enemyChargeTimer.Count <= i) enemyChargeTimer.Add(0f);
            while (enemyIsCharging_Active.Count <= i) enemyIsCharging_Active.Add(false);
            while (enemyChargeVelX.Count <= i) enemyChargeVelX.Add(0f);
            while (enemyChargeVelY.Count <= i) enemyChargeVelY.Add(0f);
            while (enemyFrenziedAngle.Count <= i) enemyFrenziedAngle.Add(0f);
            while (enemyZigzagTimer.Count <= i) enemyZigzagTimer.Add(0f);
            while (enemyZigzagDirection.Count <= i) enemyZigzagDirection.Add(1f);
            while (enemyPhasingTimer.Count <= i) enemyPhasingTimer.Add(0f);
            while (enemyIsVisible.Count <= i) enemyIsVisible.Add(true);

            enemyIsFrenzied[i] = RollEffect(2);
            enemyIsZigzag[i] = !enemyIsFrenzied[i] && RollEffect(4);
            enemyIsCharging[i] = !enemyIsFrenzied[i] && !enemyIsZigzag[i] && RollEffect(3);
            enemyIsArmored[i] = RollEffect(4);
            enemyIsRegenerating[i] = RollEffect(5);
            enemyIsReflective[i] = RollEffect(6);
            enemyIsBerserker[i] = RollEffect(2);
            enemyIsPhasing[i] = RollEffect(6);
            enemyIsCorrupted[i] = RollEffect(7);
            enemyArmorBroken[i] = false;
            enemyChargeCooldown[i] = 3f;
            enemyChargeTimer[i] = 0f;
            enemyIsCharging_Active[i] = false;
            enemyFrenziedAngle[i] = (float)(rng.NextDouble() * Math.PI * 2);
            enemyZigzagTimer[i] = 0f;
            enemyZigzagDirection[i] = 1f;
            enemyPhasingTimer[i] = 0f;
            enemyIsVisible[i] = true;
        }
        private void SaveRunHistory()
        {
            try
            {
                var lines = runHistory.Select(r =>
                    $"{r.score}|{r.kills}|{r.time}|{r.difficulty}|{r.sandbox}|{r.multiplayer}");
                File.WriteAllLines(Path.Combine(GetSaveDir(), "history.dat"),
                    lines);
            }
            catch { }
        }

        private void LoadRunHistory()
        {
            try
            {
                string path = Path.Combine(GetSaveDir(), "history.dat");
                if (!File.Exists(path)) return;
                runHistory.Clear();
                foreach (var line in File.ReadAllLines(path))
                {
                    var parts = line.Split('|');
                    if (parts.Length == 5)
                        runHistory.Add((float.Parse(parts[0]), int.Parse(parts[1]),
                            float.Parse(parts[2]), int.Parse(parts[3]), bool.Parse(parts[4]), false));
                    else if (parts.Length == 6)
                        runHistory.Add((float.Parse(parts[0]), int.Parse(parts[1]),
                            float.Parse(parts[2]), int.Parse(parts[3]), bool.Parse(parts[4]), bool.Parse(parts[5])));
                }
            }
            catch { }
        }

        private void SaveBeastiary()
        {
            try
            {
                var lines = beastiaryKills.Select(kv => $"{kv.Key}|{kv.Value}");
                File.WriteAllLines(Path.Combine(GetSaveDir(), "bestiary.dat"),
                    lines);
            }
            catch { }
        }

        private void LoadBeastiary()
        {
            try
            {
                string path = Path.Combine(GetSaveDir(), "bestiary.dat");
                if (!File.Exists(path)) return;
                foreach (var line in File.ReadAllLines(path))
                {
                    var parts = line.Split('|');
                    if (parts.Length == 2 && beastiaryKills.ContainsKey(parts[0]))
                        beastiaryKills[parts[0]] = int.Parse(parts[1]);
                }
                beastiaryUnlocked = beastiaryKills.Values.Sum() > 0;
            }
            catch { }
        }

        private void SaveAchievements()
        {
            try
            {
                File.WriteAllLines(
                    Path.Combine(GetSaveDir(), "achievements.dat"),
                    unlockedAchievements);
            }
            catch { }
        }

        private void LoadAchievements()
        {
            try
            {
                string path = Path.Combine(GetSaveDir(), "achievements.dat");
                if (!File.Exists(path)) return;
                foreach (var line in File.ReadAllLines(path))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        unlockedAchievements.Add(line.Trim());
                }
            }
            catch { }
        }

        private void SaveRedCoins()
        {
            try
            {
                File.WriteAllText(Path.Combine(GetSaveDir(), "redcoins.dat"),
                    $"{redCoins}|{permSpeedLevel}|{permDamageLevel}|{permBulletSpeedLevel}");
            }
            catch { }
        }

        private void LoadRedCoins()
        {
            try
            {
                string path = Path.Combine(GetSaveDir(), "redcoins.dat");
                if (!File.Exists(path)) return;
                string data = File.ReadAllText(path).Trim();
                var parts = data.Split('|');
                if (parts.Length >= 1 && int.TryParse(parts[0], out int val)) redCoins = val;
                if (parts.Length >= 2 && int.TryParse(parts[1], out int sp)) permSpeedLevel = sp;
                if (parts.Length >= 3 && int.TryParse(parts[2], out int dm)) permDamageLevel = dm;
                if (parts.Length >= 4 && int.TryParse(parts[3], out int bs)) permBulletSpeedLevel = bs;
            }
            catch { }
        }

        private int GetUpgradeCost(int basePrice, int level)
        {
            // Price increases by 50% each level (rounded up)
            int cost = basePrice;
            for (int i = 0; i < level; i++)
                cost = (int)Math.Ceiling(cost * 1.5);
            return cost;
        }

        private void ShowRedCoinShop()
        {
            var shopPanel = CreatePaintedOverlay(450, 380);
            shopPanel.BackColor = Color.FromArgb(20, 20, 30);
            float s = scale;

            Label title = new Label();
            title.Text = $"🔴 RED COIN SHOP — {redCoins} coins";
            title.Font = new Font("Arial", Math.Max(1, (int)(15 * s)), FontStyle.Bold);
            title.ForeColor = Color.FromArgb(220, 60, 60);
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Size = new Size(shopPanel.Width - (int)(20 * s), (int)(35 * s));
            title.Location = new Point((int)(10 * s), (int)(10 * s));
            shopPanel.Controls.Add(title);

            int yPos = (int)(55 * s);
            int btnW = shopPanel.Width - (int)(40 * s);
            int btnH = (int)(60 * s);

            // Speed upgrade
            int speedCost = GetUpgradeCost(5, permSpeedLevel);
            Button speedBtn = new Button();
            speedBtn.Text = $"🏃 Speed +0.2  (Lv {permSpeedLevel})  —  🔴 {speedCost}";
            speedBtn.Size = new Size(btnW, btnH);
            speedBtn.Location = new Point((int)(20 * s), yPos);
            speedBtn.BackColor = redCoins >= speedCost ? Color.FromArgb(40, 80, 40) : Color.FromArgb(40, 40, 40);
            speedBtn.ForeColor = redCoins >= speedCost ? Color.White : Color.Gray;
            speedBtn.FlatStyle = FlatStyle.Flat;
            speedBtn.FlatAppearance.BorderColor = redCoins >= speedCost ? Color.FromArgb(60, 120, 60) : Color.FromArgb(60, 60, 60);
            speedBtn.Font = new Font("Arial", Math.Max(1, (int)(11 * s)), FontStyle.Bold);
            speedBtn.Cursor = redCoins >= speedCost ? Cursors.Hand : Cursors.Default;
            speedBtn.Enabled = redCoins >= speedCost;
            shopPanel.Controls.Add(speedBtn);
            yPos += btnH + (int)(10 * s);

            // Damage upgrade
            int dmgCost = GetUpgradeCost(5, permDamageLevel);
            Button dmgBtn = new Button();
            dmgBtn.Text = $"⚔ Damage +0.1  (Lv {permDamageLevel})  —  🔴 {dmgCost}";
            dmgBtn.Size = new Size(btnW, btnH);
            dmgBtn.Location = new Point((int)(20 * s), yPos);
            dmgBtn.BackColor = redCoins >= dmgCost ? Color.FromArgb(80, 40, 40) : Color.FromArgb(40, 40, 40);
            dmgBtn.ForeColor = redCoins >= dmgCost ? Color.White : Color.Gray;
            dmgBtn.FlatStyle = FlatStyle.Flat;
            dmgBtn.FlatAppearance.BorderColor = redCoins >= dmgCost ? Color.FromArgb(120, 60, 60) : Color.FromArgb(60, 60, 60);
            dmgBtn.Font = new Font("Arial", Math.Max(1, (int)(11 * s)), FontStyle.Bold);
            dmgBtn.Cursor = redCoins >= dmgCost ? Cursors.Hand : Cursors.Default;
            dmgBtn.Enabled = redCoins >= dmgCost;
            shopPanel.Controls.Add(dmgBtn);
            yPos += btnH + (int)(10 * s);

            // Bullet speed upgrade
            int bsCost = GetUpgradeCost(3, permBulletSpeedLevel);
            Button bsBtn = new Button();
            bsBtn.Text = $"💨 Bullet Speed +0.5  (Lv {permBulletSpeedLevel})  —  🔴 {bsCost}";
            bsBtn.Size = new Size(btnW, btnH);
            bsBtn.Location = new Point((int)(20 * s), yPos);
            bsBtn.BackColor = redCoins >= bsCost ? Color.FromArgb(40, 60, 100) : Color.FromArgb(40, 40, 40);
            bsBtn.ForeColor = redCoins >= bsCost ? Color.White : Color.Gray;
            bsBtn.FlatStyle = FlatStyle.Flat;
            bsBtn.FlatAppearance.BorderColor = redCoins >= bsCost ? Color.FromArgb(60, 90, 140) : Color.FromArgb(60, 60, 60);
            bsBtn.Font = new Font("Arial", Math.Max(1, (int)(11 * s)), FontStyle.Bold);
            bsBtn.Cursor = redCoins >= bsCost ? Cursors.Hand : Cursors.Default;
            bsBtn.Enabled = redCoins >= bsCost;
            shopPanel.Controls.Add(bsBtn);
            yPos += btnH + (int)(15 * s);

            // Close button
            Button closeBtn = new Button();
            closeBtn.Text = "Close";
            closeBtn.Size = new Size((int)(120 * s), (int)(35 * s));
            closeBtn.Location = new Point((shopPanel.Width - (int)(120 * s)) / 2, yPos);
            closeBtn.BackColor = Color.FromArgb(80, 30, 30);
            closeBtn.ForeColor = Color.White;
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.Font = new Font("Arial", Math.Max(1, (int)(11 * s)), FontStyle.Bold);
            closeBtn.Cursor = Cursors.Hand;
            closeBtn.Click += (s2, e2) => this.Controls.Remove(shopPanel);
            shopPanel.Controls.Add(closeBtn);

            // Buy handlers - rebuild shop on purchase
            speedBtn.Click += (s2, e2) =>
            {
                int cost = GetUpgradeCost(5, permSpeedLevel);
                if (redCoins >= cost) { redCoins -= cost; permSpeedLevel++; SaveRedCoins(); this.Controls.Remove(shopPanel); ShowRedCoinShop(); }
            };
            dmgBtn.Click += (s2, e2) =>
            {
                int cost = GetUpgradeCost(5, permDamageLevel);
                if (redCoins >= cost) { redCoins -= cost; permDamageLevel++; SaveRedCoins(); this.Controls.Remove(shopPanel); ShowRedCoinShop(); }
            };
            bsBtn.Click += (s2, e2) =>
            {
                int cost = GetUpgradeCost(3, permBulletSpeedLevel);
                if (redCoins >= cost) { redCoins -= cost; permBulletSpeedLevel++; SaveRedCoins(); this.Controls.Remove(shopPanel); ShowRedCoinShop(); }
            };

            this.Controls.Add(shopPanel);
            shopPanel.BringToFront();
            AnimateZoomIn(shopPanel);
        }

        private Panel CreatePaintedOverlay(int baseW, int baseH)
        {
            int w = Math.Min(ClientSize.Width - 20, (int)(baseW * scale));
            int h = Math.Min(ClientSize.Height - 20, (int)(baseH * scale));
            var panel = new Panel { Size = new Size(w, h), Location = new Point((ClientSize.Width - w) / 2, (ClientSize.Height - h) / 2), BackColor = Color.Transparent };
            typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(panel, true);
            panel.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int r = (int)(14 * scale);
                var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(rect.X, rect.Y, r, r, 180, 90); path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90); path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                path.CloseFigure();
                g.FillPath(new SolidBrush(Color.FromArgb(240, 20, 20, 30)), path);
                g.DrawPath(new Pen(Color.FromArgb(100, 60, 60, 100), 2 * scale), path);
            };
            return panel;
        }

        private void ShowOverlayBlocking(Panel overlay)
        {
            bool closed = false;
            showDimOverlay = true;
            this.Invalidate();

            overlay.Tag = (Action)(() => closed = true);
            this.Controls.Add(overlay);
            overlay.BringToFront();
            AnimateZoomIn(overlay);
            while (!closed) Application.DoEvents();
            this.Controls.Remove(overlay);
            overlay.Dispose();
            showDimOverlay = false;
            this.Invalidate();
            this.Focus();
        }

        private void ShowRunHistory()
        {
            var histForm = CreatePaintedOverlay(700, 500);
            histForm.BackColor = Color.FromArgb(20, 20, 30);
            float s = scale;

            Label title = new Label();
            title.Text = "📜 RUN HISTORY";
            title.Font = new Font("Arial", Math.Max(1, (int)(20 * s)), FontStyle.Bold);
            title.ForeColor = Color.White;
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Size = new Size(histForm.Width - (int)(40 * s), (int)(40 * s));
            title.Location = new Point((int)(20 * s), (int)(15 * s));
            histForm.Controls.Add(title);

            if (runHistory.Count == 0)
            {
                Label noRuns = new Label();
                noRuns.Text = "No runs yet. Play a game first!";
                noRuns.Font = new Font("Arial", Math.Max(1, (int)(14 * s)));
                noRuns.ForeColor = Color.Gray;
                noRuns.TextAlign = ContentAlignment.MiddleCenter;
                noRuns.Size = new Size(histForm.Width - (int)(40 * s), (int)(40 * s));
                noRuns.Location = new Point((int)(20 * s), (int)(200 * s));
                histForm.Controls.Add(noRuns);
            }
            else
            {
                int rowH = (int)(65 * s);
                int rowGap = (int)(75 * s);
                for (int i = 0; i < runHistory.Count; i++)
                {
                    var run = runHistory[i];
                    int minutes = (int)(run.time / 60f);
                    int seconds = (int)(run.time % 60f);
                    int diff = Math.Clamp(run.difficulty, 0, 8);

                    Panel row = new Panel();
                    row.Size = new Size((int)(650 * s), rowH);
                    row.Location = new Point((int)(25 * s), (int)(65 * s) + i * rowGap);
                    row.BackColor = i % 2 == 0 ? Color.FromArgb(30, 30, 45) : Color.FromArgb(25, 25, 38);
                    histForm.Controls.Add(row);

                    Label runNum = new Label();
                    runNum.Text = $"#{i + 1}";
                    runNum.Font = new Font("Arial", Math.Max(1, (int)(14 * s)), FontStyle.Bold);
                    runNum.ForeColor = Color.Gold;
                    runNum.Size = new Size((int)(40 * s), rowH);
                    runNum.Location = new Point((int)(10 * s), 0);
                    runNum.TextAlign = ContentAlignment.MiddleCenter;
                    row.Controls.Add(runNum);

                    Label diffLabel = new Label();
                    diffLabel.Text = (run.sandbox ? "🧪 " : "") + (run.multiplayer ? "👥 " : "") + DifficultyNames[diff];
                    diffLabel.Font = new Font("Arial", Math.Max(1, (int)(12 * s)), FontStyle.Bold);
                    diffLabel.ForeColor = run.sandbox ? Color.MediumPurple : DifficultyColors[diff];
                    diffLabel.Size = new Size((int)(120 * s), rowH);
                    diffLabel.Location = new Point((int)(55 * s), 0);
                    diffLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(diffLabel);

                    Label scoreLabel = new Label();
                    scoreLabel.Text = $"💲 {run.score:F0}";
                    scoreLabel.Font = new Font("Arial", Math.Max(1, (int)(11 * s)));
                    scoreLabel.ForeColor = Color.White;
                    scoreLabel.Size = new Size((int)(150 * s), rowH);
                    scoreLabel.Location = new Point((int)(180 * s), 0);
                    scoreLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(scoreLabel);

                    Label killsLabel = new Label();
                    killsLabel.Text = $"💀 {run.kills} kills";
                    killsLabel.Font = new Font("Arial", Math.Max(1, (int)(11 * s)));
                    killsLabel.ForeColor = Color.White;
                    killsLabel.Size = new Size((int)(130 * s), rowH);
                    killsLabel.Location = new Point((int)(340 * s), 0);
                    killsLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(killsLabel);

                    Label timeLabel = new Label();
                    timeLabel.Text = $"⏱ {minutes:00}:{seconds:00}";
                    timeLabel.Font = new Font("Arial", Math.Max(1, (int)(11 * s)));
                    timeLabel.ForeColor = Color.White;
                    timeLabel.Size = new Size((int)(120 * s), rowH);
                    timeLabel.Location = new Point((int)(480 * s), 0);
                    timeLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(timeLabel);
                }
            }

            Button closeBtn = new Button();
            closeBtn.Text = "Close";
            closeBtn.Font = new Font("Arial", Math.Max(1, (int)(10 * s)), FontStyle.Bold);
            closeBtn.Size = new Size((int)(120 * s), (int)(35 * s));
            closeBtn.Location = new Point((histForm.Width - (int)(120 * s)) / 2, histForm.Height - (int)(70 * s));
            closeBtn.BackColor = Color.FromArgb(80, 30, 30);
            closeBtn.ForeColor = Color.White;
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.Click += (ss, ee) => ((Action)histForm.Tag!)();
            histForm.Controls.Add(closeBtn);

            ShowOverlayBlocking(histForm);
        }
        private void ShowBestiary()
        {
            var bestForm = CreatePaintedOverlay(800, 600);
            bestForm.BackColor = Color.FromArgb(20, 20, 30);
            float s = scale;

            Label title = new Label();
            title.Text = "📖 BESTIARY";
            title.Font = new Font("Arial", Math.Max(1, (int)(20 * s)), FontStyle.Bold);
            title.ForeColor = Color.White;
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Size = new Size(bestForm.Width - (int)(40 * s), (int)(40 * s));
            title.Location = new Point((int)(20 * s), (int)(15 * s));
            bestForm.Controls.Add(title);

            var entries = new[]
            {
        new { Name = "Normal",      Icon = "🟥", Color = Color.Red,         MinDiff = 0, Desc = "Basic enemy. Moves toward player.",                                          Effect = "" },
        new { Name = "Gunner",      Icon = "🟧", Color = Color.OrangeRed,   MinDiff = 0, Desc = "Shoots bullets at the player.",                                              Effect = "" },
        new { Name = "Tank",        Icon = "🟫", Color = Color.DarkRed,     MinDiff = 0, Desc = "High HP. Deals 3x damage on contact.",                                       Effect = "" },
        new { Name = "Runner",      Icon = "🩷", Color = Color.HotPink,     MinDiff = 0, Desc = "Low HP. Moves 2.5x faster than normal.",                                     Effect = "" },
        new { Name = "Parasitic",   Icon = "🟣", Color = Color.MediumPurple,MinDiff = 2, Desc = "Decays over time. Releases 3 parasites when killed by player.",              Effect = "Spawns parasites" },
        new { Name = "Frenzied",    Icon = "🟠", Color = Color.Orange,      MinDiff = 2, Desc = "Moves erratically. Partially homes toward player.",                           Effect = "Erratic movement" },
        new { Name = "Charging",    Icon = "🟡", Color = Color.Yellow,      MinDiff = 3, Desc = "Periodically dashes at high speed toward the player.",                       Effect = "Dash attack" },
        new { Name = "Berserker",   Icon = "🔴", Color = Color.OrangeRed,   MinDiff = 2, Desc = "Below 50% HP: moves faster, deals 2x damage, takes less damage.",            Effect = "Enrages at 50% HP" },
        new { Name = "Armored",     Icon = "⬜", Color = Color.Silver,      MinDiff = 4, Desc = "First hit is always blocked by armor.",                                       Effect = "Blocks first hit" },
        new { Name = "Regenerating",Icon = "🟩", Color = Color.LimeGreen,   MinDiff = 5, Desc = "Slowly heals over time.",                                                    Effect = "Heals over time" },
        new { Name = "Zigzag",      Icon = "🔵", Color = Color.Cyan,        MinDiff = 4, Desc = "Moves in a zigzag pattern toward player.",                                   Effect = "Zigzag movement" },
        new { Name = "Phasing",     Icon = "👻", Color = Color.LightBlue,   MinDiff = 6, Desc = "Periodically becomes nearly invisible and untouchable.",                     Effect = "Becomes invisible" },
        new { Name = "Reflective",  Icon = "💠", Color = Color.LightCyan,   MinDiff = 6, Desc = "20% chance to reflect bullets back at the player.",                          Effect = "Reflects bullets" },
        new { Name = "Corrupted",   Icon = "🟪", Color = Color.MediumPurple,MinDiff = 7, Desc = "Leaves a damaging purple trail behind it.",                                  Effect = "Leaves damage trail" },
    };

            int rowH = (int)(60 * s);
            int rowW = (int)(730 * s);

            Panel scrollPanel = new Panel();
            scrollPanel.Size = new Size(bestForm.Width - (int)(50 * s), bestForm.Height - (int)(120 * s));
            scrollPanel.Location = new Point((int)(25 * s), (int)(60 * s));
            scrollPanel.AutoScroll = true;
            scrollPanel.BackColor = Color.FromArgb(20, 20, 30);
            bestForm.Controls.Add(scrollPanel);

            int yPos = (int)(5 * s);
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                int kills = beastiaryKills.ContainsKey(entry.Name) ? beastiaryKills[entry.Name] : 0;
                bool discovered = kills > 0;
                bool available = difficulty >= entry.MinDiff || kills > 0;

                Panel row = new Panel();
                row.Size = new Size(rowW, rowH);
                row.Location = new Point((int)(5 * s), yPos);
                row.BackColor = i % 2 == 0 ? Color.FromArgb(28, 28, 42) : Color.FromArgb(22, 22, 35);
                scrollPanel.Controls.Add(row);

                // Icon/color indicator
                Panel colorDot = new Panel();
                colorDot.Size = new Size((int)(20 * s), (int)(20 * s));
                colorDot.Location = new Point((int)(10 * s), (int)(20 * s));
                colorDot.BackColor = discovered ? entry.Color : Color.FromArgb(50, 50, 50);
                row.Controls.Add(colorDot);

                // Name
                Label nameLabel = new Label();
                nameLabel.Text = discovered ? entry.Name : "???";
                nameLabel.Font = new Font("Arial", Math.Max(1, (int)(13 * s)), FontStyle.Bold);
                nameLabel.ForeColor = discovered ? entry.Color : Color.FromArgb(60, 60, 60);
                nameLabel.Size = new Size((int)(130 * s), rowH);
                nameLabel.Location = new Point((int)(35 * s), 0);
                nameLabel.TextAlign = ContentAlignment.MiddleLeft;
                row.Controls.Add(nameLabel);

                // Description
                Label descLabel = new Label();
                descLabel.Text = discovered ? entry.Desc : "Kill this enemy to unlock its entry.";
                descLabel.Font = new Font("Arial", Math.Max(1, (int)(9 * s)));
                descLabel.ForeColor = discovered ? Color.LightGray : Color.FromArgb(60, 60, 60);
                descLabel.Size = new Size((int)(330 * s), rowH);
                descLabel.Location = new Point((int)(170 * s), 0);
                descLabel.TextAlign = ContentAlignment.MiddleLeft;
                row.Controls.Add(descLabel);

                // Effect
                Label effectLabel = new Label();
                effectLabel.Text = discovered && entry.Effect != "" ? "⚡ " + entry.Effect : "";
                effectLabel.Font = new Font("Arial", Math.Max(1, (int)(9 * s)), FontStyle.Italic);
                effectLabel.ForeColor = Color.Gold;
                effectLabel.Size = new Size((int)(150 * s), rowH);
                effectLabel.Location = new Point((int)(505 * s), 0);
                effectLabel.TextAlign = ContentAlignment.MiddleLeft;
                row.Controls.Add(effectLabel);

                // Kill count
                Label killLabel = new Label();
                killLabel.Text = discovered ? $"💀 {kills}" : "";
                killLabel.Font = new Font("Arial", Math.Max(1, (int)(11 * s)), FontStyle.Bold);
                killLabel.ForeColor = Color.Gold;
                killLabel.Size = new Size((int)(80 * s), rowH);
                killLabel.Location = new Point((int)(640 * s), 0);
                killLabel.TextAlign = ContentAlignment.MiddleRight;
                row.Controls.Add(killLabel);

                // Min difficulty badge
                Label diffBadge = new Label();
                diffBadge.Text = DifficultyNames[Math.Clamp(entry.MinDiff, 0, 8)];
                diffBadge.Font = new Font("Arial", Math.Max(1, (int)(8 * s)));
                diffBadge.ForeColor = DifficultyColors[Math.Clamp(entry.MinDiff, 0, 8)];
                diffBadge.Size = new Size((int)(60 * s), (int)(15 * s));
                diffBadge.Location = new Point((int)(35 * s), (int)(45 * s));
                diffBadge.TextAlign = ContentAlignment.MiddleLeft;
                row.Controls.Add(diffBadge);

                yPos += (int)(65 * s);
            }

            scrollPanel.AutoScrollMinSize = new Size(rowW, yPos);

            Button closeBtn = new Button();
            closeBtn.Text = "Close";
            closeBtn.Font = new Font("Arial", Math.Max(1, (int)(10 * s)), FontStyle.Bold);
            closeBtn.Size = new Size((int)(120 * s), (int)(35 * s));
            closeBtn.Location = new Point((bestForm.Width - (int)(120 * s)) / 2, bestForm.Height - (int)(52 * s));
            closeBtn.BackColor = Color.FromArgb(80, 30, 30);
            closeBtn.ForeColor = Color.White;
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.Click += (ss, e2) => ((Action)bestForm.Tag!)();
            bestForm.Controls.Add(closeBtn);

            ShowOverlayBlocking(bestForm);
        }

        private void ShowAchievements()
        {
            var achForm = CreatePaintedOverlay(750, 600);
            achForm.BackColor = Color.FromArgb(20, 20, 30);
            float s = scale;

            Label title = new Label();
            title.Text = $"🏆 ACHIEVEMENTS ({unlockedAchievements.Count}/{achievements.Length})";
            title.Font = new Font("Arial", Math.Max(1, (int)(16 * s)), FontStyle.Bold);
            title.ForeColor = Color.Gold;
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Size = new Size(achForm.Width - (int)(40 * s), (int)(40 * s));
            title.Location = new Point((int)(20 * s), (int)(10 * s));
            achForm.Controls.Add(title);

            Panel scrollPanel = new Panel();
            scrollPanel.Location = new Point((int)(10 * s), (int)(55 * s));
            scrollPanel.Size = new Size(achForm.Width - (int)(20 * s), achForm.Height - (int)(110 * s));
            scrollPanel.AutoScroll = true;
            scrollPanel.BackColor = Color.FromArgb(20, 20, 30);
            achForm.Controls.Add(scrollPanel);

            string[] categories = { "Kills", "Survival", "Boss", "Score", "Upgrades", "Abilities", "Difficulty", "Misc" };
            Color[] catColors = {
                Color.FromArgb(180, 60, 60),
                Color.FromArgb(60, 140, 60),
                Color.FromArgb(160, 80, 40),
                Color.FromArgb(60, 120, 180),
                Color.FromArgb(140, 100, 40),
                Color.FromArgb(100, 60, 160),
                Color.FromArgb(160, 160, 40),
                Color.FromArgb(100, 100, 120)
            };

            int achRowW = (int)(680 * s);
            int achRowH = (int)(45 * s);
            int yPos = (int)(5 * s);
            for (int c = 0; c < categories.Length; c++)
            {
                string cat = categories[c];
                var catAchs = achievements.Where(a => a.category == cat).ToArray();
                if (catAchs.Length == 0) continue;

                int catUnlocked = catAchs.Count(a => unlockedAchievements.Contains(a.id));

                Label catLabel = new Label();
                catLabel.Text = $"  {cat.ToUpper()} ({catUnlocked}/{catAchs.Length})";
                catLabel.Font = new Font("Arial", Math.Max(1, (int)(12 * s)), FontStyle.Bold);
                catLabel.ForeColor = catColors[c];
                catLabel.BackColor = Color.FromArgb(30, 30, 45);
                catLabel.Size = new Size(achRowW, (int)(30 * s));
                catLabel.Location = new Point((int)(5 * s), yPos);
                catLabel.TextAlign = ContentAlignment.MiddleLeft;
                scrollPanel.Controls.Add(catLabel);
                yPos += (int)(35 * s);

                for (int i = 0; i < catAchs.Length; i++)
                {
                    var ach = catAchs[i];
                    bool unlocked = unlockedAchievements.Contains(ach.id);

                    Panel row = new Panel();
                    row.Size = new Size(achRowW, achRowH);
                    row.Location = new Point((int)(5 * s), yPos);
                    row.BackColor = unlocked ? Color.FromArgb(35, 45, 35) : Color.FromArgb(25, 25, 30);
                    scrollPanel.Controls.Add(row);

                    Label iconLabel = new Label();
                    iconLabel.Text = unlocked ? ach.icon : "🔒";
                    iconLabel.Font = new Font("Segoe UI Emoji", Math.Max(1, (int)(16 * s)));
                    iconLabel.Size = new Size((int)(40 * s), achRowH);
                    iconLabel.Location = new Point((int)(5 * s), 0);
                    iconLabel.TextAlign = ContentAlignment.MiddleCenter;
                    row.Controls.Add(iconLabel);

                    Label nameLabel = new Label();
                    nameLabel.Text = unlocked ? ach.name : "???";
                    nameLabel.Font = new Font("Arial", Math.Max(1, (int)(12 * s)), FontStyle.Bold);
                    nameLabel.ForeColor = unlocked ? Color.White : Color.FromArgb(80, 80, 80);
                    nameLabel.Size = new Size((int)(200 * s), achRowH);
                    nameLabel.Location = new Point((int)(50 * s), 0);
                    nameLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(nameLabel);

                    Label descLabel = new Label();
                    descLabel.Text = ach.description;
                    descLabel.Font = new Font("Arial", Math.Max(1, (int)(10 * s)));
                    descLabel.ForeColor = unlocked ? Color.FromArgb(180, 200, 180) : Color.FromArgb(60, 60, 60);
                    descLabel.Size = new Size((int)(320 * s), achRowH);
                    descLabel.Location = new Point((int)(255 * s), 0);
                    descLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(descLabel);

                    int reward = GetAchievementRedCoins(ach.id);
                    Label rewardLabel = new Label();
                    rewardLabel.Text = $"🔴 {reward}";
                    rewardLabel.Font = new Font("Segoe UI Emoji", Math.Max(1, (int)(10 * s)));
                    rewardLabel.ForeColor = unlocked ? Color.FromArgb(180, 80, 80) : Color.FromArgb(80, 40, 40);
                    rewardLabel.Size = new Size((int)(55 * s), achRowH);
                    rewardLabel.Location = new Point((int)(580 * s), 0);
                    rewardLabel.TextAlign = ContentAlignment.MiddleCenter;
                    row.Controls.Add(rewardLabel);

                    Label statusLabel = new Label();
                    statusLabel.Text = unlocked ? "✔" : "✗";
                    statusLabel.Font = new Font("Arial", Math.Max(1, (int)(14 * s)), FontStyle.Bold);
                    statusLabel.ForeColor = unlocked ? Color.LimeGreen : Color.FromArgb(60, 30, 30);
                    statusLabel.Size = new Size((int)(35 * s), achRowH);
                    statusLabel.Location = new Point((int)(640 * s), 0);
                    statusLabel.TextAlign = ContentAlignment.MiddleCenter;
                    row.Controls.Add(statusLabel);

                    yPos += (int)(50 * s);
                }
                yPos += (int)(10 * s);
            }

            Button closeBtn = new Button();
            closeBtn.Text = "Close";
            closeBtn.Size = new Size((int)(120 * s), (int)(35 * s));
            closeBtn.Location = new Point((achForm.Width - (int)(120 * s)) / 2, achForm.Height - (int)(50 * s));
            closeBtn.BackColor = Color.FromArgb(80, 30, 30);
            closeBtn.ForeColor = Color.White;
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.Font = new Font("Arial", Math.Max(1, (int)(11 * s)), FontStyle.Bold);
            closeBtn.Click += (ss, e2) => ((Action)achForm.Tag!)();
            achForm.Controls.Add(closeBtn);

            ShowOverlayBlocking(achForm);
        }

        // --- Multiplayer sync helpers ---
        private GameStatePacket BuildGameStatePacket()
        {
            // Normalize all positions to 0-1 range so different resolutions work
            float nw = ClientSize.Width;
            float nh = ClientSize.Height;

            var pkt = new GameStatePacket
            {
                HostX = posX / nw, HostY = posY / nh,
                HostHealth = health, HostMaxHealth = maxHealth,
                HostScore = score,
                HostDashing = isDashing,

                ClientX = p2X / nw, ClientY = p2Y / nh,
                ClientHealth = p2Health, ClientMaxHealth = p2MaxHealth,
                ClientDashing = p2Dashing,
                ClientDashCooldown = p2DashCooldown,

                TimeAlive = timeAlive,
                TotalKills = totalKills,
                BossActive = bossAlive,
                BossX = bossX / nw, BossY = bossY / nh,
                BossHealth = bossHealth, BossMaxHealth = bossMaxHealth,

                SuperActive = superActive,
                SuperTimer = superTimer,
                SuperCooldown = superCooldown,
                WallActive = wallActive,
                WallTimer = wallTimer,
                BoxWall = boxWall,

                HostDead = hostDead,
                ClientDead = p2Dead,

                Reloading = reloading,
                ReloadProgress = reloadTime > 0 ? reloadTimer / reloadTime : 0f,
                P2Reloading = p2Reloading,
                P2ReloadProgress = reloadTime > 0 ? p2ReloadTimer / reloadTime : 0f,

                FlameWall = flameWall,
                OrbitCount = orbitCount,
                OrbitAngle = orbitAngle,
                OrbitRadiusBonus = orbitRadiusBonus,
                PlayerSize = playerSize,
                BulletSize = bulletSize,
                HostPlayerColor = playerColor.ToArgb(),
                PurchasedUpgradesMask = BuildPurchasedUpgradesMask(),
            };

            try
            {
                // Pack turret positions
                int tCount = Math.Min(turrets.Count, 10);
                pkt.TurretCount = tCount;
                pkt.TurretX = new float[tCount];
                pkt.TurretY = new float[tCount];
                for (int i = 0; i < tCount; i++)
                {
                    pkt.TurretX[i] = turrets[i].x / nw;
                    pkt.TurretY[i] = turrets[i].y / nh;
                }

                // Pack wall data (normalized)
                if (wallActive)
                {
                    if (boxWall && boxWalls.Count > 0)
                    {
                        int wCount = Math.Min(boxWalls.Count, 4);
                        pkt.WallCount = wCount;
                        pkt.WallX = new float[wCount]; pkt.WallY = new float[wCount];
                        pkt.WallWidth = new float[wCount]; pkt.WallHeight = new float[wCount];
                        pkt.WallAngle = new float[wCount];
                        for (int i = 0; i < wCount; i++)
                        {
                            pkt.WallX[i] = boxWalls[i].x / nw; pkt.WallY[i] = boxWalls[i].y / nh;
                            pkt.WallWidth[i] = boxWalls[i].width / nw; pkt.WallHeight[i] = boxWalls[i].height / nh;
                            pkt.WallAngle[i] = boxWalls[i].angle;
                        }
                    }
                    else
                    {
                        pkt.WallCount = 1;
                        pkt.WallX = new float[] { tempWall.x / nw }; pkt.WallY = new float[] { tempWall.y / nh };
                        pkt.WallWidth = new float[] { tempWall.width / nw }; pkt.WallHeight = new float[] { tempWall.height / nh };
                        pkt.WallAngle = new float[] { tempWall.angle };
                    }
                }
                else
                {
                    pkt.WallCount = 0;
                    pkt.WallX = Array.Empty<float>(); pkt.WallY = Array.Empty<float>();
                    pkt.WallWidth = Array.Empty<float>(); pkt.WallHeight = Array.Empty<float>();
                    pkt.WallAngle = Array.Empty<float>();
                }

                int eCount = Math.Min(enemies.Count, 80);
                pkt.EnemyCount = eCount;
                pkt.EnemyX = new float[eCount];
                pkt.EnemyY = new float[eCount];
                pkt.EnemyAlive = new bool[eCount];
                pkt.EnemyType = new int[eCount];
                pkt.EnemyEffectFlags = new byte[eCount];
                pkt.EnemyHealthPacked = new byte[eCount];
                for (int i = 0; i < eCount && i < enemies.Count; i++)
                {
                    pkt.EnemyX[i] = enemies[i].x / nw;
                    pkt.EnemyY[i] = enemies[i].y / nh;
                    pkt.EnemyAlive[i] = i < enemyAlive.Count && enemyAlive[i];
                    pkt.EnemyType[i] = (i < enemyIsTank.Count && enemyIsTank[i]) ? 1 :
                                       (i < enemyCanShoot.Count && enemyCanShoot[i]) ? 2 : 0;
                    byte flags = 0;
                    if (i < enemyIsRunner.Count && enemyIsRunner[i]) flags |= 0x01;
                    if (i < enemyIsBerserker.Count && enemyIsBerserker[i]) flags |= 0x02;
                    if (i < enemyIsParasitic.Count && enemyIsParasitic[i]) flags |= 0x04;
                    if (i < enemyIsPhasing.Count && enemyIsPhasing[i]) flags |= 0x08;
                    if (i < enemyIsVisible.Count && enemyIsVisible[i]) flags |= 0x10;
                    pkt.EnemyEffectFlags[i] = flags;
                    float hp = (i < enemyHealth.Count) ? enemyHealth[i] : 0f;
                    pkt.EnemyHealthPacked[i] = (byte)Math.Min(255, Math.Max(0, hp * 16f));
                }

                // Cap bullets aggressively so the GameState packet always fits in MTU,
                // even when super spawns hundreds of bullets per second.
                int bCount = Math.Min(bullets.Count, 60);
                pkt.BulletCount = bCount;
                pkt.BulletX = new float[bCount];
                pkt.BulletY = new float[bCount];
                for (int i = 0; i < bCount && i < bullets.Count; i++)
                {
                    pkt.BulletX[i] = bullets[i].x / nw;
                    pkt.BulletY[i] = bullets[i].y / nh;
                }

                int cCount = Math.Min(coins.Count, 30);
                pkt.CoinCount = cCount;
                pkt.CoinX = new float[cCount];
                pkt.CoinY = new float[cCount];
                for (int i = 0; i < cCount && i < coins.Count; i++)
                {
                    pkt.CoinX[i] = coins[i].x / nw;
                    pkt.CoinY[i] = coins[i].y / nh;
                }

                int ebCount = Math.Min(enemyBullets.Count, 60);
                pkt.EnemyBulletCount = ebCount;
                pkt.EnemyBulletX = new float[ebCount];
                pkt.EnemyBulletY = new float[ebCount];
                for (int i = 0; i < ebCount && i < enemyBullets.Count; i++)
                {
                    pkt.EnemyBulletX[i] = enemyBullets[i].x / nw;
                    pkt.EnemyBulletY[i] = enemyBullets[i].y / nh;
                }
            }
            catch { /* list changed during iteration, skip this frame */ }

            return pkt;
        }

        private long BuildPurchasedUpgradesMask()
        {
            long mask = 0;
            foreach (int idx in purchasedOneTimeUpgrades)
                if (idx >= 0 && idx < 64) mask |= (1L << idx);
            return mask;
        }

        private void ApplyPurchasedUpgradesMask(long mask)
        {
            for (int i = 0; i < 64; i++)
                if ((mask & (1L << i)) != 0) purchasedOneTimeUpgrades.Add(i);
        }

        private void ApplyGameState(GameStatePacket state)
        {
            try
            {
                // Scale from normalized 0-1 coords to our resolution
                float sw = ClientSize.Width;
                float sh = ClientSize.Height;

                // On client: host is "Player 2" visually, client is "me"
                p2X = state.HostX * sw;
                p2Y = state.HostY * sh;
                p2Health = state.HostHealth;
                p2MaxHealth = state.HostMaxHealth;
                p2Dashing = state.HostDashing;
                p2Dead = state.HostDead;

                posX = state.ClientX * sw;
                posY = state.ClientY * sh;
                health = state.ClientHealth;
                maxHealth = state.ClientMaxHealth;
                isDashing = state.ClientDashing;
                dashCooldown = state.ClientDashCooldown;
                hostDead = state.ClientDead; // "hostDead" on client means "my dead state"

                score = state.HostScore;
                timeAlive = state.TimeAlive;
                totalKills = state.TotalKills;

                // Sync abilities
                superActive = state.SuperActive;
                superTimer = state.SuperTimer;
                superCooldown = state.SuperCooldown;
                wallActive = state.WallActive;
                wallTimer = state.WallTimer;
                boxWall = state.BoxWall;

                // Sync reload state (swap: host's reload → P2 on client, client's → mine)
                p2Reloading = state.Reloading;
                p2ReloadTimer = state.ReloadProgress * reloadTime;
                reloading = state.P2Reloading;
                reloadTimer = state.P2ReloadProgress * reloadTime;

                // Sync upgrade visuals
                flameWall = state.FlameWall;
                orbitCount = state.OrbitCount;
                orbitAngle = state.OrbitAngle;
                orbitRadiusBonus = state.OrbitRadiusBonus;
                playerSize = (int)state.PlayerSize;
                bulletSize = state.BulletSize;

                // Sync host's player color (shown as P2 on client)
                p2Color_synced = Color.FromArgb(state.HostPlayerColor);

                // Sync purchased upgrades so both players see them as bought
                ApplyPurchasedUpgradesMask(state.PurchasedUpgradesMask);

                // Sync turret positions for rendering
                turrets.Clear();
                turretShootTimers.Clear();
                for (int i = 0; i < state.TurretCount; i++)
                {
                    turrets.Add((state.TurretX[i] * sw, state.TurretY[i] * sh));
                    turretShootTimers.Add(0f);
                }
                if (state.TurretCount > 0) turret = true;

                // Sync walls (scale from normalized)
                if (state.WallActive && state.WallCount > 0)
                {
                    if (state.BoxWall && state.WallCount > 1)
                    {
                        boxWalls = new List<(float x, float y, float width, float height, float angle)>(state.WallCount);
                        for (int i = 0; i < state.WallCount; i++)
                            boxWalls.Add((state.WallX[i] * sw, state.WallY[i] * sh, state.WallWidth[i] * sw, state.WallHeight[i] * sh, state.WallAngle[i]));
                    }
                    else if (state.WallCount >= 1)
                    {
                        tempWall = (state.WallX[0] * sw, state.WallY[0] * sh, state.WallWidth[0] * sw, state.WallHeight[0] * sh, state.WallAngle[0]);
                    }
                }

                bossAlive = state.BossActive;
                if (state.BossActive)
                {
                    bossX = state.BossX * sw;
                    bossY = state.BossY * sh;
                    bossHealth = state.BossHealth;
                    bossMaxHealth = state.BossMaxHealth;
                }

                // Sync enemies — first truncate any extras (client may have spawned
                // local ghosts before this state arrived), then pad parallel lists.
                int ec = state.EnemyCount;
                if (enemies.Count > ec) enemies.RemoveRange(ec, enemies.Count - ec);
                if (enemyAlive.Count > ec) enemyAlive.RemoveRange(ec, enemyAlive.Count - ec);
                if (enemyIsTank.Count > ec) enemyIsTank.RemoveRange(ec, enemyIsTank.Count - ec);
                if (enemyCanShoot.Count > ec) enemyCanShoot.RemoveRange(ec, enemyCanShoot.Count - ec);
                if (enemyIsRunner.Count > ec) enemyIsRunner.RemoveRange(ec, enemyIsRunner.Count - ec);
                if (enemyHealth.Count > ec) enemyHealth.RemoveRange(ec, enemyHealth.Count - ec);
                if (enemyRespawnTimers.Count > ec) enemyRespawnTimers.RemoveRange(ec, enemyRespawnTimers.Count - ec);
                if (enemyIsParasitic.Count > ec) enemyIsParasitic.RemoveRange(ec, enemyIsParasitic.Count - ec);
                if (enemyIsPhasing.Count > ec) enemyIsPhasing.RemoveRange(ec, enemyIsPhasing.Count - ec);
                if (enemyIsBerserker.Count > ec) enemyIsBerserker.RemoveRange(ec, enemyIsBerserker.Count - ec);
                if (enemyIsVisible.Count > ec) enemyIsVisible.RemoveRange(ec, enemyIsVisible.Count - ec);
                while (enemies.Count < ec) enemies.Add((0, 0));
                while (enemyAlive.Count < ec) enemyAlive.Add(false);
                while (enemyIsTank.Count < ec) enemyIsTank.Add(false);
                while (enemyCanShoot.Count < ec) enemyCanShoot.Add(false);
                while (enemyIsRunner.Count < ec) enemyIsRunner.Add(false);
                while (enemyHealth.Count < ec) enemyHealth.Add(0);
                while (enemyRespawnTimers.Count < ec) enemyRespawnTimers.Add(0);
                while (enemyIsParasitic.Count < ec) enemyIsParasitic.Add(false);
                while (enemyIsPhasing.Count < ec) enemyIsPhasing.Add(false);
                while (enemyIsBerserker.Count < ec) enemyIsBerserker.Add(false);
                while (enemyIsVisible.Count < ec) enemyIsVisible.Add(true);

                for (int i = 0; i < ec; i++)
                {
                    enemies[i] = (state.EnemyX[i] * sw, state.EnemyY[i] * sh);
                    enemyAlive[i] = state.EnemyAlive[i];
                    enemyIsTank[i] = state.EnemyType[i] == 1;
                    enemyCanShoot[i] = state.EnemyType[i] == 2;
                    byte flags = (state.EnemyEffectFlags != null && i < state.EnemyEffectFlags.Length)
                        ? state.EnemyEffectFlags[i] : (byte)0;
                    enemyIsRunner[i]    = (flags & 0x01) != 0;
                    enemyIsBerserker[i] = (flags & 0x02) != 0;
                    enemyIsParasitic[i] = (flags & 0x04) != 0;
                    enemyIsPhasing[i]   = (flags & 0x08) != 0;
                    enemyIsVisible[i]   = (flags & 0x10) != 0;
                    if (state.EnemyHealthPacked != null && i < state.EnemyHealthPacked.Length)
                        enemyHealth[i] = state.EnemyHealthPacked[i] / 16f;
                }

                // Sync bullets
                var newBullets = new List<(float x, float y, float velX, float velY, int bounces)>(state.BulletCount);
                for (int i = 0; i < state.BulletCount; i++)
                    newBullets.Add((state.BulletX[i] * sw, state.BulletY[i] * sh, 0, 0, 0));
                bullets = newBullets;

                // Sync coins
                var newCoins = new List<(float x, float y, float velX, float velY)>(state.CoinCount);
                for (int i = 0; i < state.CoinCount; i++)
                    newCoins.Add((state.CoinX[i] * sw, state.CoinY[i] * sh, 0, 0));
                coins = newCoins;

                // Sync enemy bullets
                var newEB = new List<(float x, float y, float velX, float velY)>(state.EnemyBulletCount);
                for (int i = 0; i < state.EnemyBulletCount; i++)
                    newEB.Add((state.EnemyBulletX[i] * sw, state.EnemyBulletY[i] * sh, 0, 0));
                enemyBullets = newEB;
            }
            catch { /* state packet race condition, skip frame */ }
        }

        private void ApplyP2Input(PlayerInputPacket input)
        {
            if (p2Dead) return; // dead P2 can't move or act

            // Scale normalized aim coords to host's resolution
            float hw = ClientSize.Width;
            float hh = ClientSize.Height;
            float aimX = input.AimX * hw;
            float aimY = input.AimY * hh;
            float wallAimX = input.WallAimX * hw;
            float wallAimY = input.WallAimY * hh;

            // Host simulates P2 movement
            float p2Speed = speed;
            float toughLoveBonus2 = toughLove ? 1f + (1f - (p2Health / p2MaxHealth)) * 1.5f : 1f;
            p2Speed *= toughLoveBonus2;
            float p2PrevX = p2X;
            float p2PrevY = p2Y;
            if (input.MoveX != 0 || input.MoveY != 0)
            {
                float len = (float)Math.Sqrt(input.MoveX * input.MoveX + input.MoveY * input.MoveY);
                if (len > 0)
                {
                    p2X += (input.MoveX / len) * p2Speed * deltaTime * 60f;
                    p2Y += (input.MoveY / len) * p2Speed * deltaTime * 60f;
                }
            }
            // Block p2 from phasing through walls
            if (CollidesWithWall(p2X, p2Y, boxSize))
            {
                p2X = p2PrevX;
                p2Y = p2PrevY;
            }

            // P2 dash — host owns the simulation. The Dashing field arrives as a
            // one-shot trigger from the client (set on space keypress, cleared on send).
            if (p2DashCooldown > 0) p2DashCooldown -= deltaTime;
            if (input.Dashing && !p2Dashing && p2DashCooldown <= 0)
            {
                float mvLen = (float)Math.Sqrt(input.MoveX * input.MoveX + input.MoveY * input.MoveY);
                if (mvLen > 0)
                {
                    p2Dashing = true;
                    p2DashTimer = dashDuration;
                    p2DashCooldown = dashCooldownTime;
                    p2DashVelX = (input.MoveX / mvLen) * dashPower;
                    p2DashVelY = (input.MoveY / mvLen) * dashPower;
                }
                // consume the trigger so a stale latch can't re-fire next tick
                latestP2Input.Dashing = false;
            }
            if (p2Dashing)
            {
                p2DashTimer -= deltaTime;
                float dashProg = p2DashTimer / dashDuration;
                float p2DashPrevX = p2X;
                float p2DashPrevY = p2Y;
                p2X += p2DashVelX * dashProg * deltaTime * 60f;
                p2Y += p2DashVelY * dashProg * deltaTime * 60f;
                if (ghostDash)
                    dashTrail.Add((p2X, p2Y, dashTrailDuration));
                // P2 can dash through walls
                if (p2DashTimer <= 0)
                {
                    p2Dashing = false;
                    p2DashVelX = 0f;
                    p2DashVelY = 0f;
                }
            }

            p2X = Math.Max(0, Math.Min(p2X, ClientSize.Width - boxSize));
            p2Y = Math.Max(0, Math.Min(p2Y, ClientSize.Height - boxSize));

            // P2 shooting — independent cooldown and ammo
            if (p2ShootCooldown > 0) p2ShootCooldown -= deltaTime;
            if (input.Shooting && p2ShootCooldown <= 0 && !p2Reloading && (p2Ammo > 0 || superActive))
            {
                float cx = p2X + boxSize / 2f;
                float cy = p2Y + boxSize / 2f;
                float dirX = aimX - cx;
                float dirY = aimY - cy;
                float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
                if (dist > 0)
                {
                    float vx = dirX / dist * bulletSpeed;
                    float vy = dirY / dist * bulletSpeed;
                    if (doubleTap)
                    {
                        p2DoubleTapCounter++;
                        if (p2DoubleTapCounter >= 5)
                        {
                            p2DoubleTapCounter = 0;
                            float spread = 0.3f;
                            bullets.Add((cx, cy, vx, vy, 0));
                            bullets.Add((cx, cy,
                                vx * (float)Math.Cos(spread) - vy * (float)Math.Sin(spread),
                                vx * (float)Math.Sin(spread) + vy * (float)Math.Cos(spread), 0));
                            bullets.Add((cx, cy,
                                vx * (float)Math.Cos(-spread) - vy * (float)Math.Sin(-spread),
                                vx * (float)Math.Sin(-spread) + vy * (float)Math.Cos(-spread), 0));
                            p2ShootCooldown = superActive ? 0f : Math.Max(0.05f, shootRate - fireRateBonus);
                            if (!superActive) { p2Ammo--; if (p2Ammo <= 0) p2Reloading = true; }
                            return;
                        }
                    }
                    bullets.Add((cx + dirX / dist * 20, cy + dirY / dist * 20, vx, vy, 0));
                    p2ShootCooldown = superActive ? 0f : Math.Max(0.05f, shootRate - fireRateBonus);
                    if (!superActive) { p2Ammo--; if (p2Ammo <= 0) p2Reloading = true; }
                }
            }

            // P2 ability activations — consume the latched trigger after handling
            if (input.ActivateSuper)
            {
                if (!superActive && superCooldown <= 0)
                {
                    superActive = true;
                    superTimer = superDuration;
                    reloading = false;
                    reloadTimer = 0f;
                }
                latestP2Input.ActivateSuper = false;
            }
            if (input.ActivateWall)
            {
                latestP2Input.ActivateWall = false;
            }
            if (input.ActivateWall && !wallActive && wallCooldown <= 0)
            {
                wallActive = true;
                wallTimer = wallDuration;
                wallCooldown = wallCooldownTime;
                // p2 wall activation aims from p2's center, not the host's center
                // (handled below — boxWall vs tempWall use p2X/p2Y).
                if (boxWall)
                {
                    float cx2 = p2X + playerSize / 2;
                    float cy2 = p2Y + playerSize / 2;
                    float offset = wallLength / 2;
                    boxWalls = new List<(float x, float y, float width, float height, float angle)>
                    {
                        (cx2, cy2 - offset, wallLength, boxSize, 0f),
                        (cx2, cy2 + offset, wallLength, boxSize, 0f),
                        (cx2 - offset, cy2, wallLength, boxSize, (float)(Math.PI / 2)),
                        (cx2 + offset, cy2, wallLength, boxSize, (float)(Math.PI / 2)),
                    };
                }
                else
                {
                    boxWalls.Clear();
                    float wDirX = wallAimX - (p2X + boxSize / 2);
                    float wDirY = wallAimY - (p2Y + boxSize / 2);
                    float wDist = (float)Math.Sqrt(wDirX * wDirX + wDirY * wDirY);
                    float angle = (float)Math.Atan2(wDirY, wDirX) + (float)(Math.PI / 2);
                    float spawnX = p2X + boxSize / 2;
                    float spawnY = p2Y + boxSize / 2;
                    if (wDist > 0) { spawnX = p2X + (wDirX / wDist) * (boxSize * 2); spawnY = p2Y + (wDirY / wDist) * (boxSize * 2); }
                    tempWall = (spawnX, spawnY, wallLength, boxSize, angle);
                }
            }

            // P2 blink (right-click teleport)
            if (input.ActivateBlink)
            {
                latestP2Input.ActivateBlink = false;
                if (blink && p2BlinkCooldown <= 0)
                {
                    float blinkX = input.BlinkAimX * ClientSize.Width;
                    float blinkY = input.BlinkAimY * ClientSize.Height;
                    p2X = Math.Max(0, Math.Min(blinkX - boxSize / 2, ClientSize.Width - boxSize));
                    p2Y = Math.Max(0, Math.Min(blinkY - boxSize / 2, ClientSize.Height - boxSize));
                    p2BlinkCooldown = blinkCooldownTime;
                }
            }

            // P2 turret placement
            if (input.PlaceTurret)
            {
                latestP2Input.PlaceTurret = false;
                if (turret && p2TurretCooldown <= 0)
                {
                    float tX = input.TurretAimX * ClientSize.Width;
                    float tY = input.TurretAimY * ClientSize.Height;
                    turrets.Add((tX, tY));
                    turretShootTimers.Add(0f);
                    p2TurretCooldown = turretCooldownTime;
                }
            }

            // P2 decoy placement (shared cooldown)
            if (input.ActivateDecoy)
            {
                latestP2Input.ActivateDecoy = false;
                if (decoy && !decoyActive && decoyCooldown <= 0)
                {
                    decoyActive = true;
                    decoyTimer = decoyDuration;
                    decoyCooldown = decoyCooldownTime;
                    decoyX = input.DecoyAimX * ClientSize.Width;
                    decoyY = input.DecoyAimY * ClientSize.Height;
                }
            }

            // P2 speed trap placement (shared cooldown)
            if (input.ActivateSpeedTrap)
            {
                latestP2Input.ActivateSpeedTrap = false;
                if (speedTrap && !speedTrapActive && speedTrapCooldown <= 0)
                {
                    speedTrapActive = true;
                    speedTrapTimer = speedTrapDuration;
                    speedTrapX = input.SpeedTrapAimX * ClientSize.Width;
                    speedTrapY = input.SpeedTrapAimY * ClientSize.Height;
                }
            }

            // P2 upgrade purchases (shared money)
            if (input.UpgradePurchaseIndex >= 0)
            {
                ApplyUpgradeByIndex(input.UpgradePurchaseIndex);
                latestP2Input.UpgradePurchaseIndex = -1;
            }
        }

        private static readonly int[] UpgradeCosts = {
            190, 450, 470, 500, 510, 600, 610, 650, 660, 670, 690, 730, 750, 790, 800, 800,
            900, 950, 950, 1000, 1010, 1200, 1230, 1250, 1290, 1300, 1350, 1390, 1410, 1600,
            2200, 3100, 950, 750, 900, 1300, 600, 1400, 820, 1500, 1400, 1100, 1200, 1100,
            1220
        };

        private void ApplyUpgradeByIndex(int index)
        {
            if (index < 0 || index >= UpgradeCosts.Length) return;
            int cost = UpgradeCosts[index];
            if (score < cost) return;
            score -= cost;
            totalUpgradesPurchased++;
            if (cashback) totalSpentSinceLastCashback += cost;
            switch (index)
            {
                case 0: wallLength += boxSize; wallDuration += 5; break;
                case 1:
                    maxHealth += 10; health = Math.Min(health + 10, maxHealth);
                    if (isMultiplayer) { p2MaxHealth += 10; p2Health = Math.Min(p2Health + 10, p2MaxHealth); }
                    break;
                case 2: if (scoreTimerMax > 0.05) scoreTimerMax -= 0.05f; else score += 470; break;
                case 3: bulletSpeed++; break;
                case 4: scorePerSecond++; break;
                case 5: playerSize += 10; bulletSize += 3; speed += 0.5f; dashPower += 1; break;
                case 6: decoyDuration += 1; break;
                case 7: shootRate = Math.Max(0.05f, shootRate - 0.1f); break;
                case 8: dashDuration += 0.1f; break;
                case 9: dashCooldownTime = Math.Max(0.5f, dashCooldownTime - 0.1f); break;
                case 10: explosiveFinish = true; break;
                case 11: reloadTime = Math.Max(0.5f, reloadTime - 0.5f); break;
                case 12: afterburn = true; break;
                case 13: blinkCooldownTime = Math.Max(5f, blinkCooldownTime - 5f); break;
                case 14:
                    maxAmmo += 5; ammo = Math.Min(ammo + 5, maxAmmo);
                    if (isMultiplayer) { p2MaxAmmo += 5; p2Ammo = Math.Min(p2Ammo + 5, p2MaxAmmo); }
                    break;
                case 15: toughLove = true; break;
                case 16: superCooldownTime = Math.Max(2f, superCooldownTime - 2f); break;
                case 17: ricochetBounces += 2; break;
                case 18: doubleTap = true; break;
                case 19: medic = true; break;
                case 20: jackpot = true; break;
                case 21: orbitCount++; break;
                case 22: boxWall = true; break;
                case 23: flameWall = true; break;
                case 24: coinWorth++; break;
                case 25: ghostDash = true; break;
                case 26: decoy = true; break;
                case 27: blink = true; break;
                case 28: shrapnel = true; break;
                case 29: lifeSteal++; break;
                case 30: piercingBullets = true; break;
                case 31: homing = true; break;
                case 32: speedTrap = true; break;
                case 33: thorns = true; break;
                case 34: cashback = true; break;
                case 35: orbitalStrike = true; break;
                case 36: orbitRadiusBonus += 30f; break;
                case 37: turret = true; break;
                case 38: rapidReload = true; break;
                case 39: explosiveOrbit = true; break;
                case 40: ricochetExplosion = true; break;
                case 41: bloodMoney = true; break;
                case 42: parasiteImmune = true; break;
                case 43: lastStand = true; break;
                case 44: smartBounce = true; ricochetBounces += 1; break;
            }
        }

        private void InitMultiplayerCallbacks()
        {
            if (netManager == null) return;
            // Clear any leftover lobby/ready-up handlers so we don't get duplicates
            netManager.ClearAllCallbacks();
            netManager.OnPlayerInputReceived += input =>
            {
                // Latch one-shot triggers from previous packets so they survive until
                // the host's next tick consumes them. Without this, a trigger packet
                // could be silently overwritten by a follow-up input packet, causing
                // random ability/dash drops on p2.
                bool latchedSuper = latestP2Input.ActivateSuper || input.ActivateSuper;
                bool latchedWall = latestP2Input.ActivateWall || input.ActivateWall;
                bool latchedDash = latestP2Input.Dashing || input.Dashing;
                bool latchedBlink = latestP2Input.ActivateBlink || input.ActivateBlink;
                bool latchedTurret = latestP2Input.PlaceTurret || input.PlaceTurret;
                bool latchedDecoy = latestP2Input.ActivateDecoy || input.ActivateDecoy;
                bool latchedSpeedTrap = latestP2Input.ActivateSpeedTrap || input.ActivateSpeedTrap;
                int latchedUpgrade = input.UpgradePurchaseIndex >= 0
                    ? input.UpgradePurchaseIndex
                    : latestP2Input.UpgradePurchaseIndex;
                latestP2Input = input;
                latestP2Input.ActivateSuper = latchedSuper;
                latestP2Input.ActivateWall = latchedWall;
                latestP2Input.Dashing = latchedDash;
                latestP2Input.ActivateBlink = latchedBlink;
                latestP2Input.PlaceTurret = latchedTurret;
                latestP2Input.ActivateDecoy = latchedDecoy;
                latestP2Input.ActivateSpeedTrap = latchedSpeedTrap;
                latestP2Input.UpgradePurchaseIndex = latchedUpgrade;
            };
            netManager.OnGameStateReceived += state => latestGameState = state;
            netManager.OnPeerLeft += () =>
            {
                this.Invoke(() =>
                {
                    isMultiplayer = false;
                });
            };
            // Client receives explicit GameOver from host
            if (!isNetHost)
            {
                netManager.OnGameOver += () =>
                {
                    this.Invoke(() =>
                    {
                        HandleMultiplayerGameOver();
                    });
                };
            }
        }

        private void ShowMultiplayerMenu()
        {
            // Hide menu buttons (same as Preferences) so alpha overlay covers them
            menuPlayBtn.Visible = false;
            menuQuitBtn.Visible = false;
            if (menuPrefsBtn != null) menuPrefsBtn.Visible = false;
            if (menuBestiaryBtn != null) menuBestiaryBtn.Visible = false;
            if (menuHistoryBtn != null) menuHistoryBtn.Visible = false;
            if (menuAchievementsBtn != null) menuAchievementsBtn.Visible = false;
            if (menuMultiplayerBtn != null) menuMultiplayerBtn.Visible = false;

            List<Control> mpControls = new();

            showDimOverlay = true;
            this.Invalidate();

            // Painted centered panel backdrop
            int panelW = (int)(420 * scale);
            int panelH = (int)(600 * scale);
            int panelX = (ClientSize.Width - panelW) / 2;
            int panelY = (ClientSize.Height - panelH) / 2;

            Panel mpPanel = new Panel();
            mpPanel.Size = new Size(panelW, panelH);
            mpPanel.Location = new Point(panelX, panelY);
            mpPanel.BackColor = Color.FromArgb(18, 18, 28);
            typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(mpPanel, true);
            mpPanel.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int r = (int)(16 * scale);
                var rect = new Rectangle(0, 0, mpPanel.Width - 1, mpPanel.Height - 1);
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(rect.X, rect.Y, r, r, 180, 90);
                path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                path.CloseFigure();
                using var bgBrush = new SolidBrush(Color.FromArgb(230, 18, 18, 28));
                g.FillPath(bgBrush, path);
                using var borderPen2 = new Pen(Color.FromArgb(100, 60, 110, 180), 2 * scale);
                g.DrawPath(borderPen2, path);
            };
            mpControls.Add(mpPanel);

            // Helper: position relative to panel interior
            int pad = (int)(24 * scale);
            int innerW = panelW - pad * 2;
            int yOff = pad;

            Label titleLabel = new Label();
            titleLabel.Text = "MULTIPLAYER";
            titleLabel.Font = new Font("Arial", (int)(18 * scale), FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(120, 160, 255);
            titleLabel.Size = new Size(innerW, (int)(35 * scale));
            titleLabel.Location = new Point(pad, yOff);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.BackColor = Color.Transparent;
            mpPanel.Controls.Add(titleLabel);
            yOff += (int)(42 * scale);

            Label statusLabel = new Label();
            statusLabel.Text = "Choose Host or Join";
            statusLabel.Font = new Font("Arial", (int)(10 * scale));
            statusLabel.ForeColor = Color.Gray;
            statusLabel.Size = new Size(innerW, (int)(22 * scale));
            statusLabel.Location = new Point(pad, yOff);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.BackColor = Color.Transparent;
            mpPanel.Controls.Add(statusLabel);

            Label pingLabel = new Label();
            pingLabel.Text = "";
            pingLabel.Font = new Font("Arial", (int)(9 * scale));
            pingLabel.ForeColor = Color.FromArgb(130, 130, 150);
            pingLabel.Size = new Size(innerW, (int)(18 * scale));
            pingLabel.Location = new Point(pad, yOff + (int)(22 * scale));
            pingLabel.TextAlign = ContentAlignment.MiddleCenter;
            pingLabel.BackColor = Color.Transparent;
            pingLabel.Visible = false;
            mpPanel.Controls.Add(pingLabel);
            yOff += (int)(30 * scale);

            // --- HOST section ---
            Button hostBtn = new Button();
            hostBtn.Text = "Host Game";
            hostBtn.Size = new Size(innerW, (int)(44 * scale));
            hostBtn.Location = new Point(pad, yOff);
            hostBtn.BackColor = Color.FromArgb(35, 90, 35);
            hostBtn.ForeColor = Color.White;
            hostBtn.FlatStyle = FlatStyle.Flat;
            hostBtn.FlatAppearance.BorderColor = Color.FromArgb(60, 140, 60);
            hostBtn.Font = new Font("Arial", (int)(13 * scale), FontStyle.Bold);
            hostBtn.Cursor = Cursors.Hand;
            mpPanel.Controls.Add(hostBtn);
            yOff += (int)(52 * scale);

            Label roomCodeLabel = new Label();
            roomCodeLabel.Text = "";
            roomCodeLabel.Font = new Font("Consolas", (int)(20 * scale), FontStyle.Bold);
            roomCodeLabel.ForeColor = Color.Gold;
            roomCodeLabel.Size = new Size(innerW, (int)(35 * scale));
            roomCodeLabel.Location = new Point(pad, yOff);
            roomCodeLabel.TextAlign = ContentAlignment.MiddleCenter;
            roomCodeLabel.BackColor = Color.Transparent;
            mpPanel.Controls.Add(roomCodeLabel);
            yOff += (int)(38 * scale);

            Label ipInfoLabel = new Label();
            ipInfoLabel.Text = "";
            ipInfoLabel.Font = new Font("Arial", (int)(9 * scale));
            ipInfoLabel.ForeColor = Color.FromArgb(150, 180, 220);
            ipInfoLabel.Size = new Size(innerW, (int)(40 * scale));
            ipInfoLabel.Location = new Point(pad, yOff);
            ipInfoLabel.TextAlign = ContentAlignment.MiddleCenter;
            ipInfoLabel.BackColor = Color.Transparent;
            mpPanel.Controls.Add(ipInfoLabel);
            yOff += (int)(44 * scale);

            // --- OR ---
            Label orLabel = new Label();
            orLabel.Text = "--- OR JOIN ---";
            orLabel.Font = new Font("Arial", (int)(9 * scale));
            orLabel.ForeColor = Color.FromArgb(80, 80, 100);
            orLabel.Size = new Size(innerW, (int)(20 * scale));
            orLabel.Location = new Point(pad, yOff);
            orLabel.TextAlign = ContentAlignment.MiddleCenter;
            orLabel.BackColor = Color.Transparent;
            mpPanel.Controls.Add(orLabel);
            yOff += (int)(24 * scale);

            // --- JOIN section ---
            int fieldH = (int)(28 * scale);
            int ipBoxW = (int)(180 * scale);
            int codeBoxW = (int)(100 * scale);
            int labelW = (int)(55 * scale);

            Label hostIpLabel = new Label();
            hostIpLabel.Text = "IP:";
            hostIpLabel.Font = new Font("Arial", (int)(10 * scale));
            hostIpLabel.ForeColor = Color.White;
            hostIpLabel.Size = new Size(labelW, fieldH);
            hostIpLabel.Location = new Point(pad, yOff + 2);
            hostIpLabel.BackColor = Color.Transparent;
            mpPanel.Controls.Add(hostIpLabel);

            TextBox hostIpBox = new TextBox();
            hostIpBox.Font = new Font("Consolas", (int)(11 * scale));
            hostIpBox.Size = new Size(ipBoxW, fieldH);
            hostIpBox.Location = new Point(pad + labelW, yOff);
            hostIpBox.BackColor = Color.FromArgb(25, 25, 40);
            hostIpBox.ForeColor = Color.White;
            hostIpBox.BorderStyle = BorderStyle.FixedSingle;
            mpPanel.Controls.Add(hostIpBox);
            yOff += (int)(34 * scale);

            Label joinCodeLabel = new Label();
            joinCodeLabel.Text = "Code:";
            joinCodeLabel.Font = new Font("Arial", (int)(10 * scale));
            joinCodeLabel.ForeColor = Color.White;
            joinCodeLabel.Size = new Size(labelW, fieldH);
            joinCodeLabel.Location = new Point(pad, yOff + 2);
            joinCodeLabel.BackColor = Color.Transparent;
            mpPanel.Controls.Add(joinCodeLabel);

            TextBox codeBox = new TextBox();
            codeBox.Font = new Font("Consolas", (int)(13 * scale));
            codeBox.Size = new Size(codeBoxW, fieldH);
            codeBox.Location = new Point(pad + labelW, yOff);
            codeBox.BackColor = Color.FromArgb(25, 25, 40);
            codeBox.ForeColor = Color.White;
            codeBox.BorderStyle = BorderStyle.FixedSingle;
            codeBox.MaxLength = 5;
            codeBox.CharacterCasing = CharacterCasing.Upper;
            mpPanel.Controls.Add(codeBox);

            int joinBtnX = pad + labelW + codeBoxW + (int)(8 * scale);
            Button joinBtn = new Button();
            joinBtn.Text = "Join";
            joinBtn.Size = new Size(innerW - labelW - codeBoxW - (int)(8 * scale), fieldH);
            joinBtn.Location = new Point(joinBtnX, yOff);
            joinBtn.BackColor = Color.FromArgb(35, 70, 130);
            joinBtn.ForeColor = Color.White;
            joinBtn.FlatStyle = FlatStyle.Flat;
            joinBtn.FlatAppearance.BorderColor = Color.FromArgb(50, 100, 180);
            joinBtn.Font = new Font("Arial", (int)(11 * scale), FontStyle.Bold);
            joinBtn.Cursor = Cursors.Hand;
            mpPanel.Controls.Add(joinBtn);
            yOff += (int)(38 * scale);

            Button lanBtn = new Button();
            lanBtn.Text = "Find LAN Host";
            lanBtn.Size = new Size(innerW, (int)(28 * scale));
            lanBtn.Location = new Point(pad, yOff);
            lanBtn.BackColor = Color.FromArgb(30, 50, 80);
            lanBtn.ForeColor = Color.FromArgb(160, 190, 230);
            lanBtn.FlatStyle = FlatStyle.Flat;
            lanBtn.FlatAppearance.BorderColor = Color.FromArgb(50, 80, 120);
            lanBtn.Font = new Font("Arial", (int)(9 * scale));
            lanBtn.Cursor = Cursors.Hand;
            mpPanel.Controls.Add(lanBtn);
            yOff += (int)(36 * scale);

            // --- Difficulty selector (for host) ---
            int selectedDifficulty = 0;

            Label diffLabel = new Label();
            diffLabel.Text = "Difficulty:";
            diffLabel.Font = new Font("Arial", (int)(9 * scale));
            diffLabel.ForeColor = Color.FromArgb(180, 180, 200);
            diffLabel.Size = new Size(innerW, (int)(18 * scale));
            diffLabel.Location = new Point(pad, yOff);
            diffLabel.TextAlign = ContentAlignment.MiddleLeft;
            diffLabel.BackColor = Color.Transparent;
            diffLabel.Visible = false;
            mpPanel.Controls.Add(diffLabel);
            yOff += (int)(20 * scale);

            int diffBtnW = (innerW - 2 * (int)(4 * scale)) / 3;
            int diffBtnH = (int)(24 * scale);
            List<Button> diffButtons = new List<Button>();
            int diffStartY = yOff;
            for (int d = 0; d < 9; d++)
            {
                int captured = d;
                bool locked = d > highestUnlockedDifficulty;
                int col = d % 3;
                int row = d / 3;
                Button diffBtn = new Button();
                diffBtn.Text = locked ? "🔒 " + DifficultyNames[d] : DifficultyStarNames[d];
                diffBtn.Size = new Size(diffBtnW, diffBtnH);
                diffBtn.Location = new Point(pad + col * (diffBtnW + (int)(4 * scale)), diffStartY + row * (diffBtnH + (int)(3 * scale)));
                diffBtn.BackColor = (d == 0 && !locked) ? Color.FromArgb(Math.Min(255, DifficultyBgColors[d].R + 30), Math.Min(255, DifficultyBgColors[d].G + 30), Math.Min(255, DifficultyBgColors[d].B + 30)) : (locked ? Color.FromArgb(40, 40, 40) : DifficultyBgColors[d]);
                diffBtn.ForeColor = locked ? Color.Gray : Color.White;
                diffBtn.FlatStyle = FlatStyle.Flat;
                diffBtn.FlatAppearance.BorderColor = (d == 0 && !locked) ? Color.Gold : (locked ? Color.FromArgb(60, 60, 60) : Color.FromArgb(Math.Min(255, DifficultyBgColors[d].R + 30), Math.Min(255, DifficultyBgColors[d].G + 30), Math.Min(255, DifficultyBgColors[d].B + 30)));
                diffBtn.Font = new Font("Arial", Math.Max(1, (int)(7 * scale)), FontStyle.Bold);
                diffBtn.Cursor = locked ? Cursors.Default : Cursors.Hand;
                diffBtn.Enabled = !locked;
                diffBtn.Visible = false;
                diffBtn.Click += (s2, e2) =>
                {
                    selectedDifficulty = captured;
                    for (int i = 0; i < diffButtons.Count; i++)
                    {
                        bool iLocked = i > highestUnlockedDifficulty;
                        if (iLocked) continue;
                        bool sel = (i == captured);
                        diffButtons[i].BackColor = sel
                            ? Color.FromArgb(Math.Min(255, DifficultyBgColors[i].R + 30), Math.Min(255, DifficultyBgColors[i].G + 30), Math.Min(255, DifficultyBgColors[i].B + 30))
                            : DifficultyBgColors[i];
                        diffButtons[i].FlatAppearance.BorderColor = sel ? Color.Gold : Color.FromArgb(Math.Min(255, DifficultyBgColors[i].R + 30), Math.Min(255, DifficultyBgColors[i].G + 30), Math.Min(255, DifficultyBgColors[i].B + 30));
                    }
                };
                diffButtons.Add(diffBtn);
                mpPanel.Controls.Add(diffBtn);
            }
            yOff = diffStartY + 3 * (diffBtnH + (int)(3 * scale)) + (int)(8 * scale);

            Button readyBtn = new Button();
            readyBtn.Text = "Ready";
            readyBtn.Size = new Size(innerW, (int)(44 * scale));
            readyBtn.Location = new Point(pad, yOff);
            readyBtn.BackColor = Color.FromArgb(40, 120, 40);
            readyBtn.ForeColor = Color.White;
            readyBtn.FlatStyle = FlatStyle.Flat;
            readyBtn.FlatAppearance.BorderColor = Color.FromArgb(60, 160, 60);
            readyBtn.Font = new Font("Arial", (int)(13 * scale), FontStyle.Bold);
            readyBtn.Cursor = Cursors.Hand;
            readyBtn.Visible = false;
            mpPanel.Controls.Add(readyBtn);

            Button startBtn = new Button();
            startBtn.Text = "Start Game";
            startBtn.Size = new Size(innerW, (int)(44 * scale));
            startBtn.Location = new Point(pad, yOff);
            startBtn.BackColor = Color.FromArgb(35, 90, 35);
            startBtn.ForeColor = Color.White;
            startBtn.FlatStyle = FlatStyle.Flat;
            startBtn.FlatAppearance.BorderColor = Color.FromArgb(60, 140, 60);
            startBtn.Font = new Font("Arial", (int)(13 * scale), FontStyle.Bold);
            startBtn.Cursor = Cursors.Hand;
            startBtn.Visible = false;
            mpPanel.Controls.Add(startBtn);
            yOff += (int)(52 * scale);

            Button backBtn = new Button();
            backBtn.Text = "Back";
            backBtn.Size = new Size(innerW, (int)(32 * scale));
            backBtn.Location = new Point(pad, yOff);
            backBtn.BackColor = Color.FromArgb(50, 50, 55);
            backBtn.ForeColor = Color.FromArgb(180, 180, 180);
            backBtn.FlatStyle = FlatStyle.Flat;
            backBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 90);
            backBtn.Font = new Font("Arial", (int)(11 * scale));
            backBtn.Cursor = Cursors.Hand;
            mpPanel.Controls.Add(backBtn);

            void Cleanup()
            {
                foreach (var c in mpControls) { this.Controls.Remove(c); c.Dispose(); }
                if (netManager != null && !isMultiplayer)
                {
                    netManager.Disconnect();
                    netManager = null;
                }
                if (embeddedRelay != null && !isMultiplayer)
                {
                    embeddedRelay.Stop();
                    embeddedRelay = null;
                    LanDiscovery.StopHost();
                }
                showDimOverlay = false;
                this.Invalidate();
                AnimateZoomInGroup(new Control[] {
                    menuPlayBtn, menuQuitBtn,
                    menuPrefsBtn!, menuBestiaryBtn!, menuHistoryBtn!,
                    menuAchievementsBtn!, menuMultiplayerBtn!
                });
            }

            void StartGame(bool asHost)
            {
                isMultiplayer = true;
                isNetHost = asHost;
                showDimOverlay = false;
                InitMultiplayerCallbacks();
                if (asHost) netManager?.SendGameStart();
                foreach (var c in mpControls) this.Controls.Remove(c);
                this.Controls.Remove(menuPlayBtn);
                this.Controls.Remove(menuQuitBtn);
                this.Controls.Remove(menuPrefsBtn);
                this.Controls.Remove(menuHistoryBtn);
                this.Controls.Remove(menuBestiaryBtn);
                this.Controls.Remove(menuAchievementsBtn);
                this.Controls.Remove(menuShopBtn);
                this.Controls.Remove(menuMultiplayerBtn);
                onMainMenu = false;
                isPaused = false;
                difficulty = selectedDifficulty;
                sandboxMode = false;
                ApplyDifficulty();
                ResetGame();
                if (asHost) { p2X = posX + 50; p2Y = posY; p2Health = maxHealth; p2MaxHealth = maxHealth; }
                lastTick = DateTime.Now;
                this.Focus();
            }

            backBtn.Click += (s, e) => Cleanup();

            hostBtn.Click += (s, e) =>
            {
                if (playerName == "YOU")
                {
                    statusLabel.Text = "Set your name first! (change in Preferences)";
                    statusLabel.ForeColor = Color.Red;
                    return;
                }
                var hostWarn = MessageBox.Show(
                    "Hosting a multiplayer game requires:\n\n" +
                    "  • A reliable internet connection (good upload bandwidth)\n" +
                    $"  • UDP port {EmbeddedRelay.Port} forwarded to this PC\n" +
                    "    (or your friend on the same LAN)\n" +
                    "  • Windows Firewall must allow this app\n\n" +
                    "If your friend can't connect, the most common cause is that\n" +
                    $"port {EmbeddedRelay.Port} (UDP) is not forwarded on your router.\n\n" +
                    "Continue hosting?",
                    "Host Multiplayer Game",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (hostWarn != DialogResult.Yes) return;
                // Start embedded relay server
                embeddedRelay = new EmbeddedRelay();
                try
                {
                    embeddedRelay.Start();
                }
                catch (Exception ex)
                {
                    statusLabel.Text = $"Failed to start server: {ex.Message}";
                    statusLabel.ForeColor = Color.Red;
                    return;
                }

                statusLabel.Text = "Server started! Connecting...";
                statusLabel.ForeColor = Color.Yellow;

                // Advertise this host on the LAN so peers can auto-discover us.
                LanDiscovery.StartHost(playerName);

                // Get public IP to show the player
                ipInfoLabel.Text = "Fetching your public IP...";
                Task.Run(async () =>
                {
                    try
                    {
                        using var http = new HttpClient();
                        string publicIp = (await http.GetStringAsync("https://api.ipify.org")).Trim();
                        this.Invoke(() =>
                        {
                            ipInfoLabel.Text = $"Your friend connects to: {publicIp}\n(Port forward UDP {EmbeddedRelay.Port} if on different network)";
                        });
                    }
                    catch
                    {
                        this.Invoke(() => ipInfoLabel.Text = $"Could not fetch public IP. Port: {EmbeddedRelay.Port}");
                    }
                });

                // Connect to our own relay
                netManager = new NetworkManager();
                netManager.OnRoomCreated += () =>
                {
                    this.Invoke(() =>
                    {
                        roomCodeLabel.Text = $"Code: {netManager.RoomCode}";
                        statusLabel.Text = "Waiting for player to join...";
                        statusLabel.ForeColor = Color.LimeGreen;
                        // Show difficulty selector for host
                        diffLabel.Visible = true;
                        foreach (var db in diffButtons) db.Visible = true;
                    });
                };
                netManager.OnPeerJoined += () =>
                {
                    this.Invoke(() =>
                    {
                        statusLabel.Text = $"{netManager.PeerName} joined — waiting for them to ready up...";
                        statusLabel.ForeColor = Color.Gold;
                        p2Name = netManager.PeerName;
                        startBtn.Visible = false; // wait for ready
                    });
                };
                netManager.OnPlayerReady += () =>
                {
                    this.Invoke(() =>
                    {
                        statusLabel.Text = $"{netManager.PeerName} is ready! Pick difficulty & start.";
                        statusLabel.ForeColor = Color.LimeGreen;
                        startBtn.Visible = true;
                    });
                };
                netManager.OnPeerLeft += () =>
                {
                    this.Invoke(() =>
                    {
                        statusLabel.Text = "Peer disconnected";
                        statusLabel.ForeColor = Color.Red;
                        startBtn.Visible = false;
                    });
                };
                netManager.OnError += msg =>
                {
                    this.Invoke(() => { statusLabel.Text = msg; statusLabel.ForeColor = Color.Red; });
                };

                hostBtn.Enabled = false;
                joinBtn.Enabled = false;

                // Delay first tick to let server start, then connect and keep polling
                int attempts = 0;
                bool connected = false;
                System.Windows.Forms.Timer pollTimer = new() { Interval = 50 };
                pollTimer.Tick += (s2, e2) =>
                {
                    if (netManager == null) { pollTimer.Stop(); return; }
                    attempts++;

                    // First tick: initiate connection
                    if (attempts == 3 && !connected)
                    {
                        netManager.Connect("127.0.0.1");
                    }

                    netManager.PollEvents();

                    // Update ping display
                    if (connected && netManager.PingMs >= 0)
                    {
                        pingLabel.Visible = true;
                        int ping = netManager.PingMs;
                        pingLabel.Text = $"Ping: {ping}ms";
                        pingLabel.ForeColor = ping < 80 ? Color.LimeGreen : ping < 150 ? Color.Gold : Color.Red;
                    }

                    if (!connected && netManager.IsConnected)
                    {
                        connected = true;
                        statusLabel.Text = "Connected! Creating room...";
                        netManager.CreateRoom(playerName);
                        return;
                    }

                    // Timeout after 10 seconds if not connected
                    if (!connected && attempts > 200)
                    {
                        pollTimer.Stop();
                        statusLabel.Text = "Connection timed out. Windows Firewall may be blocking port 9050.";
                        statusLabel.ForeColor = Color.Red;
                        hostBtn.Enabled = true;
                        joinBtn.Enabled = true;
                    }
                };
                pollTimer.Start();
            };

            joinBtn.Click += (s, e) =>
            {
                if (playerName == "YOU")
                {
                    statusLabel.Text = "Set your name first! (change in Preferences)";
                    statusLabel.ForeColor = Color.Red;
                    return;
                }
                string ip = hostIpBox.Text.Trim();
                string code = codeBox.Text.Trim().ToUpper();
                if (ip.Length == 0) { statusLabel.Text = "Enter host IP"; statusLabel.ForeColor = Color.Red; return; }
                if (code.Length < 5) { statusLabel.Text = "Enter 5-char room code"; statusLabel.ForeColor = Color.Red; return; }

                netManager = new NetworkManager();
                netManager.OnRoomJoined += () =>
                {
                    this.Invoke(() =>
                    {
                        statusLabel.Text = $"Joined {netManager.PeerName}'s room — press Ready!";
                        statusLabel.ForeColor = Color.LimeGreen;
                        p2Name = netManager.PeerName;
                        readyBtn.Visible = true;
                    });
                };
                netManager.OnGameStartReceived += () =>
                {
                    this.Invoke(() => StartGame(false));
                };
                netManager.OnPeerLeft += () =>
                {
                    this.Invoke(() =>
                    {
                        statusLabel.Text = "Host disconnected";
                        statusLabel.ForeColor = Color.Red;
                        readyBtn.Visible = false;
                        hostBtn.Enabled = true;
                        joinBtn.Enabled = true;
                    });
                };
                netManager.OnError += msg =>
                {
                    this.Invoke(() => { statusLabel.Text = msg; statusLabel.ForeColor = Color.Red; });
                };

                netManager.Connect(ip);
                statusLabel.Text = "Connecting...";
                statusLabel.ForeColor = Color.Yellow;
                hostBtn.Enabled = false;
                joinBtn.Enabled = false;

                int joinAttempts = 0;
                bool joinConnected = false;
                System.Windows.Forms.Timer joinPollTimer = new() { Interval = 50 };
                joinPollTimer.Tick += (s2, e2) =>
                {
                    if (netManager == null) { joinPollTimer.Stop(); return; }
                    joinAttempts++;
                    netManager.PollEvents();

                    // Update ping display
                    if (joinConnected && netManager.PingMs >= 0)
                    {
                        pingLabel.Visible = true;
                        int ping = netManager.PingMs;
                        pingLabel.Text = $"Ping: {ping}ms";
                        pingLabel.ForeColor = ping < 80 ? Color.LimeGreen : ping < 150 ? Color.Gold : Color.Red;
                    }

                    if (!joinConnected && netManager.IsConnected)
                    {
                        joinConnected = true;
                        statusLabel.Text = "Connected! Joining room...";
                        netManager.JoinRoom(code, playerName);
                        return;
                    }

                    if (!joinConnected && joinAttempts > 200)
                    {
                        joinPollTimer.Stop();
                        statusLabel.Text = "Connection timed out. Check the IP and that port 9050 is open.";
                        statusLabel.ForeColor = Color.Red;
                        hostBtn.Enabled = true;
                        joinBtn.Enabled = true;
                    }
                    else if (!joinConnected)
                    {
                        statusLabel.Text = $"Connecting to {ip}...";
                    }
                };
                joinPollTimer.Start();
            };

            readyBtn.Click += (s, e) =>
            {
                if (netManager == null) return;
                netManager.SendReady();
                readyBtn.Enabled = false;
                readyBtn.Text = "✔ Ready!";
                readyBtn.BackColor = Color.FromArgb(30, 80, 30);
                statusLabel.Text = "Ready! Waiting for host to start...";
                statusLabel.ForeColor = Color.LimeGreen;
            };

            lanBtn.Click += (s, e) =>
            {
                statusLabel.Text = "Searching LAN...";
                statusLabel.ForeColor = Color.Yellow;
                lanBtn.Enabled = false;
                Task.Run(() =>
                {
                    var hosts = LanDiscovery.FindHosts(1000);
                    this.Invoke(() =>
                    {
                        lanBtn.Enabled = true;
                        if (hosts.Count == 0)
                        {
                            statusLabel.Text = "No LAN hosts found";
                            statusLabel.ForeColor = Color.Red;
                            return;
                        }
                        var first = hosts[0];
                        hostIpBox.Text = first.ip;
                        statusLabel.Text = hosts.Count == 1
                            ? $"Found {first.hostName} at {first.ip}"
                            : $"Found {hosts.Count} hosts — using {first.hostName}";
                        statusLabel.ForeColor = Color.LimeGreen;
                    });
                });
            };

            startBtn.Click += (s, e) => StartGame(true);

            // Add the backdrop panel first; hide loose controls until the zoom completes,
            // so the panel zooms in cohesively rather than each label/button animating separately.
            Control? backdrop = mpControls.FirstOrDefault();
            foreach (var c in mpControls)
            {
                this.Controls.Add(c);
                c.BringToFront();
                if (c != backdrop) c.Visible = false;
            }
            if (backdrop != null)
            {
                AnimateZoomIn(backdrop, onComplete: () =>
                {
                    foreach (var c in mpControls)
                        if (c != backdrop) c.Visible = true;
                });
            }
        }

        // --- Menu zoom-in animation helpers ---
        private void AnimateZoomIn(Control c, int durationMs = 180, Action? onComplete = null)
        {
            if (c == null) { onComplete?.Invoke(); return; }
            // Keep bounds at full target; animate a Region clip so nothing draws outside the final rect
            // (avoids border ghosting from intermediate frames).
            int fullW = c.Width;
            int fullH = c.Height;
            int cx = fullW / 2;
            int cy = fullH / 2;
            if (!c.Visible) c.Visible = true;
            c.Region = new Region(new Rectangle(cx, cy, 1, 1));
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var timer = new System.Windows.Forms.Timer { Interval = 15 };
            timer.Tick += (s, e) =>
            {
                float t = Math.Min(1f, sw.ElapsedMilliseconds / (float)durationMs);
                float u = 1f - t;
                float eased = 1f - u * u * u; // easeOutCubic
                int w = Math.Max(1, (int)(fullW * eased));
                int h = Math.Max(1, (int)(fullH * eased));
                c.Region = new Region(new Rectangle(cx - w / 2, cy - h / 2, w, h));
                if (t >= 1f)
                {
                    c.Region = null; // remove clip
                    timer.Stop();
                    timer.Dispose();
                    onComplete?.Invoke();
                }
            };
            timer.Start();
        }

        private void AnimateZoomInGroup(Control[] controls, int durationMs = 180)
        {
            if (controls == null || controls.Length == 0) return;
            var items = controls.Where(c => c != null).ToArray();
            if (items.Length == 0) return;
            var sizes = new Size[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                sizes[i] = items[i].Size;
                if (!items[i].Visible) items[i].Visible = true;
                items[i].Region = new Region(new Rectangle(sizes[i].Width / 2, sizes[i].Height / 2, 1, 1));
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var timer = new System.Windows.Forms.Timer { Interval = 15 };
            timer.Tick += (s, e) =>
            {
                float t = Math.Min(1f, sw.ElapsedMilliseconds / (float)durationMs);
                float u = 1f - t;
                float eased = 1f - u * u * u;
                for (int i = 0; i < items.Length; i++)
                {
                    int w = Math.Max(1, (int)(sizes[i].Width * eased));
                    int h = Math.Max(1, (int)(sizes[i].Height * eased));
                    items[i].Region = new Region(new Rectangle(sizes[i].Width / 2 - w / 2, sizes[i].Height / 2 - h / 2, w, h));
                }
                if (t >= 1f)
                {
                    for (int i = 0; i < items.Length; i++) items[i].Region = null;
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        private void ShowWithZoom(params Control[] controls)
        {
            AnimateZoomInGroup(controls);
        }

        // Click-through reticle overlay. Its Region is shaped like the reticle itself,
        // so non-reticle pixels are genuinely cut out of the control — siblings show
        // through with no grey box.
        private class ReticleOverlay : Control
        {
            private readonly gameForm _host;
            public ReticleOverlay(gameForm host)
            {
                _host = host;
                SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
                BackColor = Color.Black; // irrelevant — Region cuts away everything but the reticle
                TabStop = false;
            }

            public void RefreshShape()
            {
                float cx = Width / 2f, cy = Height / 2f;
                float s = _host.scale;
                float pulse = (float)Math.Sin(Environment.TickCount / 180.0) * 0.5f + 0.5f;
                float r = 14f * s + pulse * 2f * s;
                float ringThickness = 2.0f * s + 1f;
                float gap = 5f * s, len = 8f * s, lineW = 1.6f * s + 1f;
                float dotR = 1.5f * s + 0.5f;

                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                // outer ring annulus = outer ellipse minus inner ellipse
                using (var outer = new System.Drawing.Drawing2D.GraphicsPath())
                using (var inner = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    outer.AddEllipse(cx - r - ringThickness / 2, cy - r - ringThickness / 2, (r + ringThickness / 2) * 2, (r + ringThickness / 2) * 2);
                    inner.AddEllipse(cx - r + ringThickness / 2, cy - r + ringThickness / 2, (r - ringThickness / 2) * 2, (r - ringThickness / 2) * 2);
                    using var region = new Region(outer);
                    region.Exclude(inner);
                    // we'll combine by adding ellipse + excluding in the Region itself later
                    path.AddPath(outer, false);
                }
                // 4 tick rects
                path.AddRectangle(new RectangleF(cx - gap - len, cy - lineW / 2, len, lineW));
                path.AddRectangle(new RectangleF(cx + gap, cy - lineW / 2, len, lineW));
                path.AddRectangle(new RectangleF(cx - lineW / 2, cy - gap - len, lineW, len));
                path.AddRectangle(new RectangleF(cx - lineW / 2, cy + gap, lineW, len));
                // center dot
                path.AddEllipse(cx - dotR, cy - dotR, dotR * 2, dotR * 2);

                // Build region: outer disc, then carve out the inner disc for the ring hole,
                // then re-union the ticks and center dot.
                var rgn = new Region(path);
                using (var innerEllipsePath = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    innerEllipsePath.AddEllipse(cx - r + ringThickness / 2, cy - r + ringThickness / 2, (r - ringThickness / 2) * 2, (r - ringThickness / 2) * 2);
                    rgn.Exclude(innerEllipsePath);
                }
                // Re-add ticks + dot (they may have been partially inside the inner ellipse)
                using (var extras = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    extras.AddRectangle(new RectangleF(cx - gap - len, cy - lineW / 2, len, lineW));
                    extras.AddRectangle(new RectangleF(cx + gap, cy - lineW / 2, len, lineW));
                    extras.AddRectangle(new RectangleF(cx - lineW / 2, cy - gap - len, lineW, len));
                    extras.AddRectangle(new RectangleF(cx - lineW / 2, cy + gap, lineW, len));
                    extras.AddEllipse(cx - dotR, cy - dotR, dotR * 2, dotR * 2);
                    rgn.Union(extras);
                }
                this.Region = rgn;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                _host.DrawAimReticle(e.Graphics, Width / 2f, Height / 2f);
            }
            protected override void WndProc(ref Message m)
            {
                const int WM_NCHITTEST = 0x0084;
                const int HTTRANSPARENT = -1;
                if (m.Msg == WM_NCHITTEST) { m.Result = (IntPtr)HTTRANSPARENT; return; }
                base.WndProc(ref m);
            }
        }
    }
}
