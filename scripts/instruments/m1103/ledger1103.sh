#!/usr/bin/env bash
# (1103) 合流：ledger1096.sh の写し（錨の写しを fff0f1658→合わせた木・作業フォルダを m1103）。
# (1096) 合流：台帳の錨の写し（m1087/reanchor1087.py〔fff0f1658→合わせた木〕）と r900/ledger_check.py（ledger1087.sh の型・ledger1096.py は先に当てた後）。
#   bash scripts/instruments/m1096/ledger1096.sh <合わせた木のコミット>
H=${1:?木}; W=D:/PoB_work/m1103_work; T=D:/PoB_work/wt_m1103; R=C:/Users/dirge/Documents/PriceOfBread
python -X utf8 $T/scripts/instruments/m1087/reanchor1087.py $R fff0f1658 $H $T/docs/機能の台帳.md $W/ledger_re.md $W/reanchor1103.tsv || exit 1
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R $H $W/ledger_re.md > $W/lc_merged.txt 2>&1; echo "check rc=$?"
python -X utf8 $T/scripts/instruments/r900/ledger_check.py $R fff0f1658 $T/docs/機能の台帳.md > $W/lc_before.txt 2>&1; echo "check_before rc=$?"
head -1 $W/lc_merged.txt; grep "^門ごと\|^門" $W/lc_merged.txt | head -12; cut -f4 $W/reanchor1103.tsv | sort | uniq -c
