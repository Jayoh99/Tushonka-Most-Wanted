
namespace TarkovMostWanted.Trader;

using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Common.Models.Logging;
using System.Reflection;
using Path = System.IO.Path;

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 1)]
public class HandlerTrader(
    ModHelper modHelper,
    ImageRouter imageRouter,
    TraderConfig traderConfig,
    RagfairConfig ragfairConfig,
    TimeUtil timeUtil,
    HandlerTraderHelper traderHelper,
    ISptLogger<HandlerTrader> logger)
    : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // Get the mod directory
        var pathToMod = modHelper.GetAbsolutePathToModFolder(
            Assembly.GetExecutingAssembly());

        // Get Handler's portrait
        var traderImagePath = Path.Combine(
            pathToMod, "data/handler.jpg");

        // Load trader settings
        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(
            pathToMod, "data/base.json");

        if (traderBase == null)
        {
            logger.Error("[Most Wanted] Failed to load base.json.");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(traderBase.Avatar))
        {
            logger.Error("[Most Wanted] Handler's avatar path is missing.");
            return Task.CompletedTask;
        }

        // Register trader portrait
        imageRouter.AddRoute(
            traderBase.Avatar.Replace(".jpg", ""),
            traderImagePath);

        // Set inventory refresh time
        traderHelper.SetTraderUpdateTime(
            traderConfig,
            traderBase,
            timeUtil.GetHoursAsSeconds(1),
            timeUtil.GetHoursAsSeconds(2));

        // Disable flea market offers
        ragfairConfig.Traders.TryAdd(traderBase.Id, false);

        // Register Handler
        traderHelper.AddTraderWithEmptyAssortToDb(traderBase);

        // Register trader localization
        traderHelper.AddTraderToLocales(
            traderBase,
            "Handler",
            "Little is known about Handler's past. " +
            "Operating through an underground intelligence network, " +
            "he offers lucrative contracts to mercenaries willing " +
            "to hunt down Tarkov's most wanted individuals. " +
            "Complete the contract, collect your payment, " +
            "and don't ask questions.");

        // Load trader inventory
        var assort = modHelper.GetJsonDataFromFile<TraderAssort>(
            pathToMod, "data/assort.json");

        if (assort == null)
        {
            logger.Warning("[Most Wanted] Failed to load assort.json.");
        }
        else
        {
            traderHelper.OverwriteTraderAssort(
                traderBase.Id, assort);
        }

        logger.Info("[Most Wanted] Handler registration finished.");

        return Task.CompletedTask;
    }
}

