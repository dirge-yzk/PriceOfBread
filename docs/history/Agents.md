# PriceOfBread.SimCore/Agents.cs の注（1cd66aae5 の時点・注を履歴へ移した記録）

見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。

## L3-6 W 直後 `public interface INationAgent`
````
/// <summary>
/// 国家（プレイヤー or AI）の意思決定インターフェース。四半期ごとに介入レバー（§3.2）を
/// 操作する。プレイヤーと AI は同じ問題を解くため同一インターフェースを共有する（§8）。
/// </summary>
````

## L12-12 W 直後 `public sealed class NullAgent : INationAgent`
````
/// <summary>何もしない放置エージェント（§8 開発ループの第一段＝null agent）。</summary>
````

## L15 T `public void DecideQuarter(World world) {`
````
/* 介入しない */
````

## L18-23 W 直後 `public sealed class ModernizerAgent : INationAgent`
````
/// <summary>
/// 近代化派AI（決定論のテスト/デモ用 archetype）：公教育・高教育費・適度な所得税で財源を作り、治安維持力を緩めて改革を容認、
/// 近代化に応じて参政権を**普通選挙まで積極的に押す**（利益集団ゲートが制限選挙で頭打ちにするのを検証する用途＝PolicyPoliticsTests）。
/// 実 selfplay の AI は <see cref="UtilityAgent"/>（効用最大化・教義は支配ブロックから創発）に統合済み。反動派 archetype は UtilityAgent の
/// 軍事/均衡スタンスに吸収されたため削除（倍率排除⑧：散在するハードコード archetype を効用フレームへ統合）。
/// </summary>
````

## L26 T `private const double IncomeTaxTarget = 0.25;`
````
// 富裕層に課税して財源（議会が強いと上げにくい）
````

## L27 T `private const double CoercionTarget = 200;`
````
// 治安維持力を緩め改革を容認（旧支配層は縮小に抵抗）
````

## L32 T `p.EducationLaw = EducationLaw.Public;`
````
// 世俗の公教育＝エリート創出
````

## L33 T `p.EducationSpendingTarget = Government.EducationSpendingTop;`
````
// ★錨2026-09-18＝教育費の教義最大（Modernize と同値）
````

## L36 T `if (p.Suffrage < Suffrage.Census) p.Suffrage = Suffrage.Census;`
````
// ★(613) 段2b（旗 TechThresholdOff は (824) で焼き込み）：制限選挙へ踏み出す技術の閾値は無い
````

