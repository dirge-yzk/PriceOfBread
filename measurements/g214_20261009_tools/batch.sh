#!/usr/bin/env bash
# (213) 線G：作業ツリー（または指定のコミット）の写し＋読むだけの計器で measure.sh を1本。
#   bash batch.sh prep <腕> <tree:dir|commit:sha> <inst yes|no>
#   bash batch.sh run <腕> <種の数> [POB_COEF の文字列]
export AVALONIA_TELEMETRY_OPTOUT=1
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export PYTHONUTF8=1
W=/d/PoB_work/g214; ARM=$2; SRC=$W/src_$ARM
if [ "$1" = prep ]; then
  FROM=$3; INST=$4
  rm -rf $SRC && mkdir -p $SRC
  case "$FROM" in
    tree:*) tar -cf - -C ${FROM#tree:} --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs . | tar -xf - -C $SRC || exit 1 ;;
    commit:*) (cd /d/PoB_work/wt_gld && git archive ${FROM#commit:}) | tar -x -C $SRC || exit 1 ;;
  esac
  if [ "$INST" = yes ]; then python $W/tools/inj_g214.py $SRC || { echo INJFAIL; exit 1; }; fi
  echo PREPDONE $ARM $FROM $INST
elif [ "$1" = run ]; then
  N=${3:-10}; COEF="${4:-}"; [ "$COEF" = "-" ] && COEF=""
  O=$W/out_$ARM; mkdir -p $O
  (cd /d/PoB_work/wt_gld && git show 1a3c227:.pob-fidelity-baseline.tsv) > $W/fid_$ARM.tsv
  echo "START $(date +%T) selfplay-batch N=$N SRC=$SRC COEF=$COEF" > $W/ms_$ARM.log
  SEEDS=$N SNAP_DIR=$W/snap_$ARM POB_FIDELITY_BASELINE=$W/fid_$ARM.tsv POB_S208_DIR=$O POB_COEF="$COEF" \
    bash $SRC/measure.sh $W/b${N}_$ARM.txt selfplay-batch >> $W/ms_$ARM.log 2>&1
  echo "rc=$? END $(date +%T)" >> $W/ms_$ARM.log
  rm -rf $W/snap_$ARM
  tail -4 $W/ms_$ARM.log
  echo BATCHDONE $ARM
fi
