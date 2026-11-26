#!/usr/bin/env bash
# (1381) 読む線：q3_sink.py を M（keep_1376/base_ins 種 1〜10）・N1・N2（keep_1362/n1・n2 種 1〜10）・S11（keep_1362/a11 種 11〜20）に回し、q1_deadcap.py を M の種 1〜10 に回す。使い方 bash q3_all.sh <出力フォルダ>
O="${1:?出力}"; H=/c/Users/dirge/Documents/PriceOfBread/scripts/instruments/r1381; mkdir -p "$O"
R=バーミンガム,ロンドン,マンチェスター
for s in 1 2 3 4 5 6 7 8 9 10; do
  python $H/q3_sink.py /d/PoB_work/keep_1376/base_ins $s "$O/sink_M$s.tsv" $R || exit 1
  python $H/q1_deadcap.py /d/PoB_work/keep_1376/base_ins $s バーミンガム "$O/deadcap_M$s.tsv" || exit 1
  python $H/q3_sink.py /d/PoB_work/keep_1362/n1 $s "$O/sink_N1_$s.tsv" $R || exit 1
  python $H/q3_sink.py /d/PoB_work/keep_1362/n2 $s "$O/sink_N2_$s.tsv" $R || exit 1
done
for s in 11 12 13 14 15 16 17 18 19 20; do python $H/q3_sink.py /d/PoB_work/keep_1362/a11 $s "$O/sink_S11_$s.tsv" $R || exit 1; done
