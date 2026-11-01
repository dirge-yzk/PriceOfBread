#!/usr/bin/env bash
# (727) 合流：波51 の事前登録の材料の表（読むだけ）。(719) の m719/prereg_base.sh の写しに §4（rdh の結論が名指した観測量＝州の間の移民〔国内〕のうち 3,000 km 超の割合と量・rdh/far_share.py）を足した。
#   生＝新しい列つきの keep_727（base_ins・n1・n2・a11）。空洞化は旧の名簿（-ex old）と規則 A2（-ex rule・hol3 の world.json の popK で判定）を両方とも明示して呼ぶ（既定に頼らない）。
# 使い方：bash scripts/instruments/m727/prereg_base.sh <生の親フォルダ（base_ins・n1・n2・a11 を含む）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
K=${1:?生の親フォルダ}; S=scripts/instruments; O=measurements/m727_20261101; mkdir -p $O
python -X utf8 -I $S/urb3/base.py base=$K/base_ins n1=$K/n1 n2=$K/n2 > $O/urb3_base_3arms.txt 2>&1
python -X utf8 -I $S/urb3/base.py a11=$K/a11 --seeds 11-20 > $O/urb3_base_a11.txt 2>&1
for a in base_ins:1 n1:1 n2:1 a11:11; do d=${a%%:*}; s=${a#*:}
  python -X utf8 -I $S/cesd/cess_read.py $O/cess_$d.tsv $K/$d > $O/cess_$d.txt 2>&1
  python -X utf8 -I $S/r1/judge_gate.py $K/$d $s 10 > $O/r1gate_$d.txt 2>&1
  python -X utf8 $S/hol/an_h2.py $K/$d -v4 -ex old > $O/hol_$d.txt 2>&1
  python -X utf8 $S/hol/an_h2.py $K/$d -v4 -ex rule > $O/holrule_$d.txt 2>&1
  python -X utf8 $S/rdh/far_share.py $K/$d > $O/far_$d.txt 2>&1
done
python -X utf8 -I $S/m727/prereg_base.py $O > measurements/m727_20261101_prereg_base.txt
echo PREREGBASEDONE
