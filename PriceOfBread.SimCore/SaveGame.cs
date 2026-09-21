using System.Reflection;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace PriceOfBread.SimCore;

/// <summary>
/// セーブ/ロード＝シミュレーション全体（<see cref="MultiNationSim"/>）の汎用バイナリ・グラフ直列化。
/// リフレクションで**全インスタンスフィールド（private/readonly含む）**を辿るので、フィールドを足しても自動追従する。
/// - 参照同一性を保存（同じ Region/Power/Coefficients を指す参照はロード後も同一インスタンスを指す）
/// - List/Dictionary/HashSet は要素列として書く（BCL内部レイアウト非依存）
/// - デリゲート型フィールドは書かない（ロード後 null）。static フィールドは対象外（Catalog等は起動時に同一内容で再構築される）
/// - 決定論：RNG（DeterministicRng＝ulong×4）も他の状態と同様に保存＝ロード後の進行はセーブ時点の続きとビット一致
///   ⚠前提＝Dictionary/HashSet は要素列から Add で再構築され内部の空きスロットは復元されない＝ロード後に削除→追加を挟むと列挙順が元の走と分岐しうる
///   ので、シム論理は Dictionary/HashSet の列挙順に依存しないこと（規律⑥）
/// - ★v2（POBSIM2・2026-08-31）＝**フィールド名テーブル**：型の初出時に直列化フィールドのキー（宣言型名.フィールド名）を書く。
///   ロードは名前で突き合わせ＝保存側にだけある名前は読み捨て・現行側にだけある名前は既定値（無引数ctorのドナー）で埋める
///   ＝**係数・フィールドを足してもセーブが壊れない**（プロイセン走§4-4「Boolean cannot be converted to Double」＝位置ずれの是正）。
///   旧POBSIM1は従来の位置読み（フィールド集合が一致するビルド内でのみ読める＝現状維持）。
/// </summary>
public static class SaveGame
{
    private const string Magic = "POBSIM2";
    private const string MagicV1 = "POBSIM1";

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
        if (magic != Magic && magic != MagicV1) throw new InvalidDataException($"セーブデータの形式が違う（magic={magic}）");
        object? root = new GraphReader(br, named: magic == Magic).ReadValue();
        var sim = root as MultiNationSim ?? throw new InvalidDataException("セーブデータの復元に失敗（ルートが MultiNationSim でない）");
        // ★国債の保有台帳（2026-09-21）：台帳導入前のセーブは Pop.BondHoldings が既定値0で埋まる＝国内債のある国で不変条件（BondLedger）が
        //   最初の月に割れる（セーブ破れ）。台帳が空で国内債がある国だけ、構築時と同じ起票（購入と同じ規則）で持ち主を記す。
        //   台帳入りのセーブでは Σ保有＞0 か国内債0 のどちらか＝何もしない（ビット不変）。
        foreach (var n in sim.Nations) Fiscal.SeedBondHoldings(n.World);
        return sim;
    }

    // --- 値タグ ---
    private enum Tag : byte
    {
        Null = 0, Ref = 1, Obj = 2, Struct = 3, Arr = 4, ListT = 5, DictT = 6, SetT = 7,
        Bool = 10, Int32 = 11, Int64 = 12, UInt64 = 13, Double = 14, Str = 15, Enum = 16,
        Byte = 17, Single = 18, Int16 = 19, UInt32 = 20, Char = 21, SByte = 22,   // SByte追加2026-08-08＝GoodTradeBias(sbyte[])。switchに無い値型はStruct経路で内部フィールドへ無限自己再帰する（深度ガードが検出）
    }

    // 型ごとの直列化対象フィールド（宣言順を型階層で辿る・デリゲートは除外）。名前順に固定＝決定論。
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
                    if (typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;   // イベント/コールバックは保存しない（ロード後null）
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
        // 深度ガード（2026-08-08）：一本鎖のオブジェクトグラフ（各オブジェクトのフィールドが次を指す連鎖）は
        // 再帰深度＝鎖長になり StackOverflow で落ちる（診断不能）。700段で経路つき例外に変える＝犯人が名乗る。
        private readonly Stack<FieldInfo> _fieldPath = new();

        private int TypeId(Type t)
        {
            if (_typeIds.TryGetValue(t, out int id)) return id;
            id = _typeIds.Count;
            _typeIds[t] = id;
            bw.Write(-1);                                   // 新型マーカー
            bw.Write(t.AssemblyQualifiedName ?? t.FullName!);
            return id;
        }
        private void WriteTypeRef(Type t)
        {
            if (_typeIds.TryGetValue(t, out int id)) bw.Write(id);
            else TypeId(t);                                  // 初出＝-1＋型名を書き、以後この型は連番で参照
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
            if (t.IsValueType)   // 構造体（タプル等）＝値としてフィールドを直書き（参照idなし）
            {
                bw.Write((byte)Tag.Struct); WriteTypeRef(t);
                WriteFields(t, v);
                return;
            }
            // 参照型：既出なら参照、初出なら本体
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
            // 一般のオブジェクト：型＋全フィールド
            bw.Write((byte)Tag.Obj); WriteTypeRef(t);
            WriteFields(t, v);
        }

        // ★v2：型の初出時にフィールドのキー（宣言型名.フィールド名＝FieldsOfのソートキーそのもの）を1度だけ書く。
        //   読み手は同じ「初出」タイミングで表を読む＝ストリーム位置が写像で一致（書き手と読み手は同じ走査順）。
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

    private sealed class GraphReader(BinaryReader br, bool named)
    {
        private readonly List<object> _objects = new();
        private readonly List<Type> _types = new();
        // ★v2：型→（保存順のフィールド対応・現行に無い保存名はnull＝読み捨て）と（保存に無い現行フィールド＝既定値で埋める）。
        private readonly Dictionary<Type, (FieldInfo?[] Saved, FieldInfo[] Missing)> _fieldMap = new();
        private readonly Dictionary<Type, object?> _donors = new();

        // 保存側の名前と現行フィールドを突き合わせる（型の初出＝表がストリームのここにある）。
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
                if (byKey.TryGetValue(key, out var f)) { saved[i] = f; used.Add(f); }   // 現行に無い名前＝null＝値は自己記述なので読み捨てられる
            }
            var missing = new List<FieldInfo>();
            foreach (var f in cur) if (!used.Contains(f)) missing.Add(f);
            var entry = (saved, missing.ToArray());
            _fieldMap[t] = entry;
            return entry;
        }

        // 欠落フィールドの既定値の出どころ＝無引数ctorのインスタンス（Coefficients等＝宣言既定値が入る）。
        // public/非public を問わず探す＝World 等は「ドナー専用の private 無引数ctor」を持つ（フィールド初期化子だけ走らせる）。
        // それも無い型（MultiNationSim等）はドナーなし＝値型は0・stringは""・参照はnull（ゼロ初期化のまま）。
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
            if (!named)   // 旧POBSIM1＝位置読み（フィールド集合が一致するビルド内のみ＝従来と同じ）
            {
                foreach (var f in FieldsOf(t)) f.SetValue(obj, ReadValue());
                return;
            }
            var (saved, missing) = MapOf(t);
            foreach (var f in saved)
            {
                object? v = ReadValue();   // タグ自己記述＝知らない名前でも安全に読み飛ばせる
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
                    if (dv is Array arr) dv = arr.Clone();   // ★2026-09-07 レビュー是正：donor の配列を共有しない（旧セーブの全州が1本の Rule/RuleStateYears を共有し、1州への書き込みが全州に効く潜在バグ）
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
