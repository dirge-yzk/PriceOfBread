#!/usr/bin/env bash
# (849) 突然変異の 1 種の計器つきの腕（使い捨て）：bash mutarm.sh <M1..M6>
M=${1:?}
export POB_WORK=D:/PoB_work/c69ss_work/mut POB_REPO=D:/PoB_work/wt_c69ss MEASURE_CACHE=0
mkdir -p /d/PoB_work/c69ss_work/mut
IS=/d/PoB_work/wt_c69ss/scripts/instruments
bash $IS/batch.sh prep c69ss$M commit:6cd65a80 plain "$IS/c69ss/mut849.py $M" && INS=1 bash $IS/batch.sh run c69ss$M 1
