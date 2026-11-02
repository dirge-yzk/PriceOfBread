#!/usr/bin/env bash
# (795) 成績表の比べ：腕の b5 と基準 measurements/b5_20261102_m784 の全行（違う行の数と中身）。
BASE=/c/Users/dirge/Documents/PriceOfBread/measurements/b5_20261102_m784
for a in r795a r795b; do
  F=D:/PoB_work/r795_work/b5_${a}_5.txt
  echo "== $a：行 $(wc -l < $F)／基準 $(wc -l < $BASE)・違う行 $(diff $BASE $F | grep -c '^[<>]')"
done
echo "== r795b の違う行（先頭 40）"; diff /c/Users/dirge/Documents/PriceOfBread/measurements/b5_20261102_m784 D:/PoB_work/r795_work/b5_r795b_5.txt | head -40
echo "== r795a の違う行"; diff /c/Users/dirge/Documents/PriceOfBread/measurements/b5_20261102_m784 D:/PoB_work/r795_work/b5_r795a_5.txt | head -20
