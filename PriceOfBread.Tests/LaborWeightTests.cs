using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1037) 線 c98lw：旗 <see cref="Coefficients.LaborWeightsByPower"/>（労働力の重み＝手作業は女 0.6・子 0.35、機械の操作は差なし）の器の番人。
/// 事前登録 measurements/m1037_20261111_prereg_lw.txt の [4] T1〜T6。期待は紙の値（試験が自分で書いた式と数）で置く＝エンジンの補助の関数で期待を作らない。</summary>
public class LaborWeightTests
{
    private static Simulation Sim(ScenarioKind kind, double flag, double femaleNeed = -1)
    {
        var c = new Coefficients { LaborWeightsByPower = flag };
        if (femaleNeed >= 0) c.FemaleNeedWeight = femaleNeed;
        return new Simulation(new Scenario { Seed = 1, Kind = kind, Coef = c }, new NullAgent());
    }

    private static void Days(Simulation s, int n) { for (int d = 0; d < n; d++) s.StepDay(); }

    // 試験が自分で書く紙の式（形 A・OldLaborFactor 0.75・ChildWorkFactor 0.5）。h＝Habit の係数（フランスは 1）。
    private static double Paper(Pop p, double part, double wf, double wc, double h = 1.0)
        => (p.AgeShare(15, 65, 0) + 0.75 * p.AgeShare(65, double.PositiveInfinity, 0)) * h
           + (p.AgeShare(15, 65, 1) * part + 0.75 * p.AgeShare(65, double.PositiveInfinity, 1) * part) * wf * h
           + p.AgeShare(10, 15) * wc * p.ChildLaborRate.Value;

    private static int PopOf(Region r, int good) { foreach (var s in r.Producers) if (s.GoodId == good) return s.LaborPopIndex; return -1; }

    [Theory]
    [InlineData(ScenarioKind.France)]
    [InlineData(ScenarioKind.FullChain)]
    public void T1_開幕の月は旗0と旗1で全ての生産者の能力が一致する(ScenarioKind kind)
    {
        var a = Sim(kind, 0); var b = Sim(kind, 1);
        Days(a, 30); Days(b, 30);
        for (int ri = 0; ri < a.World.Regions.Count; ri++)
        {
            var ra = a.World.Regions[ri]; var rb = b.World.Regions[ri];
            int checkedLabor = 0;
            foreach (var p in rb.Pops) if (p.InitialWork >= 0) checkedLabor++;
            Assert.True(checkedLabor > 0, "1 か月目の労働の塊が回っていない（InitialWork が捕えられていない）");
            for (int i = 0; i < ra.Producers.Count; i++)
                Assert.Equal(ra.Producers[i].Capacity, rb.Producers[i].Capacity);
        }
    }

    [Fact]
    public void T2_T3_重みの式は手で女06子035_m05で08と0675_m1で1()
    {
        var sim = Sim(ScenarioKind.France, 1);
        var r = sim.World.Regions[0]; var c = sim.World.Coef;
        var p = r.Pops[PopOf(r, Catalog.LaborId)];
        p.ChildLaborRate = new Share(0.8);
        double h = Habit.LaborFactor(c, r);
        Assert.Equal(1.0, h);   // フランスはアヘンの習慣が無い＝紙の式の h＝1
        foreach (var (m, wf, wc) in new[] { (0.0, 0.6, 0.35), (0.5, 0.8, 0.675), (1.0, 1.0, 1.0) })
        {
            double got = EconomyStep.WeightedWork(p, r, c, 0.4, m);
            Assert.Equal(Paper(p, 0.4, wf, wc), got, 12);
        }
        // 女の項だけ・子の項だけを取り出して、それぞれの重みが動くことを別に見る（M5・M6 の番人）
        double kids = p.AgeShare(10, 15) * 0.8;
        double d1 = EconomyStep.WeightedWork(p, r, c, 0.0, 1.0) - EconomyStep.WeightedWork(p, r, c, 0.0, 0.0);   // 参加 0＝女の項なし＝子の項の差だけ
        Assert.Equal(kids * (1.0 - 0.35), d1, 12);
        p.ChildLaborRate = new Share(0.0);
        double fem = (p.AgeShare(15, 65, 1) + 0.75 * p.AgeShare(65, double.PositiveInfinity, 1)) * 0.5;
        double d2 = EconomyStep.WeightedWork(p, r, c, 0.5, 1.0) - EconomyStep.WeightedWork(p, r, c, 0.5, 0.0);   // 児童労働 0＝子の項なし＝女の項の差だけ
        Assert.Equal(fem * (1.0 - 0.6), d2, 12);
    }

    [Fact]
    public void T4_役割は売る労働の財で切る_フランスと簡易世界()
    {
        var fr = Sim(ScenarioKind.France, 1).World.Regions[0];
        var role = new byte[fr.Pops.Count];
        EconomyStep.LaborRoles(fr, role);
        for (int i = 0; i < fr.Pops.Count; i++)
        {
            int cls = fr.Pops[i].ClassId;
            if (cls == PopClass.Worker) Assert.Equal(1, role[i] & 3);
            if (cls == PopClass.Peasant) Assert.Equal(2, (int)role[i]);
            if (cls == PopClass.Engineer) Assert.Equal(4, (int)role[i]);
        }
        var fc = Sim(ScenarioKind.FullChain, 1).World.Regions[0];
        var role2 = new byte[fc.Pops.Count];
        EconomyStep.LaborRoles(fc, role2);
        Assert.Equal(1 | 4, (int)role2[1]);   // 工員＝労働力と高度労働力の両方（労働力の側の式で 1 つの work）
        Assert.Equal(2, (int)role2[0]);       // 一次労働者＝農業労働力だけ
    }

    [Fact]
    public void T5_動力の割合は工場の動力の需要を労働の総需要で割る_分母0とNaNは0()
    {
        var sim = Sim(ScenarioKind.France, 1);
        var r = sim.World.Regions[0]; var c = sim.World.Coef;
        Producer? f = null;
        foreach (var s in r.Producers)
        {
            s.ElecFill = 0; s.SteamFill = 0;
            if (f is null && s.BuysLabor && s.Inputs.Exists(i => i.GoodId == Catalog.LaborId)) f = s;
        }
        Assert.NotNull(f);
        f!.Output = 10; f.ElecFill = 1.0;
        var inp = f.Inputs.Find(i => i.GoodId == Catalog.LaborId);
        double q = inp.QtyPerOutput * (1.0 - c.ElecLaborSave * 1.0) * (1.0 - c.LaborSaveRate * r.Mechanization);
        r.Markets[Catalog.LaborId].LastDemand = 40 * q;   // 建設などの需要を含む総需要＝工場の需要 10q の 4 倍
        Assert.Equal(0.25, EconomyStep.PowerShare(r, c), 12);
        f.ElecFill = 0.5; f.SteamFill = 0.5;   // p＝0.5＋0.5×0.5＝0.75・電化の投入の節約も 0.5 に
        double q2 = inp.QtyPerOutput * (1.0 - c.ElecLaborSave * 0.5) * (1.0 - c.LaborSaveRate * r.Mechanization);
        Assert.Equal(10 * q2 * 0.75 / (40 * q), EconomyStep.PowerShare(r, c), 12);
        r.Markets[Catalog.LaborId].LastDemand = 0;
        Assert.Equal(0.0, EconomyStep.PowerShare(r, c));
        r.Markets[Catalog.LaborId].LastDemand = double.NaN;
        Assert.Equal(0.0, EconomyStep.PowerShare(r, c));
        r.Markets[Catalog.LaborId].LastDemand = 40 * q; f.Output = double.NaN;
        Assert.Equal(0.0, EconomyStep.PowerShare(r, c));
    }

    [Fact]
    public void T7_労働力の市場が無い小さな世界でも旗1が回り_動力の割合は0()
    {
        var sim = Sim(ScenarioKind.RegionalFamine, 1);
        var r = sim.World.Regions[0];
        Assert.True(Catalog.LaborId >= r.Markets.Length, "前提：この世界には労働力の市場が無い");
        Assert.Equal(0.0, EconomyStep.PowerShare(r, sim.World.Coef));
        var ex = Record.Exception(() => sim.Run(4));
        Assert.Null(ex);
    }

    [Fact]
    public void T6_2か月目に動力を入れると工員だけが紙のbで伸び_農民は手のまま_技師は旧のまま()
    {
        var a = Sim(ScenarioKind.France, 0, femaleNeed: 0); var b = Sim(ScenarioKind.France, 1, femaleNeed: 0);
        Days(a, 30); Days(b, 30);
        // 工場の動力を両方の世界で同じに入れる（電化 1）。試験が自分で m を数える（前の月の産出と総需要）
        int best = -1; double bestM = -1;
        for (int ri = 0; ri < b.World.Regions.Count; ri++)
        {
            var rb = b.World.Regions[ri]; var c = b.World.Coef; double num = 0;
            foreach (var s in a.World.Regions[ri].Producers) if (s.BuysLabor) s.ElecFill = 1.0;
            foreach (var s in rb.Producers)
            {
                if (!s.BuysLabor) continue;
                s.ElecFill = 1.0;
                foreach (var inp in s.Inputs)
                    if (inp.GoodId == Catalog.LaborId) num += s.Output * inp.QtyPerOutput * (1.0 - c.ElecLaborSave) * (1.0 - c.LaborSaveRate * rb.Mechanization);
            }
            double den = rb.Markets[Catalog.LaborId].LastDemand;
            double m = den > 0 ? num / den : 0;
            if (m > bestM) { bestM = m; best = ri; }
        }
        Assert.True(bestM > 0.3, $"動力を入れた州の m が小さすぎて試験が見分けられない（{bestM}）");
        Days(a, 30); Days(b, 30);
        var ra = a.World.Regions[best]; var rbb = b.World.Regions[best];
        double Cap(Region r, int good) { foreach (var s in r.Producers) if (s.GoodId == good && s.Capacity > 0) return s.Capacity; return double.NaN; }
        double part = rbb.FemaleParticipation;
        // 工員：b＝[新(m)÷新の基準]÷[旧÷旧の基準]（基準はそれぞれの世界の Pop.InitialWork）
        int wi = PopOf(rbb, Catalog.LaborId); var pw = rbb.Pops[wi]; var pwa = ra.Pops[wi];
        double expW = (Paper(pw, part, 0.6 + 0.4 * bestM, 0.35 + 0.65 * bestM) / pw.InitialWork) / (Paper(pwa, part, 1.0, 0.5) / pwa.InitialWork);
        double gotW = Cap(rbb, Catalog.LaborId) / Cap(ra, Catalog.LaborId);
        Assert.True(expW > 1.05, $"紙の b が 1 に近すぎる（{expW}）");
        Assert.InRange(gotW / expW, 0.98, 1.02);
        // 農民：手の重み（m を読まない）
        int fi = PopOf(rbb, Catalog.FarmLaborId); var pf = rbb.Pops[fi]; var pfa = ra.Pops[fi];
        double expF = (Paper(pf, part, 0.6, 0.35) / pf.InitialWork) / (Paper(pfa, part, 1.0, 0.5) / pfa.InitialWork);
        double expFm = (Paper(pf, part, 0.6 + 0.4 * bestM, 0.35 + 0.65 * bestM) / pf.InitialWork) / (Paper(pfa, part, 1.0, 0.5) / pfa.InitialWork);
        double gotF = Cap(rbb, Catalog.FarmLaborId) / Cap(ra, Catalog.FarmLaborId);
        Assert.True(System.Math.Abs(expFm / expF - 1.0) > 0.02, "農民に m を掛けた形と見分けられない");
        Assert.InRange(gotF / expF, 0.99, 1.01);
        // 技師：旧の式のまま＝能力は旗 0 と同じ
        double gotE = Cap(rbb, Catalog.AdvLaborId) / Cap(ra, Catalog.AdvLaborId);
        Assert.Equal(1.0, gotE, 12);
    }
}
