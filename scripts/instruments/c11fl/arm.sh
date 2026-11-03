#!/usr/bin/env bash
# (801) c11fl の腕の駆動：bash arm.sh <腕> <INS 0|1> <種の数> <種の起点> [POB_COEF] [コミット]
#   コミットを省くと合流の木の HEAD（git archive）。(784) の arm.sh の写し（作業フォルダと木だけ c11fl へ）。
S=D:/PoB_work/wt_c11fl/scripts/instruments
export POB_WORK=D:/PoB_work/c11fl POB_REPO=D:/PoB_work/wt_c11fl
ARM=$1; INSF=$2; N=$3; SB=$4; COEF=${5:-}; H=${6:-}
if [ -n "$COEF" ]; then printf '%s' "$COEF" > $POB_WORK/coef_$ARM.txt; else rm -f $POB_WORK/coef_$ARM.txt; fi
[ -n "$H" ] || H=$(git -C $POB_REPO rev-parse HEAD)
echo "$ARM start $(date '+%F %T') ins=$INSF N=$N SB=$SB coef=$COEF HEAD=${H:0:8}" >> $POB_WORK/arms_start.txt
bash $S/batch.sh prep $ARM commit:$H plain || exit 1
if [ "$INSF" = 1 ]; then INS=1 bash $S/batch.sh run $ARM $N $SB; else bash $S/batch.sh run $ARM $N $SB; fi
