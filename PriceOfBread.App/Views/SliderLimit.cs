using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace PriceOfBread.App.Views;

/// <summary>
/// スライダーの稼働域制限。軸（Minimum/Maximum）は常に全幅のまま、thumb がこの値で物理的に止まる。
/// VMセッターのクランプ＋SnapBackは再通知方式＝ドラッグ中は素通りするため、コントロールの Value 変更を
/// 同期でクランプする（ドラッグ中もハードストップ）。制限が動的に狭まった場合（緊縮の逆進フロア等）は
/// thumb を制限位置へ引き戻し、双方向バインディング経由で目標値も追従する。
/// さらに、制限位置に「これ以上動かせない」壁のマーカーを AdornerLayer で描画する（2026-07-20）。
/// AdornerLayer はウィンドウ級で常駐するため、GameView が作り直される（新規ゲーム/ロード/タイトル復帰）
/// たびにマーカーCanvasが取り残されると旧VM+シム全体を保持し続ける。DetachedFromVisualTree で Canvas を
/// 明示的に除去し、この保持リークを断つ。
/// </summary>
public static class SliderLimit
{
    public static readonly AttachedProperty<double> MaxProperty =
        AvaloniaProperty.RegisterAttached<Slider, double>("Max", typeof(SliderLimit), double.NaN);
    public static readonly AttachedProperty<double> MinProperty =
        AvaloniaProperty.RegisterAttached<Slider, double>("Min", typeof(SliderLimit), double.NaN);
    private static readonly AttachedProperty<bool> HookedProperty =
        AvaloniaProperty.RegisterAttached<Slider, bool>("Hooked", typeof(SliderLimit));
    private static readonly AttachedProperty<Canvas?> MarkerProperty =
        AvaloniaProperty.RegisterAttached<Slider, Canvas?>("Marker", typeof(SliderLimit));
    private static readonly AttachedProperty<IDisposable?> AncestorWatchProperty =
        AvaloniaProperty.RegisterAttached<Slider, IDisposable?>("AncestorWatch", typeof(SliderLimit));

    // 壁の色：くすんだ煉瓦色（羊皮紙テーマで「停止」を意味する）。半透明で軸を覆う。
    private static readonly IBrush WallBrush = new SolidColorBrush(Color.FromArgb(0xC0, 0xA0, 0x39, 0x2B));
    private const double ThumbInsetFallback = 10.0;  // thumb 半径ぶんの内側寄せ。実測（PART_Track の Thumb 幅/2）が取れない時だけ使う
    private const double WallHeight = 6.0;           // 溝（≈4px）から上下1pxだけ覗く刻み。これ以上大きくしない（2026-07-26/27ユーザー指摘）

    public static void SetMax(Slider s, double v) => s.SetValue(MaxProperty, v);
    public static double GetMax(Slider s) => s.GetValue(MaxProperty);
    public static void SetMin(Slider s, double v) => s.SetValue(MinProperty, v);
    public static double GetMin(Slider s) => s.GetValue(MinProperty);

    static SliderLimit()
    {
        var observer = new LimitObserver();
        MaxProperty.Changed.Subscribe(observer);
        MinProperty.Changed.Subscribe(observer);
    }

    private sealed class LimitObserver : IObserver<AvaloniaPropertyChangedEventArgs<double>>
    {
        public void OnNext(AvaloniaPropertyChangedEventArgs<double> e)
        {
            if (e.Sender is not Slider s) return;
            Hook(s);
            Enforce(s);
            UpdateMarker(s);
        }
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }

    private static void Hook(Slider s)
    {
        if (s.GetValue(HookedProperty)) return;
        s.SetValue(HookedProperty, true);
        s.PropertyChanged += static (sender, e) =>
        {
            if (e.Property == RangeBase.ValueProperty && sender is Slider sl) Enforce(sl);
        };
        // レイアウト確定・リサイズのたびに壁位置を再計算（Bounds監視は初回レイアウトでも発火する）。
        // 自己循環（s→observable→observer→s）＝外部ルートが無ければ回収可。外部ルートは下の Canvas 除去で断つ。
        s.GetObservable(Visual.BoundsProperty).Subscribe(new BoundsObserver(s));
        // 実効可視領域の監視：AdornerLayer は常駐でスライダーのタブ切替/スクロールに追従しないため、
        // 見えなくなったら壁マーカーを隠す（タブを切り替えても赤線が画面に残る不具合の修正・2026-07-20）。
        // EffectiveViewportChanged はスクロールでは発火するが、**祖先が IsVisible=false になった時は発火しない**
        // （測定も配置もされない要素にはビューポートが配られない）。タブは全タブの内容を同じ Grid に重ねて
        // IsVisible で切り替える作りなので、財政タブの壁だけが他タブの上に残っていた（作者報告・2026-08-10）。
        // ＝祖先の IsVisible そのものを見張る。
        s.EffectiveViewportChanged += static (sender, _) => { if (sender is Slider sl) UpdateMarker(sl); };
        s.AttachedToVisualTree += static (sender, _) => { if (sender is Slider sl) { WatchAncestors(sl); UpdateMarker(sl); } };
        if (s.IsAttachedToVisualTree()) WatchAncestors(s);   // 既にツリーに居る（＝バインド解決が接続より後）なら AttachedToVisualTree は二度と来ない
        // ビジュアルツリーから外れたら Canvas を常駐 AdornerLayer から除去（保持リーク防止）。再アタッチ時は Bounds 発火で再生成。
        s.DetachedFromVisualTree += static (sender, _) =>
        {
            if (sender is not Slider sl) return;
            var canvas = sl.GetValue(MarkerProperty);
            if (canvas?.Parent is Panel layer) layer.Children.Remove(canvas);
            sl.SetValue(MarkerProperty, null);
            sl.GetValue(AncestorWatchProperty)?.Dispose();   // 祖先の購読も解く（外れた木を掴み続けない）
            sl.SetValue(AncestorWatchProperty, null);
        };
    }

    /// <summary>自分と全祖先の IsVisible を購読し、変化のたびに壁マーカーを描き直す（2026-08-10）。
    /// 購読は再アタッチのたびに張り直す＝古い木への参照を残さない。</summary>
    private static void WatchAncestors(Slider s)
    {
        s.GetValue(AncestorWatchProperty)?.Dispose();
        var subs = new List<IDisposable>();
        foreach (var v in s.GetSelfAndVisualAncestors())
            subs.Add(v.GetObservable(Visual.IsVisibleProperty).Subscribe(new VisibilityObserver(s)));
        s.SetValue(AncestorWatchProperty, new Subscriptions(subs));
    }

    private sealed class VisibilityObserver(Slider s) : IObserver<bool>
    {
        public void OnNext(bool value) => UpdateMarker(s);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }

    private sealed class Subscriptions(List<IDisposable> items) : IDisposable
    {
        public void Dispose() { foreach (var d in items) d.Dispose(); items.Clear(); }
    }

    private sealed class BoundsObserver(Slider s) : IObserver<Rect>
    {
        public void OnNext(Rect value) => UpdateMarker(s);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }

    private static void Enforce(Slider s)
    {
        double max = s.GetValue(MaxProperty);
        if (!double.IsNaN(max) && s.Value > max) s.Value = max;
        double min = s.GetValue(MinProperty);
        if (!double.IsNaN(min) && s.Value < min) s.Value = min;
    }

    private static void UpdateMarker(Slider s)
    {
        var layer = AdornerLayer.GetAdornerLayer(s);
        if (layer is null) return;

        var canvas = s.GetValue(MarkerProperty);
        if (canvas is null)
        {
            canvas = new Canvas { IsHitTestVisible = false, ClipToBounds = false };
            s.SetValue(MarkerProperty, canvas);
            layer.Children.Add(canvas);
            AdornerLayer.SetAdornedElement(canvas, s);
        }
        // スライダーが実効的に見えない（タブ非表示・折りたたみ）ときは壁マーカーも隠す＝赤線が他タブに残らない。
        canvas.IsVisible = s.IsEffectivelyVisible;
        canvas.Children.Clear();
        if (!s.IsEffectivelyVisible) return;

        double w = s.Bounds.Width, h = s.Bounds.Height;
        double range = s.Maximum - s.Minimum;
        if (!(w > 0) || !(range > 0)) return;

        // 溝（PART_Track）の実位置から描く（2026-07-27）：箱の中心決め打ちだと、テーマの上下余白行が非対称だったり
        // Slider に Height を被せて内部Gridが上詰めになったときに、赤い刻みだけが溝から浮いて別物に見える（項(2)の真因）。
        // 実測が取れないとき（テンプレート未適用の初回など）だけ箱の中心へフォールバック。
        double trackTop = 0, trackH = h, thumbInset = ThumbInsetFallback;
        if (s.GetVisualDescendants().OfType<Track>().FirstOrDefault() is { } track && track.Bounds.Height > 0
            && track.TranslatePoint(new Point(0, 0), s) is { } tp)
        {
            trackTop = tp.Y;
            trackH = track.Bounds.Height;
            if (track.Thumb is { } th && th.Bounds.Width > 0) thumbInset = th.Bounds.Width / 2.0;
        }
        double usable = Math.Max(1.0, w - thumbInset * 2);

        void AddWall(double val)
        {
            double frac = (val - s.Minimum) / range;
            if (!(frac >= 0 && frac <= 1)) return;
            double x = thumbInset + frac * usable;
            double barH = Math.Min(WallHeight, Math.Max(4.0, trackH));
            var bar = new Border
            {
                Width = 2,
                Height = barH,
                Background = WallBrush,
                CornerRadius = new CornerRadius(1),
            };
            Canvas.SetLeft(bar, x - 1);
            Canvas.SetTop(bar, trackTop + (trackH - barH) / 2);   // 溝の中心に合わせる
            canvas.Children.Add(bar);
        }

        double max = s.GetValue(MaxProperty);
        if (!double.IsNaN(max) && max < s.Maximum) AddWall(max);
        double min = s.GetValue(MinProperty);
        if (!double.IsNaN(min) && min > s.Minimum) AddWall(min);
    }
}
