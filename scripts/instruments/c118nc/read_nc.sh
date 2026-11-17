#!/usr/bin/env bash
# (1186) c118nc 腕 B の読み手（c116hd/read_hd.sh の型・基線とヌル腕と NG の基準は m1184〔keep_1184/base_ins・n1・n2・ng1184〕）。
#   門＝止める線（stoplines1002・plvl_seeds・popjump1017・an_h2 -v4・warcount の 10 倍・.cost）＋史実の桁 H（magn.py）。他は記録。
#   使い方: bash read_nc.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}
K=D:/PoB_work/keep_1184; BASE=$K/base_ins
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c118nc/warcount.py" M=$BASE N1=$K/n1 N2=$K/n2 B=$INS > "$OUT/warcount.txt" 2>&1; echo "warcount rc=$?"
python -X utf8 -I "$S/c118nc/floor_count.py" "$INS" > "$OUT/floor_B.txt" 2>&1; echo "floor rc=$?"
python -X utf8 -I "$S/c118nc/secfac.py" "$INS" > "$OUT/secfac_B.txt" 2>&1; echo "secfac rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
for n in n1 n2; do python -X utf8 -I "$S/brk693/plvl_seeds.py" "$K/$n" > "$OUT/plvl_$n.txt" 2>&1; echo "plvl $n rc=$?"; done
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261116_m1184_ins" "$R/measurements/b10_20261116_m1184_n1_ins" "$R/measurements/b10_20261116_m1184_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1184_20261116:m1184 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c118nc/magn.py" "$R/measurements/ng1184_20261116/mil180_m1184.txt" "$OUT/ng/mil180_$TAG.txt" > "$OUT/magn_H.txt" 2>&1; echo "magn rc=$?"
echo READDONE
