#!/usr/bin/env bash
# (1376) 線の ident の生の要約を取り、生を消す（4 線とも反証役が読み終えた・恒等の線）
R=C:/Users/dirge/Documents/PriceOfBread; W=D:/PoB_work/m1376_work
for k in ts sv rv sm; do
  d=D:/PoB_work/ident_c145$k/out_ib_10/ins
  [ -d $d ] && python -I $R/scripts/instruments/tools/keepsum.py $d $W/keepsum_ident_c145$k.tsv
done
python -c "import shutil;print('free_before',round(shutil.disk_usage('D:/').free/1e9,1))"
rm -rf D:/PoB_work/ident_c145ts D:/PoB_work/ident_c145sv D:/PoB_work/ident_c145rv D:/PoB_work/ident_c145sm D:/PoB_work/c145ts_art D:/PoB_work/c145rv_art D:/PoB_work/c145sv_art
python -c "import shutil;print('free_after',round(shutil.disk_usage('D:/').free/1e9,1))"
