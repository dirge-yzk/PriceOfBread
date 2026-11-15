#!/usr/bin/env bash
# (1166) c115sv 1 種の計器つきの腕（表を絞る・使い捨ての写しの木＝<コミット>＋任意の編集 mut_sv.py）。旗 1 の短い走りと門の突然変異に使う（bg.sh で起こす）。
#   bash scripts/instruments/c115sv/arm1.sh <コミット> <作業フォルダ（D:/…）> <名> <POB_COEF の文字列|-> [突然変異の形|-]
#   出力：<作業フォルダ>/out_sv_<名>_1/ins。門は read の paper_sv.py＋gates_sv.py（1 種＝床と閾値は 10 種の物＝向きと桁だけ読む）。
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}; N=${3:?名}; CO=${4:--}; MUT=${5:--}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
export POB_INSTRUMENTS_TABLES=cpop_s,nation_s,admrule_s,rule_s,soldiervac_s,monlaw_s,scalars_s
if [ "$CO" != "-" ]; then echo "$CO" > $POB_WORK/coef_sv_$N.txt; else rm -f $POB_WORK/coef_sv_$N.txt; fi
if [ "$MUT" != "-" ]; then
  SV_MUT=$MUT bash $S/batch.sh prep sv_$N commit:$C plain "$S/c115sv/mut_sv.py" > $POB_WORK/prep_$N.txt 2>&1 || { echo "PREPFAIL $N"; exit 1; }
else
  bash $S/batch.sh prep sv_$N commit:$C plain > $POB_WORK/prep_$N.txt 2>&1 || { echo "PREPFAIL $N"; exit 1; }
fi
INS=1 bash $S/batch.sh run sv_$N 1 > $POB_WORK/run_$N.txt 2>&1; RC=$?
O=$POB_WORK/out_sv_${N}_1/ins
python -X utf8 -I $S/c115sv/paper_sv.py $O 1 1 $POB_WORK/paper_$N.txt > /dev/null 2>&1
python -X utf8 -I $S/c115sv/gates_sv.py $O $POB_WORK/paper_$N.txt 1 1 > $POB_WORK/gates_$N.txt 2>&1
echo "$N rc=$RC $(tail -1 $POB_WORK/run_$N.txt)"
exit $RC
