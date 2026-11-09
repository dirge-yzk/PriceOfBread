#!/usr/bin/env bash
# (997) c91sl 腕の読み手（読むだけ・read991.sh と m988/read988.sh の記録の節の写し）：門 gates997（事前登録 m997_20261109_prereg_sl.txt [5]）・
#   止める線（stoplines949 pair・NULLS は m988）・空洞化（an_h2 -v4）・記録（NG・nullgate・gdp_zero10・plvl・ukdef・home_break・.cost）・
#   生の比べ（rawcmp.py＝keep_994_rej の (991) の腕の生と表ごと）・need997（写しと実際の向きと大きさ）。
#   使い方: bash read997.sh <腕の作業フォルダ（D:/ の形）> <タグ（out_<タグ>・b10_<タグ>.txt）> <種の起点 1|11> <出力フォルダ（D:/ の形）> <比べる (991) の生（D:/ の形）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
W=${1:?作業}; TAG=${2:?タグ}; SB=${3:-1}; OUT=${4:?出力}; OLD=${5:?旧の生}
INS=$W/out_$TAG/ins; CARD=$W/b10_$TAG.txt
if [ "$SB" = 11 ]; then BASE=D:/PoB_work/keep_988/a11; else BASE=D:/PoB_work/keep_994/base_ins; fi
mkdir -p "$OUT"
python -X utf8 "$D0/gates997.py" "$INS" 10 "$SB" > "$OUT/gates997.txt" 2>&1; echo "gates rc=$?"
python -X utf8 "$D0/need997.py" "$INS" 10 "$SB" > "$OUT/need997.txt" 2>&1; echo "need rc=$?"
RULE=pair NULLS="$R/measurements/m988_20261109/null/stop906_n1.txt $R/measurements/m988_20261109/null/stop906_n2.txt" bash "$S/c84gz/stoplines949.sh" "$INS" "$OUT/stop949.txt" "$SB" > "$OUT/stop949.log" 2>&1; echo "stop949 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m764/rawcmp.py" "$INS" "$OLD" > "$OUT/rawcmp.txt" 2>&1; echo "rawcmp rc=$?"
if [ "$SB" = "1" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261109_m988_ins" "$R/measurements/b10_20261109_m988_n1_ins" "$R/measurements/b10_20261109_m988_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=D:/PoB_work/keep_988/n1 n2=D:/PoB_work/keep_988/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
  bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng988_20261109:m988 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
fi
grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
echo READDONE
