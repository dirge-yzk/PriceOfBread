namespace PriceOfBread.SimCore;

// ---------------------------------------------------------------- (417) 不満度の読み手の表 dcont_s（読むだけ・恒等） ----------------------------------------------------------------
// 部分クラス＝Instruments.cs の本体に足さず 1 本のファイルで持つ（InstrumentsAdmin.cs の先例）。力学の値を 1 つも書かない・乱数を引かない・新しいシミュの状態を足さない。INS=1 のときだけ動く。
// 目的＝事前登録 measurements/dc_20261019_prereg.txt の記録の線 R4〜R8（闘争度の 5 本の内訳・水準が高いのに闘争度が高い割合・参加の述語の旧と新・慣れが吸う量・運動体）を読む。
// 行＝国×四半期（挙げた国は毎四半期・他の全ての国は年末）。人口が 0 の国は出さない。旗 ParticipationDiscontent に依らず出す（旗 0 の世界でも旧と新の両方の述語で数える）。
// 取り方＝Instruments.Quarter から世界の状態を直に読む（本体への hook は 0 行）。不満度は Politics.Discontent を今の盤面で呼び直す（Politics.Step が闘争度を更新した後の LivingRef を読む）。
// 列（単位は接尾辞：Share＝0〜1・Index＝無次元・K＝千人・Count＝件数。大衆＝Politics.IsMassPop の人口加重。参加の側は FactionSnapshot.ComputeClusters と同じ集合＝活きた州の規模＞0 の全 POP）：
//   massPopK／satMeanShare（大衆の満足度）／discontentMeanShare（大衆の不満度＝Politics.AvgMassDiscontent と同じ式）／militancyMeanShare（大衆の闘争度）／
//   milLivingMeanShare・milVoiceMeanShare・milWarMeanShare・milNationMeanShare・milFamineMeanShare（大衆の闘争度の目標への 5 本の寄与＝Pop.MilLiving ほか・Politics.Step が最後に書いた値）／
//   chronicShortageMeanShare（大衆の 1−満足度＝慢性の不足を含む）・livingDropMeanIndex（大衆の 慣れた水準−今の水準＝慣れの形が政治へ運ぶ落差。未捕捉の POP は 0）／
//   highSatHighMilShare（大衆のうち満足度≧0.6 かつ闘争度＞0.5 の人口割合・閾値は事前登録 R5 のリテラル）／
//   partOldK（旧の二値の頭数＝闘争度＞0.5 の POP の規模の和）・partNewK（連続の頭数＝Σ 規模×max(不満度,闘争度)）・dGtMShare（参加の側の全 POP のうち不満度＞闘争度の人口割合）／
//   clusterCountOld・clusterCountNew（足切り FactionSnapshot.MinSizeFraction を越えた運動体〔州×束×主軸〕の数）・clusterAvgMilOldIndex・clusterAvgMilNewIndex（足切りを越えた運動体の平均闘争度＝希釈の指標）／
//   factionsCount・factionsMutatedCount・factionsMaxCohesionIndex（永続した運動体 World.Factions）・factionRevoltsCum（World.FactionRevolts＝運動体の蜂起の発火の累計）。
//   ★追記2（反証役の点検の処置・読むだけ）：partMilK（Σ 規模×闘争度＝参加の重みが闘争度だけのときの頭数＝partNewK−partMilK が不満度が先行した分・partMilK−partOldK が二値→連続の分）／
//   clusterCountMil・milSumMilK（重みが闘争度だけのときの運動体の数と、足切りを越えた運動体の MilSum の和）・milSumOldK・milSumNewK（旧と新の同じ和＝FactionStrength の素）／
//   facCohGateCount（結束が蜂起の門 FactionCohesionFightThreshold 以上の運動体の数）・facCohMutCount（結束が 0.3 以上＝変異ロールの門 Factions.MutationCohesionGate のリテラル）・
//   facStrongCount（結束が門以上で FactionStrength が 保持力＋郷勇 を超える運動体の数＝点火の 2 本の門のうち旗が効く側）・strengthRatioMaxIndex（その運動体の FactionStrength÷(保持力＋郷勇) の最大。無ければ 0）。
//   保持力は今の盤面から Politics.EffectiveCoerce と Politics.LocalizedHold で再計算（CheckFactionRevolt と同じ式・今期の導出値）。
// ⚠限界（計器が自己申告する）：四半期の末の断面／不満度は今の盤面から再計算（FactionSnapshot の注の〈同一ティック内の時点差は仕様〉と同じ）／運動体の数は足切りの後／
//   旗 1 の世界では clusterCountNew・partNewK が実際の Factions.Step の入力と一致し、旗 0 の世界では〈旗を立てたらこうなる〉の反事実になる（同じ世界の同じ瞬間の 2 通りの数え方）。
public static partial class Instruments
{
    internal const string DcontHeaderText = "quarter\tyear\tnation\tmassPopK\tsatMeanShare\tdiscontentMeanShare\tmilitancyMeanShare\tmilLivingMeanShare\tmilVoiceMeanShare\tmilWarMeanShare\tmilNationMeanShare\tmilFamineMeanShare\t"
        + "chronicShortageMeanShare\tlivingDropMeanIndex\thighSatHighMilShare\tpartOldK\tpartNewK\tdGtMShare\tclusterCountOld\tclusterCountNew\tclusterAvgMilOldIndex\tclusterAvgMilNewIndex\t"
        + "factionsCount\tfactionsMutatedCount\tfactionsMaxCohesionIndex\tfactionRevoltsCum\t"
        + "partMilK\tclusterCountMil\tmilSumOldK\tmilSumMilK\tmilSumNewK\tfacCohGateCount\tfacCohMutCount\tfacStrongCount\tstrengthRatioMaxIndex";

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
        double mass = 0, sat = 0, dis = 0, mil = 0, mLiving = 0, mVoice = 0, mWar = 0, mNation = 0, mFamine = 0, chronic = 0, drop = 0, hiHi = 0;
        double partOld = 0, partNew = 0, partMil = 0, dGtM = 0, partDen = 0;
        var oldCl = new Dictionary<(int, int, int), (double Size, double MilSum)>();
        var newCl = new Dictionary<(int, int, int), (double Size, double MilSum)>();
        var milCl = new Dictionary<(int, int, int), (double Size, double MilSum)>();
        foreach (var r in w.Regions)
        {
            if (r.State != RegionState.Active) continue;
            foreach (var p in r.Pops)
            {
                if (p.Size <= 1e-9) continue;
                double bgap = gap.Length > 0 ? gap[PolicyPreferences.BlocOf(p)] : 0;
                double d = Politics.Discontent(w, c, p, coord, bgap);
                if (Politics.IsMassPop(p))
                {
                    double s = p.Size;
                    mass += s; sat += s * p.Satisfaction; dis += s * d; mil += s * p.Militancy;
                    mLiving += s * p.MilLiving; mVoice += s * p.MilVoice; mWar += s * p.MilWar; mNation += s * p.MilNation; mFamine += s * p.MilFamine;
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
            if (f.Region.State != RegionState.Active || !w.Regions.Contains(f.Region)) continue;   // CheckFactionRevolt と同じ除外（幽霊の封じ）
            double hold = Politics.LocalizedHold(w, c, f.Region, coerce) + Politics.LoyalistMilitia(w, c, f);
            double ratio = Politics.FactionStrength(w, c, f) / Math.Max(1e-9, hold);
            if (ratio > 1) fStrong++;
            if (ratio > ratioMax) ratioMax = ratio;
        }
        double m = mass > 0 ? mass : double.NaN;
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        sk.Dcont ??= Open("dcont", seed);
        if (!sk.DcontHeader) { sk.Dcont.WriteLine(DcontHeaderText); sk.DcontHeader = true; }
        sk.Dcont.WriteLine(string.Join('\t', I(q), I(w.Year), NameOf(w), F(mass), F(sat / m), F(dis / m), F(mil / m), F(mLiving / m), F(mVoice / m), F(mWar / m), F(mNation / m), F(mFamine / m),
            F(chronic / m), F(drop / m), F(hiHi / m), F(partOld), F(partNew), F(partDen > 0 ? dGtM / partDen : double.NaN), I(nOld), I(nNew), F(avgOld), F(avgNew),
            I(w.Factions.Count), I(fMut), F(fMaxCoh), I(w.FactionRevolts),
            F(partMil), I(nMil), F(msOld), F(msMil), F(msNew), I(fGate), I(fMutGate), I(fStrong), F(ratioMax)));
    }
}
