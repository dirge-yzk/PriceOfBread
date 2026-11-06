#!/usr/bin/env bash
# (920) c80eb ヌル腕の幅の取り直し（読むだけ）：基線 m919 と新しいヌル腕 n1・n2（生は keep_919）で、G1′ の分けと続きの終わり方を出す。
#   bash scripts/instruments/c80eb/null920.sh <出力 txt>   （run_in_background で）
OUT="$1"; D=D:/PoB_work/wt_c80eb/scripts/instruments/c80eb; : > "$OUT"
for arm in base_ins n1 n2; do
  case $arm in base_ins) tag=m919;; n1) tag=m919_n1;; n2) tag=m919_n2;; esac
  ins=D:/PoB_work/keep_919/$arm; [ -d "$ins/ins" ] && ins="$ins/ins"
  echo "=== $tag ($ins)" >> "$OUT"
  python -X utf8 -I "$D/read920_carry.py" "$tag" "$ins" >> "$OUT" 2>&1
  python -X utf8 -I "$D/cont920.py" "$tag" "$ins" >> "$OUT" 2>&1
done
