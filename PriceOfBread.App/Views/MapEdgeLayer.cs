using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PriceOfBread.App.ViewModels;

namespace PriceOfBread.App.Views;

/// <summary>
/// 地図の線（陸グラフ・海グラフ）を**1つのコントロールで一括描画**するレイヤ（2026-08-01）。
///
/// ── なぜ要るか ────────────────────────────────────────────────────────────
/// 旧＝<c>ItemsControl</c> の <c>ItemTemplate</c> に <c>&lt;Line/&gt;</c> を置いていたため、
/// **辺1本につき Avalonia のコントロールが1つ**生成されていた。本数は
///   陸グラフ 700辺 ＋ 海グラフの辺　を、反対子午線を跨ぐ表示のため**それぞれ×3**（k=−1,0,1）
/// ＝おおむね **3,000〜4,000個の視覚要素**。ズームやパンのたびに全要素のレイアウトと描画が走り、
/// ユーザー報告「マップ全体が重い。ゲームに支障をきたす」の直接の原因になっていた。
///
/// 本クラスは同じ絵を **1コントロールの1回の Render** で描く。要素数 4,000 → 1。
/// ⚠**見た目は1ピクセルも変えない**（同じ点・同じ色・同じ太さで DrawLine するだけ）。
///
/// ── 速さのための細部 ──────────────────────────────────────────────────────
/// ・<see cref="Pen"/> を辺ごとに作らない。線は色と太さでまとまって並ぶので、直前と同じなら使い回す。
/// ・線の集合は差し替え時にしか変わらないので <c>AffectsRender</c> で再描画を促すだけでよい。
/// </summary>
public sealed class MapEdgeLayer : Control
{
    public static readonly StyledProperty<IReadOnlyList<MapEdgeLine>?> EdgesProperty =
        AvaloniaProperty.Register<MapEdgeLayer, IReadOnlyList<MapEdgeLine>?>(nameof(Edges));

    public IReadOnlyList<MapEdgeLine>? Edges
    {
        get => GetValue(EdgesProperty);
        set => SetValue(EdgesProperty, value);
    }

    static MapEdgeLayer() => AffectsRender<MapEdgeLayer>(EdgesProperty);

    public MapEdgeLayer() => IsHitTestVisible = false;   // 線はクリックを奪わない（州○の当たり判定を妨げない）

    public override void Render(DrawingContext ctx)
    {
        var edges = Edges;
        if (edges is null || edges.Count == 0) return;
        IBrush? curBrush = null;
        double curThickness = double.NaN;
        Pen? pen = null;
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            // ⚠否定形で比較する（太さに NaN が紛れても Pen を作り直す側へ倒す）
            if (!ReferenceEquals(curBrush, e.Brush) || !(curThickness == e.Thickness))
            {
                curBrush = e.Brush;
                curThickness = e.Thickness;
                pen = new Pen(curBrush, curThickness);
            }
            ctx.DrawLine(pen!, e.Start, e.End);
        }
    }
}
