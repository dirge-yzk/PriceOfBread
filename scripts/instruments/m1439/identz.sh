#!/usr/bin/env bash
# (1439) 合流の腕 Z（ident 段 2・合わせた木から挙動の旗 4 本〔SatReadersToDiscontent・FactionFloorByRegion・FactionStrengthByMilitancy・ReceiveCapByRoom〕を 0 へ戻した使い捨ての木 wt_m1439z）。
#   宣言：rx の tensionwhy_s の Religion 列を消した・sd の facfloor2_s を足した・テストの差 +17（rx 0・tl 0・sd +11・ha +6）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/m1439_work/identz.mark bash scripts/instruments/m1439/identz.sh   （run_in_background で起こす）
unset POB_INSTRUMENTS_TABLES
export TEMP=D:/PoB_work/m1439_work/tmp TMP=D:/PoB_work/m1439_work/tmp
mkdir -p $TEMP
cd D:/PoB_work/wt_m1439z || exit 2
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,2))")
echo "identZ start $(date '+%F %T') tree=$(git rev-parse --short=9 HEAD) Dfree=${FREE}GB armsfree=$(bash scripts/instruments/tools/arms.sh free)" >> D:/PoB_work/m1439_work/arms_start.txt
bash scripts/instruments/ident/ident.sh run m1439z --tree D:/PoB_work/wt_m1439z --work D:/PoB_work/ident_m1439z --saveleak --ng --save 20261128 --stage2 --removed-cols tensionwhy_s:Religion --added-tables facfloor2_s --tests-delta ${TD:-17}
RC=$?
echo "identZ end $(date '+%F %T') rc=$RC" >> D:/PoB_work/m1439_work/arms_start.txt
exit $RC
