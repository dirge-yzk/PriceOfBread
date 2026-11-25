unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1369_work/tmp TMP=D:/PoB_work/m1369_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1369 || exit 2
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,1))")GB" >> D:/PoB_work/m1369_work/arms_start.txt
PRUNE=0 bash scripts/instruments/ident/ident.sh run m1369z --tree D:/PoB_work/wt_m1369 --work D:/PoB_work/ident_m1369z --saveleak --ng --save 20261126 --stage2 --tests-delta 16 --rewritten-tables cwend_s@scripts/instruments/c144cf/cwend_check.py
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1369_work/arms_start.txt
exit $RC
