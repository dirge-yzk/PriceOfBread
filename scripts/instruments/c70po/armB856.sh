#!/usr/bin/env bash
# (856) c70po 腕 B の駆動（読むだけでない＝計器つき 10 種を回す）：POB_CEARN=0（宣言：cearn_s・cgrp_s・cpop_s を書かない・門と止める線はそれらを読まない）。
#   bash armB856.sh <コミット>   出力 D:/PoB_work/c70po_work/out_c70poB_10（ins は計器の生）
S=D:/PoB_work/wt_c70po/scripts/instruments
export POB_WORK=D:/PoB_work/c70po_work POB_REPO=D:/PoB_work/wt_c70po POB_CEARN=0
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
H=${1:?コミット}
echo "c70poB start $(date '+%F %T') HEAD=${H:0:8} POB_CEARN=$POB_CEARN" >> $POB_WORK/arms_start.txt
bash $S/batch.sh prep c70poB commit:$H plain || exit 1
INS=1 bash $S/batch.sh run c70poB 10
RC=$?
echo "c70poB end $(date '+%F %T') rc=$RC" >> $POB_WORK/arms_start.txt
exit $RC
