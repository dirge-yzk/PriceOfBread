#!/usr/bin/env bash
# (930) c81la 腕の読み（読むだけ）：門 gate930・新÷旧 tab930・止める線 stoplines906・空洞化 an_h2 -v4・NG（記録）・nullgate（記録）・本国の破れ（記録）・keepsum。
#   bash scripts/instruments/tools/bg.sh <印> bash scripts/instruments/c81la/reads930.sh <腕の名 c81laB|c81laS> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"; cd "$REPO" || exit 9
ARM=${1:?腕}; SB=${2:-1}; W=D:/PoB_work/c81la_work; SUF=_10; [ "$SB" = 11 ] && SUF=_10_s11; I=$W/out_${ARM}${SUF}/ins; O=$W/reads_$ARM; mkdir -p $O
K=D:/PoB_work/keep_919; if [ "$SB" = 11 ]; then BASE=$K/a11; else BASE=$K/base_ins; fi; export BASE1=$K/base_ins   # tab930.py のヌル腕の比の分母（種 1〜10 の基線）＝種 11〜20 の腕でも必要
SEED_START=$SB python -X utf8 $SP/gate930.py gate $I $BASE $K/n1 $K/n2 > $O/gate.txt 2>&1; echo "gate rc=$?"
python -X utf8 $SP/tab930.py $I $BASE $K/n1 $K/n2 $SB > $O/tab.txt 2>&1; echo "tab rc=$?"
bash scripts/instruments/c78gz/stoplines906.sh $I $O/stop906.txt $SB; echo "stop rc=$?"
python -X utf8 scripts/instruments/hol/an_h2.py $I -v4 > $O/hol.txt 2>&1; echo "hol rc=$?"
bash scripts/instruments/ng.sh $I $ARM D:/PoB_work/c81la_work/ng_$ARM rd:measurements/ng919_20261107:m919 > $O/ng.txt 2>&1; echo "ng rc=$?"
python -X utf8 -I scripts/instruments/tools/keepsum.py $I $O/keepsum.tsv > $O/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$SB" = 1 ]; then python -X utf8 scripts/instruments/tools/nullgate.py $W/b10_${ARM}_10.txt measurements/b10_20261107_m919_ins measurements/b10_20261107_m919_n1_ins measurements/b10_20261107_m919_n2_ins > $O/nullgate.txt 2>&1; echo "nullgate rc=$?"; fi
if [ "$SB" = 1 ]; then python -X utf8 -I scripts/instruments/brk693/home_break.py arm=$I base=$BASE n1=$K/n1 n2=$K/n2 --test arm > $O/home_break.txt 2>&1; echo "home rc=$?"; fi
echo READSDONE
