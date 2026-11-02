#!/usr/bin/env bash
# (787) c9op 腕 B の記録の駆動（読むだけ）。腕 B（計器つき 10 種）の生と基線 keep_784 の生を読む。bash scripts/instruments/c9op/rec787.sh <腕 B の ins> <出力ファイル>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c9op; S=scripts/instruments
M="${1:?腕 B の ins}"; OUT="${2:?出力}"; W=D:/PoB_work/c9op
B=D:/PoB_work/keep_784/base_ins; BN1=D:/PoB_work/keep_784/n1; BN2=D:/PoB_work/keep_784/n2
: > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec gate_B; python -X utf8 -I $S/c9op/gate.py $M >> $OUT 2>&1
for t in "B:$M" "base:$B" "n1:$BN1" "n2:$BN2"; do n=${t%%:*}; d=${t#*:}; sec "plvl_$n"; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
for t in "B:$M" "base:$B" "n1:$BN1" "n2:$BN2"; do n=${t%%:*}; d=${t#*:}; sec "hol_$n"; python -X utf8 $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
sec home_break; python -X utf8 -I $S/brk693/home_break.py B=$M n1=$BN1 n2=$BN2 base=$B --opening-only >> $OUT 2>&1
echo RECDONE >> $OUT
