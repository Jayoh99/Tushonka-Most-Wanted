
namespace TarkovMostWanted.Bounties;

using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Common.Models.Logging;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 2)]
public class BountyLocales(
    LocaleTable localeTable,
    ISptLogger<BountyLocales> logger)
    : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        const string traderId = "6aff49a05ada18d09f1b9e72";
        const string templateId = "616052ea3054fc0e2c24ce6e";

        foreach (var (_, locale) in localeTable.Global)
        {
            locale.AddTransformer(localeData =>
            {
                if (localeData == null)
                {
                    return localeData;
                }

                localeData[$"{templateId} name {traderId}"] =
                    "Bounty Contract";

                localeData[$"{templateId} note {traderId}"] =
                    "Eliminate the designated targets.";

                localeData[$"{templateId} description {traderId} 0"] =
                    "I've received a new contract. Someone wants these targets removed. " +
                    "Take care of them and return to me for your payment.";

                localeData[$"{templateId} successMessageText {traderId} 0"] =
                    "Good work. The client is satisfied. Here's your payment.";

                localeData[$"{templateId} failMessageText {traderId} 0"] =
                    "You missed your opportunity. The contract has expired.";

                localeData[$"{templateId} startedMessageText {traderId} 0"] =
                    "The contract is yours. Don't disappoint me.";

                localeData[$"{templateId} changeQuestMessageText {traderId} 0"] =
                    "Not interested? Fine. I have other contracts available.";

                localeData[$"{templateId} acceptPlayerMessage {traderId}"] =
                    "I'll handle it.";

                localeData[$"{templateId} declinePlayerMessage {traderId}"] =
                    "Not interested.";

                localeData[$"{templateId} completePlayerMessage {traderId}"] =
                    "The targets have been eliminated.";

                // Fix missing boss objective localization
                localeData["QuestCondition/Elimination/Kill/BotRole/bossKolontay"] =
                    "Kollontay";

                return localeData;

            });
        }

        logger.Info("[Most Wanted] Bounty localization registered.");

        return Task.CompletedTask;
    }
}

