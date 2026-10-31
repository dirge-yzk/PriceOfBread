#!/usr/bin/env bash
# (711) 合流：波49 の事前登録の材料の表（読むだけ）。新しい列つきの生 keep_711（base_ins・n1・n2・a11）から
#   urbd（urb3/base.py：W1・W2・往復の戻す割合）・U4（u4z/base.py：予算が縛る州×月の割合）・影の N（prd2/shadow_n.py）を回し、
#   m711/prereg_base.py で 1 枚の表 measurements/m711_20261031_prereg_base.txt にまとめる。
# 使い方：bash scripts/instruments/m711/prereg_base.sh <生の親フォルダ（base_ins・n1・n2・a11 を含む）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
K=${1:?生の親フォルダ}; S=scripts/instruments; O=measurements/m711_20261031; mkdir -p $O
python -X utf8 -I $S/urb3/base.py base=$K/base_ins n1=$K/n1 n2=$K/n2 > $O/urb3_base_3arms.txt 2>&1
python -X utf8 -I $S/urb3/base.py a11=$K/a11 --seeds 11-20 > $O/urb3_base_a11.txt 2>&1
for a in base_ins:1 n1:1 n2:1 a11:11; do d=${a%%:*}; s=${a#*:}; python $S/u4z/base.py $K/$d $O/u4z_$d.txt 10 $s > /dev/null 2>&1; python -X utf8 -I $S/prd2/shadow_n.py $K/$d 10 $d $s > $O/shadowN_$d.txt 2>&1; done
python -X utf8 -I $S/m711/prereg_base.py $O > measurements/m711_20261031_prereg_base.txt
echo PREREGBASEDONE
