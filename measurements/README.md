# measurements — 測定の生出力

★ **人が読む文書ではなく、機械が吐いた実測の出力。** `docs/` から分離した（2026-08-09）。

| 中身 | 出どころ |
|---|---|
| `chronicle_baseline_20260728_geo0.md` | `chronicle 1`（12,436行・手順0/1後・seed1）＝帯比較の基準点 |
| `<ラン名>` / `<ラン名>.manifest` / `<ラン名>.tests` | `measure.sh` の3点セット＝`selfplay-batch` の出力・ソースのSHA-256台帳・テスト結果 |
| `driver*_stdout.log` / `driver*_stderr.log` | 探索ドライバの実行ログ |
| `再較正探索_2026-08-08.tsv` | 再較正探索のセル別結果表 |

⚠ **結論はここに書かない。** 数値の解釈と採否は `docs/` 側の実装ログへ書き、ここは出力を置くだけにする。

## ルートからの回収（2026-08-11）

ルート直下に溜まっていた測定3点セット63ランを整理した。

- **`docs/` の実装ログが基線として名指しする16ラン（41ファイル）をここへ回収**
  `colony_promo` `danzen73` `final_1to8` `frontier_soldier` `geo_stage1`
  `ign_p11` `ign_p12` `ign_tier` `ign_transit` `mega_catalog`
  `rs_D1_dep045_cap045` `soviet_tax` `sustain_k025` `unify_fallback`
  `wavef_full` `wavef_noD1`
  → 既存の `colony_promo` と内容が異なったため、ルート側は `colony_promo_root` として保存
- **どこからも引用されていない旧ラウンドの中間探索36ラン（107ファイル）は削除**
  `rs_A1_*`〜`rs_C2_*` の探索セル・`garrison_*` `crystal_*` `warm*` 以外の使い捨て
- **進行中のラウンド（08-10以降）はルートに据え置き**
  `all_final` `naval_lever` `naval_removed` `scale0` `soe_bisect` `soe_final`
  `ui_batch` `warm0` `warm4` `warm20` `warm40`

★ **測定が一段落したら、ルートの進行中ランもここへ回収すること。** ルート直下は
ソリューション・素材原本・`measure.sh`・現役の `.pob-fidelity-baseline.tsv` だけに保つ。
