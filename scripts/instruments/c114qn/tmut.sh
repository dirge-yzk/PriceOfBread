#!/usr/bin/env bash
# (1158) c114qn 試験の突然変異（使い捨ての木＝<コミット> の git archive＋scripts/instruments/c113qn/mut.py・ColonialQuinineTests だけ）。
#   bash scripts/instruments/c114qn/tmut.sh <コミット> <作業フォルダ（D:/…）> <出力の表> 形...
export PATH="/c/Program Files/dotnet:$PATH" TEMP=${2}/tmp TMP=${2}/tmp
C=${1:?コミット}; W=${2:?作業フォルダ}; OUT=${3:?出力}; shift 3
cd "$(dirname "$0")/../../.."; R=$(pwd); mkdir -p $W/tmp
: > $OUT
for F in "$@"; do
  T=$W/tmut_$F; rm -rf "$T"; mkdir -p "$T"
  git -C "$R" archive "$C" | tar -x -C "$T"
  if [ "$F" != m0 ]; then QN_MUT=$F python -X utf8 "$R/scripts/instruments/c113qn/mut.py" "$T" > $W/tmut_$F.edit.txt 2>&1 || { echo "$F EDITFAIL" >> $OUT; continue; }; fi
  dotnet build $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/tmut_art_$F --disable-build-servers > $W/tmut_$F.build.txt 2>&1 || { echo "$F BUILDFAIL" >> $OUT; continue; }
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $W/tmut_art_$F --filter "FullyQualifiedName~ColonialQuinineTests" > $W/tmut_$F.test.txt 2>&1
  FAILED=$(grep -o 'ColonialQuinineTests\.Q[0-9b]*' $W/tmut_$F.test.txt | sed 's/.*\.//' | sort -u | tr '\n' ' ')
  echo "$F 赤＝${FAILED:-なし} ・$(grep -h '合計:' $W/tmut_$F.test.txt | tail -1 | sed 's/ - .*//')" >> $OUT
  rm -rf "$T" "$W/tmut_art_$F"
done
cat $OUT
