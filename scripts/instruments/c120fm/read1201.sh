#!/usr/bin/env bash
# (1201) c120fm の腕の読み手（m1198/read1198.sh の型・基準は keep_1198/base_ins・ヌル腕は keep_1198/n1）。
#   bash read1201.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）>
#   種 1〜10 だけ（種 11〜20 は線では回さない）。門は事前登録 measurements/m1201_20261118_prereg_fm.txt §3、狙いの量と記録は §4。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}
K=D:/PoB_work/keep_1198; BASE=$K/base_ins; BC=$R/measurements/b10_20261118_m1198_ins
mkdir -p "$OUT"
cd "$R"
# 狙いの量（記録）：formation_s の集計・今の規則の組み直し（腕の生の rule_s・dep_s から）・統一の宣戦
python -X utf8 -I "$S/c120fm/formsum.py" "$INS" 1 10 > "$OUT/formsum.txt" 2>&1; echo "formsum rc=$?"
python -X utf8 -I "$S/c120fm/paper_fm.py" "$INS" 1 10 "$R/PriceOfBread.SimCore/world.json" "$OUT/paper_old" old 0.25 > "$OUT/paper_old_out.txt" 2>&1; echo "paper rc=$?"
python -X utf8 -I "$S/c118ur/ur.py" paper "$INS" 1 10 "$OUT/paper_ur.json" > "$OUT/paper_ur.txt" 2>&1; echo "ur paper rc=$?"
python -X utf8 -I "$S/c118ur/ur.py" paper "$BASE" 1 10 "$OUT/paper_ur_base.json" > "$OUT/paper_ur_base.txt" 2>&1; echo "ur paper base rc=$?"
python -X utf8 -I "$S/c118ur/ur.py" gates "$OUT/paper_ur.json" "$OUT/paper_ur_base.json" > "$OUT/gates_ur.txt" 2>&1; echo "ur gates rc=$?"
python -X utf8 -I "$S/c118nc/warcount.py" M=$BASE N1=$K/n1 B=$INS --seeds=1-10 > "$OUT/warcount.txt" 2>&1; echo "warcount rc=$?"
# 止める線（基準と腕を同じ台本で・N1 は measurements/m1198_20261118/stopbase/stop1002_N1.txt）
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" 1 > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" 1 > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
# 記録
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" 1 > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" 1 > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$BC" "$R/measurements/b10_20261118_m1198_n1_ins" "$R/measurements/b10_20261118_m1198_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1198_20261118:m1198 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c120fm/samebd.py" "$INS" "$BASE" 1 10 > "$OUT/samebd.txt" 2>&1; echo "samebd rc=$?"
echo READDONE
