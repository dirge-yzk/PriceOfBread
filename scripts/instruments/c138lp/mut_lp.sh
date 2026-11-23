#!/usr/bin/env bash
# (1323) c138lp 突然変異の腕（使い捨ての木・LawPreferenceDirectionTests だけを回す）。結果は <作業>/mut/summary.txt と M*.txt。
#   bash mut_lp.sh <コミット> <作業フォルダ（D:/…）> [N1 N2 …]
C=${1:?コミット}; WK=${2:?作業}; shift 2; MS=${*:-N1 N2 N3 N4 N5 N6 N7}
cd "$(dirname "$0")/../../.."; R=$(pwd); D0=$R/scripts/instruments/c138lp
export TEMP=$WK/tmp TMP=$WK/tmp; mkdir -p $WK/tmp $WK/mut
O=$WK/mut/summary.txt; echo "# (1323) 突然変異 基=$C $(date '+%F %T')" > $O
for M in $MS; do
  T=$WK/mut_$M; rm -rf $T; mkdir -p $T
  git archive $C | tar -x -C $T
  python -X utf8 -I $D0/mut_lp.py $T $M >> $O 2>&1 || { echo "$M MUTFAIL" >> $O; continue; }
  ( cd $T && dotnet build PriceOfBread.Tests -c Release --artifacts-path $WK/mutart -v q -nologo > $WK/mut/build_$M.txt 2>&1 ) || { echo "$M BUILDFAIL" >> $O; continue; }
  ( cd $T && dotnet test PriceOfBread.Tests -c Release --artifacts-path $WK/mutart --no-build --filter "FullyQualifiedName~LawPreferenceDirectionTests" > $WK/mut/$M.txt 2>&1 )
  echo "$M rc=$? $(grep -E '合計:' $WK/mut/$M.txt | tail -1 | tr -s ' ')" >> $O
  grep -oE "LawPreferenceDirectionTests\.T[0-9][^ (]*(\([^)]*\))?" $WK/mut/$M.txt | sort -u | sed "s/^/  $M 赤: /" >> $O
  rm -rf $T
done
echo "MUTDONE $(date '+%F %T')" >> $O
