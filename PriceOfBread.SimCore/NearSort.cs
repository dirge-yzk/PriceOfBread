namespace PriceOfBread.SimCore;

internal static class NearSort
{
    internal const int MoveFactor = 4;

    internal static bool TryReorder<T>(System.Span<T> items, int[]? prevKeys, int prevCount, System.Func<T, int> keyOf, System.Comparison<T> cmp, System.Func<T, bool> bad, int[] slot, T[] buf)
    {
        int n = items.Length;
        if (prevKeys is null || prevCount != n || n < 2 || buf.Length < n) return false;
        bool ok = true;
        for (int i = 0; i < n; i++)
        {
            var x = items[i];
            if (bad(x)) { ok = false; break; }
            int k = keyOf(x);
            if ((uint)k >= (uint)slot.Length || slot[k] != 0) { ok = false; break; }
            slot[k] = i + 1;
        }
        if (ok)
            for (int i = 0; i < n; i++)
            {
                int k = prevKeys[i];
                int at = (uint)k < (uint)slot.Length ? slot[k] - 1 : -1;
                if (at < 0) { ok = false; break; }
                buf[i] = items[at];
            }
        for (int i = 0; i < n; i++) { int k = keyOf(items[i]); if ((uint)k < (uint)slot.Length) slot[k] = 0; }
        if (!ok) return false;
        if (!Insertion(buf, n, cmp, n * MoveFactor)) return false;
        for (int i = 0; i < n; i++) items[i] = buf[i];
        return true;
    }

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

    internal static void Remember<T>(System.ReadOnlySpan<T> items, System.Func<T, int> keyOf, ref int[]? keys, ref int count)
    {
        int n = items.Length;
        if (keys is null || keys.Length < n) keys = new int[System.Math.Max(n, 8)];
        for (int i = 0; i < n; i++) keys[i] = keyOf(items[i]);
        count = n;
    }
}
