using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace PriceOfBread.App.Controls;

/// <summary>名前と値の 1 行（左に名前・右に値・ホバーに内訳）。ページの欄に並ぶ国の量（政体・協力度・総動員の規模など）の共通の部品。
/// 欄の中の行の左端と値の右端は、どの欄でも同じ余白で揃う。Avalonia の描画の部品だけ（XAML を持たない）＝結線は 4 つの値（呼び手の XAML が x:DataType 付きで束縛する）。</summary>
public sealed class StatRow : Border
{
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<StatRow, string>(nameof(Label), "");
    public static readonly StyledProperty<string> ValueProperty = AvaloniaProperty.Register<StatRow, string>(nameof(Value), "");
    public static readonly StyledProperty<string?> TipProperty = AvaloniaProperty.Register<StatRow, string?>(nameof(Tip));
    public static readonly StyledProperty<IBrush?> ValueBrushProperty = AvaloniaProperty.Register<StatRow, IBrush?>(nameof(ValueBrush));

    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    /// <summary>ホバーの文（null か空なら吹き出しを出さない）。</summary>
    public string? Tip { get => GetValue(TipProperty); set => SetValue(TipProperty, value); }
    /// <summary>値の色（null なら既定）。</summary>
    public IBrush? ValueBrush { get => GetValue(ValueBrushProperty); set => SetValue(ValueBrushProperty, value); }

    private readonly TextBlock _label = new() { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _value = new() { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(8, 0, 0, 0) };

    static StatRow()
    {
        LabelProperty.Changed.AddClassHandler<StatRow>((r, _) => r._label.Text = r.Label);
        ValueProperty.Changed.AddClassHandler<StatRow>((r, _) => r._value.Text = r.Value);
        TipProperty.Changed.AddClassHandler<StatRow>((r, _) => ToolTip.SetTip(r, string.IsNullOrEmpty(r.Tip) ? null : r.Tip));
        ValueBrushProperty.Changed.AddClassHandler<StatRow>((r, _) => r.ApplyBrush());
    }

    public StatRow()
    {
        Background = Brushes.Transparent;   // 透明でも当たる＝ホバーを受ける
        Padding = new Thickness(0, 2);
        ToolTip.SetShowDelay(this, 200);
        _label.FontSize = Num("FontBody", 13);
        _label.Foreground = Brush("InkSoftBrush", Brushes.Gray);
        _value.Classes.Add("val");
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        Grid.SetColumn(_value, 1);
        grid.Children.Add(_label); grid.Children.Add(_value);
        Child = grid;
    }

    private void ApplyBrush()
    {
        if (ValueBrush is { } b) _value.Foreground = b; else _value.ClearValue(TextBlock.ForegroundProperty);
    }

    private static IBrush Brush(string key, IBrush fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is IBrush b ? b : fallback;

    private static double Num(string key, double fallback)
        => Application.Current is { } app && app.TryFindResource(key, app.ActualThemeVariant, out var v) && v is double d ? d : fallback;
}
