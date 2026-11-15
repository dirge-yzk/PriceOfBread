#!/usr/bin/env bash
# (1164) 合流の腕 Z（m1157/identz1157.sh の写し・木を wt_m1164z〔合わせた木 61de637fc＋MalariaQuininePerColony を 6.0 へ戻す 1 コミット 01f659872・push しない〕へ）
#   宣言：表が増えた t1buy_s（t1）・colqn_s（qn）・列が消えた region_s の 2 列（sr）・tb_s の 18 列（t1 の単位の直し＝t1 の宣言と同じ）・.cost の清算：A-1 の 2 行（t1）・テストの差 +15（t1 10・qn 5）・段 2・PRUNE は既定。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1164_work/identz.mark bash scripts/instruments/m1164/identz1164.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1164_work/tmp TMP=D:/PoB_work/m1164_work/tmp
cd D:/PoB_work/wt_m1164z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=8 HEAD) Dfree=${FREE}GB armsfree=$(bash D:/PoB_work/wt_m1164z/scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1164_work/arms_start.txt
bash D:/PoB_work/wt_m1164z/scripts/instruments/ident/ident.sh run m1164z --tree D:/PoB_work/wt_m1164z --work D:/PoB_work/ident_m1164z --saveleak ${NOINS1164:+--no-ins} --tests-delta 15 --ng --save 20261115 --stage2 --added-tables t1buy_s,colqn_s --removed-cols region_s:settlerOutKPerQuarter,region_s:settlerInKPerQuarter --known-extra tb_s:sk1,tb_s:sk2,tb_s:ps1,tb_s:ps2,tb_s:dsk1,tb_s:dsk2,tb_s:dps1,tb_s:dps2,tb_s:a1Close,tb_s:a1CloseD,tb_s:evalDone,tb_s:u1Rel,tb_s:u1Abs,tb_s:u1SmallDRel,tb_s:u1MaxRel,tb_s:u1Gt1pct,tb_s:u1Gt10pct,tb_s:u1ExcessQty --cost-allow 清算：A-1
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1164_work/arms_start.txt
exit $RC
