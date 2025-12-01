namespace PineappleBlitz;

public class ModConfig
{
    public double FuzeTimer { get; set; } = 1.5;
    public int Fragmentations { get; set; } = 250;
    public int ExplosionMinimum { get; set; } = 20;
    public int ExplosionMaximum { get; set; } = 25;
    public double HeavyBleedPercent { get; set; } = 0.57;
    public double LightBleedPercent { get; set; } = 0.87;
    public int Damage { get; set; } = 50;
    public int Penetration { get; set; } = 120;
    public int Price { get; set; } = 5000;
    public bool BlacklistFromBots { get; set; } = true;
}
