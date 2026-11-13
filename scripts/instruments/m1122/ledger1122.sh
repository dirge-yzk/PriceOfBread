#!/usr/bin/env bash
# (1122) 合流：m1116/ledger1116.sh の写し（錨の写しを cfd8de3e0→合わせた木・作業フォルダを m1122）。
# (1116) 合流：m1103/ledger1103.sh の型（錨の写しを 1cd66aae5→合わせた木・reanchor1116.py〔行末の注を除いたコードの字で行を対応させる〕・作業フォルダを m1116）。
#   bash scripts/instruments/m1122/ledger1122.sh <合わせた木のコミット>
H=${1:?木}; O=cfd8de3e0; W=D:/PoB_work/m1122_work; T=D:/PoB_work/wt_m1122; R=C:/Users/dirge/Documents/PriceOfBread
python -X utf8 $T/scripts/instruments/m1116/reanchor1116.py $R $O $H $T/docs/機能の台帳.md $W/ledger_re.md $W/reanchor1122.tsv || exit 1
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $H $W/ledger_re.md > $W/lc_merged.txt 2>&1; echo "check rc=$?"
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $H $T/docs/機能の台帳.md > $W/lc_merged_raw.txt 2>&1; echo "check_raw rc=$?"
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $O $T/docs/機能の台帳.md > $W/lc_before.txt 2>&1; echo "check_before rc=$?"
for f in lc_before lc_merged_raw lc_merged; do echo "$f: $(head -1 $W/$f.txt)"; done; grep "^門" $W/lc_merged.txt | head -12; cut -f4 $W/reanchor1122.tsv | sort | uniq -c
