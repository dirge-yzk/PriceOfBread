namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsSecession.cs#1〕
public static partial class Instruments
{
    internal const string SecessionHeaderText = "quarter\tyear\tnation\tregion\ttier\t"
        + "legitimacyIndex\ttotalPopK\tcoerceIndex\tholdBaseIndex\tholdBaseRebuildGapIndex\tstanceIndex\tsharedDivIndex\trestiveCount\t"
        + "popK\tcrowdK\tcrowdFactorShare\tmilitancyMeanIndex\tcultureDistanceIndex\tcoreShareMeanShare\tsuffrageInclusionShare\t"
        + "tariffInterestIndex\ttariffGapIndex\ttariffTermIndex\tethnicTermIndex\tsuffrageFactorShare\t"
        + "pressureIndex\tpressureRebuiltIndex\tpressureRebuildGapIndex\teffHoldBeforeShareIndex\tholdShareIndex\tmarginIndex\tdominanceShare\tgoFlag\tcapitalDistanceKm\t"
        + "orderRawNowIndex\torderOldIndex\tmilitancyNationMeanIndex\tmilitancyRegionOwnIndex\torderRegionNowIndex\treachOwnShare\t"
        + "ungovTargetOldShare\tungovTargetNewShare\truleUngovBeforeShare\truleForeignBeforeShare";

    // ⑧首都の行は無い（CheckSecession の州の行と同じ）＝m̄ にだけ入る。〔履歴 InstrumentsSecession.cs#2〕
    static double SecC01(double x) => !(x > 0) ? 0 : x > 1 ? 1 : x;

    static string SecP1Columns(World w, Coefficients c, Region r, double rawNow, double orderOld, double mbar)
    {
        double mil = 0, size = 0;
        foreach (var p in r.Pops) if (p.IsCommoner) { mil += p.Militancy * p.Size; size += p.Size; }
        double mr = size > 1e-9 ? mil / size : 0;
        double orderR = !(mr > 0) ? 1.0 : SecC01(rawNow * mbar / mr);
        double reach = 1.0;
        var rc = Places.Coord(r.Name);
        if (!(rc.Lat == 0 && rc.Lon == 0))
        {
            double km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
            if (km > 0 && double.IsFinite(km))
            {
                double v = 1.0 / (1.0 + c.DistanceWarDecay * km / 10_000.0);
                reach = !(v > 0 && v <= 1.0) ? 1.0 : v;
            }
        }
        double before3 = r.Rule.Length == 4 ? r.Rule[3] : double.NaN, before2 = r.Rule.Length == 4 ? r.Rule[2] : double.NaN;
        return string.Join('\t', F(rawNow), F(orderOld), F(mbar), F(mr), F(orderR), F(reach),
            F((1.0 - orderOld) * (1.0 - reach)), F((1.0 - orderR) * (1.0 - reach)), F(before3), F(before2));
    }

    /// <summary>CheckSecession が支配度を積分した直後に呼ぶ（Instruments.On のときだけ）。行は Quarter の SecFold が書く。
    /// hold＝共有分母で割った後の国の保持力（本体の変数そのまま）・margins＝州ごとの（共有分母の前の引き留め, 本体の圧力）。</summary>
    internal static void SecObserve(World w, Coefficients c, double coerce, double stance, double hold, double sharedDiv, int restive, List<(Region R, double Hold, double Pressure)> margins)
    {
        var st = S(w);
        var rows = new List<string>(margins.Count);
        var cap = w.Regions[0];
        double holdBase = w.Legitimacy / (1 + w.TotalPopulation * c.SecessionHoldPerPop)
                        + c.SecessionHoldCoercion * c.SecessionThreshold * (coerce / (coerce + c.PoliticalReferencePop));
        double holdBaseObserved = hold * sharedDiv;
        string head = string.Join('\t', F(w.Legitimacy), F(w.TotalPopulation), F(coerce), F(holdBase), F(Rel(holdBase, holdBaseObserved)), F(stance), F(sharedDiv),
            restive.ToString(System.Globalization.CultureInfo.InvariantCulture));
        // effNow〔履歴 InstrumentsSecession.cs#3〕
        double effNow = w.Coercion * (w.TotalPopulation / Math.Max(1.0, c.PoliticalReferencePop));
        double rawNow = effNow / Math.Max(1e-9, w.TotalPopulation * c.CrimeOrderPerPop);
        double orderOld = SecC01(w.PublicOrder);
        double milAll = 0, sizeAll = 0;
        foreach (var rr in w.Regions)
        {
            double mi = 0, si = 0;
            foreach (var p in rr.Pops) if (p.IsCommoner) { mi += p.Militancy * p.Size; si += p.Size; }
            milAll += mi; sizeAll += si;
        }
        double mbar = sizeAll > 1e-9 ? milAll / sizeAll : 0;
        foreach (var (r, effBefore, pressure) in margins)
        {
            var p = Politics.SecessionParts(w, c, r, cap, stance, r.TariffInterest);   // (1220) 写しをやめエンジンと同じ関数＝pressureRebuildGapIndex は作りから 0
            double pop = 0; foreach (var pp in r.Pops) pop += pp.Size;
            double holdShare = effBefore / sharedDiv;
            double km = double.NaN;
            var rc = Places.Coord(r.Name);
            if (!(rc.Lat == 0 && rc.Lon == 0)) km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
            rows.Add(string.Join('\t', r.Name, r.Tier.ToString(), head,
                F(pop), F(p.CrowdK), F(p.CrowdFactor), F(p.Militancy), F(p.CultureDistance), F(p.CoreShare), F(p.Inclusion),
                F(r.TariffInterest), F(Math.Abs(stance - r.TariffInterest)), F(p.TariffTerm), F(p.EthnicTerm), F(p.SuffrageFactor),
                F(pressure), F(p.Pressure), F(Rel(p.Pressure, pressure)), F(effBefore), F(holdShare), F(holdShare - pressure), F(r.Dominance), r.Dominance > 0 ? "0" : "1", F(km),
                SecP1Columns(w, c, r, rawNow, orderOld, mbar)));
        }
        st.SecRows = rows;
    }

    /// <summary>番人（テスト）が CheckSecession を直に踏んだ後の行（Quarter の前・SecFold が消す前）を読む口。</summary>
    internal static List<string>? SecRowsPending(World w) => S(w).SecRows;

    /// <summary>相対の差 (a−b)÷max(|b|, 1e-12)。両方 0 なら 0。</summary>
    static double Rel(double a, double b) => a == b ? 0.0 : (a - b) / Math.Max(Math.Abs(b), 1e-12);

    static void SecFold(Sink sk, ulong seed, World w, WState st, int q, bool picked, bool yearEnd)
    {
        var rows = st.SecRows;
        st.SecRows = null;
        if (rows is null || !(picked || yearEnd)) return;
        if (!(w.TotalPopulation > 0)) return;   // 死んだ国（人口 0）は行を出さない
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Sec ??= Open("secession", seed);
        if (!sk.SecHeader) { sk.Sec.WriteLine(SecessionHeaderText); sk.SecHeader = true; }
        string nation = NameOf(w), year = I(w.Year), qs = I(q);
        foreach (var row in rows)
        {
            // 行は region・tier から始まる＝quarter／year／nation を前に足して列の順を見出しに合わせる
            sk.Sec.WriteLine(qs + "\t" + year + "\t" + nation + "\t" + row);
        }
    }
}
