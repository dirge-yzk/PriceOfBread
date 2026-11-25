#!/usr/bin/env bash
# (1357) c143sp 腕 Z の近似（段 1 の表＋この線の計器・器の木＝旗 0）。
#   ident.sh の段 2（全表 約 30GB）は D: の空き 38.9GB で〈35GB＋量〉を満たせない＝段 1 の表（ident.cfg の SMALL）＋stproc_s・nationgood_s だけを書かせて回す（約 3.5GB）。
#   段は ident の stage.py が段 2 と決める木＝これは〈近似〉と名乗る（段 2 の代わりにしない・合流の腕 Z が全表で読む）。
#   a＝素の 5 種（batch.sh run c143spZa 5）・b＝計器つき 10 種（INS=1 batch.sh run c143spZb 10）を順に回し、比べは identz_cmp.py。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c143sp_work/identz.mark bash scripts/instruments/c143sp/identz_s1.sh
TREE=D:/PoB_work/wt_c143sp; C=30f4e1d8d
export POB_WORK=D:/PoB_work/ident_c143sp
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp; mkdir -p $POB_WORK/tmp
S="$(cd "$(dirname "$0")/.." && pwd)"
SMALL=$(grep '^SMALL=' "$S/ident/ident.cfg" | cut -d= -f2)
cd $TREE || exit 2
[ -z "$(git status --porcelain)" ] && [ "$(git rev-parse --short=9 HEAD)" = "$C" ] || { echo "木が汚れているか HEAD が $C でない"; exit 2; }
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZs1 start $(date '+%F %T') tree=$C Dfree=${FREE}GB armsfree=$(bash $S/tools/arms.sh free)" >> D:/PoB_work/c143sp_work/arms_start.txt
unset POB_INSTRUMENTS_TABLES
bash $S/batch.sh prep c143spZa dir:$TREE plain > $POB_WORK/prep_a.txt 2>&1 || { echo PREPFAIL_a; exit 1; }
bash $S/batch.sh run c143spZa 5 > $POB_WORK/run_a.txt 2>&1; RA=$?
rm -rf $POB_WORK/src_c143spZa
echo "a rc=$RA $(tail -1 $POB_WORK/run_a.txt)"
export POB_INSTRUMENTS_TABLES="$SMALL,stproc_s,nationgood_s"
bash $S/batch.sh prep c143spZb dir:$TREE plain > $POB_WORK/prep_b.txt 2>&1 || { echo PREPFAIL_b; exit 1; }
INS=1 bash $S/batch.sh run c143spZb 10 > $POB_WORK/run_b.txt 2>&1; RB=$?
rm -rf $POB_WORK/src_c143spZb
echo "b rc=$RB $(tail -1 $POB_WORK/run_b.txt)"
echo "identZs1 end $(date '+%F %T') a=$RA b=$RB" >> D:/PoB_work/c143sp_work/arms_start.txt
echo "ZDONE $(date '+%F %T')"
[ $RA = 0 ] && [ $RB = 0 ]
