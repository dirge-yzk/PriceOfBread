#!/usr/bin/env bash
# (1017) 合流：c94ro の突然変異 Mu1〜Mu6 を、コミットした試験ファイル（両シチリアの盤面つき）で回し直す（反証役の低 ⑥・c94ro/mutate.py をそのまま使う）。
#   bash mutro1017.sh <コミット>
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; W=D:/PoB_work/m1017_work/mutro; S=$W/src
rm -rf $S && mkdir -p $S && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H) | tar -x -C $S || exit 1
python -X utf8 $S/scripts/instruments/c94ro/mutate.py $S $W/out $W/art || exit 1
echo MUTRODONE
