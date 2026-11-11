(893) c76lg の出力（読む線・コミットしない・合流 (899) が写す）

docs/機能の台帳.md          ← 機能の台帳_after.md（今の main の台帳＝ledger_731d3de1.md に S15 の節＝ledger_s15.md〔= ledger_s15_section.md〕を tools/build_after893.py で入れた物）
                               変える所：§0 の 4 行（確かめた系統に S15・道具のパスを r893 へ・下側の門の説明・R1′）／§1 の S15 の状態／§2b を新しく／§3 の S15 の仮の行 32 本を 1 行の案内へ
docs/ゲーム概要と実装機能.md ← overview_s15.patch（L2594〜2794 の 46 行だけ・行の数は変えない・git apply --check rc=0＝selftest/patch_check.txt）
scripts/instruments/r893/    ← tools/ の (893) で書いた物だけ：ledger_check.py（下側の門）・changed_rows.py（係数名の拾い）・build_after893.py・mk_fixtures893.py・mutate893.py・patch_overview893.py・selftest893_all.sh・selftest_changed893.sh
                               ⚠ tools/ の他のファイル（anchors.py・build_ledger.py・mutate_trees.py・parse_doc.py・patch_overview.py・s16_*.py・sys*.py・selftest_all.sh・selftest_changed.sh）は r886 の写しのまま＝写さない
measurements/m893_<日付>/     ← s15/（数えた台本・出力・grep の記録・next_wave_draft.md）・selftest/（自己試験の台帳と出力）・plan.txt
