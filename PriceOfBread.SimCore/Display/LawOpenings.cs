namespace PriceOfBread.SimCore.Display;

/// <summary>法ごとの今の段と、次の段が前提の門を越えるか（ポップアップの差分の材料）。</summary>
public readonly record struct LawGateState(string Key, int Current, bool NextOpen);

/// <summary>「新たに設定できる法」の検出（状態を持たない純関数）。前提の門（<see cref="StageGate.NextOpen"/>）が閉から開になった法の段だけを数える。
/// 政治の門（賛成不足・政治資本）は見ない＝支持と在庫で月ごとに開閉するため。覚える物（見た段）は呼び手の集合＝世界にもセーブにも置かない。</summary>
public static class LawOpenings
{
    /// <summary>画面の行を持つ法（<see cref="LawSlot.NoRow"/> でなく、出す条件が真）ごとの写し。</summary>
    public static IReadOnlyList<LawGateState> Snapshot(World w)
    {
        var list = new List<LawGateState>();
        foreach (var d in LawCatalog.All)
        {
            if (d.Slot == LawSlot.NoRow || !d.IsVisible(w)) continue;
            list.Add(new LawGateState(d.Key, d.Current(w), StageGate.NextOpen(w, d)));
        }
        return list;
    }

    /// <summary>前の写しと今の写しから、新たに開いた法（現在の段が同じで、閉から開になり、<paramref name="seen"/> に無い）。
    /// <paramref name="seen"/> には今開いている（鍵, 次の段）を全て足す＝一度開いた段は閉じて開き直しても二度は入らない・最初の写し（prev が空）は基準として取り込むだけ。</summary>
    public static IReadOnlyList<LawDef> Opened(IReadOnlyList<LawGateState> prev, IReadOnlyList<LawGateState> now, ISet<(string Key, int Level)> seen)
    {
        var before = new Dictionary<string, LawGateState>(prev.Count);
        foreach (var p in prev) before[p.Key] = p;
        var result = new List<LawDef>();
        foreach (var n in now)
        {
            if (!n.NextOpen) continue;
            var key = (n.Key, n.Current + 1);
            bool fresh = !seen.Contains(key) && before.TryGetValue(n.Key, out var p0) && p0.Current == n.Current && !p0.NextOpen;
            if (fresh && LawCatalog.ByField(n.Key) is { } def) result.Add(def);
        }
        foreach (var n in now) if (n.NextOpen) seen.Add((n.Key, n.Current + 1));
        return result;
    }
}
