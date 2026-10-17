using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(390) 工場の能力の内訳の表（<c>factory_s</c>）と思想の割合の表（<c>ideology_s</c>）の番人（読むだけの計器・恒等）。
/// ①能力の鎖：capacity ≥ desired ≥ filled ≥ 0・各割合が [0,1]・materialFill ≥ inputFill（素材だけの充足は役務込みの充足より緩い）・
///   投入の律速の 4 分類の desired の和＝desired・失った量の 3 分類の和＝desired−filled（別の列の組み合わせから書き直す）。
/// ②世界との突き合わせ：最後の四半期の行が、世界の Producer／Market を番人の側で数え直した値（能力・作りたい量・産出・配給率）に合う（写しと本体の取り違えはここで赤）。
/// ③思想：列の数が Ideology 列挙の数に合う・National の和が 1・素の平均の和が 1・最後の四半期の行が Ideologies.National と POP の数え直しに合う・
///   National と素の平均が違う国×四半期がある（意識と声の重みが効いている対照＝同じ列を 2 本出しているだけでないこと）。
/// ④軌道が計器で変わらないことは既存の全ビット一致の番人（InstrumentsTests）と成績表が見る（この計器は EconomyStep に hook を持たず四半期の末の読みだけ＝切っている間に走る命令が無い）。
/// </summary>
[Collection("Instruments")]
public class FactoryIdeologyInstrumentTests
{
    private static MultiNationSim Make() => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());

    private static (string[] Header, List<string[]> Rows) Table(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }
    private static double Col(string[] h, string[] r, string name)
    {
        int i = Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name);
        return r[i] switch { "nan" => double.NaN, "" => double.NaN, "inf" => double.PositiveInfinity, "-inf" => double.NegativeInfinity, var s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) };
    }
    private static void Near(double expect, double actual, double rel = 1e-8, string what = "") =>
        Assert.True(Math.Abs(expect - actual) <= rel * Math.Max(1.0, Math.Abs(expect)), $"{what} 期待 {expect}・実際 {actual}");

    private static (string dir, MultiNationSim sim) Run(int quarters)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_fi_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Make();
        // ★(393) 隔離：この番人が測るのは思想の割合の集計であって政治ではない。首都の蜂起（内戦）が立つと反乱国が増えて国の数が変わる＝拮抗の帯を 0 にして即時の政変に倒す。
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        Instruments.Phase(sim, 1789UL, "open");
        sim.StartWarmup();
        Instruments.Phase(sim, 1789UL, "warm");
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
        finally { Instruments.Close(sim); }
        return (dir, sim);
    }
    private static void Cleanup(string dir) { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }

    [Fact]
    public void 能力の鎖と投入の律速の分類が閉じ_最後の行は世界を数え直した値に合う()
    {
        var (dir, sim) = Run(24);
        try
        {
            var (h, rows) = Table(System.IO.Path.Combine(dir, "factory_s1789.tsv"));
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            Assert.Equal(4 + 33 + 27, h.Length);   // ★(395) 末尾に労働の財ごとの 27 列（既存の 33 列は動かさない）
            Assert.True(rows.Count >= 4 * 24 - 4, $"行が少ない {rows.Count}");
            int withCap = 0, chainOk = 0, anyLaborBound = 0, anyOtherBound = 0, anyNotBound = 0;
            double unmatched = 0, desiredAll = 0;
            foreach (var r in rows)
            {
                double cap = Col(h, r, "capacityRefValuePerMonth"), des = Col(h, r, "desiredRefValuePerMonth"), fil = Col(h, r, "filledRefValuePerMonth"), mfil = Col(h, r, "materialFilledRefValuePerMonth"), outp = Col(h, r, "outputRefValuePerMonth");
                Assert.True(cap >= 0 && des >= -1e-9 && fil >= -1e-9 && outp >= 0, $"負の量 {cap} {des} {fil} {outp}");
                Assert.True(des <= cap * (1 + 1e-9) + 1e-9, $"作りたい量 {des} が能力 {cap} を超えた（採算の率は [0,1]）");
                Assert.True(fil <= des * (1 + 1e-9) + 1e-9, $"充足後 {fil} が作りたい量 {des} を超えた");
                Assert.True(fil <= mfil * (1 + 1e-9) + 1e-9, $"役務込みの充足後 {fil} が素材だけの充足後 {mfil} を超えた（最小の配給率は投入を増やすと下がる一方）");
                Assert.Equal(Col(h, r, "factoryCount"), Col(h, r, "factoryBankruptCount") + Col(h, r, "factoryIdleCount") + Col(h, r, "factoryActiveCount"));
                if (cap > 0) { withCap++; foreach (var c in new[] { "priceFactorShare", "inputFillShare", "materialFillShare" }) { double v = Col(h, r, c); Assert.True(double.IsNaN(v) || (v >= -1e-9 && v <= 1 + 1e-9), $"{c} が [0,1] の外 {v}"); } }
                // 4 分類の desired の和＝desired／失った量の 3 分類の和＝desired−filled（別の列の組み合わせ）
                double dn = Col(h, r, "notInputBoundDesiredRefValuePerMonth"), dl = Col(h, r, "laborBoundDesiredRefValuePerMonth"), dO = Col(h, r, "otherBoundDesiredRefValuePerMonth"), du = Col(h, r, "unmatchedBoundDesiredRefValuePerMonth");
                Near(des, dn + dl + dO + du, 1e-9, "分類の和");
                double lossSum = Col(h, r, "laborBoundLossRefValuePerMonth") + Col(h, r, "otherBoundLossRefValuePerMonth") + Col(h, r, "unmatchedBoundLossRefValuePerMonth");
                Assert.True(Math.Abs((des - fil) - lossSum) <= 1e-8 * Math.Max(1.0, des), $"失った量の和 {lossSum} が desired−filled {des - fil} に合わない");
                chainOk++;
                if (dl > 0) anyLaborBound++; if (dO > 0) anyOtherBound++; if (dn > 0) anyNotBound++;
                unmatched += du; desiredAll += des;
            }
            Assert.True(withCap > 0 && chainOk == rows.Count, "能力が正の行が無い＝計器が空");
            Assert.True(anyNotBound > 0, "投入に縛られない工場が 1 行も無い（対照）");
            // 計器の限界の申告：どの投入の配給率とも一致しない月は少数（本体の ration と Market.LastRation の記帳が同じ月に揃っている前提）
            Assert.True(unmatched <= 0.05 * desiredAll, $"分類できない量が desired の 5% を超えた {unmatched / desiredAll:P2}（LastRation の記帳が LastInputFill とずれている）");

            // 世界を番人の側で数え直す：最後の四半期（行を書いた時点の世界）
            var byNation = rows.Where(r => Col(h, r, "quarter") == 23).ToDictionary(r => r[Array.IndexOf(h, "nation")]);
            Assert.Equal(4, byNation.Count);
            foreach (var n in sim.Nations)
            {
                var w = n.World; string nm = w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "(単国)";
                var r = byNation[nm];
                double cap = 0, des = 0, outp = 0, fil = 0, mfil = 0, built = 0, labDem = 0, labRationW = 0, labSupply = 0;
                double dLab = 0, dOth = 0, lossLab = 0, lossOth = 0, popKsum = 0, ind = 0, needDes = 0;
                var popSeen = new HashSet<(Region, int)>();
                int nBank = 0;
                foreach (var rg in w.Regions)
                {
                    if (rg.State != RegionState.Active) continue;
                    var lm = rg.Markets[Catalog.LaborId]; labDem += lm.LastDemand; labRationW += lm.LastDemand * lm.LastRation.Value;
                    foreach (var s in rg.Producers)
                    {
                        if (s.GoodId == Catalog.LaborId && !s.Bankrupt) labSupply += s.Capacity;
                        if (!s.BuysLabor) continue;
                        if (s.Bankrupt) { nBank++; continue; }
                        if (s.Scale == 0 && s.Output == 0) continue;
                        double rp = Catalog.RefPrices[s.GoodId];
                        cap += s.Capacity * rp; des += s.DesiredOutput * rp; outp += s.Output * rp; fil += s.DesiredOutput * s.LastInputFill * rp; mfil += s.DesiredOutput * s.LastMaterialFill * rp; built += s.BuiltScale;
                        ind += s.Capacity * rp * rg.ProductiveIndustrialization;
                        if (popSeen.Add((rg, s.LaborPopIndex))) popKsum += rg.Pops[s.LaborPopIndex].Size;
                        // 投入の律速の分類を番人の側で書き直す：労働財の投入の配給率の最小が LastInputFill に一致すれば労働・そうでなく全投入の最小が一致すればその他
                        var labIn = s.Inputs.Where(x => x.GoodId == Catalog.LaborId || x.GoodId == Catalog.AdvLaborId || x.GoodId == Catalog.FarmLaborId).ToList();
                        needDes += s.DesiredOutput * labIn.Sum(x => x.QtyPerOutput);
                        if (s.LastInputFill < 1.0 - 1e-9)
                        {
                            double labMin = labIn.Count > 0 ? labIn.Min(x => rg.Markets[x.GoodId].LastRation.Value) : double.PositiveInfinity;
                            double allMin = s.Inputs.Count > 0 ? s.Inputs.Min(x => rg.Markets[x.GoodId].LastRation.Value) : double.PositiveInfinity;
                            double loss = s.DesiredOutput * (1.0 - s.LastInputFill) * rp;
                            if (labIn.Count > 0 && Math.Abs(labMin - s.LastInputFill) <= 1e-12) { dLab += s.DesiredOutput * rp; lossLab += loss; }
                            else if (Math.Abs(allMin - s.LastInputFill) <= 1e-12) { dOth += s.DesiredOutput * rp; lossOth += loss; }
                        }
                    }
                }
                Near(cap, Col(h, r, "capacityRefValuePerMonth"), 1e-8, nm + " 能力");
                Near(des, Col(h, r, "desiredRefValuePerMonth"), 1e-8, nm + " 作りたい量");
                Near(fil, Col(h, r, "filledRefValuePerMonth"), 1e-8, nm + " 充足後");
                Near(mfil, Col(h, r, "materialFilledRefValuePerMonth"), 1e-8, nm + " 素材の充足後");
                Near(outp, Col(h, r, "outputRefValuePerMonth"), 1e-8, nm + " 産出");
                Near(built, Col(h, r, "builtScaleSum"), 1e-8, nm + " 建てた設備");
                Near(labSupply, Col(h, r, "laborSupplyCapacityUnitsPerMonth"), 1e-8, nm + " 労働の供給の器");
                Near(labDem, Col(h, r, "laborMarketDemandUnitsPerMonth"), 1e-8, nm + " 労働の需要");
                if (labDem > 0) Near(labRationW / labDem, Col(h, r, "laborRationMeanShare"), 1e-8, nm + " 労働の配給率");
                Assert.Equal(nBank, (int)Col(h, r, "factoryBankruptCount"));
                Near(dLab, Col(h, r, "laborBoundDesiredRefValuePerMonth"), 1e-8, nm + " 労働に縛られた作りたい量");
                Near(dOth, Col(h, r, "otherBoundDesiredRefValuePerMonth"), 1e-8, nm + " 他の投入に縛られた作りたい量");
                Near(lossLab, Col(h, r, "laborBoundLossRefValuePerMonth"), 1e-8, nm + " 労働で失った量");
                Near(lossOth, Col(h, r, "otherBoundLossRefValuePerMonth"), 1e-8, nm + " 他の投入で失った量");
                Near(popKsum, Col(h, r, "factoryLaborPopK"), 1e-8, nm + " 担い手 POP の重複を除いた人数");
                Near(needDes, Col(h, r, "laborNeedAtDesiredUnitsPerMonth"), 1e-8, nm + " 作りたい量に要る労働");
                if (cap > 0) Near(ind / cap, Col(h, r, "industrializationMean"), 1e-8, nm + " 能力加重の工業化度");
            }
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void 思想の割合は列の数が列挙に合い_集計の和が1で_最後の行は数え直した値に合い_意識の重みが効いている()
    {
        Assert.Equal(Ideologies.Count, Enum.GetNames<Ideology>().Length);
        var (dir, sim) = Run(20);
        try
        {
            var (h, rows) = Table(System.IO.Path.Combine(dir, "ideology_s1789.tsv"));
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            Assert.Equal(6 + 2 * Ideologies.Count, h.Length);
            var names = Enum.GetNames<Ideology>();
            for (int k = 0; k < names.Length; k++)
            {
                Assert.Equal("ideoNational" + names[k] + "Share", h[6 + k]);
                Assert.Equal("ideoPopMean" + names[k] + "Share", h[6 + names.Length + k]);
            }
            int differ = 0;
            foreach (var r in rows)
            {
                double sn = 0, sm = 0, maxDiff = 0;
                for (int k = 0; k < names.Length; k++)
                {
                    double a = Col(h, r, "ideoNational" + names[k] + "Share"), b = Col(h, r, "ideoPopMean" + names[k] + "Share");
                    Assert.True(a >= -1e-12 && b >= -1e-12, "負の割合");
                    sn += a; sm += b; maxDiff = Math.Max(maxDiff, Math.Abs(a - b));
                }
                Assert.True(Math.Abs(sn - 1) <= 1e-6 || (sn == 0 && Col(h, r, "nationalWeightSumIndex") <= 1e-9), $"国民の集計の和 {sn}");
                Assert.True(Math.Abs(sm - 1) <= 1e-6, $"素の平均の和 {sm}");
                if (maxDiff > 1e-4) differ++;
            }
            Assert.True(differ > rows.Count / 2, $"National と素の平均がほぼ同じ行が多い {differ}/{rows.Count}＝意識と声の重みが効いていない（同じ列を 2 本出しただけ？）");
            // 最後の四半期：Ideologies.National と POP の数え直し（番人の側で別に書く）
            var last = rows.Where(r => Col(h, r, "quarter") == 19).ToDictionary(r => r[Array.IndexOf(h, "nation")]);
            Assert.Equal(4, last.Count);
            foreach (var n in sim.Nations)
            {
                var w = n.World; string nm = w.SelfPower is { } sp ? (sp.BaseName.Length > 0 ? sp.BaseName : sp.Name) : "(単国)";
                var r = last[nm];
                var nat = Ideologies.National(w);
                var mean = new double[Ideologies.Count]; double tot = 0;
                foreach (var rg in w.Regions) foreach (var p in rg.Pops) { tot += p.Size; for (int k = 0; k < mean.Length; k++) mean[k] += p.IdeologyShares[k] * p.Size; }
                double wsum = 0;
                foreach (var rg in w.Regions) foreach (var p in rg.Pops)
                    wsum += p.Size * p.Consciousness * (w.Coef.IdeologyVoiceFloor + (1.0 - w.Coef.IdeologyVoiceFloor) * InterestGroups.FranchiseVoice(p, w.Suffrage, w.Coef));
                Near(wsum, Col(h, r, "nationalWeightSumIndex"), 1e-8, nm + " 重みの和");
                Near(tot, Col(h, r, "popK"), 1e-8, nm + " 人口");
                for (int k = 0; k < names.Length; k++)
                {
                    Near(nat[k], Col(h, r, "ideoNational" + names[k] + "Share"), 1e-8, nm + " National " + names[k]);
                    Near(mean[k] / tot, Col(h, r, "ideoPopMean" + names[k] + "Share"), 1e-8, nm + " 素の平均 " + names[k]);
                }
            }
        }
        finally { Cleanup(dir); }
    }
}
