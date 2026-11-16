using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1172) 線 c116t1 段 T1-0′：計器 t1buy_s の行の束（T1RowAcc）の自己検査。事前登録 measurements/m1172_20261116_prereg_t1.txt [4]。
/// 盤面は試験の中で手で置く（★(1136)）。期待値は手で置いた数（計器の式を写さない）。
/// 盤面 A＝access 0.5 の形・同じ行に 3 つの成約・Q＜P：退きは行ごとに 1 回（突然変異 M7〈成約ごとに足す〉で 23）・影を越えた量 2.625（M8〈影に Q〉で 0）・
///   上界 E≦R＋spread を守る（M11〈影の上界に access を掛ける〉で E＝11.5＞10＝errOverBound 1）。
/// 盤面 B＝spread 0・access 1：E＝R＝上界にちょうど一致（errOverBound 0）。盤面 C＝旗 0 の形（エンジンの Q が NaN・旧の天井の内）：退き 0・越えた量 0。
/// </summary>
public class TradeT1RowAccTests
{
    private const double D = 100, Ts = 20, S0 = 30, I0 = 0, P = 50, Pow2e = 10;   // D·pow＝1000（値段の側は効かない）・root 1 → 影の天井 Y_sh＝min(950, 40)＝40

    [Fact]
    public void 行の束は退きを1回数え上界を守る()
    {
        // 盤面 A：Q＝45, 42, 40・qA＝0.5×(Y_A1−y)＝22.5, 12.75, 7.375（Y_A1＝45, 48, 50）。
        var nA = new Instruments.T1N(); var a = new Instruments.T1RowAcc();
        Instruments.T1AccBegin(a, nA, P, D, Ts, S0, I0);
        Instruments.T1AccDeal(a, 22.5, true, 45, 45, Pow2e, 1.0);
        Instruments.T1AccDeal(a, 12.75, true, 42, 42, Pow2e, 1.0);
        Instruments.T1AccDeal(a, 7.375, false, 40, 40, Pow2e, 1.0);
        Instruments.T1AccFlush(a);
        Assert.Equal(1, nA.RowsDealt);
        Assert.Equal(1, nA.RowsDealtX);
        Assert.Equal(1, nA.RetreatRowN);
        Assert.Equal(10.0, nA.RetreatRowSum, 9);       // P−min Q＝50−40（成約ごとに足すと 5＋8＋10＝23）
        Assert.Equal(10.0, nA.RetreatRowInsSum, 9);
        Assert.Equal(0, nA.ErrOverBound);              // 2.625≦10＋0（影の上界に access を掛けると 11.5＞10＝1）
        Assert.Equal(2.625, nA.ExcessQtyAll, 9);       // 3 本目だけ：7.375−(40−35.25)
        Assert.Equal(0.0, nA.ExcessQtyX, 9);           // 3 本目は国内
        Assert.Equal(0, nA.ErrNegQ);
        Assert.Equal(0, nA.ErrOverIns);
        // 越境の割合で振り分けた上側：max Y_A1＝50・買いの和 42.625・越境 35.25 → (50−42.625)×35.25÷42.625
        Assert.Equal((50 - 42.625) * 35.25 / 42.625, nA.ExcessQtyInsX, 9);

        // 盤面 B：Q＝40 で 30＋20（access 1）→ E＝20−(40−30)＝10＝R・spread 0＝上界にちょうど一致。
        var nB = new Instruments.T1N(); var b = new Instruments.T1RowAcc();
        Instruments.T1AccBegin(b, nB, P, D, Ts, S0, I0);
        Instruments.T1AccDeal(b, 30, true, 40, 40, Pow2e, 1.0);
        Instruments.T1AccDeal(b, 20, true, 40, 40, Pow2e, 1.0);
        Instruments.T1AccFlush(b);
        Assert.Equal(10.0, nB.ExcessQtyX, 9);
        Assert.Equal(10.0, nB.RetreatRowSum, 9);
        Assert.Equal(0, nB.ErrOverBound);

        // 盤面 C：旗 0 の形（エンジンの Q は NaN・qA は旧の天井 TS＋D−P−S0−y＝40−y の内）＝退き 0・越えた量 0・上界の誤り 0。計器の影の退きは出る（門にしない）。
        var nC = new Instruments.T1N(); var cc = new Instruments.T1RowAcc();
        Instruments.T1AccBegin(cc, nC, P, D, Ts, S0, I0);
        Instruments.T1AccDeal(cc, 25, true, double.NaN, 45, Pow2e, 1.0);
        Instruments.T1AccDeal(cc, 15, true, double.NaN, 42, Pow2e, 1.0);
        Instruments.T1AccFlush(cc);
        Assert.Equal(1, nC.RowsDealt);
        Assert.Equal(0, nC.RetreatRowN);
        Assert.Equal(0.0, nC.RetreatRowSum);
        Assert.Equal(0.0, nC.ExcessQtyX);
        Assert.Equal(0.0, nC.ExcessQtyAll);
        Assert.Equal(0, nC.ErrOverBound);
        Assert.Equal(8.0, nC.RetreatRowInsSum, 9);     // 50−42

        // 退きが P を越える誤り（エンジンの Q＞P）は成約ごとに数える。閉じた行の後の成約は束ねない。
        var nD = new Instruments.T1N(); var d = new Instruments.T1RowAcc();
        Instruments.T1AccBegin(d, nD, P, D, Ts, S0, I0);
        Instruments.T1AccDeal(d, 1, true, 51, 50, Pow2e, 1.0);
        Instruments.T1AccFlush(d);
        Instruments.T1AccDeal(d, 1, true, 52, 50, Pow2e, 1.0);
        Instruments.T1AccFlush(d);
        Assert.Equal(1, nD.ErrNegQ);
        Assert.Equal(1, nD.RowsDealt);
        Assert.Equal(0, nD.RetreatRowN);
    }
}
