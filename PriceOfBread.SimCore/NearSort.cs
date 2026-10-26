namespace PriceOfBread.SimCore;

/// <summary>★(spt2) 前の並びから挿入で並べ直す（恒等の高速化・2026-10-26 フォーク元の採否「3つとも進めてください」）。
/// 清算の買い手と売り手の並べ替え（財ごと）・生産の段の消費の順（州ごと）が読む。値段は月ごとに少ししか動かない＝前の月の並びはほぼ整列済み＝挿入なら件数にほぼ比例する手間で済む。
/// 前提：比べ cmp が要素の間の**全順序**（同じ要素でない 2 つは決して 0 を返さない）＝どの正しい並べ替えでも結果は 1 つ＝全部の並べ替え（呼び手の元の Array.Sort）と同じ並び。
/// 前提が崩れうる場合は false を返し、呼び手は元の全部の並べ替えへ戻る（結果は旧と 1 ビットも変わらない）：
///   ①前の並びが無い・顔ぶれが違う（州の割譲・建国・吸収や財の生き死に）②鍵に NaN がある（呼び手の bad が真＝比べの全順序が崩れうる）
///   ③動かした数が上限（件数×<see cref="MoveFactor"/>）を越えた（大きく崩れた月に挿入を重くしない）④並べた後に隣り合う 2 つの比べが 0 以上（全順序でない＝同値の組がある）。
/// 前の並びは呼び手の作業領域（セーブに載らない・無くても全部の並べ替えで同じ結果）＝新しい状態ではない。</summary>
internal static class NearSort
{
    /// <summary>動かした数の上限＝件数×この値（挿入の手間を全部の並べ替えの数倍までに抑える）。</summary>
    internal const int MoveFactor = 4;

    /// <summary>前の並び <paramref name="prevKeys"/>（鍵の列）から、今の顔ぶれ <paramref name="items"/>（呼び手が元の順で作った一覧）を挿入で並べ直す。
    /// 成功＝<paramref name="items"/> を並べ直した順に書き換えて true。失敗＝<paramref name="items"/> は元の順のまま false（呼び手は元の全部の並べ替え）。
    /// <paramref name="keyOf"/>＝要素→鍵（0 以上 <paramref name="slot"/> の長さ未満・要素ごとに一意）。<paramref name="slot"/>＝呼び手の作業配列（全て 0 で渡し、全て 0 で返す）。
    /// <paramref name="buf"/>＝並べ直しの作業配列（長さは件数以上）。</summary>
    internal static bool TryReorder<T>(System.Span<T> items, int[]? prevKeys, int prevCount, System.Func<T, int> keyOf, System.Comparison<T> cmp, System.Func<T, bool> bad, int[] slot, T[] buf)
    {
        int n = items.Length;
        if (prevKeys is null || prevCount != n || n < 2 || buf.Length < n) return false;   // ①前の並びが無い・数が違う（2 件未満は並べ替えの手間が無い＝元の並べ替えに任せる）
        bool ok = true;
        for (int i = 0; i < n; i++)
        {
            var x = items[i];
            if (bad(x)) { ok = false; break; }   // ②NaN
            int k = keyOf(x);
            if ((uint)k >= (uint)slot.Length || slot[k] != 0) { ok = false; break; }   // 鍵の範囲の外・重複
            slot[k] = i + 1;
        }
        if (ok)
            for (int i = 0; i < n; i++)
            {
                int k = prevKeys[i];
                int at = (uint)k < (uint)slot.Length ? slot[k] - 1 : -1;
                if (at < 0) { ok = false; break; }   // ①前の並びの要素が今の顔ぶれに居ない（数が同じなので、居なければ顔ぶれが違う）
                buf[i] = items[at];
            }
        for (int i = 0; i < n; i++) { int k = keyOf(items[i]); if ((uint)k < (uint)slot.Length) slot[k] = 0; }   // 作業配列を 0 に戻す
        if (!ok) return false;
        if (!Insertion(buf, n, cmp, n * MoveFactor)) return false;   // ③動かしすぎ・④同値
        for (int i = 0; i < n; i++) items[i] = buf[i];
        return true;
    }

    /// <summary>挿入の並べ替え（昇順＝cmp が負なら前）。動かした数が <paramref name="moveLimit"/> を越えたら false。並べた後に隣り合う 2 つの比べが 0 以上なら false（全順序でない）。</summary>
    internal static bool Insertion<T>(T[] a, int n, System.Comparison<T> cmp, int moveLimit)
    {
        int moves = 0;
        for (int i = 1; i < n; i++)
        {
            var x = a[i];
            int j = i - 1;
            while (j >= 0 && cmp(a[j], x) > 0)
            {
                a[j + 1] = a[j];
                j--;
                if (++moves > moveLimit) return false;
            }
            a[j + 1] = x;
        }
        for (int i = 1; i < n; i++) if (cmp(a[i - 1], a[i]) >= 0) return false;
        return true;
    }

    /// <summary>並べた後の鍵の列を作業配列へ写す（次の月の前の並び）。配列は長さが足りれば使い回す。</summary>
    internal static void Remember<T>(System.ReadOnlySpan<T> items, System.Func<T, int> keyOf, ref int[]? keys, ref int count)
    {
        int n = items.Length;
        if (keys is null || keys.Length < n) keys = new int[System.Math.Max(n, 8)];
        for (int i = 0; i < n; i++) keys[i] = keyOf(items[i]);
        count = n;
    }
}
