#!/usr/bin/env bash
# (1195) c119us 腕 B の読み手（c118ur/read_ur.sh の型・基線とヌル腕は m1191〔keep_1191/base_ins・n1〕・NG の基準は ng1191）。
#   使い方: bash read_us.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}
SE=$((SB+9)); K=D:/PoB_work/keep_1191; BASE=$K/base_ins; [ "$SB" = 11 ] && BASE=$K/a11; P=$R/measurements/m1195_20261118
PM=$P/paper_M.json; [ "$SB" = 11 ] && PM=$P/paper_A11.json
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c119us/sha_cmp.py" "$INS" "$BASE" > "$OUT/sha_cmp.txt" 2>&1; echo "sha rc=$?"
python -X utf8 -I "$S/c118ur/ur.py" paper "$INS" "$SB" "$SE" "$OUT/paper_B.json" > "$OUT/paper_B.txt" 2>&1; echo "paper rc=$?"
python -X utf8 -I "$S/c118ur/ur.py" gates "$OUT/paper_B.json" "$PM" > "$OUT/gates_ur.txt" 2>&1; echo "gates rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261118_m1191_ins" "$R/measurements/b10_20261118_m1191_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  cmp <(grep -v "SOURCE-HASH" "$CARD") <(grep -v "SOURCE-HASH" "$R/measurements/b10_20261118_m1191_ins") > "$OUT/card_cmp.txt" 2>&1; echo "card_cmp rc=$?" | tee -a "$OUT/card_cmp.txt"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1191_20261118:m1191 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
