#!/usr/bin/env bash
# (1426) c153tp 突然変異（台本だけ・使い捨て）：bash gate_tp.sh <after の作業フォルダ> <出力フォルダ>
# μ1（重複）・μ2（欠け）・μ3（Ui を和に入れない）は trx_sum.py の赤、μ1'・μ2'・μ4 は split_suite.sh の list-tests の確かめ（STOP）の赤。
export PYTHONUTF8=1
AW=${1:?after の作業フォルダ}; OUT=${2:?出力}; mkdir -p $OUT
cd "$(dirname "$0")/../../.."; R=$(pwd); TS=$R/scripts/instruments/suite/trx_sum.py; N=3343
H=$AW/trx/tests_H.trx; RR=$AW/trx/tests_R.trx; U=$AW/trx/ui.trx
res(){ echo "$1 rc=$2（期待 $3）$([ "$2" = "$3" ] && echo 赤になった || echo 想定外)" | tee -a $OUT/gate.txt; }
: > $OUT/gate.txt
python $TS --trx $H $RR --ui $U --list-total $N > $OUT/m0.txt; res "元の和" $? 0
python $TS --trx $H $RR $H --ui $U --list-total $N > $OUT/m1.txt; res "μ1 重複（H を 2 度）" $? 1
python - $H $OUT/H_drop1.trx <<'PY'
import sys,re
b=open(sys.argv[1],encoding='utf-8').read()
# 先頭の結果 1 件を落とす（自己閉じか閉じ札つきかの両方）
m=re.search(r'<UnitTestResult\b[^>]*?(/>|>.*?</UnitTestResult>)',b,re.S)
open(sys.argv[2],'w',encoding='utf-8').write(b[:m.start()]+b[m.end():])
PY
python $TS --trx $OUT/H_drop1.trx $RR --ui $U --list-total $N > $OUT/m2.txt; res "μ2 欠け（H から 1 本）" $? 1
python $TS --trx $H $RR --list-total $N > $OUT/m3.txt; res "μ3 Ui を和に入れない" $? 1
# list-tests 段の確かめ（split_suite.sh の STOP）：filters.sh を差し替えて、ビルド済みの artifacts で走らせる
mk(){ cp $R/scripts/instruments/suite/filters.sh $OUT/f_$1.sh; }
mk m1; python - $OUT/f_m1.sh <<'PY'
import sys; p=sys.argv[1]; s=open(p,encoding='utf-8').read(); s=s.replace('_r="$_r&FullyQualifiedName!~PriceOfBread.Tests.$c."','_r="$_r"'); open(p,'w',encoding='utf-8',newline='\n').write(s)
PY
mk m2; python - $OUT/f_m2.sh <<'PY'
import sys; p=sys.argv[1]; s=open(p,encoding='utf-8').read(); s=s.replace('HEAVY_CLASSES="InstrumentsTests InstrumentHookClosureTests"','HEAVY_CLASSES="InstrumentsTests InstrumentHookClosureTests"\n_drop=1'); s=s.replace('FILTER_H="Category!=FullLength&($_h)"','FILTER_H="Category!=FullLength&($_h)&FullyQualifiedName!~EveryTableAlone"'); open(p,'w',encoding='utf-8',newline='\n').write(s)
PY
mk m4; python - $OUT/f_m4.sh <<'PY'
import sys; p=sys.argv[1]; s=open(p,encoding='utf-8').read(); s=s.replace('HEAVY_CLASSES="InstrumentsTests InstrumentHookClosureTests"','HEAVY_CLASSES="NoSuchClassZ"'); open(p,'w',encoding='utf-8',newline='\n').write(s)
PY
for m in m1 m2 m4; do
  FILTERS_SH=$OUT/f_$m.sh ART=$AW/art SKIPFULL=1 SKIPINS=1 bash $R/scripts/instruments/suite/split_suite.sh $OUT/w_$m > /dev/null 2>&1; rc=$?
  echo "μ$m(list 段) rc=$rc $(grep -E 'STOP|list-tests' $OUT/w_$m/suite.txt | tr '\n' ' ')" | tee -a $OUT/gate.txt
done
