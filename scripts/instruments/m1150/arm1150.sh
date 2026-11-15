#!/usr/bin/env bash
# (1150) 合流（m1143/arm1143.sh の写し・木と作業フォルダを wt_m1150・m1150_work へ・TABLES1150 で表を絞る〔S11・S11z〕）
# (1143) 合流（m1136/arm1136.sh の写し・木と作業フォルダを m1143 にした・POB_INSTRUMENTS_TABLES は呼び手が TABLES1150 で渡せば絞る〔S11・S11z・A_lo・A_rb・A_tp〕）
# (1136) 合流（m1122/arm1122.sh の写し・木と作業フォルダを m1136 にした・POB_INSTRUMENTS_TABLES は呼び手が TABLES1150 で渡せば絞る〔S11・S11z〕）
#   bash arm1136.sh <腕> <INS 0|1> <種の数> <種の起点> <CEARN 0|1> <POB_COEF か -> <コミット>
#   腕の成否は batch.sh の終了コード（0＝完走・2＝計器の国が空・3＝ARMFULL・4＝BATCHFAIL）と成績表の有無の両方で読む。
S=D:/PoB_work/wt_m1150/scripts/instruments
if [ -n "${TABLES1150:-}" ]; then export POB_INSTRUMENTS_TABLES="$TABLES1150"; else unset POB_INSTRUMENTS_TABLES; fi
export POB_WORK=${M1150W:-D:/PoB_work/m1150_work} POB_REPO=D:/PoB_work/wt_m1150
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
