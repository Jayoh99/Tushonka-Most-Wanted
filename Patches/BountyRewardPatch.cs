

namespace TarkovMostWanted.Patches;

using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Generators.RepeatableQuests;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using TarkovMostWanted.Bounties;
using TarkovMostWanted.Configuration;

[Injectable]
public class BountyRewardPatch : AbstractPatch
{
    private const string HandlerId =
        "6aff49a05ada18d09f1b9e72";

    private const string BountyCategory = "Bounties";

    private const string RoublesId =
        "5449016a4bdc2d6f028b456f";

    private static ISptLogger<BountyRewardPatch> _logger = null!;
    private static TemplateTable _templateTable = null!;
    private static MostWantedConfig _config = null!;

    public BountyRewardPatch(
        ISptLogger<BountyRewardPatch> logger,
        TemplateTable templateTable,
        MostWantedConfig config)
    {
        _logger = logger;
        _templateTable = templateTable;
        _config = config;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(EliminationQuestGenerator),
            nameof(EliminationQuestGenerator.Generate));
    }

    [PatchPostfix]
    public static void Postfix(
        RepeatableQuest? __result,
        MongoId traderId,
        RepeatableQuestConfig repeatableConfig)
    {
        // Only modify Handler's bounty contracts
        if (__result == null ||
            traderId != HandlerId ||
            repeatableConfig.Name != BountyCategory)
        {
            return;
        }

        try
        {
            var finishCondition = __result.Conditions
                .AvailableForFinish?
                .FirstOrDefault(condition =>
                    condition.Counter?.Conditions?
                    .Any(counter =>
                        counter.ConditionType == "Kills") == true);

            var killCondition = finishCondition?
                .Counter?.Conditions?
                .FirstOrDefault(counter =>
                    counter.ConditionType == "Kills");

            if (finishCondition == null ||
                killCondition == null)
            {
                return;
            }

            var isBoss = killCondition.SavageRole?
                .Any(role =>
                    role != null &&
                    role.StartsWith(
                        "boss",
                        StringComparison.OrdinalIgnoreCase))
                == true;

            var killCount =
                Convert.ToInt32(finishCondition.Value);

            if (killCount <= 0)
            {
                return;
            }

            string bountyType;
            ContractSettings settings;

            // ==========================================
            // IDENTIFY BOUNTY TYPE
            // ==========================================

            if (isBoss)
            {
                bountyType = "Boss";
                settings = _config.Boss;
            }
            else
            {
                var targetJson =
                    System.Text.Json.JsonSerializer
                        .Serialize(killCondition.Target);

                if (targetJson.Contains(
                    "AnyPmc",
                    StringComparison.OrdinalIgnoreCase))
                {
                    bountyType = "PMC";
                    settings = _config.Pmc;
                }
                else if (targetJson.Contains(
                    "Savage",
                    StringComparison.OrdinalIgnoreCase))
                {
                    bountyType = "Scav";
                    settings = _config.Scav;
                }
                else
                {
                    _logger.Warning(
                        "[Most Wanted] Unknown bounty target: " +
                        targetJson);

                    return;
                }
            }

            // ==========================================
            // VALIDATE REWARD SETTINGS
            // ==========================================

            if (!AreRewardSettingsValid(settings))
            {
                _logger.Warning(
                    $"[Most Wanted] Invalid {bountyType} " +
                    "reward settings. Using default reward settings.");

                var defaults = new MostWantedConfig();

                var defaultSettings = bountyType switch
                {
                    "Boss" => defaults.Boss,
                    "PMC" => defaults.Pmc,
                    _ => defaults.Scav
                };

                settings = new ContractSettings
                {
                    MinKills = settings.MinKills,
                    MaxKills = settings.MaxKills,
                    MinRoubles = defaultSettings.MinRoubles,
                    MaxRoubles = defaultSettings.MaxRoubles,
                    MinExperience = defaultSettings.MinExperience,
                    MaxExperience = defaultSettings.MaxExperience,
                    MinGpCoins = defaultSettings.MinGpCoins,
                    MaxGpCoins = defaultSettings.MaxGpCoins,
                    Reputation = defaultSettings.Reputation,
                    MinBonusItems = settings.MinBonusItems,
                    MaxBonusItems = settings.MaxBonusItems
                };
            }

            // ==========================================
            // GENERATE CONFIGURABLE REWARDS
            // ==========================================

            int roubles;
            int experience;

            if (isBoss)
            {
                roubles = RandomInclusive(
                    settings.MinRoubles,
                    settings.MaxRoubles);

                experience = RandomInclusive(
                    settings.MinExperience,
                    settings.MaxExperience);
            }
            else
            {
                var progress = CalculateProgress(
                    killCount,
                    settings.MinKills,
                    settings.MaxKills);

                roubles = CalculateScaledReward(
                    settings.MinRoubles,
                    settings.MaxRoubles,
                    progress);

                experience = CalculateScaledReward(
                    settings.MinExperience,
                    settings.MaxExperience,
                    progress);
            }

            var gpCoins = RandomInclusive(
                settings.MinGpCoins,
                settings.MaxGpCoins);

            // ==========================================
            // GLOBAL REWARD MULTIPLIERS
            // ==========================================

            var multipliers = _config.RewardMultipliers;

            roubles = ApplyIntegerMultiplier(
                roubles,
                ValidateMultiplier(
                    multipliers.Roubles,
                    "Roubles"));

            experience = ApplyIntegerMultiplier(
                experience,
                ValidateMultiplier(
                    multipliers.Experience,
                    "Experience"));

            var reputation = settings.Reputation *
                ValidateMultiplier(
                    multipliers.Reputation,
                    "Reputation");

            if (!double.IsFinite(reputation))
            {
                _logger.Warning(
                    "[Most Wanted] Reputation reward overflow. " +
                    "Using base reputation.");

                reputation = settings.Reputation;
            }

            // ==========================================
            // CREATE CUSTOM REWARDS
            // ==========================================

            if (__result.Rewards == null)
            {
                return;
            }

            var rewards = new List<Reward>();
            var rewardNames = new List<string>();

            // Experience reward
            if (experience > 0)
            {
                rewards.Add(new Reward
                {
                    Id = new MongoId(),
                    Unknown = false,
                    GameMode = [],
                    AvailableInGameEditions = [],
                    Type = RewardType.Experience,
                    Value = experience,
                    Index = rewards.Count
                });
            }

            // Rouble reward
            if (roubles > 0)
            {
                rewards.Add(CreateItemReward(
                    RoublesId,
                    roubles,
                    rewards.Count,
                    false));
            }

            // GP coin reward
            if (gpCoins > 0)
            {
                if (_templateTable.Items.ContainsKey(
                    BountyRewardPools.GpCoinId))
                {
                    rewards.Add(CreateItemReward(
                        BountyRewardPools.GpCoinId,
                        gpCoins,
                        rewards.Count,
                        false));
                }
                else
                {
                    _logger.Warning(
                        "[Most Wanted] GP coin template missing.");
                }
            }

            // ==========================================
            // CUSTOM BONUS ITEMS
            // ==========================================

            // Validate all templates before reward selection
            var bonusItems =
                BountyRewardPools.GenerateRewards(
                    bountyType,
                    _config,
                    templateId =>
                        _templateTable.Items.ContainsKey(
                            templateId));

            foreach (var bonusItem in bonusItems)
            {
                // Final defensive database check
                if (!_templateTable.Items.ContainsKey(
                    bonusItem.TemplateId))
                {
                    _logger.Warning(
                        "[Most Wanted] Invalid reward template: " +
                        $"{bonusItem.Name} " +
                        $"({bonusItem.TemplateId})");

                    continue;
                }

                var count = Random.Shared.Next(
                    bonusItem.MinCount,
                    bonusItem.MaxCount + 1);

                rewards.Add(CreateItemReward(
                    bonusItem.TemplateId,
                    count,
                    rewards.Count,
                    true));

                rewardNames.Add(
                    $"{bonusItem.Name} x{count}");
            }

            // Handler reputation
            if (reputation > 0)
            {
                rewards.Add(new Reward
                {
                    Id = new MongoId(),
                    Unknown = false,
                    GameMode = [],
                    AvailableInGameEditions = [],
                    Type = RewardType.TraderStanding,
                    Target = traderId,
                    Value = reputation,
                    Index = rewards.Count
                });
            }

            // Replace vanilla rewards only after all
            // custom rewards have been generated
            __result.Rewards["Success"] = rewards;

            // ==========================================
            // SERVER LOGGING
            // ==========================================

            var bonusText = rewardNames.Count > 0
                ? string.Join(", ", rewardNames)
                : "None";

            _logger.Info(
                $"[Most Wanted] {bountyType} bounty generated | " +
                $"Kills: {killCount} | " +
                $"Roubles: {roubles:N0} | " +
                $"XP: {experience:N0} | " +
                $"Reputation: {reputation:F2} | " +
                $"GP Coins: {gpCoins} | " +
                $"Bonus Items: {bonusText}");
        }
        catch (Exception exception)
        {
            _logger.Error(
                "[Most Wanted] Reward generation error: " +
                exception);
        }
    }

    // ==========================================
    // MULTIPLIER VALIDATION
    // ==========================================

    private static double ValidateMultiplier(
        double multiplier,
        string rewardName)
    {
        if (!double.IsFinite(multiplier) ||
            multiplier < 0)
        {
            _logger.Warning(
                $"[Most Wanted] Invalid {rewardName} " +
                "multiplier. Using 1.0.");

            return 1.0;
        }

        return multiplier;
    }

    // ==========================================
    // APPLY INTEGER MULTIPLIER
    // ==========================================

    private static int ApplyIntegerMultiplier(
        int amount,
        double multiplier)
    {
        var scaled = amount * multiplier;

        if (scaled > int.MaxValue)
        {
            _logger.Warning(
                "[Most Wanted] Scaled reward exceeds " +
                "integer range. Using base reward.");

            return amount;
        }

        return (int)scaled;
    }

    // ==========================================
    // REWARD VALIDATION
    // ==========================================

    private static bool AreRewardSettingsValid(
        ContractSettings settings)
    {
        return
            settings.MinRoubles >= 0 &&
            settings.MaxRoubles >= settings.MinRoubles &&
            settings.MinExperience >= 0 &&
            settings.MaxExperience >= settings.MinExperience &&
            settings.MinGpCoins >= 0 &&
            settings.MaxGpCoins >= settings.MinGpCoins &&
            double.IsFinite(settings.Reputation) &&
            settings.Reputation >= 0;
    }

    // ==========================================
    // REWARD SCALING
    // ==========================================

    private static double CalculateProgress(
        int killCount,
        int minKills,
        int maxKills)
    {
        if (maxKills <= minKills)
        {
            return 1.0;
        }

        return Math.Clamp(
            (killCount - minKills) /
            (double)(maxKills - minKills),
            0.0,
            1.0);
    }

    private static int CalculateScaledReward(
        int minimum,
        int maximum,
        double progress)
    {
        return (int)(
            minimum +
            (maximum - minimum) * progress);
    }

    // ==========================================
    // INCLUSIVE RANDOM REWARD
    // ==========================================

    private static int RandomInclusive(
        int minimum,
        int maximum)
    {
        if (minimum == maximum)
        {
            return minimum;
        }

        return (int)Random.Shared.NextInt64(
            minimum,
            (long)maximum + 1);
    }

    // ==========================================
    // CREATE ITEM REWARD
    // ==========================================

    private static Reward CreateItemReward(
        MongoId templateId,
        int count,
        int index,
        bool foundInRaid)
    {
        var itemId = new MongoId();

        return new Reward
        {
            Id = new MongoId(),
            Unknown = false,
            GameMode = [],
            AvailableInGameEditions = [],
            Index = index,
            Target = itemId,
            Value = count,
            IsEncoded = false,
            FindInRaid = foundInRaid,
            Type = RewardType.Item,

            Items =
            [
                new Item
                {
                    Id = itemId,
                    Template = templateId,
                    Upd = new Upd
                    {
                        StackObjectsCount = count,
                        SpawnedInSession = foundInRaid
                    }
                }
            ]
        };
    }
}

