using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace PriceOfBread.App.Controls;

/// <summary>折り返す文の英数字を Times New Roman に揃える添付の値（2026-11-12 点検⑧）。
/// 折り返す長い文は、かなや漢字の直後の数字・記号が日本語側の字体（Noto Sans JP）へ寄ってしまう（「戦勝点+0.175」の数）。
/// そこで文を「英数字の連なり」と「それ以外」に割り、前者の Run にだけ Times New Roman を明示する（かなと漢字の落ち先は従来のまま）。
/// 使い方＝TextBlock の <c>Text</c> の代わりに <c>ctl:LatinText.Text="{Binding …}"</c>。
/// <c>NumberBrush</c>（任意）＝数字を含む連なりの字の色。見出しの字は淡い灰だが、見出しの中の数（予測・実績・順位）は本文の灰で読ませる（点検⑯）。</summary>
public static class LatinText
{
    public static readonly AttachedProperty<string?> TextProperty = AvaloniaProperty.RegisterAttached<TextBlock, string?>("Text", typeof(LatinText));
    public static string? GetText(TextBlock t) => t.GetValue(TextProperty);
    public static void SetText(TextBlock t, string? v) => t.SetValue(TextProperty, v);

    private static readonly FontFamily Latin = new("Times New Roman");
    // ASCII の可視文字と空白・£・＋の字の連なり（数字か英字を 1 つでも含む物だけを英数字の Run にする）
    private static readonly Regex Segment = new(@"[\x20-\x7E£]+", RegexOptions.Compiled);

    public static readonly AttachedProperty<IBrush?> NumberBrushProperty = AvaloniaProperty.RegisterAttached<TextBlock, IBrush?>("NumberBrush", typeof(LatinText));
    public static IBrush? GetNumberBrush(TextBlock t) => t.GetValue(NumberBrushProperty);
    public static void SetNumberBrush(TextBlock t, IBrush? v) => t.SetValue(NumberBrushProperty, v);

    static LatinText()
    {
        TextProperty.Changed.AddClassHandler<TextBlock>((tb, e) => Build(tb, e.NewValue as string, GetNumberBrush(tb)));
        NumberBrushProperty.Changed.AddClassHandler<TextBlock>((tb, e) => { if (GetText(tb) is { } t) Build(tb, t, e.NewValue as IBrush); });
    }

    /// <summary>コードから直接（欄の見出しなど）。<paramref name="numberBrush"/> が null でなければ、数字を含む連なりをその色にする。</summary>
    public static void Fill(TextBlock tb, string? text, IBrush? numberBrush) => Build(tb, text, numberBrush);

    private static void Build(TextBlock tb, string? text, IBrush? numberBrush)
    {
        tb.Text = null;
        if (string.IsNullOrEmpty(text)) { tb.Inlines?.Clear(); return; }
        var inlines = tb.Inlines ??= new InlineCollection();
        inlines.Clear();
        int pos = 0;
        foreach (Match m in Segment.Matches(text))
        {
            if (!HasAlnum(m.Value)) continue;
            if (m.Index > pos) inlines.Add(new Run(text[pos..m.Index]));
            var run = new Run(m.Value) { FontFamily = Latin };
            if (numberBrush is not null && HasDigit(m.Value)) run.Foreground = numberBrush;
            inlines.Add(run);
            pos = m.Index + m.Length;
        }
        if (pos < text.Length) inlines.Add(new Run(text[pos..]));
    }

    private static bool HasDigit(string s)
    {
        foreach (char c in s) if (char.IsAsciiDigit(c)) return true;
        return false;
    }

    private static bool HasAlnum(string s)
    {
        foreach (char c in s) if (char.IsAsciiLetterOrDigit(c)) return true;
        return false;
    }
}
