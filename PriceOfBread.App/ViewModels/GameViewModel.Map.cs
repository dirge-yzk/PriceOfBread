// GameViewModel.Map.cs ＝ 地図＝州○/ラベル/経路グラフの構築とホバー詳細。
// ★2026-08-10のUI分割：GameViewModel.cs から**メンバーを移しただけ**（ロジック・順序とも不変）。
//   同じ partial class GameViewModel の一部＝どのファイルからでも全メンバーに触れる。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    // 地図中心UI（2026-06-29 再構成）：全国の active 州を○として配置（所有国の色・人口で大小・自国は白縁で強調）。
    // 座標は MapData.CityCoords（実在都市の緯度経度）→等距円筒投影。未登録の都市は地図に出さない。
    // 州○のズーム補正：拡大しても○が画面上で巨大化しないよう、ズーム率で直径を割る（縮小側は1でクランプ＝設計サイズ維持）。
    // 再構築はデバウンス（ホイール停止後に1回だけ・OnTickで消化）＝ズームの重さの主因を除去。
    private double _dotZoom = 1.0;
    private double _pendingZoom = 1.0;
    private bool _zoomDirty;
    private long _zoomStamp;
    public void SetDotZoom(double scale, bool immediate = false)
    {
        _pendingZoom = System.Math.Max(1.0, scale);
        if (immediate)   // 初期カメラ設定（HomeView）用：デバウンスを飛ばして即時補正＝起動の一瞬だけ○が大きく見えるちらつきを根絶（2026-07-26ユーザー指摘）
        {
            _zoomDirty = false;
            _dotZoom = _pendingZoom;
            RefreshMap(zoomOnly: true);
            return;
        }
        if (System.Math.Abs(_pendingZoom - _dotZoom) / _dotZoom < 0.10) return;
        _zoomDirty = true;
        _zoomStamp = System.Environment.TickCount64;
    }
    private void ConsumeZoom()
    {
        // ★2026-08-31軽量化（作者報告「時間が動いている際のズームが重い」）：走行中は再構築の間合いを広げる（シムがコアを食う裏で
        //   ○600個の作り直しを連打しない）。ズームの見た目自体は変換行列で即時＝この待ちは「○の大きさ補正と地名の出し直し」だけ。
        long wait = Speed > 0 ? 300 : 120;
        if (!_zoomDirty || System.Environment.TickCount64 - _zoomStamp < wait) return;   // ホイールが止まってから反映
        _zoomDirty = false;
        _dotZoom = _pendingZoom;
        RefreshMap(zoomOnly: true);
    }

    // 州ホバーの「世界の n%」の分母＝財別の世界計。⚠州ごとに組み直すと 162州 × 全モデル国 × 全州 × 全財 の走査になる＝
    // ホバー詳細を作り直す時（RefreshMap の非ズーム）に**1回だけ**組んで全州で使い回す。
    private double[]? _worldProdByGood;   // 生産＝全モデル国の active 州の Market.LastProduced 合算（月次・数量）
    private double[]? _worldExpByGood;    // 輸出＝全モデル国の World.ExportsByGood 合算（四半期・世界価格建ての額）
    private double _worldProdValueTotal;  // 上の生産量を参照価格で額にした総和（幻の生産の足切りの母数）
    private double _worldExpTotal;        // 上の輸出額の総和（同・輸出側の母数）

    // ホバー詳細のキャッシュ：ズーム再構築では再計算しない（内容は四半期でしか変わらない）。
    private readonly Dictionary<Region, MapHoverInfo> _provHoverCache = new();
    private readonly Dictionary<Colony, MapHoverInfo> _colHoverCache = new();
    private readonly Dictionary<string, List<Chokepoint>> _cpByHost = new();
    // ★2026-09-03（キューA-13／出典 トリアージ 2-A 11-6）：巨大建造物の host 州名→情報。
    //   ⚠建造物は「地理に固定され所有は政治で動く」物なのに、地図から完全に読めなかった（生産タブの帯は自国の建設地しか出さない）。
    //   _cpByHost（海峡/運河）と同じ多値辞書＝同じ州に複数建つ場合も落とさない。
    private readonly Dictionary<string, List<Megaproject>> _megaByHost = new();   // 海峡/運河/建造物の host 州名→情報（州ホバーに統合表示）。**多値**＝同一 host に複数の個体が載る（2026-07-28：旧 1対1 辞書は最後の1件で静かに上書きしていた）
    private double _zd = 1.0;   // サイズ用ズーム係数（5.0でクランプ＝それ以上ズームしても文字/○が縮まない）
    // 02断線走査2026-07-28：講和の優先割譲（Colony.PlayerInterest）の唯一の書き手＝他国植民地○のクリックで狙いを付ける（再クリックで解除・金縁で表示）
    private SimCore.Colony? _interestColony;
    private static readonly IBrush InterestStroke = new SolidColorBrush(Color.Parse("#C9A227"));
    private static readonly IBrush HomeProvinceStroke = new SolidColorBrush(Color.FromArgb(150, 20, 14, 8));      // 共通の薄い暗縁（自国の白縁は廃止）
    private static readonly IBrush UnclaimedColonyFill = new SolidColorBrush(Color.FromArgb(70, 175, 175, 175));  // 未領有の植民地化標的（中立色）の塗り
    private static readonly IBrush UnclaimedColonyStroke = new SolidColorBrush(Color.FromArgb(200, 150, 150, 150));
    public void ToggleColonyInterest(SimCore.Colony col)
    {
        _interestColony = ReferenceEquals(_interestColony, col) ? null : col;
        RefreshMap(zoomOnly: true);
    }

    private void RefreshMap(bool zoomOnly = false)
    {
        var dots = new List<ProvinceDot>(ProvinceDots.Count + 64);
        var labels = new List<MapLabel>(MapLabels.Count + 16);
        // 地名ラベルの候補（重なり回避の貪欲配置＝○を全部積んでから確定）。
        // Tier＝0本国州／1植民地／2未領有＝場所の取り合いは本国州が先（同Tier内は Prio 降順→名前順＝決定論）。
        var labelCands = new List<(double X, double Cy, string Text, int Tier, double Prio)>();
        // 国名ラベル（2026-09-02作者指示）：州名が出ないズーム帯＝引いた全景でだけ、国名を英語で本国州の重心へ。
        // 重みは人口＝飛び地や小さな辺境に引かれず「国の重心」に乗る。
        var nationCands = new List<(double X, double Cy, string Text, double Prio, double Ext)>();   // Ext＝本国州の東西の広がり（キャンバスpx・B3の文字間隔用）
        _zd = System.Math.Min(_dotZoom, 5.0);   // ★2026-08-31：ズーム上限2倍（6→12）に合わせ○の縮小補正も2.5→5.0（それ以上は縮まない）
        if (!zoomOnly)
        {
            BuildWorldTotals();   // 州ホバーの世界シェアの分母（財別の世界計）＝ホバーを組む前に1回だけ
            _provHoverCache.Clear(); _colHoverCache.Clear();
            _cpByHost.Clear();
            foreach (var cp in _sim.Diplomacy.Chokepoints)
            {
                if (!_cpByHost.TryGetValue(cp.Host, out var lst)) _cpByHost[cp.Host] = lst = new List<Chokepoint>();
                lst.Add(cp);
            }
            _megaByHost.Clear();
            foreach (var mp in _sim.Diplomacy.Megaprojects)   // ★A-13：巨大建造物も host 州名で引けるようにする
            {
                if (!_megaByHost.TryGetValue(mp.Host, out var mlst)) _megaByHost[mp.Host] = mlst = new List<Megaproject>();
                mlst.Add(mp);
            }
            RefreshPoliticalFill();   // 政治の塗り分け（2026-09-02）＝所有が変わった時だけ焼き直す（ズームだけの更新では触らない）
        }
        // 〇の大きさ＝**ラベルの開発度と完全一致**（2026-08-31作者指示）：TotalIndustrialization を最大州で正規化した比の**線形**写像。
        //   旧＝生産力ベース×べき0.7＝表示数字と序列が食い違い得た。開始直後（工業化ほぼゼロ）は人口で代替（従来と同じ立ち上がり保護）。
        double maxInd = 1e-9, maxPop = 1e-9;
        for (int si = 0; si < _sim.Nations.Count; si++)
            foreach (var rg in _sim.Nations[si].World.Regions)
                if (rg.State == RegionState.Active && MapData.CityCoords.ContainsKey(rg.Name))
                {
                    maxInd = System.Math.Max(maxInd, rg.TotalIndustrialization);
                    maxPop = System.Math.Max(maxPop, rg.SumPopulation());
                }
        bool useProd = maxInd > 0.02;   // 世界の最大開発度が実質ゼロの開幕だけ人口比で代替
        for (int ni = 0; ni < _sim.Nations.Count; ni++)
        {
            var w = _sim.Nations[ni].World;
            string nation = w.SelfPower?.Name ?? w.Region.Name;
            // 属国は「国名（○○の属国）」形式（作者指示2026-08-03）＝地図ホバーで主権の帰属が一目で読める
            if (w.SelfPower is { } spN && _sim.Diplomacy.Suzerain(spN) is { } suzN) nation = $"{nation}({suzN.Name}の属国)";
            var baseCol = MapData.NationColor(w.SelfPower?.Name ?? w.Region.Name, ni);   // 所有国色（政体色マップモードは撤去2026-07-17）
            var fill = new SolidColorBrush(baseCol);
            var homeCoords = new List<(double Lat, double Lon)>();
            double cenX = 0, cenY = 0, cenW = 0;
            double extMinX = double.PositiveInfinity, extMaxX = double.NegativeInfinity;   // B3（2026-09-05）：国名を領土の広がりに沿わせる   // 国名ラベルの位置＝中核州の人口重み重心（植民地・植民地格の州は入れない＝ラベルが海へ流れない）
            foreach (var rg in w.Regions)
            {
                if (rg.State != RegionState.Active) continue;
                if (!MapData.CityCoords.TryGetValue(rg.Name, out var co)) continue;
                homeCoords.Add(co);
                var (cx, cy) = MapData.Project(co.Lat, co.Lon);
                double pop = rg.SumPopulation();
                double ratio = useProd ? rg.TotalIndustrialization / maxInd : pop / maxPop;   // [0,1]＝最大州で正規化（表示の開発度と同じ量）
                double d = (DotMin + (DotMax - DotMin) * System.Math.Clamp(ratio, 0.0, 1.0)) / _zd;   // ★線形＝○の大きさが数字と完全一致（ズーム補正は5.0でクランプ）
                if (!_provHoverCache.TryGetValue(rg, out var hover)) { hover = BuildProvinceHover(rg, w, nation, pop); _provHoverCache[rg] = hover; }   // ホバー詳細（ズーム時はキャッシュ）
                // 縁は直径比例の細線（塗り分けはしない＝所有国色がそのまま見える。港の情報はホバーへ）
                if (rg.Tier != RegionTier.Colonial) { cenX += cx * pop; cenY += cy * pop; cenW += pop; if (cx < extMinX) extMinX = cx; if (cx > extMaxX) extMaxX = cx; }   // 国名の位置は**中核の重心**＝植民地格の州（州へ昇格した植民地）は入れない（西・葡・蘭の名が大西洋やインド洋へ流れる）
                double dotThick = System.Math.Max(0.4, d * 0.10);
                // 東西ループ用に3コピー（-MapWidth / 0 / +MapWidth）。MapHost のクリップで可視範囲だけ描画され、パンの折り返しが継ぎ目なく繋がる。
                // ★追記2026-08-31（軽量版）：○の中の規模（人口）は**可視サイズのときだけ MapLabel を1枚追加**（ドット側の視覚は増やさない）。
                //   はみ出さない条件＝見かけのフォント7px以上（d×0.34×zoom/zd）。中央寄せは文字数×0.30em の近似。
                double sizeFont = d * 0.34;
                if (sizeFont * _dotZoom / _zd >= 7.0)
                {
                    double people = pop * 1000.0;
                    string sizeText = people >= 1e6 ? $"{people / 1e6:0.#}M" : people >= 1e3 ? $"{people / 1e3:0}k" : "";
                    if (sizeText.Length > 0)
                        labels.Add(new MapLabel
                        {
                            X = cx - sizeText.Length * sizeFont * 0.30, Y = cy - sizeFont * 0.66,
                            Text = sizeText, FontSize = sizeFont, Brush = DotSizeBrush, Tooltip = null,
                        });
                }
                for (int k = 0; k <= 1; k++)   // ★2026-08-02：世界の複製は**2枚で足りる**。ClampAndWrap が translate.X を (−period, 0] に折り返し、period = 2000×scale ≥ ウィンドウ幅なので k=−1 の複製は必ず画面外（[tx−period, tx] ⊆ (−∞,0]）＝全レイヤの要素の1/3が毎フレーム無駄だった。見た目は不変。
                    dots.Add(new ProvinceDot
                    {
                        X = cx + k * MapData.MapWidth - d / 2, Y = cy - d / 2, Diameter = d, Fill = fill,
                        Stroke = HomeProvinceStroke, StrokeThickness = dotThick, Hover = hover, Region = rg,
                    });
                // ズーム時＝州名を英語で○の右隣に（世界全景では出さない＝密集を避ける・軽量化）。ズームを引くと消える。
                // 括弧の数値＝州の開発度（項3・2026-07-27）＝工業化度×70（初期の最先進＝ロンドン0.42→29・マンチェスター→26。終盤は100超も正常）。表示のみ。
                //   ★2026-09-01：初期按分を人口シェア→indShare（産業地理の一次データ）へ＝ダブリン最先進の倒錯は解消済み。仏も0.25で点灯（パリ4.5）。
                // ⚠★2026-08-02の是正：**新旧合計（TotalIndustrialization）で表示する**。
                //   旧は rg.Industrialization だけを読んでおり、OldVintageCapital（旧世代の設備）が丸ごと抜けていた。
                //   その結果、古い設備を大量に抱えた国（清）は開発度1〜2、新しく積み直した国（日本）は147、という
                //   実態とかけ離れた表示になっていた（作者報告「清の国が軒並み開発度1になっている」）。
                //   ★TotalIndustrialization は都市化天井・衛生・スコア・投資上限・移民引力が読む「資本の総量」＝
                //     地図の開発度も明らかにそちら側である。⚠SimCore の値は1つも変えない（表示だけ）。
                // ここでは候補に積むだけ＝州ループ後に重なり回避の貪欲配置（2026-07-27ユーザー指摘「文字がかぶらないように」）
                // ★2026-09-23（作者指摘「都市名の先頭1文字が○に隠れる」）：ラベルの左端に**縁の太さ dotThick を足す**。
                //   旧 cx+d/2+2.5/_zd は○の**塗りの端**までしか送っておらず、縁とアンチエイリアスが 2.5px の隙間を食っていた
                //   （実測 gb1783/03e：パリ＝○の外端966.5・文字インク969＝隙間2.5px／グラスゴー＝隙間ほぼ0で G が縁に接する）。
                //   ⚠新しい数は作らない＝隙間の素は○と同じ dotThick（＝d×0.10）だけ。
                if (_dotZoom >= LabelThreshold)
                    labelCands.Add((cx + d / 2 + dotThick + 2.5 / _zd, cy,
                        $"{MapData.En(rg.Name)}({rg.TotalIndustrialization * 70:F1})", 0, rg.TotalIndustrialization));   // ★2026-08-31：開発度は小数1桁
            }
            if (_dotZoom < LabelThreshold && cenW > 1e-9)   // 州名が消えている帯＝入れ替わりで国名を出す（両方は出さない）
                nationCands.Add((cenX / cenW, cenY / cenW, MapData.EnNation(w.SelfPower?.Name ?? w.Region.Name), cenW, extMaxX > extMinX ? extMaxX - extMinX : 0));
            // 植民地○（本国色の中空ドット＝本国州と区別）。本国州とほぼ同位置の植民地（例：ボスポラス＝イスタンブール）は重複なので出さない。
            if (w.SelfPower is { } sp)
            {
                var colFill = new SolidColorBrush(Color.FromArgb(80, baseCol.R, baseCol.G, baseCol.B));
                var colStroke = new SolidColorBrush(baseCol);
                foreach (var col in sp.ColonyList)
                {
                    if (System.Math.Abs(col.Latitude) < 1e-9 && System.Math.Abs(col.Longitude) < 1e-9) continue;   // 座標未登録(0,0)は出さない
                    bool overlapsHome = false;
                    foreach (var hc in homeCoords)
                        if (System.Math.Abs(hc.Lat - col.Latitude) < 0.6 && System.Math.Abs(hc.Lon - col.Longitude) < 0.6) { overlapsHome = true; break; }
                    if (overlapsHome) continue;   // 本国州と重なる植民地（咽喉点等）は二重丸になるので省く
                    var (ccx, ccy) = MapData.Project(col.Latitude, col.Longitude);
                    double cd = 9.0 / _zd;
                    if (!_colHoverCache.TryGetValue(col, out var chover)) { chover = BuildColonyHover(col, $"{nation}の植民地"); _colHoverCache[col] = chover; }
                    bool aim = ReferenceEquals(col, _interestColony);   // 狙い中＝金縁（02断線走査2026-07-28）
                    double colThick = aim ? System.Math.Max(1.6, cd * 0.26) : System.Math.Max(0.8, cd * 0.13);   // 縁の太さ＝○とラベル位置の両方がこの1本を読む（同じ式を2本持たない）
                    for (int k = 0; k <= 1; k++)   // 世界の複製は2枚（州○のループと同じ理由）
                        dots.Add(new ProvinceDot
                        {
                            X = ccx + k * MapData.MapWidth - cd / 2, Y = ccy - cd / 2, Diameter = cd,
                            Fill = colFill, Stroke = aim ? InterestStroke : colStroke,
                            StrokeThickness = colThick, Hover = chover,
                            Colony = sp != _player.SelfPower ? col : null,
                        });
                    // 植民地名も本国州と同じ規則（○の右隣・同じズーム閾値・同じ重なり回避）で出す（作者要望2026-08-16）。
                    // 括弧の開発度は付けない＝植民地に州の工業化に相当する実体が無い（無い数字は書かない）。順位は人口規模。
                    if (_dotZoom >= LabelThreshold)
                        labelCands.Add((ccx + cd / 2 + colThick + 2.5 / _zd, ccy, MapData.En(col.Name), 1, col.Population));   // ★2026-09-23：州名と同じく縁の太さを吸う
                }
            }
        }
        // 未領有の植民地化標的（抽象勢力＝Nation==null）を中立色(灰)の○で表示＝「アフリカ分割」等の盤面。征服されると本国色○へ移る。
        // ★ラベルの貪欲配置より**前**に置く（後ろだと未領有の地名が候補に間に合わない）。dots への追加順は変わらない＝見た目は不変。
        foreach (var p in _sim.Diplomacy.Powers)
        {
            if (p.Nation is not null) continue;   // 実国は上で本国色＋植民地を描画済み
            foreach (var col in p.ColonyList)
            {
                if (System.Math.Abs(col.Latitude) < 1e-9 && System.Math.Abs(col.Longitude) < 1e-9) continue;
                var (gx, gy) = MapData.Project(col.Latitude, col.Longitude);
                double gd = 8.0 / _zd;
                double gThick = System.Math.Max(0.8, gd * 0.13);   // 縁の太さ＝○とラベル位置の両方がこの1本を読む（同じ式を2本持たない）
                if (!_colHoverCache.TryGetValue(col, out var ghover)) { ghover = BuildColonyHover(col, "未領有"); _colHoverCache[col] = ghover; }
                for (int k = 0; k <= 1; k++)   // 世界の複製は2枚（州○のループと同じ理由）
                    dots.Add(new ProvinceDot
                    {
                        X = gx + k * MapData.MapWidth - gd / 2, Y = gy - gd / 2, Diameter = gd,
                        Fill = UnclaimedColonyFill,
                        Stroke = UnclaimedColonyStroke, StrokeThickness = gThick,
                        Hover = ghover,
                    });
                if (_dotZoom >= LabelThreshold)   // 空白地にも地名を出す（作者要望2026-08-16）＝場所の取り合いでは本国州・植民地に譲る
                    labelCands.Add((gx + gd / 2 + gThick + 2.5 / _zd, gy, MapData.En(col.Name), 2, col.Population));   // ★2026-09-23：州名と同じく縁の太さを吸う
            }
        }
        // 地名ラベルの重なり回避（2026-07-27ユーザー指摘）：本国州（開発度の高い順）→植民地→未領有の順に置き、矩形が重なる後続をスキップ（貪欲・決定論＝同値は名前順）。
        // 判定はキャンバス座標＝ズームインするほど文字がキャンバス比で小さくなり、密集地（低地諸国・北イタリア等）もズームすれば順次現れる。
        if (labelCands.Count > 0)
        {
            double fsn = 9.5 / _zd;
            labelCands.Sort((a, b) =>
            {
                int ct = a.Tier.CompareTo(b.Tier); if (ct != 0) return ct;   // 本国州→植民地→未領有
                int c0 = b.Prio.CompareTo(a.Prio); return c0 != 0 ? c0 : string.CompareOrdinal(a.Text, b.Text);
            });
            var placed = new List<(double X, double Y, double W, double H)>();
            foreach (var cand in labelCands)
            {
                double lw = cand.Text.Length * fsn * 0.60, lh = fsn * 1.25;
                double ly = cand.Cy - fsn * 0.62;
                // ★2026-09-23：先頭1文字に**隣の州の○**が掛かっているときは、その○の右端まで送る。
                //   ⚠前の回の診断（「自分の○の縁を吸えていない」）は現物と食い違う。実測 gb1783/03e：
                //     パリは 自分の○の外端966.5・文字インク969＝式どおり隙間2.5px で全文字が出ているのに、
                //     ルアーヴルは ラベルX≈902 に対し**別の○が904.5まで伸びていて** L が丸ごと消えていた（「e Havre」）。
                //   ⚠この注記は同じ回の別の枠の変更で前提が変わった（検証担当が commit 前に訂正・2026-09-23）＝
                //     MapLabels 層は**もう ProvinceDots 層より下ではない**（GameView.axaml の ItemsSource="{Binding MapLabels}" が
                //     ProvinceDots の後ろへ移り、文字が○の上に描かれるようになった）。よって「○に食われて消える」ことは層で無くなった。
                //     それでも位置を送るのは**読みやすさのため**＝送らないと先頭文字が○の塗りの上に重なって地色とコントラストを失う。
                //   判定は**先頭1文字の箱**だけ（幅 fsn×0.60＝下の lw と同じ1文字幅）＝
                //   名前の後ろに掛かる○は避けない（送りすぎて自分の○から離れない）。送り先の隙間は基本式と同じ 2.5/_zd。
                double lx = cand.X;
                foreach (var dt in dots)
                    if (dt.Y < ly + lh && dt.Y + dt.Diameter > ly && dt.X < cand.X + fsn * 0.60 && dt.X + dt.Diameter > cand.X)
                        lx = System.Math.Max(lx, dt.X + dt.Diameter + 2.5 / _zd);   // 最も右へ伸びる○に合わせる（逐次更新しない＝○の並び順に依らず決定論）
                bool clash = false;
                foreach (var r in placed)
                    if (lx < r.X + r.W && lx + lw > r.X && ly < r.Y + r.H && ly + lh > r.Y) { clash = true; break; }
                if (clash) continue;
                placed.Add((lx, ly, lw, lh));
                for (int k = 0; k <= 1; k++)   // 世界の複製は2枚（州○のループと同じ理由）
                    labels.Add(new MapLabel { X = lx + k * MapData.MapWidth, Y = ly, Text = cand.Text, FontSize = fsn, Brush = ProvinceNameBrush });
            }
        }
        // 国名ラベルの重なり回避＝州名と同じ貪欲（人口の多い国から置き、重なる後続は落とす＝全景で小国が潰し合わない）。
        // 文字は州名より大きく（画面上サイズ一定＝キャンバス比では 15/_zd）。中央寄せは文字数×0.30em の近似（○内の規模表示と同じ作法）。
        if (nationCands.Count > 0)
        {
            double fsn2 = 15.0 / _zd;
            nationCands.Sort((a, b) =>
            {
                int c0 = b.Prio.CompareTo(a.Prio);
                return c0 != 0 ? c0 : string.CompareOrdinal(a.Text, b.Text);   // 同値は名前順＝決定論
            });
            var placedN = new List<(double X, double Y, double W, double H)>();
            foreach (var cand in nationCands)
            {
                // ★B3（2026-09-05）：文字間隔を領土の東西幅に沿わせる（Vic風）。基本幅より領土が広いときだけ広げ、上限は1文字幅＝細長い国で伸びすぎない。
                double baseW = cand.Text.Length * fsn2 * 0.58;
                int gaps = System.Math.Max(1, cand.Text.Length - 1);
                double spacing = System.Math.Clamp((cand.Ext * 0.6 - baseW) / gaps, 0.0, fsn2 * 0.9);
                double lw = baseW + spacing * gaps, lh = fsn2 * 1.25;
                double lx = cand.X - lw / 2, ly = cand.Cy - fsn2 * 0.62;
                bool clash = false;
                foreach (var r in placedN)
                    if (lx < r.X + r.W && lx + lw > r.X && ly < r.Y + r.H && ly + lh > r.Y) { clash = true; break; }
                if (clash) continue;
                placedN.Add((lx, ly, lw, lh));
                for (int k = 0; k <= 1; k++)   // 世界の複製は2枚（州○のループと同じ理由）
                    labels.Add(new MapLabel { X = lx + k * MapData.MapWidth, Y = ly, Text = cand.Text, FontSize = fsn2, Brush = NationNameBrush, LetterSpacing = spacing });
            }
        }
        BuildMapLabels(labels);   // 海盆名＋海峡/運河マーカー（データのツールチップ付き）
        BuildPathGraphLayer();    // 03地理 手順1：陸グラフの辺＋バリア表示（§6＝地理の型F検出器を兼ねる）

        ProvinceDots = dots;   // 一括差し替え＝バインディング通知1回（要素単位の更新イベントを出さない）
        MapLabels = labels;
        RefreshPinnedHover();   // ★州パネルのピン＝作り直した要約へ差し替え（レビュー是正2026-09-07）
    }

    // 海盆ラベルの位置（海上の代表点）。表示は英語・ツールチップに日本語名＋母港とする国の一覧。
    private static readonly (Basin Basin, string En, string Ja, double Lat, double Lon)[] BasinLabels =
    [
        (Basin.Atlantic, "ATLANTIC OCEAN", "大西洋", 28, -42),
        (Basin.Mediterranean, "MEDITERRANEAN SEA", "地中海", 34.5, 17),
        (Basin.BlackSea, "BLACK SEA", "黒海", 43.6, 34.5),
        (Basin.IndianOcean, "INDIAN OCEAN", "インド洋", -8, 75),
        (Basin.Pacific, "PACIFIC OCEAN", "太平洋", 18, 168),
    ];
    private static readonly IBrush NationNameBrush = new SolidColorBrush(Color.Parse("#F5FFF8E4"));
    private static readonly IBrush ProvinceNameBrush = new SolidColorBrush(Color.Parse("#F0FFF4DC"));   // ズーム時の州名（英語・明るいクリーム＝海の紺・陸のタンの両方で読める）
    private static readonly IBrush BasinBrush = new SolidColorBrush(Color.Parse("#7FA8C4DC"));          // 海盆ラベル（ブラシ生成を再構築ごとに繰り返さない）

    // 海盆名をラベル層へ追加（海峡/運河は host 州のホバーへ統合済み＝マーカーなし）。
    // 地名を出すズーム閾値：フィット倍率の3倍（＝世界全景から少し寄せるまでは州名・海盆名を出さない＝描画軽量化・2026-07-05）。
    // フィット倍率が未取得なら絶対値 1.6 にフォールバック。文字サイズは一定（画面上サイズ不変）＝閾値未満で消えるだけ。
    private double _mapFitScale;
    public void SetMapFitScale(double s) { if (s > 1e-6) _mapFitScale = s; }
    private double LabelThreshold => _mapFitScale > 1e-6 ? _mapFitScale * 3.0 : 1.6;

    // 03地理 手順1（2026-07-28・§6作者裁定）：陸グラフ（PathGraph）の辺とバリアを地図に描く。
    // 「マップに線を引けば×0.23の誤りが一目で分かる」＝refprice と同役の地理検出器。静的幾何＝ズーム時は太さのみ追随。
    private static readonly IBrush EdgeBrush = new SolidColorBrush(Color.Parse("#2E4E6E"));       // 陸路の骨格（海色より少し明るい藍）
    private static readonly IBrush DotSizeBrush = new SolidColorBrush(Color.Parse("#F5F0E2"));    // ○内の規模テキスト（クリーム＝州色の上で読める）
    private static readonly IBrush PassEdgeBrush = new SolidColorBrush(Color.Parse("#8A6E2E"));   // 峠経由の辺（割増＝琥珀系）
    // ★2026-08-01：海の辺を**はっきり明るく**した（ユーザー報告「海上の線が海と同化して非常に見づらい」）。
    // ⚠旧 #1F3A55 は海の塗り #16314E と各チャンネル差が9しかなく、コントラスト比 約1.1:1＝事実上見えない。
    //   設計意図は「陸の辺より控えめ」だったが、控えめを通り越して消えていた。
    // ★新 #4E7FA8 のコントラスト比は 約3.1:1（対 #16314E）＝細線でも追える。琥珀の通路 #8A6E2E とも色相が離れている。
    // ★2026-08-02：海路は**1色で統一**する（作者指摘「港-海路だけが薄い水色で違和感がある」）。
    // ⚠一度は「骨格を控えめ・接ぎ木をはっきり」と色を分けたが、接ぎ木も**海路の一部**であって別の種類の線ではない。
    //   分けると接ぎ木だけが浮いて、港から放射状に別物が生えているように見えた。同じ色なら港から港まで1本の網に読める。
    private static readonly IBrush SeaEdgeBrush = new SolidColorBrush(Color.Parse("#4E7FA8"));
    private static readonly IBrush SeaStitchBrush = SeaEdgeBrush;   // 接ぎ木＝海路と同一（太さだけ僅かに細い）
    private static readonly IBrush BarrierFill = new SolidColorBrush(Color.Parse("#4A3B2A"));     // 山脈・砂漠（暗褐）
    private static readonly IBrush PassFill = new SolidColorBrush(Color.Parse("#C9A227"));        // 峠（金）
    private double _edgeZd = -1;   // ★2026-08-02：陸/海グラフの線を最後に組んだときのズーム。同じなら組み直さない
    private (bool Suez, bool Panama) _edgeChokes = (true, true);   // ⚠運河は開通で線が増える＝ズームが同じでも組み直す必要がある

    // ★★2026-08-02：地図は**骨格ではなく、実際に使われた経路**を描く。
    // ── なぜ変えたか ──────────────────────────────────────────────────────────
    // 骨格（グラフの全辺）は 陸713＋海214＋接ぎ木 を世界2枚ぶん＝**1,800本超**が同時に出る。
    // 一方、1本の航路が実際に使う辺は5〜15本。必要な15本を探すために1,800本を描いていた＝
    // 「見づらい」のではなく**原理的に読めない**。しかも密なヨーロッパほど線が濃くなるので、
    // 一番読みたい場所が一番読めなかった。
    // ⚠**グラフの密度を下げる道は実測で塞がっている**（docs/202608/02）：見た目が直る水準（陸301本）まで削ると
    //   硬性ゲートが赤になり、帯が保てる水準（594本）では見た目が直らない。残る道は描くものを変えることだけ。
    // ── 何を描くか（エンジンが実際に使う量だけ）────────────────────────────────
    // ・海＝国どうしの交易。太さは <c>InternationalMarket</c> の重力カーネルと同じ量
    //   wij = TradeWeight_j × Half/(Half + 海路距離) を、経路上の**辺ごとに積算**したもの。
    // ・陸＝内陸州から最寄り港までの陸脚（<c>Region.PortLandKm</c> の実体＝キエフ/ハリコフ→ロストフの穀物動線）。
    // ★積算するので、要素数は**グラフの辺数が上限**（増えない）。そして大西洋→地中海の航路は全部
    //   ジブラルタルで重なるので、そこが**自然に太くなる**＝作為ではなく実データの重なりが海図の形を作る。
    // ★使われない辺は1本も描かれない＝誤りが残っていても実害の無い辺は目に入らない（探す価値が低いものを見せない）。
    private const int RouteRefreshYears = 5;   // 交易の重みはゆっくり動く＝この年数ごとに積算し直す（毎四半期は重い）
    private Dictionary<(int A, int B), double>? _landUse;
    private Dictionary<(int A, int B), double>? _seaUse;
    private Dictionary<(string Port, int Node), double>? _stitchUse;   // 港→最寄り海ノードの接ぎ木（航路の出入口）
    private Dictionary<string, (double Lat, double Lon)>? _stitchCoord;   // 接ぎ木端点の座標フォールバック（植民地＝CityCoord に無い・2026-08-02）
    private int _routeUseYear = int.MinValue;
    private int _routeRebuilds;   // 積算し直した回数。★最初の2回だけ1年周期（助走中は植民地の直送が未生成で線が全部足切りされ、5年周期だと開始から5年間ずっと植民地線が出ない）

    // ★★2026-08-16の足切り：**3種類の線（陸路・海路・接ぎ木）すべてに同一の規則**を掛ける。通路（琥珀）も例外にしない
    //   ——通路は別レイヤではなく _seaUse の部分集合（passages.json 由来かで色を変えているだけ）なので、
    //   海路が消えれば通路も消える。実測（mapflow・1783年）＝経路に現れた通路9本のうち2本（ホーン岬沖↔チリ南沖・
    //   紅海南↔アデン湾）が足切りで消え、未開通の運河（東地中海↔スエズ湾・カリブ西↔パナマ湾）は従来どおり
    //   「経路に現れない」＝この足切りは「未開通の運河は描かない」の担保を壊さない。
    // ⚠**辞書の値そのものは流通量ではない**（陸＝その脚を使う後背地の州の数／海・接ぎ木＝重力カーネル wij の積算＝無次元）。
    //   実測（mapflow・1783年）＝陸 最小1/中央1/最大6、海 最小0.018/中央1.44/最大11.6、接ぎ木 最小0.018/中央1/最大8.05。
    //   この値に貿易一覧の丸め（£0.0k／0.0k）を当てると **3種類とも100%が消えて地図が空になる**＝物差しが違う。
    //   また「経路に現れた辺しか積まない」ので**値0.0の要素は1件も存在しない**（実測0件）＝辺の側で0を切っても本数は変わらない。
    //   よって足切りは**積算の出どころ**（州の決済額・国の輸出入額・港の取扱高・植民地の直送）に掛ける＝そこにだけ
    //   貿易一覧と同じ単位（£/月）の実測がある。辺の側の 0 以下は番人として別途落とす（下の3ループ）。
    /// <summary>流通の足切り（作者指示2026-08-16「輸出・輸入量0.0k以下はUIに表示しない方針になったので、
    /// 合わせて流通量0.0K以下の陸路・海路も削除」）。
    /// ★判定は**貿易一覧が使っている関数そのもの**＝月額が £0.0k に丸まる流れは「無かった」として扱う
    ///   （<c>GameViewModel.Trade.cs</c> 輸出入一覧の <c>Shown</c>／植民地直送の <c>Money(q*p) is not "£0.0k"</c> と同一）。
    /// ⚠UI に第二の閾値を作らない。<c>Money(0)</c> と比べるので、金額の書式を変えても足切りが自動で追随する
    ///   （数値の 50 を書き写すと書式変更でずれる）。符号は問わない＝絶対値で判定（"-£0.0k" も 0 扱い）。</summary>
    private static bool FlowShown(double monthlyValue) => Money(System.Math.Abs(monthlyValue)) != Money(0);

    /// <summary>実際に使われた経路を辿り、グラフの辺ごとに使用量を積む。ズームでは呼ばない（重いのはここだけ）。</summary>
    private void BuildRouteUsage((bool Suez, bool Panama) chokes)
    {
        var land = new Dictionary<(int, int), double>();
        var sea = new Dictionary<(int, int), double>();
        var stitch = new Dictionary<(string, int), double>();
        _routeUseYear = _sim.Year;

        var landIx = new Dictionary<string, int>(SimCore.PathGraph.Nodes.Length);
        for (int i = 0; i < SimCore.PathGraph.Nodes.Length; i++) landIx[SimCore.PathGraph.Nodes[i].Name] = i;
        var seaIx = new Dictionary<string, int>(SimCore.SeaGraph.Nodes.Length);
        for (int i = 0; i < SimCore.SeaGraph.Nodes.Length; i++) seaIx[SimCore.SeaGraph.Nodes[i].Name] = i;
        static void Bump(Dictionary<(int, int), double> d, int a, int b, double w)
        {
            var key = a < b ? (a, b) : (b, a);
            d[key] = d.TryGetValue(key, out double v) ? v + w : w;
        }

        // ── 陸：内陸州 → 最寄り港（穀物動線）。太さは州の数＝どれだけの後背地がその道を使うか
        foreach (var sim in _sim.Nations)
            foreach (var rr in sim.World.Regions)
            {
                if (rr.State != RegionState.Active || rr.IsPort) continue;
                // ★★2026-08-16：**その州が実際に何も動かしていないなら陸脚を描かない**（作者指示＝貿易一覧と同じ規則）。
                // ⚠陸脚の流通量＝州の決済額 <c>Region.LastPortThroughput</c>（名前に反して内陸州にも立つ＝
                //   その州が世界市場と取引した額そのもの・四半期ごとに BilateralMarket が 0 から積み直す）。
                //   月額へ直して貿易一覧と同じ丸め（£0.0k）で切る＝一覧に1行も出ない州の道は地図にも出さない。
                if (!FlowShown(rr.LastPortThroughput / Calendar.MonthsPerQuarter)) continue;
                // ★トランジット州の陸路も描く（作者指摘2026-08-11「内陸国が直接、海に接続している」）。
                //   旧＝NearestPortName が空なら飛ばしていた＝**それはまさにトランジット利用州の条件**で、
                //   借りた港までの陸脚が1本も描かれず、海の区間だけが残って「内陸から直接海へ」に見えていた。
                //   エンジン側は正しく3脚を通っている（通行料も流れている）＝これは地図の取りこぼしだった。
                string portName = rr.NearestPortName.Length > 0 ? rr.NearestPortName : rr.LastTransitPortName;
                if (portName.Length == 0) continue;   // 借りる港も無い＝海に出る経路が無い（描く線も無い）
                var cc = SimCore.Climate.CityCoord(rr.Name);
                var pc = SimCore.Climate.CityCoord(portName);
                if ((System.Math.Abs(cc.Lat) < 1e-9 && System.Math.Abs(cc.Lon) < 1e-9)
                 || (System.Math.Abs(pc.Lat) < 1e-9 && System.Math.Abs(pc.Lon) < 1e-9)) continue;
                var path = SimCore.PathGraph.RoutePath(cc.Lat, cc.Lon, pc.Lat, pc.Lon);
                for (int k = 1; k < path.Count; k++)
                    if (landIx.TryGetValue(path[k - 1], out int a) && landIx.TryGetValue(path[k], out int b)) Bump(land, a, b, 1.0);
            }

        // ── 陸直行：州↔州の陸路交易（LandRouteEnabled・2026-08-31＝作者報告「陸上交易の経路が非表示」の修正）。
        //   エンジンの純記帳（Region.LastLandPartnerName/Flow＝viaLand<viaSea だった最大の相手）を PathGraph の実経路で描く。
        //   足切りは陸脚・海路と同一規則（FlowShown＝貿易一覧と同じ丸め）。重複（両端が互いを指す）は Bump が同キーに畳む。
        foreach (var sim in _sim.Nations)
            foreach (var rr in sim.World.Regions)
            {
                if (rr.State != RegionState.Active || rr.LastLandPartnerName.Length == 0) continue;
                if (!FlowShown(rr.LastLandPartnerFlow / Calendar.MonthsPerQuarter)) continue;
                var ccL = SimCore.Climate.CityCoord(rr.Name);
                var pcL = SimCore.Climate.CityCoord(rr.LastLandPartnerName);
                if ((System.Math.Abs(ccL.Lat) < 1e-9 && System.Math.Abs(ccL.Lon) < 1e-9)
                 || (System.Math.Abs(pcL.Lat) < 1e-9 && System.Math.Abs(pcL.Lon) < 1e-9)) continue;
                var pathL = SimCore.PathGraph.RoutePath(ccL.Lat, ccL.Lon, pcL.Lat, pcL.Lon);
                for (int k = 1; k < pathL.Count; k++)
                    if (landIx.TryGetValue(pathL[k - 1], out int a) && landIx.TryGetValue(pathL[k], out int b)) Bump(land, a, b, 1.0);
            }

        // ── 海：国どうしの交易（重力カーネル）。⚠自国だけでなく全対＝世界の航路網が出る
        double half = _sim.TradeGravityHalfKm;
        var act = new List<(double Lat, double Lon, double Tw, string Name)>();
        var gateOf = new Dictionary<World, (string Name, double Lat, double Lon)>();   // 国→海へ出る口。★下の植民地ループも**同じ口**を使う（端点の規約を1本にする）
        foreach (var sim in _sim.Nations)
        {
            var w = sim.World;
            // ★海の線の端点は**その国が実際に海へ出る点**にする（作者指摘2026-08-11「ベルンから海へつながっている」）。
            //   旧＝首都の座標を無条件に端点にしていたので、内陸国（スイス＝ベルン）の首都から航路が生えていた。
            //   正しい端点＝①自国の港州 ②無ければ借りている港（トランジット） ③どちらも無ければ**線を描かない**
            //   （海に出る手段が無い国は航路を持たない＝エンジンでも交易が立たない）。
            string gate = ""; double glat = 0, glon = 0;
            foreach (var rr in w.Regions)
            {
                if (rr.State != RegionState.Active) continue;
                if (rr.IsPort) { var pc0 = SimCore.Climate.CityCoord(rr.Name); if (pc0.Lat != 0 || pc0.Lon != 0) { gate = rr.Name; glat = pc0.Lat; glon = pc0.Lon; break; } }
                if (gate.Length == 0 && rr.LastTransitPortName.Length > 0)
                {
                    var pc1 = SimCore.Climate.CityCoord(rr.LastTransitPortName);
                    if (pc1.Lat != 0 || pc1.Lon != 0) { gate = rr.LastTransitPortName; glat = pc1.Lat; glon = pc1.Lon; }
                }
            }
            if (gate.Length == 0) continue;   // 海へ出る口が無い＝航路を描かない
            gateOf[w] = (gate, glat, glon);
            // ★★2026-08-16：**実際に決済された輸出入が無い国の交易線は描かない**（作者指示＝貿易一覧と同じ規則）。
            // ⚠交易線の流通量＝国の輸出入額 <c>World.ExportsByGood/ImportsByGood</c>（**清算単位**の決済台帳＝
            //   BilateralMarket が毎清算 Array.Clear する。四半期清算=3ヶ月分／月次清算=1ヶ月分）。月割は貿易タブと
            //   同じ単位ヘルパー <c>Coef.MonthsPerClearing</c> を使う。州別・相手別の量は月初に消えるので、
            //   清算を通して残るこの台帳が唯一の実測（州ホバーの輸出材が同じ台帳を読むのと同じ理由）。
            // ⚠gateOf には残す＝**植民地線は別の流れ**（EconomyStep の植民地供給＝世界市場を通らない）で、
            //   交易ゼロの国でも植民地からの直送はありうる。端点の規約だけを共有する。
            double tradeQ = 0;
            if (w.ExportsByGood is { } egT) foreach (double v in egT) tradeQ += v;
            if (w.ImportsByGood is { } igT) foreach (double v in igT) tradeQ += v;
            if (!FlowShown(tradeQ / w.Coef.MonthsPerClearing)) continue;   // 清算単位→月割（月次清算ONで1/3過小評価になり小規模交易国の線が消えるのを防ぐ・2026-08-23）
            act.Add((glat, glon, w.SelfPower?.TradeWeight ?? 1.0, gate));
        }
        for (int i = 0; i < act.Count; i++)
            for (int j = i + 1; j < act.Count; j++)
            {
                double km = SimCore.SeaGraph.SeaDistanceKm(act[i].Lat, act[i].Lon, act[j].Lat, act[j].Lon, chokes.Suez, chokes.Panama);
                if (!(km > 0)) continue;   // ⚠否定形＝NaN は描かない
                double wij = act[i].Tw * act[j].Tw * half / (half + km);
                if (!(wij > 1e-6)) continue;
                var path = SimCore.SeaGraph.RoutePath(act[i].Lat, act[i].Lon, act[j].Lat, act[j].Lon, chokes.Suez, chokes.Panama);
                for (int k = 1; k < path.Count; k++)
                    if (seaIx.TryGetValue(path[k - 1], out int a) && seaIx.TryGetValue(path[k], out int b)) Bump(sea, a, b, wij);
                // ★経路の**端点の接ぎ木**もここで記録する（2026-08-02・作者報告「マダガスカルに途切れた線がある」）。
                // ⚠接ぎ木を港州だけに描いていたため、首都が内陸の国（マダガスカル＝アンタナナリヴは coastal=false）では
                //   航路が沖の海ノードで終わり、そこから国へ繋がる線が出ずに**大洋の真ん中で途切れて見えた**。
                //   経路が実際にその地点から出ている以上、港かどうかに関わらず描くのが正しい。
                if (path.Count > 0)
                {
                    void Stitch(int idx, string nodeName)
                    {
                        if (!seaIx.TryGetValue(nodeName, out int sn)) return;
                        var key = (act[idx].Name, sn);
                        stitch[key] = stitch.TryGetValue(key, out double v) ? v + wij : wij;
                    }
                    Stitch(i, path[0]);
                    Stitch(j, path[^1]);
                }
            }

        // ── 植民地：宗主国⇄植民地の資源/補給線（2026-08-02・作者指摘「植民地から導線が伸びていない。
        //    もしかして植民地が港でないから、本国に資源が流れていないのでは？」）。
        //    ★経済の実体は流れている＝EconomyStep の植民地輸入が**首都市場へ毎月直接** Stock+=（港ゲートは無い。
        //      律速は ColonialAccess＝制海と ColonialOutput＝開発度だけ）。**描いていなかっただけ**なので、
        //      国ペアの重力と同じカーネル（相手側 Tw＝act の既定 1.0）で描く＝新しい定数を置かない。
        // ★★2026-08-16：宗主側の端点を**首都から「海へ出る口」へ**移した（作者報告「陸地を横断する海路が描かれている」）。
        // ⚠2026-08-11に国どうしの交易線では直した「内陸の首都から航路が生える」を、**この植民地ループだけが取りこぼしていた**。
        //   実測（mapedges）＝モスクワ→フィンランド湾口が陸上868km・マドリード→アルボラン海が381km・パリ→イギリス海峡東が157km＝
        //   ロシア/スペイン/フランスの首都から大陸を横断する青い線が出ていた（首都が港でない国はすべて同じ症状）。
        //   ★端点の規約は交易線と1本に揃える＝①自国の港州 ②借りている港 ③どちらも無ければ描かない。
        // ⚠この節が読むのは sp.ColonyList（昇格前の Colony のみ）。ColoniesAreRegions=1.0（既定）では開始時の
        //   植民地は最初から州（RegionTier.Colonial）として w.Regions に入り、実交易は上の陸脚/海路ループが
        //   （LastPortThroughput/ExportsByGood 経由で）既に描いている＝この節は未昇格の植民地だけの残り経路。
        var colStitchCoord = new Dictionary<string, (double, double)>();
        foreach (var sim in _sim.Nations)
        {
            var w = sim.World;
            if (w.SelfPower is not { } sp || sp.ColonyList.Count == 0) continue;
            if (!gateOf.TryGetValue(w, out var gt)) continue;   // 海へ出る口が無い＝航路を描かない（交易線と同じ規約）
            // ★★2026-08-16：**実際に届いている財が無い植民地の線は描かない**（作者指示＝貿易一覧と同じ規則）。
            // ⚠植民地線の流通量＝<c>Region.ColonialSupply</c>（首都州に毎月着く供給駆動の産出）。判定は貿易タブの
            //   「植民地直送」節と同一（数量>1e-9 かつ 月額が £0.0k に丸まらない）＝一覧に出ない財しか持たない植民地は
            //   線も持たない。★財の単位でしか実測が無い（個々の植民地の寄与は台帳に残らない）ので、
            //   「一覧に出る財」を集めて、その財を産する植民地だけを描く＝一覧と地図で同じものが見える。
            var shownRes = new HashSet<int>();
            foreach (var rr in w.Regions)
            {
                if (rr.State != RegionState.Active || rr.ColonialSupply is not { } cs) continue;
                for (int gi = 0; gi < cs.Length && gi < Catalog.Goods.Length; gi++)
                {
                    if (!(cs[gi] > 1e-9)) continue;
                    double pg = gi < _sim.Market.Price.Count ? System.Math.Max(_sim.Market.Price[gi], 1e-9)
                              : gi < Catalog.RefPrices.Length ? Catalog.RefPrices[gi] : 1.0;
                    if (FlowShown(cs[gi] * pg)) shownRes.Add(gi);
                }
            }
            if (shownRes.Count == 0) continue;   // 直送が一覧に1行も出ない＝この国の植民地線は描かない
            double tw = sp.TradeWeight;
            foreach (var col in sp.ColonyList)
            {
                if (System.Math.Abs(col.Latitude) < 1e-9 && System.Math.Abs(col.Longitude) < 1e-9) continue;   // 座標未登録＝描かない（維持費と同じ規約）
                bool delivers = false;
                foreach (int res in col.Resources ?? []) if (shownRes.Contains(res)) { delivers = true; break; }
                if (!delivers) continue;   // 一覧に出る財を1つも産していない＝運んでいる物が無い
                double km = SimCore.SeaGraph.SeaDistanceKm(gt.Lat, gt.Lon, col.Latitude, col.Longitude, chokes.Suez, chokes.Panama);
                if (!(km > 0)) continue;   // ⚠否定形＝NaN は描かない
                double wij = tw * half / (half + km);
                if (!(wij > 1e-6)) continue;
                var path = SimCore.SeaGraph.RoutePath(gt.Lat, gt.Lon, col.Latitude, col.Longitude, chokes.Suez, chokes.Panama);
                for (int k = 1; k < path.Count; k++)
                    if (seaIx.TryGetValue(path[k - 1], out int a2) && seaIx.TryGetValue(path[k], out int b2)) Bump(sea, a2, b2, wij);
                if (path.Count > 0)
                {
                    if (seaIx.TryGetValue(path[0], out int sn0))   // 宗主側の端点＝海へ出る口（旧＝首都）
                    {
                        var key = (gt.Name, sn0);
                        stitch[key] = stitch.TryGetValue(key, out double v0) ? v0 + wij : wij;
                    }
                    if (seaIx.TryGetValue(path[^1], out int sn1))   // 植民地側の端点（座標はフォールバック表で解決）
                    {
                        var key = (col.Name, sn1);
                        stitch[key] = stitch.TryGetValue(key, out double v1) ? v1 + wij : wij;
                        colStitchCoord[col.Name] = (col.Latitude, col.Longitude);
                    }
                }
            }
        }

        // ── 接ぎ木：港州がどこから海へ出るか。
        // ⚠**航路が通っている海ノードに繋がる港だけ**を描く（2026-08-02・作者報告「描画に失敗している個所がある」
        //   「スエズも未開通なのに直前まで線が来ている」）。港ごとに無条件で描いていたため、
        //   ①チャールストンや大阪の線が**大洋の真ん中で途切れ**（その海ノードを通る航路が1本も無い）
        //   ②未開通のスエズ湾へも線が届いていた（紅海側の港が接いでいるだけで、そこを通る交易は無い）。
        //   使われた経路だけを描くという原則を、接ぎ木にも通す＝行き止まりの線が原理的に出なくなる。
        var usedSeaNodes = new HashSet<int>();
        foreach (var k in sea.Keys) { usedSeaNodes.Add(k.Item1); usedSeaNodes.Add(k.Item2); }
        foreach (var sim in _sim.Nations)
            foreach (var rr in sim.World.Regions)
            {
                if (rr.State != RegionState.Active || !rr.IsPort) continue;
                // ★★2026-08-16：**取扱高が £0.0k に丸まる港の接ぎ木は描かない**（作者指示＝貿易一覧と同じ規則）。
                //   接ぎ木は「この港はここから海へ出る」を示す線＝出入りする荷が無ければ示すものが無い。
                if (!FlowShown(rr.LastPortThroughput / Calendar.MonthsPerQuarter)) continue;
                var pc = SimCore.Climate.CityCoord(rr.Name);
                if (System.Math.Abs(pc.Lat) < 1e-9 && System.Math.Abs(pc.Lon) < 1e-9) continue;
                int sn = SimCore.SeaGraph.NearestSeaNode(pc.Lat, pc.Lon);
                if (!usedSeaNodes.Contains(sn)) continue;
                var key = (rr.Name, sn);
                stitch[key] = stitch.TryGetValue(key, out double v) ? v + 1.0 : 1.0;
            }
        _landUse = land; _seaUse = sea; _stitchUse = stitch; _stitchCoord = colStitchCoord;
    }

    // passages.json 由来の海の辺（手置きの通路）＝琥珀で描く対象。⚠**辺の集合は静的**（プロセス中不変）なので1回だけ組む。
    // 出自は SeaGraph の診断 API（PassageBypassReport＝通路の辺だけを返す）から取る＝UI 側に通路の一覧を書き写さない。
    private static HashSet<(int A, int B)>? _seaPassageKeys;
    private static Dictionary<(int A, int B), string?>? _seaPassageChoke;   // 通路の辺→咽喉ID（null＝扼者のいない通路＝閉鎖不能）。海峡の門の描画（2026-09-05）が読む
    private static HashSet<(int A, int B)> SeaPassageKeys()
    {
        if (_seaPassageKeys is { } cached) return cached;
        var ix = new Dictionary<string, int>(SimCore.SeaGraph.Nodes.Length);
        for (int i = 0; i < SimCore.SeaGraph.Nodes.Length; i++) ix[SimCore.SeaGraph.Nodes[i].Name] = i;
        var set = new HashSet<(int, int)>();
        var choke = new Dictionary<(int, int), string?>();
        foreach (var p in SimCore.SeaGraph.PassageBypassReport())
            if (ix.TryGetValue(p.A, out int a) && ix.TryGetValue(p.B, out int b))
            {
                var key = (System.Math.Min(a, b), System.Math.Max(a, b));
                set.Add(key); choke[key] = p.ChokeId;
            }
        _seaPassageChoke = choke;
        return _seaPassageKeys = set;
    }

    private void BuildPathGraphLayer()
    {
        // ★★不変の幾何を四半期ごとに作り直さない（2026-08-02）。
        // ⚠読んでいるデータ（PathGraph.Edges / SeaGraph.Edges / Nodes）は全て static readonly ＝プロセス起動時に
        //   1回作られたきり**永久に変化しない**。四半期ごとに変わりうるのは線の太さ（ズーム依存）だけ。
        //   それなのに RefreshMap から毎四半期（速度3なら4.5秒ごと）に呼ばれ、千本単位のオブジェクトを作り直して
        //   リストごと差し替えていた＝「カク付き」の正体。ズームが動いていなければ何もしない。
        // ⚠★ズームだけで判定してはいけない——運河（スエズ1869・パナマ1914）は開通すると線が増える。
        //   開通状態も鍵に含める（これを忘れると運河が永久に地図へ現れない＝自分で踏んだ罠・2026-08-02）。
        // ★世界全体の航路網なので x=null（＝cp.IsOpen だけ・封鎖はどの国と誰の戦争かで変わる）を渡す。
        //   自国（Me）基準にすると、自国が運河の持ち主と交戦中なだけで**他国どうしの航路まで**閉じて描かれていた。
        var chokes = (Suez: _sim.Diplomacy.ChokeOpenFor(SimCore.Diplomacy.SuezIdPublic, null),
                      Panama: _sim.Diplomacy.ChokeOpenFor(SimCore.Diplomacy.PanamaIdPublic, null));
        // ⚠積算し直す年（RouteRefreshYears おき）にだけ線を組み直す。**_routeUseYear で早期returnを判定してはいけない**
        //   ——積算を飛ばした年は _routeUseYear が古いままなので、毎四半期に線を組み直すことになる（カク付きの再発）。
        // ★2026-08-16：助走（StartWarmupQuarters）中は外交・植民地が凍結され ColonialSupply が未生成＝植民地線の月額が £0.0k で
        //   全部足切りされる。RouteRefreshYears おきの再構築だと開始から5年間ずっと植民地線が出ないので、
        //   **助走明けの最初の年だけ1年で組み直す**（線が出てからは通常の周期へ戻る＝毎四半期の再構築にはならない）。
        int refreshYears = _routeRebuilds < 2 ? 1 : RouteRefreshYears;
        bool rebuildRoutes = chokes != _edgeChokes || _landUse is null || _sim.Year - _routeUseYear >= refreshYears;
        if (System.Math.Abs(_zd - _edgeZd) < 1e-9 && chokes == _edgeChokes && !rebuildRoutes) return;
        _edgeZd = _zd; _edgeChokes = chokes;
        if (rebuildRoutes) { _routeRebuilds++; TimeIt("経路の積算(BuildRouteUsage)", () => BuildRouteUsage(chokes)); }
        var edges = new List<MapEdgeLine>(512);
        // 使用量→太さ：最大を1として平方根で圧縮する（重みは重力カーネルで裾が長く、線形だと1本だけ太く残りが消える）
        static double Scale(double w, double max, double base_) => base_ * (0.6 + 1.9 * System.Math.Sqrt(max > 1e-12 ? w / max : 0));
        double th = System.Math.Max(0.35, 0.7 / _zd);
        double landMax = 0; foreach (var v in _landUse!.Values) if (v > landMax) landMax = v;
        foreach (var kv in _landUse)
        {
            if (!(kv.Value > 0)) continue;   // 積算0以下は描かない（番人・否定形＝NaNも落とす）。実測では0の要素は生じない
            var a = SimCore.PathGraph.Nodes[kv.Key.A]; var b = SimCore.PathGraph.Nodes[kv.Key.B];
            // ⚠★両端が地図に**描かれる**ものだけ引く（2026-08-02・作者報告「ボルドーからの線が途切れている」）。
            //   陸グラフのノードは landmass.json 由来。州は MapData.CityCoords、植民地は Geo.ColonyCoord で描かれる＝
            //   **座標表が2つある**ので、州だけを CityCoords で判定する（植民地まで弾くと45本の正しい線が消える）。
            //   ★真の欠落は15町だけで、それは MapData.CityCoords へ座標を写して解消済み＝この番人は再発防止。
            if (a.Kind == "state" && !MapData.CityCoords.ContainsKey(a.Name)) continue;
            if (b.Kind == "state" && !MapData.CityCoords.ContainsKey(b.Name)) continue;
            var (x1, y1) = MapData.Project(a.Lat, a.Lon);
            var (x2, y2) = MapData.Project(b.Lat, b.Lon);
            if (x2 - x1 > MapData.MapWidth / 2) x2 -= MapData.MapWidth;       // 反対子午線を跨ぐ辺は近い側の複製で描く
            else if (x1 - x2 > MapData.MapWidth / 2) x1 -= MapData.MapWidth;
            var brush = a.Kind == "pass" || b.Kind == "pass" ? PassEdgeBrush : EdgeBrush;
            double t = Scale(kv.Value, landMax, th);
            for (int k = 0; k <= 1; k++)   // 世界の複製は2枚（州○のループと同じ理由）
                edges.Add(new MapEdgeLine
                {
                    Start = new Avalonia.Point(x1 + k * MapData.MapWidth, y1),
                    End = new Avalonia.Point(x2 + k * MapData.MapWidth, y2),
                    Thickness = t, Brush = brush,
                });
        }
        // 03地理 手順3（§6）：海の辺＝海路の骨格（大陸を横切っていないことが見える）。通路（海峡/運河）は**海路と同じ青**（2026-09-05：旧「琥珀の太線」は作者指示で廃止＝扼者のいる咽喉だけ国色の門を横切らせる・下の「海峡の門」）。
        double sth = System.Math.Max(0.45, 0.55 / _zd);   // ★2026-08-01：下限を上げる（0.3pxはサブピクセルで霞んで消える）
        double seaMax = 0; foreach (var v in _seaUse!.Values) if (v > seaMax) seaMax = v;
        var passageKeys = SeaPassageKeys();
        foreach (var kv in _seaUse)
        {
            if (!(kv.Value > 0)) continue;   // 積算0以下は描かない（番人・否定形＝NaNも落とす）。⚠通路（琥珀）も同じ規則＝例外を作らない
            bool isPassage = passageKeys.Contains(kv.Key);
            var a = SimCore.SeaGraph.Nodes[kv.Key.A]; var b = SimCore.SeaGraph.Nodes[kv.Key.B];
            var (x1, y1) = MapData.Project(a.Lat, a.Lon);
            var (x2, y2) = MapData.Project(b.Lat, b.Lon);
            if (x2 - x1 > MapData.MapWidth / 2) x2 -= MapData.MapWidth;
            else if (x1 - x2 > MapData.MapWidth / 2) x1 -= MapData.MapWidth;
            // ★2026-08-01：**未開通の運河は描かない**（作者指摘「パナマを突っ切って交易できている気がする」）。
            // SeaGraph.Edges は運河の辺を常に含み、実際の経路計算では開通状態で**辺ごと消える**設計だが、
            // 地図はその開通状態を見ずに全部描いていた＝1783年の画面にパナマ運河の線が出て、
            // 「そこを通れる」と読めてしまっていた。⚠交易の経路自体は正しく迂回している（表示だけの不整合）。
            // ⚠経路に現れた辺しか来ないので、未開通の運河はそもそも積算されない（経路計算が辺ごと外すため）。
            // ★★2026-08-16：琥珀（通路）の判定を**運河/海峡のIDの有無ではなく passages.json 由来かどうか**に直した。
            // ⚠この行の意図は最初から「通路（海峡/運河）は琥珀の実線」（上の見出しコメント）だったが、実装は
            //   ChokeId が付いた通路（ジブラルタル・ボスポラス・スエズ・パナマ・マラッカ・ズンド）だけを琥珀にしていた。
            //   ChokeId の無い通路6本（ホーン岬・喜望峰・バブ・エル・マンデブ・ダーダネルス・イギリス海峡・エルベ水路）は
            //   海路と同じ青で描かれていた＝**手置きの通路は陸判定を通さない**ので、そのまま「陸を横断する青い線」に見えた
            //   （実測 mapedges＝ホーン岬沖↔チリ南沖が陸上794km・バブ・エル・マンデブ307km・ダーダネルス159km）。
            //   通路は「そこを通れる」ことを示す線なので、色の規約を出自で揃える＝海路の青は「大陸を横切らない」を保つ。
            // ★海峡の門（作者指示2026-09-05「海峡がわざとらしい（茶色い棒）＝閉鎖可能とわかりやすい形に・アイコンなし」）。
            //   通路の辺そのものは**普通の海路と同じ青・同じ太さ**（琥珀の太線＝棒を廃止）。扼者のいる咽喉（Chokepoint.Owner）だけ、
            //   辺の中点に**水路を横切る短い線**を扼者の国色で引く＝「この水路は○○が扼している（戦争で閉じる）」を線と色だけで読ませる。
            //   プレイヤーに対して閉じている（扼者と交戦中＝BilateralMarket.Blocked と同じ条件）ときは**辺を描かず**門を太くする＝断たれた航路がそのまま見える。
            //   扼者のいない通路（イギリス海峡・喜望峰・ホーン岬…）は門を持たない＝閉鎖不能＝ただの海路。エンジンは読まない（表示のみ）。
            var brush = SeaEdgeBrush;
            double st = Scale(kv.Value, seaMax, sth);
            SimCore.Chokepoint? gateCp = null;
            if (isPassage && _seaPassageChoke is { } chokeMap && chokeMap.TryGetValue(kv.Key, out var chokeId) && chokeId is not null)
                foreach (var cpx in _sim.Diplomacy.Chokepoints) if (cpx.Id == chokeId && cpx.IsOpen && cpx.Owner is { Nation: not null }) { gateCp = cpx; break; }
            bool gateClosed = gateCp is not null && _player.SelfPower is { } gsp && gateCp.Owner != gsp && _sim.Diplomacy.IsAtWar(gsp, gateCp.Owner!);
            if (!gateClosed)
                for (int k = 0; k <= 1; k++)   // 世界の複製は2枚（州○のループと同じ理由）
                    edges.Add(new MapEdgeLine
                    {
                        Start = new Avalonia.Point(x1 + k * MapData.MapWidth, y1),
                        End = new Avalonia.Point(x2 + k * MapData.MapWidth, y2),
                        Thickness = st, Brush = brush,
                    });
            if (gateCp is not null)
            {
                int oni = 0;
                for (int i2 = 0; i2 < _sim.Nations.Count; i2++) if (ReferenceEquals(_sim.Nations[i2].World, gateCp.Owner!.Nation)) { oni = i2; break; }
                var gateBrush = new SolidColorBrush(MapData.NationColor(gateCp.Owner!.Name, oni));
                double mx = (x1 + x2) / 2, my = (y1 + y2) / 2, ddx = x2 - x1, ddy = y2 - y1, dl = System.Math.Sqrt(ddx * ddx + ddy * ddy);
                if (dl > 1e-9)
                {
                    double px = -ddy / dl, py = ddx / dl;            // 水路に直交する単位ベクトル
                    double half = (gateClosed ? 9.0 : 7.0) / _zd;      // 門の半長（ズーム補正＝○と同じ物差し・海路より一回り長く水路を跨ぐ）
                    double gth = System.Math.Max(1.0, (gateClosed ? 3.2 : 2.0) / _zd);   // 海路（≈0.5〜1px）より太い＝門が主役
                    for (int k = 0; k <= 1; k++)
                        edges.Add(new MapEdgeLine
                        {
                            Start = new Avalonia.Point(mx - px * half + k * MapData.MapWidth, my - py * half),
                            End = new Avalonia.Point(mx + px * half + k * MapData.MapWidth, my + py * half),
                            Thickness = gth, Brush = gateBrush,
                        });
                }
            }
        }
        // ★★港州と海路網をつなぐ「接ぎ木」を描く（2026-08-02・作者指摘「港州と海路が接続されておらず、
        //   どこの港を経由しているかわからない」）。
        // ── なぜ欠けていたか ────────────────────────────────────────────────────
        // 海の辺は**洋上ウェイポイントどうし**しか結ばない。港は距離を問う瞬間に最寄りウェイポイントへ
        // 接ぎ木される（SeaGraph.SeaDistanceKm の stitch）が、その接ぎ木は**地図に一度も描かれていなかった**。
        // だから線は港の手前で終わり、海の何もない点を指しているように見えた＝内部構造だけが露出していた。
        // ★接ぎ木を描けば「この港はここから海へ出る」が読め、港から港への道筋が繋がって見える。
        // ⚠計算は最寄りノード1回だけ＝港州の数（約150）ぶん。静的なのでズーム時しか組み直さない。
        double stitchTh = System.Math.Max(0.4, 0.5 / _zd);   // 骨格よりわずかに細い＝港へ寄る枝であることが太さで分かる
        foreach (var kv in _stitchUse!)
        {
            if (!(kv.Value > 0)) continue;   // 積算0以下は描かない（番人・否定形＝NaNも落とす）
            var pc = SimCore.Climate.CityCoord(kv.Key.Port);
            if (System.Math.Abs(pc.Lat) < 1e-9 && System.Math.Abs(pc.Lon) < 1e-9)
            {
                // 植民地の端点＝CityCoord（州の表）に無い＝植民地座標のフォールバック（2026-08-02）
                if (_stitchCoord is { } cc2 && cc2.TryGetValue(kv.Key.Port, out var colPos)) pc = (colPos.Lat, colPos.Lon);
                else continue;   // 座標未登録は描かない
            }
            var sn = SimCore.SeaGraph.Nodes[kv.Key.Node];
            var (px, py) = MapData.Project(pc.Lat, pc.Lon);
            var (sx, sy) = MapData.Project(sn.Lat, sn.Lon);
            if (sx - px > MapData.MapWidth / 2) sx -= MapData.MapWidth;
            else if (px - sx > MapData.MapWidth / 2) px -= MapData.MapWidth;
            for (int k = 0; k <= 1; k++)
                edges.Add(new MapEdgeLine
                {
                    Start = new Avalonia.Point(px + k * MapData.MapWidth, py),
                    End = new Avalonia.Point(sx + k * MapData.MapWidth, sy),
                    Thickness = stitchTh, Brush = SeaStitchBrush,
                });
        }
        MapEdges = edges;
        var bdots = new List<ProvinceDot>(64 * 3);
        double bd = 5.0 / _zd;
        foreach (var n in SimCore.PathGraph.Nodes)
        {
            if (n.Kind is not ("barrier" or "pass")) continue;
            var (bx, by) = MapData.Project(n.Lat, n.Lon);
            var hover = new MapHoverInfo { Title = n.Name };   // データのみ（指標名規約＝説明文なし）
            for (int k = 0; k <= 1; k++)   // 世界の複製は2枚（州○のループと同じ理由）
                bdots.Add(new ProvinceDot
                {
                    X = bx + k * MapData.MapWidth - bd / 2, Y = by - bd / 2, Diameter = bd,
                    Fill = n.Kind == "pass" ? PassFill : BarrierFill, Stroke = Brushes.Transparent, StrokeThickness = 0, Hover = hover,
                });
        }
        BarrierDots = bdots;
    }

    private void BuildMapLabels(List<MapLabel> labels)
    {
        if (_dotZoom < LabelThreshold) return;   // ズームを引くと海盆名も消える（軽量化）
        // 海盆：海の上に薄い英語ラベル。ツールチップ＝日本語名と母港とする国（4カ国/行）。
        foreach (var (basin, ben, bja, blat, blon) in BasinLabels)
        {
            var members = new List<string>();
            foreach (var sim in _sim.Nations)
                if (sim.World.SelfPower is { } bp && sim.World.Regions.Exists(rg => rg.State == RegionState.Active && rg.IsPort && rg.SeaBasin == basin)) members.Add(bp.Name);   // ★2026-09-06：国単位の海盆を撤去＝この海盆に港州を持つ国（州単位の SeaBasin）
            string tipB = $"{bja}\n港を持つ国({members.Count}):";
            for (int i = 0; i < members.Count; i += 4)
                tipB += "\n" + string.Join("・", members.Skip(i).Take(4));
            double fs = 15.0 / _zd;
            var (bx, by) = MapData.Project(blat, blon);
            for (int k = 0; k <= 1; k++)   // 世界の複製は2枚（州○のループと同じ理由）
                labels.Add(new MapLabel
                {
                    X = bx + k * MapData.MapWidth - ben.Length * fs * 0.30, Y = by - fs * 0.6,
                    Text = ben, FontSize = fs,
                    Brush = BasinBrush, Tooltip = tipB,
                });
        }
    }

    // 海峡/運河の説明行（州・植民地のホバーに統合）。
    private void AppendChokepoint(MapHoverInfo info, string hostName)
    {
        if (!_cpByHost.TryGetValue(hostName, out var list)) return;
        foreach (var cp in list)
        {
            string kind = cp.IsCanal ? "運河" : "海峡";
            string state = cp.IsOpen ? "開通" : $"未開通(進捗 {(cp.BuildCost > 1e-9 ? cp.BuildProgress / cp.BuildCost : 0):P0})";
            string basins = string.Join("・", cp.ServesBasins.Select(b => BasinLabels.FirstOrDefault(x => x.Basin == b).Ja ?? b.ToString()));
            info.Lines.Add(new MapHoverLine { Text = $"{cp.Name}({kind}・{state})", Brush = HoverHead, Weight = FontWeight.Bold });
            info.Lines.Add(new MapHoverLine { Text = $"扼者: {cp.Owner?.Name ?? "なし"}" + (basins.Length > 0 ? $"　通航: {basins}" : ""), Brush = HoverDim });
        }
    }

    /// <summary>★A-13（2026-09-03）：その州に建つ巨大建造物を州ホバーへ。⚠データだけを置く（説明文を書かない＝規律⑦）。
    /// 未着工／建設中（進捗）／完成 の3状態と、効果の宛先（耕地・送電容量・工業化）を出す。</summary>
    private void AppendMegaprojects(MapHoverInfo info, string hostName)
    {
        if (!_megaByHost.TryGetValue(hostName, out var list)) return;
        foreach (var mp in list)
        {
            string state = mp.IsComplete ? "完成"
                : !mp.Started ? "未着工"
                : $"建設中 {(mp.BuildUnits > 1e-9 ? mp.BuildProgress / mp.BuildUnits : 0):P0}";
            // ★2026-09-07 作者指示「名前と効果を1行で・効果が全て0なのを修正」：
            //   旧＝2行に割れ、しかも effectValue だけを読んでいた。威信型（87/119件）は effectValue=0 かつ Megaprojects.ApplyEffect に
            //   分岐が無い＝州への効果は無いが、**完成には全件 MegaprojectPrestige の正統性加算が付く**（Megaprojects.cs:102-107・稼働中）。
            //   UIはエンジンと同じ1本を読む＝州への効果（あれば）と正統性を続けて1行に出す。
            string target = mp.EffectTarget switch
            {
                "ArableLand" => "耕地",
                "GridCapacity" => "送電容量",
                "Industrialization" => "工業化",
                _ => "",   // 威信型は州への効果を持たない（正統性だけ）
            };
            string eff = target.Length > 0 && System.Math.Abs(mp.EffectValue) > 1e-9
                ? $"{target} {(mp.EffectMode == "scale" ? $"×{1 + mp.EffectValue:F2}" : $"+{mp.EffectValue:F2}")}"
                : "";
            double leg = _player.Coef.MegaprojectPrestige;
            if (leg > 1e-9) eff = eff.Length > 0 ? $"{eff}・正統性 +{leg:F2}" : $"正統性 +{leg:F2}";
            info.Lines.Add(new MapHoverLine { Text = eff.Length > 0 ? $"{mp.Name}({state}) {eff}" : $"{mp.Name}({state})", Brush = HoverHead, Weight = FontWeight.Bold });
        }
    }

    private const double DotMin = 5.0, DotMax = 19.0;   // ○の直径の範囲＝正規化比[0,1]を線形で画素径へ（RefreshMap 内）

    // 州の生産力＝産出（LastProduced）を参照価格で評価した総額。Header の生産履歴グラフが使う。
    private static double RegionProduction(Region rg)
    {
        double v = 0;
        int n = System.Math.Min(rg.Markets.Length, Catalog.RefPrices.Length);
        for (int i = 0; i < n; i++) v += System.Math.Max(0, rg.Markets[i].LastProduced) * Catalog.RefPrices[i];
        return v;
    }

    private static readonly IBrush HoverText = new SolidColorBrush(Color.Parse("#2A2118"));
    private static readonly IBrush HoverDim = new SolidColorBrush(Color.Parse("#5A4E3C"));
    private static readonly IBrush HoverHead = new SolidColorBrush(Color.Parse("#6B5230"));
    private static readonly IBrush HoverWarn = new SolidColorBrush(Color.Parse("#A0432D"));  // 状態（過密・疫病・主食不足）

    // 州ホバー（左上パネル）：州名（所有国）・人口（失業者）・文化/宗教・状態（過密/疫病/主食不足）・港・海峡/運河・生産物。
    private MapHoverInfo BuildProvinceHover(Region rg, World ownerWorld, string nation, double pop)
    {
        // タイトル＝州名（国名）。国名の横の緊張表示は撤去（2026-07-12）。
        // ★支配度 §7（州パネル・2026-09-07 作者指示「半円で」）＋2026-09-23 作者指示「支配度は常に表示・切り替え不要／円は縦の幅が半分」：
        //   4区分の半円（箱 幅70×高さ36・底辺の中点(35,35)・半径34＝旧 140×72 の縦半分）＋外周の帯＝国家区分の10年前（半径32・線）。
        //   色＝国家:鋼青・在地勢力:褐・外国勢力:琥珀・無統治:淡墨（政治タブの金/錆と衝突させない）。値は凡例に数字で。
        // ★4区分を何から作るか（2026-09-23）：
        //   ・「国家」＝live な rg.Dominance。Politics.cs:1797-1805 の積分は**常に走る**（RuleSharesEnabled とは無関係）＝毎四半期動く唯一の量。
        //   ・残り（1−支配度）は rg.Rule の 在地/外国/無統治 の**比**で配る。Rule は RuleSharesEnabled=0 の間 Politics.cs:1810 が積分しない＝
        //     初期構成（world.json の rule）のまま動かないので、「動く量」ではなく**残りの内訳の型紙**としてだけ使う。
        //   ・⚠2026-09-23 ㊱で rule は**全61カ国＋州165件**へ入った（旧＝英・露・オスマン・清の4カ国だけ＝残り57カ国が Rule=[1,0,0,0]）。
        //     いまは比の元がどの州にも在るので、残りは 在地/外国/無統治 の実データの比で割れる。比の元が無い州（3成分の和が0）は残りが丸ごと「無統治」。
        //     0% はそのまま 0% と出す＝**無い値をUIで作らない**。
        //   ⚠エンジンの目標式（Politics.cs:1820-1825＝治安カバレッジ×…）をここへ書き写さない。同じ量の定義がエンジンとUIの2本になるのが最も再発するバグ型。
        double dom = rg.Dominance;
        if (!(dom > 0)) dom = 0; else if (!(dom < 1)) dom = 1;   // ⚠Math.Clamp は NaN を落とさない＝否定形で書く
        double rest = 1.0 - dom;
        double sLocal = 0, sForeign = 0, sUngov = 0;
        if (rg.Rule.Length == 4)
        {
            sLocal = rg.Rule[1]; sForeign = rg.Rule[2]; sUngov = rg.Rule[3];
            if (!(sLocal > 0)) sLocal = 0;
            if (!(sForeign > 0)) sForeign = 0;
            if (!(sUngov > 0)) sUngov = 0;
        }
        double sSum = sLocal + sForeign + sUngov;
        double fLocal, fForeign, fUngov;
        if (sSum > 1e-9) { fLocal = rest * sLocal / sSum; fForeign = rest * sForeign / sSum; fUngov = rest * sUngov / sSum; }
        else { fLocal = 0; fForeign = 0; fUngov = rest; }
        double f1 = dom, f2 = f1 + fLocal, f3 = f2 + fForeign;
        string rs = HalfSlice(35, 35, 34, 0, f1), rl = HalfSlice(35, 35, 34, f1, f2), rf = HalfSlice(35, 35, 34, f2, f3), ru = HalfSlice(35, 35, 34, f3, 1.0);
        string rb = "", rbText = "";
        double ago = rg.RuleStateTenYearsAgo();
        if (ago >= 0) { rb = HalfArc(35, 35, 32, ago); rbText = $"10年前の国家 {ago:P0}"; }
        var info = new MapHoverInfo
        {
            // ★2026-09-23（作者指摘「フィラデルフィア(アメリカ(シャムの属国))」＝括弧が二重）：州名と国名は**全角スペースで並べる**。
            //   nation は属国のとき既に「アメリカ(シャムの属国)」という括弧付きの名前（:124・作者指示2026-08-03）＝これを更に括弧で包まない。
            Title = $"{rg.Name}　{nation}", HasRule = true,   // 常に表示＝係数のゲートで隠さない（2026-09-23 作者指示）
            RuleState = rs, RuleLocal = rl, RuleForeign = rf, RuleUngoverned = ru, RuleBand = rb,
            RuleStateText = $"{dom:P0}", RuleLocalText = $"{fLocal:P0}",
            RuleForeignText = $"{fForeign:P0}", RuleUngovernedText = $"{fUngov:P0}", RuleBandText = rbText,
        };
        double unemployed = 0;
        foreach (var p in rg.Pops) unemployed += p.Size * p.UnemploymentRate;
        info.Lines.Add(new MapHoverLine { Text = $"人口: {PopFmt(pop)}(失業者: {PopFmt(unemployed)})", Brush = HoverText });   // 内部単位=千人（監査2026-07-11 他10：「人」直付けで1000倍の過小表示だった）
        // 開発度（2026-08-02・作者指摘「名前出てない州のプロヴィンスの開発度が見れない」）：ズームを引くと州名ラベル
        // （＝括弧の開発度）が消える／重なり回避で小さい州のラベルは出ないため、ホバーでも同じ値を出す。
        // ★定義も丸めもラベルと完全一致（TotalIndustrialization×70・指標名はUIと通知で完全一致の規約）。2026-08-31にラベルが小数1桁(F1)へ変わったのに合わせる。
        info.Lines.Add(new MapHoverLine { Text = $"開発度: {rg.TotalIndustrialization * 70:F1}", Brush = HoverText });
        // ★2026-09-03（キューA-12／出典 トリアージ 2-A 11-2）：国民意識は距離減衰で州ごとに違う（首都から遠い州ほど遅い）のに
        //   どこにも出ていなかった。動員上限（Military の国民意識ゲート）が州で違う理由がこれで読める。
        //   ⚠値は POP 属性（Pop.NationalConsciousness）の人口加重平均＝エンジンと同じ量。未捕捉(-1)の pop は分母から外す。
        {
            double ncW = 0, ncSum = 0;
            foreach (var pp in rg.Pops) if (pp.NationalConsciousness >= 0) { ncW += pp.Size; ncSum += pp.Size * pp.NationalConsciousness; }
            if (ncW > 1e-9)
                info.Lines.Add(new MapHoverLine { Text = $"国民意識: {ncSum / ncW:P0}", Brush = HoverText });
        }
        if (!string.IsNullOrEmpty(rg.Culture) || !string.IsNullOrEmpty(rg.Religion))
            info.Lines.Add(new MapHoverLine { Text = $"文化: {rg.Culture}　宗教: {rg.Religion}", Brush = HoverDim });
        // 状態：過密（収穫逓減）・疫病・主食不足（該当時のみ）
        if (rg.ArableLand < 1e8)
        {
            double rural = 0; foreach (var p in rg.Pops) if (!p.IsUrban) rural += p.Size;
            double landFac = Territory.LandFactor(rg, rural, ownerWorld.Coef);
            if (landFac < FillWarnThreshold) info.Lines.Add(new MapHoverLine { Text = $"過密(土地 {landFac:P0})", Brush = HoverWarn, Weight = FontWeight.Bold });
        }
        if (ownerWorld.EpidemicActiveYears > 0) info.Lines.Add(new MapHoverLine { Text = "疫病 流行中", Brush = HoverWarn, Weight = FontWeight.Bold });
        if (rg.FoodFill < FillWarnThreshold) info.Lines.Add(new MapHoverLine { Text = $"主食不足(充足 {rg.FoodFill:P0})", Brush = HoverWarn, Weight = FontWeight.Bold });
        if (ownerWorld.Coef.OpiumAddictionGrowth > 0 && rg.OpiumAddiction > 0.0005)   // P3：州の依存が有意な時のみ表示
            info.Lines.Add(new MapHoverLine { Text = $"アヘン中毒度 {rg.OpiumAddiction:P1}", Brush = HoverWarn, Weight = FontWeight.Bold });
        if (ownerWorld.Coef.HousingNeedPerUrban > 0 && rg.HousingFill < FillWarnThreshold)   // 都市家賃（2026-07-18）：住居が足りない州のみ表示
            info.Lines.Add(new MapHoverLine { Text = $"住居不足(充足 {rg.HousingFill:P0})", Brush = HoverWarn, Weight = FontWeight.Bold });
        // 治安（作者指示2026-08-15＝ヘッダーの国スカラーを廃し州ホバーへ）：州別の窃盗率 rg.CrimeRate（Crime.cs が
        // 移民pushに使う州別値）をエンジン定数だけで治安率へ正規化＝1−率/(CrimeBase×(1+CrimeUrbanWeight))。
        // 分母は「秩序0・全都市」の理論最悪率＝新しい魔法数を作らない。悪化した州のみ表示（窃盗n/月の表記は廃止）。
        {
            double crimeCap = ownerWorld.Coef.CrimeBase * (1.0 + ownerWorld.Coef.CrimeUrbanWeight);
            if (crimeCap > 1e-12)
            {
                double security = System.Math.Clamp(1.0 - rg.CrimeRate / crimeCap, 0, 1);
                if (security < FillWarnThreshold)
                    info.Lines.Add(new MapHoverLine { Text = $"治安 {security:P0}", Brush = HoverWarn, Weight = FontWeight.Bold });
            }
        }
        if (rg.IsPort)
            info.Lines.Add(new MapHoverLine { Text = $"港 容量:{rg.PortCapacity:F0} 取引額:{Money(rg.LastPortThroughput / Calendar.MonthsPerQuarter)}/月", Brush = HoverText, Weight = FontWeight.Bold });   // 3桁区切りは全廃（作者裁定2026-08-10）。取扱高は四半期累計（BilateralMarket が第1清算のみリセット）＝月割して他タブの/月と物差しを揃える（2026-08-23）
        AppendChokepoint(info, rg.Name);   // 海峡/運河のhost州はここに統合表示
        AppendMegaprojects(info, rg.Name);   // ★A-13：巨大建造物も同じ州の情報として統合表示
        AppendWorldShare(info, rg, ownerWorld);
        return info;
    }

    // ── 州ホバーの「世界の中での位置」（作者フィードバック2026-08-10）─────────────────────────
    // 旧「生産物: 全品目を生産量の降順で列挙」は廃止。量の羅列は「この州は世界の中でどれだけ重いのか」に答えられず、
    // 品目が増えるほど読めなくなっていた。代わりに**世界シェアの高い上位3材**だけを、量とシェアの対で出す。
    // ・生産材＝この州が作っている財を、**州の生産量 ÷ 世界生産**の高い順に3つ。
    //   （量とシェアは同じ2つの数の比＝「0.6k が世界の12%」と読めて矛盾しない）
    // ・輸出材＝**所有国の輸出額 ÷ 世界輸出額**の高い順に3つ。⚠輸出だけ国単位なのは、州の Market.LastExport が
    //   月初にゼロへ戻る（四半期のうち2ヶ月は0）ためで、四半期を通して残る台帳は World.ExportsByGood しかない。
    //   量は同じ台帳を世界価格で割った月割＝貿易タブの輸出一覧と同一の式（UI側で別の式を作らない）。
    // ── 足切り（幻の生産）：世界計が事実上0の財は微小値どうしの比になり、**シェア順に並べるとその幻が上位を独占する**。
    //   絶対量では切れない（穀物と火砲では桁が違う）ので、**世界市場に占める重み**＝世界の生産額（数量×参照価格）／
    //   輸出額に対する比で切る。該当が無ければ「なし」（⚠2026-09-23：1材＝1行へ変えたので「行は常に2本」ではなくなった＝最大で 生産材3行＋輸出材3行）。
    //   実測2026-08-10で踏んだ2つの罠と、それぞれに要った桁：
    //   ①生産材＝1788年のパリが「チョコレート 世界の35%」（本当の主産業の石炭は1%で押し出されていた）→ 0.5%で解消。
    //   ②輸出材＝全州が「戦闘機 世界の63%」（まだ誰も作れない財＝各州の初期在庫が決済に載っただけ）。額シェアでは切れず、
    //     **世界のどこかで実際に作られているか**で切る。⚠この生産ゲートに①と同じ0.5%を課すと厳しすぎ、貿易タブに
    //     世界シェア2%/1%で出ている実在の輸出（弾薬・火薬）まで消えて全州が「—」になった＝1万分の1へ緩める。
    private const double MarketPresenceShare = 0.005;    // 生産材の候補：世界の生産額に占める重みの下限（これ未満は世界市場が無い扱い）
    private const double MadeInWorldShare = 0.0001;      // 輸出材の候補：世界のどこかで実際に作られている最低線（＝未来技術の財を排除）
    private const double ShareFloor = 0.001;             // 表示するシェアの下限＝0.1%（「世界の0%」という無内容な行を出さない）
    private const double DisplayGrainQty = 50.0;         // 表示粒度の下限＝Qty表記で0.1k以上に見える最小量（0.0kの幻が世界シェアを名乗らない・作者指摘2026-08-16）
    private void AppendWorldShare(MapHoverInfo info, Region rg, World ownerWorld)
    {
        var top3Prod = new List<(string Name, double Amt, double Share)>();
        var top3Exp = new List<(string Name, double Amt, double Share)>();
        var wp = _sim.Market.Price;
        var oeg = ownerWorld.ExportsByGood;
        var wprod = _worldProdByGood;
        int gn = System.Math.Min(rg.Markets.Length, Catalog.Goods.Length);
        double prodFloor = MarketPresenceShare * _worldProdValueTotal;   // 生産材の候補：財ごとの世界生産**額**の下限
        double madeFloor = MadeInWorldShare * _worldProdValueTotal;      // 世界のどこかで作られている最低線（輸出材の生産ゲート）
        double expFloor = MadeInWorldShare * _worldExpTotal;             // 輸出材の候補：財ごとの世界輸出額の下限（無視できる取引だけを切る）
        for (int gi = 0; gi < Catalog.Goods.Length; gi++)
        {
            if (Catalog.Goods[gi].Tier == GoodTier.Service || Catalog.Goods[gi].NonTradeable) continue;   // 役務・非交易財は世界市場を持たない
            double refPrice = gi < Catalog.RefPrices.Length ? Catalog.RefPrices[gi] : 0;
            double worldValue = wprod is not null && gi < wprod.Length ? wprod[gi] * refPrice : 0;
            bool existsInWorld = wprod is not null && gi < wprod.Length && wprod[gi] > 1e-9 && worldValue >= madeFloor;
            if (existsInWorld && worldValue >= prodFloor && gi < gn)
            {
                double amt = rg.Markets[gi].LastProduced;
                double share = amt / wprod![gi];
                if (amt >= DisplayGrainQty && share >= ShareFloor) top3Prod.Add((Catalog.Goods[gi].Name, amt, share));
            }
            if (existsInWorld && _worldExpByGood is { } wexp && gi < wexp.Length && wexp[gi] >= expFloor && wexp[gi] > 1e-9
                && oeg is not null && gi < oeg.Length && oeg[gi] > 1e-9)
            {
                double p = gi < wp.Count ? System.Math.Max(wp[gi], 1e-9) : 1.0;
                double share = oeg[gi] / wexp[gi];
                double qty = oeg[gi] / p / ownerWorld.Coef.MonthsPerClearing;   // 清算単位→月割＝貿易タブの輸出一覧と同一の式（2026-08-23）
                // 作者指摘2026-08-16「輸出0.0kなのに世界の83%」＝世界貿易が細い間は微小値どうしの比が高シェアを名乗る。
                // 表示粒度未満（Qtyで0.0kに丸まる量）は世界シェアを語る資格がない＝量の下限で切る（分母側の相対floorでは細い世界を切れない）。
                if (share >= ShareFloor && qty >= DisplayGrainQty) top3Exp.Add((Catalog.Goods[gi].Name, qty, share));
            }
        }
        // シェア降順→同率は量の多い順→名前順（同率が並ぶ序盤でも並びが揺れない＝決定論）
        static int ByShare((string Name, double Amt, double Share) x, (string Name, double Amt, double Share) y)
        {
            int c = y.Share.CompareTo(x.Share); if (c != 0) return c;
            c = y.Amt.CompareTo(x.Amt); return c != 0 ? c : string.CompareOrdinal(x.Name, y.Name);
        }
        top3Prod.Sort(ByShare);
        top3Exp.Sort(ByShare);
        // 1%未満は小数1桁（P0だと全て「0%」に潰れて情報が消える）。1%以上は他のUIと同じ P0。
        // 表記＝「材名 量(世界n%)」（作者指摘2026-08-16「材名(量) 世界のn% は読みづらい」＝POPタブ規模の「量(比%)」と同じ読み順に統一）。
        // 該当なしは「なし」（旧「—」・無理に3つ出さない＝資格を満たした分だけ並ぶ）。
        // ★2026-09-23（作者指摘「コークス 0.3k(世 / 界0.1%)」＝語の途中で折り返す）：読点1行を捨てて**1材＝1行**にする。
        //   日本語は語境界を持たないので TextWrapping は文字の途中で割る（ProductionTab が全角スペース区切りの断片を
        //   WrapPanel へ並べているのと同じ理由）。ここは XAML を持たない側なので、改行を材の切れ目に自分で入れる＝
        //   1材は最長でも「ジャガイモ 1234.5k(世界100%)」≈190px でパネルの内幅470px（GameView.axaml:304 Width=500・Padding14）に必ず収まり、
        //   折返しそのものが起きない。2行目以降は全角スペース1つ下げて頭の「生産材:」の続きだと分かる形にする。
        static string Top3(List<(string Name, double Amt, double Share)> src)
            => src.Count == 0 ? "なし" : string.Join("\n　", src.Take(3).Select(t => $"{t.Name} {Qty(t.Amt)}(世界{(t.Share < 0.01 ? t.Share.ToString("P1") : t.Share.ToString("P0"))})"));
        info.Lines.Add(new MapHoverLine { Text = $"生産材: {Top3(top3Prod)}", Brush = HoverText });
        info.Lines.Add(new MapHoverLine { Text = $"輸出材: {Top3(top3Exp)}", Brush = HoverText });
    }

    /// <summary>州ホバーの世界シェアの分母（財別の世界計）。エンジンの実式をそのまま合算する＝
    /// 生産は <c>Market.LastProduced</c>（全モデル国の active 州）、輸出は <c>World.ExportsByGood</c>（国別×財別の決済額）。
    /// 生産タブの <c>BuildTradeInfo</c>・貿易タブの <c>RefreshTrade</c> と同一の分母（UI側で別の式を作らない）。</summary>
    private void BuildWorldTotals()
    {
        int gc = Catalog.Goods.Length;
        var prod = new double[gc];
        var exp = new double[gc];
        foreach (var sim in _sim.Nations)
        {
            var w = sim.World;
            foreach (var rr in w.Regions)
            {
                if (rr.State != RegionState.Active) continue;
                int n = System.Math.Min(rr.Markets.Length, gc);
                for (int gi = 0; gi < n; gi++) prod[gi] += rr.Markets[gi].LastProduced;
            }
            if (w.ExportsByGood is { } eg)
                for (int gi = 0; gi < gc && gi < eg.Length; gi++) exp[gi] += eg[gi];
        }
        // 足切りの基準＝世界の総生産額／総輸出額（財の重みを測る母数）。生産は参照価格で額へ直す
        // （地図の○の大きさ RegionProduction と同じ評価軸）。輸出は既に世界価格建ての額。
        double prodValue = 0, expValue = 0;
        for (int gi = 0; gi < gc; gi++)
        {
            if (Catalog.Goods[gi].Tier == GoodTier.Service || Catalog.Goods[gi].NonTradeable) continue;
            if (gi < Catalog.RefPrices.Length) prodValue += prod[gi] * Catalog.RefPrices[gi];
            expValue += exp[gi];
        }
        _worldProdByGood = prod; _worldExpByGood = exp;
        _worldProdValueTotal = prodValue; _worldExpTotal = expValue;
    }

    // 植民地/未領有標的のホバー：名称（帰属）・不満・海峡/運河（hostなら）・生産物（名前のみ・4品目/行）。
    private MapHoverInfo BuildColonyHover(Colony col, string owner)
    {
        var info = new MapHoverInfo { Title = $"{col.Name}　{owner}" };   // ★2026-09-23：州ホバーと同じ＝owner が「アメリカ(シャムの属国)の植民地」のとき括弧が二重にならない
        info.Lines.Add(new MapHoverLine { Text = $"不満 {col.Militancy:P0}　規模指数 {col.Population:F1}　開発度 {col.Infra:P0}", Brush = HoverDim });   // 開発度＝R13植民地州単位化（発展した植民地を奪えば高産出を引き継ぐ）
        AppendChokepoint(info, col.Name);
        AppendMegaprojects(info, col.Name);   // ★A-13
        var names = (col.Resources ?? []).Where(id => id >= 0 && id < Catalog.Goods.Length).Select(id => Catalog.Goods[id].Name).ToList();
        if (names.Count > 0)
        {
            info.Lines.Add(new MapHoverLine { Text = "生産物:", Brush = HoverHead, Weight = FontWeight.Bold });
            info.Lines.Add(new MapHoverLine { Text = string.Join("、", names), Brush = HoverText });   // 読点区切りの1行（折返しに任せる）
        }
        return info;
    }
}
