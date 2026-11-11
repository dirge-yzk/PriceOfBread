#!/usr/bin/env bash
# (871) c72po（(866) の stoplines866.sh の写し・木を wt_c72po にしただけ・台本の並びは同じ）＝止める線を読む（読むだけ）。
#   使い方: bash stoplines871.sh <ins フォルダ（D:/ の形）> <出力ファイル> [種の起点 1|11]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c72po; S=scripts/instruments
M=${1:?ins}; OUT=${2:?出力}; SB=${3:-1}; SE=$((SB+9))
FX=$OUT.fixrows.txt
{
echo "# (871) 止める線の読み手（$M・種 $SB〜$SE）"
echo "== ukdefault"; python -X utf8 -I $S/c10cp/ukdefault.py $M $SB 10
echo "== pxjump"; python -X utf8 -I $S/c11fl/pxjump.py M=$M
echo "== gdpjump（旧の台本 gdp_jump797）"; python -X utf8 -I $S/m797/gdp_jump797.py M=$M
python -X utf8 -I $S/r833/gdp_jump833.py --rows FIX=$S/r833/fixture > $FX
echo "== gdpjump833 の両側の門（fixgate・緑が前提）"; python -X utf8 -I $S/r833/fixgate.py $FX
echo "== gdpjump833（3 区分・行つき）"; python -X utf8 -I $S/r833/gdp_jump833.py --rows M=$M
echo "== gdp_zero10"; python -X utf8 -I $S/m808/gdp_zero10.py M=$M
echo "== plvl"; python -X utf8 -I $S/brk693/plvl_seeds.py $M
echo "== hol"; python -X utf8 $S/hol/an_h2.py $M -v5
} > $OUT 2>&1
rm -f $FX
echo STOPDONE
