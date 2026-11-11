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
// Views/MapPane.axaml.cs ＝ 地図の欄（(501) 段2a：GameView.axaml.cs から地図の部分を**そのまま**移した。挙動は不変）。
// 地図中心UI（2026-06-29 再構成）：MapRoot（仮想2000×780＝MapData.MapWidth×MapHeight）に拡縮(Scale)＋移動(Translate)の RenderTransform を当てる。
// ホイール=カーソル中心ズーム／背景ドラッグ=パン。**縦はマップ外に出ない（クランプ）／横は東西ループ（折り返し）**。
// ★F7（2026-09-28）：州○を廃止＝州のホバー・クリックは**塗り分けのセル**で当てる（GameViewModel.MapHitAt）。押下は常にパンの始まりで、
//   動かさずに離したときだけクリック（州パネルのピン留め／他国植民地の狙い）。横ループは VM が層を2コピー描き、ここで translate.X を周期で折り返して継ぎ目なく繋ぐ。
public partial class MapPane : UserControl
{
    private readonly ScaleTransform _scale = new(1, 1);
    private readonly TranslateTransform _translate = new(0, 0);
    private bool _panning, _fitted;
    private Point _last;
    private const double MaxScale = 12.0;   // 過剰ズームの防止（20→6・2026-07-05）。★6→12（2026-08-31作者指示「解像度2倍＝2倍ズーム」）

    public MapPane()
    {
        InitializeComponent();
        MapRoot.RenderTransform = new TransformGroup { Children = { _scale, _translate } };
        MapRoot.RenderTransformOrigin = RelativePoint.TopLeft;
        MapHost.PointerWheelChanged += OnWheel;
        MapHost.PointerPressed += OnPressed;
        MapHost.PointerMoved += OnMoved;
        MapHost.PointerReleased += OnReleased;
        MapHost.PointerCaptureLost += (_, _) => _panning = false;   // 窓の外で離した・別のアプリへ移った＝離した知らせが来ない。パンが残るとホバーが効かなくなる
        MapHost.SizeChanged += OnHostSizeChanged;
        MapHost.PointerExited += (_, _) => { if (DataContext is ViewModels.GameViewModel vmx && !vmx.HoverPinned) vmx.MapHover = null; };   // 地図から離れたら左上パネルを閉じる
        MapHost.Focusable = true;
        Loaded += OnMapLoaded;
        Unloaded += OnMapUnloaded;
        TryLoadWorldMap();
    }

    /// <summary>地図の箱の寸法と、自国の首都が箱のどこに映っているか（撮影の確かめ・設計 §9「地図の箱の大きさ」）。HomeView 後は首都が箱の中心に来る＝差が数画素以内なら、拡縮・パン・当たりが読む箱の寸法は箱に合っている。
    /// 横は東西ループ（周期 = 地図の幅 × 倍率）で折り返されるので、差は周期で最も近い値に直して出す。</summary>
    public string BoxReport()
    {
        double w = MapHost.Bounds.Width, h = MapHost.Bounds.Height;
        if (DataContext is not ViewModels.GameViewModel vm || vm.PlayerCapitalCanvas() is not { } cap) return $"地図の箱 {w:F0}×{h:F0}・首都なし";
        double s = _scale.ScaleX, period = MapData.MapWidth * s;
        double sx = cap.X * s + _translate.X, sy = cap.Y * s + _translate.Y;
        double dx = (sx - w / 2) % period; if (dx > period / 2) dx -= period; if (dx < -period / 2) dx += period;
        double dy = sy - h / 2;
        // 縦は地図が極を越えないクランプ（ClampAndWrap）で、高緯度の首都は箱の中心まで寄り切れない＝クランプの端に当たっていればずれではない
        bool yClamped = Math.Abs(_translate.Y) < 1e-6 || Math.Abs(_translate.Y - (h - MapData.MapHeight * s)) < 1e-6;
        string verdict = Math.Abs(dx) > 3 ? "　横が中心からずれている" : Math.Abs(dy) <= 3 ? "　中心に合っている" : yClamped ? "　横は中心・縦はクランプの端で寄り切れない" : "　縦が中心からずれている";
        return $"地図の箱 {w:F0}×{h:F0}・倍率 {s:F2}・首都の画面座標 ({sx:F0},{sy:F0})・箱の中心 ({w / 2:F0},{h / 2:F0})・差 ({dx:F1},{dy:F1}){verdict}";
    }

    /// <summary>地図の箱にキーの焦点を置く（スペースや数字キーが効く）。</summary>
    public void FocusHost() => MapHost.Focus();

    private ViewModels.GameViewModel? _vmHooked;   // CenterMap を購読したVM（解放用）

    private bool _homed;   // 自国センタリングを一度だけ実行
    private Size? _lastBox;   // 直前の箱の大きさ（大きさが変わったとき中心を保つ）

    private void OnMapLoaded(object? sender, RoutedEventArgs e)
    {
        // ツリー構築直後だと Border.Focus() が通らないことがあるので遅延実行。
        Dispatcher.UIThread.Post(() => MapHost.Focus(), DispatcherPriority.Loaded);
        if (_vmHooked is null && DataContext is ViewModels.GameViewModel vm) { _vmHooked = vm; vm.CenterMap += OnCenterMap; }
        Dispatcher.UIThread.Post(HomeView, DispatcherPriority.Background);   // レイアウト確定後に自国を中央へ
    }

    private void OnMapUnloaded(object? sender, RoutedEventArgs e)
    {
        _panning = false;
        if (_vmHooked is not null) { _vmHooked.CenterMap -= OnCenterMap; _vmHooked = null; }
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
        (DataContext as ViewModels.GameViewModel)?.SetMapZoom(s, immediate: true);   // 即時補正＝初回描画（_mapZoom=1）と初期ズームの間の「一瞬大きい文字」を根絶。首都なし分岐でも適用
        _homed = true;
    }

    // 州ホバー：詳細を地図左上の固定パネルへ（ツールチップより長いリストを出せる）。
    // ★F7（2026-09-28）：当たりは塗り分けのセル＝カーソルの下に塗られている州／植民地（塗りと同じ割り当て・VM の MapHitAt）。
    //   州と州の境は塗りの境そのもの＝隣へ移れば持ち替え、海・塗られていない陸へ出れば閉じる（旧・○の縁の点滅止めの幅は不要＝境に隙間が無い）。
    private const double ClickSlop = 5.0;   // 押してから離すまでにこれ以内しか動かなければクリック（画面px）＝ドラッグのパンと分ける。値は旧・○のホバーの離脱幅を引き継ぐ
    private Point _pressAt;

    private void HoverAt(PointerEventArgs e)
    {
        if (DataContext is not ViewModels.GameViewModel vm || vm.HoverPinned) return;   // ピン留め中は持ち替えない（§7）
        var p = e.GetPosition(MapRoot);   // キャンバス座標（拡縮・移動を織り込む）
        var hover = vm.MapHitAt(p.X, p.Y).Hover;
        if (!ReferenceEquals(vm.MapHover, hover)) vm.MapHover = hover;
    }

    // クリック（動かさずに離した）：他国の植民地＝狙いのトグル（02断線走査2026-07-28＝講和の優先割譲 Colony.PlayerInterest の書き手）。
    //   州＝左クリックで州パネルをピン留め（同じ州でもう一度＝解除・2026-09-07）。右クリックは植民地の狙いだけ（旧・植民地○と同じ）。
    private void ClickAt(PointerEventArgs e, bool left)
    {
        if (DataContext is not ViewModels.GameViewModel vm) return;
        var p = e.GetPosition(MapRoot);
        var (hover, region, aim) = vm.MapHitAt(p.X, p.Y);
        if (aim is not null) vm.ToggleColonyInterest(aim);
        else if (left && region is not null && hover is not null) vm.TogglePin(hover, region);   // 鍵は州＝四半期の作り直しに耐える
    }

    // 外交の行クリック：その国の座標（仮想キャンバス）が画面中央に来るよう translate を合わせる（ズームは維持）。
    private void OnCenterMap(double x, double y)
    {
        double s = _scale.ScaleX;
        _translate.X = MapHost.Bounds.Width / 2 - x * s;
        _translate.Y = MapHost.Bounds.Height / 2 - y * s;
        ClampAndWrap();
    }

    // スライダーの硬い上下限は Views/SliderLimit.cs（添付プロパティ）。

    // 生産ページ（ProdScroll のパン・ノード選択）のハンドラは Views/Pages/ProductionPage.axaml.cs にある。

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
        catch { /* 背景が出なくても地図の他の層とゲームは動く（致命的にしない） */ }
    }

    private static RenderTargetBitmap? BuildLandBitmap()
    {
        const double vbW = 2752.766, vbH = 1537.631;   // worldmap.svg の viewBox
        using var stream = AssetLoader.Open(new Uri("avares://PriceOfBread.App/Assets/worldmap.svg"));
        using var reader = new StreamReader(stream);
        var doc = XDocument.Parse(reader.ReadToEnd());

        // 陸＝明るいタン／湖＝海色。
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
        (DataContext as ViewModels.GameViewModel)?.SetMapZoom(s, immediate: true);
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
        (DataContext as ViewModels.GameViewModel)?.SetMapZoom(s, immediate: true);
    }

    // 計測用（2026-09-06）：パン試験＝translate を動かす（ユーザーのドラッグと同じ経路）。
    public void PanBy(double dx, double dy) { _translate.X += dx; _translate.Y += dy; ClampAndWrap(); }

    private double MinScale()
        => Math.Max(MapHost.Bounds.Width / MapData.MapWidth, MapHost.Bounds.Height / MapData.MapHeight);

    private void OnHostSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (MapHost.Bounds.Width < 1 || MapHost.Bounds.Height < 1) return;
        // 箱の大きさが変わっても、映している中心は動かさない（地図が欄に畳まれて広さが変わる・分割の境目を動かす）。最初の大きさの決まりは HomeView が持つ。
        if (_homed && _lastBox is { } prev) { _translate.X += (MapHost.Bounds.Width - prev.Width) / 2; _translate.Y += (MapHost.Bounds.Height - prev.Height) / 2; }
        _lastBox = MapHost.Bounds.Size;
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
        if (DataContext is ViewModels.GameViewModel vmz) vmz.SetMapZoom(newScale);   // 文字のズーム補正（拡大しても文字が巨大化しない）
        e.Handled = true;
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        MapHost.Focus();   // 地図のどこかを押すたびにフォーカスを確保＝スペース/数字キーが効く
        if (e.Source is Button) return;
        _pressAt = e.GetPosition(MapHost);
        if (!e.GetCurrentPoint(MapHost).Properties.IsLeftButtonPressed)
        {
            ClickAt(e, left: false);   // 右/中ボタン＝パンしない＝押した時点でクリック（植民地の狙いだけ）
            return;
        }
        _panning = true;
        _last = _pressAt;
        e.Pointer.Capture(MapHost);
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_panning && !e.GetCurrentPoint(MapHost).Properties.IsLeftButtonPressed) _panning = false;   // 押していないのにパン中＝離しを取り逃した
        if (!_panning) { HoverAt(e); return; }   // パン中は持ち替えない（地図が動いているだけ）
        var cur = e.GetPosition(MapHost);
        _translate.X += cur.X - _last.X;
        _translate.Y += cur.Y - _last.Y;
        _last = cur;
        ClampAndWrap();
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_panning)
        {
            var d = e.GetPosition(MapHost) - _pressAt;
            if (Math.Abs(d.X) <= ClickSlop && Math.Abs(d.Y) <= ClickSlop) ClickAt(e, left: true);
        }
        _panning = false;
        e.Pointer.Capture(null);
    }

    // 縦＝マップ外（極より上下）に出さないクランプ。横＝東西ループ（translate.X を地図1周期で折り返す。層は2コピーなので継ぎ目なし）。
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
