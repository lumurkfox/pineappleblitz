using SPTarkov.Server.Core.Models.Spt.Mod;

namespace PineappleBlitz;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "fox.pineappleblitz";
    public override string Name { get; init; } = "PineappleBlitz";
    public override string Author { get; init; } = "LumurkFox";
    public override List<string>? Contributors { get; init; } = new() { "Echo55 (original concept)" };
    public override SemanticVersioning.Version Version { get; init; } = new("2.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; } = new();
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new();
    public override string? Url { get; init; } = "";
    public override bool? IsBundleMod { get; init; } = false;
    public override string License { get; init; } = "MPL-2.0";
}
