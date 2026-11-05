#!/usr/bin/env bash
export POB_WORK=D:/PoB_work/c69ss_work/armB POB_REPO=D:/PoB_work/wt_c69ss MEASURE_CACHE=0
mkdir -p /d/PoB_work/c69ss_work/armB
IS=/d/PoB_work/wt_c69ss/scripts/instruments
bash $IS/batch.sh prep c69ssB commit:6cd65a80 plain && INS=1 bash $IS/batch.sh run c69ssB 10
