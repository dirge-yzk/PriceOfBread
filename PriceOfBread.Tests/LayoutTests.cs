using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>★(501) 段2：画面の配置（分割の形・枠のページ・キー・保存）と、ページの欄の並べの計算の番人。描画そのものは --uishot が見る＝ここは Avalonia に依らない部分だけを見る。</summary>
public class LayoutTests
{
    [Fact]
    public void キーは作者の指示の順_Q生産_W市場_E財政_R人口_A政治と法_S外交と戦争_D発見_F地図_ZXCVは空き()
    {
        Assert.Equal("QWERASDFZXCV", PageKeys.Priority);
        var expected = new (Slot Slot, char Key, string Name)[]
        {
            (Slot.Production, 'Q', "生産"), (Slot.Market, 'W', "市場・清算・貿易"), (Slot.FiscalMoney, 'E', "財政と貨幣"), (Slot.Population, 'R', "人口"),
            (Slot.PoliticsLaw, 'A', "政治と法"), (Slot.DiplomacyWar, 'S', "外交と戦争"), (Slot.Discovery, 'D', "発見"), (Slot.Map, 'F', "地図"),
        };
        Assert.Equal(expected.Length, SlotInfo.All.Count);
        foreach (var (slot, key, name) in expected)
        {
            Assert.Equal(key, SlotInfo.Key(slot));
            Assert.Equal(name, SlotInfo.Name(slot));
            Assert.Equal(slot, SlotInfo.OfKey(key));
            Assert.Equal(slot, SlotInfo.OfKey(char.ToLowerInvariant(key)));   // 小文字でも引ける
        }
        foreach (char free in "ZXCV") Assert.Null(SlotInfo.OfKey(free));      // 空きのキーは何にも割り当てない
        Assert.Equal("ZXCV", PageKeys.FreeKeys);
        Assert.Null(SlotInfo.OfKey('1'));
    }

    [Fact]
    public void 分割の形ごとの枠の数と置き場所_枠は格子の中で重ならず_境目は枠の間を通る()
    {
        Assert.Equal(1, SlotInfo.PaneCount(SplitShape.One)); Assert.Equal(2, SlotInfo.PaneCount(SplitShape.LeftRight)); Assert.Equal(2, SlotInfo.PaneCount(SplitShape.TopBottom));
        Assert.Equal(3, SlotInfo.PaneCount(SplitShape.LeftAndRightStack)); Assert.Equal(4, SlotInfo.PaneCount(SplitShape.Four));
        foreach (var shape in Enum.GetValues<SplitShape>())
        {
            var p = SlotInfo.Place(shape);
            Assert.Equal(SlotInfo.PaneCount(shape), p.Panes.Length);
            // 3×3 の格子の各マスを占める枠は高々 1 つ
            var taken = new HashSet<(int, int)>();
            foreach (var c in p.Panes)
                for (int r = c.Row; r < c.Row + c.RowSpan; r++)
                    for (int col = c.Col; col < c.Col + c.ColSpan; col++)
                        Assert.True(taken.Add((r, col)), $"{shape}: 枠が重なる ({r},{col})");
            foreach (var c in p.Panes) { Assert.InRange(c.Row + c.RowSpan, 1, 3); Assert.InRange(c.Col + c.ColSpan, 1, 3); }
            // 境目のマスは枠のどれにも占められない（枠が境目にかぶらない）か、枠が境目の列・行をまたぐだけ（1 枚・上下の枠が縦の境目を、左右の枠が横の境目をまたぐ）
            if (p.VerticalSplitter is { } v) Assert.Equal(1, v.Col);
            if (p.HorizontalSplitter is { } h) Assert.Equal(1, h.Row);
        }
        Assert.Null(SlotInfo.Place(SplitShape.One).VerticalSplitter);
        Assert.NotNull(SlotInfo.Place(SplitShape.Four).VerticalSplitter);
        Assert.NotNull(SlotInfo.Place(SplitShape.Four).HorizontalSplitter);
    }

    [Fact]
    public void 初めの配置は左右に2つ_生産と市場_左右の列は開く()
    {
        var m = new LayoutModel();
        Assert.Equal(SplitShape.LeftRight, m.Shape);
        Assert.Equal(2, m.PaneCount);
        Assert.Equal(Slot.Production, m.SlotAt(0)); Assert.Equal(Slot.Market, m.SlotAt(1));
        Assert.True(m.LeftOpen && m.RightOpen);
        Assert.Equal(0, m.Selected);
        Assert.True(m.MainRatio > 0.5);   // 生産が広め
    }

    private static void AssertSeatsDistinct(LayoutModel m) => Assert.Equal(4, m.Seats.Distinct().Count());

    [Fact]
    public void 開く操作は4つの席を常に違う物に保つ_別の席にあれば入れ替え_どこにも無ければ置き換え()
    {
        var m = new LayoutModel();
        int changed = 0; m.Changed += () => changed++;
        m.Open(Slot.Population, 0);          // 席 3 にある（見えていない）→ 入れ替え
        Assert.Equal(Slot.Population, m.SlotAt(0)); Assert.Equal(Slot.Production, m.Seats[3]);
        AssertSeatsDistinct(m);
        m.Open(Slot.Map, 1);                 // どの席にも無い → 置き換え（市場は席から外れる）
        Assert.Equal(Slot.Map, m.SlotAt(1)); Assert.DoesNotContain(Slot.Market, m.Seats);
        AssertSeatsDistinct(m);
        m.Open(Slot.Population, 1);          // 見えている別の枠にある → 入れ替え
        Assert.Equal(Slot.Population, m.SlotAt(1)); Assert.Equal(Slot.Map, m.SlotAt(0));
        AssertSeatsDistinct(m);
        Assert.Equal(1, m.Selected);         // 開いた枠が選んでいる枠になる
        Assert.Equal(3, changed);
        // 同じ物をその枠へ開き直しても席は変わらない
        var before = m.Seats.ToArray();
        m.Open(Slot.Population, 1);
        Assert.Equal(before, m.Seats.ToArray());
        // 全ての組み合わせの操作の後も 4 つは違う（総当たり）
        foreach (var s in SlotInfo.All)
            for (int pane = 0; pane < 4; pane++)
            {
                var x = new LayoutModel(); x.SetShape(SplitShape.Four);
                x.Open(s, pane);
                AssertSeatsDistinct(x);
                Assert.Equal(s, x.SlotAt(pane));
            }
    }

    [Fact]
    public void 選んでいる枠へ開く_Shiftは次の枠へ開く_枠が1つなら同じ枠()
    {
        var m = new LayoutModel();
        m.OpenInSelected(Slot.FiscalMoney);
        Assert.Equal(Slot.FiscalMoney, m.SlotAt(0)); Assert.Equal(0, m.Selected);
        m.OpenInNext(Slot.Discovery);
        Assert.Equal(Slot.Discovery, m.SlotAt(1)); Assert.Equal(1, m.Selected);
        m.OpenInNext(Slot.Map);              // 次は先頭へ戻る
        Assert.Equal(Slot.Map, m.SlotAt(0)); Assert.Equal(0, m.Selected);
        m.SetShape(SplitShape.One);
        m.OpenInNext(Slot.Market);
        Assert.Equal(Slot.Market, m.SlotAt(0)); Assert.Equal(0, m.Selected);
        Assert.Equal(1, m.PaneCount);
        Assert.True(m.IsShown(Slot.Market)); Assert.False(m.IsShown(Slot.Map));
    }

    [Fact]
    public void 生産の全面_1枚に置き左右の列を畳む_もう一度で前の配置へ戻る_別のページを開いても戻る_保存は前の配置()
    {
        var m = new LayoutModel();
        m.SetShape(SplitShape.Four); m.OpenInSelected(Slot.Discovery); m.Select(2); m.SetRatios(0.4, 0.3);
        string before = m.ToJson();
        var seats = m.Seats.ToArray(); int sel = m.Selected;
        m.ToggleFull(Slot.Production);
        Assert.True(m.IsFull); Assert.Equal(Slot.Production, m.FullSlot);
        Assert.Equal(SplitShape.One, m.Shape); Assert.Equal(0, m.Selected); Assert.Equal(Slot.Production, m.SlotAt(0));
        Assert.False(m.LeftOpen); Assert.False(m.RightOpen);
        Assert.Equal(before, m.ToJson());              // 保存するのは入る前の配置（全面のまま落ちても次は元の配置）
        m.ToggleFull(Slot.Production);                 // もう一度で戻る
        Assert.False(m.IsFull);
        Assert.Equal(before, m.ToJson());
        Assert.Equal(seats, m.Seats.ToArray()); Assert.Equal(sel, m.Selected); Assert.Equal(SplitShape.Four, m.Shape);
        Assert.True(m.LeftOpen && m.RightOpen);
        // 別のページを開く（キーでも見出しからでも）＝前の配置へ戻ってから開く
        m.ToggleFull(Slot.Production);
        m.OpenInSelected(Slot.Market);
        Assert.False(m.IsFull); Assert.Equal(SplitShape.Four, m.Shape); Assert.Equal(Slot.Market, m.SlotAt(sel));
        // Esc（LeaveFull）・形の選び直し・列の開け閉め
        m.ToggleFull(Slot.Production); Assert.True(m.LeaveFull()); Assert.False(m.IsFull); Assert.False(m.LeaveFull());
        m.ToggleFull(Slot.Production); m.SetShape(SplitShape.LeftRight); Assert.False(m.IsFull); Assert.Equal(SplitShape.LeftRight, m.Shape);
        m.ToggleFull(Slot.Production); m.SetColumns(true, false);
        Assert.False(m.IsFull); Assert.Equal(SplitShape.One, m.Shape); Assert.True(m.LeftOpen);   // 列を自分で開けたら全面の印だけ外す（今の配置のまま）
        // 全面に入るページが席に無くても入れる・戻ると席は元のまま
        var m2 = new LayoutModel(); m2.OpenInSelected(Slot.Discovery); m2.OpenInSelected(Slot.Map);
        Assert.DoesNotContain(Slot.Production, m2.Seats);
        var seats2 = m2.Seats.ToArray();
        m2.ToggleFull(Slot.Production); Assert.Equal(Slot.Production, m2.SlotAt(0)); Assert.Equal(4, m2.Seats.Distinct().Count());
        m2.LeaveFull(); Assert.Equal(seats2, m2.Seats.ToArray());
        m2.ToggleFull(Slot.Production); m2.Reset(); Assert.False(m2.IsFull);
    }

    [Fact]
    public void 形を小さくしても席の中身は残る_選んでいる枠は範囲に収まる_見えているものだけが見えている()
    {
        var m = new LayoutModel();
        m.SetShape(SplitShape.Four);
        m.Select(3);
        var seats = m.Seats.ToArray();
        m.SetShape(SplitShape.LeftRight);
        Assert.Equal(1, m.Selected);         // 範囲に収まる
        Assert.Equal(seats, m.Seats.ToArray());
        Assert.False(m.IsShown(seats[3]));
        m.SetShape(SplitShape.Four);
        Assert.True(m.IsShown(seats[3]));    // 戻すと現れる
        Assert.Equal(seats.ToHashSet(), m.Shown().ToHashSet());
    }

    [Fact]
    public void 境目の割合は端まで動かせない_非有限は既定へ()
    {
        var m = new LayoutModel();
        m.SetRatios(0.0, 1.0);
        Assert.InRange(m.MainRatio, 0.15, 0.85); Assert.InRange(m.SubRatio, 0.15, 0.85);
        m.SetRatios(double.NaN, double.PositiveInfinity);
        Assert.Equal(LayoutModel.DefaultMainRatio, m.MainRatio); Assert.InRange(m.SubRatio, 0.15, 0.85);
    }

    [Fact]
    public void 配置の保存は往復する_壊れた保存は初めの配置へ_席の重なりや欠けを捨てる()
    {
        var m = new LayoutModel();
        m.SetShape(SplitShape.LeftAndRightStack); m.Open(Slot.Map, 1); m.Open(Slot.Discovery, 2); m.SetRatios(0.4, 0.3); m.SetColumns(false, true);
        var json = m.ToJson();
        var back = LayoutModel.FromJson(json);
        Assert.Equal(json, back.ToJson());
        Assert.Equal(m.Shape, back.Shape); Assert.Equal(m.Seats.ToArray(), back.Seats.ToArray());
        Assert.Equal(m.Selected, back.Selected); Assert.False(back.LeftOpen); Assert.True(back.RightOpen);
        var init = new LayoutModel().ToJson();
        Assert.Equal(init, LayoutModel.FromJson(null).ToJson());
        Assert.Equal(init, LayoutModel.FromJson("").ToJson());
        Assert.Equal(init, LayoutModel.FromJson("{壊れ").ToJson());
        Assert.Equal(init, LayoutModel.FromJson("{\"Shape\":\"Nope\",\"Seats\":[]}").ToJson());
        Assert.Equal(init, LayoutModel.FromJson("{\"Shape\":\"One\",\"Seats\":[\"Map\",\"Map\",\"Market\",\"Population\"]}").ToJson());   // 席が重なる
        Assert.Equal(init, LayoutModel.FromJson("{\"Shape\":\"One\",\"Seats\":[\"Map\",\"Market\"]}").ToJson());                          // 席が欠ける
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
        var junk = LayoutModel.FromJson("{\"Shape\":\"LeftRight\",\"Seats\":[\"Production\",\"Market\",\"FiscalMoney\",\"Population\"],\"Sections\":{\"Nope\":{\"Opened\":[\"a\"],\"Closed\":[]},\"Market\":{\"Opened\":[],\"Closed\":[]}}}");
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
            var m = new LayoutModel(); m.SetShape(SplitShape.Four); m.Open(Slot.Map, 2);
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
