#!/usr/bin/env bash
# (1381) 読む線：q3_chain.py を 40 本（M・N1・N2 種 1〜10・S11 種 11〜20）に回す。使い方 bash q3_chain_all.sh <出力の tsv> <q3_sink_table.tsv>
O="${1:?}"; ST="${2:?}"; H=/c/Users/dirge/Documents/PriceOfBread/scripts/instruments/r1381
python $H/q3_chain.py --header > "$O"
for s in 1 2 3 4 5 6 7 8 9 10; do
  python $H/q3_chain.py /d/PoB_work/keep_1376/base_ins $s M "$ST" >> "$O" || exit 1
  python $H/q3_chain.py /d/PoB_work/keep_1362/n1 $s N1 "$ST" >> "$O" || exit 1
  python $H/q3_chain.py /d/PoB_work/keep_1362/n2 $s N2 "$ST" >> "$O" || exit 1
done
for s in 11 12 13 14 15 16 17 18 19 20; do python $H/q3_chain.py /d/PoB_work/keep_1362/a11 $s S11 "$ST" >> "$O" || exit 1; done
