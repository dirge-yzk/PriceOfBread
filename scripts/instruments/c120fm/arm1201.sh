#!/usr/bin/env bash
# (1201) c120fm の腕（m1198/arm1198.sh の型・木と作業フォルダを wt_c120fm・c120fm_work へ・表は門と記録の読み手が開く物に絞る）
#   bash arm1201.sh <腕> <INS 0|1> <種の数> <種の起点> <POB_COEF か -> <コミット>
#   腕 Ba：bash arm1201.sh c120fmBa 1 10 1 FormationNatioGate=1 <sha>／腕 Bb：bash arm1201.sh c120fmBb 1 10 1 FormationOwnDirectOnly=1 <sha>
#   表＝合流の N1 の表（m1198/arms1198.sh の NUL）＋formation_s・dep_s・cess_s・landsale_s・rulepower_s。CEARN 1（基線 keep_1198/base_ins と同じ）。
#   腕の成否は batch.sh の終了コード（0＝完走・2＝計器の国が空・3＝ARMFULL・4＝BATCHFAIL）と成績表の有無の両方で読む。
S=D:/PoB_work/wt_c120fm/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d '\r'); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '\r')
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
NUL="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,mintcap_s,admrule_s,t1buy_s,pop_s,sf_s"
export POB_INSTRUMENTS_TABLES="${TABLES1201:-$NUL,formation_s,dep_s,cess_s,landsale_s,rulepower_s}"
export POB_WORK=D:/PoB_work/c120fm_work POB_REPO=D:/PoB_work/wt_c120fm
export TEMP=D:/PoB_work/c120fm_work/tmp TMP=D:/PoB_work/c120fm_work/tmp
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
ARM=$1; INSF=$2; N=$3; SB=$4; COEF=$5; H=$6
if [ "$COEF" != "-" ]; then printf '%s' "$COEF" > $POB_WORK/coef_$ARM.txt; else rm -f $POB_WORK/coef_$ARM.txt; fi
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
AF=$(bash $S/tools/arms.sh free)
echo "$ARM start $(date '+%F %T') ins=$INSF N=$N SB=$SB coef=$COEF HEAD=${H:0:9} Dfree=${FREE}GB armsfree=$AF" >> $POB_WORK/arms_start.txt
bash $S/batch.sh prep $ARM commit:$H plain || exit 4
if [ "$INSF" = 1 ]; then INS=1 bash $S/batch.sh run $ARM $N $SB; else bash $S/batch.sh run $ARM $N $SB; fi
RC=$?
echo "$ARM end $(date '+%F %T') rc=$RC" >> $POB_WORK/arms_start.txt
exit $RC
