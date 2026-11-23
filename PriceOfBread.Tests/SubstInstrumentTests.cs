using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(837) 線 c67ss：自給の段 1 の表 <c>subst_s</c>（読むだけ）の番人＝計器の不変条件。事前登録 measurements/m837_20261104_prereg_ss.txt の S1〜S4。
/// 期待は計器と別の出所から作る：D1・D2 は手で書いた表（S1）・農業労働の能力と売れた量は labor_s（エンジンの IsHumanLaborSupply を通る別の計器・S2）・
/// Q2 の材料は試験が POP から自前の式で数えた値（S4）。開幕の比（S3）の D1 の帯は紙の見積もり（0.884×0.89÷(0.7×1.10)＝1.02）から腕の前に置いた（D2 の帯 0.06 は助走を通した ident の生で読む＝下の注）。
/// 突然変異（使い捨ての木・measurements/m837_20261104_mutation.txt）：Mu1 能力を産出で読む・Mu4 較正の定数を 1.1 に戻す・Mu5 D1 の clamp を外す。</summary>
[Collection("Instruments")]
public class SubstInstrumentTests
{
    private static MultiNationSim Two(ulong seed)
        => new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: seed) { DiplomacyFrozen = true };

    internal static List<Dictionary<string, string>> Rows(string path, string? header)
    {
        var lines = System.IO.File.ReadAllLines(path);
        if (header is not null) Assert.Equal(header, lines[0]);
        var h = lines[0].Split('\t');
        var res = new List<Dictionary<string, string>>();
        foreach (var l in lines.Skip(1))
        {
            var c = l.Split('\t');
            Assert.Equal(h.Length, c.Length);
            var d = new Dictionary<string, string>();
            for (int i = 0; i < h.Length; i++) d[h[i]] = c[i];
            res.Add(d);
        }
        return res;
    }
    internal static double D(Dictionary<string, string> row, string col)
        => row[col] == "nan" ? double.NaN : double.Parse(row[col], System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>英仏を n 四半期回し、出力のフォルダを返す（計器は閉じる）。</summary>
    internal static (string Dir, MultiNationSim Sim) Run(ulong seed, int quarters, Action<MultiNationSim, int>? after = null, Action<MultiNationSim, int>? before = null)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_subst_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        var sim = Two(seed);
        try
        {
            for (int q = 0; q < quarters; q++) { sim.StepQuarter(); before?.Invoke(sim, q); Instruments.Quarter(sim, seed, q); after?.Invoke(sim, q); }
        }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        return (dir, sim);
    }

    /// <summary>S1：D1・D2 の純関数を手の表と突き合わせる（能力の基準と産出の基準で値が違う表・clamp・産出 0 の枝）。</summary>
    [Fact]
    public void D1とD2は手の表のとおり()
    {
        Assert.Equal(0.7, Instruments.SubstD1(3, 10), 12);          // 能力 10 のうち 3 しか売れない＝売れ残り 0.7
        Assert.Equal(0.0, Instruments.SubstD1(12, 10), 12);         // 売れた量が能力を超えても負の売れ残りにしない（clamp）
        Assert.True(double.IsNaN(Instruments.SubstD1(5, 0)));       // 能力が 0＝nan
        Assert.Equal(1.0 - 3.0 / 3.3, Instruments.SubstD2(3, 3.3), 12);   // 産出の基準＝同じ売れた量でも売れ残りが小さい
        Assert.Equal(0.0, Instruments.SubstD2(5, 0), 12);           // 産出 0＝今の失業の式の Eps の枝と同じ 0
        Assert.Equal(0.0, Instruments.SubstD2(12, 10), 12);
        Assert.True(Instruments.SubstD1(3, 10) > Instruments.SubstD2(3, 3.3) + 0.5, "能力の基準と産出の基準が同じ値になっている");
        Assert.Equal(0.7 * 0.89 * 20000, Instruments.SubstDerived(0.7, 0.89, 20000), 9);
    }

    /// <summary>S2：英仏の 3 四半期。行の D1 が行の能力と売れた量から組んだ値に合い、国ごとの能力と売れた量の和が labor_s に合う（labor_s はエンジンの IsHumanLaborSupply を通る別の計器）。</summary>
    [Fact]
    public void 売れ残りは能力の基準で数え_国の和はlabor_sに合う()
    {
        var (dir, sim) = Run(1789UL, 3);
        var rows = Rows(System.IO.Path.Combine(dir, "subst_s1789.tsv"), Instruments.SubstHeaderText);
        Assert.NotEmpty(rows);
        int withCap = 0;
        foreach (var r in rows)
        {
            double cap = D(r, "farmCapPerMonth"), sold = D(r, "farmSoldPerMonth"), prod = D(r, "farmProducedPerMonth");
            double d1 = D(r, "unsoldD1Share"), d2 = D(r, "unsoldD2Share");
            if (cap > 0)
            {
                withCap++;
                double want = Math.Clamp(1.0 - sold / cap, 0.0, 1.0);
                Assert.True(Math.Abs(d1 - want) <= 1e-6, $"D1 が能力の基準でない {r["nation"]} {r["region"]} q{r["quarter"]}：{d1} ≠ {want}");
            }
            else Assert.True(double.IsNaN(d1));
            double want2 = prod > 1e-9 ? Math.Clamp(1.0 - sold / prod, 0.0, 1.0) : 0.0;
            Assert.True(Math.Abs(d2 - want2) <= 1e-6, $"D2 が産出の基準でない {r["nation"]} {r["region"]} q{r["quarter"]}：{d2} ≠ {want2}");
        }
        Assert.True(withCap > rows.Count / 2, $"能力の正の行が少ない {withCap}/{rows.Count}");
        // 国ごと・四半期ごとの和＝labor_s（列 farmCapPerMonth・farmSoldPerMonth）
        var lab = Rows(System.IO.Path.Combine(dir, "labor_s1789.tsv"), null);
        int matched = 0;
        foreach (var l in lab)
        {
            double capL = D(l, "farmCapPerMonth"), soldL = D(l, "farmSoldPerMonth");
            var mine = rows.Where(r => r["nation"] == l["nation"] && r["quarter"] == l["quarter"]).ToList();
            double capS = mine.Sum(r => D(r, "farmCapPerMonth")), soldS = mine.Sum(r => D(r, "farmSoldPerMonth"));
            Assert.True(Math.Abs(capS - capL) <= 1e-6 * Math.Max(1, Math.Abs(capL)), $"能力の和が labor_s と違う {l["nation"]} q{l["quarter"]}：{capS} ≠ {capL}");
            Assert.True(Math.Abs(soldS - soldL) <= 1e-6 * Math.Max(1, Math.Abs(soldL)), $"売れた量の和が labor_s と違う {l["nation"]} q{l["quarter"]}：{soldS} ≠ {soldL}");
            if (capL > 0) matched++;
        }
        Assert.True(matched >= 3, $"labor_s と突き合わせた行が少ない {matched}");
    }

    /// <summary>S3：開幕の比。自給率 0.7 の州で Σ導いた自給（D1）÷Σ名目の自給が帯の中、D2 の比は小さい（D2 の基準では開幕の自給が再現しない）。</summary>
    [Fact]
    public void 開幕の比はD1で1に近くD2では遠い()
    {
        var (dir, _) = Run(1789UL, 1);
        var rows = Rows(System.IO.Path.Combine(dir, "subst_s1789.tsv"), Instruments.SubstHeaderText);
        double d1 = 0, d2 = 0, nom = 0; int n = 0;
        foreach (var r in rows)
        {
            if (Math.Abs(D(r, "subRateWeighted") - 0.7) > 0.005 || double.IsNaN(D(r, "derivedD1PerMonth"))) continue;
            d1 += D(r, "derivedD1PerMonth"); d2 += D(r, "derivedD2PerMonth"); nom += D(r, "nominalSubPerMonth"); n++;
        }
        Assert.True(n >= 5 && nom > 0, $"自給率 0.7 の州の行が少ない {n}");
        Assert.InRange(d1 / nom, 0.90, 1.15);
        // 結果の後の変更 1（事前登録の追記）：D2 の帯 0.30 未満は助走を通した盤面の見積もりで、助走なしの試験の盤面（産出が落ち着いていない）では 0.87 だった＝試験では向きだけ（D2 は D1 より小さい）を見て、帯は ident の生の読み (a) に回した。
        Assert.True(d2 < d1, $"D2 の導いた自給が D1 以上 {d2} ≥ {d1}");
    }

    /// <summary>S4：Q2 の材料（人頭税の請求・所得税の上界・地代の受け手別の和・必要量の値）が、試験が POP から自前の式で数えた値に合う。</summary>
    [Fact]
    public void Q2の材料は試験がPOPから数えた値に合う()
    {
        // 試験が盤面を置く（Quarter を呼ぶ前）：人頭税の段を正にし、閉鎖階級と兵士の POP にも Subsistence を置く＝人頭税の対象の述語（平民だけ）が効く盤面にする（突然変異 Mu8 の穴を塞ぐ）
        var (dir, sim) = Run(1789UL, 2, before: (s, q) =>
        {
            if (q != 1) return;
            foreach (var n0 in s.Nations)
            {
                n0.World.PollTax = 0.5;
                foreach (var reg0 in n0.World.Regions) foreach (var p0 in reg0.Pops) if ((p0.Closed || p0.IsSoldier) && p0.SubsistenceBasis <= 0) p0.SubsistenceBasis = 0.5;
            }
        });   // 最後の四半期の世界の状態は Quarter を呼んだ直後と同じ（計器は何も書き換えない）
        var rows = Rows(System.IO.Path.Combine(dir, "subst_s1789.tsv"), Instruments.SubstHeaderText).Where(r => r["quarter"] == "1").ToList();
        int checkedRows = 0; double pollAll = 0, incAll = 0, othersAll = 0, valueAll = 0, closedSubPop = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World; string nat = Instruments.NameOf(w);
            foreach (var reg in w.Regions)
            {
                var row = rows.FirstOrDefault(r => r["nation"] == nat && r["region"] == reg.Name);
                if (row is null) continue;
                double poll = 0, inc = 0, others = 0, farmers = 0, subPop = 0;
                foreach (var p in reg.Pops)
                {
                    double rent = Math.Max(0, p.LandRentPerMonth.Value);
                    if (p.SubsistenceBasis > 0)
                    {
                        subPop += p.Size; farmers += rent;
                        if (!p.IsCommoner) closedSubPop += p.Size;
                        if (w.PollTax > 1e-9 && p.IsCommoner && !p.IsCriminal) poll += w.Coef.PollTaxRate * w.PollTax * p.Size;
                        inc += w.TaxRate.Value * Math.Max(0, p.LastEarnedPerMonth.Value);
                    }
                    else others += rent;
                }
                Assert.True(Math.Abs(D(row, "pollClaimPerMonth") - poll) <= 1e-6 * Math.Max(1, poll), $"人頭税の請求が違う {nat} {reg.Name}");
                Assert.True(Math.Abs(D(row, "incomeTaxUpperPerMonth") - inc) <= 1e-6 * Math.Max(1, inc), $"所得税の上界が違う {nat} {reg.Name}");
                Assert.True(Math.Abs(D(row, "rentToOthersPerMonth") - others) <= 1e-6 * Math.Max(1, others), $"他の層の地代が違う {nat} {reg.Name}");
                Assert.True(Math.Abs(D(row, "rentToFarmersPerMonth") - farmers) <= 1e-6 * Math.Max(1, farmers), $"担い手の地代が違う {nat} {reg.Name}");
                Assert.True(Math.Abs(D(row, "subPopK") - subPop) <= 1e-6 * Math.Max(1, subPop), $"担い手の人数が違う {nat} {reg.Name}");
                // 必要量の値＝担い手の人数×主食の名目の必要量の和×必要量で重み付けた主食の値段（試験が Catalog と市場から自前で組む）
                double need = 0, pw = 0, pn = 0;
                for (int g = 0; g < Math.Min(reg.Markets.Length, Catalog.Goods.Length); g++)
                    if (Catalog.Goods[g].Tier == GoodTier.Staple) { need += Catalog.Goods[g].BaseNeed; pw += Catalog.Goods[g].BaseNeed * reg.Markets[g].Price; pn += Catalog.Goods[g].BaseNeed; }
                double value = subPop * need * (pn > 0 ? pw / pn : 0);
                Assert.True(Math.Abs(D(row, "stapleNeedValuePerMonth") - value) <= 1e-6 * Math.Max(1, value), $"主食の必要量の値が違う {nat} {reg.Name}");
                Assert.True(Math.Abs(D(row, "stapleNeedPerCapita") - need) <= 1e-9, "主食の名目の必要量が違う");
                pollAll += poll; incAll += inc; othersAll += others; valueAll += value; checkedRows++;
            }
        }
        Assert.True(checkedRows >= 10, $"突き合わせた行が少ない {checkedRows}");
        Assert.True(valueAll > 0 && (pollAll > 0 || incAll > 0 || othersAll > 0), "材料が全て 0＝何もしない計器で緑にならない");
        Assert.True(pollAll > 0, "人頭税の請求が全て 0＝述語（平民だけ）の試験にならない");
        Assert.True(closedSubPop > 0, "Subsistence を置いた閉鎖階級・兵士が無い＝述語の試験にならない");
    }

    /// <summary>★(849) 自給の段 2 の末尾の列を、旗 0 と旗 1 の両方で読む（助走 1 四半期を通す）。旗 1：計器が自分で焼いた ρ・自分で数えた食べる割合と失業 (b′) がエンジンと 1e-9 で一致（G2・G3a・G4 の型）。
    /// 旗 0：エンジンの印は OpeningShare のまま・食べる割合は欄そのもの・計器の反実仮想の食べる割合はエンジンと違う（門が旗 0 で落ちる）。</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void 自給の段2の列は旗0と旗1で計器とエンジンを並べる(double flag)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_subst2_" + Guid.NewGuid().ToString("N"));
        var sim = Two(1791UL);
        foreach (var n in sim.Nations) { n.World.Coef.SubsistenceStage2 = flag; }
        sim.StartWarmup(quartersOverride: 1);
        Instruments.Configure(dir, "*");
        try { for (int q = 0; q < 2; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1791UL, q); } }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var rows = Rows(System.IO.Path.Combine(dir, "subst_s1791.tsv"), Instruments.SubstHeaderText).Where(r => r["quarter"] == "1" && r["s2RhoSource"] == "1").ToList();
        Assert.True(rows.Count >= 20, $"計器が焼いた州が少ない {rows.Count}");
        int sigDiff = 0, eNeg = 0;
        foreach (var r in rows)
        {
            double se = D(r, "s2SigmaEngine"), si = D(r, "s2SigmaInst"), ub = D(r, "s2UnempB"), ue = D(r, "s2UnempEngine"), e = D(r, "s2EEnd");
            if (flag > 0)
            {
                Assert.Equal("2", r["s2BasisKind"]);
                Assert.True(Math.Abs(se - si) <= 1e-9, $"食べる割合が計器と違う {se} vs {si}");
                Assert.True(D(r, "s2BakeRhoRelDiff") <= 1e-9 && D(r, "s2BakeSigmaDiff") <= 1e-9, "焼いた月の ρ か食べる割合が計器と違う");
                if (e > 0) Assert.True(Math.Abs(ue - ub) <= 1e-9, $"失業が (b′) と違う {ue} vs {ub}");
                else { eNeg++; Assert.True(ue <= 1e-12, $"必要量に届かない売れ残りを失業と数えた {ue}"); }
            }
            else
            {
                Assert.Equal("0", r["s2BasisKind"]);
                Assert.Equal(D(r, "s2BasisEngine"), se);
                if (Math.Abs(se - si) > 1e-6) sigDiff++;
            }
        }
        if (flag > 0) Assert.True(eNeg >= 10, $"下側の州が少ない {eNeg}");
        else Assert.True(sigDiff >= rows.Count / 2, $"旗 0 で計器の反実仮想とエンジンの食べる割合が同じ＝門が旗 0 で落ちない {sigDiff}/{rows.Count}");
    }

    // ---- (1317) 段 3-0 の器：s3 の列。期待値は試験の中で手の式から作る（計器の関数の式を写さない）。事前登録 measurements/m1317_20261123_prereg_s3.txt の 1〜7。
    private static readonly string[] S3Names = ["s3SrawInst", "s3HarvestInst", "s3QInst", "s3DInst", "s3EInst", "s3EWheatInst", "s3ESoldShareInst", "s3EValueInst", "s3KappaInst", "s3KappaEInst",
        "s3UnempNewInst", "s3EatPerCapInst", "s3LandTaxPeasantInst"];

    [Fact]
    public void s3の見出しは13本で末尾に並ぶ()
    {
        var h = Instruments.SubstHeaderText.Split('\t');
        Assert.Equal(13, S3Names.Length);
        // (1325) 3-1 の器で s3 の後ろに 6 列（SubstS31HeaderText）を足した＝s3 の 13 本はその前（並びは変えない）
        var s31 = Instruments.SubstS31HeaderText.Split('	');
        Assert.Equal(["s3ESoldInst", "s3EOfferedEngine", "s3ESoldEngine", "s3RPaidEngine", "s3QEngine", "s3CrossInst"], s31);
        Assert.Equal(s31, h.Skip(h.Length - 6).ToArray());
        Assert.Equal(S3Names, h.Skip(h.Length - 19).Take(13).ToArray());
        Assert.Equal(h.Length, h.Distinct().Count());
        Assert.Equal("s2NewOverOld", h[h.Length - 20]);   // 既存の列の並びは変えない（s3 は s2 の後ろ）
    }

    [Fact]
    public void s3は手で置いた盤面で式どおり()
    {
        // 2 財：g1（必要量 0.25・f=1・売る財へ換算 0.5・売れ行き 0.4・値段 3）・g2（必要量 0.125・f=0.5）。人数 8＝必要量×人数 3
        var st = new[] { new Instruments.S3Staple(0.25, 1.0, 0.4, 3.0, 0.5, true), new Instruments.S3Staple(0.125, 0.5, 0.9, 7.0, 1.0, false) };
        var r = Instruments.SubstS3Calc(rho: 1.0, u: 3.0, t: 1.5, cap: 10.0, size: 8.0, st);
        double sraw = 1.0 * 3.0 * 1.5 / 3.0;                 // 1.5
        double fbar = (0.25 * 1.0 + 0.125 * 0.5) / 0.375;    // 必要量加重の f
        double q = 2.0 * 1.5 * 1.0 + 1.0 * 1.5 * 0.5;        // 3.75（g1 は x=1.5・g2 は x=0.75）
        double d = 2.0 * 1.0 + 1.0 * 0.75;                   // 2.75
        double e = 2.0 * 0.5;                                // g1 だけ余る 1.0
        double lneed = 3.0 / (1.0 * 1.5 * fbar);             // 2.4（f を入れる）
        Assert.Equal(sraw, r.Sraw, 12); Assert.Equal(fbar, r.Harvest, 12);
        Assert.Equal(q, r.Q, 12); Assert.Equal(d, r.D, 12); Assert.Equal(e, r.E, 12);
        Assert.Equal(0.5, r.EWheat, 12);                     // 余り 1.0×換算 0.5
        Assert.Equal(0.4, r.ESoldShare, 12);                 // 余りは g1 だけ＝売れ行き 0.4
        Assert.Equal(0.5 * 3.0, r.EValue, 12);
        Assert.Equal(1.0 - 2.4 / 10.0, r.Kappa, 12);
        Assert.Equal(e / q, r.KappaE, 12);
        Assert.Equal((3.0 - lneed) / 10.0 * (1.0 - 0.4), r.UnempNew, 12);   // 0.036（Lneed に f を入れないと 0.06）
        Assert.Equal(d / 8.0, r.EatPerCap, 12);
        Assert.True(Math.Abs(r.UnempNew - 0.06) > 1e-3);     // Lneed に f を入れない値 0.06 とは別
        // ρ が nan（焼かれていない州）なら全て nan
        var n = Instruments.SubstS3Calc(double.NaN, 3.0, 1.5, 10.0, 8.0, st);
        Assert.True(double.IsNaN(n.Sraw) && double.IsNaN(n.Q) && double.IsNaN(n.UnempNew) && double.IsNaN(n.Kappa));
    }

    [Fact]
    public void s3はσrawが1未満なら余りが厳密に0()
    {
        var st = new[] { new Instruments.S3Staple(0.25, 1.0, 0.4, 3.0, 0.5, true), new Instruments.S3Staple(0.125, 1.0, 0.9, 7.0, 1.0, false) };
        var r = Instruments.SubstS3Calc(1.0, 1.5, 1.5, 10.0, 8.0, st);   // σraw＝0.75
        Assert.Equal(0.75, r.Sraw, 12);
        Assert.Equal(0.0, r.E); Assert.Equal(0.0, r.EWheat); Assert.Equal(0.0, r.EValue);
        Assert.Equal(0.0, r.ESoldShare);                     // ΣE＝0 の時の s_E は 0
        Assert.Equal(0.0, r.KappaE);
        Assert.Equal(r.Q, r.D, 12);
        Assert.Equal(0.75 * 3.0, r.D, 12);                   // 必要量×人数 3 の 0.75
    }

    [Fact]
    public void s3は凶作で食べる量がσrawかけるfで決まる()
    {
        // 凶作：σraw＝1.2・f＝0.7＝今の sub は GoodNeed×min(1,1.2)×0.7＝1.4 だが、新しい D は GoodNeed×min(1,0.84)＝0.84×GoodNeed
        var bad = new[] { new Instruments.S3Staple(0.5, 0.7, 0.0, 1.0, 1.0, false) };
        var r = Instruments.SubstS3Calc(1.0, 1.6, 1.5, 10.0, 4.0, bad);   // 1×1.6×1.5÷(0.5×4)＝1.2
        Assert.Equal(1.2, r.Sraw, 12);
        Assert.Equal(0.84 * 2.0, r.D, 12); Assert.Equal(0.0, r.E);
        Assert.NotEqual(2.0 * 1.0 * 0.7, r.D);
    }

    [Fact]
    public void s3は豊作で余りが立ち売れ行きと失業の式に入る()
    {
        // 豊作：σraw＝0.9・f＝1.2＝x＝1.08＝余りは 0.08×GoodNeed（今の規則は σraw＜1 で余りを数えない）
        var good = new[] { new Instruments.S3Staple(0.5, 1.2, 0.5, 2.0, 1.0, false) };
        double sraw = 1.0 * 1.2 * 1.5 / 2.0;                 // 0.9
        var g2 = Instruments.SubstS3Calc(1.0, 1.2, 1.5, 10.0, 4.0, good);
        Assert.Equal(sraw, g2.Sraw, 12);
        Assert.Equal(0.08 * 2.0, g2.E, 12); Assert.Equal(1.0 * 2.0, g2.D, 12); Assert.Equal(0.5, g2.ESoldShare, 12);
        Assert.Equal(0.08 * 2.0 * 2.0, g2.EValue, 12);
        // 売れ行き 1（余りが全て売れる）なら失業は 0・f＝0 でも nan にならない
        var all = new[] { new Instruments.S3Staple(0.5, 1.2, 1.0, 2.0, 1.0, false) };
        Assert.Equal(0.0, Instruments.SubstS3Calc(1.0, 1.2, 1.5, 10.0, 4.0, all).UnempNew, 12);
        var zero = new[] { new Instruments.S3Staple(0.5, 0.0, 1.0, 2.0, 1.0, false) };
        var z = Instruments.SubstS3Calc(1.0, 1.2, 1.5, 10.0, 4.0, zero);
        Assert.Equal(0.0, z.UnempNew); Assert.False(double.IsNaN(z.Kappa)); Assert.Equal(0.0, z.D);
    }

    [Fact]
    public void s3の列は実走で式の関係を保つ()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_subst3_" + Guid.NewGuid().ToString("N"));
        var sim = Two(1791UL);
        sim.StartWarmup(quartersOverride: 1);
        Instruments.Configure(dir, "*");
        try { for (int q = 0; q < 3; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1791UL, q); } }
        finally { Instruments.Close(sim); Instruments.Configure(null, null); }
        var rows = Rows(System.IO.Path.Combine(dir, "subst_s1791.tsv"), Instruments.SubstHeaderText).Where(r => r["s2RhoSource"] == "1" && r["quarter"] != "0").ToList();
        Assert.True(rows.Count >= 20, $"焼いた州が少ない {rows.Count}");
        int sr = 0;
        foreach (var r in rows)
        {
            double sraw = D(r, "s3SrawInst"), q = D(r, "s3QInst"), d = D(r, "s3DInst"), e = D(r, "s3EInst");
            Assert.False(double.IsNaN(sraw) || double.IsNaN(q) || double.IsNaN(d) || double.IsNaN(e), "焼いた州で s3 が nan");
            Assert.True(Math.Abs(q - (d + e)) <= 1e-9 * Math.Max(1.0, q), $"Q≠D＋E {q} {d} {e}");
            Assert.True(e >= 0 && d >= 0);
            Assert.True(D(r, "s3EatPerCapInst") <= D(r, "stapleNeedPerCapita") + 1e-9, "食べる量が必要量を超えた");
            double u = D(r, "s3UnempNewInst"); Assert.True(u >= 0 && u <= 1);
            if (sraw > 0) sr++;
        }
        Assert.True(sr >= 10, $"σraw が正の州が少ない＝何もしない計器で緑にならない {sr}");
    }
    // ---- (1325) 段 3-1 の器：s3 の写しの部品を本体と突き合わせる（事前登録 measurements/m1325_20261123_prereg_ss.txt の S1〜S4・突然変異 Ma1〜Ma3）----
    [Fact]
    public void s3の土地係数の写しは本体と同じ()
    {
        var c = new Coefficients();
        int n = 0;
        foreach (var (arable, rural) in new[] { (1e9, 1000.0), (500.0, 1000.0), (2000.0, 1000.0), (300.0, 0.5), (0.0, 10.0) })
        {
            var r = new Region { ArableLand = arable };
            Assert.Equal(BitConverter.DoubleToInt64Bits(Territory.LandFactor(r, rural, c)), BitConverter.DoubleToInt64Bits(Instruments.S3LandFactor(arable, rural, c)));
            if (Territory.LandFactor(r, rural, c) < 1.0) n++;
        }
        Assert.True(n >= 2, "土地係数が 1 未満の盤面が無い＝写しの誤りを拾わない");
    }

    [Fact]
    public void s3の収穫係数の三重積の写しは本体と同じ()
    {
        var hf = typeof(EconomyStep).GetMethod("StapleHarvestFactor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var r = new Region { HarvestFactor = 0.9, WeatherBandFactor = 1.1 };
        var crop = new double[Catalog.Goods.Length]; Array.Fill(crop, 0.8); crop[Catalog.BreadId] = 0.6; r.CropHealth = crop;
        int n = 0;
        foreach (var gd in Catalog.Goods)
        {
            if (gd.Tier != GoodTier.Staple) continue;
            double e = (double)hf.Invoke(null, [r, gd.Id, 0.75])!;
            Assert.Equal(BitConverter.DoubleToInt64Bits(e), BitConverter.DoubleToInt64Bits(Instruments.S3HarvestF(r, gd.Id, 0.75)));
            Assert.NotEqual(0.9 * 1.1 * crop[gd.Id], e);   // 土地係数 0.75 が効く盤面
            n++;
        }
        Assert.True(n >= 5);
    }

    [Fact]
    public void s3の売る財の写しは本体と同じ()
    {
        int chain = 0;
        for (int g = 0; g < Catalog.Goods.Length; g++)
        {
            var (ri, qi) = Instruments.S3SellGood(g);
            var (re, qe) = EconomyStep.SubsistenceSellGood(g);
            Assert.Equal(re, ri); Assert.Equal(BitConverter.DoubleToInt64Bits(qe), BitConverter.DoubleToInt64Bits(qi));
            if (re != g) chain++;
        }
        Assert.True(chain >= 10, $"遡る財が少ない {chain}");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void s3の土地税の基数の写しは本体と同じ(double fixedBasis)
    {
        var c = new Coefficients { LandTaxFixedBasis = fixedBasis };
        var p = new Pop { Size = 10, LandRentPerMonth = new MoneyPerMonth(40), LandTaxBasisAdjPerMonth = new MoneyPerMonth(-15) };
        Assert.Equal(BitConverter.DoubleToInt64Bits(Fiscal.LandTaxBase(c, p).Value), BitConverter.DoubleToInt64Bits(Instruments.S3LandTaxBase(c, p)));
        Assert.Equal(fixedBasis > 0 ? 25.0 : 40.0, Instruments.S3LandTaxBase(c, p));
    }
}
