using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(621) 線 inst：制度と思想の行の錨の番人（機構と不変条件）。事前登録 measurements/inst621_20261028_prereg.txt の [5]。
/// 係数 <see cref="Coefficients.SpreadLiteracyFull"/>（技術の比＝本国の識字÷基準・2026-10-28 に 0.5 で点火・0 で旧の式）と、辺（共産主義 ← 社会主義＝<see cref="Catalog.DiscoveryAfter"/> へ移した 1 本）。
/// 期待値は表から作らない：識字と点数は手で数える・辺は手書きの一覧（出典は『共産党宣言』1848・DiscoveryTierTests の手書きの一覧と別に、ここは 1 本の振る舞いを見る）。
/// ⚠壊し方（突然変異で赤を確かめた）＝クランプを外す／本国でなく全州の識字にする／辺の読みを外す／係数 0 でも識字を読む → 赤。</summary>
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
    public void 係数0は識字に依らず技術の比は1_既定は0点5()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        Assert.Equal(0.5, w.Coef.SpreadLiteracyFull.Value);   // 既定 0.5（点火）
        w.Discovered[Row("社会主義")] = true;   // 共産主義 ← 社会主義の辺：前提が既知の国で見る
        w.Coef.SpreadLiteracyFull = new Share(0.0);           // 0＝識字を読まない（比 1）
        SetLiteracy(w, 0.0, 0.0);
        w.TechLevel = 0.0;                                     // 技術水準 0 でも比は 1（制度と思想の行は技術水準を読まない）
        foreach (int row in Discovery.SpreadRows)
            Assert.Equal(Discovery.TermMax, Discovery.Evaluate(w, null, w.Coef, includeKnown: true)[row].Tech, 12);   // 比 1＝満点
    }

    [Fact]
    public void 係数が正なら技術の項は本国の識字に比例し基準以上は満点で特許は識字に依らない()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        w.Discovered[Row("社会主義")] = true;   // 共産主義 ← 社会主義の辺：前提が既知の国で見る
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
        // 係数が NaN なら 0 と同じ（否定形）
        SetLiteracy(w, 0.0, 0.0);
        w.Coef.SpreadLiteracyFull = new Share(double.NaN);
        t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        foreach (string n in SpreadNamesWithD) Assert.Equal(Discovery.TermMax, t[Row(n)].Tech, 12);
    }

    [Fact]
    public void 前提の辺は共産主義が社会主義の後の1本だけで制度と思想の他の行は前提を持たない()
    {
        for (int r = 0; r < Catalog.Discoveries.Length; r++)
        {
            if (!Discovery.IsSpread(Catalog.Discoveries[r].Field)) continue;
            int expect = Catalog.Discoveries[r].Name == "共産主義" ? Row("社会主義") : -1;
            Assert.Equal(expect, Catalog.DiscoveryAfter[r]);
        }
    }

    [Fact]
    public void 前提の行が未解禁の国では共産主義を判定せず解禁した次の判定で出す()
    {
        var sim = Board();
        var w = Nation(sim, "イギリス");
        int soc = Row("社会主義"), comm = Row("共産主義");
        w.Discovered[soc] = false; w.Discovered[comm] = false;
        var t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        Assert.Equal(0.0, t[comm].Tech); Assert.Equal(0.0, t[comm].Need); Assert.Equal(0.0, t[comm].Transmit);
        Assert.True(t[soc].Tech > 0.0, "社会主義は影響されない");
        w.Discovered[soc] = true;
        t = Discovery.Evaluate(w, null, w.Coef, includeKnown: true);
        Assert.True(t[comm].Tech > 0.0, "前提を解禁した次の判定で出る");
    }
}
