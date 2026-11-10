#!/usr/bin/env bash
# 使い捨ての木で突然変異 1 本：木を写し、変異を当て、WarInstrumentTests を回す。出力の全文を OUT へ追記。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SRC=D:/PoB_work/wt_c95wn; W=D:/PoB_work/c95wn_work; OUT=$W/mutations_full.txt
for M in "$@"; do
  T=$W/mut_$M; rm -rf $T; mkdir -p $T
  tar -cf - -C $SRC --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs . | tar -xf - -C $T
  echo "==================== 突然変異 M-$M ====================" >> $OUT
  python $W/scripts/mut.py $T $M >> $OUT 2>&1 || { echo "APPLYFAIL $M" >> $OUT; continue; }
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/mutart --filter "FullyQualifiedName~WarInstrumentTests" >> $OUT 2>&1
  echo "---- M-$M の終了コード $?" >> $OUT
  rm -rf $T
done
echo done
