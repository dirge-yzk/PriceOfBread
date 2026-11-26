#!/usr/bin/env bash
# (1378) c146bk 腕 Z（ident・器の木＝旗 0・木 04ace0ab3 を別の worktree で＝㉙）。宣言：テストの差 +6（BankruptReentryShortageTests）。
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/c146bk_work/tmp TMP=D:/PoB_work/c146bk_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_c146bk_z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/c146bk_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run c146bkz --tree D:/PoB_work/wt_c146bk_z --work D:/PoB_work/ident_c146bk --saveleak --ng --save 20261126 --tests-delta 6
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/c146bk_work/arms_start.txt
exit $RC
