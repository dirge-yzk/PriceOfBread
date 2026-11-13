#!/usr/bin/env bash
# (1119) c108mc 腕 B（旗 MineralScaleCapLift 既定 1 の木・計器つき 10 種・種 1〜10）。
#   bash scripts/instruments/c108mc/armB.sh <コミット>   ＝ POB_WORK=D:/PoB_work/c108mc_work/armB・出力 out_c108mcB/ins・成績表 b10_c108mcB.txt
export TEMP=D:/PoB_work/c108mc_work/tmp TMP=D:/PoB_work/c108mc_work/tmp
export POB_WORK=D:/PoB_work/c108mc_work/armB
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
H=${1:?コミット}; IS="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$POB_WORK"
bash "$IS/batch.sh" prep c108mcB "commit:$H" plain || exit 1
INS=1 bash "$IS/batch.sh" run c108mcB 10
