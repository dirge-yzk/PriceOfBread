#!/usr/bin/env bash
# (793) 読み手 r793 の腕：bash run_r793.sh <腕> <旗の値> <種の数>（run_in_background で起動）
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/r793_work PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1 MEASURE_CACHE=0
ARM="$1"; FLAG="$2"; N="$3"; REPO=C:/Users/dirge/Documents/PriceOfBread
export POB_REPO=$REPO
mkdir -p $POB_WORK/marks; rm -f $POB_WORK/marks/$ARM.done
bash $REPO/scripts/instruments/batch.sh prep "$ARM" commit:3b95b218 plain $POB_WORK/scripts/inj_r793.py || { echo "rc=9 prep" > $POB_WORK/marks/$ARM.done; exit 9; }
echo "OpiumAddictionCapOff=$FLAG" > $POB_WORK/coef_$ARM.txt
INS=1 bash $REPO/scripts/instruments/batch.sh run "$ARM" "$N"
RC=$?
echo "rc=$RC end=$(date '+%F %T')" > $POB_WORK/marks/$ARM.done
exit $RC
