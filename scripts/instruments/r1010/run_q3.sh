#!/usr/bin/env bash
# (1010) 今の盤面（m1001＝keep_1006/base_ins）とヌル腕（keep_1001/n1・n2・a11）で m998 の q3（貿易÷GDP）を取り直す。
Q=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments/r998/q3_trade_gdp.py
O=D:/PoB_work/r1010_work
python -X utf8 -I $Q D:/PoB_work/keep_1006/base_ins m1001 $O/q3_m1001.tsv 1-10 || exit 1
python -X utf8 -I $Q D:/PoB_work/keep_1001/n1 N1 $O/q3_N1.tsv 1-10 || exit 1
python -X utf8 -I $Q D:/PoB_work/keep_1001/n2 N2 $O/q3_N2.tsv 1-10 || exit 1
python -X utf8 -I $Q D:/PoB_work/keep_1001/a11 S11 $O/q3_S11.tsv 11-20 || exit 1
