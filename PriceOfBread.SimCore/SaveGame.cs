using System.Reflection;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace PriceOfBread.SimCore;

public static class SaveGame
{
    private const string Magic = "POBSIM2";

    public static void Save(MultiNationSim sim, Stream stream)
    {
        var bw = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        bw.Write(Magic);
        bw.Write(Pop.Layout.Bands);   // (1125) 年齢の層の数（環境変数 POB_AGE_BANDS で決まる static）＝読む時に食い違えば止める
        new GraphWriter(bw).WriteValue(sim);
        bw.Flush();
    }

    public static MultiNationSim Load(Stream stream)
    {
        var br = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        string magic = br.ReadString();
        if (magic != Magic) throw new InvalidDataException($"セーブデータの形式が違う（magic={magic}）");
        int bands = br.ReadInt32();
        if (bands != Pop.Layout.Bands) throw new InvalidDataException($"セーブデータの年齢の層の数（{bands}）が今の設定（{Pop.Layout.Bands}・POB_AGE_BANDS）と違う");
        object? root = new GraphReader(br).ReadValue();
        var sim = root as MultiNationSim ?? throw new InvalidDataException("セーブデータの復元に失敗（ルートが MultiNationSim でない）");
        return sim;
    }

    /// <summary>(1309) 欄の比べの結果。Values＝値の違い・OrderOnly＝辞書や集合の列挙の順だけの違い（値は同じ）・Compared＝比べた対象の数。</summary>
    internal sealed class StateDiffResult
    {
        public int Values, OrderOnly; public long Compared;
        public readonly List<string> Samples = new();
    }

    /// <summary>(1309) 2 つのシミュの保存される欄（セーブと同じ FieldsOf）を並行にたどって比べる。シミュの道からは呼ばない（savetest と試験だけ）。
    /// 値は bit で比べる。辞書と集合は鍵で突き合わせ、値が同じで列挙の順だけ違うものは OrderOnly に数える。max＝Samples に残す件数。</summary>
    internal static StateDiffResult StateDiff(MultiNationSim a, MultiNationSim b, int max) => StateDiffOf(a, b, max);

    /// <summary>(1316) 比べる本体（盤面でなく任意の 2 つの物を比べる口＝集合の順だけと要素違いを分ける自己試験が使う・動きは StateDiff と同じ）。</summary>
    internal static StateDiffResult StateDiffOf(object a, object b, int max)
    {
        var res = new StateDiffResult();
        var seen = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        var work = new Stack<(object? A, object? B, Func<string> Path)>();
        void Diff(Func<string> path, string what) { res.Values++; if (res.Samples.Count < max) res.Samples.Add($"{path()}：{what}"); }
        static bool ValueKey(Type t) => t.IsValueType || t == typeof(string);
        work.Push((a, b, () => "sim"));
        while (work.Count > 0)
        {
            var (x, y, path) = work.Pop();
            res.Compared++;
            if (x is null || y is null) { if (!(x is null && y is null)) Diff(path, "片方が null"); continue; }
            var t = x.GetType();
            if (t != y.GetType()) { Diff(path, $"型 {t.Name}⇔{y.GetType().Name}"); continue; }
            if (x is double dx) { if (System.BitConverter.DoubleToInt64Bits(dx) != System.BitConverter.DoubleToInt64Bits((double)y)) Diff(path, $"{dx:R}⇔{(double)y:R}"); continue; }
            if (x is IUnitValue ux) { if (System.BitConverter.DoubleToInt64Bits(ux.Value) != System.BitConverter.DoubleToInt64Bits(((IUnitValue)y).Value)) Diff(path, $"{ux.Value:R}⇔{((IUnitValue)y).Value:R}"); continue; }
            if (t.IsPrimitive || t.IsEnum || x is string) { if (!x.Equals(y)) Diff(path, $"{x}⇔{y}"); continue; }
            if (x is Array za && za.Length == 0) { if (((Array)y).Length != 0) Diff(path, "長さ 0⇔" + ((Array)y).Length); continue; }   // (1370) 長さ 0 の配列は不変で値も続きも持てない＝共有の形（Array.Empty と別実体）を比べない
            if (!t.IsValueType)
            {
                if (seen.TryGetValue(x, out var prev)) { if (!ReferenceEquals(prev, y)) Diff(path, "共有の形が違う"); continue; }
                seen[x] = y;
            }
            if (t.IsArray || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)))
            {
                var la = (System.Collections.IList)x; var lb = (System.Collections.IList)y;
                if (la.Count != lb.Count) { Diff(path, $"長さ {la.Count}⇔{lb.Count}"); continue; }
                if (la is double[] pda)
                {   // 数の配列は 1 要素ずつ作業の列に積まず、その場で bit で比べる（メモリを節約）
                    var pdb = (double[])lb;
                    for (int i = 0; i < pda.Length; i++) if (System.BitConverter.DoubleToInt64Bits(pda[i]) != System.BitConverter.DoubleToInt64Bits(pdb[i])) { int k = i; Diff(() => $"{path()}[{k}]", $"{pda[k]:R}⇔{pdb[k]:R}"); }
                    res.Compared += pda.Length; continue;
                }
                for (int i = la.Count - 1; i >= 0; i--) { int k = i; work.Push((la[k], lb[k], () => $"{path()}[{k}]")); }
                continue;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var da = (System.Collections.IDictionary)x; var db = (System.Collections.IDictionary)y;
                if (da.Count != db.Count) { Diff(path, $"件数 {da.Count}⇔{db.Count}"); continue; }
                var ka = new List<object>(); foreach (var k in da.Keys) ka.Add(k);
                var kb = new List<object>(); foreach (var k in db.Keys) kb.Add(k);
                if (!ValueKey(t.GetGenericArguments()[0]))
                {   // 鍵が参照型＝列挙の順で突き合わせる（順の違いは値の違いに出る）
                    for (int i = ka.Count - 1; i >= 0; i--) { int k = i; work.Push((ka[k], kb[k], () => $"{path()}.鍵[{k}]")); work.Push((da[ka[k]], db[kb[k]], () => $"{path()}.値[{k}]")); }
                    continue;
                }
                bool order = true;
                for (int i = 0; i < ka.Count; i++)
                {
                    var key = ka[i];
                    if (!db.Contains(key)) { Diff(() => $"{path()}.鍵", $"{key} が片方に無い"); continue; }
                    if (!key.Equals(kb[i])) order = false;
                    work.Push((da[key], db[key], () => $"{path()}[{key}]"));
                }
                if (!order) { res.OrderOnly++; if (res.Samples.Count < max) res.Samples.Add($"{path()}：順だけ（{t.Name}）"); }
                continue;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                var ia = new List<object>(); foreach (var o in (System.Collections.IEnumerable)x) ia.Add(o);
                var ib = new List<object>(); foreach (var o in (System.Collections.IEnumerable)y) ib.Add(o);
                if (ia.Count != ib.Count) { Diff(path, $"件数 {ia.Count}⇔{ib.Count}"); continue; }
                if (!ValueKey(t.GetGenericArguments()[0]))
                { for (int i = ia.Count - 1; i >= 0; i--) { int k = i; work.Push((ia[k], ib[k], () => $"{path()}[{k}]")); } continue; }
                var setB = new HashSet<object>(ib); bool allIn = true, order = true;
                for (int i = 0; i < ia.Count; i++) { if (!setB.Contains(ia[i])) allIn = false; if (!ia[i].Equals(ib[i])) order = false; }
                if (!allIn) Diff(path, "要素が違う");
                else if (!order) { res.OrderOnly++; if (res.Samples.Count < max) res.Samples.Add($"{path()}：順だけ（{t.Name}）"); }
                continue;
            }
            foreach (var f in FieldsOf(t)) { var fi = f; work.Push((fi.GetValue(x), fi.GetValue(y), () => $"{path()}.{fi.DeclaringType!.Name}.{fi.Name}")); }
        }
        return res;
    }

    /// <summary>(1309) 保存の点の対照＝A の側で、眠りの印・清算の失敗の印・清算の覚えが立っている市場の数（印が次の読み手より前に立っている件数）。</summary>
    internal static (int Asleep, int FailLp, int Partners) MarkCounts(MultiNationSim s)
    {
        int a = 0, f = 0, p = 0;
        foreach (var n in s.Nations) foreach (var r in n.World.Regions) foreach (var m in r.Markets)
        { if (m.FlowAsleep) a++; if (m.ClearFailLp > 0) f++; if (m.ClearPartners is not null) p++; }
        return (a, f, p);
    }

    private enum Tag : byte
    {
        Null = 0, Ref = 1, Obj = 2, Struct = 3, Arr = 4, ListT = 5, DictT = 6, SetT = 7,
        Bool = 10, Int32 = 11, Int64 = 12, UInt64 = 13, Double = 14, Str = 15, Enum = 16,
        Byte = 17, Single = 18, Int16 = 19, UInt32 = 20, Char = 21, SByte = 22,
        Unit = 23,
    }

    private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new();
    internal static FieldInfo[] FieldsOf(Type t)
    {
        lock (FieldCache)
        {
            if (FieldCache.TryGetValue(t, out var cached)) return cached;
            var list = new List<FieldInfo>();
            for (var cur = t; cur is not null && cur != typeof(object); cur = cur.BaseType)
                foreach (var f in cur.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
                    if (f.IsDefined(typeof(NonSerializedAttribute), false)) continue;
                    list.Add(f);
                }
            list.Sort((a, b) => string.CompareOrdinal($"{a.DeclaringType!.FullName}.{a.Name}", $"{b.DeclaringType!.FullName}.{b.Name}"));
            var arr = list.ToArray();
            FieldCache[t] = arr;
            return arr;
        }
    }

    private sealed class GraphWriter(BinaryWriter bw)
    {
        private readonly Dictionary<object, int> _ids = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<Type, int> _typeIds = new();
        private readonly Stack<FieldInfo> _fieldPath = new();

        private int TypeId(Type t)
        {
            if (_typeIds.TryGetValue(t, out int id)) return id;
            id = _typeIds.Count;
            _typeIds[t] = id;
            bw.Write(-1);
            bw.Write(t.AssemblyQualifiedName ?? t.FullName!);
            return id;
        }
        private void WriteTypeRef(Type t)
        {
            if (_typeIds.TryGetValue(t, out int id)) bw.Write(id);
            else TypeId(t);
        }

        public void WriteValue(object? v)
        {
            switch (v)
            {
                case null: bw.Write((byte)Tag.Null); return;
                case bool b: bw.Write((byte)Tag.Bool); bw.Write(b); return;
                case int i: bw.Write((byte)Tag.Int32); bw.Write(i); return;
                case long l: bw.Write((byte)Tag.Int64); bw.Write(l); return;
                case ulong ul: bw.Write((byte)Tag.UInt64); bw.Write(ul); return;
                case double d: bw.Write((byte)Tag.Double); bw.Write(d); return;
                case IUnitValue u: bw.Write((byte)Tag.Unit); WriteTypeRef(v.GetType()); bw.Write(u.Value); return;
                case string s: bw.Write((byte)Tag.Str); bw.Write(s); return;
                case byte by: bw.Write((byte)Tag.Byte); bw.Write(by); return;
                case float f: bw.Write((byte)Tag.Single); bw.Write(f); return;
                case short sh: bw.Write((byte)Tag.Int16); bw.Write(sh); return;
                case uint ui: bw.Write((byte)Tag.UInt32); bw.Write(ui); return;
                case char c: bw.Write((byte)Tag.Char); bw.Write(c); return;
                case sbyte sb: bw.Write((byte)Tag.SByte); bw.Write(sb); return;
            }
            var t = v.GetType();
            if (t.IsEnum)
            {
                bw.Write((byte)Tag.Enum); WriteTypeRef(t); bw.Write(Convert.ToInt64(v));
                return;
            }
            if (t.IsValueType)
            {
                bw.Write((byte)Tag.Struct); WriteTypeRef(t);
                WriteFields(t, v);
                return;
            }
            if (_ids.TryGetValue(v, out int refId)) { bw.Write((byte)Tag.Ref); bw.Write(refId); return; }
            _ids[v] = _ids.Count;

            if (t.IsArray)
            {
                var arr = (Array)v;
                bw.Write((byte)Tag.Arr); WriteTypeRef(t.GetElementType()!); bw.Write(arr.Length);
                for (int i = 0; i < arr.Length; i++) WriteValue(arr.GetValue(i));
                return;
            }
            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                if (def == typeof(List<>))
                {
                    var list = (System.Collections.IList)v;
                    bw.Write((byte)Tag.ListT); WriteTypeRef(t.GetGenericArguments()[0]); bw.Write(list.Count);
                    foreach (var item in list) WriteValue(item);
                    return;
                }
                if (def == typeof(Dictionary<,>))
                {
                    var dict = (System.Collections.IDictionary)v;
                    var ga = t.GetGenericArguments();
                    bw.Write((byte)Tag.DictT); WriteTypeRef(ga[0]); WriteTypeRef(ga[1]); bw.Write(dict.Count);
                    foreach (System.Collections.DictionaryEntry e in dict) { WriteValue(e.Key); WriteValue(e.Value); }
                    return;
                }
                if (def == typeof(HashSet<>))
                {
                    bw.Write((byte)Tag.SetT); WriteTypeRef(t.GetGenericArguments()[0]);
                    var items = new List<object?>();
                    foreach (var item in (System.Collections.IEnumerable)v) items.Add(item);
                    bw.Write(items.Count);
                    foreach (var item in items) WriteValue(item);
                    return;
                }
            }
            bw.Write((byte)Tag.Obj); WriteTypeRef(t);
            WriteFields(t, v);
        }

        private void WriteFields(Type t, object v)
        {
            var fields = FieldsOf(t);
            if (_fieldListWritten.Add(t))
            {
                bw.Write(fields.Length);
                foreach (var f in fields) bw.Write($"{f.DeclaringType!.FullName}.{f.Name}");
            }
            foreach (var f in fields) WriteGuarded(f, v);
        }

        private readonly HashSet<Type> _fieldListWritten = new();

        private void WriteGuarded(FieldInfo f, object owner)
        {
            _fieldPath.Push(f);
            if (_fieldPath.Count > 700)
                throw new InvalidOperationException("セーブのオブジェクト鎖が深すぎる（一本鎖の構造の疑い）。直近の経路（深い側から20段）: "
                    + string.Join(" → ", System.Linq.Enumerable.Take(_fieldPath, 20).Select(x => $"{x.DeclaringType!.Name}.{x.Name}")));
            WriteValue(f.GetValue(owner));
            _fieldPath.Pop();
        }
    }

    private sealed class GraphReader(BinaryReader br)
    {
        private readonly List<object> _objects = new();
        private readonly List<Type> _types = new();
        private readonly Dictionary<Type, (FieldInfo?[] Saved, FieldInfo[] Missing)> _fieldMap = new();
        private readonly Dictionary<Type, object?> _donors = new();

        private (FieldInfo?[] Saved, FieldInfo[] Missing) MapOf(Type t)
        {
            if (_fieldMap.TryGetValue(t, out var cached)) return cached;
            var cur = FieldsOf(t);
            var byKey = new Dictionary<string, FieldInfo>(cur.Length);
            foreach (var f in cur) byKey[$"{f.DeclaringType!.FullName}.{f.Name}"] = f;
            int n = br.ReadInt32();
            var saved = new FieldInfo?[n];
            var used = new HashSet<FieldInfo>();
            for (int i = 0; i < n; i++)
            {
                string key = br.ReadString();
                if (byKey.TryGetValue(key, out var f)) { saved[i] = f; used.Add(f); }
            }
            var missing = new List<FieldInfo>();
            foreach (var f in cur) if (!used.Contains(f)) missing.Add(f);
            var entry = (saved, missing.ToArray());
            _fieldMap[t] = entry;
            return entry;
        }

        private object? DonorOf(Type t)
        {
            if (_donors.TryGetValue(t, out var d)) return d;
            try
            {
                var ctor = t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                d = ctor?.Invoke(null);
            }
            catch { d = null; }
            _donors[t] = d;
            return d;
        }

        private void ReadFields(Type t, object obj)
        {
            var (saved, missing) = MapOf(t);
            foreach (var f in saved)
            {
                object? v = ReadValue();
                f?.SetValue(obj, v);
            }
            if (missing.Length > 0)
            {
                var donor = DonorOf(t);
                foreach (var f in missing)
                {
                    object? dv = donor is not null ? f.GetValue(donor)
                        : f.FieldType == typeof(string) ? ""
                        : f.FieldType.IsValueType ? Activator.CreateInstance(f.FieldType) : null;
                    if (dv is Array arr) dv = arr.Clone();
                    f.SetValue(obj, dv);
                }
            }
        }

        private Type ReadTypeRef()
        {
            int id = br.ReadInt32();
            if (id >= 0) return _types[id];
            string name = br.ReadString();
            var t = Type.GetType(name) ?? throw new InvalidDataException($"セーブ内の型が見つからない: {name}");
            _types.Add(t);
            return t;
        }

        public object? ReadValue()
        {
            var tag = (Tag)br.ReadByte();
            switch (tag)
            {
                case Tag.Null: return null;
                case Tag.Bool: return br.ReadBoolean();
                case Tag.Int32: return br.ReadInt32();
                case Tag.Int64: return br.ReadInt64();
                case Tag.UInt64: return br.ReadUInt64();
                case Tag.Double: return br.ReadDouble();
                case Tag.Str: return br.ReadString();
                case Tag.Byte: return br.ReadByte();
                case Tag.Single: return br.ReadSingle();
                case Tag.Int16: return br.ReadInt16();
                case Tag.UInt32: return br.ReadUInt32();
                case Tag.Char: return br.ReadChar();
                case Tag.SByte: return br.ReadSByte();
                case Tag.Unit: { var ut = ReadTypeRef(); return Activator.CreateInstance(ut, br.ReadDouble())!; }
                case Tag.Ref: return _objects[br.ReadInt32()];
                case Tag.Enum:
                {
                    var t = ReadTypeRef();
                    return Enum.ToObject(t, br.ReadInt64());
                }
                case Tag.Struct:
                {
                    var t = ReadTypeRef();
                    object boxed = GetUninitializedObject(t);
                    ReadFields(t, boxed);
                    return boxed;
                }
                case Tag.Arr:
                {
                    var et = ReadTypeRef();
                    int n = br.ReadInt32();
                    var arr = Array.CreateInstance(et, n);
                    _objects.Add(arr);
                    for (int i = 0; i < n; i++) arr.SetValue(ReadValue(), i);
                    return arr;
                }
                case Tag.ListT:
                {
                    var et = ReadTypeRef();
                    int n = br.ReadInt32();
                    var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(et), n)!;
                    _objects.Add(list);
                    for (int i = 0; i < n; i++) list.Add(ReadValue());
                    return list;
                }
                case Tag.DictT:
                {
                    var kt = ReadTypeRef(); var vt = ReadTypeRef();
                    int n = br.ReadInt32();
                    var dict = (System.Collections.IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(kt, vt), n)!;
                    _objects.Add(dict);
                    for (int i = 0; i < n; i++) { object? k = ReadValue(); object? val = ReadValue(); dict.Add(k!, val); }
                    return dict;
                }
                case Tag.SetT:
                {
                    var et = ReadTypeRef();
                    int n = br.ReadInt32();
                    var set = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(et))!;
                    _objects.Add(set);
                    var add = set.GetType().GetMethod("Add")!;
                    for (int i = 0; i < n; i++) add.Invoke(set, [ReadValue()]);
                    return set;
                }
                case Tag.Obj:
                {
                    var t = ReadTypeRef();
                    object obj = GetUninitializedObject(t);
                    _objects.Add(obj);
                    ReadFields(t, obj);
                    return obj;
                }
                default: throw new InvalidDataException($"セーブデータの読み取りに失敗（tag={tag}）");
            }
        }
    }
}
