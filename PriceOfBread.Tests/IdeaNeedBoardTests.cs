using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(640) 線 idn：思想の必要性を盤面の量から読む旗の番人（機構と不変条件）。事前登録 measurements/idn640_20261028_prereg.txt の [5]。
/// 旗 <see cref="Coefficients.SpreadNeedFascismDecline"/>（★(640) 採用：ファシズムの落ち込みは既定 1・発展度の旗 IdeologyDevTermOff は読み手ごと消した・社会主義と共産主義の盤面の旗は (798) で消した）。
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
        foreach (var p in Civ(w)) { p.FoodNeed = 1; p.FoodMet = fill; p.NeedTotal = 0; p.NeedMet = 0; p.Militancy = mil; if (p.ClassId == PopClass.Worker) p.SubsistenceBasis = 0; }
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
        Assert.Equal(1.0, w.Coef.SpreadNeedFascismDecline);   // 既定＝ファシズムの落ち込みは点火済み（1）。社会主義と共産主義の盤面の旗は (798) で消した
        w.Coef.SpreadNeedFascismDecline = 0;   // この試験は旗 0（落ち込みを掛けない旧の式）の経路
        double wc = WorkerOverCiv(w); Assert.True(wc > 0);
        var t = Terms(w);
        double old = Sat(0.5 * wc, w.Coef);
        Assert.Equal(old, t[Discovery.SocialismRow].Need, 9);
        Assert.Equal(old, t[Discovery.CommunismRow].Need, 9);
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
        w.Coef.SpreadNeedFascismDecline = 0;
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
    public void 発展度は読み手ごと消え_ファシズムの支持の式は工業化度を掛けない_最先端の工業化度を動かしても同じ支持()
    {
        // 発展度 dev（世界最先端の工業化度と都市の割合の加重）は消した：EraGateSet は 4 つの解禁度だけ・最先端の工業化度を動かしても解禁度と思想のシェアが変わらない
        var names = typeof(Ideologies.EraGateSet).GetProperties().Select(x => x.Name).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "Communism", "Fascism", "Imperialism", "Socialism" }, names);
        Assert.Null(typeof(Coefficients).GetField("IdeologyDevTermOff"));
        Assert.Null(typeof(Coefficients).GetField("IdeologyDevFromUrban"));
        // ★(645) 合流（反証役 idn〔bv〕中）：1783 年の盤面ではファシズムの行が未発見＝r[9]=0 で項が空だった。ファシズムを既知にし闘争度を正にして、前提 r[9]>0 を確かめてから最先端の工業化度を動かす。
        double[] Support(double frontier)
        {
            var w = Nation(Board(), "イギリス");
            w.Discovered[Discovery.FascismRow] = true;
            foreach (var p in Civ(w)) { p.Militancy = 0.6; p.LivingRef = p.Satisfaction + 0.75; }   // (1398) c149sd：既定（旗 SatReadersToDiscontent 1）の思想は闘争度でなく不満度 D を読む＝D＝0.8×0.75＝0.6 も置く（旗 0 では闘争度 0.6 が同じ役）
            w.IndustrialFrontier = frontier; Ideologies.UpdateShares(w); return Ideologies.National(w);
        }
        var a = Support(0.0); var b2 = Support(50.0);
        Assert.True(a[(int)Ideology.Fascism] > 0, $"前提：ファシズムの支持が正（{a[(int)Ideology.Fascism]}）");
        for (int i = 0; i < a.Length; i++) Assert.Equal(a[i], b2[i]);
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
