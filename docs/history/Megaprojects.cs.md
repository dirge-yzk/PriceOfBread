# PriceOfBread.SimCore/Megaprojects.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-6 W 直後 `public static class Megaprojects`
````
/// <summary>(1081) 巨大建造物の機構は消した（作者 2026-11-12・〈作者「全て推奨で」で推奨を採った〉）。残るのは 2 つの口だけ。
/// ①運河の発見の門（<see cref="RequirementMet"/>・world.json の chokepoints[].requires から読み込みで 1 回だけ作る表）。
/// ②画面と Runner がコンパイルするための空の形（<see cref="CanStart"/>・<see cref="TryStart"/>・<see cref="EstimateCost"/>・<see cref="StartRefusal"/>）。
/// <see cref="Diplomacy.Megaprojects"/> は常に空。②は App と Runner の欄を消した後の小さな消す線（DM-P）で消す。</summary>
````

## L9-9 W 直後 `private static readonly Dictionary<string, int> _requiresRow = BuildRequiresRows();`
````
// ★(613) 段1：運河の門を発見の行 requires へ。Id → 行の表を読み込みで 1 回だけ作る。名前は Catalog.DiscoveryRowNamed で添字へ落とし、表に無い名前は読み込みで投げる。
````

## L23-23 W 直後 `internal static int RequiresRowOf(string id) => _requiresRow.TryGetValue(id, out var r) ? `
````
/// <summary>個体 Id の requires の発見の行（無し＝-1）。</summary>
````

## L25-25 W 直後 `internal static bool RequirementMet(World w, string id) { int r = RequiresRowOf(id); retur`
````
/// <summary>requires を満たしているか（requires の無い個体は常に満たす）＝その国が自国の <see cref="World.Discovered"/> で判定する。</summary>
````

## L27-27 W 直後 `internal static string RequiresNameOf(string id) { int r = RequiresRowOf(id); return r < 0`
````
/// <summary>画面・報告用の requires の名前（無し＝なし）。</summary>
````

## L30-30 W 直後 `public static double EstimateCost(Region host, Megaproject mp, Coefficients coef) => 0;`
````
/// <summary>(1081) 口（空の形）：見積りは 0。</summary>
````

## L32-32 W 直後 `public static bool CanStart(World w, Megaproject mp) => false;`
````
/// <summary>(1081) 口（空の形）：着工はできない。</summary>
````

## L34-34 W 直後 `public static string? StartRefusal(World w, Megaproject mp) => "巨大建造物の機構は無い";`
````
/// <summary>(1081) 口（空の形）：拒否の理由は定数。</summary>
````

## L36-36 W 直後 `public static bool TryStart(World w, Megaproject mp) => false;`
````
/// <summary>(1081) 口（空の形）：何もしない。</summary>
````

