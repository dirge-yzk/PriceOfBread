using System;
using System.Collections.Generic;
using Avalonia.Controls;
using PriceOfBread.App.Layout;

namespace PriceOfBread.App.Views;

/// <summary>画面の中央＝いま出している 1 つ（7 ページか地図）をそのまま映す箱。枠も見出しの行も持たない（作者の指示 2026-11-04：分割とページの囲いは廃止）。
/// 中身は <see cref="LayoutModel.Current"/> が決める＝ここは映すだけ。ページの部品は種類ごとに 1 度だけ作って使い回す（行き来しても並べ替えの状態などが残る）。
/// 外れたページは DataContext を外す（見えない部品が毎 tick の更新を受けない）。</summary>
public sealed class PageHost : Border
{
    private LayoutModel? _layout;
    private Func<Slot, Control>? _factory;

    public Slot? Current { get; private set; }
    public Control? Page { get; private set; }

    /// <summary>配置とページの作り手をつなぐ。</summary>
    public void Attach(LayoutModel layout, Func<Slot, Control> factory)
    {
        if (_layout is not null) _layout.Changed -= Apply;
        _layout = layout; _factory = factory;
        layout.Changed += Apply;
        Apply();
    }

    public void Detach()
    {
        if (_layout is not null) _layout.Changed -= Apply;
        _layout = null;
        SetContent(null, null);
    }

    /// <summary>いま載っているページの部品（撮影の検査用）。</summary>
    public IEnumerable<(int Pane, Slot Slot, Control Page)> Mounted()
    {
        if (Current is { } s && Page is { } p && IsVisible) yield return (0, s, p);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (Page is { } page) page.DataContext = DataContext;
    }

    private void Apply()
    {
        if (_layout is null || _factory is null) return;
        var want = _layout.Current;
        if (Current == want) return;
        SetContent(null, null);   // 外れる物を先に外してから載せる（同じ部品が 2 か所の親に付く瞬間を作らない）
        var page = _factory(want);
        page.DataContext = DataContext;
        SetContent(want, page);
    }

    private void SetContent(Slot? slot, Control? page)
    {
        if (Page is { } old) { old.DataContext = null; Child = null; }
        Current = slot; Page = page;
        if (page is not null) Child = page;
    }
}
