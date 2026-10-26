using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(574) 線 svf＝開幕の貨幣の量（読む線 sv〔(564)〕・事前登録 measurements/svf574_20261026_prereg.txt〔追記 2 で形を直した〕）の番人。
/// 規則＝旗 <see cref="Coefficients.OpeningMoneyFromRatioMode"/> が1以上のとき、助走の各四半期の末の頭（<c>Simulation.QuarterEnd</c>）に全 Pop の貯蓄を国ごとの1つの倍率で戻し、
/// 貯蓄の和÷年名目GDP を <see cref="NationConfig.BroadMoneyToGdpYear"/> にする（<see cref="World.RescaleOpeningSavings"/>）。モード2は (332) の国債の年GDP を価格水準1で測る。
/// ⚠最初の版は構築子で当てた＝構築直後の産出は生産者ごとの置き定数の和で、最初の四半期の後の産出と国ごとに 1.7〜14 倍ずれ、比が 1 桁違った（追記 2）。試験が構築直後の年GDP に対して緑でも意味が無かった＝
/// 今の試験は助走を実際に回して、**走っている経済の産出**に対する比を見る。
/// 期待値は式の出力から作らない：m は下の手書きの表（(221) の4階・344f8883 の world.json から写した）／貯蓄の和は Pop を直接走査／年GDP は 12× を試験側で書く（月と年の取り違えを捕まえる）／
/// 構築の直書き（資本家 3200・農民 200・工場労働者 300）は試験側の定数。
/// 突然変異：①年GDP に月の名目GDP をそのまま使う（12倍ずれる）→関数の検査が赤／②一部の階級（閉じた階級）だけに掛ける→倍率が全 Pop に同じの検査が赤／
/// ③モード2でも名目で置く→国債の検査が赤／④旗0でも当てる→旗0の検査が赤／⑤建国の持ち込み州の門を外す→持ち込み州の検査が赤／⑥四半期の末の頭でなく物価の更新の後に当てる→趨勢の検査が赤。
/// </summary>
public class OpeningMoneyRatioTests
{
    // 手書きの表（(221) の4階）。既定の 0.1 の国は表に無い。
    private static readonly string[] Tier50 = ["イギリス", "ネーデルラント"];
    private static readonly string[] Tier35 = ["フランス", "スペイン", "サルデーニャ＝ピエモンテ", "スウェーデン", "デンマーク", "スイス", "ポルトガル", "ジェノヴァ", "トスカーナ", "ヴェネツィア"];
    private static readonly string[] Tier20 = ["日本", "アメリカ", "ロシア", "プロイセン", "オスマン", "オーストリア", "清", "朝鮮", "ポーランド", "ライン諸邦", "両シチリア", "教皇領", "バイエルン", "ザクセン", "ハノーファー", "ヴュルテンベルク"];

    private static double Expected(string name) => Tier50.Contains(name) ? 0.5 : Tier35.Contains(name) ? 0.35 : Tier20.Contains(name) ? 0.2 : 0.1;

    private static Simulation Build(NationConfig cfg, int mode, ulong seed = 1789UL)
    {
        var sc = cfg.ToScenario(seed, sharedWorld: true);
        sc.Coef.OpeningMoneyFromRatioMode = mode;   // 国ごとの係数は Scenario が持つ（World.Coef は国ごとに別インスタンス）
        return new Simulation(sc, new NullAgent());
    }

    private static double SumSavings(World w) { double s = 0; foreach (var r in w.Regions) foreach (var p in r.Pops) s += p.Savings.Value; return s; }

    /// <summary>試験側の 貯蓄の和÷年名目GDP（年GDP＝12×実質の月次GDP×価格水準を試験側で書く）。</summary>
    private static double MoneyToGdpYear(World w) => SumSavings(w) / (12.0 * w.GdpPerMonth.Value * w.PriceLevel);

    /// <summary>61 国を組み、旗を立てて（または立てずに）助走の最初の四半期だけ回し、その直後の（比・趨勢・価格水準）を国ごとに返す。</summary>
    private static Dictionary<string, (double Ratio, double Trend, double P)> AfterFirstWarmupQuarter(int mode)
    {
        var sim = new MultiNationSim(WorldData.Nations(), 1789UL, _ => new NullAgent());
        foreach (var n in sim.Nations) n.World.Coef.OpeningMoneyFromRatioMode = mode;
        var got = new Dictionary<string, (double, double, double)>();
        sim.StartWarmup(() =>
        {
            if (got.Count > 0) return;   // 最初の四半期の直後だけ
            foreach (var n in sim.Nations) got[n.World.SelfPower!.Name] = (MoneyToGdpYear(n.World), n.World.MoTrendForInstruments, n.World.PriceLevel);
        }, 1);
        return got;
    }

    [Fact]
    public void データは4階の表どおり_61国_既定は最頻の0_1()
    {
        var nations = WorldData.Nations();
        Assert.Equal(61, nations.Length);
        Assert.Equal(0.1, new NationConfig().BroadMoneyToGdpYear);
        var bad = nations.Where(c => c.BroadMoneyToGdpYear != Expected(c.Name)).Select(c => $"{c.Name}={c.BroadMoneyToGdpYear}（表 {Expected(c.Name)}）").ToList();
        Assert.True(bad.Count == 0, string.Join("、", bad));
        Assert.Equal(2, nations.Count(c => c.BroadMoneyToGdpYear == 0.5));
        Assert.Equal(10, nations.Count(c => c.BroadMoneyToGdpYear == 0.35));
        Assert.Equal(16, nations.Count(c => c.BroadMoneyToGdpYear == 0.2));
        Assert.Equal(33, nations.Count(c => c.BroadMoneyToGdpYear == 0.1));
        Assert.Equal(28, Tier50.Length + Tier35.Length + Tier20.Length);   // 表に載る国数 2+10+16（残る 33 国は既定の 0.1）
    }

    [Fact]
    public void 全61国_助走の最初の四半期の末に貨幣と年名目GDPの比が国のデータへ戻る_旗0では桁違い()
    {
        var on = AfterFirstWarmupQuarter(1);
        var off = AfterFirstWarmupQuarter(0);
        Assert.Equal(61, on.Count); Assert.Equal(61, off.Count);
        var bad = new List<string>();
        foreach (var cfg in WorldData.Nations())
        {
            double m = Expected(cfg.Name);
            var (ratio, _, _) = on[cfg.Name];
            if (!(System.Math.Abs(ratio / m - 1.0) <= 0.10)) bad.Add($"{cfg.Name}：旗1の比 {ratio:F4}（目標 {m}・±10%）");
            double control = off[cfg.Name].Ratio;
            if (!(control >= 5.0 * m)) bad.Add($"{cfg.Name}：旗0の比 {control:F3} が目標の 5 倍に届かない（試験が旗の有無を見分けられない）");
        }
        Assert.True(bad.Count == 0, string.Join("\n", bad));
    }

    [Fact]
    public void 最初の物価の更新の趨勢は戻した後の貨幣で取る()
    {
        // 趨勢＝貨幣÷月次実質GDP（ヶ月）の遅い平均。最初の更新は趨勢の基準を取るだけ＝基準は貨幣を戻した後の比（≒12×m）でなければならない。
        // 戻す前の比で基準を取ると（物価の更新の後に当てる）趨勢は 12×m の 5 倍超に残り、cyclical が 1/5 以下になって物価が床へ落ちる。
        var got = AfterFirstWarmupQuarter(1);
        var bad = new List<string>();
        foreach (var cfg in WorldData.Nations())
        {
            double m = Expected(cfg.Name); var (_, trend, p) = got[cfg.Name];
            double want = 12.0 * m;
            if (!(trend >= 0.8 * want && trend <= 1.3 * want)) bad.Add($"{cfg.Name}：趨勢 {trend:F3}（12×m＝{want:F3} の 0.8〜1.3 倍）");
            if (!(p >= 0.5 && p <= 2.0)) bad.Add($"{cfg.Name}：最初の四半期の末の価格水準 {p:F3}");
        }
        Assert.True(bad.Count == 0, string.Join("\n", bad));
    }

    [Fact]
    public void 倍率は国の全Popに同じ_階級の相対配分と人口は保たれ_目標の比ちょうどになる()
    {
        var cfg = WorldData.ByName("イギリス");
        var w0 = Build(cfg, 0).World; var w1 = Build(cfg, 0).World;
        double lambda = w1.RescaleOpeningSavings(0.5);   // 関数だけを直接呼ぶ（その時点の産出に対して比ちょうどにすることを見る）
        Assert.True(lambda > 0 && lambda < 1);
        Assert.True(System.Math.Abs(MoneyToGdpYear(w1) - 0.5) <= 1e-9 * 0.5);
        var p0 = w0.Regions.SelectMany(r => r.Pops).ToList(); var p1 = w1.Regions.SelectMany(r => r.Pops).ToList();
        Assert.Equal(p0.Count, p1.Count);
        int checkedPops = 0;
        for (int i = 0; i < p0.Count; i++)
        {
            Assert.Equal(p0[i].Size, p1[i].Size);   // 人口には触れない
            if (!(p0[i].Savings.Value > 0)) { Assert.Equal(0.0, p1[i].Savings.Value); continue; }
            double k = p1[i].Savings.Value / p0[i].Savings.Value;
            Assert.True(System.Math.Abs(k - lambda) <= 1e-12 * lambda, $"Pop {i}（階級 {p0[i].ClassId}）の倍率 {k} が λ={lambda} と違う");
            checkedPops++;
        }
        Assert.True(checkedPops >= 10, $"貯蓄が正の Pop が少なすぎる（{checkedPops}）");
    }

    [Fact]
    public void 旗0は構築の直書きのまま_資本家3200_農民200_工場労働者300()
    {
        var w = Build(WorldData.ByName("イギリス"), 0).World;
        int nBourgeois = 0, nPeasant = 0, nWorker = 0;
        foreach (var r in w.Regions) foreach (var p in r.Pops)
        {
            if (!(p.Savings.Value > 0) || !(p.Size > 0)) continue;   // 植民地の器（Size0・貯蓄0）は除く
            double per = p.Savings.Value / p.Size;
            if (p.ClassId == 3) { Assert.Equal(3200.0, per, 6); nBourgeois++; }
            else if (p.ClassId == 1) { Assert.Equal(200.0, per, 6); nPeasant++; }
            else if (p.ClassId == 2) { Assert.Equal(300.0, per, 6); nWorker++; }
        }
        Assert.True(nBourgeois > 0 && nPeasant > 0 && nWorker > 0, $"直書きの階級が見つからない（資本家 {nBourgeois}・農民 {nPeasant}・工場労働者 {nWorker}）");
    }

    [Fact]
    public void 目標が0の国と建国の持ち込み州は読み手の値が0_否定形の守り()
    {
        var cfg = WorldData.ByName("イギリス");
        // 通常の国は国のデータを写す・建国の持ち込み州（PrebuiltRegions）は 0（貯蓄を運んでくる国に当てない）
        var normal = Build(cfg, 1).World;
        Assert.Equal(0.5, normal.OpeningBroadMoneyToGdpYear);
        var donor = Build(cfg, 0);
        var sc = new Scenario { Seed = 1, Kind = ScenarioKind.Nation, NationConfig = cfg, PrebuiltRegions = donor.World.Regions.ToList(), Coef = new Coefficients { OpeningMoneyFromRatioMode = 1 }, SharedWorld = true };
        Assert.Equal(0.0, new Simulation(sc, new NullAgent()).World.OpeningBroadMoneyToGdpYear);
        // 関数の否定形：目標が正でない・NaN なら何もしない
        var w = Build(cfg, 0).World;
        double before = SumSavings(w);
        Assert.Equal(1.0, w.RescaleOpeningSavings(0));
        Assert.Equal(1.0, w.RescaleOpeningSavings(-0.5));
        Assert.Equal(1.0, w.RescaleOpeningSavings(double.NaN));
        Assert.Equal(before, SumSavings(w));
    }

    [Fact]
    public void モード2は国債の年GDPを価格水準1で測る_モード0と1は名目のまま()
    {
        var cfg = WorldData.ByName("イギリス");
        Assert.True(cfg.GovDebtToGdpYear > 0 && cfg.TreasuryMonths > 0);
        foreach (int mode in new[] { 0, 1, 2 })
        {
            var w = Build(cfg, mode).World;
            w.PriceLevel = 1.5;   // 助走で物価が動いた盤面
            w.ForeignDebt = default;
            w.RebaseOpeningFiscal(7.0);
            double realYear = 12.0 * w.GdpPerMonth.Value;   // 価格水準1の年GDP（試験側の 12×）
            double want = cfg.GovDebtToGdpYear * realYear * (mode >= 2 ? 1.0 : 1.5);
            Assert.True(System.Math.Abs(w.GovDebt.Value - want) <= 1e-9 * want, $"モード {mode}：国債 {w.GovDebt.Value}（期待 {want}）");
        }
    }
}
