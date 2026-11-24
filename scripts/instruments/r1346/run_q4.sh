cd D:/PoB_work/r1346_work
S=1,2,3,4,5,6,7,8,9,10
export PYTHONIOENCODING=utf-8
python scripts/q4_bands.py D:/PoB_work/keep_1341_rej/mM_hfsp $S static > out/q4_hfsp.txt 2>/dev/null
python scripts/q4_bands.py D:/PoB_work/keep_1341_rej/aS_spOnly/ins $S static > out/q4_Asp.txt 2>/dev/null
python scripts/q4_bands.py D:/PoB_work/keep_1341/base_ins $S static > out/q4_Ahf.txt 2>/dev/null
python scripts/q4_bands.py D:/PoB_work/keep_1341/n1 $S > out/q4_N1.txt 2>/dev/null
python scripts/q4_bands.py D:/PoB_work/keep_1341/n2 $S > out/q4_N2.txt 2>/dev/null
echo done > out/q4.done
