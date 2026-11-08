#!/usr/bin/env bash
# (981) 合流の腕の読み（c88cs の read978.sh と m975 の read975.sh の写し）：門は c88cs/gates978.py（種 1〜10）と gates978_s11.py（種 11〜20・SEEDS だけ差し替え）・基準は keep_975・NULLS は m975 の n1・n2 と、この合流の新しい N1・N2（newnulls）。
#   bash read981.sh <タグ> <腕の out フォルダ（D:/ の形・中に ins）> <成績表（D:/ の形）> <種の起点 1|11> [newnulls] [nogate]
#   出力＝D:/PoB_work/m981_work/read/<タグ>/。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m981; cd $T || exit 9; S=scripts/instruments
TAG=${1:?タグ}; O=${2:?out}; B=${3:?成績表}; SB=${4:-1}; NN=${5:-}; NG=${6:-}
W=${M981W:-D:/PoB_work/m981_work}; R=$W/read/$TAG; mkdir -p $R; I=$O/ins; K=D:/PoB_work/keep_975
if [ "$SB" = 11 ]; then BASE=$K/a11; else BASE=$K/base_ins; fi
[ -d "$I" ] || { echo "ins が無い：$I"; exit 2; }
if [ "$NG" != nogate ]; then
  if [ "$SB" = 1 ]; then python -X utf8 -I $S/c88cs/gates978.py gates $I $R/gates.txt $BASE > $R/gates.log 2>&1; echo "gates rc=$?"
  else python -X utf8 -I $S/m981/gates978_s11.py $I $R/gates.txt $BASE > $R/gates.log 2>&1; echo "gates rc=$?"; fi
fi
bash $S/c78gz/stoplines906.sh $I $R/stop906.txt $SB > $R/stop906.log 2>&1; echo "stop906 rc=$?"
RULE=pair NULLS="measurements/m975_20261109/null/stop906_n1.txt measurements/m975_20261109/null/stop906_n2.txt" bash $S/c84gz/stoplines949.sh $I $R/stop949_m975nulls.txt $SB > $R/stop949_m975nulls.log 2>&1; echo "stop949 rc=$?"
if [ "$NN" = newnulls ]; then
  RULE=pair NULLS="$W/read/n1/stop906.txt $W/read/n2/stop906.txt" bash $S/c84gz/stoplines949.sh $I $R/stop949_newnulls.txt $SB > $R/stop949_newnulls.log 2>&1; echo "stop949new rc=$?"
fi
python -X utf8 $S/hol/an_h2.py $I -v4 > $R/hol_v4.txt 2>&1; echo "hol rc=$?"
python -X utf8 -I $S/m808/gdp_zero10.py arm=$I base=$BASE > $R/gdp_zero10.txt 2>&1; echo "gdpz rc=$?"
python -X utf8 -I $S/brk693/plvl_seeds.py $I > $R/plvl.txt 2>&1; echo "plvl rc=$?"
python -X utf8 -I $S/m975/ukdef975.py $I $SB > $R/ukdef.txt 2>&1; echo "ukdef rc=$?"
python -X utf8 -I $S/tools/keepsum.py $I $R/keepsum.tsv > $R/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$SB" = 1 ]; then
  python -X utf8 $S/tools/nullgate.py $B measurements/b10_20261109_m975_ins measurements/b10_20261109_m975_n1_ins measurements/b10_20261109_m975_n2_ins > $R/nullgate.txt 2>&1; echo "nullgate rc=$?"
  python -X utf8 -I $S/brk693/home_break.py arm=$I base=$BASE n1=$K/n1 n2=$K/n2 --test arm > $R/home_break.txt 2>&1; echo "home rc=$?"
  bash $S/ng.sh $I $TAG $W/ng_$TAG rd:measurements/ng975_20261109:m975 > $R/ng.txt 2>&1; echo "ng rc=$?"
fi
echo READDONE
