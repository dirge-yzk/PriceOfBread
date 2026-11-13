namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsSf.cs#1〕
public static partial class Instruments
{
    internal const string SfHeaderText = "quarter\tyear\tnation\tpensionLevel\telder60K\tsoldierElder60K\tpensionToSoldiersCum\t"
        + "cheapGoodCount\tdearGoodCount\tcheapExportValue\tcheapProducedRefValue\tcheapSubsidyPaidCum\t"
        + "voteWeightAll\tvoteWeightScoped\tvoteWeightUsed\tcolonialVoteWeight\tbudgetNegCumPopMonthK\tbudgetNegCumAmount\t"
        + "voteWeightScopedColonialOnly\tcolonialSuffrageLaw\tmigrantNativeSuffrageLaw\tbiasSwitchCum\tstressFoldCum\tcheapWithNegTariffCount\t"
        + "seatCons\tseatLib\tseatSoc\tgoverningParty";

    /// <summary>国の sf_s の行の値（列の順＝<see cref="SfHeaderText"/> の pensionLevel 以後）。番人（テスト）が世界から直に呼べるよう値の組で返す。</summary>
    internal static double[] SfRow(World w)
    {
        double level = w.Pension switch { ReformLevel.Full => 1.0, ReformLevel.Limited => 0.5, _ => 0.0 };
        double elder = 0, soldierElder = 0, all = 0, scoped = 0, colonial = 0, scopedCol = 0;
        foreach (var (rg, p) in w.AllPopsWithRegion())
        {
            double os = p.Size * p.AgeShare(60, double.PositiveInfinity);
            if (p.IsSoldier) soldierElder += os; else elder += os;
            double v = p.Size * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef);
            all += v; scoped += v * InterestGroups.SuffrageInclusion(w, rg, p);
            if (rg.Tier == RegionTier.Colonial) { colonial += v; scopedCol += v * InterestGroups.SuffrageInclusion(w, rg, p); } else scopedCol += v;
        }
        int ng = Catalog.Goods.Length;
        double cheapN = 0, dearN = 0, cheapExp = 0, cheapProd = 0;
        for (int g = 0; g < ng; g++)
        {
            int bias = w.TradeBiasOf(g);
            if (bias > 0) dearN++;
            else if (bias < 0)
            {
                cheapN++;
                if (w.ExportsByGood is { } eg && g < eg.Length) cheapExp += eg[g];
                double rp = g < Catalog.RefPrices.Length ? Catalog.RefPrices[g] : 0.0;
                foreach (var r in w.Regions)
                    if (g < r.Markets.Length) cheapProd += r.Markets[g].LastProduced * rp;
            }
        }
        return [level, elder, soldierElder, w.DiagSfPensionToSoldiersCum, cheapN, dearN, cheapExp, cheapProd, w.DiagSfCheapSubsidyPaidCum,
                all, scoped, w.DiagSfVoteWeightUsed, colonial, w.DiagSfBudgetNegCumPopMonthK, w.DiagSfBudgetNegCumAmount,
                scopedCol, (double)(int)w.ColonialSuffrage, (double)(int)w.MigrantNativeSuffrage, w.DiagSfBiasSwitchCum, w.DiagSfStressFoldCum, w.Tariff < 0 ? cheapN : 0.0,
                w.PartySeats[0], w.PartySeats[1], w.PartySeats[2], (double)w.GoverningParty];
    }

    static void SfFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var v = SfRow(w);
        sk.Sf ??= Open("sf", seed);
        if (!sk.SfHeader) { sk.Sf.WriteLine(SfHeaderText); sk.SfHeader = true; }
        var cells = new string[3 + v.Length];
        cells[0] = q.ToString(System.Globalization.CultureInfo.InvariantCulture); cells[1] = w.Year.ToString(System.Globalization.CultureInfo.InvariantCulture); cells[2] = NameOf(w);
        for (int i = 0; i < v.Length; i++) cells[3 + i] = F(v[i]);
        sk.Sf.WriteLine(string.Join('\t', cells));
    }
}
