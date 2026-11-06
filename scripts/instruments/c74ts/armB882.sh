#!/usr/bin/env bash
# (882) c74ts 腕 B（旗 1 の木の計器つき 10 種・種 1〜10・POB_CEARN=0＝cearn_s・cgrp_s・cpop_s を書かない宣言・この線の門と止める線はその表を読まない）
#   bash armB882.sh <旗 1 のコミット>   ⚠起こす前に arms.sh free と D: の空きを呼び手が確かめる（写しの元：c73ts/armB877.sh・パスと名だけ変えた）
#   出力：D:/PoB_work/c74ts_work/armB/out_c74tsB_10/ins・成績表 D:/PoB_work/c74ts_work/armB/b10_c74tsB_10.txt
H=${1:?コミット}
F=$(python -c "import shutil;print(int(shutil.disk_usage('D:/').free/1e9))")
[ "$F" -ge 30 ] || { echo "D: の空き ${F}GB＜床 30GB"; exit 9; }
echo "D: の空き ${F}GB（起こす時）"
export POB_WORK=D:/PoB_work/c74ts_work/armB POB_REPO=D:/PoB_work/wt_c74ts MEASURE_CACHE=0 POB_CEARN=0
mkdir -p /d/PoB_work/c74ts_work/armB
IS=/d/PoB_work/wt_c74ts/scripts/instruments
bash $IS/batch.sh prep c74tsB commit:$H plain && INS=1 bash $IS/batch.sh run c74tsB 10 1
