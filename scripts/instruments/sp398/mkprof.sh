#!/bin/bash
# usage: mkprof.sh <src tree> <dest tree> <art dir> [patchsuffix]
set -e
SRC=$1; DST=$2; ART=$3; PS=${4:-_b}
rm -rf $DST && mkdir -p $DST
tar -cf - -C $SRC --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs . | tar -xf - -C $DST
python D:/PoB_work/sp/patch_pt$PS.py $DST
python D:/PoB_work/sp/patch_pt2.py $DST
python D:/PoB_work/sp/patch_pt3.py $DST
cd $DST && dotnet build PriceOfBread.Runner -c Release --artifacts-path $ART 2>&1 | grep -E "エラー|error" | head -3
