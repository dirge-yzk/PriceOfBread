#!/usr/bin/env bash
# (855) c70ss 腕 B の読み（読むだけ・事前登録 measurements/m855_20261105_prereg_ss.txt の [8]〜[10] のまま）：門・止める線・空洞化の振り分け・門 A′（記録）・NG（記録）
#   bash read855.sh <腕の出力フォルダ（ins を持つ・D:/ の形）> <腕の成績表（measurements/b10_…）> <読みの出力フォルダ（D:/ の形）> [<木の根>=D:/PoB_work/wt_c70ss]
#   比べる側は m852（keep_852/base_ins）と新しいヌル腕（keep_852/n1・n2）・空洞化の〈元からの弱さ〉の基準は measurements/m852_20261105/armM/stop_mM.txt。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
A=${1:?腕の出力}; BAND=${2:?成績表}; O=${3:?出力}; R=${4:-D:/PoB_work/wt_c70ss}; S=$R/scripts/instruments
mkdir -p $O
B=D:/PoB_work/keep_852/base_ins; N1=D:/PoB_work/keep_852/n1; N2=D:/PoB_work/keep_852/n2
python -X utf8 $S/c70ss/gates_ss853.py $A/ins $B $N1 $N2 $O/armB_gates.txt 10 1 > /dev/null 2> $O/armB_gates.err
bash $S/c70ss/stoplines855.sh $A/ins $O/armB_stop.txt 1 > /dev/null
python -X utf8 -I $S/m839/holclass839.py $O/armB_stop.txt $R/measurements/m852_20261105/armM/stop_mM.txt > $O/armB_holclass.txt 2>&1
(cd $R && python $S/tools/nullgate.py $BAND measurements/b10_20261105_m852_ins measurements/b10_20261105_m852_n1_ins measurements/b10_20261105_m852_n2_ins > $O/armB_nullgate.txt 2>&1)
(cd $R && bash $S/ng.sh $A/ins c70ssB $O/ng rd:measurements/ng852_20261105:m852 > $O/armB_ng.txt 2>&1)
echo READDONE
