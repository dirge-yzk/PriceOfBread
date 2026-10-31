#!/usr/bin/env bash
# (719) 合流：波50 の事前登録の材料の表（読むだけ）。新しい列（cess_s・cessp_s・urxpd_s の wAbsGsym）つきの生 keep_719（base_ins・n1・n2・a11）から
#   cesd §4-2 の門の量（cesd/cess_read.py）・r1 の再測の材料（r1/judge_gate.py の開幕の工場労働者の割合の差・hol/an_h2.py の空洞化の件数〔旧の名簿と規則の版〕）・
#   urbd の W1abs・W2・P・戻す割合（urb3/base.py）を回し、m719/prereg_base.py で 1 枚の表 measurements/m719_20261031_prereg_base.txt にまとめる。
# 使い方：bash scripts/instruments/m719/prereg_base.sh <生の親フォルダ（base_ins・n1・n2・a11 を含む）>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
K=${1:?生の親フォルダ}; S=scripts/instruments; O=measurements/m719_20261031; mkdir -p $O
python -X utf8 -I $S/urb3/base.py base=$K/base_ins n1=$K/n1 n2=$K/n2 > $O/urb3_base_3arms.txt 2>&1
python -X utf8 -I $S/urb3/base.py a11=$K/a11 --seeds 11-20 > $O/urb3_base_a11.txt 2>&1
for a in base_ins:1 n1:1 n2:1 a11:11; do d=${a%%:*}; s=${a#*:}
  python -X utf8 -I $S/cesd/cess_read.py $O/cess_$d.tsv $K/$d > $O/cess_$d.txt 2>&1
  python -X utf8 -I $S/r1/judge_gate.py $K/$d $s 10 > $O/r1gate_$d.txt 2>&1
  python -X utf8 $S/hol/an_h2.py $K/$d -v4 > $O/hol_$d.txt 2>&1
  python -X utf8 $S/hol/an_h2.py $K/$d -v4 -ex rule > $O/holrule_$d.txt 2>&1
done
python -X utf8 -I $S/m719/prereg_base.py $O > measurements/m719_20261031_prereg_base.txt
echo PREREGBASEDONE
