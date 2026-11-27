#!/usr/bin/env bash
# (1418) 合流の読み手：挙動の線 c151lk の門 G1′（と記録の G1）・c151fl の門 G1〜G3 と止める線と記録を合わせた木の腕（M・S11・ヌル腕）で読む（m1411/read_m1411.sh の写し・門を lk と fl へ）。
#   使い方: bash read_m1411.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <基準の成績表（measurements/ の名）> <基準の生（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
#   止める線の 100 倍は腕・基準・ヌル腕を同じ台本で数え直す（⑰）＝STOPALL=1 のとき基準と N1・N2 も数える。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; BCARD=${3:?基準の成績表}; BASE=${4:?基準の生}; TAG=${5:?タグ}; OUT=${6:?出力}; SB=${7:?種の起点}
K=${K:-D:/PoB_work/keep_1411}   # popjump と home_break の N1・N2（m1411 のヌル腕）
NGB=${NGB:-ng1411_20261127:m1411}
N1C=${N1C:-b10_20261127_m1411_n1_ins}; N2C=${N2C:-b10_20261127_m1411_n2_ins}
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c139sl/stoplines1330.sh" "$INS" "$OUT/stop1330.txt" "$SB" > "$OUT/stop1330.log" 2>&1; echo "stop1330 rc=$?"
if [ "${STOPALL:-0}" = 1 ]; then for x in base:$BASE n1:$K/n1 n2:$K/n2; do n=${x%%:*}; d=${x#*:}
  bash "$S/c78gz/stoplines906.sh" "$d" "$OUT/stop906_$n.txt" "$SB" > "$OUT/stop906_$n.log" 2>&1; echo "stop906 $n rc=$?"
  bash "$S/c139sl/stoplines1330.sh" "$d" "$OUT/stop1330_$n.txt" "$SB" > "$OUT/stop1330_$n.log" 2>&1; echo "stop1330 $n rc=$?"
done; fi
python -X utf8 -I $S/c11fl/pxjump.py arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 > "$OUT/pxjump.txt" 2>&1; echo "pxjump rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "holbase rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/$BCARD" "$R/measurements/$N1C" "$R/measurements/$N2C" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/$BCARD") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/$NGB > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
# 線 lk の門 G1′（事前登録の追記 48ca5ed9e＝許し 2e-9）と記録の G1（許し 1e-10）・進行不能の数（lk_gate の TOTAL・MAX）
SE=$((SB+9))
python -X utf8 -I "$S/m1418/lk_gate_g1p.py" "$INS" $SB-$SE > "$OUT/gate_lk_g1p.txt" 2>&1; echo "lk g1p rc=$?"
if [ "$SB" = 1 ]; then python -X utf8 -I "$S/c151lk/lk_gate.py" "$INS" > "$OUT/gate_lk_g1.txt" 2>&1; echo "lk g1 rc=$?"; fi
# 線 fl の門 G1〜G3（事前登録 m1416_20261127_prereg_fl.txt）と新÷旧
python -X utf8 -I "$S/c151fl/fl_gate.py" "$INS" $SB-$SE "$OUT/gate_fl.tsv" > "$OUT/gate_fl.txt" 2>&1; echo "fl gate rc=$?"
python -X utf8 -I "$S/c151fl/fl_gate.py" "$BASE" $SB-$SE "$OUT/gate_fl_base.tsv" > "$OUT/gate_fl_base.txt" 2>&1; echo "fl gate base rc=$?"
python -X utf8 -I "$S/c151fl/newold_fl.py" "$INS" "$BASE" $SB-$SE > "$OUT/newold_fl.txt" 2>&1; echo "fl newold rc=$?"
echo READDONE
