#!/usr/bin/env bash
# (1383) 合流の読み手：挙動の線 c146wy・c146bk の門と止める線と記録を合わせた木の腕（M・S11・ヌル腕）で読む（c146wy/read_wy.sh・c146bk/read_bk.sh の和・m1362/read_m1362.sh の写し）。
#   使い方: bash read_m1383.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <基準の成績表（measurements/ の名）> <基準の生（D:/ の形）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; BCARD=${3:?基準の成績表}; BASE=${4:?基準の生}; TAG=${5:?タグ}; OUT=${6:?出力}; SB=${7:?種の起点}
K=${K:-D:/PoB_work/keep_1362}   # popjump と home_break の N1
NGB=${NGB:-ng1362_20261125:m1362}
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c78gz/stoplines906.sh" "$INS" "$OUT/stop906.txt" "$SB" > "$OUT/stop906.log" 2>&1; echo "stop906 rc=$?"
bash "$S/c139sl/stoplines1330.sh" "$INS" "$OUT/stop1330.txt" "$SB" > "$OUT/stop1330.log" 2>&1; echo "stop1330 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "holbase rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/$BCARD" "$R/measurements/b10_20261125_m1362_n1_ins" "$R/measurements/b10_20261125_m1362_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$R/measurements/$BCARD") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/$NGB > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
# 線 wy の門 G1（milWarMean≠0 の行 0）と記録
python -X utf8 -I "$S/c146wy/paper_wy.py" "$INS" "$SB" 10 > "$OUT/paper_wy.txt" 2>&1; echo "paperwy rc=$?"
# 線 bk の記録（倒産の数の新÷旧）
python -X utf8 -I "$S/c146bk/bk_paper.py" "$INS" "$BASE" > "$OUT/paper_bk.txt" 2>&1; echo "paperbk rc=$?"
python -X utf8 -I "$S/c142lr/bham.py" "$INS" > "$OUT/bham.txt" 2>&1; echo "bham rc=$?"
python -X utf8 -I "$S/c142lr/bham.py" "$INS" セントポール > "$OUT/stpaul.txt" 2>&1; echo "stpaul rc=$?"
echo READDONE
