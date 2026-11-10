#!/usr/bin/env bash
# (1023) 使い捨ての木で突然変異：木を写し、変異を当て、赤を期待する試験の束を回す。出力の全文を OUT へ。 使い方：mut.sh <変異>...
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SRC=D:/PoB_work/wt_c96wn; W=D:/PoB_work/c96wn_work; OUT=$W/mutations_full.txt
for M in "$@"; do
  case $M in N4) F="OnDoubleReadScanTests";; N5) F="InvadeInstrumentTests";; *) F="WarInstrumentTests";; esac
  T=$W/mut_$M; rm -rf $T; mkdir -p $T
  tar -cf - -C $SRC --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs . | tar -xf - -C $T
  echo "==================== 突然変異 M-$M（束 $F）====================" >> $OUT
  python $SRC/scripts/instruments/c96wn/mut.py $T $M >> $OUT 2>&1 || { echo "APPLYFAIL $M" >> $OUT; continue; }
  dotnet test $T/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/mutart --filter "FullyQualifiedName~$F" >> $OUT 2>&1
  echo "---- M-$M の終了コード $?" >> $OUT
  rm -rf $T
done
echo done
