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

    private Pages.DiplomacyWarPage? _diploPage;   // 地図を欄として畳み込めるページ（外交と戦争）。作られたら覚える
    private ViewModels.GameViewModel? _layoutVm;   // 配置の変更を購読したVM（解放用）

    /// <summary>枠に置くものの部品（種類ごとに 1 つ・作り直さない）。ページは Views/Pages・地図は MapView。</summary>
    private Control PartOf(Slot slot)
    {
        if (slot == Slot.Map) { _diploPage?.HostMap(null); return MapView; }   // 地図が枠に単独で載る＝外交と戦争の欄から外す（同じ部品は同時に 1 か所にしか置けない）
        if (!_pages.TryGetValue(slot, out var c))
        {
            _pages[slot] = c = Pages.PageFactory.Create(slot);
            if (c is Pages.DiplomacyWarPage dp) _diploPage = dp;
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
        if (_layoutVm is not null) { _layoutVm.Layout.Changed -= SyncMapHost; _layoutVm = null; }
        if (DataContext is ViewModels.GameViewModel vm)
        {
            Splits.Attach(vm.Layout, PartOf);
            _layoutVm = vm; vm.Layout.Changed += SyncMapHost;   // 外枠（SplitHost）の購読より後に走る＝枠の付け替えが済んでから、地図の置き場を決める
            SyncMapHost();
        }
        else Splits.Detach();
    }

    /// <summary>地図の置き場を決める。地図が単独の枠にあればそこ・無ければ外交と戦争のページの欄（ページが作られていれば。見えていなくても欄が持つ）。
    /// 地図の部品は盤面に 1 つだけなので、枠から外れたとき（欄に戻るとき）にだけ付け替える。</summary>
    private void SyncMapHost()
    {
        if (_diploPage is null || _layoutVm is null) return;
        if (_layoutVm.Layout.IsShown(Slot.Map)) { _diploPage.HostMap(null); return; }
        if (_diploPage.HostsMap || MapView.Parent is not null) return;
        MapView.DataContext = _layoutVm;   // 枠から外れたとき DataContext を空に戻してあるので、欄の親から受け継げるよう明示する
        _diploPage.HostMap(MapView);
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
            Controls.PinHost.Current = vm;                         // 表の行・線グラフ・ヘッダーの指標の右クリックのピン留めの受け口
            vm.FocusRequested += OnFocusRequested;                 // 問題の一覧とピン留めの飛び先（ページの表の行・欄）
            HeaderStatList.AddHandler(ContextRequestedEvent, OnHeaderContext, RoutingStrategies.Bubble);
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
        if (ReferenceEquals(Controls.PinHost.Current, _vmHooked)) Controls.PinHost.Current = null;
        if (_vmHooked is not null) _vmHooked.FocusRequested -= OnFocusRequested;
        HeaderStatList.RemoveHandler(ContextRequestedEvent, OnHeaderContext);
        _vmHooked = null;
    }

    /// <summary>ヘッダーの指標の右クリック＝ピン留め／ピン留めを外す。</summary>
    private void OnHeaderContext(object? sender, ContextRequestedEventArgs e)
    {
        if (DataContext is not ViewModels.GameViewModel vm || e.Source is not Control src || src.DataContext is not ViewModels.HeaderStat st) return;
        var pin = vm.HeaderPin(st);
        var mi = new MenuItem { Header = vm.IsPinned(pin.Id) ? "ピン留めを外す" : "ピン留め" };
        mi.Click += (_, _) => vm.Toggle(pin);
        new MenuFlyout { ItemsSource = new object[] { mi } }.ShowAt(src);
        e.Handled = true;
    }

    /// <summary>問題の一覧とピン留めの飛び先：欄を開き（畳まれていれば）、表の行を見える所へ送って目印を付ける。生産の財の表は財を選ぶ（行を押したのと同じ）。
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
        if (e.Source is ComboBox or TextBox) return;
        if ((e.Source as Control)?.FindAncestorOfType<ComboBox>() is not null) return;   // ComboBox展開中は Source が ComboBoxItem 等になる＝祖先でもガード
        if (DataContext is not ViewModels.GameViewModel vm) return;
        // モーダル（確認/Escメニュー/#106外交・戦争・国内危機ポップアップ）表示中はタブ・速度・停止系ショートカットを無視（Esc/F5/F9/F11は従来どおり）
        bool modal = vm.AnyPopupOpen;   // ★2026-08-24：VM側の1本と同じ集合（旧＝ここに列挙していて**選挙モーダルが漏れ**、公示中にスペースで時間が動いた）
        // ページと地図のキー（PageKeys.Priority の QWERASDFZXCV＝空きのキーも含む）。モーダル中は何もしない
        char? letter = e.Key >= Key.A && e.Key <= Key.Z && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) == 0 ? (char)('A' + (e.Key - Key.A)) : null;
        bool isPriorityKey = letter is { } lc && PageKeys.Priority.Contains(lc);
        if (modal && (e.Key is Key.Space or (>= Key.D1 and <= Key.D3) or (>= Key.NumPad1 and <= Key.NumPad3) || isPriorityKey)) return;
        if (e.Key == Key.Tab) { e.Handled = true; return; }   // Tab は何にも割り当てない（既定のフォーカス移動だけを抑止）。旧の生産のキーは Q に移った
        // 押しっぱなしのリピート（毎秒30回）を捨てる。Avalonia 12 の KeyEventArgs にリピートの印は無いので、握ったキーを1つ覚えて同じキーの再入を捨てる
        //（Windowsのリピートは最後に押した1キーだけ＝覚えるのは1つで足りる）。これが無いと切り替え系（スペース＝停止/再開・ページと地図のキー・F11・Esc）は
        // 離した時の状態が押した長さ任せになり、F5/F9 は押している間ずっと保存/読込が走る。
        if (_heldKey != Key.None && e.Key == _heldKey) { e.Handled = true; return; }
        if (e.Key == Key.Space)
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
        // ページと地図のキー（作者の指示 2026-10-23「QWERASDFZXCV の優先度で、順に埋める」）：割り当ては本体の PageKeys が持つ（Q 生産・W 市場・清算・貿易・E 財政と貨幣・R 人口・A 政治と法・S 外交と戦争・D 発見・F 地図・ZXCV 空き）。
        // 選んでいる枠へ開く。Shift を足すと次の枠へ開く。空きのキーは何もしない。旧の TAB（生産）・Q（財政）・W（法律）などの割り当ては、この表に置き換えた。
        else if (letter is { } ch && SlotInfo.OfKey(ch) is { } slot) { vm.OpenSlotByKey(slot, (e.KeyModifiers & KeyModifiers.Shift) != 0); e.Handled = true; }
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
