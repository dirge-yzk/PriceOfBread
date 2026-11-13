namespace PriceOfBread.SimCore;

// 力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。〔履歴 InstrumentsDiscontent.cs#1〕
public static partial class Instruments
{
    internal const string DcontHeaderText = "quarter\tyear\tnation\tmassPopK\tsatMeanShare\tdiscontentMeanShare\tmilitancyMeanShare\tmilVoiceMeanShare\tmilWarMeanShare\tmilNationMeanShare\tmilFamineMeanShare\t"
        + "chronicShortageMeanShare\tlivingDropMeanIndex\thighSatHighMilShare\tpartOldK\tpartNewK\tdGtMShare\tclusterCountOld\tclusterCountNew\tclusterAvgMilOldIndex\tclusterAvgMilNewIndex\t"
        + "factionsCount\tfactionsMutatedCount\tfactionsMaxCohesionIndex\tfactionRevoltsCum\t"
        + "partMilK\tclusterCountMil\tmilSumOldK\tmilSumMilK\tmilSumNewK\tfacCohGateCount\tfacCohMutCount\tfacStrongCount\tstrengthRatioMaxIndex\tpartDenK	strikeRegions	strikeIntensitySum";

    /// <summary>事前登録 R5 のリテラル（満足度がこれ以上かつ闘争度がこれを超える POP を〈水準が高いのに闘争度が高い〉と数える）。</summary>
    internal const double DcontHighSat = 0.6, DcontHighMil = 0.5;
    /// <summary>追記2 のリテラル：変異ロールの結束の門（Factions.MutationCohesionGate〔private〕と同じ 0.3）。</summary>
    internal const double DcontMutGate = 0.3;

    static void DcontFold(Sink sk, ulong seed, World w, int q, bool picked, bool yearEnd)
    {
        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;
        var c = w.Coef;
        double[] coord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        double[] gap = c.DiscontentLawGap > 0 ? PolicyPreferences.LawGap(w) : [];
        double mass = 0, sat = 0, dis = 0, mil = 0, mVoice = 0, mWar = 0, mNation = 0, mFamine = 0, chronic = 0, drop = 0, hiHi = 0;
        double partOld = 0, partNew = 0, partMil = 0, dGtM = 0, partDen = 0;
        var oldCl = new Dictionary<(int, int, int), (double Size, double MilSum)>();
        var newCl = new Dictionary<(int, int, int), (double Size, double MilSum)>();
        var milCl = new Dictionary<(int, int, int), (double Size, double MilSum)>();
        int strikeRegions = 0; double strikeIntensitySum = 0;   // 追記3：World.TechProductivity の罷業の導出と同じ式（読むだけ・地域の人口加重の闘争度が門を超えた地域の数と超過分の和）
        foreach (var r in w.Regions)
        {
            if (c.StrikeProductionHit > 0 && w.LaborUnion != LaborUnionLaw.Illegal)
            {
                double rmil = 0, rsz = 0; foreach (var mp in r.Pops) { rmil += mp.Size * mp.Militancy; rsz += mp.Size; }
                double si = Math.Max(0, (rsz > 1e-9 ? rmil / rsz : 0.0) - c.StrikeMilitancyThreshold);
                if (si > 0) { strikeRegions++; strikeIntensitySum += si; }
            }
            foreach (var p in r.Pops)
            {
                if (p.Size <= 1e-9) continue;
                double bgap = gap.Length > 0 ? gap[PolicyPreferences.BlocOf(p)] : 0;
                double d = Politics.Discontent(w, c, p, coord, bgap);
                if (Politics.IsMassPop(p))
                {
                    double s = p.Size;
                    mass += s; sat += s * p.Satisfaction; dis += s * d; mil += s * p.Militancy;
                    mVoice += s * p.MilVoice; mWar += s * p.MilWar; mNation += s * p.MilNation; mFamine += s * p.MilFamine;
                    chronic += s * (1.0 - p.Satisfaction);
                    drop += s * (p.LivingRef < 0 ? 0.0 : p.LivingRef - p.Satisfaction);
                    if (p.Satisfaction >= DcontHighSat && p.Militancy > DcontHighMil) hiHi += s;
                }
                // 参加の側（FactionSnapshot.Participation と同じ式を旧と新の両方で）
                double pOld = p.Militancy <= FactionSnapshot.MilitancyThreshold ? 0.0 : 1.0;
                double pNew = Math.Max(d, p.Militancy);
                double pMil = p.Militancy;   // 追記2：重みが闘争度だけ（不満度を読まない連続化）
                partDen += p.Size;
                if (d > p.Militancy) dGtM += p.Size;
                if (pOld > 0) partOld += p.Size * pOld;
                if (pNew > 0) partNew += p.Size * pNew;
                if (pMil > 0) partMil += p.Size * pMil;
                var (bloc, ideo) = FactionSnapshot.BlocIdeo(p);
                var key = (r.Id, bloc, ideo);
                if (pOld > 0) { oldCl.TryGetValue(key, out var cur); oldCl[key] = (cur.Size + p.Size * pOld, cur.MilSum + p.Militancy * p.Size * pOld); }
                if (pNew > 0) { newCl.TryGetValue(key, out var cur); newCl[key] = (cur.Size + p.Size * pNew, cur.MilSum + p.Militancy * p.Size * pNew); }
                if (pMil > 0) { milCl.TryGetValue(key, out var cur); milCl[key] = (cur.Size + p.Size * pMil, cur.MilSum + p.Militancy * p.Size * pMil); }
            }
        }
        double totalPop = w.TotalPopulation, floor = FactionSnapshot.MinSizeFraction * totalPop;
        static (int Count, double AvgMil, double MilSum) Summ(Dictionary<(int, int, int), (double Size, double MilSum)> cl, double floor)
        {
            int n = 0; double sz = 0, ms = 0;
            foreach (var v in cl.Values) if (v.Size >= floor) { n++; sz += v.Size; ms += v.MilSum; }
            return (n, sz > 0 ? ms / sz : 0.0, ms);
        }
        var (nOld, avgOld, msOld) = Summ(oldCl, floor);
        var (nNew, avgNew, msNew) = Summ(newCl, floor);
        var (nMil, _, msMil) = Summ(milCl, floor);
        int fMut = 0, fGate = 0, fMutGate = 0, fStrong = 0; double fMaxCoh = 0, ratioMax = 0;
        double coerce = PoliticalQuantities.EffectiveCoerce(w, c);
        foreach (var f in w.Factions)
        {
            if (f.Mutated) fMut++;
            if (f.Cohesion > fMaxCoh) fMaxCoh = f.Cohesion;
            if (f.Cohesion >= DcontMutGate) fMutGate++;
            if (f.Cohesion < c.FactionCohesionFightThreshold) continue;
            fGate++;
            if (!w.Regions.Contains(f.Region)) continue;   // CheckFactionRevolt と同じ除外（幽霊の封じ）
            double hold = Politics.LocalizedHold(w, c, f.Region, coerce) + Politics.LoyalistMilitia(w, c, f);
            double ratio = Politics.FactionStrength(w, c, f) / Math.Max(1e-9, hold);
            if (ratio > 1) fStrong++;
            if (ratio > ratioMax) ratioMax = ratio;
        }
        double m = mass > 0 ? mass : double.NaN;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Dcont ??= Open("dcont", seed);
        if (!sk.DcontHeader) { sk.Dcont.WriteLine(DcontHeaderText); sk.DcontHeader = true; }
        sk.Dcont.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(mass), F(sat / m), F(dis / m), F(mil / m), F(mVoice / m), F(mWar / m), F(mNation / m), F(mFamine / m),
            F(chronic / m), F(drop / m), F(hiHi / m), F(partOld), F(partNew), F(partDen > 0 ? dGtM / partDen : double.NaN), I(nOld), I(nNew), F(avgOld), F(avgNew),
            I(w.Factions.Count), I(fMut), F(fMaxCoh), I(w.FactionRevolts),
            F(partMil), I(nMil), F(msOld), F(msMil), F(msNew), I(fGate), I(fMutGate), I(fStrong), F(ratioMax), F(partDen), I(strikeRegions), F(strikeIntensitySum)));
    }
}
