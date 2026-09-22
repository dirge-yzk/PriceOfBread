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

// Views/GameView.axaml.cs ＝ 外枠の code-behind（地図の拡縮/パン・全画面キー操作）。各タブのハンドラは Views/Tabs/*.axaml.cs。
// 地図中心UI（2026-06-29 再構成）：MapRoot（仮想2000×780＝MapData.MapWidth×MapHeight）に拡縮(Scale)＋移動(Translate)の RenderTransform を当てる。
// ホイール=カーソル中心ズーム／背景ドラッグ=パン。**縦はマップ外に出ない（クランプ）／横は東西ループ（折り返し）**。
// 州○（Ellipse）上の押下はパンせずクリック（詳細モーダル）に通す。横ループは VM が州○を2コピー描き、ここで translate.X を周期で折り返して継ぎ目なく繋ぐ。
public partial class GameView : UserControl
{
    private readonly ScaleTransform _scale = new(1, 1);
    private readonly TranslateTransform _translate = new(0, 0);
    private bool _panning, _fitted;
    private Point _last;
    private const double MaxScale = 12.0;   // 過剰ズームの防止（20→6・2026-07-05）。★6→12（2026-08-31作者指示「解像度2倍＝2倍ズーム」）

    public GameView()
    {
        InitializeComponent();
        MapRoot.RenderTransform = new TransformGroup { Children = { _scale, _translate } };
        MapRoot.RenderTransformOrigin = RelativePoint.TopLeft;
        MapHost.PointerWheelChanged += OnWheel;
        MapHost.PointerPressed += OnPressed;
        MapHost.PointerMoved += OnMoved;
        MapHost.PointerReleased += OnReleased;
        MapHost.SizeChanged += OnHostSizeChanged;
        MapHost.PointerExited += (_, _) => { _hoverDot = null; if (DataContext is ViewModels.GameViewModel vmx && !vmx.HoverPinned) vmx.MapHover = null; };   // 地図から離れたら左上パネルを閉じる
        // スペース＝停止/再開、1〜3＝速度。フォーカス位置に依存せず効くよう Window(TopLevel) にトンネル登録して
        // どの子（タブ/速度ボタン等）より先に横取りする。ComboBox/TextBox 上では選択・入力を邪魔しない。
        MapHost.Focusable = true;
        Loaded += OnViewLoaded;
        Unloaded += OnViewUnloaded;
        TryLoadWorldMap();
    }

    private TopLevel? _keyTop;   // キーハンドラを付けた Window（解放用）

    private Key _heldKey = Key.None;   // いま握っているキー＝KeyDown を Handled にした1つ。押しっぱなしのリピート除去と、対になる KeyUp の判別に使う（OnGlobalKeyDown／OnGlobalKeyUp）

    private ViewModels.GameViewModel? _vmHooked;   // CenterMap を購読したVM（解放用）

    private bool _homed;   // 自国センタリングを一度だけ実行

    private void OnViewLoaded(object? sender, RoutedEventArgs e)
    {
        // ツリー構築直後だと Border.Focus() が通らないことがあるので遅延実行。フォーカスが MapHost にあれば KeyDown が発火する。
        Dispatcher.UIThread.Post(() => MapHost.Focus(), DispatcherPriority.Loaded);
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
            vm.CenterMap += OnCenterMap;
            vm.RebuildPoliticalFill();   // 背景（塗りの型紙）はコンストラクタで焼き済み＝型紙付きで1枚焼き直す
        }
        Dispatcher.UIThread.Post(HomeView, DispatcherPriority.Background);   // レイアウト確定後に自国を中央へ
    }

    // 地図の初期表示：自国が見やすい倍率にズームし、自国首都を画面中央へ。レイアウト未確定なら再試行。
    private void HomeView()
    {
        if (_homed) return;
        if (MapHost.Bounds.Width < 1 || MapHost.Bounds.Height < 1) { Dispatcher.UIThread.Post(HomeView, DispatcherPriority.Background); return; }
        double s = Math.Clamp(MinScale() * 2.2, MinScale(), MaxScale);
        _scale.ScaleX = _scale.ScaleY = s;
        _fitted = true;
        (DataContext as ViewModels.GameViewModel)?.SetMapFitScale(MinScale());   // 地名の表示閾値をフィット倍率基準に
        if (DataContext is ViewModels.GameViewModel vm && vm.PlayerCapitalCanvas() is { } p)
        {
            _translate.X = MapHost.Bounds.Width / 2 - p.X * s;
            _translate.Y = MapHost.Bounds.Height / 2 - p.Y * s;
        }
        else { _translate.X = MapHost.Bounds.Width / 2 - MapData.MapWidth * s / 2; _translate.Y = 0; }
        ClampAndWrap();
        (DataContext as ViewModels.GameViewModel)?.SetDotZoom(s, immediate: true);   // 即時補正＝初回描画（_dotZoom=1）と初期ズームの間の「一瞬大きい○」を根絶。首都なし分岐でも適用
        _homed = true;
    }

    private void OnViewUnloaded(object? sender, RoutedEventArgs e)
    {
        _vmHooked?.Shutdown();   // 旧VMのタイマー停止（DataContext は差し替え済みで null/新VMになり得るため保持参照で確実に止める）
        _keyTop?.RemoveHandler(KeyDownEvent, OnGlobalKeyDown);
        _keyTop?.RemoveHandler(KeyUpEvent, OnGlobalKeyUp);   // 登録と対（付けた分だけ外す）
        _keyTop = null;
        _heldKey = Key.None;
        if (_vmHooked is not null) { _vmHooked.CenterMap -= OnCenterMap; _vmHooked = null; }
    }

    // 州○ホバー：詳細を地図左上の固定パネルへ（ツールチップより長いリストを出せる）。
    private Ellipse? _hoverDot;                       // いま詳細を出している○（消す判定の基準＝OnMoved のヒステリシス）
    private const double HoverExitMargin = 5.0;       // 出した○の外周からこれだけ離れて初めて消す（円の縁での点滅を消す幅・単位は○のローカル座標＝px）

    private void OnDotEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Ellipse el && el.DataContext is ViewModels.ProvinceDot dot && DataContext is ViewModels.GameViewModel vm)
        {
            if (!vm.HoverPinned) vm.MapHover = dot.Hover;   // ピン留め中は持ち替えない（§7）
            _hoverDot = el;
        }
    }

    /// <summary>いま詳細を出している○から**十分に**離れたか。基準の○を見失った（州○は四半期ごとに作り直される）時は
    /// 即座に消す＝残骸を残さない（○の上に居る場合は呼び出し側が先に持ち替えている）。</summary>
    private bool LeftHoverDot(PointerEventArgs e)
    {
        if (_hoverDot is null || !_hoverDot.IsAttachedToVisualTree()) return true;
        var p = e.GetPosition(_hoverDot);   // ○のローカル座標（0,0〜Bounds）＝ズーム・東西ループを織り込んだ実位置
        var b = _hoverDot.Bounds;
        return p.X < -HoverExitMargin || p.Y < -HoverExitMargin
            || p.X > b.Width + HoverExitMargin || p.Y > b.Height + HoverExitMargin;
    }

    // 植民地○クリック：狙いのトグル（02断線走査2026-07-28＝講和の優先割譲 Colony.PlayerInterest の書き手）。州○（Colony=null）は左クリックで州パネルをピン留め（同じ州でもう一度＝解除・2026-09-07）。
    private void OnDotPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Ellipse el && el.DataContext is ViewModels.ProvinceDot dot && DataContext is ViewModels.GameViewModel vm)
        {
            if (dot.Colony is not null) { vm.ToggleColonyInterest(dot.Colony); e.Handled = true; }
            else if (dot.Region is not null && dot.Hover is not null && e.GetCurrentPoint(el).Properties.IsLeftButtonPressed)
            {
                vm.TogglePin(dot.Hover, dot.Region);   // ★州パネルのピン留め（§7）：同じ州の○でもう一度＝解除（鍵は州＝四半期の作り直しに耐える）
                e.Handled = true;
            }
        }
    }

    // 外交の行クリック：その国の座標（仮想キャンバス）が画面中央に来るよう translate を合わせる（ズームは維持）。
    private void OnCenterMap(double x, double y)
    {
        double s = _scale.ScaleX;
        _translate.X = MapHost.Bounds.Width / 2 - x * s;
        _translate.Y = MapHost.Bounds.Height / 2 - y * s;
        ClampAndWrap();
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
            MapHost.Focus();
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

    // スライダーの硬い上下限は Views/SliderLimit.cs（添付プロパティ）。

    // 生産タブ（ProdScroll のパン・ノード選択）のハンドラは Views/Tabs/ProductionTab.axaml.cs へ移送（2026-08-10のUI分割）。

    // 背景の世界地図：SVG（Assets/worldmap.svg）の全 <path> を Avalonia の Geometry に解析し、viewBox(2752.766×1537.631)→仮想(2000×780)へ
    // スケールして1枚のビットマップに焼く。それを東西ループ用に3枚（-2000/0/+2000）MapBackground に敷く。外部SVGライブラリに依存しない（描画安定）。
    private RenderTargetBitmap? _landHi, _landMid, _landLo;   // LOD（2026-09-06）：5倍／2.5倍／1倍
    private readonly Image?[] _landImgs = new Image?[2];
    private int _landLod = -1;   // 0=lo 1=mid 2=hi（-1＝未設定）

    /// <summary>倍率に応じて背景の解像度を選ぶ。画面1pxあたりの地図単位＝1/scale。焼きの倍率 ss は「地図1単位あたり ss px」なので、scale ≤ ss なら十分。</summary>
    private void ApplyLandLod()
    {
        if (_landLo is null || _landMid is null || _landHi is null) return;
        double s = _scale.ScaleX;
        int want = s <= 1.15 ? 0 : s <= 2.6 ? 1 : 2;   // 1倍で足りる帯／2.5倍／5倍（少し余裕を持たせて切替の縁でにじまない）
        if (want == _landLod) return;
        _landLod = want;
        var src = want == 0 ? _landLo : want == 1 ? _landMid : _landHi;
        foreach (var img in _landImgs) if (img is not null) img.Source = src;
        (DataContext as ViewModels.GameViewModel)?.SetMapLod(want == 0);   // 政治塗りも同じ帯で1倍の写しへ
    }

    /// <summary>焼いた絵の縮小写し（起動時に1回）。地図単位あたり ss px。</summary>
    private static RenderTargetBitmap Downscale(RenderTargetBitmap src, double ss)
    {
        int pw = (int)(MapData.MapWidth * ss), ph = (int)(MapData.MapHeight * ss);
        var rtb = new RenderTargetBitmap(new PixelSize(pw, ph), new Vector(96, 96));
        using (var ctx = rtb.CreateDrawingContext())
        using (ctx.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.HighQuality }))
            ctx.DrawImage(src, new Rect(0, 0, src.PixelSize.Width, src.PixelSize.Height), new Rect(0, 0, pw, ph));
        return rtb;
    }

    private void TryLoadWorldMap()
    {
        try
        {
            var swBake = System.Diagnostics.Stopwatch.StartNew();
            var bmp = BuildLandBitmap();
            BootLog.Write($"⏱ 背景の焼き {swBake.ElapsedMilliseconds}ms");   // A1（2026-09-05）：起動の最大コスト＝A2のキャッシュ判断の物差し
            if (bmp is null) return;
            // ★LOD（作者指示2026-09-06「マップスクロールがかなり重い」・実測＝全景のパンが 5倍焼き 174ms/フレーム・2倍 54ms＝縮小描画が支配的）：
            //   5倍の焼き（10000×3900）は寄った時だけ使い、引いた帯では同じ絵を 1倍／2.5倍へ縮小した写しを表示する（画面の画素密度を超える解像度は描かない）。
            //   写しは起動時に1回だけ作る（拡縮1回ずつ）。切替は ClampAndWrap（拡縮の全経路が通る）で倍率を見て行う。
            _landHi = bmp;
            _landMid = Downscale(bmp, 2.5);
            _landLo = Downscale(bmp, 1.0);
            for (int k = 0; k <= 1; k++)   // ★2026-08-02：世界の複製は**2枚で足りる**。ClampAndWrap が translate.X を (−period, 0] に折り返し、period = 2000×scale ≥ viewport
            {
                var img = new Image { Source = _landLo, Width = MapData.MapWidth, Height = MapData.MapHeight, Stretch = Stretch.Fill };
                RenderOptions.SetBitmapInterpolationMode(img, Program.LandInterp == "LowQuality" ? BitmapInterpolationMode.LowQuality : Program.LandInterp == "MediumQuality" ? BitmapInterpolationMode.MediumQuality : BitmapInterpolationMode.HighQuality);
                Canvas.SetLeft(img, k * MapData.MapWidth);
                Canvas.SetTop(img, 0);
                MapBackground.Children.Add(img);
                _landImgs[k] = img;
            }
            _landLod = 0;
            ApplyLandLod();
        }
        catch { /* 背景が出なくても○とゲームは動く（致命的にしない） */ }
    }

    private static RenderTargetBitmap? BuildLandBitmap()
    {
        const double vbW = 2752.766, vbH = 1537.631;   // worldmap.svg の viewBox
        using var stream = AssetLoader.Open(new Uri("avares://PriceOfBread.App/Assets/worldmap.svg"));
        using var reader = new StreamReader(stream);
        var doc = XDocument.Parse(reader.ReadToEnd());

        // 陸＝明るいタン／湖＝海色。○（彩度高）はどちらの上でも映える。
        // ★2026-08-31作者指示：**国境線を消す**＝SVGは国ごとのパスなので、旧「海岸線の薄い暗線」は実際には現代の国境として
        //   見えていた（1783年の世界に現代国境は不適）。線は**陸と同色の縫い目埋め**へ＝隣接国パス間の1px隙間（AAシーム）を塞ぎつつ
        //   国境は不可視。海岸線は陸/海の色境界として自然に残る。
        var land = new SolidColorBrush(Color.FromRgb(0xAE, 0xA0, 0x76));
        var water = new SolidColorBrush(Color.FromRgb(0x16, 0x31, 0x4E));   // 湖は海と同色
        var coast = new Pen(land, 1.2);   // 同色ペン＝縫い目埋め（国境・海岸線の線は描かない）

        // 背景SVG自体の四隅の緯度（実測較正）。表示は MapData.LatTop..LatBottom にクロップするので、その緯度帯の viewBox y スライスだけ焼く（上下の空白除去）。
        const double svgLatTop = 98.0, svgLatBot = -100.0;
        double sliceTop = (svgLatTop - MapData.LatTop) / (svgLatTop - svgLatBot) * vbH;   // クロップ上端の viewBox y
        double sliceBot = (svgLatTop - MapData.LatBottom) / (svgLatTop - svgLatBot) * vbH;
        double sliceH = sliceBot - sliceTop;

        double ss = Program.LandSS;   // スーパーサンプル：既定5＝表示の5倍解像度で焼く（計測腕 --perf-ss で切替・2026-09-06）（約10000×3900・RAM≈156MB）。★3→5（2026-08-31作者指示＝ズーム上限12倍化でジャギー）
                                 //   ベクター源泉なので焼き解像度は定数1つ＝毎フレームコストは不変（GPUの1枚絵拡縮）・増えるのはロード時の焼きとRAMのみ
        int pw = (int)(MapData.MapWidth * ss), ph = (int)(MapData.MapHeight * ss);
        var rtb = new RenderTargetBitmap(new PixelSize(pw, ph), new Vector(96, 96));
        double scaleX = pw / vbW, scaleY = ph / sliceH;
        int drawn;
        using (var ctx = rtb.CreateDrawingContext())
        using (ctx.PushTransform(Matrix.CreateScale(scaleX, scaleY) * Matrix.CreateTranslation(0, -sliceTop * scaleY)))
            drawn = DrawLandPaths(ctx, doc, land, water, coast);
        if (drawn > 0) CaptureDrawnLandMask(rtb, pw, ph);   // 政治の塗り分けの型紙＝いま焼いた絵から陸を読む（2026-09-02）
        return drawn > 0 ? rtb : null;
    }

    /// <summary>SVG の陸/湖パスを1本ずつ描く（背景の焼きと陸マスクの焼き直しで**同じ**描画器を使う＝見えている陸と判定の陸が一致する）。戻り＝描いた本数。</summary>
    private static int DrawLandPaths(DrawingContext ctx, XDocument doc, IBrush land, IBrush water, Pen? coast)
    {
        int drawn = 0;
            foreach (var e in doc.Descendants().Where(e => e.Name.LocalName == "path"))
            {
                var d = (string?)e.Attribute("d");
                if (string.IsNullOrWhiteSpace(d)) continue;
                // クラスは path 自身か祖先 <g> にある。このSVGは多くの国を <g class="land coast jp"><path d=.../></g> で持つ
                // （子pathは無クラス）一方で中国等は <path class="land cn"> 直書き。両対応のため自身→祖先へ遡って最初のクラスを採る。
                string cls = "";
                for (XElement? n = e; n != null; n = n.Parent)
                {
                    var c = (string?)n.Attribute("class");
                    if (!string.IsNullOrEmpty(c)) { cls = c; break; }
                }
                if (cls.Contains("ocean") || cls.Contains("circle")) continue;   // 海の背景／極小島嶼の○マーカーは描かない
                bool isLake = cls.Contains("lake");
                // class付きで land/lake 以外（境界線等）は除外。**class無しのパス**（例：transform持ちのスーダン id="sd"）は陸として描く＝取りこぼし防止。
                if (cls.Length > 0 && !isLake && !cls.Contains("land")) continue;
                Geometry g;
                try { g = Geometry.Parse(d); } catch { continue; }
                var brush = isLake ? water : land;
                var pen = isLake ? null : coast;
                // 一部の国（例：スーダン）は path/祖先に transform="matrix(...)" を持つ＝独自座標系。DrawGeometry は Geometry.Transform を見ないので、
                // その場で PushTransform して描く（地図全体スケールに合成）。適用しないと位置がずれて消える。
                var tm = CumulativeTransform(e);
                if (tm.IsIdentity) ctx.DrawGeometry(brush, pen, g);
                else using (ctx.PushTransform(tm)) ctx.DrawGeometry(brush, pen, g);
                drawn++;
            }
                return drawn;
    }

    /// <summary>★陸マスクの焼き直し（2026-09-05）：`--bakemask <out.bin>`。0.1°格子（3600×1800）へ SVG を等距円筒で描き、
    /// 赤>青を陸として読み、海岸線を1格子収縮（4近傍のどれかが海なら陸としない＝SimCore/Landmask.cs の規約）して LSB 先で詰める。
    /// SVG の四隅緯度（svgLatTop=98/svgLatBot=−100）と経度（MapData.LonLeft..LonRight）は BuildLandBitmap と同じ較正値。
    /// 経度 180 を超える右端（〜193.6°）は −180 側へ折り返して2回描く。</summary>
    public static void BakeLandmask(string outPath, bool forRoads = false)
    {
        // forRoads（2026-09-05）＝**陸路用**の陸マスク：多数決・**収縮なし**（any-coverage は群島の湾を陸続きにするので使わない）。海岸線の収縮は海路（沿岸をかすめる航路）の
        //   ためのもので、陸路の判定に使うと幅1〜2格子の地峡（諫早・クラ）が消えて半島が島になる（実測＝長崎が北九州から届かない）。
        //   海路用（既定）は従来どおり多数決＋収縮1格子。
        const double vbW = 2752.766, vbH = 1537.631, svgLatTop = 98.0, svgLatBot = -100.0;
        const int Nx = 3600, Ny = 1800; const double Step = 0.1;
        using var stream = AssetLoader.Open(new Uri("avares://PriceOfBread.App/Assets/worldmap.svg"));
        using var reader = new StreamReader(stream);
        var doc = XDocument.Parse(reader.ReadToEnd());
        var land = new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0x00));
        var water = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0xFF));
        var rtb = new RenderTargetBitmap(new PixelSize(Nx, Ny), new Vector(96, 96));
        // SVG座標→格子：lon = LonLeft + x/vbW×360 → px = (lon+180)/Step；lat = svgLatTop − y/vbH×(svgLatTop−svgLatBot) → py = (90−lat)/Step
        double sx = 360.0 / vbW / Step, sy = (svgLatTop - svgLatBot) / vbH / Step;
        double tx = (MapData.LonLeft + 180.0) / Step, ty = (90.0 - svgLatTop) / Step;
        using (var ctx = rtb.CreateDrawingContext())
        {
            ctx.FillRectangle(water, new Rect(0, 0, Nx, Ny));
            foreach (double shift in new[] { 0.0, -Nx })   // 右端（lon>180）の折り返し
                using (ctx.PushTransform(Matrix.CreateScale(sx, sy) * Matrix.CreateTranslation(tx + shift, ty)))
                    DrawLandPaths(ctx, doc, land, water, null);
        }
        var raw = new byte[Nx * Ny];
        int stride = Nx * 4; var row = new byte[stride];
        var buf = System.Runtime.InteropServices.Marshal.AllocHGlobal(stride);
        try
        {
            for (int iy = 0; iy < Ny; iy++)
            {
                rtb.CopyPixels(new PixelRect(0, iy, Nx, 1), buf, stride, stride);
                System.Runtime.InteropServices.Marshal.Copy(buf, row, 0, stride);
                // BGRA：**赤が少しでも載れば陸**（AAの部分被覆も陸）。格子（11km）より狭い海峡（ジブラルタル14km・ズンド4km・ベルト16km）は
                //   両岸の被覆で潰れて陸になる＝旧マスクと同じ性質（Landmask.cs の注記「海峡は passages.json の通路が担う」）。次の収縮1格子で海岸線は戻る。
                for (int ix = 0; ix < Nx; ix++) raw[iy * Nx + ix] = row[ix * 4 + 2] > row[ix * 4] ? (byte)1 : (byte)0;   // 多数決（赤>青）。⚠陸路用も多数決＝「少しでも赤なら陸」は群島の湾（ゴナーヴ湾・クヴァルネル湾・インサイドパッセージ）を陸続きにした（実測2026-09-05）
            }
        }
        finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(buf); }
        var bits = new byte[(Nx * Ny + 7) / 8];
        int landCells = 0;
        for (int iy = 0; iy < Ny; iy++)
            for (int ix = 0; ix < Nx; ix++)
            {
                if (raw[iy * Nx + ix] == 0) continue;
                bool n = iy > 0 && raw[(iy - 1) * Nx + ix] != 0, s2 = iy < Ny - 1 && raw[(iy + 1) * Nx + ix] != 0;
                bool w = raw[iy * Nx + (ix + Nx - 1) % Nx] != 0, e = raw[iy * Nx + (ix + 1) % Nx] != 0;
                if (!forRoads && !(n && s2 && w && e)) continue;   // 収縮1格子（海路用のみ。極の行は隣が無い側を海扱い＝航行しないので影響なし）
                int idx = iy * Nx + ix; bits[idx >> 3] |= (byte)(1 << (idx & 7)); landCells++;
            }
        File.WriteAllBytes(outPath, bits);
        BootLog.Write($"bakemask: {outPath} 陸セル {landCells}");
    }

    // 焼いた背景から「描かれている陸」を MapFill の格子へ読み出す。陸色は赤>青・海/湖色は青>赤＝1本の判別式で足りる
    // （中間色のアンチエイリアス画素も濃い側へ倒れる）。⚠取れなければ何もしない＝塗りは陸マスクだけで出る（湖が薄く染まる）。
    private static void CaptureDrawnLandMask(RenderTargetBitmap rtb, int pw, int ph)
    {
        try
        {
            var mask = new byte[MapFill.Nx * MapFill.Ny];
            int stride = pw * 4;
            var row = new byte[stride];
            var buf = System.Runtime.InteropServices.Marshal.AllocHGlobal(stride);
            try
            {
                for (int iy = 0; iy < MapFill.Ny; iy++)
                {
                    int py = Math.Min(ph - 1, (int)((iy + 0.5) / MapFill.Ny * ph));
                    rtb.CopyPixels(new PixelRect(0, py, pw, 1), buf, stride, stride);
                    System.Runtime.InteropServices.Marshal.Copy(buf, row, 0, stride);
                    int mrow = iy * MapFill.Nx;
                    for (int ix = 0; ix < MapFill.Nx; ix++)
                    {
                        int o = Math.Min(pw - 1, (int)((ix + 0.5) / MapFill.Nx * pw)) * 4;   // BGRA
                        mask[mrow + ix] = row[o + 2] > row[o] ? (byte)1 : (byte)0;
                    }
                }
            }
            finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(buf); }
            MapFill.SetDrawnLand(mask);
        }
        catch { }
    }

    // path 自身→祖先の transform を合成（SVG。row-vector で path を先に適用＝M = Tpath * Tparent * …）。worldmap.svg は matrix(...) のみだが translate/scale も解釈。
    private static Matrix CumulativeTransform(XElement e)
    {
        var m = Matrix.Identity;
        for (XElement? n = e; n != null; n = n.Parent)
        {
            var t = (string?)n.Attribute("transform");
            if (!string.IsNullOrEmpty(t)) m *= ParseTransform(t);
        }
        return m;
    }

    private static Matrix ParseTransform(string t)
    {
        static double D(string s) => double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
        static string[] Nums(string g) => g.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var mm = System.Text.RegularExpressions.Regex.Match(t, @"matrix\(([^)]+)\)");
        if (mm.Success) { var p = Nums(mm.Groups[1].Value); if (p.Length == 6) return new Matrix(D(p[0]), D(p[1]), D(p[2]), D(p[3]), D(p[4]), D(p[5])); }
        var tr = System.Text.RegularExpressions.Regex.Match(t, @"translate\(([^)]+)\)");
        if (tr.Success) { var p = Nums(tr.Groups[1].Value); return Matrix.CreateTranslation(D(p[0]), p.Length > 1 ? D(p[1]) : 0); }
        var sc = System.Text.RegularExpressions.Regex.Match(t, @"scale\(([^)]+)\)");
        if (sc.Success) { var p = Nums(sc.Groups[1].Value); return Matrix.CreateScale(D(p[0]), p.Length > 1 ? D(p[1]) : D(p[0])); }
        return Matrix.Identity;
    }

    // 最小倍率：マップが縦に画面を満たし、かつ横方向もループ用に viewport 幅を超える倍率（max(幅フィット, 高さフィット)）。
    /// <summary>全景（世界全体が入る倍率）へ引く。無人撮影が「州名が消えて国名が出る帯」を必ず撮れるようにするための入口
    /// （2026-09-02＝初期倍率は窓の大きさで決まり、実行ごとに閾値の上下どちらにも転ぶため）。ホイールのズームと同じ経路を使う。</summary>
    public void ZoomToWorld()
    {
        double s = MinScale();
        _scale.ScaleX = _scale.ScaleY = s;
        _translate.X = MapHost.Bounds.Width / 2 - MapData.MapWidth * s / 2;
        _translate.Y = 0;
        ClampAndWrap();
        (DataContext as ViewModels.GameViewModel)?.SetDotZoom(s, immediate: true);
    }

    // uishot 用（2026-09-05・B4検証）：画面中心を保ったままフィット倍率の factor 倍へ＝州名の帯を決定的に撮る。
    public void SetZoomFactor(double factorOfFit)
    {
        double s0 = _scale.ScaleX, s = MinScale() * factorOfFit;
        double cx = (MapHost.Bounds.Width / 2 - _translate.X) / s0, cy = (MapHost.Bounds.Height / 2 - _translate.Y) / s0;
        _scale.ScaleX = _scale.ScaleY = s;
        _translate.X = MapHost.Bounds.Width / 2 - cx * s;
        _translate.Y = MapHost.Bounds.Height / 2 - cy * s;
        ClampAndWrap();
        (DataContext as ViewModels.GameViewModel)?.SetDotZoom(s, immediate: true);
    }

    // 計測用（2026-09-06）：パン試験＝translate を動かす（ユーザーのドラッグと同じ経路）。
    public void PanBy(double dx, double dy) { _translate.X += dx; _translate.Y += dy; ClampAndWrap(); }

    private double MinScale()
        => Math.Max(MapHost.Bounds.Width / MapData.MapWidth, MapHost.Bounds.Height / MapData.MapHeight);

    private void OnHostSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (MapHost.Bounds.Width < 1 || MapHost.Bounds.Height < 1) return;
        (DataContext as ViewModels.GameViewModel)?.SetMapFitScale(MinScale());   // ウィンドウサイズでフィット倍率が変わる＝地名閾値も追従
        if (!_fitted)
        {
            double s = MinScale();
            _scale.ScaleX = _scale.ScaleY = s;
            _translate.X = MapHost.Bounds.Width / 2 - MapData.MapWidth * s / 2;   // 経度0を中央に
            _translate.Y = 0;
            _fitted = true;
        }
        else if (_scale.ScaleX < MinScale())   // ウィンドウ拡大で最小倍率を割ったら引き上げ
            _scale.ScaleX = _scale.ScaleY = MinScale();
        ClampAndWrap();
    }

    // ホイール＝カーソル位置を固定点にしたズーム。
    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (e.Delta.Y == 0) return;   // 横スクロール（タッチパッドの横方向）ではズームしない
        var p = e.GetPosition(MapHost);
        double target = _scale.ScaleX * (e.Delta.Y > 0 ? 1.15 : 1.0 / 1.15);
        double newScale = Math.Clamp(target, MinScale(), MaxScale);
        double factor = newScale / _scale.ScaleX;
        _translate.X = p.X - (p.X - _translate.X) * factor;   // カーソル下の内容点を固定
        _translate.Y = p.Y - (p.Y - _translate.Y) * factor;
        _scale.ScaleX = _scale.ScaleY = newScale;
        ClampAndWrap();
        if (DataContext is ViewModels.GameViewModel vmz) vmz.SetDotZoom(newScale);   // 州○のズーム補正（拡大しても○が巨大化しない）
        e.Handled = true;
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        MapHost.Focus();   // 地図のどこかを押すたびにフォーカスを確保＝スペース/数字キーが効く
        if (e.Source is Ellipse or Button) return;   // 州○の押下はクリック扱い（パンしない）
        if (!e.GetCurrentPoint(MapHost).Properties.IsLeftButtonPressed) return;
        _panning = true;
        _last = e.GetPosition(MapHost);
        e.Pointer.Capture(MapHost);
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        // ○から離れたら左上のホバーパネルを消す（残骸を残さない）。
        // ★2026-08-10（作者報告「国にマウスを当てた時、円の縁で表示が出たり消えたりする」）：
        //   旧は「Source が Ellipse でなければ即消す」で、当たり判定の境界＝円の縁ではポインタの微動が
        //   内/外を1pxごとに跨ぐたび PointerEntered（出す）と PointerMoved（消す）が交互に走っていた。
        //   出す線と消す線を分ける＝①いま○の上なら出す（同じ○なら同じ実体の代入＝通知も再描画も起きない）
        //   ②直前の○の外周から HoverExitMargin px 離れて初めて消す。境界に幅がある限り点滅は構造的に起きない。
        //   判定は円の実体（Ellipse）のローカル座標＝地図の東西ループ・ズームに自然に追従する。
        if (DataContext is ViewModels.GameViewModel vmm)
        {
            if (e.Source is Ellipse over && over.DataContext is ViewModels.ProvinceDot od)
            {
                if (!vmm.HoverPinned) vmm.MapHover = od.Hover;   // 重なった○の間を渡っても切れない（持ち替える）。ピン留め中は固定
                _hoverDot = over;
            }
            else if (vmm.MapHover is not null && !vmm.HoverPinned && LeftHoverDot(e))
            {
                vmm.MapHover = null;
                _hoverDot = null;
            }
        }
        if (!_panning) return;
        var cur = e.GetPosition(MapHost);
        _translate.X += cur.X - _last.X;
        _translate.Y += cur.Y - _last.Y;
        _last = cur;
        ClampAndWrap();
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        _panning = false;
        e.Pointer.Capture(null);
    }

    // 縦＝マップ外（極より上下）に出さないクランプ。横＝東西ループ（translate.X を地図1周期で折り返す。州○は2コピーなので継ぎ目なし）。
    private void ClampAndWrap()
    {
        double s = _scale.ScaleX;
        double mapHpx = MapData.MapHeight * s, mapWpx = MapData.MapWidth * s;
        double hostH = MapHost.Bounds.Height, hostW = MapHost.Bounds.Width;
        if (hostH < 1 || hostW < 1) return;

        // 縦クランプ：地図が画面より高ければ端を越えさせない／低ければ中央寄せ（通常は MinScale で高さは満たされる）。
        _translate.Y = mapHpx <= hostH ? (hostH - mapHpx) / 2 : Math.Clamp(_translate.Y, hostH - mapHpx, 0);

        // 横ループ：translate.X を (-周期, 0] に折り返す。2コピー描画と周期≥viewport幅で常に継ぎ目なく覆う。
        double period = mapWpx;
        double tx = _translate.X % period;
        if (tx > 0) tx -= period;
        _translate.X = tx;
        ApplyLandLod();   // ★LOD（2026-09-06）：拡縮の全経路がここを通る
    }
}
