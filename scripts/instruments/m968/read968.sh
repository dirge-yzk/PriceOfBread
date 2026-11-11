#!/usr/bin/env bash
# (968) 合流の腕の読み（読むだけ）。lt の門は c86lt/gate964.py・tab937・sup945・paper951・why945 を c86lt/read964.sh と同じ引数で 1 字も変えずに呼ぶ。
#   止める線は stoplines906 の後に stoplines949（RULE=pair・4 区分）を 2 通りの NULLS（m961 の n1・n2 と、この合流の新しい N1・N2）で数える・空洞化 an_h2 -v4・名目 GDP 0 の続き・NG（SB=1 だけ）（read961.sh の型）。
#   bash read968.sh <タグ> <腕の out フォルダ（D:/ の形・中に ins）> <成績表（D:/ の形）> <種の起点 1|11> [newnulls] [nogate]
#   出力＝D:/PoB_work/m968_work/read/<タグ>/。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m968; cd $T || exit 9; S=scripts/instruments
TAG=${1:?タグ}; O=${2:?out}; B=${3:?成績表}; SB=${4:-1}; NN=${5:-}; NG=${6:-}
W=${M968W:-D:/PoB_work/m968_work}; R=$W/read/$TAG; mkdir -p $R; I=$O/ins; K=D:/PoB_work/keep_961
if [ "$SB" = 11 ]; then BASE=$K/a11; else BASE=$K/base_ins; fi
[ -d "$I" ] || { echo "ins が無い：$I"; exit 2; }
if [ "$NG" != nogate ]; then
  python -X utf8 $S/c86lt/gate964.py $I $SB > $R/gate.txt 2>&1; echo "gate rc=$?"
  python -X utf8 $S/c82lt/tab937.py $I $BASE $SB $SB > $R/tab.txt 2>&1; echo "tab rc=$?"
  python -X utf8 $S/c83lt/sup945.py $I $BASE > $R/sup.txt 2>&1; echo "sup rc=$?"
  python -X utf8 $S/c84lb/paper951.py $I $I > $R/g2_new.txt 2>&1; echo "g2new rc=$?"
  python -X utf8 $S/c83lt/why945.py $I > $R/g2_old_why.txt 2>&1; echo "why rc=$?"
fi
bash $S/c78gz/stoplines906.sh $I $R/stop906.txt $SB > $R/stop906.log 2>&1; echo "stop906 rc=$?"
RULE=pair NULLS="measurements/m961_20261108/null/stop906_n1.txt measurements/m961_20261108/null/stop906_n2.txt" bash $S/c84gz/stoplines949.sh $I $R/stop949_m961nulls.txt $SB > $R/stop949_m961nulls.log 2>&1; echo "stop949 rc=$?"
if [ "$NN" = newnulls ]; then
  RULE=pair NULLS="$W/read/n1/stop906.txt $W/read/n2/stop906.txt" bash $S/c84gz/stoplines949.sh $I $R/stop949_newnulls.txt $SB > $R/stop949_newnulls.log 2>&1; echo "stop949new rc=$?"
fi
python -X utf8 $S/hol/an_h2.py $I -v4 > $R/hol_v4.txt 2>&1; echo "hol rc=$?"
python -X utf8 -I $S/m808/gdp_zero10.py arm=$I base=$BASE > $R/gdp_zero10.txt 2>&1; echo "gdpz rc=$?"
python -X utf8 -I $S/tools/keepsum.py $I $R/keepsum.tsv > $R/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$SB" = 1 ]; then
  python -X utf8 $S/tools/nullgate.py $B measurements/b10_20261108_m961_ins measurements/b10_20261108_m961_n1_ins measurements/b10_20261108_m961_n2_ins > $R/nullgate.txt 2>&1; echo "nullgate rc=$?"
  python -X utf8 -I $S/brk693/home_break.py arm=$I base=$BASE n1=$K/n1 n2=$K/n2 --test arm > $R/home_break.txt 2>&1; echo "home rc=$?"
  bash $S/ng.sh $I $TAG $W/ng_$TAG rd:measurements/ng961_20261108:m961 > $R/ng.txt 2>&1; echo "ng rc=$?"
fi
echo READDONE
