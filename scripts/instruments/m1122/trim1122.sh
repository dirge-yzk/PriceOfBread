#!/usr/bin/env bash
# (1122) 合流：線の生を、表ごとの sha1 を取ってから刈る（m1103/trim1103.sh の写し・ps・mc の腕 B の生は SMALL と門の表〔parlseat_s・prds_s・recipeout_s・minecap_s・metal_s〕だけ残す・sm の ident の生は sha1 の後に消す）。
#   rr・gp の腕 B の生＝門の表と SMALL（＋trrail_s・trnation_s・trroute_s・trbin_s）だけ残す（アブレーションの腕と突き合わせるため・突き合わせた後に消す）。
#   ix の ident の生＝判定と成績表はコミット済み＝sha1 の後に消す。gp の突然変異の腕の生（mw）は採否まで残す。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1122_work/trim.mark bash scripts/instruments/m1122/trim1122.sh
export PYTHONUTF8=1
S=D:/PoB_work/wt_m1122/scripts/instruments; W=D:/PoB_work/m1122_work; O=D:/PoB_work/wt_m1122/measurements/m1122_20261114/hash; mkdir -p $O
H="python -X utf8 -I $S/m1044/hash1044.py"
KEEP="nation_s,stock_s,wars_s,casualty_s,region_s,migration_s,pxmonth_s,scalars_s,aib_s,canal_s,class_s,disc_s,engineer_s,epi_s,goods_s,ideology_s,idn_s,invade_s,labor_s,monai_s,mongood_s,monlaw_s,pclamp_s,pxgoods_s,lawgate_s,reform_s,sbu_s,sf_s,fiscal_s,parlseat_s,prds_s,recipeout_s,minecap_s,metal_s"
for p in "psB:D:/PoB_work/c108ps_work/armB/out_c108psB_10/ins" "mcB:D:/PoB_work/c108mc_work/armB/out_c108mcB_10/ins"; do
  k=${p%%:*}; d=${p#*:}
  [ -d "$d" ] || { echo "$k 無い $d"; continue; }
  $H "$d" $O/line_${k}.tsv || { echo "$k の sha1 に失敗＝刈らない"; continue; }
  python -X utf8 -I - "$d" "$KEEP" "$W/deleted.txt" "$k" <<'EOF'
import os, re, sys
d, keep, log, k = sys.argv[1], set(sys.argv[2].split(',')), sys.argv[3], sys.argv[4]
n = 0; b = 0
for f in os.listdir(d):
    m = re.match(r'(.+_s)\d+\.tsv$', f)
    if m and m.group(1) in keep: continue
    p = os.path.join(d, f); b += os.path.getsize(p); os.remove(p); n += 1
open(log, 'a', encoding='utf-8').write(f'刈った {d}（{k}・{n} 本・{b/1e9:.2f}GB・門の表と SMALL は残す・表ごとの sha1 は measurements/m1122_20261114/hash/line_{k}.tsv）\n')
print(k, n, round(b/1e9, 2))
EOF
done
d=D:/PoB_work/ident_c108sm
if [ -d "$d" ]; then
  [ -d "$d/out_ib_10/ins" ] && $H "$d/out_ib_10/ins" $O/ident_sm_ib.tsv
  [ -d "$d/out_ia_5/ins" ] && $H "$d/out_ia_5/ins" $O/ident_sm_ia.tsv
  GB=$(python -c "import os;print(round(sum(os.path.getsize(os.path.join(r,f)) for r,_,fs in os.walk(r'$d') for f in fs)/1e9,2))")
  rm -rf "$d" && echo "$(date '+%F %T') 消した $d（${GB}GB・線 c108sm の ident の生・判定と成績表は origin/c108sm にコミット済み・表ごとの sha1 は measurements/m1122_20261114/hash/ident_sm_*.tsv）" >> $W/deleted.txt
fi
python -c "import shutil;print('D: free',round(shutil.disk_usage('D:/').free/1e9,2),'GB')"
