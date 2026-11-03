#!/usr/bin/env bash
# (802) NG の読み手（記録）：合流の木 M（新しい基準 ng802）・S11・N1・N2 を基準 m797 と比べる。読み手の出力フォルダは D:/… の形（★(254)）。
cd D:/PoB_work/wt_m802; S=scripts/instruments; W=D:/PoB_work/m802
bash $S/ng.sh $W/out_mB_10/ins m802 D:/PoB_work/wt_m802/measurements/ng802_20261103 rd:measurements/ng797_20261103:m797 > $W/ng_M.txt 2>&1
bash $S/ng.sh $W/out_mS_10_s11/ins m802s11 D:/PoB_work/m802/ng_s11 rd:measurements/ng797_20261103:m797 > $W/ng_S11.txt 2>&1
bash $S/ng.sh $W/out_n1_10/ins m802n1 D:/PoB_work/m802/ng_n1 rd:measurements/ng797_20261103:m797 > $W/ng_N1.txt 2>&1
bash $S/ng.sh $W/out_n2_10/ins m802n2 D:/PoB_work/m802/ng_n2 rd:measurements/ng797_20261103:m797 > $W/ng_N2.txt 2>&1
echo NGDONE >> $W/ng_M.txt
