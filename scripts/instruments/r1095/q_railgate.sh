#!/usr/bin/env bash
# 撤回（(1095) material.md の印・(1096) 合流が頭に足した）：この台本の数は政体の名の行で国を取り違えた読み＝使わない
# (1095) 鉄道の門（trnation の railGateFlag）が最初に開く行（種ごと）と、年ごとに門が開いている国の数。使い方: bash q_railgate.sh <表のフォルダ> > out
D="${1:?表のフォルダ}"
echo "種 最初の年 国"
for s in 1 2 3 4 5 6 7 8 9 10; do
  grep -v "^year" "$D/trnation_s$s.tsv" | sort -t$'\t' -k1,1n | awk -F'\t' -v s=$s '$3>0 {print s, $1, $2; exit}'
done
echo "年: 種 1〜10 の門が開いた国の数"
for y in 1800 1810 1820 1830; do
  printf "%s: " "$y"
  for s in 1 2 3 4 5 6 7 8 9 10; do
    awk -F'\t' -v y=$y 'NR>1 && $1==y && $3>0 {n[$2]=1} END{printf "%d ", length(n)}' "$D/trnation_s$s.tsv"
  done
  echo
done
