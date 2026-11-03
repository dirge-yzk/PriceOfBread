#!/usr/bin/env bash
# (810) c13fl の記録と門の駆動（読むだけ）。腕（旗 1・計器つき）と基線 m797（keep_808/base_ins）・ヌル腕 N1・N2（keep_797/n1・n2）の生を読む。
#   使い方: bash rec810.sh <腕の ins フォルダ> <出力ファイル> <腕の名前> <種 1-10 か 1,2> <旗 0 の ins（G6 の比べる側）>
#   python が読む道は D:/ の形。走っている間にこの台本を書き換えない（README ★(778)）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c13fl; S=scripts/instruments; W=D:/PoB_work/c13fl_work
B=${1:?腕の ins}; OUT=${2:?出力}; NAME=${3:?名前}; SD=${4:?種}; Z=${5:?旗 0 の ins}
BASE=D:/PoB_work/keep_808/base_ins; N1=D:/PoB_work/keep_797/n1; N2=D:/PoB_work/keep_797/n2
NS=$(python -c "s='$SD'; print(len(range(int(s.split('-')[0]),int(s.split('-')[1])+1)) if '-' in s else len(s.split(',')))")
: > $OUT
sec(){ echo "## ===== $1" >> $OUT; }
sec "gate810（G2〜G6）"; python -X utf8 -I $S/c13fl/gate810.py $B $SD --base $Z $SD >> $OUT 2>&1
sec "gate810 旗 0 の盤面（同じ読み手＝落ちる門の確かめ）"; python -X utf8 -I $S/c13fl/gate810.py $Z $SD --base $Z $SD >> $OUT 2>&1
sec "G1 O（9 国・1850〜1949・inv_income）"
python -X utf8 -I $S/r794/inv_income.py $B $NS > $W/inv_income_${NAME}.tsv 2>&1
[ -f $W/inv_income_n1.tsv ] || python -X utf8 -I $S/r794/inv_income.py $N1 10 > $W/inv_income_n1.tsv 2>&1
[ -f $W/inv_income_n2.tsv ] || python -X utf8 -I $S/r794/inv_income.py $N2 10 > $W/inv_income_n2.tsv 2>&1
python -X utf8 -I - <<PY >> $OUT
import csv
for k, f in (('${NAME}', 'D:/PoB_work/c13fl_work/inv_income_${NAME}.tsv'), ('base', 'D:/PoB_work/wt_c13fl/measurements/m810_20261103/inv_income_base.tsv'), ('n1', 'D:/PoB_work/c13fl_work/inv_income_n1.tsv'), ('n2', 'D:/PoB_work/c13fl_work/inv_income_n2.tsv')):
    d = o = 0.0
    for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'):
        if r['nation'] == '9国計' and 1850 <= int(r['dec']) <= 1940: d += -float(r['div']); o += float(r['otherPos'])
    print(f'O {k}: 配当の払い÷それ以外の受け取り（9 国・1850〜1949）= {d / o:.4f}（配当の払い {d:.6g}・受け取り {o:.6g}）{"  → G1 " + ("通る" if d / o <= 0.5 else "落ちる") if k == "${NAME}" else ""}')
PY
sec "G7 名目 GDP 0 の続き（gdp_zero10）"; python -X utf8 -I $S/m808/gdp_zero10.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "止める線：価格水準の 1 四半期の比（pxjump・同じ台本で数え直し）"; python -X utf8 -I $S/c11fl/pxjump.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "止める線：名目 GDP の 1 四半期の比（gdp_jump797）"; python -X utf8 -I $S/m797/gdp_jump797.py ${NAME}=$B base=$BASE n1=$N1 n2=$N2 >> $OUT 2>&1
sec "記録：開幕の 1000 倍以上（plvl_seeds）"; for t in "${NAME}:$B" "base:$BASE" "n1:$N1" "n2:$N2"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/brk693/plvl_seeds.py $d >> $OUT 2>&1; done
sec "記録：英の国債の不履行（ukdefault）"; for t in "${NAME}:$B" "base:$BASE" "n1:$N1" "n2:$N2"; do n=${t%%:*}; d=${t#*:}; echo "-- $n" >> $OUT; python -X utf8 -I $S/c10cp/ukdefault.py $d 1 $NS >> $OUT 2>&1; done
sec "止める線：空洞化（an_h2 -v4・規則 A2）"; python -X utf8 $S/hol/an_h2.py $B -v4 >> $OUT 2>&1
echo RECDONE >> $OUT
