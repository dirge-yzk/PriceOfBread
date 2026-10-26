using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(417) 線 dc（不満度の読み手の見直し・00-DISC）の番人。機構と不変条件だけ（創発の主張は成績表と計器が見る）。事前登録 measurements/dc_20261019_prereg.txt [6]。
/// ①闘争度の目標の法の項（政治の隔たり）が不満度の法の項と同じ式を通る（Step の MilVoice＝不満度の前クランプ・手計算の 0.25 の落差）＋DiscontentLawGap は闘争度へ広がらない。
/// ②参加の頭数：闘争度の高い POP と低い POP が旧の二値と新の連続で手計算の和に合う。
/// ③計算量の台帳：旗 0 で件数 0・旗 1 で問うた POP の数に合う。
/// ④計器 dcont_s：手計算の盤面で partOldK・partNewK・満足度・不満度が合う。
/// ⑤Politics.Discontent の呼び手の本数（SimCore のファイルごと）が増えていない。
/// </summary>
[Collection("Instruments")]
public class DiscontentReaderTests
{
    /// <summary>仏 1789 の盤面（8 四半期）。全 POP の意識を 0・満足度 0.5・参照点＝0.5＋drop・闘争度を既知にする（不満度＝WantWeight×drop だけ＝式で予言できる）。</summary>
    private static World Board(double drop, double militancy = 0.1)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent());
        sim.Run(8);
        var w = sim.World; var c = w.Coef;
        Assert.True(c.LivingRefWeight >= 1.0, "前提が崩れた：失望（相対）でない＝drop の予言が成り立たない");
        c.MilitancySpeed = 0; c.ConsciousnessSpeed = 0;
        foreach (var r in w.Regions)
            foreach (var p in r.Pops) { p.Militancy = militancy; p.Consciousness = 0; p.Satisfaction = 0.5; p.LivingRef = 0.5 + drop; }
        return w;
    }

    private static IEnumerable<Pop> ActivePops(World w)
    {
        foreach (var r in w.Regions)
        {
            foreach (var p in r.Pops) if (p.Size > 1e-9) yield return p;
        }
    }

    [Fact]
    public void 闘争度の目標に生活の項は入らず_不満度の前クランプは落差の0_25に合う()
    {
        var w = Board(drop: 0.25);
        var c = w.Coef;
        // 意識を 0 にすると政治の隔たりは 0 になる（wEdu＝min(意識,識字)＝0）＝生活の項だけが立つ。手計算＝WantWeight×0.25。
        var coord = PoliticalQuantities.DriftedPolityCoord(w.Polity, w.Suffrage, c);
        var before = new Dictionary<Pop, double>();
        foreach (var p in ActivePops(w)) before[p] = Politics.Discontent(w, c, p, coord, 0);
        Politics.Step(w, c);
        int n = 0;
        foreach (var p in ActivePops(w))
        {
            if (!before.TryGetValue(p, out double d0)) continue;   // Step の中で生まれた POP は比べない
            n++;
            Assert.Equal(0.0, p.MilVoice, 12);
            Assert.Equal(d0, c.WantWeight * 0.25, 12);   // 不満度は生活の項を持つ（意識 0＝法の項 0・クランプの内）
        }
        Assert.True(n > 50, $"比べた POP が少ない {n}");
    }

    [Fact]
    public void 意識のある盤面でも闘争度の政治の項は不満度の政治の項と同じ式で_DiscontentLawGapは闘争度に広がらない()
    {
        // 意識と識字を高く置く＝政治の隔たりが立つ。2 つの同一の盤面で DiscontentLawGap だけ変える（闘争度の目標の政治の項は変わらない＝4 軸の距離のまま）。
        World Make(double lawGapCoef)
        {
            var w = Board(drop: 0.0);
            foreach (var p in ActivePops(w)) { p.Consciousness = 0.9; p.Literacy = 0.9; }
            w.Coef.DiscontentLawGap = lawGapCoef;
            return w;
        }
        var w0 = Make(0.0); var w1 = Make(1.0);
        var coord = PoliticalQuantities.DriftedPolityCoord(w0.Polity, w0.Suffrage, w0.Coef);
        var pre = new List<double>(); var expectVoice = new List<double>();
        foreach (var p in ActivePops(w0))
        {
            pre.Add(Politics.Discontent(w0, w0.Coef, p, coord, 0));
            // 政治の項の手計算（テストの側の書き方）＝PoliticalDistanceWeight×min(意識,識字)×(1−発言権)×(4 軸の距離÷2)
            var ideal = Ideologies.IdealPoint(p.IdeologyShares);
            double s2 = 0; for (int k = 0; k < 4; k++) s2 += (ideal[k] - coord[k]) * (ideal[k] - coord[k]);
            expectVoice.Add(w0.Coef.PoliticalDistanceWeight * Math.Min(p.Consciousness, p.Literacy) * (1.0 - Politics.EffectiveVoice(w0, w0.Coef, p)) * (Math.Sqrt(s2) / 2.0));
        }
        Politics.Step(w0, w0.Coef); Politics.Step(w1, w1.Coef);
        var a = ActivePops(w0).ToList(); var b = ActivePops(w1).ToList();
        Assert.Equal(a.Count, b.Count);
        Assert.Equal(pre.Count, a.Count);
        int voiced = 0;
        for (int i = 0; i < a.Count; i++)
        {
            Assert.Equal(expectVoice[i], a[i].MilVoice, 12);   // 手計算（4 軸の距離÷2）に合う
            Assert.Equal(a[i].MilVoice, b[i].MilVoice);   // 旗に依らず同じ（ビット一致）
            if (a[i].MilVoice > 0) voiced++;
            // 不満度（DiscontentLawGap＝0 の旧経路）の前クランプ＝闘争度の目標の 2 項の和。両方が [0,1] の内なので等しい。
            double sum = a[i].MilVoice;   // 生活の項は闘争度の目標に入らない（(431)・(551) で Pop.MilLiving の欄も削除）＝目標の先頭は法の項だけ
            Assert.Equal(sum > 1 ? 1.0 : sum, pre[i], 12);
        }
        Assert.True(voiced > 50, $"政治の隔たりが立った POP が少ない {voiced}＝この番人は政治の項を見ていない");
    }

    [Fact]
    public void 参加の頭数は闘争度の高いPOPと低いPOPで旧の二値と新の連続に分かれる()
    {
        // 不満度 0（意識 0・落差 0）・闘争度は POP の並びで 0.8 と 0.2 に分ける。手計算＝旧は 0.8 の POP の規模の和・新は 0.8×高の和＋0.2×低の和。
        var w = Board(drop: 0.0);
        // 3 種類：高 0.8（旧も数える）・境界 0.5 ちょうど（旧は数えない＝闘争度＞0.5 の二値）・低 0.2。
        double hi = 0, mid = 0, lo = 0; int i = 0;
        foreach (var p in ActivePops(w))
        {
            switch (i++ % 3) { case 0: p.Militancy = 0.8; hi += p.Size; break; case 1: p.Militancy = 0.5; mid += p.Size; break; default: p.Militancy = 0.2; lo += p.Size; break; }
        }
        Assert.True(hi > 0 && mid > 0 && lo > 0);
        static double Total(World w2) { double s = 0; foreach (var cl in FactionSnapshot.ComputeClusters(w2)) s += cl.Size; return s; }
        w.Coef.ParticipationDiscontent = 0;
        Assert.Equal(hi, Total(w), 6);
        w.Coef.ParticipationDiscontent = 1.0;
        Assert.Equal(0.8 * hi + 0.5 * mid + 0.2 * lo, Total(w), 6);
    }

    [Fact]
    public void 計算量の台帳の参加の述語の行は旗0で0_旗1で問うたPOPの数に合う()
    {
        Assert.Equal(CostLedger.Count, CostLedger.Names.Length);
        Assert.Equal(CostLedger.Count, CostLedger.UnitMicros.Length);
        Assert.True(CostLedger.ParticipationPops < CostLedger.Count);
        Assert.Equal(CostLedger.ParticipationUnitMicros, CostLedger.UnitMicros[CostLedger.ParticipationPops]);
        var w = Board(drop: 0.0);
        int expect = ActivePops(w).Count();
        long b0 = w.Cost[CostLedger.ParticipationPops];   // 盤面を作る Run の中の思想の更新が載せた分（2026-10-19 (431)：思想の入力は常に不満度を問う）からの差分で見る
        w.Coef.ParticipationDiscontent = 0;
        FactionSnapshot.ComputeClusters(w, countCost: true);
        Assert.Equal(b0, w.Cost[CostLedger.ParticipationPops]);   // 参加の述語の旗 0＝この呼びは件数を足さない（推定の計算量は旧と同じ）
        w.Coef.ParticipationDiscontent = 1.0;
        FactionSnapshot.ComputeClusters(w);
        FactionSnapshot.Compute(w);   // 画面の表示の経路は台帳へ足さない（World 非変更の約束・画面と headless で台帳が分かれない）
        Assert.Equal(b0, w.Cost[CostLedger.ParticipationPops]);
        FactionSnapshot.ComputeClusters(w, countCost: true);
        Assert.Equal(b0 + expect, w.Cost[CostLedger.ParticipationPops]);
        FactionSnapshot.ComputeClusters(w, countCost: true);
        Assert.Equal(b0 + 2L * expect, w.Cost[CostLedger.ParticipationPops]);   // エンジンの呼びごとに足される
        // 実際のエンジンの呼び手（Factions.Step）が countCost を立てている＝本番の経路で台帳に載る（引数を落とすと赤）
        Assert.True(w.ParliamentEnabled && w.FactionRng is not null, "前提：Factions.Step が走る世界");
        long before = w.Cost[CostLedger.ParticipationPops];
        Factions.Step(w, w.Coef);
        Assert.Equal(before + expect, w.Cost[CostLedger.ParticipationPops]);
    }

    /// <summary>計器 dcont_s の 1 行を取る足場。<paramref name="setup"/> は 1 四半期進めた仏の盤面を手計算の盤面に書き換える（意識 0・満足度 0.5・参照点 0.5＋drop）。</summary>
    private static (Func<string, double> Col, World W) DcontRow(Action<World> setup, double drop = 0.0)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_dc_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia()], 1789UL, _ => new UtilityAgent());
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            sim.StepQuarter();
            var w = sim.Nations[0].World;
            foreach (var r in w.Regions)
                foreach (var p in r.Pops) { p.Consciousness = 0; p.Satisfaction = 0.5; p.LivingRef = 0.5 + drop; p.Militancy = 0.2; }
            setup(w);
            Instruments.Quarter(sim, 1789UL, 0);
            Instruments.Close(sim);
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "dcont_s1789.tsv"));
            Assert.Equal(Instruments.DcontHeaderText, lines[0]);
            var h = lines[0].Split('\t');
            string nameOfW = Instruments.NameOf(w);
            var row = lines.Skip(1).Select(l => l.Split('\t')).First(r => r[2] == nameOfW);
            return (name => double.Parse(row[Array.IndexOf(h, name)], System.Globalization.CultureInfo.InvariantCulture), w);
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    private static void Near(double expect, double actual, string what) => Assert.True(Math.Abs(expect - actual) <= 1e-8 * Math.Max(1.0, Math.Abs(expect)), $"{what} 期待 {expect}・実際 {actual}");   // 計器の書式は有効数字 10 桁

    [Fact]
    public void 計器dcont_sは手計算の盤面で頭数と満足度と不満度に合う()
    {
        double hi = 0, lo = 0, mass = 0;
        var (col, _) = DcontRow(w =>
        {
            int i = 0;
            foreach (var p in ActivePops(w))
            {
                bool high = (i++ % 4) == 0; p.Militancy = high ? 0.8 : 0.2;
                if (high) hi += p.Size; else lo += p.Size;
                if (Politics.IsMassPop(p)) mass += p.Size;
            }
        });
        Near(hi, col("partOldK"), "partOldK");                      // 旧の二値＝闘争度 0.8 の POP の規模の和
        Near(0.8 * hi + 0.2 * lo, col("partNewK"), "partNewK");    // 連続＝max(不満度 0, 闘争度)
        Near(0.8 * hi + 0.2 * lo, col("partMilK"), "partMilK");    // 不満度 0 なら重みが闘争度だけの頭数と同じ
        Near(mass, col("massPopK"), "massPopK");
        Assert.Equal(0.5, col("satMeanShare"), 9);
        Assert.Equal(0.0, col("discontentMeanShare"), 9);          // 意識 0・落差 0
        Assert.Equal(0.5, col("chronicShortageMeanShare"), 9);
        Assert.Equal(0.0, col("livingDropMeanIndex"), 9);
        Assert.Equal(0.0, col("dGtMShare"), 9);                    // 不満度 0 は闘争度を超えない
        Assert.Equal(0.0, col("highSatHighMilShare"), 9);          // 満足度 0.5＜0.6
    }

    [Fact]
    public void 計器dcont_sの罷業の列は地域の闘争度が門を超えた地域の数と超過分に合う()
    {
        // 手計算：全ての POP の闘争度を 0.8 にすると、人口のある活動中の地域ごとに平均 0.8・門 0.5・超過 0.3。組合容認（≠違法）のとき地域の数×0.3 が和。違法なら 0。
        int regions = 0;
        var (col, _) = DcontRow(w =>
        {
            w.LaborUnion = LaborUnionLaw.Tolerated;
            foreach (var r in w.Regions)
            {
                double sz = 0; foreach (var p in r.Pops) { p.Militancy = 0.8; sz += p.Size; }
                if (sz > 1e-9) regions++;
            }
        });
        Assert.True(regions > 0);
        Near(regions, col("strikeRegions"), "strikeRegions");
        Near(0.3 * regions, col("strikeIntensitySum"), "strikeIntensitySum");
        var (col2, _) = DcontRow(w =>
        {
            w.LaborUnion = LaborUnionLaw.Illegal;
            foreach (var r in w.Regions) foreach (var p in r.Pops) p.Militancy = 0.8;
        });
        Assert.Equal(0.0, col2("strikeRegions"));   // 違法なら罷業の導出は働かない（World.TechProductivity と同じ門）
        Assert.Equal(0.0, col2("strikeIntensitySum"));
    }

    [Fact]
    public void 計器dcont_sは不満度が闘争度を上回る盤面で不満度の分と運動体の門の列に合う()
    {
        // 落差 0.5・意識 0 → 不満度＝WantWeight×0.5＝0.4（手計算・WantWeight＝0.8 は前提として確かめる）。闘争度は 0.8 と 0.2 → 低い方は不満度が先行する。
        double hi = 0, lo = 0, total = 0;
        var (col, w0) = DcontRow(w =>
        {
            Assert.Equal(0.8, w.Coef.WantWeight);
            int i = 0;
            foreach (var p in ActivePops(w))
            {
                bool high = (i++ % 4) == 0; p.Militancy = high ? 0.8 : 0.2;
                if (high) hi += p.Size; else lo += p.Size;
                total += p.Size;
            }
            // 運動体 4 件：結束 0.9（門以上・巨大）／0.5（変異の門以上・蜂起の門の下）／0.1（どちらの門も下）／0.9 で極小。巨大な規模＝戦力が保持力＋郷勇を超える。
            var home = w.Regions.First();
            w.Factions.Clear();
            w.Factions.Add(new FactionState { Region = home, Bloc = 2, Ideo = 0, Size = 1e12, AvgMilitancy = 0.9, Cohesion = 0.9 });
            w.Factions.Add(new FactionState { Region = home, Bloc = 2, Ideo = 1, Size = 1e12, AvgMilitancy = 0.9, Cohesion = 0.5 });
            w.Factions.Add(new FactionState { Region = home, Bloc = 2, Ideo = 2, Size = 1e12, AvgMilitancy = 0.9, Cohesion = 0.1 });
            // 結束は門以上だが規模が極小＝戦力は正だが保持力＋郷勇を超えない（戦力比が 0 超 1 以下の運動体を数えない）
            w.Factions.Add(new FactionState { Region = home, Bloc = 2, Ideo = 3, Size = 1e-3, AvgMilitancy = 0.9, Cohesion = 0.9 });
        }, drop: 0.5);
        Assert.True(hi > 0 && lo > 0);
        Near(hi, col("partOldK"), "partOldK");
        Near(0.8 * hi + 0.2 * lo, col("partMilK"), "partMilK");                 // 重みが闘争度だけ
        Near(0.8 * hi + 0.4 * lo, col("partNewK"), "partNewK");                 // 低い方は max(0.4, 0.2)＝0.4
        Near(0.2 * lo, col("partNewK") - col("partMilK"), "不満度が先行した分");   // partNewK−partMilK＝(0.4−0.2)×低い方の規模
        Near(lo / total, col("dGtMShare"), "dGtMShare");
        Near(total, col("partDenK"), "partDenK");                               // (431) 参加の側の全 POP の規模の和＝dGtMShare の分母
        // 運動体の MilSum の和（足切り 1% を越えた運動体だけ）の上限・順序：Σ 闘争度×規模×重み。重みが闘争度だけ＜重みが max（低い方だけ増える）。
        Assert.True(col("milSumMilK") > 0 && col("milSumMilK") <= 0.64 * hi + 0.04 * lo + 1e-6, $"milSumMilK {col("milSumMilK")}");
        Assert.True(col("milSumNewK") <= 0.64 * hi + 0.08 * lo + 1e-6, $"milSumNewK {col("milSumNewK")}");
        Assert.True(col("milSumNewK") > col("milSumMilK"), "低い闘争度の POP は不満度の分だけ運動体の MilSum を増やす");
        // 運動体の門：件数（4 件を置いた＝w.Factions の 4 件だけ）
        Assert.Equal(4.0, col("factionsCount"));
        Assert.Equal(2.0, col("facCohGateCount"));    // 結束 0.9 の 2 件が蜂起の門（0.8）以上
        Assert.Equal(3.0, col("facCohMutCount"));     // 結束 0.9・0.9・0.5 が変異の門（0.3）以上
        Assert.Equal(1.0, col("facStrongCount"));     // 規模 1e12 の運動体だけが戦力が保持力＋郷勇を超える（極小の運動体は超えない）
        Assert.True(col("strengthRatioMaxIndex") > 1.0);
        Assert.NotNull(w0);
    }

    [Fact]
    public void 計器dcont_sの戦力比は規模0の運動体で0に合う()
    {
        var (col, _) = DcontRow(w =>
        {
            var home = w.Regions.First();
            w.Factions.Clear();
            w.Factions.Add(new FactionState { Region = home, Bloc = 2, Ideo = 0, Size = 0, AvgMilitancy = 0.9, Cohesion = 0.9 });
        });
        Assert.Equal(1.0, col("facCohGateCount"));
        Assert.Equal(0.0, col("facStrongCount"));
        Assert.Equal(0.0, col("strengthRatioMaxIndex"));
    }

    [Fact]
    public void Politics_Discontentの呼び手の本数がファイルごとに増えていない()
    {
        // 製品コード（SimCore）の中の `Discontent(` の出現（コメント行を除く・`AvgMassDiscontent(` のような前に英字が付く名前は数えない）。
        // 想定＝Politics.cs 2（定義＋AvgMassDiscontent）・FactionSnapshot.cs 1（参加の述語）・InstrumentsDiscontent.cs 1（計器）。増やすときはこの表と事前登録を直す。
        var re = new System.Text.RegularExpressions.Regex(@"(?<![A-Za-z])Discontent\(");
        var found = new SortedDictionary<string, int>();
        string root = System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.SimCore");
        foreach (var f in System.IO.Directory.EnumerateFiles(root, "*.cs", System.IO.SearchOption.AllDirectories))
        {
            if (f.Contains(System.IO.Path.DirectorySeparatorChar + "obj" + System.IO.Path.DirectorySeparatorChar) || f.Contains(System.IO.Path.DirectorySeparatorChar + "bin" + System.IO.Path.DirectorySeparatorChar)) continue;
            int n = 0;
            foreach (var line in System.IO.File.ReadAllLines(f))
            {
                string t = line.TrimStart();
                if (t.StartsWith("//")) continue;
                int cut = line.IndexOf("//", StringComparison.Ordinal);
                string code = cut >= 0 ? line[..cut] : line;
                n += re.Matches(code).Count;
            }
            if (n > 0) found[System.IO.Path.GetFileName(f)] = n;
        }
        Assert.Equal("FactionSnapshot.cs=1;InstrumentsDiscontent.cs=1;Politics.cs=2", string.Join(";", found.Select(kv => kv.Key + "=" + kv.Value)));
    }
}
