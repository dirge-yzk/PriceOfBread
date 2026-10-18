using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>権力の取り分の**部品**（<see cref="PowerShares"/>＝シムが読む純関数）の番人。
/// ★2026-09-26（棚卸し pol-09）：表モデル（階級×機関の表・取り分・軍部の常在項と漏れ・軍政の判定）は SimCore にも画面にも読み手が無く削除した＝
///   その番人（表の合計・「王そのもの」の行・陣営の内生・単調でない参政権・常在項・将校と兵卒・軍事独裁の相対条件・削減率）も一緒に外した。
/// ここで固定するのは残った部品の性質＝①値域[0,1]・退化・NaN・∞で発散しない（否定形ガード）②特権の保全＝農奴制・免税・身分の3本
/// ③恩顧＝関税・発注・秩序の3本 ④議会の掌握＝年次議決の税だけなら1（第109条の逃げ道を塞いだ状態） ⑤文民制度の厚みは制度で厚くなる
/// ⑥兵卒の実質は補給の台帳のパンを数える ⑦純関数＝世界を1ビットも変えない。</summary>
public class PowerSharesTests
{
    private static void AssertPartsInRange(World w, string who)
    {
        foreach (var (name, v) in new[]
        {
            ("特権の保全", PowerShares.PrivilegeIntact(w)), ("恩顧", PowerShares.StatePatronage(w)),
            ("形式上の議会統制", PowerShares.ParliamentControl(w)), ("議会の掌握", PowerShares.ParliamentGrip(w)),
            ("固定歳出", PowerShares.LockedSpending(w)), ("議会の実効", PowerShares.ParliamentEffective(w)),
            ("文民制度", PowerShares.CivilianInstitutions(w)), ("兵卒の実質", PowerShares.SoldierWelfare(w)),
        })
        {
            Assert.False(double.IsNaN(v), $"{who}：{name}が NaN");
            Assert.InRange(v, 0.0, 1.0);
        }
    }

    /// <summary>兵士POPの補給の台帳（食料＝FoodMet/FoodNeed・日用品＝NeedMet/NeedTotal）を充足率で置く（PowerShares.SoldierWelfare の入力）。</summary>
    private static void SetRanksSupply(World w, double food, double daily)
    {
        foreach (var p in w.AllPops())
        {
            if (!p.IsSoldier) continue;
            p.FoodNeed = p.Size; p.FoodMet = p.Size * food;
            p.NeedTotal = p.Size; p.NeedMet = p.Size * daily;
        }
    }

    [Fact]
    public void 全参照国_部品は0から1()
    {
        int n = 0;
        foreach (var cfg in WorldData.Nations())
        {
            AssertPartsInRange(new Simulation(cfg.ToScenario(7), new NullAgent()).World, cfg.Name);
            n++;
        }
        Assert.True(n >= 50, $"参照国が少なすぎる（{n}）＝WorldData の読み込み前提が崩れた");
    }

    [Fact]
    public void 特権の保全は農奴制と免税と身分の3本()
    {
        // ★史学反証：1787-88仏の名士会・高等法院／ヨーゼフ2世／シュタイン改革／1861露農奴解放／1871-76明治の廃藩置県・秩禄処分。
        var w = new Simulation(NationConfig.France().ToScenario(1), new NullAgent()).World;
        w.SerfEmancipation = ReformLevel.None; w.LandTax = 0; w.ProgressiveTax = 0; w.Bureaucracy = BureaucracyLaw.Spoils;
        Assert.Equal(1.0, PowerShares.PrivilegeIntact(w), 9);
        w.SerfEmancipation = ReformLevel.Full; w.LandTax = 0.6; w.ProgressiveTax = 0.4; w.Bureaucracy = BureaucracyLaw.Professional;
        Assert.Equal(0.0, PowerShares.PrivilegeIntact(w), 9);
        w.SerfEmancipation = ReformLevel.Limited; w.LandTax = 0.25; w.ProgressiveTax = 0.25; w.Bureaucracy = BureaucracyLaw.MeritExam;
        Assert.Equal(0.5, PowerShares.PrivilegeIntact(w), 9);   // 3本とも中間段＝連続に効く
    }

    [Fact]
    public void 恩顧は関税と発注と秩序の3本()
    {
        // ★史学反証：第二帝政のクレディ・モビリエ／1879独「鉄とライ麦」／明治の政商／露十月党。
        var w = new Simulation(NationConfig.Britain().ToScenario(1), new NullAgent()).World;
        w.Tariff = 0; w.ArmyUpkeep = new Share(0); w.NavyUpkeep = new Share(0); w.PublicOrder = 0;
        Assert.Equal(0.0, PowerShares.StatePatronage(w), 9);
        w.Tariff = 1.0; w.ArmyUpkeep = new Share(1.0); w.NavyUpkeep = new Share(0); w.PublicOrder = 1.0;
        Assert.Equal(1.0, PowerShares.StatePatronage(w), 9);
    }

    [Fact]
    public void 年次議決の税だけなら議会の掌握は1()
    {
        // ★史学検証4（1850年プロイセン憲法）：第109条で常設の税は存続＝議会が噛めるのは年次議決の税と起債だけ。
        var c = new Coefficients();
        var rep = new Simulation(NationConfig.France().ToScenario(1), new NullAgent()).World;
        CrownParliament.ApplyRegime(rep, c, Polity.Republic);          // 統制力1.0
        rep.TaxRate = new Share(1.0); rep.ProgressiveTax = 1.0; rep.PollTax = 0; rep.LandTax = 0; rep.SetStateMonopolyTier(0);
        rep.Tariff = 0; rep.ArmyUpkeep = new Share(0); rep.NavyUpkeep = new Share(0);
        Assert.Equal(1.0, PowerShares.ParliamentGrip(rep), 9);
        Assert.Equal(PowerShares.ParliamentControl(rep) * PowerShares.ParliamentGrip(rep) * (1.0 - PowerShares.LockedSpending(rep)),
            PowerShares.ParliamentEffective(rep), 12);
        rep.TaxRate = new Share(0); rep.ProgressiveTax = 0; rep.LandTax = 1.0;    // 常設の税だけ＝第109条の逃げ道
        rep.GovDebt = new MoneyStock(0);                                                // 起債への依存も無い
        Assert.Equal(0.0, PowerShares.ParliamentGrip(rep), 9);
    }

    [Fact]
    public void 文民制度の厚みは議会と官僚と政党と正統性で厚くなる()
    {
        // ★史学検証3の修正④：純軍政の割合は普遍でなく文民制度の厚みで決まる（アンデス三国約70% vs スペイン14%・ブラジル8%）。
        //   ⚠国別の定数を1つも置かない＝地域差は**既存の制度の値の差**として出る。
        var w = new Simulation(NationConfig.Prussia().ToScenario(2), new NullAgent()).World;
        w.ParliamentControl = 0; w.Bureaucracy = BureaucracyLaw.Spoils; w.Suffrage = Suffrage.None; w.Legitimacy = 0;
        double thin = PowerShares.CivilianInstitutions(w);
        w.ParliamentControl = 1.0; w.Bureaucracy = BureaucracyLaw.Professional; w.Suffrage = Suffrage.Universal; w.Legitimacy = 1.0;
        w.TaxRate = new Share(1.0); w.ProgressiveTax = 0; w.PollTax = 0; w.LandTax = 0; w.SetStateMonopolyTier(0); w.Tariff = 0;   // 歳入の掌握＝1（専売は対象0・2026-09-20）
        double thick = PowerShares.CivilianInstitutions(w);
        Assert.True(thick > thin, "議会・官僚・政党・正統性を厚くしても文民制度の厚みが増えない");
        Assert.InRange(thin, 0.0, 1.0);
        Assert.True(thick > 0.9, $"4本とも満点に近いはず（{thick:F3}）");   // 硬性条件の国が構造的に満たす向き
    }

    [Fact]
    public void 兵卒の実質は補給の台帳のパンを数える()
    {
        // ★1917ペトログラード守備隊：給与は払われ日用品は買えたが、パンが無かった。★階層3分割（2026-09-22 既定化）の後は
        //   満足度の台帳に食料が載らない＝満足度を読んでいたら、パンが無くても実質は満額のまま（Military の即応度が読む）。
        var w = new Simulation(NationConfig.Russia().ToScenario(2), new NullAgent()).World;
        SetRanksSupply(w, food: 1.0, daily: 1.0);
        Assert.Equal(1.0, PowerShares.SoldierWelfare(w), 9);
        SetRanksSupply(w, food: 0.0, daily: 1.0);
        Assert.True(PowerShares.SoldierWelfare(w) < 1.0, $"パンが無いのに兵卒の実質が満額（{PowerShares.SoldierWelfare(w)}）＝兵卒の実質がパンを数えていない");
        var empty = new Simulation(new Scenario { Seed = 1 }, new NullAgent()).World;   // 兵士POPが居ない世界＝中立の1.0
        Assert.Equal(1.0, PowerShares.SoldierWelfare(empty), 12);
    }

    [Fact]
    public void 退化世界_発散せずNaNも無限大も出ない()
    {
        AssertPartsInRange(new Simulation(new Scenario { Seed = 1 }, new NullAgent()).World, "退化");   // 最小世界＝兵士も資産も発言権も無い
        var w2 = new Simulation(NationConfig.France().ToScenario(5), new NullAgent()).World;
        // NaN の注入（Math.Clamp は NaN を除去しない＝否定形ガードの番人）
        w2.Legitimacy = double.NaN; w2.ParliamentControl = double.NaN; w2.ArmyUpkeep = new Share(double.NaN);
        w2.Tariff = double.NaN; w2.PublicOrder = double.NaN; w2.LandTax = double.NaN; w2.ProgressiveTax = double.NaN; w2.TaxRate = new Share(double.NaN);
        foreach (var p in w2.AllPops()) { p.FoodMet = double.NaN; p.NeedMet = double.NaN; }
        AssertPartsInRange(w2, "NaN");
        w2.Legitimacy = double.PositiveInfinity; w2.ParliamentControl = double.PositiveInfinity; w2.ArmyUpkeep = new Share(double.PositiveInfinity);
        AssertPartsInRange(w2, "∞");
    }

    [Fact]
    public void 純関数_同じ状態から2回呼ぶとビット一致し_Worldを1ビットも変えない()
    {
        var w = new Simulation(NationConfig.Britain().ToScenario(11), new NullAgent()).World;
        double leg = w.Legitimacy, pc = w.ParliamentControl, tar = w.Tariff, ord = w.PublicOrder, au = w.ArmyUpkeep.Value, nu = w.NavyUpkeep.Value;
        var suffrage = w.Suffrage; var serf = w.SerfEmancipation;
        double[] Parts() =>
        [
            PowerShares.PrivilegeIntact(w), PowerShares.StatePatronage(w), PowerShares.ParliamentControl(w), PowerShares.ParliamentGrip(w),
            PowerShares.LockedSpending(w), PowerShares.ParliamentEffective(w), PowerShares.CivilianInstitutions(w), PowerShares.SoldierWelfare(w),
        ];
        var a = Parts(); var b = Parts();
        for (int i = 0; i < a.Length; i++) Assert.Equal(a[i], b[i]);   // 精度指定なし＝厳密一致
        Assert.Equal(leg, w.Legitimacy); Assert.Equal(pc, w.ParliamentControl); Assert.Equal(tar, w.Tariff);
        Assert.Equal(ord, w.PublicOrder); Assert.Equal(au, w.ArmyUpkeep.Value); Assert.Equal(nu, w.NavyUpkeep.Value);
        Assert.Equal(suffrage, w.Suffrage); Assert.Equal(serf, w.SerfEmancipation);
    }
}
