#!/usr/bin/env bash
# (811) c13ia 突然変異の出力（<作業>/M*.txt）を 1 枚にまとめる。使い方: bash mut_collect.sh <作業フォルダ> > measurements/m811_20261103_mutation.txt
W="$1"
echo "(811) c13ia 突然変異（焼き込み後のソースに当てた使い捨ての木・台本 scripts/instruments/c13ia/mut_run.sh・mut_apply.py）"
for f in $(ls "$W"/M*.txt | sort -V); do
  m=$(basename "$f" .txt)
  echo "== $m：$(sed -n 1p "$f" | cut -c1-120)"
  grep -E "^rc=" "$f" | cut -c1-80
  grep -E "成功!|失敗!" "$f" | sed 's/^/  /'
  grep -E "^\s+失敗 " "$f" | sed 's/^/  /' | head -12
done
