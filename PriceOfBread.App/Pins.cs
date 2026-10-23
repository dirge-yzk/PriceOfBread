using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PriceOfBread.App;

/// <summary>ピン留めの対象の種類（設計 §4-3）：表の行／ヘッダーの指標／線グラフ。</summary>
public enum PinKind { Row = 'r', Header = 'h', Chart = 'c' }

/// <summary>ピン留め 1 つの参照（設計 §4-3）。値は持たない＝鍵で毎月引き直す（行が消えたら枠を薄くして名前だけ残す）。
/// ・<see cref="Table"/>＝表の鍵（行）／指標の名前（ヘッダー）／線グラフの鍵（線グラフ）。<see cref="Row"/>＝行の鍵（行だけ）。
/// ・<see cref="Page"/>＝元のページの名前（<c>SimCore.Display.Page</c> の名前・無ければ空）。押すとそのページを選んでいる分割に開く。
/// ・<see cref="Name"/>＝行の名前など枠の見出し／<see cref="Source"/>＝表の名前など枠の小さい見出し（消えた行でも残す）。</summary>
public sealed record PinRef(PinKind Kind, string Page, string Table, string Row, string Name, string Source)
{
    /// <summary>一意の鍵（同じ対象を二重にピン留めしない）。</summary>
    public string Id => $"{(char)Kind}|{Table}|{Row}";

    private const char Sep = '\t';

    /// <summary>1 行の文字にする（セーブの中の形）。区切りは行頭の種類の 1 字＋タブ。</summary>
    public string Encode() => string.Join(Sep, (char)Kind, Page, Table, Row, Name, Source);

    /// <summary>1 行の文字から。壊れた物は null（読み手は捨てる）。</summary>
    public static PinRef? Decode(string? line)
    {
        if (string.IsNullOrEmpty(line)) return null;
        var p = line.Split(Sep);
        if (p.Length != 6 || p[0].Length != 1) return null;
        var kind = (PinKind)p[0][0];
        if (!Enum.IsDefined(kind) || p[2].Length == 0) return null;
        return new PinRef(kind, p[1], p[2], p[3], p[4], p[5]);
    }
}

/// <summary>ピン留めの一覧（順序を持つ・同じ鍵は 1 つだけ）。Avalonia に依らない＝xUnit で読める。セーブは文字の列で書く（位置で並べない・EOF を許す読み手が使う）。</summary>
public sealed class PinStore
{
    public const int MaxPins = 24;   // 右の列に並ぶ数の上限（際限なく積んで読めなくなるのを防ぐ）

    private readonly List<PinRef> _pins = new();
    public IReadOnlyList<PinRef> Pins => _pins;
    public bool Contains(string id) => _pins.Any(p => p.Id == id);

    /// <summary>ピン留めを付ける。すでにあれば何もしない。上限に達していれば付けない（false）。</summary>
    public bool Add(PinRef pin)
    {
        if (Contains(pin.Id)) return true;
        if (_pins.Count >= MaxPins) return false;
        _pins.Add(pin);
        return true;
    }

    public bool Remove(string id) => _pins.RemoveAll(p => p.Id == id) > 0;

    /// <summary>付いていれば外し（false を返す）、無ければ付ける（true）。</summary>
    public bool Toggle(PinRef pin)
    {
        if (Remove(pin.Id)) return false;
        return Add(pin);
    }

    public void Clear() => _pins.Clear();

    public void Write(BinaryWriter bw)
    {
        bw.Write(_pins.Count);
        foreach (var p in _pins) bw.Write(p.Encode());
    }

    /// <summary>読む。壊れた行は捨てる・上限を超えた分は捨てる（読み手が EOF を許す側）。</summary>
    public void ReadFrom(BinaryReader br)
    {
        _pins.Clear();
        int n = br.ReadInt32();
        for (int i = 0; i < n && i < 10_000; i++)
        {
            var p = PinRef.Decode(br.ReadString());
            if (p is not null) Add(p);
        }
    }
}
