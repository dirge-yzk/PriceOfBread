#!/usr/bin/env bash
# (1247) 合流の読み手（c126pf/read_pf.sh の写し・種の起点 11 なら基線の生は keep_1233/a11・ヌル腕は keep_1233/n1・NG の基準は ng1233・nullgate は引数が 4 つ要る＝ヌル腕は N1 の 1 本しか無いので N1 を 2 回渡す〔幅は N1 だけ・記録〕）。
# (1244) c126pf 腕 B の読み手（c124bk/read_bk.sh の写し・基線の生は keep_1233/base_ins・ヌル腕は keep_1233/n1・NG の基準は ng1233）。
#   門＝止める線（stoplines1002・plvl_seeds・popjump1017・an_h2 -v4・.cost）＋史実の桁の線 H1〜H5（magn_pf2.py）。H1（人口）は記録。他は記録。
#   使い方: bash read_bk.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; SE=$((SB+9))
K=D:/PoB_work/keep_1233; BASE=D:/PoB_work/keep_1233/base_ins
[ "$SB" = 11 ] && BASE=D:/PoB_work/keep_1233/a11
mkdir -p "$OUT"
cd "$R"
python -X utf8 -I "$S/c123k3/histpop_k3.py" "$INS" $SB $SE > "$OUT/h1.txt" 2>&1; echo "h1 rc=$?"
python -X utf8 -I "$S/c123k3/histpop_k3.py" "$BASE" $SB $SE > "$OUT/h1_base.txt" 2>&1; echo "h1 base rc=$?"
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$BASE" "$OUT/stop1002_base.txt" "$SB" > "$OUT/stop1002_base.log" 2>&1; echo "stop1002 base rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$BASE" -v4 > "$OUT/hol_v4_base.txt" 2>&1; echo "hol base rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$BASE" > "$OUT/plvl_base.txt" 2>&1; echo "plvl base rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$K/n1" > "$OUT/plvl_n1.txt" 2>&1; echo "plvl n1 rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$BASE" "$SB" > "$OUT/ukdef_base.txt" 2>&1; echo "ukdef base rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261119_m1233_ins" "$R/measurements/b10_20261119_m1233_n1_ins" "$R/measurements/b10_20261119_m1233_n1_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
  B10=$R/measurements/b10_20261119_m1233_ins; [ "$SB" = 11 ] && B10=$R/measurements/b10s11_20261119_m1233_ins
  diff <(grep -v SOURCE-HASH "$CARD") <(grep -v SOURCE-HASH "$B10") | grep -c '^[<>]' > "$OUT/card_difflines.txt"
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1233_20261119:m1233 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
python -X utf8 -I "$S/c126pf/magn_pf2.py" "$INS" "$BASE" $SB > "$OUT/magn.txt" 2>&1; echo "magn rc=$?"
python -X utf8 -I "$S/c126pf/capskip_sum.py" "$INS" $SB $SE > "$OUT/capskip.txt" 2>&1; echo "capskip rc=$?"
echo READDONE

