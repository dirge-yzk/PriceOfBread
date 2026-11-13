namespace PriceOfBread.SimCore;

public sealed class AgeLayout
{
    public const int MaxBands = 11;
    public const int MaxSlots = 2 * MaxBands;

    public readonly int Bands;
    public readonly double[] Lower;
    public readonly double[] Upper;
    public readonly double[] Width;
    public readonly double[] MonthsPerBand;
    public readonly double[] Pyramid;
    public readonly int AdultFirstBand;
    public readonly int EntryBand;
    public readonly double AdultAge;
    public readonly int SoldierBandA, SoldierBandB;
    public readonly int OldBandA, OldBandB;

    private AgeLayout(double[] lower, double[] width, double[] pyramid, int adultFirstBand, int entryBand, double adultAge, int soldierA, int soldierB, int oldA, int oldB)
    {
        Bands = lower.Length; Lower = lower; Width = width; Pyramid = pyramid;
        Upper = new double[Bands]; MonthsPerBand = new double[Bands];
        for (int b = 0; b < Bands - 1; b++) { Upper[b] = lower[b] + width[b]; MonthsPerBand[b] = width[b] * 12.0; }
        AdultFirstBand = adultFirstBand; EntryBand = entryBand; AdultAge = adultAge; SoldierBandA = soldierA; SoldierBandB = soldierB; OldBandA = oldA; OldBandB = oldB;
    }

    public static readonly AgeLayout Bands8 = new(
        [0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0],
        [10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 10.0, 0.0],
        [0.255, 0.195, 0.160, 0.130, 0.105, 0.080, 0.050, 0.025],
        adultFirstBand: 2, entryBand: 1, adultAge: 20.0, soldierA: 2, soldierB: 3, oldA: 6, oldB: 7);

    public static readonly AgeLayout Bands11 = new(
        [0.0, 1.0, 6.0, 11.0, 16.0, 21.0, 31.0, 41.0, 51.0, 61.0, 71.0],
        [1.0, 5.0, 5.0, 5.0, 5.0, 10.0, 10.0, 10.0, 10.0, 10.0, 0.0],
        [0.0300, 0.1317, 0.1150, 0.1005, 0.0903, 0.1568, 0.1273, 0.1025, 0.0770, 0.0474, 0.0215],
        adultFirstBand: 5, entryBand: 4, adultAge: 21.0, soldierA: 5, soldierB: 6, oldA: 9, oldB: 10);

    public static AgeLayout OfLength(int length)
        => length == 2 * 8 ? Bands8 : length == 2 * 11 ? Bands11 : throw new ArgumentException($"年齢の層の配列の長さが不正（{length}）＝16（8層）か22（11層）");

    internal static AgeLayout FromEnvironment() => Parse(Environment.GetEnvironmentVariable("POB_AGE_BANDS"));

    internal static AgeLayout Parse(string? v)
        => string.IsNullOrEmpty(v) || v == "11" ? Bands11 : v == "8" ? Bands8 : throw new InvalidOperationException($"POB_AGE_BANDS は 8 か 11（{v}）");

    internal double Overlap(int b, double lo, double hi)
    {
        if (b == Bands - 1) return lo <= Lower[b] && double.IsPositiveInfinity(hi) ? 1.0 : 0.0;
        double bl = Lower[b], ov = Math.Min(hi, Upper[b]) - Math.Max(lo, bl);
        return ov > 0 ? ov / Width[b] : 0.0;
    }

    internal double Outflow(int b, double band) => band / MonthsPerBand[b];
}
