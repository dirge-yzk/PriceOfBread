#!/usr/bin/env bash
# (1144) c112t1 腕の読み手（c111tp/read_tp.sh の型・基準は INSRAW keep_1143/base_ins とヌル腕 keep_1143/n1・n2・成績表 m1143・NG ng1143）
#   使い方: bash read_t1.sh <ins のフォルダ（D:/ の形）> <成績表のファイル（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> [full|lite]
#   lite＝アブレーション（門と止める線の表だけ・ng と home_break を回さない）。止める線の 100 倍と 1e10 倍は基準とヌル腕も同じ台本で数え直す。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; MODE=${5:-full}
K=D:/PoB_work/keep_1143; BASE=$K/base_ins
mkdir -p "$OUT"
python -X utf8 -I "$D0/t1sum.py" arm=$INS 旗0=D:/PoB_work/c112t1_work/flag0_t1buy > "$OUT/gates_t1buy.txt" 2>&1; echo "t1sum rc=$?"
python -X utf8 -I "$D0/u1awb.py" 基線=$BASE 腕=$INS N2=$K/n2 > "$OUT/gates_w1.txt" 2>&1; echo "w1 rc=$?"
python -X utf8 -I "$D0/w2b.py" 基線=$BASE N1=$K/n1 N2=$K/n2 腕=$INS > "$OUT/gates_w2b.txt" 2>&1; echo "w2b rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" 1 > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
# 基準とヌル腕の stop1002 は measurements/m1143_20261115/stopbase/stop1002_M・_N1・_N2（同じ台本 c92gf/stoplines1002.sh）を並べて読む
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/m609/plvl_all.py" "$INS" 10 "$TAG" > "$OUT/plvl_all.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" 1 > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261115_m1143_ins" "$R/measurements/b10_20261115_m1143_n1_ins" "$R/measurements/b10_20261115_m1143_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
if [ "$MODE" = full ]; then
  python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
  bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1143_20261115:m1143 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
fi
[ -f "$CARD.cost" ] && grep -h "推定\|A-1\|着値の充填\|見た買い手行" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
