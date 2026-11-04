#!/usr/bin/env bash
# (821) c65md 腕 B の後の読み（読むだけ）：門（gate821.py・c10cp の gates.py）・止める線（m818 の stoplines818.sh と同じ読み手）・〈壊す〉①（home_break）・門 A′（nullgate）・NG（ng.sh）・.cost を並べる。
#   使い方: bash post821.sh（Bash の run_in_background で）。腕 B の生 D:/PoB_work/c65md_work/armB/out_c65mdB_10/ins。
#   基準の止める線の数え直し＝measurements/m818_20261104/stoplines_M.txt（keep_818/base_ins）・ヌル腕＝measurements/m808_20261103/stoplines_null.txt（keep_797/n1・n2 を同じ台本で数え直した物）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c65md; S=scripts/instruments; W=D:/PoB_work/c65md_work; O=measurements/m821_20261104
mkdir -p $O
B=$W/armB/out_c65mdB_10/ins
for x in "" .cost .manifest; do cp $W/armB/b10_c65mdB_10.txt$x measurements/b10_20261104_c65mdB_ins$x; done
python -X utf8 -I $S/c65md/gate821.py $B > $O/gates_B.txt 2>&1
python -X utf8 -I $S/c10cp/gates.py $B > $O/gates_c10cp_B.txt 2>&1
{
echo "== ukdefault"; python -X utf8 -I $S/c10cp/ukdefault.py $B 1 10
echo "== pxjump"; python -X utf8 -I $S/c11fl/pxjump.py M=$B
echo "== gdpjump"; python -X utf8 -I $S/m797/gdp_jump797.py M=$B
echo "== gdp_zero10"; python -X utf8 -I $S/m808/gdp_zero10.py M=$B
echo "== plvl"; python -X utf8 -I $S/brk693/plvl_seeds.py $B
echo "== hol"; python -X utf8 $S/hol/an_h2.py $B -v4
} > $O/stoplines_B.txt 2>&1
python -X utf8 -I $S/brk693/home_break.py --test c65mdB c65mdB=$B base=D:/PoB_work/keep_818/base_ins n1=D:/PoB_work/keep_797/n1 n2=D:/PoB_work/keep_797/n2 > $O/homebreak_B.txt 2>&1
python -X utf8 $S/tools/nullgate.py measurements/b10_20261104_c65mdB_ins measurements/b10_20261103_m797_ins measurements/b10_20261103_m797_n1_ins measurements/b10_20261103_m797_n2_ins > $O/nullgate_B.txt 2>&1
bash $S/ng.sh $B c65mdB D:/PoB_work/c65md_work/ngB rd:measurements/ng797_20261103:m797 > $O/ng_B.txt 2>&1
echo POSTDONE >> $O/gates_B.txt
