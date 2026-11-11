#!/usr/bin/env bash
# (849) c69ss 腕の読み（読むだけ）：門・空洞化・100 倍・1e10 倍・名目 GDP 0・価格水準・NG（記録）・門 A′（記録）
#   bash read849.sh <腕の出力フォルダ（ins と成績表を持つ）> <腕の成績表> <読みの出力フォルダ> [種の数=10] [母集合の下限の倍率=1]
export PYTHONUTF8=1
S=/d/PoB_work/wt_c69ss/scripts/instruments
A=${1:?腕の出力}; BAND=${2:?成績表}; O=${3:?出力}; NS=${4:-10}; SC=${5:-1}
mkdir -p $O
B=/d/PoB_work/keep_845/base_ins; N1=/d/PoB_work/keep_839/n1; N2=/d/PoB_work/keep_839/n2
python -X utf8 $S/c69ss/gates849.py $A/ins $B $N1 $N2 $O/gates.txt $NS $SC > /dev/null 2>$O/gates.err
python $S/hol/an_h2.py $A/ins -v4 > $O/hol.txt 2>&1
python -X utf8 -I $S/m839/holclass839.py $O/hol.txt /d/PoB_work/c69ss_work/null3/hol_base.txt > $O/holclass.txt 2>&1
python -X utf8 -I $S/m797/gdp_jump797.py arm=$A/ins base=$B n839_1=$N1 n839_2=$N2 > $O/jump797.txt 2>&1
python -X utf8 -I $S/r833/gdp_jump833.py arm=$A/ins base=$B n839_1=$N1 n839_2=$N2 > $O/jump833.txt 2>&1
python -X utf8 -I $S/m808/gdp_zero10.py arm=$A/ins base=$B > $O/gdpzero.txt 2>&1
python -X utf8 -I $S/brk693/plvl_seeds.py $A/ins > $O/plvl_arm.txt 2>&1
python -X utf8 -I $S/brk693/plvl_seeds.py $B > $O/plvl_base.txt 2>&1
if [ "$NS" = 10 ]; then
  python $S/tools/nullgate.py $BAND /d/PoB_work/wt_c69ss/measurements/b10_20261105_m839_ins /d/PoB_work/wt_c69ss/measurements/b10_20261105_m839_n1_ins /d/PoB_work/wt_c69ss/measurements/b10_20261105_m839_n2_ins > $O/nullgate.txt 2>&1
  W=$(cygpath -m $O 2>/dev/null || echo $O)
  (cd /d/PoB_work/wt_c69ss && bash $S/ng.sh $A/ins c69ssB $W/ng rd:measurements/ng839_20261105:m839 > $O/ng.txt 2>&1)
fi
