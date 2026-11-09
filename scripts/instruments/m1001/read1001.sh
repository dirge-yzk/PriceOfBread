#!/usr/bin/env bash
# (1001) 合流の腕の読み（m988/read988.sh と c91sl/read997.sh の写し）：門は c91sl/gates997.py（事前登録 measurements/m997_20261109_prereg_sl.txt [5] のまま・1 字も変えない）。
#   種 1〜10 の基準は keep_994/base_ins・種 11〜20 は keep_988/a11（どちらも m988 の盤面）。
#   止める線は stoplines949（RULE=pair）を m988 のヌル腕（measurements/m988_20261109/null）と、この合流の新しいヌル腕（newnulls）の両方で数える。
#   c91rx の stoplines996（州の出入り）は記録の列だけ（採否に使わない）。
#   bash read1001.sh <タグ> <腕の out フォルダ（D:/ の形・中に ins）> <成績表（D:/ の形）> <種の起点 1|11> [newnulls] [nogate]
#   出力＝D:/PoB_work/m1001_work/read/<タグ>/。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m1001; cd $T || exit 9; S=scripts/instruments
TAG=${1:?タグ}; O=${2:?out}; B=${3:?成績表}; SB=${4:-1}; NN=${5:-}; NG=${6:-}
W=${M1001W:-D:/PoB_work/m1001_work}; R=$W/read/$TAG; mkdir -p $R; I=$O/ins; K=D:/PoB_work/keep_988
if [ "$SB" = 11 ]; then BASE=$K/a11; else BASE=D:/PoB_work/keep_994/base_ins; fi
[ -d "$I" ] || { echo "ins が無い：$I"; exit 2; }
if [ "$NG" != nogate ]; then
  python -X utf8 $S/c91sl/gates997.py $I 10 $SB > $R/gates997.txt 2>&1; echo "gates997 rc=$?"
  python -X utf8 $S/c91sl/need997.py $I 10 $SB > $R/need997.txt 2>&1; echo "need997 rc=$?"
fi
bash $S/c78gz/stoplines906.sh $I $R/stop906.txt $SB > $R/stop906.log 2>&1; echo "stop906 rc=$?"
RULE=pair NULLS="measurements/m988_20261109/null/stop906_n1.txt measurements/m988_20261109/null/stop906_n2.txt" bash $S/c84gz/stoplines949.sh $I $R/stop949_m988nulls.txt $SB > $R/stop949_m988nulls.log 2>&1; echo "stop949 rc=$?"
if [ "$NN" = newnulls ]; then
  RULE=pair NULLS="$W/read/n1/stop906.txt $W/read/n2/stop906.txt" bash $S/c84gz/stoplines949.sh $I $R/stop949_newnulls.txt $SB > $R/stop949_newnulls.log 2>&1; echo "stop949new rc=$?"
fi
bash $S/c91rx/stoplines996.sh $I $R/stop996.txt $SB > $R/stop996.log 2>&1; echo "stop996 rc=$?"
python -X utf8 $S/hol/an_h2.py $I -v4 > $R/hol_v4.txt 2>&1; echo "hol rc=$?"
python -X utf8 -I $S/m808/gdp_zero10.py arm=$I base=$BASE > $R/gdp_zero10.txt 2>&1; echo "gdpz rc=$?"
python -X utf8 -I $S/brk693/plvl_seeds.py $I > $R/plvl.txt 2>&1; echo "plvl rc=$?"
python -X utf8 -I $S/m975/ukdef975.py $I $SB > $R/ukdef.txt 2>&1; echo "ukdef rc=$?"
python -X utf8 -I $S/tools/keepsum.py $I $R/keepsum.tsv > $R/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$SB" = 1 ]; then
  python -X utf8 $S/tools/nullgate.py $B measurements/b10_20261109_m988_ins measurements/b10_20261109_m988_n1_ins measurements/b10_20261109_m988_n2_ins > $R/nullgate.txt 2>&1; echo "nullgate rc=$?"
  python -X utf8 -I $S/brk693/home_break.py arm=$I base=$BASE n1=$K/n1 n2=$K/n2 --test arm > $R/home_break.txt 2>&1; echo "home rc=$?"
  bash $S/ng.sh $I $TAG $W/ng_$TAG rd:measurements/ng988_20261109:m988 > $R/ng.txt 2>&1; echo "ng rc=$?"
fi
grep -h "推定" "$B.cost" > $R/cost.txt 2>&1
echo READDONE
