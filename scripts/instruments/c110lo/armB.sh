#!/usr/bin/env bash
# (1130) c110lo 腕 B（旗 RailLocoNeedsDiscovery 既定 1 の木・計器つき 10 種・種 1〜10）。c108mc/armB.sh の写し。
#   bash scripts/instruments/c110lo/armB.sh <コミット>   ＝ POB_WORK=D:/PoB_work/c110lo_work/armB・出力 out_c110loB_10/ins・成績表 b10_c110loB_10.txt
export TEMP=D:/PoB_work/c110lo_work/tmp TMP=D:/PoB_work/c110lo_work/tmp
export POB_WORK=D:/PoB_work/c110lo_work/armB
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
H=${1:?コミット}; IS="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$POB_WORK"
bash "$IS/batch.sh" prep c110loB "commit:$H" plain || exit 1
INS=1 bash "$IS/batch.sh" run c110loB 10
