#!/usr/bin/env bash
# (806) c12rd：腕の生を読む（読むだけ）。bash read806.sh <腕の名 r806f1|r806f0>
export PYTHONUTF8=1
W=D:/PoB_work/r806_work; R=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments
A=$1; I=$W/out_${A}_4_s14/ins; O=$W/rd_$A; mkdir -p $O
python -X utf8 $R/hol/an_h2.py $I -v5 > $O/anh2_v5.txt 2>&1
python -I $R/m802/gdp_zero.py $A=$I > $O/gdp_zero.txt 2>&1
python -I $W/r806_series.py $I 14 ネーデルラント バーミンガム > $O/ser_s14_NL_bham.tsv
python -I $W/r806_series.py $I 17 スペイン フォートウィリアム > $O/ser_s17_ES_fw.tsv
python -I $W/r806_series.py $I 16 ネーデルラント ロッテルダム --q 1895-1925 > $O/ser_s16_NL_rot.tsv
python -I $W/r806_hol.py $I 14 バーミンガム > $O/hol_s14_bham.txt
python -I $W/r806_hol.py $I 17 フォートウィリアム > $O/hol_s17_fw.txt
echo READDONE $A
