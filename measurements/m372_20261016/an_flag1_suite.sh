#!/bin/bash
# 線 an の旗 1（CapacityAnchorFromRegions=1）のフルスイート：作者が〈採る〉を選べる状態にする段（採るのではない）
W=D:/PoB_work/merge_372; REPO=/c/Users/dirge/Documents/PriceOfBread
C=564bfcb5; T=$W/src_an1; rm -rf $T && mkdir -p $T
git -C $REPO archive $C | tar -xf - -C $T
PYTHONUTF8=1 python - "$T/PriceOfBread.SimCore/Coefficients.cs" <<'PY'
import sys
p=sys.argv[1]; b=open(p,'rb').read(); k=b'public double CapacityAnchorFromRegions = 0.0;'
assert b.count(k)==1; open(p,'wb').write(b.replace(k, b'public double CapacityAnchorFromRegions = 1.0;'))
PY
O=$W/an_flag1_suite.txt
{ echo "木 $(git -C $REPO rev-parse $C)（枝 an の先端・コードは 0cf4b15d と同じ）を git archive で写し、既定を書き換えた：CapacityAnchorFromRegions=1"; grep -n "public double CapacityAnchorFromRegions" $T/PriceOfBread.SimCore/Coefficients.cs | cut -c1-80; } > $O
START=$(date +%s)
"/c/Program Files/dotnet/dotnet" test "$T/PriceOfBread.slnx" -c Release --artifacts-path "$W/art_an1" --filter "Category!=FullLength" > $W/an_flag1_suite.log 2>&1
echo "rc=$? 壁 $(( $(date +%s) - START )) 秒" >> $O
grep -E "^\s+失敗 |合計:" $W/an_flag1_suite.log >> $O
grep -A3 "^\s+失敗 " $W/an_flag1_suite.log | grep -v "^\s+at " | head -80 >> $O
