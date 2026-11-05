#!/usr/bin/env bash
# (862) c71po（(856) の stoplines856.sh の写し・木と作業フォルダだけ c71po）←(856) c70po（(850) の stoplines850.sh の写し）← (850) c69po（(839) の stoplines839.sh の写し） # (839) 腕の計器つき 10 種の生を止める線と記録の読み手で読む（読むだけ・(832) の stoplines832.sh と同じ並び＋〈100 倍〉の 3 区分〔(833) c67sl の gdp_jump833.py〕）。
#   使い方: bash stoplines850.sh <ins フォルダ（D:/ の形）> <出力ファイル> [種の起点 1|11]
#   100 倍は旧の台本 gdp_jump797.py（合計の続き）と新の台本 gdp_jump833.py（建国の直後・分母の痩せ・それ以外）を両方出す。新の台本の前に両側の門 fixgate.py を当てる（緑が前提）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c71po; S=scripts/instruments; W=D:/PoB_work/c71po_work
M=${1:?ins}; OUT=${2:?出力}; SB=${3:-1}; SE=$((SB+9))
FX=$OUT.fixrows.txt
{
echo "# (862) 止める線の読み手（$M・種 $SB〜$SE）"
echo "== ukdefault"; python -X utf8 -I $S/c10cp/ukdefault.py $M $SB 10
echo "== pxjump"; python -X utf8 -I $S/c11fl/pxjump.py M=$M
echo "== gdpjump（旧の台本 gdp_jump797）"; python -X utf8 -I $S/m797/gdp_jump797.py M=$M
python -X utf8 -I $S/r833/gdp_jump833.py --rows FIX=$S/r833/fixture > $FX
echo "== gdpjump833 の両側の門（fixgate・緑が前提）"; python -X utf8 -I $S/r833/fixgate.py $FX
echo "== gdpjump833（3 区分・行つき）"; python -X utf8 -I $S/r833/gdp_jump833.py --rows M=$M
echo "== gdp_zero10"; python -X utf8 -I $S/m808/gdp_zero10.py M=$M
echo "== plvl"; python -X utf8 -I $S/brk693/plvl_seeds.py $M
echo "== hol"; python -X utf8 $S/hol/an_h2.py $M -v5   # -v5＝全ての原因の事象を 1 件ずつ（件数は -v4 と同じ・振り分けの表 holclass839.py が読む）
} > $OUT 2>&1
rm -f $FX
echo STOPDONE
