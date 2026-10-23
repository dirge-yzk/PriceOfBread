using PriceOfBread.App.ViewModels;
using PriceOfBread.SimCore;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>★(501) 段3：政治と法のページの表（SimCore/Display/LawTables.cs）の番人。機構と不変条件だけを見る（画面の見た目は --uishot が見る）。
/// 法の表は法の目録（<see cref="LawCatalog"/>）を回して並べる＝法を足しても画面は直さない。段の押し釦の可否とホバーは法の行（<see cref="LawRowVM"/>）と同じ 1 本。</summary>
public class PoliticsTablesTests
{
    private static (MultiNationSim Sim, World W) Board()
    {
        var sim = new MultiNationSim([NationConfig.Britain(), NationConfig.Prussia()], seed: 1789);
        return (sim, sim.Nations[0].World);
    }

    private static IEnumerable<StageCell> Stages(RowData r) => r.Cells.OfType<StageCell>();

    [Fact]
    public void 法の表の行は法の目録から並び_群は法の群と国有化だけ_鍵は重ならない()
    {
        var (sim, w) = Board();
        w.Coef.NationalizationGate = 1; w.Coef.GuildLawGate = 1;   // 休眠中の法（出す条件が偽）は行ごと出ない＝門を開けて「その他」の群を確かめる
        var t = PoliticsTables.Laws.Read(sim, w);
        var visible = LawCatalog.In(LawSlot.Law).Where(d => d.IsVisible(w)).Concat(LawCatalog.In(LawSlot.Nationalization).Where(d => d.IsVisible(w))).Select(d => d.Key).ToArray();
        Assert.Equal(visible, t.Rows.Select(r => r.Key).ToArray());   // 目録の並びのまま
        Assert.Equal(t.Rows.Count, t.Rows.Select(r => r.Key).Distinct().Count());
        Assert.Equal(LawCatalog.In(LawSlot.Nationalization).Count(), t.Rows.Count(r => r.Group == PoliticsTables.NationalizationGroup));
        Assert.All(t.Rows, r => Assert.False(string.IsNullOrEmpty(r.Group)));   // 群に載らない法は「その他」に入る
        Assert.Contains(t.Rows, r => r.Group == PoliticsTables.OtherGroup);
        // 財政と貨幣に置く法と報告だけの法は出さない
        Assert.DoesNotContain(t.Rows, r => LawCatalog.In(LawSlot.Econ).Concat(LawCatalog.In(LawSlot.Welfare)).Concat(LawCatalog.In(LawSlot.NoRow)).Any(d => d.Key == r.Key));
        Assert.All(t.Rows, r => Assert.Single(Stages(r)));
    }

    [Fact]
    public void 国有化の群は門が閉じている間は行ごと出ない()
    {
        var (sim, w) = Board();
        w.Coef.NationalizationGate = 0;
        var t = PoliticsTables.Laws.Read(sim, w);
        Assert.DoesNotContain(t.Rows, r => r.Group == PoliticsTables.NationalizationGroup);
    }

    [Fact]
    public void 段の押し釦の可否とホバーは法の行と同じ文字_押すと目標が置かれる()
    {
        var (sim, w) = Board();
        w.Coef.NationalizationGate = 1;
        var t = PoliticsTables.Laws.Read(sim, w);
        foreach (var r in t.Rows)
        {
            var def = LawCatalog.ByField(r.Key)!;
            var st = Stages(r).Single();
            var vm = new LawRowVM(w, def);
            Assert.Equal(def.Stages, st.Labels);
            Assert.Equal(def.LevelBase, st.LevelBase);
            for (int i = 0; i < st.Labels.Count; i++)
            {
                int lv = st.LevelBase + i;
                Assert.Equal(vm.CanReachAndAfford(lv), st.CanSelect(lv));
                Assert.Equal(vm.StageHint(lv), st.Hover(lv));   // 同じ 1 本（StageGate）・空は null＝空の吹き出しを出さない
                Assert.NotEqual("", st.Hover(lv));
            }
            // 押すと AI・議会と同じ口（NationPolicy の目標の欄）に置かれる。確かめたら戻す
            var saved = w.Policy.Clone();
            for (int i = 0; i < st.Labels.Count; i++)
            {
                int lv = st.LevelBase + i;
                st.Select(lv);
                Assert.Equal(lv, def.Target(w));
            }
            w.Policy.CopyFrom(saved);
        }
    }

    [Fact]
    public void 条件の列は法の行の右の条件と同じ()
    {
        var (sim, w) = Board();
        var t = PoliticsTables.Laws.Read(sim, w);
        int costColumn = t.Columns.ToList().FindIndex(c => c.Key == "cost");
        Assert.True(costColumn >= 0);
        foreach (var r in t.Rows)
        {
            var def = LawCatalog.ByField(r.Key)!;
            var vm = new LawRowVM(w, def);
            Assert.Equal(vm.Detail, (string)r.Cells[costColumn]!);
        }
    }

    [Fact]
    public void 制度の表は特許制度_株式会社_鉄道の3行で_状態は発見の2値を読むだけ()
    {
        var (sim, w) = Board();
        var t = PoliticsTables.Institutions.Read(sim, w);
        Assert.Equal(new[] { "特許制度", "株式会社", "鉄道" }, t.Rows.Select(r => r.Key).ToArray());
        Assert.Equal(new[] { w.PatentLaw, w.JointStock, w.Railways }.Select(b => b ? "施行" : "未施行").ToArray(), t.Rows.Select(r => (string)r.Cells[1]!).ToArray());
    }

    [Fact]
    public void 表の動詞に押せない理由のホバーを持たせられ_行はホバーと強調を持てる()
    {
        // 共通の表（国の一覧）が使う追加の口：動詞のホバー・行のホバーと強調の既定は「無し」
        var v = new VerbCell("侵略", false, () => { });
        Assert.Null(v.Tip);
        Assert.Null(new RowData("k", "n", null, []).Tip);
        Assert.False(new RowData("k", "n", null, []).Emphasis);
        var v2 = new VerbCell("侵略", false, () => { }, null, "戦力余剰が必要");
        Assert.Equal("戦力余剰が必要", v2.Tip);
    }
}
