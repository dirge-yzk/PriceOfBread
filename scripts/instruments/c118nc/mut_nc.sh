#!/usr/bin/env bash
# (1186) c118nc 突然変異 M1〜M3（使い捨ての木で係数の既定を 1 本ずつ旧の値へ戻し、T1〜T3 だけを回す）。
#   bash scripts/instruments/c118nc/mut_nc.sh <コミット> <作業フォルダ（D:/…）>   結果は <作業フォルダ>/mut_M?.txt
C=${1:?コミット}; WK=${2:?作業フォルダ}; mkdir -p $WK/tmp
export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; R=$(pwd); RP=scripts/instruments/tools/replace1.py
for M in M1 M2 M3; do
  T=$WK/tree_$M; rm -rf $T; mkdir -p $T
  git archive $C | tar -x -C $T
  case $M in
    M1) python -I $RP $T/PriceOfBread.SimCore/Coefficients.p10_ai_silver_overrides.cs "SecessionConsciousnessWeight = 0.0;" "SecessionConsciousnessWeight = 1.0;" ;;
    M2) python -I $RP $T/PriceOfBread.SimCore/Coefficients.p10_ai_silver_overrides.cs "NationBuildMobilizationWeight = 0.0;" "NationBuildMobilizationWeight = 0.5;" ;;
    M3) python -I $RP $T/PriceOfBread.SimCore/Coefficients.p06_rank_discovery.cs "RevolutionConsciousness = 0.0;" "RevolutionConsciousness = 0.25;" ;;
  esac
  echo "replace rc=$?" > $WK/mut_$M.txt
  (cd $T && dotnet build PriceOfBread.Tests -c Release --artifacts-path $WK/art_$M -v q -nologo > $WK/build_$M.txt 2>&1) || { echo "BUILDFAIL" >> $WK/mut_$M.txt; continue; }
  (cd $T && dotnet test PriceOfBread.Tests -c Release --artifacts-path $WK/art_$M --no-build --filter "FullyQualifiedName~NationalConsciousnessReadersOffTests" > $WK/t_$M.txt 2>&1)
  grep -E "^\s+(失敗|合格) " $WK/t_$M.txt >> $WK/mut_$M.txt; grep -E "失敗!|成功!" $WK/t_$M.txt >> $WK/mut_$M.txt
  rm -rf $T $WK/art_$M
done
echo MUTDONE
