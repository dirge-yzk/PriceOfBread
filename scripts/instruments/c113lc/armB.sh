#!/usr/bin/env bash
# (1152) c113lc 腕 B（旗 RailLocoGateWorld 既定 1 の木・計器つき 10 種・種 1〜10）。c110lo/armB.sh の写し。
#   bash scripts/instruments/c113lc/armB.sh <コミット>   ＝ POB_WORK=D:/PoB_work/c113lc_work/armB・出力 out_c113lcB_10/ins・成績表 b10_c113lcB_10.txt
export TEMP=D:/PoB_work/c113lc_work/tmp TMP=D:/PoB_work/c113lc_work/tmp
export POB_WORK=D:/PoB_work/c113lc_work/armB
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
H=${1:?コミット}; IS="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$POB_WORK"
bash "$IS/batch.sh" prep c113lcB "commit:$H" plain || exit 1
INS=1 bash "$IS/batch.sh" run c113lcB 10
