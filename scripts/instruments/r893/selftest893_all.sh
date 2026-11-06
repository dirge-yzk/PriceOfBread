#!/usr/bin/env bash
# (893) c76lg の自己試験の一式（読むだけ・コミットしない）：
#  ①下側の門 4 本＋1 本（空・S16 の行を消す・S15 の 1 行の錨をずらす・S15 の仮の行を 1 本残す・S15 を足した後の台帳）と、今の main の台帳（比べ）
#  ②changed_rows の 2 本（何も変えない木・RevoltIgnitionIntensity の宣言の行だけを変えた木）＝selftest_changed893.sh
set -u
W=/d/PoB_work/r893_work
cd $W
python -X utf8 tools/build_after893.py ledger_731d3de1.md ledger_s15_section.md 機能の台帳_after.md
python -X utf8 tools/mk_fixtures893.py 機能の台帳_after.md selftest
cp ledger_731d3de1.md selftest/ledger_before_main.md
for t in empty noS16 shiftS15 provS15 after before_main; do
  python -X utf8 tools/ledger_check.py C:/Users/dirge/Documents/PriceOfBread 731d3de1 selftest/ledger_$t.md > selftest/check_$t.txt 2>&1
  echo "rc=$?" >> selftest/check_$t.txt
  echo "== $t: $(grep -E '引けない:|下側:|^門（|rc=' selftest/check_$t.txt | tr '\n' ' ')"
done
bash tools/selftest_changed893.sh
