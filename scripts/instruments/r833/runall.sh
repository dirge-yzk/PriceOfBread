#!/usr/bin/env bash
# (833) 10 個の生と小さな盤面に恒等の門と新しい区分を当てる（読むだけ）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
W=D:/PoB_work/r833_work; OLD=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments/m797/gdp_jump797.py; P=D:/PoB_work
A="M=$P/keep_826/base_ins N1n=$P/keep_826/n1 N2n=$P/keep_826/n2 A11=$P/keep_826/a11 N1o=$P/keep_797/n1 N2o=$P/keep_797/n2 I832=$P/keep_832/base_ins MD=$P/keep_826_rej/c65mdB_10/ins DW=$P/keep_832_rej/c66dwB_10/ins DWS=$P/keep_832_rej/mS_s11/ins FIX=$W/fixture"
python -I -X utf8 $W/gdp_jump833.py --rows $A > $W/out_new.txt
python -I -X utf8 $W/gate833.py $W/gdp_jump833.py $OLD $A > $W/out_gate.txt
