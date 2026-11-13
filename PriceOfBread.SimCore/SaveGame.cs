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
        new GraphWriter(bw).WriteValue(sim);
        bw.Flush();
    }

    public static MultiNationSim Load(Stream stream)
    {
        var br = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        string magic = br.ReadString();
        if (magic != Magic) throw new InvalidDataException($"セーブデータの形式が違う（magic={magic}）");
        object? root = new GraphReader(br).ReadValue();
        var sim = root as MultiNationSim ?? throw new InvalidDataException("セーブデータの復元に失敗（ルートが MultiNationSim でない）");
        foreach (var n in sim.Nations) Fiscal.SeedBondHoldings(n.World);
        return sim;
    }

    private enum Tag : byte
    {
        Null = 0, Ref = 1, Obj = 2, Struct = 3, Arr = 4, ListT = 5, DictT = 6, SetT = 7,
        Bool = 10, Int32 = 11, Int64 = 12, UInt64 = 13, Double = 14, Str = 15, Enum = 16,
        Byte = 17, Single = 18, Int16 = 19, UInt32 = 20, Char = 21, SByte = 22,
        Unit = 23,
    }

    private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new();
    private static FieldInfo[] FieldsOf(Type t)
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
