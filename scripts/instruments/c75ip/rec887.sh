#!/usr/bin/env bash
export PYTHONUTF8=1
cd D:/PoB_work/wt_c75ip; W=D:/PoB_work/c75ip_work/s11
python -I -X utf8 scripts/instruments/tools/nullgate.py $W/b10_c75ipS_10_s11.txt measurements/b10s11_20261106_m873_ins measurements/b10_20261106_m873_n1_ins measurements/b10_20261106_m873_n2_ins > $W/nullgate_S.txt 2>&1; echo "nullgate rc=$?"
bash scripts/instruments/ng.sh $W/out_c75ipS_10_s11/ins c75ipS D:/PoB_work/c75ip_work/s11/ng_S > $W/ng_S.txt 2>&1; echo "ng S rc=$?"
bash scripts/instruments/ng.sh D:/PoB_work/keep_873/a11 a11 D:/PoB_work/c75ip_work/s11/ng_a11 rd:D:/PoB_work/c75ip_work/s11/ng_S:c75ipS > $W/ng_a11_vs_S.txt 2>&1; echo "ng a11 rc=$?"
bash scripts/instruments/ng.sh $W/out_c75ipS_10_s11/ins c75ipS D:/PoB_work/c75ip_work/s11/ng_S2 rd:measurements/ng873_20261106:m873 > $W/ng_S_vs_ng873.txt 2>&1; echo "ng S vs 873 rc=$?"
echo RECDONE
