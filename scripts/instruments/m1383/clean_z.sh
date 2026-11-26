#!/usr/bin/env bash
# (1383) 合流：自分の腕 Z（ident m1383z〈判定: 恒等〉）の生を、要約を残してから消す（M・S11 の空きのため）。線の作業フォルダには触れない。
set -e
R=D:/PoB_work/wt_m1383; M=$R/measurements/m1383_20261126; S=$R/scripts/instruments
python -X utf8 -I $S/tools/keepsum.py D:/PoB_work/ident_m1383z/out_ib_10/ins $M/keepsum_ident_m1383z_ins.tsv
python -X utf8 -I $S/m1362/rsum.py $M/rsum_ident_m1383z.tsv D:/PoB_work/ident_m1383z
rm -rf D:/PoB_work/ident_m1383z
echo "$(date '+%F %T') 消した：D:/PoB_work/ident_m1383z（この回に作った腕 Z の作業フォルダ・判定: 恒等の後・要約 keepsum_ident_m1383z_ins.tsv・rsum_ident_m1383z.tsv）" >> $M/deleted.txt
python -c "import shutil;print('free',shutil.disk_usage('D:/').free/1e9)"
