using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(660) 線 mon2（頻度の監視の第 2 段・読むだけの 3 表 monlaw_s・mongood_s・monai_s）の番人。機構と不変条件だけ（創発の主張は成績表と注視の一覧が見る）。事前登録 measurements/mon660_20261030_prereg.txt と 追記 1。
/// ①数え（MonCount）：仕込んだ年数が数に一致し、閾値ちょうどの標本・相対の床ちょうどの標本の境界が正しい（不等号を反転する突然変異で赤）。
/// ②年末の断面（MonReadGood）：州ごとに r.PriceLevel（1 以外）と m.Price を手で置き、価格比の中央値が手置きの比の中央値に合う（分母から PriceLevel を外す突然変異で赤）。
/// ③monai_s：陽性対照（毎四半期書く欄は数えた四半期の数・書かない欄は 0）・陰性対照（NullAgent は全欄 0）・窓の外の書き込みは数えない。
/// ④monlaw_s：列の数が LevelLaws の長さ×2 に合う・国有化が Nationalization と #2〜#5 になる・現在段と目標が別の値を持てる（Target を現在段に入れる突然変異で赤）・nationUid が nation_s と一致する。
/// ⑤On が偽なら何も溜めず何も開かない・恒等（同じ種の同じ世界を計器あり・なしで走らせて終わりの状態の要約が一致する）。
/// </summary>
[Collection("Instruments")]
public class InstrumentsMonTests
{
    private static MultiNationSim Make(Func<INationAgent>? agent = null)
        => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => agent?.Invoke() ?? new NullAgent());

    private static string TempDir() => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_mon_" + Guid.NewGuid().ToString("N"));

    private static (string[] Header, List<string[]> Rows) Table(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }

    /// <summary>計器を入れて quarters 四半期走らせ Close まで行う。後始末は呼び手（dir を返す）。</summary>
    private static (MultiNationSim Sim, string Dir) RunIns(Func<INationAgent>? agent, int quarters, Action<MultiNationSim, int>? beforeStep = null, Action<MultiNationSim, int>? afterStep = null)
    {
        string dir = TempDir();
        Instruments.Configure(dir, "*");
        var sim = Make(agent);
        Instruments.Phase(sim, 1789UL, "open");
        sim.StartWarmup();
        Instruments.Phase(sim, 1789UL, "warm");
        try
        {
            for (int q = 0; q < quarters; q++)
            {
                beforeStep?.Invoke(sim, q);
                sim.StepQuarter();
                afterStep?.Invoke(sim, q);
                Instruments.Quarter(sim, 1789UL, q);
            }
        }
        finally { Instruments.Close(sim); }
        return (sim, dir);
    }

    private static void Cleanup(string dir) { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }

    // ---------------------------------------------------------------- ① 数え ----------------------------------------------------------------

    [Fact]
    public void 数えの各列は手計算の数に合う()
    {
        var dem = new List<double> { 100, 100, 100, 100, 100, 100, 100, 100, 100, 1.0, 0.99, 0 };
        var flow = new List<double> { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 0.5, 0 };   // 年10＝流量 0.5（床 1 未満）・年11＝流量 0
        var sh = new List<double> { 50, 49.999, 60, 0, 0, 0, 0, 0, 0, 1.0, 0.99, 0 };
        var stk = new List<double> { 2400, 2399.9, 0, 0, 0, 0, 0, 0, 0, 0, 5, 5 };
        var pxr = new List<double> { 20, 19.99, 0.05, 0.0501, 50, 0.02, double.NaN, 0, -1, 1, 1, 1 };
        var c = Instruments.MonCount(dem, flow, sh, stk, pxr);
        // 床：需要の正の年の中央値 100（11 本の正のうち…）。正の需要は 11 本 [100×9, 1.0, 0.99]→ 中央値 100 → 床 1.0／流量の正は 11 本 [100×10, 0.5]→ 中央値 100 → 床 1.0
        Assert.Equal(1.0, c.DemandFloor, 12);
        Assert.Equal(1.0, c.FlowFloor, 12);
        // 活きた：需要が床以上（年0〜9・10 年）か流量が床以上（年0〜9）→ 10。年10（需要 0.99＜1・流量 0.5＜1）と年11 は活きない
        Assert.Equal(10, c.Active);
        // 不足：年0（ちょうど 0.5）・年2（0.6）・年9（床ちょうど・share 1）＝3。年1（0.49999）・年10（床の直下）は数えない
        Assert.Equal(3, c.Short);
        Assert.Equal(1.0, c.MaxShortShare, 12);
        // 余り：流量が床以上の年（年0〜9）で 在庫÷流量 ≧ 24：年0（2400÷100＝ちょうど 24）だけ。年1（23.999）は数えない
        Assert.Equal(1, c.Glut);
        Assert.Equal(24.0, c.MaxStockMonths, 9);
        // 生産なし：需要が床以上で流量が床未満の年＝0（年0〜9 は流量 100）
        Assert.Equal(0, c.NoProd);
        // 価格比：pxr>0 の有限は 20・19.99・0.05・0.0501・50・0.02・1・1・1（9 年）。NaN・0・−1 は外す
        Assert.Equal(9, c.PxYears);
        Assert.Equal(2, c.PxHi);     // 20（ちょうど）・50
        Assert.Equal(2, c.PxLo);     // 0.05（ちょうど）・0.02
        Assert.Equal(1, c.PxHiB1);   // 50（ちょうど）
        Assert.Equal(1, c.PxLoB1);   // 0.02（ちょうど）
        Assert.Equal(50.0, c.MaxPxr, 12);
        Assert.Equal(0.02, c.MinPxr, 12);
    }

    [Fact]
    public void 生産なしは需要が床以上で流量が床未満の年だけ数え_流量だけの年は数えない()
    {
        var dem = new List<double> { 10, 10, 10, 10, 0 };
        var flow = new List<double> { 0, 0, 10, 10, 10 };
        var z = new List<double> { 0, 0, 0, 0, 0 };
        var c = Instruments.MonCount(dem, flow, z, z, z);
        Assert.Equal(2, c.NoProd);   // 年0・1＝需要あり・流量なし
        Assert.Equal(5, c.Active);   // 年4＝需要なしでも流量が床以上なら活きた
    }

    // ---------------------------------------------------------------- ② 年末の断面 ----------------------------------------------------------------

    [Fact]
    public void 年末の価格比の中央値は州ごとの物価水準で割った手置きの比の中央値に合う()
    {
        var sim = Make();
        sim.StartWarmup();
        sim.StepQuarter();
        var w = sim.Nations[0].World;
        int g = Array.FindIndex(Catalog.Goods, x => x.Tier == GoodTier.Staple);
        Assert.True(g >= 0);
        var ratios = new List<double>(); int i = 0;
        double demSum = 0, shSum = 0, stkSum = 0;
        foreach (var r in w.Regions)
        {
            double ratio = 1.0 + (i % 5) * 0.7;                    // 1.0・1.7・2.4・3.1・3.8 の繰り返し
            r.PriceLevel = 0.5 + (i % 3) * 0.75;                   // 0.5・1.25・2.0（1 以外）
            var m = r.Markets[g];
            m.Price = ratio * Catalog.RefPrices[g] * r.PriceLevel;
            m.LastDemand = 3 + i; m.LastShortage = 1 + i % 4; m.Stock = 7 + i;
            demSum += m.LastDemand; shSum += m.LastShortage; stkSum += m.Stock;
            ratios.Add(ratio); i++;
        }
        Assert.True(ratios.Count >= 3, "州が少なすぎて中央値の検査にならない");
        Assert.True(Instruments.MonReadGood(w, g, out double dem, out _, out double sh, out double stk, out double pxr));
        ratios.Sort(); int mid = ratios.Count / 2;
        double expect = ratios.Count % 2 == 1 ? ratios[mid] : 0.5 * (ratios[mid - 1] + ratios[mid]);
        Assert.Equal(expect, pxr, 9);
        Assert.Equal(demSum, dem, 9); Assert.Equal(shSum, sh, 9); Assert.Equal(stkSum, stk, 9);
    }

    // ---------------------------------------------------------------- ③ monai_s ----------------------------------------------------------------

    private sealed class WriterAgent : INationAgent
    {
        public int Calls;
        public void DecideQuarter(World w) { Calls++; w.Policy.IncomeTaxTarget = 0.10 + 0.001 * Calls; }   // 毎四半期ちがう値を置く欄は 1 本だけ
    }

    [Fact]
    public void monaiは書く欄を数え_書かない欄と窓の外の書き込みは数えず_NullAgentは全欄0()
    {
        // 陽性対照：IncomeTaxTarget だけ毎四半期書く。窓の外：PollTaxTarget を StepQuarter の前に書く（DecideQuarter の窓の外）
        int outsideStep = 0;
        var (_, dir) = RunIns(() => new WriterAgent(), 8, beforeStep: (s, q) => { foreach (var n in s.Nations) n.World.Policy.PollTaxTarget = 0.01 * (++outsideStep); });
        try
        {
            var (h, rows) = Table(System.IO.Path.Combine(dir, "monai_s1789.tsv"));
            Assert.Equal(Instruments.MonAiHeader, string.Join('\t', h));
            int fi = Array.IndexOf(h, "field"), ci = Array.IndexOf(h, "changedQuarters"), ai = Array.IndexOf(h, "aliveQuarters");
            int cnt = 0, zeroOthers = 0;
            foreach (var r in rows)
            {
                Assert.Equal(h.Length, r.Length);
                Assert.True(int.TryParse(r[ai], out int alive) && alive == 8, $"数えた四半期の数が 8 でない {r[ai]}（助走中を数えた・窓が足りない）");
                if (r[fi] == "IncomeTaxTarget") { Assert.Equal("8", r[ci]); cnt++; }
                else if (r[ci] == "0") zeroOthers++;
                if (r[fi] == "PollTaxTarget") Assert.Equal("0", r[ci]);   // 窓の外の書き込みは数えない
            }
            Assert.Equal(4, cnt);              // 4 国
            Assert.True(zeroOthers > 4 * 20, $"書かない欄が少ない {zeroOthers}");
        }
        finally { Cleanup(dir); }
        var (_, dir2) = RunIns(null, 8);
        try
        {
            var (h, rows) = Table(System.IO.Path.Combine(dir2, "monai_s1789.tsv"));
            int ci = Array.IndexOf(h, "changedQuarters");
            Assert.True(rows.Count > 4 * 40);
            Assert.All(rows, r => Assert.True(r[ci] == "0" || r[ci] == "比較不能", $"NullAgent なのに書き換えがある {r[3]}={r[ci]}"));
        }
        finally { Cleanup(dir2); }
    }

    // ---------------------------------------------------------------- ④ monlaw_s ----------------------------------------------------------------

    [Fact]
    public void monlawの列は表の長さから作られ_国有化は添字つきで_現在段と目標は別の値を持ち_nationUidはnation_sと一致する()
    {
        var laws = Government.LevelLaws;
        // 列名の生成は表の長さだけで呼べる（軸が足されても自動で増える）
        var synth = Instruments.MonLawNames([PolicyAxis.Suffrage, PolicyAxis.Nationalization, PolicyAxis.Nationalization, PolicyAxis.Nationalization, PolicyAxis.Censorship]);
        Assert.Equal(["Suffrage", "Nationalization", "Nationalization#2", "Nationalization#3", "Censorship"], synth);
        var names = Instruments.MonLawNamesOfTable;
        Assert.Equal(laws.Length, names.Length);
        Assert.Equal(laws.Length, names.Distinct().Count());
        Assert.Contains("Nationalization#5", names);

        int eduIdx = Array.FindIndex(laws, l => l.Axis == PolicyAxis.EducationLaw);
        Assert.True(eduIdx >= 0);
        int differ = -1;
        var (_, dir) = RunIns(null, 6, afterStep: (s, q) =>
        {
            if (q != 5) return;
            var w = s.Nations[0].World;
            int cur = laws[eduIdx].Get(w);
            int tgt = cur == laws[eduIdx].Top ? 0 : laws[eduIdx].Top;          // Set・Enact は呼ばず目標だけを現在段と別の値に置く
            w.Policy.EducationLaw = (EducationLaw)tgt; differ = tgt;
        });
        try
        {
            var (h, rows) = Table(System.IO.Path.Combine(dir, "monlaw_s1789.tsv"));
            Assert.Equal(Instruments.MonLawHeader(), string.Join('\t', h));
            int tail = 7 + laws.Count(l => l.Open != null);
            Assert.Equal(tail + 2 * laws.Length, h.Length);
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            int qi = Array.IndexOf(h, "quarter"), ui = Array.IndexOf(h, "nationUid"), ii = Array.IndexOf(h, "nationIndex");
            int ci = Array.IndexOf(h, "EducationLaw"), ti = Array.IndexOf(h, "EducationLawTarget");
            Assert.True(ci >= 0 && ti >= 0);
            var last = rows.Where(r => r[qi] == "5" && r[ii] == "0").ToList();
            Assert.Single(last);
            Assert.Equal(differ.ToString(), last[0][ti]);
            Assert.NotEqual(last[0][ci], last[0][ti]);   // 現在段の列に目標を入れる突然変異はここで赤
            // nationUid は nation_s の nationUid と同じ四半期・同じ国で一致する
            var (nh, nrows) = Table(System.IO.Path.Combine(dir, "nation_s1789.tsv"));
            int nq = Array.IndexOf(nh, "quarter"), nu = Array.IndexOf(nh, "nationUid"), nix = Array.IndexOf(nh, "nationIndex");
            Assert.True(nu >= 0);
            var uidByKey = nrows.ToDictionary(r => (r[nq], r[nix]), r => r[nu]);
            int matched = 0;
            foreach (var r in rows) { Assert.Equal(uidByKey[(r[qi], r[ii])], r[ui]); matched++; }
            Assert.True(matched >= 4 * 6);
            Assert.Equal(4, rows.Select(r => r[ui]).Distinct().Count());
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void mongoodの見出しの閾値は定数と一致し_行の列数は見出しに合う()
    {
        var (_, dir) = RunIns(null, 12);
        try
        {
            var (h, rows) = Table(System.IO.Path.Combine(dir, "mongood_s1789.tsv"));
            Assert.Equal(Instruments.MonGoodHeader, string.Join('\t', h));
            Assert.Contains("shortYears_ge0.5", h); Assert.Contains("glutYears_ge24", h); Assert.Contains("pxHiYears_ge20", h); Assert.Contains("pxLoB1Years_le0.02", h);
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            int ai = Array.IndexOf(h, "aliveYears"), gi = Array.IndexOf(h, "goodName");
            Assert.True(rows.Count > 4 * 20, $"行が少ない {rows.Count}");
            Assert.All(rows, r => Assert.Equal("3", r[ai]));   // 12 四半期＝年末が 3 回（q%4==3）
            Assert.Equal(rows.Select(r => r[gi]).Distinct().Count(), Catalog.Goods.Length);   // 全ての財（段階を問わない）
        }
        finally { Cleanup(dir); }
    }

    // ---------------------------------------------------------------- ⑤ Off と恒等 ----------------------------------------------------------------

    [Fact]
    public void Onが偽なら何も溜めず何も開かない()
    {
        string dir = TempDir();
        Instruments.Configure(dir, "*");
        Instruments.On = false;
        try
        {
            var sim = Make(() => new WriterAgent());
            sim.StartWarmup();
            for (int q = 0; q < 6; q++) sim.StepQuarter();
            Instruments.On = true;
            Instruments.Close(sim);                                                  // 溜めが無ければ書く物も無い
            Assert.False(System.IO.File.Exists(System.IO.Path.Combine(dir, "monai_s1789.tsv")), "On が偽なのに monai_s が出た");
            Assert.False(System.IO.File.Exists(System.IO.Path.Combine(dir, "mongood_s1789.tsv")));
            Assert.False(System.IO.File.Exists(System.IO.Path.Combine(dir, "monlaw_s1789.tsv")));
        }
        finally { Cleanup(dir); }
    }

    private static string Summary(MultiNationSim sim)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            sb.Append(w.Treasury.Value.ToString("R")).Append('|').Append(w.TotalPopulation.ToString("R")).Append('|').Append(w.PriceLevel.ToString("R")).Append('|');
            foreach (var l in Government.LevelLaws) sb.Append(l.Get(w)).Append(',');
            foreach (var f in typeof(NationPolicy).GetFields()) sb.Append(Convert.ToString(f.GetValue(w.Policy), System.Globalization.CultureInfo.InvariantCulture)).Append(';');
            sb.Append('\n');
        }
        return sb.ToString();
    }

    [Fact]
    public void 計器ありとなしで同じ世界が同じ状態になる()
    {
        Instruments.Configure(null, null);
        var a = Make(() => new UtilityAgent());
        a.StartWarmup();
        for (int q = 0; q < 12; q++) a.StepQuarter();
        string off = Summary(a);
        var (b, dir) = RunIns(() => new UtilityAgent(), 12);
        try { Assert.Equal(off, Summary(b)); }
        finally { Cleanup(dir); }
    }
}
