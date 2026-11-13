# PriceOfBread.SimCore/SaveGame.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L6-20 W 直後 `public static class SaveGame`
````
/// <summary>
/// セーブ/ロード＝シミュレーション全体（<see cref="MultiNationSim"/>）の汎用バイナリ・グラフ直列化。
/// リフレクションで**全インスタンスフィールド（private/readonly含む）**を辿るので、フィールドを足しても自動追従する。
/// - 参照同一性を保存（同じ Region/Power/Coefficients を指す参照はロード後も同一インスタンスを指す）
/// - List/Dictionary/HashSet は要素列として書く（BCL内部レイアウト非依存）
/// - デリゲート型フィールドは書かない（ロード後 null）。static フィールドは対象外（Catalog等は起動時に同一内容で再構築される）
/// - [NonSerialized] の印のフィールド（作業領域＝計算量の台帳・工事中の一覧など・2026-09-30）も書かない（ロード後は既定値＝使う側が作り直す）
/// - 決定論：RNG（DeterministicRng＝ulong×4）も他の状態と同様に保存＝ロード後の進行はセーブ時点の続きとビット一致
///   ⚠前提＝Dictionary/HashSet は要素列から Add で再構築され内部の空きスロットは復元されない＝ロード後に削除→追加を挟むと列挙順が元の走と分岐しうる
///   ので、シム論理は Dictionary/HashSet の列挙順に依存しないこと（規律⑥）
/// - ★v2（POBSIM2・2026-08-31）＝**フィールド名テーブル**：型の初出時に直列化フィールドのキー（宣言型名.フィールド名）を書く。
///   ロードは名前で突き合わせ＝保存側にだけある名前は読み捨て・現行側にだけある名前は既定値（無引数ctorのドナー）で埋める
///   ＝**係数・フィールドを足してもセーブが壊れない**（プロイセン走§4-4「Boolean cannot be converted to Double」＝位置ずれの是正）。
///   旧POBSIM1（位置読み）は 2026-10-18 に読み口ごと削除（旧版セーブが壊れても構わない＝作者 2026-09-26）。
/// </summary>
````

## L40-42 W 直後 `foreach (var n in sim.Nations) Fiscal.SeedBondHoldings(n.World);`
````
// ★国債の保有台帳（2026-09-21）：台帳導入前のセーブは Pop.BondHoldings が既定値0で埋まる＝国内債のある国で不変条件（BondLedger）が
//   最初の月に割れる（セーブ破れ）。台帳が空で国内債がある国だけ、構築時と同じ起票（購入と同じ規則）で持ち主を記す。
//   台帳入りのセーブでは Σ保有＞0 か国内債0 のどちらか＝何もしない（ビット不変）。
````

## L47-47 W 直後 `private enum Tag : byte`
````
// --- 値タグ ---
````

## L52 T `Byte = 17, Single = 18, Int16 = 19, UInt32 = 20, Char = 21, SByte = 22,`
````
// SByte追加2026-08-08＝GoodTradeBias(sbyte[])。switchに無い値型はStruct経路で内部フィールドへ無限自己再帰する（深度ガードが検出）
````

## L53 T `Unit = 23,`
````
// ★(246) 専用の値型（Units.cs の IUnitValue＝double 1つを包む readonly record struct）：型名＋ double。Struct 経路（内部フィールド）を通らない
````

## L56-56 W 直後 `private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new();`
````
// 型ごとの直列化対象フィールド（宣言順を型階層で辿る・デリゲートは除外）。名前順に固定＝決定論。
````

## L67 T `if (typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;`
````
// イベント/コールバックは保存しない（ロード後null）
````

## L68-69 W 直後 `if (f.IsDefined(typeof(NonSerializedAttribute), false)) continue;`
````
// ★作業領域（[NonSerialized]・2026-09-30 計算量の予算）＝計算量の台帳・工事中の一覧・死んでいない財の一覧などは状態ではない
//   ＝保存しない（ロード後は null・false・0＝使う側が盤面から作り直す）。この印を付けたフィールドだけが対象＝既存の全フィールドは従来どおり。
````

## L84-85 W 直後 `private readonly Stack<FieldInfo> _fieldPath = new();`
````
// 深度ガード（2026-08-08）：一本鎖のオブジェクトグラフ（各オブジェクトのフィールドが次を指す連鎖）は
// 再帰深度＝鎖長になり StackOverflow で落ちる（診断不能）。700段で経路つき例外に変える＝犯人が名乗る。
````

## L93 T `bw.Write(-1);`
````
// 新型マーカー
````

## L100 T `else TypeId(t);`
````
// 初出＝-1＋型名を書き、以後この型は連番で参照
````

## L113 T `case IUnitValue u: bw.Write((byte)Tag.Unit); WriteTypeRef(v.GetType()); bw.Write(u.Value);`
````
// ★(246) 専用の値型
````

## L128 T `if (t.IsValueType)`
````
// 構造体（タプル等）＝値としてフィールドを直書き（参照idなし）
````

## L134-134 W 直後 `if (_ids.TryGetValue(v, out int refId)) { bw.Write((byte)Tag.Ref); bw.Write(refId); return`
````
// 参照型：既出なら参照、初出なら本体
````

## L173-173 W 直後 `bw.Write((byte)Tag.Obj); WriteTypeRef(t);`
````
// 一般のオブジェクト：型＋全フィールド
````

## L178-179 W 直後 `private void WriteFields(Type t, object v)`
````
// ★v2：型の初出時にフィールドのキー（宣言型名.フィールド名＝FieldsOfのソートキーそのもの）を1度だけ書く。
//   読み手は同じ「初出」タイミングで表を読む＝ストリーム位置が写像で一致（書き手と読み手は同じ走査順）。
````

## L208-208 W 直後 `private readonly Dictionary<Type, (FieldInfo?[] Saved, FieldInfo[] Missing)> _fieldMap = n`
````
// ★v2：型→（保存順のフィールド対応・現行に無い保存名はnull＝読み捨て）と（保存に無い現行フィールド＝既定値で埋める）。
````

## L212-212 W 直後 `private (FieldInfo?[] Saved, FieldInfo[] Missing) MapOf(Type t)`
````
// 保存側の名前と現行フィールドを突き合わせる（型の初出＝表がストリームのここにある）。
````

## L225 T `if (byKey.TryGetValue(key, out var f)) { saved[i] = f; used.Add(f); }`
````
// 現行に無い名前＝null＝値は自己記述なので読み捨てられる
````

## L234-236 W 直後 `private object? DonorOf(Type t)`
````
// 欠落フィールドの既定値の出どころ＝無引数ctorのインスタンス（Coefficients等＝宣言既定値が入る）。
// public/非public を問わず探す＝World 等は「ドナー専用の private 無引数ctor」を持つ（フィールド初期化子だけ走らせる）。
// それも無い型（MultiNationSim等）はドナーなし＝値型は0・stringは""・参照はnull（ゼロ初期化のまま）。
````

## L255 T `object? v = ReadValue();`
````
// タグ自己記述＝知らない名前でも安全に読み飛ばせる
````

## L266 T `if (dv is Array arr) dv = arr.Clone();`
````
// ★2026-09-07 レビュー是正：donor の配列を共有しない（旧セーブの全州が1本の Rule/RuleStateYears を共有し、1州への書き込みが全州に効く潜在バグ）
````

## L300 T `case Tag.Unit: { var ut = ReadTypeRef(); return Activator.CreateInstance(ut, br.ReadDouble`
````
// ★(246) 専用の値型（Units.cs）
````

