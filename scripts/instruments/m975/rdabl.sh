cd D:/PoB_work/wt_m975 || exit 9
W=D:/PoB_work/m975_work; mkdir -p $W/read/abl
SB=1 python -X utf8 scripts/instruments/c87mw/gates973.py $W/out_amt0_10/ins D:/PoB_work/keep_968/base_ins > $W/read/abl/gate_mw_amt0.txt 2>&1; echo "gate amt0 rc=$?"
SB=1 python -X utf8 scripts/instruments/c87mw/gates973.py $W/out_amw0_10/ins D:/PoB_work/keep_968/base_ins > $W/read/abl/gate_mw_amw0.txt 2>&1; echo "gate amw0 rc=$?"
python -X utf8 -I scripts/instruments/c87mt/pinit972.py $W/out_amw0_10/ins 1 > $W/read/abl/pinit_amw0.txt 2>&1; echo "pinit amw0 rc=$?"
python -X utf8 -I $W/rawcmp.py $W/out_amt0_10/ins $W/out_mM_10/ins > $W/read/abl/rawcmp_amt0_vs_M.txt 2>&1; echo "cmp1 rc=$?"
python -X utf8 -I $W/rawcmp.py $W/out_amw0_10/ins D:/PoB_work/keep_968/base_ins > $W/read/abl/rawcmp_amw0_vs_keep968.txt 2>&1; echo "cmp2 rc=$?"
echo ABLDONE
