

namespace TarkovMostWanted.Configuration;

using TarkovMostWanted.Bounties;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Server.Core.DI;
using SPTarkov.DI.Annotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ConfigRegistration : IOnDIConstruct
{
    public static async Task OnDIConstructAsync(
        IServiceCollection serviceCollection,
        CancellationToken cancellationToken)
    {
        var assemblyPath =
            Assembly.GetExecutingAssembly().Location;

        var modPath =
            Path.GetDirectoryName(assemblyPath)
            ?? throw new InvalidOperationException(
                "Unable to locate Most Wanted mod folder.");

        var dataPath = Path.Combine(modPath, "data");

        Directory.CreateDirectory(dataPath);

        var configPath = Path.Combine(
            dataPath, "config.json");

        var rewardsPath = Path.Combine(
            dataPath, "rewards.json");

        // ==========================================
        // GENERAL CONFIGURATION OPTIONS
        // ==========================================

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PreferredObjectCreationHandling =
                JsonObjectCreationHandling.Populate
        };

        // ==========================================
        // REWARD POOL CONFIGURATION OPTIONS
        // ==========================================

        var rewardJsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PreferredObjectCreationHandling =
                JsonObjectCreationHandling.Replace
        };

        var config = new MostWantedConfig();
        var rewardPools = new RewardPoolSettings();

        // ==========================================
        // LOAD CONFIG.JSON
        // ==========================================

        try
        {
            // Create default configuration if missing
            if (!File.Exists(configPath))
            {
                var defaultJson = JsonSerializer.Serialize(
                    config, jsonOptions);

                await File.WriteAllTextAsync(
                    configPath,
                    defaultJson,
                    cancellationToken);

                Console.WriteLine(
                    "[Most Wanted] Default config.json created.");
            }
            else
            {
                // Load existing player configuration
                await using var stream =
                    File.OpenRead(configPath);

                config = await JsonSerializer
                    .DeserializeAsync<MostWantedConfig>(
                        stream,
                        jsonOptions,
                        cancellationToken)
                    ?? new MostWantedConfig();

                Console.WriteLine(
                    "[Most Wanted] Configuration loaded.");
            }
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            Console.WriteLine(
                "[Most Wanted] Configuration error: " +
                exception.Message);

            Console.WriteLine(
                "[Most Wanted] Using default settings.");

            config = new MostWantedConfig();
        }

        // ==========================================
        // LOAD REWARDS.JSON
        // ==========================================

        try
        {
            // Create default reward pools if missing
            if (!File.Exists(rewardsPath))
            {
                var defaultRewardsJson =
                    JsonSerializer.Serialize(
                        rewardPools,
                        rewardJsonOptions);

                await File.WriteAllTextAsync(
                    rewardsPath,
                    defaultRewardsJson,
                    cancellationToken);

                Console.WriteLine(
                    "[Most Wanted] Default rewards.json created.");
            }
            else
            {
                // Load customized reward pools
                await using var stream =
                    File.OpenRead(rewardsPath);

                rewardPools = await JsonSerializer
                    .DeserializeAsync<RewardPoolSettings>(
                        stream,
                        rewardJsonOptions,
                        cancellationToken)
                    ?? throw new JsonException(
                        "Reward configuration is empty.");

                Console.WriteLine(
                    "[Most Wanted] Reward pools loaded.");
            }
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            Console.WriteLine(
                "[Most Wanted] Reward configuration error: " +
                exception.Message);

            Console.WriteLine(
                "[Most Wanted] Using default reward pools.");

            rewardPools = new RewardPoolSettings();
        }

        // ==========================================
        // REGISTER CONFIGURATION SERVICES
        // ==========================================

        BountyRewardPools.Initialize(rewardPools);

        serviceCollection.AddSingleton(config);
        serviceCollection.AddSingleton(rewardPools);
    }
}
