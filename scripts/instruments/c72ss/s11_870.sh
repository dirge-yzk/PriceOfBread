#!/usr/bin/env bash
# (870) c72ss 腕 S11（(855) armB855.sh の形・種の起点だけ 11）＝旗 1 の木の計器つき 10 種・種 11〜20・POB_CEARN=0（cearn_s・cgrp_s・cpop_s を書かない宣言・門と止める線はその表を読まない）
#   bash s11_870.sh <旗 1 のコミット>   ⚠起こす前に arms.sh free と D: の空き（30GB 以上）を呼び手が確かめる
#   出力：D:/PoB_work/c72ss_work/s11/out_c72ssS_10_s11/ins・成績表 D:/PoB_work/c72ss_work/s11/b10_c72ssS_10_s11.txt
H=${1:?コミット}
export POB_WORK=D:/PoB_work/c72ss_work/s11 POB_REPO=D:/PoB_work/wt_c72ss MEASURE_CACHE=0 POB_CEARN=0
mkdir -p /d/PoB_work/c72ss_work/s11
IS=/d/PoB_work/wt_c72ss/scripts/instruments
bash $IS/batch.sh prep c72ssS commit:$H plain && INS=1 bash $IS/batch.sh run c72ssS 10 11
