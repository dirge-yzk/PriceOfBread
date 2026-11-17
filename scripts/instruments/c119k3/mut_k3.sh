#!/usr/bin/env bash
# (1194) c119k3 突然変異 M1〜M4（使い捨ての木で試験 PeacetimeConscriptGrievanceTests だけを回す・c118nc/mut_nc.sh の型）。
#   bash scripts/instruments/c119k3/mut_k3.sh <試験のコミット（係数 0）> <点火のコミット> <作業フォルダ（D:/…）>   結果は <作業フォルダ>/mut_M?.txt
C0=${1:?試験のコミット}; C1=${2:?点火のコミット}; WK=${3:?作業フォルダ}; mkdir -p $WK/tmp
export TEMP=$WK/tmp TMP=$WK/tmp
cd "$(dirname "$0")/../../.."; R=$(pwd); RP=scripts/instruments/tools/replace1.py; MF=PriceOfBread.SimCore/Military.cs
for M in M1 M2 M3 M4; do
  T=$WK/tree_$M; rm -rf $T; mkdir -p $T
  if [ $M = M1 ]; then git archive $C0 | tar -x -C $T; echo "M1＝係数 0 の木（$C0）" > $WK/mut_$M.txt; else git archive $C1 | tar -x -C $T; : > $WK/mut_$M.txt; fi
  case $M in
    M2) python -I $RP $T/$MF "(r.Tier == RegionTier.Colonial || (r.Rule.Length == 4 && r.Rule[1] > r.Rule[0]))" "true" >> $WK/mut_$M.txt ;;
    M3) python -I $RP $T/$MF "(r.Tier == RegionTier.Colonial || (r.Rule.Length == 4 && r.Rule[1] > r.Rule[0]))" "(r.Tier == RegionTier.Colonial)" >> $WK/mut_$M.txt ;;
    M4) python -I $RP $T/$MF "peacetimeColonial ? c.PeacetimeConscriptGrievance : 1.0" "peacetimeColonial ? c.PeacetimeConscriptGrievance : c.PeacetimeConscriptGrievance" >> $WK/mut_$M.txt ;;
  esac
  (cd $T && dotnet build PriceOfBread.Tests -c Release --artifacts-path $WK/art_$M -v q -nologo > $WK/build_$M.txt 2>&1) || { echo "BUILDFAIL" >> $WK/mut_$M.txt; continue; }
  (cd $T && dotnet test PriceOfBread.Tests -c Release --artifacts-path $WK/art_$M --no-build --filter "FullyQualifiedName~PeacetimeConscriptGrievanceTests" > $WK/t_$M.txt 2>&1)
  grep -E "^\s+(失敗|合格) " $WK/t_$M.txt >> $WK/mut_$M.txt; grep -E "失敗!|成功!" $WK/t_$M.txt >> $WK/mut_$M.txt
  rm -rf $T $WK/art_$M
done
echo MUTDONE
