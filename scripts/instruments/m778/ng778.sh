#!/usr/bin/env bash
# (778) NG の読み手（記録）：合流の木 M（新しい基準 ng778）・S11・Z を基準 m773 と比べる。読み手の出力フォルダは D:/… の形（★(254)）。
cd D:/PoB_work/wt_m778; S=scripts/instruments; W=D:/PoB_work/m778
bash $S/ng.sh $W/out_mB_10/ins m778 D:/PoB_work/wt_m778/measurements/ng778_20261102 rd:measurements/ng773_20261102:m773 > $W/ng_M.txt 2>&1
bash $S/ng.sh $W/out_mS_10_s11/ins m778s11 D:/PoB_work/m778/ng_s11 rd:measurements/ng773_20261102:m773 > $W/ng_S11.txt 2>&1
bash $S/ng.sh $W/out_mZ_10/ins m778z D:/PoB_work/m778/ng_z rd:measurements/ng773_20261102:m773 > $W/ng_Z.txt 2>&1
bash $S/ng.sh $W/out_n1_10/ins m778n1 D:/PoB_work/m778/ng_n1 rd:measurements/ng773_20261102:m773 > $W/ng_N1.txt 2>&1
bash $S/ng.sh $W/out_n2_10/ins m778n2 D:/PoB_work/m778/ng_n2 rd:measurements/ng773_20261102:m773 > $W/ng_N2.txt 2>&1
echo NGDONE >> $W/ng_M.txt
