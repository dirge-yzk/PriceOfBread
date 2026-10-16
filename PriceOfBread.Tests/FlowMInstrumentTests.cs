using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// ★(371) 流れM の計器 2 本（<c>fm_s</c>・読むだけ）の番人。
/// ①純支持の分解：街頭の 3 ブロック＋議会＋軍の項の和が <c>PolicyPreferences.NetSupport</c> に合う（計器の写しと本体の突き合わせ＝同じ式の写しが食い違えばここで赤）・
///   軍の項は <c>ArmyPoliticalWeight</c> に比例し 0 で 0・向きを反転すると符号が反転する（期待値は式でなく係数の倍数から書く）。
/// ②陸の必要量の組成：3 部分の和が <c>Power.NeedLand</c> に合い、海の 2 部分の和が <c>NeedSea</c> に合い、海の金の割合の分母が <c>max(陸＋内, 自国の軍の大きさ)</c> の式に合う（列から書き直した式）。
/// ③走行：行が出る・値が入った行がある（何もしない番人は必ず緑になる＝非空虚の対照）。軌道が計器で変わらないことは既存の全ビット一致の番人（InstrumentsTests）が同じ hook を通って担う。
/// </summary>
[Collection("Instruments")]
public class FlowMInstrumentTests
{
    private static MultiNationSim Make() => new([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent());

    private static (string[] Header, List<string[]> Rows) Table(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        return (lines[0].Split('\t'), lines.Skip(1).Select(l => l.Split('\t')).ToList());
    }
    private static double Col(string[] h, string[] r, string name)
    {
        int i = Array.IndexOf(h, name); Assert.True(i >= 0, "列が無い " + name);
        return r[i] switch { "nan" => double.NaN, "" => double.NaN, "inf" => double.PositiveInfinity, "-inf" => double.NegativeInfinity, var s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) };
    }

    private static void Near(double expect, double actual) => Assert.True(Math.Abs(expect - actual) <= 1e-8 * Math.Max(1.0, Math.Abs(expect)), $"期待 {expect}・実際 {actual}");

    [Fact]
    public void 純支持の項の和が本体の純支持に合い_軍の項は係数に比例し向きで符号が反転する()
    {
        var sim = Make();
        sim.StartWarmup();
        for (int q = 0; q < 24; q++) sim.StepQuarter();
        int checkedWorlds = 0, armyNonZero = 0;
        foreach (var n in sim.Nations)
        {
            var w = n.World;
            foreach (bool inc in new[] { true, false })
            {
                var e = PolicyPreferences.Build(w);
                double net = PolicyPreferences.NetSupport(w, PolicyAxis.Coercion, inc, e);
                Instruments.FmNet(w, e, inc);
                var a = Instruments.FmAccOf(w);
                double sum = a.NetTrad + a.NetModern + a.NetMass + a.NetParl + a.NetArmy;
                Assert.True(Math.Abs(sum - net) <= 1e-9 * Math.Max(1.0, Math.Abs(net)), $"{n.World.Year} {(inc ? "増" : "減")} 項の和 {sum} が純支持 {net} に合わない");
                // 軍の項＝軍の政治力×向き：係数を 0 にすると 0・2 倍にすると 2 倍（期待値は係数の倍数から）
                double w0 = w.Coef.ArmyPoliticalWeight, army0 = a.NetArmy;
                if (w0 > 0 && a.ArmyPowerV > 0)
                {
                    armyNonZero++;
                    Assert.Equal(inc ? 1.0 : -1.0, Math.Sign(army0));
                    w.Coef.ArmyPoliticalWeight = 2 * w0;
                    Instruments.FmNet(w, e, inc);
                    Assert.Equal(2 * army0, Instruments.FmAccOf(w).NetArmy, 12);
                    w.Coef.ArmyPoliticalWeight = 0;
                    Instruments.FmNet(w, e, inc);
                    Assert.Equal(0.0, Instruments.FmAccOf(w).NetArmy, 12);
                    w.Coef.ArmyPoliticalWeight = w0;
                }
                Instruments.FmAccOf(w).ClearNet();
                checkedWorlds++;
            }
        }
        Assert.Equal(sim.Nations.Count * 2, checkedWorlds);
        Assert.True(armyNonZero > 0, "軍の政治力が正の国が 1 つも無い＝軍の項の検査が空振り（対照の盤面でない）");
    }

    [Fact]
    public void 陸と海の必要量の組成の和が本体に合い_海の金の割合の分母が式に閉じる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_fm_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try
        {
            var sim = Make();
            Instruments.Phase(sim, 1789UL, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, 1789UL, "warm");
            try
            {
                for (int q = 0; q < 24; q++) { sim.StepQuarter(); Instruments.Quarter(sim, 1789UL, q); }
            }
            finally { Instruments.Close(sim); }
            var (h, rows) = Table(System.IO.Path.Combine(dir, "fm_s1789.tsv"));
            Assert.All(rows, r => Assert.Equal(h.Length, r.Length));
            Assert.True(rows.Count >= 4 * 24 - 4, $"行が少ない {rows.Count}");
            int army = 0, homeBorder = 0, binds = 0, notBinds = 0, withNet = 0, outward = 0;
            foreach (var r in rows)
            {
                if (r[Array.IndexOf(h, "landSideBindsFlag")] != "")
                {
                    army++;
                    double needLand = Col(h, r, "needLandIndex"), parts = Col(h, r, "needLandBorderHomeIndex") + Col(h, r, "needLandBorderColonialIndex") + Col(h, r, "needLandProjectionIndex");
                    Assert.True(Math.Abs(parts - needLand) <= 1e-9 * Math.Max(1.0, needLand), $"陸の必要量の 3 部分の和 {parts} が本体の {needLand} に合わない");
                    double sea = Col(h, r, "needSeaIndex"), seaParts = Col(h, r, "needSeaFleetIndex") + Col(h, r, "needSeaCrossIndex");
                    Assert.True(Math.Abs(seaParts - sea) <= 1e-9 * Math.Max(1.0, sea), $"海の必要量の 2 部分の和 {seaParts} が本体の {sea} に合わない");
                    double inner = Col(h, r, "needInternalIndex"), fp = Col(h, r, "forcePotentialIndex"), den = Col(h, r, "landSideDenominatorIndex");
                    Near(Math.Max(needLand + inner, fp), den);
                    int flag = (int)Col(h, r, "landSideBindsFlag");
                    Assert.Equal(needLand + inner >= fp ? 1 : 0, flag);
                    if (flag == 1) binds++; else notBinds++;
                    if (sea + needLand + inner > 0) Near(sea / (sea + den), Col(h, r, "navalSpendShareIdealShare"));   // 列は 10 桁（G10）なので相対 1e-8
                    // 内の守りの材料：人口×不穏度×係数＝要る人の頭数（質で兵の戦力に直す前）
                    Assert.True(Col(h, r, "garrisonMenIndex") >= 0 && Col(h, r, "unrestShare") is >= 0 and <= 1);
                    if (Col(h, r, "needLandBorderHomeIndex") > 0) homeBorder++;
                    // 自国の陸軍が外へ向く割合＝外向きの和÷（外向きの和＋不穏度）：別の列（不穏度）から書き直す
                    double outW = Col(h, r, "ownArmyOutwardWeightIndex"), outS = Col(h, r, "ownArmyOutwardShare"), unrestS = Col(h, r, "unrestShare");
                    Assert.True(outW >= 0 && outS is >= 0 and <= 1, $"外向きの割合が [0,1] の外 {outS}（和 {outW}）");
                    if (outW + unrestS > 1e-12) Near(outW / (outW + unrestS), outS);
                    if (outW > 0) outward++;
                }
                if (r[Array.IndexOf(h, "netIncreasingFlag")] != "")
                {
                    withNet++;
                    double sum = Col(h, r, "netTermsSumIndex"), net = Col(h, r, "moveNetSupportIndex");
                    Assert.True(Math.Abs(sum - net) <= 1e-9 * Math.Max(1.0, Math.Abs(net)), $"純支持の項の和 {sum} が nation の列の純支持 {net} に合わない");
                    Assert.True(Math.Abs(Col(h, r, "netTermsGapIndex")) <= 1e-9 * Math.Max(1.0, Math.Abs(net)));
                }
            }
            Assert.True(army >= 4 * 20, $"組成の行が少ない {army}（hook が Compute の終わりに来ていない）");
            Assert.True(homeBorder > 0, "本国どうしの国境から来る陸の必要量が 1 行も無い＝4 国は陸続きなのに部分が空");
            Assert.True(outward > 0, "外向きの和が正の行が無い＝注意配分の外向きの列が空");
            Assert.True(binds + notBinds > 0 && withNet > 0, $"分母の判定 {binds + notBinds}・純支持の行 {withNet} が空振り");
        }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
