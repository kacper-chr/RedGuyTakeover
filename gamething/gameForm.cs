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
        private float bulletSpeed = 15f;
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
        private string playerName = "YOU";
        private List<(float x, float y, float timer, float maxTimer, int size)> deathFlashes = new List<(float x, float y, float timer, float maxTimer, int size)>();
        private List<(float x, float y, float timer, float maxTimer, int size)> hitFlashes = new List<(float x, float y, float timer, float maxTimer, int size)>();
        private Button? pauseQuitBtn = null;
        // --- Coins ---
        private List<(float x, float y, float velX, float velY)> coins = new List<(float x, float y, float velX, float velY)>();
        private const int coinSize = 6;
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
        private float reloadTime = 5f;
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
        private const float bossBulletSpeed = 40f;
        private float bossBulletHitCooldown = 0f;
        private const float bossBulletHitCooldownTime = 0.3f;
        private float bossOrbitHitCooldown = 0f;
        private const float bossOrbitHitCooldownTime = 0.3f;

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
        private const float enemyBulletSpeed = 12f;
        private const float enemyShootRate = 1f;
        private const int enemyBulletSize = 10;
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

        private float effectChance_Normal = 0.05f;
        private float effectChance_Hard = 0.10f;
        private float effectChance_Nightmare = 0.15f;
        // --- Difficulty ---
        private int difficulty = 0; // 0=Easy, 1=Normal, 2=Hard, 3=Nightmare
        private int highestUnlockedDifficulty = 0;
        private bool difficultyUnlocked_Normal = false;
        private bool difficultyUnlocked_Hard = false;
        private bool difficultyUnlocked_Nightmare = false;
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
        private List<(float score, int kills, float time, int difficulty, bool sandbox)> runHistory =
            new List<(float score, int kills, float time, int difficulty, bool sandbox)>();
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
        private string p2Name = "Player 2";

        // Client-side: latest game state from host
        private GameStatePacket? latestGameState = null;

        // Host-side: latest input from client
        private PlayerInputPacket latestP2Input = new();

        // --- Achievements ---
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
            ("normal_unlock",  "⭐",  "Stepping Up",       "Unlock Normal difficulty and achievements",           "Difficulty",g => g.difficultyUnlocked_Normal),
            ("hard_unlock",    "⭐",  "Getting Serious",   "Unlock Hard difficulty",             "Difficulty",g => g.difficultyUnlocked_Hard),
            ("nightmare_unlock","💀", "Nightmare Fuel",    "Unlock Nightmare difficulty",        "Difficulty",g => g.difficultyUnlocked_Nightmare),
            ("nightmare_boss", "🔥",  "True Champion",     "Defeat a boss on Nightmare",         "Difficulty",g => g.difficulty == 3 && g.bossesDefeatedOnDifficulty >= 1),

            // Misc achievements
            ("full_health",    "💚",  "Full Health",       "Reach max HP above 100",             "Misc",     g => g.maxHealth >= 100 && g.health >= g.maxHealth),
            ("close_call",     "😰",  "Close Call",        "Survive with less than 1 HP",        "Misc",     g => g.health > 0 && g.health < 1f && g.timeAlive > 10f),
            ("bullet_hell",    "🔫",  "Bullet Hell",       "Have 50+ bullets on screen",         "Misc",     g => g.bullets.Count >= 50),
            ("coin_collector", "🪙",  "Coin Collector",    "Collect 100 coins in one run",       "Misc",     g => g.totalCoinsCollected >= 100),
            ("coin_hoarder",   "🪙",  "Coin Hoarder",     "Collect 500 coins in one run",       "Misc",     g => g.totalCoinsCollected >= 500),
            ("parasite_immune","🧬",  "Immune System",     "Unlock parasite immunity",           "Misc",     g => g.parasiteImmune),
            ("super_active",   "⚡",  "Super Saiyan",      "Activate Super mode",                "Misc",     g => g.superActive),
        };

        private void UnlockAchievement(string id)
        {
            if (difficulty < 1) return; // Only unlock on Normal (1) or higher
            if (unlockedAchievements.Contains(id)) return;
            unlockedAchievements.Add(id);
            SaveAchievements();
            var ach = achievements.FirstOrDefault(a => a.id == id);
            if (ach.id != null)
            {
                achievementToastText = ach.name;
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
            this.Text = "Red Guy Takeover ALPHA RELEASE";
            this.ClientSize = new Size(1900, 1080);
            this.WindowState = FormWindowState.Maximized;
            this.Deactivate += (s, e) => isPaused = true;
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.Shown += (s, e) =>
            {
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
            };
            enemyRespawnTimers = new List<float>(new float[enemies.Count]);
            enemyAlive = Enumerable.Repeat(true, enemies.Count).ToList();
            this.DoubleBuffered = true;
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
                    if (!isDashing && dashCooldown <= 0)
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
                    if (!superActive && superCooldown <= 0)
                    {
                        superActive = true;
                        superTimer = superDuration;
                        reloading = false;
                        reloadTimer = 0f;
                    }
                    break;
                case Keys.E:
                    if (!wallActive && wallCooldown <= 0)
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
                    isPaused = false;
                    velocityX = 0f;
                    velocityY = 0f;
                    HidePauseButtons();
                    break;
                case Keys.F:
                    if (decoy && !decoyActive && decoyCooldown <= 0)
                    {
                        decoyActive = true;
                        decoyTimer = decoyDuration;
                        decoyCooldown = decoyCooldownTime;
                        decoyX = mousePos.X;
                        decoyY = mousePos.Y;
                    }
                    break;
                case Keys.G:
                    if (speedTrap && !speedTrapActive && speedTrapCooldown <= 0)
                    {
                        speedTrapActive = true;
                        speedTrapTimer = speedTrapDuration;
                        speedTrapX = mousePos.X;
                        speedTrapY = mousePos.Y;
                    }
                    break;
                case Keys.H:
                    if (turret && turretCooldown <= 0)
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
                            }
                        }
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
            if (mouseHeld && gameStartTimer > gameStartDelay && !reloading)
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
                float bDirX = posX - bossX;
                float bDirY = posY - bossY;
                float bDist = (float)Math.Sqrt(bDirX * bDirX + bDirY * bDirY);
                superCooldown = superCooldownTime;
                if (bDist > 0)
                {
                    bossX += (bDirX / bDist) * bossSpeed * scale * deltaTime * 60;
                    bossY += (bDirY / bDist) * bossSpeed * scale * deltaTime * 60;
                }

                if (!isDashing &&
                    bossX < posX + playerSize && bossX + bossSize * scale > posX &&
                    bossY < posY + playerSize && bossY + bossSize * scale > posY)
                {
                    if (bossHitCooldown <= 0)
                    {
                        health -= bossDamage;
                        bossHitCooldown = 0.5f;
                        if (health <= 0)
                            HandlePlayerDeath();
                    }
                }
                if (bossHitCooldown > 0)
                    bossHitCooldown -= deltaTime;

                bossShootTimer += deltaTime;
                if (bossShootTimer >= currentBossShootRate)
                {
                    bossShootTimer = 0f;
                    float targetX = decoyActive ? decoyX : posX + playerSize / 2;
                    float targetY = decoyActive ? decoyY : posY + playerSize / 2;
                    float bsDirX = targetX - (bossX + bossSize * scale / 2);
                    float bsDirY = targetY - (bossY + bossSize * scale / 2);
                    float bsDist = (float)Math.Sqrt(bsDirX * bsDirX + bsDirY * bsDirY);
                    if (bsDist > 0)
                    {
                        float[] angles = { -0.3f, -0.1f, 0.1f, 0.3f };
                        foreach (float offset in angles)
                        {
                            float cos = (float)Math.Cos(offset);
                            float sin = (float)Math.Sin(offset);
                            float rotX = (bsDirX / bsDist) * cos - (bsDirY / bsDist) * sin;
                            float rotY = (bsDirX / bsDist) * sin + (bsDirY / bsDist) * cos;
                            enemyBullets.Add((
                                bossX + bossSize * scale / 2,
                                bossY + bossSize * scale / 2,
                                rotX * bossBulletSpeed,
                                rotY * bossBulletSpeed
                            ));
                        }
                    }
                }
            }

            // Enemies
            if (gameStartTimer > gameStartDelay)
            {
                if (!bossAlive)
                {
                    enemySpawnTimer += deltaTime;
                    if (enemySpawnTimer >= enemySpawnRate)
                    {
                        if (isPaused) return;
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
                        dirX = (posX - (playerSize / 2)) - enemies[i].x;
                        dirY = (posY - (playerSize / 2)) - enemies[i].y;
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
                    if (!isDashing && enemies[i].x < posX + playerSize && enemies[i].x + boxSize > posX &&
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
                    if (isMultiplayer && isNetHost && !p2Dashing &&
                        enemies[i].x < p2X + playerSize && enemies[i].x + boxSize > p2X &&
                        enemies[i].y < p2Y + playerSize && enemies[i].y + boxSize > p2Y)
                    {
                        bool isTank2 = i < enemyIsTank.Count && enemyIsTank[i];
                        float dmg2 = isTank2 ? enemyDamage * 3f : enemyDamage;
                        p2Health -= dmg2 * deltaTime * 2f;
                        if (p2Health <= 0) p2Health = 0;
                    }
                    if (i < enemyCanShoot.Count && enemyCanShoot[i])
                    {
                        enemyShootTimers[i] += deltaTime;
                        if (enemyShootTimers[i] >= enemyShootRate)
                        {
                            enemyShootTimers[i] = 0f;
                            float targetX = decoyActive ? decoyX : posX + playerSize / 2;
                            float targetY = decoyActive ? decoyY : posY + playerSize / 2;
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
                    if (dist2 < boxSize * 2)
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
                    if (lastStand && health <= 15f) bulletDmg *= 2f;
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
                        if (lastStand && health <= 15f) bulletDmg *= 2f;
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

            // Parasites
            var newParasites = new List<(float x, float y, float velX, float velY, float timer, float spawnDelay, float hitCooldown)>();
            var parasitesCopy = new List<(float x, float y, float velX, float velY, float timer, float spawnDelay, float hitCooldown)>(parasites);
            foreach (var p in parasitesCopy)
            {
                float newTimer = p.timer - deltaTime;
                if (newTimer <= 0) continue;
                float newDelay = Math.Max(0f, p.spawnDelay - deltaTime);
                float newHitCooldown = Math.Max(0f, p.hitCooldown - deltaTime);

                float dx = posX + playerSize / 2 - p.x;
                float dy = posY + playerSize / 2 - p.y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
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

                if (newDelay <= 0 &&
    newX + parasiteSize > posX && newX < posX + playerSize &&
    newY + parasiteSize > posY && newY < posY + playerSize)
                {
                    if (!parasiteImmune && newHitCooldown <= 0)
                    {
                        health -= enemyDamage * 0.5f;
                        newHitCooldown = 0.5f;
                        if (health <= 0)
                            HandlePlayerDeath();
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
                if (nx < posX + playerSize && nx + enemyBulletSize > posX &&
                    ny < posY + playerSize && ny + enemyBulletSize > posY)
                {
                    if (hitCooldown <= 0)
                    {
                        health -= enemyBulletDamage;
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
                if (nx > 0 && nx < ClientSize.Width && ny > 0 && ny < ClientSize.Height)
                    newEnemyBullets.Add((nx, ny, b.velX, b.velY));
            }
            enemyBullets = newEnemyBullets;

            var newCoins = new List<(float x, float y, float velX, float velY)>();
            foreach (var c in coins)
            {
                float dirX = posX - c.x;
                float dirY = posY - c.y;
                float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
                float newVelX = c.velX;
                float newVelY = c.velY;
                if (dist > 0) { newVelX += (dirX / dist) * (boxSize / 5f) * deltaTime * 60; newVelY += (dirY / dist) * (boxSize / 5f) * deltaTime * 60; }
                newVelX *= 0.9f;
                newVelY *= 0.9f;
                float nx = c.x + newVelX * deltaTime * 60;
                float ny = c.y + newVelY * deltaTime * 60;
                if (nx < posX + boxSize && nx + coinSize > posX && ny < posY + boxSize && ny + coinSize > posY)
                {
                    score += coinWorth;
                    totalScore += coinWorth;
                    totalCoinsCollected++;
                    if (medic) health = Math.Min(health + 0.5f, maxHealth);
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
            regenTimer += deltaTime;
            if (regenTimer >= regenTime) { health = Math.Min(health + 1f, maxHealth); regenTimer = 0f; }

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
                netManager.PollEvents();

                if (isNetHost)
                {
                    // Apply P2 input from client
                    ApplyP2Input(latestP2Input);

                    // Send game state to client
                    var state = BuildGameStatePacket();
                    netManager.SendGameState(state);
                }
                else
                {
                    // Client: send our input to host
                    var input = new PlayerInputPacket
                    {
                        MoveX = velocityX,
                        MoveY = velocityY,
                        AimX = mousePos.X,
                        AimY = mousePos.Y,
                        Shooting = mouseHeld,
                        Dashing = isDashing
                    };
                    netManager.SendPlayerInput(input);

                    // Apply latest state from host
                    if (latestGameState.HasValue)
                        ApplyGameState(latestGameState.Value);
                }
            }

            this.Invalidate();
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

                // Controls bottom
                e.Graphics.DrawString("WASD: Move  |  LMB: Shoot  |  Space: Dash  |  Tab: Upgrades  |  ESC: Pause  |  MMB: Inspect",
                    new Font("Arial", 11 * scaleY),
                    new SolidBrush(Color.FromArgb(80, 80, 80)),
                    new RectangleF(0, ClientSize.Height - 40 * scaleY, ClientSize.Width, 30 * scaleY),
                    new StringFormat { Alignment = StringAlignment.Center });

                // Draw blue player square
                int mSize = (int)(40 * scale);
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
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
                if (!onPreferences)
                {
                    var dLabels = new[] { "⭐ Easy", "⭐⭐ Normal", "⭐⭐⭐ Hard", "💀 Nightmare" };
                    var dColors = new[] { Color.LimeGreen, Color.DodgerBlue, Color.Orange, Color.Red };
                    var dLocked = new[] { false, !difficultyUnlocked_Normal, !difficultyUnlocked_Hard, !difficultyUnlocked_Nightmare };

                    for (int d = 0; d < 4; d++)
                    {
                        Color c = dLocked[d] ? Color.FromArgb(60, 60, 60) : dColors[d];
                        e.Graphics.DrawString(dLocked[d] ? "🔒 Locked" : "✓ Unlocked",
                            new Font("Arial", 9 * scaleY),
                            new SolidBrush(c),
                            ClientSize.Width * 0.6f,
                            ClientSize.Height / 2 - 80 + d * 60);
                        e.Graphics.DrawString(dLabels[d],
                            new Font("Arial", 12 * scaleY, FontStyle.Bold),
                            new SolidBrush(c),
                            ClientSize.Width * 0.6f,
                            ClientSize.Height / 2 - 60 + d * 60);
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
                    string[] diffBorderColors = { "EASY", "NORMAL", "HARD", "NIGHTMARE" };
                    Color[] panelBorderColors = { Color.LimeGreen, Color.DodgerBlue, Color.Orange, Color.Red };
                    Color panelBorder = unlockedDifficultyIndex >= 0 && unlockedDifficultyIndex < 4
                        ? panelBorderColors[unlockedDifficultyIndex] : Color.White;
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
                        Color ringColor = unlockedDifficultyIndex >= 0 && unlockedDifficultyIndex < 4
                            ? diffColors[unlockedDifficultyIndex] : Color.White;
                        e.Graphics.DrawEllipse(
                            new Pen(Color.FromArgb(ringAlpha, ringColor), 4),
                            ClientSize.Width / 2f - ringRadius,
                            ClientSize.Height / 2f - ringRadius,
                            ringRadius * 2, ringRadius * 2);
                    }

                    // Main text
                    string[] names = { "EASY", "NORMAL", "HARD", "NIGHTMARE" };
                    Color[] colors = { Color.LimeGreen, Color.DodgerBlue, Color.Orange, Color.Red };
                    string unlockedName = unlockedDifficultyIndex >= 0 && unlockedDifficultyIndex < 4
                        ? names[unlockedDifficultyIndex] : "";
                    Color unlockedColor = unlockedDifficultyIndex >= 0 && unlockedDifficultyIndex < 4
                        ? colors[unlockedDifficultyIndex] : Color.White;

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
                        1 => new[] { "Enemy Speed: 1x", "Boss Timer: 120s", "Score Multiplier: 1x", "Parasitic Enemies Spawn" },
                        2 => new[] { "Enemy Speed: 1.2x", "Enemy Damage: 1.5x", "Boss Timer: 90s", "Score Multiplier: 2x", "Parasitic Chance: 5x" },
                        3 => new[] { "Enemy Speed: +1.4x", "Enemy Damage: 2x", "Boss Timer: 60s", "Parasite Chance: 10x", "Score Multiplier: 2x" },
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

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                float r = playerSize / 5;
                path.AddArc(posX, posY, r, r, 180, 90);
                path.AddArc(posX + playerSize - r, posY, r, r, 270, 90);
                path.AddArc(posX + playerSize - r, posY + playerSize - r, r, r, 0, 90);
                path.AddArc(posX, posY + playerSize - r, r, r, 90, 90);
                path.CloseFigure();
                e.Graphics.FillPath(new SolidBrush(playerColor), path);
                e.Graphics.DrawPath(borderPen, path);
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
                    Color p2Color = p2Dashing ? Color.FromArgb(150, 100, 200, 255) : Color.FromArgb(255, 80, 140, 255);
                    e.Graphics.FillPath(new SolidBrush(p2Color), p2path);
                    e.Graphics.DrawPath(new Pen(Color.FromArgb(40, 80, 160), 2), p2path);
                }
                // P2 name tag
                using (var nameBrush = new SolidBrush(Color.FromArgb(180, 80, 140, 255)))
                {
                    e.Graphics.DrawString(p2Name, GetFontSmall(), nameBrush,
                        p2X + playerSize / 2 - 20, p2Y - 18);
                }
                // P2 health bar
                if (p2MaxHealth > 0)
                {
                    float hbW = playerSize + 10;
                    float hbX = p2X - 5;
                    float hbY = p2Y + playerSize + 4;
                    float p2HpFill = Math.Max(0, p2Health / p2MaxHealth);
                    e.Graphics.FillRectangle(Brushes.DarkGray, hbX, hbY, hbW, 4);
                    e.Graphics.FillRectangle(Brushes.DodgerBlue, hbX, hbY, hbW * p2HpFill, 4);
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
                    int enemyBarH = 4;
                    float enemyBarX = enemies[i].x;
                    float enemyBarY = enemies[i].y - 8;
                    e.Graphics.FillRectangle(Brushes.DarkRed, enemyBarX, enemyBarY, enemyBarW, enemyBarH);
                    e.Graphics.FillRectangle(Brushes.LimeGreen, enemyBarX, enemyBarY, enemyBarW * enemyHpFill, enemyBarH);
                    e.Graphics.DrawRectangle(borderPen, enemyBarX, enemyBarY, enemyBarW, enemyBarH);
                }

                // Effect indicators
                if (i < enemyIsArmored.Count && enemyIsArmored[i] && !enemyArmorBroken[i])
                {
                    e.Graphics.DrawEllipse(new Pen(Color.Silver, 3),
                        enemies[i].x - 3, enemies[i].y - 3, eSize + 6, eSize + 6);
                }
                if (i < enemyIsCharging.Count && enemyIsCharging[i] && i < enemyIsCharging_Active.Count && enemyIsCharging_Active[i])
                {
                    e.Graphics.DrawEllipse(new Pen(Color.Yellow, 2),
                        enemies[i].x - 3, enemies[i].y - 3, eSize + 6, eSize + 6);
                }
                if (i < enemyIsReflective.Count && enemyIsReflective[i])
                {
                    e.Graphics.DrawEllipse(new Pen(Color.FromArgb(100, 200, 200, 255), 2),
                        enemies[i].x - 2, enemies[i].y - 2, eSize + 4, eSize + 4);
                }
                if (i < enemyIsBerserker.Count && enemyIsBerserker[i])
                {
                    float maxHpDraw = isTank ? 8f : canShoot ? 4f : isRunner ? 1f : 2f;
                    if (i < enemyHealth.Count && enemyHealth[i] < maxHpDraw * 0.5f)
                    {
                        float pulse = (float)Math.Abs(Math.Sin(gameStartTimer * 8f));
                        e.Graphics.DrawEllipse(new Pen(Color.FromArgb((int)(pulse * 255), 255, 50, 0), 2),
                            enemies[i].x - 3, enemies[i].y - 3, eSize + 6, eSize + 6);
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
            foreach (var b in bullets)
            {
                e.Graphics.FillRectangle(darkMode ? Brushes.White : Brushes.DarkRed, b.x, b.y, bulletSize, bulletSize);
                e.Graphics.DrawRectangle(darkMode ? Pens.LightGray : Pens.Black, b.x, b.y, bulletSize, bulletSize);
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
            if (isPaused)
            {
                e.Graphics.DrawString("Game Paused", new Font(Ufont, 32 * scaleY), textBrush, ClientSize.Width / 2 - 120, ClientSize.Height / 2 - 20);
                e.Graphics.DrawString("Press ESC", GetFontUI(), textBrush, ClientSize.Width / 2 - 15, ClientSize.Height / 2 + 20);
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

            int hpBarWidth = (int)(200 * scaleX);
            int hpBarHeight = (int)(32 * scaleY);
            int hpBarX = (int)(20 * scaleX);
            int hpBarY = (int)(30 * scaleY);
            e.Graphics.FillRectangle(Brushes.Gray, hpBarX, hpBarY, hpBarWidth, hpBarHeight);
            float nameFontSize = 14f * scaleY;
            Font nameFont = new Font(Ufont, nameFontSize, FontStyle.Bold);
            SizeF nameSize = e.Graphics.MeasureString(playerName, nameFont);
            int nameBoxWidth = Math.Max(hpBarWidth / 3, (int)(nameSize.Width + 10));
            float hpFill = health / maxHealth;
            e.Graphics.DrawRectangle(borderPen, hpBarX, hpBarY - 10 * scaleY, hpBarWidth + 10, hpBarHeight + 60 * scaleY);
            e.Graphics.FillRectangle(Brushes.Goldenrod, hpBarX - 5, hpBarY - 25 * scaleY, nameBoxWidth, hpBarHeight + 75 * scaleY);
            e.Graphics.DrawRectangle(borderPen, hpBarX - 5, hpBarY - 25 * scaleY, nameBoxWidth, hpBarHeight + 75 * scaleY);
            e.Graphics.FillRectangle(Brushes.Goldenrod, hpBarX, hpBarY - 10 * scaleY, hpBarWidth + 10, hpBarHeight + (60 * scaleY));
            e.Graphics.FillRectangle(Brushes.DarkRed, hpBarX, hpBarY, hpBarWidth, hpBarHeight / 2);
            e.Graphics.FillRectangle(Brushes.Lime, hpBarX, hpBarY, (int)(hpBarWidth * hpFill), hpBarHeight / 2);
            e.Graphics.DrawString(" " + (int)health, GetFontUI(), barTextBrush, hpBarX, hpBarY);
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

            e.Graphics.DrawString("$: " + score, GetFontUI(), Brushes.Black, 22 * scaleX, (int)(90 * scaleY));

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
            string[] diffLabels = { "EASY", "NORMAL", "HARD", "NIGHTMARE" };
            Color[] diffLabelColors = {
    Color.LimeGreen,
    Color.DodgerBlue,
    Color.Orange,
    Color.Red
};
            e.Graphics.DrawString(diffLabels[difficulty],
                new Font("Arial", 10 * scaleY, FontStyle.Bold),
                new SolidBrush(diffLabelColors[difficulty]),
                22 * scaleX, (int)(105 * scaleY));

            if (lastStand && health <= 15f)
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
            else if (e.Button == MouseButtons.Right && blink && blinkCooldown <= 0)
            {
                posX = Math.Max(0, Math.Min(mousePos.X - boxSize / 2, ClientSize.Width - boxSize));
                posY = Math.Max(0, Math.Min(mousePos.Y - boxSize / 2, ClientSize.Height - boxSize));
                blinkCooldown = blinkCooldownTime;
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
            float dirX = mousePos.X - (posX + playerSize / 2);
            float dirY = mousePos.Y - (posY + playerSize / 2);
            float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            if (dist > 0)
            {
                float velX = (dirX / dist) * bulletSpeed;
                float velY = (dirY / dist) * bulletSpeed;
                if (doubleTap)
                {
                    doubleTapCounter++;
                    if (doubleTapCounter >= 5)
                    {
                        doubleTapCounter = 0;
                        float spread = 0.3f;
                        bullets.Add((posX + playerSize / 2, posY + playerSize / 2, velX, velY, 0));
                        bullets.Add((posX + playerSize / 2, posY + playerSize / 2,
                            velX * (float)Math.Cos(spread) - velY * (float)Math.Sin(spread),
                            velX * (float)Math.Sin(spread) + velY * (float)Math.Cos(spread), 0));
                        bullets.Add((posX + playerSize / 2, posY + playerSize / 2,
                            velX * (float)Math.Cos(-spread) - velY * (float)Math.Sin(-spread),
                            velX * (float)Math.Sin(-spread) + velY * (float)Math.Cos(-spread), 0));
                        return;
                    }
                }
                bullets.Add((posX + playerSize / 2, posY + playerSize / 2, velX, velY, 0));
            }
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
            scoreTimer = 0f; shootCooldown = 0f; gameStartTimer = 0f;
            superActive = false; superTimer = 0f; superCooldown = 0f; superCooldownTime = 90f;
            mouseHeld = false; wallActive = false; wallTimer = 0f; wallCooldown = 0f;
            maxAmmo = 60; ammo = maxAmmo; reloading = false; reloadTimer = 0f;
            maxHealth = 50f; health = maxHealth; hitCooldown = 0f; regenTimer = 0f;
            dashCooldown = 0f; lifeSteal = 0; fireRateBonus = 0f; scorePerSecond = 1;
            reloadTime = 5f; dashDuration = 0.7f; wallDuration = 20f;
            ghostDash = false; ricochetBounces = 0;
            bulletSize = (int)(6 * scale);
            afterburn = false; isAfterburn = false; afterburnTimer = 0f;
            blink = false; blinkCooldown = 0f; jackpot = false;
            speed = 4.8f * scale; playerSize = (int)(30 * scale);
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

        private void ShowUpgradeMenu()
        {
            Form upgradeForm = new Form();
            upgradeForm.Text = "Upgrades";
            upgradeForm.Size = new Size(900, 490);
            upgradeForm.StartPosition = FormStartPosition.CenterScreen;
            upgradeForm.BackColor = Color.FromArgb(30, 30, 30);
            upgradeForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            upgradeForm.MaximizeBox = false;

            Button upgradesNavBtn = new Button();
            upgradesNavBtn.Text = "⚔ Upgrades";
            upgradesNavBtn.Size = new Size(120, 30);
            upgradesNavBtn.Location = new Point(20, 10);
            upgradesNavBtn.FlatStyle = FlatStyle.Flat;
            upgradesNavBtn.ForeColor = Color.White;
            upgradesNavBtn.BackColor = Color.FromArgb(80, 80, 130);
            upgradesNavBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            upgradeForm.Controls.Add(upgradesNavBtn);

            Button colorNavBtn = new Button();
            colorNavBtn.Text = "🎨 Preferences";
            colorNavBtn.Size = new Size(120, 30);
            colorNavBtn.Location = new Point(150, 10);
            colorNavBtn.FlatStyle = FlatStyle.Flat;
            colorNavBtn.ForeColor = Color.White;
            colorNavBtn.BackColor = Color.FromArgb(50, 50, 50);
            colorNavBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            upgradeForm.Controls.Add(colorNavBtn);

            Label scoreLabel = new Label();
            scoreLabel.Text = "💲: " + score;
            scoreLabel.Font = new Font("Arial", 12, FontStyle.Bold);
            scoreLabel.ForeColor = Color.Gold;
            scoreLabel.Size = new Size(500, 30);
            scoreLabel.Location = new Point(300, 10);
            upgradeForm.Controls.Add(scoreLabel);

            Button darkModeBtn = new Button();
            darkModeBtn.Text = darkMode ? "☀ Light Mode" : "🌙 Dark Mode";
            darkModeBtn.Size = new Size(120, 30);
            darkModeBtn.Location = new Point(upgradeForm.ClientSize.Width - 140, 10);
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
                new { Title = "Minigun Trait",   Icon = "⚡", Description = "Fire rate +0.1",                    Cost = 650,  Stack = "Stacks", Category = "Offensive" },
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
            };

            int cardWidth = 155;
            int cardHeight = 290;
            int totalWidth = upgrades.Length * (cardWidth + 15) + 15;
            int scrollOffset = 0;
            int scrollStep = cardWidth + 15;
            int maxScroll = Math.Max(0, totalWidth - 800);
            string selectedCategory = "All";

            var categories = new[] { "All", "Offensive", "Defensive", "Economy", "Cooldown" };
            List<Button> tabButtons = new List<Button>();
            int tabX = 20;

            Panel scrollPanel = new Panel();
            scrollPanel.Size = new Size(800, cardHeight + 10);
            scrollPanel.Location = new Point(50, 80);
            scrollPanel.BackColor = Color.FromArgb(30, 30, 30);
            upgradeForm.Controls.Add(scrollPanel);

            Panel innerPanel = new Panel();
            innerPanel.Size = new Size(totalWidth, cardHeight + 10);
            innerPanel.Location = new Point(0, 0);
            innerPanel.BackColor = Color.FromArgb(30, 30, 30);
            scrollPanel.Controls.Add(innerPanel);

            Panel colorPanel = new Panel();
            colorPanel.Size = new Size(860, 370);
            colorPanel.Location = new Point(20, 80);
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
            colorTitle.Font = new Font("Arial", 14, FontStyle.Bold);
            colorTitle.ForeColor = Color.White;
            colorTitle.Size = new Size(400, 30);
            colorTitle.Location = new Point(20, 10);
            colorPanel.Controls.Add(colorTitle);

            Panel previewBox = new Panel();
            previewBox.Size = new Size(60, 60);
            previewBox.BackColor = playerColor;
            previewBox.BorderStyle = BorderStyle.FixedSingle;
            colorPanel.Controls.Add(previewBox);

            Label previewLabel = new Label();
            previewLabel.Text = "Preview";
            previewLabel.ForeColor = Color.LightGray;
            previewLabel.Font = new Font("Arial", 9);
            previewLabel.Size = new Size(60, 20);
            previewLabel.TextAlign = ContentAlignment.MiddleCenter;
            colorPanel.Controls.Add(previewLabel);

            int colorX = 20;
            int colorY = 50;
            foreach (var preset in presetColors)
            {
                var capturedColor = preset.Color;
                var capturedName = preset.Name;
                Panel colorBtn = new Panel();
                colorBtn.Size = new Size(80, 80);
                colorBtn.Location = new Point(colorX, colorY);
                colorBtn.BackColor = preset.Color;
                colorBtn.Cursor = Cursors.Hand;
                colorBtn.BorderStyle = BorderStyle.FixedSingle;
                Label colorName = new Label();
                colorName.Text = capturedName;
                colorName.Font = new Font("Arial", 8);
                colorName.ForeColor = Color.White;
                colorName.BackColor = Color.Transparent;
                colorName.TextAlign = ContentAlignment.BottomCenter;
                colorName.Size = new Size(80, 80);
                colorName.Location = new Point(0, 0);
                colorBtn.Controls.Add(colorName);
                colorBtn.Click += (s, e) => { playerColor = capturedColor; previewBox.BackColor = capturedColor; };
                colorName.Click += (s, e) => { playerColor = capturedColor; previewBox.BackColor = capturedColor; };
                colorBtn.MouseEnter += (s, e) => colorBtn.BorderStyle = BorderStyle.Fixed3D;
                colorBtn.MouseLeave += (s, e) => colorBtn.BorderStyle = BorderStyle.FixedSingle;
                colorPanel.Controls.Add(colorBtn);
                colorX += 90;
                if (colorX > 650) { colorX = 20; colorY += 90; }
            }

            previewBox.Location = new Point(20, colorY + 95);
            previewLabel.Location = new Point(20, colorY + 158);

            Button customColorBtn = new Button();
            customColorBtn.Text = "🎨 Custom Color...";
            customColorBtn.Size = new Size(160, 35);
            customColorBtn.Location = new Point(100, colorY + 95);
            customColorBtn.FlatStyle = FlatStyle.Flat;
            customColorBtn.ForeColor = Color.White;
            customColorBtn.BackColor = Color.FromArgb(60, 60, 60);
            customColorBtn.Click += (s, e) =>
            {
                using ColorDialog cd = new ColorDialog();
                cd.Color = playerColor;
                if (cd.ShowDialog() == DialogResult.OK) { playerColor = cd.Color; previewBox.BackColor = cd.Color; }
            };
            colorPanel.Controls.Add(customColorBtn);

            Label nameLabel = new Label();
            nameLabel.Text = "Player Name:";
            nameLabel.Font = new Font("Arial", 10, FontStyle.Bold);
            nameLabel.ForeColor = Color.White;
            nameLabel.Size = new Size(120, 25);
            nameLabel.Location = new Point(20, colorY + 140);
            colorPanel.Controls.Add(nameLabel);

            TextBox nameBox = new TextBox();
            nameBox.Text = playerName;
            nameBox.Font = new Font("Arial", 10);
            nameBox.Size = new Size(150, 25);
            nameBox.Location = new Point(150, colorY + 140);
            nameBox.MaxLength = 8;
            nameBox.BackColor = Color.FromArgb(50, 50, 50);
            nameBox.ForeColor = Color.White;
            nameBox.BorderStyle = BorderStyle.FixedSingle;
            nameBox.TextChanged += (s, e) =>
            {
                string input = nameBox.Text;
                playerName = string.IsNullOrWhiteSpace(input) ? "YOU" : input.ToUpper();
            };
            colorPanel.Controls.Add(nameBox);

            Label nameLimitLabel = new Label();
            nameLimitLabel.Text = "Max 8 characters";
            nameLimitLabel.Font = new Font("Arial", 8);
            nameLimitLabel.ForeColor = Color.Gray;
            nameLimitLabel.Size = new Size(150, 20);
            nameLimitLabel.Location = new Point(150, colorY + 168);
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
                        if (show) { c.Location = new Point(15 + visibleIndex * (cardWidth + 15), 5); visibleIndex++; }
                    }
                }
                int newTotal = Math.Max(visibleIndex * (cardWidth + 15) + 15, scrollPanel.Width);
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
                tab.Size = new Size(80, 25);
                tab.Location = new Point(tabX, 45);
                tab.FlatStyle = FlatStyle.Flat;
                tab.ForeColor = Color.White;
                tab.BackColor = cat == "All" ? Color.FromArgb(80, 80, 130) : Color.FromArgb(50, 50, 50);
                tab.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
                tab.Click += (s, e) =>
                {
                    selectedCategory = capturedCat;
                    foreach (var tb in tabButtons) tb.BackColor = Color.FromArgb(50, 50, 50);
                    tab.BackColor = Color.FromArgb(80, 80, 130);
                    RefreshCardPositions();
                };
                tabButtons.Add(tab);
                upgradeForm.Controls.Add(tab);
                tab.BringToFront();
                tabX += 85;
            }

            System.Windows.Forms.Timer colorTimer = new System.Windows.Forms.Timer();
            colorTimer.Interval = 50;
            colorTimer.Tick += (s, e) =>
            {
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
                    }
                }
                scoreLabel.Text = "💲: " + score;
            };
            colorTimer.Start();
            upgradeForm.FormClosed += (s, e) => colorTimer.Stop();

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
                card.Location = new Point(15 + i * (cardWidth + 15), 5);
                card.BackColor = alreadyPurchased ? Color.FromArgb(25, 25, 25) : score >= cost ? Color.FromArgb(50, 50, 80) : Color.FromArgb(40, 40, 40);
                card.Cursor = alreadyPurchased ? Cursors.Default : score >= cost ? Cursors.Hand : Cursors.Default;
                card.Tag = alreadyPurchased ? int.MaxValue : cost;
                card.AccessibleName = category;

                Panel categoryBar = new Panel();
                categoryBar.Size = new Size(cardWidth, 8);
                categoryBar.Location = new Point(0, 0);
                categoryBar.BackColor = catColor;
                card.Controls.Add(categoryBar);

                Label title = new Label();
                title.Text = upgrade.Title;
                title.Font = new Font("Arial", 10, FontStyle.Bold);
                title.ForeColor = Color.White;
                title.TextAlign = ContentAlignment.MiddleCenter;
                title.Size = new Size(cardWidth, 40);
                title.Location = new Point(0, 10);
                title.AutoSize = false;

                Label icon = new Label();
                icon.Text = upgrade.Icon;
                icon.Font = new Font("Segoe UI Emoji", 34);
                icon.ForeColor = Color.White;
                icon.TextAlign = ContentAlignment.MiddleCenter;
                icon.Size = new Size(cardWidth, 100);
                icon.Location = new Point(0, 55);
                icon.AutoSize = false;

                Label desc = new Label();
                desc.Text = upgrade.Description;
                desc.Font = new Font("Arial", 9);
                desc.ForeColor = Color.LightGray;
                desc.TextAlign = ContentAlignment.MiddleCenter;
                desc.Size = new Size(cardWidth - 10, 60);
                desc.Location = new Point(5, 155);
                desc.AutoSize = false;

                Label stacking = new Label();
                stacking.Text = upgrade.Stack;
                stacking.Font = new Font("Arial", 7);
                stacking.ForeColor = Color.LightGray;
                stacking.TextAlign = ContentAlignment.MiddleCenter;
                stacking.Size = new Size(cardWidth - 10, 20);
                stacking.Location = new Point(5, 210);
                stacking.AutoSize = false;

                Label categoryLabel = new Label();
                categoryLabel.Text = GetCategoryIcon(category) + " " + category;
                categoryLabel.Font = new Font("Segoe UI Emoji", 7);
                categoryLabel.ForeColor = Color.FromArgb(180, 180, 180);
                categoryLabel.TextAlign = ContentAlignment.MiddleCenter;
                categoryLabel.Size = new Size(cardWidth, 20);
                categoryLabel.Location = new Point(0, 230);
                categoryLabel.AutoSize = false;

                Label costLabel = new Label();
                costLabel.Text = cost + " pts";
                costLabel.Font = new Font("Arial", 9, FontStyle.Bold);
                costLabel.ForeColor = score >= cost ? Color.Gold : Color.Gray;
                costLabel.TextAlign = ContentAlignment.MiddleCenter;
                costLabel.Size = new Size(cardWidth, 30);
                costLabel.Location = new Point(0, 253);
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
                    score -= cost;
                    totalUpgradesPurchased++;
                    if (cashback) totalSpentSinceLastCashback += cost;
                    switch (index)
                    {
                        case 0: wallLength += boxSize; wallDuration += 5; break;
                        case 1: maxHealth += 10; health = Math.Min(health + 10, maxHealth); break;
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
                        case 14: maxAmmo += 5; ammo = Math.Min(ammo + 5, maxAmmo); break;
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
                innerPanel.Controls.Add(card);
            }

            Button scrollLeft = new Button();
            scrollLeft.Text = "◀";
            scrollLeft.Size = new Size(40, cardHeight + 10);
            scrollLeft.Location = new Point(5, 80);
            scrollLeft.BackColor = Color.FromArgb(50, 50, 50);
            scrollLeft.ForeColor = Color.White;
            scrollLeft.FlatStyle = FlatStyle.Flat;
            scrollLeft.Click += (s, e) => { scrollOffset = Math.Max(0, scrollOffset - scrollStep); innerPanel.Location = new Point(-scrollOffset, 0); };
            upgradeForm.Controls.Add(scrollLeft);
            scrollLeft.BringToFront();

            Button scrollRight = new Button();
            scrollRight.Text = "▶";
            scrollRight.Size = new Size(40, cardHeight + 10);
            scrollRight.Location = new Point(855, 80);
            scrollRight.BackColor = Color.FromArgb(50, 50, 50);
            scrollRight.ForeColor = Color.White;
            scrollRight.FlatStyle = FlatStyle.Flat;
            scrollRight.Click += (s, e) => { scrollOffset = Math.Min(maxScroll, scrollOffset + scrollStep); innerPanel.Location = new Point(-scrollOffset, 0); };
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
            close.Size = new Size(100, 35);
            close.Location = new Point(upgradeForm.ClientSize.Width / 2 - 50, 420);
            close.BackColor = Color.FromArgb(80, 30, 30);
            close.ForeColor = Color.White;
            close.FlatStyle = FlatStyle.Flat;
            close.Click += (s, e) => upgradeForm.Close();
            upgradeForm.Controls.Add(close);

            upgradeForm.ShowDialog();
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
            Form deathForm = new Form();
            deathForm.Text = "Game Over";
            deathForm.Size = new Size(500, 420);
            deathForm.StartPosition = FormStartPosition.CenterScreen;
            deathForm.BackColor = Color.FromArgb(30, 30, 30);
            deathForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            deathForm.MaximizeBox = false;

            Label titleLabel = new Label();
            titleLabel.Text = "GAME OVER";
            titleLabel.Font = new Font("Arial", 28, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(200, 50, 50);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Size = new Size(460, 50);
            titleLabel.Location = new Point(20, 20);
            deathForm.Controls.Add(titleLabel);

            Panel divider = new Panel();
            divider.Size = new Size(440, 2);
            divider.Location = new Point(30, 75);
            divider.BackColor = Color.FromArgb(80, 80, 80);
            deathForm.Controls.Add(divider);

            Panel statsPanel = new Panel();
            statsPanel.Size = new Size(440, 180);
            statsPanel.Location = new Point(30, 85);
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
                row.Size = new Size(440, 55);
                row.Location = new Point(0, i * 58 + 5);
                row.BackColor = i % 2 == 0 ? Color.FromArgb(45, 45, 55) : Color.FromArgb(38, 38, 48);

                Label iconLbl = new Label();
                iconLbl.Text = stats[i].Icon;
                iconLbl.Font = new Font("Segoe UI Emoji", 18);
                iconLbl.ForeColor = Color.White;
                iconLbl.Size = new Size(50, 55);
                iconLbl.Location = new Point(15, 0);
                iconLbl.TextAlign = ContentAlignment.MiddleCenter;
                row.Controls.Add(iconLbl);

                Label nameLbl = new Label();
                nameLbl.Text = stats[i].Label;
                nameLbl.Font = new Font("Arial", 11);
                nameLbl.ForeColor = Color.FromArgb(180, 180, 180);
                nameLbl.Size = new Size(200, 55);
                nameLbl.Location = new Point(70, 0);
                nameLbl.TextAlign = ContentAlignment.MiddleLeft;
                row.Controls.Add(nameLbl);

                Label valueLbl = new Label();
                valueLbl.Text = stats[i].Value;
                valueLbl.Font = new Font("Arial", 14, FontStyle.Bold);
                valueLbl.ForeColor = Color.Gold;
                valueLbl.Size = new Size(150, 55);
                valueLbl.Location = new Point(270, 0);
                valueLbl.TextAlign = ContentAlignment.MiddleRight;
                row.Controls.Add(valueLbl);

                statsPanel.Controls.Add(row);
            }

            Panel divider2 = new Panel();
            divider2.Size = new Size(440, 2);
            divider2.Location = new Point(30, 275);
            divider2.BackColor = Color.FromArgb(80, 80, 80);
            deathForm.Controls.Add(divider2);

            Button retryBtn = new Button();
            retryBtn.Text = "▶  Retry";
            retryBtn.Size = new Size(180, 45);
            retryBtn.Location = new Point(60, 295);
            retryBtn.BackColor = Color.FromArgb(40, 100, 40);
            retryBtn.ForeColor = Color.White;
            retryBtn.FlatStyle = FlatStyle.Flat;
            retryBtn.FlatAppearance.BorderColor = Color.FromArgb(60, 140, 60);
            retryBtn.Font = new Font("Arial", 12, FontStyle.Bold);
            retryBtn.Click += (s, e) => { retry = true; deathForm.Close(); };
            deathForm.Controls.Add(retryBtn);

            Button quitBtn = new Button();
            quitBtn.Text = "✕  Quit";
            quitBtn.Size = new Size(180, 45);
            quitBtn.Location = new Point(260, 295);
            quitBtn.BackColor = Color.FromArgb(100, 40, 40);
            quitBtn.ForeColor = Color.White;
            quitBtn.FlatStyle = FlatStyle.Flat;
            quitBtn.FlatAppearance.BorderColor = Color.FromArgb(140, 60, 60);
            quitBtn.Font = new Font("Arial", 12, FontStyle.Bold);
            quitBtn.Click += (s, e) => { retry = false; deathForm.Close(); };
            deathForm.Controls.Add(quitBtn);

            retryBtn.MouseEnter += (s, e) => retryBtn.BackColor = Color.FromArgb(50, 130, 50);
            retryBtn.MouseLeave += (s, e) => retryBtn.BackColor = Color.FromArgb(40, 100, 40);
            quitBtn.MouseEnter += (s, e) => quitBtn.BackColor = Color.FromArgb(130, 50, 50);
            quitBtn.MouseLeave += (s, e) => quitBtn.BackColor = Color.FromArgb(100, 40, 40);

            deathForm.ShowDialog();
            return retry;
        }

        private void DamageEnemy(int i, float damage, bool canShrapnel = true)
        {
            if (i < 0 || i >= enemies.Count || i >= enemyHealth.Count || i >= enemyAlive.Count) return;
            if (!enemyAlive[i]) return;

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
                }
                enemyAlive[i] = false;
                int flashSize = enemyIsTank.Count > i && enemyIsTank[i] ? boxSize + 20 :
                enemyCanShoot.Count > i && enemyCanShoot[i] ? boxSize + 8 :
                enemyIsRunner.Count > i && enemyIsRunner[i] ? boxSize - 8 : boxSize;
                deathFlashes.Add((enemies[i].x + flashSize / 2, enemies[i].y + flashSize / 2, 0.4f, 0.4f, flashSize));
                enemyRespawnTimers[i] = enemyRespawnTime;
                totalKills++;
                health = Math.Min(health + lifeSteal, maxHealth);
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
                if (difficulty == 0) { difficultyUnlocked_Normal = true; pendingUnlockAnimation = 1; }
                else if (difficulty == 1) { difficultyUnlocked_Hard = true; pendingUnlockAnimation = 2; }
                else if (difficulty == 2) { difficultyUnlocked_Nightmare = true; pendingUnlockAnimation = 3; }
            }
            SaveDifficultyUnlocks();
            if (difficulty > highestUnlockedDifficulty)
                highestUnlockedDifficulty = difficulty;
            for (int r = 0; r < enemyRespawnTimers.Count; r++)
            {
                if (enemyRespawnTimers[r] > 0)
                    enemyRespawnTimers[r] = Math.Min(enemyRespawnTimers[r], 3f);
            }
        }

        private void HandlePlayerDeath()
        {
            parasites.Clear();
            health = 0;
            isPaused = true;
            ResetEnemies();
            enemySpawnTimer = 0f;
            runHistory.Insert(0, (totalScore, totalKills, timeAlive, difficulty, sandboxMode));
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

                var diffNames = new[] { "⭐ Easy", "⭐⭐ Normal", "⭐⭐⭐ Hard", "💀 Nightmare" };
                var diffColors = new[]
                {
        Color.FromArgb(40, 120, 40),
        Color.FromArgb(40, 80, 140),
        Color.FromArgb(140, 80, 40),
        Color.FromArgb(120, 20, 20)
    };
                var diffLocked = new[]
                {
        false,
        !difficultyUnlocked_Normal,
        !difficultyUnlocked_Hard,
        !difficultyUnlocked_Nightmare
    };

                List<Button> diffButtons = new List<Button>();
                Button backBtn2 = new Button();
                for (int d = 0; d < 4; d++)
                {
                    int captured = d;
                    Button diffBtn = new Button();
                    diffBtn.Text = diffLocked[d] ? "🔒 " + diffNames[d].Split(' ')[1] : diffNames[d];
                    diffBtn.Size = new Size(250, 50);
                    diffBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 - 80 + d * 60);
                    diffBtn.BackColor = diffLocked[d] ? Color.FromArgb(40, 40, 40) : diffColors[d];
                    diffBtn.ForeColor = diffLocked[d] ? Color.Gray : Color.White;
                    diffBtn.FlatStyle = FlatStyle.Flat;
                    diffBtn.FlatAppearance.BorderColor = diffLocked[d] ? Color.FromArgb(60, 60, 60) : Color.FromArgb(
                        Math.Min(255, diffColors[d].R + 30),
                        Math.Min(255, diffColors[d].G + 30),
                        Math.Min(255, diffColors[d].B + 30));
                    diffBtn.Font = new Font("Arial", 14, FontStyle.Bold);
                    diffBtn.Cursor = diffLocked[d] ? Cursors.Default : Cursors.Hand;
                    diffBtn.Enabled = !diffLocked[d];

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
                            menuPlayBtn.Visible = true;
                            menuQuitBtn.Visible = true;
                            menuPrefsBtn.Visible = true;
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
                backBtn2.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 170);
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
                    menuPlayBtn.Visible = true;
                    menuQuitBtn.Visible = true;
                    menuPrefsBtn.Visible = true;
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
                    menuPlayBtn.Visible = true;
                    menuQuitBtn.Visible = true;
                    menuPrefsBtn.Visible = true;
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
                    playerName = string.IsNullOrWhiteSpace(nameBox.Text) ? "YOU" : nameBox.Text.ToUpper();
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

            menuMultiplayerBtn = new Button();
            menuMultiplayerBtn.Text = "🌐 Multiplayer";
            menuMultiplayerBtn.Size = new Size(250, 45);
            menuMultiplayerBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 395);
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
        }
        private void ApplyDifficulty()
        {
            switch (difficulty)
            {
                case 0: // Easy
                    currentEnemySpeed = 3f * scale;
                    enemyDamage = 0.5f;
                    bossSpawnInterval_Current = 180f;
                    currentBossMaxHealth = 150f;
                    currentBossShootRate = 2.5f;
                    scoreMultiplier = 3f;
                    parasiticEnemyChance = 0f;
                    break;
                case 1: // Normal
                    currentEnemySpeed = 5f * scale;
                    enemyDamage = 1f;
                    bossSpawnInterval_Current = 120f;
                    scoreMultiplier = 1f;
                    currentBossMaxHealth = 300f;
                    currentBossShootRate = 2f;
                    parasiticEnemyChance = 0.01f;
                    break;
                case 2: // Hard
                    currentEnemySpeed = 6f * scale;
                    enemyDamage = 1.5f;
                    bossSpawnInterval_Current = 90f;
                    scoreMultiplier = 2f;
                    currentBossMaxHealth = 400f;
                    currentBossShootRate = 1.8f;
                    parasiticEnemyChance = 0.05f;
                    break;
                case 3: // Nightmare
                    currentEnemySpeed = 7f * scale;
                    enemyDamage = 2f;
                    bossSpawnInterval_Current = 60f;
                    scoreMultiplier = 2f;
                    currentBossMaxHealth = 500f;
                    currentBossShootRate = 1.5f;
                    parasiticEnemyChance = 0.1f;
                    break;
            }
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
            string saveData = "";
            if (difficultyUnlocked_Normal) saveData += "CRIMSON ";
            if (difficultyUnlocked_Hard) saveData += "PHANTOM ";
            if (difficultyUnlocked_Nightmare) saveData += "OBLIVION ";
            File.WriteAllText(GetSavePath(), saveData.Trim());
        }

        private void LoadDifficultyUnlocks()
        {
            string path = GetSavePath();
            if (!File.Exists(path)) return;
            string saveData = File.ReadAllText(path);
            difficultyUnlocked_Normal = saveData.Contains("CRIMSON");
            difficultyUnlocked_Hard = saveData.Contains("PHANTOM");
            difficultyUnlocked_Nightmare = saveData.Contains("OBLIVION");
        }
        private Button? pauseResumeBtn = null;
        private void ShowPauseButtons()
        {
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
                int savedUnlock = pendingUnlockAnimation;
                ResetGame();
                pendingUnlockAnimation = savedUnlock;
                ShowMainMenu();
            };
            this.Controls.Add(pauseResumeBtn);
            this.Controls.Add(pauseQuitBtn);
            pauseResumeBtn.BringToFront();
            pauseQuitBtn.BringToFront();
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
                Color[] colors = unlockedDifficultyIndex switch
                {
                    1 => new[] { Color.DodgerBlue, Color.Cyan, Color.White },
                    2 => new[] { Color.Orange, Color.OrangeRed, Color.Yellow },
                    3 => new[] { Color.Red, Color.DarkRed, Color.Crimson },
                    _ => new[] { Color.White, Color.LightGray, Color.Silver }
                };
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
            float chance = difficulty == 1 ? effectChance_Normal :
                           difficulty == 2 ? effectChance_Hard :
                           difficulty == 3 ? effectChance_Nightmare : 0f;
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

            enemyIsFrenzied[i] = RollEffect(1);
            enemyIsZigzag[i] = !enemyIsFrenzied[i] && RollEffect(2);
            enemyIsCharging[i] = !enemyIsFrenzied[i] && !enemyIsZigzag[i] && RollEffect(1);
            enemyIsArmored[i] = RollEffect(2);
            enemyIsRegenerating[i] = RollEffect(2);
            enemyIsReflective[i] = RollEffect(3);
            enemyIsBerserker[i] = RollEffect(1);
            enemyIsPhasing[i] = RollEffect(3);
            enemyIsCorrupted[i] = RollEffect(3);
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
                    $"{r.score}|{r.kills}|{r.time}|{r.difficulty}|{r.sandbox}");
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
                            float.Parse(parts[2]), int.Parse(parts[3]), bool.Parse(parts[4])));
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

        private void ShowRunHistory()
        {
            Form histForm = new Form();
            histForm.Text = "Run History";
            histForm.Size = new Size(700, 500);
            histForm.StartPosition = FormStartPosition.CenterScreen;
            histForm.BackColor = Color.FromArgb(20, 20, 30);
            histForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            histForm.MaximizeBox = false;

            Label title = new Label();
            title.Text = "📜 RUN HISTORY";
            title.Font = new Font("Arial", 20, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Size = new Size(660, 40);
            title.Location = new Point(20, 15);
            histForm.Controls.Add(title);

            string[] diffNames = { "Easy", "Normal", "Hard", "Nightmare" };
            Color[] diffColors = { Color.LimeGreen, Color.DodgerBlue, Color.Orange, Color.Red };

            if (runHistory.Count == 0)
            {
                Label noRuns = new Label();
                noRuns.Text = "No runs yet. Play a game first!";
                noRuns.Font = new Font("Arial", 14);
                noRuns.ForeColor = Color.Gray;
                noRuns.TextAlign = ContentAlignment.MiddleCenter;
                noRuns.Size = new Size(660, 40);
                noRuns.Location = new Point(20, 200);
                histForm.Controls.Add(noRuns);
            }
            else
            {
                for (int i = 0; i < runHistory.Count; i++)
                {
                    var run = runHistory[i];
                    int minutes = (int)(run.time / 60f);
                    int seconds = (int)(run.time % 60f);
                    int diff = Math.Max(0, Math.Min(3, run.difficulty));

                    Panel row = new Panel();
                    row.Size = new Size(650, 65);
                    row.Location = new Point(25, 65 + i * 75);
                    row.BackColor = i % 2 == 0 ? Color.FromArgb(30, 30, 45) : Color.FromArgb(25, 25, 38);
                    histForm.Controls.Add(row);

                    Label runNum = new Label();
                    runNum.Text = $"#{i + 1}";
                    runNum.Font = new Font("Arial", 14, FontStyle.Bold);
                    runNum.ForeColor = Color.Gold;
                    runNum.Size = new Size(40, 65);
                    runNum.Location = new Point(10, 0);
                    runNum.TextAlign = ContentAlignment.MiddleCenter;
                    row.Controls.Add(runNum);

                    Label diffLabel = new Label();
                    diffLabel.Text = (run.sandbox ? "🧪 " : "") + diffNames[diff];
                    diffLabel.Font = new Font("Arial", 12, FontStyle.Bold);
                    diffLabel.ForeColor = run.sandbox ? Color.MediumPurple : diffColors[diff];
                    diffLabel.Size = new Size(120, 65);
                    diffLabel.Location = new Point(55, 0);
                    diffLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(diffLabel);

                    Label scoreLabel = new Label();
                    scoreLabel.Text = $"💲 {run.score:F0}";
                    scoreLabel.Font = new Font("Arial", 11);
                    scoreLabel.ForeColor = Color.White;
                    scoreLabel.Size = new Size(150, 65);
                    scoreLabel.Location = new Point(180, 0);
                    scoreLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(scoreLabel);

                    Label killsLabel = new Label();
                    killsLabel.Text = $"💀 {run.kills} kills";
                    killsLabel.Font = new Font("Arial", 11);
                    killsLabel.ForeColor = Color.White;
                    killsLabel.Size = new Size(130, 65);
                    killsLabel.Location = new Point(340, 0);
                    killsLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(killsLabel);

                    Label timeLabel = new Label();
                    timeLabel.Text = $"⏱ {minutes:00}:{seconds:00}";
                    timeLabel.Font = new Font("Arial", 11);
                    timeLabel.ForeColor = Color.White;
                    timeLabel.Size = new Size(120, 65);
                    timeLabel.Location = new Point(480, 0);
                    timeLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(timeLabel);
                }
            }

            Button closeBtn = new Button();
            closeBtn.Text = "Close";
            closeBtn.Size = new Size(120, 35);
            closeBtn.Location = new Point(290, 430);
            closeBtn.BackColor = Color.FromArgb(80, 30, 30);
            closeBtn.ForeColor = Color.White;
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.Click += (s, e) => histForm.Close();
            histForm.Controls.Add(closeBtn);

            histForm.ShowDialog();
        }
        private void ShowBestiary()
        {
            Form bestForm = new Form();
            bestForm.Text = "Bestiary";
            bestForm.Size = new Size(800, 600);
            bestForm.StartPosition = FormStartPosition.CenterScreen;
            bestForm.BackColor = Color.FromArgb(20, 20, 30);
            bestForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            bestForm.MaximizeBox = false;

            Label title = new Label();
            title.Text = "📖 BESTIARY";
            title.Font = new Font("Arial", 20, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Size = new Size(760, 40);
            title.Location = new Point(20, 15);
            bestForm.Controls.Add(title);

            var entries = new[]
            {
        new { Name = "Normal",      Icon = "🟥", Color = Color.Red,         MinDiff = 0, Desc = "Basic enemy. Moves toward player.",                                          Effect = "" },
        new { Name = "Gunner",      Icon = "🟧", Color = Color.OrangeRed,   MinDiff = 0, Desc = "Shoots bullets at the player.",                                              Effect = "" },
        new { Name = "Tank",        Icon = "🟫", Color = Color.DarkRed,     MinDiff = 0, Desc = "High HP. Deals 3x damage on contact.",                                       Effect = "" },
        new { Name = "Runner",      Icon = "🩷", Color = Color.HotPink,     MinDiff = 0, Desc = "Low HP. Moves 2.5x faster than normal.",                                     Effect = "" },
        new { Name = "Parasitic",   Icon = "🟣", Color = Color.MediumPurple,MinDiff = 1, Desc = "Decays over time. Releases 3 parasites when killed by player.",              Effect = "Spawns parasites" },
        new { Name = "Frenzied",    Icon = "🟠", Color = Color.Orange,      MinDiff = 1, Desc = "Moves erratically. Partially homes toward player.",                           Effect = "Erratic movement" },
        new { Name = "Charging",    Icon = "🟡", Color = Color.Yellow,      MinDiff = 1, Desc = "Periodically dashes at high speed toward the player.",                       Effect = "Dash attack" },
        new { Name = "Berserker",   Icon = "🔴", Color = Color.OrangeRed,   MinDiff = 1, Desc = "Below 50% HP: moves faster, deals 2x damage, takes less damage.",            Effect = "Enrages at 50% HP" },
        new { Name = "Armored",     Icon = "⬜", Color = Color.Silver,      MinDiff = 2, Desc = "First hit is always blocked by armor.",                                       Effect = "Blocks first hit" },
        new { Name = "Regenerating",Icon = "🟩", Color = Color.LimeGreen,   MinDiff = 2, Desc = "Slowly heals over time.",                                                    Effect = "Heals over time" },
        new { Name = "Zigzag",      Icon = "🔵", Color = Color.Cyan,        MinDiff = 2, Desc = "Moves in a zigzag pattern toward player.",                                   Effect = "Zigzag movement" },
        new { Name = "Phasing",     Icon = "👻", Color = Color.LightBlue,   MinDiff = 3, Desc = "Periodically becomes nearly invisible and untouchable.",                     Effect = "Becomes invisible" },
        new { Name = "Reflective",  Icon = "💠", Color = Color.LightCyan,   MinDiff = 3, Desc = "20% chance to reflect bullets back at the player.",                          Effect = "Reflects bullets" },
        new { Name = "Corrupted",   Icon = "🟪", Color = Color.MediumPurple,MinDiff = 3, Desc = "Leaves a damaging purple trail behind it.",                                  Effect = "Leaves damage trail" },
    };

            string[] diffNames = { "Easy", "Normal", "Hard", "Nightmare" };
            Color[] diffColors = { Color.LimeGreen, Color.DodgerBlue, Color.Orange, Color.Red };

            Panel scrollPanel = new Panel();
            scrollPanel.Size = new Size(750, 480);
            scrollPanel.Location = new Point(25, 60);
            scrollPanel.AutoScroll = true;
            scrollPanel.BackColor = Color.FromArgb(20, 20, 30);
            bestForm.Controls.Add(scrollPanel);

            int yPos = 5;
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                int kills = beastiaryKills.ContainsKey(entry.Name) ? beastiaryKills[entry.Name] : 0;
                bool discovered = kills > 0;
                bool available = difficulty >= entry.MinDiff || kills > 0;

                Panel row = new Panel();
                row.Size = new Size(730, 60);
                row.Location = new Point(5, yPos);
                row.BackColor = i % 2 == 0 ? Color.FromArgb(28, 28, 42) : Color.FromArgb(22, 22, 35);
                scrollPanel.Controls.Add(row);

                // Icon/color indicator
                Panel colorDot = new Panel();
                colorDot.Size = new Size(20, 20);
                colorDot.Location = new Point(10, 20);
                colorDot.BackColor = discovered ? entry.Color : Color.FromArgb(50, 50, 50);
                row.Controls.Add(colorDot);

                // Name
                Label nameLabel = new Label();
                nameLabel.Text = discovered ? entry.Name : "???";
                nameLabel.Font = new Font("Arial", 13, FontStyle.Bold);
                nameLabel.ForeColor = discovered ? entry.Color : Color.FromArgb(60, 60, 60);
                nameLabel.Size = new Size(130, 60);
                nameLabel.Location = new Point(35, 0);
                nameLabel.TextAlign = ContentAlignment.MiddleLeft;
                row.Controls.Add(nameLabel);

                // Description
                Label descLabel = new Label();
                descLabel.Text = discovered ? entry.Desc : "Kill this enemy to unlock its entry.";
                descLabel.Font = new Font("Arial", 9);
                descLabel.ForeColor = discovered ? Color.LightGray : Color.FromArgb(60, 60, 60);
                descLabel.Size = new Size(330, 60);
                descLabel.Location = new Point(170, 0);
                descLabel.TextAlign = ContentAlignment.MiddleLeft;
                row.Controls.Add(descLabel);

                // Effect
                Label effectLabel = new Label();
                effectLabel.Text = discovered && entry.Effect != "" ? "⚡ " + entry.Effect : "";
                effectLabel.Font = new Font("Arial", 9, FontStyle.Italic);
                effectLabel.ForeColor = Color.Gold;
                effectLabel.Size = new Size(150, 60);
                effectLabel.Location = new Point(505, 0);
                effectLabel.TextAlign = ContentAlignment.MiddleLeft;
                row.Controls.Add(effectLabel);

                // Kill count
                Label killLabel = new Label();
                killLabel.Text = discovered ? $"💀 {kills}" : "";
                killLabel.Font = new Font("Arial", 11, FontStyle.Bold);
                killLabel.ForeColor = Color.Gold;
                killLabel.Size = new Size(80, 60);
                killLabel.Location = new Point(640, 0);
                killLabel.TextAlign = ContentAlignment.MiddleRight;
                row.Controls.Add(killLabel);

                // Min difficulty badge
                Label diffBadge = new Label();
                diffBadge.Text = diffNames[entry.MinDiff];
                diffBadge.Font = new Font("Arial", 8);
                diffBadge.ForeColor = diffColors[entry.MinDiff];
                diffBadge.Size = new Size(60, 15);
                diffBadge.Location = new Point(35, 45);
                diffBadge.TextAlign = ContentAlignment.MiddleLeft;
                row.Controls.Add(diffBadge);

                yPos += 65;
            }

            scrollPanel.AutoScrollMinSize = new Size(730, yPos);

            Button closeBtn = new Button();
            closeBtn.Text = "Close";
            closeBtn.Size = new Size(120, 35);
            closeBtn.Location = new Point(340, 548);
            closeBtn.BackColor = Color.FromArgb(80, 30, 30);
            closeBtn.ForeColor = Color.White;
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.Click += (s, e2) => bestForm.Close();
            bestForm.Controls.Add(closeBtn);

            bestForm.ShowDialog();
        }

        private void ShowAchievements()
        {
            Form achForm = new Form();
            achForm.Text = "Achievements";
            achForm.Size = new Size(750, 600);
            achForm.StartPosition = FormStartPosition.CenterScreen;
            achForm.BackColor = Color.FromArgb(20, 20, 30);
            achForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            achForm.MaximizeBox = false;

            Label title = new Label();
            title.Text = $"🏆 ACHIEVEMENTS ({unlockedAchievements.Count}/{achievements.Length})";
            title.Font = new Font("Arial", 16, FontStyle.Bold);
            title.ForeColor = Color.Gold;
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Size = new Size(710, 40);
            title.Location = new Point(20, 10);
            achForm.Controls.Add(title);

            Panel scrollPanel = new Panel();
            scrollPanel.Location = new Point(10, 55);
            scrollPanel.Size = new Size(715, 490);
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

            int yPos = 5;
            for (int c = 0; c < categories.Length; c++)
            {
                string cat = categories[c];
                var catAchs = achievements.Where(a => a.category == cat).ToArray();
                if (catAchs.Length == 0) continue;

                int catUnlocked = catAchs.Count(a => unlockedAchievements.Contains(a.id));

                Label catLabel = new Label();
                catLabel.Text = $"  {cat.ToUpper()} ({catUnlocked}/{catAchs.Length})";
                catLabel.Font = new Font("Arial", 12, FontStyle.Bold);
                catLabel.ForeColor = catColors[c];
                catLabel.BackColor = Color.FromArgb(30, 30, 45);
                catLabel.Size = new Size(680, 30);
                catLabel.Location = new Point(5, yPos);
                catLabel.TextAlign = ContentAlignment.MiddleLeft;
                scrollPanel.Controls.Add(catLabel);
                yPos += 35;

                for (int i = 0; i < catAchs.Length; i++)
                {
                    var ach = catAchs[i];
                    bool unlocked = unlockedAchievements.Contains(ach.id);

                    Panel row = new Panel();
                    row.Size = new Size(680, 45);
                    row.Location = new Point(5, yPos);
                    row.BackColor = unlocked ? Color.FromArgb(35, 45, 35) : Color.FromArgb(25, 25, 30);
                    scrollPanel.Controls.Add(row);

                    Label iconLabel = new Label();
                    iconLabel.Text = unlocked ? ach.icon : "🔒";
                    iconLabel.Font = new Font("Segoe UI Emoji", 16);
                    iconLabel.Size = new Size(40, 45);
                    iconLabel.Location = new Point(5, 0);
                    iconLabel.TextAlign = ContentAlignment.MiddleCenter;
                    row.Controls.Add(iconLabel);

                    Label nameLabel = new Label();
                    nameLabel.Text = unlocked ? ach.name : "???";
                    nameLabel.Font = new Font("Arial", 12, FontStyle.Bold);
                    nameLabel.ForeColor = unlocked ? Color.White : Color.FromArgb(80, 80, 80);
                    nameLabel.Size = new Size(200, 45);
                    nameLabel.Location = new Point(50, 0);
                    nameLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(nameLabel);

                    Label descLabel = new Label();
                    descLabel.Text = ach.description;
                    descLabel.Font = new Font("Arial", 10);
                    descLabel.ForeColor = unlocked ? Color.FromArgb(180, 200, 180) : Color.FromArgb(60, 60, 60);
                    descLabel.Size = new Size(380, 45);
                    descLabel.Location = new Point(255, 0);
                    descLabel.TextAlign = ContentAlignment.MiddleLeft;
                    row.Controls.Add(descLabel);

                    Label statusLabel = new Label();
                    statusLabel.Text = unlocked ? "✔" : "✗";
                    statusLabel.Font = new Font("Arial", 14, FontStyle.Bold);
                    statusLabel.ForeColor = unlocked ? Color.LimeGreen : Color.FromArgb(60, 30, 30);
                    statusLabel.Size = new Size(35, 45);
                    statusLabel.Location = new Point(640, 0);
                    statusLabel.TextAlign = ContentAlignment.MiddleCenter;
                    row.Controls.Add(statusLabel);

                    yPos += 50;
                }
                yPos += 10;
            }

            Button closeBtn = new Button();
            closeBtn.Text = "Close";
            closeBtn.Size = new Size(120, 35);
            closeBtn.Location = new Point(315, 550);
            closeBtn.BackColor = Color.FromArgb(80, 30, 30);
            closeBtn.ForeColor = Color.White;
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.Font = new Font("Arial", 11, FontStyle.Bold);
            closeBtn.Click += (s, e2) => achForm.Close();
            achForm.Controls.Add(closeBtn);

            achForm.ShowDialog();
        }

        // --- Multiplayer sync helpers ---
        private GameStatePacket BuildGameStatePacket()
        {
            var pkt = new GameStatePacket
            {
                HostX = posX, HostY = posY,
                HostHealth = health, HostMaxHealth = maxHealth,
                HostScore = score,
                HostDashing = isDashing,

                ClientX = p2X, ClientY = p2Y,
                ClientHealth = p2Health, ClientMaxHealth = p2MaxHealth,
                ClientDashing = p2Dashing,

                TimeAlive = timeAlive,
                TotalKills = totalKills,
                BossActive = bossAlive,
                BossX = bossX, BossY = bossY,
                BossHealth = bossHealth, BossMaxHealth = bossMaxHealth,
            };

            int eCount = Math.Min(enemies.Count, 100);
            pkt.EnemyCount = eCount;
            pkt.EnemyX = new float[eCount];
            pkt.EnemyY = new float[eCount];
            pkt.EnemyAlive = new bool[eCount];
            pkt.EnemyType = new int[eCount];
            for (int i = 0; i < eCount; i++)
            {
                pkt.EnemyX[i] = enemies[i].x;
                pkt.EnemyY[i] = enemies[i].y;
                pkt.EnemyAlive[i] = i < enemyAlive.Count && enemyAlive[i];
                pkt.EnemyType[i] = (i < enemyIsTank.Count && enemyIsTank[i]) ? 1 :
                                   (i < enemyCanShoot.Count && enemyCanShoot[i]) ? 2 : 0;
            }

            int bCount = Math.Min(bullets.Count, 200);
            pkt.BulletCount = bCount;
            pkt.BulletX = new float[bCount];
            pkt.BulletY = new float[bCount];
            for (int i = 0; i < bCount; i++)
            {
                pkt.BulletX[i] = bullets[i].x;
                pkt.BulletY[i] = bullets[i].y;
            }

            int cCount = Math.Min(coins.Count, 50);
            pkt.CoinCount = cCount;
            pkt.CoinX = new float[cCount];
            pkt.CoinY = new float[cCount];
            for (int i = 0; i < cCount; i++)
            {
                pkt.CoinX[i] = coins[i].x;
                pkt.CoinY[i] = coins[i].y;
            }

            return pkt;
        }

        private void ApplyGameState(GameStatePacket state)
        {
            // On client: update everything from host's authoritative state
            // Host player position (rendered as "other player" on client)
            p2X = state.HostX;
            p2Y = state.HostY;
            p2Health = state.HostHealth;
            p2MaxHealth = state.HostMaxHealth;
            p2Dashing = state.HostDashing;

            // Our own position (as simulated by host)
            posX = state.ClientX;
            posY = state.ClientY;
            health = state.ClientHealth;
            maxHealth = state.ClientMaxHealth;
            isDashing = state.ClientDashing;

            score = state.HostScore;
            timeAlive = state.TimeAlive;
            totalKills = state.TotalKills;

            bossAlive = state.BossActive;
            if (state.BossActive)
            {
                bossX = state.BossX;
                bossY = state.BossY;
                bossHealth = state.BossHealth;
                bossMaxHealth = state.BossMaxHealth;
            }

            // Sync enemies for rendering
            while (enemies.Count < state.EnemyCount)
                enemies.Add((0, 0));
            while (enemyAlive.Count < state.EnemyCount)
                enemyAlive.Add(false);
            while (enemyIsTank.Count < state.EnemyCount)
                enemyIsTank.Add(false);
            while (enemyCanShoot.Count < state.EnemyCount)
                enemyCanShoot.Add(false);

            for (int i = 0; i < state.EnemyCount; i++)
            {
                enemies[i] = (state.EnemyX[i], state.EnemyY[i]);
                enemyAlive[i] = state.EnemyAlive[i];
                enemyIsTank[i] = state.EnemyType[i] == 1;
                enemyCanShoot[i] = state.EnemyType[i] == 2;
            }

            // Sync bullets for rendering
            bullets.Clear();
            for (int i = 0; i < state.BulletCount; i++)
                bullets.Add((state.BulletX[i], state.BulletY[i], 0, 0, 0));

            // Sync coins for rendering
            coins.Clear();
            for (int i = 0; i < state.CoinCount; i++)
                coins.Add((state.CoinX[i], state.CoinY[i], 0, 0));
        }

        private void ApplyP2Input(PlayerInputPacket input)
        {
            // Host simulates P2 movement
            float p2Speed = speed;
            if (input.MoveX != 0 || input.MoveY != 0)
            {
                float len = (float)Math.Sqrt(input.MoveX * input.MoveX + input.MoveY * input.MoveY);
                if (len > 0)
                {
                    p2X += (input.MoveX / len) * p2Speed * deltaTime * 60f;
                    p2Y += (input.MoveY / len) * p2Speed * deltaTime * 60f;
                }
            }
            p2X = Math.Max(0, Math.Min(p2X, ClientSize.Width - boxSize));
            p2Y = Math.Max(0, Math.Min(p2Y, ClientSize.Height - boxSize));

            // P2 shooting — create bullets aimed at their mouse position
            if (input.Shooting && shootCooldown <= 0)
            {
                float cx = p2X + boxSize / 2f;
                float cy = p2Y + boxSize / 2f;
                float dirX = input.AimX - cx;
                float dirY = input.AimY - cy;
                float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
                if (dist > 0)
                {
                    float vx = dirX / dist * bulletSpeed;
                    float vy = dirY / dist * bulletSpeed;
                    bullets.Add((cx + dirX / dist * 20, cy + dirY / dist * 20, vx, vy, 0));
                }
            }
        }

        private void InitMultiplayerCallbacks()
        {
            if (netManager == null) return;
            netManager.OnPlayerInputReceived += input => latestP2Input = input;
            netManager.OnGameStateReceived += state => latestGameState = state;
            netManager.OnPeerLeft += () =>
            {
                this.Invoke(() =>
                {
                    isMultiplayer = false;
                    // Could show a message or return to menu
                });
            };
        }

        private void ShowMultiplayerMenu()
        {
            menuPlayBtn.Visible = false;
            menuQuitBtn.Visible = false;
            menuPrefsBtn.Visible = false;

            List<Control> mpControls = new();

            Label titleLabel = new Label();
            titleLabel.Text = "🌐 MULTIPLAYER";
            titleLabel.Font = new Font("Arial", 18, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.Size = new Size(300, 40);
            titleLabel.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 - 100);
            mpControls.Add(titleLabel);

            Label statusLabel = new Label();
            statusLabel.Text = "Choose Host or Join";
            statusLabel.Font = new Font("Arial", 11);
            statusLabel.ForeColor = Color.Gray;
            statusLabel.Size = new Size(500, 25);
            statusLabel.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 - 55);
            mpControls.Add(statusLabel);

            // --- HOST section ---
            Button hostBtn = new Button();
            hostBtn.Text = "🏠 Host Game";
            hostBtn.Size = new Size(250, 50);
            hostBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 - 15);
            hostBtn.BackColor = Color.FromArgb(40, 100, 40);
            hostBtn.ForeColor = Color.White;
            hostBtn.FlatStyle = FlatStyle.Flat;
            hostBtn.Font = new Font("Arial", 14, FontStyle.Bold);
            hostBtn.Cursor = Cursors.Hand;
            mpControls.Add(hostBtn);

            Label roomCodeLabel = new Label();
            roomCodeLabel.Text = "";
            roomCodeLabel.Font = new Font("Consolas", 22, FontStyle.Bold);
            roomCodeLabel.ForeColor = Color.Gold;
            roomCodeLabel.Size = new Size(400, 40);
            roomCodeLabel.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 45);
            mpControls.Add(roomCodeLabel);

            Label ipInfoLabel = new Label();
            ipInfoLabel.Text = "";
            ipInfoLabel.Font = new Font("Arial", 10);
            ipInfoLabel.ForeColor = Color.FromArgb(150, 180, 220);
            ipInfoLabel.Size = new Size(500, 45);
            ipInfoLabel.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 85);
            mpControls.Add(ipInfoLabel);

            // --- OR ---
            Label orLabel = new Label();
            orLabel.Text = "— OR —";
            orLabel.Font = new Font("Arial", 11);
            orLabel.ForeColor = Color.Gray;
            orLabel.Size = new Size(250, 25);
            orLabel.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 130);
            orLabel.TextAlign = ContentAlignment.MiddleCenter;
            mpControls.Add(orLabel);

            // --- JOIN section ---
            Label hostIpLabel = new Label();
            hostIpLabel.Text = "Host IP:";
            hostIpLabel.Font = new Font("Arial", 11);
            hostIpLabel.ForeColor = Color.White;
            hostIpLabel.Size = new Size(70, 25);
            hostIpLabel.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 162);
            mpControls.Add(hostIpLabel);

            TextBox hostIpBox = new TextBox();
            hostIpBox.Font = new Font("Consolas", 12);
            hostIpBox.Size = new Size(180, 30);
            hostIpBox.Location = new Point((int)(115 * scaleX), ClientSize.Height / 2 + 160);
            hostIpBox.BackColor = Color.FromArgb(30, 30, 45);
            hostIpBox.ForeColor = Color.White;
            hostIpBox.BorderStyle = BorderStyle.FixedSingle;
            mpControls.Add(hostIpBox);

            Label joinCodeLabel = new Label();
            joinCodeLabel.Text = "Code:";
            joinCodeLabel.Font = new Font("Arial", 11);
            joinCodeLabel.ForeColor = Color.White;
            joinCodeLabel.Size = new Size(50, 25);
            joinCodeLabel.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 197);
            mpControls.Add(joinCodeLabel);

            TextBox codeBox = new TextBox();
            codeBox.Font = new Font("Consolas", 14);
            codeBox.Size = new Size(110, 30);
            codeBox.Location = new Point((int)(95 * scaleX), ClientSize.Height / 2 + 195);
            codeBox.BackColor = Color.FromArgb(30, 30, 45);
            codeBox.ForeColor = Color.White;
            codeBox.BorderStyle = BorderStyle.FixedSingle;
            codeBox.MaxLength = 5;
            codeBox.CharacterCasing = CharacterCasing.Upper;
            mpControls.Add(codeBox);

            Button joinBtn = new Button();
            joinBtn.Text = "🔗 Join";
            joinBtn.Size = new Size(120, 50);
            joinBtn.Location = new Point((int)(215 * scaleX), ClientSize.Height / 2 + 185);
            joinBtn.BackColor = Color.FromArgb(40, 80, 140);
            joinBtn.ForeColor = Color.White;
            joinBtn.FlatStyle = FlatStyle.Flat;
            joinBtn.Font = new Font("Arial", 14, FontStyle.Bold);
            joinBtn.Cursor = Cursors.Hand;
            mpControls.Add(joinBtn);

            Button startBtn = new Button();
            startBtn.Text = "▶ Start Game";
            startBtn.Size = new Size(250, 50);
            startBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 250);
            startBtn.BackColor = Color.FromArgb(40, 100, 40);
            startBtn.ForeColor = Color.White;
            startBtn.FlatStyle = FlatStyle.Flat;
            startBtn.Font = new Font("Arial", 14, FontStyle.Bold);
            startBtn.Cursor = Cursors.Hand;
            startBtn.Visible = false;
            mpControls.Add(startBtn);

            Button backBtn = new Button();
            backBtn.Text = "◀ Back";
            backBtn.Size = new Size(250, 35);
            backBtn.Location = new Point((int)(40 * scaleX), ClientSize.Height / 2 + 310);
            backBtn.BackColor = Color.FromArgb(60, 60, 60);
            backBtn.ForeColor = Color.White;
            backBtn.FlatStyle = FlatStyle.Flat;
            backBtn.Font = new Font("Arial", 12);
            backBtn.Cursor = Cursors.Hand;
            mpControls.Add(backBtn);

            void Cleanup()
            {
                foreach (var c in mpControls) this.Controls.Remove(c);
                if (netManager != null && !isMultiplayer)
                {
                    netManager.Disconnect();
                    netManager = null;
                }
                if (embeddedRelay != null && !isMultiplayer)
                {
                    embeddedRelay.Stop();
                    embeddedRelay = null;
                }
                menuPlayBtn.Visible = true;
                menuQuitBtn.Visible = true;
                menuPrefsBtn.Visible = true;
            }

            void StartGame(bool asHost)
            {
                isMultiplayer = true;
                isNetHost = asHost;
                InitMultiplayerCallbacks();
                if (asHost) netManager?.SendGameStart();
                foreach (var c in mpControls) this.Controls.Remove(c);
                this.Controls.Remove(menuPlayBtn);
                this.Controls.Remove(menuQuitBtn);
                this.Controls.Remove(menuPrefsBtn);
                this.Controls.Remove(menuHistoryBtn);
                this.Controls.Remove(menuBestiaryBtn);
                this.Controls.Remove(menuAchievementsBtn);
                this.Controls.Remove(menuMultiplayerBtn);
                onMainMenu = false;
                isPaused = false;
                difficulty = 0;
                sandboxMode = false;
                ApplyDifficulty();
                ResetGame();
                if (asHost) { p2X = posX + 50; p2Y = posY; p2Health = maxHealth; p2MaxHealth = maxHealth; }
            }

            backBtn.Click += (s, e) => Cleanup();

            hostBtn.Click += (s, e) =>
            {
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
                    });
                };
                netManager.OnPeerJoined += () =>
                {
                    this.Invoke(() =>
                    {
                        statusLabel.Text = $"{netManager.PeerName} joined! Ready to start.";
                        statusLabel.ForeColor = Color.Gold;
                        p2Name = netManager.PeerName;
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
                string ip = hostIpBox.Text.Trim();
                string code = codeBox.Text.Trim().ToUpper();
                if (ip.Length == 0) { statusLabel.Text = "Enter host IP"; statusLabel.ForeColor = Color.Red; return; }
                if (code.Length < 5) { statusLabel.Text = "Enter 5-char room code"; statusLabel.ForeColor = Color.Red; return; }

                netManager = new NetworkManager();
                netManager.OnRoomJoined += () =>
                {
                    this.Invoke(() =>
                    {
                        statusLabel.Text = $"Joined {netManager.PeerName}'s room — waiting for host to start...";
                        statusLabel.ForeColor = Color.LimeGreen;
                        p2Name = netManager.PeerName;
                    });
                };
                netManager.OnGameStartReceived += () =>
                {
                    this.Invoke(() => StartGame(false));
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

            startBtn.Click += (s, e) => StartGame(true);

            foreach (var c in mpControls)
            {
                this.Controls.Add(c);
                c.BringToFront();
            }
        }
    }
}