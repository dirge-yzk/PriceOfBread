#!/usr/bin/env bash
# (807) c12sv の記録の駆動（読むだけ）。腕 B（旗 1・計器つき 10 種）と基線 m797・ヌル腕 N1・N2 の生を同じ台本で読む。
#   使い方: bash rec807.sh <腕 B の ins フォルダ> <出力ファイル> [腕の名前]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c12sv; S=scripts/instruments
B=${1:?腕 B の ins フォルダ}; OUT=${2:?出力}; NAME=${3:-B}
BASE=D:/PoB_work/keep_802/base_ins; N1=D:/PoB_work/keep_797/n1; N2=D:/PoB_work/keep_797/n2
: > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "gates807（W1・W2・U1・S1・記録）"; python -X utf8 -I $S/c12sv/gates807.py $B --tag ${NAME} --table >> $OUT 2>&1
sec "ukdefault（英の国債の不履行の種の数）"; for t in "${NAME}:$B" "base:$BASE" "n1:$N1" "n2:$N2"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/c10cp/ukdefault.py $d 1 10 >> $OUT 2>&1; done
sec "pxjump（同じ台本で数え直し）"; python -X utf8 -I $S/c11fl/pxjump.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "gdpjump（名目 GDP の 1 四半期の比）"; python -X utf8 -I $S/m797/gdp_jump797.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "gdp_zero（名目 GDP 0 が続く国）"; python -X utf8 -I $S/m802/gdp_zero.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "plvl（価格水準）"; for t in "${NAME}:$B" "n1:$N1" "n2:$N2" "base:$BASE"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
sec "hol（空洞化・規則 A2）"; for t in "${NAME}:$B" "base:$BASE"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
echo RECDONE >> $OUT
