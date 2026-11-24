namespace TarkovMostWanted.Trader;

using SPTarkov.Server.Core.Models.Spt.Mod;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.jayoh99.Tushonka: Most Wanted";

    public string Name { get; init; } = "Tarkov: Most Wanted";

    public string Author { get; init; } = "jayoh";

    public List<string>? Contributors { get; init; }

    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");

    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.4");

    public List<string>? Incompatibilities { get; init; }

    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }

    public string? Url { get; init; }

    public string License { get; init; } = "MIT";

    public bool HasPrepatcher { get; init; } = false;
}
