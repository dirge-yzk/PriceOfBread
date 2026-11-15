#!/usr/bin/env bash
# (1166) c115sv 試験の突然変異：<コミット> を写した使い捨ての木に mut_sv.py の形を当て、SoldierVacancyFromZeroTests だけを回す。
#   bash scripts/instruments/c115sv/tmut.sh <コミット> <作業フォルダ（D:/…）> <形...>   出力：<作業フォルダ>/tmut_<形>.txt（合格と失敗の行）
C=${1:?コミット}; WK=${2:?作業フォルダ}; shift 2
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$WK/tmp TMP=$WK/tmp; mkdir -p $WK/tmp
for M in "$@"; do
  T=$WK/tree_$M; rm -rf $T; mkdir -p $T
  git archive $C | tar -x -C $T
  SV_MUT=$M python -I $S/c115sv/mut_sv.py $T > $WK/tmut_$M.txt 2>&1 || { echo "$M EDITFAIL"; continue; }
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $WK/art_$M -v q -nologo > $WK/tmut_${M}_build.txt 2>&1 || { echo "$M BUILDFAIL" >> $WK/tmut_$M.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $WK/art_$M --no-build --filter "FullyQualifiedName~SoldierVacancyFromZeroTests" 2>&1 | grep -E "^\s+(失敗|合格|Failed|Passed) |成功!|失敗!|合計" >> $WK/tmut_$M.txt
  echo "$M $(grep -E '合計' $WK/tmut_$M.txt | tail -1)"
  rm -rf $T $WK/art_$M
done
echo TMUTDONE
