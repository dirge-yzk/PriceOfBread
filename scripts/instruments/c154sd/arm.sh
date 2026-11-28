#!/usr/bin/env bash
# (1435) c154sd 腕（c153nx/arm.sh の写し・作業フォルダと表だけ替えた）：bash arm.sh <腕の名> <木の sha> [床 GB=39]
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c154sd_work/<腕>.mark bash scripts/instruments/c154sd/arm.sh ...   （run_in_background で起こす）
#   計器つき 10 種・種 1〜10・表を絞る。起こす直前に D: の空きを測り、heavy_c154sd.run を書いて終わりに消す。TABLES で表を替えられる。
export POB_WORK=D:/PoB_work/c154sd_work
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp
export POB_INSTRUMENTS_TABLES="${TABLES:-dcont_s,facfloor2_s,nation_s,cwend_s,coupgate_s,wars_s,wexit_s,scalars_s,region_s,sbu_s,parlseat_s,pxmonth_s,stock_s,casualty_s,class_s,fiscal_s,migration_s}"
mkdir -p $TEMP
A=${1:?腕}; T=${2:?木}; FL=${3:-39}
S=D:/PoB_work/wt_c154sd/scripts/instruments
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
HV=$(ls D:/PoB_work/heavy_*.run 2>/dev/null | wc -l)
echo "$A start $(date '+%F %T') tree=$T Dfree=${FREE}GB heavy=$HV armsfree=$(bash $S/tools/arms.sh free)" >> $POB_WORK/arms_start.txt
python -c "import sys;sys.exit(0 if $FREE>=$FL else 1)" || { echo "DFREE_SHORT ${FREE}GB" >> $POB_WORK/arms_start.txt; exit 6; }
[ "$HV" -ge 2 ] && { echo "HEAVY_BUSY $HV" >> $POB_WORK/arms_start.txt; exit 7; }
echo "c154sd $A $(date '+%F %T')" > D:/PoB_work/heavy_c154sd.run
POB_REPO=D:/PoB_work/wt_c154sd bash $S/batch.sh prep $A commit:$T plain || { rm -f D:/PoB_work/heavy_c154sd.run; exit 5; }
INS=1 bash $S/batch.sh run $A 10 1
RC=$?
rm -f D:/PoB_work/heavy_c154sd.run
echo "$A end $(date '+%F %T') rc=$RC" >> $POB_WORK/arms_start.txt
exit $RC
