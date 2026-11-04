#!/usr/bin/env bash
# (833) 台本の突然変異（使い捨ての写しを作り、10 個の生と小さな盤面に当てる）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
W=D:/PoB_work/r833_work; OLD=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments/m797/gdp_jump797.py; P=D:/PoB_work
A="M=$P/keep_826/base_ins N1n=$P/keep_826/n1 N2n=$P/keep_826/n2 A11=$P/keep_826/a11 N1o=$P/keep_797/n1 N2o=$P/keep_797/n2 I832=$P/keep_832/base_ins MD=$P/keep_826_rej/c65mdB_10/ins DW=$P/keep_832_rej/c66dwB_10/ins DWS=$P/keep_832_rej/mS_s11/ins FIX=$W/fixture"
mkdir -p $W/mut
python -I -X utf8 $W/mkmut.py $W/gdp_jump833.py $W/mut
python -I -X utf8 $OLD $A > $W/out_old.txt
for m in mu0 mu1 mu2 mu3 mu4; do
  python -I -X utf8 $W/mut/gdp_jump833_$m.py --rows $A > $W/mut/out_$m.txt
  python -I -X utf8 $W/gatef.py $W/mut/out_$m.txt $W/out_old.txt > $W/mut/gate_$m.txt
done
