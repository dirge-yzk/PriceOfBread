#!/usr/bin/env bash
# (937) c82lt 腕の読み（読むだけ）：門 gate937・新÷旧 tab937・止める線 stoplines906・空洞化 an_h2 -v4・NG（記録）・nullgate（記録・種 1〜10 だけ）・本国の破れ（記録）・keepsum。
#   bash scripts/instruments/tools/bg.sh <印> bash scripts/instruments/c82lt/read937.sh <腕の名 c82ltB|c82ltS> <種の起点 1|11>
#   比べる基準と止める線の基準は m933（keep_933 の base_ins・a11・n1・n2・measurements/m933_20261107/ の armM・armS11・null）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"; cd "$REPO" || exit 9
ARM=${1:?腕}; SB=${2:-1}; W=D:/PoB_work/c82lt_work/arm_$ARM; SUF=_10; [ "$SB" = 11 ] && SUF=_10_s11; I=$W/out_${ARM}${SUF}/ins; O=D:/PoB_work/c82lt_work/reads_$ARM; mkdir -p $O
K=D:/PoB_work/keep_933; if [ "$SB" = 11 ]; then BASE=$K/a11; else BASE=$K/base_ins; fi
[ -d "$I" ] || { echo "ins が無い：$I"; exit 2; }
python -X utf8 $SP/gate937.py $I $SB > $O/gate.txt 2>&1; echo "gate rc=$?"
python -X utf8 $SP/tab937.py $I $BASE $SB $SB > $O/tab.txt 2>&1; echo "tab rc=$?"
bash scripts/instruments/c78gz/stoplines906.sh $I $O/stop906.txt $SB; echo "stop rc=$?"
python -X utf8 scripts/instruments/hol/an_h2.py $I -v4 > $O/hol.txt 2>&1; echo "hol rc=$?"
bash scripts/instruments/ng.sh $I $ARM D:/PoB_work/c82lt_work/ng_$ARM rd:measurements/ng933_20261107:m933 > $O/ng.txt 2>&1; echo "ng rc=$?"
python -X utf8 -I scripts/instruments/tools/keepsum.py $I $O/keepsum.tsv > $O/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$SB" = 1 ]; then python -X utf8 scripts/instruments/tools/nullgate.py $W/b10_${ARM}_10.txt measurements/b10_20261107_m933_ins measurements/b10_20261107_m933_n1_ins measurements/b10_20261107_m933_n2_ins > $O/nullgate.txt 2>&1; echo "nullgate rc=$?"; fi
if [ "$SB" = 1 ]; then python -X utf8 -I scripts/instruments/brk693/home_break.py arm=$I base=$BASE n1=$K/n1 n2=$K/n2 --test arm > $O/home_break.txt 2>&1; echo "home rc=$?"; fi
echo READSDONE
