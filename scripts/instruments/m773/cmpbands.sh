#!/usr/bin/env bash
# (773) 合流の予言 §3 の成績表の突き合わせ（読むだけ）
cd D:/PoB_work/wt_m773; W=D:/PoB_work/m773
c(){ if cmp -s "$1" "$2"; then echo "一致 $1 ≡ $2"; else echo "不一致 $1 ≠ $2"; diff "$1" "$2" | head -20; fi; }
c $W/b10_mB_10.txt measurements/b10_20261102_c6prB_ins
c $W/b10_mS_10_s11.txt measurements/b10s11_20261102_c6prB_ins
c $W/b10_mP_10.txt $W/b10_mB_10.txt
for t in mB_10 mS_10_s11; do
  a=$W/b10_$t.txt.cost; b=measurements/b10_20261102_c6prB_ins.cost; [ $t = mS_10_s11 ] && b=measurements/b10s11_20261102_c6prB_ins.cost
  if diff <(sed -n '/名前/,/^$/p' $a | cut -f1,2) <(sed -n '/名前/,/^$/p' $b | cut -f1,2) > /dev/null; then echo "cost 件数の行 一致 $t"; else echo "cost 件数の行 不一致 $t"; diff <(sed -n '/名前/,/^$/p' $a | cut -f1,2) <(sed -n '/名前/,/^$/p' $b | cut -f1,2) | head; fi
  grep "推定の計算量" $a
done
for t in n1_10 n2_10 mP5_5; do f=$W/b*_$t.txt; grep "推定の計算量" $f.cost; done
