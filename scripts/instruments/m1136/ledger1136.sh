#!/usr/bin/env bash
# (1136) 合流（m1129/ledger1129.sh の写し・基点 50e0b5c93 から合わせた木 b6d35e2c7 へ錨を写す・作業フォルダ m1136_work・台帳は wt_m1136 の物）
H=${1:?木}; O=50e0b5c93; W=D:/PoB_work/m1136_work; R=C:/Users/dirge/Documents/PriceOfBread; T=D:/PoB_work/wt_m1136
python -X utf8 $T/scripts/instruments/m1116/reanchor1116.py $R $O $H $T/docs/機能の台帳.md $W/ledger_re.md $W/reanchor1136.tsv || exit 1
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $H $W/ledger_re.md > $W/lc_merged.txt 2>&1; echo "check rc=$?"
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $H $T/docs/機能の台帳.md > $W/lc_merged_raw.txt 2>&1; echo "check_raw rc=$?"
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $O $T/docs/機能の台帳.md > $W/lc_before.txt 2>&1; echo "check_before rc=$?"
for f in lc_before lc_merged_raw lc_merged; do echo "$f: $(head -1 $W/$f.txt)"; done; grep "^門" $W/lc_merged.txt | head -12; cut -f4 $W/reanchor1136.tsv | sort | uniq -c
