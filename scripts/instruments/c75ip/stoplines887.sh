#!/usr/bin/env bash
# (887) c75ip：m866/stoplines866.sh の写し（計算と台本の並びは同じ・読むだけ）
# (866) 合流（(862) c71po の stoplines862.sh の写し・木を main の木、作業フォルダを m866_work にしただけ・台本の並びは同じ）＝★(797) 線の担当が書いた止める線の数を、合流が同じ台本で数え直す（読むだけ）。
#   使い方: bash stoplines866.sh <ins フォルダ（D:/ の形）> <出力ファイル> [種の起点 1|11]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd C:/Users/dirge/Documents/PriceOfBread; S=scripts/instruments
M=${1:?ins}; OUT=${2:?出力}; SB=${3:-1}; SE=$((SB+9))
FX=$OUT.fixrows.txt
{
echo "# (866) 止める線の読み手（$M・種 $SB〜$SE）"
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
