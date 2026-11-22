using PriceOfBread.App.Controls;
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
        w.Coef.GuildLawGate = 1;   // 休眠中の法（出す条件が偽）は行ごと出ない＝門を開けて「その他」の群を確かめる
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
    public void 段の押し釦の可否とホバーは法の行と同じ文字_押すと目標が置かれる()
    {
        var (sim, w) = Board();
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
    public void 法の表は条件の列を持たず_選べない段にだけ短い理由を持つ()
    {
        var (sim, w) = Board();
        var t = PoliticsTables.Laws.Read(sim, w);
        Assert.DoesNotContain(t.Columns, c => c.Key == "cost");
        int stageColumn = t.Columns.ToList().FindIndex(c => c.Key == "stage");
        foreach (var r in t.Rows)
        {
            var st = (StageCell)r.Cells[stageColumn]!;
            Assert.NotNull(st.Why);
            for (int i = 0; i < st.Labels.Count; i++)
            {
                int lv = st.LevelBase + i;
                string? why = st.Why!(lv);
                if (st.CanSelect(lv)) Assert.Null(why);
                else if (why is not null) Assert.DoesNotMatch(@"\d", why);   // 閾値の数字を出さない
                if (why is not null)
                {
                    Assert.DoesNotContain(':', why);   // 日本語の文中は全角のコロン（V11）
                    Assert.DoesNotContain("（", why);   // 括弧の中の説明は出さない（V3）
                }
            }
        }
    }

    [Fact]
    public void 制度は政府の行の名前の列の末尾が持ち_発見の2値と同じ()
    {
        var (sim, w) = Board();
        var t = DiscoveryTables.SocialDiscoveries.Read(sim, w);
        int state = t.Columns.ToList().FindIndex(c => c.Key == "name");   // 制度の施行は名前の列の末尾（列は名前と棒だけ）
        Assert.True(state >= 0);
        var patent = t.Rows.First(r => r.Name == "特許");
        var joint = t.Rows.First(r => r.Name == "株式会社");
        Assert.EndsWith(w.PatentLaw ? "　施行" : "　未施行", (string)patent.Cells[state]!);
        Assert.EndsWith(w.JointStock ? "　施行" : "　未施行", (string)joint.Cells[state]!);
        var rail = t.Rows.First(r => r.Name == "鉄道");   // 鉄道の施行も政府の行の状態の列に出る（V16）
        Assert.EndsWith(w.Railways ? "　施行" : "　未施行", (string)rail.Cells[state]!);
        Assert.DoesNotContain(t.Rows, r => r.Name == "特許制度");   // 名の衝突を作らない
        Assert.DoesNotContain(DiscoveryTables.Discoveries.Read(sim, w).Rows, r => r.Name == "報せの速さ");
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

    [Fact]
    public void 賛否の欄の縦の位置は選んだ行の位置から決まる_欄の中に収まる()
    {
        const double child = 200, host = 600;
        Assert.Equal(0, FollowLayout.Top(null, child, host));            // 選びが無い＝先頭
        Assert.Equal(0, FollowLayout.Top(-50, child, host));             // 表が流れて行が上へ出た＝先頭
        Assert.Equal(0, FollowLayout.Top(double.NaN, child, host));
        Assert.Equal(137, FollowLayout.Top(137, child, host));           // 行の上端にそろう
        Assert.Equal(host - child, FollowLayout.Top(580, child, host));  // 下からはみ出す時は収まる所まで上げる
        Assert.Equal(host - child, FollowLayout.Top(5000, child, host));
        Assert.Equal(0, FollowLayout.Top(300, 900, host));               // 欄より高い中身は先頭
        for (double r = -100; r < 800; r += 37)                          // 行が下へ動けば欄も下へ動く（単調）
            Assert.True(FollowLayout.Top(r + 37, child, host) >= FollowLayout.Top(r, child, host));
    }

    [Fact]
    public void 選んだ法の賛否は段を持つ法ごとに読める()
    {
        var (sim, w) = Board();
        var defs = LawCatalog.In(LawSlot.Law).Where(d => d.IsVisible(w)).ToList();
        Assert.NotEmpty(defs);
        Assert.Contains(defs, d => LawStakes.Split(w, d, true) is not null || LawStakes.Split(w, d, false) is not null);
        var keys = PoliticsTables.Laws.Read(sim, w).Rows.Select(r => r.Key).ToList();
        Assert.All(defs, d => Assert.Contains(d.Key, keys));   // 選べる法は表の行にある＝右の欄はどの行を選んでも出る
    }
}
