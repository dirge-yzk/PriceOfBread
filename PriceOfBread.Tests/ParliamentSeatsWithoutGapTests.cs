using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1118) c108ps 旗 ParliamentSeatsWithoutGap（開院の空白を閉じる）の機構の番人。事前登録 measurements/m1118_20261114_prereg_ps.txt の [3]。
/// 主張＝(a) 開幕の選挙政体は四半期 0 に席が座り、席は保守 100% の退避ではなく、与党は席の第一党 (b) 非選挙から選挙の政体へ移った次の 1 回の Parliament.Step で席が座る
/// (c) 議会の中の思想の 1 回は思想が無い時だけ（2 回目は走らない）。どれも旗 0 の対照を置く。
/// 期待値は試験の中で盤面から取る（Parliament の Winner・HasIdeology を呼ばない＝同じ出所から作らない）。
/// </summary>
public class ParliamentSeatsWithoutGapTests
{
    private static double SeatSum(World w) => w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2];

    private static int SeatFirst(World w)
    {
        int b = 0;
        for (int i = 1; i < 3; i++) if (w.PartySeats[i] > w.PartySeats[b]) b = i;
        return b;
    }

    private static bool AnyIdeology(World w)
    {
        foreach (var p in w.AllPops())
        {
            double s = 0;
            for (int i = 0; i < p.IdeologyShares.Length; i++) s += p.IdeologyShares[i];
            if (s > 1e-9) return true;
        }
        return false;
    }

    private static World America(double flag)
    {
        var sim = new Simulation(NationConfig.America().ToScenario(29), new NullAgent());
        var w = sim.World;
        w.Coef.ParliamentSeatsWithoutGap = flag;
        Assert.True(w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic, $"前提：開幕は選挙の政体（{w.Polity}）");
        Assert.Equal(0.0, SeatSum(w));
        Assert.False(AnyIdeology(w), "前提：開幕は思想が無い");
        sim.StepQuarter();
        Assert.True(w.ParliamentEnabled && w.PoliticsEnabled && !w.InCivilWar, "前提：議会が動く世界");
        return w;
    }

    [Fact]
    public void 開幕の選挙政体は四半期0に席が座り_保守の退避ではなく_与党は席の第一党()
    {
        var w = America(1);
        Assert.True(SeatSum(w) > 0.5, $"四半期 0 に席が座る（和 {SeatSum(w)}）");
        Assert.True(w.PartySeats[0] < 0.99, $"保守 100% の退避ではない（保守 {w.PartySeats[0]}）");
        Assert.Equal(SeatFirst(w), w.GoverningParty);
        var w0 = America(0);
        Assert.Equal(0.0, SeatSum(w0));   // 対照：旗 0 は四半期 0 の席が空
    }

    private static World France(double flag)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(29), new NullAgent());
        sim.World.Coef.ParliamentSeatsWithoutGap = flag;
        sim.StepQuarter(); sim.StepQuarter();
        var w = sim.World;
        Assert.True(w.ParliamentEnabled && w.PoliticsEnabled && !w.InCivilWar, "前提：議会が動く世界");
        Assert.False(w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic, $"前提：開幕は非選挙の政体（{w.Polity}）");
        Assert.True(w.Polity is not (Polity.BourgeoisDictatorship or Polity.ProletarianDictatorship), "前提：政体の札が保守（0）の政体");
        w.Suffrage = Suffrage.Universal;
        foreach (var p in w.AllPops()) { Array.Clear(p.IdeologyShares); p.IdeologyShares[(int)Ideology.Liberalism] = 1.0; }
        Parliament.Step(w, w.Coef);
        Assert.Equal(0.0, SeatSum(w));   // 前提：非選挙の政体は席が空（NonElectiveSeatsEmpty 1）
        Assert.Equal(0, w.GoverningParty);
        w.Polity = Polity.ConstitutionalMonarchy;   // 移行
        Parliament.Step(w, w.Coef);   // 1 回だけ
        return w;
    }

    [Fact]
    public void 非選挙から選挙の政体へ移った次の1回で席が座り_与党は席の第一党()
    {
        var w = France(1);
        Assert.True(SeatSum(w) > 0.5, $"移行の次の 1 回で席が座る（和 {SeatSum(w)}）");
        Assert.Equal(1, SeatFirst(w));   // 盤面の思想（自由主義）から
        Assert.Equal(1, w.GoverningParty);
        var w0 = France(0);
        Assert.Equal(0.0, SeatSum(w0));   // 対照：旧の規則は QuartersSinceElection≥1 を待つ＝1 回では空
    }

    [Fact]
    public void 議会の中の思想の1回は思想が無い時だけ走る()
    {
        var sim = new Simulation(NationConfig.America().ToScenario(29), new NullAgent());
        var w = sim.World;
        w.Coef.ParliamentSeatsWithoutGap = 1;
        sim.StepQuarter();
        Assert.True(SeatSum(w) > 0.5 && AnyIdeology(w), "前提：四半期 0 で席と思想が有る");
        // 席を空に戻す（思想は有る）＝移行の次の四半期と同じ形：開院はするが思想の更新は走らない
        w.PartySeats[0] = 0; w.PartySeats[1] = 0; w.PartySeats[2] = 0;
        long before = w.Cost[CostLedger.ParticipationPops];
        Parliament.Step(w, w.Coef);
        Assert.Equal(before, w.Cost[CostLedger.ParticipationPops]);
        Assert.True(SeatSum(w) > 0.5, "対照：席は座った（開院の枝を通った）");
        // 思想を消すと走る（ParticipationPops が増える）＝上の Assert が何も数えない盤面でないことの確かめ
        foreach (var p in w.AllPops()) Array.Clear(p.IdeologyShares);
        w.PartySeats[0] = 0; w.PartySeats[1] = 0; w.PartySeats[2] = 0;
        long before2 = w.Cost[CostLedger.ParticipationPops];
        Parliament.Step(w, w.Coef);
        Assert.True(w.Cost[CostLedger.ParticipationPops] > before2, "思想が無い時は議会の中で思想を出す");
        Assert.True(AnyIdeology(w));
    }
}
