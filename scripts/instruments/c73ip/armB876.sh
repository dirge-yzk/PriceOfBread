#!/usr/bin/env bash
# (876) c73ip 腕 B（(869) c72ip の armB869.sh の写し・木と作業フォルダを c73ip へ・旧の基準を keep_873/base_ins と m873 の stop_mM.txt へ・python に -X utf8）
#   bash armB876.sh <コミット>（旗 ImportPayerLive 既定 1 の木）・計器つき 10 種・種 1〜10・POB_CEARN は既定（cearn_s・cpop_s を書く＝門が読む）。
#   run_in_background で bg.sh の下に置く。終わったら門（gates876.py）・記録（record876.py・peru876.py）・止める線（stoplines866.sh）・振り分け（holclass839.py）を回す。
export POB_WORK=D:/PoB_work/c73ip_work/armB PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"; C="$SP/c73ip"
H=${1:?コミット}
python -c "import shutil,sys;f=shutil.disk_usage('D:/').free/1e9;print('D: の空き（起こす前）', round(f,1), 'GB');sys.exit(0 if f>=36 else 5)" || { echo "D: の空きが 36GB 未満＝腕 B を起こさない"; exit 5; }
mkdir -p $POB_WORK
bash "$SP/tools/arms.sh" free
bash "$SP/batch.sh" prep c73ipB commit:$H plain || exit 1
INS=1 bash "$SP/batch.sh" run c73ipB 10
RC=$?
echo "batch rc=$RC"
rm -rf $POB_WORK/src_c73ipB
I=$POB_WORK/out_c73ipB_10/ins
python -I -X utf8 "$C/gates876.py" $I > $POB_WORK/gates_B.txt 2>&1
python -I -X utf8 "$C/record876.py" D:/PoB_work/keep_873/base_ins $I > $POB_WORK/record_B.txt 2>&1
python -I -X utf8 "$C/peru876.py" $I > $POB_WORK/peru_B.txt 2>&1
bash "$SP/m866/stoplines866.sh" $I $POB_WORK/stop_B.txt
python -I -X utf8 "$SP/m839/holclass839.py" $POB_WORK/stop_B.txt D:/PoB_work/wt_c73ip/measurements/m873_20261106/armM/stop_mM.txt > $POB_WORK/holclass_B.txt 2>&1
python -c "import shutil;print('D: の空き（終わり）', round(shutil.disk_usage('D:/').free/1e9,1), 'GB')"
exit $RC
