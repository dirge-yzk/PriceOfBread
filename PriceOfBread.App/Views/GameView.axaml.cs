using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace PriceOfBread.App.Views;
// Views/GameView.axaml.cs ＝ 外枠の code-behind（全画面キー操作）。地図の拡縮/パン・州の当たりは Views/MapPane.axaml.cs（(501) 段2a で移した）。各タブのハンドラは Views/Tabs/*.axaml.cs。
public partial class GameView : UserControl
{
    public GameView()
    {
        InitializeComponent();
        Loaded += OnViewLoaded;
        Unloaded += OnViewUnloaded;
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
        _vmHooked = null;
    }

    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Source is ComboBox or TextBox) return;
        if ((e.Source as Control)?.FindAncestorOfType<ComboBox>() is not null) return;   // ComboBox展開中は Source が ComboBoxItem 等になる＝祖先でもガード
        if (DataContext is not ViewModels.GameViewModel vm) return;
        // モーダル（確認/Escメニュー/#106外交・戦争・国内危機ポップアップ）表示中はタブ・速度・停止系ショートカットを無視（Esc/F5/F9/F11は従来どおり）
        bool modal = vm.AnyPopupOpen;   // ★2026-08-24：VM側の1本と同じ集合（旧＝ここに列挙していて**選挙モーダルが漏れ**、公示中にスペースで時間が動いた）
        if (modal && e.Key is Key.Space or Key.Q or Key.W or Key.E or Key.R or Key.A or Key.S or Key.D
            or (>= Key.D1 and <= Key.D3) or (>= Key.NumPad1 and <= Key.NumPad3)) return;
        if (modal && e.Key == Key.Tab) { e.Handled = true; return; }   // 生産タブは開かない（既定のフォーカス移動も抑止）
        if (modal && e.Key == Key.F) { e.Handled = true; return; }     // 発見パネルもモーダル中は開かない
        // 押しっぱなしのリピート（毎秒30回）を捨てる。Avalonia 12 の KeyEventArgs にリピートの印は無いので、握ったキーを1つ覚えて同じキーの再入を捨てる
        //（Windowsのリピートは最後に押した1キーだけ＝覚えるのは1つで足りる）。これが無いと切り替え系（スペース＝停止/再開・TAB＝生産・F＝発見・F11・Esc）は
        // 離した時の状態が押した長さ任せになり、F5/F9 は押している間ずっと保存/読込が走る。
        if (_heldKey != Key.None && e.Key == _heldKey) { e.Handled = true; return; }
        if (e.Key == Key.Space)
        {
            vm.TogglePauseCommand.Execute(null);
            // 押すたびフォーカスを地図へ集める。理由＝パネルやモーダルを閉じてフォーカス先の要素が木から消えると焦点が null のまま誰も戻さず、
            // 以後キーが届かない経路ができるから（これまでの MapHost.Focus() の呼び手は Loaded の1回と地図の押下だけだった）。
            // ⚠ フォーカスを奪うので**文字入力欄があるとその入力が切れる**。上のガードで TextBox/ComboBox が Source のときは既に抜けている（現状 TextBox は0個）。
            // スライダーの矢印キー微調整も外れるため、フォーカスを戻すのは**スペースだけ**に限る（他のキーは握るだけ）。
            MapView.FocusHost();
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
        // タブのショートカット：Q=財政・W=法律・E=貿易・R=外交・A=POP・S=政治・D=戦争（交戦中のみ）
        // ★2026-08-02：**よく触る順**に並べ替え（作者指示「主に操作するのは財政、法律」）＝Q:財政 W:法律。
        // ★2026-08-10（作者指示）：E=貿易・S=政治へ入れ替え（旧 E=政治・S=貿易）。
        //   ボタンの並び（画面の左から）は変えない＝上段の3番目が S、下段の2番目が E になる。
        //   ⚠表示ラベルは GameView.axaml のタブボタン列＝**両方を同時に直す**。
        else if (e.Key == Key.Q) { vm.SelectTabCommand.Execute("財政"); e.Handled = true; }
        else if (e.Key == Key.W) { vm.SelectTabCommand.Execute("法律"); e.Handled = true; }
        else if (e.Key == Key.E) { vm.SelectTabCommand.Execute("貿易"); e.Handled = true; }
        else if (e.Key == Key.R) { vm.SelectTabCommand.Execute("外交"); e.Handled = true; }
        else if (e.Key == Key.A) { vm.SelectTabCommand.Execute("POP"); e.Handled = true; }
        else if (e.Key == Key.S) { vm.SelectTabCommand.Execute("政治"); e.Handled = true; }
        else if (e.Key == Key.D) { vm.SelectTabCommand.Execute("戦争"); e.Handled = true; }   // 戦争タブは常設7タブの1つ（常時表示）
        // TAB＝生産（全画面のサプライチェーン）。tunnel 段階で横取り＝既定のフォーカス移動を抑止（必ず Handled）
        else if (e.Key == Key.Tab) { vm.ToggleProdCommand.Execute(null); e.Handled = true; }
        // ESC＝一元処理：開いている物（メニュー→生産→モーダル→タブ）を手前から閉じ、何も無ければメニューを開く
        else if (e.Key == Key.Escape) { vm.HandleEscape(); e.Handled = true; }
        // F＝発見パネル（2026-09-03作者指示「発見は増加予定なのでFキーに設定する」＝旧F1から移設。反乱は戦争タブへ戻した）
        else if (e.Key == Key.F) { vm.ToggleDiscoveryCommand.Execute(null); e.Handled = true; }
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
