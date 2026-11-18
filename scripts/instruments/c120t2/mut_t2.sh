#!/usr/bin/env bash
# (1202) c120t2 突然変異（使い捨ての木・事前登録 §3）：μ0〔旗 0〕・μ1〜μ7 を 1 本ずつ入れ、DepositSitesOnlyTests の赤と緑を数える。
#   bash scripts/instruments/c120t2/mut_t2.sh <コミット> <作業フォルダ（D:/…）> <出力（measurements/…/mut_t2.txt）>
C=${1:?コミット}; WK=${2:?作業フォルダ}; OUT=${3:?出力}
R="$(cd "$(dirname "$0")/../../.." && pwd)"
T=$WK/mut/tree; A=$WK/mut/art; export TEMP=$WK/tmp TMP=$WK/tmp; mkdir -p $WK/tmp
rm -rf $WK/mut; mkdir -p $T
(cd "$R" && git archive $C) | tar -x -C $T || exit 1
R1="python -I $R/scripts/instruments/tools/replace1.py"
SIM=PriceOfBread.SimCore/Simulation.cs; ECO=PriceOfBread.SimCore/EconomyStep.cs; TST=PriceOfBread.Tests/DepositSitesOnlyTests.cs
for f in $SIM $ECO $TST; do cp $T/$f $T/$f.orig; done
echo "# (1202) c120t2 突然変異 木=$C $(date '+%F %T')・試験＝DepositSitesOnlyTests（T1・T1prime・T2×2・T3・T4・T5・T6×2）" > "$OUT"
mutate() {   # mutate <名> <ファイル> <旧> <新>
  for f in $SIM $ECO $TST; do cp $T/$f.orig $T/$f; done
  if [ "$1" != base ]; then (cd $T && $R1 "$2" "$3" "$4" > /dev/null) || { echo "$1 置き換え失敗" >> "$OUT"; return; }; fi
  (cd $T && dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/mut_build_$1.txt 2>&1) || { echo "$1 BUILDFAIL" >> "$OUT"; return; }
  (cd $T && dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~DepositSitesOnlyTests" > $WK/mut_test_$1.txt 2>&1)
  RED=$(grep -E '^\s+(失敗|Failed) ' $WK/mut_test_$1.txt | sed -E 's/^\s+(失敗|Failed) PriceOfBread\.Tests\.DepositSitesOnlyTests\.//; s/ \[.*//' | sort | tr '\n' ' ')
  echo "$1	$(grep -E '合計|Total tests' $WK/mut_test_$1.txt | tail -1 | tr -s ' ')	赤: ${RED:-なし}" >> "$OUT"
}
mutate base x x x
mutate mu0_flag0 $TST 'private const double Flag = 1.0;' 'private const double Flag = 0.0;'
mutate mu1_nocapture $SIM 'if (!HoldsSiteDeposit(p)) continue;' 'if (!HoldsSiteDeposit(p) || p.GoodId >= 0) continue;'
mutate mu2_allhold $SIM '=> p.SiteRow >= 0 && p.SiteFound && Catalog.IsMineral(p.GoodId);' '=> Catalog.IsMineral(p.GoodId);'
mutate mu3_capturezero $SIM 'if (!(o > 0)) continue;' 'if (double.IsNaN(o)) continue;'
mutate mu4_nograde $SIM 'if (!(p.SiteDepositInitial > 1e-9)) return 1.0;' 'if (p.GoodId >= 0) return 1.0;'
mutate mu5_regiongrade $SIM 'if (!(c.DepositSitesOnly > 0)) return r.MineralGradeFactor;' 'if (p.GoodId >= 0) return r.MineralGradeFactor;'
mutate mu6_knee $SIM 'double grade = c.DepositGradeFloor + (1.0 - c.DepositGradeFloor) * (p.SiteDeposit / p.SiteDepositInitial);' 'double fr = p.SiteDeposit / p.SiteDepositInitial; double grade = fr >= c.DepositGradeKnee.Value ? 1.0 : c.DepositGradeFloor + (1.0 - c.DepositGradeFloor) * fr / c.DepositGradeKnee.Value;'
mutate mu7_both $ECO 'Simulation.MineralGradeOf(s, region, coef) : 1.0;' 'Simulation.MineralGradeOf(s, region, coef) * (s.SiteRow >= 0 ? region.MineralGradeFactor : 1.0) : 1.0;'
echo "MUTDONE $(date '+%F %T')" >> "$OUT"
rm -rf $WK/mut
