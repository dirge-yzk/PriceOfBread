// GameViewModel.MapFill.cs ＝ 地図の政治塗り分け（作者指示2026-09-02）のVM側。
// 種（所持州＋植民地）を集めて MapFill へ渡し、焼き上がった1枚を地図の背景の上に敷く。
// ★F7（2026-09-28）：州○を廃止＝州のホバー・クリックは**この塗りの割り当て**（格子→種→州/植民地）で当てる（MapHitAt）。
// ⚠塗り自体の規約（陸だけ・上限＝州配置の最長間隔）は MapFill 側の説明を正とする。ここは「いつ焼き直すか」だけ。
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
    // ★支配度の一般化 §7 地図モード「支配度」（2026-09-07）：政治の塗り分けを支配度の濃淡で焼く（色だけ・アイコンなし）。
    [ObservableProperty] private bool _mapDominanceMode;
    partial void OnMapDominanceModeChanged(bool value) { OnPropertyChanged(nameof(DominanceModeLabel)); RebuildPoliticalFill(); }
    // ★裁定②（2026-09-23）：濃さの元を休眠の Rule[0] から live な Region.Dominance へ替えた＝係数のゲートで隠す理由が無くなった（常に押せる）。
    //   ⚠名前は GameView.axaml:63 の IsVisible が参照している（コンパイル束縛＝消すとビルドが落ちる）。そちらを外すのは Views の担当。
    public bool RuleSharesLive => true;
    // 支配度モードの濃さの刻み＝1/DominanceQ。**値も指紋も同じ量子化入力から作る**（量子化キャッシュの不変条件）。
    //   16 の根拠：DominanceSpeed=0.025（Coefficients.cs:2646）＞1/64 なので 64 刻みだとほぼ毎四半期 MapFill.Build（120ms級）が走る。1/16=0.0625 なら2〜3四半期に1回。
    private const int DominanceQ = 16;
    public string DominanceModeLabel => MapDominanceMode ? "支配度: 表示" : "支配度";
    [RelayCommand] private void ToggleDominanceMode() => MapDominanceMode = !MapDominanceMode;
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
                if (rg.State != RegionState.Active) continue;
                if (!MapData.CityCoords.TryGetValue(rg.Name, out var co)) continue;
                // ★裁定②（2026-09-23 作者指示「支配度は国家でない割合だけ、色を薄くするか黒くする」）：濃さ＝live な rg.Dominance。
                //   旧＝休眠の rg.Rule[0]（常に 1.0＝濃淡が一度も出なかった）。1−支配度のぶんだけ薄くする（MapFill.cs:176 のアルファ変調）。
                //   ⚠旧コードは Seed に生値・指紋に ×64 の整数を渡していた＝1/64 未満しか動かない四半期は塗りが古いまま残った（量子化キャッシュの不変条件の破れ）。
                //     いまは wq ひとつから値も指紋も作る＝「焼き直さない」と「同じ絵」が一致する。
                double raw = rg.Dominance;
                if (!(raw > 0)) raw = 0; else if (!(raw < 1)) raw = 1;   // ⚠Math.Clamp は NaN を落とさない＝否定形で書く
                int wq = MapDominanceMode ? (int)(raw * DominanceQ) : DominanceQ;
                double wgt = wq / (double)DominanceQ;
                seeds.Add(new MapFill.Seed(co.Lat, co.Lon, col, wgt));
                targets.Add((rg, null, false));
                sig = Mix(Mix(Mix(sig, (ulong)rg.Name.GetHashCode()), (ulong)ni), (ulong)wq);
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

    private static ulong Mix(ulong h, ulong v) => (h ^ v) * 1099511628211UL;   // FNV-1a 64
}
