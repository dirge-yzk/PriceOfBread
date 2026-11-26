#!/usr/bin/env bash
# (1400) c149pm の腕 B：bash arm.sh <腕の名> <木の sha>   （bg.sh で run_in_background）
#   計器つき 10 種（種 1〜10）・表は keep_1383/a11 の 87 表＋pxstep に絞る・POB_WORK＝D:/PoB_work/c149pm_work。
export POB_WORK=D:/PoB_work/c149pm_work
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $TEMP
A=${1:?腕}; T=${2:?木}
S=D:/PoB_work/wt_c149pm/scripts/instruments
export POB_INSTRUMENTS_TABLES="$(ls D:/PoB_work/keep_1383/a11 | sed 's/_s[0-9]*\.tsv$//' | sort -u | tr '\n' ',')pxstep"
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
rm -f $POB_WORK/coef_$A.txt
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "$A start $(date '+%F %T') tree=$T Dfree=${FREE}GB armsfree=$(bash $S/tools/arms.sh free)" >> $POB_WORK/arms_start.txt
POB_REPO=D:/PoB_work/wt_c149pm bash $S/batch.sh prep $A commit:$T plain || exit 5
INS=1 bash $S/batch.sh run $A 10
RC=$?
echo "$A end $(date '+%F %T') rc=$RC" >> $POB_WORK/arms_start.txt
exit $RC
