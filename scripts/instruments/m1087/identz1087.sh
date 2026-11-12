#!/usr/bin/env bash
# (1087) 合流の腕 Z（m1078/identz1078.sh の写し・木を wt_m1087z〔decb＋tr＋st・挙動の線 dm・cr なし＝M の祖先 72f9bf86d〕へ）
#   宣言：tr の ident の宣言と同じ＝足した表 tskpair・tskshadow・列の増えた表 recipeout・テスト +6（tr）・.cost の件数だけの行（em・identz1078 の 3 本）。PRUNE=0・全表。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1087_work/identz.mark bash scripts/instruments/m1087/identz1087.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES; export PRUNE=0
cd D:/PoB_work/wt_m1087z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1087z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1087_work/arms_start.txt
bash D:/PoB_work/wt_m1087z/scripts/instruments/ident/ident.sh run m1087z --tree D:/PoB_work/wt_m1087z --work D:/PoB_work/ident_m1087z --saveleak --tests-delta 6 --ng --save 20261113 --added-tables tskpair,tskshadow --grown-ok recipeout --cost-allow "中身の無い物：,消費：飛ばした,消費：人口 0"
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1087_work/arms_start.txt
exit $RC
