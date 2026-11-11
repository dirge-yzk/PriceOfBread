#!/usr/bin/env bash
# (832) 腕の計器つき 10 種の生を止める線と記録の読み手で読む（読むだけ・(818) の stoplines818.sh と同じ並び＋種の範囲）。
#   使い方: bash stoplines832.sh <ins フォルダ（D:/ の形）> <出力ファイル> [種の起点 1|11]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m832; S=scripts/instruments
M=${1:?ins}; OUT=${2:?出力}; SB=${3:-1}; SE=$((SB+9))
{
echo "# (832) 止める線の読み手（$M・種 $SB〜$SE）"
echo "== ukdefault"; python -X utf8 -I $S/c10cp/ukdefault.py $M $SB 10
echo "== pxjump"; python -X utf8 -I $S/c11fl/pxjump.py M=$M
echo "== gdpjump"; python -X utf8 -I $S/m797/gdp_jump797.py M=$M
echo "== gdp_zero10"; python -X utf8 -I $S/m808/gdp_zero10.py M=$M
echo "== plvl"; python -X utf8 -I $S/brk693/plvl_seeds.py $M
echo "== hol"; python -X utf8 $S/hol/an_h2.py $M -v5   # -v5＝全ての原因の事象を 1 件ずつ（件数は -v4 と同じ・振り分けの表 holclass832.py が読む）
} > $OUT 2>&1
echo STOPDONE
