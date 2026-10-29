#!/usr/bin/env bash
# (657) 線 igi3：成績表の報告 2 本を名乗り行の SOURCE-HASH を除いて全行比較する（読むだけ）。
# 使い方：bash cmp_band.sh <報告 A> <報告 B>　　出力：差のある行の数（0 なら全行一致）と最初の差 10 行
export PYTHONUTF8=1
diff <(grep -v 'SOURCE-HASH' "$1") <(grep -v 'SOURCE-HASH' "$2") > /tmp/igi3_cmp_band.$$ ; n=$(grep -c '^[<>]' /tmp/igi3_cmp_band.$$)
echo "diff_lines $n  (A=$(wc -l < "$1") lines, B=$(wc -l < "$2") lines)"; head -n 10 /tmp/igi3_cmp_band.$$; rm -f /tmp/igi3_cmp_band.$$
