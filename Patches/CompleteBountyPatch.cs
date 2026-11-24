

namespace TarkovMostWanted.Patches;

using System.Reflection;
using HarmonyLib;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Quests;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using TarkovMostWanted.Configuration;

[Injectable]
public class CompleteBountyPatch : AbstractPatch
{
    private const string HandlerId =
        "6aff49a05ada18d09f1b9e72";

    private const string BountyCategory = "Bounties";

    private static QuestConfig _questConfig = null!;
    private static RepeatableQuestController _controller = null!;
    private static ICloner _cloner = null!;
    private static RandomUtil _randomUtil = null!;
    private static ISptLogger<CompleteBountyPatch> _logger = null!;
    private static MostWantedConfig _config = null!;

    public CompleteBountyPatch(
        QuestConfig questConfig,
        RepeatableQuestController controller,
        ICloner cloner,
        RandomUtil randomUtil,
        ISptLogger<CompleteBountyPatch> logger,
        MostWantedConfig config)
    {
        _questConfig = questConfig;
        _controller = controller;
        _cloner = cloner;
        _randomUtil = randomUtil;
        _logger = logger;
        _config = config;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(QuestController),
            nameof(QuestController.CompleteQuest));
    }

    [PatchPrefix]
    public static void Prefix(
        PmcData pmcData,
        CompleteQuestRequestData request,
        out bool __state)
    {
        __state = false;

        // Only process Handler's bounty category
        var category = pmcData.RepeatableQuests?
            .FirstOrDefault(q => q.Name == BountyCategory);

        if (category?.ActiveQuests == null)
        {
            return;
        }

        var bounty = category.ActiveQuests
            .FirstOrDefault(q =>
                q.Id == request.QuestId &&
                q.TraderId == HandlerId);

        if (bounty == null)
        {
            return;
        }

        // Only replace quests ready to complete
        var status = pmcData.Quests?
            .FirstOrDefault(q => q.QId == request.QuestId);

        __state = status?.Status ==
            QuestStatusEnum.AvailableForFinish;
    }

    [PatchPostfix]
    public static void Postfix(
        ItemEventRouterResponse __result,
        PmcData pmcData,
        CompleteQuestRequestData request,
        MongoId sessionId,
        bool __state)
    {
        if (!__state)
        {
            return;
        }

        // Confirm native completion succeeded
        var status = pmcData.Quests?
            .FirstOrDefault(q => q.QId == request.QuestId);

        if (status?.Status != QuestStatusEnum.Success)
        {
            return;
        }

        // ==========================================
        // CONFIGURABLE AUTOMATIC REPLACEMENT
        // ==========================================

        if (!_config.General.AutomaticReplacement)
        {
            if (_config.General.EnableDebugLogging)
            {
                _logger.Info(
                    "[Most Wanted] Bounty completed. " +
                    "Automatic replacement is disabled.");
            }

            return;
        }

        try
        {
            if (!__result.ProfileChanges.TryGetValue(
                    sessionId, out var profileChanges))
            {
                return;
            }

            var category = pmcData.RepeatableQuests?
                .FirstOrDefault(q => q.Name == BountyCategory);

            if (category?.ActiveQuests == null)
            {
                return;
            }

            var completedBounty = category.ActiveQuests
                .FirstOrDefault(q =>
                    q.Id == request.QuestId &&
                    q.TraderId == HandlerId);

            if (completedBounty == null)
            {
                return;
            }

            var config = _questConfig.RepeatableQuests
                .FirstOrDefault(q => q.Name == BountyCategory);

            if (config == null)
            {
                _logger.Warning(
                    "[Most Wanted] Bounty configuration not found.");

                return;
            }

            // Validate player information before generation
            var playerLevel = pmcData.Info?.Level;

            if (!playerLevel.HasValue ||
                pmcData.TradersInfo == null)
            {
                _logger.Warning(
                    "[Most Wanted] Player profile information missing.");

                return;
            }

            // Generate a fresh native quest pool
            var questPool = Traverse.Create(_controller)
                .Method(
                    "GenerateQuestPool",
                    config,
                    playerLevel.Value)
                .GetValue();

            if (questPool == null)
            {
                _logger.Warning(
                    "[Most Wanted] Failed to generate quest pool.");

                return;
            }

            // Generate a replacement bounty
            var replacement = Traverse.Create(_controller)
                .Method(
                    "AttemptToGenerateRepeatableQuest",
                    sessionId,
                    pmcData,
                    questPool,
                    config)
                .GetValue<RepeatableQuest>();

            if (replacement == null ||
                replacement.TraderId != HandlerId)
            {
                _logger.Warning(
                    "[Most Wanted] Failed to generate replacement bounty.");

                return;
            }

            // Assign quest side with a safe fallback
            replacement.Side =
                Enum.GetName(config.Side) ?? "Pmc";

            // Prepare replacement requirements
            var changeRequirement = new ChangeRequirement
            {
                ChangeCost = replacement.ChangeCost,
                ChangeStandingCost =
                    _randomUtil.GetArrayValue(config.StandingChangeCost)
            };

            // Clone category before modifying live data
            var updatedCategory = _cloner.Clone(category);

            if (updatedCategory?.ActiveQuests == null)
            {
                return;
            }

            updatedCategory.ActiveQuests.RemoveAll(
                q => q.Id == completedBounty.Id);

            updatedCategory.ActiveQuests.Add(replacement);

            updatedCategory.ChangeRequirement.Remove(
                completedBounty.Id);

            updatedCategory.ChangeRequirement[replacement.Id] =
                changeRequirement;

            var clientCategory = _cloner.Clone(updatedCategory);

            if (clientCategory == null)
            {
                return;
            }

            clientCategory.InactiveQuests = [];

            // Apply replacement to server profile
            category.ActiveQuests = updatedCategory.ActiveQuests;
            category.ChangeRequirement =
                updatedCategory.ChangeRequirement;

            // Send updated bounty category to client
            profileChanges.RepeatableQuests ??= [];

            profileChanges.RepeatableQuests.RemoveAll(
                q => q.Name == BountyCategory);

            profileChanges.RepeatableQuests.Add(clientCategory);

            _logger.Info(
                "[Most Wanted] Bounty completed. " +
                $"New contract generated: {replacement.Id}");
        }
        catch (Exception exception)
        {
            _logger.Error(
                "[Most Wanted] Replacement bounty error: " +
                exception);
        }
    }
}
