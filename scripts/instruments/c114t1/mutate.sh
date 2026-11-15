#!/bin/bash
# (1159) c114t1 突然変異の束：HEAD の写しに 1 つずつ入れて対の試験を回す（使い捨ての木・D:/PoB_work/c114t1_work/mut）
export TEMP=D:/PoB_work/c114t1_work/tmp TMP=D:/PoB_work/c114t1_work/tmp
W=D:/PoB_work/c114t1_work; OUT=$W/mutations.txt; : > $OUT
REV=${REV:-HEAD}
run() {  # 名 フィルタ [名2]
  M=$1; F=$2; T=$W/mut/$M; rm -rf $T; mkdir -p $T
  git -C /d/PoB_work/wt_c114t1 archive $REV | tar -x -C $T
  python -I $W/scripts/mutate.py $T $M >> $OUT 2>&1 || { echo "$M 置き換え失敗" >> $OUT; return; }
  [ -n "$3" ] && python -I $W/scripts/mutate.py $T $3 >> $OUT 2>&1
  (cd $T && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/mut_art_$M --filter "$F" > $W/mut_$M.log 2>&1)
  echo "$M rc=$? $(grep -E '成功!|失敗!' $W/mut_$M.log | tail -1)" >> $OUT
  grep -E "^\s+失敗 " $W/mut_$M.log | head -5 >> $OUT
  rm -rf $T $W/mut_art_$M
}
echo "木 $(git -C /d/PoB_work/wt_c114t1 rev-parse --short $REV)" >> $OUT
run M2 "FullyQualifiedName~TradeT1Tests.M7"
run M3 "FullyQualifiedName~TradeT1Tests.M7"
run M4 "FullyQualifiedName~TradeT1Tests.M8"
run M5 "FullyQualifiedName~TradeT1Tests.M3" M5b
run MF "FullyQualifiedName~TradeT1Tests.M9"
echo MUTDONE >> $OUT
