using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(970) 線 c87bx：計器 basisx_s（土地税の算定の基準 A1 の段 A-2b-0＝H4 の D・G・B と AI の規則 A・B・C1・C2・S の影の凍結）の番人。事前登録 measurements/m970_20261108_prereg_bx.txt の T1〜T9。
/// 期待は試験が手で置く（地代・率・期待インフレを試験が決め、pb・受け手はエンジンの関数〔Privileges.TaxExemptionDegree・EconomyStep.LandShareRecipient〕と欄から作る＝計器の式を呼び戻さない）。
/// 盤面はイギリス（名簿で Fixed＝助走で凍結の写しが立つ）とフランス（名簿の外）。合成の盤面では受け口と月の口を試験が直接呼び、四半期の行を書かせる。</summary>
[Collection("Instruments")]
public class BasisxInstrumentTests
{
    private const ulong Seed = 970UL;

    private static MultiNationSim NewSim(double flag = 1.0)
    {
        var sim = new MultiNationSim([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], Seed) { DiplomacyFrozen = true };
        foreach (var n in sim.Nations) n.World.Coef.LandTaxFixedBasis = flag;
        return sim;
    }

    private static string Begin() { string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_basisx_" + Guid.NewGuid().ToString("N")); Instruments.Configure(dir, "*"); return dir; }
    private static void End(MultiNationSim sim) { Instruments.Close(sim); Instruments.Configure(null, null); }
    private static List<Dictionary<string, string>> Rows(string dir) => SubstInstrumentTests.Rows(System.IO.Path.Combine(dir, $"basisx_s{Seed}.tsv"), null);
    private static double D(Dictionary<string, string> r, string c) => SubstInstrumentTests.D(r, c);
    private static Dictionary<string, string> Row(List<Dictionary<string, string>> rows, int q, string nation) => rows.Single(r => r["quarter"] == q.ToString() && r["nation"] == nation);
    private static void Near(double want, double got, string what) => Assert.True(Math.Abs(want - got) <= 1e-9 * Math.Max(1.0, Math.Max(Math.Abs(want), Math.Abs(got))), $"{what}: 期待 {want} 実際 {got}");

    private static double Deg(World w) { Assert.Equal(0.0, w.Coef.PrivilegeFromCrownShare.Value); return Privileges.TaxExemptionDegree(w, w.Coef, 0.0); }
    private static double Pb(Pop p, double deg) => p.IsCriminal ? 0.0 : (p.Closed && !p.IsSoldier ? 1.0 - deg : 1.0);

    /// <summary>月の口を 1 回回す（四半期の締め月かどうかを指定）。</summary>
    private static void Month(World w, bool quarterEnd) { w.IsQuarterEndMonth = quarterEnd; Instruments.BasisxMonth(w); }

    /// <summary>T1：見出しは 3 つの鍵＋43 列・重複なし。landbase_s と landfix_s の見出しは変わらない（25 列ずつ）。</summary>
    [Fact]
    public void T1_見出し()
    {
        var h = Instruments.BasisxHeaderText.Split('\t');
        Assert.Equal(46, h.Length);
        Assert.Equal(["quarter", "year", "nation"], h[..3]);
        Assert.Equal(h.Length, h.Distinct().Count());
        Assert.Equal(25, Instruments.LandbaseHeaderText.Split('\t').Length);
        Assert.Equal(25, Instruments.LandfixHeaderText.Split('\t').Length);
    }

    /// <summary>T2：規則 A・B の 1 歩（手で数えた系列）。P で π＜0 なら F（f＝R）。F の戻しは A が R×(1+π)＞f・B が R＞f。凍結の額 f は F の間に書き換わらない。NaN は動かさない。</summary>
    [Fact]
    public void T2_規則の1歩()
    {
        bool fx = false; double f = 0;
        Assert.False(Instruments.BxRuleStep(ref fx, ref f, false, 0.02, 100));   // P で π＞0＝動かない
        Assert.False(fx);
        Assert.True(Instruments.BxRuleStep(ref fx, ref f, false, -0.01, 100));   // P→F・f＝100
        Assert.True(fx); Assert.Equal(100.0, f);
        Assert.False(Instruments.BxRuleStep(ref fx, ref f, false, -0.01, 100));   // A：100×0.99＝99＜100＝戻らない
        Assert.True(fx);
        Assert.True(Instruments.BxRuleStep(ref fx, ref f, false, 0.02, 100));   // A：100×1.02＝102＞100＝戻る
        Assert.False(fx);
        // 規則 B：π を戻しの判定に使わない・f は F の間に R へ上書きされない（R が f を超えた月に戻る）
        fx = false; f = 0;
        Assert.True(Instruments.BxRuleStep(ref fx, ref f, true, -0.01, 100));
        Assert.False(Instruments.BxRuleStep(ref fx, ref f, true, 0.50, 100));   // B：R＝f＝戻らない（A なら 150＞100 で戻る）
        Assert.True(fx); Assert.Equal(100.0, f);
        Assert.True(Instruments.BxRuleStep(ref fx, ref f, true, 0.0, 100.5));   // B：R＞f＝戻る
        Assert.False(fx);
        // NaN
        fx = false; f = 0;
        Assert.False(Instruments.BxRuleStep(ref fx, ref f, false, double.NaN, 100));
        Assert.False(Instruments.BxRuleStep(ref fx, ref f, false, -0.01, double.NaN));
        Assert.False(fx);
    }

    /// <summary>T3：毎四半期に反転する偽の規則の台本（★(879)）＝π を −0.01 と 0.50 で交互に・R は 100 と 101 で交互に・20 四半期で A も B も 20 回切り替わる（往復の上限の物差しが 668 回の側へ行く形）。</summary>
    [Fact]
    public void T3_毎四半期に反転する台本は20回()
    {
        foreach (bool ruleB in new[] { false, true })
        {
            bool fx = false; double f = 0; int sw = 0;
            for (int i = 0; i < 20; i++)
                if (Instruments.BxRuleStep(ref fx, ref f, ruleB, i % 2 == 0 ? -0.01 : 0.50, i % 2 == 0 ? 100.0 : 101.0)) sw++;
            Assert.Equal(20, sw);
        }
    }

    /// <summary>T4（合成・凍結なし）：フランスの首都の州の RGO 2 本へ地代を手で入れる（1 本は持ち分なし・もう 1 本は貴族と教会が半分ずつ・もう 1 回は負の地代）。τ＝0.2・π＝0.03。
    /// RGO の数 3・凍結 0・Σ pb×r・基数・φ＝0・ΔC（凍結が無いので今すぐ・比例へは 0・定額へは −τ×π×Σ pb×r）・moved＝τ×π＝0.006・H4 の D・G（定額へ）。pb を外す突然変異で赤になる。</summary>
    [Fact]
    public void T4_合成の盤面_凍結なし()
    {
        string dir = Begin();
        var sim = NewSim();
        try
        {
            sim.StartWarmup(quartersOverride: 1);
            var fr = sim.Nations[1].World;
            double deg = Deg(fr);
            Assert.True(deg > 0, $"pb の突然変異に歯が立つには閉鎖階級の免税 d′＞0 が要る（{deg}）");
            fr.LandTax = 0.2; fr.ExpectedInflationPerYear = new RatePerYear(0.03);
            var reg = fr.Regions.First(x => x.Producers.Count(s => s.IsRgo && s.OwnerPopIndex >= 0 && s.OwnerPopIndex < x.Pops.Count) >= 2 && x.ClergyPopIndex >= 0);
            var rgos = reg.Producers.Where(s => s.IsRgo && s.OwnerPopIndex >= 0 && s.OwnerPopIndex < reg.Pops.Count).Take(2).ToList();
            var s1 = rgos[0]; var s2 = rgos[1];
            s1.LandShares = null; s2.LandShares = [0.5, 0.5, 0, 0];
            foreach (var rr in fr.Regions) foreach (var p in rr.Pops) p.LandRentPerMonth = new MoneyPerMonth(0);
            Month(fr, false);   // 月の和を空にする
            var rent = new Dictionary<Pop, double>();
            void Add(Pop p, double v) { rent[p] = rent.GetValueOrDefault(p) + v; }
            Add(reg.Pops[s1.OwnerPopIndex], 1000);
            Add(reg.Pops[EconomyStep.LandShareRecipient(reg, s2, 0)], 1500);
            Add(reg.Pops[EconomyStep.LandShareRecipient(reg, s2, 1)], 1500);
            Instruments.BasisxRgoRent(fr, reg, s1, 1000);
            Instruments.BasisxRgoRent(fr, reg, s2, 3000);
            Instruments.BasisxRgoRent(fr, reg, s1, -500);   // 負の地代＝正の部分だけ（受け手の所得は動かない）
            foreach (var kv in rent) kv.Key.LandRentPerMonth = new MoneyPerMonth(kv.Value);
            fr.FiscalFundingRatio = new Share(0.9); fr.GovDebt = new MoneyStock(1234567.0);   // (977) 支払い率と国債は既定でない値に置く（bxFundingRatio・bxDebtToGdpYear の列を確かめる）
            Month(fr, true);
            Instruments.Quarter(sim, Seed, 0);
            double gdpYear = fr.NominalGdpPerMonth.Value * 12.0;   // 国債の比の分母＝年の名目 GDP（月×12・手で書く）
            Assert.True(gdpYear > 0, "名目 GDP が正でない＝bxDebtToGdpYear が nan になる");
            End(sim);
            var row = Row(Rows(dir), 0, "フランス");
            Near(0.9, D(row, "bxFundingRatio"), "支払い率"); Near(1234567.0 / gdpYear, D(row, "bxDebtToGdpYear"), "国債÷年の名目 GDP");
            double rentPb = rent.Sum(kv => Pb(kv.Key, deg) * kv.Value);
            Assert.Equal(3.0, D(row, "bxRgoCount")); Assert.Equal(0.0, D(row, "bxFrozenRgoCount"));
            Near(0.2, D(row, "bxRate"), "率");
            Near(rentPb, D(row, "bxRentPbPerMonth"), "Σ pb×r");
            Near(rentPb, D(row, "bxBasePbPerMonth"), "基数（凍結なし＝地代）");
            Near(rentPb, D(row, "bxRentPbPopSidePerMonth"), "POP 側の Σ pb×r");
            Near(0.0, D(row, "bxFrozenPbPerMonth"), "凍結の額"); Near(0.0, D(row, "bxRentOfFrozenPbPerMonth"), "凍結の地代");
            Near(0.0, D(row, "bxFrozenShare"), "φ");
            Near(0.03, D(row, "bxExpInflPerYear"), "期待インフレ");
            Near(0.0, D(row, "bxDeltaCNowToPropPerMonth"), "今すぐ戻す"); Near(0.0, D(row, "bxDeltaCExpToPropPerMonth"), "1 年先に戻す");
            Near(-0.2 * 0.03 * rentPb, D(row, "bxDeltaCExpToFixedPerMonth"), "1 年先の定額へ");
            Near(0.2 * 0.03, D(row, "bxMovedToFixed"), "moved（定額へ）"); Near(0.0, D(row, "bxMovedToProp"), "moved（比例へ）");
            Near(rentPb, D(row, "bxH4BasePbPerMonth"), "H4 の B");
            Near(-0.2 * 0.03 * rentPb, D(row, "bxH4DToFixed"), "H4 の D（定額へ）"); Near(0.2 * 0.03 * rentPb, D(row, "bxH4GToFixed"), "H4 の G（定額へ）");
            Assert.True(double.IsNaN(D(row, "bxH4DToProp")) && double.IsNaN(D(row, "bxH4GToProp")), "φ＝0 は比例へ戻す物が無い＝nan");
            Assert.Equal(0.0, D(row, "bxNanFlag"));
        }
        finally { if (Instruments.On) End(sim); }
    }

    /// <summary>T5（合成・凍結あり）：イギリスの凍結した RGO 1 本（エンジンの凍結の額 f）の地代を 2f にし、持ち分を貴族 0.6・教会 0.4 に置く。τ＝0.1・π＝0.05。
    /// 凍結 1・Σ(凍結) pb×f（エンジンの欄と受け手の関数から）・φ＝1・基数＝凍結の額・ΔC（今すぐ＝τ×Σ pb×f・1 年先に戻す＝τ×Σ pb×f×(1+2π)・定額へ＝0）・moved＝τ×(1+2π)・
    /// H4 の D・G（比例へ・POP の地代は貴族 2×A・教会 0.5×A＝符号が混ざる）・B_H4＝基数。G を |c| でなく c の和にする突然変異で赤になる。</summary>
    [Fact]
    public void T5_合成の盤面_凍結あり()
    {
        string dir = Begin();
        var sim = NewSim();
        try
        {
            sim.StartWarmup(quartersOverride: 1);
            var uk = sim.Nations[0].World;
            double deg = Deg(uk);
            uk.LandTax = 0.1; uk.ExpectedInflationPerYear = new RatePerYear(0.05);
            Region? reg = null; Producer? s = null;
            foreach (var r in uk.Regions)
            {
                if (r.ClergyPopIndex < 0 || r.ClergyPopIndex >= r.Pops.Count) continue;
                var c = r.Producers.FirstOrDefault(x => x.IsRgo && x.FrozenLandRentPerMonth is not null && x.OwnerPopIndex >= 0 && x.OwnerPopIndex < r.Pops.Count && x.OwnerPopIndex != r.ClergyPopIndex);
                if (c is not null) { reg = r; s = c; break; }
            }
            Assert.NotNull(s);
            s!.LandShares = [0.6, 0.4, 0, 0];
            double f = s.FrozenLandRentPerMonth!.Value.Value;
            foreach (var rr in uk.Regions) foreach (var p in rr.Pops) p.LandRentPerMonth = new MoneyPerMonth(0);
            Month(uk, false);
            var a = new Dictionary<Pop, double>();
            a[reg!.Pops[EconomyStep.LandShareRecipient(reg, s, 0)]] = f * 0.6;
            a[reg.Pops[EconomyStep.LandShareRecipient(reg, s, 1)]] = f * 0.4;
            Instruments.BasisxRgoRent(uk, reg, s, 2 * f);
            // POP 側の地代：貴族 2×A・教会 0.5×A（符号が混ざる）
            var pops = a.Keys.ToList();
            double popSide = 0, dP = 0, gP = 0;
            for (int i = 0; i < pops.Count; i++)
            {
                double ap = a[pops[i]], rp = (i == 0 ? 2.0 : 0.5) * ap;
                pops[i].LandRentPerMonth = new MoneyPerMonth(rp);
                double pb = Pb(pops[i], deg);
                popSide += pb * rp; double cP = 0.1 * pb * (rp * 1.05 - ap / 1.0); dP += cP; gP += Math.Abs(cP);
            }
            Month(uk, true);
            Instruments.Quarter(sim, Seed, 0);
            End(sim);
            var row = Row(Rows(dir), 0, "イギリス");
            double fp = a.Sum(kv => Pb(kv.Key, deg) * kv.Value);
            Assert.Equal(1.0, D(row, "bxRgoCount")); Assert.Equal(1.0, D(row, "bxFrozenRgoCount"));
            Near(fp, D(row, "bxFrozenPbPerMonth"), "Σ(凍結) pb×f"); Near(2 * fp, D(row, "bxRentOfFrozenPbPerMonth"), "Σ(凍結) pb×r");
            Near(2 * fp, D(row, "bxRentPbPerMonth"), "Σ pb×r"); Near(fp, D(row, "bxBasePbPerMonth"), "基数＝凍結の額");
            Near(popSide, D(row, "bxRentPbPopSidePerMonth"), "POP 側");
            Near(1.0, D(row, "bxFrozenShare"), "φ");
            Near(0.1 * fp, D(row, "bxDeltaCNowToPropPerMonth"), "今すぐ戻す");
            Near(0.1 * fp * (1 + 2 * 0.05), D(row, "bxDeltaCExpToPropPerMonth"), "1 年先に戻す");
            Near(0.0, D(row, "bxDeltaCExpToFixedPerMonth"), "定額へ（凍結でない物が無い）");
            Near(0.0, D(row, "bxMovedToFixed"), "moved（定額へ）"); Near(0.1 * (1 + 2 * 0.05), D(row, "bxMovedToProp"), "moved（比例へ）");
            Near(fp, D(row, "bxH4BasePbPerMonth"), "H4 の B＝基数");
            Near(dP, D(row, "bxH4DToProp"), "H4 の D（比例へ）"); Near(gP, D(row, "bxH4GToProp"), "H4 の G（比例へ）");
            Assert.True(gP > Math.Abs(dP) + 1e-12, "符号が混ざる盤面＝G＞|D|");
        }
        finally { if (Instruments.On) End(sim); }
    }

    /// <summary>T6（合成・規則の述語）：フランス（開幕は比例）。π＝−0.02・R は一定。四半期 0 の月：支払い率 0.9→1.0（締め月は 1.0）＝C1 の述語は 0・C2 は 1。A・B・C2 は定額へ・C1 は据え置き。
    /// 四半期 1 の月：1.0→0.8（締め月は 0.8）＝C1 も C2 も 1＝C1 が定額へ。切り替えの回数は A・B・C2 が 1・C1 が 1（四半期 1）。述語を外す突然変異（C1 を毎四半期評価）で赤になる。</summary>
    [Fact]
    public void T6_合成の盤面_危機の述語()
    {
        string dir = Begin();
        var sim = NewSim();
        try
        {
            sim.StartWarmup(quartersOverride: 1);
            var fr = sim.Nations[1].World;
            fr.ExpectedInflationPerYear = new RatePerYear(-0.02);
            foreach (var rr in fr.Regions) foreach (var p in rr.Pops) p.LandRentPerMonth = new MoneyPerMonth(0);
            fr.Regions[0].Pops[0].LandRentPerMonth = new MoneyPerMonth(500);
            fr.FiscalFundingRatio = new Share(0.9); Month(fr, false);
            fr.FiscalFundingRatio = new Share(1.0); Month(fr, true);
            Instruments.Quarter(sim, Seed, 0);
            fr.FiscalFundingRatio = new Share(1.0); Month(fr, false);
            fr.FiscalFundingRatio = new Share(0.8); Month(fr, true);
            Instruments.Quarter(sim, Seed, 1);
            End(sim);
            var rows = Rows(dir);
            var r0 = Row(rows, 0, "フランス"); var r1 = Row(rows, 1, "フランス");
            Assert.Equal(0.0, D(r0, "bxCrisis1")); Assert.Equal(1.0, D(r0, "bxCrisis2"));
            Assert.Equal(1.0, D(r1, "bxCrisis1")); Assert.Equal(1.0, D(r1, "bxCrisis2"));
            Assert.Equal([1.0, 1.0, 0.0, 1.0, 0.0], new[] { D(r0, "bxTargetA"), D(r0, "bxTargetB"), D(r0, "bxTargetC1"), D(r0, "bxTargetC2"), D(r0, "bxTargetS") });
            Assert.Equal([0.0, 0.0, 0.0, 0.0, 0.0], new[] { D(r0, "bxSwitchC1"), D(r0, "bxSwitchS"), D(r0, "bxSwitchA") - 1, D(r0, "bxSwitchB") - 1, D(r0, "bxSwitchC2") - 1 });
            Assert.Equal(1.0, D(r1, "bxTargetC1")); Assert.Equal(1.0, D(r1, "bxSwitchC1"));
            Near(500.0, D(r1, "bxShadowFrozenC1"), "C1 の影の凍結＝切り替えた四半期の R");
            Assert.Equal(1.0, D(r1, "bxSwitchA")); Assert.Equal(1.0, D(r1, "bxSwitchC2"));
        }
        finally { if (Instruments.On) End(sim); }
    }

    /// <summary>T7（合成・規則 S）：近代化の姿勢（ModernizeSignal）を月の口が写す＝フランス（開幕は比例）は規則 S が定額へ・姿勢の列は近代化の番号。姿勢の信号の無いイギリス（開幕は定額）の S の切り替えは 0。写しを外す突然変異で赤になる。</summary>
    [Fact]
    public void T7_合成の盤面_規則S()
    {
        string dir = Begin();
        var sim = NewSim();
        try
        {
            sim.StartWarmup(quartersOverride: 1);
            var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
            fr.Regions[0].Pops[0].LandRentPerMonth = new MoneyPerMonth(500);
            Instruments.ModernizeSignal(fr, UtilityAgent.Stance.Modernize, 0.0);
            Month(uk, true); Month(fr, true);
            Instruments.Quarter(sim, Seed, 0);
            End(sim);
            var rows = Rows(dir);
            var f0 = Row(rows, 0, "フランス"); var u0 = Row(rows, 0, "イギリス");
            Assert.Equal((double)(int)UtilityAgent.Stance.Modernize, D(f0, "bxStance"));
            Assert.Equal(1.0, D(f0, "bxTargetS")); Assert.Equal(1.0, D(f0, "bxSwitchS"));
            Assert.Equal(-1.0, D(u0, "bxStance")); Assert.Equal(0.0, D(u0, "bxSwitchS")); Assert.Equal(1.0, D(u0, "bxTargetS"));
        }
        finally { if (Instruments.On) End(sim); }
    }

    private static List<Dictionary<string, string>> RunReal(double flag, int quarters, Action<MultiNationSim, int, List<int>>? between, out List<int> engineFrozen)
    {
        string dir = Begin();
        var sim = NewSim(flag);
        var eng = new List<int>();
        try
        {
            sim.StartWarmup(quartersOverride: 1);
            for (int q = 0; q < quarters; q++)
            {
                between?.Invoke(sim, q, eng);
                sim.StepQuarter(); Instruments.Quarter(sim, Seed, q);
                eng.Add(sim.Nations[0].World.Regions.Sum(r => r.Producers.Count(x => x.IsRgo && x.FrozenLandRentPerMonth is not null)));
            }
        }
        finally { End(sim); }
        engineFrozen = eng;
        return Rows(dir);
    }

    /// <summary>T8（実走・旗 1）：2 国×4 四半期の行が揃う。イギリスの凍結の数はエンジンの凍結の数と同じ・フランスは 0。Σ pb×r は POP 側と相対 1e-9 で一致。H4 の B は基数と一致（POP ごとの式と RGO ごとの式の 2 通り）。
    /// 規則：切り替えの回数は目標の変化の数と一致（開幕はイギリス＝定額・フランス＝比例）。q の行の規則用の期待インフレは q+1 の行の締め月の期待インフレと同じ値（時点の約束）。</summary>
    [Fact]
    public void T8_実走の整合()
    {
        var rows = RunReal(1.0, 4, null, out var eng);
        Assert.Equal(8, rows.Count);
        for (int q = 0; q < 4; q++)
        {
            var uk = Row(rows, q, "イギリス"); var fr = Row(rows, q, "フランス");
            Assert.Equal((double)eng[q], D(uk, "bxFrozenRgoCount")); Assert.True(eng[q] > 0);
            Assert.Equal(0.0, D(fr, "bxFrozenRgoCount"));
            foreach (var r in new[] { uk, fr })
            {
                Near(D(r, "bxRentPbPerMonth"), D(r, "bxRentPbPopSidePerMonth"), "Σ pb×r の 2 通り");
                Near(D(r, "bxBasePbPerMonth"), D(r, "bxH4BasePbPerMonth"), "基数の 2 通り");
                Assert.Equal(0.0, D(r, "bxNanFlag"));
            }
        }
        foreach (var nation in new[] { "イギリス", "フランス" })
        {
            double[] prevT = new double[5]; double[] prevS = new double[5];
            for (int k = 0; k < 5; k++) prevT[k] = nation == "イギリス" ? 1.0 : 0.0;
            string[] ks = ["A", "B", "C1", "C2", "S"];
            for (int q = 0; q < 4; q++)
            {
                var r = Row(rows, q, nation);
                for (int k = 0; k < 5; k++)
                {
                    double t = D(r, "bxTarget" + ks[k]), sw = D(r, "bxSwitch" + ks[k]);
                    prevS[k] += t != prevT[k] ? 1 : 0; prevT[k] = t;
                    Assert.Equal(prevS[k], sw);
                }
                if (q < 3) Near(D(r, "bxExpInflRulePerYear"), D(Row(rows, q + 1, nation), "bxExpInflPerYear"), "期待インフレの時点");
            }
        }
    }

    /// <summary>T9（実走・旗 0＝何もしない実装でも落ちない列と落ちる列）：エンジンの凍結は 0 だが、計器の凍結の写しは立つ＝bxFrozenRgoCount＞0（landfix の写しを読む）・基数は凍結の額。</summary>
    [Fact]
    public void T9_旗0でも写しの凍結が立つ()
    {
        var rows = RunReal(0.0, 2, null, out var eng);
        Assert.Equal(0, eng[1]);
        var uk = Row(rows, 1, "イギリス");
        Assert.True(D(uk, "bxFrozenRgoCount") > 0);
        Assert.True(D(uk, "bxFrozenPbPerMonth") > 0);
        Assert.Equal(0.0, D(Row(rows, 1, "フランス"), "bxFrozenRgoCount"));
    }

    /// <summary>T10（出て戻る・★(947)）：イギリスの首都でない州（エンジンの凍結のある RGO を持つ）をフランスへ出して戻す（四半期をまたぐ版と同じ四半期の中の版）。出て戻った後の行の凍結の数はエンジンの凍結の数と同じ＝写しは生き返らない。</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void T10_出て戻った州の写しは生き返らない(bool inQuarter)
    {
        string dir = Begin();
        var sim = NewSim();
        var uk = sim.Nations[0].World; var fr = sim.Nations[1].World;
        var eng = new List<int>();
        int Eng() => uk.Regions.Sum(r => r.Producers.Count(x => x.IsRgo && x.FrozenLandRentPerMonth is not null));
        try
        {
            sim.StartWarmup(quartersOverride: 1);
            sim.StepQuarter(); Instruments.Quarter(sim, Seed, 0); eng.Add(Eng());
            var moved = uk.Regions.First(x => x.Id != 0 && x.Tier != RegionTier.Colonial && x.Producers.Any(s => s.IsRgo && s.FrozenLandRentPerMonth is not null));
            if (!inQuarter)
            {
                Territory.TransferRegion(uk, fr, moved, Territory.Rehoming.Ceded);
                sim.StepQuarter(); Instruments.Quarter(sim, Seed, 1); eng.Add(Eng());
                Territory.TransferRegion(fr, uk, moved, Territory.Rehoming.Ceded);
            }
            else
            {
                for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();
                Territory.TransferRegion(uk, fr, moved, Territory.Rehoming.Ceded);
                for (int d = 0; d < Calendar.DaysPerMonth; d++) sim.StepDay();
                Territory.TransferRegion(fr, uk, moved, Territory.Rehoming.Ceded);
                for (int d = 2 * Calendar.DaysPerMonth; d < Calendar.DaysPerQuarter; d++) sim.StepDay();
                Instruments.Quarter(sim, Seed, 1); eng.Add(Eng());
            }
            sim.StepQuarter(); Instruments.Quarter(sim, Seed, 2); eng.Add(Eng());
        }
        finally { End(sim); }
        var rows = Rows(dir);
        Assert.True(eng[0] > eng[2], $"戻った州の RGO の凍結はエンジンで消えている（{eng[0]} → {eng[2]}）");
        for (int q = 1; q <= 2; q++) Assert.Equal((double)eng[q], D(Row(rows, q, "イギリス"), "bxFrozenRgoCount"));   // 四半期 1・2（財の番号の走査の番人が無名の整数の配列を拾うので for で書く）
    }
}
