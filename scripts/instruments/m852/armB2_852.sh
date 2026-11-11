#!/usr/bin/env bash
# (852) 合流が線 c69eh の腕 B2 を線の事前登録（measurements/m848_20261105_prereg_eh.txt §12）のまま回す：木 ed3b404b（旗 EmptyPopEstate 既定 1）・計器つき 10 種・種 1〜10。
#   bash armB2_852.sh   （run_in_background で bg.sh の下に置く）
export POB_WORK=D:/PoB_work/c69eh_armB2
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
python -c "import shutil;print('D: の空き（起こす前）', round(shutil.disk_usage('D:/').free/1e9,1), 'GB')"
bash "$SP/tools/arms.sh" free
bash "$SP/batch.sh" prep c69ehB commit:ed3b404b plain || exit 1
INS=1 bash "$SP/batch.sh" run c69ehB 10
RC=$?
python -c "import shutil;print('D: の空き（終わり）', round(shutil.disk_usage('D:/').free/1e9,1), 'GB')"
exit $RC
