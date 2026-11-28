#!/usr/bin/env bash
# (1432) 合流の読み手：挙動の線 c153rg・c153nx の門と止める線と記録を合わせた木の腕（M・S11・ヌル腕）で読む（m1425/read_m1425.sh の写し・門を rg・nx へ・N1・N2 は keep_1425/n1・n2）。
#   使い方: bash read_m1425.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <基準の成績表（measurements/ の名）> <基準の生（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
#   止める線の 100 倍は腕・基準・ヌル腕を同じ台本で数え直す（⑰）＝STOPALL=1 のとき基準と N1・N2 も数える。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; BCARD=${3:?基準の成績表}; BASE=${4:?基準の生}; TAG=${5:?タグ}; OUT=${6:?出力}; SB=${7:?種の起点}
K=${K:-D:/PoB_work/keep_1425}; K2=${K2:-D:/PoB_work/keep_1425}   # N1＝keep_1425/n1・N2＝keep_1425/n2
NGB=${NGB:-ng1425_20261128:m1425}
N1C=${N1C:-b10_20261128_m1425_n1_ins}; N2C=${N2C:-b10_20261128_m1425_n2_ins}
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K2/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c139sl/stoplines1330.sh" "$INS" "$OUT/stop1330.txt" "$SB" > "$OUT/stop1330.log" 2>&1; echo "stop1330 rc=$?"
if [ "${STOPALL:-0}" = 1 ]; then for x in base:$BASE n1:$K/n1 n2:$K2/n2; do n=${x%%:*}; d=${x#*:}
  bash "$S/c78gz/stoplines906.sh" "$d" "$OUT/stop906_$n.txt" "$SB" > "$OUT/stop906_$n.log" 2>&1; echo "stop906 $n rc=$?"
  bash "$S/c139sl/stoplines1330.sh" "$d" "$OUT/stop1330_$n.txt" "$SB" > "$OUT/stop1330_$n.log" 2>&1; echo "stop1330 $n rc=$?"
done; fi
python -X utf8 -I $S/c11fl/pxjump.py arm=$INS base=$BASE n1=$K/n1 n2=$K2/n2 > "$OUT/pxjump.txt" 2>&1; echo "pxjump rc=$?"
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
# 線 rg の門 G1・G2（事前登録 m1428_20261128_prereg_rg.txt）・線 nx の門 G1・G2（m1429_20261128_prereg_nx.txt）と、進行不能の記録（戦争の数・続いた戦争）
SB=$SB python -X utf8 -I "$S/m1432/rg_gate_r.py" arm=$INS base=$BASE n1=$K/n1 n2=$K2/n2 > "$OUT/gate_rg.txt" 2>&1; echo "rg gate rc=$?"
SB=$SB python -X utf8 -I "$S/m1432/nx_gate_r.py" "$INS" 10 > "$OUT/gate_nx.txt" 2>&1; echo "nx gate rc=$?"
SB=$SB python -X utf8 -I "$S/m1425/lk_gate_r.py" "$INS" 10 --wars-only > "$OUT/wars.txt" 2>&1; echo "wars rc=$?"
SB=$SB python -X utf8 -I "$S/m1425/lk_gate_r.py" "$BASE" 10 --wars-only > "$OUT/wars_base.txt" 2>&1; echo "wars base rc=$?"
python -X utf8 -I $S/r1424/q2_rematch_fix.py $INS $SB-$((SB+9)) > "$OUT/rematch.txt" 2>&1; echo "rematch rc=$?"
echo READDONE
