#!/usr/bin/env bash
# (1129) 合流：m1122/ledger1122.sh の写し（錨の写しを 758f554ed→合わせた木・作業フォルダを m1129・台本と台帳は main の木の物）。
#   bash scripts/instruments/m1129/ledger1129.sh <合わせた木のコミット>
H=${1:?木}; O=758f554ed; W=D:/PoB_work/m1129_work; R=C:/Users/dirge/Documents/PriceOfBread; T=$R
python -X utf8 $T/scripts/instruments/m1116/reanchor1116.py $R $O $H $T/docs/機能の台帳.md $W/ledger_re.md $W/reanchor1129.tsv || exit 1
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $H $W/ledger_re.md > $W/lc_merged.txt 2>&1; echo "check rc=$?"
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $H $T/docs/機能の台帳.md > $W/lc_merged_raw.txt 2>&1; echo "check_raw rc=$?"
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $O $T/docs/機能の台帳.md > $W/lc_before.txt 2>&1; echo "check_before rc=$?"
for f in lc_before lc_merged_raw lc_merged; do echo "$f: $(head -1 $W/$f.txt)"; done; grep "^門" $W/lc_merged.txt | head -12; cut -f4 $W/reanchor1129.tsv | sort | uniq -c
