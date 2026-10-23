using System;
using Avalonia;
using Avalonia.Controls;

namespace PriceOfBread.App.Controls;

/// <summary>欄（<see cref="Section"/>）の中身の高さを、欄の高さに合わせて固定する。欄の中身は欄の縦の流れ（ScrollViewer）の中に載り、高さを無限として測られる＝
/// 共通の表（<see cref="TableView"/>）が見出しを止めて行だけを流すには、有限の高さを渡す必要がある。
/// 高さ＝欄の高さ − 見出しの 1 行（<see cref="PagePlan.HeaderHeight"/>）− 見出しの下線 − 中身の上下の余白（欄が中身に付ける Margin 4+6）− 欄の枠（上下 1+1）。
/// 欄の外枠（PageGrid・Section）には手を入れず、ページの側で呼ぶ。</summary>
public static class SectionFit
{
    /// <summary>見出しの下線 1 ＋ 中身の上下の余白 10 ＋ 枠 2 ＋ 丸めの安全 1（これを超えると欄の縦の流れが出て、見出しが流れてしまう）。</summary>
    private const double Reserved = 1 + 10 + 2 + 1;

    /// <summary>欄の大きさが変わるたびに、中身の高さを欄の空きに合わせる。</summary>
    public static void Attach(Section section, Control content)
    {
        void Apply()
        {
            double h = section.Bounds.Height - PagePlan.HeaderHeight - Reserved;
            content.Height = h > 0 ? h : 0;
        }
        section.PropertyChanged += (_, e) => { if (e.Property == Visual.BoundsProperty) Apply(); };
        Apply();
    }
}
