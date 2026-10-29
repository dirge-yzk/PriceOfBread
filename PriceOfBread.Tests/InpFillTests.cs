using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(657) 工場に投入が届かない原因の表（<c>inpf_s</c>・<c>inpb_s</c>）の番人（読むだけの計器・恒等）。事前登録 measurements/inp648_20261030_prereg.txt 追記 2・measurements/igi3_657_20261030_prereg.txt [5]。
/// 期待値は盤面に自分で置いた数（手計算）と、別の出所（本体が書く LastInputFill・出力の別の列）から作る＝計器の式を写さない。
/// C1 理由の分割（理由ごとの months の和＝fill＜0.95 の工場×月の総数）／C2 fill の再計算と本体の LastInputFill の一致／C4 unsupportedRef≧0 と ok 行の fill≧0.95／C5 INS=0 で蓄えが作られない／
/// C6 陽性対照は合成の世界（ある財の解禁を 0 に置き、その財を投入に持つ工場を 1 つ置いて worldUnreleased が付く）＝実在の財や開幕の発見の状態に依存しない。
/// </summary>
[Collection("Instruments")]
public class InpFillTests
{
    private static (Instruments.InpState St, World W, double[] Ration, double[] GoodGate, double[] DemandGate, int[] Pc) Board()
    {
        var sim = new Simulation(NationConfig.Britain().ToScenario(41), new NullAgent());
        var w = sim.World; int ng = Catalog.Goods.Length;
        var st = new Instruments.InpState();
        var gg = new double[ng]; var dg = new double[ng]; var pc = new int[ng];
        Array.Fill(gg, 1.0); Array.Fill(dg, 1.0);
        st.Begin(w, w.Region, ng, gg, dg, pc);
        var ration = new double[ng]; Array.Fill(ration, 1.0);
        return (st, w, ration, gg, dg, pc);
    }

    // 工場 1 つ：製品＝鉄・投入＝inputs・作りたい量 10・産出 5。本体が書いたはずの LastInputFill を fill として置く。
    private static Producer Factory(double lastFill, params int[] inputs)
    {
        var s = new Producer { GoodId = Catalog.IronId, DesiredOutput = 10, Output = 5, LastInputFill = lastFill };
        foreach (var g in inputs) s.Inputs.Add(new Input(g, 1.0));
        return s;
    }

    private static int ReasonOf(Instruments.InpState st, int pg) => st.Rows.Keys.Single(k => k.Good == pg).Reason;

    [Fact]
    public void 合成の世界で理由が上から順に付く_C6()
    {
        int coal = Catalog.CoalId;
        // 世界の解禁も自国の解禁も無い・在庫＋産出 0 → worldUnreleased
        { var (st, _, ra, gg, dg, pc) = Board(); dg[coal] = 0; gg[coal] = 0; ra[coal] = 0; st.Factory(Factory(0, coal), ra); Assert.Equal(Instruments.InpWorldUnreleased, ReasonOf(st, Catalog.IronId)); Assert.True(st.BRows.ContainsKey((coal, Instruments.InpWorldUnreleased))); }
        // 世界の解禁は在る・自国は無い → nationUnreleased
        { var (st, _, ra, gg, dg, pc) = Board(); gg[coal] = 0; ra[coal] = 0; st.Factory(Factory(0, coal), ra); Assert.Equal(Instruments.InpNationUnreleased, ReasonOf(st, Catalog.IronId)); }
        // 解禁は在る・自州にも世界にも売り手が居ない → worldNoSeller
        { var (st, _, ra, gg, dg, pc) = Board(); ra[coal] = 0; st.Factory(Factory(0, coal), ra); Assert.Equal(Instruments.InpWorldNoSeller, ReasonOf(st, Catalog.IronId)); }
        // 世界のどこかに売り手が居る → reachLack
        { var (st, _, ra, gg, dg, pc) = Board(); st.WorldProducers = new int[Catalog.Goods.Length]; st.WorldProducers[coal] = 2; ra[coal] = 0; st.Factory(Factory(0, coal), ra); Assert.Equal(Instruments.InpReachLack, ReasonOf(st, Catalog.IronId)); }
        // 在庫は在るのに誰も求めない（配給率 0） → noOneAsks
        { var (st, _, ra, gg, dg, pc) = Board(); st.Good(coal, 5.0, 0.0, 0, 0, 0, 0); ra[coal] = 0; st.Factory(Factory(0, coal), ra); Assert.Equal(Instruments.InpNoOneAsks, ReasonOf(st, Catalog.IronId)); }
        // 取り合い：最大の取り合い手で割れる
        foreach (var (f, p, i, m, want) in new[] { (9.0, 1.0, 1.0, 1.0, Instruments.InpContF), (1.0, 9.0, 1.0, 1.0, Instruments.InpContP), (1.0, 1.0, 9.0, 1.0, Instruments.InpContI), (1.0, 1.0, 1.0, 9.0, Instruments.InpContM) })
        {
            var (st, _, ra, _, _, _) = Board(); st.Good(coal, 5.0, f + p + i + m, p, f, i, m); ra[coal] = 0.4; st.Factory(Factory(0.4, coal), ra);
            Assert.Equal(want, ReasonOf(st, Catalog.IronId));
        }
        // 労働財が束縛 → labor（在庫の事実に依らない）
        { var (st, _, ra, _, _, _) = Board(); ra[Catalog.LaborId] = 0.2; st.Factory(Factory(0.2, Catalog.LaborId), ra); Assert.Equal(Instruments.InpLabor, ReasonOf(st, Catalog.IronId)); }
        // 役務の優先配分が書いた財 → svcAlloc
        { var (st, _, ra, _, _, _) = Board(); st.SvcMark(coal); ra[coal] = 0.3; st.Factory(Factory(0.3, coal), ra); Assert.Equal(Instruments.InpSvc, ReasonOf(st, Catalog.IronId)); }
    }

    [Fact]
    public void 同点は労働以外を優先し理由が分かれた月を数える_満たされた月は_ok_作りたい量ゼロは別の行_倒産は数えない()
    {
        int coal = Catalog.CoalId;
        // 同点：労働と石炭が両方 0 → 束縛は石炭（労働以外を優先）・理由が分かれる（labor と worldUnreleased）ので nTied
        { var (st, _, ra, gg, dg, _) = Board(); dg[coal] = 0; ra[coal] = 0; ra[Catalog.LaborId] = 0; st.Factory(Factory(0, Catalog.LaborId, coal), ra);
          Assert.Equal(Instruments.InpWorldUnreleased, ReasonOf(st, Catalog.IronId)); Assert.Equal(1, st.Rows.Values.Single().NTied); }
        // fill≧0.95 は ok の 1 行（製品の財に分けない）
        { var (st, _, ra, _, _, _) = Board(); ra[coal] = 0.97; st.Factory(Factory(0.97, coal), ra); Assert.Equal((-1, Instruments.InpOk), st.Rows.Keys.Single()); Assert.Empty(st.BRows); }
        // 作りたい量 0 は zeroDesired.<理由>（作りたい量の和は 0・b 表には入らない）
        { var (st, _, ra, _, _, _) = Board(); ra[Catalog.LaborId] = 0.1; var s = Factory(0.1, Catalog.LaborId); s.DesiredOutput = 0; st.Factory(s, ra);
          var k = st.Rows.Keys.Single(); Assert.Equal(Instruments.InpZeroBase + Instruments.InpLabor, k.Reason); Assert.Equal(0.0, st.Rows[k].Desired); Assert.Empty(st.BRows); }
        // 倒産した生産者・投入の無い生産者は数えない
        { var (st, _, ra, _, _, _) = Board(); var s = Factory(0, coal); s.Bankrupt = true; st.Factory(s, ra); st.Factory(Factory(1.0), ra); Assert.Empty(st.Rows); Assert.Equal(0, st.C2Checked); }
    }

    [Fact]
    public void 重みは_months_で積む_作りたい量と産出と支えのない産出は手計算に合う_C4()
    {
        int coal = Catalog.CoalId;
        var (st, _, ra, _, _, _) = Board(); ra[coal] = 0.2;   // fill 0.2 を 3 か月
        for (int i = 0; i < 3; i++) st.Factory(Factory(0.2, coal), ra);
        var row = st.Rows.Values.Single(); double rp = Catalog.RefPrices[Catalog.IronId];
        Assert.Equal(3, row.Months); Assert.Equal(3, row.N05);   // 0.2 は 0.05〜0.3 の箱
        Assert.Equal(3 * 10 * rp, row.Desired, 9);               // 手計算：作りたい量 10 × 参照価格 × 3 か月
        Assert.Equal(3 * 10 * 0.8 * rp, row.Loss, 9);            // 失い＝作りたい量 ×(1−fill)
        Assert.Equal(3 * 5 * rp, row.Output, 9);
        Assert.Equal(3 * 5 * 0.8 * rp, row.Unsupported, 9);       // 産出のうち届いた投入が支えない部分＝産出 ×(1−fill)≧0
        Assert.True(row.Unsupported >= 0);
    }

    [Fact]
    public void fill_の再計算が本体の値と食い違えば数える_C2()
    {
        int coal = Catalog.CoalId;
        var (st, _, ra, _, _, _) = Board(); ra[coal] = 0.5;
        st.Factory(Factory(0.5, coal), ra); Assert.Equal(0, st.C2Bad);          // 一致
        st.Factory(Factory(0.25, coal), ra); Assert.Equal(1, st.C2Bad);         // 本体の値が違えば検出
        Assert.Equal(2, st.C2Checked);
    }

    [Fact]
    public void 計器が止まっている世界では蓄えが作られない_C5()
    {
        Instruments.Configure(null, null);
        var sim = new MultiNationSim([NationConfig.France(), NationConfig.Britain()], 1789UL, _ => new UtilityAgent());
        sim.StartWarmup(); sim.StepQuarter(); sim.StepQuarter();
        foreach (var n in sim.Nations) Assert.False(Instruments.InpHasState(n.World));
    }

    private static (string[] H, List<string[]> Rows) Table(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }
    private static double D(string s) => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void 本物の試合で表が書かれ_C1の和が合い_C2の食い違いが0_C4が通る()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_inp_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());
            foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            try { for (int q = 0; q < 8; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); } }
            finally { Instruments.Close(sim); }
            var (h, rows) = Table(System.IO.Path.Combine(dir, "inpf_s1789.tsv"));
            Assert.Equal(Instruments.InpfHeaderText, string.Join('\t', h));
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            var (bh, brows) = Table(System.IO.Path.Combine(dir, "inpb_s1789.tsv"));
            Assert.Equal(Instruments.InpbHeaderText, string.Join('\t', bh)); Assert.NotEmpty(brows);
            var checks = rows.Where(r => r[3] == "c2Mismatch").ToList();
            Assert.NotEmpty(checks);
            Assert.All(checks, r => Assert.Equal(0.0, D(r[4])));                       // C2：食い違い 0
            Assert.True(checks.All(r => D(r[9]) > 0), "検算の行が見た工場×月が 0");
            foreach (var chk in checks)
            {
                // C1：同じ年×国の、ok でない行（zeroDesired.ok も除く）の months の和＝fill＜0.95 の月の数（検算の行の lossRef 欄）
                var mine = rows.Where(r => r[0] == chk[0] && r[1] == chk[1] && r[3] != "c2Mismatch" && r[3] != "ok" && r[3] != "zeroDesired.ok").Sum(r => D(r[4]));
                Assert.Equal(D(chk[10]), mine);
            }
            Assert.All(rows.Where(r => r[3] != "c2Mismatch"), r => Assert.True(D(r[12]) >= 0, "unsupportedRef が負"));           // C4
            Assert.All(rows.Where(r => r[3] == "ok"), r => Assert.Equal(0.0, D(r[5]) + D(r[6]) + D(r[7]) + D(r[8])));             // C4：ok 行は fill≧0.95
            Assert.Contains(rows, r => r[3] != "ok" && r[3] != "c2Mismatch" && D(r[4]) > 0);                                      // 陽性：理由が付く行が在る
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
