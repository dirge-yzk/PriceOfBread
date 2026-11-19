#!/usr/bin/env bash
# (1232) の紙の台本を今の基線 m1233 の生で回し直す。使い方: bash run_prev.sh <INSRAW> <種の下> <種の上> <出力フォルダ>
I=$1; LO=$2; HI=$3; O=$4; S=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments/r1232; SC=C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore
python -X utf8 -I $S/paper_f.py $I $SC/places.json $SC/world.json $LO $HI $O/out_f_m1233.txt || exit 3
python -X utf8 -I $S/paper_p.py $I $LO $HI $O/out_p_m1233.txt || exit 2
python -X utf8 -I $S/paper_p2.py $I $LO $HI $O/out_p2_m1233.txt || exit 4
echo done > $O/run_prev.done
