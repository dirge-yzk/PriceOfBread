#!/usr/bin/env bash
# (813) 合わせた木の計器つき 10 種の生を止める線と記録の読み手で読む（読むだけ・(808) の stoplines_M と同じ並び）。
#   使い方: bash stoplines813.sh <ins フォルダ（D:/ の形）> <出力ファイル>
#   ヌル腕と種 11〜20（keep_797/n1・n2・a11）は (808) で同じ台本で数え直した（measurements/m808_20261103/stoplines_null.txt）＝生が同じなので数え直さない。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd C:/Users/dirge/Documents/PriceOfBread; S=scripts/instruments
M=${1:?ins}; OUT=${2:?出力}
{
echo "# (813) 合わせた木の ident m813 の計器つき 10 種の生（$M）を止める線の読み手で読む（記録・基線 m797 と同じ軌道の確かめ）"
echo "== ukdefault"; python -X utf8 -I $S/c10cp/ukdefault.py $M 1 10
echo "== pxjump"; python -X utf8 -I $S/c11fl/pxjump.py M=$M
echo "== gdpjump"; python -X utf8 -I $S/m797/gdp_jump797.py M=$M
echo "== gdp_zero10"; python -X utf8 -I $S/m808/gdp_zero10.py M=$M
echo "== plvl"; python -X utf8 -I $S/brk693/plvl_seeds.py $M
echo "== hol"; python -X utf8 $S/hol/an_h2.py $M -v4
} > $OUT 2>&1
echo STOPDONE
