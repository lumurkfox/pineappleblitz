using SPTarkov.Server.Core.Models.Spt.Mod;

namespace PineappleBlitz;

// SPT 4.1 replaced the AbstractModMetadata base record with the IModMetadata interface,
// dropped IsBundleMod and added HasPrepatcher.
public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "fox.pineappleblitz";
    public string Name { get; init; } = "PineappleBlitz";
    public string Author { get; init; } = "LumurkFox";
    public List<string>? Contributors { get; init; } = ["Echo55 (original concept)"];
    public SemanticVersioning.Version Version { get; init; } = new("2.1.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; } = [];
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = [];
    public string? Url { get; init; } = "https://github.com/lumurkfox/pineappleblitz";
    public string License { get; init; } = "MPL-2.0";
}
