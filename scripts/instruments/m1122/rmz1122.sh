#!/usr/bin/env bash
# (1122) 合流（m1103/rmz1103.sh の写し）：腕 Z の ident の生（判定〈恒等〉と成績表は measurements にコミット）を、表ごとの sha1 を取ってから消す（rmident1096.sh の型）。
export PYTHONUTF8=1
S=D:/PoB_work/wt_m1122/scripts/instruments; W=D:/PoB_work/m1122_work; O=D:/PoB_work/wt_m1122/measurements/m1122_20261114/hash; mkdir -p $O
H="python -X utf8 -I $S/m1044/hash1044.py"; d=D:/PoB_work/ident_m1122z
[ -d "$d/out_ib_10/ins" ] && $H "$d/out_ib_10/ins" $O/ident_z_ib.tsv
[ -d "$d/out_ia_5/ins" ] && $H "$d/out_ia_5/ins" $O/ident_z_ia.tsv
GB=$(python -c "import os;print(round(sum(os.path.getsize(os.path.join(r,f)) for r,_,fs in os.walk(r'$d') for f in fs)/1e9,2))")
rm -rf "$d" && echo "$(date '+%F %T') 消した $d（${GB}GB・合流の腕 Z の ident の生・判定〈恒等〉と成績表は measurements/ident_20261114_m1122z.txt ほか・表ごとの sha1 は measurements/m1122_20261114/hash/ident_z_*.tsv）" >> $W/deleted.txt
python -c "import shutil;print('D: free',round(shutil.disk_usage('D:/').free/1e9,2),'GB')"
