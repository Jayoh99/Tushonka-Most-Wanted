

namespace TarkovMostWanted.Bounties;

using TarkovMostWanted.Configuration;

public static class BountyRewardPools
{
    public record RewardItem(
        string TemplateId,
        string Name,
        int MinCount = 1,
        int MaxCount = 1);

    public const string GpCoinId =
        "5d235b4d86f7742e017bc88a";

    // ==========================================
    // DEFAULT PMC REWARD POOL
    // ==========================================

    public static readonly RewardItem[] Pmc =
    [
        new("54527ac44bdc2d36668b4567", "M855A1", 30, 60),
        new("58dd3ad986f77403051cba8f", "M80", 20, 40),
        new("56dfef82d2720bbd668b4567", "BP", 30, 60),
        new("56dff061d2720bb5668b4567", "BT", 30, 60),
        new("60098ad7c2240c0fe85c570a", "AFAK"),
        new("5d02797c86f774203f38e30a", "Surv12"),
        new("5c0e530286f7747fa1419862", "Propital"),
        new("5c0e531d86f7747fa23f4d42", "SJ6"),
        new("618a431df1eb8e24b8741deb", "RGO"),
        new("5d0375ff86f774186372f685", "Military cable"),
        new("5d0376a486f7747d8050965c", "Military circuit board"),
        new("62a0a16d0b9d3c46de5b6e97", "Military flash drive")
    ];

    // ==========================================
    // DEFAULT BOSS STANDARD REWARDS
    // ==========================================

    public static readonly RewardItem[] BossStandard =
    [
        new("59e690b686f7746c9f75e848", "M995", 30, 60),
        new("5a6086ea4f39f99cd479502f", "M61", 20, 40),
        new("61962b617c6c7b169525f168", "7N40", 30, 60),
        new("5c12613b86f7743bbe2c3f76", "Intelligence"),
        new("5d0378d486f77420421a5ff4", "Military power filter"),
        new("5d03784a86f774203e7e0c4d", "Military gyrotachometer"),
        new("5d235a5986f77443f6329bc6", "Skull ring")
    ];

    // ==========================================
    // DEFAULT BOSS RARE REWARDS
    // ==========================================

    public static readonly RewardItem[] BossRare =
    [
        new("5c05308086f7746b2101e90b", "Virtex"),
        new("5c052fb986f7746b2101e909", "RFID reader"),
        new("5c05300686f7746dce784e5d", "VPX"),
        new("5c052e6986f7746b207bc3c9", "Defibrillator"),
        new("5c0e534186f7747fa1419867", "eTG-change"),
        new("5d03775b86f774203e7e0c4b", "Phased array element"),
        new("5d03794386f77420415576f5", "Military battery")
    ];

    // ==========================================
    // DEFAULT BOSS JACKPOT REWARDS
    // ==========================================

    public static readonly RewardItem[] BossJackpot =
    [
        new("57347ca924597744596b4e71", "Graphics card"),
        new("59faff1d86f7746c51718c9c", "Bitcoin"),
        new("5c1d0efb86f7744baf2e7b7b", "Red keycard"),
        new("5c1d0dc586f7744baf2e7b79", "Green keycard"),
        new("5c1d0c5f86f7744bb2683cf0", "Blue keycard"),
        new("5c1d0f4986f7744bb01837fa", "Black keycard"),
        new("5c1d0d6d86f7744bb2683e1f", "Yellow keycard"),
        new("6656560053eaaa7a23349c86", "Lega Medal")
    ];

    // ==========================================
    // ACTIVE CONFIGURED REWARD POOLS
    // ==========================================

    private static RewardPoolSettings _configuredPools =
        new();

    // ==========================================
    // INITIALIZE CUSTOM REWARD POOLS
    // ==========================================

    public static void Initialize(
        RewardPoolSettings settings)
    {
        var defaults = new RewardPoolSettings();

        _configuredPools = new RewardPoolSettings
        {
            Pmc = ValidatePool(
                settings.Pmc,
                defaults.Pmc,
                "PMC"),

            Scav = ValidatePool(
                settings.Scav,
                defaults.Scav,
                "Scav"),

            BossStandard = ValidatePool(
                settings.BossStandard,
                defaults.BossStandard,
                "BossStandard"),

            BossRare = ValidatePool(
                settings.BossRare,
                defaults.BossRare,
                "BossRare"),

            BossJackpot = ValidatePool(
                settings.BossJackpot,
                defaults.BossJackpot,
                "BossJackpot")
        };

        Console.WriteLine(
            "[Most Wanted] Active reward pools initialized.");

        Console.WriteLine(
            $"[Most Wanted] Reward pools | " +
            $"PMC: {_configuredPools.Pmc.Length} | " +
            $"Scav: {_configuredPools.Scav.Length} | " +
            $"Boss Standard: {_configuredPools.BossStandard.Length} | " +
            $"Boss Rare: {_configuredPools.BossRare.Length} | " +
            $"Boss Jackpot: {_configuredPools.BossJackpot.Length}");
    }

    // ==========================================
    // VALIDATE CUSTOM REWARD POOLS
    // ==========================================

    private static RewardItem[] ValidatePool(
        RewardItem[]? pool,
        RewardItem[] defaultPool,
        string poolName)
    {
        if (pool == null)
        {
            Console.WriteLine(
                $"[Most Wanted] Missing {poolName} reward pool. " +
                "Using defaults.");

            return defaultPool.ToArray();
        }

        if (pool.Length == 0)
        {
            return [];
        }

        var validItems = new List<RewardItem>();

        var templateIds = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in pool)
        {
            if (item == null)
            {
                Console.WriteLine(
                    $"[Most Wanted] Null reward entry in {poolName}.");

                continue;
            }

            var templateId = item.TemplateId?.Trim();

            if (string.IsNullOrWhiteSpace(templateId) ||
                templateId.Length != 24 ||
                !templateId.All(Uri.IsHexDigit))
            {
                Console.WriteLine(
                    $"[Most Wanted] Invalid template ID in " +
                    $"{poolName}: {item.TemplateId}");

                continue;
            }

            if (item.MinCount < 1 ||
                item.MaxCount < item.MinCount ||
                item.MaxCount > 10000)
            {
                Console.WriteLine(
                    $"[Most Wanted] Invalid reward quantity in " +
                    $"{poolName}: {item.Name}");

                continue;
            }

            templateId = templateId.ToLowerInvariant();

            if (!templateIds.Add(templateId))
            {
                Console.WriteLine(
                    $"[Most Wanted] Duplicate reward removed " +
                    $"from {poolName}: {item.Name}");

                continue;
            }

            var itemName =
                string.IsNullOrWhiteSpace(item.Name)
                    ? templateId
                    : item.Name.Trim();

            validItems.Add(
                item with
                {
                    TemplateId = templateId,
                    Name = itemName
                });
        }

        if (validItems.Count == 0)
        {
            Console.WriteLine(
                $"[Most Wanted] No valid rewards in {poolName}. " +
                "Using default pool.");

            return defaultPool.ToArray();
        }

        return validItems.ToArray();
    }

    // ==========================================
    // VALIDATE ITEMS AGAINST THE SPT DATABASE
    // ==========================================

    private static RewardItem[] ValidateAvailablePool(
        RewardItem[] pool,
        RewardItem[] defaultPool,
        string poolName,
        Func<string, bool> templateExists)
    {
        // Preserve intentionally empty pools
        if (pool.Length == 0)
        {
            return [];
        }

        var validItems = new List<RewardItem>();

        foreach (var item in pool)
        {
            if (!templateExists(item.TemplateId))
            {
                Console.WriteLine(
                    $"[Most Wanted] Invalid reward template in " +
                    $"{poolName}: {item.Name} " +
                    $"({item.TemplateId})");

                continue;
            }

            validItems.Add(item);
        }

        if (validItems.Count > 0)
        {
            return validItems.ToArray();
        }

        // Fall back to existing default templates only
        Console.WriteLine(
            $"[Most Wanted] No available templates in {poolName}. " +
            "Using default reward pool.");

        var fallback = defaultPool
            .Where(item => templateExists(item.TemplateId))
            .ToArray();

        if (fallback.Length == 0)
        {
            Console.WriteLine(
                $"[Most Wanted] No available default templates " +
                $"in {poolName}. Bonus rewards disabled for this pool.");
        }

        return fallback;
    }

    // ==========================================
    // CONFIGURABLE RANDOM REWARD SELECTION
    // ==========================================

    public static List<RewardItem> GenerateRewards(
        string bountyType,
        MostWantedConfig config,
        Func<string, bool> templateExists)
    {
        var rewards = new List<RewardItem>();
        var defaults = new MostWantedConfig();
        var defaultPools = new RewardPoolSettings();

        // ==========================================
        // PMC AND SCAV CONTRACTS
        // ==========================================

        if (bountyType == "PMC" ||
            bountyType == "Scav")
        {
            var settings = bountyType == "PMC"
                ? config.Pmc
                : config.Scav;

            var defaultSettings = bountyType == "PMC"
                ? defaults.Pmc
                : defaults.Scav;

            var configuredPool = bountyType == "PMC"
                ? _configuredPools.Pmc
                : _configuredPools.Scav;

            var defaultPool = bountyType == "PMC"
                ? defaultPools.Pmc
                : defaultPools.Scav;

            var pool = ValidateAvailablePool(
                configuredPool,
                defaultPool,
                bountyType,
                templateExists);

            var count = GetRewardCount(
                settings,
                defaultSettings,
                pool.Length);

            for (var i = 0; i < count; i++)
            {
                AddUniqueReward(rewards, pool);
            }
        }

        // ==========================================
        // BOSS CONTRACTS
        // ==========================================

        else if (bountyType == "Boss")
        {
            var standardPool = ValidateAvailablePool(
                _configuredPools.BossStandard,
                defaultPools.BossStandard,
                "BossStandard",
                templateExists);

            var rarePool = ValidateAvailablePool(
                _configuredPools.BossRare,
                defaultPools.BossRare,
                "BossRare",
                templateExists);

            var jackpotPool = ValidateAvailablePool(
                _configuredPools.BossJackpot,
                defaultPools.BossJackpot,
                "BossJackpot",
                templateExists);

            var count = GetRewardCount(
                config.Boss,
                defaults.Boss,
                standardPool.Length);

            if (count == 0)
            {
                return rewards;
            }

            // Guaranteed standard rewards
            for (var i = 0; i < count - 1; i++)
            {
                AddUniqueReward(
                    rewards,
                    standardPool);
            }

            var jackpot = config.Jackpot;

            if (!AreJackpotSettingsValid(jackpot))
            {
                jackpot = defaults.Jackpot;
            }

            // One premium reward roll per contract
            var roll = Random.Shared.Next(100);

            RewardItem[] selectedPool;

            if (roll < jackpot.JackpotChance)
            {
                selectedPool = jackpotPool;
            }
            else if (roll <
                     jackpot.JackpotChance +
                     jackpot.RareChance)
            {
                selectedPool = rarePool;
            }
            else
            {
                selectedPool = standardPool;
            }

            var rewardAdded = AddUniqueReward(
                rewards,
                selectedPool);

            if (!rewardAdded)
            {
                AddUniqueReward(
                    rewards,
                    standardPool);
            }
        }

        return rewards;
    }

    // ==========================================
    // VALIDATE BONUS ITEM COUNTS
    // ==========================================

    private static int GetRewardCount(
        ContractSettings settings,
        ContractSettings defaults,
        int maximumAllowed)
    {
        var minimum = settings.MinBonusItems;
        var maximum = settings.MaxBonusItems;

        if (minimum < 0 ||
            maximum < minimum)
        {
            minimum = defaults.MinBonusItems;
            maximum = defaults.MaxBonusItems;
        }

        minimum = Math.Clamp(
            minimum,
            0,
            maximumAllowed);

        maximum = Math.Clamp(
            maximum,
            minimum,
            maximumAllowed);

        if (minimum == maximum)
        {
            return minimum;
        }

        return Random.Shared.Next(
            minimum,
            maximum + 1);
    }

    // ==========================================
    // VALIDATE JACKPOT PROBABILITIES
    // ==========================================

    private static bool AreJackpotSettingsValid(
        JackpotSettings settings)
    {
        return
            settings.StandardChance >= 0 &&
            settings.StandardChance <= 100 &&
            settings.RareChance >= 0 &&
            settings.RareChance <= 100 &&
            settings.JackpotChance >= 0 &&
            settings.JackpotChance <= 100 &&
            settings.StandardChance +
            settings.RareChance +
            settings.JackpotChance == 100;
    }

    // ==========================================
    // PREVENT DUPLICATE ITEM REWARDS
    // ==========================================

    private static bool AddUniqueReward(
        List<RewardItem> rewards,
        RewardItem[] pool)
    {
        var available = pool
            .Where(item => !rewards.Any(existing =>
                string.Equals(
                    existing.TemplateId,
                    item.TemplateId,
                    StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (available.Length == 0)
        {
            return false;
        }

        rewards.Add(
            available[Random.Shared.Next(
                available.Length)]);

        return true;
    }
}
