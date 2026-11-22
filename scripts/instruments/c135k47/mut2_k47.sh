#!/usr/bin/env bash
# (1305) c135k47 DiscoveryEngineerTests T4 の書き換えの突然変異（事前登録の追記の Mt0〜Mt3）：bash mut2_k47.sh <コミット> <出力（D:/…）>
#   Mt0＝書き換えのまま（緑の確かめ）・Mt1＝名簿を空にする・Mt2＝旗が何もしない（石油灯油を建てる）・Mt3＝名簿の行の Terms の検査を外す（記録）。
REV=${1:?}; OUT=${2:?}; R=D:/PoB_work/wt_c135k47; W=D:/PoB_work/c135k47_work/mut2
export TEMP=D:/PoB_work/c135k47_work/tmp TMP=D:/PoB_work/c135k47_work/tmp DOTNET_CLI_TELEMETRY_OPTOUT=1
mkdir -p $OUT $TEMP
for MU in Mt0 Mt1 Mt2 Mt3; do
  T=$W/$MU; rm -rf $T; mkdir -p $T
  git -C $R archive $REV PriceOfBread.SimCore PriceOfBread.Tests | tar -x -C $T || { echo ARCHFAIL; exit 1; }
  F=""
  case $MU in
    Mt1) F=$T/PriceOfBread.Tests/DiscoveryEngineerTests.cs; A='RemovedDuplicateRows = ["石油灯油"];'; B='RemovedDuplicateRows = [];';;
    Mt2) F=$T/PriceOfBread.SimCore/Simulation.cs; A='if (!(coef.LampOilPetroleumDupOff > 0))   // (1305)'; B='if (true)   // (1305)';;
    Mt3) F=$T/PriceOfBread.Tests/DiscoveryEngineerTests.cs; A='if (rp is null) { noRep.Add(d.Name); Assert.True(terms[row] == default,'; B='if (rp is null) { noRep.Add(d.Name); Assert.True(true || terms[row] == default,';;
  esac
  if [ -n "$F" ]; then python -I $R/scripts/instruments/tools/replace1.py "$F" "$A" "$B" > $OUT/$MU.replace.txt 2>&1 || { echo "$MU REPLACEFAIL" >> $OUT/mut2.txt; continue; }; fi
  dotnet build $T/PriceOfBread.Tests -c Release --artifacts-path $W/art_$MU -v q -nologo > $OUT/$MU.build.txt 2>&1 || { echo "$MU BUILDFAIL" >> $OUT/mut2.txt; continue; }
  dotnet test $T/PriceOfBread.Tests -c Release --artifacts-path $W/art_$MU --no-build --filter "FullyQualifiedName~DiscoveryEngineerTests.親の産業の定義は" > $OUT/$MU.test.txt 2>&1; RC=$?
  echo "$MU rc=$RC $(grep -E '合計' $OUT/$MU.test.txt | tail -1)" >> $OUT/mut2.txt
  grep -A2 "エラー メッセージ" $OUT/$MU.test.txt | grep -v "エラー メッセージ" | head -2 | cut -c1-160 >> $OUT/mut2.txt
  rm -rf $T $W/art_$MU
done
echo "MUT2DONE $(date '+%F %T')" >> $OUT/mut2.txt
