#!/usr/bin/env bash
# (877) c73ts 腕 S11（armB877.sh の形・種の起点だけ 11）
#   bash s11_877.sh <旗 1 のコミット>   ⚠起こす前に arms.sh free を呼び手が確かめる
#   出力：D:/PoB_work/c73ts_work/s11/out_c73tsS_10_s11/ins・成績表 D:/PoB_work/c73ts_work/s11/b10_c73tsS_10_s11.txt
H=${1:?コミット}
F=$(python -c "import shutil;print(int(shutil.disk_usage('D:/').free/1e9))")
[ "$F" -ge 30 ] || { echo "D: の空き ${F}GB＜床 30GB"; exit 9; }
echo "D: の空き ${F}GB（起こす時）"
export POB_WORK=D:/PoB_work/c73ts_work/s11 POB_REPO=D:/PoB_work/wt_c73ts MEASURE_CACHE=0 POB_CEARN=0
mkdir -p /d/PoB_work/c73ts_work/s11
IS=/d/PoB_work/wt_c73ts/scripts/instruments
bash $IS/batch.sh prep c73tsS commit:$H plain && INS=1 bash $IS/batch.sh run c73tsS 10 11
