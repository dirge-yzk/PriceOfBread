#!/usr/bin/env bash
# (793) 集計の一式（読むだけ）：出力は D:/PoB_work/r793_work/scripts/m793_*.txt（合流が measurements/ へ写す前提）
export PYTHONUTF8=1
W=D:/PoB_work/r793_work; S=$W/scripts; R=C:/Users/dirge/Documents/PriceOfBread
python $S/paper_r793.py > $S/m793_paper.txt
python $S/an_r793.py $W/out_r793on_3/ins > $S/m793_armOn_buyers.txt
python $S/an_r793.py $W/out_r793off_3/ins 0.19 > $S/m793_armOff_buyers.txt
python $S/idcmp_r793.py D:/PoB_work/keep_790/base_ins $W/out_r793off_3/ins 1,2,3 > $S/m793_armOff_ident.txt
for d in 0.1 0.2 0.3; do python $S/replay_h.py $W/out_r793off_3/ins $d; done > $S/m793_replayH.txt
(cd $R && python $S/seedfrag.py $W/b3_r793off_3.txt measurements/b10_20261102_m784_ins 1,2,3; python $S/seedfrag.py $W/b3_r793on_3.txt measurements/b10_20261103_c9opB 1,2,3) > $S/m793_seedfrag.txt
echo FINALDONE
