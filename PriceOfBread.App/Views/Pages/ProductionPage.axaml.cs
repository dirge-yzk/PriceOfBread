using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PriceOfBread.App.Controls;
using PriceOfBread.App.ViewModels;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Views.Pages;

/// <summary>生産のページ（設計 §3-3）。欄の並べの表（ページごとに 1 つだけ手で書く）と、盤の操作ハンドラ（財ノードの選択・空きのドラッグで盤を動かす）。
/// ★2026-08-10のUI分割：GameView.axaml.cs にあった生産タブ用のハンドラ4本を**そのまま**旧の生産タブへ移した物を、(501) 段3 で旧のタブごとこのページへ移した（挙動は不変）。
///   ドラッグ量は e.GetPosition(this) の差分なので、基準を移しても原点差は打ち消し合う。</summary>
public partial class ProductionPage : UserControl
{
    /// <summary>このページの幅がこれ以上のとき、盤の列と他の欄の列の 2 つに分ける（盤が幅の約 56%＝1 行に 3 枚以上並ぶ）。これ未満は 1 つの列に全部を積み、高さに入らない欄は順位の低い方から畳む。</summary>
    public const double WideWidth = 1300;
    private const double MainShare = 0.56, SideShare = 1 - MainShare;

    /// <summary>欄の宣言（鍵・名前・畳む順位・最小の行数・余りの高さの重み）と置き場（列ごとに上から下）。順位：供給の網 1・財の表 2・選んだ財 3・州別の工場 4・労働 5・設備 6・巨大建造物 7（数が大きいほど先に見出しの 1 行へ畳まれる）。
    /// 狭いとき＝1 つの PageGrid（全部を 1 列）／広いとき＝盤の列（供給の網・巨大建造物）と他の欄の列（財の表・選んだ財｜州別の工場・労働・設備＝他の列が 700 以上なら 2 列）の 2 つの PageGrid。
    /// 戻り値は [主の表] か [主の表, 側の表]。</summary>
    public static PageTable[] BuildPlans(bool wide, bool equipment, bool full = false)
    {
        var supply = new SectionSpec("supply", "供給の網", 1, MinRows: 14, Weight: 3);
        var goods = new SectionSpec("goods", "財の表", 2, MinRows: 8, Weight: 2);
        var detail = new SectionSpec("detail", "選んだ財", 3, MinRows: 9, Weight: 2);
        var regions = new SectionSpec("regions", "州別の工場", 4, MinRows: 6);
        var labor = new SectionSpec("labor", "労働", 5, MinRows: 3);
        var equip = new SectionSpec("equipment", "設備", 6, MinRows: 5);
        var mega = new SectionSpec("mega", "巨大建造物", 7, MinRows: 4);
        var lower = new System.Collections.Generic.List<SectionSpec> { regions, labor };
        if (equipment) lower.Add(equip);
        if (!wide)
        {
            var all = new System.Collections.Generic.List<SectionSpec> { supply, goods, detail };
            all.AddRange(lower); all.Add(mega);
            return [new PageTable(all, [all.Select(s => s.Key).ToList()])];
        }
        var side = new System.Collections.Generic.List<SectionSpec> { goods, detail };
        side.AddRange(lower);
        if (full)
        {
            // 全面（Q）：盤だけで左の列を使い切り（高さも幅も盤が主）・巨大建造物は右の列の最後へ。右の列は旧の生産タブの右パネルと同じ固定の幅で 1 列に積む
            side.Add(mega);
            return
            [
                new PageTable([supply], [["supply"]]),
                new PageTable(side, [side.Select(s => s.Key).ToList()]),
            ];
        }
        return
        [
            new PageTable([supply, mega], [["supply", "mega"]]),
            new PageTable(side, [["goods", "detail"], lower.Select(s => s.Key).ToList()]),
        ];
    }

    private GameViewModel? _vm;
    private (bool Wide, bool Equipment, bool Full)? _applied;   // 今の並べ（広い・設備の欄を出すか・盤を主にした並べか）

    /// <summary>盤を主にした並びのとき、右の列（財の表ほか）の固定の幅。旧の生産タブの右パネルは 530 だったが、左右の縁の釦・枠・欄の縁の分（約 60）が盤の視野を削るので、
    /// 盤の視野が旧と同じ約 1380（1 行 6 枚）になるようその分を引いた（530 のままでは視野 1306＝5 枚だった）。</summary>
    public const double FullSideWidth = 500;   // 440 では財の表の右端の見出し（国営・民営）が切れた（V19）

    public ProductionPage()
    {
        InitializeComponent();
        // 表・盤・州別は欄の高さに合わせる＝見出しを止めて行だけを欄の中で流す
        SectionFit.Attach(SupplySection, BoardBody);
        SectionFit.Attach(GoodsSection, GoodsTable);
        SectionFit.Attach(RegionsSection, RegionsBody);
        SectionFit.Attach(LaborSection, LaborTable);
        SectionFit.Attach(EquipSection, EquipTable);
        // 財の表の行を押す＝その財を選ぶ（盤の財を押したのと同じ動詞）
        GoodsTable.RowActivated += key => { if (_vm is not null && int.TryParse(key, out int gid)) _vm.SelectProdGood(gid); };
        // 盤は視野の幅に追従する（分割の枠の幅に合わせて並べ直す）
        ProdScroll.PropertyChanged += (_, e) => { if (e.Property == Visual.BoundsProperty) FeedViewport(); };
        PageRoot.PropertyChanged += (_, e) => { if (e.Property == Visual.BoundsProperty) ApplyLayout(); };
        DataContextChanged += (_, _) => AttachVm();
        ApplyLayout();
    }

    private void AttachVm()
    {
        if (_vm is not null) _vm.PropertyChanged -= OnVmChanged;
        _vm = DataContext as GameViewModel;
        if (_vm is not null) _vm.PropertyChanged += OnVmChanged;
        ApplyLayout();
        FeedViewport();
    }

    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameViewModel.ProdEquipmentOn)) ApplyLayout();
    }

    /// <summary>ページの幅から、1 列か 2 列か（盤の列＋他の欄の列）を決め、欄を動かして並べの表を付け直す。設備の欄は旗が 0 の世界では欄ごと出さない（並べの表からも外す）。</summary>
    private void ApplyLayout()
    {
        bool wide = PageRoot.Bounds.Width >= WideWidth;
        bool equipment = _vm?.ProdEquipmentOn ?? true;
        bool full = wide;   // 生産は中央いっぱいに出る（2026-11-04 に分割を廃止）＝広ければ盤を主にした並べ
        if (_applied == (wide, equipment, full)) return;   // 同じ形なら置き直さない（幅が少し動くたびに並べの表を付け直して測り直しを連鎖させない）
        _applied = (wide, equipment, full);
        var sideSections = new Control[] { GoodsSection, DetailSection, RegionsSection, LaborSection, EquipSection };
        var home = wide ? SideGrid : MainGrid;
        foreach (var s in sideSections)
            if (!ReferenceEquals(s.Parent, home)) { (s.Parent as Panel)?.Children.Remove(s); home.Children.Add(s); }
        var megaHome = full ? SideGrid : MainGrid;   // 巨大建造物は全面のとき右の列の最後（盤の高さを取らない）
        if (!ReferenceEquals(MegaSection.Parent, megaHome)) { (MegaSection.Parent as Panel)?.Children.Remove(MegaSection); megaHome.Children.Add(MegaSection); }
        EquipSection.IsVisible = equipment;
        var plans = BuildPlans(wide, equipment, full);
        MainGrid.Table = plans[0];
        SideGrid.Table = wide ? plans[1] : null;
        SideGrid.IsVisible = wide;
        if (full)
        {
            PageRoot.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            PageRoot.ColumnDefinitions[1].Width = new GridLength(FullSideWidth, GridUnitType.Pixel);
        }
        else
        {
            PageRoot.ColumnDefinitions[0].Width = new GridLength(wide ? MainShare : 1, GridUnitType.Star);
            PageRoot.ColumnDefinitions[1].Width = new GridLength(wide ? SideShare : 0, wide ? GridUnitType.Star : GridUnitType.Pixel);
        }
    }

    private void FeedViewport()
    {
        if (_vm is not null && ProdScroll.Bounds.Width > 0) _vm.SetProdViewport(ProdScroll.Bounds.Width);
    }

    // 生産タブ：財ノードのクリック＝選択（同じ財の再クリックで解除）。ホバーでは再構築しない＝軽量
    private void OnProdNodePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border b && b.DataContext is ViewModels.ProdNode node && DataContext is ViewModels.GameViewModel vm)
        {
            vm.SelectProdGood(node.GoodId);
            e.Handled = true;
        }
    }

    // 生産タブ：空き領域のドラッグで画面（グラフ）を移動。ノード上の押下は上でHandled済み＝ここには来ない
    private ScrollViewer? _prodScroll;
    private bool _prodPanning;
    private Point _prodPanStart;
    private Vector _prodPanOffset0;
    private bool _prodPanMoved;                   // 押下からドラッグ閾値を超えたか（超えなければ背景クリック＝選択解除で一覧へ）
    private const double ProdClickSlopPx = 5.0;   // クリックとドラッグの境（px）
    // 財を選んだ時の自動スクロール（2026-08-02の ScrollGraphToX）は廃止（作者仕様2026-08-09 項目12）＝
    // 選ぶたびに視界が飛ぶと隣のノードを続けて押せない。視点はドラッグとスクロールバーだけが動かす。
    private void OnProdPanPressed(object? sender, PointerPressedEventArgs e)
    {
        // 分割前は GameView 側から FindControl("ProdScroll") で引いていた同じ実体。x:Name の生成フィールドを直に使う
        // ＝名前が食い違えば**コンパイルで落ちる**（GUI起動できない環境では、これが結線の唯一の証拠になる）。
        _prodScroll ??= ProdScroll;
        if (_prodScroll is null) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;   // 左ボタンのみパン開始（地図側 OnPressed と同じ流儀）
        _prodPanning = true;
        _prodPanMoved = false;
        _prodPanStart = e.GetPosition(this);
        _prodPanOffset0 = _prodScroll.Offset;
        e.Pointer.Capture(sender as IInputElement);
        e.Handled = true;
    }
    private void OnProdPanMoved(object? sender, PointerEventArgs e)
    {
        if (!_prodPanning || _prodScroll is null) return;
        var d = e.GetPosition(this) - _prodPanStart;
        if (System.Math.Abs(d.X) > ProdClickSlopPx || System.Math.Abs(d.Y) > ProdClickSlopPx) _prodPanMoved = true;
        _prodScroll.Offset = new Vector(_prodPanOffset0.X - d.X, _prodPanOffset0.Y - d.Y);
    }
    private void OnProdPanReleased(object? sender, PointerReleasedEventArgs e)
    {
        // 動かさずに離した＝背景クリック＝財の選択を解除して一覧へ戻す（✕ボタン/TAB2段閉じと同じ既存経路）。
        // ノード上の押下は OnProdNodePressed が Handled 済みでここに来ない＝ノードクリックと衝突しない。
        if (_prodPanning && !_prodPanMoved && DataContext is ViewModels.GameViewModel vm)
            vm.CloseProdDetailCommand.Execute(null);
        _prodPanning = false;
        e.Pointer.Capture(null);
    }
}
