#!/usr/bin/env bash
# (1024) c96kf 突然変異 M5（csc_s の鍵に格を足す）：使い捨ての木へ適用→1 種の計器つき腕→基線の csc_s と比べる（ident と同じ道具の 1 種版）。使い方：bash mut5.sh <使い捨ての木> <作業フォルダ> <出力の印>
T="$1"; W="$2"; O="$3"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
IS="$(cd "$SP/.." && pwd)"
mkdir -p "$W"
(cd "$T" && git checkout -q -- . && python "$SP/mut.py" "$T" M5) || exit 1
export POB_WORK="$W" POB_REPO="$T" MEASURE_CACHE=0
bash "$IS/batch.sh" prep m5 "dir:$T" plain && INS=1 bash "$IS/batch.sh" run m5 1
(cd "$T" && git diff > "$O.diff.txt"; git checkout -q -- .)
OUTD=$(ls -d "$W"/out_m5*/ins 2>/dev/null | head -1)
{
  echo "腕の出力: $OUTD"
  for f in csc_s1.tsv cscx_s1.tsv; do
    if cmp -s "$OUTD/$f" "D:/PoB_work/keep_1022/base_ins/$f"; then echo "$f: 基線と一致"; else echo "$f: 基線と不一致（または基線に無い）"; fi
  done
  head -3 "$OUTD/csc_s1.tsv" | cut -c1-120
  head -3 "D:/PoB_work/keep_1022/base_ins/csc_s1.tsv" | cut -c1-120
} > "$O.txt" 2>&1
