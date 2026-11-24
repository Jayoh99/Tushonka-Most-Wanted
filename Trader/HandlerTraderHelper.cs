
namespace TarkovMostWanted.Trader;

using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Models.Spt.Tables;

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 1)]
public class HandlerTraderHelper(
    ISptLogger<HandlerTraderHelper> logger,
    ICloner cloner,
    TradersTable tradersTable,
    LocaleTable localeTable)
{
    // Configure inventory refresh time
    public void SetTraderUpdateTime(
        TraderConfig traderConfig,
        TraderBase baseJson,
        int refreshTimeSecondsMin,
        int refreshTimeSecondsMax)
    {
        traderConfig.UpdateTime.Add(new UpdateTime
        {
            TraderId = baseJson.Id,
            Seconds = new MinMax<int>(
                refreshTimeSecondsMin,
                refreshTimeSecondsMax)
        });
    }

    // Register Handler in the trader database
    public void AddTraderWithEmptyAssortToDb(
        TraderBase traderDetails)
    {
        var clonedBase = cloner.Clone(traderDetails);

        if (clonedBase == null)
        {
            logger.Error("[Most Wanted] Failed to clone Handler's base.");
            return;
        }

        var trader = new Trader
        {
            Base = clonedBase,

            Assort = new TraderAssort
            {
                Items = [],
                BarterScheme = new(),
                LoyalLevelItems = new()
            },

            QuestAssort = new()
            {
                { "Started", new() },
                { "Success", new() },
                { "Fail", new() }
            },

            Dialogue = []
        };

        if (!tradersTable.TryAdd(traderDetails.Id, trader))
        {
            logger.Warning(
                "[Most Wanted] Failed to register Handler.");
        }
    }

    // Register Handler's localized information
    public void AddTraderToLocales(
        TraderBase baseJson,
        string firstName,
        string description)
    {
        var traderId = baseJson.Id;

        foreach (var (_, locale) in localeTable.Global)
        {
            locale.AddTransformer(localeData =>
            {
                if (localeData == null)
                {
                    return localeData;
                }

                localeData[$"{traderId} FullName"] =
                    baseJson.Name ?? "Handler";

                localeData[$"{traderId} FirstName"] =
                    firstName;

                localeData[$"{traderId} Nickname"] =
                    baseJson.Nickname ?? "Handler";

                localeData[$"{traderId} Location"] =
                    baseJson.Location ?? "Undisclosed";

                localeData[$"{traderId} Description"] =
                    description;

                return localeData;
            });
        }
    }

    // Load Handler's inventory
    public void OverwriteTraderAssort(
        string traderId,
        TraderAssort newAssorts)
    {
        if (!tradersTable.TryGetValue(
            traderId, out var trader) || trader == null)
        {
            logger.Warning(
                "[Most Wanted] Handler was not found.");

            return;
        }

        trader.Assort = newAssorts;
    }
}

