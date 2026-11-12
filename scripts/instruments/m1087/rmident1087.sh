#!/usr/bin/env bash
# (1087) 合流：判定と成績表がコミット済みの線の ident の生（c103st・c103tr）を、表ごとの sha1 を取ってから消す（deleted.txt に名乗る）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1087_work/rmident.mark bash scripts/instruments/m1087/rmident1087.sh
export PYTHONUTF8=1
S=D:/PoB_work/wt_m1087/scripts/instruments; W=D:/PoB_work/m1087_work; O=D:/PoB_work/wt_m1087/measurements/m1087_20261113/hash; mkdir -p $O
H="python -X utf8 -I $S/m1044/hash1044.py"
for p in "st:D:/PoB_work/c103st_work/ident" "tr:D:/PoB_work/ident_c103tr"; do
  k=${p%%:*}; d=${p#*:}
  [ -d "$d" ] || { echo "$k 無い $d"; continue; }
  [ -d "$d/out_ib_10/ins" ] && $H "$d/out_ib_10/ins" $O/ident_${k}_ib.tsv
  [ -d "$d/out_ia_5/ins" ] && $H "$d/out_ia_5/ins" $O/ident_${k}_ia.tsv
  GB=$(python -c "import os;print(round(sum(os.path.getsize(os.path.join(r,f)) for r,_,fs in os.walk(r'$d') for f in fs)/1e9,2))")
  rm -rf "$d" && echo "$(date '+%F %T') 消した $d（${GB}GB・線 c103$k の ident の生・判定と成績表は枝にコミット済み・表ごとの sha1 は measurements/m1087_20261113/hash/ident_${k}_*.tsv）" >> $W/deleted.txt
done
python -c "import shutil;print('D: free',round(shutil.disk_usage('D:/').free/1e9,2),'GB')"
