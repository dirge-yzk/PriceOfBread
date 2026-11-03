using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>★(501) 段2：画面の配置（中央に出すもの・キー・保存）と、ページの欄の並べの計算の番人。描画そのものは --uishot が見る＝ここは Avalonia に依らない部分だけを見る。</summary>
public class LayoutTests
{
    [Fact]
    public void キーは生産がTab_残りが作者の指示の順_Q市場_W財政_E人口_R政治と法_A外交と戦争_S発見_地図にキーは無い_DFZXCVは空き()
    {
        Assert.Equal("QWERASDFZXCV", PageKeys.Priority);
        var expected = new (Slot Slot, char Key, string Name)[]
        {
            (Slot.Market, 'Q', "市場・清算・貿易"), (Slot.FiscalMoney, 'W', "財政と貨幣"), (Slot.Population, 'E', "人口"),
            (Slot.PoliticsLaw, 'R', "政治と法"), (Slot.DiplomacyWar, 'A', "外交と戦争"), (Slot.Discovery, 'S', "発見"),
        };
        Assert.Equal(8, SlotInfo.All.Count);
        Assert.Equal(7, SlotInfo.Pages.Count);   // 押し釦に並ぶのは 7 ページ（地図は釦もキーも持たない）
        Assert.DoesNotContain(Slot.Map, SlotInfo.Pages);
        foreach (var (slot, key, name) in expected)
        {
            Assert.Equal(key, SlotInfo.Key(slot));
            Assert.Equal(name, SlotInfo.Name(slot));
            Assert.Equal(slot, SlotInfo.OfKey(key));
            Assert.Equal(slot, SlotInfo.OfKey(char.ToLowerInvariant(key)));   // 小文字でも引ける
        }
        Assert.Equal("Tab:生産", SlotInfo.ButtonText(Slot.Production));
        Assert.Equal("生産", SlotInfo.Name(Slot.Production));
        Assert.Equal("地図", SlotInfo.Name(Slot.Map));
        Assert.Null(SlotInfo.OfKey('\0'));   // 生産（Tab）と地図は文字のキーで引けない
        foreach (char free in "DFZXCV") Assert.Null(SlotInfo.OfKey(free));   // 空きのキー（旧の地図の F を含む）は何にも割り当てない
        Assert.Equal("DFZXCV", PageKeys.FreeKeys);
        Assert.Null(SlotInfo.OfKey('1'));
    }

    [Fact]
    public void 初めの配置は地図_左の列は開く_中央に出るのは常に1つ()
    {
        var m = new LayoutModel();
        Assert.Equal(Slot.Map, m.Current);
        Assert.True(m.LeftOpen);
        Assert.Equal([Slot.Map], m.Shown().ToArray());
        Assert.True(m.IsShown(Slot.Map));
        Assert.False(m.IsShown(Slot.Production));
    }

    [Fact]
    public void 開く操作は中央を差し替える_同じ物を開いても変わりとしない()
    {
        var m = new LayoutModel();
        int changed = 0; m.Changed += () => changed++;
        m.Open(Slot.Market);
        Assert.Equal(Slot.Market, m.Current);
        Assert.Equal([Slot.Market], m.Shown().ToArray());
        Assert.Equal(1, changed);
        m.Open(Slot.Market);   // すでに出ている＝変わらない
        Assert.Equal(1, changed);
        m.Open(Slot.Map);
        Assert.Equal(Slot.Map, m.Current);
        Assert.Equal(2, changed);
    }

    [Fact]
    public void ページのキーは出ていなければ出す_もう出ていれば地図へ戻る()
    {
        var m = new LayoutModel();
        m.Toggle(Slot.Market);
        Assert.Equal(Slot.Market, m.Current);
        m.Toggle(Slot.FiscalMoney);   // 別のページのキー＝そのページへ（地図を経由しない）
        Assert.Equal(Slot.FiscalMoney, m.Current);
        m.Toggle(Slot.FiscalMoney);   // 同じキーをもう一度＝地図へ戻る
        Assert.Equal(Slot.Map, m.Current);
    }

    [Fact]
    public void Tabは生産と地図の切り替え_他のページからは生産へ()
    {
        var m = new LayoutModel();
        m.ToggleProductionMap();
        Assert.Equal(Slot.Production, m.Current);
        m.ToggleProductionMap();
        Assert.Equal(Slot.Map, m.Current);
        m.Open(Slot.Discovery);
        m.ToggleProductionMap();   // 地図以外（発見）からは生産へ
        Assert.Equal(Slot.Production, m.Current);
    }

    [Fact]
    public void Escは地図へ戻る_もう地図なら何もしない_戻ったときだけ真()
    {
        var m = new LayoutModel();
        Assert.False(m.LeaveToMap());   // 地図＝Esc はメニューの仕事
        m.Open(Slot.Production);
        Assert.True(m.LeaveToMap());
        Assert.Equal(Slot.Map, m.Current);
        Assert.False(m.LeaveToMap());
    }

    [Fact]
    public void 左の列の開閉は変わったときだけ知らせる()
    {
        var m = new LayoutModel();
        int changed = 0; m.Changed += () => changed++;
        m.SetLeftOpen(true);
        Assert.Equal(0, changed);
        m.SetLeftOpen(false);
        Assert.False(m.LeftOpen);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void 配置の保存は往復する_中央に出すものは保存しない_壊れた保存は初めの配置へ_廃止した分割の保存も読める()
    {
        var m = new LayoutModel();
        m.Open(Slot.Production); m.SetLeftOpen(false);
        var back = LayoutModel.FromJson(m.ToJson());
        Assert.Equal(m.ToJson(), back.ToJson());
        Assert.False(back.LeftOpen);
        Assert.Equal(Slot.Map, back.Current);   // 起動は地図から
        Assert.Equal(new LayoutModel().ToJson(), LayoutModel.FromJson("").ToJson());
        Assert.Equal(new LayoutModel().ToJson(), LayoutModel.FromJson("{壊れた").ToJson());
        Assert.Equal(new LayoutModel().ToJson(), LayoutModel.FromJson("null").ToJson());
        // 分割を廃止する前の保存（形・席・割合を持つ）＝知らない項目は読み捨て・左の列と欄の記録は生きる
        var old = LayoutModel.FromJson("{\"Shape\":\"Four\",\"Seats\":[\"Production\",\"Market\",\"FiscalMoney\",\"Population\"],\"Selected\":2,\"MainRatio\":0.4,\"SubRatio\":0.5,\"LeftOpen\":false,\"RightOpen\":false,\"Sections\":{\"Market\":{\"Opened\":[\"a\"],\"Closed\":[]}}}");
        Assert.False(old.LeftOpen);
        Assert.Equal(Slot.Map, old.Current);
        Assert.Equal(["a"], old.SectionState("Market").Opened);
    }

    [Fact]
    public void 見出しを押して開け閉めした欄は保存される_往復する_知らないページや空の記録は捨てる_初めの配置に戻すと消える()
    {
        var m = new LayoutModel();
        int changed = 0; m.Changed += () => changed++;
        m.SetSectionState("Production", ["goods", "goods", "equipment"], ["supply"]);
        Assert.Equal(1, changed);
        m.SetSectionState("Production", ["equipment", "goods"], ["supply"]);   // 同じ内容（順が違うだけ）は変わりとしない
        Assert.Equal(1, changed);
        var (opened, closed) = m.SectionState("Production");
        Assert.Equal(["equipment", "goods"], opened);
        Assert.Equal(["supply"], closed);
        var back = LayoutModel.FromJson(m.ToJson());
        Assert.Equal(m.ToJson(), back.ToJson());
        Assert.Equal(["equipment", "goods"], back.SectionState("Production").Opened);
        Assert.Empty(back.SectionState("Market").Opened);
        // 知らないページの記録・空の記録は読まない
        var junk = LayoutModel.FromJson("{\"Sections\":{\"Nope\":{\"Opened\":[\"a\"],\"Closed\":[]},\"Market\":{\"Opened\":[],\"Closed\":[]}}}");
        Assert.Empty(junk.SectionState("Market").Opened);
        Assert.Empty(junk.SectionState("Market").Closed);
        Assert.DoesNotContain("Nope", junk.ToJson());
        m.SetSectionState("Production", [], []);   // 全部戻すと記録ごと消える
        Assert.DoesNotContain("Sections", m.ToJson());
        m.SetSectionState("Market", [], ["imports"]);
        m.Reset();
        Assert.Empty(m.SectionState("Market").Closed);
    }

    [Fact]
    public void 保存先は上書きできる_撮影と計測は本物の配置を読まず書かない()
    {
        string dir = Path.Combine(Path.GetTempPath(), "pob_layout_test_" + Guid.NewGuid().ToString("N"));
        string old = LayoutStore.PathOverride ?? "";
        try
        {
            LayoutStore.PathOverride = Path.Combine(dir, "配置.json");
            Assert.Equal(new LayoutModel().ToJson(), LayoutStore.Load().ToJson());   // 無ければ初めの配置
            var m = new LayoutModel(); m.SetLeftOpen(false);
            LayoutStore.Save(m);
            Assert.Equal(m.ToJson(), LayoutStore.Load().ToJson());
        }
        finally { LayoutStore.PathOverride = string.IsNullOrEmpty(old) ? null : old; try { Directory.Delete(dir, true); } catch { } }
    }

    // ---- ページの欄の並べ ----

    private static PageTable Sample() => new(
    [
        new SectionSpec("a", "甲", 1, 8), new SectionSpec("b", "乙", 2, 6), new SectionSpec("c", "丙", 3, 6),
        new SectionSpec("d", "丁", 4, 6), new SectionSpec("e", "戊", 5, 6),
    ], [["a"], ["b", "c"], ["d", "e"]]);

    [Fact]
    public void 幅で列の数が決まる_1200以上は3_700以上は2_それ未満は1()
    {
        Assert.Equal(3, PagePlan.ColumnCount(1200)); Assert.Equal(3, PagePlan.ColumnCount(1920));
        Assert.Equal(2, PagePlan.ColumnCount(1199)); Assert.Equal(2, PagePlan.ColumnCount(700));
        Assert.Equal(1, PagePlan.ColumnCount(699)); Assert.Equal(1, PagePlan.ColumnCount(0));
    }

    [Fact]
    public void 欄の置き場_3列は表のとおり_2列は左から順に積んで最小の高さの半分で割る_1列は全部()
    {
        var t = Sample();
        var c3 = PagePlan.Arrange(t, 3);
        Assert.Equal(new[] { "a", "b,c", "d,e" }, c3.Select(c => string.Join(",", c.Select(s => s.Key))).ToArray());
        var c2 = PagePlan.Arrange(t, 2);
        Assert.Equal(2, c2.Count);
        Assert.Equal("abcde", string.Concat(c2.SelectMany(c => c).Select(s => s.Key)));   // 順序を保つ
        Assert.All(c2, c => Assert.NotEmpty(c));
        var c1 = PagePlan.Arrange(t, 1);
        Assert.Single(c1);
        Assert.Equal("abcde", string.Concat(c1[0].Select(s => s.Key)));
        // 宣言の間違いは落とす
        Assert.Throws<ArgumentException>(() => PagePlan.Arrange(new PageTable([new SectionSpec("a", "甲", 1)], [["x"]]), 3));
        Assert.Throws<ArgumentException>(() => PagePlan.Arrange(new PageTable([new SectionSpec("a", "甲", 1), new SectionSpec("a", "乙", 2)], [["a"]]), 3));
        Assert.Throws<ArgumentException>(() => PagePlan.Arrange(new PageTable([new SectionSpec("a", "甲", 1)], [["a"], ["a"]]), 3));
    }

    [Fact]
    public void 高さが足りなければ順位の数が大きい欄から見出しの1行へ畳む_利用者が開いた欄は最後_余りは重みで分ける()
    {
        var col = new[] { new SectionSpec("a", "甲", 1, 6), new SectionSpec("b", "乙", 2, 6), new SectionSpec("c", "丙", 3, 6), new SectionSpec("d", "丁", 4, 6) };
        double min = PagePlan.MinHeight(col[0]);
        double all = 4 * min + 3 * PagePlan.SectionGap;
        // 全部入る
        var p = PagePlan.PlanColumn(col, all + 100);
        Assert.All(p, x => Assert.False(x.Collapsed));
        Assert.Equal(all + 100 - 3 * PagePlan.SectionGap, p.Sum(x => x.Height), 6);   // 余りは使い切る（欄の間の隙間を除いて）
        // 1 つ足りない → 順位が最も低い（数が最大）の丁が畳まれる
        p = PagePlan.PlanColumn(col, all - 1);
        Assert.Equal(new[] { false, false, false, true }, p.Select(x => x.Collapsed).ToArray());
        Assert.Equal(PagePlan.HeaderHeight, p[3].Height);
        // 利用者が丁を開いた → 代わりに丙が畳まれる
        p = PagePlan.PlanColumn(col, all - 1, new HashSet<string> { "d" });
        Assert.Equal(new[] { false, false, true, false }, p.Select(x => x.Collapsed).ToArray());
        // 利用者が畳んだ欄は、高さが十分でも畳まれたまま
        p = PagePlan.PlanColumn(col, all + 100, null, new HashSet<string> { "b" });
        Assert.True(p[1].Collapsed);
        // うんと狭い → 最後まで残るのは順位 1
        p = PagePlan.PlanColumn(col, min + 30);
        Assert.False(p[0].Collapsed); Assert.All(p.Skip(1), x => Assert.True(x.Collapsed));
        // 重み
        var w = new[] { new SectionSpec("a", "甲", 1, 6, 1), new SectionSpec("b", "乙", 2, 6, 3) };
        var pw = PagePlan.PlanColumn(w, 2 * min + PagePlan.SectionGap + 400);
        Assert.Equal(100, pw[0].Height - min, 6); Assert.Equal(300, pw[1].Height - min, 6);
        // 無限の高さ＝畳まず最小の高さのまま
        var inf = PagePlan.PlanColumn(col, double.PositiveInfinity);
        Assert.All(inf, x => { Assert.False(x.Collapsed); Assert.Equal(min, x.Height, 6); });
    }

    [Fact]
    public void ページ全体の割り当て_どの幅と高さでも欄が高さを超えない_欄は全て出てくる()
    {
        var t = Sample();
        foreach (double width in new[] { 400.0, 700, 1000, 1200, 1900 })
            foreach (double height in new[] { 120.0, 300, 505, 1010 })
            {
                var plan = PagePlan.Plan(t, width, height);
                Assert.Equal(PagePlan.ColumnCount(width), plan.Count);
                Assert.Equal(new[] { "a", "b", "c", "d", "e" }, plan.SelectMany(c => c).Select(s => s.Key).OrderBy(k => k).ToArray());
                foreach (var col in plan)
                {
                    double used = col.Sum(s => s.Height) + (col.Count - 1) * PagePlan.SectionGap;
                    double floor = col.Count * PagePlan.HeaderHeight + (col.Count - 1) * PagePlan.SectionGap;   // 見出しだけでも入らない高さは仕方がない
                    Assert.True(used <= Math.Max(height, floor) + 1e-6, $"{width}×{height}: 列が高さを超える {used}");
                }
            }
    }
}
