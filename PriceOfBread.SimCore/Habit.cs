namespace PriceOfBread.SimCore;

public readonly record struct HabitProfile(bool Forms, double Growth, double Decay, double Cap, double LaborPenalty, double MiseryScale, bool Logistic = false)
{
    public bool Active => Forms && Growth > 0;
}

public static class Habit
{
    public static HabitProfile Of(Coefficients c, int goodId) =>
        goodId == c.OpiumGoodId
            ? new HabitProfile(true, c.OpiumAddictionGrowth, c.OpiumAddictionDecay, c.OpiumAddictionCap, c.OpiumLaborPenalty, c.OpiumMiseryScale, c.OpiumAddictionLogistic > 0)
            : default;

    public static double StockOf(Region region, in HabitProfile h) => h.Forms ? region.OpiumAddiction : 0;

    public static double NextStock(in HabitProfile h, double stock, double perCapita) =>
        PoliticalQuantities.ClampOr(h.Logistic ? stock + h.Growth * perCapita * (1 - stock / h.Cap) - h.Decay * stock
                                               : stock + h.Growth * perCapita - h.Decay * stock, 0, h.Cap, 0);

    public static double LaborFactor(Coefficients c, Region region)
    {
        var h = Of(c, c.OpiumGoodId);
        return h.Active && region.OpiumAddiction > 0 ? Math.Max(0.5, 1.0 - h.LaborPenalty * region.OpiumAddiction) : 1.0;
    }
}
