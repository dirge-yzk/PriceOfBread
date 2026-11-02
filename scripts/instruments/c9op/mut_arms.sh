#!/usr/bin/env bash
# (787) c9op 突然変異の腕の準備：使い捨ての木（旗の既定 1）に Mu1〜Mu3 を順に当てて prep（写しを取る）し、すぐ戻す。腕は別に INS=1 batch.sh run c9opMuN 2 で回す。
#   bash scripts/instruments/c9op/mut_arms.sh <使い捨ての木>
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/c9op PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1 MEASURE_CACHE=0
T="${1:?木}"; D="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; export POB_REPO="$T"
for m in Mu1 Mu2 Mu3; do
  python -X utf8 -I $D/mutate.py $T $m || exit 1
  bash $D/../batch.sh prep c9op$m "dir:$T" plain || { python -X utf8 -I $D/mutate.py $T reset; exit 1; }
  python -X utf8 -I $D/mutate.py $T reset || exit 1
done
echo MUTPREPDONE
