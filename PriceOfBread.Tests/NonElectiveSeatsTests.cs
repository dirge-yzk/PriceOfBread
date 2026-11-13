using System;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1099) c105gp 旗 NonElectiveSeatsEmpty の機構の番人（台帳 S18-31・推奨 (a)）。
/// 主張＝①非選挙の政体は議席を持たない（和 0）②非選挙から選挙の政体へ移ったら、最初の選挙より前に、開院で与党＝議席の第一党
/// ③選挙の政体が続く間、与党が替わるのは選挙の四半期（総選挙の数が増えた四半期）だけ。
/// 期待値は試験の中で議席から直に取る（Parliament の Winner・RulingParty を呼ばない＝同じ出所から作らない）。
/// 盤面＝フランスの開幕（非選挙）を 2 四半期進め、全ての POP の思想を 1 つに揃えて得票の第一党を政体の札（保守）と違う党にする。
/// </summary>
public class NonElectiveSeatsTests
{
    private static World Board(double flag)
    {
        var sim = new Simulation(NationConfig.France().ToScenario(29), new NullAgent());
        sim.World.Coef.NonElectiveSeatsEmpty = flag;
        sim.StepQuarter(); sim.StepQuarter();
        var w = sim.World;
        Assert.True(w.ParliamentEnabled && w.PoliticsEnabled, "前提：議会が動く世界");
        Assert.False(w.Polity is Polity.ConstitutionalMonarchy or Polity.Republic, $"前提：開幕は非選挙の政体（{w.Polity}）");
        Assert.False(w.InCivilWar, "前提：内戦でない");
        Assert.True(w.Polity is not (Polity.BourgeoisDictatorship or Polity.ProletarianDictatorship), "前提：政体の札が保守（0）の政体");
        w.Suffrage = Suffrage.Universal;
        SetIdeology(w, Ideology.Liberalism);
        return w;
    }

    private static void SetIdeology(World w, Ideology i)
    {
        foreach (var p in w.AllPops()) { Array.Clear(p.IdeologyShares); p.IdeologyShares[(int)i] = 1.0; }
    }

    private static double SeatSum(World w) => w.PartySeats[0] + w.PartySeats[1] + w.PartySeats[2];

    private static int SeatFirst(World w)
    {
        int b = 0;
        for (int i = 1; i < 3; i++) if (w.PartySeats[i] > w.PartySeats[b]) b = i;
        return b;
    }

    [Fact]
    public void 非選挙の政体では議席の和が0()
    {
        var w = Board(1);
        w.PartySeats[0] = 0.5; w.PartySeats[1] = 0.3; w.PartySeats[2] = 0.2;   // 前の議席が残っている盤面（選挙の政体から転げた国）
        Parliament.Step(w, w.Coef);
        Assert.Equal(0.0, w.PartySeats[0]);
        Assert.Equal(0.0, w.PartySeats[1]);
        Assert.Equal(0.0, w.PartySeats[2]);
        Assert.True(w.VoteShares[1] > 0.5, $"対照：得票は毎期出ている（自由 {w.VoteShares[1]}）＝議席だけが空");
    }

    [Fact]
    public void 非選挙から立憲へ移ると最初の選挙の前に与党は議席の第一党()
    {
        var w = Board(1);
        Parliament.Step(w, w.Coef);
        Assert.Equal(0.0, SeatSum(w));
        Assert.Equal(0, w.GoverningParty);   // 非選挙の与党＝政体の札（保守）
        int held = w.ElectionsHeld;
        w.Polity = Polity.ConstitutionalMonarchy;   // 移行
        for (int k = 0; k < 2; k++) Parliament.Step(w, w.Coef);
        Assert.Equal(held, w.ElectionsHeld);   // 選挙はまだ無い
        Assert.True(SeatSum(w) > 0.5, $"開院で議席が決まる（和 {SeatSum(w)}）");
        int first = SeatFirst(w);
        Assert.Equal(1, first);   // 対照：議席の第一党が政体の札（保守）と違う盤面
        Assert.Equal(first, w.GoverningParty);
    }

    [Fact]
    public void 選挙の政体が続く間は選挙の四半期の他に与党が替わらない()
    {
        var w = Board(1);
        Parliament.Step(w, w.Coef);
        w.Polity = Polity.ConstitutionalMonarchy;
        for (int k = 0; k < 2; k++) Parliament.Step(w, w.Coef);
        Assert.Equal(1, w.GoverningParty);
        // 得票の第一党を社会へ替える。選挙の前の四半期では与党も議席も動かない
        SetIdeology(w, Ideology.Socialism);
        var seats0 = (double[])w.PartySeats.Clone();
        int held = w.ElectionsHeld;
        Parliament.Step(w, w.Coef);
        Assert.True(w.VoteShares[2] > 0.5, $"対照：得票の第一党は社会（{w.VoteShares[2]}）");
        Assert.Equal(held, w.ElectionsHeld);
        Assert.Equal(seats0, w.PartySeats);
        Assert.Equal(1, w.GoverningParty);
        // 選挙（不信任の繰り上げか任期満了）まで進める：与党が替わる四半期は総選挙の数が増えた四半期だけ
        int changes = 0;
        for (int k = 0; k < 20; k++)
        {
            int gp = w.GoverningParty, h = w.ElectionsHeld;
            Parliament.Step(w, w.Coef);
            if (w.GoverningParty != gp)
            {
                changes++;
                Assert.True(w.ElectionsHeld == h + 1, $"与党が選挙の無い四半期に替わった（{gp}→{w.GoverningParty}）");
                Assert.Equal(SeatFirst(w), w.GoverningParty);
            }
        }
        Assert.True(changes >= 1, "対照：選挙で与党が替わる四半期がある");
    }

    [Fact]
    public void 旗0では非選挙の議席は得票の写しで移行の後の与党は政体の札のまま()
    {
        var w = Board(0);
        Parliament.Step(w, w.Coef);
        Assert.True(SeatSum(w) > 0.5, "旗 0＝非選挙の政体でも議席は毎期の得票の写し（今のまま）");
        w.Polity = Polity.ConstitutionalMonarchy;
        for (int k = 0; k < 2; k++) Parliament.Step(w, w.Coef);
        Assert.Equal(1, SeatFirst(w));
        Assert.Equal(0, w.GoverningParty);   // 旗 0 の型＝議席の第一党と与党が食い違う（S18-31 の元の形）
    }
}
