#!/usr/bin/env bash
# (1362) 合流の腕 1 本（m1355/arm.sh の写し・作業フォルダだけ m1362）：bash arm.sh <腕の名> <木の sha> <種の数> <種の起点> <ins 1|0> [SupplySlope の値]
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1362_work/<腕>.mark bash scripts/instruments/m1362/arm.sh ...   （run_in_background で起こす）
#   POB_WORK＝D:/PoB_work/m1362_work。成績表 $W/b<N>_<腕>_<N>[_s<起点>].txt・生 $W/out_<TAG>/ins。
export POB_WORK=D:/PoB_work/m1362_work
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp
if [ -n "${ARM_TABLES:-}" ]; then export POB_INSTRUMENTS_TABLES="$ARM_TABLES"; else unset POB_INSTRUMENTS_TABLES; fi   # 表を絞る腕（アブレーション・S11）は ARM_TABLES で渡す
mkdir -p $TEMP
A=${1:?腕}; T=${2:?木}; N=${3:?種の数}; SB=${4:?起点}; I=${5:?ins}; SS=${6:-}
S=D:/PoB_work/wt_m1362/scripts/instruments
if [ -n "$SS" ]; then echo "SupplySlope=$SS" > $POB_WORK/coef_$A.txt; else rm -f $POB_WORK/coef_$A.txt; fi
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "$A start $(date '+%F %T') tree=$T Dfree=${FREE}GB armsfree=$(bash $S/tools/arms.sh free) coef=$(cat $POB_WORK/coef_$A.txt 2>/dev/null)" >> $POB_WORK/arms_start.txt
POB_REPO=D:/PoB_work/wt_m1362 bash $S/batch.sh prep $A commit:$T plain || exit 5
if [ "$I" = 1 ]; then INS=1 bash $S/batch.sh run $A $N $SB; else bash $S/batch.sh run $A $N $SB; fi
RC=$?
echo "$A end $(date '+%F %T') rc=$RC" >> $POB_WORK/arms_start.txt
exit $RC
