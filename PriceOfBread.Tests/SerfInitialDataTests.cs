using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>★(487) 線 mg2：農奴制の初期値（world.json の <c>initialSerfEmancipation</c>）の番人。作者 2026-10-21「農奴制の初期値→推奨」。
/// 期待は**手で書いた表**（事前登録 measurements/mg487_20261021_prereg.txt [3]）＝世界データから作らない。表に無い国は全面（既定）。
/// ①全 61 国の初期値が表と一致 ②表の名前が世界データに全て在る（綴りの取り違えで表が空振りしない）③農奴の門の倍率が段の通りに効き、清は止まらず・日本は全面の半分・露は 0。
/// ④清と日本の国内の移民の農村の出が開幕から動く（旧データでは農奴解放がなし＝農村の出が全期 0 だった）。</summary>
public class SerfInitialDataTests
{
    private static readonly string[] None = ["ロシア", "プロイセン", "ポーランド"];
    private static readonly string[] Limited =
    [
        "フランス", "日本", "オスマン", "オーストリア", "スペイン", "サルデーニャ＝ピエモンテ", "ポルトガル",
        "デンマーク", "ライン諸邦", "バイエルン", "ザクセン", "ハノーファー", "ヴュルテンベルク",
        "両シチリア", "教皇領", "ビルマ", "シャム", "カンボジア", "ラオス", "エチオピア",
    ];

    [Fact]
    public void 世界データの農奴解放の初期値は史実の表と全61国で一致する()
    {
        var all = WorldData.Nations();
        Assert.Equal(61, all.Length);
        var listed = None.Concat(Limited).ToArray();
        Assert.Equal(listed.Length, listed.Distinct().Count());
        foreach (var n in listed) Assert.Contains(all, c => c.Name == n);   // 表の綴りが世界データに在る
        var wrong = new List<string>();
        foreach (var c in all)
        {
            var expect = None.Contains(c.Name) ? ReformLevel.None : Limited.Contains(c.Name) ? ReformLevel.Limited : ReformLevel.Full;
            if (c.InitialSerfEmancipation != expect) wrong.Add($"{c.Name}（期待 {expect}・実際 {c.InitialSerfEmancipation}）");
        }
        Assert.True(wrong.Count == 0, string.Join("、", wrong));
        Assert.Equal(3, all.Count(c => c.InitialSerfEmancipation == ReformLevel.None));
        Assert.Equal(20, all.Count(c => c.InitialSerfEmancipation == ReformLevel.Limited));
        Assert.Equal(38, all.Count(c => c.InitialSerfEmancipation == ReformLevel.Full));
    }

    [Fact]
    public void 清は農奴の門に掛からず日本は全面の半分でロシアは0()
    {
        var on = new Coefficients { SerfGateCoversMigration = 1 };
        double Factor(string name)
        {
            var sim = new MultiNationSim([WorldData.ByName(name)], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
            return Territory.SerfMigrationFactor(sim.Nations[0].World, on);
        }
        Assert.Equal(1.0, Factor("清"));
        Assert.Equal(0.5, Factor("日本"));
        Assert.Equal(0.0, Factor("ロシア"));
        Assert.Equal(0.0, Factor("プロイセン"));
    }

    /// <summary>国内の移民（<c>Territory.Step</c>）の農村の出：1 州を押し出す州にして 1 回だけ回し、旗 0（門なし）と旗 1（門あり）の農村の出を比べる。</summary>
    private static (double Open, double Gated) RuralOut(string nation)
    {
        double Run(double gate)
        {
            var sim = new MultiNationSim([WorldData.ByName(nation)], seed: 1789, coef: new Coefficients()) { DiplomacyFrozen = true };
            var w = sim.Nations[0].World; var c = w.Coef;
            c.ImmigrationReceiveCapPerQuarter = new RatePerQuarter(1e9);
            c.SerfGateCoversMigration = gate;
            const double O = 2.0;
            Region S = w.Regions.First(r => r.State == RegionState.Active && r.Pops.Any(p => p.ClassId == PopClass.Peasant && !p.IsUrban && !p.IsSoldier && p.Size > 1));
            foreach (var r in w.Regions)
            {
                if (r.State != RegionState.Active) continue;
                double o = Territory.Opportunity(r, c);
                r.ProductivityBonus *= (ReferenceEquals(r, S) ? 0.2 * O / Territory.Holding(r, c) : O) / o;
            }
            var sp = S.Pops.Where(p => !p.IsSoldier && (p.ClassId == PopClass.Peasant || p.ClassId == PopClass.Worker)).ToList();
            var before = sp.Select(p => p.Size).ToList();
            Territory.Step(w, c);
            double ru = 0;
            for (int i = 0; i < sp.Count; i++) if (!sp[i].IsUrban) ru += System.Math.Max(0, before[i] - sp[i].Size);
            return ru;
        }
        return (Run(0), Run(1));
    }

    [Fact]
    public void 清の農民は開幕から国内で動け日本は全面の半分でロシアは動けない()
    {
        var cn = RuralOut("清");
        Assert.True(cn.Open > 0, "前提が崩れた：旗 0 で清の農村の出が無い＝番人が何も検査していない");
        Assert.True(cn.Gated > 0, "清は全面＝農奴の門を掛けても農村の出が止まらない");
        Assert.Equal(cn.Open, cn.Gated, 9);                       // 全面の倍率は 1＝門の有無で同じ
        var jp = RuralOut("日本");
        Assert.True(jp.Open > 0, "前提が崩れた：旗 0 で日本の農村の出が無い");
        Assert.Equal(0.5 * jp.Open, jp.Gated, 9);                 // 限定＝半分（門は倍率 (1−1)＋1×0.5）
        var ru = RuralOut("ロシア");
        Assert.True(ru.Open > 0, "前提が崩れた：旗 0 でロシアの農村の出が無い");
        Assert.Equal(0.0, ru.Gated);                              // なし＝動けない
    }
}
