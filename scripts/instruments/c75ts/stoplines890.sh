#!/usr/bin/env bash
# (890) c75ts 止める線の読み手（c74ts/stoplines866.sh〔m866 の写し〕の並びに、段 0 で固定した〈100 倍〉の型の読み手 gdp_jump890 を足した版・読むだけ）。
#   使い方: bash stoplines890.sh <ins フォルダ（D:/ の形）> <出力ファイル> [種の起点 1|11]
#   旧の読み手（gdp_jump797・gdp_jump833）はそのまま並べる（合流が今の規則と段 0 の規則の両方を当てて並べる＝事前登録 [5]）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd C:/Users/dirge/Documents/PriceOfBread; S=scripts/instruments; C=D:/PoB_work/wt_c75ts/scripts/instruments/c75ts
M=${1:?ins}; OUT=${2:?出力}; SB=${3:-1}; SE=$((SB+9))
FX=$OUT.fixrows.txt
{
echo "# (890) 止める線の読み手（$M・種 $SB〜$SE）"
echo "== ukdefault"; python -X utf8 -I $S/c10cp/ukdefault.py $M $SB 10
echo "== pxjump"; python -X utf8 -I $S/c11fl/pxjump.py M=$M
echo "== gdpjump（旧の台本 gdp_jump797）"; python -X utf8 -I $S/m797/gdp_jump797.py M=$M
python -X utf8 -I $S/r833/gdp_jump833.py --rows FIX=$S/r833/fixture > $FX
echo "== gdpjump833 の両側の門（fixgate・緑が前提）"; python -X utf8 -I $S/r833/fixgate.py $FX
echo "== gdpjump833（3 区分・行つき）"; python -X utf8 -I $S/r833/gdp_jump833.py --rows M=$M
echo "== gdpjump890 の自己試験（緑が前提）"; python -X utf8 -I $C/gdp_jump890.py --selftest | tail -1
echo "== gdpjump890（段 0 の型・行つき）"; python -X utf8 -I $C/gdp_jump890.py --rows M=$M
echo "== gdpjump890（20 倍以上・型の分布の記録）"; python -X utf8 -I $C/gdp_jump890.py --rows --thr=20 M=$M
echo "== gdp_down875（下向き・記録）"; python -X utf8 -I $S/c73gt/gdp_down875.py M=$M
echo "== gdp_zero10"; python -X utf8 -I $S/m808/gdp_zero10.py M=$M
echo "== plvl"; python -X utf8 -I $S/brk693/plvl_seeds.py $M
echo "== hol"; python -X utf8 $S/hol/an_h2.py $M -v5
} > $OUT 2>&1
rm -f $FX
echo STOPDONE
