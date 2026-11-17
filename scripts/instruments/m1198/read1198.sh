#!/usr/bin/env bash
# (1198) 合流の読み手（m1191/read1191.sh の型・門は線の台本の中身＝c119nd/read_nd.sh と c119us/read_us.sh）。
#   bash read1198.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
#   基準：種 1〜10＝keep_1191/base_ins・種 11〜20＝keep_1191/a11（m1191 の種 11〜20）。ヌル腕は keep_1191/n1（記録）。
#   us の門の旗 0 の紙は種 1〜10 が measurements/m1195_20261118/paper_M.json・種 11〜20 が paper_A11.json（線が取った）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}
SE=$((SB+9)); K=D:/PoB_work/keep_1191; P=$R/measurements/m1195_20261118
if [ "$SB" = 1 ]; then BASE=$K/base_ins; PM=$P/paper_M.json; BC=$R/measurements/b10_20261118_m1191_ins; else BASE=$K/a11; PM=$P/paper_A11.json; BC=$R/measurements/b10s11_20261118_m1191_ins; fi
mkdir -p "$OUT"
cd "$R"
# nd の門（S3 戦争の数・H1・H2・A1）
python -X utf8 -I "$S/c118nc/warcount.py" M=$BASE N1=$K/n1 B=$INS --seeds=$SB-$SE > "$OUT/warcount.txt" 2>&1; echo "warcount rc=$?"
python -X utf8 -I "$S/c119nd/paper.py" "$BASE" $SB $SE "$INS" > "$OUT/paper_nd.txt" 2>&1; echo "paper nd rc=$?"
python -X utf8 -I "$S/c119nd/paper.py" "$INS" $SB $SE > "$OUT/paper_nd_self.txt" 2>&1; echo "paper nd self rc=$?"
python -X utf8 -I "$S/c119nd/paper.py" "$BASE" $SB $SE > "$OUT/paper_nd_base.txt" 2>&1; echo "paper nd base rc=$?"
# us の門（G1・G2）
python -X utf8 -I "$S/c118ur/ur.py" paper "$INS" "$SB" "$SE" "$OUT/paper_ur.json" > "$OUT/paper_ur.txt" 2>&1; echo "ur paper rc=$?"
python -X utf8 -I "$S/c118ur/ur.py" gates "$OUT/paper_ur.json" "$PM" > "$OUT/gates_ur.txt" 2>&1; echo "ur gates rc=$?"
# 止める線（基準と腕を同じ台本で）
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$BC" "$R/measurements/b10_20261118_m1191_n1_ins" "$R/measurements/b10_20261118_m1191_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1191_20261118:m1191 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
if [ "$SB" = 1 ]; then MB=$R/measurements/ng1191_20261118/mil180_m1191.txt
else bash "$S/ng.sh" "$BASE" a11 "$OUT/ng_base" rd:$R/measurements/ng1191_20261118:m1191 > "$OUT/ng_base.txt" 2>&1; MB=$OUT/ng_base/mil180_a11.txt; fi
python -X utf8 -I "$S/c118nc/magn.py" "$MB" "$OUT/ng/mil180_$TAG.txt" > "$OUT/magn_H.txt" 2>&1; echo "magn rc=$?"
echo READDONE
