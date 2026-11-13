using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.Tests;

/// <summary>★(501) 段2：画面の配置（背景・浮かぶ窓・キー・保存）と、ページの欄の並べの計算の番人。描画そのものは --uishot が見る＝ここは Avalonia に依らない部分だけを見る。</summary>
public class LayoutTests
{
    [Fact]
    public void キーは生産がTab_残りが作者の指示の順_Q市場_W財政_E人口_R政治と法_A外交と戦争_S発見_地図にキーは無い_DFZXCVは空き()
    {
        Assert.Equal("QWERASDFZXCV", PageKeys.Priority);
        var expected = new (Slot Slot, char Key, string Name)[]
        {
            (Slot.Market, 'Q', "市場と貿易"), (Slot.FiscalMoney, 'W', "財政と貨幣"), (Slot.Population, 'E', "POP"),
            (Slot.PoliticsLaw, 'R', "政治と法"), (Slot.DiplomacyWar, 'A', "外交と戦争"), (Slot.Discovery, 'S', "発見"),
        };
        Assert.Equal(9, SlotInfo.All.Count);
        Assert.Equal(7, SlotInfo.Pages.Count);   // 押し釦に並ぶのは 7 ページ（地図は釦もキーも持たない）
        Assert.DoesNotContain(Slot.Map, SlotInfo.Pages);
        Assert.DoesNotContain(Slot.Province, SlotInfo.Pages);   // 州の窓は釦もキーも持たない
        Assert.Equal("州", SlotInfo.Name(Slot.Province));
        Assert.True(LayoutModel.IsWindow(Slot.Province));
        Assert.Equal(460, LayoutModel.ProvinceRect(1600, 900).W);   // 幅は領域の大きさに依らず固定
        foreach (var (slot, key, name) in expected)
        {
            Assert.Equal(key, SlotInfo.Key(slot));
            Assert.Equal(name, SlotInfo.Name(slot));
            Assert.Equal(slot, SlotInfo.OfKey(key));
            Assert.Equal(slot, SlotInfo.OfKey(char.ToLowerInvariant(key)));   // 小文字でも引ける
        }
        Assert.Equal("Tab：生産", SlotInfo.ButtonText(Slot.Production));
        Assert.Equal("生産", SlotInfo.Name(Slot.Production));
        Assert.Equal("地図", SlotInfo.Name(Slot.Map));
        Assert.Null(SlotInfo.OfKey('\0'));   // 生産（Tab）と地図は文字のキーで引けない
        foreach (char free in "DFZXCV") Assert.Null(SlotInfo.OfKey(free));   // 空きのキー（旧の地図の F を含む）は何にも割り当てない
        Assert.Equal("DFZXCV", PageKeys.FreeKeys);
        Assert.Null(SlotInfo.OfKey('1'));
    }

    [Fact]
    public void 初めの配置は地図の背景_窓は無い_左の列は開く()
    {
        var m = new LayoutModel();
        Assert.Equal(Slot.Map, m.Background);
        Assert.Empty(m.OpenWindows);
        Assert.Null(m.Front);
        Assert.True(m.LeftOpen);
        Assert.Equal([Slot.Map], m.Shown().ToArray());
        Assert.True(m.IsShown(Slot.Map));
        Assert.False(m.IsShown(Slot.Production));
        Assert.False(m.IsShown(Slot.Market));
    }

    [Fact]
    public void 地図と生産は背景_それ以外のページは窓()
    {
        Assert.False(LayoutModel.IsWindow(Slot.Map));
        Assert.False(LayoutModel.IsWindow(Slot.Production));
        foreach (var s in new[] { Slot.Market, Slot.FiscalMoney, Slot.Population, Slot.PoliticsLaw, Slot.DiplomacyWar, Slot.Discovery })
            Assert.True(LayoutModel.IsWindow(s), s.ToString());
        var m = new LayoutModel();
        m.Open(Slot.Production);   // 背景が替わるだけ（窓は増えない）
        Assert.Equal(Slot.Production, m.Background);
        Assert.Empty(m.OpenWindows);
        Assert.False(m.IsShown(Slot.Map));
    }

    [Fact]
    public void 窓は複数を同時に開ける_開いた順に重なる_開き直すと前へ_同じ物を開いても変わりとしない()
    {
        var m = new LayoutModel();
        int changed = 0; m.Changed += () => changed++;
        m.Open(Slot.Market); m.Open(Slot.FiscalMoney); m.Open(Slot.Population);
        Assert.Equal([Slot.Market, Slot.FiscalMoney, Slot.Population], m.OpenWindows.ToArray());
        Assert.Equal(Slot.Population, m.Front);
        Assert.Equal(3, changed);
        Assert.Equal([Slot.Map, Slot.Market, Slot.FiscalMoney, Slot.Population], m.Shown().ToArray());
        m.Open(Slot.Population);   // すでに一番前
        Assert.Equal(3, changed);
        m.Open(Slot.Market);       // 開いている窓を開き直す＝前へ（増えない）
        Assert.Equal([Slot.FiscalMoney, Slot.Population, Slot.Market], m.OpenWindows.ToArray());
        Assert.Equal(4, changed);
    }

    [Fact]
    public void ページのキーは窓の開閉の切り替え_閉じても位置は覚えている()
    {
        var m = new LayoutModel();
        m.Toggle(Slot.Market);
        Assert.True(m.IsShown(Slot.Market));
        m.SetRect(Slot.Market, new WindowRect(100, 80, 600, 400));
        m.Toggle(Slot.Market);   // 同じキー＝閉じる
        Assert.False(m.IsShown(Slot.Market));
        Assert.Empty(m.OpenWindows);
        Assert.Equal(new WindowRect(100, 80, 600, 400), m.RectOf(Slot.Market));
        m.Toggle(Slot.Market);
        Assert.True(m.IsShown(Slot.Market));
        Assert.Equal(new WindowRect(100, 80, 600, 400), m.RectOf(Slot.Market));   // 開き直しても同じ位置
        m.Close(Slot.FiscalMoney);   // 開いていない窓を閉じても何も起きない
    }

    [Fact]
    public void 押した窓が前に出る_すでに前なら変わりとしない_開いていない窓は無視()
    {
        var m = new LayoutModel();
        m.Open(Slot.Market); m.Open(Slot.FiscalMoney);
        int changed = 0; m.Changed += () => changed++;
        m.BringToFront(Slot.Market);
        Assert.Equal([Slot.FiscalMoney, Slot.Market], m.OpenWindows.ToArray());
        Assert.Equal(1, changed);
        m.BringToFront(Slot.Market);
        m.BringToFront(Slot.Discovery);   // 開いていない
        Assert.Equal(1, changed);
        Assert.Equal([Slot.FiscalMoney, Slot.Market], m.OpenWindows.ToArray());
    }

    [Fact]
    public void Tabは背景の地図と生産の切り替え_窓には触れない()
    {
        var m = new LayoutModel();
        m.Open(Slot.Market);
        m.ToggleProductionMap();
        Assert.Equal(Slot.Production, m.Background);
        Assert.Equal([Slot.Market], m.OpenWindows.ToArray());
        m.ToggleProductionMap();
        Assert.Equal(Slot.Map, m.Background);
        Assert.Equal([Slot.Market], m.OpenWindows.ToArray());
    }

    [Fact]
    public void Escは一番前の窓を閉じる_窓が無ければ生産の背景から地図へ_地図ならメニューの仕事で偽()
    {
        var m = new LayoutModel();
        Assert.False(m.Escape());   // 地図・窓なし＝Esc はメニューの仕事
        m.Open(Slot.Production); m.Open(Slot.Market); m.Open(Slot.Population);
        Assert.True(m.Escape());    // 一番前（人口）が閉じる
        Assert.Equal([Slot.Market], m.OpenWindows.ToArray());
        Assert.Equal(Slot.Production, m.Background);
        Assert.True(m.Escape());    // 市場が閉じる
        Assert.Empty(m.OpenWindows);
        Assert.Equal(Slot.Production, m.Background);
        Assert.True(m.Escape());    // 窓が無い＝生産から地図へ
        Assert.Equal(Slot.Map, m.Background);
        Assert.False(m.Escape());
    }

    [Fact]
    public void 窓の位置と大きさ_最小の大きさへ丸める_非有限は捨てる_変わらなければ知らせない_背景には置けない()
    {
        var m = new LayoutModel();
        int changed = 0; m.Changed += () => changed++;
        m.SetRect(Slot.Market, new WindowRect(10, 20, 100, 50));
        Assert.Equal(new WindowRect(10, 20, LayoutModel.MinWindowWidth, LayoutModel.MinWindowHeight), m.RectOf(Slot.Market));
        Assert.Equal(1, changed);
        m.SetRect(Slot.Market, new WindowRect(10, 20, 100, 50));   // 同じ（丸めた後も同じ）
        Assert.Equal(1, changed);
        m.SetRect(Slot.Market, new WindowRect(double.NaN, 0, 500, 400));
        m.SetRect(Slot.Market, new WindowRect(0, double.PositiveInfinity, 500, 400));
        m.SetRect(Slot.Map, new WindowRect(0, 0, 500, 400));
        m.SetRect(Slot.Production, new WindowRect(0, 0, 500, 400));
        Assert.Equal(1, changed);
        Assert.Null(m.RectOf(Slot.Map));
        Assert.Null(m.RectOf(Slot.Production));
    }

    [Fact]
    public void 初めの位置_高さの空きが足りなければ縮めて置き_既に開いている窓を覆わない()
    {
        // 幅 540 の 3 枚が横に並び、3 枚目だけ高さ 600（下に 430 残る）＝4 枚目（高さ 900 を望む）は 3 枚目の下へ、空きの高さまで縮めて入る
        var placed = new List<WindowRect> { new(0, 0, 540, 1030), new(544, 0, 540, 1030), new(1088, 0, 540, 600) };
        var fourth = LayoutModel.AutoPlace(Slot.PoliticsLaw, 540, 900, placed, 1660, 1030);
        Assert.Equal(1088, fourth.X);
        Assert.Equal(604, fourth.Y);
        Assert.Equal(426, fourth.H);
        Assert.All(placed, o => Assert.False(fourth.X < o.X + o.W && o.X < fourth.X + fourth.W && fourth.Y < o.Y + o.H && o.Y < fourth.Y + fourth.H));
    }

    [Fact]
    public void 初めの位置と収め直し_空き場所へ並ぶ_空きが無ければ重ねる_領域の外へ出ない_覚えている値は書き換えない()
    {
        // 空き場所：左から右・上から下。幅 540 の窓は 1660 の領域に 3 枚が横に並ぶ
        var placed = new List<WindowRect>();
        foreach (var s in new[] { Slot.Market, Slot.FiscalMoney, Slot.Population })
        {
            var r = LayoutModel.AutoPlace(s, 540, 600, placed, 1660, 1030);
            Assert.All(placed, o => Assert.False(r.X < o.X + o.W && o.X < r.X + r.W && r.Y < o.Y + o.H && o.Y < r.Y + r.H, $"{s} が重なった"));
            Assert.Equal(0, r.Y);
            placed.Add(r);
        }
        Assert.True(placed[0].X < placed[1].X && placed[1].X < placed[2].X);
        // 4 枚目は横に入らないので、下の空き（高さ 600 のあと 430 残る）へ
        var fourth = LayoutModel.AutoPlace(Slot.PoliticsLaw, 540, 400, placed, 1660, 1030);
        Assert.Equal(0, fourth.X);
        Assert.True(fourth.Y >= 600);
        // 空きが無ければ重ねる（領域の中に収まる）
        var full = LayoutModel.AutoPlace(Slot.DiplomacyWar, 900, 1030, placed, 1660, 1030);
        Assert.True(full.X >= 0 && full.Y >= 0 && full.X + full.W <= 1660 && full.Y + full.H <= 1030);
        // 領域より大きい窓は領域まで
        var big = LayoutModel.AutoPlace(Slot.Market, 3000, 3000, [], 1200, 800);
        Assert.Equal(new WindowRect(0, 0, 1200, 800), big);
        foreach (var s in new[] { Slot.Market, Slot.FiscalMoney, Slot.Population, Slot.PoliticsLaw, Slot.DiplomacyWar, Slot.Discovery })
            Assert.True(LayoutModel.DefaultWidth(s) >= LayoutModel.MinWindowWidth, s.ToString());
        // 置き場は開いている間だけ・保存しない・閉じると消える
        var am = new LayoutModel(); am.Open(Slot.Market);
        am.SetAuto(Slot.Market, new WindowRect(1, 2, 500, 400));
        Assert.Equal(new WindowRect(1, 2, 500, 400), am.AutoRectOf(Slot.Market));
        Assert.Null(am.RectOf(Slot.Market));
        Assert.Null(LayoutModel.FromJson(am.ToJson()).RectOf(Slot.Market));
        am.Close(Slot.Market);
        Assert.Null(am.AutoRectOf(Slot.Market));
        // 収め直し：大きすぎる大きさは領域まで・外へ出た位置は内側へ
        var f = LayoutModel.Fit(new WindowRect(-50, 2000, 3000, 2000), 1200, 800);
        Assert.Equal(new WindowRect(0, 0, 1200, 800), f);
        var g = LayoutModel.Fit(new WindowRect(1100, 700, 500, 300), 1200, 800);
        Assert.Equal(new WindowRect(700, 500, 500, 300), g);
        // 覚えている値は表示のためには書き換えない
        var m = new LayoutModel(); m.SetRect(Slot.Market, new WindowRect(5000, 5000, 600, 400));
        _ = LayoutModel.Fit(m.RectOf(Slot.Market)!.Value, 1200, 800);
        Assert.Equal(new WindowRect(5000, 5000, 600, 400), m.RectOf(Slot.Market));
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
    public void 配置の保存は往復する_窓の位置と左の列は覚える_背景と開いた窓は覚えない_壊れた保存は初めの配置へ_廃止した分割の保存も読める()
    {
        var m = new LayoutModel();
        m.Open(Slot.Production); m.Open(Slot.Market); m.SetLeftOpen(false);
        m.SetRect(Slot.Market, new WindowRect(120, 60, 700, 500));
        var back = LayoutModel.FromJson(m.ToJson());
        Assert.Equal(m.ToJson(), back.ToJson());
        Assert.False(back.LeftOpen);
        Assert.Equal(new WindowRect(120, 60, 700, 500), back.RectOf(Slot.Market));
        Assert.Equal(Slot.Map, back.Background);   // 起動は地図から
        Assert.Empty(back.OpenWindows);            // 窓は開かない
        Assert.Equal(new LayoutModel().ToJson(), LayoutModel.FromJson("").ToJson());
        Assert.Equal(new LayoutModel().ToJson(), LayoutModel.FromJson("{壊れた").ToJson());
        Assert.Equal(new LayoutModel().ToJson(), LayoutModel.FromJson("null").ToJson());
        // 壊れた窓の記録は捨てる（知らない窓・背景の窓・要素の数が違う・非有限）
        var junk = LayoutModel.FromJson("{\"Version\":2,\"Windows\":{\"Nope\":[1,2,3,4],\"Map\":[1,2,500,400],\"Production\":[1,2,500,400],\"Market\":[1,2,3],\"Population\":[1,2,500,400]}}");
        Assert.Equal(new WindowRect(1, 2, 500, 400), junk.RectOf(Slot.Population));
        Assert.Null(junk.RectOf(Slot.Market));
        Assert.Null(junk.RectOf(Slot.Map));
        Assert.Null(junk.RectOf(Slot.Production));
        // 分割を廃止する前の保存（形・席・割合を持つ）＝知らない項目は読み捨て・左の列の記録は生きる
        var old = LayoutModel.FromJson("{\"Shape\":\"Four\",\"Seats\":[\"Production\",\"Market\",\"FiscalMoney\",\"Population\"],\"Selected\":2,\"MainRatio\":0.4,\"SubRatio\":0.5,\"LeftOpen\":false,\"RightOpen\":false,\"Sections\":{\"Market\":{\"Opened\":[\"a\"],\"Closed\":[]}}}");
        Assert.False(old.LeftOpen);
        Assert.Equal(Slot.Map, old.Background);
        // 旧の版（版の項目が無い）の窓の大きさと位置は読み捨てる・窓以外の設定は残す
        var v1 = LayoutModel.FromJson("{\"LeftOpen\":false,\"Windows\":{\"Market\":[1,2,900,760]}}");
        Assert.Null(v1.RectOf(Slot.Market));
        Assert.False(v1.LeftOpen);
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
        new SectionSpec("a", "甲", 8), new SectionSpec("b", "乙", 6), new SectionSpec("c", "丙", 6),
        new SectionSpec("d", "丁", 6), new SectionSpec("e", "戊", 6),
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
        Assert.Throws<ArgumentException>(() => PagePlan.Arrange(new PageTable([new SectionSpec("a", "甲")], [["x"]]), 3));
        Assert.Throws<ArgumentException>(() => PagePlan.Arrange(new PageTable([new SectionSpec("a", "甲"), new SectionSpec("a", "乙")], [["a"]]), 3));
        Assert.Throws<ArgumentException>(() => PagePlan.Arrange(new PageTable([new SectionSpec("a", "甲")], [["a"], ["a"]]), 3));
    }

    [Fact]
    public void 高さが足りなければ全ての欄を同じ比で狭める_欄は常に開いている_余りは重みで分ける()
    {
        var col = new[] { new SectionSpec("a", "甲", 6), new SectionSpec("b", "乙", 6), new SectionSpec("c", "丙", 6), new SectionSpec("d", "丁", 6) };
        double min = PagePlan.MinHeight(col[0]);
        double all = 4 * min + 3 * PagePlan.SectionGap;
        // 全部入る
        var p = PagePlan.PlanColumn(col, all + 100);
        Assert.Equal(all + 100 - 3 * PagePlan.SectionGap, p.Sum(x => x.Height), 6);   // 余りは使い切る（欄の間の隙間を除いて）
        // 足りない → 全ての欄が同じ高さのまま狭まる（見出しの 1 行より下には潰れない）
        p = PagePlan.PlanColumn(col, all - 80);
        Assert.Equal(all - 80 - 3 * PagePlan.SectionGap, p.Sum(x => x.Height), 6);
        Assert.All(p, x => Assert.Equal(p[0].Height, x.Height, 6));
        Assert.All(p, x => Assert.True(x.Height > PagePlan.HeaderHeight));
        // うんと狭い → 床（見出し＋1 行）で止まる
        p = PagePlan.PlanColumn(col, 10);
        Assert.All(p, x => Assert.Equal(PagePlan.HeaderHeight + PagePlan.RowHeight, x.Height, 6));
        // 重み
        var w = new[] { new SectionSpec("a", "甲", 6, 1), new SectionSpec("b", "乙", 6, 3) };
        var pw = PagePlan.PlanColumn(w, 2 * min + PagePlan.SectionGap + 400);
        Assert.Equal(100, pw[0].Height - min, 6); Assert.Equal(300, pw[1].Height - min, 6);
        // 無限の高さ＝最小の高さのまま
        var inf = PagePlan.PlanColumn(col, double.PositiveInfinity);
        Assert.All(inf, x => Assert.Equal(min, x.Height, 6));
    }

    [Fact]
    public void 欄の高さは中身の上限で止め_余りは他の欄へ回す()
    {
        var col = new[] { new SectionSpec("a", "甲", 6), new SectionSpec("b", "乙", 6), new SectionSpec("c", "丙", 6) };
        // 上限：甲は中身が少ない → 上限で止まり、余りは丙へ回る（全体の高さは使い切る）
        var caps = new Dictionary<string, double> { ["a"] = 100 };
        var p = PagePlan.PlanColumn(col, 1000, caps);
        Assert.Equal(100, p[0].Height, 6);
        Assert.Equal(1000 - 2 * PagePlan.SectionGap, p.Sum(x => x.Height), 6);
        Assert.True(p[1].Height > PagePlan.MinHeight(col[1]) && p[2].Height > PagePlan.MinHeight(col[2]));
        // 全部が上限なら余りは空ける
        var allCaps = new Dictionary<string, double> { ["a"] = 100, ["b"] = 100, ["c"] = 100 };
        p = PagePlan.PlanColumn(col, 3000, allCaps);
        Assert.All(p, x => Assert.Equal(100, x.Height, 6));
    }

    [Fact]
    public void 列の幅は重みの比_無ければ均等()
    {
        var even = PagePlan.ColumnWidths(3, 1000);
        Assert.All(even, x => Assert.Equal((1000 - 2 * PagePlan.ColumnGap) / 3, x, 6));
        var w = PagePlan.ColumnWidths(2, 1000, [3, 2]);
        Assert.Equal(3.0 / 2, w[0] / w[1], 6);
        Assert.Equal(1000 - PagePlan.ColumnGap, w.Sum(), 6);
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
                    double floor = col.Count * (PagePlan.HeaderHeight + PagePlan.RowHeight) + (col.Count - 1) * PagePlan.SectionGap;   // 床（見出し＋1 行）でも入らない高さは仕方がない
                    Assert.True(used <= Math.Max(height, floor) + 1e-6, $"{width}×{height}: 列が高さを超える {used}");
                }
            }
    }
}
