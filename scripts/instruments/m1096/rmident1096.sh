#!/usr/bin/env bash
# (1096) 合流：判定と成績表がコミット済みの線の ident の生（c104ui・c104cp・c104si・c104fb）と合流の腕 Z の残り（ins は keep_1096 へ移した後）を、表ごとの sha1 を取ってから消す（deleted.txt に名乗る・rmident1087.sh の型）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1096_work/rmident.mark bash scripts/instruments/m1096/rmident1096.sh
export PYTHONUTF8=1
S=D:/PoB_work/wt_m1096/scripts/instruments; W=D:/PoB_work/m1096_work; O=D:/PoB_work/wt_m1096/measurements/m1096_20261113/hash; mkdir -p $O
H="python -X utf8 -I $S/m1044/hash1044.py"
for p in "ui:D:/PoB_work/ident_c104ui" "cp:D:/PoB_work/ident_c104cp" "si:D:/PoB_work/c104si_work/ident" "fb:D:/PoB_work/c104fb_work/ident" "z:D:/PoB_work/ident_m1096z"; do
  k=${p%%:*}; d=${p#*:}
  [ -d "$d" ] || { echo "$k 無い $d"; continue; }
  [ -d "$d/out_ib_10/ins" ] && $H "$d/out_ib_10/ins" $O/ident_${k}_ib.tsv
  [ -d "$d/out_ia_5/ins" ] && $H "$d/out_ia_5/ins" $O/ident_${k}_ia.tsv
  GB=$(python -c "import os;print(round(sum(os.path.getsize(os.path.join(r,f)) for r,_,fs in os.walk(r'$d') for f in fs)/1e9,2))")
  rm -rf "$d" && echo "$(date '+%F %T') 消した $d（${GB}GB・線 c104$k の ident の生〔z は合流の腕 Z の残り・ins は keep_1096/base_ins へ移した後〕・判定と成績表はコミット済み・表ごとの sha1 は measurements/m1096_20261113/hash/ident_${k}_*.tsv）" >> $W/deleted.txt
done
python -c "import shutil;print('D: free',round(shutil.disk_usage('D:/').free/1e9,2),'GB')"
