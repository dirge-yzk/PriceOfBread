#!/usr/bin/env bash
# (942) 使い捨て：08e2f8d0 の木（プローブ入り）で建設の閉じの試験を 1 本回し、州×月の建設の払いと州の表の行を probe_<tag>.tsv へ写す。
# 引数：tag（f1＝旗 1 のまま・f0＝LandTaxFixedBasis 0）
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH"
TAG="${1:?tag}"
T=D:/PoB_work/r942_work/wt; B=D:/PoB_work/r942_work
F='FullyQualifiedName~InstrumentsTests.建設の州の表と専売の取り分が別の積算器に閉じる'
dotnet build $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $B/art -v q > $B/build_$TAG.txt 2>&1 || { echo "BUILDFAIL"; tail -20 $B/build_$TAG.txt; exit 9; }
R942_PROBE=$B/probe_$TAG.tsv dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $B/art --filter "$F" > $B/test_$TAG.txt 2>&1
echo "$TAG rc=$? $(grep -h '合計:' $B/test_$TAG.txt | head -1)"
grep -h -A1 'エラー メッセージ' $B/test_$TAG.txt | tail -1
wc -l $B/probe_$TAG.tsv
