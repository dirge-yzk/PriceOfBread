#!/usr/bin/env bash
I=D:/PoB_work/keep_1226/base_ins; S=D:/PoB_work/r1232_work/scripts; O=D:/PoB_work/r1232_work; SC=C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore
python -X utf8 -I $S/paper_rent.py $I 1 10 $O/out_rent.txt || exit 1
python -X utf8 -I $S/paper_p.py $I 1 10 $O/out_p.txt || exit 2
python -X utf8 -I $S/paper_f.py $I $SC/places.json $SC/world.json 1 10 $O/out_f.txt || exit 3
python -X utf8 -I $S/paper_p2.py $I 1 10 $O/out_p2.txt || exit 4
