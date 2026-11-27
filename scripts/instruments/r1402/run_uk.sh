#!/usr/bin/env bash
# (1402) 英の国債の年ごとの並び（r1388 の uk_traj.py・uk_first.py を使い回す）
S=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments/r1388; O=D:/PoB_work/r1402_work/out
python -X utf8 -I $S/uk_traj.py D:/PoB_work/keep_1397/base_ins 1 $O/uk_M.tsv
python -X utf8 -I $S/uk_traj.py D:/PoB_work/keep_1383/n1 1 $O/uk_N1.tsv
python -X utf8 -I $S/uk_traj.py D:/PoB_work/keep_1383/n2 1 $O/uk_N2.tsv
python -X utf8 -I $S/uk_traj.py D:/PoB_work/keep_1383/a11 11 $O/uk_S11.tsv
python -X utf8 -I $S/uk_first.py $O M N1 N2 S11 > $O/uk_first.txt
echo ALLDONE > $O/uk.done
