namespace PriceOfBread.SimCore;

// 国×四半期の行（挙げた国は毎四半期・他の全ての国は年末）。〔履歴 InstrumentsScalars.cs#1〕
public static partial class Instruments
{
    internal const string ScalarsHeader = "quarter\tyear\tnation\tborn\tpopK\tatWar\tpolityCode\teducationLaw\tsuffrage\tcensorship\tbureaucracy\t"
        + "effCorruption\tdynMandate\tlegitimacy\tdefeatShock\twarExhaustion\tmilWarMean\tmilitancyMean\tdmWar\t"
        + "adminEfficiency\tcrown\tchildLaborLegal\tworkHoursValue\t"
        + "socialSecurityLawCode\tantitrustLawCode\tprogressiveTaxShare\tnationalizedMaskCode\t"
        + "tariffTargetIndex\ttradeToGdpRatio\t"
        + "externalPressure\tfreightRevolution\twomenRightsCode";   // tfreightRevolution〔履歴 InstrumentsScalars.cs#2〕

    // 清算の直近 1 回分の断面）。〔履歴 InstrumentsScalars.cs#3〕
    static double TradeToGdp(World w)
    {
        double ex = 0, im = 0;
        if (w.ExportsByGood is { } eg) foreach (var v in eg) ex += v;
        if (w.ImportsByGood is { } ig) foreach (var v in ig) im += v;
        return (ex + im) * w.Coef.ClearingsPerYear / System.Math.Max(1.0, Calendar.PerMonthToPerYear(w.NominalGdpPerMonth).Value);
    }

    static void ScalarsFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!Tg.Scalars) return;   // (1256) 書く表がどれも選ばれていなければ積算ごと飛ぶ
        if (!(picked || yearEnd)) return;
        double popK = w.TotalPopulation;
        if (!(popK > 0)) return;   // 死んだ国（人口 0）は行を出さない
        double sz = 0, milWar = 0, mil = 0;
        foreach (var p in w.AllPops()) { sz += p.Size; milWar += p.MilWar * p.Size; mil += p.Militancy * p.Size; }
        if (sz > 0) { milWar /= sz; mil /= sz; }
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var c = w.Coef;
        sk.Sca ??= Open("scalars", seed);
        if (!sk.ScaHeader) { sk.Sca.WriteLine(ScalarsHeader); sk.ScaHeader = true; }
        sk.Sca.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), w.BornOfSecession ? "1" : "0", F(popK), Government.AtWar(w) ? "1" : "0",
            I((int)w.Polity), I((int)w.EducationLaw), I((int)w.Suffrage), I((int)w.Censorship), I((int)w.Bureaucracy),
            F(w.EffectiveCorruption), F(w.DynasticMandate), F(w.Legitimacy), F(w.DefeatShock),
            F(w.SelfPower?.WarExhaustion ?? 0), F(milWar), F(mil), F(-c.DynMandateWarWeight * (w.SelfPower?.WarExhaustion ?? 0)),
            F(w.AdminEfficiency), I((int)w.Crown), F(w.ChildLaborLegal), F(w.WorkHoursValue),
            I((int)w.SocialSecurityLaw), I((int)w.Antitrust), F(w.ProgressiveTax), I(w.NationalizedMask()),
            F(w.Policy.TariffTarget), F(TradeToGdp(w)),
            F(w.ExternalPressure), F(1.0), I((int)w.WomenRights)));
    }
}
