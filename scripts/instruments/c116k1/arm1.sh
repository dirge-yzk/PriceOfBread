#!/usr/bin/env bash
# (1173) c116k1 1 種の計器つきの腕（表を絞る・使い捨ての写しの木＝<コミット>）。旗 1 の短い走りに使う（bg.sh で起こす）。c115sv/arm1.sh の写し。
#   bash scripts/instruments/c116k1/arm1.sh <コミット> <作業フォルダ（D:/…）> <名> <POB_COEF の文字列|->
#   出力：<作業フォルダ>/out_k1_<名>_1/ins。門は gates_k1.py（1 種＝閾値は 10 種の物＝向きと桁と G2・G3a の外れだけ読む）。
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}; N=${3:?名}; CO=${4:--}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
export POB_INSTRUMENTS_TABLES=secession_s,sf_s,monlaw_s,nation_s,scalars_s
if [ "$CO" != "-" ]; then echo "$CO" > $POB_WORK/coef_k1_$N.txt; else rm -f $POB_WORK/coef_k1_$N.txt; fi
bash $S/batch.sh prep k1_$N commit:$C plain > $POB_WORK/prep_$N.txt 2>&1 || { echo "PREPFAIL $N"; exit 1; }
INS=1 bash $S/batch.sh run k1_$N 1 > $POB_WORK/run_$N.txt 2>&1; RC=$?
O=$POB_WORK/out_k1_${N}_1/ins
python -X utf8 -I $S/c116k1/gates_k1.py $O 1 1 $POB_WORK/gates_$N.txt $N > /dev/null 2>&1
echo "$N rc=$RC $(tail -1 $POB_WORK/run_$N.txt)"
exit $RC
