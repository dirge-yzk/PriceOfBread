#!/usr/bin/env bash
# (1178) 合流：線 t1 の v の指摘〈旗 1 の新しい盤面で、二重計上の突然変異が TradeClearingTests の番人で赤になるか〉を使い捨ての木で確かめる（エンジンの .cs は使い捨ての木の中だけで変える）。
#   bash mutdc1178.sh <コミット>   出力 D:/PoB_work/m1178_work/mutdc/
H=${1:?コミット}; W=D:/PoB_work/m1178_work/mutdc; export TEMP=D:/PoB_work/m1178_work/tmp TMP=D:/PoB_work/m1178_work/tmp
rm -rf $W/src && mkdir -p $W/src && (cd C:/Users/dirge/Documents/PriceOfBread && git archive $H) | tar -x -C $W/src || exit 1
python -X utf8 -I $W/src/scripts/instruments/tools/replace1.py $W/src/PriceOfBread.SimCore/BilateralMarket.cs 'double newUnmet = Math.Min(pot - doneQ, Math.Max(0, X - doneQ - unmetQ));' 'double newUnmet = Math.Min(pot - doneQ, Math.Max(0, X - doneQ));' || exit 3
F='FullyQualifiedName~TradeClearingTests'
dotnet test $W/src/PriceOfBread.Tests -c Release --artifacts-path $W/art -p:UseSharedCompilation=false --filter "$F" > $W/mut_dc.txt 2>&1; echo "mut rc=$?" >> $W/mut_dc.txt
grep -E '失敗|Failed|合格|Passed|成功' $W/mut_dc.txt | tail -8
rm -rf $W/src $W/art
echo MUTDONE
