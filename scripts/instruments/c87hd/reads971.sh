#!/usr/bin/env bash
# (971) c87hd 腕の読み（reads944.sh の写しを基準 keep_968・門 gate971・止める線 stoplines949〔RULE=pair・NULLS は m968〕へ）。
#   bash scripts/instruments/tools/bg.sh <印> bash scripts/instruments/c87hd/reads971.sh <腕の名 c87hdS> <種の起点 11|1>
#   比べる基準の出力は measurements/m968_20261108/armS11（S11）と null/（ヌル腕）にある＝数え直しは同じ台本の出力を並べる。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"; cd "$REPO" || exit 9
ARM=${1:?腕}; SB=${2:-11}; SE=$((SB+9)); W=D:/PoB_work/c87hd_work; AW=$W/arm_$ARM; SUF=_10; [ "$SB" = 11 ] && SUF=_10_s11; I=$AW/out_${ARM}${SUF}/ins; O=$W/reads_$ARM; mkdir -p $O
K=D:/PoB_work/keep_968; if [ "$SB" = 11 ]; then BASE=$K/a11; else BASE=$K/base_ins; fi
python -X utf8 -I $SP/gate971.py arm=$I base=$BASE --seeds $SB $SE > $O/gate.txt 2>&1; echo "gate rc=$?"
RULE=pair NULLS="$REPO/measurements/m968_20261108/null/stop906_n1.txt $REPO/measurements/m968_20261108/null/stop906_n2.txt" bash scripts/instruments/c84gz/stoplines949.sh $I $O/stop949.txt $SB; echo "stop949 rc=$?"
python -X utf8 scripts/instruments/hol/an_h2.py $I -v4 > $O/hol.txt 2>&1; echo "hol rc=$?"
python -X utf8 -I scripts/instruments/m808/gdp_zero10.py arm=$I > $O/gdp_zero10.txt 2>&1; echo "gdpzero rc=$?"
bash scripts/instruments/ng.sh $I $ARM D:/PoB_work/c87hd_work/ng_$ARM rd:measurements/ng968_20261108:m968 > $O/ng.txt 2>&1; echo "ng rc=$?"
python -X utf8 -I scripts/instruments/tools/keepsum.py $I $O/keepsum.tsv > $O/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$SB" = 11 ]; then python -X utf8 scripts/instruments/tools/nullgate.py $AW/b10_${ARM}${SUF}.txt measurements/b10s11_20261108_m968_ins measurements/b10_20261108_m968_n1_ins measurements/b10_20261108_m968_n2_ins > $O/nullgate.txt 2>&1; echo "nullgate rc=$?"; fi
python -X utf8 -I scripts/instruments/brk693/home_break.py arm=$I base=$BASE --test arm > $O/home_break.txt 2>&1; echo "home rc=$?"
echo READSDONE
