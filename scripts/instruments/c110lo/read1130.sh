#!/usr/bin/env bash
# (1130) c110lo 腕 B の読み手（読むだけ・c108mc/read1119.sh の写し・基準を m1122 の成績表と keep_1129/base_ins〔INSRAW〕・ヌル腕 keep_1122/n1・n2 へ・門を gates_lo.py へ）
#   使い方: bash read1130.sh <腕の作業フォルダ（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）>（種 1〜10 だけ）
#   止める線の 100 倍と 1e10 倍は、腕・基準・ヌル腕を同じ台本（stoplines906.sh）で数え直して並べる（⑰）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
W=${1:?作業}; TAG=${2:?タグ}; OUT=${3:?出力}; SB=1
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt; K=D:/PoB_work/keep_1122; BASE=D:/PoB_work/keep_1129/base_ins; B0=D:/PoB_work/c110lo_work/base0
mkdir -p "$OUT"
python -X utf8 -I "$D0/gates_lo.py" "$INS" "$B0" 1,2,3,4,5,6,7,8,9,10 > "$OUT/gates_B.txt" 2>&1; echo "gates rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
for a in arm:$INS base:$BASE n1:$K/n1 n2:$K/n2; do n=${a%%:*}; p=${a#*:}; bash "$S/c78gz/stoplines906.sh" "$p" "$OUT/stop906_$n.txt" "$SB" > "$OUT/stop906_$n.log" 2>&1; echo "stop906 $n rc=$?"; done
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261114_m1122_ins" "$R/measurements/b10_20261114_m1122_n1_ins" "$R/measurements/b10_20261114_m1122_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1122_20261114:m1122 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
