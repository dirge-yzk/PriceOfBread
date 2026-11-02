#!/usr/bin/env bash
# (784) 注視の表の MONROW を項目×区分（先頭／注視／記録）で数える（読むだけ）。bash monsum.sh <mon の .txt>...
for f in "$@"; do
  awk -F'\t' -v F="$(basename $f)" '$1=="MONROW"{c[$2"\t"$5]++; t[$5]++} END{s=F; for(i=1;i<=9;i++){s=s" 項目"i":"(c[i"\t先頭"]+0)"/"(c[i"\t注視"]+0)"/"(c[i"\t記録"]+0)}; print s" 計 "(t["先頭"]+0)"/"(t["注視"]+0)"/"(t["記録"]+0)}' "$f"
done
