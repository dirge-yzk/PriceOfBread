#!/usr/bin/env bash
# (1428) c153rg 腕 B（c152ms/arm.sh の写し・作業フォルダと表だけ替えた）：bash arm.sh <腕の名> <木の sha>
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c153rg_work/<腕>.mark bash scripts/instruments/c153rg/arm.sh ...   （run_in_background で起こす）
#   計器つき 10 種・種 1〜10・表を絞る。起こす直前に D: の空き 38GB 以上を測り、heavy_c153rg.run を書いて終わりに消す。
export POB_WORK=D:/PoB_work/c153rg_work
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp
export POB_INSTRUMENTS_TABLES="tensionwhy_s,wars_s,unifwar_s,nation_s,region_s,scalars_s,fiscal_s,casualty_s,migration_s,pxmonth_s,stock_s"
mkdir -p $TEMP
A=${1:?腕}; T=${2:?木}
S=D:/PoB_work/wt_c153rg/scripts/instruments
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
HV=$(ls D:/PoB_work/heavy_*.run 2>/dev/null | wc -l)
echo "$A start $(date '+%F %T') tree=$T Dfree=${FREE}GB heavy=$HV armsfree=$(bash $S/tools/arms.sh free)" >> $POB_WORK/arms_start.txt
python -c "import sys;sys.exit(0 if $FREE>=38 else 1)" || { echo "DFREE_SHORT ${FREE}GB" >> $POB_WORK/arms_start.txt; exit 6; }
[ "$HV" -ge 2 ] && { echo "HEAVY_BUSY $HV" >> $POB_WORK/arms_start.txt; exit 7; }
echo "c153rg $A $(date '+%F %T')" > D:/PoB_work/heavy_c153rg.run
POB_REPO=D:/PoB_work/wt_c153rg bash $S/batch.sh prep $A commit:$T plain || { rm -f D:/PoB_work/heavy_c153rg.run; exit 5; }
INS=1 bash $S/batch.sh run $A 10 1
RC=$?
rm -f D:/PoB_work/heavy_c153rg.run
echo "$A end $(date '+%F %T') rc=$RC" >> $POB_WORK/arms_start.txt
exit $RC
