using System;
using System.Collections.Generic;
using System.Linq;
using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1295) c134tk：計器 treasledger_s（国庫の科目ごとの出入りと民間の貨幣・全ての国×四半期）と、arrears_s の underLimit（前の月の比）の番人。
/// 事前登録 measurements/m1295_20261122_prereg_tk.txt §3。盤面は 2 国（イギリス・フランス）で、期待は試験が手で置いた額の算術（エンジンの式は呼ばない）。
/// 金は World.Transfer の 1 本で動かす（収入＝世界の外から国庫へ・支出＝国庫から世界の外へ）。</summary>
[Collection("Instruments")]
public class TreasLedgerInstrumentTests
{
    private const ulong Seed = 1295UL;

    private static MultiNationSim NewSim() => new([WorldData.ByName("イギリス"), WorldData.ByName("フランス")], Seed) { DiplomacyFrozen = true };
    private static World Of(MultiNationSim sim, string name) => sim.Nations.First(n => Instruments.NameOf(n.World) == name).World;

    private static void WithInstruments(Action<string> body)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pob_tl_" + Guid.NewGuid().ToString("N"));
        Instruments.Configure(dir, "*");
        try { body(dir); }
        finally { Instruments.Configure(null, null); try { System.IO.Directory.Delete(dir, true); } catch { } }
    }

    /// <summary>その四半期・その国の行を item→値 の表へ。</summary>
    private static Dictionary<string, double> Rows(MultiNationSim sim, int q, string nation)
    {
        var d = new Dictionary<string, double>();
        foreach (var line in Instruments.TreasLedgerPeek(sim))
        {
            var v = line.Split('\t');
            if (v[0] != q.ToString() || v[2] != nation) continue;
            Assert.True(d.TryAdd(v[3], double.Parse(v[4], System.Globalization.CultureInfo.InvariantCulture)), $"同じ項目の行が 2 つ：{v[3]}");
        }
        return d;
    }
    private static void Near(double want, double got, string what) => Assert.True(Math.Abs(want - got) <= 1e-12 * Math.Max(1.0, Math.Abs(want)), $"{what}: 期待 {want} 実際 {got}");
    private static string[] CatRows(Dictionary<string, double> r) => r.Keys.Where(k => !k.StartsWith('_')).OrderBy(k => k, StringComparer.Ordinal).ToArray();

    private static void In(World w, TreasuryCat cat, double x) => World.Transfer((null, null), (w, null, null), x, cat);
    private static void Out(World w, TreasuryCat cat, double x) => World.Transfer((w, null), (null, null, null), x, cat);

    [Fact]
    public void T1_差分の行が科目ごとに一致し_残差0_0の科目は書かない()
    {
        WithInstruments(_ =>
        {
            var sim = NewSim(); var uk = Of(sim, "イギリス"); var fr = Of(sim, "フランス");
            Instruments.Quarter(sim, Seed, 0);   // 最初の観測（前の値を持つ）
            In(uk, TreasuryCat.Tax, 1000.25); Out(uk, TreasuryCat.Army, 300.5); Out(uk, TreasuryCat.OtherOut, 50.125);
            Out(fr, TreasuryCat.OtherOut, 77.7);
            Instruments.Quarter(sim, Seed, 1);
            var a = Rows(sim, 1, "イギリス");
            Near(1000.25, a["Tax"], "英 Tax"); Near(-300.5, a["Army"], "英 Army"); Near(-50.125, a["OtherOut"], "英 OtherOut");
            Assert.Equal(["Army", "OtherOut", "Tax"], CatRows(a));   // 0 の科目は書かない
            Near(1000.25 - 300.5 - 50.125, a["_ledgerSum"], "英 Σ");
            Near(a["_ledgerSum"], a["_treasuryDelta"], "英 国庫の変化");
            Near(0, a["_residual"], "英 残差");
            Near(uk.Treasury.Value, a["_treasuryEnd"], "英 国庫");
            Near(uk.TotalMoney.Value, a["_privateMoney"], "英 民間の貨幣");
            Assert.False(a.ContainsKey("_rebased"));
            var b = Rows(sim, 1, "フランス");
            Assert.Equal(["OtherOut"], CatRows(b));
            Near(-77.7, b["OtherOut"], "仏 OtherOut"); Near(0, b["_residual"], "仏 残差");
        });
    }

    [Fact]
    public void T2_前の四半期の値を更新する_2四半期目は差分だけ()
    {
        WithInstruments(_ =>
        {
            var sim = NewSim(); var uk = Of(sim, "イギリス");
            Instruments.Quarter(sim, Seed, 0);
            In(uk, TreasuryCat.Tax, 1000); Instruments.Quarter(sim, Seed, 1);
            In(uk, TreasuryCat.Tax, 5); Out(uk, TreasuryCat.Navy, 2);
            Instruments.Quarter(sim, Seed, 2);
            var a = Rows(sim, 2, "イギリス");
            Assert.Equal(["Navy", "Tax"], CatRows(a));
            Near(5, a["Tax"], "2 四半期目の Tax は差分だけ"); Near(-2, a["Navy"], "Navy"); Near(0, a["_residual"], "残差");
        });
    }

    [Fact]
    public void T3_助走明けのリベースの四半期に印と跳びを書き_その後の差分は台帳の捨てを引きずらない()
    {
        WithInstruments(_ =>
        {
            var sim = NewSim(); var uk = Of(sim, "イギリス");
            Instruments.Quarter(sim, Seed, 0);
            In(uk, TreasuryCat.Tax, 1000); Instruments.Quarter(sim, Seed, 1);
            double before = uk.Treasury.Value;
            uk.RebaseTreasury(before + 4000);   // 台帳が捨てられる
            In(uk, TreasuryCat.Tax, 7);
            Instruments.Quarter(sim, Seed, 2);
            var a = Rows(sim, 2, "イギリス");
            Assert.Equal(1.0, a["_rebased"]); Near(4000, a["_rebaseJump"], "リベースの跳び");
            Assert.Equal(["Tax"], CatRows(a)); Near(7, a["Tax"], "リベース以後の Tax"); Near(0, a["_residual"], "リベースの四半期の残差");
            In(uk, TreasuryCat.Tax, 3);
            Instruments.Quarter(sim, Seed, 3);
            var c = Rows(sim, 3, "イギリス");
            Assert.False(c.ContainsKey("_rebased")); Near(3, c["Tax"], "次の四半期の Tax"); Near(0, c["_residual"], "次の残差");
        });
    }

    [Fact]
    public void T6_Closeで書き手が閉じられファイルを消せる()
    {
        WithInstruments(dir =>
        {
            var sim = NewSim();
            Instruments.Quarter(sim, Seed, 0);
            Instruments.Close(sim);
            string path = System.IO.Path.Combine(dir, $"treasledger_s{Seed}.tsv");
            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None)) Assert.True(fs.Length > 0);
        });
    }

    [Fact]
    public void T4_台帳を通らない国庫の動きは残差に出る()
    {
        WithInstruments(_ =>
        {
            var sim = NewSim(); var uk = Of(sim, "イギリス");
            Instruments.Quarter(sim, Seed, 0);
            In(uk, TreasuryCat.Tax, 10);
            uk.SetTreasuryRaw(uk.Treasury.Value + 123.5);   // フローでない代入（台帳に載らない）
            Instruments.Quarter(sim, Seed, 1);
            var a = Rows(sim, 1, "イギリス");
            Near(10, a["_ledgerSum"], "Σ"); Near(123.5, a["_residual"], "残差");
        });
    }
}
