#!/usr/bin/env bash
# (849) 3 世代のヌル腕と基線の止める線の数え直し（読むだけ）
S=/d/PoB_work/wt_c69ss/scripts/instruments
O=/d/PoB_work/c69ss_work/null3
export PYTHONUTF8=1
for t in base:/d/PoB_work/keep_845/base_ins n839_1:/d/PoB_work/keep_839/n1 n839_2:/d/PoB_work/keep_839/n2 n826_1:/d/PoB_work/keep_826/n1 n826_2:/d/PoB_work/keep_826/n2 n797_1:/d/PoB_work/keep_797/n1 n797_2:/d/PoB_work/keep_797/n2; do
  n=${t%%:*}; d=${t#*:}
  python $S/hol/an_h2.py $d -v4 > $O/hol_$n.txt 2>&1
done
python -X utf8 -I $S/m797/gdp_jump797.py base=/d/PoB_work/keep_845/base_ins n839_1=/d/PoB_work/keep_839/n1 n839_2=/d/PoB_work/keep_839/n2 > $O/jump797.txt 2>&1
python -X utf8 -I $S/r833/gdp_jump833.py base=/d/PoB_work/keep_845/base_ins n839_1=/d/PoB_work/keep_839/n1 n839_2=/d/PoB_work/keep_839/n2 > $O/jump833.txt 2>&1
python -X utf8 -I $S/m808/gdp_zero10.py base=/d/PoB_work/keep_845/base_ins n839_1=/d/PoB_work/keep_839/n1 n839_2=/d/PoB_work/keep_839/n2 > $O/gdpzero.txt 2>&1
