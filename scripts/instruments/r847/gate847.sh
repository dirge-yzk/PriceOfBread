#!/usr/bin/env bash
# (847)(853) c69hl 恒等の門 1 の判定：run847.sh の出力を measurements/m840_20261105/ の写しとバイトで比べる（an は名ごと・比の表は同じ名の行の並び）。
#   bash gate847.sh <run847.sh の出力フォルダ> [名 ...]  ＝一致・不一致を名ごとに 1 行ずつ出し、全て一致なら rc=0。
export PYTHONUTF8=1
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
R=$(cd "$SD/../../.." && pwd); REF=${R847_REF:-$R/measurements/m840_20261105}
O=${1:?run847.sh の出力フォルダ}; shift
NAMES="${*:-$(ls "$O/an" | sed 's/\.txt$//')}"
bad=0
for n in $NAMES; do
  if cmp -s "$O/an/$n.txt" "$REF/an/$n.txt"; then a=一致; else a=不一致; bad=$((bad+1)); fi
  awk -F'\t' -v n="$n" '$1==n' "$O/ratios.tsv" > "$O/.r_new.$n"; awk -F'\t' -v n="$n" '$1==n' "$REF/ratios.tsv" > "$O/.r_ref.$n"
  if cmp -s "$O/.r_new.$n" "$O/.r_ref.$n"; then r=一致; else r=不一致; bad=$((bad+1)); fi
  rm -f "$O/.r_new.$n" "$O/.r_ref.$n"
  echo -e "$n\tan $a\t比の表 $r"
done
echo "## 恒等の門 1：不一致 $bad"
[ $bad -eq 0 ]
