#!/usr/bin/env bash
# (1048) c99db（c98lw/arm1037.sh の写し・木を wt_c99db・作業フォルダを引数 C99W にした）
#   bash arm1048.sh <腕> <INS 0|1> <種の数> <種の起点> <CEARN 0|1> <POB_COEF か -> <コミット>
#   腕の成否は batch.sh の終了コード（0＝完走・2＝計器の国が空・3＝ARMFULL・4＝BATCHFAIL）と成績表の有無の両方で読む。
S=D:/PoB_work/wt_c99db/scripts/instruments
export POB_WORK=${C99W:?作業フォルダ} POB_REPO=D:/PoB_work/wt_c99db
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
ARM=$1; INSF=$2; N=$3; SB=$4; CE=$5; COEF=$6; H=$7
[ "$CE" = 0 ] && export POB_CEARN=0
if [ "$COEF" != "-" ]; then printf '%s' "$COEF" > $POB_WORK/coef_$ARM.txt; else rm -f $POB_WORK/coef_$ARM.txt; fi
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
AF=$(bash $S/tools/arms.sh free)
echo "$ARM start $(date '+%F %T') ins=$INSF N=$N SB=$SB cearn=$CE coef=$COEF HEAD=${H:0:8} Dfree=${FREE}GB armsfree=$AF" >> $POB_WORK/arms_start.txt
bash $S/batch.sh prep $ARM commit:$H plain || exit 4
if [ "$INSF" = 1 ]; then INS=1 bash $S/batch.sh run $ARM $N $SB; else bash $S/batch.sh run $ARM $N $SB; fi
RC=$?
echo "$ARM end $(date '+%F %T') rc=$RC" >> $POB_WORK/arms_start.txt
exit $RC
