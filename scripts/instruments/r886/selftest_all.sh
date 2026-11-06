#!/usr/bin/env bash
# (886) の自己試験の一式：錨の門の突然変異 2 本（行をずらす・根拠を 9-26 の記号だけにする）と、変わった行の台本の 3 つの木
set -u
W=/d/PoB_work/r886_work
cd $W
for m in shift basis; do
  python -X utf8 scripts/ledger_check.py C:/Users/dirge/Documents/PriceOfBread 11db605f selftest/ledger_mut_$m.md > selftest/check_mut_$m.txt
  echo "rc=$?" >> selftest/check_mut_$m.txt
  echo "== check_mut_$m"; grep -E '引けない:|欠け:|^門|rc=' selftest/check_mut_$m.txt
done
bash scripts/selftest_changed.sh
