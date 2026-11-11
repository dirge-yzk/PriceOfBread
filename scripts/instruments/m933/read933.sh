#!/usr/bin/env bash
# (933) 合流の腕の読み（読むだけ）。線の門の台本は 1 字も変えずに呼ぶ（c81cw＝c80cw/gates922.py・tab922.py〔reads929.sh の呼び方〕・c81la＝c81la/gate930.py・tab930.py〔reads930.sh の呼び方・S11 の tab は BASE1 を渡す〕）。
#   bash read933.sh <タグ> <腕の ins（D:/ の形）> <成績表（D:/ の形）> <種の起点 1|11> [ng]
#   出力＝D:/PoB_work/m933/read/<タグ>/。基準は keep_919（線の門の基準と同じ盤面）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_m933; cd $T || exit 9; S=scripts/instruments; K=D:/PoB_work/keep_919
TAG=${1:?タグ}; I=${2:?ins}; B=${3:?成績表}; SB=${4:-1}; NG=${5:-}
R=D:/PoB_work/m933/read/$TAG; mkdir -p $R
if [ "$SB" = 1 ]; then
  python -X utf8 -I $S/c80cw/gates922.py $I $K/base_ins $K/base_ins $K/n1 $K/n2 > $R/cw_gates.txt 2>&1; echo "cwgates rc=$?"
  python -X utf8 -I $S/c80cw/tab922.py $K/base_ins arm=$I n1=$K/n1 n2=$K/n2 > $R/cw_tab.txt 2>&1; echo "cwtab rc=$?"
  SEED_START=1 python -X utf8 $S/c81la/gate930.py gate $I $K/base_ins $K/n1 $K/n2 > $R/la_gate.txt 2>&1; echo "lagate rc=$?"
  python -X utf8 $S/c81la/tab930.py $I $K/base_ins $K/n1 $K/n2 1 > $R/la_tab.txt 2>&1; echo "latab rc=$?"
  python -X utf8 -I $S/tools/nullgate.py $B measurements/b10_20261107_m919_ins measurements/b10_20261107_m919_n1_ins measurements/b10_20261107_m919_n2_ins > $R/nullgate.txt 2>&1; echo "nullgate rc=$?"
  python -X utf8 -I $S/brk693/home_break.py arm=$I base=$K/base_ins n1=$K/n1 n2=$K/n2 --test arm > $R/home_break.txt 2>&1; echo "home rc=$?"
else
  SB=11 python -X utf8 -I $S/c80cw/gates922.py $I $K/a11 $K/base_ins $K/n1 $K/n2 > $R/cw_gates.txt 2>&1; echo "cwgates rc=$?"
  SB=11 python -X utf8 -I $S/c80cw/tab922.py $K/a11 arm=$I > $R/cw_tab.txt 2>&1; echo "cwtab rc=$?"
  SEED_START=11 python -X utf8 $S/c81la/gate930.py gate $I $K/a11 $K/n1 $K/n2 > $R/la_gate.txt 2>&1; echo "lagate rc=$?"
  BASE1=$K/base_ins python -X utf8 $S/c81la/tab930.py $I $K/a11 $K/n1 $K/n2 11 > $R/la_tab.txt 2>&1; echo "latab rc=$?"
  python -X utf8 -I $S/brk693/home_break.py arm=$I a11=$K/a11 --test arm > $R/home_break.txt 2>&1; echo "home rc=$?"
fi
echo GATESDONE
bash $S/c78gz/stoplines906.sh $I $R/stop906.txt $SB; echo "stop rc=$?"
python -X utf8 $S/hol/an_h2.py $I -v4 > $R/hol_v4.txt 2>&1; echo "hol rc=$?"
python -X utf8 -I $S/tools/keepsum.py $I $R/keepsum.tsv > $R/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$NG" = ng ]; then bash $S/ng.sh $I $TAG D:/PoB_work/m933/ng_$TAG rd:measurements/ng919_20261107:m919 > $R/ng.txt 2>&1; echo "ng rc=$?"; fi
echo READDONE
