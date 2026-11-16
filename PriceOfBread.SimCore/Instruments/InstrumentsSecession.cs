namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsSecession.cs#1〕
public static partial class Instruments
{
    internal const string SecessionHeaderText = "quarter\tyear\tnation\tregion\ttier\t"
        + "legitimacyIndex\ttotalPopK\tcoerceIndex\tholdBaseIndex\tholdBaseRebuildGapIndex\tstanceIndex\tsharedDivIndex\trestiveCount\t"
        + "popK\tcrowdK\tcrowdFactorShare\tmilitancyMeanIndex\tcultureDistanceIndex\tcoreShareMeanShare\tconsciousnessMeanIndex\tsuffrageInclusionShare\t"
        + "tariffInterestIndex\ttariffGapIndex\ttariffTermIndex\tethnicTermIndex\tconsciousnessFactorShare\tsuffrageFactorShare\t"
        + "pressureIndex\tpressureRebuiltIndex\tpressureRebuildGapIndex\teffHoldBeforeShareIndex\tholdShareIndex\tmarginIndex\tdominanceShare\tgoFlag\tcapitalDistanceKm\t"
        + "orderRawNowIndex\torderOldIndex\tmilitancyNationMeanIndex\tmilitancyRegionOwnIndex\torderRegionNowIndex\treachOwnShare\t"
        + "ungovTargetOldShare\tungovTargetNewShare\truleUngovBeforeShare\truleForeignBeforeShare\t"
        + "inclusionPersonMeanShare\tpressureRuleOldIndex\tpressureRuleOnceIndex\tmilNationMeanIndex\tmilNationRebuiltOnceIndex";

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

    /// <summary>(1173) K1 の末尾 5 列。旗に依らず、旧の規則（包摂を闘争度と圧の 2 か所）と 1 回の規則（闘争度だけ）を同じ盤面で組む。
    /// エンジンの関数を呼ばない＝POP・法・係数を自分で読む（設計の材料 (1162) §5 の文から）。
    /// 包摂の人の版＝非兵士の大きさの重み（植民地格は植民地参政権：なし 0・自国民のみ 中核・全面 1／他は移民原住民参政権が付与なら 1・他は中核）・大きさ 0 なら 1。
    /// 旧の包摂＝付与なら 1・他は非兵士の中核の平均（大きさ 0 なら 1）。圧＝(関税の項＋民族の項)×意識の割引×（旧だけ 1−旧の包摂）×大衆の飽和。
    /// 闘争度の民族の項＝MinorityMilitancy×(1−中核_p)×(1−人の版の包摂)（民族が無い世界は 0）。milNationMeanIndex は本体の p.MilNation の同じ重みの平均。</summary>
    static string SecK1Columns(World w, Coefficients c, Region r, Region cap, double stance)
    {
        static double C01(double x) => !(x > 0) ? 0 : x > 1 ? 1 : x;
        double size = 0, coreSum = 0, inclSum = 0, milNat = 0, milNatOnceRaw = 0;
        double comMil = 0, comSize = 0, ncSum = 0, ncSize = 0;
        foreach (var p in r.Pops)
        {
            if (p.IsSoldier) continue;
            double s = p.Size, core = p.CoreEthnicShare.Value;
            double coreP = core >= 0 && core <= 1 ? core : 1.0;
            double inclP = r.Tier == RegionTier.Colonial
                ? (w.ColonialSuffrage == ColonialSuffrageLaw.Full ? 1.0 : w.ColonialSuffrage == ColonialSuffrageLaw.OwnNationals ? coreP : 0.0)
                : (w.MigrantNativeSuffrage == MigrantNativeSuffrageLaw.Granted ? 1.0 : coreP);
            size += s; coreSum += core * s; inclSum += inclP * s;
            milNat += p.MilNation * s;
            milNatOnceRaw += (1.0 - core) * s;
            if (!p.Closed) { comMil += p.Militancy * s; comSize += s; }
            if (p.NationalConsciousness >= 0) { ncSum += p.NationalConsciousness * s; ncSize += s; }
        }
        bool any = size > 1e-9;
        double inclPerson = any ? inclSum / size : 1.0;
        double coreMean = any ? coreSum / size : 1.0;
        double inclOld = w.MigrantNativeSuffrage == MigrantNativeSuffrageLaw.Granted ? 1.0 : coreMean;
        double milNationMean = any ? milNat / size : 0.0;
        double milNationOnce = any && w.EthnicEnabled ? c.MinorityMilitancy * (milNatOnceRaw / size) * (1.0 - C01(inclPerson)) : 0.0;
        // 圧の項（CheckSecession の文の順：関税の項→民族の項→意識の割引→包摂の割引〔旧だけ〕→大衆の飽和）
        double mil = comSize > 1e-9 ? comMil / comSize : 0.0;
        double cult = ((r.Culture.Length > 0 && cap.Culture.Length > 0 && r.Culture != cap.Culture) ? 0.5 : 0)
                    + ((r.Religion.Length > 0 && cap.Religion.Length > 0 && r.Religion != cap.Religion) ? 0.5 : 0);
        if (c.CultureDistanceFromEthnic > 0) cult += c.CultureDistanceFromEthnic * ((1.0 - coreMean) - cult);
        double press = Math.Abs(stance - r.TariffInterest) * mil * c.SecessionPressureScale * (1 + c.CultureSecessionWeight * cult);
        if (c.SecessionCultureAdditive > 0 && w.EthnicEnabled) press += c.SecessionCultureAdditive * c.MinorityMilitancy * cult * (1.0 - coreMean) * mil;
        if (c.SecessionConsciousnessWeight > 0)
        {
            double nc = ncSize > 1e-9 ? C01(ncSum / ncSize) : 0.0;
            press *= Math.Max(0, 1.0 - c.SecessionConsciousnessWeight * nc);
        }
        double crowdF = comSize > 0 ? comSize / (comSize + c.ColonyPromotionMinPop) : 0.0;
        double pressOld = press * (1.0 - C01(inclOld)) * crowdF;
        double pressOnce = press * crowdF;
        return string.Join('\t', F(inclPerson), F(pressOld), F(pressOnce), F(milNationMean), F(milNationOnce));
    }

    /// <summary>州の離脱圧力を組み直した結果（<see cref="SecRebuild"/>）。番人（テスト）が世界から直に呼べるよう値の組で返す。</summary>
    internal readonly record struct SecParts(double Pressure, double TariffTerm, double EthnicTerm, double ConsciousnessFactor, double SuffrageFactor, double CrowdFactor, double CrowdK,
        double CultureDistance, double Militancy, double CoreShare, double Consciousness, double Inclusion);

    /// <summary>Politics.CheckSecession の州ごとの圧力の式を、同じ式・同じ順で組み直す（読むだけ）。cap＝首都の州（Regions[0]）・stance＝国の関税の姿勢。
    /// 本体が圧力の因数を足す・順を変えたらここが追いつかない＝<c>pressureRebuildGapIndex</c> が 0 でなくなる。</summary>
    internal static SecParts SecRebuild(World w, Coefficients c, Region r, Region cap, double stance)
    {
        double cultDist = ((r.Culture.Length > 0 && cap.Culture.Length > 0 && r.Culture != cap.Culture) ? 0.5 : 0)
                        + ((r.Religion.Length > 0 && cap.Religion.Length > 0 && r.Religion != cap.Religion) ? 0.5 : 0);
        double core = Politics.AvgRegionCoreShare(r);
        if (c.CultureDistanceFromEthnic > 0)
            cultDist = PoliticalQuantities.Toward(cultDist, 1.0 - core, c.CultureDistanceFromEthnic);
        double mil = Politics.AvgRegionMilitancy(r);
        double tariffTerm = Math.Abs(stance - r.TariffInterest) * mil * c.SecessionPressureScale * (1 + c.CultureSecessionWeight * cultDist);
        double pressure = tariffTerm, ethnic = 0;
        if (c.SecessionCultureAdditive > 0 && w.EthnicEnabled)
        {
            ethnic = c.SecessionCultureAdditive * c.MinorityMilitancy * cultDist * (1.0 - core) * mil;
            pressure += ethnic;
        }
        double consF = 1.0, nc = Politics.AvgRegionConsciousness(r);
        if (c.SecessionConsciousnessWeight > 0)
        {
            consF = Math.Max(0, 1.0 - c.SecessionConsciousnessWeight * nc);
            pressure *= consF;
        }
        double incl = PoliticalQuantities.Clamp01(Politics.RegionAvgSuffrageInclusion(w, c, r));
        double suffF = c.SecessionInclusionOnce > 0 ? 1.0 : 1.0 - incl;   // (1173) 旗 1 は :893 の割引が無い
        pressure *= suffF;
        double crowd = Politics.RegionCrowd(r);
        double crowdF = crowd > 0 ? crowd / (crowd + c.ColonyPromotionMinPop) : 0.0;
        pressure *= crowdF;
        return new SecParts(pressure, tariffTerm, ethnic, consF, suffF, crowdF, crowd, cultDist, mil, core, nc, incl);
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
            var p = SecRebuild(w, c, r, cap, stance);
            double pop = 0; foreach (var pp in r.Pops) pop += pp.Size;
            double holdShare = effBefore / sharedDiv;
            double km = double.NaN;
            var rc = Places.Coord(r.Name);
            if (!(rc.Lat == 0 && rc.Lon == 0)) km = Geo.DistanceKm(w.Latitude, w.Longitude, rc.Lat, rc.Lon);
            rows.Add(string.Join('\t', r.Name, r.Tier.ToString(), head,
                F(pop), F(p.CrowdK), F(p.CrowdFactor), F(p.Militancy), F(p.CultureDistance), F(p.CoreShare), F(p.Consciousness), F(p.Inclusion),
                F(r.TariffInterest), F(Math.Abs(stance - r.TariffInterest)), F(p.TariffTerm), F(p.EthnicTerm), F(p.ConsciousnessFactor), F(p.SuffrageFactor),
                F(pressure), F(p.Pressure), F(Rel(p.Pressure, pressure)), F(effBefore), F(holdShare), F(holdShare - pressure), F(r.Dominance), r.Dominance > 0 ? "0" : "1", F(km),
                SecP1Columns(w, c, r, rawNow, orderOld, mbar), SecK1Columns(w, c, r, cap, stance)));
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
