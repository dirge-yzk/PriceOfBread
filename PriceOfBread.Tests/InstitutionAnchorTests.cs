using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(621) 線 inst：制度と思想の行の錨の番人（機構と不変条件）。事前登録 measurements/inst621_20261028_prereg.txt の [5]。
/// 旗 <see cref="Coefficients.SpreadLiteracyFull"/>（技術の比＝本国の識字÷基準）と <see cref="Coefficients.SpreadPrereqEdges"/>（共産主義 ← 社会主義）の 2 本。既定 0＝今のまま。
/// 期待値は表から作らない：識字と点数は手で数える・辺は手書きの一覧（出典は『共産党宣言』1848）。
/// ⚠壊し方（突然変異で赤を確かめた）＝クランプを外す／本国でなく全州の識字にする／辺の読みを外す／旗 0 でも識字を読む → 赤。</summary>
public class InstitutionAnchorTests
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

    /// <summary>本国の州の POP の識字を core に・植民地の州の POP の識字を colonial に揃える（本国だけを読むことを分けて見る）。</summary>
    private static void SetLiteracy(World w, double core, double colonial)
    {
        foreach (var r in w.Regions)
            foreach (var p in r.Pops) p.Literacy = r.Tier == RegionTier.Colonial ? colonial : core;
    }

    private static readonly string[] SpreadNamesWithD = ["株式会社", "社会主義", "共産主義", "帝国主義", "ファシズム"];   // d＞0 の行（特許は 1780 年代＝d＝0）

    [Fact]
    public void 比の関数は識字を基準で割りクランプし不正な識字は閉じる側へ倒す()
    {
        Assert.Equal(0.0, Discovery.SpreadLiteracyRatio(0.0, 0.5));
        Assert.Equal(0.5, Discovery.SpreadLiteracyRatio(0.25, 0.5), 12);
        Assert.Equal(1.0, Discovery.SpreadLiteracyRatio(0.5, 0.5));
        Assert.Equal(1.0, Discovery.SpreadLiteracyRatio(0.9, 0.5));
        Assert.Equal(0.0, Discovery.SpreadLiteracyRatio(double.NaN, 0.5));
        Assert.Equal(0.0, Discovery.SpreadLiteracyRatio(-0.3, 0.5));
        Assert.Equal(1.0, Discovery.SpreadLiteracyRatio(0.3, 0.0));   // 基準が正でなければ呼ばれない（旗 0）＝1
    }

    [Fact]
    public void 旗0は識字に依らず今のまま_陽性対照で旧の技術水準の式に戻る()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        Assert.Equal(0.0, w.Coef.SpreadLiteracyFull.Value);   // 既定 0
        Assert.Equal(0.0, w.Coef.SpreadPrereqEdges);
        SetLiteracy(w, 0.0, 0.0);
        foreach (int row in Discovery.SpreadRows)
            Assert.Equal(Discovery.TermMax, Discovery.Evaluate(w, null, w.Coef, includeKnown: true)[row].Tech, 12);   // 既定は TechThresholdOff＝1（比 1＝満点）
        // 陽性対照：技術水準の式（旗 TechThresholdOff＝0）は d＞0 の行で技術水準 0 だと 0
        w.Coef.TechThresholdOff = 0; w.TechLevel = 0.0;
        Assert.Equal(0.0, Discovery.Evaluate(w, null, w.Coef, includeKnown: true)[Row("社会主義")].Tech);
    }

    [Fact]
    public void 旗が正なら技術の項は本国の識字に比例し基準以上は満点で特許は識字に依らない()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        // 前提：本国と植民地が両方ある（植民地の識字が混ざる変異を赤にするため）
        Assert.Contains(w.Regions, r => r.Tier == RegionTier.Colonial && r.Pops.Count > 0);
        Assert.Contains(w.Regions, r => r.Tier != RegionTier.Colonial && r.Pops.Count > 0);
        w.Coef.SpreadLiteracyFull = new Share(0.5);
        SetLiteracy(w, 0.25, 1.0);   // 本国 0.25・植民地 1.0（全州の平均だと 0.25 より上へ引き上がる）
        var t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        foreach (string n in SpreadNamesWithD)
            Assert.Equal(Discovery.TermMax * 0.5, t[Row(n)].Tech, 9);   // 70×(0.25÷0.5)
        Assert.Equal(Discovery.TermMax, t[Row("特許")].Tech, 12);   // d＝0 の行は識字に依らず満点
        SetLiteracy(w, 0.6, 0.0);
        t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        foreach (string n in SpreadNamesWithD) Assert.Equal(Discovery.TermMax, t[Row(n)].Tech, 12);   // 基準以上は満点（上へ突き抜けない）
        SetLiteracy(w, 0.0, 1.0);
        t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        foreach (string n in SpreadNamesWithD) Assert.Equal(0.0, t[Row(n)].Tech, 12);   // 本国の識字 0 なら 0
        // 不正な識字（NaN）は閉じる側
        SetLiteracy(w, 0.6, 0.6);
        foreach (var r in w.Regions) foreach (var p in r.Pops) { p.Literacy = double.NaN; break; }
        t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        foreach (string n in SpreadNamesWithD) Assert.Equal(0.0, t[Row(n)].Tech, 12);
        // 旗が NaN なら旗 0（否定形）
        SetLiteracy(w, 0.0, 0.0);
        w.Coef.SpreadLiteracyFull = new Share(double.NaN);
        t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        foreach (string n in SpreadNamesWithD) Assert.Equal(Discovery.TermMax, t[Row(n)].Tech, 12);
    }

    [Fact]
    public void 辺の表は共産主義が社会主義の後の1本だけで静的な前提の表には入れない()
    {
        var byHand = new[] { ("共産主義", "社会主義") };
        Assert.Equal(byHand, Discovery.SpreadEdgeNames);
        for (int r = 0; r < Catalog.Discoveries.Length; r++)
        {
            int expect = Catalog.Discoveries[r].Name == "共産主義" ? Row("社会主義") : -1;
            Assert.Equal(expect, Discovery.SpreadPrereqRow(r));
            Assert.True(Catalog.DiscoveryAfter[r] < 0 || !Discovery.IsSpread(Catalog.Discoveries[r].Field), "制度と思想の行は静的な前提の表に入れない（旗 0 の軌道を動かさない）");
        }
        Assert.Equal(-1, Discovery.SpreadPrereqRow(-1));
        Assert.Equal(-1, Discovery.SpreadPrereqRow(int.MaxValue));
    }

    [Fact]
    public void 辺は旗が正のときだけ前提の行が未解禁の国で子の行を判定せず解禁した次の判定で出す()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        int soc = Row("社会主義"), comm = Row("共産主義");
        w.Discovered[soc] = false; w.Discovered[comm] = false;
        // 旗 0：前提が未知でも共産主義の項は出る（今のまま）
        var t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        Assert.Equal(Discovery.TermMax, t[comm].Tech, 12);
        // 旗 >0：前提が未知なら項は出ない（点数 0）・社会主義は影響されない
        w.Coef.SpreadPrereqEdges = 1;
        t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        Assert.Equal(0.0, t[comm].Tech); Assert.Equal(0.0, t[comm].Need); Assert.Equal(0.0, t[comm].Transmit);
        Assert.Equal(Discovery.TermMax, t[soc].Tech, 12);
        // 前提を解禁した次の判定で出る
        w.Discovered[soc] = true;
        t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        Assert.Equal(Discovery.TermMax, t[comm].Tech, 12);
        // 辺の旗は NaN なら旗 0（否定形）
        w.Discovered[soc] = false; w.Coef.SpreadPrereqEdges = double.NaN;
        t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        Assert.Equal(Discovery.TermMax, t[comm].Tech, 12);
    }
}
