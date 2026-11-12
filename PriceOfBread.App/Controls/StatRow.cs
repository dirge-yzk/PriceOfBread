using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace PriceOfBread.App.Controls;

/// <summary>名前と値の 1 行（左に名前・右に値・必要なら下に小さな注記 1 行）。ページの欄に並ぶ国の量（政体・協力度・総動員の規模など）の共通の部品。
/// 欄の中の行の左端と値の右端は、どの欄でも同じ余白で揃う。Avalonia の描画の部品だけ（XAML を持たない）＝結線は 4 つの値（呼び手の XAML が x:DataType 付きで束縛する）。</summary>
public sealed class StatRow : Border
{
    public static readonly StyledProperty<string> LabelProperty = AvaloniaProperty.Register<StatRow, string>(nameof(Label), "");
    public static readonly StyledProperty<string> ValueProperty = AvaloniaProperty.Register<StatRow, string>(nameof(Value), "");
    public static readonly StyledProperty<string?> NoteProperty = AvaloniaProperty.Register<StatRow, string?>(nameof(Note));
    public static readonly StyledProperty<IBrush?> ValueBrushProperty = AvaloniaProperty.Register<StatRow, IBrush?>(nameof(ValueBrush));

    public string Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    /// <summary>行の下に常に見せる小さな注記（null か空なら出さない）。ホバーは廃止した（作者の指示 2026-11-11）。</summary>
    public string? Note { get => GetValue(NoteProperty); set => SetValue(NoteProperty, value); }
    /// <summary>値の色（null なら既定）。</summary>
    public IBrush? ValueBrush { get => GetValue(ValueBrushProperty); set => SetValue(ValueBrushProperty, value); }

    private readonly TextBlock _label = new() { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _note = new() { IsVisible = false, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _value = new() { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(8, 0, 0, 0) };

    static StatRow()
    {
        LabelProperty.Changed.AddClassHandler<StatRow>((r, _) => r._label.Text = r.Label);
        ValueProperty.Changed.AddClassHandler<StatRow>((r, _) => r._value.Text = r.Value);
        NoteProperty.Changed.AddClassHandler<StatRow>((r, _) => { r._note.Text = r.Note; r._note.IsVisible = !string.IsNullOrEmpty(r.Note); });
        ValueBrushProperty.Changed.AddClassHandler<StatRow>((r, _) => r.ApplyBrush());
    }

    public StatRow()
    {
        Padding = new Thickness(0, 2);
        _label.FontSize = Num("FontBody", 13);
        _label.Foreground = Brush("InkSoftBrush", Brushes.Gray);
        _value.Classes.Add("val");
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        Grid.SetColumn(_value, 1);
        grid.Children.Add(_label); grid.Children.Add(_value);
        _note.FontSize = Num("FontTiny", 11);
        _note.Foreground = Brush("InkMutedBrush", Brushes.Gray);
        Grid.SetRow(_note, 1); Grid.SetColumnSpan(_note, 2);
        grid.RowDefinitions = new RowDefinitions("Auto,Auto");
        grid.Children.Add(_note);
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
