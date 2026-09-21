// GameViewModel.MapFill.cs ＝ 地図の政治塗り分け（作者指示2026-09-02）のVM側。
// 種（所持州＋植民地）を集めて MapFill へ渡し、焼き上がった1枚を地図の背景と州○の間に敷く。
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
    // ★支配度の一般化 §7 地図モード「支配度」（2026-09-07）：政治の塗り分けを国家区分の濃淡で焼く（色だけ・アイコンなし）。区分が動く世界（RuleSharesEnabled>0）でだけボタンを出す。
    [ObservableProperty] private bool _mapDominanceMode;
    partial void OnMapDominanceModeChanged(bool value) { OnPropertyChanged(nameof(DominanceModeLabel)); RebuildPoliticalFill(); }
    public bool RuleSharesLive => _player.Coef.RuleSharesEnabled > 0;
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
        // 種＝地図の○と同じ出所（active な本国州＋植民地・色は MapData.NationColor）＝UIに第二の所有データを置かない。
        var seeds = new List<MapFill.Seed>(384);
        ulong sig = 1469598103934665603UL;   // FNV-1a 64の初期値（指紋＝この実行の中だけで比べる）
        for (int ni = 0; ni < _sim.Nations.Count; ni++)
        {
            var w = _sim.Nations[ni].World;
            var col = MapData.NationColor(w.SelfPower?.Name ?? w.Region.Name, ni);
            foreach (var rg in w.Regions)
            {
                if (rg.State != RegionState.Active) continue;
                if (!MapData.CityCoords.TryGetValue(rg.Name, out var co)) continue;
                double wgt = MapDominanceMode && rg.Rule.Length == 4 ? Math.Clamp(rg.Rule[0], 0, 1) : 1.0;   // 支配度モード＝国家区分の濃さ
                seeds.Add(new MapFill.Seed(co.Lat, co.Lon, col, wgt));
                sig = Mix(Mix(Mix(sig, (ulong)rg.Name.GetHashCode()), (ulong)ni), (ulong)(wgt * 64));
            }
            if (w.SelfPower is { } sp)
                foreach (var colony in sp.ColonyList)
                {
                    if (Math.Abs(colony.Latitude) < 1e-9 && Math.Abs(colony.Longitude) < 1e-9) continue;   // 座標未登録(0,0)＝ギニア湾の海上
                    seeds.Add(new MapFill.Seed(colony.Latitude, colony.Longitude, col));
                    sig = Mix(Mix(sig, (ulong)(long)(colony.Latitude * 64 + colony.Longitude * 4096)), (ulong)ni);
                }
        }
        if (sig == _fillSignature) return;   // 領土も国も変わっていない＝焼き直さない（毎四半期の常のケース）
        if (_fillBusy) return;               // 前の1枚を焼いている最中＝指紋は更新せず次の更新で拾い直す
        _fillSignature = sig;
        _fillBusy = true;
        Task.Run(() =>
        {
            WriteableBitmap? bmp = null;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try { bmp = MapFill.Build(seeds); } catch { /* 塗りが出なくてもゲームは動く */ }
            if (sw.ElapsedMilliseconds >= 120) BootLog.Write($"⏱ 政治の塗り分け {sw.ElapsedMilliseconds}ms（種{seeds.Count}）");
            Dispatcher.UIThread.Post(() =>
            {
                if (bmp is not null) PoliticalFill = bmp;
                _fillBusy = false;
            });
        });
    }

    private static ulong Mix(ulong h, ulong v) => (h ^ v) * 1099511628211UL;   // FNV-1a 64
}
