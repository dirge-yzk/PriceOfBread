using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1166) 線 c115sv：兵 0 の崖＝大きさ 0 の兵士の POP を、空きがあれば社会移動の職の行き先にする（旗 SoldierVacancyFromZero）の機構の番人と、計器 soldiervac_s の自己検査。
/// 事前登録 measurements/m1166_20261116_prereg_sv.txt の F0〜F5。盤面＝英仏の英（1 四半期の後）の首都。兵の POP を手で大きさ 0 に置き、
/// 徴募の母数（SoldierRecruitClassA・B）の前の月の所得を〈働き盛り 1 人あたり 3.0〉に置く（期待の単価 3.0 は手で書いた数）。
/// 送り手は農民（級 0・今の所得 0＝同じ級の職は所得＞0 なら候補）。他の級 0 の職の今の所得は 0（候補にならない）。
/// </summary>
[Collection("Instruments")]
public class SoldierVacancyFromZeroTests
{
    const ulong Seed = 1789UL;
    const double Unit = 3.0;

    static readonly System.Reflection.MethodInfo RouteViaPoolM =
        typeof(Meritocracy).GetMethod("RouteViaPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
    static readonly System.Reflection.MethodInfo EmptySeatIncomeM =
        typeof(Meritocracy).GetMethod("EmptySeatIncome", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

    static MultiNationSim NewSim() =>
        new([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], seed: Seed, coef: new Coefficients()) { DiplomacyFrozen = true };

    sealed record Board(World W, Region R, Coefficients C, Pop Soldier, Pop Farm, Pop? Other);

    /// <summary>首都の盤面。兵は大きさ 0（zeroSoldier）か今の大きさ。母数の前の月の所得＝働き盛り×3.0・今の所得＝0。他の級 0 の POP の今の所得 0・失業 0。</summary>
    static Board Make(MultiNationSim sim, double flag, bool zeroSoldier)
    {
        var w = sim.Nations[0].World; var r = w.Region; var c = w.Coef;
        c.SoldierVacancyFromZero = flag;
        c.SerfGateCoversPool = 0;   // 農奴の移動の門を外す（送った人数＝流れ）
        Pop? s = null, farm = null, other = null;
        for (int i = 0; i < r.Pops.Count; i++)
        {
            var p = r.Pops[i];
            if (p.IsSoldier) { s ??= p; continue; }
            if (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB)
                p.LastEarnedPerMonth = new MoneyPerMonth(Unit * p.Size * p.AgeShare(16, 60));
            if (Meritocracy.ClassOf(r, i) != 0 || p.Closed) continue;
            p.EarnedPerMonth = new MoneyPerMonth(0); p.UnemploymentRate = new Share(0);
            if (p.ClassId == c.SoldierRecruitClassA && p.Size > 1) farm ??= p;
            else if (p.Size > 1 && i != r.InvestorPopIndex && i != r.MerchantPopIndex && i != r.CriminalPopIndex) other ??= p;
        }
        Assert.True(s is not null && farm is not null, "前提：首都に兵士と農民");
        s!.UnemploymentRate = new Share(0);
        if (zeroSoldier) { s.Size = 0; s.EarnedPerMonth = new MoneyPerMonth(0); }
        return new Board(w, r, c, s, farm!, other);
    }

    /// <summary>農民から級 0 の職へ flow を送る（強制なし＝残りは農民に残る）。戻り値＝兵の増え・空きの減り・州の人口の差。</summary>
    static (double Gain, double VacUsed, double PopDiff, double OtherGain) Route(Board b, double flow, double vac)
    {
        double s0 = b.Soldier.Size, o0 = b.Other?.Size ?? 0, pop0 = b.R.SumPopulation();
        object[] args = [b.R, b.W, 0.5, 1.0, b.Farm, 0, flow, 1.0, 1.0, b.C, vac, false];
        RouteViaPoolM.Invoke(null, args);
        return (b.Soldier.Size - s0, vac - (double)args[10], b.R.SumPopulation() - pop0, (b.Other?.Size ?? 0) - o0);
    }

    [Fact]
    public void F0_旗0では大きさ0の兵の席は空きがあっても行き先にならない()
    {
        var sim = NewSim(); sim.StepQuarter();
        var b = Make(sim, 0, zeroSoldier: true);
        var x = Route(b, 0.01 * b.Farm.Size, 1e9);
        Assert.Equal(0.0, b.Soldier.Size);
        Assert.Equal(0.0, x.VacUsed);
    }

    [Fact]
    public void F1_旗1では大きさ0の兵の席が埋まり空きの減りが兵の増えと等しく人口は保存()
    {
        var sim = NewSim(); sim.StepQuarter();
        var b = Make(sim, 1, zeroSoldier: true);
        double flow = 0.01 * b.Farm.Size;
        var x = Route(b, flow, 1e9);
        Assert.True(x.Gain > 1e-6 * flow, $"大きさ 0 の兵の席が埋まらない（増え {x.Gain:R}・流れ {flow:R}）");
        Assert.True(x.Gain <= flow * (1 + 1e-12), $"送った流れより多く兵になった（{x.Gain:R}＞{flow:R}）");
        Assert.True(Math.Abs(x.VacUsed - x.Gain) <= 1e-9 * flow, $"空きの減り {x.VacUsed:R} が兵の増え {x.Gain:R} と違う");
        Assert.True(Math.Abs(x.PopDiff) <= 1e-9 * flow, "州の人口が保存されない");
    }

    /// <summary>試験の手の和：国の大きさ＞0 の兵が実際に受け取った比＝Σ兵の今の所得÷Σ（兵の大きさ×州の母数の前の月の単価×SoldierPayMult）。単価は試験が州の POP を自分で回して組む。</summary>
    static double HandRate(World w, Coefficients c, bool keep)
    {
        double num = 0, den = 0;
        foreach (var r in w.Regions)
        {
            Pop? s = null; double pe = 0, pa = 0;
            foreach (var p in r.Pops)
            {
                if (p.IsSoldier) { s ??= p; continue; }
                if (p.ClassId == c.SoldierRecruitClassA || p.ClassId == c.SoldierRecruitClassB)
                { pe += Math.Max(0, p.LastEarnedPerMonth.Value); pa += keep ? p.Size : p.Size * p.AgeShare(16, 60); }
            }
            if (s is null || !(s.Size > 1e-9)) continue;
            num += Math.Max(0, s.EarnedPerMonth.Value);
            den += s.Size * (pa > 1e-9 ? pe / pa : 0) * c.SoldierPayMult;
        }
        return den > 1e-9 ? num / den : 0;
    }

    [Fact]
    public void F2_大きさ0の兵の席の所得は母数の前の月の単価に給与の倍率と国の兵の受け取りの比と送り手の働き盛りの割合を掛ける()
    {
        var sim = NewSim(); sim.StepQuarter();
        var b = Make(sim, 1, zeroSoldier: true);
        double perHead = b.Farm.Size * b.Farm.AgeShare(16, 60) / b.Farm.Size;
        b.W.ConscriptionLaw = ConscriptionLaw.Volunteer;
        double k = HandRate(b.W, b.C, keep: false);
        Assert.True(k > 0 && Math.Abs(k - 1) > 1e-3, $"前提：受け取りの比が 0 か 1（{k:R}）＝比を外す突然変異が見えない盤面");
        double got = (double)EmptySeatIncomeM.Invoke(null, [b.W, b.R, b.Soldier, b.Farm, b.C])!;
        Assert.Equal(Unit * b.C.SoldierPayMult * k * perHead, got, 12);
        // 徴兵の国は頭数あたり（keep＝Simulation.cs:1754 と同じ）＝母数の前の月の所得の和÷頭数の和（試験の手の和）・比も頭数あたりの単価で割る
        double pe = 0, pa = 0;
        foreach (var p in b.R.Pops)
            if (!p.IsSoldier && (p.ClassId == b.C.SoldierRecruitClassA || p.ClassId == b.C.SoldierRecruitClassB)) { pe += p.LastEarnedPerMonth.Value; pa += p.Size; }
        b.W.ConscriptionLaw = ConscriptionLaw.Selective;
        double kK = HandRate(b.W, b.C, keep: true);
        double gotK = (double)EmptySeatIncomeM.Invoke(null, [b.W, b.R, b.Soldier, b.Farm, b.C])!;
        Assert.Equal(pe / pa * b.C.SoldierPayMult * kK * perHead, gotK, 12);
    }

    [Fact]
    public void F3_旗1の兵の増えは空きを超えない()
    {
        var sim = NewSim(); sim.StepQuarter();
        var b = Make(sim, 1, zeroSoldier: true);
        double flow = 0.01 * b.Farm.Size, vac = 0.1 * flow;
        var x = Route(b, flow, vac);
        Assert.True(x.Gain > 0.5 * vac, $"前提：空きまで埋まる流れ（増え {x.Gain:R}・空き {vac:R}）");
        Assert.True(x.Gain <= vac * (1 + 1e-12), $"兵の増え {x.Gain:R} が空き {vac:R} を超えた");
        Assert.True(Math.Abs(x.PopDiff) <= 1e-9 * flow, "州の人口が保存されない");
    }

    [Fact]
    public void F4_大きさが正の兵の席は自分の給与で読み新しい読みに落ちない()
    {
        var sim = NewSim(); sim.StepQuarter();
        var b = Make(sim, 1, zeroSoldier: false);
        Assert.True(b.Soldier.Size > 1 && b.Other is not null, "前提：兵の大きさが正・級 0 の競う職");
        b.W.ConscriptionLaw = ConscriptionLaw.Volunteer;
        foreach (var r in b.W.Regions)
            foreach (var p in r.Pops) if (p.IsSoldier && !ReferenceEquals(p, b.Soldier)) p.EarnedPerMonth = new MoneyPerMonth(0);   // 他の州の兵の今の所得 0
        b.Soldier.EarnedPerMonth = new MoneyPerMonth(1000.0 * b.Soldier.Size);   // 首都の兵 1 人の給与 1000
        double perHead = b.Farm.AgeShare(16, 60);
        double seatRead = Unit * b.C.SoldierPayMult * HandRate(b.W, b.C, keep: false);   // 大きさ 0 の読みで読んだときの首都の兵 1 人の給与（国の比で薄まる）
        Assert.True(seatRead < 900.0, $"前提：国の比で読むと首都の兵の給与が 900 を下回る盤面（{seatRead:R}）");
        b.Other!.EarnedPerMonth = new MoneyPerMonth(900.0 * perHead * b.Other.Size);   // 競う職＝兵の今の読みの 0.9 倍（新しい読みより上）
        double flow = 0.01 * b.Farm.Size;
        var x = Route(b, flow, 1e9);
        Assert.True(x.Gain > 0.5 * (x.Gain + x.OtherGain) && x.Gain > 1e-6 * flow,
            $"大きさが正の兵の席が自分の給与で読まれていない（兵 {x.Gain:R}・競う職 {x.OtherGain:R}）");
    }

    [Fact]
    public void F5_計器soldiervac_sは大きさ0の州の数と埋まりを手の数えと一致させる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_sv_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables("soldiervac_s");
        var sim = NewSim();
        string uk; int zero = 0; double after = 0;
        try
        {
            Instruments.Phase(sim, Seed, "open");
            sim.StepQuarter(); Instruments.Quarter(sim, Seed, 0);
            var b = Make(sim, 1, zeroSoldier: true);
            uk = Instruments.NameOf(b.W);
            var zeroRegions = new List<Pop>();
            foreach (var r in b.W.Regions)
            {
                double tot = 0; Pop? s = null;
                foreach (var p in r.Pops) { tot += p.Size; if (p.IsSoldier) s ??= p; }
                if (s is null || tot <= 1e-9 || r.MeritFromIndex < 0 || r.MeritToIndex < 0) continue;
                if (s.Size <= 1e-9) zeroRegions.Add(s);
            }
            zero = zeroRegions.Count;
            Meritocracy.Step(b.W, b.C);
            foreach (var s in zeroRegions) after += s.Size;
            Instruments.Quarter(sim, Seed, 1);
        }
        finally { Instruments.Close(sim); Instruments.SelectTables(null); Instruments.Configure(null, null); }
        try
        {
            var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"soldiervac_s{Seed}.tsv"));
            var head = lines[0].Split('\t');
            Assert.Equal(Instruments.SoldierVacHeaderText.Split('\t'), head);
            int iq = Array.IndexOf(head, "q"), inat = Array.IndexOf(head, "nation");
            string[]? row = null; int rows = 0;
            foreach (var l in lines.Skip(1))
            {
                var x = l.Split('\t'); Assert.Equal(head.Length, x.Length); rows++;
                if (x[iq] == "1" && x[inat] == uk) row = x;
            }
            Assert.True(rows >= 4, $"前提：行の数 {rows}（2 国×2 四半期）");
            Assert.NotNull(row);
            double Col(string n) { int i = Array.IndexOf(head, n); Assert.True(i >= 0, n); return double.Parse(row![i], System.Globalization.CultureInfo.InvariantCulture); }
            Assert.True(zero >= 1, "前提：大きさ 0 の兵の州が 1 つ以上");
            Assert.Equal(zero, (int)Col("zeroStart"));
            Assert.Equal(after, Col("fillZeroK"), 9);
            Assert.True(Col("overfillZeroK") <= 1e-9, "空きを超えた埋まり");
            Assert.True(Col("eqN") >= 1, "前提：大きさが正の兵の州が 1 つ以上（英の他の州）");
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch (System.IO.IOException) { } }
    }
}
