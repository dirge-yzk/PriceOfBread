#!/usr/bin/env bash
# (984) c89dd 腕の読み（読むだけ・c88cs/read978.sh の写し）：門は c89dd/gate984.py＋judge984.py（事前登録 measurements/m984_20261109_prereg_dd.txt [1]）・基準は keep_981・NULLS は m981 の n1・n2（measurements/m981_20261109/null/）。
#   記録：開幕の 1000 倍（brk693/plvl_seeds.py）・英の国債の不履行（m975/ukdef975.py）・home_break・門 A′（nullgate）・NG（基準 ng981）・.cost は呼び手が読む。
#   bash read984.sh <タグ> <腕の out フォルダ（D:/ の形・中に ins）> <成績表（measurements の腕の名）> <種の起点 1|11>
#   出力＝D:/PoB_work/c89dd_work/read/<タグ>/。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_c89dd; cd $T || exit 9; S=scripts/instruments
TAG=${1:?タグ}; O=${2:?out}; B=${3:?成績表}; SB=${4:-1}
W=D:/PoB_work/c89dd_work; R=$W/read/$TAG; mkdir -p $R; I=$O/ins; K=D:/PoB_work/keep_981; P=measurements/m984_20261109
if [ "$SB" = 11 ]; then BASE=$K/a11; else BASE=$K/base_ins; fi
[ -d "$I" ] || { echo "ins が無い：$I"; exit 2; }
if [ "$SB" = 1 ]; then
  python -X utf8 -I $S/c89dd/gate984.py $TAG $I > $R/gate984.txt 2> $R/gate984.log; echo "gate rc=$?"
  python -X utf8 -I $S/c89dd/judge984.py $R/gate984.txt $P/paper_base_ins.txt $P/paper_n1.txt $P/paper_n2.txt > $R/judge984.txt 2>&1; echo "judge rc=$?"
fi
bash $S/c78gz/stoplines906.sh $I $R/stop906.txt $SB > $R/stop906.log 2>&1; echo "stop906 rc=$?"
RULE=pair NULLS="measurements/m981_20261109/null/stop906_n1.txt measurements/m981_20261109/null/stop906_n2.txt" bash $S/c84gz/stoplines949.sh $I $R/stop949.txt $SB > $R/stop949.log 2>&1; echo "stop949 rc=$?"
python -X utf8 $S/hol/an_h2.py $I -v4 > $R/hol_v4.txt 2>&1; echo "hol rc=$?"
python -X utf8 -I $S/m808/gdp_zero10.py arm=$I base=$BASE > $R/gdp_zero10.txt 2>&1; echo "gdpz rc=$?"
python -X utf8 -I $S/brk693/plvl_seeds.py $I > $R/plvl.txt 2>&1; echo "plvl rc=$?"
python -X utf8 -I $S/m975/ukdef975.py $I $SB > $R/ukdef.txt 2>&1; echo "ukdef rc=$?"
python -X utf8 -I $S/tools/keepsum.py $I $R/keepsum.tsv > $R/keepsum.log 2>&1; echo "keepsum rc=$?"
if [ "$SB" = 1 ]; then
  python -X utf8 $S/tools/nullgate.py $B measurements/b10_20261109_m981_ins measurements/b10_20261109_m981_n1_ins measurements/b10_20261109_m981_n2_ins > $R/nullgate.txt 2>&1; echo "nullgate rc=$?"
  python -X utf8 -I $S/brk693/home_break.py arm=$I base=$BASE n1=$K/n1 n2=$K/n2 --test arm > $R/home_break.txt 2>&1; echo "home rc=$?"
  bash $S/ng.sh $I $TAG $W/ng_$TAG rd:measurements/ng981_20261109:m981 > $R/ng.txt 2>&1; echo "ng rc=$?"
fi
echo READDONE
