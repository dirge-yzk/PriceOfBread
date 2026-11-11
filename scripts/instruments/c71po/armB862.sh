#!/usr/bin/env bash
# (862) c71po 腕 B の駆動（読むだけでない＝計器つき 10 種を回す）：POB_CEARN=0（宣言：cearn_s・cgrp_s・cpop_s を書かない・門と止める線はそれらを読まない）。
#   bash armB862.sh <コミット>   出力 D:/PoB_work/c71po_work/out_c71poB_10（ins は計器の生）
S=D:/PoB_work/wt_c71po/scripts/instruments
export POB_WORK=D:/PoB_work/c71po_work POB_REPO=D:/PoB_work/wt_c71po POB_CEARN=0
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
H=${1:?コミット}
echo "c71poB start $(date '+%F %T') HEAD=${H:0:8} POB_CEARN=$POB_CEARN" >> $POB_WORK/arms_start.txt
bash $S/batch.sh prep c71poB commit:$H plain || exit 1
INS=1 bash $S/batch.sh run c71poB 10
RC=$?
echo "c71poB end $(date '+%F %T') rc=$RC" >> $POB_WORK/arms_start.txt
exit $RC
