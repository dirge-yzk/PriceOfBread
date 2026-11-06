#!/usr/bin/env bash
# (882) c74ts 腕 S11（armB882.sh の形・種の起点だけ 11）
#   bash s11_882.sh <旗 1 のコミット>   ⚠起こす前に arms.sh free と D: の空きを呼び手が確かめる
#   出力：D:/PoB_work/c74ts_work/s11/out_c74tsS_10_s11/ins・成績表 D:/PoB_work/c74ts_work/s11/b10_c74tsS_10_s11.txt
H=${1:?コミット}
F=$(python -c "import shutil;print(int(shutil.disk_usage('D:/').free/1e9))")
[ "$F" -ge 30 ] || { echo "D: の空き ${F}GB＜床 30GB"; exit 9; }
echo "D: の空き ${F}GB（起こす時）"
export POB_WORK=D:/PoB_work/c74ts_work/s11 POB_REPO=D:/PoB_work/wt_c74ts MEASURE_CACHE=0 POB_CEARN=0
mkdir -p /d/PoB_work/c74ts_work/s11
IS=/d/PoB_work/wt_c74ts/scripts/instruments
bash $IS/batch.sh prep c74tsS commit:$H plain && INS=1 bash $IS/batch.sh run c74tsS 10 11
