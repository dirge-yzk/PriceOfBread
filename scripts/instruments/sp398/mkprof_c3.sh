#!/bin/bash
set -e
SRC=$1; DST=$2; ART=$3
rm -rf $DST && mkdir -p $DST
tar -cf - -C $SRC --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs . | tar -xf - -C $DST
python D:/PoB_work/sp/patch_pt_c3.py $DST
python D:/PoB_work/sp/patch_pt2.py $DST
python D:/PoB_work/sp/patch_pt3c.py $DST
python D:/PoB_work/sp/patch_toggle3.py $DST
cd $DST && dotnet build PriceOfBread.Runner -c Release --artifacts-path $ART 2>&1 | grep -E "エラー|error" | head -5
