#!/usr/bin/env bash
# (784) NG の読み手（記録）：合流の木 M（新しい基準 ng784）・S11・N1・N2 を基準 m778 と比べる。読み手の出力フォルダは D:/… の形（★(254)）。
cd D:/PoB_work/wt_m784; S=scripts/instruments; W=D:/PoB_work/m784
bash $S/ng.sh $W/out_mB_10/ins m784 D:/PoB_work/wt_m784/measurements/ng784_20261102 rd:measurements/ng778_20261102:m778 > $W/ng_M.txt 2>&1
bash $S/ng.sh $W/out_mS_10_s11/ins m784s11 D:/PoB_work/m784/ng_s11 rd:measurements/ng778_20261102:m778 > $W/ng_S11.txt 2>&1
bash $S/ng.sh $W/out_n1_10/ins m784n1 D:/PoB_work/m784/ng_n1 rd:measurements/ng778_20261102:m778 > $W/ng_N1.txt 2>&1
bash $S/ng.sh $W/out_n2_10/ins m784n2 D:/PoB_work/m784/ng_n2 rd:measurements/ng778_20261102:m778 > $W/ng_N2.txt 2>&1
echo NGDONE >> $W/ng_M.txt
