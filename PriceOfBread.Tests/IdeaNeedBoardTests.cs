using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(640) 線 idn：思想の必要性を盤面の量から読む旗 4 本の番人（機構と不変条件）。事前登録 measurements/idn640_20261028_prereg.txt の [5]。
/// 旗 <see cref="Coefficients.SpreadNeedSocialismBoard"/>・<see cref="Coefficients.SpreadNeedCommunismBoard"/>・<see cref="Coefficients.SpreadNeedFascismDecline"/>・<see cref="Coefficients.IdeologyDevTermOff"/>（既定 0＝今の式）。
/// 期待値は本体の関数から作らない：兵士でない POP と工場労働者の人数・充足・取り分は盤面の POP を数えて手で組む。
/// ⚠壊し方（突然変異で赤を確かめる）＝不足を 1−fill でなく fill にする／兵士を分母に入れる／落ち込みの符号を逆にする／在庫 NaN を通す／旗 0 でも新しい式を読む／取り分の分母に賃金を入れない。</summary>
public class IdeaNeedBoardTests
{
    private static int Row(string name) => System.Array.FindIndex(Catalog.Discoveries, d => d.Name == name);

    private static MultiNationSim Board(ulong seed = 7)
    {
        var sim = new MultiNationSim(WorldData.Nations(), seed, _ => new UtilityAgent());
        sim.StepQuarter();
        return sim;
    }

    private static World Nation(MultiNationSim sim, string name)
        => sim.Nations.Find(n => n.World.SelfPower?.BaseName == name)?.World ?? throw new Xunit.Sdk.XunitException($"{name} が居ない");

    private static IEnumerable<Pop> Civ(World w) => w.Regions.SelectMany(r => r.Pops).Where(p => !p.IsSoldier && p.Size > 0);
    private static IEnumerable<Pop> Workers(World w) => Civ(w).Where(p => p.ClassId == PopClass.Worker);

    /// <summary>兵士でない全ての POP の充足を fill に揃える（SupplyFill＝(FoodMet+NeedMet)÷(FoodNeed+NeedTotal)）・工場労働者の自給を 0・闘争度を mil に。</summary>
    private static void SetBoard(World w, double fill, double mil = 0.0)
    {
        foreach (var p in Civ(w)) { p.FoodNeed = 1; p.FoodMet = fill; p.NeedTotal = 0; p.NeedMet = 0; p.Militancy = mil; if (p.ClassId == PopClass.Worker) p.Subsistence = 0; }
    }

    private static double Sat(double s, Coefficients c) => Discovery.TermMax * s / (s + c.DiscoveryNeedHalfIdea);

    /// <summary>前提の行（共産主義 ← 社会主義 など）を既知にして、includeKnown で全ての思想の行の点を読む。</summary>
    private static Discovery.Terms[] Terms(World w, Discovery.WorldView? view = null)
    {
        foreach (int row in Discovery.SpreadRows) { int after = Catalog.DiscoveryAfter[row]; if (after >= 0) w.Discovered[after] = true; }
        return Discovery.Evaluate(w, view, w.Coef, includeKnown: true);
    }

    private static double WorkerOverCiv(World w) => Workers(w).Sum(p => p.Size) / Civ(w).Sum(p => p.Size);

    [Fact]
    public void 旗0は今の式_社会主義と共産主義は工場労働者の人数かけ闘争度で同じ量_ファシズムは危機かけ闘争度()
    {
        var w = Nation(Board(), "イギリス");
        SetBoard(w, fill: 0.6, mil: 0.5);
        Assert.Equal(0.0, w.Coef.SpreadNeedSocialismBoard); Assert.Equal(0.0, w.Coef.SpreadNeedCommunismBoard); Assert.Equal(0.0, w.Coef.SpreadNeedFascismDecline);
        double wc = WorkerOverCiv(w); Assert.True(wc > 0);
        var t = Terms(w);
        double old = Sat(0.5 * wc, w.Coef);
        Assert.Equal(old, t[Discovery.SocialismRow].Need, 9);
        Assert.Equal(old, t[Discovery.CommunismRow].Need, 9);
        // 陽性対照：旗 1 にすると社会主義だけ別の量になる（不足 0.4 × 人口比）
        w.Coef.SpreadNeedSocialismBoard = 1;
        t = Terms(w);
        Assert.Equal(Sat(0.4 * wc, w.Coef), t[Discovery.SocialismRow].Need, 9);
        Assert.NotEqual(old, t[Discovery.SocialismRow].Need, 6);
        Assert.Equal(old, t[Discovery.CommunismRow].Need, 9);   // 共産主義の旗は別
    }

    [Fact]
    public void 社会主義は賃金だけで暮らす人の不足の人数割り_充足が上がれば下がる()
    {
        var w = Nation(Board(), "イギリス");
        w.Coef.SpreadNeedSocialismBoard = 1;
        double wc = WorkerOverCiv(w);
        SetBoard(w, fill: 0.9, mil: 0.0);
        double hi = Terms(w)[Discovery.SocialismRow].Need;
        SetBoard(w, fill: 0.6, mil: 0.0);
        double lo = Terms(w)[Discovery.SocialismRow].Need;
        Assert.Equal(Sat(0.1 * wc, w.Coef), hi, 9);   // 不足 0.1
        Assert.Equal(Sat(0.4 * wc, w.Coef), lo, 9);   // 不足 0.4
        Assert.True(lo > hi);
        SetBoard(w, fill: 1.0, mil: 1.0);
        Assert.Equal(0.0, Terms(w)[Discovery.SocialismRow].Need);   // 満たされていれば闘争度が高くても 0（闘争度を読まない）
    }

    [Fact]
    public void 工場労働者以外の不足は数えない_兵士は分母に入れない()
    {
        var w = Nation(Board(), "イギリス");
        w.Coef.SpreadNeedSocialismBoard = 1;
        SetBoard(w, fill: 1.0);
        foreach (var p in Civ(w).Where(p => p.ClassId != PopClass.Worker)) { p.FoodMet = 0; }   // 工場労働者でない POP は全く食えていない
        Assert.Equal(0.0, Terms(w)[Discovery.SocialismRow].Need);   // 工場労働者は充足 1＝強度 0
        // 兵士にした POP は分母から外れる（人口比が上がる）
        SetBoard(w, fill: 0.5);
        var big = Civ(w).Where(p => p.ClassId == PopClass.Peasant).OrderByDescending(p => p.Size).First();
        double before = Terms(w)[Discovery.SocialismRow].Need;
        Assert.Equal(Sat(0.5 * WorkerOverCiv(w), w.Coef), before, 9);
        big.IsSoldier = true;
        double after = Terms(w)[Discovery.SocialismRow].Need;
        Assert.Equal(Sat(0.5 * WorkerOverCiv(w), w.Coef), after, 9);   // WorkerOverCiv は兵士を外した分母で数え直す
        Assert.True(after > before);
    }

    [Fact]
    public void 共産主義は持ち主の取り分かける賃金だけで暮らす人の人口比_取り分が増えれば増え_稼ぎが無ければ0()
    {
        var w = Nation(Board(), "イギリス");
        w.Coef.SpreadNeedCommunismBoard = 1;
        SetBoard(w, fill: 0.8, mil: 0.7);
        var workers = Workers(w).ToList(); Assert.NotEmpty(workers);
        var owner = Civ(w).First(p => p.ClassId == PopClass.Capitalist);
        foreach (var p in Civ(w)) { p.IndustrialRentPerMonth = new MoneyPerMonth(0); p.EarnedPerMonth = new MoneyPerMonth(0); }
        Assert.Equal(0.0, Terms(w)[Discovery.CommunismRow].Need);   // 稼ぎが全く無い＝分母が 0＝取り分 0
        owner.IndustrialRentPerMonth = new MoneyPerMonth(30);
        foreach (var p in workers) p.EarnedPerMonth = new MoneyPerMonth(70.0 / workers.Count);   // 賃金の合計 70
        double wc = WorkerOverCiv(w);
        double at30 = Terms(w)[Discovery.CommunismRow].Need;
        Assert.Equal(Sat(0.3 * wc, w.Coef), at30, 9);   // 取り分 30÷(30+70)
        owner.IndustrialRentPerMonth = new MoneyPerMonth(70);
        double at70 = Terms(w)[Discovery.CommunismRow].Need;
        Assert.Equal(Sat(0.5 * wc, w.Coef), at70, 9);   // 取り分 70÷(70+70)
        Assert.True(at70 > at30);
        // 闘争度は読まない（旗 1 の共産主義は今の式 工場労働者の人数×闘争度 と別の量）
        Assert.NotEqual(Sat(0.7 * wc, w.Coef), at70, 6);
    }

    [Fact]
    public void ファシズムは危機かけ闘争度かけ国力の落ち込み_在庫が無ければ0_今が在庫以上なら0()
    {
        var w = Nation(Board(), "イギリス");
        SetBoard(w, fill: 0.8, mil: 0.5);
        w.DefeatShock = 1.0;   // 危機＝敗戦か恐慌 × 左派の脅威
        foreach (var p in Civ(w)) { System.Array.Clear(p.IdeologyShares); p.IdeologyShares[(int)Ideology.Socialism] = 1.0; p.Consciousness = 1.0; }
        double crisis = Ideologies.FascismCrisis(w);
        Assert.True(crisis > 0, "前提：危機が正");
        int fas = Discovery.FascismRow;
        w.ScoreStanding = 50;
        // 旗 0：落ち込みを掛けない（今の式 危機×人々の闘争度＝0.5）
        Assert.Equal(Sat(crisis * 0.5, w.Coef), Terms(w, new Discovery.WorldView(null, ScoreNow: 40.0))[fas].Need, 9);
        w.Coef.SpreadNeedFascismDecline = 1;
        Assert.Equal(Sat(crisis * 0.5 * 0.2, w.Coef), Terms(w, new Discovery.WorldView(null, ScoreNow: 40.0))[fas].Need, 9);   // (50−40)÷50
        Assert.Equal(0.0, Terms(w, new Discovery.WorldView(null, ScoreNow: 50.0))[fas].Need);       // 今が在庫と同じ＝落ち込み 0
        Assert.Equal(0.0, Terms(w, new Discovery.WorldView(null, ScoreNow: 70.0))[fas].Need);       // 今が在庫より上＝0（符号を逆にしない）
        Assert.Equal(0.0, Terms(w, null)[fas].Need);                                                 // 単独の世界（今が無い）＝0
        w.ScoreStanding = double.NaN;
        Assert.Equal(0.0, Terms(w, new Discovery.WorldView(null, ScoreNow: 40.0))[fas].Need);       // 在庫が未初期化＝0（NaN を通さない）
    }

    [Fact]
    public void 国力の落ち込みの関数は在庫が正でないか今が無ければ0_上限は1()
    {
        Assert.Equal(0.0, Discovery.ScoreDecline(double.NaN, 30.0));
        Assert.Equal(0.0, Discovery.ScoreDecline(0.0, 30.0));
        Assert.Equal(0.0, Discovery.ScoreDecline(-5.0, 30.0));
        Assert.Equal(0.0, Discovery.ScoreDecline(50.0, null));
        Assert.Equal(0.0, Discovery.ScoreDecline(50.0, double.NaN));
        Assert.Equal(0.0, Discovery.ScoreDecline(50.0, -1.0));
        Assert.Equal(0.0, Discovery.ScoreDecline(50.0, 60.0));
        Assert.Equal(0.2, Discovery.ScoreDecline(50.0, 40.0), 12);
        Assert.Equal(1.0, Discovery.ScoreDecline(50.0, 0.0), 12);
    }

    [Fact]
    public void 発展度の旗は発展度を1に固定し_旗0は工業化度の項と都市の割合の項を読んだ1未満の値()
    {
        var w = Nation(Board(), "イギリス");
        Assert.Equal(0.0, w.Coef.IdeologyDevTermOff);
        double dev0 = Ideologies.EraGates(w).Dev;
        Assert.InRange(dev0, 0.0, 0.999);
        w.Coef.IdeologyDevTermOff = 1;
        Assert.Equal(1.0, Ideologies.EraGates(w).Dev);
    }

    [Fact]
    public void 計器の盤面の量は本体と同じ関数を読む_旧の3部品は本体の旧の強度と一致()
    {
        var w = Nation(Board(), "イギリス");
        SetBoard(w, fill: 0.7, mil: 0.4);
        var b = Discovery.IdeaBoardOf(w, 30.0);
        double wc = WorkerOverCiv(w);
        Assert.Equal(0.4 * wc, b.WorkerMil, 12);
        Assert.Equal(0.3 * wc, b.WageUnmet, 12);
        Assert.Equal(wc, b.WageShare, 12);
        Assert.Equal(0.4, b.PopMil, 12);
        Assert.Equal(0.7, b.WorkerSupplyFill, 12);
    }
}
