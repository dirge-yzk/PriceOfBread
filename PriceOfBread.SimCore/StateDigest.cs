using System.Reflection;

namespace PriceOfBread.SimCore;

/// <summary>(1391) 短い確かめの道具の核＝保存される欄（SaveGame.FieldsOf）を StateDiffOf と同じ順にたどり、浅い所の群ごとに 64 ビットの指紋を出す。
/// 2 本の木（別の assembly）は同じ process に載らないので、木ごとに四半期ごとの指紋を書き出し、台本 scripts/instruments/statediff/ が突き合わせる。
/// シミュの道からは呼ばない（試験の道具だけ）。辞書と集合（値の鍵）は鍵の文字で整列して列挙の順に依らない。</summary>
internal static class StateDigest
{
    private static ulong Mix(ulong h, ulong v) { h ^= v + 0x9E3779B97F4A7C15UL + (h << 6) + (h >> 2); h *= 0xFF51AFD7ED558CCDUL; return h ^ (h >> 33); }
    private static ulong StrHash(string s) { ulong h = 14695981039346656037UL; foreach (char c in s) { h ^= c; h *= 1099511628211UL; } return h; }

    /// <summary>depth＝群の深さ（欄とリストの添字を 1 段と数える）。戻り＝群の名→指紋（名の昇順）。</summary>
    internal static SortedDictionary<string, ulong> Digest(object root, int depth)
    {
        var acc = new SortedDictionary<string, ulong>(StringComparer.Ordinal);
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var work = new Stack<(object? X, string Group, int D, string Path)>();
        work.Push((root, "sim", 0, "sim"));
        static bool ValueKey(Type t) => t.IsValueType || t == typeof(string);
        void Add(string g, ulong v) { acc.TryGetValue(g, out var h); acc[g] = Mix(h == 0 ? 1469598103934665603UL : h, v); }
        while (work.Count > 0)
        {
            var (x, g, d, path) = work.Pop();
            string Child(string seg, out int nd) { nd = d + 1; return nd <= depth ? path + seg : path; }
            if (x is null) { Add(g, 0xA5A5); continue; }
            var t = x.GetType();
            if (x is double dx) { Add(g, (ulong)BitConverter.DoubleToInt64Bits(dx)); continue; }
            if (x is IUnitValue ux) { Add(g, (ulong)BitConverter.DoubleToInt64Bits(ux.Value)); continue; }
            if (x is string sx) { Add(g, StrHash(sx)); continue; }
            if (t.IsPrimitive || t.IsEnum)
            {
                if (x is float fx) Add(g, (ulong)BitConverter.SingleToInt32Bits(fx));
                else if (x is char cx) Add(g, cx);
                else if (x is bool bx) Add(g, bx ? 1UL : 0UL);
                else if (x is ulong ul) Add(g, ul);
                else Add(g, (ulong)Convert.ToInt64(x));
                continue;
            }
            if (x is Array za && za.Length == 0) { Add(g, 0xE0); continue; }
            if (!t.IsValueType) { if (!seen.Add(x)) { Add(g, 0xB1); continue; } }
            string Seg(string s, out int nd2, out string ng) { var np = Child(s, out nd2); ng = nd2 <= depth ? np : g; return np; }
            if (t.IsArray || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)))
            {
                var la = (System.Collections.IList)x; Add(g, (ulong)la.Count);
                if (la is double[] pda) { foreach (var v in pda) Add(g, (ulong)BitConverter.DoubleToInt64Bits(v)); continue; }
                for (int i = la.Count - 1; i >= 0; i--) { var np = Seg($"[{i}]", out var nd, out var ng); work.Push((la[i], ng, nd, np)); }
                continue;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var da = (System.Collections.IDictionary)x; Add(g, (ulong)da.Count);
                var ks = new List<object>(); foreach (var k in da.Keys) ks.Add(k);
                if (ValueKey(t.GetGenericArguments()[0])) ks.Sort((a, b) => string.CompareOrdinal(a.ToString(), b.ToString()));
                for (int i = ks.Count - 1; i >= 0; i--)
                {
                    var np = Seg($"[{ks[i]}]", out var nd, out var ng);
                    work.Push((da[ks[i]], ng, nd, np));
                    if (!ValueKey(t.GetGenericArguments()[0])) work.Push((ks[i], ng, nd, np + ".鍵"));
                }
                continue;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                var ia = new List<object>(); foreach (var o in (System.Collections.IEnumerable)x) ia.Add(o);
                Add(g, (ulong)ia.Count);
                if (ValueKey(t.GetGenericArguments()[0])) ia.Sort((a, b) => string.CompareOrdinal(a.ToString(), b.ToString()));
                for (int i = ia.Count - 1; i >= 0; i--) { var np = Seg($"[{i}]", out var nd, out var ng); work.Push((ia[i], ng, nd, np)); }
                continue;
            }
            foreach (var f in SaveGame.FieldsOf(t).Reverse())
            {
                var np = Seg($".{f.DeclaringType!.Name}.{f.Name}", out var nd, out var ng);
                work.Push((f.GetValue(x), ng, nd, np));
            }
        }
        return acc;
    }
}
