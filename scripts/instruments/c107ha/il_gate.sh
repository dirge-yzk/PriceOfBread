#!/usr/bin/env bash
# 門 3：同じ絶対パスの使い捨ての木で前後を建て、SimCore.dll を比べる。使い方：bash il_gate.sh <前のコミット> <後のコミット> <出力フォルダ>
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
BEF=$1; AFT=$2; OUT=$3; IL=D:/PoB_work/wt_c107ha_il; REPO=D:/PoB_work/wt_c107ha
mkdir -p $OUT
[ -d $IL ] || git -C $REPO worktree add --detach $IL $BEF
for st in bef aft; do
  for tr in raw tr; do
    c=$BEF; [ $st = aft ] && c=$AFT
    git -C $IL checkout -q --detach $c
    git -C $IL clean -fdxq -e nothing
    if [ $tr = tr ]; then python -X utf8 -I $REPO/scripts/instruments/c107ha/strip_callerline.py $IL; fi
    rm -rf $OUT/art_x
    dotnet build $IL/PriceOfBread.SimCore/PriceOfBread.SimCore.csproj -c Release --artifacts-path $OUT/art_x --disable-build-servers -p:Deterministic=true -p:DebugType=none -p:IncludeSourceRevisionInInformationalVersion=false > $OUT/build_${st}_$tr.txt 2>&1
    echo "build $st $tr rc=$?" >> $OUT/summary.txt
    f=$(find $OUT/art_x -name PriceOfBread.SimCore.dll -path "*release*" | head -1)
    cp "$f" $OUT/SimCore_${st}_$tr.dll
    git -C $IL checkout -q -- . 
  done
done
for tr in raw tr; do
  cmp -l $OUT/SimCore_bef_$tr.dll $OUT/SimCore_aft_$tr.dll | wc -l | sed "s/^/diffbytes $tr /" >> $OUT/summary.txt
  ls -l $OUT/SimCore_bef_$tr.dll $OUT/SimCore_aft_$tr.dll | awk '{print $5}' | tr '\n' ' ' >> $OUT/summary.txt; echo >> $OUT/summary.txt
done
echo DONE >> $OUT/summary.txt
