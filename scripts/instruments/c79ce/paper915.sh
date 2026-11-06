#!/usr/bin/env bash
# (915) c79ce の紙：(902) の台本を今の盤面 keep_912/base_ins で取り直し、keep_905_rej の c77ce の腕に stoplines906 を当てる。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=/d/PoB_work/wt_c79ce; S=$T/scripts/instruments; W=D:/PoB_work/c79ce_work
cd $T
python -X utf8 -I $S/c77ce/gate902.py --selftest > $W/selftest902.txt 2>&1
python -X utf8 -I $S/c77ce/gate902.py D:/PoB_work/keep_912/base_ins $W/gate_base912.txt
python -X utf8 -I $S/c77ce/gate902.py --paper D:/PoB_work/keep_912/base_ins $W/paper_base912.txt
python -X utf8 -I $S/c77ce/dom902.py D:/PoB_work/keep_912/base_ins $W/dom_base912.txt
python -X utf8 -I $S/c77ce/gdpzero_born902.py D:/PoB_work/keep_905_rej/c77ceS_10_s11 > $W/gdpzero_born_c77ceS.txt 2>&1
bash $S/c78gz/stoplines906.sh D:/PoB_work/keep_905_rej/c77ceB_10 $W/stop906_c77ceB.txt 1
bash $S/c78gz/stoplines906.sh D:/PoB_work/keep_905_rej/c77ceS_10_s11 $W/stop906_c77ceS.txt 11
bash $S/c78gz/stoplines906.sh D:/PoB_work/keep_912/base_ins $W/stop906_base912.txt 1
bash $S/c78gz/stoplines906.sh D:/PoB_work/keep_905/a11 $W/stop906_a11.txt 11
bash $S/c78gz/stoplines906.sh D:/PoB_work/keep_905/n2 $W/stop906_n2.txt 1
echo PAPERDONE
