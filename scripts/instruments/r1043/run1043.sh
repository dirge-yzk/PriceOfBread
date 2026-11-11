#!/usr/bin/env bash
# (1043) c98cp 設計の材料の読み（腕 0 本）：m1027 の盤面（keep_1035/base_ins）とヌル腕 n1・n2 の crownpc_s を数える。
# 使い方：bash run1043.sh <出力のフォルダ>
set -e
H=$(cd "$(dirname "$0")" && pwd)
O=${1:-.}
N="イギリス 清 フランス ロシア プロイセン 日本 オスマン オーストリア アメリカ"
python -X utf8 -I "$H/read1043.py" D:/PoB_work/keep_1035/base_ins $N > "$O/r1043_base.txt"
python -X utf8 -I "$H/read1043.py" D:/PoB_work/keep_1027/n1 $N > "$O/r1043_n1.txt"
python -X utf8 -I "$H/read1043.py" D:/PoB_work/keep_1027/n2 $N > "$O/r1043_n2.txt"
python -X utf8 -I "$H/cnt3a.py" D:/PoB_work/keep_1035/base_ins > "$O/cnt3a_base.txt"
python -X utf8 -I "$H/cntgrant.py" D:/PoB_work/keep_1035/base_ins > "$O/cntgrant_base.txt"
