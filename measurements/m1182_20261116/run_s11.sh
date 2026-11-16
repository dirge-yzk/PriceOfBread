cd D:/PoB_work/r1182_work
python -X utf8 -I r1182/s11_v.py D:/PoB_work/keep_1178/base_ins M > out_s11_v_M.txt 2>&1
python -X utf8 -I r1182/s11_v.py D:/PoB_work/keep_1178/n1 N1 > out_s11_v_N1.txt 2>&1
python -X utf8 -I r1182/s11_v.py D:/PoB_work/keep_1178/n2 N2 > out_s11_v_N2.txt 2>&1
python -X utf8 -I r1182/s11_v.py D:/PoB_work/keep_1150/n1 N1p > out_s11_v_N1p.txt 2>&1
python -X utf8 -I r1182/s11_v.py D:/PoB_work/keep_1150/n2 N2p > out_s11_v_N2p.txt 2>&1
echo done > s11_v.done
