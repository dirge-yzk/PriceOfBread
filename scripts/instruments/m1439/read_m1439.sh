#!/usr/bin/env bash
# (1439) 合流の読み手：挙動の線 c154sd・c154ha の門と止める線と記録を合わせた木の腕（M・S11・ヌル腕）で読む（m1432/read_m1432.sh の写し・門を sd・ha へ）。
#   使い方: bash read_m1439.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <基準の成績表（measurements/ の名）> <基準の生（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
#   止める線の 100 倍の基準とヌル腕（keep_1432 の base・a11・n1・n2）は (1432) の readM・readS11 の stop906_・stop1330_ の _base・_n1・_n2 を写す（同じ keep・数え直しを省く＝★(1432) の提案）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; BCARD=${3:?基準の成績表}; BASE=${4:?基準の生}; TAG=${5:?タグ}; OUT=${6:?出力}; SB=${7:?種の起点}
K=${K:-D:/PoB_work/keep_1432}
NGB=${NGB:-ng1432_20261128:m1432}
N1C=${N1C:-b10_20261128_m1432_n1_ins}; N2C=${N2C:-b10_20261128_m1432_n2_ins}
mkdir -p "$OUT"
cd "$R"
if [ "${COPYBASE:-1}" = 1 ]; then RD=$R/measurements/m1432_20261128/$([ "$SB" = 11 ] && echo readS11 || echo readM)
  for n in base n1 n2; do cp "$RD/stop906_$n.txt" "$OUT/stop906_$n.txt"; cp "$RD/stop1330_$n.txt" "$OUT/stop1330_$n.txt"; done; echo "stop base copied from $RD"; fi
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c139sl/stoplines1330.sh" "$INS" "$OUT/stop1330.txt" "$SB" > "$OUT/stop1330.log" 2>&1; echo "stop1330 rc=$?"
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
# 線 sd の門（事前登録 m1435_20261128_prereg_sd.txt：G1 R1・R2・R3・G3 下・O1・O2）・線 ha の門（m1436_20261128_prereg_ha.txt：種ごとの国内の移民の和÷基線 ≥0.1）
python -X utf8 -I $S/c154sd/g1_sd.py $INS 10 $SB > "$OUT/g1_sd.txt" 2>&1; echo "g1 rc=$?"
python -X utf8 -I $S/c154sd/sd_gate.py $INS 10 $SB > "$OUT/gate_sd.txt" 2>&1; echo "sd gate rc=$?"
[ "$SB" = 1 ] && { python -X utf8 -I $S/c154sd/dc_base.py $INS > "$OUT/dc.txt" 2>&1; echo "dc rc=$?"; }
SB=$SB python -X utf8 -I $S/m1439/ha_gate_r.py $INS $BASE 10 > "$OUT/gate_ha.txt" 2>&1; echo "ha gate rc=$?"
python -X utf8 -I $S/m1439/sbu_sum.py arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 SB=$SB > "$OUT/sbu.txt" 2>&1; echo "sbu rc=$?"
SB=$SB python -X utf8 -I "$S/m1425/lk_gate_r.py" "$INS" 10 --wars-only > "$OUT/wars.txt" 2>&1; echo "wars rc=$?"
SB=$SB python -X utf8 -I "$S/m1425/lk_gate_r.py" "$BASE" 10 --wars-only > "$OUT/wars_base.txt" 2>&1; echo "wars base rc=$?"
python -X utf8 -I $S/r1424/q2_rematch_fix.py $INS $SB-$((SB+9)) > "$OUT/rematch.txt" 2>&1; echo "rematch rc=$?"
echo READDONE
