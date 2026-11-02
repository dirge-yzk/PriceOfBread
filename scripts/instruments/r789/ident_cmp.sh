#!/usr/bin/env bash
# r789: 計器を足した腕の生の表（r789* を除く）を基準 keep_784/a11 とバイトで比べ、成績表を基準 b10s11 と名乗り行以外で比べる
A=$1; B=$2; SA=$3; SB=$4
n=0; bad=0; miss=0
for f in "$B"/*.tsv; do
  b=$(basename "$f"); n=$((n+1))
  if [ ! -f "$A/$b" ]; then echo "MISSING $b"; miss=$((miss+1)); continue; fi
  cmp -s "$f" "$A/$b" || { echo "DIFF $b"; bad=$((bad+1)); }
done
extra=$(ls "$A" | grep -v '^r789' | while read x; do [ -f "$B/$x" ] || echo "$x"; done | wc -l)
echo "基準の表 $n 本・違う $bad 本・腕に無い $miss 本・腕だけの表（r789 を除く）$extra 本・腕だけの r789 の表 $(ls "$A" | grep -c '^r789') 本"
if [ -n "$SA" ]; then
  diff <(grep -v -E 'SOURCE-HASH|^条件|^実測|^負荷' "$SB") <(grep -v -E 'SOURCE-HASH|^条件|^実測|^負荷' "$SA") > /dev/null && echo "成績表：名乗り行を除き全行一致（$(wc -l < "$SB") 行）" || { echo "成績表：不一致"; diff <(grep -v SOURCE-HASH "$SB") <(grep -v SOURCE-HASH "$SA") | head -20; }
fi
