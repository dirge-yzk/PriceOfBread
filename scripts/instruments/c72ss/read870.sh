#!/usr/bin/env bash
# (870) c72ss 腕 S11 の読み（読むだけ・事前登録 measurements/m870_20261106_prereg_ss.txt の [5]〜[6] と追記のまま）
#   bash read870.sh <腕の出力フォルダ（ins を持つ・D:/ の形）> <読みの出力フォルダ（D:/ の形）>
#   止める線＝stoplines866.sh（種の起点 11）・空洞化の振り分け＝holclass839.py（基準＝keep_852/a11 を同じ台本で数え直した stop_a11_852.txt・並べて measurements/m852_20261105/armS11/stop_mS.txt）
#   記録＝門（gates_s11_870.py・近似）・NG（ng.sh・基準 rd:measurements/ng852_20261105:m852）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
A=${1:?腕の出力}; O=${2:?出力}; R=D:/PoB_work/wt_c72ss; S=$R/scripts/instruments
mkdir -p $O
bash $S/m866/stoplines866.sh $A/ins $O/s11_stop.txt 11 > /dev/null
python -X utf8 -I $S/m839/holclass839.py $O/s11_stop.txt D:/PoB_work/c72ss_work/read/stop_a11_852.txt > $O/s11_holclass.txt 2>&1
python -X utf8 -I $S/m839/holclass839.py $O/s11_stop.txt $R/measurements/m852_20261105/armS11/stop_mS.txt > $O/s11_holclass_mS.txt 2>&1
python -X utf8 -I $S/c72ss/gates_s11_870.py $A/ins D:/PoB_work/keep_852/a11 $O/view_s11 $O/s11_gates.txt > /dev/null 2> $O/s11_gates.err
(cd $R && bash $S/ng.sh $A/ins c72ssS $O/ng rd:measurements/ng852_20261105:m852 > $O/s11_ng.txt 2>&1)
echo READDONE
