#!/usr/bin/env bash
# (1305) c135k47 突然変異（事前登録の〈突然変異ごとの門の赤〉の表・㉚）：bash mut_k47.sh <コミット> <出力（D:/…）>
#   コミットを git archive で使い捨ての木へ出し、M ごとに 1 か所だけ書き換えて LampOilDuplicateRecipeTests と SecondRecipeContentsTests を回す。
#   M0＝試験の旗を 0 に置く（旗 0 の盤面で赤か）・M1＝旗が何もしない・M2＝ケロシンの方を消す（石油ケロシンを建てない）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c135k47; W=D:/PoB_work/c135k47_work/mut
export TEMP=D:/PoB_work/c135k47_work/tmp TMP=D:/PoB_work/c135k47_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in M0 M1 M2; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  case $MU in
    M0) F=$T/PriceOfBread.Tests/LampOilDuplicateRecipeTests.cs; A='        var w = Board(1);'; B='        var w = Board(0);';;
    M1) F=$T/PriceOfBread.SimCore/Simulation.cs; A='if (!(coef.LampOilPetroleumDupOff > 0))   // (1305)'; B='if (true)   // (1305)';;
    M2) F=$T/PriceOfBread.SimCore/Simulation.cs; A='if (keroDefault is not null)   // 灯油を作らない世界'; B='if (keroDefault is null)   // 灯油を作らない世界';;
  esac
  python -I $R/scripts/instruments/tools/replace1.py "$F" "$A" "$B" > $OUT/$MU.replace.txt 2>&1 || { echo "$MU REPLACEFAIL" >> $OUT/mut.txt; continue; }
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $W/art_$MU -v q -nologo > $OUT/$MU.build.txt 2>&1 || { echo "$MU BUILDFAIL" >> $OUT/mut.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $W/art_$MU --no-build --filter "FullyQualifiedName~LampOilDuplicateRecipeTests|FullyQualifiedName~SecondRecipeContentsTests" > $OUT/$MU.test.txt 2>&1; RC=$?
  echo "$MU rc=$RC $(grep -E '合計|成功!|失敗!' $OUT/$MU.test.txt | tail -1)" >> $OUT/mut.txt
  grep -E "^\s+失敗 " $OUT/$MU.test.txt | sed 's/^ */    /' >> $OUT/mut.txt
  rm -rf $T $W/art_$MU
done
echo "MUTDONE $(date '+%F %T')" >> $OUT/mut.txt
