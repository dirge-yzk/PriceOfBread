using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>
/// (1061) 線 c101sel：計器の〈出す表を選ぶ口〉（POB_INSTRUMENTS_TABLES・Instruments.SelectTables）の番人。事前登録 measurements/m1061_20261112_prereg_sel.txt。
/// S1＝未設定・空は全表。S2＝選んだ表は全表の run とバイト一致・選ばない表のファイルは 0 本（Open を通らない 3 表を除く）。
/// S3＝計算の切り（prodcap・sellr・subst・cpop・cearn）が他の表を変えない。S4＝対照：全表の run では切られる表が正の行数で出る（何もしない実装で緑にしない）。
/// </summary>
[Collection("Instruments")]
public class InstrumentsTablesSelectTests
{
    private const ulong Seed = 1789UL;
    // Open を通らず File.WriteAllText で書く表（選びの外・prereg [2]）
    private static readonly string[] Bypass = ["habitlg_s", "inheritintent_s", "inheritpar_s"];
    private static readonly string[] Cut = ["prodcap_s", "sellr_s", "subst_s", "cpop_s", "cearn_s", "cgrp_s"];

    private static string Run(string? sel, int quarters = 12)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_sel_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        Instruments.SelectTables(sel);
        int every0 = Instruments.NgEveryYears; Instruments.NgEveryYears = 1;
        try
        {
            var sim = new MultiNationSim([NationConfig.France(), NationConfig.Prussia(), NationConfig.Britain(), NationConfig.Austria()], Seed, _ => new UtilityAgent(), null);
            foreach (var n in sim.Nations) n.World.Coef.CivilWarLandslideRatio = 0;
            sim.DiplomacyFrozen = true;
            Instruments.Phase(sim, Seed, "open");
            sim.StartWarmup();
            Instruments.Phase(sim, Seed, "warm");
            try { for (int q = 0; q < quarters; q++) { sim.StepQuarter(); Instruments.Quarter(sim, Seed, q); } }
            finally { Instruments.Close(sim); }
        }
        finally { Instruments.NgEveryYears = every0; Instruments.SelectTables(null); Instruments.Configure(null, null); }
        return dir;
    }

    private static string[] Tables(string dir) => System.IO.Directory.GetFiles(dir).Select(f => System.IO.Path.GetFileName(f)).Select(f => f[..f.IndexOf($"{Seed}.tsv", StringComparison.Ordinal)]).OrderBy(x => x, StringComparer.Ordinal).ToArray();

    [Fact]
    public void S1_未設定と空は全表()
    {
        Instruments.SelectTables(null);
        Assert.True(Instruments.Wants("nation_s") && Instruments.Wants("anything_s"));
        Instruments.SelectTables("");
        Assert.True(Instruments.Wants("nation_s") && Instruments.Wants("anything_s"));
        Instruments.SelectTables("nation_s, stock_s");
        Assert.True(Instruments.Wants("nation_s") && Instruments.Wants("stock_s"));
        Assert.False(Instruments.Wants("nation") || Instruments.Wants("region_s"));   // 完全一致（頭の一致ではない）
        Instruments.SelectTables(null);
        Assert.True(Instruments.CearnOn == (System.Environment.GetEnvironmentVariable("POB_CEARN") != "0"));
    }

    [Fact]
    public void S2_S3_S4_選んだ表は全表の走りとバイト一致で選ばない表は0本()
    {
        string full = Run(null), sel = Run("nation_s,stock_s");
        try
        {
            var tf = Tables(full); var ts = Tables(sel);
            // S4 対照：全表の走りでは、切られる表が正の行数で出る（切りが何もしない実装でも緑になる、を防ぐ）
            int cutPositive = 0;
            foreach (var t in Cut)
            {
                string p = System.IO.Path.Combine(full, $"{t}{Seed}.tsv");
                if (System.IO.File.Exists(p) && System.IO.File.ReadLines(p).Skip(1).Any()) cutPositive++;
            }
            Assert.True(cutPositive >= 3, $"全表の走りで切られる表が正の行数で出ていない（{cutPositive}/{Cut.Length}）");
            Assert.True(tf.Length > 20);
            // S2：選ばない表のファイルは 0 本（口を通らない表を除く）
            var extra = ts.Where(t => t != "nation_s" && t != "stock_s" && !Bypass.Contains(t)).ToArray();
            Assert.Empty(extra);
            // S2：選んだ表は全表の走りとバイト一致
            foreach (var t in new[] { "nation_s", "stock_s" })
            {
                string pf = System.IO.Path.Combine(full, $"{t}{Seed}.tsv"), ps = System.IO.Path.Combine(sel, $"{t}{Seed}.tsv");
                Assert.True(System.IO.File.Exists(pf) && System.IO.File.Exists(ps), t);
                Assert.Equal(System.IO.File.ReadAllBytes(pf), System.IO.File.ReadAllBytes(ps));
            }
            // S3：計算の切りが入っても選んだ表を含む世界の動きは変わらない（nation_s は全国・全四半期の人口・GDP を持つ）＝上のバイト一致が根拠。切られる表が 1 本も出ていないことも確かめる。
            foreach (var t in Cut) Assert.DoesNotContain(t, ts);
        }
        finally { try { System.IO.Directory.Delete(full, true); } catch { } try { System.IO.Directory.Delete(sel, true); } catch { } }
    }
}
