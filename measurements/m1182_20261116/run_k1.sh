cd D:/PoB_work/r1182_work
B=D:/PoB_work/keep_1178/base_ins
python -X utf8 -I r1182/k1_attr.py $B D:/PoB_work/keep_1178_rej/m1178_M_t1k1/ins "M_t1k1対M′（k1 だけが違う）" > out_k1_Mt1k1.txt 2>&1
python -X utf8 -I r1182/k1_attr.py $B D:/PoB_work/keep_1178_rej/c116k1_B/ins "c116k1_B対M′（k1＋木の違い t1・cl）" > out_k1_B.txt 2>&1
python -X utf8 -I r1182/k1_attr.py $B D:/PoB_work/keep_1178/n1 "N1対M′（ヌル腕）" > out_k1_N1.txt 2>&1
python -X utf8 -I r1182/k1_attr.py $B D:/PoB_work/keep_1178/n2 "N2対M′（ヌル腕）" > out_k1_N2.txt 2>&1
python -X utf8 -I r1182/k1_attr.py D:/PoB_work/keep_1178/n1 D:/PoB_work/keep_1178/n2 "N2対N1（ヌル腕どうし）" > out_k1_N1N2.txt 2>&1
echo done > k1.done
