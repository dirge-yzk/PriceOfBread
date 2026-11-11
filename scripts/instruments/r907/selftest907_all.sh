#!/usr/bin/env bash
# (907) c78lg の自己試験の一式（読むだけ・コミットしない）：(900) selftest900_all.sh の型
#  ①mk_fixtures907.py で自己試験の台帳を作る（門 6 の両側＝錨が無い側 gate6・最後に確かめたが無い側 gate6last）
#  ②全ての台帳に ledger_check.py（r900 の写し・書き換えていない）を当て、門ごとの通る／落ちると rc を出す
#  使い方: bash selftest907_all.sh <作業フォルダ＝ledger_after907.md のある所> <repo> <木>
set -u
W=${1:-/d/PoB_work/r907_work}; R=${2:-C:/Users/dirge/Documents/PriceOfBread}; T=${3:-fc9aeb4c}
cd $W
python -X utf8 tools/mk_fixtures907.py ledger_after907.md selftest > selftest/fixtures_log.txt
cp ledger_258a4543.md selftest/ledger_before_main.md
for t in empty gate6 gate6last gate6last_v dup nocol kesu after before_main; do
  python -X utf8 tools/ledger_check.py $R $T selftest/ledger_$t.md > selftest/check_$t.txt 2>&1
  echo "rc=$?" >> selftest/check_$t.txt
  echo "== $t: $(grep -E '^門ごと|rc=' selftest/check_$t.txt | tr '\n' ' ')"
done
