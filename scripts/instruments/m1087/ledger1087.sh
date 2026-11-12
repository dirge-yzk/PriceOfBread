#!/usr/bin/env bash
# (1087) 合流：台帳の更新を一続きで回す（ledger1087.py → reanchor1087.py〔decb50fa5→測った木〕→ r900/ledger_check.py）。
#   bash scripts/instruments/m1087/ledger1087.sh <測った木のコミット>
H=${1:?木}; W=D:/PoB_work/m1087_work; T=D:/PoB_work/wt_m1087; R=C:/Users/dirge/Documents/PriceOfBread
python -X utf8 $T/scripts/instruments/m1087/ledger1087.py ${H:0:9} $W/ledger_new.md || exit 1
python -X utf8 $T/scripts/instruments/m1087/reanchor1087.py $R decb50fa5 $H $W/ledger_new.md $W/ledger_re.md $W/reanchor1087.tsv || exit 1
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $H $W/ledger_re.md > $W/lc_merged.txt 2>&1; echo "check rc=$?"
head -1 $W/lc_merged.txt; grep "^門ごと" $W/lc_merged.txt; cut -f4 $W/reanchor1087.tsv | sort | uniq -c
