#!/usr/bin/env bash
# (834) 機構の試験の突然変異（使い捨ての木・(827) mutunit827.sh の型）。  bash mutunit834.sh <コミット> <出力> <フィルタ> [M1 M2 ...]
#   木 D:/PoB_work/c67sv_mut/tree_<M>・ビルドの出力 D:/PoB_work/c67sv_mut/art。順に回す（並べない）。元の木は D:/PoB_work/wt_c67sv。
export PATH="/c/Program Files/dotnet:$PATH" PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1
H=${1:?コミット}; OUT=${2:?出力}; F=${3:?フィルタ}; shift 3
MS=${*:-M1 M2 M3 M5}
W=D:/PoB_work/c67sv_mut; A=$W/art; SRC=D:/PoB_work/wt_c67sv
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p $W; echo "# (834) mutunit コミット $H フィルタ $F" >> $OUT
for m in $MS; do
  T=$W/tree_$m; rm -rf $T; mkdir -p $T && (cd $SRC && git archive $H) | tar -x -C $T
  python -I $SP/mutate834.py $T $m >> $OUT 2>&1 || { echo "$m APPLYFAIL" >> $OUT; continue; }
  dotnet build $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --disable-build-servers -v q > $W/build_$m.txt 2>&1 || { echo "$m BUILDFAIL" >> $OUT; continue; }
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --filter "$F" > $W/test_$m.txt 2>&1
  echo "$m $(grep -E '^(失敗|成功)!' $W/test_$m.txt | head -1) :: $(grep -E '^\s+失敗 ' $W/test_$m.txt | cut -d'[' -f1 | tr '\n' ';')" >> $OUT
  rm -rf $T
done
echo MUTUNITDONE >> $OUT
