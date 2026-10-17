using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(393) 軍の力の入口の作り直し（旗 <c>HoldSplitBySoldierIdeology</c>）の番人。事前登録 measurements/mc_20261018_prereg.txt。
/// 形＝①軍政の支持への足し算 CoupSupport を行わない ②引力点が決まった後、保持力 κ を兵士 POP だけの思想で守る側 κ×a と挑戦の側 κ×(1−a) に分ける
/// ③MeasureMobilization は兵士の規模×意識を動員に足さない。期待値は数値と政体のリテラルで書く（本体の関数から作らない）。
/// 突然変異（足し算を残す／a の向きを逆にする／分けを挑戦へ足さない／縁の a を 0 にする／兵士でない POP も集計する／兵士を動員から外さない）で赤になることを確かめた（実装ログ (393)）。</summary>
public class SoldierHoldSplitTests
{
    private const int Trad = 0, Nat = 4;   // 思想の添字：0 伝統・4 ナショ

    /// <summary>仏 1789 の盤面。全 POP を一つの思想に、兵士 POP だけ別の思想に揃える（IdeologyShares は Politics.Step が毎四半期書き直す＝Step を通さず Transition を直接叩く）。</summary>
    private static World Board(double flag, int civilIdeology, int soldierIdeology, double slope = 4.0)
    {
        var w = new Simulation(NationConfig.France().ToScenario(1789), new NullAgent()).World;
        w.Coef.HoldSplitBySoldierIdeology = flag;
        w.Coef.IdeologyAffinitySlope = slope;
        w.Coef.BourgeoisCoupWeight = 0;
        foreach (var p in w.AllPops())
        {
            p.Consciousness = 0.5; p.Militancy = 0.3;
            System.Array.Clear(p.IdeologyShares);
            p.IdeologyShares[p.IsSoldier ? soldierIdeology : civilIdeology] = 1.0;
        }
        return w;
    }

    private static double A(World w, Polity cur, Polity att)
        => Politics.SoldierHoldShare(w, w.Coef, Ideologies.BasePolityCoord(cur), att, out _, out _, out _);

    [Fact]
    public void 兵の支持の比aは現政体と引力点の兵士での支持の比で負は0()
    {
        // 傾き 2.0・兵士は全員ナショ・現政体 立憲君主制・引力点 軍事独裁：支持＝2−2.0×距離（座標から手計算：立憲まで √0.605・軍政まで √0.185）
        var w = Board(1.0, Nat, Nat, slope: 2.0);
        Assert.Equal(0.28051, A(w, Polity.ConstitutionalMonarchy, Polity.MilitaryDictatorship), 5);
        // 傾き 4.0：ナショ兵の現政体（絶対君主制）への支持は 2−4×0.971＜−1 で床 −1（負は 0）・軍政へは 2−4×0.430＝0.28 → a＝0
        w = Board(1.0, Nat, Nat);
        Assert.Equal(0.0, A(w, Polity.Absolutist, Polity.MilitaryDictatorship));
        // 向きを逆にする：現政体が軍政・引力点が絶対君主制 → 現政体の支持だけが正 → a＝1
        Assert.Equal(1.0, A(w, Polity.MilitaryDictatorship, Polity.Absolutist));
    }

    [Fact]
    public void 兵士の思想が読めないときは守る側が全部でa1()
    {
        var w = Board(1.0, Nat, Nat);
        foreach (var p in w.AllPops()) if (p.IsSoldier) p.Consciousness = 0;   // 重み＝規模×意識×発言権 が 0＝集計は全て 0
        double a = Politics.SoldierHoldShare(w, w.Coef, Ideologies.BasePolityCoord(Polity.Absolutist), Polity.MilitaryDictatorship, out _, out _, out double wsum);
        Assert.Equal(0.0, wsum);
        Assert.Equal(1.0, a);
        // 兵士 POP がまったく無い盤面
        foreach (var p in w.AllPops()) if (p.IsSoldier) p.Size = 0;
        Assert.Equal(1.0, A(w, Polity.Absolutist, Polity.MilitaryDictatorship));
    }

    [Fact]
    public void aは兵士POPだけを読み兵士でないPOPの思想では動かない()
    {
        var w1 = Board(1.0, Trad, Nat, slope: 2.0);
        var w2 = Board(1.0, Nat, Nat, slope: 2.0);   // 民間人の思想だけが違う
        Assert.Equal(A(w1, Polity.ConstitutionalMonarchy, Polity.MilitaryDictatorship), A(w2, Polity.ConstitutionalMonarchy, Polity.MilitaryDictatorship));
        Assert.Equal(0.28051, A(w1, Polity.ConstitutionalMonarchy, Polity.MilitaryDictatorship), 5);
    }

    /// <summary>国民の集計＝ナショ（引力点＝軍政）の盤面で、動員 100・保持力 1000 を直接渡して門 2 を叩く。</summary>
    private static Polity AfterTransition(double flag, int soldierIdeology, double challengeRadical = 100, double coerce = 1000)
    {
        var w = Board(flag, Nat, soldierIdeology);
        Assert.Equal(Polity.Absolutist, w.Polity);
        Politics.Transition(w, w.Coef, 0.0, challengeRadical, coerce, eroding: false);
        return w.Polity;
    }

    [Fact]
    public void 旗0では保持力の分けは無く動員が保持力に届かなければ国は倒れない()
    {
        Assert.Equal(Polity.Absolutist, AfterTransition(0.0, Nat));
        Assert.Equal(Polity.Absolutist, AfterTransition(0.0, Trad));
    }

    [Fact]
    public void 兵が引力点側へ回ると動員が保持力に届かなくても門2が開く()
    {
        // a＝0（兵は全員ナショ＝軍政側）：守る側 κ×0・挑戦 100＋1000×1 ＞ 0 → 軍政へ
        Assert.Equal(Polity.MilitaryDictatorship, AfterTransition(1.0, Nat));
    }

    [Fact]
    public void 兵が今の政体を支えるなら旧の門2と同じ判定で国は倒れない()
    {
        // a＝1（兵は全員伝統＝絶対君主制側）：守る側 κ×1・挑戦 100＋0 ＜ 1000 → 変化なし
        Assert.Equal(Polity.Absolutist, AfterTransition(1.0, Trad));
        // 動員が保持力を超えれば旧と同じく倒れる（分けは a＝1 で恒等）
        Assert.Equal(Polity.MilitaryDictatorship, AfterTransition(1.0, Trad, challengeRadical: 1500));
    }

    [Fact]
    public void 兵が半々なら門2の高さは保持力かけ2aマイナス1まで下がる()
    {
        // 傾き 2.0・兵士の思想は伝統とナショが半々・現政体 絶対君主制・引力点 軍政：a＝0.5487（手計算：現政体への支持 0.9174・軍政への支持 0.7544）
        //   → 守る側 κ×a＝548.7・挑戦は 動員＋κ×(1−a)＝動員＋451.3 → 門 2 は 動員 ＞ κ×(2a−1)＝97.5 のとき開く（保持力 κ＝1000）
        Polity Run(double flag, double mobilization)
        {
            var w = Board(flag, Nat, Trad, slope: 2.0);
            foreach (var p in w.AllPops()) if (p.IsSoldier) { p.IdeologyShares[Trad] = 0.5; p.IdeologyShares[Nat] = 0.5; }
            Politics.Transition(w, w.Coef, 0.0, mobilization, 1000.0, eroding: false);
            return w.Polity;
        }
        Assert.Equal(Polity.MilitaryDictatorship, Run(1.0, 300));   // 300 ＞ 97.5（分けた門）。旧の門なら 300 ＜ 1000 で止まる
        Assert.Equal(Polity.Absolutist, Run(0.0, 300));
        Assert.Equal(Polity.Absolutist, Run(1.0, 20));              // 20 ＜ 97.5 は止まる
    }

    [Fact]
    public void 旗1では軍政の支持へ足し算が無く旗0では兵力が引力点を反転させる()
    {
        // 国民は全員伝統（絶対君主制の支持 2−0.7×0.112≒1.92・軍政 2−0.7×0.815≒1.43＝差 0.49 ＜ 足し算の上限 0.6）。兵力は極大・保持力は微小（取り分 σ≒1）
        foreach (var (flag, expect) in new[] { (0.0, Polity.MilitaryDictatorship), (1.0, Polity.Absolutist) })
        {
            var w = Board(flag, Trad, Trad, slope: 0.7);
            foreach (var p in w.AllPops()) if (p.IsSoldier) { p.Size = 50; p.Militancy = 1.0; }
            w.Coef.PolityInertia = 0.01;
            Politics.Transition(w, w.Coef, 0.0, 1.0, coerce: 1e-3, eroding: false);   // 動員 1 ＞ 保持力 1e-3
            Assert.Equal(expect, w.Polity);
        }
    }

    [Fact]
    public void 動員の測定は旗1で兵士の規模と意識を足さず平均意識の母数は変えない()
    {
        World Measured(double flag, out double conscSum, out double sizeSum)
        {
            var w = Board(flag, Trad, Trad);
            w.ExternalPressure = 0; w.RecentRegimeChanges = 0;
            w.Coef.RiotMilitancyThreshold = 0.9;   // 窮乏の項は出さない
            foreach (var p in w.AllPops()) if (p.IsSoldier) { p.Size = 2.0; p.Consciousness = 0.5; p.Militancy = 0.4; }
            (_, conscSum, sizeSum) = Politics.MeasureMobilization(w, w.Coef);
            return w;
        }
        var w0 = Measured(0.0, out double c0, out double s0);
        var w1 = Measured(1.0, out double c1, out double s1);
        int n = w0.AllPops().Count(p => p.IsSoldier);
        Assert.True(n >= 1);
        Assert.Equal(n * 0.6, w0.ReformistPower - w1.ReformistPower, 9);   // 2.0×0.5×(1−0.4)＝0.6 を兵士 POP の数だけ
        Assert.Equal(n * 0.4, w0.RadicalPower - w1.RadicalPower, 9);       // 2.0×0.5×0.4＝0.4
        Assert.Equal(c0, c1);
        Assert.Equal(s0, s1);
    }
}
