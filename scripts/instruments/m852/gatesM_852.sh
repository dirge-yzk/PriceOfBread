#!/usr/bin/env bash
# (852) 合流：合わせた木の M（種 1〜10）と S11（種 11〜20）で線 c69eh の門（事前登録 §4＝G1 paper848.py・G2 gate848.py）と止める線（§8＝stoplines848.sh・holclass839.py）を線の台本のまま読む（読むだけ）。
#   bash gatesM_852.sh <名> <ins> <種の起点 1|11> <出力フォルダ>
#   種 11〜20 は線の門の台本が種 1〜10 の名（cearn_s1〜10・cpop_s1〜10）だけを読む＝ハードリンクで名を付け替えた読み用のフォルダを作って読む（中身は同じ・D: を使わない）。
#   空洞化の〈元からの弱さ〉の基準＝種 1〜10 は線の stop_base.txt（keep_845/base_ins の数え直し）・種 11〜20 は keep_839/a11 を同じ台本で数え直した物。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
N=${1:?名}; I=${2:?ins}; SB=${3:?起点}; O=${4:?出力}; mkdir -p $O
R=D:/PoB_work/wt_m852; S=$R/scripts/instruments
G=$I
if [ "$SB" = 11 ]; then
  G=D:/PoB_work/m852/rd_$N; rm -rf $G; mkdir -p $G
  for t in cearn_s cpop_s; do for k in 1 2 3 4 5 6 7 8 9 10; do ln "$I/${t}$((k+10)).tsv" "$G/${t}${k}.tsv" || exit 1; done; done
fi
python -I $S/c69eh/paper848.py $O/g1_paper848_$N.txt $N:$G > /dev/null
python -I $S/c69eh/gate848.py $O/g2_gate848_$N.txt $G 10 > /dev/null
bash $S/c69eh/stoplines848.sh $I $O/stop_$N.txt $SB > /dev/null
if [ "$SB" = 11 ]; then
  [ -f $O/stop_a11_839.txt ] || bash $S/c69eh/stoplines848.sh D:/PoB_work/keep_839/a11 $O/stop_a11_839.txt 11 > /dev/null
  python -X utf8 -I $S/m839/holclass839.py $O/stop_$N.txt $O/stop_a11_839.txt > $O/holclass_$N.txt 2>&1
  rm -rf $G
else
  python -X utf8 -I $S/m839/holclass839.py $O/stop_$N.txt $R/measurements/m848_20261105/stop_null/stop_base.txt > $O/holclass_$N.txt 2>&1
fi
echo GATESDONE
