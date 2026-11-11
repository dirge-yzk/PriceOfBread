#!/usr/bin/env bash
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
W=D:/PoB_work/m905; T=D:/PoB_work/wt_m905/scripts/instruments
python -X utf8 -I $T/tools/keepsum.py $W/out_mM_10/ins $W/keepsum_mM_all.tsv
python -X utf8 -I $T/tools/keepsum.py $W/out_mS_10_s11/ins $W/keepsum_mS_s11_all.tsv
python -X utf8 -I $T/tools/keepsum.py D:/PoB_work/ident_m905z/out_ib_10/ins $W/keepsum_identZ_ins.tsv
bash $T/m905/read905.sh b mM $W/out_mM_10/ins $W/b10_mM_10.txt
bash $T/m905/read905.sh s mS $W/out_mS_10_s11/ins $W/b10_mS_10_s11.txt
