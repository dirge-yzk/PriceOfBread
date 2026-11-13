#!/usr/bin/env bash
# (1103) 合流：アブレーションの腕 A_rr・A_gp の生（成績表と共通の表が線の腕 B と一致を読んだ後）と、線の腕 B の刈った生・gp の突然変異の腕の生（採否の後）を、表ごとの sha1 を取ってから消す。
export PYTHONUTF8=1
S=D:/PoB_work/wt_m1103/scripts/instruments; W=D:/PoB_work/m1103_work; O=D:/PoB_work/wt_m1103/measurements/m1103_20261113/hash; mkdir -p $O
H="python -X utf8 -I $S/m1044/hash1044.py"
for p in "aRr:D:/PoB_work/m1103_work/out_aRr_10" "aGp:D:/PoB_work/m1103_work/out_aGp_10" "lineRr:D:/PoB_work/c105rr_work/out_c105rrB_10" "lineGp:D:/PoB_work/c105gp_work/b/out_c105gpB_10" "gpMw:D:/PoB_work/c105gp_work/mw"; do
  k=${p%%:*}; d=${p#*:}
  [ -d "$d" ] || { echo "$k 無い $d"; continue; }
  if [ -d "$d/ins" ]; then $H "$d/ins" $O/rm_${k}.tsv || { echo "$k sha1 失敗＝消さない"; continue; }; else find "$d" -path '*/ins' -type d | while read x; do n=$(basename $(dirname $x)); $H "$x" $O/rm_${k}_$n.tsv; done; fi
  GB=$(python -c "import os;print(round(sum(os.path.getsize(os.path.join(r,f)) for r,_,fs in os.walk(r'$d') for f in fs)/1e9,2))")
  rm -rf "$d" && echo "$(date '+%F %T') 消した $d（${GB}GB・$k・成績表は measurements にある・表ごとの sha1 は measurements/m1103_20261113/hash/rm_${k}*.tsv）" >> $W/deleted.txt
done
python -c "import shutil;print('D: free',round(shutil.disk_usage('D:/').free/1e9,2),'GB')"
