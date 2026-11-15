#!/usr/bin/env bash
# (1150) 合流（m1143/ledger1143.sh の写し・基点 aa8ee7e51 から合わせた木へ錨を写す・作業フォルダ m1150_work・台帳は wt_m1150 の物〔S26 の 50 行を s26 の版へ入れ替えた後〕）
H=${1:?木}; O=aa8ee7e51; W=D:/PoB_work/m1150_work; R=C:/Users/dirge/Documents/PriceOfBread; T=D:/PoB_work/wt_m1150
python -X utf8 $T/scripts/instruments/m1116/reanchor1116.py $R $O $H $T/docs/機能の台帳.md $W/ledger_re.md $W/reanchor1150.tsv || exit 1
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $H $W/ledger_re.md > $W/lc_merged.txt 2>&1; echo "check rc=$?"
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $H $T/docs/機能の台帳.md > $W/lc_merged_raw.txt 2>&1; echo "check_raw rc=$?"
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $O $T/docs/機能の台帳.md > $W/lc_before.txt 2>&1; echo "check_before rc=$?"
for f in lc_before lc_merged_raw lc_merged; do echo "$f: $(head -1 $W/$f.txt)"; done; grep "^門" $W/lc_merged.txt | head -12; cut -f4 $W/reanchor1150.tsv | sort | uniq -c
