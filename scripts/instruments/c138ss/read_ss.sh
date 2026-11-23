#!/usr/bin/env bash
# (1325) c138ss 腕 B の読み手（m1321/read1321.sh の写し）：止める線と記録の並びは (1321) と同じ・基準は m1321（INSRAW keep_1321/base_ins・KEEPNULL keep_1321/n1・a11）・NG の基準 ng1321_20261123。
#   基準は種 1〜10 が keep_1321/base_ins（INSRAW）・種 11〜20 が keep_1321/a11・ヌル腕は keep_1321/n1（KEEPNULL）。成績表の差は m1321 の M・S11 と数える。
#   使い方: bash read1321.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> [nong]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9)); NONG=${6:-}
K=D:/PoB_work/keep_1321; BASE=$K/base_ins; NUL=$K/n1; CB=$R/measurements/b10_20261123_m1321_ins
[ -n "${BASE_OVR:-}" ] && BASE=$BASE_OVR; [ -n "${NUL_OVR:-}" ] && NUL=$NUL_OVR
[ "$SB" = 11 ] && BASE=$K/a11 && CB=$R/measurements/b10s11_20261123_m1321_ins
mkdir -p "$OUT"
cd "$R"
# 止める線（基準とヌル腕も同じ台本で＝⑰）
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$NUL > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c78gz/stoplines906.sh" "$BASE" "$OUT/stop906_base.txt" "$SB" > "$OUT/stop906_base.log" 2>&1; echo "stop906 base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m1247/holcase.py" "$OUT/hol_v4.txt" "$BASE" "$NUL" > "$OUT/holcase.txt" 2>&1; echo "holcase rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I $S/m797/gdp_jump797.py arm=$INS base=$BASE n1=$NUL > "$OUT/gdpjump.txt" 2>&1; echo "gdpjump rc=$?"
# 線の門 G1 と史実の桁の線 H1（κ_E・記録）・H2（1913 年の農業の外の割合・門）
python -X utf8 -I "$S/c138ss/ss_gate.py" "$INS" $SB 10 > "$OUT/ss_gate.txt" 2>&1; echo "ss_gate rc=$?"
# 記録
python -X utf8 -I "$S/c132df/arrears_sum.py" "$INS" $SB $SE > "$OUT/arrears.txt" 2>&1; echo "arrears rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$NUL --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
python -X utf8 -I "$S/c127bd/metal100.py" "$INS" $SB $SE > "$OUT/metal.txt" 2>&1; echo "metal rc=$?"
python -X utf8 -I "$S/m1254/dec_after.py" "$INS" $SB $SE > "$OUT/dec_after.txt" 2>&1; echo "dec_after rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261123_m1321_ins" "$R/measurements/b10_20261123_m1321_n1_ins" "$R/measurements/b10_20261123_m1321_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$CB") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
[ -z "$NONG" ] && { bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1321_20261123:m1321 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"; }
echo READDONE
