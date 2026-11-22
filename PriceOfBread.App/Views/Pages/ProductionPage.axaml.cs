using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PriceOfBread.App.Controls;
using PriceOfBread.App.ViewModels;

namespace PriceOfBread.App.Views.Pages;

/// <summary>生産のページ（設計 §3-3）。欄の並べの表（ページごとに 1 つだけ手で書く）と、盤の操作ハンドラ（財ノードの選択・空きのドラッグで盤を動かす）。
/// ★2026-08-10のUI分割：GameView.axaml.cs にあった生産タブ用のハンドラ4本を**そのまま**旧の生産タブへ移した物を、(501) 段3 で旧のタブごとこのページへ移した（挙動は不変）。
///   ドラッグ量は e.GetPosition(this) の差分なので、基準を移しても原点差は打ち消し合う。</summary>
public partial class ProductionPage : UserControl
{
    public static readonly PageTable SupplyPlan = new(
        [new SectionSpec("supply", "供給の網", MinRows: 14, Weight: 1)],
        [["supply"]]);

    public static readonly PageTable DetailPlan = new(
        [
            new SectionSpec("detail", "選んだ財", MinRows: 9),
            new SectionSpec("regions", "州別の工場", MinRows: 6, Weight: 1),
        ],
        [["detail", "regions"]]);

    public ProductionPage()
    {
        InitializeComponent();
        MainGrid.Table = SupplyPlan;
        DetailGrid.Table = DetailPlan;
        SectionFit.Attach(SupplySection, BoardBody);
        SectionFit.Attach(RegionsSection, RegionsBody);
        RegionsRows.RowControls = () => RegionsList.ItemsPanelRoot?.Children ?? (IEnumerable<Control>)System.Array.Empty<Control>();
        ProdScroll.PropertyChanged += (_, e) => { if (e.Property == Visual.BoundsProperty) FeedViewport(); };
        DataContextChanged += (_, _) => FeedViewport();
    }

    private void FeedViewport()
    {
        if (DataContext is GameViewModel vm && ProdScroll.Bounds.Width > 0) vm.SetProdViewport(ProdScroll.Bounds.Width);
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
