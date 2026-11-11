#!/usr/bin/env bash
# (942) 使い捨て：fix942.patch を当てた 08e2f8d0 の木で、両側の門（旗 1・旗 0 で緑）と突然変異（赤）を 1 本ずつ回す。木は各変種の後に元へ戻す。
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH"
T=D:/PoB_work/r942_work/wt; B=D:/PoB_work/r942_work; G=$B/gate
mkdir -p $G
F='FullyQualifiedName~InstrumentsTests.建設の州の表と専売の取り分が別の積算器に閉じる'
cp $T/PriceOfBread.SimCore/Coefficients.cs $G/Coefficients.cs.keep
cp $T/PriceOfBread.SimCore/Instruments.cs $G/Instruments.cs.keep
cp $T/PriceOfBread.SimCore/EconomyStep.cs $G/EconomyStep.cs.keep
cp $T/PriceOfBread.Tests/InstrumentsTests.cs $G/InstrumentsTests.cs.keep
for v in G1_flag1 G2_flag0 M1_flag1 M1_flag0 M2_flag1 M2_flag0 M3_flag1 M3_flag0; do
  python -X utf8 -I $B/mut942.py "$T" "$v" || { echo "$v EDITFAIL"; continue; }
  dotnet build $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $B/art -v q > $G/build_$v.txt 2>&1 || { echo "$v BUILDFAIL"; tail -5 $G/build_$v.txt; }
  R942_PROBE=$G/probe_$v.tsv dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $B/art --filter "$F" > $G/test_$v.txt 2>&1
  echo "$v rc=$? $(grep -h '合計:' $G/test_$v.txt | head -1)"
  grep -h -A1 'エラー メッセージ' $G/test_$v.txt | tail -1
  cp $G/Coefficients.cs.keep $T/PriceOfBread.SimCore/Coefficients.cs
  cp $G/Instruments.cs.keep $T/PriceOfBread.SimCore/Instruments.cs
  cp $G/EconomyStep.cs.keep $T/PriceOfBread.SimCore/EconomyStep.cs
  cp $G/InstrumentsTests.cs.keep $T/PriceOfBread.Tests/InstrumentsTests.cs
done
echo GATEDONE
