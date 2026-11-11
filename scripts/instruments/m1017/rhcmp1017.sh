#!/usr/bin/env bash
# (1017) 合流：c94rh の rh_board.py の財の番号の直書きを名前へ替えた写しが、元の台本と同じ出力を出すかを確かめる（読むだけ）。
#   bash rhcmp1017.sh <元の台本> <写しの台本> <出力フォルダ>
export PYTHONUTF8=1
T=D:/PoB_work/wt_m1017; O=${3:?出力}; mkdir -p $O
for v in old:$1 new:$2; do
  python -X utf8 -I ${v#*:} $T/PriceOfBread.SimCore/world.json $T/PriceOfBread.SimCore/goods.json $T/PriceOfBread.SimCore/Catalog.cs D:/PoB_work/keep_1011/base_ins 1-10 $O/board_${v%%:*}.tsv || exit 1
done
cmp $O/board_old.tsv $O/board_new.tsv && echo SAME_OLD_NEW
cmp $O/board_old.tsv D:/PoB_work/r1015_work/out/board.tsv && echo SAME_AS_LINE
echo RHDONE
