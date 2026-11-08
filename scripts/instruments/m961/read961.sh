#!/usr/bin/env bash
# (961) 合流の腕の読み（読むだけ）。kp の門は c84kp/paper952.py、lt の門は c85lt/gate958.py を 1 字も変えずに呼ぶ（線の reads957.sh・read958.sh と同じ引数）。
#   止める線は stoplines949（RULE=pair・4 区分）を 2 通りの NULLS（m947 の n1・n2 と、この合流の新しい N1・N2）で数える・空洞化 an_h2 -v4・名目 GDP 0 の続き・NG（SB=1 だけ）。
#   bash read961.sh <タグ> <腕の out フォルダ（D:/ の形・中に ins）> <成績表（D:/ の形）> <種の起点 1|11> [newnulls]
#   出力＝D:/PoB_work/m961_work/read/<タグ>/。newnulls を付けると新しい N1・N2 の stop906 の出力（read/n1/stop906.txt・read/n2/stop906.txt）でも数える。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m961; cd $T || exit 9; S=scripts/instruments
TAG=${1:?タグ}; O=${2:?out}; B=${3:?成績表}; SB=${4:-1}; NN=${5:-}
R=D:/PoB_work/m961_work/read/$TAG; mkdir -p $R; I=$O/ins; K=D:/PoB_work/keep_947
if [ "$SB" = 11 ]; then BASE=$K/a11; else BASE=D:/PoB_work/keep_954/base_ins; fi
if [ "$SB" = 1 ]; then
  python -X utf8 -I $S/c84kp/paper952.py arm=$I base=$K/base_ins n1=$K/n1 n2=$K/n2 > $R/kp_gates.txt 2>&1; echo "kp rc=$?"
  python -X utf8 -I $S/tools/nullgate.py $B measurements/b10_20261108_m947_ins measurements/b10_20261108_m947_n1_ins measurements/b10_20261108_m947_n2_ins > $R/nullgate.txt 2>&1; echo "nullgate rc=$?"
  python -X utf8 -I $S/brk693/home_break.py arm=$I base=$BASE n1=$K/n1 n2=$K/n2 --test arm > $R/home_break.txt 2>&1; echo "home rc=$?"
else
  python -X utf8 -I $S/c84kp/paper952.py arm=$I a11=$K/a11 > $R/kp_gates.txt 2>&1; echo "kp rc=$?"
  python -X utf8 -I $S/brk693/home_break.py arm=$I a11=$K/a11 --test arm > $R/home_break.txt 2>&1; echo "home rc=$?"
fi
if ls $I/landfix_s* > /dev/null 2>&1; then   # lt の門は landfix_s のある木だけ（(961) で lt を外した後の木には表が無い）
  python -X utf8 $S/c85lt/gate958.py $I $SB > $R/lt_gate.txt 2>&1; echo "lt rc=$?"
  python -X utf8 $S/c82lt/tab937.py $I $BASE $SB $SB > $R/lt_tab.txt 2>&1; echo "tab rc=$?"
  python -X utf8 $S/c84lb/paper951.py $I $I > $R/lt_g2_new.txt 2>&1; echo "g2new rc=$?"
fi
bash $S/c78gz/stoplines906.sh $I $R/stop906.txt $SB > $R/stop906.log 2>&1; echo "stop906 rc=$?"
RULE=pair NULLS="measurements/m947_20261108/null/stop906_n1.txt measurements/m947_20261108/null/stop906_n2.txt" bash $S/c84gz/stoplines949.sh $I $R/stop949_m947nulls.txt $SB > $R/stop949_m947nulls.log 2>&1; echo "stop949 rc=$?"
if [ "$NN" = newnulls ]; then
  RULE=pair NULLS="D:/PoB_work/m961_work/read/n1/stop906.txt D:/PoB_work/m961_work/read/n2/stop906.txt" bash $S/c84gz/stoplines949.sh $I $R/stop949_newnulls.txt $SB > $R/stop949_newnulls.log 2>&1; echo "stop949new rc=$?"
fi
python -X utf8 $S/hol/an_h2.py $I -v4 > $R/hol_v4.txt 2>&1; echo "hol rc=$?"
python -X utf8 -I $S/m808/gdp_zero10.py arm=$I base=$BASE > $R/gdp_zero10.txt 2>&1; echo "gdpz rc=$?"
python -X utf8 -I $S/tools/keepsum.py $I $R/keepsum.tsv > $R/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$SB" = 1 ]; then bash $S/ng.sh $I $TAG D:/PoB_work/m961_work/ng_$TAG rd:measurements/ng947_20261108:m947 > $R/ng.txt 2>&1; echo "ng rc=$?"; fi
echo READDONE
