using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(574) 線 svf＝開幕の貨幣の量（読む線 sv〔(564)〕・事前登録 measurements/svf574_20261026_prereg.txt）の番人。
/// 規則＝旗 <see cref="Coefficients.OpeningMoneyFromRatioMode"/> が1以上のとき、Simulation の構築子が全 Pop の貯蓄を国ごとの1つの倍率で下げ、
/// 構築直後の 貯蓄の和÷年名目GDP を <see cref="NationConfig.BroadMoneyToGdpYear"/> にする（<see cref="World.RescaleOpeningSavings"/>）。モード2は (332) の国債の年GDP を価格水準1で測る。
/// 期待値は式の出力から作らない：m は下の手書きの表（(221) の4階・344f8883 の world.json から写した）／貯蓄の和は Pop を直接走査／年GDP は 12× を試験側で書く（月と年の取り違えを捕まえる）／
/// 構築の直書き（資本家 3200・農民 200・工場労働者 300）は試験側の定数。
/// 突然変異：①年GDP に月の名目GDP をそのまま使う（12倍ずれる）→61国の比が赤／②一部の階級（閉じた階級）だけに掛ける→倍率が全 Pop に同じの検査が赤／
/// ③モード2でも名目で置く→国債の検査が赤／④旗0でも当てる→直書きの検査が赤／⑤建国の持ち込み州の門を外す→持ち込み州の検査が赤。
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
    public void 全61国_構築直後の貨幣と年名目GDPの比が国のデータちょうど()
    {
        var bad = new List<string>();
        foreach (var cfg in WorldData.Nations())
        {
            var w = Build(cfg, 1).World;
            double m = Expected(cfg.Name);
            double gdpYear = 12.0 * w.GdpPerMonth.Value * w.PriceLevel;   // 試験側で月→年（12×）と価格水準を書く＝本体が月の値をそのまま年に使う取り違えを捕まえる
            double ratio = SumSavings(w) / gdpYear;
            Assert.True(gdpYear > 0, $"{cfg.Name}：年GDP が正でない");
            Assert.Equal(1.0, w.PriceLevel);   // 構築直後は価格水準 1（この試験の前提）
            if (!(System.Math.Abs(ratio - m) <= 1e-9 * m)) bad.Add($"{cfg.Name}：貨幣÷年GDP {ratio:F6}（目標 {m}）");
        }
        Assert.True(bad.Count == 0, string.Join("\n", bad));
    }

    [Fact]
    public void 倍率は国の全Popに同じ_階級の相対配分と人口は保たれる()
    {
        var cfg = WorldData.ByName("イギリス");
        var w0 = Build(cfg, 0).World; var w1 = Build(cfg, 1).World;
        var p0 = w0.Regions.SelectMany(r => r.Pops).ToList(); var p1 = w1.Regions.SelectMany(r => r.Pops).ToList();
        Assert.Equal(p0.Count, p1.Count);
        double? lambda = null; int checkedPops = 0;
        for (int i = 0; i < p0.Count; i++)
        {
            Assert.Equal(p0[i].Size, p1[i].Size);   // 人口には触れない
            Assert.Equal(p0[i].ClassId, p1[i].ClassId);
            if (!(p0[i].Savings.Value > 0)) { Assert.Equal(0.0, p1[i].Savings.Value); continue; }
            double k = p1[i].Savings.Value / p0[i].Savings.Value;
            lambda ??= k;
            Assert.True(System.Math.Abs(k - lambda.Value) <= 1e-12 * lambda.Value, $"Pop {i}（階級 {p0[i].ClassId}）の倍率 {k} が最初の {lambda} と違う");
            checkedPops++;
        }
        Assert.True(checkedPops >= 10, $"貯蓄が正の Pop が少なすぎる（{checkedPops}）");
        Assert.True(lambda < 0.2, $"英の倍率が小さくない（{lambda}）＝基線の貨幣は目標の約33倍");
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
    public void 目標が0の国と建国の持ち込み州には当てない()
    {
        // m＝0（データの欠け）の国は何もしない
        var zero = WorldData.ByName("イギリス").ShallowClone(); zero.BroadMoneyToGdpYear = 0;
        double off = SumSavings(Build(zero, 0).World), on = SumSavings(Build(zero, 1).World);
        Assert.Equal(off, on);
        // 建国の持ち込み州（PrebuiltRegions）は貯蓄を運んでくる＝当てない（旗1でも持ち込んだ貯蓄が1ビットも変わらない）
        var cfg = WorldData.ByName("イギリス");
        var donor = Build(cfg, 0);
        var regions = donor.World.Regions.ToList();
        double carried = SumSavings(donor.World);
        var sc = new Scenario { Seed = 1, Kind = ScenarioKind.Nation, NationConfig = cfg, PrebuiltRegions = regions, Coef = new Coefficients { OpeningMoneyFromRatioMode = 1 }, SharedWorld = true };
        var founded = new Simulation(sc, new NullAgent());
        Assert.Equal(carried, SumSavings(founded.World));
    }

    [Fact]
    public void 否定形の守り_目標が正でない_NaN_貯蓄0なら何もしない()
    {
        var w = Build(WorldData.ByName("イギリス"), 0).World;
        double before = SumSavings(w);
        Assert.Equal(1.0, w.RescaleOpeningSavings(0));
        Assert.Equal(1.0, w.RescaleOpeningSavings(-0.5));
        Assert.Equal(1.0, w.RescaleOpeningSavings(double.NaN));
        Assert.Equal(before, SumSavings(w));
        double lambda = w.RescaleOpeningSavings(0.5);   // 実際に当てると目標の比になる（試験側の 12× で検算）
        Assert.True(lambda > 0 && lambda < 1);
        Assert.True(System.Math.Abs(SumSavings(w) / (12.0 * w.GdpPerMonth.Value * w.PriceLevel) - 0.5) <= 1e-9 * 0.5);
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
