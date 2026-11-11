#!/usr/bin/env bash
# (887) c75ip 腕 S11（(876) の armB876.sh と (882) の s11_882.sh の形・種の起点 11・計器つき 10 種・POB_CEARN は既定＝cearn_s・cpop_s を書く〔門が読む〕）
#   bash s11_887.sh <旗 1 のコミット>   run_in_background で bg.sh の下に置く。⚠起こす前に呼び手も arms.sh free と D: の空きを確かめる。
#   D: の空きが 36GB 未満なら起こさない（exit 9）。終わったらビルドの写し src_* を消し、門（gates887.py）・止める線（stoplines887.sh・holclass839.py・基準 m873 の armS11/stop_mS.txt）・
#   記録（peru887.py・periph887.py・fw887.py〔フォートウィリアム〕・holown880.py〔R880・記録〕・gdp_down875.py〔下向き・記録〕）を回す。生は消さない・縮めない（反証役が読むまで）。
export PYTHONUTF8=1
H=${1:?コミット}
F=$(python -c "import shutil;print(int(shutil.disk_usage('D:/').free/1e9))")
[ "$F" -ge 36 ] || { echo "D: の空き ${F}GB＜床 36GB＝起こさない"; exit 9; }
echo "D: の空き ${F}GB（起こす時）"
export POB_WORK=D:/PoB_work/c75ip_work/s11 POB_REPO=D:/PoB_work/wt_c75ip MEASURE_CACHE=0
mkdir -p /d/PoB_work/c75ip_work/s11
IS=/d/PoB_work/wt_c75ip/scripts/instruments; C=$IS/c75ip; W=$POB_WORK
bash $IS/tools/arms.sh free
bash $IS/batch.sh prep c75ipS commit:$H plain || { echo PREPFAIL; exit 4; }
INS=1 bash $IS/batch.sh run c75ipS 10 11
RC=$?
echo "batch rc=$RC"
rm -rf $W/src_c75ipS
ls $W
I=$W/out_c75ipS_10_s11/ins
[ -d "$I" ] || { echo "ins が無い（$I）"; exit $RC; }
python -I -X utf8 $C/gates887.py $I 11 20 > $W/gates_S.txt 2>&1; echo "gates rc=$?"
bash $C/stoplines887.sh $I $W/stop_S.txt 11
python -I -X utf8 $IS/m839/holclass839.py $W/stop_S.txt D:/PoB_work/wt_c75ip/measurements/m873_20261106/armS11/stop_mS.txt > $W/holclass_S.txt 2>&1; echo "holclass rc=$?"
python -I -X utf8 $IS/c74id/holown880.py $W/stop_S.txt $I $W/holown_S.txt --base D:/PoB_work/wt_c75ip/measurements/m873_20261106/armS11/stop_mS.txt D:/PoB_work/keep_873/a11 > $W/holown_S.log 2>&1; echo "holown rc=$?"
python -I -X utf8 $IS/c73gt/gdp_down875.py S=$I a11=D:/PoB_work/keep_873/a11 > $W/down_S.txt 2>&1; echo "down rc=$?"
python -I -X utf8 $C/peru887.py $I 11 20 > $W/peru_S.txt 2>&1; echo "peru rc=$?"
python -I -X utf8 $C/periph887.py S=$I --seeds 11 20 > $W/periph_S.txt 2>&1; echo "periph rc=$?"
python -I -X utf8 $C/fw887.py フォートウィリアム 11 20 a11=D:/PoB_work/keep_873/a11 S=$I > $W/fw_S.txt 2>&1; echo "fw rc=$?"
python -c "import shutil;print('D: の空き（終わり）', round(shutil.disk_usage('D:/').free/1e9,1), 'GB')"
echo S11DONE
exit $RC
