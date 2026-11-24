
namespace TarkovMostWanted.Patches;

using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Generators.RepeatableQuests;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Repeatable;
using TarkovMostWanted.Configuration;

[Injectable]
public class BossLimitPatch : AbstractPatch
{
    private const string HandlerId =
        "6aff49a05ada18d09f1b9e72";

    private const string BountyCategory = "Bounties";

    private static ProfileHelper _profileHelper = null!;

    private static ISptLogger<BossLimitPatch> _logger = null!;

    private static MostWantedConfig _config = null!;

    public BossLimitPatch(
        ProfileHelper profileHelper,
        ISptLogger<BossLimitPatch> logger,
        MostWantedConfig config)
    {
        _profileHelper = profileHelper;
        _logger = logger;
        _config = config;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(EliminationQuestGenerator),
            nameof(EliminationQuestGenerator.Generate));
    }

    [PatchPrefix]
    public static void Prefix(
        MongoId sessionId,
        MongoId traderId,
        QuestTypePool questTypePool,
        RepeatableQuestConfig repeatableConfig)
    {
        // Only affect Handler's bounty contracts
        if (traderId != HandlerId ||
            repeatableConfig.Name != BountyCategory)
        {
            return;
        }

        // Read maximum boss contracts from config
        var maxBossContracts =
            _config.General.MaximumBossContracts;

        // Collect boss target identifiers
        var bossTargets = repeatableConfig.QuestConfig.Elimination
            .SelectMany(config => config.Targets)
            .Where(target => target.Data?.IsBoss == true)
            .Select(target => target.Key)
            .OfType<string>()
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet();

        var bossCount = 0;

        var pmcData = _profileHelper.GetPmcProfile(sessionId);

        var category = pmcData?.RepeatableQuests?
            .FirstOrDefault(q => q.Name == BountyCategory);

        if (category?.ActiveQuests != null)
        {
            foreach (var quest in category.ActiveQuests)
            {
                if (quest.TraderId != HandlerId)
                {
                    continue;
                }

                var status = pmcData?.Quests?
                    .FirstOrDefault(q => q.QId == quest.Id);

                // Ignore completed and failed contracts
                if (status?.Status == QuestStatusEnum.Success ||
                    status?.Status == QuestStatusEnum.Fail)
                {
                    continue;
                }

                // Identify boss contracts from kill conditions
                var isBossContract =
                    quest.Conditions.AvailableForFinish?
                        .Any(condition =>
                            condition.Counter?.Conditions?
                                .Any(counter =>
                                    counter.SavageRole?
                                        .Any(role =>
                                            role != null &&
                                            bossTargets.Contains(role))
                                    == true)
                            == true)
                    == true;

                if (isBossContract)
                {
                    bossCount++;
                }
            }
        }

        // Allow boss generation when below the limit
        if (bossCount < maxBossContracts)
        {
            return;
        }

        var targets = questTypePool.Pool.Elimination.Targets;

        if (targets == null)
        {
            return;
        }

        // Prevent additional boss contracts
        foreach (var boss in bossTargets)
        {
            targets.Remove(boss);
        }

        // Optional debug logging
        if (_config.General.EnableDebugLogging)
        {
            _logger.Info(
                $"[Most Wanted] Boss contract limit reached " +
                $"({bossCount}/{maxBossContracts}). " +
                "Generating PMC or Scav contracts only.");
        }
    }
}
