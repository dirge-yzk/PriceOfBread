#!/usr/bin/env bash
# r794: プローブの木の計器の生（r794* を除く）を基準 keep_790/base_ins とバイトで比べ、成績表を基準と名乗り行以外で比べる。引数：<腕の ins> <基準の ins> <腕の成績表> <基準の成績表>
A=$1; B=$2; SA=$3; SB=$4
n=0; bad=0; miss=0
for f in "$B"/*.tsv; do
  b=$(basename "$f"); n=$((n+1))
  if [ ! -f "$A/$b" ]; then echo "MISSING $b"; miss=$((miss+1)); continue; fi
  cmp -s "$f" "$A/$b" || { echo "DIFF $b"; bad=$((bad+1)); }
done
extra=$(ls "$A" | grep -v '^r794' | while read x; do [ -f "$B/$x" ] || echo "$x"; done | wc -l)
echo "基準の表 $n 本・違う $bad 本・腕に無い $miss 本・腕だけの表（r794 を除く）$extra 本・r794 の表 $(ls "$A" | grep -c '^r794') 本"
diff <(grep -v -E 'SOURCE-HASH|^条件|^実測|^負荷' "$SB") <(grep -v -E 'SOURCE-HASH|^条件|^実測|^負荷' "$SA") > /dev/null && echo "成績表：名乗り行を除き全行一致（$(wc -l < "$SB") 行）" || { echo "成績表：不一致"; diff <(grep -v -E 'SOURCE-HASH|^条件|^実測|^負荷' "$SB") <(grep -v -E 'SOURCE-HASH|^条件|^実測|^負荷' "$SA") | head -20; }
