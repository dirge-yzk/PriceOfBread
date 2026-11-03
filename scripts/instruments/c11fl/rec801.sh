#!/usr/bin/env bash
# (805) python が読む道は D:/ の形（/d/… だと Windows の python が C:/d/… と読んで空振りする）。
# (801) c11fl の記録の駆動（読むだけ）。腕 B（旗 1・計器つき 10 種）と基線 m797・ヌル腕 N1・N2 の生を読む。使い方: bash rec801.sh <腕 B の ins フォルダ> <出力ファイル> [腕の名前]
#   ⚠走っている間にこの台本を書き換えない（README ★(778)）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c11fl; S=scripts/instruments
B=${1:?腕 B の ins フォルダ}; OUT=${2:?出力}; NAME=${3:-B}
BASE=D:/PoB_work/keep_797/base_ins; N1=D:/PoB_work/keep_797/n1; N2=D:/PoB_work/keep_797/n2
: > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "divhost_${NAME}（門 U・L・⑦形の一致・⑤同じ盤面の旧新・O 観測の予言）"; python -X utf8 -I $S/c11fl/divhost_read.py $B 10 --other D:/PoB_work/c11fl/inv_income_base.tsv >> $OUT 2>&1
sec "inv_income_${NAME}（門 O の実測＝9 国・投資家の 配当の払い÷それ以外の受け取り）"; python -X utf8 -I $S/r794/inv_income.py $B 10 > D:/PoB_work/c11fl/inv_income_${NAME}.tsv 2>&1
python -X utf8 -I - <<PY >> $OUT
import csv
for k, f in (('${NAME}', 'D:/PoB_work/c11fl/inv_income_${NAME}.tsv'), ('base', 'D:/PoB_work/c11fl/inv_income_base.tsv'), ('n1', 'D:/PoB_work/c11fl/inv_income_n1.tsv'), ('n2', 'D:/PoB_work/c11fl/inv_income_n2.tsv')):
    d = o = 0.0
    for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'):
        if r['nation'] == '9国計' and 1850 <= int(r['dec']) <= 1940: d += -float(r['div']); o += float(r['otherPos'])
    print(f'O {k}: 配当の払い÷それ以外の受け取り（9 国・1850〜1949）= {d / o:.4f}（配当の払い {d:.6g}・受け取り {o:.6g}）')
PY
sec "ukdefault"; for t in "${NAME}:$B" "base:$BASE" "n1:$N1" "n2:$N2"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/c10cp/ukdefault.py $d 1 10 >> $OUT 2>&1; done
sec "plvl（価格水準）"; for t in "${NAME}:$B" "n1:$N1" "n2:$N2" "base:$BASE"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
sec "pxjump（同じ台本で数え直し）"; python -X utf8 -I $S/c11fl/pxjump.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "gdpjump（名目 GDP の 1 四半期の比）"; python -X utf8 -I $S/m797/gdp_jump797.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "hol（空洞化・規則 A2）"; for t in "${NAME}:$B" "base:$BASE"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 $S/hol/an_h2.py $d -v4 >> $OUT 2>&1; done
echo RECDONE >> $OUT
