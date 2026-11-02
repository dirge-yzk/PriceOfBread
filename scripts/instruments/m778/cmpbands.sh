#!/usr/bin/env bash
# (778) 合流の予言 §3・§8 の成績表の突き合わせ（読むだけ・名乗り行の SOURCE-HASH を除く）
cd D:/PoB_work/wt_m778; W=D:/PoB_work/m778
# CR は除く（線の写しに CRLF の混ざった物がある＝(776) の記録 12）
c(){ if diff <(tr -d '\r' < "$1" | grep -v 'SOURCE-HASH') <(tr -d '\r' < "$2" | grep -v 'SOURCE-HASH') > /dev/null; then echo "一致 $1 ≡ $2"; else echo "不一致 $1 ≠ $2"; diff <(tr -d '\r' < "$1" | grep -v 'SOURCE-HASH') <(tr -d '\r' < "$2" | grep -v 'SOURCE-HASH') | head -20; fi; }
cc(){ a=$1; b=$2; if diff <(tr -d '\r' < $a | sed -n '/名前/,/^$/p' | cut -f1,2) <(tr -d '\r' < $b | sed -n '/名前/,/^$/p' | cut -f1,2) > /dev/null; then echo "cost 件数の行 一致 $a ≡ $b"; else echo "cost 件数の行 不一致 $a ≠ $b"; diff <(tr -d '\r' < $a | sed -n '/名前/,/^$/p' | cut -f1,2) <(tr -d '\r' < $b | sed -n '/名前/,/^$/p' | cut -f1,2) | head; fi; }
[ -f $W/b10_mZ_10.txt ] && { c $W/b10_mZ_10.txt measurements/b10_20261102_c7hlB_ins; cc $W/b10_mZ_10.txt.cost measurements/b10_20261102_c7hlB_ins.cost; }
[ -f $W/b10_mP_10.txt ] && c $W/b10_mP_10.txt $W/b10_mB_10.txt
[ -f $W/b10_mG_10.txt ] && { c $W/b10_mG_10.txt measurements/b10_20261102_m773_ins; cc $W/b10_mG_10.txt.cost measurements/b10_20261102_m773_ins.cost; }
for f in $W/b*_m*.txt $W/b*_n[12]_10.txt; do [ -f $f.cost ] && echo "$(basename $f) $(grep '推定の計算量' $f.cost | head -1)"; done
