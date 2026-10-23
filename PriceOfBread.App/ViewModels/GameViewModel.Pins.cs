// GameViewModel.Pins.cs ＝ ピン留め（(501) UI の作り直し 段4・設計 §4-3）。右の列に集める。
// ・参照は鍵だけ（<see cref="PinRef"/>）。値は毎月、表の最後に映した形（<see cref="TableSnapshot"/>）・線グラフ・ヘッダーの指標から引き直す＝新しい状態を貯めない（設計思想⑩）。
// ・行が消えた（国が滅んだ・財の工場が無くなった）ときは枠を薄くして名前だけ残す（黙って消さない）。
// ・本体の目録に載る表（<see cref="TableCatalog"/>）は、見えていないページの分も毎月ここで読み直す。呼び手が行を渡す表（支出・収入・台帳・国の一覧など）は
//   最後に映した形のまま＝そのページが見えている間だけ新しくなる。
// ・保存はセーブの追記ブロック（POBPIN1・EOF を許す）。
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceOfBread.App.Controls;
using PriceOfBread.App.Layout;
using PriceOfBread.SimCore.Display;

namespace PriceOfBread.App.ViewModels;

/// <summary>ピン留めの 1 枠（設計 §4-3）。名前・その行の数字（最大 4 つ）・小さな線（系列がある対象）。行が消えた枠は薄くして名前だけ残す（<see cref="Faded"/>）。</summary>
public sealed partial class PinSlot : ObservableObject
{
    public PinSlot(string key, Action<string> open, Action<string> unpin)
    {
        Key = key;
        OpenCommand = new RelayCommand(() => open(Key));
        UnpinCommand = new RelayCommand(() => unpin(Key));
    }

    public string Key { get; }
    public IRelayCommand OpenCommand { get; }
    public IRelayCommand UnpinCommand { get; }
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _source = "";
    /// <summary>数字の組（「列の名前 値」を 1 つずつ）。枠の中では組ごとに折り返す＝名前と値が別の行に割れない。</summary>
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ValuesText))] private IReadOnlyList<string> _pairs = [];
    public string ValuesText => string.Join("　", Pairs);
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasLine))] private IReadOnlyList<double>? _line;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Opacity))] private bool _faded;
    public bool HasLine => Line is { Count: > 1 };
    public double Opacity => Faded ? 0.45 : 1.0;
    public void CopyFrom(PinSlot s) { Name = s.Name; Source = s.Source; Pairs = s.Pairs; Line = s.Line; Faded = s.Faded; }
}

public partial class GameViewModel : IPinHost
{
    /// <summary>1 枠に出す数字の最大（その表の先頭の列から）。</summary>
    public const int PinValueColumns = 4;

    private readonly PinStore _pinStore = new();
    private readonly Dictionary<string, TableSnapshot> _seenTables = new();
    private readonly Dictionary<string, WeakReference<LineChart>> _seenCharts = new();

    public ObservableCollection<PinSlot> Pins { get; } = new();
    public bool HasPins => Pins.Count > 0;
    public bool NoPins => Pins.Count == 0;

    // ---- IPinHost（部品が呼ぶ）----
    public bool IsPinned(string id) => _pinStore.Contains(id);

    public void Toggle(PinRef pin)
    {
        bool pinned = _pinStore.Toggle(pin);
        if (pinned && !Layout.RightOpen && !Layout.IsFull) Layout.SetColumns(Layout.LeftOpen, true);   // 付けたら見える所へ（右の列が畳んであれば開く）。生産の全面の間は開かない（全面から戻れなくなる＝戻れば見える）
        RefreshPins();
    }

    public void SnapshotSeen(TableSnapshot snapshot) => _seenTables[snapshot.Key] = snapshot;

    public void ChartSeen(string key, LineChart chart) => _seenCharts[key] = new WeakReference<LineChart>(chart);

    /// <summary>ヘッダーの指標のピン留め（右クリック）。</summary>
    public PinRef HeaderPin(HeaderStat s) => new(PinKind.Header, "", s.Key, "", s.Key, "");

    // ---- 引き直し ----
    private TableSnapshot? SnapshotFor(string tableKey, Dictionary<string, TableSnapshot?> cache)
    {
        if (cache.TryGetValue(tableKey, out var hit)) return hit;
        TableSnapshot? s = TableCatalog.ByKey(tableKey) is { } t ? TableSnapshot.From(t.Read(_sim, _player))   // 本体の目録の表＝見えていないページの分も新しい
                         : _seenTables.TryGetValue(tableKey, out var seen) ? seen : null;                       // 呼び手が行を渡す表＝最後に映した形
        return cache[tableKey] = s;
    }

    private static IReadOnlyList<string> ValuesOf(TableSnapshot snap, RowView row)
    {
        var parts = new List<string>(PinValueColumns);
        for (int i = 0; i < snap.Columns.Count && i < row.Cells.Count && parts.Count < PinValueColumns; i++)
        {
            if (snap.Columns[i].Kind != ColumnKind.Number) continue;
            string t = row.Cells[i].Text;
            if (t.Length == 0 || t == "—") continue;
            parts.Add(snap.Columns[i].Name + " " + t);
        }
        return parts;
    }

    private PinSlot ResolvePin(PinRef pin, Dictionary<string, TableSnapshot?> cache)
    {
        var slot = new PinSlot(pin.Id, OpenPin, UnpinById) { Name = pin.Name, Source = pin.Source };
        switch (pin.Kind)
        {
            case PinKind.Row:
                {
                    var snap = SnapshotFor(pin.Table, cache);
                    var row = snap?.Rows.FirstOrDefault(r => r.Key == pin.Row);
                    if (snap is null || row is null) { slot.Faded = true; break; }   // 行が消えた＝薄くして名前だけ残す
                    slot.Name = row.Name; slot.Source = snap.Name; slot.Pairs = ValuesOf(snap, row);
                    break;
                }
            case PinKind.Header:
                {
                    var stat = HeaderStats.FirstOrDefault(h => h.Key == pin.Table);
                    if (stat is null) { slot.Faded = true; break; }
                    slot.Pairs = stat.Text.Length > 0 ? [stat.Text] : []; slot.Line = stat.Values;
                    break;
                }
            case PinKind.Chart:
                {
                    if (!_seenCharts.TryGetValue(pin.Table, out var wr) || !wr.TryGetTarget(out var chart) || chart.Values is not { Count: > 0 } vals) { slot.Faded = true; break; }
                    double last = double.NaN;
                    for (int i = vals.Count - 1; i >= 0; i--) if (vals[i] > double.MinValue && vals[i] < double.MaxValue) { last = vals[i]; break; }   // 非有限は否定形で落とす
                    slot.Pairs = double.IsNaN(last) ? [] : [chart.LabelFormat?.Invoke(last) ?? last.ToString("0.##", CultureInfo.InvariantCulture)];
                    slot.Line = vals.ToArray();
                    break;
                }
        }
        return slot;
    }

    /// <summary>右の列の枠を引き直す（毎月・付け外しの直後）。行は鍵で突き合わせて値だけ流す。</summary>
    private void RefreshPins()
    {
        var cache = new Dictionary<string, TableSnapshot?>();
        var wanted = _pinStore.Pins.Select(p => ResolvePin(p, cache)).ToList();
        SyncRows(Pins, wanted, s => s.Key, (t, s) => t.CopyFrom(s));
        OnPropertyChanged(nameof(HasPins)); OnPropertyChanged(nameof(NoPins));
    }

    private void UnpinById(string id)
    {
        if (_pinStore.Remove(id)) RefreshPins();
    }

    /// <summary>枠を押した＝元のページを選んでいる分割に開き、その行へ飛ぶ。ヘッダーの指標はページを持たない（開かない）。</summary>
    private void OpenPin(string id)
    {
        var pin = _pinStore.Pins.FirstOrDefault(p => p.Id == id);
        if (pin is null || !Enum.TryParse<Page>(pin.Page, out var page) || !Enum.IsDefined(page)) return;
        OpenAndFocus(page, pin.Kind == PinKind.Row ? pin.Table : "", pin.Kind == PinKind.Row ? pin.Row : "", "");
    }

    // ---- セーブ（追記ブロック）----
    private const string PinMagic = "POBPIN1";

    private void WritePins(BinaryWriter bw)
    {
        bw.Write(PinMagic);
        _pinStore.Write(bw);
    }

    /// <summary>追記ブロックを読む。旧セーブには無い（EOF）＝空のまま。</summary>
    public static List<PinRef> ReadPins(BinaryReader br)
    {
        var store = new PinStore();
        try { if (br.ReadString() == PinMagic) store.ReadFrom(br); }
        catch { store.Clear(); }
        return store.Pins.ToList();
    }

    /// <summary>ピン留めの要約（セーブの往復の一致を無人で確かめる＝uishot 専用）。</summary>
    internal string PinSummary => string.Join(";", _pinStore.Pins.Select(p => p.Encode().Replace('\t', '|')));

    /// <summary>撮影専用：ピン留めを直接付ける（右クリックの代わり）。</summary>
    internal void PinForCapture(PinRef pin) => Toggle(pin);

    private void LoadPins(IReadOnlyList<PinRef>? pins)
    {
        _pinStore.Clear();
        if (pins is not null) foreach (var p in pins) _pinStore.Add(p);
    }
}
