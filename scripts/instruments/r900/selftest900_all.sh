#!/usr/bin/env bash
# (900) c77lg の自己試験の一式（読むだけ・コミットしない）：
#  ①build_after900.py で台帳を作る ②自己試験の台帳 5 本＋そのまま＋今の main の台帳（比べ）に ledger_check.py（門 7・8・9 を足した版）を当てる
#  ③畳む前と後の id の集合（fold_ids900.py）④changed_rows.py（12 列を読む版）を今の木に当てて、行の数が r893 の版を 11 列の台帳に当てた数と同じか
set -u
# (905) 合流が写すときに作業フォルダと木を引数にした（反証役の指摘）：bash selftest900_all.sh <作業フォルダ＝ledger_101d4329.md・s19/・tools/ のある所> <木（worktree）>
W=${1:-/d/PoB_work/r900_work}; T=${2:-D:/PoB_work/wt_c77lg}
R=C:/Users/dirge/Documents/PriceOfBread
cd $W
python -X utf8 tools/build_after900.py ledger_101d4329.md s19/ledger_s19_section.md ledger_after900.md s19/ids_before.txt > selftest/build_log.txt
python -X utf8 tools/mk_fixtures900.py ledger_after900.md selftest > selftest/fixtures_log.txt
cp ledger_101d4329.md selftest/ledger_before_main.md
for t in empty gate6 dup nocol kesu after before_main; do
  python -X utf8 tools/ledger_check.py $R 101d4329 selftest/ledger_$t.md > selftest/check_$t.txt 2>&1
  echo "rc=$?" >> selftest/check_$t.txt
  echo "== $t: $(grep -E '^門ごと|rc=' selftest/check_$t.txt | tr '\n' ' ')"
done
python -X utf8 tools/fold_ids900.py ledger_101d4329.md ledger_after900.md > selftest/fold_ids.txt; echo "fold rc=$?"; cat selftest/fold_ids.txt
python -X utf8 tools/changed_rows.py $T ledger_after900.md > selftest/changed_after900.txt
python -X utf8 $R/scripts/instruments/r893/changed_rows.py $T ledger_after900.md > selftest/changed_after900_old893.txt
python -X utf8 $R/scripts/instruments/r893/changed_rows.py $T ledger_101d4329.md > selftest/changed_before_old893.txt
for f in changed_after900 changed_after900_old893 changed_before_old893; do echo "== $f: $(head -1 selftest/$f.txt)"; done
