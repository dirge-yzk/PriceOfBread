using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(474) 発見の技術力の前提を今の機構（発見の時計の細い傾斜〔(461)〕＝技術力 70×S×C）で作る試験用の口。旧は <c>LegacyDiscovery.Apply</c>（旗 <see cref="Coefficients.DiscoveryEngineerHalfPerMonth"/>＝0＝旧の式〔技術水準÷難易度〕）に固定して
/// 技術水準を書き換えて前提を作っていた（(415)）。今の既定では製法・新製品・新兵器の行の技術力が <see cref="Discovery.WorldView.WorldAdvLabor"/>（W＝時計 S の入力）と自国の項 C（親の産業の技師 A÷(A＋Am)）で決まる
/// ＝前提は W（<see cref="OpenClock"/>・<see cref="ClosedClock"/>）と C（<see cref="OwnShare"/>）で作る。旗は倒さない。</summary>
internal static class DiscoveryPremise
{
    /// <summary>時計 S を 1 にする W（月あたり・W0×d の最大〔d＜50〕より桁違いに大きい）。</summary>
    internal const double OpenClockW = 1e12;

    /// <summary>時計 S＝1（技術力の上限は C）の見え方。<paramref name="v"/> の接触・損耗・市場はそのまま持つ。</summary>
    internal static Discovery.WorldView OpenClock(Discovery.WorldView? v = null)
        => v is null ? new Discovery.WorldView(null, WorldAdvLabor: OpenClockW) : v with { WorldAdvLabor = OpenClockW };

    /// <summary>自国の項 C＝A÷(A＋Am) を 1 に近づける Am（極小の正）＝親の産業に技師が居る国（A＞0）は C≈1・技師の居ない国（A＝0）は C＝0 のまま。</summary>
    internal const double FullOwnShareAm = 1e-9;

    /// <summary>時計 S＝0（W＝0）の見え方＝技術力 0。</summary>
    internal static Discovery.WorldView ClosedClock(Discovery.WorldView? v = null)
        => v is null ? new Discovery.WorldView(null, WorldAdvLabor: 0) : v with { WorldAdvLabor = 0 };

    /// <summary>その国のその行の自国の項 C を <paramref name="cTarget"/>∈(0,1) にする＝Am を親の産業の技師 A から逆算して国の係数へ書く（A＝今の盤面の値・時計は開けて測る）。
    /// A が正でない国は作れない（false）。書くのは国の <see cref="World.Coef"/>（国ごとの別インスタンス）だけ。</summary>
    internal static bool OwnShare(World w, int row, double cTarget, Discovery.WorldView? v = null)
    {
        double a = Discovery.ParentEngineersByRow(w, OpenClock(v), w.Coef)[row];
        if (!(a > 0)) return false;
        w.Coef.DiscoveryOwnHalfPerMonth = a * (1.0 - cTarget) / cTarget;
        return true;
    }
}

/// <summary>★(415) 発見の技術力の置き換え・前半（段3の器）の番人。係数 <see cref="Coefficients.DiscoveryEngineerHalfPerMonth"/>（事前登録 measurements/dt_20261019_prereg.txt）。
/// 0以下（既定）＝今の式（技術水準÷難易度）・正＝技術力＝70×A÷(A＋A0×d)×押し（A＝親の産業の生産者が雇った高度労働力の月あたりの量・d＝(行の年代−1780)÷10）。
/// 機構だけを検査する（A0 の値・解禁の年の並びは創発＝成績表が見る）。</summary>
public class DiscoveryEngineerTests
{
    private static int Row(string name) => System.Array.FindIndex(Catalog.Discoveries, d => d.Name == name);

    private static string SimCoreDir([CallerFilePath] string self = "")
        => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(self))!, "PriceOfBread.SimCore");

    private const double TestHalf = 5.0;   // 検査用の A0（較正値ではない）

    /// <summary>1四半期だけ回した盤面。係数は国ごとの World.Coef に書く（MultiNationSim へ渡す係数は国の World に届かない＝構築時に読む係数は SeedCompatRows の検査で別に見る）。
    /// ★(420) 反証役 dt〔bv〕中：係数は最初の四半期の前に書く（旧は 1 四半期を既定〔(415) 後半で 3.42〕で回してから書いた＝旗0の盤面が旗正の 1 四半期を経ていた）。</summary>
    private static MultiNationSim Board(double half)
    {
        var sim = new MultiNationSim(WorldData.Nations(), 7, _ => new UtilityAgent());
        sim.Coef.DiscoveryEngineerHalfPerMonth = half;   // 多国の判定が読む共有の係数
        sim.Coef.TechThresholdOff = 0; foreach (var n in sim.Nations) n.World.Coef.TechThresholdOff = 0;   // ★(613) この番人は旗 DiscoveryEngineerHalfPerMonth の旧の枝〔制度と思想の行は技術水準を読む〕を見る＝旗 TechThresholdOff（既定 1）の旧の枝〔段6 で旧の枝ごと消す〕
        foreach (var n in sim.Nations) n.World.Coef.DiscoveryEngineerHalfPerMonth = half;
        if (!(half > 0)) foreach (var n in sim.Nations) Discovery.SeedCompatRows(n.World);   // 旗0の構築と同じ（構築は既定の係数を読むので、旗を0にした盤面は互換の解禁を最初の判定の前に呼ぶ）
        sim.StepQuarter();
        return sim;
    }

    private static readonly Lazy<MultiNationSim> BoardOff = new(() => Board(0));
    private static readonly Lazy<MultiNationSim> BoardOn = new(() => Board(TestHalf));

    private static World Nation(MultiNationSim sim, string name)
        => sim.Nations.Find(n => n.World.SelfPower?.BaseName == name)?.World ?? throw new Xunit.Sdk.XunitException($"{name} が居ない");

    private static Producer RepOf(World w, int row)
        => w.Regions.Where(rg => rg.State == RegionState.Active).SelectMany(rg => rg.Producers).First(p => p.DiscoveryRow == row);

    private static double AdvQty(Producer p)
    {
        foreach (var inp in p.Inputs) if (inp.GoodId == Catalog.AdvLaborId) return inp.QtyPerOutput;
        return 0;
    }

    /// <summary>番人の側で組んだ A＝財の集合（行の財か置き換える財・代表のレシピの労働でない投入）を作る生産者のうち門の開いたものの 実産出×高度労働力の投入量 の和。
    /// 本体の <c>eng[]</c> の配列を使わず、生産者を直接たどる（本体と同じ出所から期待値を作らない）。</summary>
    private static double HandA(World w, int row)
    {
        var d = Catalog.Discoveries[row];
        var rp = RepOf(w, row);
        var goods = new HashSet<int> { d.Replaces >= 0 ? d.Replaces : d.Good };
        foreach (var inp in rp.Inputs)
            if (inp.GoodId != Catalog.LaborId && inp.GoodId != Catalog.AdvLaborId && inp.GoodId != Catalog.FarmLaborId) goods.Add(inp.GoodId);
        double a = 0;
        foreach (var rg in w.Regions)
        {
            if (rg.State != RegionState.Active) continue;
            foreach (var p in rg.Producers)
                if (goods.Contains(p.GoodId) && Discovery.Gate(rg, p) > 0 && p.Output > 0) a += p.Output * AdvQty(p);
        }
        return a;
    }

    /// <summary>T1・T2：技師の比の性質。A＝0 の産業は技術力 0（d＞0）・d＝0 の行は A に関わらず最大・A に対して単調に増え1を超えない・同じ A で d が大きいほど小さい・A＝A0×d で半分。
    /// ⚠壊し方＝A＝0 のとき 1 を返す・d の向きを逆にする・分母から A0 を落とす → 赤。</summary>
    [Fact]
    public void 技師の比は_技師0なら0_d0なら1_単調で1を超えず_dが大きいほど小さい()
    {
        var c = new Coefficients { DiscoveryEngineerHalfPerMonth = TestHalf };
        int steam = Row("蒸気機関"), bess = Row("転炉鋼"), loco = Row("機関車");
        Assert.Equal(0, Catalog.Discoveries[steam].Decade - Catalog.DiscoveryBaseDecade);
        Assert.Equal(1.0, Discovery.EngineerFactor(steam, 0, c));              // d＝0 の行は A＝0 でも最大
        Assert.Equal(1.0, Discovery.EngineerFactor(steam, 12.5, c));
        Assert.Equal(0.0, Discovery.EngineerFactor(bess, 0, c));               // A＝0 の産業は 0
        Assert.Equal(0.0, Discovery.EngineerFactor(bess, double.NaN, c));      // NaN は 0 へ（否定形）
        Assert.Equal(0.0, Discovery.EngineerFactor(bess, -3, c));
        double d = (Catalog.Discoveries[bess].Decade - Catalog.DiscoveryBaseDecade) / 10.0;
        Assert.Equal(0.5, Discovery.EngineerFactor(bess, TestHalf * d, c), 12);   // A＝A0×d で半分
        double prev = 0;
        foreach (double a in new[] { 0.1, 1, 10, 100, 1e4, 1e8 })
        {
            double f = Discovery.EngineerFactor(bess, a, c);
            Assert.True(f > prev && f <= 1.0, $"A={a}：単調に増えず 1 を超えた（{f}）");
            prev = f;
        }
        // 同じ A で d が大きい行ほど小さい（転炉鋼 1850 ＞ 機関車 1820）
        Assert.True(Catalog.Discoveries[bess].Decade > Catalog.Discoveries[loco].Decade);
        Assert.True(Discovery.EngineerFactor(bess, 30, c) < Discovery.EngineerFactor(loco, 30, c));
    }

    /// <summary>行の技師の量は、親の産業（行の財か置き換える財＋代表のレシピの労働でない投入）の門の開いた生産者が雇った高度労働力の和と一致する（実盤面・全ての行と大国）。
    /// 全ての非制度・非思想の行で A が NaN でなく、制度と思想の行は NaN（求めない）。⚠壊し方＝投入の財を数えない・門の閉じた生産者を数える・労働力の投入まで数える → 赤。</summary>
    [Fact]
    public void 行の技師の量は親の産業の生産者の雇用の和と一致する()
    {
        var sim = BoardOn.Value;
        int checkedRows = 0, positive = 0;
        foreach (var name in new[] { "イギリス", "フランス", "プロイセン", "清" })
        {
            var w = Nation(sim, name);
            var a = Discovery.ParentEngineersByRow(w, w.LastDiscoveryView, w.Coef);
            for (int row = 0; row < a.Length; row++)
            {
                var d = Catalog.Discoveries[row];
                bool hasRep = w.Regions.Any(rg => rg.State == RegionState.Active && rg.Producers.Any(p => p.DiscoveryRow == row));
                if (Discovery.IsSpread(d.Field) || !hasRep || Catalog.DiscoveryAfter[row] >= 0 && !Discovery.Known(w.Discovered, Catalog.DiscoveryAfter[row]))
                {
                    if (Discovery.IsSpread(d.Field)) Assert.True(double.IsNaN(a[row]), $"{name} {d.Name}：制度と思想は技師の量を求めない");
                    continue;
                }
                double want = HandA(w, row);
                Assert.True(System.Math.Abs(a[row] - want) <= 1e-9 * System.Math.Max(1.0, want), $"{name} {d.Name}：本体 {a[row]} ≠ 手計算 {want}");
                checkedRows++; if (want > 0) positive++;
            }
        }
        Assert.True(checkedRows > 40, $"検査した行が少なすぎる（{checkedRows}）＝検査が空回りしている");
        Assert.True(positive > 20, $"技師の量が正の行が少なすぎる（{positive}）＝産出か投入の読みが死んでいる");
    }

    /// <summary>T4：本体の <see cref="Discovery.ParentEngineers"/> を、開幕の英の盤面で門の開いた高度労働力の雇い手（生産者の財）の集合に1ずつ置いた配列で呼ぶ＝親の産業（行の財か置き換える財・代表のレシピの労働でない投入の財）に雇い手が1つでも居る行は A＞0 になる。
    /// 開幕に雇い手が親の産業に居ない行は、別の発見が先に要る連鎖（名簿 <see cref="NeedsEarlierDiscovery"/>）として手書きで固定する（名簿の外で A＝0 の行が出たら赤・名簿の行が A＞0 になっても赤＝名簿が古い）。
    /// ⚠壊し方＝本体の ParentEngineers が投入の財を親に入れない → 機関車（置き換える物が財でない）などで赤。T4 が守るのは〈親の定義が構造として A＞0 になりうる〉ことだけで、〈発見の前に A が正になる〉ことは census が見る。</summary>
    private static readonly string[] NeedsEarlierDiscovery = ["電動機", "化学兵器", "合成ゴム"];   // ★2026-10-23 (493)：化学兵器（塩＋電力）と合成ゴム（カーバイド＝石灰石＋石炭＋電力）は化学薬品でなく電力を材料にした＝電力の工場が先に要る連鎖へ移った（機構の意味は変えない・名簿の追随）   // 電力の工場（石炭火力・石油火力の発見）が先に要る＝census でも A＝0（6/6 件）の既知の連鎖

    [Fact]
    public void 親の産業の定義は_開幕の雇い手が居る行で技師の量が正になる()
    {
        var sim = BoardOff.Value;
        var w = Nation(sim, "イギリス");
        var eng = new double[Catalog.Goods.Length];
        foreach (var rg in w.Regions) foreach (var p in rg.Producers)
            if (AdvQty(p) > 0 && Discovery.Gate(rg, p) > 0 && p.GoodId >= 0 && p.GoodId < eng.Length) eng[p.GoodId] = 1;
        var zero = new List<string>();
        for (int row = 0; row < Catalog.Discoveries.Length; row++)
        {
            var d = Catalog.Discoveries[row];
            if (Discovery.IsSpread(d.Field)) continue;
            var rp = w.Regions.SelectMany(rg => rg.Producers).FirstOrDefault(p => p.DiscoveryRow == row);
            Assert.True(rp is not null, $"{d.Name}：イギリスに代表の生産者が居ない");
            if (!(Discovery.ParentEngineers(row, rp!, eng) > 0)) zero.Add(d.Name);
        }
        Assert.Equal(NeedsEarlierDiscovery.OrderBy(x => x), zero.OrderBy(x => x));
    }

    /// <summary>T3：旗が0のとき、制度と思想の行の技術力は今の式そのもの（70×min(1, 技術水準÷難易度)）で、他の行は 70×min(1, 技術水準÷難易度) を超えない（押しは1以下）。
    /// 技術水準を動かすと旗0では技術力が動く。⚠壊し方＝旗0でも技師の式を使う → 技術水準を動かしても動かず赤。</summary>
    [Fact]
    public void 旗0は技術水準の式のまま()
    {
        var sim = BoardOff.Value;
        var w = Nation(sim, "フランス");
        double saved = w.TechLevel;
        try
        {
            int patent = Discovery.PatentRow, soc = Discovery.SocialismRow, bess = Row("転炉鋼");
            foreach (double tl in new[] { 0.0, 0.5, 1.5, 100.0 })
            {
                w.TechLevel = tl;
                var t = Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef, includeKnown: true);
                foreach (int row in new[] { patent, soc })
                {
                    double diff = Discovery.Difficulty(row, w.Coef);
                    double ratio = !(diff > 0) ? 1.0 : System.Math.Min(1.0, tl / diff);
                    Assert.Equal(Discovery.TermMax * ratio, t[row].Tech, 9);
                }
                Assert.True(t[bess].Tech <= Discovery.TermMax * System.Math.Min(1.0, tl / Discovery.Difficulty(bess, w.Coef)) + 1e-9, "転炉鋼の技術力が 70×比 を超えた");
            }
            w.TechLevel = 0.0; double lo = Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef, includeKnown: true)[bess].Tech;
            w.TechLevel = 100.0; double hi = Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef, includeKnown: true)[bess].Tech;
            Assert.True(hi > lo, "旗0で技術水準が技術力に効いていない");
        }
        finally { w.TechLevel = saved; }
    }

    /// <summary>旗が正のとき、製法・新製品・新兵器の行と産地の技術力は技術水準を読まない（技術水準を 0 にしても 100 にしても同じ）。制度と思想の行は読む（置き換えは次の波・事前登録 [3-3]）。
    /// ⚠壊し方＝旗が正でも比 min(1, 技術水準÷難易度) を掛ける → 赤。</summary>
    [Fact]
    public void 旗が正なら製法と産地は技術水準を読まず_制度と思想は読む()
    {
        var sim = BoardOn.Value;
        var w = Nation(sim, "フランス");
        double saved = w.TechLevel;
        try
        {
            w.TechLevel = 0.0;
            var t0 = Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef, includeKnown: true);
            w.TechLevel = 100.0;
            var t1 = Discovery.Evaluate(w, w.LastDiscoveryView, w.Coef, includeKnown: true);
            int nonSpread = 0;
            for (int row = 0; row < t0.Length; row++)
            {
                if (Discovery.IsSpread(Catalog.Discoveries[row].Field)) continue;
                Assert.Equal(t0[row].Tech, t1[row].Tech);
                nonSpread++;
            }
            Assert.True(nonSpread > 30);
            Assert.True(t1[Discovery.SocialismRow].Tech > t0[Discovery.SocialismRow].Tech, "制度と思想の行は（この線では）技術水準を読み続ける");
        }
        finally { w.TechLevel = saved; }
        // 産地：まだ見つかっていない産地を持つ国で、技術水準を 0 にしても 100 にしても技術力は同じ（人の厚み N÷(N＋N0) だけ・0〜70）
        World? sw = null; Region? sr = null; Producer? site = null;
        foreach (var n in sim.Nations)
            foreach (var rg in n.World.Regions.Where(r => r.State == RegionState.Active))
                foreach (var p in rg.Producers)
                    if (site is null && p.SiteRow >= 0 && !p.SiteFound) { sw = n.World; sr = rg; site = p; }
        Assert.NotNull(site);
        double savedS = sw!.TechLevel;
        try
        {
            sw.TechLevel = 0.0; var s0 = Discovery.SiteTerms(sw, sr!, site!, sw.Coef);
            sw.TechLevel = 100.0; var s1 = Discovery.SiteTerms(sw, sr!, site!, sw.Coef);
            Assert.Equal(s0.Tech, s1.Tech);
            Assert.True(s1.Tech >= 0 && s1.Tech <= Discovery.TermMax);
            sw.Coef.DiscoveryEngineerHalfPerMonth = 0;   // 旗0では技術水準が効く（今の式）
            sw.TechLevel = 0.0; var o0 = Discovery.SiteTerms(sw, sr!, site!, sw.Coef);
            sw.TechLevel = 100.0; var o1 = Discovery.SiteTerms(sw, sr!, site!, sw.Coef);
            Assert.True(o1.Tech > o0.Tech, "旗0で産地の技術力が技術水準を読んでいない");
        }
        finally { sw.TechLevel = savedS; sw.Coef.DiscoveryEngineerHalfPerMonth = TestHalf; }
    }

    /// <summary>T6：合成染料の行（添字は既存の行の末尾）。旗0は全ての国が開幕で解禁済み（旧＝行が無く常に作れた）・旗が正は見つかるまで作れない（門が0）。</summary>
    [Fact]
    public void 合成染料の行は旗0で解禁済み_旗が正で未解禁()
    {
        int dye = Discovery.SyntheticDyeRow;
        Assert.True(dye >= 0 && Catalog.Discoveries.Skip(dye + 1).Take(3).Select(x => x.Name).SequenceEqual(["ルブラン法", "ソルベー法", "電解"]), "合成染料の行の直後はソーダ灰の 3 行（★(493)・既存の行の添字を動かさない）");
        Assert.Equal(Catalog.DyeId, Catalog.Discoveries[dye].Good);
        Assert.Equal(DiscoveryField.Process, Catalog.Discoveries[dye].Field);
        foreach (var n in BoardOff.Value.Nations) Assert.True(n.World.Discovered[dye], $"{n.World.SelfPower?.Name}：旗0で合成染料が未解禁");
        // 既定（旗が正）の構築：MultiNationSim が係数の既定のまま作った盤面（BoardOn は構築の後で係数を書き換えるだけ）では全ての国で未解禁
        foreach (var n in BoardOn.Value.Nations) Assert.False(n.World.Discovered[dye], $"{n.World.SelfPower?.Name}：既定の構築で合成染料が解禁済み");
        // 旗が正のときの開幕の互換：構築時に読む係数なので、世界の係数を書いてから同じ関数（構築が呼ぶ SeedCompatRows）を呼んで確かめる
        var w = Nation(BoardOff.Value, "フランス");
        double savedHalf = w.Coef.DiscoveryEngineerHalfPerMonth; bool savedKnown = w.Discovered[dye];
        try
        {
            var rg = w.Regions.First(r => r.State == RegionState.Active && r.Producers.Any(p => p.GoodId == Catalog.DyeId && p.RecipeName == "合成染料"));
            var pr = rg.Producers.First(p => p.GoodId == Catalog.DyeId && p.RecipeName == "合成染料");
            Assert.Equal(dye, pr.DiscoveryRow);
            w.Coef.DiscoveryEngineerHalfPerMonth = TestHalf; w.Discovered[dye] = false;
            Discovery.SeedCompatRows(w);
            Assert.False(w.Discovered[dye], "旗が正で合成染料が開幕で解禁済みにされた");
            Assert.Equal(0.0, Discovery.Gate(rg, pr));   // 見つかるまで作れない
            w.Coef.DiscoveryEngineerHalfPerMonth = 0; Discovery.SeedCompatRows(w);
            Assert.True(w.Discovered[dye]);
            Assert.Equal(1.0, Discovery.Gate(rg, pr));
        }
        finally { w.Coef.DiscoveryEngineerHalfPerMonth = savedHalf; w.Discovered[dye] = savedKnown; }
    }

    /// <summary>T5：発見の技術力が技術水準を読むコード行は Discovery.cs の自分の 2 か所だけ（表の比と産地の比＝どちらも旗が正なら読まない）。
    /// 全体の本数は固定しない（別の線が読み手を消すため・<c>NationalScore.Tech</c> は (456) で消えた）。コメントの言及は数えない。
    /// ⚠壊し方＝Discovery.cs に 3 か所目の <c>w.TechLevel</c> を足す → 赤。</summary>
    [Fact]
    public void 発見の技術力が技術水準を読むのは自分の2か所だけ()
    {
        var rx = new Regex(@"\bw\.TechLevel\b");
        int n = 0;
        foreach (var line in File.ReadLines(Path.Combine(SimCoreDir(), "Discovery.cs")))
        {
            var code = line; int c = code.IndexOf("//", System.StringComparison.Ordinal);
            if (c >= 0) code = code[..c];
            if (rx.IsMatch(code)) n++;
        }
        Assert.True(n == 2, $"Discovery.cs の w.TechLevel を読むコード行が {n} 本（宣言 2＝表の比と産地の比）。技術力の出所を増やさないこと（作者 2026-10-16）。");
    }
}
