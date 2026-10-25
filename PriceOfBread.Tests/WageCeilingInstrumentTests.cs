using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(514) 賃金の天井の表 <c>wcap_s</c>・<c>wcapreg_s</c>（読むだけ）の番人。天井の近くに置いた 1 つの州の値段を、表が別の経路で求めた期待と合わせて読む：
/// 位置 s（=ln(値段/(参照×物価水準))/ln(天井の倍率)）・天井側の月の数・縛った州の行・歩が上へ押す力の通る割合 damp（期待は SoftBandLn の有限差分から別に求める＝表の式の逆算と同じ出所にしない）。
/// 軌道が計器で変わらないことは既存の全ビット一致の番人（InstrumentsTests）が同じ hook を通って担う。非空虚の対照＝他の州は縛られていない（c70 は 1 か月分だけ）。</summary>
[Collection("Instruments")]
public class WageCeilingInstrumentTests
{
    private static double Col(string[] h, string[] r, string name)
    {
        int i = System.Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name);
        return r[i] switch { "nan" => double.NaN, "" => double.NaN, "inf" => double.PositiveInfinity, var s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) };
    }

    [Fact]
    public void 天井の近くに置いた州を表が別の経路の期待と合わせて読む()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_wc_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], 1789UL, _ => new UtilityAgent());
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            double expectDamp = double.NaN, expectS = double.NaN;
            try
            {
                sim.StepQuarter();
                Instruments.Quarter(sim, 1789UL, 0);   // 蓄えを戻す（以後の行は手で置いた 1 か月分だけ）
                var w = sim.Nations[1].World; string nation = Instruments.NameOf(w); var c = w.Coef;
                Assert.True(c.IndustrializationOff > 0 && c.SoftBandSharpness > 0, "前提：天井は固定の倍率");
                var reg = w.Regions.First(r => r.Markets[Catalog.LaborId].LastDemand > 0 && r.Pops.Count > 0);
                double ceilMult = c.WageClampMax;   // 工業化度の項は IndustrializationOff で 0
                double lnC = System.Math.Log(ceilMult), lnF = -System.Math.Log(c.WageClampMin), k = c.SoftBandSharpness;
                double wrp = reg.GoodOf(Catalog.LaborId)!.RefPrice * reg.PriceLevel;
                // 歩の入力 t_in=1.28（天井を超える目標）を帯に通した値段を置く＝歩が出す値段そのもの
                double pin = wrp * System.Math.Exp(lnC * 1.28);
                double pobs = MarketFlow.SoftBandLn(pin, wrp, lnF, lnC, k);
                double h = 1e-6;
                expectDamp = System.Math.Log(MarketFlow.SoftBandLn(pin * (1 + h), wrp, lnF, lnC, k) / pobs) / System.Math.Log(1 + h);   // 帯の弾力＝歩が押す力の通る割合
                expectS = System.Math.Log(pobs / wrp) / lnC;
                var m = reg.Markets[Catalog.LaborId];
                m.Price = pobs; m.LastDemand = 10; m.LastProduced = 2; m.LastImport = 0; m.Stock = 0;
                // 対照＝別の州を s=0.8（0.7 以上・0.9 未満）に置く：c70 には数えるが c90・c95 には数えない（閾値の取り違えを赤にする）
                var reg2 = w.Regions.First(r => r != reg && r.Markets[Catalog.LaborId].LastDemand > 0 && r.Pops.Count > 0);
                double wrp2 = reg2.GoodOf(Catalog.LaborId)!.RefPrice * reg2.PriceLevel;
                var m2 = reg2.Markets[Catalog.LaborId];
                double pin2 = wrp2 * System.Math.Exp(lnC * System.Math.Pow(System.Math.Pow(0.8, -k) - 1, -1.0 / k));   // 帯を通すと s=0.8 になる入力
                double pobs2 = MarketFlow.SoftBandLn(pin2, wrp2, lnF, lnC, k);
                expectDamp = (expectDamp + System.Math.Log(MarketFlow.SoftBandLn(pin2 * (1 + h), wrp2, lnF, lnC, k) / pobs2) / System.Math.Log(1 + h)) / 2;   // 2 州の帯の弾力の平均
                m2.Price = pobs2; m2.LastDemand = 10; m2.LastProduced = 2; m2.LastImport = 0; m2.Stock = 0;
                Instruments.WcMonth(w);
                Instruments.Quarter(sim, 1789UL, 1);
            }
            finally { Instruments.Close(sim); }
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "wcap_s1789.tsv"));
            var hd = lines[0].Split('\t');
            Assert.Equal(Instruments.WcHeaderText.Split('\t'), hd);
            var rows = lines.Skip(1).Select(l => l.Split('\t')).ToList();
            Assert.All(rows, r => Assert.Equal(hd.Length, r.Length));
            int qi = System.Array.IndexOf(hd, "quarter"), gi = System.Array.IndexOf(hd, "group"), ni = System.Array.IndexOf(hd, "nation");
            var lab = rows.Where(r => r[qi] == "1" && r[gi] == "labor" && r[ni] != "").ToList();
            Assert.NotEmpty(lab);
            // 英の行（手を入れた州の国）を取る：c70 が 1 か月分ある行
            var hit = lab.Single();
            Assert.Equal(2.0, Col(hd, hit, "cN"));                         // s≥0.7 は 2 州（0.95 と 0.8）
            double live = Col(hd, hit, "liveRegionMonths");
            Assert.True(live >= 2, "前提：生きた州が複数");
            Assert.Equal(2.0 / live, Col(hd, hit, "c70Share"), 9);          // 0.7 以上は 2 州×1 か月
            Assert.Equal(1.0 / live, Col(hd, hit, "c90Share"), 9);
            Assert.Equal(1.0 / live, Col(hd, hit, "c95Share"), 9);
            Assert.Equal(0.0, Col(hd, hit, "overShare"));
            Assert.Equal(1.0 / live, Col(hd, hit, "wantUp90Share"), 9);     // 需要 10 > 供給 2＝まだ上へ押されている
            Assert.Equal(expectS, Col(hd, hit, "maxS"), 8);
            Assert.True(expectS is > 0.9 and < 1.0);
            Assert.Equal(expectDamp, Col(hd, hit, "dampMeanC70"), 4);       // 帯の弾力の有限差分（別の経路）と合う
            Assert.InRange(expectDamp, 0.3, 0.6);                            // 0.95 の州は強く押し戻され（約 0.09）・0.8 の州はほぼ通る（約 0.8）
            Assert.Equal(3.0, Col(hd, hit, "ceilMult"), 9);
            var reglines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, "wcapreg_s1789.tsv"));
            Assert.Equal(Instruments.WcRegHeaderText.Split('\t'), reglines[0].Split('\t'));
            var rh = reglines[0].Split('\t');
            var rr = reglines.Skip(1).Select(l => l.Split('\t')).Where(r => r[System.Array.IndexOf(rh, "quarter")] == "1" && r[System.Array.IndexOf(rh, "group")] == "labor" && Col(rh, r, "c90Months") > 0).ToList();
            Assert.Single(rr);   // 天井側で縛った州の行は手を入れた 1 州だけ（床側の行〔労働は帯の下側が外れている＝参照よりずっと下の州が多い〕は別に出る）
            Assert.Equal(1.0, Col(rh, rr[0], "c90Months"));
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
