#!/usr/bin/env bash
# (863) c71ip 腕 B：bash armB863.sh <コミット>（旗 ImportPayerLive 既定 1 の木）・計器つき 10 種・種 1〜10・POB_CEARN は既定（cearn_s・cpop_s を書く＝門が読む）。
#   run_in_background で bg.sh の下に置く。終わったら門（gates863.py）・記録（record863.py・peru863.py）・止める線（stoplines863.sh）を回す。
export POB_WORK=D:/PoB_work/c71ip_work/armB PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"; C="$SP/c71ip"
H=${1:?コミット}
mkdir -p $POB_WORK
python -c "import shutil;print('D: の空き（起こす前）', round(shutil.disk_usage('D:/').free/1e9,1), 'GB')"
bash "$SP/tools/arms.sh" free
bash "$SP/batch.sh" prep c71ipB commit:$H plain || exit 1
INS=1 bash "$SP/batch.sh" run c71ipB 10
RC=$?
echo "batch rc=$RC"
rm -rf $POB_WORK/src_c71ipB
I=$POB_WORK/out_c71ipB_10/ins
python -I -X utf8 "$C/gates863.py" $I > $POB_WORK/gates_B.txt 2>&1
python -I -X utf8 "$C/record863.py" D:/PoB_work/keep_852/base_ins $I > $POB_WORK/record_B.txt 2>&1
python -I -X utf8 "$C/peru863.py" $I > $POB_WORK/peru_B.txt 2>&1
bash "$C/stoplines863.sh" $I $POB_WORK/stop_B.txt
python -I -X utf8 "$SP/m839/holclass839.py" $POB_WORK/stop_B.txt D:/PoB_work/wt_c71ip/measurements/m856_20261105/stop_null/stop_M852.txt > $POB_WORK/holclass_B.txt 2>&1
python -c "import shutil;print('D: の空き（終わり）', round(shutil.disk_usage('D:/').free/1e9,1), 'GB')"
exit $RC
