// GameViewModel.MapFill.cs ＝ 地図の政治塗り分け（作者指示2026-09-02）のVM側。
// 種（所持州＋植民地）を集めて MapFill へ渡し、焼き上がった1枚を地図の背景の上に敷く。
// ★F7（2026-09-28）：州○を廃止＝州のホバー・クリックは**この塗りの割り当て**（格子→種→州/植民地）で当てる（MapHitAt）。
// ⚠塗り自体の規約（陸を広げる・上限＝州配置の最長間隔・種の無い島へは海を渡る）は MapFill 側の説明を正とする。ここは「いつ焼き直すか」だけ。
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.SimCore;

namespace PriceOfBread.App.ViewModels;

public partial class GameViewModel
{
    [ObservableProperty] private WriteableBitmap? _politicalFill;
    // ★支配度（2026-11-03 作者「支配度切り替えは廃止し、100%でない場合その分だけ塗りを黒くする」）：切替の釦と地図モードは廃止＝政治の塗りは常に支配度で暗くする（色だけ・アイコンなし）。
    // 支配度の暗さの刻み＝1/DominanceQ。**値も指紋も同じ量子化入力から作る**（量子化キャッシュの不変条件）。
    //   16 の根拠：DominanceSpeed=0.025（Coefficients.cs:2646）＞1/64 なので 64 刻みだとほぼ毎四半期 MapFill.Build（120ms級）が走る。1/16=0.0625 なら2〜3四半期に1回。
    private const int DominanceQ = 16;
    // ★政治塗りのLOD（2026-09-06・作者指示「マップスクロールがかなり重い」）：3600×1380 の塗りを引いた帯では 1倍（2000×780）の写しで描く。
    //   背景と同じ規則（View の ApplyLandLod が SetMapLod で知らせる）。写しは塗りが変わった時だけ作る（所有が変わる四半期のみ）。
    private Avalonia.Media.Imaging.RenderTargetBitmap? _politicalFillLo;
    private bool _mapLodLow = true;
    public Avalonia.Media.Imaging.Bitmap? PoliticalFillView => _mapLodLow && _politicalFillLo is not null ? _politicalFillLo : PoliticalFill;
    public void SetMapLod(bool low) { if (_mapLodLow == low) return; _mapLodLow = low; OnPropertyChanged(nameof(PoliticalFillView)); }
    partial void OnPoliticalFillChanged(WriteableBitmap? value)
    {
        _politicalFillLo = null;
        if (value is not null)
        {
            try
            {
                int pw = (int)MapData.MapWidth, ph = (int)MapData.MapHeight;
                var rtb = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(pw, ph), new Avalonia.Vector(96, 96));
                using (var ctx = rtb.CreateDrawingContext())
                using (ctx.PushRenderOptions(new Avalonia.Media.RenderOptions { BitmapInterpolationMode = Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality }))
                    ctx.DrawImage(value, new Avalonia.Rect(0, 0, value.PixelSize.Width, value.PixelSize.Height), new Avalonia.Rect(0, 0, pw, ph));
                _politicalFillLo = rtb;
            }
            catch { _politicalFillLo = null; }
        }
        OnPropertyChanged(nameof(PoliticalFillView));
    }

    private ulong _fillSignature;   // 直近に焼いた「誰がどこを持っているか」の指紋（変わった時だけ焼き直す）
    // ★F7：州の当たり＝いま表示している1枚の割り当て（格子→種の番号）と、種の番号→州/植民地。塗りと同時に差し替える＝絵と当たりが食い違わない。
    //   Aim＝クリックで狙いを付けられる植民地（他国の植民地だけ＝旧・植民地○の Colony と同じ条件）。
    private short[]? _fillHit;
    private List<(Region? Rg, Colony? Col, bool Aim)>? _fillTargets;
    private bool _fillBusy;         // 焼いている最中（重なって走らせない）

    /// <summary>所有が変わっていなくても焼き直す。地図の背景（＝塗りの型紙）が用意できた直後に View から1回だけ呼ぶ
    /// ＝型紙の無いまま焼いた初回の1枚（湖が染まる）を、次の四半期を待たずに置き換える。</summary>
    public void RebuildPoliticalFill()
    {
        _fillSignature = 0;
        RefreshPoliticalFill();
    }

    /// <summary>所有が変わっていれば塗りを焼き直す（四半期の地図更新から呼ぶ）。
    /// ⚠焼くのは背景スレッド＝征服や割譲の瞬間に時間が止まらない。種の採取だけはUIスレッドで行う
    /// （エンジンはUIスレッドで進む＝背景スレッドから World を読むと競合する）。</summary>
    private void RefreshPoliticalFill()
    {
        // 種＝active な本国州＋植民地（色は MapData.NationColor）＝UIに第二の所有データを置かない。
        var seeds = new List<MapFill.Seed>(384);
        var targets = new List<(Region? Rg, Colony? Col, bool Aim)>(384);   // seeds と同じ並び＝種の番号→州/植民地
        ulong sig = 1469598103934665603UL;   // FNV-1a 64の初期値（指紋＝この実行の中だけで比べる）
        for (int ni = 0; ni < _sim.Nations.Count; ni++)
        {
            var w = _sim.Nations[ni].World;
            var col = MapData.NationColor(w.SelfPower?.Name ?? w.Region.Name, ni);
            foreach (var rg in w.Regions)
            {
                if (!MapData.CityCoords.TryGetValue(rg.Name, out var co)) continue;
                // ★州の色＝支配度の 4 区分（PopTables.RuleParts の 1 本・表の〈支配度〉の列と州の窓の半円と同じ定義）を混ぜる（2026-11-22 作者指示）：
                //   国家の分＝持ち主の国の色・外国勢力の分＝その外国の国ごとの色（Region.ForeignRulers・ForeignShares の持分）・在地勢力の分＝黒・無統治の分＝透明（不透明度に掛ける）。
                //   ⚠値も指紋も量子化した入力（DominanceQ 刻みの整数）から作る＝「焼き直さない」と「同じ絵」が一致する（量子化キャッシュの不変条件）。
                var rp = PriceOfBread.SimCore.Display.PopTables.RuleParts(rg);
                int qState = QuantShare(rp[0].Share), qLocal = QuantShare(rp[1].Share), qUngov = QuantShare(rp[3].Share);
                int qForeign = System.Math.Max(0, DominanceQ - qState - qLocal - qUngov);   // 4 区分の合計＝DominanceQ に保つ（丸めの差は外国勢力が受ける）
                double inv = 1.0 / DominanceQ;
                double cr = qState * inv * col.R, cg = qState * inv * col.G, cb = qState * inv * col.B;
                sig = Mix(Mix(Mix(Mix(sig, (ulong)rg.Name.GetHashCode()), (ulong)ni), (ulong)qState), (ulong)(qLocal * 64 + qUngov));
                if (qForeign > 0)
                {
                    var (fr, fg, fb, fsig) = ForeignMix(rg, qForeign);
                    cr += fr * inv; cg += fg * inv; cb += fb * inv;
                    sig = Mix(sig, fsig);
                }
                int qCover = DominanceQ - qUngov;
                double cover = qCover * inv;
                Avalonia.Media.Color mixed = qCover <= 0 ? default : Avalonia.Media.Color.FromRgb(ToByte(cr / cover), ToByte(cg / cover), ToByte(cb / cover));
                seeds.Add(new MapFill.Seed(co.Lat, co.Lon, mixed, cover));
                targets.Add((rg, null, false));
            }
            if (w.SelfPower is { } sp)
                foreach (var colony in sp.ColonyList)
                {
                    if (Math.Abs(colony.Latitude) < 1e-9 && Math.Abs(colony.Longitude) < 1e-9) continue;   // 座標未登録(0,0)＝ギニア湾の海上
                    seeds.Add(new MapFill.Seed(colony.Latitude, colony.Longitude, col));
                    targets.Add((null, colony, sp != _player.SelfPower));
                    sig = Mix(Mix(sig, (ulong)(long)(colony.Latitude * 64 + colony.Longitude * 4096)), (ulong)ni);
                }
        }
        // 未領有の植民地化標的（抽象勢力＝Nation==null）＝**当たりだけの種**（塗らない）。どの国の塗りも届かない陸だけを取る＝塗りの絵は変わらない。
        int painted = seeds.Count;
        foreach (var p in _sim.Diplomacy.Powers)
        {
            if (p.Nation is not null) continue;
            foreach (var colony in p.ColonyList)
            {
                if (Math.Abs(colony.Latitude) < 1e-9 && Math.Abs(colony.Longitude) < 1e-9) continue;
                seeds.Add(new MapFill.Seed(colony.Latitude, colony.Longitude, default));
                targets.Add((null, colony, false));
                sig = Mix(Mix(sig, (ulong)(long)(colony.Latitude * 64 + colony.Longitude * 4096)), 0xFFFFUL);
            }
        }
        if (sig == _fillSignature) return;   // 領土も国も変わっていない＝焼き直さない（毎四半期の常のケース）
        if (_fillBusy) return;               // 前の1枚を焼いている最中＝指紋は更新せず次の更新で拾い直す
        _fillSignature = sig;
        _fillBusy = true;
        Task.Run(() =>
        {
            WriteableBitmap? bmp = null;
            short[]? hit = null;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try { bmp = MapFill.Build(seeds, painted, out hit); } catch { /* 塗りが出なくてもゲームは動く */ }
            if (sw.ElapsedMilliseconds >= 120) BootLog.Write($"⏱ 政治の塗り分け {sw.ElapsedMilliseconds}ms（種{seeds.Count}）");
            Dispatcher.UIThread.Post(() =>
            {
                if (bmp is not null) { PoliticalFill = bmp; _fillHit = hit; _fillTargets = targets; }
                _fillBusy = false;
            });
        });
    }

    /// <summary>★F7：地図のキャンバス座標（2000×780・東西の複製ぶんは折り返す）→そこに塗られている州／植民地。
    /// 当たりは塗りの画素そのもの＝見えている色の持ち主が返る（海岸の町・小島は種の吸着と同じ半径で最寄りの持ち主＝MapFill.HitAt）。海・未焼成は null。</summary>
    public (MapHoverInfo? Hover, Region? Region, Colony? AimColony) MapHitAt(double x, double y)
    {
        if (_fillHit is not { } hit || _fillTargets is not { } tg) return default;
        int s = MapFill.HitAt(hit, x, y);
        if (s < 0 || s >= tg.Count) return default;
        var t = tg[s];
        if (t.Rg is { } rg) return (_provHoverCache.GetValueOrDefault(rg), rg, null);
        if (t.Col is { } col) return (_colHoverCache.GetValueOrDefault(col), null, t.Aim ? col : null);
        return default;
    }

    /// <summary>uishot 用（2026-10-23）：緯度経度を画面の中央へ（島の塗りの無人の確かめ）。</summary>
    public void CenterOnLatLon(double lat, double lon) { var (x, y) = MapData.Project(lat, lon); CenterMap?.Invoke(x, y); }

    private static byte ToByte(double v) => (byte)System.Math.Round(!(v > 0) ? 0 : v < 255 ? v : 255);   // ⚠NaN は 0 へ（否定形）
    private static int QuantShare(double share) { if (!(share > 0)) return 0; if (!(share < 1)) return DominanceQ; return (int)(share * DominanceQ); }

    /// <summary>国名→色（模型国の並びの番号を代替の番号に使う＝国の塗りと同じ色）。見つからない名前は名前の文字列から決める。</summary>
    internal Avalonia.Media.Color NationColorByName(string name)
    {
        for (int ni = 0; ni < _sim.Nations.Count; ni++)
        {
            var w = _sim.Nations[ni].World;
            if ((w.SelfPower?.Name ?? w.Region.Name) == name) return MapData.NationColor(name, ni);
        }
        return MapData.NationColor(name, name.Length);
    }

    /// <summary>外国勢力の分（qForeign/DominanceQ）を、持分の比で外国ごとの色へ按分した色の和（0..255×その分）と指紋。持ち主の内訳が無いときは黒。</summary>
    private (double R, double G, double B, ulong Sig) ForeignMix(Region rg, int qForeign)
    {
        double total = 0;
        int n = System.Math.Min(rg.ForeignRulers.Length, rg.ForeignShares.Length);
        for (int k = 0; k < n; k++) if (rg.ForeignShares[k] > 0 && !string.IsNullOrEmpty(rg.ForeignRulers[k])) total += rg.ForeignShares[k];
        if (!(total > 0)) return (0, 0, 0, 0xF0F0UL);
        double r = 0, g = 0, b = 0; ulong sg = 0xF1F1UL;
        for (int k = 0; k < n; k++)
        {
            if (!(rg.ForeignShares[k] > 0) || string.IsNullOrEmpty(rg.ForeignRulers[k])) continue;
            int qk = (int)(rg.ForeignShares[k] / total * qForeign * 8);   // 指紋の刻み＝按分の 1/8（値も同じ整数から作る）
            double f = qk / 8.0;                                          // DominanceQ 刻みの分
            var c = NationColorByName(rg.ForeignRulers[k]);
            r += f * c.R; g += f * c.G; b += f * c.B;
            sg = Mix(Mix(sg, (ulong)rg.ForeignRulers[k].GetHashCode()), (ulong)qk);
        }
        return (r, g, b, sg);
    }

    private static ulong Mix(ulong h, ulong v) => (h ^ v) * 1099511628211UL;   // FNV-1a 64
}
