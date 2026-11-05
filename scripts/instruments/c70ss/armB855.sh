#!/usr/bin/env bash
# (855) c70ss 腕 B（計器つき 10 種・種 1〜10・POB_CEARN=0＝cearn_s・cgrp_s・cpop_s を書かない宣言・門と止める線はその表を読まない）
#   bash armB855.sh <旗 1 のコミット>   ⚠起こす前に arms.sh free と D: の空き（30GB 以上）を呼び手が確かめる
H=${1:?コミット}
export POB_WORK=D:/PoB_work/c70ss_work/armB POB_REPO=D:/PoB_work/wt_c70ss MEASURE_CACHE=0 POB_CEARN=0
mkdir -p /d/PoB_work/c70ss_work/armB
IS=/d/PoB_work/wt_c70ss/scripts/instruments
bash $IS/batch.sh prep c70ssB commit:$H plain && INS=1 bash $IS/batch.sh run c70ssB 10
