#!/usr/bin/env bash
# (1122) 合流の読み手（m1103/read1103.sh の写し・門を ps の gate_ps.py と mc の gates.py〔どちらも変えずに〕へ・記録に v2 の 2 つ〔rec1122.py〕・ヌル腕と NG の基準は m1103〔keep_1103/n1・n2・ng1103〕）
# (1103) 合流の読み手（m1087/read1087.sh の写し・門を rr の gates_rr1103.py〔gates_rr.py の写し・種の範囲だけ〕と gp の gate1099.py〔変えずに〕へ・
#   記録に gp の反証役 v2 の最低賃金法の施行の行（mw1103.py）を足した・ヌル腕と NG の基準は m1087〔keep_1087/n1・n2・ng1087〕）
#   使い方: bash read1103.sh <ins のフォルダ（D:/ の形）> <成績表のファイル（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <種の起点> <基準の生（D:/ の形）> [門 0|1]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; BASE=${6:?基準}; GATE=${7:-1}
SE=$((SB+9)); K=D:/PoB_work/keep_1103
mkdir -p "$OUT"
if [ "$GATE" = 1 ]; then
  # 種 11〜20 は旗 0 の parlseat_s が無い＝G4 を読まない。gate_ps.py は -- の無い引数を旗 0 の生と読む＝種の範囲を渡さない（生には種 11〜20 だけがある・(1122) で 1 回空振りした）。
  if [ "$SB" = 1 ]; then python -X utf8 -I "$S/c108ps/gate_ps.py" "$INS" "D:/PoB_work/c108ps_work/flag0_ins" --seeds $SB-$SE > "$OUT/gate_ps.txt" 2>&1
  else python -X utf8 -I "$S/c108ps/gate_ps.py" "$INS" > "$OUT/gate_ps.txt" 2>&1; fi; echo "gate_ps rc=$?"
  python -X utf8 -I "$S/c108mc/gates.py" "$INS" "$TAG" $SB > "$OUT/gate_mc.txt" 2>&1; echo "gate_mc rc=$?"
fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261113_m1103_ins" "$R/measurements/b10_20261113_m1103_n1_ins" "$R/measurements/b10_20261113_m1103_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1103_20261113:m1103 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/m1122/rec1122.py" $SB-$SE arm="$INS" base="$BASE" > "$OUT/rec1122.txt" 2>&1; echo "rec rc=$?"
[ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
