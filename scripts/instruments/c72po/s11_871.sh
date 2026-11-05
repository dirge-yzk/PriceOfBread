#!/usr/bin/env bash
# (871) c72po 種 11〜20 の腕 S11（c72poS）の駆動（(862) の armB862.sh の写し・木と作業フォルダを c72po にし、種の起点を 11 にした）。
#   POB_CEARN=0（宣言：cearn_s・cgrp_s・cpop_s を書かない・門と止める線はそれらの表を読まない）。
#   bash s11_871.sh <点火のコミット>   出力 D:/PoB_work/c72po_work/out_c72poS_10_s11（ins は計器の生）
#   腕の成否は batch.sh の終了コード（0＝完走・2＝計器の国が空・3＝ARMFULL・4＝BATCHFAIL）と成績表の有無の両方で読む。
S=D:/PoB_work/wt_c72po/scripts/instruments
export POB_WORK=D:/PoB_work/c72po_work POB_REPO=D:/PoB_work/wt_c72po POB_CEARN=0
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
H=${1:?コミット}
echo "c72poS start $(date '+%F %T') HEAD=${H:0:8} POB_CEARN=$POB_CEARN" >> $POB_WORK/arms_start.txt
bash $S/batch.sh prep c72poS commit:$H plain || exit 4
INS=1 bash $S/batch.sh run c72poS 10 11
RC=$?
echo "c72poS end $(date '+%F %T') rc=$RC" >> $POB_WORK/arms_start.txt
exit $RC
