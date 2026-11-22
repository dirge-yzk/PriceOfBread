#!/usr/bin/env bash
# (1308) 合流（波135・m1294/arm1294.sh の写し・木と作業フォルダを wt_m1308・m1308_work へ・TABLES1308 で表を絞る）
#   bash arm1184.sh <腕> <INS 0|1> <種の数> <種の起点> <CEARN 0|1> <POB_COEF か -> <コミット>
#   腕の成否は batch.sh の終了コード（0＝完走・2＝計器の国が空・3＝ARMFULL・4＝BATCHFAIL）と成績表の有無の両方で読む。
S=D:/PoB_work/wt_m1308/scripts/instruments
if [ -n "${TABLES1308:-}" ]; then export POB_INSTRUMENTS_TABLES="$TABLES1308"; else unset POB_INSTRUMENTS_TABLES; fi
export POB_WORK=D:/PoB_work/m1308_work POB_REPO=D:/PoB_work/wt_m1308
export TEMP=D:/PoB_work/m1308_work/tmp TMP=D:/PoB_work/m1308_work/tmp
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
ARM=$1; INSF=$2; N=$3; SB=$4; CE=$5; COEF=$6; H=$7
[ "$CE" = 0 ] && export POB_CEARN=0
if [ "$COEF" != "-" ]; then printf '%s' "$COEF" > $POB_WORK/coef_$ARM.txt; else rm -f $POB_WORK/coef_$ARM.txt; fi
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
AF=$(bash $S/tools/arms.sh free)
echo "$ARM start $(date '+%F %T') ins=$INSF N=$N SB=$SB cearn=$CE coef=$COEF HEAD=${H:0:9} Dfree=${FREE}GB armsfree=$AF" >> $POB_WORK/arms_start.txt
bash $S/batch.sh prep $ARM commit:$H plain || exit 4
if [ "$INSF" = 1 ]; then INS=1 bash $S/batch.sh run $ARM $N $SB; else bash $S/batch.sh run $ARM $N $SB; fi
RC=$?
echo "$ARM end $(date '+%F %T') rc=$RC" >> $POB_WORK/arms_start.txt
exit $RC
