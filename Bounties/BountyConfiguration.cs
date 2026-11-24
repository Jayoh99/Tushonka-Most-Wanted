

namespace TarkovMostWanted.Bounties;

using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Common.Models.Logging;
using TarkovMostWanted.Configuration;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class BountyConfiguration(
    QuestConfig questConfig,
    ICloner cloner,
    ISptLogger<BountyConfiguration> logger,
    MostWantedConfig config) : IOnLoad
{
    public Task OnLoadAsync(
        CancellationToken cancellationToken)
    {
        const string handlerId =
            "6aff49a05ada18d09f1b9e72";

        const string bountyCategoryId =
            "6aff49a05ada18d09f1b9e73";

        ValidateConfiguration();

        // Prevent duplicate category registration
        if (questConfig.RepeatableQuests
            .Any(q => q.Name == "Bounties"))
        {
            return Task.CompletedTask;
        }

        var daily = questConfig.RepeatableQuests
            .FirstOrDefault(q => q.Name == "Daily");

        if (daily == null)
        {
            logger.Error(
                "[Most Wanted] Daily configuration not found.");

            return Task.CompletedTask;
        }

        var bounties = cloner.Clone(daily);

        if (bounties == null)
        {
            logger.Error(
                "[Most Wanted] Failed to clone daily configuration.");

            return Task.CompletedTask;
        }

        // ==========================================
        // GENERAL SETTINGS
        // ==========================================

        bounties.Id = bountyCategoryId;
        bounties.Name = "Bounties";

        bounties.MinPlayerLevel =
            config.General.MinimumPlayerLevel;

        bounties.NumQuests =
            config.General.ContractCount;

        bounties.ResetTime =
            config.General.RefreshHours * 3600;

        bounties.Types = ["Elimination"];

        // ==========================================
        // HANDLER TRADER REGISTRATION
        // ==========================================

        var trader = bounties.TraderWhitelist
            .FirstOrDefault(t =>
                string.Equals(
                    t.Name,
                    "Prapor",
                    StringComparison.OrdinalIgnoreCase));

        if (trader == null)
        {
            logger.Error(
                "[Most Wanted] Trader template not found.");

            return Task.CompletedTask;
        }

        trader.TraderId = handlerId;
        trader.Name = "Handler";
        trader.QuestTypes = ["Elimination"];

        bounties.TraderWhitelist = [trader];

        // ==========================================
        // ELIMINATION SETTINGS
        // ==========================================

        foreach (var elimination in
                 bounties.QuestConfig.Elimination)
        {
            elimination.MinKills =
                config.Scav.MinKills;

            elimination.MaxKills =
                config.Scav.MaxKills;

            elimination.MinPmcKills =
                config.Pmc.MinKills;

            elimination.MaxPmcKills =
                config.Pmc.MaxKills;

            elimination.MinBossKills =
                config.Boss.MinKills;

            elimination.MaxBossKills =
                config.Boss.MaxKills;

            // Disable additional restrictions
            elimination.BodyPartChance = 0;
            elimination.DistanceProbability = 0;
            elimination.SpecificLocationChance = 0;
            elimination.WeaponRequirementChance = 0;
            elimination.WeaponCategoryRequirementChance = 0;

            // ==========================================
            // TARGET PROBABILITY WEIGHTS
            // ==========================================

            var bosses = elimination.Targets
                .Where(t =>
                    t.Data?.IsBoss == true &&
                    t.RelativeProbability > 0)
                .ToList();

            foreach (var target in elimination.Targets)
            {
                if (target.Key == "Savage")
                {
                    target.RelativeProbability =
                        (float)config.Targets.ScavWeight;
                }
                else if (target.Key == "AnyPmc")
                {
                    target.RelativeProbability =
                        (float)config.Targets.PmcWeight;
                }
                else if (
                    target.Data?.IsBoss == true &&
                    target.RelativeProbability > 0 &&
                    bosses.Count > 0)
                {
                    target.RelativeProbability =
                        (float)(config.Targets.BossWeight /
                                bosses.Count);
                }
                else
                {
                    target.RelativeProbability = 0;
                }
            }

            if (config.General.EnableDebugLogging)
            {
                logger.Info(
                    "[Most Wanted] Target balancing applied. " +
                    $"Eligible bosses: {bosses.Count}");
            }
        }

        // ==========================================
        // REGISTER BOUNTY CATEGORY
        // ==========================================

        questConfig.RepeatableQuests.Add(bounties);

        logger.Info(
            "[Most Wanted] Bounty category registered successfully.");

        logger.Info(
            $"[Most Wanted] Contracts: {bounties.NumQuests} | " +
            $"Refresh: {bounties.ResetTime} seconds | " +
            $"Minimum Level: {bounties.MinPlayerLevel}");

        logger.Info(
            $"[Most Wanted] Scavs: " +
            $"{config.Scav.MinKills}-{config.Scav.MaxKills} | " +
            $"PMCs: {config.Pmc.MinKills}-{config.Pmc.MaxKills} | " +
            $"Bosses: {config.Boss.MinKills}-{config.Boss.MaxKills}");

        logger.Info(
            "[Most Wanted] Target weights: " +
            $"Scavs {config.Targets.ScavWeight} | " +
            $"PMCs {config.Targets.PmcWeight} | " +
            $"Bosses {config.Targets.BossWeight} combined");

        return Task.CompletedTask;
    }

    // ==========================================
    // CONFIGURATION VALIDATION
    // ==========================================

    private void ValidateConfiguration()
    {
        var defaults = new MostWantedConfig();

        // Restore missing configuration sections
        config.General ??= defaults.General;
        config.Targets ??= defaults.Targets;
        config.Scav ??= defaults.Scav;
        config.Pmc ??= defaults.Pmc;
        config.Boss ??= defaults.Boss;
        config.Jackpot ??= defaults.Jackpot;
        config.RewardMultipliers ??= defaults.RewardMultipliers;

        // ==========================================
        // GENERAL SETTINGS
        // ==========================================

        if (config.General.MinimumPlayerLevel < 1 ||
            config.General.MinimumPlayerLevel > 100)
        {
            logger.Warning(
                "[Most Wanted] Invalid minimum player level. " +
                "Using default value.");

            config.General.MinimumPlayerLevel =
                defaults.General.MinimumPlayerLevel;
        }

        if (config.General.ContractCount < 1 ||
            config.General.ContractCount > 20)
        {
            logger.Warning(
                "[Most Wanted] Invalid contract count. " +
                "Using default value.");

            config.General.ContractCount =
                defaults.General.ContractCount;
        }

        if (config.General.RefreshHours < 1 ||
            config.General.RefreshHours > 168)
        {
            logger.Warning(
                "[Most Wanted] Invalid refresh interval. " +
                "Using default value.");

            config.General.RefreshHours =
                defaults.General.RefreshHours;
        }

        if (config.General.MaximumBossContracts < 0)
        {
            logger.Warning(
                "[Most Wanted] Invalid boss contract limit. " +
                "Using default value.");

            config.General.MaximumBossContracts =
                defaults.General.MaximumBossContracts;
        }

        config.General.MaximumBossContracts =
            Math.Min(
                config.General.MaximumBossContracts,
                config.General.ContractCount);

        // ==========================================
        // TARGET WEIGHTS
        // ==========================================

        if (!double.IsFinite(config.Targets.ScavWeight) ||
            !double.IsFinite(config.Targets.PmcWeight) ||
            !double.IsFinite(config.Targets.BossWeight) ||
            config.Targets.ScavWeight <= 0 ||
            config.Targets.PmcWeight < 0 ||
            config.Targets.BossWeight < 0 ||
            config.Targets.ScavWeight > 10000 ||
            config.Targets.PmcWeight > 10000 ||
            config.Targets.BossWeight > 10000)
        {
            logger.Warning(
                "[Most Wanted] Invalid target weights. " +
                "Using default values.");

            config.Targets = defaults.Targets;
        }

        // ==========================================
        // KILL REQUIREMENTS
        // ==========================================

        ValidateKillRange(
            config.Scav,
            defaults.Scav,
            "Scav");

        ValidateKillRange(
            config.Pmc,
            defaults.Pmc,
            "PMC");

        ValidateKillRange(
            config.Boss,
            defaults.Boss,
            "Boss");

        // ==========================================
        // REWARD AMOUNTS
        // ==========================================

        ValidateRewardSettings(
            config.Scav,
            defaults.Scav,
            "Scav");

        ValidateRewardSettings(
            config.Pmc,
            defaults.Pmc,
            "PMC");

        ValidateRewardSettings(
            config.Boss,
            defaults.Boss,
            "Boss");

        // ==========================================
        // BONUS ITEM COUNTS
        // ==========================================

        ValidateBonusItemCounts(
            config.Scav,
            defaults.Scav,
            "Scav");

        ValidateBonusItemCounts(
            config.Pmc,
            defaults.Pmc,
            "PMC");

        ValidateBonusItemCounts(
            config.Boss,
            defaults.Boss,
            "Boss");

        // ==========================================
        // JACKPOT PROBABILITIES
        // ==========================================

        ValidateJackpotSettings(defaults.Jackpot);

        // ==========================================
        // GLOBAL REWARD MULTIPLIERS
        // ==========================================

        ValidateRewardMultipliers(
            defaults.RewardMultipliers);

        logger.Info(
            "[Most Wanted] Configuration validation complete.");
    }

    // ==========================================
    // VALIDATE KILL REQUIREMENTS
    // ==========================================

    private void ValidateKillRange(
        ContractSettings settings,
        ContractSettings defaults,
        string contractType)
    {
        if (settings.MinKills < 1 ||
            settings.MaxKills < settings.MinKills ||
            settings.MaxKills > 100)
        {
            logger.Warning(
                $"[Most Wanted] Invalid {contractType} " +
                "kill requirements. Using default values.");

            settings.MinKills = defaults.MinKills;
            settings.MaxKills = defaults.MaxKills;
        }
    }

    // ==========================================
    // VALIDATE REWARD AMOUNTS
    // ==========================================

    private void ValidateRewardSettings(
        ContractSettings settings,
        ContractSettings defaults,
        string contractType)
    {
        // Maximum base rewards before multipliers:
        // 10 million roubles, 1 million XP,
        // 1,000 GP coins, and 1.0 reputation.

        if (settings.MinRoubles < 0 ||
            settings.MaxRoubles < settings.MinRoubles ||
            settings.MaxRoubles > 10000000)
        {
            logger.Warning(
                $"[Most Wanted] Invalid {contractType} " +
                "rouble rewards. Using defaults.");

            settings.MinRoubles = defaults.MinRoubles;
            settings.MaxRoubles = defaults.MaxRoubles;
        }

        if (settings.MinExperience < 0 ||
            settings.MaxExperience < settings.MinExperience ||
            settings.MaxExperience > 1000000)
        {
            logger.Warning(
                $"[Most Wanted] Invalid {contractType} " +
                "experience rewards. Using defaults.");

            settings.MinExperience = defaults.MinExperience;
            settings.MaxExperience = defaults.MaxExperience;
        }

        if (settings.MinGpCoins < 0 ||
            settings.MaxGpCoins < settings.MinGpCoins ||
            settings.MaxGpCoins > 1000)
        {
            logger.Warning(
                $"[Most Wanted] Invalid {contractType} " +
                "GP coin rewards. Using defaults.");

            settings.MinGpCoins = defaults.MinGpCoins;
            settings.MaxGpCoins = defaults.MaxGpCoins;
        }

        if (!double.IsFinite(settings.Reputation) ||
            settings.Reputation < 0 ||
            settings.Reputation > 1.0)
        {
            logger.Warning(
                $"[Most Wanted] Invalid {contractType} " +
                "reputation reward. Using default.");

            settings.Reputation = defaults.Reputation;
        }
    }

    // ==========================================
    // VALIDATE BONUS ITEM COUNTS
    // ==========================================

    private void ValidateBonusItemCounts(
        ContractSettings settings,
        ContractSettings defaults,
        string contractType)
    {
        if (settings.MinBonusItems < 0 ||
            settings.MaxBonusItems < settings.MinBonusItems ||
            settings.MaxBonusItems > 20)
        {
            logger.Warning(
                $"[Most Wanted] Invalid {contractType} " +
                "bonus item counts. Using defaults.");

            settings.MinBonusItems =
                defaults.MinBonusItems;

            settings.MaxBonusItems =
                defaults.MaxBonusItems;
        }
    }

    // ==========================================
    // VALIDATE JACKPOT PROBABILITIES
    // ==========================================

    private void ValidateJackpotSettings(
        JackpotSettings defaults)
    {
        var jackpot = config.Jackpot;

        var valid =
            jackpot.StandardChance >= 0 &&
            jackpot.StandardChance <= 100 &&
            jackpot.RareChance >= 0 &&
            jackpot.RareChance <= 100 &&
            jackpot.JackpotChance >= 0 &&
            jackpot.JackpotChance <= 100 &&
            jackpot.StandardChance +
            jackpot.RareChance +
            jackpot.JackpotChance == 100;

        if (!valid)
        {
            logger.Warning(
                "[Most Wanted] Invalid jackpot probabilities. " +
                "Using default chances.");

            jackpot.StandardChance =
                defaults.StandardChance;

            jackpot.RareChance =
                defaults.RareChance;

            jackpot.JackpotChance =
                defaults.JackpotChance;
        }
    }

    // ==========================================
    // VALIDATE GLOBAL REWARD MULTIPLIERS
    // ==========================================

    private void ValidateRewardMultipliers(
        RewardMultiplierSettings defaults)
    {
        var multipliers = config.RewardMultipliers;

        if (!IsValidMultiplier(multipliers.Roubles))
        {
            logger.Warning(
                "[Most Wanted] Invalid rouble multiplier. " +
                "Using default.");

            multipliers.Roubles = defaults.Roubles;
        }

        if (!IsValidMultiplier(multipliers.Experience))
        {
            logger.Warning(
                "[Most Wanted] Invalid experience multiplier. " +
                "Using default.");

            multipliers.Experience = defaults.Experience;
        }

        if (!IsValidMultiplier(multipliers.Reputation))
        {
            logger.Warning(
                "[Most Wanted] Invalid reputation multiplier. " +
                "Using default.");

            multipliers.Reputation = defaults.Reputation;
        }
    }

    private static bool IsValidMultiplier(double value)
    {
        return double.IsFinite(value) &&
               value >= 0 &&
               value <= 10;
    }
}
