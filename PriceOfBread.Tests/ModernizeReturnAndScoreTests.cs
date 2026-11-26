using System;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (416) 線 sc の番人（事前登録 measurements/sc_20261019_prereg.txt [6]）。
/// ①AI の近代化投資を止める信号（旗 <c>AiModernizeFromReturn</c> は (591) で旧の腕ごと消した）を、集計の工業化度の天井でなく設備の限界収益（私的な投資家と同じ式）から出す。
/// ②国力＝世界の総量に対する本国の割合の 6 項（旗 <c>ScoreFromWorldShare</c> は (456) で焼き込んだ）。
/// 期待値は手計算の数値のリテラルで書く（実装の関数から作らない＝同語反復にしない）。
/// </summary>
public class ModernizeReturnAndScoreTests
{
    private static MultiNationSim Four()
        => new([NationConfig.France(), NationConfig.Japan(), NationConfig.Britain(), NationConfig.America()], seed: 1789);

    /// <summary>限界収益の式の係数を事前登録 [6] M1 の値に固定し、techProd を 1.0 に（技術水準 0・労働時間の疲労なし・労働組合なし・罷業なし）、州の資本をそろえる。</summary>
    private static World Neutral(MultiNationSim sim, int idx, double industrialization)
    {
        var w = sim.Nations[idx].World;
        var c = w.Coef;
        c.IndProductivityGain = 1.2; c.IndProductivityScale = 2.5; c.IndCapitalCost = 0.7; c.IndTechLeverage = 2.4;
        c.CrowdingOutWeight = 1.0; c.EquityHeatEuphoria = 0.05;
        c.LaborFatigueExp = 1.0; c.LaborFatiguePenalty = 0; c.StrikeProductionHit = 0;
        w.LaborUnion = LaborUnionLaw.Illegal;
        w.TechLevel = 0;
        foreach (var r in w.Regions) { r.Industrialization = industrialization; r.OldVintageCapital = 0; r.StrandedVintage = 0; r.CrowdingPremium = 0; r.FinancialHeat = 0; }
        return w;
    }

    // ================= M1 設備の限界収益 =================

    [Fact]
    public void 設備の収益の持ち分は手計算の値になる()
    {
        var w = Neutral(Four(), 0, 0);
        var r = w.Regions.First();
        var c = w.Coef;
        // I=0・技術の梃子 0：gross＝1.2÷1²＝1.2・hurdle＝0.7 → 1−0.7÷1.2
        Assert.Equal(0.41666666666666663, EconomyStep.ReturnShare(r, c, 1.0), 12);
        // I=0・techProd=2（techLev=1）：gross＝1.2×(1＋2.4)＝4.08 → 1−0.7÷4.08
        Assert.Equal(0.8284313725490196, EconomyStep.ReturnShare(r, c, 2.0), 12);
        // I=1.0・技術の梃子 0：sat＝1.4・gross＝1.2÷1.96＝0.6122…＜0.7（ハードルを割る）→ 0
        r.Industrialization = 1.0;
        Assert.Equal(0.0, EconomyStep.ReturnShare(r, c, 1.0));
        // 締め出し：CrowdingPremium＝0.2（重み 1）でハードルが 0.9 に上がる → 1−0.9÷1.2
        r.Industrialization = 0; r.CrowdingPremium = 0.2;
        Assert.Equal(0.25, EconomyStep.ReturnShare(r, c, 1.0), 12);
        // 金融の熱はハードルを下げる：FinancialHeat＝1.0（係数 0.05）で hurdle＝0.7＋0.2−0.05＝0.85
        r.FinancialHeat = 1.0;
        Assert.Equal(1.0 - 0.85 / 1.2, EconomyStep.ReturnShare(r, c, 1.0), 12);
        // NaN の入力は 0（fail-closed）
        r.CrowdingPremium = 0; r.FinancialHeat = 0;
        c.IndProductivityGain = double.NaN;
        Assert.Equal(0.0, EconomyStep.ReturnShare(r, c, 1.0));
    }

    [Fact]
    public void 私的な投資家の建設の式と信号は同じ関数を通る()
    {
        // EconomyStep.Run の私的投資の netReturn＝gross−hurdle（InvestmentReturn）。信号は同じ関数の gross と hurdle から割合を作る。
        var w = Neutral(Four(), 0, 0.5);
        var r = w.Regions.First();
        var (techLev, sat, gross, hurdle) = EconomyStep.InvestmentReturn(r, w.Coef, 1.5);
        Assert.Equal(0.5, techLev, 12);
        Assert.Equal(1.2, sat, 12);                                         // 1＋0.5÷2.5
        Assert.Equal(1.2 / 1.44 * (1.0 + 2.4 * 0.5), gross, 12);            // 1.2÷sat²×(1＋2.4×0.5)
        Assert.Equal(0.7, hurdle, 12);
        Assert.Equal(1.0 - 0.7 / gross, EconomyStep.ReturnShare(r, w.Coef, 1.5), 12);
    }

    [Fact]
    public void 工場の生産性の乗数の抜き出しは技術の逓減の式のままである()
    {
        var w = Neutral(Four(), 0, 0);
        var r = w.Regions.First();
        // 疲労なし・組合なし・罷業なし：techProd＝1＋min(T,2)＋max(0,T−2)×0.3
        w.TechLevel = 0.0; Assert.Equal(1.0, w.TechProductivity(r), 12);
        w.TechLevel = 1.5; Assert.Equal(2.5, w.TechProductivity(r), 12);
        w.TechLevel = 2.0; Assert.Equal(3.0, w.TechProductivity(r), 12);
        w.TechLevel = 4.0; Assert.Equal(3.6, w.TechProductivity(r), 12);   // 3＋(4−2)×0.3
        // 労働組合の容認は産出に掛かる（係数を 0.8 に置く）
        //   ★(520) 組合の産出の乗数は旗 UnionBargaining が正（既定 1）のとき 1.0＝交渉の取り分が代わりを務める。ここは乗数の抜き出しの式そのものを見る番人なので 旗 0 の腕を明示する（既定の盤面は SbBargainTests の U6 が見る）。
        w.Coef.UnionBargaining = 0;
        w.TechLevel = 0.0; w.LaborUnion = LaborUnionLaw.Tolerated; w.Coef.LaborUnionToleratedOutputMul = 0.8;
        Assert.Equal(0.8, w.TechProductivity(r), 12);
    }

    // ================= M3 信号の国の値（按分の重みつき平均） =================

    /// <summary>州の建設材の手当を 4 材とも同じ単位数にそろえる（重みは上限までの余地だけで決まる）。</summary>
    private static void EqualStock(World w, Region r)
    {
        var c = w.Coef;
        var bn = EconomyStep.BundleOf(r, c);   // ★(555) 今の建設の束の財（既定は建造物＋機械・旗 0 なら鋼・機械・セメント・製材）の手当を同じ単位数にそろえる
        r.Markets[bn.G0].Stock = 100 * bn.Q0; r.Markets[bn.G1].Stock = 100 * bn.Q1;
        if (bn.Slots == 4) { r.Markets[bn.G2].Stock = 100 * bn.Q2; r.Markets[bn.G3].Stock = 100 * bn.Q3; }
    }

    [Fact]
    public void 適格の州が無ければ信号は0で_重みは上限までの余地と手当で決まる()
    {
        var w = Neutral(Four(), 0, 5.0);   // 全州が州の上限 5.0 に達している
        Assert.Equal(0.0, w.ModernizeReturnShare());
        var act = w.Regions.Where(x => x.CapitalEnabled && x.InvestorPopIndex >= 0).Take(2).ToList();
        Assert.True(act.Count == 2, "前提が崩れた：資本区画つきの活きた州が 2 つ要る");
        foreach (var r in act) EqualStock(w, r);
        act[0].Industrialization = 0.0;   // 信号 0.41666…（techProd＝1）・余地 5.0
        act[1].Industrialization = 1.0;   // 信号 0（ハードルを割る）・余地 4.0
        // 重み 5u と 4u（u は同じ手当）→ (5×0.416666…＋4×0)÷9
        Assert.Equal(5.0 * 0.41666666666666663 / 9.0, w.ModernizeReturnShare(), 12);
        // 片方の手当を 0 にすると重みは 0（その州には注がない）→ 残りの州の値
        foreach (var m in new[] { w.Coef.SteelGoodId, w.Coef.MachineGoodId, w.Coef.CementGoodId, w.Coef.LumberGoodId }) act[1].Markets[m].Stock = 0;
        Assert.Equal(0.41666666666666663, w.ModernizeReturnShare(), 12);
        // どの州も手当が 0（重みが全て 0）なら均等平均（等分の枝と同じ規則）→ (0.41666…＋0)÷2
        foreach (var m in new[] { w.Coef.SteelGoodId, w.Coef.MachineGoodId, w.Coef.CementGoodId, w.Coef.LumberGoodId }) act[0].Markets[m].Stock = 0;
        Assert.Equal(0.41666666666666663 / 2.0, w.ModernizeReturnShare(), 12);
    }

    // ================= M2 AI とプレイヤーの対称 =================

    private static void SetAllIndustrialization(World w, double v)
    {
        foreach (var r in w.Regions) { r.Industrialization = v; r.OldVintageCapital = 0; r.StrandedVintage = 0; }
    }

    [Fact]
    public void 信号は設備の収益だけで_世界前線に依らない()
    {
        // 信号は設備の収益だけ。前線（IndustrialFrontier）を読まない＝前線が 0 でも 50 でも同じ率（(591) で旧の包絡 max(2.5, α×前線) の腕を消した）。
        var w1 = Neutral(Four(), 0, 0.5);
        w1.IndustrialFrontier = 0; double a1 = UtilityAgent.AutoModernizationFrac(w1, 0);
        w1.IndustrialFrontier = 50; double b1 = UtilityAgent.AutoModernizationFrac(w1, 0);
        Assert.Equal(a1, b1);
        Assert.True(a1 > 0, "前提が崩れた：I＝0.5・技術 0 で信号が立たない");
        // I＝0.5・techProd＝1：sat＝1.2・gross＝1.2÷1.44＝0.8333…・信号＝1−0.7÷0.8333…＝0.16 → 率＝0.16×(1−0)×ModernizeInvestFrac
        Assert.Equal(0.16 * 0.08, a1, 9);   // ModernizeInvestFrac＝0.08（近代化スタンスの率〔SelfPlay の定数〕）
        // 集計の工業化度が高くても（旧の天井 2.5 を超えても）、設備の収益が立つなら止まらない。収益が立たない（I＝1.0 でハードルを割る）なら 0。
        var w2 = Neutral(Four(), 0, 3.0);
        w2.TechLevel = 10.0;   // techProd＝5.4 → 収益が立つ（下のダンピングの試験と同じ前提）
        Assert.True(UtilityAgent.AutoModernizationFrac(w2, 0) > 0, "工業化度 3.0 でも技術が高ければ設備の収益は立つ＝止まらない");
        var w3 = Neutral(Four(), 0, 1.0);
        Assert.Equal(0.0, UtilityAgent.AutoModernizationFrac(w3, 0));
    }

    [Fact]
    public void AIの近代化投資と自動化は同じ信号を読む()
    {
        // Apply（AI）と AutoModernizationFrac（NullAgent の既定式）が同じ盤面で同じ率を置く（片方だけ換えると新しい非対称ができる）。
        var sim = Four();
        var w = Neutral(sim, 0, 0.5);
        w.SetTreasuryRaw(1e12);   // 財政逼迫 0
        double fs = UtilityAgent.FiscalStressOf(w);
        UtilityAgent.Apply(w, UtilityAgent.Stance.Modernize);
        Assert.Equal(UtilityAgent.AutoModernizationFrac(w, fs), w.ModernizationFrac, 12);
        Assert.True(w.ModernizationFrac > 0, "前提が崩れた：率が 0 では対称を見ていない");
    }

    [Fact]
    public void ダンピングの後発判定は設備の収益の信号である()
    {
        // 工業化度 3.0（旧の後発判定 <2.5 を超える）でも、技術の高い州では設備の収益が立つ（信号＞0）＝後発＝ダンピング。収益が立たない州ではしない。
        double TariffAfterApply(double industrialization, double techLevel)
        {
            var sim = Four();
            var w = Neutral(sim, 0, industrialization);
            w.TechLevel = techLevel;   // 10.0：techProd＝1＋2＋8×0.3＝5.4 → techLev＝4.4・gross＝1.2÷(2.2²)×11.56＝2.866…＞0.7
            w.SetTreasuryRaw(1e12);
            w.LastExportValue = 100; w.LastImportValue = 10;   // 輸出超過比 10＞1.3
            UtilityAgent.Apply(w, UtilityAgent.Stance.Industrialize);
            return w.Policy.TariffTarget;
        }
        Assert.True(TariffAfterApply(3.0, 10.0) < 0, "信号＞0＝後発＝ダンピング（負関税の目標）");
        Assert.True(TariffAfterApply(1.0, 0.0) >= 0, "I＝1.0・技術 0＝ハードルを割る（信号 0）＝後発でない＝ダンピングしない");
    }

    [Fact]
    public void スタンスの工業化の余地は目盛り4_0かける信号である()
    {
        var w = Neutral(Four(), 0, 0);
        w.WorldTechLevel = w.TechLevel;   // 技術の遅れ 0
        UtilityAgent.ChooseStance(w, out var bd);
        Assert.Equal(1.2 * 4.0 * 0.41666666666666663, bd.UInd, 9);    // 1.2×4.0×信号（5/12）＝2.0
        // 収益が立たない盤面（I＝1.0）では余地 0
        var w1 = Neutral(Four(), 0, 1.0);
        w1.WorldTechLevel = w1.TechLevel;
        UtilityAgent.ChooseStance(w1, out var bd1);
        Assert.Equal(0.0, bd1.UInd, 9);
    }

    // ================= S1〜S3 国力の物差し =================

    private static NationalScore.ScoreTerms T(double a, double b, double c, double d, double e, double f) => new(a, b, c, d, e, f);

    [Fact]
    public void 国力の和は100で_全ての量を同じ倍率にしても割合は変わらず_総量0の項は外れる()
    {
        var t = new[] { T(1, 2, 3, 4, 5, 6), T(3, 2, 1, 0, 5, 14), T(6, 6, 6, 0, 10, 0) };
        var s = NationalScore.SharesFromTerms(t);
        Assert.Equal(100.0, s.Sum(), 9);
        // 国 0：兵 1/10・軍事費 2/10・鉄 3/10・動力 4/4・都市 5/20・総人口 6/20 → 平均 (0.1＋0.2＋0.3＋1.0＋0.25＋0.3)/6
        Assert.Equal(100.0 * (0.1 + 0.2 + 0.3 + 1.0 + 0.25 + 0.3) / 6.0, s[0], 9);
        var big = t.Select(x => T(x.Soldiers * 7, x.MilitarySpend * 7, x.Iron * 7, x.Energy * 7, x.UrbanPop * 7, x.TotalPop * 7)).ToArray();
        var sb = NationalScore.SharesFromTerms(big);
        for (int i = 0; i < 3; i++) Assert.Equal(s[i], sb[i], 9);
        // 動力の総量が 0 なら 5 項の平均（国 0：(0.1＋0.2＋0.3＋0.25＋0.3)/5）
        var z = t.Select(x => T(x.Soldiers, x.MilitarySpend, x.Iron, 0, x.UrbanPop, x.TotalPop)).ToArray();
        var sz = NationalScore.SharesFromTerms(z);
        Assert.Equal(100.0 * (0.1 + 0.2 + 0.3 + 0.25 + 0.3) / 5.0, sz[0], 9);
        Assert.Equal(100.0, sz.Sum(), 9);
        // 全ての項が 0 なら 0 点（NaN にしない）
        Assert.Equal(new[] { 0.0, 0.0 }, NationalScore.SharesFromTerms([T(0, 0, 0, 0, 0, 0), T(0, 0, 0, 0, 0, 0)]));
    }

    [Fact]
    public void 国力の素の量は本国の州だけで_鉄は数え鋼は数えず_軍事費は物価水準で割る()
    {
        var w = Neutral(Four(), 0, 0);
        var regs = w.Regions.Where(x => x.Tier != RegionTier.Colonial).ToList();
        Assert.True(regs.Count >= 1);
        foreach (var r in w.Regions) { r.Markets[Catalog.IronId].LastProduced = 0; r.Markets[Catalog.SteelId].LastProduced = 0; r.Markets[Catalog.CoalId].LastSold = 0; r.Markets[Catalog.CrudeOilId].LastSold = 0; r.Markets[Catalog.CoalId].LastDemand = 0; r.Markets[Catalog.CrudeOilId].LastDemand = 0; }
        var home = regs[0];
        home.Markets[Catalog.IronId].LastProduced = 40;
        home.Markets[Catalog.SteelId].LastProduced = 999;        // 鋼は数えない（鉄を 1.0 投入する＝二重計上）
        home.Markets[Catalog.CoalId].LastSold = 10; home.Markets[Catalog.CoalId].LastDemand = 40;   // 満たされた量だけ数える（需要と売れた量の差＝不足は数えない）
        home.Markets[Catalog.CrudeOilId].LastSold = 6; home.Markets[Catalog.CrudeOilId].LastDemand = 60;           // 石炭換算＝6×ColonyFuelOilEnergyRatio（1.5）＝9
        w.Coef.ColonyFuelOilEnergyRatio = 1.5;
        w.LastMilitaryClaimPerMonth = new MoneyPerMonth(30); w.LastSoldierPayPerMonth = new MoneyPerMonth(10); w.PriceLevel = 2.0;
        var t = NationalScore.TermsOf(w);
        Assert.Equal(40.0, t.Iron, 9);
        Assert.Equal(19.0, t.Energy, 9);
        home.Markets[Catalog.CoalId].LastDemand = 4000;   // 不足を増やしても動力の項は増えない
        Assert.Equal(19.0, NationalScore.TermsOf(w).Energy, 9);
        Assert.Equal(20.0, t.MilitarySpend, 9);   // (30＋10)÷2
        double pop = regs.Sum(x => x.Pops.Sum(p => p.Size));
        Assert.Equal(pop, t.TotalPop, 6);
        // 植民地の州は人口も産出も数えない
        var other = regs.Count > 1 ? regs[1] : null;
        if (other is not null)
        {
            double popOther = other.Pops.Sum(p => p.Size);
            other.Markets[Catalog.IronId].LastProduced = 5;
            Assert.Equal(45.0, NationalScore.TermsOf(w).Iron, 9);
            other.Tier = RegionTier.Colonial;
            Assert.Equal(40.0, NationalScore.TermsOf(w).Iron, 9);
            Assert.Equal(pop - popOther, NationalScore.TermsOf(w).TotalPop, 6);
        }
        // NaN は総量に混ぜない
        home.Markets[Catalog.IronId].LastProduced = double.NaN;
        Assert.Equal(0.0, NationalScore.TermsOf(w).Iron);
    }

    [Fact]
    public void 国力は割合で_全国の合計は100で_単独の世界は100か0である()
    {
        // ★(456) 旗 ScoreFromWorldShare は焼き込んだ（旧の腕の対照は消えた＝割合の国力が唯一の物差し）
        var sim = Four();
        sim.Run(2);
        var ws = sim.Nations.ConvertAll(n => n.World);
        var sc = NationalScore.Scores(ws);
        Assert.Equal(100.0, sc.Sum(), 6);
        for (int i = 0; i < ws.Count; i++) Assert.Equal(sc[i], NationalScore.Of(ws[i]), 9);   // Of（世界の総量を自分で求める道）と Scores（呼び手が求める道）が同じ値
        // 外交盤の無い単独の世界＝総量は自分だけ＝正の項の割合は全て 1 → 100
        var solo = new Simulation(NationConfig.France().ToScenario(7), new NullAgent());
        Assert.Equal(100.0, NationalScore.Of(solo.World), 9);
    }

    [Fact]
    public void 条約港の風化は新しい国力の列強との比を読む()
    {
        // 最強の列強（ランク Great の最大）に近い国ほど速く外国持分が風化する。旧（Military＋Wealth）と同じ向き。
        var sim = Four();
        sim.Run(1);
        var d = sim.Diplomacy;
        var c = sim.Nations[0].World.Coef;
        c.TreatyPortLapseRatio = new Share(0.5); c.ForeignShareFadeSpeed = FractionPerQuarter.FromQuarter(0.5);
        // 最強の列強以外の国の州に外国持分を積む
        var ws = sim.Nations.ConvertAll(n => n.World);
        var sc = NationalScore.Scores(ws);
        int top = Array.IndexOf(sc, sc.Max());
        int low = Array.IndexOf(sc, sc.Min());
        Assert.NotEqual(top, low);
        foreach (var w in ws)
            foreach (var r in w.Regions)
            { r.ForeignShares = new double[] { 0.4, 0.4 }; }
        d.ComputeRanks(c);
        d.TreatyPortLapse(c, 3);
        double Share(int i) => ws[i].Regions.Where(r => r.ForeignShares.Length > 0).Average(r => r.ForeignShares[0]);
        // 風化の速さ＝Clamp01(比÷0.5)×0.5。比が大きい（最強に近い）国ほど残りが小さい。
        Assert.True(Share(top) <= Share(low) + 1e-12, "最強に近い国のほうが外国持分が残っている（風化の向きが逆）");
    }
}
