#!/usr/bin/env bash
# (901) 突然変異の腕（使い捨ての木 D:/PoB_work/c77cs_work/mut＝worktree の今のファイルの写し・SimCore の 2 ファイルを毎回元へ戻してから置き換える）
#   bash mut901.sh <出力ファイル>   試験の組＝SummonByInterestShareTests＋CrownPcInstrumentTests
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
OUT=${1:?出力}; WT=D:/PoB_work/wt_c77cs; T=D:/PoB_work/c77cs_work/mut; A=D:/PoB_work/c77cs_work/mut_art
rm -rf $T && mkdir -p $T || exit 1
(cd $WT && git archive HEAD) | tar -x -C $T || exit 1
cp $WT/PriceOfBread.Tests/CrownPcInstrumentTests.cs $WT/PriceOfBread.Tests/SummonByInterestShareTests.cs $T/PriceOfBread.Tests/   # 今の試験（未コミットでも）
F="FullyQualifiedName~SummonByInterestShareTests|FullyQualifiedName~CrownPcInstrumentTests"
: > $OUT
for m in M0_none M1e_engine M1_s0 M2_t1 M3_noguard M4_step M5_insnosplit M6_clamp M7_nomax M8_flag0; do
  cp $WT/PriceOfBread.SimCore/CrownParliament.cs $WT/PriceOfBread.SimCore/InstrumentsCrown.cs $T/PriceOfBread.SimCore/
  python -X utf8 $WT/scripts/instruments/c77cs/mut901.py $T $m >> $OUT || { echo "APPLYFAIL $m" >> $OUT; continue; }
  dotnet build $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $A --disable-build-servers > $T/build_$m.txt 2>&1 || { echo "BUILDFAIL $m" >> $OUT; tail -5 $T/build_$m.txt >> $OUT; continue; }
  echo "== $m" >> $OUT
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $A --filter "$F" > $T/test_$m.txt 2>&1
  grep -h "^\s*失敗 \|合計:\|中止" $T/test_$m.txt | sed 's/\[[0-9 .]*m\?s\]//' >> $OUT
done
echo MUTDONE >> $OUT
