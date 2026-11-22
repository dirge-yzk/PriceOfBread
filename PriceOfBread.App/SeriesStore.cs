using System;
using System.Collections.Generic;
using System.IO;

namespace PriceOfBread.App;

/// <summary>名前つきの系列の倉（設計 §6-6）。年次（または四半期）の系列を**名前**で持つ。
/// ・同じ名前は生涯同じ実体（<see cref="Of"/> が返す List の参照＝描画の点列の使い回しが参照で引く）。
/// ・セーブは「名前と値の組」で書く（位置で読まない）＝系列を足しても途中に挟んでも既存の系列が黙ってずれない
///   （2026-08-13 に 3 本を先頭へ足して旧セーブが全系列ずれた事故の根）。足した系列は旧セーブに無い＝空で始まる。
/// ・置き場は App（シムの軌道に触れない・本体に新しい状態を作らない）。</summary>
public sealed class SeriesStore
{
    private readonly Dictionary<string, List<double>> _by = new();
    private readonly List<string> _order = new();

    /// <summary>名前の系列。無ければ空で作る（同じ名前は同じ実体）。</summary>
    public List<double> Of(string key)
    {
        if (!_by.TryGetValue(key, out var list))
        {
            _by[key] = list = new List<double>();
            _order.Add(key);
        }
        return list;
    }

    public bool Contains(string key) => _by.ContainsKey(key);

    /// <summary>作った順の名前。</summary>
    public IReadOnlyList<string> Keys => _order;

    /// <summary>名前と値の組で書く（件数・[名前・点数・値…]）。</summary>
    public void Write(BinaryWriter bw)
    {
        bw.Write(_order.Count);
        foreach (var key in _order)
        {
            var l = _by[key];
            bw.Write(key);
            bw.Write(l.Count);
            foreach (var v in l) bw.Write(v);
        }
    }

    /// <summary>名前と値の組を読んで、同じ名前の系列へ流し込む（既存の系列は置き換える）。読み手が知らない名前も保つ（後の版が足した系列を捨てない）。</summary>
    public void ReadFrom(BinaryReader br)
    {
        int n = br.ReadInt32();
        for (int i = 0; i < n; i++)
        {
            string key = br.ReadString();
            int count = br.ReadInt32();
            var l = Of(key);
            l.Clear();
            for (int k = 0; k < count; k++) l.Add(br.ReadDouble());
        }
    }
}

/// <summary>ヘッダー（国の量）の年次系列の名前。セーブの鍵はこの文字列＝改名しない（改名は旧セーブの系列を捨てる）。</summary>
public static class SeriesKeys
{
    public const string Industry = "nation.industry", Credit = "nation.credit", Finance = "nation.finance",
        Population = "nation.population", Gdp = "nation.gdp", Production = "nation.production", Tech = "nation.tech",
        Literacy = "nation.literacy", Satisfaction = "nation.satisfaction", Militancy = "nation.militancy",
        Food = "nation.food", Legitimacy = "nation.legitimacy",
        Treasury = "nation.treasury", Gini = "nation.gini", Discontent = "nation.discontent";
}
