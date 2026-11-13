#!/usr/bin/env bash
# (1099) c105gp 腕 B の読み手（読むだけ・c103cr/read1082.sh の写し・基準を m1087 の成績表と keep_1096/base_ins（INSRAW）・ヌル腕 keep_1087/n1・n2 へ・門を gate1099.py へ・記録 rec_*.py を足した）
#   使い方: bash read1099.sh <腕の作業フォルダ（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）>（種 1〜10 だけ）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
W=${1:?作業}; TAG=${2:?タグ}; OUT=${3:?出力}; SB=1
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt; K=D:/PoB_work/keep_1087; BASE=D:/PoB_work/keep_1096/base_ins
mkdir -p "$OUT"
python -X utf8 -I "$D0/gate1099.py" "$OUT/gate1099.txt" B=$INS:1-10 base=$BASE:1-10 n1=$K/n1:1-10 n2=$K/n2:1-10; echo "gate rc=$?"
for k in events elect2 pesplit; do python -X utf8 -I "$D0/rec_$k.py" "$OUT/rec_$k.txt" B=$INS:1-10 base=$BASE:1-10; echo "rec_$k rc=$?"; done
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261113_m1087_ins" "$R/measurements/b10_20261113_m1087_n1_ins" "$R/measurements/b10_20261113_m1087_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1087_20261113:m1087 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
