using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (948) 建設の州の表 <c>cons_s</c> の出入り：州が四半期の途中で国を出ると、その四半期の払いは支払いの表（払った時点の国）にだけ載り、州の表（四半期の末の持ち主の行）には載らなかった
/// ＝戻った四半期に古い積算が戻った国の行として出た。出る州の積算を出た国の行（activeFlag 0）として吐き出して州から外す。
/// 盤面：4 国・外交凍結・助走の後 6 四半期目の途中で仏の州（首都以外）を全て普へ割譲し（<c>Territory.TransferRegion</c>）、仏と普の払いの和が両側の表で全ての区切りに閉じることを確かめる。
/// 閉じる相手は別の積算器（spend_s の「建設の材料と工賃」）。対照：仏の出た州の行（activeFlag 0）が 1 本以上あり払いが正・比べた区切りを一つも除かない。
/// </summary>
[Collection("Instruments")]
public class ConsDepartTests
{
    static (string[] H, List<string[]> R) Tab(string path)
    {
        var lines = System.IO.File.ReadAllLines(path);
        return (lines[0].Split('	'), lines.Skip(1).Select(l => l.Split('	')).ToList());
    }
    static double Num(string[] h, string[] r, string c) { var s = r[Array.IndexOf(h, c)]; return s == "nan" ? double.NaN : double.Parse(s, System.Globalization.CultureInfo.InvariantCulture); }

    [Fact]
    public void 出た州の建設の払いは出た国の行に載り両側の表が閉じる()
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_cd_" + Guid.NewGuid().ToString("N"));
        try
        {
            Instruments.Configure(dir, "*");
            int moved = 0;
            try
            {
                var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], 1789UL, _ => new UtilityAgent()) { DiplomacyFrozen = true };
                Instruments.Phase(sim, 1789UL, "open"); sim.StartWarmup(); Instruments.Phase(sim, 1789UL, "warm");
                for (int q = 0; q < 10; q++)
                {
                    sim.StepQuarter();
                    if (q == 5)
                    {
                        var fr = sim.Nations.First(n => Instruments.NameOf(n.World) == "フランス").World; var pr = sim.Nations.First(n => Instruments.NameOf(n.World) == "プロイセン").World;
                        foreach (var r in fr.Regions.Where(r => r.Id != 0).ToList()) { Territory.TransferRegion(fr, pr, r, Territory.Rehoming.Ceded); moved++; }
                    }
                    Instruments.Quarter(sim, 1789UL, q);
                }
                Instruments.Close(sim);
            }
            finally { Instruments.Configure(null, null); }
            Assert.True(moved > 0, "対照：割譲した州が無い");

            var (ch, cr) = Tab(System.IO.Path.Combine(dir, "cons_s1789.tsv")); var (sh, sr) = Tab(System.IO.Path.Combine(dir, "spend_s1789.tsv"));
            int cQ = Array.IndexOf(ch, "quarter"), cN = Array.IndexOf(ch, "nation"), cA = Array.IndexOf(ch, "activeFlag");
            int sB = Array.IndexOf(sh, "bucket"), sN = Array.IndexOf(sh, "nation"), sK = Array.IndexOf(sh, "kind"), sS = Array.IndexOf(sh, "slotName");
            static int BucketOf(int q) { int b = 0; for (int i = 1; i < Instruments.SpendBucketStart.Length; i++) if (q >= Instruments.SpendBucketStart[i]) b = i; return b; }
            double departedPaid = 0; int departedRows = 0, compared = 0;
            foreach (var nation in new[] { "フランス", "プロイセン" })
            {
                var cp = new Dictionary<int, double>(); var sp = new Dictionary<int, double>();
                foreach (var r in cr.Where(r => r[cN] == nation))
                {
                    double pm = Num(ch, r, "paidPerMonth"); if (double.IsNaN(pm)) continue;
                    int b = BucketOf(int.Parse(r[cQ])); cp[b] = cp.GetValueOrDefault(b) + pm * Num(ch, r, "monthsCount");
                    if (r[cA] == "0" && nation == "フランス") { departedRows++; departedPaid += pm; }
                }
                foreach (var r in sr.Where(r => r[sN] == nation && r[sK] == "extra" && r[sS] == "建設の材料と工賃")) { int b = int.Parse(r[sB]); sp[b] = sp.GetValueOrDefault(b) + Num(sh, r, "payPerMonth") * Num(sh, r, "monthsCount"); }
                Assert.True(sp.Values.Sum() > 0, $"対照：{nation}の建設の払いが 0");
                foreach (var b in cp.Keys.Union(sp.Keys))
                {
                    double a = cp.GetValueOrDefault(b), c = sp.GetValueOrDefault(b); compared++;
                    Assert.True(Math.Abs(a - c) <= 1e-9 * Math.Max(1.0, Math.Abs(c)), $"{nation} 区切り{b}：州の表の払い {a} が支払いの表 {c} に閉じない");
                }
            }
            Assert.True(departedRows > 0 && departedPaid > 0, $"対照：出た州の行が無いか払いが 0（行 {departedRows}・払い {departedPaid}）");
            Assert.True(departedRows <= moved, $"出た州の行 {departedRows} が割譲した州 {moved} より多い");
        }
        finally { try { System.IO.Directory.Delete(dir, true); } catch { } }
    }
}
