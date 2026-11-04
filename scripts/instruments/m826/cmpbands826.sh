#!/usr/bin/env bash
# (826) 合流の予言 [2] の成績表の突き合わせ（読むだけ・名乗り行の SOURCE-HASH を除く・CR を除く）・(797) の cmpbands797.sh の写し（腕の名だけ m826 へ）
cd D:/PoB_work/wt_m826; W=D:/PoB_work/m826
c(){ if diff <(tr -d '\r' < "$1" | grep -v 'SOURCE-HASH') <(tr -d '\r' < "$2" | grep -v 'SOURCE-HASH') > /dev/null; then echo "一致 $1 ≡ $2"; else echo "不一致 $1 ≠ $2（違う行 $(diff <(tr -d '\r' < "$1" | grep -v 'SOURCE-HASH') <(tr -d '\r' < "$2" | grep -v 'SOURCE-HASH') | grep -c '^[<>]')）"; fi; }
cc(){ a=$1; b=$2; if diff <(tr -d '\r' < $a | sed -n '/名前/,/^$/p' | cut -f1,2) <(tr -d '\r' < $b | sed -n '/名前/,/^$/p' | cut -f1,2) > /dev/null; then echo "cost 件数の行 一致 $a ≡ $b"; else echo "cost 件数の行 不一致 $a ≠ $b"; fi; }
[ -f $W/b10_mOP0_10.txt ] && { c $W/b10_mOP0_10.txt measurements/b10_20261104_c14flB_ins; cc $W/b10_mOP0_10.txt.cost measurements/b10_20261104_c14flB_ins.cost; }
[ -f $W/b10_mFL0_10.txt ] && { c $W/b10_mFL0_10.txt measurements/b10_20261104_c65opB_ins; cc $W/b10_mFL0_10.txt.cost measurements/b10_20261104_c65opB_ins.cost; }
[ -f $W/b10_mB_10.txt ] && { c $W/b10_mB_10.txt measurements/b10_20261104_c14flB_ins; c $W/b10_mB_10.txt measurements/b10_20261104_c65opB_ins; c $W/b10_mB_10.txt measurements/b10_20261103_m797_ins; }
[ -f $W/b10_mP_10.txt ] && { c $W/b10_mP_10.txt $W/b10_mB_10.txt; }
for f in $W/b*_m*.txt $W/b*_n[12]_10.txt; do [ -f $f.cost ] && echo "$(basename $f) $(grep '推定の計算量' $f.cost | head -1)"; done
