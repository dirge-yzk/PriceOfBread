using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(823) 線 c65sv：計器 silverst_s（銀の平価の注入の州の表・読むだけ）と silverwt_s へ足した反実仮想・自走査の列の番人。
/// 機構の試験＝薄い側の額の純関数の手の値／実盤面で計器が自分で読んだ市場とエンジンが渡した値がビット一致／手で組んだ盤面で入った・出た州と上位の並びと造幣の取り込み。
/// ⚠突然変異＝min を max に→U1 が赤／自走査が金鉱を銀の市場から読む・前の州の市場を読む→U2 が赤／入った・出たの取り違え・上位を昇順・前の集合を更新しない→U3 が赤／取り込みを 0 に戻さない・金属の取り違え→U4 が赤。</summary>
public class SilverstInstrumentTests
{
    [Fact]
    public void 薄い側の額の票は2辺の小さい方で_非有限と非正の辺は票なし()
    {
        Assert.Equal(3.0, Instruments.SilverMinWeight(3.0, 5.0));
        Assert.Equal(3.0, Instruments.SilverMinWeight(5.0, 3.0));
        Assert.Equal(4.0, Instruments.SilverMinWeight(4.0, 4.0));
        Assert.Equal(0.0, Instruments.SilverMinWeight(0.0, 5.0));
        Assert.Equal(0.0, Instruments.SilverMinWeight(-1.0, 5.0));
        Assert.Equal(0.0, Instruments.SilverMinWeight(double.NaN, 5.0));
        Assert.Equal(0.0, Instruments.SilverMinWeight(5.0, double.PositiveInfinity));
        Assert.Equal(0.0, Instruments.SilverMinWeight(double.NegativeInfinity, double.NaN));
    }

    static List<Dictionary<string, string>> Rows(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        var h = lines[0].Split('\t');
        return lines.Skip(1).Select(l => { var p = l.Split('\t'); Assert.Equal(h.Length, p.Length); var d = new Dictionary<string, string>(); for (int i = 0; i < h.Length; i++) d[h[i]] = p[i]; return d; }).ToList();
    }
    static double D(Dictionary<string, string> r, string k) => r[k] == "nan" ? double.NaN : double.Parse(r[k], System.Globalization.CultureInfo.InvariantCulture);

    static bool Live(Region r)
    {
        if (Catalog.SilverId >= r.Markets.Length || Catalog.GoldOreId >= r.Markets.Length) return false;
        var a = r.Markets[Catalog.SilverId]; var u = r.Markets[Catalog.GoldOreId];
        return (a.LastDemand > 1e-9 || a.LastProduced > 1e-9) && (u.LastDemand > 1e-9 || u.LastProduced > 1e-9) && a.Price > 1e-9 && u.Price > 1e-9;
    }

    [Collection("Instruments")]
    public class OnTheBoard
    {
        static (List<Dictionary<string, string>> wt, List<Dictionary<string, string>> st) Quarters(double flag, int quarters)
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_sst_" + Guid.NewGuid().ToString("N"));
            Instruments.Configure(dir, "*");
            try
            {
                var sim = new MultiNationSim(MultiNationShared.FourNations(), seed: 1789, coef: new Coefficients { SilverParityDemandWeighted = flag });
                try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
                finally { Instruments.Close(sim); }
                return (Rows(System.IO.Path.Combine(dir, "silverwt_s1789.tsv")), Rows(System.IO.Path.Combine(dir, "silverst_s1789.tsv")));
            }
            finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void 計器が自分で読んだ市場はエンジンが渡した値とビット一致し_上位の票と入った出た州の数が別の道の値と合う(double flag)
        {
            var (wt, st) = Quarters(flag, 3);
            Assert.Equal(3, wt.Count);
            foreach (var r in wt)
            {
                Assert.True(D(r, "stateCount") > 0, "前提が崩れた：走査に入る州が無い");
                Assert.Equal(0.0, D(r, "stateCountDiff"));
                Assert.Equal(0.0, D(r, "w1SumDiff"));
                Assert.Equal(0.0, D(r, "relW1ReadDiff"));
                Assert.Equal(D(r, "stateCount"), D(r, "selfStateCount"));
                Assert.False(double.IsNaN(D(r, "relMinRefIndex")) || double.IsNaN(D(r, "relMinValIndex")) || double.IsNaN(D(r, "relMaxRefIndex")));
                double seq = D(r, "seq");
                var top = st.Where(x => D(x, "seq") == seq && x["kind"] == "T1" && x["rank"] == "1").ToList();
                Assert.Single(top);
                double share = D(top[0], "w1Share");
                Assert.True(share > 0 && share <= 1, "T1 の 1 位の票の割合が (0,1] の外");
                Assert.True(Math.Abs(share - D(r, "w1MaxShare")) <= 1e-12, $"別の道の 1 州の最大の票の割合が合わない：行 {share} と silverwt_s {D(r, "w1MaxShare")}");
                Assert.Equal(D(r, "enterCount"), (double)st.Count(x => D(x, "seq") == seq && x["kind"] == "E"));
                Assert.Equal(D(r, "leaveCount"), (double)st.Count(x => D(x, "seq") == seq && x["kind"] == "L"));
                if (seq == 1)
                {
                    Assert.True(double.IsNaN(D(r, "enterShareMinRef")));
                    Assert.DoesNotContain(st, x => D(x, "seq") == 1 && (x["kind"] == "E" || x["kind"] == "L"));
                }
            }
            foreach (var g in st.Where(s => s["kind"] == "T1" || s["kind"] == "TR" || s["kind"] == "TV").GroupBy(s => (seq: s["seq"], kind: s["kind"])))
            {
                string col = g.Key.kind == "T1" ? "w1" : g.Key.kind == "TR" ? "wMinRef" : "wMinVal";
                var ws = g.Select(y => D(y, col)).ToList();
                Assert.True(ws.Count <= 5);
                for (int i = 1; i < ws.Count; i++) Assert.True(ws[i] <= ws[i - 1], "上位の並びが票の降順でない");
            }
        }

        [Fact]
        public void 手で組んだ盤面で入った州と出た州と上位の並びと造幣の取り込みが数えられる()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_sst_" + Guid.NewGuid().ToString("N"));
            Instruments.Configure(dir, "*");
            try
            {
                var sim = new MultiNationSim(MultiNationShared.FourNations(), seed: 1789, coef: new Coefficients());
                Region target, other;
                Instruments.SilverstAgg a0, a1, a2, a3, a4;
                try
                {
                    for (int q = 0; q < 2; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); }
                    var live = sim.Nations.SelectMany(n => n.World.Regions).Where(Live).ToList();
                    Assert.True(live.Count >= 3, "前提が崩れた：生きた州が 3 つ無い");
                    target = live[0]; other = live.First(r => r.Name != target.Name);
                    var tAg = target.Markets[Catalog.SilverId];
                    double d0 = tAg.LastDemand, p0 = tAg.LastProduced;
                    a0 = Instruments.SilverstEnd(sim, false, 900);
                    // ① 出た：銀の市場を死なせる
                    tAg.LastDemand = 0; tAg.LastProduced = 0;
                    a1 = Instruments.SilverstEnd(sim, false, 901);
                    // ② 入った：戻す＋造幣の取り込み（銀 5 を 1 回・金鉱 3 と 2 の 2 回）
                    tAg.LastDemand = d0; tAg.LastProduced = p0;
                    Instruments.SilverMintCapture(target, Catalog.SilverId, 5.0);
                    Instruments.SilverMintCapture(target, Catalog.GoldOreId, 3.0);
                    Instruments.SilverMintCapture(target, Catalog.GoldOreId, 2.0);
                    a2 = Instruments.SilverstEnd(sim, false, 902);
                    // ③ 取り込みは注入ごとに 0 へ戻る：もう一度出す
                    tAg.LastDemand = 0; tAg.LastProduced = 0;
                    a3 = Instruments.SilverstEnd(sim, false, 903);
                    // ④ 別の州の金鉱の需要を桁違いに上げる＋戻して入れる
                    tAg.LastDemand = d0; tAg.LastProduced = p0;
                    other.Markets[Catalog.GoldOreId].LastDemand = 1e9;
                    a4 = Instruments.SilverstEnd(sim, false, 904);
                }
                finally { Instruments.Close(sim); }
                Assert.Equal(0, a0.EnterCount + a0.LeaveCount);
                Assert.Equal(1, a1.LeaveCount); Assert.Equal(0, a1.EnterCount);
                Assert.Equal(1, a2.EnterCount); Assert.Equal(0, a2.LeaveCount);
                Assert.Equal(1, a3.LeaveCount);
                Assert.Equal(1, a4.EnterCount);
                var st = Rows(System.IO.Path.Combine(dir, "silverst_s1789.tsv"));
                List<Dictionary<string, string>> Pick(int seq, string kind) => st.Where(x => D(x, "seq") == seq && x["kind"] == kind && x["region"] == target.Name).ToList();
                Assert.Single(Pick(901, "L")); Assert.Equal("0", Pick(901, "L")[0]["inScan"]);
                var e = Pick(902, "E"); Assert.Single(e);
                Assert.Equal(5.0, D(e[0], "mintQtyAg")); Assert.Equal(5.0, D(e[0], "mintQtyAu"));
                Assert.Equal(1.0, D(e[0], "mintMonthsAg")); Assert.Equal(2.0, D(e[0], "mintMonthsAu"));
                var l = Pick(903, "L"); Assert.Single(l);
                Assert.Equal(0.0, D(l[0], "mintQtyAg")); Assert.Equal(0.0, D(l[0], "mintQtyAu")); Assert.Equal(0.0, D(l[0], "mintMonthsAu"));
                Assert.Single(Pick(904, "E"));
                var t1 = st.Where(x => D(x, "seq") == 904 && x["kind"] == "T1").OrderBy(x => D(x, "rank")).ToList();
                Assert.True(t1.Count >= 2);
                Assert.Equal(other.Name, t1[0]["region"]); Assert.Equal(1e9, D(t1[0], "w1"));
                for (int i = 1; i < t1.Count; i++) Assert.True(D(t1[i], "w1") <= D(t1[i - 1], "w1"));
                Assert.True(t1.Count <= 5);
            }
            finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
        }
    }
}
