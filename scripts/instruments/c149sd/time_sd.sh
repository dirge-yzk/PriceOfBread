#!/usr/bin/env bash
# (1398) c149sd 速さの計器の素の 1 種（種 1・selfplay-batch 1・POB_TIME=1）＝旗 1 の木（m1397_20261127/time と同じ形・dotnet-counters なし）
T=D:/PoB_work/c149sd_work/time; A=D:/PoB_work/c149sd_work/time_art
export TEMP=$T/tmp TMP=$T/tmp; mkdir -p $TEMP
cd D:/PoB_work/wt_c149sd || exit 2
dotnet build PriceOfBread.Runner -c Release --artifacts-path $A -v q -nologo > $T/build.txt 2>&1 || exit 3
echo "start $(date '+%F %T') HEAD=$(git rev-parse --short HEAD) dotnet=$(tasklist 2>/dev/null | grep -c -i dotnet)" > $T/load.txt
S0=$SECONDS
POB_TIME=1 POB_COST_OUT=$T/b1.txt.cost dotnet $A/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.dll selfplay-batch 1 > $T/b1.txt 2> $T/err.txt; RC=$?
echo "end $(date '+%F %T') rc=$RC wall=$((SECONDS-S0))s dotnet=$(tasklist 2>/dev/null | grep -c -i dotnet)" >> $T/load.txt
grep "POB_TIME段" $T/b1.txt > $T/stages.tsv
exit $RC
