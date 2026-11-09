using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1003) 線 c92tg：貿易の計器 C1 <c>nationgood_s</c>・C2 <c>tradeskip_s</c>（読むだけ）の番人。事前登録 measurements/m1003_20261110_prereg_tg.txt [3]。
/// G1＝C1 の越境の額（国×財）が World.ImportsByGood・ExportsByGood（エンジンの別の欄・四半期の末＝最後の清算 1 回分）と合う（計器の蓄えは TradeDeal の引数から積む＝同じ欄を読んでいない）。
/// G2＝C2 の行の結果の排他（rows＝4 つの結果の和・rowScan＝3 つの和・Σrows＝エンジンの台帳 Cost[ClearingRows] の増分）。G3＝xPairs の和・R2 の 4 区分の和。
/// G4＝crossImportQty×priceMeanGold と crossImportValue が 10 倍以内。T4＝関税 0 の盤面で R2a（関税を抜けば通る）＝0・関税 &gt;0 の盤面で &gt;0（正の対照）。T5＝計器を切ると蓄えが作られない。
/// 何もしない計器は 0 で緑になる＝全ての門に〈正の行が 1 つ以上ある〉対照を置く。
/// </summary>
[Collection("Instruments")]
public class TradeInstrumentTests
{
    private static MultiNationSim Make(bool still = false) => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, still ? null : _ => new UtilityAgent());   // still＝AI を置かない（関税を盤面で固定する試験）
    private const ulong Seed = 1789UL;

    private sealed class Tab
    {
        public string[] H = []; public List<string[]> R = [];
        public int I(string n) { int i = Array.IndexOf(H, n); Assert.True(i >= 0, "列が無い " + n); return i; }
        public double D(string[] r, string n) { var s = r[I(n)]; return s == "nan" ? double.NaN : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture); }
        public string S(string[] r, string n) => r[I(n)];
    }
    private static Tab Read(string dir, string kind)
    {
        var lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(dir, $"{kind}_s{Seed}.tsv"));
        return new Tab { H = lines[0].Split('\t'), R = lines.Skip(1).Select(l => l.Split('\t')).ToList() };
    }

    /// <summary>quarters 四半期回す（q%4==3 の四半期の終わりごとに afterQuarterEnd）。年ごとに調べる（NgEveryYears＝1）。</summary>
    private static (string dir, MultiNationSim sim, Action restore) Run(int quarters, Action<MultiNationSim>? before = null, Action<MultiNationSim, int>? afterStep = null, bool instrumentsOn = true, bool still = false, int every = 1)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_tg_" + Guid.NewGuid().ToString("N"));
        if (instrumentsOn) Instruments.Configure(dir, "*"); else Instruments.Configure(null, null);
        int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = every;
        var sim = Make(still);
        foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
        sim.DiplomacyFrozen = true;
        before?.Invoke(sim);
        if (instrumentsOn) Instruments.Phase(sim, Seed, "open");
        sim.StartWarmup();
        if (instrumentsOn) Instruments.Phase(sim, Seed, "warm");
        before?.Invoke(sim);
        try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); afterStep?.Invoke(sim, q); if (instrumentsOn) Instruments.Quarter(sim, Seed, q); before?.Invoke(sim); } }
        finally { if (instrumentsOn) Instruments.Close(sim); }
        return (dir, sim, () => { Instruments.NgEveryYears = every0; Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } });
    }

    private static void Near(double expect, double actual, double rel, string what) => Assert.True(Math.Abs(expect - actual) <= rel * Math.Max(1.0, Math.Abs(expect)), $"{what}：期待 {expect}・実際 {actual}");

    /// <summary>G1：国×財の越境の額が、四半期の末の World.ImportsByGood・ExportsByGood（別の欄）と合う。全ての年・全ての国・全ての財（1e-9）。国内を混ぜる・運賃を落とす・計器を書かないは赤。</summary>
    [Fact]
    public void G1_越境の額はエンジンの輸入と輸出の欄と財ごとに合う()
    {
        var engine = new Dictionary<(int year, string nation, string good), (double imp, double exp)>();
        var (dir, sim, restore) = Run(24, afterStep: (s, q) =>
        {
            if (q % 4 != 3) return;
            foreach (var n in s.Nations)
            {
                var w = n.World; string nm = Instruments.NameOf(w);
                for (int g = 0; g < Catalog.Goods.Length; g++)
                    engine[(w.Year, nm, Catalog.Goods[g].Name)] = (w.ImportsByGood is { } ig && g < ig.Length ? ig[g] : 0, w.ExportsByGood is { } eg && g < eg.Length ? eg[g] : 0);
            }
        });
        try
        {
            var t = Read(dir, "nationgood");
            int rowsCmp = 0; double posImp = 0, posExp = 0; var yearsPos = new HashSet<string>();
            foreach (var r in t.R)
            {
                var key = (int.Parse(t.S(r, "year")), t.S(r, "nation"), t.S(r, "good"));
                Assert.True(engine.TryGetValue(key, out var e), $"エンジンの欄に無い行 {key}");
                double ci = t.D(r, "crossImportValue"), ce = t.D(r, "crossExportValue");
                Near(e.imp, ci, 1e-9, $"輸入 {key}"); Near(e.exp, ce, 1e-9, $"輸出 {key}");
                rowsCmp++; posImp += e.imp; posExp += e.exp; if (e.imp > 0 || e.exp > 0) yearsPos.Add(key.Item1.ToString());
            }
            Assert.True(rowsCmp >= 4 * Catalog.Goods.Length * 5, "行が少ない（年ごとの行が出ていない）");
            Assert.True(posImp > 0 && posExp > 0, "対照：エンジンの輸出入が 0＝この盤面は越境の成約を起こしていない");
            Assert.True(yearsPos.Count >= 3, "対照：越境のある年が 3 年以上ある");
        }
        finally { restore(); }
    }

    /// <summary>G2・G3：tradeskip_s の行の結果の排他と和・台帳との突き合わせ。</summary>
    [Fact]
    public void G2G3_成約0の理由の和は走査した行の数とエンジンの台帳に合う()
    {
        var (dir, sim, restore) = Run(24, every: 2);   // 2 年おき＝C2 の標本の四半期が C1 とずれていると年の印の集合が合わない
        try
        {
            var t = Read(dir, "tradeskip");
            var byYear = new Dictionary<string, (double rows, double ledger)>(); double scan = 0, wedge = 0, cross = 0, noDeal = 0, dealtCross = 0;
            foreach (var r in t.R)
            {
                string y = t.S(r, "year"), nat = t.S(r, "nation"), good = t.S(r, "good");
                double rowsLedger = t.D(r, "rowsLedger"), rows = t.D(r, "rows");
                if (nat == "ALL") { if (good == "ALL") { var c = byYear.GetValueOrDefault(y); byYear[y] = (c.rows, c.ledger + rowsLedger); } continue; }
                // G2：行の結果は排他で全てを数える
                Near(rows, t.D(r, "rowIdle") + t.D(r, "rowNoNeed") + t.D(r, "rowBudget0") + t.D(r, "rowScan"), 0, "行の結果の和");
                Near(t.D(r, "rowScan"), t.D(r, "rowDealtCross") + t.D(r, "rowDealtDomOnly") + t.D(r, "rowNoDeal"), 0, "走査した行の内訳");
                // G3：越境の売り手の最初の門・R2 の区分
                Near(t.D(r, "xPairs"), t.D(r, "xSoldOut") + t.D(r, "xCap") + t.D(r, "xWedge") + t.D(r, "xPass"), 0, "越境の売り手の和");
                Near(t.D(r, "xWedge"), t.D(r, "xWedgeA") + t.D(r, "xWedgeB") + t.D(r, "xWedgeC") + t.D(r, "xWedgeD"), 0, "R2 の区分の和");
                double bins = 0; for (int b = 0; b < 10; b++) bins += t.D(r, "ratioBin" + b);
                Assert.True(bins <= t.D(r, "xPairs") + 1e-9, "値段の比の度数は越境の売り手の数を超えない");
                var cc = byYear.GetValueOrDefault(y); byYear[y] = (cc.rows + rows, cc.ledger);
                scan += t.D(r, "rowScan"); wedge += t.D(r, "xWedge"); cross += t.D(r, "xPairs"); noDeal += t.D(r, "rowNoDeal"); dealtCross += t.D(r, "rowDealtCross");
            }
            Assert.True(byYear.Count >= 3, "対照：調べた年が 3 年以上ある");
            // C1 と C2 は同じ四半期を読む：年の集合が合う（ident の最初の読みで C2 の年の印が 1 年ずれていた＝World.Year は四半期の末で進む・結果を見た後に足した番人）
            var ngYears = Read(dir, "nationgood").R.Select(r => r[0]).ToHashSet();
            Assert.True(byYear.Keys.ToHashSet().SetEquals(ngYears) && ngYears.Count >= 2 && ngYears.All(y => int.Parse(y) % 2 == 0), "tradeskip の年の集合が nationgood の年と違う（四半期のずれ）");
            foreach (var (y, v) in byYear) { Assert.True(v.ledger > 0, "台帳が 0＝対照が空 " + y); Near(v.ledger, v.rows, 0, "Σ行の数＝エンジンの台帳 " + y); }
            Assert.True(scan > 0 && wedge > 0 && cross > 0 && noDeal > 0 && dealtCross > 0, $"対照：走査 {scan}・楔 {wedge}・越境の売り手 {cross}・成約なし {noDeal}・越境の成約 {dealtCross}");
        }
        finally { restore(); }
    }

    /// <summary>G4：量×平均の値段と額の桁（単位の取り違えを拾う）。</summary>
    [Fact]
    public void G4_越境の量と平均の値段の積は額と10倍以内()
    {
        var (dir, sim, restore) = Run(24);
        try
        {
            var t = Read(dir, "nationgood"); var ratios = new List<double>();
            foreach (var r in t.R)
            {
                double q = t.D(r, "crossImportQty"), p = t.D(r, "priceMeanGold"), v = t.D(r, "crossImportValue");
                if (!(q > 0) || !(v > 0) || !(p > 0)) continue;
                ratios.Add(q * p / v);
            }
            ratios.Sort(); int n = ratios.Count;
            Assert.True(n >= 8, "対照：正の行が 8 以上ある（" + n + "）");
            // 行ごとの比は州の平均の値段と売り手の積出し価格の差で外れる行がある（試験の盤面で 25.6 倍の行が 1 本・結果を見た後の形＝事前登録の追記）＝単位の取り違えは全行を同じ向きにずらす
            // （量の代わりに額を積むと比は値段そのもの＝中央値 10 超）。中央値が 0.3〜3 と、7 割以上の行が 10 倍以内で見る。
            Assert.True(ratios[n / 2] > 0.3 && ratios[n / 2] < 3, "中央値 " + ratios[n / 2]);
            Assert.True(ratios.Count(x => x > 0.1 && x < 10) >= 0.7 * n, "10 倍以内の行の割合が低い");
            // G4b（M4 の穴を塞ぐ・突然変異 M4 が中央値の門を通ったため結果の後に足した）：値段が 1 前後の盤面では量の代わりに額を積んでも中央値が 3 を超えない。
            //   越境の量は輸入側と輸出側が同じ qA を積む＝年×財ごとに Σ国 crossImportQty ＝ Σ国 crossExportQty（別々の欄で積んだ量の保存）。輸入の額は運賃の分だけ輸出の額より大きい（CIF≧FOB）。
            var qsum = new Dictionary<(string, string), (double imp, double exp, double impV, double expV)>();
            foreach (var r in t.R)
            {
                var k = (t.S(r, "year"), t.S(r, "good")); var c = qsum.GetValueOrDefault(k);
                qsum[k] = (c.imp + t.D(r, "crossImportQty"), c.exp + t.D(r, "crossExportQty"), c.impV + t.D(r, "crossImportValue"), c.expV + t.D(r, "crossExportValue"));
            }
            Assert.True(qsum.Values.Count(v => v.exp > 0) >= 10, "対照：越境のある年×財が 10 以上ある");
            foreach (var (k, v) in qsum) { Near(v.exp, v.imp, 1e-9, "量の保存 " + k); Assert.True(v.impV >= v.expV * (1 - 1e-9), "輸入の額（CIF）は輸出の額（FOB）以上 " + k); }
        }
        finally { restore(); }
    }

    /// <summary>T4：R2 の 4 区分の判定を合成の盤面で直接測る（実盤の関税は毎四半期の政策が動かす＝0 に固定できない）。関税 0（duty＝0）では R2a＝0（関税を抜いても何も変わらない）・
    /// 関税 &gt;0 では R2a が正（対照＝a の判定が関税を見ている）。B（運賃を抜けば通る）・C（両方抜いて通る）・D（両方抜いても通らない）も 1 件ずつ数える（期待値は不等式から手で作る）。</summary>
    [Fact]
    public void T4_関税0の盤面では関税を抜けば通る区分が0で関税を掛けると正になる()
    {
        // 状態 0＝買い手（国 0）・状態 1〜4＝売り手（国 1）。lp＝1.0。
        var tr = new (int nation, Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk)[5];
        for (int i = 0; i < 5; i++) tr[i] = (i == 0 ? 0 : 1, null!, 0, 0, 0, 1, 1, true, 0, 0, true);
        Instruments.TsAcc Board(double[] prices, double baseMul, double duty, out double[] left, out double[] cap)
        {
            var a = new Instruments.TsAcc { Nn = 2, Gc = 1, C = new int[2 * 1 * (int)Instruments.TsCol.Count], NoSell = new int[1] };
            a.GoodBegin(prices.Length); for (int i = 0; i < prices.Length; i++) a.AddSeller(i + 1, prices[i]);
            left = Enumerable.Repeat(1.0, prices.Length).ToArray(); cap = Enumerable.Repeat(1.0, 5).ToArray();
            a.Row(0, 0); a.Scan(tr, 0, 1.0, left, cap, baseMul, duty /* wedge＝duty−buyDear（buyDear＝0）*/, duty, null); a.End(0);
            return a;
        }
        int Get(Instruments.TsAcc a, Instruments.TsCol c) => a.At(0, 0, c);
        // 関税 0.3・運賃の倍率 1.05：0.9→A（0.9×1.35≧1・0.9×1.05＜1）・0.95→A・0.98→C（関税を抜いても 1.029≧1・運賃を抜くと 1.274≧1・両方抜くと 0.98＜1）・1.1→D
        var tax = Board([0.9, 0.95, 0.98, 1.1], 1.05, 0.3, out _, out _);
        Assert.Equal(4, Get(tax, Instruments.TsCol.XWedge));
        Assert.Equal(2, Get(tax, Instruments.TsCol.WA)); Assert.Equal(0, Get(tax, Instruments.TsCol.WB)); Assert.Equal(1, Get(tax, Instruments.TsCol.WC)); Assert.Equal(1, Get(tax, Instruments.TsCol.WD));
        // 運賃の倍率 1.5・関税 0.05：0.8→B（関税を抜いても 0.8×1.5＝1.2≧1・運賃を抜くと 0.8×1.05＝0.84＜1）
        var frt = Board([0.8], 1.5, 0.05, out _, out _);
        Assert.Equal(1, Get(frt, Instruments.TsCol.XWedge)); Assert.Equal(1, Get(frt, Instruments.TsCol.WB)); Assert.Equal(0, Get(frt, Instruments.TsCol.WA));
        // 関税 0：同じ売り手でも A は 0（楔で飛ばす売り手は運賃だけのせい）。1.5 倍の運賃で 0.8→B・0.98→D？（1.5×0.98＝1.47≧1・運賃を抜くと 0.98＜1 なので B）
        var zero = Board([0.8, 0.98, 1.1], 1.5, 0.0, out _, out _);
        Assert.Equal(3, Get(zero, Instruments.TsCol.XWedge)); Assert.Equal(0, Get(zero, Instruments.TsCol.WA));
        Assert.Equal(2, Get(zero, Instruments.TsCol.WB)); Assert.Equal(1, Get(zero, Instruments.TsCol.WD));
    }

    /// <summary>T5：計器を切っている間は蓄えが作られず表も書かれない（呼び口が 1 命令も走らない）。</summary>
    [Fact]
    public void T5_計器を切ると蓄えは作られない()
    {
        var (dir, sim, restore) = Run(8, instrumentsOn: false);
        try
        {
            Assert.Null(Instruments.NgSnapshot(sim.Market));
            Assert.Null(Instruments.TsSnapshot(sim.Market));
            Assert.False(System.IO.File.Exists(System.IO.Path.Combine(dir, $"nationgood_s{Seed}.tsv")));
        }
        finally { restore(); }
    }

    // ---------------------------------------------------------------- (1008) 線 c93tq：貿易の段0 の本体の計器（事前登録 measurements/m1008_20261110_prereg_tq.txt [3]）
    private static Instruments.TsAcc Board2(double[] tarEff, double cornPen, double duty, double baseMul, double[] prices, double[]? capLeft = null, double lp = 1.0)
    {
        var tr = new (int nation, Region r, double lat, double lon, double landKm, double access, double cap, bool linked, double ownLat, double ownLon, bool seaOk)[5];
        for (int i = 0; i < 5; i++) tr[i] = (i == 0 ? 0 : 1, null!, 0, 0, 0, 1, 1, true, 0, 0, true);
        var a = new Instruments.TsAcc { Nn = 2, Gc = 1, C = new int[2 * (int)Instruments.TsCol.Count], NoSell = new int[1], D = new double[6], TarEff = tarEff };
        a.GoodBegin(prices.Length); for (int i = 0; i < prices.Length; i++) a.AddSeller(i + 1, prices[i]);
        var left = Enumerable.Repeat(1.0, prices.Length).ToArray(); var cap = capLeft ?? Enumerable.Repeat(1.0, 5).ToArray();
        a.Row(0, 0); a.Scan(tr, 0, lp, left, cap, baseMul, duty, duty, null, cornPen); a.End(0);
        return a;
    }
    private static int Gt(Instruments.TsAcc a, Instruments.TsCol c) => a.At(0, 0, c);

    /// <summary>G5：関税の分＋穀物法の分＝duty。合成の盤面（穀物法 0・関税 0・両方）と実盤の全ての走査した行。xWedgeA の 3 分けの和＝xWedgeA。</summary>
    [Fact]
    public void G5_関税の分と穀物法の分の和はdutyに合う()
    {
        var none = Board2([0.4, 0], 0.0, 0.4, 1.05, [0.8]);   // 穀物法なし
        Assert.Equal(0.0, none.D[2]); Near(none.D[0], none.D[1], 1e-12, "穀物法なし");
        var zero = Board2([0.0, 0], 0.4, 0.4, 1.05, [0.8]);   // 関税 0（穀物法だけ）
        Assert.Equal(0.0, zero.D[1]); Near(zero.D[0], zero.D[2], 1e-12, "関税 0");
        var both = Board2([0.3, 0], 0.1, 0.4, 1.05, [0.8, 0.95]);
        Near(both.D[0], both.D[1] + both.D[2], 1e-12, "両方"); Assert.True(both.D[1] > 0 && both.D[2] > 0, "対照：両方が正");
        Assert.Equal(1, Gt(both, Instruments.TsCol.WAT)); Assert.Equal(1, Gt(both, Instruments.TsCol.WAB)); Assert.Equal(0, Gt(both, Instruments.TsCol.WAC));   // 0.8＝関税を抜けば通る・0.95＝どちらか一方では通らない
        var corn = Board2([0.1, 0], 0.3, 0.4, 1.05, [0.8]);
        Assert.Equal(1, Gt(corn, Instruments.TsCol.WAC));
        // 実盤：穀物法を全面にした国が主食を輸入しようとする行で、行ごとに和が合う
        var (dir, sim, restore) = Run(16, before: s => { foreach (var n in s.Nations) n.World.CornLaws = ReformLevel.Full; });
        try
        {
            var t = Read(dir, "tradeskip"); int cmp = 0; double cornPos = 0, tarPos = 0, wa = 0;
            foreach (var r in t.R)
            {
                if (t.S(r, "nation") == "ALL") continue;
                double d = t.D(r, "dutyMean"), dt = t.D(r, "dutyTariffMean"), dc = t.D(r, "dutyCornMean");
                Near(d, dt + dc, 1e-12, "行の duty の和");
                Near(t.D(r, "xWedgeA"), t.D(r, "xWedgeAT") + t.D(r, "xWedgeAC") + t.D(r, "xWedgeAB"), 0, "xWedgeA の 3 分け");
                if (t.D(r, "rowScan") > 0) cmp++; cornPos += dc; tarPos += dt; wa += t.D(r, "xWedgeA");
            }
            Assert.True(cmp > 50 && cornPos > 0 && tarPos > 0 && wa > 0, $"対照：走査した行 {cmp}・穀物法の平均の和 {cornPos}・関税 {tarPos}・xWedgeA {wa}");
        }
        finally { restore(); }
    }

    /// <summary>G6：国ごとに Σ財(関税の払い＋穀物法の払い)＝World.LastTariffRevenuePerClearing（四半期の末の最後の清算 1 回分・エンジンの別の欄）。tar＜0 の国は輸出補助が同じ欄から引かれるので除く。</summary>
    [Fact]
    public void G6_越境の払いの内訳の和はエンジンの関税収入に合う()
    {
        int compared = 0; double sumCorn = 0, sumTar = 0;
        var (dir, sim, restore) = Run(16, before: s => { foreach (var n in s.Nations) n.World.CornLaws = ReformLevel.Full; }, afterStep: (s, q) =>
        {
            var sn = Instruments.NgDutySnapshot(s.Market); if (sn is null) return;
            for (int i = 0; i < s.Nations.Count; i++)
            {
                if (!(sn.Value.Tar[i] >= 0)) continue;
                double c = 0, tt = 0;
                for (int g = 0; g < sn.Value.Gc; g++) { tt += sn.Value.DutyT[i * sn.Value.Gc + g]; c += sn.Value.DutyC[i * sn.Value.Gc + g]; }
                Near(s.Nations[i].World.LastTariffRevenuePerClearing, tt + c, 1e-9, $"国 {i} 四半期 {q}");
                compared++; sumCorn += c; sumTar += tt;
            }
        });
        try { Assert.True(compared >= 3 && sumCorn > 0 && sumTar > 0, $"対照：比べた国×四半期 {compared}・穀物法の払い {sumCorn}・関税の払い {sumTar}"); }
        finally { restore(); }
    }

    /// <summary>G7：最初の門ごとの度数の和＝旧の ratioBin（全行・全 k）・全ての門の度数＋ratioNaN＝xPairs。</summary>
    [Fact]
    public void G7_門ごとの値段の比の度数は旧の度数と件数に合う()
    {
        var (dir, sim, restore) = Run(16, before: s => { foreach (var n in s.Nations) n.World.CornLaws = ReformLevel.Full; });
        try
        {
            var t = Read(dir, "tradeskip"); int rows = 0; double cap = 0, wedge = 0;
            foreach (var r in t.R)
            {
                if (t.S(r, "nation") == "ALL") continue;
                double all = 0, gSold = 0, gCap = 0, gWedge = 0, gPass = 0;
                for (int k = 0; k < 10; k++)
                {
                    double a1 = t.D(r, "xSoldBin" + k), a2 = t.D(r, "xCapBin" + k), a3 = t.D(r, "xWedgeBin" + k), a4 = t.D(r, "xPassBin" + k);
                    Near(t.D(r, "ratioBin" + k), a1 + a2 + a3 + a4, 0, "度数 k=" + k); all += a1 + a2 + a3 + a4;
                    gSold += a1; gCap += a2; gWedge += a3; gPass += a4;
                }
                Near(t.D(r, "xPairs"), all + t.D(r, "ratioNaN"), 0, "度数の総和＋NaN＝xPairs");
                Assert.True(gSold <= t.D(r, "xSoldOut") && gCap <= t.D(r, "xCap") && gWedge <= t.D(r, "xWedge") && gPass <= t.D(r, "xPass"), "門ごとの度数は門の件数を超えない");
                if (t.D(r, "ratioNaN") == 0) { Near(t.D(r, "xCap"), gCap, 0, "xCap の度数の和"); Near(t.D(r, "xWedge"), gWedge, 0, "xWedge の度数の和"); Near(t.D(r, "xPass"), gPass, 0, "xPass の度数の和"); }
                rows++; cap += t.D(r, "xCap"); wedge += t.D(r, "xWedge");
            }
            Assert.True(rows > 50 && cap > 0 && wedge > 0, $"対照：行 {rows}・xCap {cap}・xWedge {wedge}");
        }
        finally { restore(); }
    }

    /// <summary>G8（条約港）：tarEff＝Clamp(tariffNow×treatyScale, TariffFloor, 1)。英の港州に外国の持分を置いた盤面で treatyScale＜1・tarEff＜tariffNow の行がある。</summary>
    [Fact]
    public void G8_実効の関税は条約港の目減りを含む()
    {
        bool done = false;
        var (dir, sim, restore) = Run(12, before: s =>
        {
            if (done) return; done = true;
            foreach (var r in s.Nations[2].World.Regions) if (r.IsPort) Politics.AddForeignShare(r, "テスト国", 0.5);
        });
        try
        {
            var t = Read(dir, "tradeskip"); int cmp = 0, cut = 0;
            foreach (var r in t.R)
            {
                if (t.S(r, "nation") == "ALL") continue;
                double now = t.D(r, "tariffNow"), sc = t.D(r, "treatyScale"), eff = t.D(r, "tarEff");
                Near(Math.Clamp(now * sc, -0.25, 1), eff, 1e-12, "tarEff");
                cmp++; if (sc < 1 - 1e-6 && now > 0.01 && eff < now - 1e-9) cut++;
            }
            Assert.True(cmp > 50 && cut > 0, $"対照：行 {cmp}・条約港で目減りした行 {cut}");
        }
        finally { restore(); }
    }

    /// <summary>G8（床）：関税を床より下へ置いた国の tarEff＝TariffFloor。</summary>
    [Fact]
    public void G8_実効の関税は床で止まる()
    {
        var (dir, sim, restore) = Run(12, before: s => { s.Nations[1].World.Tariff = -0.9; });
        try
        {
            var t = Read(dir, "tradeskip"); int bound = 0, cmp = 0;
            foreach (var r in t.R)
            {
                if (t.S(r, "nation") == "ALL") continue;
                double now = t.D(r, "tariffNow"), sc = t.D(r, "treatyScale"), eff = t.D(r, "tarEff");
                Near(Math.Clamp(now * sc, -0.25, 1), eff, 1e-12, "tarEff"); cmp++;
                if (now * sc <= -0.25 + 1e-12) { bound++; Near(-0.25, eff, 1e-12, "床"); }
            }
            Assert.True(cmp > 50 && bound > 0, $"対照：行 {cmp}・床に当たった行 {bound}");
        }
        finally { restore(); }
    }

    /// <summary>G9：門の順（容量→楔）。容量が 0 の売り手は楔にも落ちる値段でも xCap に数える（楔→容量の順に替えると xWedge になる）。</summary>
    [Fact]
    public void G9_門の順は容量が楔より先()
    {
        var capZero = new double[] { 1, 0, 1, 1, 1 };   // 最初の売り手（状態 1）の容量が 0
        var a = Board2([0.4, 0], 0.1, 0.4, 1.05, [0.8, 0.8], capZero);   // 0.8×(1.05＋0.4)＝1.16≧1＝楔でも落ちる
        Assert.Equal(1, Gt(a, Instruments.TsCol.XCap)); Assert.Equal(1, Gt(a, Instruments.TsCol.XWedge));
        Assert.Equal(1, a.C[(int)Instruments.TsCol.GBin0 + 10 + 2]);   // xCapBin の 0.8÷1.0（境 0.8 以上 0.9 未満＝k=2）
    }
}
