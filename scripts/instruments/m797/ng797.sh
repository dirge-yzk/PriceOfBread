#!/usr/bin/env bash
# (797) NG の読み手（記録）：合流の木 M（新しい基準 ng797）・S11・N1・N2 を基準 m784 と比べる。読み手の出力フォルダは D:/… の形（★(254)）。
cd D:/PoB_work/wt_m797; S=scripts/instruments; W=D:/PoB_work/m797
bash $S/ng.sh $W/out_mB_10/ins m797 D:/PoB_work/wt_m797/measurements/ng797_20261103 rd:measurements/ng784_20261102:m784 > $W/ng_M.txt 2>&1
bash $S/ng.sh $W/out_mS_10_s11/ins m797s11 D:/PoB_work/m797/ng_s11 rd:measurements/ng784_20261102:m784 > $W/ng_S11.txt 2>&1
bash $S/ng.sh $W/out_n1_10/ins m797n1 D:/PoB_work/m797/ng_n1 rd:measurements/ng784_20261102:m784 > $W/ng_N1.txt 2>&1
bash $S/ng.sh $W/out_n2_10/ins m797n2 D:/PoB_work/m797/ng_n2 rd:measurements/ng784_20261102:m784 > $W/ng_N2.txt 2>&1
echo NGDONE >> $W/ng_M.txt
