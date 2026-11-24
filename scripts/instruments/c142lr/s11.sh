#!/usr/bin/env bash
# (1350) c142lr 単独の S11（旗 1 の木・計器つき・種 11〜20・表は keep_1341/a11 と同じ 82 表）と読み手（止める線・空洞化・英の不履行・lr_count・bham）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c142lr_work/s11.mark bash scripts/instruments/c142lr/s11.sh <コミット> <作業フォルダ（D:/…）>   （run_in_background で起こす）
C=${1:?コミット}; export POB_WORK=${2:?作業フォルダ}
cd "$(dirname "$0")/../../.."; R=$(pwd); S=$R/scripts/instruments; D0=$S/c142lr
export TEMP=$POB_WORK/tmp TMP=$POB_WORK/tmp PYTHONUTF8=1 PYTHONIOENCODING=utf-8; mkdir -p $POB_WORK/tmp
export POB_INSTRUMENTS_TABLES=$(ls D:/PoB_work/keep_1341/a11 | grep -E '_s11\.tsv$' | sed -E 's/_s11\.tsv$//' | sort -u | paste -sd, -)
[ -z "$(git status --porcelain)" ] && [ "$(git rev-parse HEAD)" = "$(git rev-parse $C)" ] || { echo "木が汚れているか HEAD がコミットと違う"; exit 1; }
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "S11 start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash $S/tools/arms.sh free) tables=$(echo $POB_INSTRUMENTS_TABLES | tr ',' '\n' | wc -l)" >> D:/PoB_work/c142lr_work/arms_start.txt
bash $S/batch.sh prep c142lrS dir:$R plain > $POB_WORK/prep_S.txt 2>&1 || { echo "PREPFAIL"; exit 1; }
INS=1 bash $S/batch.sh run c142lrS 10 11 > $POB_WORK/run_S.txt 2>&1; RC=$?
echo "S11 rc=$RC $(tail -1 $POB_WORK/run_S.txt)"
rm -rf $POB_WORK/src_c142lrS
WD=$(cd $POB_WORK && pwd -W); I=$(ls -d $WD/out_c142lrS_*/ins | head -1); O=$WD/read; mkdir -p $O
bash "$S/c78gz/stoplines906.sh" "$I" "$O/stop906.txt" 11 > "$O/stop906.log" 2>&1; echo "stop906 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$I" -v4 > "$O/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 "$S/hol/an_h2.py" D:/PoB_work/keep_1341/a11 -v4 > "$O/hol_v4_a11.txt" 2>&1; echo "hol a11 rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$I base=D:/PoB_work/keep_1341/a11 > "$O/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$I" 11 > "$O/ukdef.txt" 2>&1; echo "ukdef rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" D:/PoB_work/keep_1341/a11 11 > "$O/ukdef_a11.txt" 2>&1; echo "ukdef a11 rc=$?"
python -X utf8 -I "$D0/lr_count.py" "$I" > "$O/lrcount.txt" 2>&1; echo "lrcount rc=$?"
python -X utf8 -I "$D0/bham.py" "$I" > "$O/bham.txt" 2>&1; python -X utf8 -I "$D0/bham.py" D:/PoB_work/keep_1341/a11 > "$O/bham_a11.txt" 2>&1; echo "bham rc=$?"
echo "S11DONE $(date '+%F %T')"
exit $RC
