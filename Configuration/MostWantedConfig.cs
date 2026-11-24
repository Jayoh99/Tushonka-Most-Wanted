
namespace TarkovMostWanted.Configuration;

public class MostWantedConfig
{
    public GeneralSettings General { get; set; } = new();

    public TargetSettings Targets { get; set; } = new();

    public ContractSettings Scav { get; set; } = new()
    {
        MinKills = 5,
        MaxKills = 15,
        MinRoubles = 20000,
        MaxRoubles = 50000,
        MinExperience = 3000,
        MaxExperience = 8000,
        Reputation = 0.01,
        MinGpCoins = 0,
        MaxGpCoins = 0,
        MinBonusItems = 0,
        MaxBonusItems = 0
    };

    public ContractSettings Pmc { get; set; } = new()
    {
        MinKills = 2,
        MaxKills = 5,
        MinRoubles = 50000,
        MaxRoubles = 100000,
        MinExperience = 8000,
        MaxExperience = 15000,
        Reputation = 0.02,
        MinGpCoins = 2,
        MaxGpCoins = 5,
        MinBonusItems = 1,
        MaxBonusItems = 2
    };

    public ContractSettings Boss { get; set; } = new()
    {
        MinKills = 1,
        MaxKills = 1,
        MinRoubles = 100000,
        MaxRoubles = 250000,
        MinExperience = 15000,
        MaxExperience = 25000,
        Reputation = 0.03,
        MinGpCoins = 5,
        MaxGpCoins = 10,
        MinBonusItems = 2,
        MaxBonusItems = 4
    };

    public JackpotSettings Jackpot { get; set; } = new();

    public RewardMultiplierSettings RewardMultipliers
    { get; set; } = new();
}

public class GeneralSettings
{
    public int MinimumPlayerLevel { get; set; } = 1;

    public int ContractCount { get; set; } = 5;

    public int RefreshHours { get; set; } = 24;

    public bool AutomaticReplacement { get; set; } = true;

    public int MaximumBossContracts { get; set; } = 2;

    public bool EnableDebugLogging { get; set; } = false;
}

public class TargetSettings
{
    public double ScavWeight { get; set; } = 40;

    public double PmcWeight { get; set; } = 40;

    public double BossWeight { get; set; } = 20;
}

public class ContractSettings
{
    public int MinKills { get; set; }

    public int MaxKills { get; set; }

    public int MinRoubles { get; set; }

    public int MaxRoubles { get; set; }

    public int MinExperience { get; set; }

    public int MaxExperience { get; set; }

    public double Reputation { get; set; }

    public int MinGpCoins { get; set; }

    public int MaxGpCoins { get; set; }

    public int MinBonusItems { get; set; }

    public int MaxBonusItems { get; set; }
}

public class JackpotSettings
{
    public int StandardChance { get; set; } = 45;

    public int RareChance { get; set; } = 50;

    public int JackpotChance { get; set; } = 5;
}

public class RewardMultiplierSettings
{
    public double Roubles { get; set; } = 1.0;

    public double Experience { get; set; } = 1.0;

    public double Reputation { get; set; } = 1.0;
}
