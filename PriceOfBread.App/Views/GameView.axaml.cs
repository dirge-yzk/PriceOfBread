using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PriceOfBread.App.Layout;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.Views;
// Views/GameView.axaml.cs ＝ 外枠の code-behind（全画面キー操作）。地図の拡縮/パン・州の当たりは Views/MapPane.axaml.cs（(501) 段2a で移した）。各ページのハンドラは Views/Pages/*.axaml.cs。
public partial class GameView : UserControl
{
    /// <summary>地図の欄。盤面に 1 つだけ（地図の拡縮・パンと VM の拡大率は 1 つの値）。枠に置かれていないときは木に付かないが、生きている（陸の焼きは作った時に 1 回）。</summary>
    public MapPane MapView { get; } = new();

    private readonly System.Collections.Generic.Dictionary<Slot, Control> _pages = new();

    public GameView()
    {
        InitializeComponent();
        Focusable = true;   // 地図が枠に無いとき、スペースなどのキーの受け皿
        Loaded += OnViewLoaded;
        Unloaded += OnViewUnloaded;
    }

    private ViewModels.GameViewModel? _layoutVm;   // 配置の変更を購読したVM（解放用）

    /// <summary>枠に置くものの部品（種類ごとに 1 つ・作り直さない）。ページは Views/Pages・地図は MapView。</summary>
    private Control PartOf(Slot slot)
    {
        if (slot == Slot.Map) return MapView;
        if (!_pages.TryGetValue(slot, out var c))
        {
            _pages[slot] = c = Pages.PageFactory.Create(slot);
            // 見出しを押して開け閉めした欄は配置と一緒に保存する（前回の開け閉めを戻し、変わるたびに渡す）
            if (DataContext is ViewModels.GameViewModel gv && c.GetLogicalDescendants().OfType<Controls.PageGrid>().FirstOrDefault() is { } grid)
            {
                string pageKey = slot.ToString();
                var (opened, closed) = gv.Layout.SectionState(pageKey);
                grid.SetUserState(opened, closed);
                grid.UserStateChanged += () => gv.Layout.SetSectionState(pageKey, grid.UserOpened, grid.UserClosed);
            }
        }
        return c;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _layoutVm = null;
        if (DataContext is ViewModels.GameViewModel vm)
        {
            Center.Attach(vm.Layout, PartOf);
            Windows.Attach(vm.Layout, PartOf);
            _layoutVm = vm;
        }
        else { Center.Detach(); Windows.Detach(); }
    }

    /// <summary>スペースなどを押したあと、キーの焦点を戻す先（地図が枠にあれば地図の箱・無ければこの画面）。</summary>
    private void FocusSink()
    {
        if (MapView.IsLoaded) MapView.FocusHost(); else Focus();
    }

    private TopLevel? _keyTop;   // キーハンドラを付けた Window（解放用）

    private Key _heldKey = Key.None;   // いま握っているキー＝KeyDown を Handled にした1つ。押しっぱなしのリピート除去と、対になる KeyUp の判別に使う（OnGlobalKeyDown／OnGlobalKeyUp）

    private ViewModels.GameViewModel? _vmHooked;   // 終了処理を持つVM（解放用）

    private void OnViewLoaded(object? sender, RoutedEventArgs e)
    {
        if (_keyTop is null && TopLevel.GetTopLevel(this) is { } top)
        {
            _keyTop = top;
            top.AddHandler(KeyDownEvent, OnGlobalKeyDown, RoutingStrategies.Tunnel);   // Tunnelのみ（Bubble併記は未処理キーでハンドラが2回走る二重登録だった）
            top.AddHandler(KeyUpEvent, OnGlobalKeyUp, RoutingStrategies.Tunnel);       // 離した側も横取り（理由は OnGlobalKeyUp のコメント＝KeyDown を握ってもスペースがボタンを押す）
            if (top is Window win) win.WindowState = WindowState.FullScreen;   // ゲーム開始（地図を開いた時）＝フルスクリーン
        }
        if (_vmHooked is null && DataContext is ViewModels.GameViewModel vm)
        {
            _vmHooked = vm;
            Controls.TableView.DefaultConfirm = vm.ConfirmVerb;   // 共通の表の動詞の確かめの口（確かめの文を持つ動詞は、この窓を通らずには発動しない）
            vm.FocusRequested += OnFocusRequested;                 // 問題の一覧の飛び先（ページの表の行・欄）
            vm.RebuildPoliticalFill();   // 背景（塗りの型紙）は MapPane の構築で焼き済み＝型紙付きで1枚焼き直す
        }
    }

    private void OnViewUnloaded(object? sender, RoutedEventArgs e)
    {
        _vmHooked?.Shutdown();   // 旧VMのタイマー停止（DataContext は差し替え済みで null/新VMになり得るため保持参照で確実に止める）
        _keyTop?.RemoveHandler(KeyDownEvent, OnGlobalKeyDown);
        _keyTop?.RemoveHandler(KeyUpEvent, OnGlobalKeyUp);   // 登録と対（付けた分だけ外す）
        _keyTop = null;
        _heldKey = Key.None;
        Controls.TableView.DefaultConfirm = null;
        if (_vmHooked is not null) _vmHooked.FocusRequested -= OnFocusRequested;
        _vmHooked = null;
    }

    /// <summary>問題の一覧の飛び先：欄を開き（畳まれていれば）、表の行を見える所へ送って目印を付ける。生産の財の表は財を選ぶ（行を押したのと同じ）。
    /// 部品の配置が済むまで 1 回後ろへ回し、行がまだ無ければ数回だけ待つ。</summary>
    private void OnFocusRequested(ViewModels.FocusRequest r)
    {
        if (!_pages.TryGetValue((Slot)(int)r.Page, out var page)) return;
        if (r.Section.Length > 0 && page.GetVisualDescendants().OfType<Controls.PageGrid>().FirstOrDefault() is { } grid) grid.Reveal(r.Section);
        if (r.TableKey == "production.goods" && int.TryParse(r.RowKey, out int gid) && DataContext is ViewModels.GameViewModel vm) vm.SelectProdGoodIfNew(gid);
        if (r.TableKey.Length > 0) Dispatcher.UIThread.Post(() => FocusRow(page, r, 0), DispatcherPriority.Background);
    }

    private void FocusRow(Control page, ViewModels.FocusRequest r, int attempt)
    {
        var tv = page.GetVisualDescendants().OfType<Controls.TableView>().FirstOrDefault(t => t.Snapshot?.Key == r.TableKey);
        if (tv?.FocusRow(r.RowKey) == true) return;
        if (attempt < 4) Dispatcher.UIThread.Post(() => FocusRow(page, r, attempt + 1), DispatcherPriority.Background);
    }

    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        if (Program.UiShotDir is not null) return;   // 無人の撮影の間は本物のキーを受けない（撮影は VM の口を直接呼ぶ＝外から届いたキーで配置が動いて撮影がずれた回があった）
        if (e.Source is ComboBox or TextBox) return;
        if ((e.Source as Control)?.FindAncestorOfType<ComboBox>() is not null) return;   // ComboBox展開中は Source が ComboBoxItem 等になる＝祖先でもガード
        if (DataContext is not ViewModels.GameViewModel vm) return;
        // モーダル（確認/Escメニュー/#106外交・戦争・国内危機ポップアップ）表示中はタブ・速度・停止系ショートカットを無視（Esc/F5/F9/F11は従来どおり）
        bool modal = vm.AnyPopupOpen;   // ★2026-08-24：VM側の1本と同じ集合（旧＝ここに列挙していて**選挙モーダルが漏れ**、公示中にスペースで時間が動いた）
        // ページのキー（PageKeys.Priority の QWERASDFZXCV＝空きのキーも含む）と Tab。モーダル中は何もしない
        char? letter = e.Key >= Key.A && e.Key <= Key.Z && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0 ? (char)('A' + (e.Key - Key.A)) : null;
        bool isPriorityKey = letter is { } lc && PageKeys.Priority.Contains(lc);
        if (modal && (e.Key is Key.Space or Key.Tab or (>= Key.D1 and <= Key.D3) or (>= Key.NumPad1 and <= Key.NumPad3) || isPriorityKey)) return;
        // 押しっぱなしのリピート（毎秒30回）を捨てる。Avalonia 12 の KeyEventArgs にリピートの印は無いので、握ったキーを1つ覚えて同じキーの再入を捨てる
        //（Windowsのリピートは最後に押した1キーだけ＝覚えるのは1つで足りる）。これが無いと切り替え系（スペース＝停止/再開・ページと地図のキー・F11・Esc）は
        // 離した時の状態が押した長さ任せになり、F5/F9 は押している間ずっと保存/読込が走る。
        if (_heldKey != Key.None && e.Key == _heldKey) { e.Handled = true; return; }
        if (e.Key == Key.Tab) { vm.ToggleProductionMap(); e.Handled = true; }   // Tab＝生産と地図（背景）の切替（2026-11-04 作者）。既定のフォーカス移動も握って抑止
        else if (e.Key == Key.Space)
        {
            vm.TogglePauseCommand.Execute(null);
            // 押すたびフォーカスを地図へ集める。理由＝パネルやモーダルを閉じてフォーカス先の要素が木から消えると焦点が null のまま誰も戻さず、
            // 以後キーが届かない経路ができるから（これまでの MapHost.Focus() の呼び手は Loaded の1回と地図の押下だけだった）。
            // ⚠ フォーカスを奪うので**文字入力欄があるとその入力が切れる**。上のガードで TextBox/ComboBox が Source のときは既に抜けている（現状 TextBox は0個）。
            // スライダーの矢印キー微調整も外れるため、フォーカスを戻すのは**スペースだけ**に限る（他のキーは握るだけ）。
            FocusSink();
            e.Handled = true;
        }
        else if (e.Key is >= Key.D1 and <= Key.D3)
        {
            vm.SetSpeedCommand.Execute(((int)e.Key - (int)Key.D0).ToString());
            e.Handled = true;
        }
        else if (e.Key is >= Key.NumPad1 and <= Key.NumPad3)
        {
            vm.SetSpeedCommand.Execute(((int)e.Key - (int)Key.NumPad0).ToString());
            e.Handled = true;
        }
        // F11＝フルスクリーンの切替（開始時は全画面。ウィンドウへ戻す安全弁）
        else if (e.Key == Key.F11)
        {
            if (_keyTop is Window w) w.WindowState = w.WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
            e.Handled = true;
        }
        // ページのキー（作者の指示 2026-10-23「QWERASDFZXCV の優先度で、順に埋める」・2026-11-04「生産は Tab・市場が Q・地図のキーは無い」・2026-11-11「窓の開閉」）：
        // 割り当ては本体の PageKeys が持つ（Q 市場・清算・貿易・W 財政と貨幣・E 人口・R 政治と法・A 外交と戦争・S 発見・DFZXCV 空き）。
        // 浮かぶ窓の開閉の切り替え（開いていれば閉じ・閉じていれば開いて前へ）。空きのキーは何もしない。Tab（背景の生産と地図の切り替え）は上で処理する。
        else if (letter is { } ch && SlotInfo.OfKey(ch) is { } slot) { vm.OpenSlotCommand.Execute(slot); e.Handled = true; }
        else if (isPriorityKey) e.Handled = true;   // 空きのキー＝何も開かない（押下だけ握る）
        // ESC＝一元処理：開いている物（メニュー→モーダル）を手前から閉じ、何も無ければメニューを開く（ページは常に枠にある＝閉じる物ではない）
        else if (e.Key == Key.Escape) { vm.HandleEscape(); e.Handled = true; }
        // F5＝クイックセーブ／F9＝クイックロード
        else if (e.Key == Key.F5) { vm.SaveGameNowCommand.Execute(null); e.Handled = true; }
        else if (e.Key == Key.F9) { vm.RequestLoadCommand.Execute(null); e.Handled = true; }
        if (e.Handled) _heldKey = e.Key;   // 握ったキーだけ覚える（未処理キーのリピートは素通しのまま＝上のリピート除去は横取りしたキーにしか効かない）
    }

    // KeyUp も横取りする理由：**KeyDown を Handled にしても KeyUp は素通りし、フォーカスのあるボタンがスペースで押される**（2026-09-23 実測）。
    // 前提＝本作のボタンは App.axaml の全クラス（speed/tab/mini/counter/row/warn/dialog/close/lawstage）で Focusable=False ＝
    // 「スペースでボタンを押す」キーボード操作は元々使えず、ここで握っても実害は無い。**将来 Focusable なボタンを足すとこの1本が邪魔になる**。
    // ボタン以外で唯一フォーカスを取るのは開戦ポップアップの CheckBox「総動員」だが、モーダル中のスペースは上で横取りせず返している＝
    // _heldKey が立たないのでここも素通り＝スペースでの開閉は従来どおり効く。
    private void OnGlobalKeyUp(object? sender, KeyEventArgs e)
    {
        if (_heldKey == Key.None || e.Key != _heldKey) return;   // KeyDown を握っていないキー（モーダル中のスペース等）はそのまま通す
        _heldKey = Key.None;
        if (e.Key == Key.Space) e.Handled = true;   // 押下を握った時だけ対で握る
    }
}
