#!/usr/bin/env bash
S=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments/r1402; O=D:/PoB_work/r1402_work/out
python -X utf8 -I $S/bham.py M D:/PoB_work/keep_1397/base_ins 1 $O
python -X utf8 -I $S/bham.py N1 D:/PoB_work/keep_1383/n1 1 $O
python -X utf8 -I $S/bham.py N2 D:/PoB_work/keep_1383/n2 1 $O
python -X utf8 -I $S/bham.py S11 D:/PoB_work/keep_1383/a11 11 $O
