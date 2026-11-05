#!/usr/bin/env bash
# (848) c69eh 腕 B の読み手（読むだけ・事前登録 measurements/m848_20261105_prereg_eh.txt §4・§8）。
#   使い方: bash postB848.sh <出力フォルダ（D:/ の形）> [腕の作業フォルダ]
#   G1＝paper848.py（cpop_s の断面）・G2＝gate848.py（cearn_s・cpop_s の末尾の列）・止める線＝stoplines848.sh（基線とヌル腕は stopnull848.sh で同じ台本）・
#   空洞化の振り分け＝holclass839.py（基線 keep_845/base_ins の数え直しと比べる）・門 A′＝nullgate.py（記録）・NG＝ng.sh（記録・基準 rd:measurements/ng839_20261105:m839）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
R=D:/PoB_work/wt_c69eh; S=$R/scripts/instruments; W=${2:-D:/PoB_work/c69eh_armB}; I=$W/out_c69ehB_10/ins   # 2 番目の引数＝腕の作業フォルダ（B2 は D:/PoB_work/c69eh_armB2）
O=${1:?出力フォルダ}; mkdir -p $O
cd $R
python -I $S/c69eh/paper848.py $O/g1_paper848_armB.txt armB:$I > /dev/null
python -I $S/c69eh/gate848.py $O/g2_gate848_armB.txt $I 10 > /dev/null
bash $S/c69eh/stoplines848.sh $I $O/stop_armB.txt
python -X utf8 -I $S/m839/holclass839.py $O/stop_armB.txt $R/measurements/m848_20261105/stop_null/stop_base.txt > $O/holclass_armB.txt 2>&1
B10=$(ls $W/b10_c69ehB_10*.txt 2>/dev/null | grep -v "\.cost\|\.manifest" | head -1)
python -I $S/tools/nullgate.py $B10 measurements/b10_20261105_m839_ins measurements/b10_20261105_m839_n1_ins measurements/b10_20261105_m839_n2_ins > $O/nullgate_armB.txt 2>&1
bash $S/ng.sh $I c69ehB $W/ng rd:measurements/ng839_20261105:m839 > $O/ng_armB.txt 2>&1
{ echo "== .cost（腕 B と基線 m839 の計器つき 10 種）"; grep -h "推定" $B10.cost measurements/b10_20261105_m839_ins.cost; } > $O/cost_armB.txt 2>&1
echo POSTDONE
